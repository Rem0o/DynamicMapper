using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicMapper
{
    public class DynamicMapperContainer<T> : IDynamicMapperContainer<T>
    {
        private readonly Dictionary<Type, object> _dictionnary = new Dictionary<Type, object>();
        private readonly Func<string, string, Func<PropertyInfo, string>> GetSinglePropertyCodeDelegate;
        private readonly Globals<T> _globals;
     
        public DynamicMapperContainer(Expression<Func<T, string, object>> mapExpression)
        {
            _globals = new Globals<T>();
            var propertyFunctionMap = InitPropertyFunctionMap(mapExpression);
            GetSinglePropertyCodeDelegate = 
                (source, target) => propertyInfo => 
                $"{target}.{propertyInfo.Name} = ({GetPropertyTypeString(propertyInfo.PropertyType)}){source}{propertyFunctionMap(propertyInfo.Name)};";
        }

        public DynamicMapperContainer(Action<T, string, object> singlePropertyAction)
        {
            _globals = new Globals<T> { Action = singlePropertyAction };
            var delName = nameof(_globals.Action);
            GetSinglePropertyCodeDelegate = 
                (source, target) => propertyInfo => 
                $"{delName}({source}, \"{propertyInfo.Name}\", {target}.{propertyInfo.Name});";
        }

        public IDynamicMapperContainer<T> CompileMappers(params Type[] types)
        {
            // [PART 1] Generate the code as string       
            var containerName = "dictionary";
            var varPrefix = "mapper";
            // declare a new empty dictionary
            var mapperContainerCode = $"var {containerName} = new " + GetFriendlyTypeName(typeof(Dictionary<Type, object>)) + "();";
            // for each type, create a mapping Action and add it into the dictionary
            var mapperCreationCode = types.Select(MapperCodeGeneratorFactory(varPrefix));
            var addMapperToContainerCode = types.Select(AddIntoContainerCodeGeneratorFactory(varPrefix, containerName));
            // return the dictionary
            var returnCode = $"return {containerName};";

            // [PART 2] Evaluate the generated code
            var code = mapperContainerCode + mapperCreationCode.Concat(addMapperToContainerCode).Aggregate(string.Concat) + returnCode;
            var options = GetScriptOptions(types.Select(t => t.Assembly).Distinct().ToArray());
            var task = CSharpScript.EvaluateAsync<Dictionary<Type, object>>(code, options, _globals );
            task.Wait();

            // [PART 3] Get the generated code evaluation result       
            foreach (var kv in task.Result)
                _dictionnary.Add(kv.Key, kv.Value);

            return this;
        }

        public IDynamicMapperContainer<T> CompileMappers(Assembly assembly, Func<Type, bool> typeFilter)
        {
            var types = assembly.GetTypes()
                .Where(typeFilter)
                .ToArray();

            return CompileMappers(types);
        }

        private Func<string, string> InitPropertyFunctionMap(Expression<Func<T, string, object>> mapExpression)
        {
            var methodCall = mapExpression.Body as MethodCallExpression;
            var method = methodCall?.Method;

            var parameters = method.GetParameters().ToList();

            if (parameters.Count != 1 || parameters.FirstOrDefault().ParameterType != typeof(string))
            {
                throw new Exception("The mapping expression should be a lambda containing a single function that takes a single string parameter.");
            }

            var methodName = methodCall?.Method.Name;

            if (methodName.ToLower() == "get_item".ToLower())
                return propertyName => $"[\"{propertyName}\"]";
            else
                return propertyName => $".{methodName}(\"{propertyName}\")";
        }


        public IDynamicMapperContainer<T> GetMapper<U>(out Action<T, U> mapper)
        {
            mapper = null;

            var success = _dictionnary.TryGetValue(typeof(U), out var m);
            if (success)
                mapper = (Action<T, U>)m;
            else
            {
                var type = typeof(U);
                _dictionnary.Add(typeof(U), GetSingleMapper<U>());
                return GetMapper(out mapper);
            }

            return this;
        }

        private ScriptOptions GetScriptOptions(params Assembly[] assemblies)
        {
            return ScriptOptions.Default.WithImports(
                typeof(object).Namespace,
                typeof(Dictionary<string, object>).Namespace,
                typeof(T).Namespace)
                .WithReferences(assemblies);
        }

        private string GetFriendlyTypeName(Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyTypeName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        private Func<Type, int, string> MapperCodeGeneratorFactory(string varPrefix)
        {
            return (type, index) =>
            {
                var expression = GetMapperExpression(type);
                return $"{typeof(Action).Name}<{GetFriendlyTypeName(typeof(T))}" +
                    $", {type.FullName}> {varPrefix}{index} = {expression} ;";
            };
        }

        private string GetMapperExpression(Type type)
        {
            var sourceParam = "source";
            var targetParam = "target";

            var properties = type.GetProperties().Where(p => p.CanWrite);
            var code = properties
                .Select(GetSinglePropertyCodeDelegate(sourceParam, targetParam))
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

        private Action<T, U> GetSingleMapper<U>()
        {
            var type = typeof(U);
            var task = CSharpScript.EvaluateAsync<Action<T, U>>(GetMapperExpression(type), GetScriptOptions(type.Assembly), _globals );
            task.Wait();
            return task.Result;
        }
    }
}
