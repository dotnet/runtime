// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationSectionDebugViewTest
    {
        [Fact]
        public void FromConfiguration_Root()
        {
            var config = new ConfigurationManager();

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            });

            var items = ConfigurationSectionDebugView.FromConfiguration(config, config);

            Assert.Collection(items,
                i =>
                {
                    Assert.Equal("Mem1", i.Path);
                    Assert.Null(i.Value);
                    Assert.Null(i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem1:", i.Path);
                    Assert.Equal("NoKeyValue1", i.Value);
                    Assert.IsType<MemoryConfigurationProvider>(i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem1:KeyInMem1", i.Path);
                    Assert.Equal("ValueInMem1", i.Value);
                    Assert.IsType<MemoryConfigurationProvider>(i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem1:KeyInMem1:Deep1", i.Path);
                    Assert.Equal("ValueDeep1", i.Value);
                    Assert.IsType<MemoryConfigurationProvider>(i.Provider);
                });
        }

        [Fact]
        public void FromConfiguration_Section()
        {
            var config = new ConfigurationManager();

            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:", "NoKeyValue2"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"},
                {"Mem1:KeyInMem2", "ValueInMem1"}
            });

            var section = config.GetSection("Mem1:KeyInMem1");

            var items = ConfigurationSectionDebugView.FromConfiguration(section, config);

            Assert.Collection(items,
                i =>
                {
                    Assert.Equal("", i.Path);
                    Assert.Equal("NoKeyValue2", i.Value);
                    Assert.IsType<MemoryConfigurationProvider>(i.Provider);
                },
                i =>
                {
                    Assert.Equal("Deep1", i.Path);
                    Assert.Equal("ValueDeep1", i.Value);
                    Assert.IsType<MemoryConfigurationProvider>(i.Provider);
                });
        }

        [Fact]
        public void FromConfiguration_MultipleProviders()
        {
            var provider1 = new TestMemorySourceProvider(new Dictionary<string, string>
            {
                {"Mem1:", "NoKeyValue1"},
                {"Key1", "ValueInMem1"}
            });
            var provider2 = new TestMemorySourceProvider(new Dictionary<string, string>
            {
                {"Mem2:", "NoKeyValue2"},
                {"Key2", "ValueInMem2"}
            });

            var config = new ConfigurationManager();
            config.Sources.Add(provider1);
            config.Sources.Add(provider2);

            var items = ConfigurationSectionDebugView.FromConfiguration(config, config);

            Assert.Collection(items,
                i =>
                {
                    Assert.Equal("Key1", i.Path);
                    Assert.Equal("Key1", i.Key);
                    Assert.Equal("ValueInMem1", i.Value);
                    Assert.Equal(provider1, i.Provider);
                },
                i =>
                {
                    Assert.Equal("Key2", i.Path);
                    Assert.Equal("Key2", i.Key);
                    Assert.Equal("ValueInMem2", i.Value);
                    Assert.Equal(provider2, i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem1", i.Path);
                    Assert.Equal("Mem1", i.Key);
                    Assert.Null(i.Value);
                    Assert.Null(i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem1:", i.Path);
                    Assert.Equal(string.Empty, i.Key);
                    Assert.Equal("NoKeyValue1", i.Value);
                    Assert.Equal(provider1, i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem2", i.Path);
                    Assert.Equal("Mem2", i.Key);
                    Assert.Null(i.Value);
                    Assert.Null(i.Provider);
                },
                i =>
                {
                    Assert.Equal("Mem2:", i.Path);
                    Assert.Equal(string.Empty, i.Key);
                    Assert.Equal("NoKeyValue2", i.Value);
                    Assert.Equal(provider2, i.Provider);
                });
        }

        public class TestMemorySourceProvider : MemoryConfigurationProvider, IConfigurationSource
        {
            public TestMemorySourceProvider(Dictionary<string, string> initialData)
                : base(new MemoryConfigurationSource { InitialData = initialData })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                return this;
            }
        }
    }
}
