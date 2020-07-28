// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Odbc.Tests
{
    public class OdbcConnectionSchemaTests
    {
        [CheckConnStrSetupFact]
        public void TestConnectionSchemaOnOpenConnection()
        {
            string connectionString = DataTestUtility.OdbcConnStr;

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.GetSchema();
                connection.Open();
                DataTable schema = connection.GetSchema();
                Assert.NotNull(schema);

                DataTable tableSchema = connection.GetSchema("Tables");
                Assert.NotNull(tableSchema);
            }
        }

        [Fact]
        public void TestConnectionSchemaOnNonOpenConnection()
        {
            using (OdbcConnection connection = new OdbcConnection(string.Empty))
            {
                Assert.Throws<InvalidOperationException>(() => connection.GetSchema());
            }
        }
    }
}
