# DynamicSqlMapper

Create all your POCO SQL mappers dynamically from your POCO types directly using static reflection and the Roselyn compiler.

The mappers can be compiled once dynamically at startup. Once created, the mappers are then cached. Each mapper should give similar performance to manual object mapping directly from a IDataReader object.

### Code exemple

```c#
// Compiles mappers for all classes under a specific namespace at startup;
var mapperContainer = new DynamicSqlMapperContainer(
    this.GetType().Assembly,
    t => t.Namespace == typeof(MyPocoClass).Namespace);

// (...)

// If the type was not included during the container construction, the container will try to compile the mapper
// for the given class dynamically and cache it for later use. 
if (mapperContainer.TryGetMapper<MyPocoClass>(out Action<IDataReader, MyPocoClass> mapper))
{
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
}

```

### Some performance testing
Here are some basic performance testing comparing manual mapping, DynamicMapper and Dapper. The results are for demonstration purpose only and may not reflect your personal results.

Test conditions:

- Mapping a simple POCO with 7 properties
- 100 000 itterations
- Manual mapping and DynamicMapper both use a basic SqlConnection/SqlCommand/SqlDataReader implementation like the code exemple above.
- Dapper uses the SqlConnection.Query<T> extension method.

Results:

| Mapper         | Time (ms) |
| -------------- | --------- |
| DynamicMapper  | 917       |
| Manual mapping | 669       |
| Dapper         | 1061      |
