using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DynamicMapper
{
    public class DynamicSqlMapperContainer : IDynamicSqlMapperContainer
    {
        private readonly Dictionary<Type, object> _dictionnary = new Dictionary<Type, object>();

        public DynamicSqlMapperContainer(Assembly assembly, Func<Type, bool> typeFilter) : this(assembly.GetTypes().Where(typeFilter).ToArray()) { }

        public DynamicSqlMapperContainer(Type[] types)
        {
            // [PART 1] Generate the code as string       
            var containerName = "dictionary";
            var varPrefix = "mapper";
            // declare a new empty dictionary
            var mapperContainerCode = $"var {containerName} = new {typeof(Dictionary<Type, object>).Name.Replace("`2", "")}"
                + $"<{typeof(Type).Name}, {typeof(object).Name}>();";
            // for each type, create a mapping Action and add it into the dictionary
            var mapperCreationCode = types.Select(MapperCodeGeneratorFactory(varPrefix));
            var addMapperToContainerCode = types.Select(AddIntoContainerCodeGeneratorFactory(varPrefix, containerName));
            // return the dictionary
            var returnCode = $"return {containerName};";

            // [PART 2] Evaluate the generated code
            var code = mapperContainerCode + mapperCreationCode.Concat(addMapperToContainerCode).Aggregate(string.Concat) + returnCode;
            var options = GetScriptOptions(types.Select(t => t.Assembly).Distinct().ToArray());
            var task = CSharpScript.EvaluateAsync<Dictionary<Type, object>>(code, options);
            task.Wait();

            // [PART 3] Get the generated code evaluation result       
            _dictionnary = task.Result;
        }

        public bool TryGetMapper<T>(out Action<IDataReader, T> mapper)
        {
            mapper = null;

            var success = _dictionnary.TryGetValue(typeof(T), out var m);
            if (success)
                mapper = (Action<IDataReader, T>)m;
            else
            {
                var type = typeof(T);
                if (_dictionnary.ContainsKey(type))
                    return false;
                else
                {
                    _dictionnary.Add(typeof(T), GetSingleMapper<T>());
                    return TryGetMapper(out mapper);
                }
            }

            return success;
        }

        private ScriptOptions GetScriptOptions(params Assembly[] assemblies)
        {
            return ScriptOptions.Default.WithImports(
                typeof(object).Namespace, 
                typeof(Dictionary<string, object>).Namespace, 
                typeof(IDataReader).Namespace).WithReferences(assemblies);
        }

        private Func<Type, int, string> MapperCodeGeneratorFactory(string varPrefix)
        {
            return (type, index) =>
            {
                var expression = GetMapperExpression(type);
                return $"{typeof(Action).Name}<{typeof(IDataReader)}" +
                    $", {type.FullName}> {varPrefix}{index} = {expression} ;";
            };
        }

        private string GetMapperExpression(Type type)
        {
            var sourceParam = "source";
            var targetParam = "target";

            var properties = type.GetProperties().Where(p => p.CanWrite);
            var code = properties
                .Select(p => $"{targetParam}.{p.Name} = ({GetPropertyTypeString(p.PropertyType)}){sourceParam}[\"{p.Name}\"];")
                .Aggregate(string.Concat);

            return $"({sourceParam}, {targetParam}) => {{ {code} }}";
        }

        private string GetPropertyTypeString(Type propertyType)
        {
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return $"{typeof(Nullable).Name}<{propertyType.GetGenericArguments()[0]}>";
            else
                return propertyType.ToString();
        }

        private Func<Type, int, string> AddIntoContainerCodeGeneratorFactory(string varPrefix, string containerName)
        {
            Dictionary<Type, object> dummy = null;
            return (type, index) => $"{containerName}.{nameof(dummy.Add)}(typeof({type.FullName}), {varPrefix}{index});";
        }

        private Action<IDataReader, T> GetSingleMapper<T>()
        {
            var type = typeof(T);
            var task = CSharpScript.EvaluateAsync<Action<IDataReader, T>>(GetMapperExpression(type), GetScriptOptions(type.Assembly));
            task.Wait();
            return task.Result;
        }
    }
}
