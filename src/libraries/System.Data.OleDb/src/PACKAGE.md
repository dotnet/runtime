## About

This package implements a data provider for OLE DB data sources.

## Key Features

Allows access to legacy OLE DB data sources.

## How to Use

This is a basic example of retrieving the results of a query using an [OleDbDataReader](https://learn.microsoft.com/dotnet/api/system.data.oledb.oledbdatareader). For examples of using an [OleDbDataAdapter](https://learn.microsoft.com/dotnet/api/system.data.oledb.oledbdataadapter), and of updating an OLE DB data source, please see the documentation.

```cs
using System.Data.OleDb;

string connectionString = ""; // Fill in
string queryString = "SELECT OrderID, CustomerID FROM Orders";

using OleDbConnection connection = new OleDbConnection(connectionString);
using OleDbCommand command = new OleDbCommand(queryString, connection);

connection.Open();
using OleDbDataReader reader = command.ExecuteReader();

while (reader.Read())
{
    Console.WriteLine(reader.GetInt32(0) + ", " + reader.GetString(1));
}
```

## Main Types

* [OleDbConnection](https://learn.microsoft.com/dotnet/api/system.data.oledb.oledbconnection) represents an open connection to an OLE DB data source.
* [OleDbCommand](https://learn.microsoft.com/dotnet/api/system.data.oledb.oledbcommand) represents an SQL statement or stored procedure to execute against an OLE DB data source.
* [OleDbDataReader](https://learn.microsoft.com/dotnet/api/system.data.oledb.oledbdatareader) provides a way of reading a forward-only stream of data rows from an OLE DB data source.
* [OleDbDataAdapter](https://learn.microsoft.com/dotnet/api/system.data.oledb.oledbdataadapter) represents a set of data commands and a database connection that are used to fill a `DataSet` and update the OLE DB data source.

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.data.oledb)

## Related Packages

System.Data.Odbc is a similar package for accessing ODBC data sources.

## Feedback & Contributing

System.Data.OleDb is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports are welcome at [the GitHub repository](https://github.com/dotnet/runtime). This package is considered complete and we only consider low-risk, high-impact fixes that are necessary to maintain or improve quality.
