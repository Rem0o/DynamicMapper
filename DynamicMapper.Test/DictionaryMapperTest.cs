using System;
using System.Collections.Generic;
using Xunit;

namespace DynamicMapper.Test
{

    public class DictionaryMapperTest
    {
        private static POCO Dummy = null;
        private readonly Dictionary<string, object> dic = new Dictionary<string, object>()
            {
                { nameof(Dummy.Id), Guid.NewGuid() },
                { nameof(Dummy.Name) , "Hola" },
                { nameof(Dummy.Date), DateTime.Now },
                { nameof(Dummy.Count), null },
                { nameof(Dummy.Long), 123_456_789_111 },
                { nameof(Dummy.Question), false },
            };

        private IDynamicMapperContainer<IDictionary<string, object>> GetContainer() => new DynamicMapperContainer<IDictionary<string, object>>(
            (dic, propertyName) => dic[propertyName])
            .CompileMappers(new Type[] { typeof(POCO) });

        [Fact]
        public void ContainerCreation_GetMapper_MapperExist()
        {
            var container = GetContainer()
                .GetMapper<POCO>(out var mapper);

            Assert.NotNull(mapper);
        }

        [Fact]
        public void Mapper_MapObject_AllPropertiesMapped()
        {
            var container = GetContainer()
                .GetMapper<POCO>(out var mapper);

            var poco = new POCO();
            mapper(dic, poco);

            foreach (var prop in typeof(POCO).GetProperties())
                Assert.True(prop.GetValue(poco)?.Equals(dic[prop.Name]) ?? dic[prop.Name] == null,
                    $"Property {prop.Name} of type {prop.PropertyType.Name} was not mapped correctly.");
        }
    }
}
