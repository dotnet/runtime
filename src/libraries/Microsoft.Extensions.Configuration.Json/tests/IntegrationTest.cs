// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Test;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.Configuration.Json.Test
{
    public class IntegrationTest
    {
        [Fact]
        public void MinimalJson_GetChildrenFromConfiguration_NoConfigurationSection()
        {
            var json = @"{
            }";

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(TestStreamHelpers.StringToStream(json));
            var configuration = configurationBuilder.Build();

            Assert.Collection(configuration.GetChildren(),
                new Action<IConfigurationSection>[] {
            });
        }

        [Fact]
        public void LoadJsonConfiguration()
        {
            var json = @"{
                ""a"": ""b"",
                ""c"": {
                    ""d"": ""e""
                },
                ""f"": """",
                ""g"": null,
                ""h"": {},
                ""i"": {
                    ""k"": {}
                } 
            }";

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(TestStreamHelpers.StringToStream(json));
            var configuration = configurationBuilder.Build();

            Assert.Collection(configuration.GetChildren(),
                new Action<IConfigurationSection>[] {
                    x => AssertSection(x, "a", "b"),
                    x => AssertSection(x, "c", null, new Action<IConfigurationSection>[] {
                        x => AssertSection(x, "d", "e"),
                    }),
                    x => AssertSection(x, "f", ""),
                    x => AssertSection(x, "g", ""),
                    x => AssertSection(x, "h", null),
                    x => AssertSection(x, "i", null, new Action<IConfigurationSection>[] {
                        x => AssertSection(x, "k", null),
                    }),
            });
        }

        private static void AssertSection(IConfigurationSection configurationSection, string key, string value)
            => AssertSection(configurationSection, key, value, new Action<IConfigurationSection>[0]);

        private static void AssertSection(IConfigurationSection configurationSection, string key, string value, Action<IConfigurationSection>[] childrenInspectors)
        {
            if (key != configurationSection.Key || value != configurationSection.Value)
            {
                throw EqualException.ForMismatchedValues(
                    expected: GetString(key, value),
                    actual: GetString(configurationSection));
            }

            Assert.Collection(configurationSection.GetChildren(), childrenInspectors);
        }

        private static string GetString(IConfigurationSection configurationSection) => GetString(configurationSection.Key, configurationSection.Value);
        private static string GetString(string key, string value) => $"\"{key}\":" + (value is null ? "null" : $"\"{value}\"");
    }
}
