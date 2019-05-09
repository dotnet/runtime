// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Xunit;

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
        public void StateChange_ChangeState_TriggersEvent()
        {
            int timesCalled = 0;
            Action<object, StateChangeEventArgs> OnStateChange = (sender, args) => {
                timesCalled++;
            };
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.StateChange += new StateChangeEventHandler(OnStateChange);  
                oleDbConnection.Open();
                oleDbConnection.Close();
                Assert.Equal(2, timesCalled);
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

        [ConditionalFact(Helpers.IsAceDriverAvailable)]
        public void BeginTransaction_CallTwice_Throws()
        {
            using (var conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "CREATE TABLE table_x.csv (column_y NVARCHAR(40));";
                    cmd.Transaction = tx;
                    cmd.ExecuteNonQuery();
                    AssertExtensions.Throws<InvalidOperationException>(
                        () => conn.BeginTransaction(),
                        $"{nameof(OleDbConnection)} does not support parallel transactions."
                    );
                }
                conn.Close();
            }
        }

        [ConditionalFact(Helpers.IsAceDriverAvailable)]
        public void CommitTransaction_AfterConnectionClosed_Throws()
        {
            using (var conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "CREATE TABLE table_x.csv (column_y NVARCHAR(40));";
                    cmd.Transaction = tx;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    AssertExtensions.Throws<InvalidOperationException>(
                        () => tx.Commit(),
                        $"This {nameof(OleDbTransaction)} has completed; it is no longer usable."
                    );
                }
            }
        }
        
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetDefaults_AnyGivenState_DoesNotThrow()
        {
            const int DefaultTimeout = 15;
            Action<OleDbConnection> VerifyDefaults = (conn) => {
                Assert.Equal(DefaultTimeout, conn.ConnectionTimeout);
                Assert.Contains(conn.DataSource, TestDirectory);
                Assert.Empty(conn.Database);
            };
            using (var oleDbConnection = new OleDbConnection())
            {
                VerifyDefaults(oleDbConnection);
                oleDbConnection.ConnectionString = ConnectionString;
                oleDbConnection.Open();
                VerifyDefaults(oleDbConnection);
                oleDbConnection.Close();
                VerifyDefaults(oleDbConnection);
            }
        }
        
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void CreateCommand_AsDbConnection_IsOleDb()
        {
            DbConnection dbConnection = connection as DbConnection;
            DbCommand dbCommand = dbConnection.CreateCommand();
            Assert.NotNull(dbCommand);
            Assert.IsType<OleDbCommand>(dbCommand);
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
        public void GetSchema_NoArgs_ReturnsMetaDataCollections()
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // [ActiveIssue(37438)]
            }

            DataTable t1 = connection.GetSchema();
            DataTable t2 = connection.GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
            Assert.Equal(t1.Rows.Count, t2.Rows.Count);

            foreach (DataColumn dc in t1.Columns)
            {
                for (int i = 0; i < t1.Rows.Count; i++)
                {
                    Assert.Equal(t1.Rows[i][dc.ColumnName], t2.Rows[i][dc.ColumnName]);
                }
            }
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [InlineData(nameof(DbMetaDataCollectionNames.MetaDataCollections), "CollectionName")]
        [InlineData(nameof(DbMetaDataCollectionNames.DataSourceInformation), "CompositeIdentifierSeparatorPattern")]
        [InlineData(nameof(DbMetaDataCollectionNames.DataTypes), "TypeName")]
        public void GetSchema(string tableName, string columnName)
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // [ActiveIssue(37438)]
            }

            DataTable schema = connection.GetSchema(tableName);
            Assert.True(schema != null && schema.Rows.Count > 0);
            var exception = Record.Exception(() => schema.Rows[0].Field<string>(columnName));
            Assert.Null(exception);

            AssertExtensions.Throws<ArgumentException>(
                () => connection.GetSchema(tableName, new string[] { null }), 
                $"More restrictions were provided than the requested schema ('{tableName}') supports."
            );
            const string MissingColumn = "MissingColumn";
            AssertExtensions.Throws<ArgumentException>(
                () => schema.Rows[0].Field<IEnumerable<char>>(MissingColumn), 
                $"Column '{MissingColumn}' does not belong to table {tableName}.");
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetOleDbSchemaTable_ReturnsTableInfo()
        {
            string tableName = Helpers.GetTableName(nameof(GetOleDbSchemaTable_ReturnsTableInfo));
            command.CommandText = @"CREATE TABLE t1.csv (CustomerName NVARCHAR(40));";
            command.ExecuteNonQuery();
            command.CommandText = @"CREATE TABLE t2.csv (CustomerName NVARCHAR(40));";
            command.ExecuteNonQuery();
            DataTable listedTables = connection.GetOleDbSchemaTable(
                OleDbSchemaGuid.Tables, new object[] {null, null, null, "Table"});

            Assert.NotNull(listedTables);
            Assert.Equal(2, listedTables.Rows.Count);
            Assert.Equal("t1#csv", listedTables.Rows[0][2].ToString());
            Assert.Equal("t2#csv", listedTables.Rows[1][2].ToString());

            command.CommandText = @"DROP TABLE t1.csv";
            command.ExecuteNonQuery();
            command.CommandText = @"DROP TABLE t2.csv";
            command.ExecuteNonQuery();
        }

        [ConditionalFact(Helpers.IsAceDriverAvailable)]
        public void ChangeDatabase_EmptyDatabase_Throws()
        {
            using (var oleDbConnection = new OleDbConnection(ConnectionString))
            {
                oleDbConnection.Open();
                Assert.Throws<ArgumentException>(() => oleDbConnection.ChangeDatabase(null));
                Assert.Throws<ArgumentException>(() => oleDbConnection.ChangeDatabase(" "));
                Assert.Throws<ArgumentException>(() => oleDbConnection.ChangeDatabase(string.Empty));
                AssertExtensions.Throws<InvalidOperationException>(
                    () => oleDbConnection.ChangeDatabase("ReadOnlyShouldThrow"), 
                    "The 'current catalog' property was read-only, or the consumer attempted to set values of properties " + 
                    "in the Initialization property group after the data source object was initialized. " + 
                    "Consumers can set the value of a read-only property to its current value. " + 
                    "This status is also returned if a settable column property could not be set for the particular column."
                );
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

            AssertExtensions.Throws<ArgumentException>(
                () => new OleDbConnection(@"file name = " + udlFile), 
                "Invalid UDL file.");
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
