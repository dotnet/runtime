// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Json.Tests
{
    public class ConfigurationBinderTest
    {
        public class EnumConf : Dictionary<TestSettingsEnum, string>
        {
        }
        public enum TestSettingsEnum
        {
            Option1,
            Option2,
        }
        [Fact]
        public void EnumBindCaseInsensitiveNotThrows()
        {

            var json = @"{
                ""setting"": {
                    ""Option1"":""opt1"",
                    ""option2"":""opt2"",
                }
            }";

            var jsonConfigSource = new JsonConfigurationSource { FileProvider = TestStreamHelpers.StringToFileProvider(json) };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(jsonConfigSource);
            var config = configurationBuilder.Build();
            var configSection = config.GetSection("setting");

            var configOptions = new EnumConf();
            configSection.Bind(configOptions);

            Assert.Equal("opt1", configOptions[TestSettingsEnum.Option1]);
            Assert.Equal("opt2", configOptions[TestSettingsEnum.Option2]);
        }

    }
}
