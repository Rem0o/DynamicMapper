using System;
using System.Linq;

namespace DynamicMapper.Test
{
    public class POCO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public int? Count { get; set; }
        public long Long { get; set; }
        public bool Question { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is POCO casted)
                return this.GetType()
                    .GetProperties()
                    .All(prop => prop.GetValue(this)?.Equals(prop.GetValue(obj)) ?? prop.GetValue(obj) == null);

            return false;
        }
    }
}
