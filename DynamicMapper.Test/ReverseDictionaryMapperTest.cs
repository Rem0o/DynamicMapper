using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace DynamicMapper.Test
{
    public class ReverseDictionaryMapperTest
    {
        private void SetPropertyInDictionary(IDictionary dictionary, string propertyName, object @object)
        {
            dictionary[propertyName] = @object;
        }

        [Fact]
        public void ContainerCreation_GetMapper_MapperExist()
        {
            var container = new DynamicMapperContainer<IDictionary>(SetPropertyInDictionary)
                .GetMapper<POCO>(out var mapper);

            Assert.NotNull(mapper);
        }

        [Fact]
        public void Mapper_MapObject_AllPropertiesMapped()
        {
            var container = new DynamicMapperContainer<IDictionary>(SetPropertyInDictionary)
                .CompileMappers( typeof(POCO) );

            var dictionary = new Dictionary<string, object>();
            var poco = new POCO
            {
                Count = 1,
                Date = DateTime.Now,
                Long = 5,
                Name = "hi",
                Id = Guid.NewGuid(),
                Question = true
            };

            var mapper = container.GetMapper<POCO>(out var action);

            action(dictionary, poco);

            foreach (var prop in typeof(POCO).GetProperties())
                Assert.True(prop.GetValue(poco)?.Equals(dictionary[prop.Name]) ?? dictionary[prop.Name] == null,
                    $"Property {prop.Name} of type {prop.PropertyType.Name} was not mapped correctly.");
        }
    }
}
