// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Xunit;

namespace System.Data.OleDb.Tests
{
    [Collection("System.Data.OleDb")] // not let tests run in parallel
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
            connection.Dispose();
            connection = (OleDbConnection)OleDbFactory.Instance.CreateConnection();
            connection.ConnectionString = null;
            Assert.Throws<InvalidOperationException>(() => connection.Open());
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void BeginTransaction_IsolationLevelIsUnspecified_SetsReadCommitted()
        {
            Assert.Equal(IsolationLevel.ReadCommitted, transaction.IsolationLevel);
            transaction.Dispose();
            transaction = connection.BeginTransaction(IsolationLevel.Unspecified);
            Assert.Equal(IsolationLevel.ReadCommitted, transaction.IsolationLevel);
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(IsolationLevelsExceptUnspecified))]
        public void BeginTransaction_SpecificIsolationLevel_Success(IsolationLevel isolationLevel)
        {
            transaction.Dispose();
            transaction = connection.BeginTransaction(isolationLevel);
            Assert.Equal(isolationLevel, transaction.IsolationLevel);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void StateChange_ChangeState_TriggersEvent()
        {
            int timesCalled = 0;
            Action<object, StateChangeEventArgs> OnStateChange = (sender, args) => {
                timesCalled++;
            };
            connection.StateChange += new StateChangeEventHandler(OnStateChange);
            connection.Close();
            connection.Open();
            Assert.Equal(2, timesCalled);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void BeginTransaction_InvalidIsolationLevel_Throws()
        {
            transaction.Dispose();
            Assert.Throws<ArgumentOutOfRangeException>(() => connection.BeginTransaction((IsolationLevel)0));
        }

        [ConditionalFact(Helpers.IsAceDriverAvailable)]
        public void BeginTransaction_CallTwice_Throws()
        {
            // ctor in OleDbTestBase already called BeginTransaction once
            AssertExtensions.Throws<InvalidOperationException>(
                () => connection.BeginTransaction(),
                $"{nameof(OleDbConnection)} does not support parallel transactions."
            );
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void GetDefaults_AnyGivenState_DoesNotThrow()
        {
            const int DefaultTimeout = 15;
            Action VerifyDefaults = () => {
                Assert.Equal(DefaultTimeout, connection.ConnectionTimeout);
                Assert.Contains(connection.DataSource, TestDirectory);
                Assert.Empty(connection.Database);
            };
            VerifyDefaults();
            connection.Close();
            VerifyDefaults();
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
        public void GetSchema_NoArgs_ReturnsMetaDataCollections()
        {
            if (PlatformDetection.IsWindows7)
            {
                return; // see https://github.com/dotnet/corefx/pull/37450
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
                return; // see https://github.com/dotnet/corefx/pull/37450
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
            Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(null));
            Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(" "));
            Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(string.Empty));
            AssertExtensions.Throws<InvalidOperationException>(
                () => connection.ChangeDatabase("ReadOnlyShouldThrow"),
                "The 'current catalog' property was read-only, or the consumer attempted to set values of properties " +
                "in the Initialization property group after the data source object was initialized. " +
                "Consumers can set the value of a read-only property to its current value. " +
                "This status is also returned if a settable column property could not be set for the particular column."
            );
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(ManufacturedOleDbSchemaGuids))]
        public void GetOleDbSchemaTable_NoRestrictions_Success(Guid oleDbSchemaGuid)
        {
            DataTable oleDbSchemaTable = connection.GetOleDbSchemaTable(oleDbSchemaGuid, restrictions: null);
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
            Assert.Throws<ArgumentException>(() => connection.GetOleDbSchemaTable(oleDbSchemaGuid, restrictions));
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
            connection.Dispose();
            connection = new OleDbConnection(@"file name = " + udlFile);
            Assert.NotNull(connection);
            connection.Open();
            Assert.Equal(Helpers.ProviderName, connection.Provider);
            connection.Dispose();
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

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void TransactionRollBackTest()
        {
            using (OleDbConnection connection = new OleDbConnection(ConnectionString))
            {
                connection.Open();
                OleDbTransaction transaction = connection.BeginTransaction();
                // This call shouldn't throw
                transaction.Rollback();
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ServerVersionTest()
        {
            using (OleDbConnection connection = new OleDbConnection(ConnectionString))
            {
                connection.Open();
                string version = connection.ServerVersion;
                Assert.False(string.IsNullOrEmpty(version));
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ConnectionDatabasePropertyTest()
        {
            using (OleDbConnection connection = new OleDbConnection(ConnectionString))
            {
                connection.Open();

                // This call shouldn't throw
                _ = connection.Database;
            }
        }

        public static IEnumerable<object[]> ProviderNamesForConnectionString
        {
            get
            {
                // This provider must exist on the test OS.
                yield return new object[] { Helpers.ProviderName };
                // This is a non-existent provider.
                yield return new object[] { "Unavailable" };
            }
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(ProviderNamesForConnectionString))]
        public void ConnectionStringTest(string provider)
        {
            // This should be a provider that exists in the test environment
            OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder
            {
                Provider = provider,
                DataSource = "myDB.mdb"
            };
            string connStr = builder.ConnectionString;
            Assert.Equal($"Provider={provider};Data Source=myDB.mdb", connStr);
        }

        // Bug #96278 fixed only on .NET, not on .NET Framework
        [ConditionalFact(Helpers.IsDriverAvailable)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void DbConnectionFactories_GetFactory_from_connection()
        {
            DbProviderFactory factory = DbProviderFactories.GetFactory(connection);
            Assert.Same(OleDbFactory.Instance, factory);
        }
    }
}
