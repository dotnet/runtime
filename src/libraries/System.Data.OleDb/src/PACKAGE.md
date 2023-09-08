## About

This package implements a data provider for OLE DB data sources.

## Key Features

Allows access to legacy OLE DB data sources.

## How to Use

This is a basic example of retrieving the results of a query using an `OleDbDataReader`. For examples of using an `OleDbDataAdapter`, and of updating an OLE DB data source, please see the documentation.

```cs
string queryString = "SELECT OrderID, CustomerID FROM Orders";
using (OleDbConnection connection = new OleDbConnection(connectionString))
{
    OleDbCommand command = new OleDbCommand(queryString, connection);
    connection.Open();
    OleDbDataReader reader = command.ExecuteReader();

    while (reader.Read())
    {
        Console.WriteLine(reader.GetInt32(0) + ", " + reader.GetString(1));
    }
    // always call Close when done reading.
    reader.Close();
}
```

## Main Types

* `OleDbDataAdapter` represents a set of data commands and a database connection that are used to fill a `DataSet` and update the OLE DB data source.
* `OleDbDataReader` provides a way of reading a forward-only stream of data rows from an OLE DB data source.
* `OleDbCommand` represents an SQL statement or stored procedure to execute against an OLE DB data source.
* `OleDbConnection` represents an open connection to an OLE DB data source.

## Additional Documentation

* [API documentation](https://learn.microsoft.com/en-us/dotnet/api/system.data.oledb)

## Related Packages

System.Data.Odbc is a similar package for accessing ODBC data sources.

## Feedback & Contributing

**System.Data.OleDb** is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports are welcome at [the GitHub repository](https://github.com/dotnet/runtime). This package is considered complete and we only consider lower-risk or high-impact fixes that will maintain or improve quality.