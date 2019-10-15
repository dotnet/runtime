// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class LocalDBTest
    {
        private static bool IsLocalDBEnvironmentSet() => DataTestUtility.IsLocalDBInstalled();

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // No Registry support on UAP
        [ConditionalFact(nameof(IsLocalDBEnvironmentSet))]
        public static void LocalDBConnectionTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(@"server=(localdb)\MSSQLLocalDB");
            builder.IntegratedSecurity = true;
            builder.ConnectTimeout = 2;
            OpenConnection(builder.ConnectionString);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // No Registry support on UAP
        [ConditionalFact(nameof(IsLocalDBEnvironmentSet))]
        public static void LocalDBMarsTest()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(@"server=(localdb)\MSSQLLocalDB;");
            builder.IntegratedSecurity = true;
            builder.MultipleActiveResultSets = true;
            builder.ConnectTimeout = 2;
            OpenConnection(builder.ConnectionString);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // No Registry support on UAP
        [ConditionalFact(nameof(IsLocalDBEnvironmentSet))]
        public static void InvalidDBTest()
        {
            using (var connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLOCALDB;Database=DOES_NOT_EXIST;Pooling=false;"))
            {
                DataTestUtility.AssertThrowsWrapper<SqlException>(() => connection.Open());
            }
        }

        private static void OpenConnection(string connString)
        {
            using (SqlConnection connection = new SqlConnection(connString))
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
