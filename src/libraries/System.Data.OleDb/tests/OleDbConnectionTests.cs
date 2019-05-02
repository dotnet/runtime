// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Data.OleDb.Tests
{
    public class OleDbConnectionTests : OleDbTestBase
    {
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_ConnectionStringMissingProvider_Throws()
        {
            Assert.Throws<ArgumentException>(() => new OleDbConnection("Reason=missingProvider"));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_LongProvider_Throws()
        {
            Assert.Throws<ArgumentException>(() => new OleDbConnection("provider=" + new string('c', 256)));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_MSDASQLNotSupported_Throws()
        {
            Assert.Throws<ArgumentException>(() => new OleDbConnection("provider=MSDASQL"));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_MissingUdlFile_Throws()
        {
            Assert.Throws<ArgumentException>(() => new OleDbConnection(@"file name = missing-file.udl"));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_AsynchronousNotSupported_Throws()
        {
            Assert.Throws<ArgumentException>(() => 
                new OleDbConnection(ConnectionString + ";asynchronous processing=true"));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_InvalidConnectTimeout_Throws()
        {
            Assert.Throws<ArgumentException>(() => 
                new OleDbConnection(ConnectionString + ";connect timeout=-2"));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Open_NoConnectionString_Throws()
        {
            using (var innerConnection = (OleDbConnection)OleDbFactory.Instance.CreateConnection())
            {
                innerConnection.ConnectionString = null;
                Assert.Throws<InvalidOperationException>(() => innerConnection.Open());
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void BeginTransaction_IsolationLevelIsUnspecified_SetsReadCommitted()
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                using (OleDbTransaction transaction = oleDbConnection.BeginTransaction())
                {
                    Assert.Equal(IsolationLevel.ReadCommitted, transaction.IsolationLevel);
                }
                using (OleDbTransaction transaction = oleDbConnection.BeginTransaction(IsolationLevel.Unspecified))
                {
                    Assert.Equal(IsolationLevel.ReadCommitted, transaction.IsolationLevel);
                }
            }
        }
        
        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(IsolationLevelsExceptUnspecified))]
        public void BeginTransaction_SpecificIsolationLevel_Success(IsolationLevel isolationLevel)
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                using (OleDbTransaction transaction = oleDbConnection.BeginTransaction(isolationLevel))
                {
                    Assert.Equal(isolationLevel, transaction.IsolationLevel);
                }
            }
        }
        
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void BeginTransaction_InvalidIsolationLevel_Throws()
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                Assert.Throws<ArgumentOutOfRangeException>(() => oleDbConnection.BeginTransaction((IsolationLevel)0));
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Provider_SetProperlyFromCtor()
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                Assert.Equal(Helpers.ProviderName, oleDbConnection.Provider);
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetSchema()
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();

                DataTable metaDataCollections = oleDbConnection.GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
                Assert.True(metaDataCollections != null && metaDataCollections.Rows.Count > 0);

                DataTable metaDataSourceInfo = oleDbConnection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
                Assert.True(metaDataSourceInfo != null && metaDataSourceInfo.Rows.Count > 0);

                DataTable metaDataTypes = oleDbConnection.GetSchema(DbMetaDataCollectionNames.DataTypes);
                Assert.True(metaDataTypes != null && metaDataTypes.Rows.Count > 0);
                
                DataTable schema = oleDbConnection.GetSchema();
                Assert.True(schema != null && schema.Rows.Count > 0);

                Assert.Throws<NotSupportedException>(
                    () => oleDbConnection.GetSchema(DbMetaDataCollectionNames.MetaDataCollections, new string[] { new string('a', 5000) } ));
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ChangeDatabase_EmptyDatabase_Throws()
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                Assert.Throws<ArgumentException>(() => oleDbConnection.ChangeDatabase(string.Empty));
            }
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(ManufacturedOleDbSchemaGuids))]
        public void GetOleDbSchemaTable_NoRestrictions_Success(Guid oleDbSchemaGuid)
        {
            DataTable oleDbSchemaTable = null;
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                oleDbSchemaTable = oleDbConnection.GetOleDbSchemaTable(oleDbSchemaGuid, restrictions: null);
            }
            Assert.NotNull(oleDbSchemaTable);
            Assert.NotNull(oleDbSchemaTable.Rows);
            foreach (DataRow dataRow in oleDbSchemaTable.Rows)
            {
                Assert.NotNull(dataRow.ItemArray);
            }
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(ManufacturedOleDbSchemaGuids))]
        public void GetOleDbSchemaTable_SomeRestrictions_Throws(Guid oleDbSchemaGuid)
        {
            object[] restrictions = new object[] { null };
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                Assert.Throws<ArgumentException>(() => 
                    oleDbConnection.GetOleDbSchemaTable(oleDbSchemaGuid, restrictions));
            }
        }

        public static IEnumerable<object[]> ManufacturedOleDbSchemaGuids
        {
            get
            {
                yield return new object[] { OleDbSchemaGuid.DbInfoLiterals };
                yield return new object[] { OleDbSchemaGuid.SchemaGuids };
                yield return new object[] { OleDbSchemaGuid.DbInfoKeywords };
            }
        }

        public static IEnumerable<object[]> IsolationLevelsExceptUnspecified
        {
            get
            {
                yield return new object[] { IsolationLevel.Chaos };
                yield return new object[] { IsolationLevel.ReadUncommitted };
                yield return new object[] { IsolationLevel.ReadCommitted };
            }
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 2)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(0, 3)]
        public void Ctor_InvalidUdlFile_Throws(int start, int length)
        {
            string udlFile = GetTestFilePath() + ".udl";
            Span<string> lines = new string[] { 
                "[oledb]", 
                "; Everything after this line is an OLE DB initstring", 
                ConnectionString }.AsSpan();
            File.WriteAllLines(udlFile, lines.Slice(start, length).ToArray());

            var exception = Record.Exception(() => new OleDbConnection(@"file name = " + udlFile));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
            Assert.Equal(
                "Invalid UDL file.",
                exception.Message);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_ValidUdlFile_Success()
        {
            string udlFile = GetTestFilePath() + ".udl";
            File.WriteAllLines(udlFile, new string[] { 
                "[oledb]",
                "; Everything after this line is an OLE DB initstring",
                ConnectionString
            }, System.Text.Encoding.Unicode);
            var oleDbConnection = new OleDbConnection(@"file name = " + udlFile);
            Assert.NotNull(oleDbConnection);
            oleDbConnection.Open();
            Assert.Equal(Helpers.ProviderName, oleDbConnection.Provider);
            oleDbConnection.Dispose();
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void OleDbConnectionStringBuilder_Success()
        {
            var connectionStringBuilder = (OleDbConnectionStringBuilder)OleDbFactory.Instance.CreateConnectionStringBuilder();
            Assert.Empty(connectionStringBuilder.Provider);
            Assert.True(connectionStringBuilder.ContainsKey("Provider"));
            Assert.Empty((string)connectionStringBuilder["Provider"]);

            Assert.False(connectionStringBuilder.ContainsKey("MissingKey"));
            Assert.False(connectionStringBuilder.TryGetValue("MissingKey", out var value));

            // Default values
            Assert.Equal(-13, connectionStringBuilder.OleDbServices);
            Assert.False(connectionStringBuilder.PersistSecurityInfo);

            connectionStringBuilder = new OleDbConnectionStringBuilder(ConnectionString);
            Assert.Equal(Helpers.ProviderName, connectionStringBuilder.Provider);
            Assert.Equal(TestDirectory, connectionStringBuilder.DataSource);

            string udlFile = GetTestFilePath() + ".udl";
            connectionStringBuilder = new OleDbConnectionStringBuilder(@"file name = " + udlFile);
            Assert.Equal(udlFile, connectionStringBuilder.FileName);

            connectionStringBuilder  = new OleDbConnectionStringBuilder();
            connectionStringBuilder.Provider = "myProvider";
            connectionStringBuilder.DataSource = "myServer";
            connectionStringBuilder.FileName = "myAccessFile.mdb";
            connectionStringBuilder.PersistSecurityInfo = true;
            connectionStringBuilder.OleDbServices = 0;

            Assert.Equal(connectionStringBuilder.Provider, connectionStringBuilder["Provider"]);
            Assert.Equal(connectionStringBuilder.DataSource, connectionStringBuilder["Data Source"]);
            Assert.Equal(connectionStringBuilder.FileName, connectionStringBuilder["File Name"]);
            Assert.Equal(connectionStringBuilder.OleDbServices, connectionStringBuilder["OLE DB Services"]);
            Assert.Equal(connectionStringBuilder.PersistSecurityInfo, connectionStringBuilder["Persist Security Info"]);

            connectionStringBuilder["CustomKey"] = "CustomValue";
            string connectionString = connectionStringBuilder.ToString();

            Assert.Contains(@"Provider=" + connectionStringBuilder.Provider, connectionString);
            Assert.Contains(@"Data Source=" + connectionStringBuilder.DataSource, connectionString);
            Assert.Contains(@"File Name=" + connectionStringBuilder.FileName, connectionString);
            Assert.Contains(@"OLE DB Services=" + connectionStringBuilder.OleDbServices, connectionString);
            Assert.Contains(@"Persist Security Info=" + connectionStringBuilder.PersistSecurityInfo, connectionString);
            Assert.Contains(@"CustomKey=" + connectionStringBuilder["CustomKey"], connectionString);

            connectionStringBuilder["OLE DB Services"] = 3;
            Assert.Contains(@"OLE DB Services=" + 3, connectionStringBuilder.ToString());
            connectionStringBuilder["Persist Security Info"] = false;
            Assert.Contains(@"Persist Security Info=" + false, connectionStringBuilder.ToString());

            connectionStringBuilder["Provider"] = string.Empty;
            Assert.Contains(@"Provider=;", connectionStringBuilder.ToString());
            Assert.Equal(string.Empty, connectionStringBuilder["Provider"]);

            Assert.Throws<ArgumentNullException>(() => connectionStringBuilder.Remove(null));
            Assert.False(connectionStringBuilder.Remove("NonExistentKey"));
            Assert.True(connectionStringBuilder.Remove("File Name"));
            Assert.Empty(connectionStringBuilder.FileName);
            Assert.True(connectionStringBuilder.Remove("Provider"));
            Assert.Empty(connectionStringBuilder.Provider);
        }
    }
}