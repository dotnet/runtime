// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class LocalDBTest
    {
        private static bool IsLocalDBEnvironmentSet() => DataTestUtility.IsLocalDBInstalled();

        [ConditionalFact(nameof(IsLocalDBEnvironmentSet))]
        public static void LocalDBConnectionTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder("server=(localdb)\\MSSQLLocalDB");
            builder.IntegratedSecurity = true;
            builder.ConnectTimeout = 2;
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT @@SERVERNAME", connection))
                {
                    var result = command.ExecuteScalar();
                    Assert.NotNull(result);
                }
            }
        }
    }
}
