// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

        private class TestConfigurationProvider : ConfigurationProvider
        {
            public TestConfigurationProvider(string key, string value)
                => Data.Add(key, value);
        }
    }
}
