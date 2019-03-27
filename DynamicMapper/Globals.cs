using System;

namespace DynamicMapper
{
    public class Globals<T>
    {
        public Action<T, string, object> Action;
    }
}
