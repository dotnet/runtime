// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Xunit;

namespace System.Data.Common
{
    public sealed class TestProviderFactory : DbProviderFactory
    {
        public static readonly TestProviderFactory Instance = new TestProviderFactory();
        private TestProviderFactory() { }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
    public class DbProviderFactoriesTests
    {
        [Fact]
        public void GetFactoryClassesDataTableShapeTest()
        {
            DataTable initializedTable = DbProviderFactories.GetFactoryClasses();
            Assert.NotNull(initializedTable);
            Assert.Equal(4, initializedTable.Columns.Count);
            Assert.Equal("Name", initializedTable.Columns[0].ColumnName);
            Assert.Equal("Description", initializedTable.Columns[1].ColumnName);
            Assert.Equal("InvariantName", initializedTable.Columns[2].ColumnName);
            Assert.Equal("AssemblyQualifiedName", initializedTable.Columns[3].ColumnName);
        }

        [Fact]
        public void GetFactoryNoRegistrationTest()
        {
            ClearRegisteredFactories();
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory("System.Data.SqlClient"));
        }

        [Fact]
        public void GetFactoryWithInvariantNameTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory)));
            DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            Assert.NotNull(factory);
            Assert.Equal(typeof(System.Data.SqlClient.SqlClientFactory), factory.GetType());
            Assert.Equal(System.Data.SqlClient.SqlClientFactory.Instance, factory);
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void GetFactoryWithDbConnectionTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' and 'SqlConnection' are obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory)));
            DbProviderFactory factory = DbProviderFactories.GetFactory(new System.Data.SqlClient.SqlConnection());
            Assert.NotNull(factory);
            Assert.Equal(typeof(System.Data.SqlClient.SqlClientFactory), factory.GetType());
            Assert.Equal(System.Data.SqlClient.SqlClientFactory.Instance, factory);
#pragma warning restore CS0618 // 'SqlClientFactory' and 'SqlConnection' are obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void GetFactoryWithDataRowTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=> DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory)));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithTypeNameTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory).AssemblyQualifiedName));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithTypeTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory)));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithInstanceTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithWrongTypeTest()
        {
            ClearRegisteredFactories();
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory("System.Data.SqlClient"));
#pragma warning disable CS0618 // 'SqlConnection' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            Assert.Throws<ArgumentException>(() => DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlConnection)));
#pragma warning restore CS0618 // 'SqlConnection' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithBadInvariantNameTest()
        {
            ClearRegisteredFactories();
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory("System.Data.SqlClient"));
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            Assert.Throws<ArgumentException>(() => DbProviderFactories.RegisterFactory(string.Empty, typeof(System.Data.SqlClient.SqlClientFactory)));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithAssemblyQualifiedNameTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory).AssemblyQualifiedName));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void RegisterFactoryWithWrongAssemblyQualifiedNameTest()
        {
            ClearRegisteredFactories();
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory("System.Data.SqlClient"));
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(0, providerTable.Rows.Count);
            // register the connection type which is the wrong type. Registraton should succeed, as type registration/checking is deferred.
#pragma warning disable CS0618 // 'SqlConnection' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlConnection).AssemblyQualifiedName);
#pragma warning restore CS0618 // 'SqlConnection' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(1, providerTable.Rows.Count);
            // obtaining the factory will kick in the checks of the registered type name, which will cause exceptions. The checks were deferred till the GetFactory() call.
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory(providerTable.Rows[0]));
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory("System.Data.SqlClient"));
        }

        [Fact]
        public void UnregisterFactoryTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            Assert.True(DbProviderFactories.UnregisterFactory("System.Data.SqlClient"));
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(0, providerTable.Rows.Count);
        }

        [Fact]
        public void TryGetFactoryTest()
        {
            ClearRegisteredFactories();
            Assert.False(DbProviderFactories.TryGetFactory("System.Data.SqlClient", out DbProviderFactory f));
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(() => DbProviderFactories.RegisterFactory("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance));
            Assert.True(DbProviderFactories.TryGetFactory("System.Data.SqlClient", out DbProviderFactory factory));
            Assert.NotNull(factory);
            Assert.Equal(typeof(System.Data.SqlClient.SqlClientFactory), factory.GetType());
            Assert.Equal(System.Data.SqlClient.SqlClientFactory.Instance, factory);
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }

        [Fact]
        public void ReplaceFactoryWithRegisterFactoryWithTypeTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(()=>DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory)));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(TestProviderFactory));
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(1, providerTable.Rows.Count);
            DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            Assert.NotNull(factory);
            Assert.Equal(typeof(TestProviderFactory), factory.GetType());
            Assert.Equal(TestProviderFactory.Instance, factory);
        }

        [Fact]
        public void GetProviderInvariantNamesTest()
        {
            ClearRegisteredFactories();
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            RegisterSqlClientAndTestRegistration(() => DbProviderFactories.RegisterFactory("System.Data.SqlClient", typeof(System.Data.SqlClient.SqlClientFactory)));
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            DbProviderFactories.RegisterFactory("System.Data.Common.TestProvider", typeof(TestProviderFactory));
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(2, providerTable.Rows.Count);
            List<string> invariantNames = DbProviderFactories.GetProviderInvariantNames().ToList();
            Assert.Equal(2, invariantNames.Count);
            Assert.Contains("System.Data.Common.TestProvider", invariantNames);
            Assert.Contains("System.Data.SqlClient", invariantNames);
        }

        private void ClearRegisteredFactories()
        {
            // as the DbProviderFactories table is shared, for tests we need a clean one before a test starts to make sure the tests always succeed.
            Type type = typeof(DbProviderFactories);
            FieldInfo info = type.GetField("_registeredFactories", BindingFlags.NonPublic | BindingFlags.Static);
            IDictionary providerStorage = info.GetValue(null) as IDictionary;
            Assert.NotNull(providerStorage);
            providerStorage.Clear();
            Assert.Equal(0, providerStorage.Count);
        }


        private void RegisterSqlClientAndTestRegistration(Action registrationFunc)
        {
            Assert.NotNull(registrationFunc);
            Assert.Throws<ArgumentException>(() => DbProviderFactories.GetFactory("System.Data.SqlClient"));
            DataTable providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(0, providerTable.Rows.Count);
            registrationFunc();
            providerTable = DbProviderFactories.GetFactoryClasses();
            Assert.Equal(1, providerTable.Rows.Count);
            DbProviderFactory factory = DbProviderFactories.GetFactory(providerTable.Rows[0]);
            Assert.NotNull(factory);
#pragma warning disable CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
            Assert.Equal(typeof(System.Data.SqlClient.SqlClientFactory), factory.GetType());
            Assert.Equal(System.Data.SqlClient.SqlClientFactory.Instance, factory);
#pragma warning restore CS0618 // 'SqlClientFactory' is obsolete: 'Use the Microsoft.Data.SqlClient package instead.
        }
    }
}
