using System;
using System.Reflection;

namespace DynamicMapper
{
    public interface IDynamicMapperContainer<T>
    {
        IDynamicMapperContainer<T> CompileMappers(Type[] types);
        IDynamicMapperContainer<T> CompileMappers(Assembly assembly, Func<Type, bool> typeFilter);
        IDynamicMapperContainer<T> GetMapper<U>(out Action<T, U> mapper);
    }
}