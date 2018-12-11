# DynamicMapper [![Build Status](https://travis-ci.com/Rem0o/DynamicMapper.svg?branch=master)](https://travis-ci.com/Rem0o/DynamicMapper)

Create all your POCO mappers dynamically from your POCO types directly using static reflection and the Roselyn compiler.

The mappers can be compiled once dynamically at startup. Once created, the mappers are then cached. Each mapper should give similar performance to manual object mapping directly from your source object.

### Code exemple

```c#
// Compiles mappers for all classes under a specific namespace at startup to map from a IDataReader source object;
var mapperContainer = new DynamicMapperContainer<IDataReader>((reader, propertyName) => reader[propertyName])
    .CompileMappers(this.GetType().Assembly, t => t.Namespace == typeof(MyPocoClass).Namespace);
// (...)

// If the type was not included during the container construction, the container will try to compile the mapper
// for the given class dynamically and cache it for later use. 
mapperContainer.GetMapper<MyPocoClass>(out Action<IDataReader, MyPocoClass> mapper);

// use mapper here ...
using (var connection = new SqlConnection("some connection string"))
{
    SqlCommand command = new SqlCommand("SELECT * FROM SomeTable", connection);
    connection.Open();
    
    SqlDataReader reader = command.ExecuteReader();
    
    while (reader.Read())
    {
        var poco = new MyPocoClass();
        mapper(reader, poco);
        // poco was mapped!
    }
    
    reader.Close();
}


```

### Some performance testing
Here are some basic performance testing comparing manual mapping, DynamicMapper and Dapper. The results are for demonstration purpose only and may not reflect your personal results.

Test conditions:

- Mapping a simple POCO with 7 properties
- 100 000 itterations
- Manual mapping and DynamicMapper both use a basic SqlConnection/SqlCommand/SqlDataReader implementation like the code exemple above.
- Dapper uses the IDbConnection.Query<T> extension method.

Results:

| Mapper         | Time (ms) |
| -------------- | --------- |
| DynamicMapper  | 917       |
| Manual mapping | 669       |
| Dapper         | 1061      |
