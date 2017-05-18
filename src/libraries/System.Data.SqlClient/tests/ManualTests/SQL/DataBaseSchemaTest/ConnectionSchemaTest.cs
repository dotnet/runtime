// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ConnectionSchemaTest
    {

        [CheckConnStrSetupFact]
        public static void GetAllTablesFromSchema()
        {
            VerifySchemaTable(SqlClientMetaDataCollectionNames.Tables, new string[] {"TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME", "TABLE_TYPE" });
        }

        [CheckConnStrSetupFact]
        public static void GetAllProceduresFromSchema()
        {
            VerifySchemaTable(SqlClientMetaDataCollectionNames.Procedures, new string[] { "ROUTINE_SCHEMA", "ROUTINE_NAME", "ROUTINE_TYPE" });
        }

        private static void VerifySchemaTable(string schemaItemName, string[] testColumnNames)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr)
            {
                InitialCatalog = "master"
            };

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                // Connect to the database then retrieve the schema information.  
                connection.Open();
                DataTable table = connection.GetSchema(schemaItemName);

                // Display the contents of the table.  
                Assert.InRange<int>(table.Rows.Count, 1, int.MaxValue);

                // Get all table columns 
                HashSet<string> columnNames = new HashSet<string>();
                
                foreach (DataColumn column in table.Columns)
                {
                    columnNames.Add(column.ColumnName);
                }

                Assert.All<string>(testColumnNames, column => Assert.Contains<string>(column, columnNames));
            }
        }
    }
}
