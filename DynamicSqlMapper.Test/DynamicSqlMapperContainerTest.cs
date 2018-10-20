using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

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

    public class DynamicSqlMapperContainerTest
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

        [Fact]
        public void ContainerCreation_TryGetMapper_MapperExist()
        {
            var container = new DynamicSqlMapperContainer(new Type[] { typeof(POCO) });
            Assert.True(container.TryGetMapper<POCO>(out var mapper));
        }

        [Fact]
        public void Mapper_MapObject_AllPropertiesMapped()
        {
            var container = new DynamicSqlMapperContainer(new Type[] { typeof(POCO) });
            container.TryGetMapper<POCO>(out var mapper);

            var reader = GetDataReaderMock();

            var list = new List<POCO>();
            while (reader.Read())
            {
                POCO poco = new POCO();
                mapper(reader, poco);
                list.Add(poco);
            }

            foreach (var prop in typeof(POCO).GetProperties())
                Assert.True(list.TrueForAll(poco => prop.GetValue(poco)?.Equals(dic[prop.PropertyType]) ?? dic[prop.PropertyType] == null),
                    $"Property {prop.Name} of type {prop.PropertyType.Name} was not mapped correctly.");
        }

        [Fact]
        public void ManualMapping_SameOutput()
        {
            var reader = GetDataReaderMock();
            var manualList = new List<POCO>();
            while (reader.Read())
            {
                manualList.Add(new POCO()
                {
                    Id = (Guid)reader["Id"],
                    Name = (string)reader["Name"],
                    Date = (DateTime)reader["Date"],
                    Count = (int?)reader["Count"],
                    Long = (long)reader["Long"],
                    Question = (bool)reader["Question"]
                });
            }

            reader = GetDataReaderMock();
            var dynamicList = new List<POCO>();
            var container = new DynamicSqlMapperContainer(new Type[] { typeof(POCO) });
            container.TryGetMapper<POCO>(out var mapper);
            while (reader.Read())
            {
                var poco = new POCO();
                mapper(reader, poco);
                dynamicList.Add(poco);
            }

            Assert.Equal(manualList.Count, dynamicList.Count);
            Assert.True(dynamicList.Zip(manualList, (a, b) => a.Equals(b)).All(x => x), "All items in both lists should be the same.");
            
        }
    }
}
