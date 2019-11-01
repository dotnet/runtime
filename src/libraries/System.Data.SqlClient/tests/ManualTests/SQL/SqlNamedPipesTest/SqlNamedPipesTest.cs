// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlNamedPipesTest
    {
        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        [PlatformSpecific(TestPlatforms.Windows)] // Named pipes with the given input strings are not supported on Unix
        public static void ValidConnStringTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.NpConnStr);
            builder.ConnectTimeout = 5;

            OpenGoodConnection(builder.ConnectionString);
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
