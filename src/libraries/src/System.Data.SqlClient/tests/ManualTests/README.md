# SqlClient Manual Tests

These tests require dedicated test servers, so they're designed to be run manually using a custom set of connection strings. These connection strings should be added in [ConnectionString.xml](https://github.com/dotnet/corefx/blob/master/src/System.Data.SqlClient/tests/ManualTests/DataCommon/ConnectionString.xml). Each connection string references which version of SQL Server it targets, as well as the sample database it expects. Info for these sample databases can be found [here](https://msdn.microsoft.com/en-us/library/8b6y4c7s.aspx). Connection strings that end in "NamedPipes" are also meant to be used with named pipes, rather than the default TCP.

Instructions for running tests: [Unix](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md) and [Windows](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md). 

Documentation for connection string parameters: [SqlConnection.ConnectionString](https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlconnection.connectionstring.aspx).
