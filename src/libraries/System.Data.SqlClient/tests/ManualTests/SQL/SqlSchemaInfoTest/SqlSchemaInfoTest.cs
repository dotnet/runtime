// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlSchemaInfoTest
    {
        [CheckConnStrSetupFact]
        public static void TestGetSchema()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                DataTable dataTable = connection.GetSchema("DATABASES");

                if (dataTable.Rows.Count > 0)
                {
                    DataTable metaDataCollections = connection.GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
                    Assert.True(metaDataCollections != null && metaDataCollections.Rows.Count > 0);
                    
                    DataTable metaDataSourceInfo = connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
                    Assert.True(metaDataSourceInfo != null && metaDataSourceInfo.Rows.Count > 0);

                    DataTable metaDataTypes = connection.GetSchema(DbMetaDataCollectionNames.DataTypes);
                    Assert.True(metaDataTypes != null && metaDataTypes.Rows.Count > 0);
                }

                connection.Close();
            }
        }

        [CheckConnStrSetupFact]
        public static void TestCommandBuilder()
        {
            // CommandBuilder is not supported yet in .NET Core.
        }

        [CheckConnStrSetupFact]
        public static void TestInitialCatalogStandardValues()
        {
            // PropertyDescriptor is not supported yet in .NET Core.
        }
    }
}
