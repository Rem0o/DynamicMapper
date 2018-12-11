using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace DynamicMapper.Test
{

    public class DataReaderMapperTest
    {
        private Func<bool> ReadCreator(int i)
        {
            int c = i;
            return () => c-- > 0;
        }

        private readonly Dictionary<Type, object> dic = new Dictionary<Type, object>()
            {
                { typeof(Guid), Guid.NewGuid() },
                { typeof(string), "Hola" },
                { typeof(DateTime), DateTime.Now },
                { typeof(int?), null },
                { typeof(long), 123_456_789_111 },
                { typeof(bool), false },
            };

        private Func<object> MapperCreator(Type t) => () => dic.GetValueOrDefault(t);

        private IDataReader GetDataReaderMock()
        {
            var mock = new Mock<IDataReader>();

            mock.Setup(dr => dr.Read()).Returns(ReadCreator(1000));
            foreach (var p in typeof(POCO).GetProperties())
                mock.Setup(dr => dr[p.Name]).Returns(MapperCreator(p.PropertyType));

            return mock.Object;
        }

        private IDynamicMapperContainer<IDataReader> GetContainer() => new DynamicMapperContainer<IDataReader>(
            (reader, propertyName) => reader[propertyName])
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

            var dataReader = GetDataReaderMock();

            var list = new List<POCO>();
            while (dataReader.Read())
            {
                POCO poco = new POCO();
                mapper(dataReader, poco);
                list.Add(poco);
            }

            foreach (var prop in typeof(POCO).GetProperties())
                Assert.True(list.TrueForAll(poco => prop.GetValue(poco)?.Equals(dic[prop.PropertyType]) ?? dic[prop.PropertyType] == null),
                    $"Property {prop.Name} of type {prop.PropertyType.Name} was not mapped correctly.");
        }

        [Fact]
        public void ManualMapping_SameOutput()
        {
            var dataReader = GetDataReaderMock();
            var manualList = new List<POCO>();
            while (dataReader.Read())
            {
                manualList.Add(new POCO()
                {
                    Id = (Guid)dataReader["Id"],
                    Name = (string)dataReader["Name"],
                    Date = (DateTime)dataReader["Date"],
                    Count = (int?)dataReader["Count"],
                    Long = (long)dataReader["Long"],
                    Question = (bool)dataReader["Question"]
                });
            }

            dataReader = GetDataReaderMock();
            var dynamicList = new List<POCO>();
            var container = GetContainer();
            container.GetMapper<POCO>(out var mapper);
            while (dataReader.Read())
            {
                var poco = new POCO();
                mapper(dataReader, poco);
                dynamicList.Add(poco);
            }

            Assert.Equal(manualList.Count, dynamicList.Count);
            Assert.True(dynamicList.Zip(manualList, (a, b) => a.Equals(b)).All(x => x), "All items in both lists should be the same.");
            
        }
    }
}
