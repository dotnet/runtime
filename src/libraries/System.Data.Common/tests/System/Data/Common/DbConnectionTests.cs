// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.Common.Tests
{
    public class DbConnectionTests
    {
        private static volatile bool _wasFinalized;

        private class MockDbConnection : DbConnection
        {
            [AllowNull]
            public override string ConnectionString
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public override string Database => throw new NotImplementedException();
            public override string DataSource => throw new NotImplementedException();
            public override string ServerVersion => throw new NotImplementedException();
            public override ConnectionState State => throw new NotImplementedException();
            public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
            public override void Close() => throw new NotImplementedException();
            public override void Open() => throw new NotImplementedException();
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
            protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
        }

        private class FinalizingConnection : MockDbConnection
        {
            public static void CreateAndRelease() => new FinalizingConnection();

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                    _wasFinalized = true;
                base.Dispose(disposing);
            }
        }

        private class GetSchemaConnection : MockDbConnection
        {
            public override DataTable GetSchema()
            {
                var table = new DataTable();
                table.Columns.Add(new DataColumn("CollectionName", typeof(string)));
                table.Columns.Add(new DataColumn("WithRestrictions", typeof(bool)));
                table.Rows.Add("Default", false);
                return table;
            }

            public override DataTable GetSchema(string collectionName)
            {
                var table = new DataTable();
                table.Columns.Add(new DataColumn("CollectionName", typeof(string)));
                table.Columns.Add(new DataColumn("WithRestrictions", typeof(bool)));
                table.Rows.Add(collectionName, false);
                return table;
            }

            public override DataTable GetSchema(string collectionName, string?[] restrictionValues)
            {
                var table = new DataTable();
                table.Columns.Add(new DataColumn("CollectionName", typeof(string)));
                table.Columns.Add(new DataColumn("WithRestrictions", typeof(bool)));
                table.Rows.Add(collectionName, true);
                return table;
            }
        }

        private class DbProviderFactoryConnection : MockDbConnection
        {
            protected override DbProviderFactory DbProviderFactory => TestDbProviderFactory.Instance;
        }

        private class TestDbProviderFactory : DbProviderFactory
        {
            public static DbProviderFactory Instance = new TestDbProviderFactory();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void CanBeFinalized()
        {
            FinalizingConnection.CreateAndRelease();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(_wasFinalized);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15180", TestRuntimes.Mono)]
        public void ProviderFactoryTest()
        {
            DbProviderFactoryConnection con = new DbProviderFactoryConnection();
            PropertyInfo providerFactoryProperty = con.GetType().GetProperty("ProviderFactory", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Assert.NotNull(providerFactoryProperty);
            DbProviderFactory? factory = providerFactoryProperty.GetValue(con) as DbProviderFactory;
            Assert.NotNull(factory);
            Assert.Same(typeof(TestDbProviderFactory), factory!.GetType());
            Assert.Same(TestDbProviderFactory.Instance, factory);
        }

        [Fact]
        public void GetSchemaAsync_with_cancelled_token()
        {
            var conn = new MockDbConnection();
            Assert.ThrowsAsync<TaskCanceledException>(async () => await conn.GetSchemaAsync(new CancellationToken(true)));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await conn.GetSchemaAsync("MetaDataCollections", new CancellationToken(true)));
            Assert.ThrowsAsync<TaskCanceledException>(async () => await conn.GetSchemaAsync("MetaDataCollections", new string[0], new CancellationToken(true)));
        }

        [Fact]
        public void GetSchemaAsync_with_exception()
        {
            var conn = new MockDbConnection();
            Assert.ThrowsAsync<NotSupportedException>(async () => await conn.GetSchemaAsync());
            Assert.ThrowsAsync<NotSupportedException>(async () => await conn.GetSchemaAsync("MetaDataCollections"));
            Assert.ThrowsAsync<NotSupportedException>(async () => await conn.GetSchemaAsync("MetaDataCollections", new string[0]));
        }

        [Fact]
        public async Task GetSchemaAsync_calls_GetSchema()
        {
            var conn = new GetSchemaConnection();

            var row = (await conn.GetSchemaAsync()).Rows[0];
            Assert.Equal("Default", row["CollectionName"]);
            Assert.Equal(false, row["WithRestrictions"]);

            row = (await conn.GetSchemaAsync("MetaDataCollections")).Rows[0];
            Assert.Equal("MetaDataCollections", row["CollectionName"]);
            Assert.Equal(false, row["WithRestrictions"]);

            row = (await conn.GetSchemaAsync("MetaDataCollections", new string?[0])).Rows[0];
            Assert.Equal("MetaDataCollections", row["CollectionName"]);
            Assert.Equal(true, row["WithRestrictions"]);
        }
    }
}
