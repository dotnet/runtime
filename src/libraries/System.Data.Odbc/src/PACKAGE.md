## About

This package implements a data provider for ODBC data sources.

## Key Features

Allows access to ODBC data sources.

## How to Use

This is a basic example of retrieving the results of a query using an [OdbcDataReader](https://learn.microsoft.com/dotnet/api/system.data.odbc.odbcdatareader). For examples of using an [OdbcDataAdapter](https://learn.microsoft.com/dotnet/api/system.data.odbc.odbcdataadapter), and of updating an ODBC data source, please see the documentation.

```cs
using System.Data.Odbc;

string connectionString = ""; // Fill in
string queryString = "SELECT DISTINCT CustomerID FROM Orders";

using OdbcConnection connection = new OdbcConnection(connectionString);
using OdbcCommand command = new OdbcCommand(queryString, connection);

connection.Open();
using OdbcDataReader reader = command.ExecuteReader();

while (reader.Read())
{
    Console.WriteLine("CustomerID={0}", reader[0]);
}
```

## Main Types

* [OdbcConnection](https://learn.microsoft.com/dotnet/api/system.data.odbc.odbcconnection) represents a connection to an ODBC data source.
* [OdbcCommand](https://learn.microsoft.com/dotnet/api/system.data.odbc.odbccommand) represents an SQL statement or stored procedure to execute against an ODBC data source..
* [OdbcDataReader](https://learn.microsoft.com/dotnet/api/system.data.odbc.odbcdatareader) provides a way of reading a forward-only stream of data rows from an ODBC data source.
* [OdbcDataAdapter](https://learn.microsoft.com/dotnet/api/system.data.odbc.odbcdataadapter) represents a set of data commands and a database connection that are used to fill a [DataSet](https://learn.microsoft.com/dotnet/api/system.data.dataset) and update the ODBC data source.

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.data.odbc)

## Related Packages

System.Data.OleDb is a similar package for accessing OLE DB data sources.

## Feedback & Contributing

System.Data.Odbc is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports are welcome at [the GitHub repository](https://github.com/dotnet/runtime). This package is considered complete and we only consider low-risk, high-impact fixes that are necessary to maintain or improve quality.
