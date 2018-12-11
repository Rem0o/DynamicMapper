using System;
using System.Data;
using Xunit;

namespace DynamicMapper.Test
{

    public class DynamicMapperContainerTest
    {
        [Fact]
        public void ContainerCreation_InvalidLambda_ThrowException()
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                new DynamicMapperContainer<IDataReader>((reader, str) => reader.GetValues(new object[] { }));
            });

            Assert.ThrowsAny<Exception>(() =>
            {
                new DynamicMapperContainer<IDataReader>((reader, str) => reader.GetHashCode());
            });
        }
    }
}
