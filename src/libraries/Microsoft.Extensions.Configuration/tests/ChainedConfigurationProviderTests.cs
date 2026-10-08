// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
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

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_OverConfigurationRoot_TryGetFindsKeyWithNonNullValue(string value)
        {
            IConfigurationProvider provider = BuildChainedProvider(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } })
                .Build());

            Assert.True(provider.TryGet("Key", out string? actual));
            Assert.Equal(value, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_OverConfigurationSection_TryGetFindsKeyWithNonNullValue(string value)
        {
            IConfigurationProvider provider = BuildChainedProvider(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Section:Key", value } })
                .Build()
                .GetSection("Section"));

            Assert.True(provider.TryGet("Key", out string? actual));
            Assert.Equal(value, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_OverConfigurationManager_TryGetFindsKeyWithNonNullValue(string value)
        {
            using var inner = new ConfigurationManager();
            inner.AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } });

            IConfigurationProvider provider = BuildChainedProvider(inner);

            Assert.True(provider.TryGet("Key", out string? actual));
            Assert.Equal(value, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_ShadowsPrecedingProvider(string value)
        {
            var inner = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } })
                .Build();

            var outer = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", "earlier-value" } })
                .AddConfiguration(inner)
                .Build();

            Assert.Equal(value, outer["Key"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_OverConfigurationSection_ShadowsPrecedingProvider(string value)
        {
            var inner = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Section:Key", value } })
                .Build();

            var outer = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", "earlier-value" } })
                .AddConfiguration(inner.GetSection("Section"))
                .Build();

            Assert.Equal(value, outer["Key"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_BindsSameValueAsAnEquivalentDirectlyAddedSource(string value)
        {
            var direct = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", "earlier-value" } })
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } })
                .Build();

            var chained = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", "earlier-value" } })
                .AddConfiguration(new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } })
                    .Build())
                .Build();

            var directOptions = new OptionsWithPresetValue();
            var chainedOptions = new OptionsWithPresetValue();

#pragma warning disable IL2026, IL3050 // https://github.com/dotnet/runtime/issues/126862
            direct.Bind(directOptions);
            chained.Bind(chainedOptions);
#pragma warning restore IL2026, IL3050

            Assert.Equal(value, directOptions.Key);
            Assert.Equal(value, chainedOptions.Key);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_TryGetAgreesWithGetChildKeys(string value)
        {
            IConfigurationProvider provider = BuildChainedProvider(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } })
                .Build());

            // GetChildKeys enumerates the wrapped configuration, which lists a key whatever its value. TryGet has to
            // agree, otherwise the provider announces a key that it then refuses to return.
            Assert.Contains("Key", provider.GetChildKeys(Array.Empty<string>(), parentPath: null));
            Assert.True(provider.TryGet("Key", out _));
        }

        public static TheoryData<Func<IConfigurationRoot, IConfiguration>> ChainedConfigurationKinds => new()
        {
            root => root,
            root => root.GetSection("Section"),
            root => new PlainConfiguration(root),
        };

        [Theory]
        [MemberData(nameof(ChainedConfigurationKinds))]
        public void ChainedConfiguration_NullValueIsNotContributed(Func<IConfigurationRoot, IConfiguration> selectConfiguration)
        {
            var inner = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { "Key", null }, { "Section:Key", null } })
                .Build();

            var outer = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", "earlier-value" } })
                .AddConfiguration(selectConfiguration(inner))
                .Build();

            // A chained configuration is a merged unit, and a unit reports the absence of a value as null. There is
            // nothing to contribute, so the preceding provider still wins.
            Assert.Equal("earlier-value", outer["Key"]);
        }

        [Theory]
        [MemberData(nameof(ChainedConfigurationKinds))]
        public void ChainedConfiguration_TryGetReturnsFalseForMissingKey(Func<IConfigurationRoot, IConfiguration> selectConfiguration)
        {
            var inner = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Section:Key", "inner-value" } })
                .Build();

            IConfigurationProvider provider = BuildChainedProvider(selectConfiguration(inner));

            Assert.False(provider.TryGet("MissingKey", out string? value));
            Assert.Null(value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("inner-value")]
        public void ChainedConfiguration_SectionWithNonNullValueExists(string value)
        {
            var outer = new ConfigurationBuilder()
                .AddConfiguration(new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { { "Key", value } })
                    .Build())
                .Build();

            Assert.True(outer.GetSection("Key").Exists());
            Assert.Equal(value, outer.GetRequiredSection("Key").Value);
        }

        [Fact]
        public void ChainedConfiguration_EmptyValueShadowingATypedValue_FailsToBindLikeADirectlyAddedSource()
        {
            static IConfigurationBuilder AddEarlierProvider(IConfigurationBuilder builder)
                => builder.AddInMemoryCollection(new Dictionary<string, string> { { "Port", "9000" } });

            var direct = AddEarlierProvider(new ConfigurationBuilder())
                .AddInMemoryCollection(new Dictionary<string, string> { { "Port", "" } })
                .Build();

            var chained = AddEarlierProvider(new ConfigurationBuilder())
                .AddConfiguration(new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { { "Port", "" } })
                    .Build())
                .Build();

            // An empty value shadows the earlier provider, so binding it to a non-string type fails. This matches
            // what an equivalent directly added source has always done.
#pragma warning disable IL2026, IL3050 // https://github.com/dotnet/runtime/issues/126862
            Assert.Throws<InvalidOperationException>(() => direct.Bind(new TypedOptions()));
            Assert.Throws<InvalidOperationException>(() => chained.Bind(new TypedOptions()));
#pragma warning restore IL2026, IL3050
        }

        [Fact]
        public void ChainedConfiguration_MissingKeyDoesNotShadowPrecedingProvider()
        {
            var outer = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Key", "earlier-value" } })
                .AddConfiguration(new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { { "OtherKey", "inner-value" } })
                    .Build())
                .Build();

            Assert.Equal("earlier-value", outer["Key"]);
        }

        private static IConfigurationProvider BuildChainedProvider(IConfiguration configuration)
            => new ChainedConfigurationSource
            {
                Configuration = configuration,
                ShouldDisposeConfiguration = false,
            }
            .Build(new ConfigurationBuilder());

        private class OptionsWithPresetValue
        {
            public string Key { get; set; } = "preset-value";
        }

        private class TypedOptions
        {
            public int Port { get; set; }
        }

        private class PlainConfiguration : IConfiguration
        {
            private readonly IConfiguration _inner;

            public PlainConfiguration(IConfiguration inner) => _inner = inner;

            public string? this[string key]
            {
                get => _inner[key];
                set => _inner[key] = value;
            }

            public IEnumerable<IConfigurationSection> GetChildren() => _inner.GetChildren();

            public IChangeToken GetReloadToken() => _inner.GetReloadToken();

            public IConfigurationSection GetSection(string key) => _inner.GetSection(key);
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
