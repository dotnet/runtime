// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.ComponentModel;
using System.Collections.Generic;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlSchemaInfoTest
    {
        #region TestMethods
        [CheckConnStrSetupFact]
        public static void TestGetSchema()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                conn.Open();
                DataTable dataBases = conn.GetSchema("DATABASES");

                Assert.True(dataBases.Rows.Count > 0, "At least one database is expected");

                DataTable metaDataCollections = conn.GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
                Assert.True(metaDataCollections != null && metaDataCollections.Rows.Count > 0);

                DataTable metaDataSourceInfo = conn.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
                Assert.True(metaDataSourceInfo != null && metaDataSourceInfo.Rows.Count > 0);

                DataTable metaDataTypes = conn.GetSchema(DbMetaDataCollectionNames.DataTypes);
                Assert.True(metaDataTypes != null && metaDataTypes.Rows.Count > 0);
            }
        }

        [CheckConnStrSetupFact]
        public static void TestCommandBuilder()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommandBuilder commandBuilder = new SqlCommandBuilder())
            using (SqlCommand command = connection.CreateCommand())
            {
                string identifier = "TestIdentifier";
                string quotedIdentifier = commandBuilder.QuoteIdentifier(identifier);
                DataTestUtility.AssertEqualsWithDescription(
                    "[TestIdentifier]", quotedIdentifier,
                    "Unexpected QuotedIdentifier string.");

                string unquotedIdentifier = commandBuilder.UnquoteIdentifier(quotedIdentifier);
                DataTestUtility.AssertEqualsWithDescription(
                    "TestIdentifier", unquotedIdentifier,
                    "Unexpected UnquotedIdentifier string.");

                identifier = "identifier]withclosesquarebracket";
                quotedIdentifier = commandBuilder.QuoteIdentifier(identifier);
                DataTestUtility.AssertEqualsWithDescription(
                    "[identifier]]withclosesquarebracket]", quotedIdentifier,
                    "Unexpected QuotedIdentifier string.");

                unquotedIdentifier = null;
                unquotedIdentifier = commandBuilder.UnquoteIdentifier(quotedIdentifier);
                DataTestUtility.AssertEqualsWithDescription(
                    "identifier]withclosesquarebracket", unquotedIdentifier,
                    "Unexpected UnquotedIdentifier string.");
            }
        }

        // This test validates behavior of SqlInitialCatalogConverter used to present database names in PropertyGrid
        // with the SqlConnectionStringBuilder object presented in the control underneath.
        [CheckConnStrSetupFact]
        public static void TestInitialCatalogStandardValues()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                string currentDb = connection.Database;
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connection.ConnectionString);
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(builder);
                PropertyDescriptor descriptor = properties["InitialCatalog"];

                DataTestUtility.AssertEqualsWithDescription(
                    "SqlInitialCatalogConverter", descriptor.Converter.GetType().Name,
                    "Unexpected TypeConverter type.");

                // GetStandardValues of this converter calls GetSchema("DATABASES")
                var dbNames = descriptor.Converter.GetStandardValues(new DescriptorContext(descriptor, builder));
                HashSet<string> searchSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (string name in dbNames)
                {
                    searchSet.Add(name);
                }

                // ensure master and current database exist there
                Assert.True(searchSet.Contains("master"), "Cannot find database: master.");
                Assert.True(searchSet.Contains(currentDb), $"Cannot find database: {currentDb}.");
            }
        }
        #endregion

        #region UtilityMethodsClasses
        // primitive implementation of ITypeDescriptorContext to be used with component model APIs
        private class DescriptorContext : ITypeDescriptorContext
        {
            SqlConnectionStringBuilder _instance;
            PropertyDescriptor _descriptor;

            public DescriptorContext(PropertyDescriptor descriptor, SqlConnectionStringBuilder instance)
            {
                _instance = instance;
                _descriptor = descriptor;
            }

            public object Instance
            {
                get { return _instance; }
            }

            public IContainer Container
            {
                get { return null; }
            }

            public PropertyDescriptor PropertyDescriptor
            {
                get { return _descriptor; }
            }

            public void OnComponentChanged()
            {
                throw new NotImplementedException();
            }

            public bool OnComponentChanging()
            {
                throw new NotImplementedException();
            }

            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }

        private static void DumpDataTable(DataTable dataTable, int rowPrintCount)
        {
            Console.WriteLine("DumpDataTable");
            Console.WriteLine("");

            if (dataTable == null)
            {
                Console.WriteLine("DataTable object is null.");
                return;
            }
            int columnCount = dataTable.Columns.Count;
            int currentColumn;

            int rowCount = dataTable.Rows.Count;
            int currentRow;

            Console.WriteLine("Table \"{0}\" has {1} columns", dataTable.TableName.ToString(), columnCount.ToString());
            Console.WriteLine("Table \"{0}\" has {1} rows. At most the first {2} are dumped.", dataTable.TableName.ToString(), rowCount.ToString(), rowPrintCount.ToString());

            if ((rowPrintCount != 0) && (rowPrintCount < rowCount))
            {
                rowCount = rowPrintCount;
            }

            for (currentColumn = 0; currentColumn < columnCount; currentColumn++)
            {
                DumpDataColumn(dataTable.Columns[currentColumn]);
            }

            for (currentRow = 0; currentRow < rowCount; currentRow++)
            {
                DumpDataRow(dataTable.Rows[currentRow], dataTable);
            }

            return;

        }

        private static void DumpDataRow(DataRow dataRow, DataTable dataTable)
        {
            Console.WriteLine(" ");
            Console.WriteLine("<DumpDataRow>");

            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                Console.WriteLine("{0}.{1} = {2}", dataTable.TableName, dataColumn.ColumnName, dataRow[dataColumn, DataRowVersion.Current].ToString());
            }
            return;
        }

        private static void DumpDataColumn(DataColumn dataColumn)
        {

            Console.WriteLine("Column Name = {0}, Column Type =  {1}", dataColumn.ColumnName, dataColumn.DataType.ToString());
            return;
        }
        #endregion
    }
}