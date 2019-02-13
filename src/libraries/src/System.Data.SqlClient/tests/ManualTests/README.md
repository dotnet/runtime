# SqlClient Manual Tests

These tests require dedicated test servers, so they're designed to be run manually using a custom set of connection strings. 

## Prerequisites

- CoreFX building. you need to be able to do a successful build and run the standard tests, [Unix](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md) or [Windows](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md) Use build.cmd for windows and build.sh for Linux to build CoreFX.

  **N.B.** if you want to run the EFCore tests later you will need to build -allconfigurations to generate the NuGet packages, build -allconfigurations works only on windows.

- an [MS SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-editions-express) (any edition) 2012 or later that you can connect to with tcp and named pipes, 

  **N.B**. if you want to run the EFCore tests it should be a dedicated instance because they create a lot of databases.

- The  [Northwind Sample Database](https://msdn.microsoft.com/en-us/library/mt710790.aspx)

- The [UDT Test Database](https://github.com/dotnet/corefx/tree/master/src/System.Data.SqlClient/tests/ManualTests/createUdtTestDb_corefx.sql) 

- TCP and Named Pipe [connection strings](https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlconnection.connectionstring.aspx) to your instance with Northwind set as the initial catalog



## Running All Tests

1. set the environment variables needed for the tests you want. At the minimum you need to set
    `TEST_NP_CONN_STR` and `TEST_TCP_CONN_STR` to the connection strings. 

2. Optionally you may also want to setup other environment variables to test specific optional features such as [TEST_LOCALDB_INSTALLED](https://github.com/dotnet/corefx/blob/8f7b490ca874ee2a9f11f0163412f7c95811298b/src/System.Data.SqlClient/tests/ManualTests/DataCommon/DataTestUtility.cs#L96) or [TEST_INTEGRATEDSECURITY_SETUP](https://github.com/dotnet/corefx/blob/8f7b490ca874ee2a9f11f0163412f7c95811298b/src/System.Data.SqlClient/tests/ManualTests/DataCommon/DataTestUtility.cs#L98). Other scenarios lke azure tests may need configuration so if you see those being skipped and you want to run them invesigate the skipped test code to identify how to configure it.

3. run `dotnet msbuild .\src\System.Data.SqlClient\tests\ManualTests\System.Data.SqlClient.ManualTesting.Tests.csproj /t:Rebuild` to build the debug version with all the assertions and run the tests.

4. If you need to re-run the test suite without having changed the build (e.g. if you've changed the exnvironment variables) you can use `dotnet msbuild .\src\System.Data.SqlClient\tests\ManualTests\System.Data.SqlClient.ManualTesting.Tests.csproj /t:Test`

  ​    

## Running A Specific Test

Once you have all tests running you may need to debug a single failing test. To do this navigate into the manual tests project directory `cd src\System.Data.SqlClient\tests\ManualTests` then run dotnet msbuild and specify the name of the test you want to execute, like this:
 `dotnet msbuild /t:RebuildAndTest /p:XunitMethodName=System.Data.SqlClient.ManualTesting.Tests.DDDataTypesTest.MaxTypesTest`