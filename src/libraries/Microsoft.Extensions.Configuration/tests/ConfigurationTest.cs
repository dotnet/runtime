// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationTest
    {
        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationProviders()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2:KeyInMem2", "ValueInMem2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3:KeyInMem3", "ValueInMem3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            var memVal1 = config["mem1:keyinmem1"];
            var memVal2 = config["Mem2:KeyInMem2"];
            var memVal3 = config["MEM3:KEYINMEM3"];

            // Assert
            Assert.Contains(memConfigSrc1, configurationBuilder.Sources);
            Assert.Contains(memConfigSrc2, configurationBuilder.Sources);
            Assert.Contains(memConfigSrc3, configurationBuilder.Sources);

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Equal("ValueInMem1", config["mem1:keyinmem1"]);
            Assert.Equal("ValueInMem2", config["Mem2:KeyInMem2"]);
            Assert.Equal("ValueInMem3", config["MEM3:KEYINMEM3"]);
            Assert.Null(config["NotExist"]);
        }

        [Fact]
        public void GetChildKeys_CanChainEmptyKeys()
        {
            var input = new Dictionary<string, string>() { };
            for (int i = 0; i < 1000; i++)
            {
                input.Add(new string(' ', i), string.Empty);
            }

            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .Add(new MemoryConfigurationSource
                {
                    InitialData = input
                })
                .Build();

            var chainedConfigurationSource = new ChainedConfigurationSource
            {
                Configuration = configurationRoot,
                ShouldDisposeConfiguration = false,
            };
            
            var chainedConfiguration = new ChainedConfigurationProvider(chainedConfigurationSource);
            IEnumerable<string> childKeys = chainedConfiguration.GetChildKeys(new string[0], null);
            Assert.Equal(1000, childKeys.Count());
            Assert.Equal(string.Empty, childKeys.First());
            Assert.Equal(999, childKeys.Last().Length);
        }

        [Fact]
        public void GetChildKeys_CanChainKeyWithNoDelimiter()
        {
            var input = new Dictionary<string, string>() { };
            for (int i = 1000; i < 2000; i++)
            {
                input.Add(i.ToString(), string.Empty);
            }

            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .Add(new MemoryConfigurationSource
                {
                    InitialData = input
                })
                .Build();

            var chainedConfigurationSource = new ChainedConfigurationSource
            {
                Configuration = configurationRoot,
                ShouldDisposeConfiguration = false,
            };
            
            var chainedConfiguration = new ChainedConfigurationProvider(chainedConfigurationSource);
            IEnumerable<string> childKeys = chainedConfiguration.GetChildKeys(new string[0], null);
            Assert.Equal(1000, childKeys.Count());
            Assert.Equal("1000", childKeys.First());
            Assert.Equal("1999", childKeys.Last());
        }

        [Fact]
        public void CanChainConfiguration()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2:KeyInMem2", "ValueInMem2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3:KeyInMem3", "ValueInMem3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            var chained = new ConfigurationBuilder().AddConfiguration(config).Build();
            var memVal1 = chained["mem1:keyinmem1"];
            var memVal2 = chained["Mem2:KeyInMem2"];
            var memVal3 = chained["MEM3:KEYINMEM3"];

            // Assert

            Assert.Equal("ValueInMem1", memVal1);
            Assert.Equal("ValueInMem2", memVal2);
            Assert.Equal("ValueInMem3", memVal3);

            Assert.Null(chained["NotExist"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ChainedAsEnumerateFlattensIntoDictionaryTest(bool removePath)
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2", "Value2"},
                {"Mem2:", "NoKeyValue2"},
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3", "Value3"},
                {"Mem3:", "NoKeyValue3"},
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            var config = new ConfigurationBuilder()
                .AddConfiguration(configurationBuilder.Build())
                .Add(memConfigSrc3)
                .Build();
            var dict = config.AsEnumerable(makePathsRelative: removePath).ToDictionary(k => k.Key, v => v.Value);

            // Assert
            Assert.Equal("Value1", dict["Mem1"]);
            Assert.Equal("NoKeyValue1", dict["Mem1:"]);
            Assert.Equal("ValueDeep1", dict["Mem1:KeyInMem1:Deep1"]);
            Assert.Equal("ValueInMem2", dict["Mem2:KeyInMem2"]);
            Assert.Equal("Value2", dict["Mem2"]);
            Assert.Equal("NoKeyValue2", dict["Mem2:"]);
            Assert.Equal("ValueDeep2", dict["Mem2:KeyInMem2:Deep2"]);
            Assert.Equal("Value3", dict["Mem3"]);
            Assert.Equal("NoKeyValue3", dict["Mem3:"]);
            Assert.Equal("ValueInMem3", dict["Mem3:KeyInMem3"]);
            Assert.Equal("ValueDeep3", dict["Mem3:KeyInMem3:Deep3"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AsEnumerateFlattensIntoDictionaryTest(bool removePath)
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2", "Value2"},
                {"Mem2:", "NoKeyValue2"},
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3", "Value3"},
                {"Mem3:", "NoKeyValue3"},
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);
            var config = configurationBuilder.Build();
            var dict = config.AsEnumerable(makePathsRelative: removePath).ToDictionary(k => k.Key, v => v.Value);

            // Assert
            Assert.Equal("Value1", dict["Mem1"]);
            Assert.Equal("NoKeyValue1", dict["Mem1:"]);
            Assert.Equal("ValueDeep1", dict["Mem1:KeyInMem1:Deep1"]);
            Assert.Equal("ValueInMem2", dict["Mem2:KeyInMem2"]);
            Assert.Equal("Value2", dict["Mem2"]);
            Assert.Equal("NoKeyValue2", dict["Mem2:"]);
            Assert.Equal("ValueDeep2", dict["Mem2:KeyInMem2:Deep2"]);
            Assert.Equal("Value3", dict["Mem3"]);
            Assert.Equal("NoKeyValue3", dict["Mem3:"]);
            Assert.Equal("ValueInMem3", dict["Mem3:KeyInMem3"]);
            Assert.Equal("ValueDeep3", dict["Mem3:KeyInMem3:Deep3"]);
        }

        [Fact]
        public void AsEnumerateStripsKeyFromChildren()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:", "NoKeyValue1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Mem2", "Value2"},
                {"Mem2:", "NoKeyValue2"},
                {"Mem2:KeyInMem2", "ValueInMem2"},
                {"Mem2:KeyInMem2:Deep2", "ValueDeep2"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Mem3", "Value3"},
                {"Mem3:", "NoKeyValue3"},
                {"Mem3:KeyInMem3", "ValueInMem3"},
                {"Mem3:KeyInMem4", "ValueInMem4"},
                {"Mem3:KeyInMem3:Deep3", "ValueDeep3"},
                {"Mem3:KeyInMem3:Deep4", "ValueDeep4"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            var dict = config.GetSection("Mem1").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal(3, dict.Count);
            Assert.Equal("NoKeyValue1", dict[""]);
            Assert.Equal("ValueInMem1", dict["KeyInMem1"]);
            Assert.Equal("ValueDeep1", dict["KeyInMem1:Deep1"]);

            var dict2 = config.GetSection("Mem2").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal(3, dict2.Count);
            Assert.Equal("NoKeyValue2", dict2[""]);
            Assert.Equal("ValueInMem2", dict2["KeyInMem2"]);
            Assert.Equal("ValueDeep2", dict2["KeyInMem2:Deep2"]);

            var dict3 = config.GetSection("Mem3").AsEnumerable(makePathsRelative: true).ToDictionary(k => k.Key, v => v.Value);
            Assert.Equal(5, dict3.Count);
            Assert.Equal("NoKeyValue3", dict3[""]);
            Assert.Equal("ValueInMem3", dict3["KeyInMem3"]);
            Assert.Equal("ValueInMem4", dict3["KeyInMem4"]);
            Assert.Equal("ValueDeep3", dict3["KeyInMem3:Deep3"]);
            Assert.Equal("ValueDeep4", dict3["KeyInMem3:Deep4"]);
        }


        [Fact]
        public void NewConfigurationProviderOverridesOldOneWhenKeyIsDuplicated()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
                {
                    {"Key1:Key2", "ValueInMem1"}
                };
            var dic2 = new Dictionary<string, string>()
                {
                    {"Key1:Key2", "ValueInMem2"}
                };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);

            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal("ValueInMem2", config["Key1:Key2"]);
        }

        [Fact]
        public void NewConfigurationRootMayBeBuiltFromExistingWithDuplicateKeys()
        {
            var configurationRoot = new ConfigurationBuilder()
                                    .AddInMemoryCollection(new Dictionary<string, string>
                                        {
                                            {"keya:keyb", "valueA"},
                                        })
                                    .AddInMemoryCollection(new Dictionary<string, string>
                                        {
                                            {"KEYA:KEYB", "valueB"}
                                        })
                                    .Build();
            var newConfigurationRoot = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationRoot.AsEnumerable())
                .Build();
            Assert.Equal("valueB", newConfigurationRoot["keya:keyb"]);
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

        [Fact]
        public void SettingValueUpdatesAllConfigurationProviders()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"}
            };

            var memConfigSrc1 = new TestMemorySourceProvider(dict);
            var memConfigSrc2 = new TestMemorySourceProvider(dict);
            var memConfigSrc3 = new TestMemorySourceProvider(dict);

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            // Act
            config["Key1"] = "NewValue1";
            config["Key2"] = "NewValue2";

            var memConfigProvider1 = memConfigSrc1.Build(configurationBuilder);
            var memConfigProvider2 = memConfigSrc2.Build(configurationBuilder);
            var memConfigProvider3 = memConfigSrc3.Build(configurationBuilder);

            // Assert
            Assert.Equal("NewValue1", config["Key1"]);
            Assert.Equal("NewValue1", memConfigProvider1.Get("Key1"));
            Assert.Equal("NewValue1", memConfigProvider2.Get("Key1"));
            Assert.Equal("NewValue1", memConfigProvider3.Get("Key1"));
            Assert.Equal("NewValue2", config["Key2"]);
            Assert.Equal("NewValue2", memConfigProvider1.Get("Key2"));
            Assert.Equal("NewValue2", memConfigProvider2.Get("Key2"));
            Assert.Equal("NewValue2", memConfigProvider3.Get("Key2"));
        }

        [Fact]
        public void CanGetConfigurationSection()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Data:DB1:Connection1", "MemVal1"},
                {"Data:DB1:Connection2", "MemVal2"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"DataSource:DB2:Connection", "MemVal3"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"Data", "MemVal4"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            // Act
            var configFocus = config.GetSection("Data");

            var memVal1 = configFocus["DB1:Connection1"];
            var memVal2 = configFocus["DB1:Connection2"];
            var memVal3 = configFocus["DB2:Connection"];
            var memVal4 = configFocus["Source:DB2:Connection"];
            var memVal5 = configFocus.Value;

            // Assert
            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal4", memVal5);

            Assert.Equal("MemVal1", configFocus["DB1:Connection1"]);
            Assert.Equal("MemVal2", configFocus["DB1:Connection2"]);
            Assert.Null(configFocus["DB2:Connection"]);
            Assert.Null(configFocus["Source:DB2:Connection"]);
            Assert.Equal("MemVal4", configFocus.Value);
        }

        [Fact]
        public void CanGetConnectionStrings()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"ConnectionStrings:DB1:Connection1", "MemVal1"},
                {"ConnectionStrings:DB1:Connection2", "MemVal2"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"ConnectionStrings:DB2:Connection", "MemVal3"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);

            var config = configurationBuilder.Build();

            // Act
            var memVal1 = config.GetConnectionString("DB1:Connection1");
            var memVal2 = config.GetConnectionString("DB1:Connection2");
            var memVal3 = config.GetConnectionString("DB2:Connection");

            // Assert
            Assert.Equal("MemVal1", memVal1);
            Assert.Equal("MemVal2", memVal2);
            Assert.Equal("MemVal3", memVal3);
        }

        [Fact]
        public void CanGetConfigurationChildren()
        {
            // Arrange
            var dic1 = new Dictionary<string, string>()
            {
                {"Data:DB1:Connection1", "MemVal1"},
                {"Data:DB1:Connection2", "MemVal2"}
            };
            var dic2 = new Dictionary<string, string>()
            {
                {"Data:DB2Connection", "MemVal3"}
            };
            var dic3 = new Dictionary<string, string>()
            {
                {"DataSource:DB3:Connection", "MemVal4"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dic1 };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dic2 };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dic3 };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            // Act
            var configSections = config.GetSection("Data").GetChildren().ToList();

            // Assert
            Assert.Equal(2, configSections.Count());
            Assert.Equal("MemVal1", configSections.FirstOrDefault(c => c.Key == "DB1")["Connection1"]);
            Assert.Equal("MemVal2", configSections.FirstOrDefault(c => c.Key == "DB1")["Connection2"]);
            Assert.Equal("MemVal3", configSections.FirstOrDefault(c => c.Key == "DB2Connection").Value);
            Assert.False(configSections.Exists(c => c.Key == "DB3"));
            Assert.False(configSections.Exists(c => c.Key == "DB3"));
        }

        [Fact]
        public void SourcesReturnsAddedConfigurationProviders()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem:KeyInMem", "MemVal"}
            };
            var memConfigSrc1 = new MemoryConfigurationSource { InitialData = dict };
            var memConfigSrc2 = new MemoryConfigurationSource { InitialData = dict };
            var memConfigSrc3 = new MemoryConfigurationSource { InitialData = dict };

            var srcSet = new HashSet<IConfigurationSource>()
            {
                memConfigSrc1,
                memConfigSrc2,
                memConfigSrc3
            };

            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.Add(memConfigSrc1);
            configurationBuilder.Add(memConfigSrc2);
            configurationBuilder.Add(memConfigSrc3);

            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal(new[] { memConfigSrc1, memConfigSrc2, memConfigSrc3 }, configurationBuilder.Sources);
        }

        [Fact]
        public void SetValueThrowsExceptionNoSourceRegistered()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            var expectedMsg = SR.Error_NoSources;

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => config["Title"] = "Welcome");

            // Assert
            Assert.Equal(expectedMsg, ex.Message);
        }

        [Fact]
        public void ReferenceResolutionIsDisabledByDefault()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["BaseUrl"] = "https://example.com",
                    ["ServiceUrl"] = "fmt({BaseUrl}/api)",
                })
                .Build();

            Assert.Equal("fmt({BaseUrl}/api)", config["ServiceUrl"]);
        }

        [Fact]
        public void EnableReferenceResolutionResolvesValues()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["BaseUrl"] = "https://example.com",
                    ["ServiceUrl"] = "fmt({BaseUrl}/api)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("https://example.com/api", config["ServiceUrl"]);
        }

        [Fact]
        public void EnableReferenceResolutionUsesLastProviderValue()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Environment"] = "development",
                    ["ServiceUrl"] = "fmt(https://{Environment}.example.com)",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Environment"] = "production",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("https://production.example.com", config["ServiceUrl"]);
        }

        [Fact]
        public void EnableReferenceResolutionLeavesValueWhenRequiredReferenceIsMissing()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ServiceUrl"] = "fmt(https://{Host}.example.com)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("fmt(https://{Host}.example.com)", config["ServiceUrl"]);
        }

        [Theory]
        // Brace doubling inside fmt(...) yields a literal brace.
        [InlineData("fmt({{Host}})", "{Host}")]
        [InlineData("fmt(prefix-{{Host}}-suffix)", "prefix-{Host}-suffix")]
        [InlineData("fmt({{A}}-{Host}-{{B}})", "{A}-my-host-{B}")]
        // A `$` outside a placeholder is just a literal; only `ref(`/`fmt(` activate the parser.
        [InlineData("fmt(${Host})", "$my-host")]
        [InlineData("fmt(It costs ${Host})", "It costs $my-host")]
        [InlineData("fmt({{}})", "{}")]
        public void EnableReferenceResolutionTreatsEscapeBlockAsLiteral(string raw, string expected)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Host"] = "my-host",
                    ["Value"] = raw,
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal(expected, config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionProjectsSectionReferenceChildren()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Feature:Enabled"] = "true",
                    ["Defaults:Feature:Name"] = "feature-default",
                    ["Feature"] = "ref(Defaults:Feature)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Null(config["Feature"]);
            Assert.Equal("true", config["Feature:Enabled"]);
            Assert.Equal("feature-default", config["Feature:Name"]);
            Assert.Equal(
                new[] { "Enabled", "Name" },
                config.GetSection("Feature").GetChildren().Select(c => c.Key).OrderBy(k => k));
        }

        [Fact]
        public void EnableReferenceResolutionProjectsNestedSectionReferenceChildren()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Feature:Nested:Enabled"] = "true",
                    ["Feature"] = "ref(Defaults:Feature)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("true", config["Feature:Nested:Enabled"]);
            Assert.Equal("Enabled", config.GetSection("Feature:Nested").GetChildren().Single().Key);
        }

        [Fact]
        public void EnableReferenceResolutionKeepsSingleTokenReferenceAsLeafWhenTargetIsValue()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Feature"] = "feature-default",
                    ["Feature"] = "ref(Defaults:Feature)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("feature-default", config["Feature"]);
            Assert.Null(config["Feature:Enabled"]);
            Assert.Empty(config.GetSection("Feature").GetChildren());
        }

        [Fact]
        public void EnableReferenceResolutionOverridesValuesBeforeSectionReferenceProvider()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature:Enabled"] = "from-early-provider",
                    ["Defaults:Feature:Enabled"] = "from-defaults",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature"] = "ref(Defaults:Feature)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("from-defaults", config["Feature:Enabled"]);
        }

        [Fact]
        public void EnableReferenceResolutionResolvesTokenViaAncestorSectionAlias()
        {
            // ref(Services:Primary:Host) inside the ConnectionString fmt(...) must follow the section alias just like a
            // direct read of config["Services:Primary:Host"] would.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Service:Host"] = "primary.example.com",
                    ["Defaults:Service:Port"] = "5432",
                    ["Services:Primary"] = "ref(Defaults:Service)",
                    ["ConnectionString"] = "fmt({Services:Primary:Host}:{Services:Primary:Port})",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("primary.example.com:5432", config["ConnectionString"]);
        }

        [Fact]
        public void EnableReferenceResolutionTokenViaAncestorAliasRespectsShadowing()
        {
            // Alias at a later provider shadows a direct value at an earlier provider, both for
            // direct reads and for tokens embedded in values.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Services:Primary:Host"] = "direct-literal",
                    ["Defaults:Service:Host"] = "aliased-target",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Services:Primary"] = "ref(Defaults:Service)",
                    ["ConnectionString"] = "fmt(host={Services:Primary:Host})",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("aliased-target", config["Services:Primary:Host"]);
            Assert.Equal("host=aliased-target", config["ConnectionString"]);
        }

        [Fact]
        public void EnableReferenceResolutionMergesEarlierChildrenNotPresentInAliasedSection()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature:Legacy"] = "from-early-provider",
                    ["Defaults:Feature:Enabled"] = "from-defaults",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature"] = "ref(Defaults:Feature)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("from-early-provider", config["Feature:Legacy"]);
            Assert.Equal(
                new[] { "Enabled", "Legacy" },
                config.GetSection("Feature").GetChildren().Select(c => c.Key).OrderBy(k => k));
        }

        [Fact]
        public void EnableReferenceResolutionStrictAliasHidesEarlierChildrenUnderAliasedSection()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature:Legacy"] = "from-early-provider",
                    ["Defaults:Feature:Enabled"] = "from-defaults",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature"] = "ref(Defaults:Feature!)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Null(config["Feature:Legacy"]);
            Assert.Equal("from-defaults", config["Feature:Enabled"]);
            Assert.Equal(
                new[] { "Enabled" },
                config.GetSection("Feature").GetChildren().Select(c => c.Key).OrderBy(k => k));
        }

        [Fact]
        public void EnableReferenceResolutionStrictAliasAllowsLaterProviderToShadow()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature:Legacy"] = "from-early-provider",
                    ["Defaults:Feature:Enabled"] = "from-defaults",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature"] = "ref(Defaults:Feature!)",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature:Extra"] = "from-late-provider",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Null(config["Feature:Legacy"]);
            Assert.Equal("from-defaults", config["Feature:Enabled"]);
            Assert.Equal("from-late-provider", config["Feature:Extra"]);
            Assert.Equal(
                new[] { "Enabled", "Extra" },
                config.GetSection("Feature").GetChildren().Select(c => c.Key).OrderBy(k => k));
        }

        [Fact]
        public void EnableReferenceResolutionPreservesValuesAfterSectionReferenceProvider()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Feature:Enabled"] = "from-defaults",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature"] = "ref(Defaults:Feature)",
                })
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Feature:Enabled"] = "from-late-provider",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("from-late-provider", config["Feature:Enabled"]);
        }

        [Fact]
        public void EnableReferenceResolutionTreatsInterpolatedSectionReferenceAsLeafValue()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Feature:Enabled"] = "true",
                    ["Feature"] = "fmt(prefix-{Defaults:Feature})",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("fmt(prefix-{Defaults:Feature})", config["Feature"]);
            Assert.Empty(config.GetSection("Feature").GetChildren());
        }

        [Fact]
        public void EnableReferenceResolutionAppliesToAllSourcesRegardlessOfOrder()
        {
            // EnableReferenceResolution is a root-level signal, not a source. Sources added
            // after the call still participate in substitution.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ServiceUrl"] = "fmt(https://{Host|}fallback)",
                })
                .EnableReferenceResolution()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Host"] = "api.example.com",
                })
                .Build();

            Assert.Equal("https://api.example.comfallback", config["ServiceUrl"]);
            Assert.Equal("api.example.com", config["Host"]);
        }

        [Fact]
        public void EnableReferenceResolutionIsIdempotent()
        {
            // Calling EnableReferenceResolution repeatedly keeps the single shared engine;
            // there is no longer a per-call resolution "horizon".
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["name"] = "Alice",
                    ["greeting"] = "fmt(Hello {name})",
                })
                .EnableReferenceResolution()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["name"] = "Bob",
                    ["farewell"] = "fmt(Bye {name})",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("Bob", config["name"]);
            Assert.Equal("Hello Bob", config["greeting"]);
            Assert.Equal("Bye Bob", config["farewell"]);
        }

        [Fact]
        public void EnableReferenceResolutionEscapeIsStillALiteral()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Template"] = "fmt({{hidden}})",
                    ["hidden"] = "secret",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("{hidden}", config["Template"]);
        }

        [Fact]
        public void EnableReferenceResolutionOptionalChainCollapsesToEmpty()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ServiceUrl"] = "fmt(https://host{Suffix|})",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("https://host", config["ServiceUrl"]);
        }

        [Fact]
        public void EnableReferenceResolutionChainTriesReferencesInOrder()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Primary"] = "first",
                    ["Secondary"] = "second",
                    ["Fallback"] = "fallback",
                    ["A"] = "ref(Missing?Primary)",
                    ["B"] = "ref(Missing?AlsoMissing?Secondary)",
                    ["C"] = "ref(Missing?AlsoMissing?Fallback)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("first", config["A"]);
            Assert.Equal("second", config["B"]);
            Assert.Equal("fallback", config["C"]);
        }

        [Fact]
        public void EnableReferenceResolutionChainFirstPresentWins()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Primary"] = "primary-value",
                    ["Secondary"] = "secondary-value",
                    ["Fallback"] = "fallback",
                    ["Value"] = "ref(Primary?Secondary?Fallback)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("primary-value", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionOptionalChainWithAllMissingCollapsesToEmpty()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = "ref(A?B?C|)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionOptionalChainPrefersFirstResolvedReference()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Primary"] = "hit",
                    ["Value"] = "ref(Missing?Primary|)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("hit", config["Value"]);
        }

        [Theory]
        [InlineData("ref()")]
        [InlineData("ref( )")]
        [InlineData("ref(?A)")]
        [InlineData("ref(?)")]
        [InlineData("ref(A??)")]
        [InlineData("ref(|)")]
        [InlineData("ref(|literal)")]
        [InlineData("ref( |literal)")]
        public void EnableReferenceResolutionMalformedExpressionThrows(string raw)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = raw,
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Throws<FormatException>(() => _ = config["Value"]);
        }

        [Theory]
        [InlineData("ref(\"weird?key\")", "weird?key")]
        [InlineData("ref('weird?key')", "weird?key")]
        [InlineData("ref(\"has|pipe\")", "has|pipe")]
        [InlineData("ref('needs!bang')", "needs!bang")]
        [InlineData("ref(foo\"?\"bar)", "foo?bar")]
        [InlineData("ref('it''s')", "it's")]
        [InlineData("ref(\"say \"\"hi\"\"\")", "say \"hi\"")]
        [InlineData("ref('say \"hi\"')", "say \"hi\"")]
        public void EnableReferenceResolutionQuotedSegmentResolvesLiteralKey(string raw, string literalKey)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [literalKey] = "hit",
                    ["Value"] = raw,
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("hit", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionQuotedSegmentInsidePathResolves()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Section:weird?sub:Leaf"] = "hit",
                    ["Value"] = "ref(Section:\"weird?sub\":Leaf)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("hit", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionQuotedSegmentInFmtPlaceholderResolves()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["odd?key"] = "value",
                    ["Greeting"] = "fmt(Hello, {\"odd?key\"}!)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("Hello, value!", config["Greeting"]);
        }

        [Theory]
        [InlineData("ref(\"unterminated)")]
        [InlineData("ref('unterminated)")]
        [InlineData("ref(\"mixed')")]
        public void EnableReferenceResolutionUnterminatedQuoteThrows(string raw)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = raw,
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Throws<FormatException>(() => _ = config["Value"]);
        }

        [Theory]
        [InlineData("ref(Missing|\"quoted default\")", "quoted default")]
        [InlineData("ref(Missing|'quoted default')", "quoted default")]
        [InlineData("ref(Missing|\"has}brace\")", "has}brace")]
        [InlineData("ref(Missing|\"  spaced  \")", "  spaced  ")]
        [InlineData("ref(Missing|'it''s')", "it's")]
        [InlineData("ref(Missing|foo\"}\"bar)", "foo}bar")]
        [InlineData("ref(Missing|\"\")", "")]
        public void EnableReferenceResolutionQuotedLiteralTail(string raw, string expected)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = raw,
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal(expected, config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionQuotedLiteralTailInFmtPlaceholderAllowsBrace()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Template"] = "fmt(prefix-{Missing|\"has}brace\"}-suffix)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("prefix-has}brace-suffix", config["Template"]);
        }

        [Fact]
        public void EnableReferenceResolutionUnterminatedQuoteInLiteralTailThrows()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = "ref(A|\"unterminated)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Throws<FormatException>(() => _ = config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionDefaultTailIsTemplateWithPlaceholder()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Host"] = "example.com",
                    ["Value"] = "ref(Missing|{Host}/path)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("example.com/path", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionDefaultTailComposesCoalesceAndLiteral()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Backup"] = "bee",
                    ["Value"] = "ref(Missing|{Unknown?Backup|fallback}-end)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("bee-end", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionDefaultTailFallsBackToInnerLiteralWhenAllMiss()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = "ref(Missing|{Unknown?AlsoUnknown|inner}-end)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("inner-end", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionFmtPlaceholderWithNestedDefault()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["B"] = "bee",
                    ["Value"] = "fmt(prefix-{A|{B}}-suffix)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("prefix-bee-suffix", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionDefaultTailQuotedBraceIsLiteralNotPlaceholder()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Host"] = "should-not-appear",
                    ["Value"] = "ref(Missing|\"{Host}\")",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("{Host}", config["Value"]);
        }

        [Fact]
        public void EnableReferenceResolutionRelativeRefSiblingResolves()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Services:Billing:Host"] = "billing.example.com",
                    ["Services:Billing:Url"] = "fmt(https://{..:Host}/api)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("https://billing.example.com/api", config["Services:Billing:Url"]);
        }

        [Fact]
        public void EnableReferenceResolutionRelativeRefUncleResolves()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["App:Shared:Region"] = "eu-west",
                    ["App:Services:Billing:Zone"] = "ref(..:..:..:Shared:Region)",
                })
                .EnableReferenceResolution()
                .Build();

            // Three hops up from App:Services:Billing:Zone = App, then :Shared:Region.
            Assert.Equal("eu-west", config["App:Services:Billing:Zone"]);
        }

        [Fact]
        public void EnableReferenceResolutionRelativeRefAnchorsAtStorageNotQuery()
        {
            // A section alias maps Services:Billing onto the Defaults subtree. A relative ref
            // inside a Defaults value must resolve relative to its storage key (Defaults:Db:Conn),
            // not relative to the user-facing query key (Services:Billing:Db:Conn).
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Db:Host"] = "defaults-db-host",
                    ["Defaults:Db:Conn"] = "ref(..:Host)",
                    ["Services:Billing:Db:Host"] = "billing-db-host",
                    ["Services:Billing"] = "ref(Defaults)",
                })
                .EnableReferenceResolution()
                .Build();

            // ..:Host anchors at Defaults:Db (storage), so it picks Defaults:Db:Host even when
            // queried via the aliased path. The user's Services:Billing:Db:Host value (which
            // exists at the query side) is irrelevant to this resolution.
            Assert.Equal("defaults-db-host", config["Services:Billing:Db:Conn"]);
        }

        [Fact]
        public void EnableReferenceResolutionRelativeRefAboveRootFallsThroughChain()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Fallback"] = "fallback-value",
                    ["TopLevel"] = "ref(..:..:DoesNotExist?Fallback)",
                })
                .EnableReferenceResolution()
                .Build();

            // The relative ref wants two parents above a 1-segment key; that fails, chain falls
            // through to the absolute Fallback reference.
            Assert.Equal("fallback-value", config["TopLevel"]);
        }

        [Fact]
        public void EnableReferenceResolutionRelativeRefAboveRootOptionalYieldsEmpty()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TopLevel"] = "fmt(prefix-{..:..:Nothing|}-suffix)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("prefix--suffix", config["TopLevel"]);
        }

        [Fact]
        public void EnableReferenceResolutionRelativeRefPureParentResolvesSection()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Defaults:Host"] = "h",
                    ["Defaults:Port"] = "p",
                    ["Services:Db"] = "ref(..:..:Defaults)",
                })
                .EnableReferenceResolution()
                .Build();

            // ..:..:Defaults from Services:Db = Defaults (section). Rebase exposes its children.
            Assert.Equal("h", config["Services:Db:Host"]);
            Assert.Equal("p", config["Services:Db:Port"]);
        }

        [Theory]
        [InlineData("ref(A:..:B)")]
        [InlineData("ref(A:..)")]
        public void EnableReferenceResolutionRelativeSegmentNotAtStartThrows(string raw)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Value"] = raw,
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Throws<FormatException>(() => _ = config["Value"]);
        }

        [Fact]
        public void ConfigureReferenceResolutionOnEmptyBuilderThrows()
        {
            var builder = new ConfigurationBuilder();

            Assert.Throws<InvalidOperationException>(() => builder.ConfigureReferenceResolution(ReferenceMode.Ignore));
        }

        [Fact]
        public void ConfigureReferenceResolutionIgnoreHidesValuesFromSubstitution()
        {
            var hiddenSource = new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string> { ["X"] = "from-hidden" }
            };

            var config = new ConfigurationBuilder()
                .Add(hiddenSource)
                .ConfigureReferenceResolution(ReferenceMode.Ignore)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["K"] = "ref(X)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("ref(X)", config["K"]);
            Assert.Equal("from-hidden", config["X"]);
        }

        [Fact]
        public void ConfigureReferenceResolutionBySourceNoneHidesValuesFromSubstitution()
        {
            var hiddenSource = new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string> { ["X"] = "from-hidden" }
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["K"] = "ref(X)",
                })
                .EnableReferenceResolution();

            builder.Add(hiddenSource);
            builder.ConfigureReferenceResolution(hiddenSource, ReferenceMode.Ignore);

            IConfigurationRoot config = builder.Build();

            Assert.Equal("ref(X)", config["K"]);
            Assert.Equal("from-hidden", config["X"]);
        }

        [Fact]
        public void ConfigureReferenceResolutionBySourceNotPresentThrows()
        {
            var builder = new ConfigurationBuilder().AddInMemoryCollection();
            var otherSource = new MemoryConfigurationSource();

            Assert.Throws<ArgumentException>(() => builder.ConfigureReferenceResolution(otherSource, ReferenceMode.Ignore));
        }

        [Fact]
        public void ConfigureReferenceResolutionIgnoreChildrenStillVisibleInGetChildren()
        {
            var hiddenSource = new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string>
                {
                    ["Section:A"] = "a",
                    ["Section:B"] = "b",
                }
            };

            var config = new ConfigurationBuilder()
                .Add(hiddenSource)
                .ConfigureReferenceResolution(ReferenceMode.Ignore)
                .EnableReferenceResolution()
                .Build();

            IEnumerable<string> childKeys = config.GetSection("Section").GetChildren().Select(c => c.Key).OrderBy(k => k);
            Assert.Equal(new[] { "A", "B" }, childKeys);
        }

        [Fact]
        public void ConfigureReferenceResolutionReadTreatsOwnValuesAsLiteralButServesAsTarget()
        {
            // Source in Read-only mode is a substitution target but its own ref(...)/fmt(...) values stay literal.
            var readOnlySource = new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string>
                {
                    ["X"] = "from-readonly",
                    ["Self"] = "ref(X)",
                }
            };

            var config = new ConfigurationBuilder()
                .Add(readOnlySource)
                .ConfigureReferenceResolution(ReferenceMode.Read)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["K"] = "ref(X)",
                })
                .EnableReferenceResolution()
                .Build();

            Assert.Equal("from-readonly", config["K"]);
            Assert.Equal("ref(X)", config["Self"]);
        }

        private sealed class StubBuilder : IConfigurationBuilder
        {
            public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
            public IList<IConfigurationSource> Sources { get; } = new List<IConfigurationSource>();
            public IConfigurationBuilder Add(IConfigurationSource source) { Sources.Add(source); return this; }
            public IConfigurationRoot Build() => throw new NotImplementedException();
        }

        [Fact]
        public void SameReloadTokenIsReturnedRepeatedly()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var token2 = config.GetReloadToken();

            // Assert
            Assert.Same(token1, token2);
        }

        [Fact]
        public void DifferentReloadTokenReturnedAfterReloading()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var token2 = config.GetReloadToken();
            config.Reload();
            var token3 = config.GetReloadToken();
            var token4 = config.GetReloadToken();

            // Assert
            Assert.Same(token1, token2);
            Assert.Same(token3, token4);
            Assert.NotSame(token1, token3);
        }

        [Fact]
        public void TokenTriggeredWhenReloadOccurs()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var hasChanged1 = token1.HasChanged;
            config.Reload();
            var hasChanged2 = token1.HasChanged;

            // Assert
            Assert.False(hasChanged1);
            Assert.True(hasChanged2);
        }

        [Fact]
        public void MultipleCallbacksCanBeRegisteredToReload()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var called1 = 0;
            token1.RegisterChangeCallback(_ => called1++, state: null);
            var called2 = 0;
            token1.RegisterChangeCallback(_ => called2++, state: null);

            // Assert
            Assert.Equal(0, called1);
            Assert.Equal(0, called2);

            config.Reload();
            Assert.Equal(1, called1);
            Assert.Equal(1, called2);

            var token2 = config.GetReloadToken();
            var cleanup1 = token2.RegisterChangeCallback(_ => called1++, state: null);
            token2.RegisterChangeCallback(_ => called2++, state: null);

            cleanup1.Dispose();

            config.Reload();
            Assert.Equal(1, called1);
            Assert.Equal(2, called2);
        }

        [Fact]
        public void AsyncLocalsNotCapturedAndRestoredConfigurationReloadToken()
        {
            // Capture clean context
            var executionContext = ExecutionContext.Capture();

            var configurationReloadToken = new ConfigurationReloadToken();
            var executed = false;

            // Set AsyncLocal
            var asyncLocal = new AsyncLocal<int>();
            asyncLocal.Value = 1;

            // Register Callback
            configurationReloadToken.RegisterChangeCallback(al =>
            {
                // AsyncLocal not set, when run on clean context
                // A suppressed flow runs in current context, rather than restoring the captured context
                Assert.Equal(0, ((AsyncLocal<int>)al).Value);
                executed = true;
            }, asyncLocal);

            // AsyncLocal should still be set
            Assert.Equal(1, asyncLocal.Value);

            // Check AsyncLocal is not restored by running on clean context
            ExecutionContext.Run(executionContext, crt => ((ConfigurationReloadToken)crt).OnReload(), configurationReloadToken);

            // AsyncLocal should still be set
            Assert.Equal(1, asyncLocal.Value);
            Assert.True(executed);
        }

        [Fact]
        public void NewTokenAfterReloadIsNotChanged()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            // Act
            var token1 = config.GetReloadToken();
            var hasChanged1 = token1.HasChanged;
            config.Reload();
            var hasChanged2 = token1.HasChanged;
            var token2 = config.GetReloadToken();
            var hasChanged3 = token2.HasChanged;

            // Assert
            Assert.False(hasChanged1);
            Assert.True(hasChanged2);
            Assert.False(hasChanged3);
            Assert.NotSame(token1, token2);
        }

        [Fact]
        public void KeyStartingWithColonMeansFirstSectionHasEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                [":Key2"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Single(children);
            Assert.Equal(string.Empty, children.First().Key);
            Assert.Single(children.First().GetChildren());
            Assert.Equal("Key2", children.First().GetChildren().First().Key);
        }

        [Fact]
        public void KeyWithDoubleColonHasSectionWithEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Key1::Key3"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Single(children);
            Assert.Equal("Key1", children.First().Key);
            Assert.Single(children.First().GetChildren());
            Assert.Equal(string.Empty, children.First().GetChildren().First().Key);
            Assert.Single(children.First().GetChildren().First().GetChildren());
            Assert.Equal("Key3", children.First().GetChildren().First().GetChildren().First().Key);
        }

        [Fact]
        public void KeyEndingWithColonMeansLastSectionHasEmptyName()
        {
            // Arrange
            var dict = new Dictionary<string, string>
            {
                ["Key1:"] = "value"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var children = config.GetChildren().ToArray();

            // Assert
            Assert.Single(children);
            Assert.Equal("Key1", children.First().Key);
            Assert.Single(children.First().GetChildren());
            Assert.Equal(string.Empty, children.First().GetChildren().First().Key);
        }

        [Fact]
        public void SectionWithValueExists()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var sectionExists1 = config.GetSection("Mem1").Exists();
            var sectionExists2 = config.GetSection("Mem1:KeyInMem1").Exists();
            var sectionNotExists = config.GetSection("Mem2").Exists();

            // Assert
            Assert.True(sectionExists1);
            Assert.True(sectionExists2);
            Assert.False(sectionNotExists);
        }

        [Fact]
        public void SectionGetRequiredSectionSuccess()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            IConfigurationRoot config = configurationBuilder.Build();

            // Act
            var sectionExists1 = config.GetRequiredSection("Mem1").Exists();
            var sectionExists2 = config.GetRequiredSection("Mem1:KeyInMem1").Exists();

            // Assert
            Assert.True(sectionExists1);
            Assert.True(sectionExists2);
        }

        [Fact]
        public void SectionGetRequiredSectionMissingThrowException()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", "Value1"},
                {"Mem1:Deep1", "Value1"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            IConfigurationRoot config = configurationBuilder.Build();

            Assert.Throws<InvalidOperationException>(() => config.GetRequiredSection("Mem2"));
            Assert.Throws<InvalidOperationException>(() => config.GetRequiredSection("Mem1:Deep2"));
        }

        [Fact]
        public void SectionGetRequiredSectionNullThrowException()
        {
            IConfigurationRoot config = null;
            Assert.Throws<ArgumentNullException>(() => config.GetRequiredSection("Mem1"));
        }

        [Fact]
        public void SectionWithChildrenExists()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"},
                {"Mem1:KeyInMem1:Deep1", "ValueDeep1"},
                {"Mem2:KeyInMem2:Deep1", "ValueDeep2"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var sectionExists1 = config.GetSection("Mem1").Exists();
            var sectionExists2 = config.GetSection("Mem2").Exists();
            var sectionNotExists = config.GetSection("Mem3").Exists();

            // Assert
            Assert.True(sectionExists1);
            Assert.True(sectionExists2);
            Assert.False(sectionNotExists);
        }

        [Theory]
        [InlineData("Value1")]
        [InlineData("")]
        public void KeyWithValueAndWithoutChildrenExistsAsSection(string value)
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", value}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var sectionExists = config.GetSection("Mem1").Exists();

            // Assert
            Assert.True(sectionExists);
        }

        [Fact]
        public void KeyWithNullValueAndWithoutChildrenIsASectionButNotExists()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1", null}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var sections = config.GetChildren();
            var sectionExists = config.GetSection("Mem1").Exists();
            var sectionChildren = config.GetSection("Mem1").GetChildren();

            // Assert
            Assert.Single(sections, section => section.Key == "Mem1");
            Assert.False(sectionExists);
            Assert.Empty(sectionChildren);
        }

        [Fact]
        public void SectionWithChildrenHasNullValue()
        {
            // Arrange
            var dict = new Dictionary<string, string>()
            {
                {"Mem1:KeyInMem1", "ValueInMem1"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dict);
            var config = configurationBuilder.Build();

            // Act
            var sectionValue = config.GetSection("Mem1").Value;

            // Assert
            Assert.Null(sectionValue);
        }

        [Fact]
        public void NullSectionDoesNotExist()
        {
            // Arrange
            // Act
            var sectionExists = ConfigurationExtensions.Exists(null);

            // Assert
            Assert.False(sectionExists);
        }

        internal class NullReloadTokenConfigSource : IConfigurationSource, IConfigurationProvider
        {
            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) => throw new NotImplementedException();
            public Primitives.IChangeToken GetReloadToken() => null;
            public void Load() { }
            public void Set(string key, string value) => throw new NotImplementedException();
            public bool TryGet(string key, out string value) => throw new NotImplementedException();
            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
        }

        [Fact]
        public void ProviderWithNullReloadToken()
        {
            // Arrange
            var builder = new ConfigurationBuilder();
            builder.Add(new NullReloadTokenConfigSource());

            // Act
            var config = builder.Build();

            // Assert
            Assert.NotNull(config);
        }
    }
}
