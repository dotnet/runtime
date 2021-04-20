// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Configuration.FunctionalTests
{
    public class ArrayTests : IDisposable
    {
        private string _iniConfigFilePath;
        private string _xmlConfigFilePath;
        private string _json1ConfigFilePath;
        private string _json2ConfigFilePath;

        private static readonly string _iniConfigFileContent = @"
[address]
2=ini_2.2.2.2
i=ini_i.i.i.i
";
        private static readonly string _xmlConfigFileContent = @"
<settings>
    <address name=""4"">xml_4.4.4.4</address>
    <address name=""1"">xml_1.1.1.1</address>
    <address name=""x"">xml_x.x.x.x</address>
</settings>
";
        private static readonly string _json1ConfigFileContent = @"
{
    ""address"": [
        ""json_0.0.0.0"",
        ""json_1.1.1.1"",
        ""json_2.2.2.2""
    ]
}
";

        private static readonly string _json2ConfigFileContent = @"
{
    ""address"": {
        ""j"": ""json_j.j.j.j"",
        ""3"": ""json_3.3.3.3""
    }
}
";

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void DifferentConfigSources_Merged_KeysAreSorted()
        {
            var config = BuildConfig();

            var configurationSection = config.GetSection("address");
            var indexConfigurationSections = configurationSection.GetChildren().ToArray();

            Assert.Equal(8, indexConfigurationSections.Length);
            Assert.Equal("0", indexConfigurationSections[0].Key);
            Assert.Equal("1", indexConfigurationSections[1].Key);
            Assert.Equal("2", indexConfigurationSections[2].Key);
            Assert.Equal("3", indexConfigurationSections[3].Key);
            Assert.Equal("4", indexConfigurationSections[4].Key);
            Assert.Equal("i", indexConfigurationSections[5].Key);
            Assert.Equal("j", indexConfigurationSections[6].Key);
            Assert.Equal("x", indexConfigurationSections[7].Key);

            Assert.Equal("address:0", indexConfigurationSections[0].Path);
            Assert.Equal("address:1", indexConfigurationSections[1].Path);
            Assert.Equal("address:2", indexConfigurationSections[2].Path);
            Assert.Equal("address:3", indexConfigurationSections[3].Path);
            Assert.Equal("address:4", indexConfigurationSections[4].Path);
            Assert.Equal("address:i", indexConfigurationSections[5].Path);
            Assert.Equal("address:j", indexConfigurationSections[6].Path);
            Assert.Equal("address:x", indexConfigurationSections[7].Path);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void DifferentConfigSources_Merged_WithOverwrites()
        {
            var config = BuildConfig();

            Assert.Equal("json_0.0.0.0", config["address:0"]);
            Assert.Equal("xml_1.1.1.1", config["address:1"]);
            Assert.Equal("ini_2.2.2.2", config["address:2"]);
            Assert.Equal("json_3.3.3.3", config["address:3"]);
            Assert.Equal("xml_4.4.4.4", config["address:4"]);
            Assert.Equal("ini_i.i.i.i", config["address:i"]);
            Assert.Equal("json_j.j.j.j", config["address:j"]);
            Assert.Equal("xml_x.x.x.x", config["address:x"]);
        }

        private IConfiguration BuildConfig()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(_json1ConfigFilePath);
            configurationBuilder.AddIniFile(_iniConfigFilePath);
            configurationBuilder.AddJsonFile(_json2ConfigFilePath);
            configurationBuilder.AddXmlFile(_xmlConfigFilePath);
            return configurationBuilder.Build();
        }

        public ArrayTests()
        {
            var basePath = AppContext.BaseDirectory ?? string.Empty;
            _iniConfigFilePath = Path.GetRandomFileName();
            _xmlConfigFilePath = Path.GetRandomFileName();
            _json1ConfigFilePath = Path.GetRandomFileName();
            _json2ConfigFilePath = Path.GetRandomFileName();

            File.WriteAllText(Path.Combine(basePath, _iniConfigFilePath), _iniConfigFileContent);
            File.WriteAllText(Path.Combine(basePath, _xmlConfigFilePath), _xmlConfigFileContent);
            File.WriteAllText(Path.Combine(basePath, _json1ConfigFilePath), _json1ConfigFileContent);
            File.WriteAllText(Path.Combine(basePath, _json2ConfigFilePath), _json2ConfigFileContent);
        }

        public void Dispose()
        {
            File.Delete(_iniConfigFilePath);
            File.Delete(_xmlConfigFilePath);
            File.Delete(_json1ConfigFilePath);
            File.Delete(_json2ConfigFilePath);
        }
    }
}
