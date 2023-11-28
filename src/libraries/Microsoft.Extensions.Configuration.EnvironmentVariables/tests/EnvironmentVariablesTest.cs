// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables.Test
{
    public class EnvironmentVariablesTest
    {
        [Fact]
        public void LoadKeyValuePairsFromEnvironmentDictionary()
        {
            var dict = new Hashtable()
                {
                    {"DefaultConnection:ConnectionString", "TestConnectionString"},
                    {"DefaultConnection:Provider", "SqlClient"},
                    {"Inventory:ConnectionString", "AnotherTestConnectionString"},
                    {"Inventory:Provider", "MySql"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider(null);

            envConfigSrc.Load(dict);

            Assert.Equal("TestConnectionString", envConfigSrc.Get("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", envConfigSrc.Get("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", envConfigSrc.Get("Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", envConfigSrc.Get("Inventory:Provider"));
            Assert.Equal("EnvironmentVariablesConfigurationProvider", envConfigSrc.ToString());
        }

        [Fact]
        public void LoadKeyValuePairsFromEnvironmentDictionaryWithPrefix()
        {
            var dict = new Hashtable()
                {
                    {"DefaultConnection:ConnectionString", "TestConnectionString"},
                    {"DefaultConnection:Provider", "SqlClient"},
                    {"Inventory:ConnectionString", "AnotherTestConnectionString"},
                    {"Inventory:Provider", "MySql"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("DefaultConnection:");

            envConfigSrc.Load(dict);

            Assert.Equal("TestConnectionString", envConfigSrc.Get("ConnectionString"));
            Assert.Equal("SqlClient", envConfigSrc.Get("Provider"));
            Assert.Equal("EnvironmentVariablesConfigurationProvider Prefix: 'DefaultConnection:'", envConfigSrc.ToString());
        }

        [Fact]
        public void LoadKeyValuePairsFromAzureEnvironment()
        {
            var dict = new Hashtable()
                {
                    {"APPSETTING_AppName", "TestAppName"},
                    {"CUSTOMCONNSTR_db1", "CustomConnStr"},
                    {"SQLCONNSTR_db2", "SQLConnStr"},
                    {"MYSQLCONNSTR_db3", "MySQLConnStr"},
                    {"SQLAZURECONNSTR_db4", "SQLAzureConnStr"},
                    {"CommonEnv", "CommonEnvValue"},
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider();

            envConfigSrc.Load(dict);

            string value;
            Assert.Equal("TestAppName", envConfigSrc.Get("APPSETTING_AppName"));
            Assert.False(envConfigSrc.TryGet("AppName", out value));
            Assert.Equal("CustomConnStr", envConfigSrc.Get("ConnectionStrings:db1"));
            Assert.Equal("SQLConnStr", envConfigSrc.Get("ConnectionStrings:db2"));
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Get("ConnectionStrings:db2_ProviderName"));
            Assert.Equal("MySQLConnStr", envConfigSrc.Get("ConnectionStrings:db3"));
            Assert.Equal("MySql.Data.MySqlClient", envConfigSrc.Get("ConnectionStrings:db3_ProviderName"));
            Assert.Equal("SQLAzureConnStr", envConfigSrc.Get("ConnectionStrings:db4"));
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Get("ConnectionStrings:db4_ProviderName"));
            Assert.Equal("CommonEnvValue", envConfigSrc.Get("CommonEnv"));
        }

        [Fact]
        public void LoadKeyValuePairsFromAzureEnvironmentWithPrefix()
        {
            var dict = new Hashtable()
            {
                {"CUSTOMCONNSTR_db1", "CustomConnStr"},
                {"SQLCONNSTR_db2", "SQLConnStr"},
                {"MYSQLCONNSTR_db3", "MySQLConnStr"},
                {"SQLAZURECONNSTR_db4", "SQLAzureConnStr"},
                {"CommonEnv", "CommonEnvValue"},
            };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("ConnectionStrings:");

            envConfigSrc.Load(dict);

            Assert.Equal("CustomConnStr", envConfigSrc.Get("db1"));
            Assert.Equal("SQLConnStr", envConfigSrc.Get("db2"));
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Get("db2_ProviderName"));
            Assert.Equal("MySQLConnStr", envConfigSrc.Get("db3"));
            Assert.Equal("MySql.Data.MySqlClient", envConfigSrc.Get("db3_ProviderName"));
            Assert.Equal("SQLAzureConnStr", envConfigSrc.Get("db4"));
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Get("db4_ProviderName"));
        }

        [Fact]
        public void LastVariableAddedWhenKeyIsDuplicatedInAzureEnvironment()
        {
            var dict = new Hashtable()
                {
                    {"ConnectionStrings:db2", "CommonEnvValue"},
                    {"SQLCONNSTR_db2", "SQLConnStr"},
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider();

            envConfigSrc.Load(dict);

            Assert.True(!string.IsNullOrEmpty(envConfigSrc.Get("ConnectionStrings:db2")));
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Get("ConnectionStrings:db2_ProviderName"));
        }

        [Fact]
        public void LastVariableAddedWhenMultipleEnvironmentVariablesWithSameNameButDifferentCaseExist()
        {
            var dict = new Hashtable()
                {
                    {"CommonEnv", "CommonEnvValue1"},
                    {"commonenv", "commonenvValue2"},
                    {"cOMMonEnv", "commonenvValue3"},
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider();

            envConfigSrc.Load(dict);

            Assert.True(!string.IsNullOrEmpty(envConfigSrc.Get("cOMMonEnv")));
            Assert.True(!string.IsNullOrEmpty(envConfigSrc.Get("CommonEnv")));
        }

        [Fact]
        public void ReplaceDoubleUnderscoreInEnvironmentVariables()
        {
            var dict = new Hashtable()
                {
                    {"data__ConnectionString", "connection"},
                    {"SQLCONNSTR_db1", "connStr"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider();

            envConfigSrc.Load(dict);

            Assert.Equal("connection", envConfigSrc.Get("data:ConnectionString"));
            Assert.Equal("System.Data.SqlClient", envConfigSrc.Get("ConnectionStrings:db1_ProviderName"));
        }

        [Fact]
        public void ReplaceDoubleUnderscoreInEnvironmentVariablesDoubleUnderscorePrefixStillMatches()
        {
            var dict = new Hashtable()
                {
                    {"test__prefix__with__double__underscores__data__ConnectionString", "connection"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("test__prefix__with__double__underscores__");

            envConfigSrc.Load(dict);

            Assert.Equal("connection", envConfigSrc.Get("data:ConnectionString"));
        }

        [Fact]
        public void MixingPathSeparatorsInPrefixStillMatchesEnvironmentVariable()
        {
            var dict = new Hashtable()
                {
                    {"_____EXPERIMENTAL__data__ConnectionString", "connection"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("::_EXPERIMENTAL:");

            envConfigSrc.Load(dict);

            Assert.Equal("connection", envConfigSrc.Get("data:ConnectionString"));
        }

        [Fact]
        public void OnlyASinglePrefixIsRemovedFromMatchingKey()
        {
            var dict = new Hashtable()
                {
                    {"test__test__ConnectionString", "connection"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("test__");

            envConfigSrc.Load(dict);

            Assert.Equal("connection", envConfigSrc.Get("test:ConnectionString"));
        }

        [Fact]
        public void OnlyEnvironmentVariablesMatchingTheGivenPrefixAreIncluded()
        {
            var dict = new Hashtable()
                {
                    {"projectA__section1__project", "A"},
                    {"projectA__section1__projectA", "true"},
                    {"projectB__section1__project", "B"},
                    {"projectB__section1__projectB", "true"},
                    {"section1__project", "unknown"},
                    {"section1__noProject", "true"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("projectB__");

            envConfigSrc.Load(dict);

            Assert.Equal("B", envConfigSrc.Get("section1:project"));
            Assert.Equal("true", envConfigSrc.Get("section1:projectB"));
            Assert.Throws<InvalidOperationException>(() => envConfigSrc.Get("section1:projectA"));
            Assert.Throws<InvalidOperationException>(() => envConfigSrc.Get("section1:noProject"));
        }

        [Fact]
        public void PrefixPreventsLoadingSqlConnectionStrings()
        {
            var dict = new Hashtable()
                {
                    {"test__test__ConnectionString", "connection"},
                    {"SQLCONNSTR_db1", "connStr"}
                };
            var envConfigSrc = new EnvironmentVariablesConfigurationProvider("test:");

            envConfigSrc.Load(dict);

            Assert.Equal("connection", envConfigSrc.Get("test:ConnectionString"));
            Assert.Throws<InvalidOperationException>(() => envConfigSrc.Get("ConnectionStrings:db1_ProviderName"));
        }

        public const string EnvironmentVariable = "Microsoft__Extensions__Configuration__EnvironmentVariables__Test__Foo";
        public class SettingsWithFoo
        {
            public string? Foo { get; set; }
        }

        [Fact]
        public void AddEnvironmentVariablesUsingNormalizedPrefix_Bind_PrefixMatches()
        {
            try
            {
                Environment.SetEnvironmentVariable(EnvironmentVariable, "myFooValue");
                var configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables("Microsoft:Extensions:Configuration:EnvironmentVariables:Test:")
                    .Build();

                var settingsWithFoo = new SettingsWithFoo();
                configuration.Bind(settingsWithFoo);

                Assert.Equal("myFooValue", settingsWithFoo.Foo);
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvironmentVariable, null);
            }
        }

        [Fact]
        public void AddEnvironmentVariablesUsingPrefixWithDoubleUnderscores_Bind_PrefixMatches()
        {
            try
            {
                Environment.SetEnvironmentVariable(EnvironmentVariable, "myFooValue");
                var configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables("Microsoft__Extensions__Configuration__EnvironmentVariables__Test__")
                    .Build();

                var settingsWithFoo = new SettingsWithFoo();
                configuration.Bind(settingsWithFoo);

                Assert.Equal("myFooValue", settingsWithFoo.Foo);
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvironmentVariable, null);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void BindingDoesNotThrowIfReloadedDuringBinding()
        {
            var dic = new Dictionary<string, string>
            {
                {"Number", "-2"},
                {"Text", "Foo"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            configurationBuilder.AddEnvironmentVariables();
            var config = configurationBuilder.Build();

            MyOptions options = null;

            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250)))
            {
                void ReloadLoop()
                {
                    while (!cts.IsCancellationRequested)
                    {
                        config.Reload();
                    }
                }

                _ = Task.Run(ReloadLoop);

                while (!cts.IsCancellationRequested)
                {
                    options = config.Get<MyOptions>();
                }
            }

            Assert.Equal(-2, options.Number);
            Assert.Equal("Foo", options.Text);
        }

        private sealed class MyOptions
        {
            public int Number { get; set; }
            public string Text { get; set; }
        }
    }
}
