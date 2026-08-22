// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
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

        [Fact]
        public void GetChildrenDeduplicatesSameChildKeyAcrossProviders()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Section:A", "1" }, { "Section:B", "2" } })
                .AddInMemoryCollection(new Dictionary<string, string> { { "Section:B", "3" }, { "Section:C", "4" } })
                .Build();

            string[] children = config.GetSection("Section").GetChildren().Select(c => c.Key).ToArray();

            Assert.Equal(new[] { "A", "B", "C" }, children);
            Assert.Equal("3", config["Section:B"]);
        }

        [Fact]
        public void GetChildrenDeduplicatesKeysFromProviderReturningDuplicates()
        {
            var config = new ConfigurationBuilder()
                .Add(new DuplicateChildKeysSource())
                .Build();

            string[] children = config.GetChildren().Select(c => c.Key).ToArray();

            Assert.Equal(new[] { "Dup" }, children);
        }

        [Fact]
        public void GetChildKeys_AccumulatedKeysAreEnumerable()
        {
            var capture = new CaptureEarlierKeysSource();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "B", "1" }, { "10", "2" }, { "2", "3" }, { "A", "4" }
                })
                .Add(capture)
                .Build();

            config.GetChildren().ToList();

            // A provider receives the keys accumulated so far as a plain sequence, deliberately unordered:
            // ordering belongs to the handover, once every provider has contributed.
            IEnumerable<string> captured = capture.Captured;
            Assert.NotNull(captured);
            Assert.Equal(new[] { "2", "10", "A", "B" }, captured.OrderBy(k => k, ConfigurationKeyComparer.Instance));

            // The keys must still be there afterwards, and in the order they always had.
            Assert.Equal(new[] { "2", "10", "A", "B" }, config.GetChildren().Select(c => c.Key).ToArray());
        }

        [Fact]
        public void GetChildren_CopyToOrdersLikeTheEnumerator()
        {
            // ToArray, ToList and List<T>.AddRange copy through ICollection<IConfigurationSection>.CopyTo rather
            // than enumerating, so it has to apply the same ordering the enumerator does.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Zebra", "1" }, { "10", "2" }, { "2", "3" }, { "apple", "4" }
                })
                .Build();

            string[] expected = ["2", "10", "apple", "Zebra"];

            var collection = (ICollection<IConfigurationSection>)config.GetChildren();
            Assert.Equal(expected, collection.ToArray().Select(s => s.Key));
            Assert.Equal(expected, collection.ToList().Select(s => s.Key));

            var appended = new List<IConfigurationSection>();
            appended.AddRange(collection);
            Assert.Equal(expected, appended.Select(s => s.Key));

            var enumerated = new List<string>();
            foreach (IConfigurationSection child in config.GetChildren())
            {
                enumerated.Add(child.Key);
            }
            Assert.Equal(expected, enumerated);

            var destination = new IConfigurationSection[6];
            collection.CopyTo(destination, 1);
            Assert.Equal([null, "2", "10", "apple", "Zebra", null], destination.Select(s => s?.Key));
        }

        public static IEnumerable<object[]> CopyToInvalidArgumentsData()
        {
            yield return new object[] { null, 0, typeof(ArgumentNullException) };
            yield return new object[] { new IConfigurationSection[4], -1, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { new IConfigurationSection[3], 0, typeof(ArgumentException) };
            yield return new object[] { new IConfigurationSection[4], 1, typeof(ArgumentException) };
            yield return new object[] { new IConfigurationSection[4], 5, typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(CopyToInvalidArgumentsData))]
        public void GetChildren_CopyToValidatesItsArguments(IConfigurationSection[] destination, int arrayIndex, Type expected)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Zebra", "1" }, { "10", "2" }, { "2", "3" }, { "apple", "4" }
                })
                .Build();

            var collection = (ICollection<IConfigurationSection>)config.GetChildren();

            Assert.Throws(expected, () => collection.CopyTo(destination, arrayIndex));
        }

        [Fact]
        public void GetChildren_CountAndAnyDoNotRequireOrdering()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Zebra", "1" }, { "10", "2" }, { "2", "3" }, { "apple", "4" }
                })
                .Build();

            IEnumerable<IConfigurationSection> children = config.GetChildren();
            Assert.Equal(4, children.Count());
            Assert.True(children.Any());

            // Ordering still applies once the keys are actually read.
            Assert.Equal(["2", "10", "apple", "Zebra"], children.Select(s => s.Key));
        }

        [Fact]
        public void GetChildren_LargeProvider_MatchesBruteForce()
        {
            // A provider with many keys across a deep hierarchy. Cross-check several parent paths against a
            // brute-force computation of the expected immediate children.
            var data = new Dictionary<string, string>();
            for (int s = 0; s < 30; s++)
            {
                for (int g = 0; g < 5; g++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        data[$"Section{s}:Group{g}:Item{i}"] = "v";
                    }
                }
            }
            for (int i = 0; i < 12; i++)
            {
                data[$"Array:{i}"] = i.ToString();
            }
            // Case-insensitive duplicate immediate child of the root ("Dup" vs "dup").
            data["Dup:Child"] = "1";
            data["dup:Other"] = "2";

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            foreach (string parent in new[] { null, "Section0", "Section0:Group0", "Array", "Section29:Group4", "Dup", "Missing" })
            {
                string[] expected = ExpectedImmediateChildren(data, parent);
                IEnumerable<IConfigurationSection> children = parent is null
                    ? config.GetChildren()
                    : config.GetSection(parent).GetChildren();

                Assert.Equal(expected, children.Select(c => c.Key).ToArray(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void GetChildren_VeryDeepSegmentStart_Works()
        {
            // A key whose child segment starts far into the key (past ushort range) is handled by the plain scan,
            // which slices the segment as a span off the key, so there is no offset limit.
            var data = new Dictionary<string, string>();
            string longParent = new string('a', ushort.MaxValue + 100);
            data[longParent + ":Child"] = "v";
            data[longParent + ":Sibling"] = "v";

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            string[] children = config.GetSection(longParent).GetChildren().Select(c => c.Key).ToArray();
            Assert.Equal(new[] { "Child", "Sibling" }, children, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<object[]> ChildKeyOrderingData()
        {
            // An array's children: dense integers, which can be ordered without comparing anything.
            yield return new object[] { Enumerable.Range(0, 20).Select(i => i.ToString()).ToArray() };
            // Integers with gaps, so the dense placement must not be taken.
            yield return new object[] { Enumerable.Range(0, 20).Select(i => (i * 5).ToString()).ToArray() };
            // Integers sort ahead of text.
            yield return new object[] { Enumerable.Range(0, 20).Select(i => i % 2 == 0 ? i.ToString() : $"Name{i}").ToArray() };
            // Small sets, which take a different path from large ones.
            yield return new object[] { new[] { "b", "a", "c" } };
            yield return new object[] { new[] { "3", "1", "2" } };
            // Numerically equal but textually distinct, which also rules out a dense placement.
            yield return new object[] { Enumerable.Range(0, 18).Select(i => i.ToString()).Concat(new[] { "00", "01" }).ToArray() };
            // A duplicate value that still spans a dense range: values 0,0,2..15 over 16 keys, so max-min == count-1
            // holds even though "0" and "00" collide and 1 is absent.
            yield return new object[] { new[] { "0", "00" }.Concat(Enumerable.Range(2, 14).Select(i => i.ToString())).ToArray() };
            // Signs and leading white space parse as integers, and an empty segment sorts ahead of everything.
            yield return new object[] { new[] { "-1", "+2", " 3", "Name", "", "10" } };
            yield return new object[] { Enumerable.Range(0, 16).Select(i => i.ToString()).Concat(new[] { "-1", "+2", " 3", "", "Name" }).ToArray() };
            // Values that overflow int fall back to text.
            yield return new object[] { new[] { "2147483648", "2147483647", "0", "x" } };
            // Consecutive but one-based, so the index placement cannot apply and the comparison has to order them.
            yield return new object[] { Enumerable.Range(1, 20).Select(i => i.ToString()).ToArray() };
            // Indexes that run out of range partway, so the placement abandons a partly permuted array.
            yield return new object[] { Enumerable.Range(0, 19).Select(i => i.ToString()).Concat(new[] { "40" }).ToArray() };
            // Same-length digit strings whose values do not fit in an int.
            yield return new object[] { new[] { "9999999999", "9999999998", "1111111111", "5" } };
            // The same, but large enough to reach the classified path, and arranged so that an overflowed value would
            // land inside a dense run: "5000000000" truncated to 32 bits is 705032704, which another segment claims.
            yield return new object[]
            {
                Enumerable.Range(705032694, 16).Where(i => i != 705032697).Select(i => i.ToString())
                    .Concat(new[] { "5000000000" }).ToArray()
            };
        }

        [Theory]
        [MemberData(nameof(ChildKeyOrderingData))]
        public void GetChildren_OrdersChildKeysWithConfigurationKeyComparer(string[] childKeys)
        {
            var data = new Dictionary<string, string>();
            foreach (string child in childKeys)
            {
                data["Parent:" + child] = "v";
            }

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            string[] actual = config.GetSection("Parent").GetChildren().Select(c => c.Key).ToArray();

            Assert.Equal(childKeys.OrderBy(k => k, StringComparer.Ordinal), actual.OrderBy(k => k, StringComparer.Ordinal));

            // Any two orderings that satisfy the comparer differ only where it reports equality, so checking that the
            // sequence never decreases is both necessary and sufficient.
            for (int i = 1; i < actual.Length; i++)
            {
                Assert.True(
                    ConfigurationKeyComparer.Instance.Compare(actual[i - 1], actual[i]) <= 0,
                    $"'{actual[i - 1]}' must not sort after '{actual[i]}'");
            }
        }

        [Theory]
        [InlineData("Parent", new[] { "Alpha", "Beta" })]
        [InlineData("PARENT", new[] { "Alpha", "Beta" })]
        [InlineData("parent", new[] { "Alpha", "Beta" })]
        [InlineData("Grüße", new[] { "Alpha" })]
        [InlineData("GRÜßE", new[] { "Alpha" })]
        [InlineData("GRÜSSE", new string[0])]
        [InlineData("Root:0", new[] { "Alpha" })]
        [InlineData("Root:10", new[] { "Alpha" })]
        [InlineData("Root:99", new string[0])]
        public void GetChildren_MatchesParentPathIgnoringCase(string requested, string[] expected)
        {
            // The scan rejects most keys on a single character before running the ignore-case comparison, so the
            // cases that matter are a prefix differing only by case, same-length siblings that must not match, and
            // non-ASCII where the cheap check has to defer rather than decide. "ß" does not fold to "SS" under an
            // ordinal comparison, so "GRÜSSE" must find nothing.
            var data = new Dictionary<string, string>
            {
                ["Parent:Alpha"] = "1",
                ["Parent:Beta"] = "2",
                ["Sibling:Alpha"] = "3",
                ["Grüße:Alpha"] = "4",
                ["Root:0:Alpha"] = "5",
                ["Root:1:Alpha"] = "6",
                ["Root:10:Alpha"] = "7",
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            Assert.Equal(expected, config.GetSection(requested).GetChildren().Select(c => c.Key));
        }

        [Theory]
        // Two segments naming the same slot cannot reach the sorter, because the accumulator discards duplicates
        // before it. The placement must not depend on that: it orders these by comparison instead of looping.
        [InlineData(new[] { "0", "0", "2", "3" }, new[] { "0", "0", "2", "3" })]
        [InlineData(new[] { "1", "1" }, new[] { "1", "1" })]
        [InlineData(new[] { "2", "2", "2", "2" }, new[] { "2", "2", "2", "2" })]
        [InlineData(new[] { "3", "1", "3", "0" }, new[] { "0", "1", "3", "3" })]
        public void ChildKeySorter_TerminatesOnDuplicateIndexes(string[] input, string[] expected)
        {
            string[] keys = (string[])input.Clone();

            ChildKeySorter.Sort(keys, keys.Length);

            Assert.Equal(expected, keys);
        }

        [Theory]
        [InlineData("+", "-")]
        [InlineData("p", "n")]
        [InlineData("\u2212", "\u2212")]
        [InlineData("", "")]
        public void ChildKeySorter_OrdersLikeConfigurationKeyComparer_UnderAnySign(string positiveSign, string negativeSign)
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.PositiveSign = positiveSign;
            culture.NumberFormat.NegativeSign = negativeSign;

            string[] keys = ["p2", "n3", "10", "2", "Name", "\u22124", "+5", "-6", "p", "0"];
            string[] sorted = (string[])keys.Clone();

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = culture;

                ChildKeySorter.Sort(sorted, sorted.Length);

                // Any two orderings that satisfy the comparer differ only where it reports equality, so the sorter
                // agrees with it exactly when the sequence never decreases. This has to be judged under the same
                // culture the sort ran in, since the comparer reads the ambient one on every call.
                Assert.Equal(keys.OrderBy(k => k, StringComparer.Ordinal), sorted.OrderBy(k => k, StringComparer.Ordinal));
                for (int i = 1; i < sorted.Length; i++)
                {
                    Assert.True(
                        ConfigurationKeyComparer.Instance.Compare(sorted[i - 1], sorted[i]) <= 0,
                        $"'{sorted[i - 1]}' must not sort after '{sorted[i]}' under '{positiveSign}'/'{negativeSign}'");
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void GetChildren_RejectionMemoKeepsCaseInsensitiveMatches()
        {
            // A key that fails the prefix comparison leaves behind the index at which it diverged, and the next key is
            // tested at that index first. Here it lands on a character that differs only by case, which still matches.
            var data = new Dictionary<string, string>
            {
                ["Root:AXC:One"] = "1",
                ["Root:AbC:Two"] = "2",
                ["Root:AYC:Three"] = "3",
                ["Root:abc:Four"] = "4",
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            Assert.Equal(new[] { "Four", "Two" }, config.GetSection("Root:ABC").GetChildren().Select(c => c.Key));
        }

        [Fact]
        public void GetChildren_SameLengthSiblingsDoNotLeak()
        {
            // Every one of these has ':' at the same index as the requested parent path, so they all survive the
            // cheap length test and only the full comparison can separate them.
            var data = new Dictionary<string, string>();
            for (int i = 1000; i < 1010; i++)
            {
                data[$"Root:{i}:Name"] = i.ToString();
                data[$"Root:{i}:Value"] = i.ToString();
            }

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            Assert.Equal(new[] { "Name", "Value" }, config.GetSection("Root:1007").GetChildren().Select(c => c.Key));
            Assert.Empty(config.GetSection("Root:1099").GetChildren());
        }

        [Fact]
        public void GetChildren_ChainedConfigurationIsSorted_AtAnyDepth()
        {
            // Each nesting level aggregates into its own accumulator and merges upward. Only the outermost one is
            // handed to a consumer, so it is the only one that has to establish the order.
            var data = new Dictionary<string, string> { ["Root:10"] = "ten", ["Root:2"] = "two", ["Root:1"] = "one" };

            IConfigurationRoot inner = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
            IConfigurationRoot middle = new ConfigurationBuilder().AddConfiguration(inner).Build();
            IConfigurationRoot outer = new ConfigurationBuilder().AddConfiguration(middle).Build();

            Assert.Equal(new[] { "1", "2", "10" }, middle.GetSection("Root").GetChildren().Select(c => c.Key));
            Assert.Equal(new[] { "1", "2", "10" }, outer.GetSection("Root").GetChildren().Select(c => c.Key));
            Assert.Equal(new[] { "Root" }, outer.GetChildren().Select(c => c.Key));
        }

        [Fact]
        public void GetChildren_AggregateIsSorted_EvenWhenTheLastProviderIsNot()
        {
            // Array binding reads element order straight from GetChildren, so a provider that contributes no data of
            // its own must not be able to change the order other providers' data binds in.
            var data = new Dictionary<string, string> { ["Root:10"] = "ten", ["Root:2"] = "two", ["Root:1"] = "one" };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Add(new ReverseSortedChildKeysSource())
                .Build();

            Assert.Equal(new[] { "1", "2", "10" }, config.GetSection("Root").GetChildren().Select(c => c.Key));
            Assert.Equal(new[] { "Root" }, config.GetChildren().Select(c => c.Key));
        }

        [Fact]
        public void GetChildren_ProviderCanHideInheritedChildKey()
        {
            // The GetChildKeys contract threads the preceding providers' keys through each provider and uses what it
            // returns, so a later provider can filter out a child key contributed by an earlier one.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["a"] = "1",
                    ["b"] = "2",
                    ["c"] = "3",
                })
                .Add(new FilteringChildKeysSource("b"))
                .Build();

            string[] children = config.GetChildren().Select(c => c.Key).ToArray();

            Assert.Equal(new[] { "a", "c" }, children, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("b", children, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetChildren_EarlierKeysSurvive_ProviderReturningLazySelfReferentialKeys()
        {
            // A provider may return a lazy sequence built from earlierKeys (for example earlierKeys.Concat(ownKeys)).
            // Aggregation must materialize that result before it replaces the accumulated keys, otherwise the earlier
            // providers' keys are read after being cleared and are lost.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "First", "1" } })
                .Add(new DuplicateChildKeysSource())
                .Build();

            string[] children = config.GetChildren().Select(c => c.Key).ToArray();

            // The earlier provider's key must survive the lazy provider's result, and the duplicate is collapsed.
            Assert.Contains("First", children, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Dup", children, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(2, children.Length);
        }

        [Fact]
        public void GetChildren_ChainedRoot_InnerProviderCannotDropOuterKeys()
        {
            // A provider inside a chained configuration root only sees the chained root's own keys, never the outer
            // providers' keys, so it cannot filter out a child contributed by an earlier outer provider.
            IConfigurationRoot inner = new ConfigurationBuilder()
                .Add(new FilteringChildKeysSource("b"))
                .Build();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "a", "1" }, { "b", "2" }, { "c", "3" } })
                .AddConfiguration(inner)
                .Build();

            string[] children = config.GetChildren().Select(c => c.Key).ToArray();

            Assert.Equal(new[] { "a", "b", "c" }, children, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetChildren_ChainedRootWithNoProviders_KeepsOuterKeys()
        {
            // Chaining a configuration root that has no providers must not discard the keys contributed by the outer
            // providers.
            IConfigurationRoot empty = new ConfigurationBuilder().Build();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "A", "1" }, { "B", "2" } })
                .AddConfiguration(empty)
                .Build();

            string[] children = config.GetChildren().Select(c => c.Key).ToArray();

            Assert.Equal(new[] { "A", "B" }, children, StringComparer.OrdinalIgnoreCase);
        }

        private static string[] ExpectedImmediateChildren(Dictionary<string, string> data, string parentPath)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in data.Keys)
            {
                int start;
                if (parentPath is null)
                {
                    start = 0;
                }
                else if (key.Length > parentPath.Length &&
                         key.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase) &&
                         key[parentPath.Length] == ':')
                {
                    start = parentPath.Length + 1;
                }
                else
                {
                    continue;
                }

                int colon = key.IndexOf(':', start);
                set.Add(colon < 0 ? key.Substring(start) : key.Substring(start, colon - start));
            }

            string[] result = set.ToArray();
            Array.Sort(result, ConfigurationKeyComparer.Instance);
            return result;
        }

        // Captures the accumulated child keys a provider is handed, so a test can inspect the collection the
        // aggregation passes around rather than only what finally comes out of GetChildren.
        private sealed class CaptureEarlierKeysSource : IConfigurationSource, IConfigurationProvider
        {
            public IEnumerable<string> Captured { get; private set; }

            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
            {
                if (parentPath is null)
                {
                    Captured = earlierKeys;
                }

                return earlierKeys;
            }

            public bool TryGet(string key, out string value)
            {
                value = null;
                return false;
            }

            public Primitives.IChangeToken GetReloadToken() => new ConfigurationReloadToken();
            public void Load() { }
            public void Set(string key, string value) { }
        }

        private sealed class DuplicateChildKeysSource : IConfigurationSource, IConfigurationProvider
        {
            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) =>
                parentPath is null ? earlierKeys.Concat(new[] { "Dup", "Dup" }) : earlierKeys;
            public bool TryGet(string key, out string value)
            {
                value = string.Equals(key, "Dup", StringComparison.OrdinalIgnoreCase) ? "v" : null;
                return value is not null;
            }
            public Primitives.IChangeToken GetReloadToken() => new ConfigurationReloadToken();
            public void Load() { }
            public void Set(string key, string value) { }
        }

        // A provider that removes a specific immediate child of the root from the keys inherited from the earlier
        // providers, exercising the contract that a provider may filter the preceding providers' keys.
        // Returns the accumulated keys in reverse order, which no part of the contract forbids.
        private sealed class ReverseSortedChildKeysSource : IConfigurationSource, IConfigurationProvider
        {
            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) =>
                earlierKeys.OrderByDescending(key => key, ConfigurationKeyComparer.Instance).ToArray();

            public bool TryGet(string key, out string value)
            {
                value = null;
                return false;
            }

            public Primitives.IChangeToken GetReloadToken() => new ConfigurationReloadToken();
            public void Load() { }
            public void Set(string key, string value) { }
        }

        private sealed class FilteringChildKeysSource : IConfigurationSource, IConfigurationProvider
        {
            private readonly string _hidden;
            public FilteringChildKeysSource(string hidden) => _hidden = hidden;
            public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) =>
                parentPath is null
                    ? earlierKeys.Where(key => !string.Equals(key, _hidden, StringComparison.OrdinalIgnoreCase)).ToArray()
                    : earlierKeys;
            public bool TryGet(string key, out string value)
            {
                value = null;
                return false;
            }
            public Primitives.IChangeToken GetReloadToken() => new ConfigurationReloadToken();
            public void Load() { }
            public void Set(string key, string value) { }
        }
    }
}
