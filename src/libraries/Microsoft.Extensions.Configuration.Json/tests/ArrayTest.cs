// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Json.Test
{
    public class ArrayTest
    {
        [Fact]
        public void ArraysAreConvertedToKeyValuePairs()
        {
            var json = @"{
                ""ip"": [
                    ""1.2.3.4"",
                    ""7.8.9.10"",
                    ""11.12.13.14""
                ]
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));
            
            Assert.Equal("1.2.3.4", jsonConfigSource.Get("ip:0"));
            Assert.Equal("7.8.9.10", jsonConfigSource.Get("ip:1"));
            Assert.Equal("11.12.13.14", jsonConfigSource.Get("ip:2"));
        }

        [Fact]
        public void ArrayOfObjects()
        {
            var json = @"{
                ""ip"": [
                    {
                        ""address"": ""1.2.3.4"",
                        ""hidden"": false
                    },
                    {
                        ""address"": ""5.6.7.8"",
                        ""hidden"": true
                    }
                ]
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("1.2.3.4", jsonConfigSource.Get("ip:0:address"));
            Assert.Equal("False", jsonConfigSource.Get("ip:0:hidden"));
            Assert.Equal("5.6.7.8", jsonConfigSource.Get("ip:1:address"));
            Assert.Equal("True", jsonConfigSource.Get("ip:1:hidden"));
        }

        [Fact]
        public void NestedArrays()
        {
            var json = @"{
                ""ip"": [
                    [ 
                        ""1.2.3.4"",
                        ""5.6.7.8""
                    ],
                    [ 
                        ""9.10.11.12"",
                        ""13.14.15.16""
                    ]
                ]
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("1.2.3.4", jsonConfigSource.Get("ip:0:0"));
            Assert.Equal("5.6.7.8", jsonConfigSource.Get("ip:0:1"));
            Assert.Equal("9.10.11.12", jsonConfigSource.Get("ip:1:0"));
            Assert.Equal("13.14.15.16", jsonConfigSource.Get("ip:1:1"));
        }

        [Fact]
        public void ImplicitArrayItemReplacement()
        {
            var json1 = @"{
                ""ip"": [
                    ""1.2.3.4"",
                    ""7.8.9.10"",
                    ""11.12.13.14""
                ]
            }";

            var json2 = @"{
                ""ip"": [
                    ""15.16.17.18""
                ]
            }";

            var jsonConfigSource1 = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json1) };
            var jsonConfigSource2 = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json2) };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource1);
            configurationBuilder.Add(jsonConfigSource2);
            var config = configurationBuilder.Build();

            Assert.Equal(3, config.GetSection("ip").GetChildren().Count());
            Assert.Equal("15.16.17.18", config["ip:0"]);
            Assert.Equal("7.8.9.10", config["ip:1"]);
            Assert.Equal("11.12.13.14", config["ip:2"]);
        }

        [Fact]
        public void ExplicitArrayReplacement()
        {
            var json1 = @"{
                ""ip"": [
                    ""1.2.3.4"",
                    ""7.8.9.10"",
                    ""11.12.13.14""
                ]
            }";

            var json2 = @"{
                ""ip"": {
                    ""1"": ""15.16.17.18""
                }
            }";

            var jsonConfigSource1 = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json1) };
            var jsonConfigSource2 = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json2) };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource1);
            configurationBuilder.Add(jsonConfigSource2);
            var config = configurationBuilder.Build();

            Assert.Equal(3, config.GetSection("ip").GetChildren().Count());
            Assert.Equal("1.2.3.4", config["ip:0"]);
            Assert.Equal("15.16.17.18", config["ip:1"]);
            Assert.Equal("11.12.13.14", config["ip:2"]);
        }

        [Fact]
        public void ArrayMerge()
        {
            var json1 = @"{
                ""ip"": [
                    ""1.2.3.4"",
                    ""7.8.9.10"",
                    ""11.12.13.14""
                ]
            }";

            var json2 = @"{
                ""ip"": {
                    ""3"": ""15.16.17.18""
                }
            }";

            var jsonConfigSource1 = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json1) };
            var jsonConfigSource2 = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json2) };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource1);
            configurationBuilder.Add(jsonConfigSource2);
            var config = configurationBuilder.Build();

            Assert.Equal(4, config.GetSection("ip").GetChildren().Count());
            Assert.Equal("1.2.3.4", config["ip:0"]);
            Assert.Equal("7.8.9.10", config["ip:1"]);
            Assert.Equal("11.12.13.14", config["ip:2"]);
            Assert.Equal("15.16.17.18", config["ip:3"]);
        }

        [Fact]
        public void ArraysAreKeptInFileOrder()
        {
            var json = @"{
                ""setting"": [
                    ""b"",
                    ""a"",
                    ""2""
                ]
            }";

            var jsonConfigSource = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json) };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource);
            var config = configurationBuilder.Build();

            var configurationSection = config.GetSection("setting");
            var indexConfigurationSections = configurationSection.GetChildren().ToArray();

            Assert.Equal(3, indexConfigurationSections.Count());
            Assert.Equal("b", indexConfigurationSections[0].Value);
            Assert.Equal("a", indexConfigurationSections[1].Value);
            Assert.Equal("2", indexConfigurationSections[2].Value);
        }

        [Fact]
        public void PropertiesAreSortedByNumberOnlyFirst()
        {
            var json = @"{
                ""setting"": {
                    ""hello"": ""a"",
                    ""bob"": ""b"",
                    ""42"": ""c"",
                    ""4"":""d"",
                    ""10"": ""e"",
                    ""1text"": ""f""
                }
            }";

            var jsonConfigSource = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json) };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource);
            var config = configurationBuilder.Build();

            var configurationSection = config.GetSection("setting");
            var indexConfigurationSections = configurationSection.GetChildren().ToArray();

            Assert.Equal(6, indexConfigurationSections.Count());
            Assert.Equal("4", indexConfigurationSections[0].Key);
            Assert.Equal("10", indexConfigurationSections[1].Key);
            Assert.Equal("42", indexConfigurationSections[2].Key);
            Assert.Equal("1text", indexConfigurationSections[3].Key);
            Assert.Equal("bob", indexConfigurationSections[4].Key);
            Assert.Equal("hello", indexConfigurationSections[5].Key);
        }

        [Fact]
        public void TrailingCommas()
        {
            var json = @"{
                ""ip"": [
                    [ 
                        ""1.2.3.4"",
                        ""5.6.7.8"",
                    ],
                    [ 
                        ""9.10.11.12"",
                        ""13.14.15.16"",
                    ],
                ]
            }";

            var jsonConfigSource = new JsonConfigurationProvider(new JsonConfigurationSource());
            jsonConfigSource.Load(TestStreamHelpers.StringToStream(json));

            Assert.Equal("1.2.3.4", jsonConfigSource.Get("ip:0:0"));
            Assert.Equal("5.6.7.8", jsonConfigSource.Get("ip:0:1"));
            Assert.Equal("9.10.11.12", jsonConfigSource.Get("ip:1:0"));
            Assert.Equal("13.14.15.16", jsonConfigSource.Get("ip:1:1"));
        }
    }
}
