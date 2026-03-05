// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ChainedConfigurationProvierTests
    {
        [Fact]
        public void ChainedConfiguration_UsingMemoryConfigurationSource_ChainedCouldExposeProvider()
        {
            var chainedConfigurationProvider = new ChainedConfigurationSource
                {
                    Configuration = new ConfigurationBuilder()
                            .Add(new MemoryConfigurationSource {
                                InitialData = new Dictionary<string, string>() { { "a:b", "c" } }
                            })
                            .Build(),
                    ShouldDisposeConfiguration = false,
                }
                .Build(new ConfigurationBuilder()) as ChainedConfigurationProvider;

            Assert.True(chainedConfigurationProvider.TryGet("a:b", out string? value));
            Assert.Equal("c", value);
            Assert.Equal("c", chainedConfigurationProvider.Configuration["a:b"]);

            var configRoot = chainedConfigurationProvider.Configuration as IConfigurationRoot;
            Assert.NotNull(configRoot);
            Assert.Equal(1, configRoot.Providers.Count());
            Assert.IsType<MemoryConfigurationProvider>(configRoot.Providers.First());
        }

        [Fact]
        public void ChainedConfiguration_ExposesProvider()
        {
            var providers = new IConfigurationProvider[] {
                new TestConfigurationProvider("foo", "foo-value")
            };
            var chainedConfigurationSource = new ChainedConfigurationSource
            {
                Configuration = new ConfigurationRoot(providers),
                ShouldDisposeConfiguration = false,
            };

            var chainedConfigurationProvider = chainedConfigurationSource
                .Build(new ConfigurationBuilder()) as ChainedConfigurationProvider;

            var configRoot = chainedConfigurationProvider.Configuration as IConfigurationRoot;
            Assert.NotNull(configRoot);
            Assert.Equal(providers, configRoot.Providers);
        }

        [Fact]
        public void ChainedConfiguration_ReloadPropagatesToInnerConfigurationRoot()
        {
            var innerConfig = new ConfigurationBuilder()
                .Add(new CountingValueConfigurationSource())
                .Build();

            var outerConfig = new ConfigurationBuilder()
                .AddConfiguration(innerConfig)
                .Build();

            Assert.Equal("1", outerConfig["SomeValue"]);

            outerConfig.Reload();

            Assert.Equal("2", outerConfig["SomeValue"]);
        }

        [Fact]
        public void ChainedConfiguration_ReloadDoesNotPropagateToInnerConfigurationSection()
        {
            var innerConfig = new ConfigurationBuilder()
                .Add(new CountingValueConfigurationSource("Section:SomeValue"))
                .Build();

            var outerConfig = new ConfigurationBuilder()
                .AddConfiguration(innerConfig.GetSection("Section"))
                .Build();

            Assert.Equal("1", outerConfig["SomeValue"]);

            outerConfig.Reload();

            Assert.Equal("1", outerConfig["SomeValue"]);
        }

        [Fact]
        public void ChainedConfiguration_BuildingOuterConfigurationRoot_DoesNotReloadInnerConfigurationRoot()
        {
            var innerProvider = new CountingValueConfigurationProvider("Value");
            var innerConfig = new ConfigurationRoot(new[] { innerProvider });

            int notifications = 0;
            innerConfig.GetReloadToken().RegisterChangeCallback(_ => notifications++, state: null);

            var outerConfig = new ConfigurationBuilder()
                .AddConfiguration(innerConfig)
                .Build();

            Assert.Equal(1, innerProvider.LoadCount);
            Assert.Equal("1", innerConfig["Value"]);
            Assert.Equal("1", outerConfig["Value"]);
            Assert.Equal(0, notifications);
        }

        [Fact]
        public void ChainedConfiguration_AddingToConfigurationManager_DoesNotReloadInnerConfigurationRoot()
        {
            var innerProvider = new CountingValueConfigurationProvider("Value");
            var innerConfig = new ConfigurationRoot(new[] { innerProvider });

            int notifications = 0;
            innerConfig.GetReloadToken().RegisterChangeCallback(_ => notifications++, state: null);

            var outerConfig = new ConfigurationManager();
            outerConfig.AddConfiguration(innerConfig);

            Assert.Equal(1, innerProvider.LoadCount);
            Assert.Equal("1", innerConfig["Value"]);
            Assert.Equal("1", outerConfig["Value"]);
            Assert.Equal(0, notifications);
        }

        [Fact]
        public void ChainedConfiguration_ReloadingOuterConfigurationRoot_RaisesSingleOuterNotificationAndNoInnerNotification()
        {
            var innerProvider = new CountingValueConfigurationProvider("Value");
            var innerConfig = new ConfigurationRoot(new[] { innerProvider });

            var outerConfig = new ConfigurationBuilder()
                .AddConfiguration(innerConfig)
                .Build();

            int innerNotifications = 0;
            int outerNotifications = 0;

            innerConfig.GetReloadToken().RegisterChangeCallback(_ => innerNotifications++, state: null);
            outerConfig.GetReloadToken().RegisterChangeCallback(_ => outerNotifications++, state: null);

            outerConfig.Reload();

            Assert.Equal(2, innerProvider.LoadCount);
            Assert.Equal("2", innerConfig["Value"]);
            Assert.Equal("2", outerConfig["Value"]);
            Assert.Equal(1, outerNotifications);
            Assert.Equal(0, innerNotifications);
        }

        private class TestConfigurationProvider : ConfigurationProvider
        {
            public TestConfigurationProvider(string key, string value)
                => Data.Add(key, value);
        }

        private class CountingValueConfigurationSource : IConfigurationSource
        {
            private readonly string _key;

            public CountingValueConfigurationSource(string key = "SomeValue")
                => _key = key;

            public IConfigurationProvider Build(IConfigurationBuilder builder)
                => new CountingValueConfigurationProvider(_key);
        }

        private class CountingValueConfigurationProvider : ConfigurationProvider
        {
            private readonly string _key;

            public CountingValueConfigurationProvider(string key)
                => _key = key;

            public int LoadCount { get; private set; }

            public override void Load()
                => Data[_key] = (++LoadCount).ToString(CultureInfo.InvariantCulture);
        }
    }
}
