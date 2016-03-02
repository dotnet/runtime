// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlNamedPipesTest
    {
        [Fact]
        public static void ValidConnStringTest()
        {

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestClass.SQL2008_Northwind_NamedPipes);
            builder.ConnectTimeout = 5;

            string plainConnString = builder.ConnectionString;
            builder.DataSource = "np:" + GetHostFromDataSource(builder.DataSource);
            string serverOnlyConnString = builder.ConnectionString;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpenBadConnection<PlatformNotSupportedException>(plainConnString);
                OpenBadConnection<PlatformNotSupportedException>(serverOnlyConnString);
            }
            else
            {
                OpenGoodConnection(plainConnString);
                OpenGoodConnection(serverOnlyConnString);
            }
        }

        [Fact]
        public static void InvalidConnStringTest()
        {
#if MANAGED_SNI
            const string invalidConnStringError = "A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: Named Pipes Provider, error: 25 - Connection string is not valid)";
#else
            const string invalidConnStringError = "A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections.";
#endif

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestClass.SQL2008_Northwind_NamedPipes);
            builder.ConnectTimeout = 2;

            string host = GetHostFromDataSource(builder.DataSource);

            // Using forwad slashes
            builder.DataSource = "np://" + host + "/pipe/sql/query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Without pipe token
            builder.DataSource = @"np:\\" + host + @"\sql\query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Without a pipe name
            builder.DataSource = @"np:\\" + host + @"\pipe";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Nothing after server
            builder.DataSource = @"np:\\" + host;
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // No leading slashes
            builder.DataSource = @"np:" + host + @"\pipe\sql\query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // No server name
            builder.DataSource = @"np:\\\pipe\sql\query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Nothing but slashes
            builder.DataSource = @"np:\\\\\";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Empty string
            builder.DataSource = "np:";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);
        }

        private static void OpenBadConnection<T>(string connectionString, string errorMessage = null) where T : Exception
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                DataTestClass.AssertThrowsWrapper<T>(() => conn.Open(), errorMessage);
            }
        }

        private static void OpenGoodConnection(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                DataTestClass.AssertEqualsWithDescription(ConnectionState.Open, conn.State, "FAILED: Connection should be in open state");
            }
        }

        private static string GetHostFromDataSource(string dataSource)
        {
            // NP Data Source can be prefixed with "np:" and then a path/hostname, or can just be the hostname
            int colonIndex = dataSource.IndexOf(':');
            return (colonIndex == -1) ? dataSource : new Uri(dataSource.Substring(colonIndex + 1)).Host;
        }
    }
}
