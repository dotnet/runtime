// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlNamedPipesTest
    {
        [CheckConnStrSetupFact]
        public static void ValidConnStringTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.NpConnStr);
            builder.ConnectTimeout = 5;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpenBadConnection<PlatformNotSupportedException>(builder.ConnectionString);
            }
            else
            {
                OpenGoodConnection(builder.ConnectionString);
            }
        }

#if MANAGED_SNI
        [CheckConnStrSetupFact]
        public static void InvalidConnStringTest()
        {
            string invalidConnStringError = SystemDataResourceManager.Instance.SNI_ERROR_25;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.NpConnStr);
            builder.ConnectTimeout = 2;

            string fakeServerName = Guid.NewGuid().ToString("N");

            // Using forward slashes
            builder.DataSource = "np://" + fakeServerName + "/pipe/sql/query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Without pipe token
            builder.DataSource = @"np:\\" + fakeServerName + @"\sql\query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Without a pipe name
            builder.DataSource = @"np:\\" + fakeServerName + @"\pipe";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Nothing after server
            builder.DataSource = @"np:\\" + fakeServerName;
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // No leading slashes
            builder.DataSource = @"np:" + fakeServerName + @"\pipe\sql\query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // No server name
            builder.DataSource = @"np:\\\pipe\sql\query";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);

            // Nothing but slashes
            builder.DataSource = @"np:\\\\\";
            OpenBadConnection<SqlException>(builder.ConnectionString, invalidConnStringError);
        }
#endif

        private static void OpenBadConnection<T>(string connectionString, string errorMessage = null) where T : Exception
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                DataTestUtility.AssertThrowsWrapper<T>(() => conn.Open(), errorMessage);
            }
        }

        private static void OpenGoodConnection(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                DataTestUtility.AssertEqualsWithDescription(ConnectionState.Open, conn.State, "FAILED: Connection should be in open state");
            }
        }
    }
}
