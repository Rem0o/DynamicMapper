using System;
using System.Data;

namespace DynamicMapper
{
    public interface IDynamicSqlMapperContainer
    {
        bool TryGetMapper<T>(out Action<IDataReader, T> mapper);
    }
}