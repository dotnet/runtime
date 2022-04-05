// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ChainedConfigurationProvierTests
    {
        [Fact]
        public void Configuration_Basic()
        {
            var inputData = new Dictionary<string, string>() { { "a:b", "c" } };

            ChainedConfigurationProvider chainedConfig;
            chainedConfig = new ChainedConfigurationProvider(new ChainedConfigurationSource
            {
                Configuration = new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource { InitialData = inputData })
                    .Build(),
                ShouldDisposeConfiguration = false,
            });

            Assert.True(chainedConfig.TryGet("a:b", out string? value));
            Assert.Equal("c", value);
            Assert.Equal("c", chainedConfig.Configuration["a:b"]);
        }

        [Fact]
        public void ConfigurationRoot()
        {
            var inputData = new Dictionary<string, string>() { { "a:b", "c" } };
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                    .Add(new MemoryConfigurationSource { InitialData = inputData })
                    .Build();

            var chainedConfigurationProvider = new ChainedConfigurationProvider(new ChainedConfigurationSource
            {
                Configuration = configRoot,
                ShouldDisposeConfiguration = false,
            });

            Assert.NotNull(chainedConfigurationProvider.Configuration as IConfigurationRoot);
            Assert.NotNull((chainedConfigurationProvider.Configuration as IConfigurationRoot).Providers);
        }

        [Fact]
        public void ChainedConfigurationCouldExposeProvider()
        {
            var providers = new IConfigurationProvider[] {
                new TestConfigurationProvider("foo", "foo-value")
            };

            var configRoot = new ConfigurationRoot(providers);

            var chainedConfigurationSource = new ChainedConfigurationSource
            {
                Configuration = configRoot,
                ShouldDisposeConfiguration = false,
            };

            var chainedConfigurationProvider = chainedConfigurationSource
                .Build(new ConfigurationBuilder()) as ChainedConfigurationProvider;

            Assert.NotNull(chainedConfigurationProvider.Configuration as IConfigurationRoot);
            Assert.Equal(providers, (chainedConfigurationProvider.Configuration as IConfigurationRoot).Providers);
        }

        private class TestConfigurationProvider : ConfigurationProvider
        {
            public TestConfigurationProvider(string key, string value)
                => Data.Add(key, value);
        }
    }
}
