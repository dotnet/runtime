// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class AppSettingsTests
    {
        [Fact]
        public void EmptyRuntimeAppSettings()
        {
            var appSettings = ConfigurationManager.AppSettings;
            Assert.NotNull(appSettings);
            Assert.Empty(appSettings);
        }

        [Fact]
        public void EmptyDesignTimeAppSettings()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config.AppSettings);
                Assert.Empty(config.AppSettings.Settings);
            }
        }

        [Fact]
        public void SimpleAppSettings()
        {
            using (var temp = new TempConfig(TestData.SimpleConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config.AppSettings);
                Assert.Equal(2, config.AppSettings.Settings.Count);
                Assert.Equal("FooValue", config.AppSettings.Settings["FooKey"].Value);
                Assert.Equal("BarValue", config.AppSettings.Settings["BarKey"].Value);
            }
        }

        [Fact]
        public void AddToAppSettings_NoSave()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config.AppSettings);
                Assert.Empty(config.AppSettings.Settings);

                config.AppSettings.Settings.Add("NewKey", "NewValue");
                Assert.NotEmpty(config.AppSettings.Settings);
                Assert.Equal("NewValue", config.AppSettings.Settings["NewKey"].Value);

                config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.Empty(config.AppSettings.Settings);
            }
        }

        [Fact]
        [ActiveIssue("dotnet/corefx #19336", TargetFrameworkMonikers.NetFramework)]
        public void AddToAppSettings_Save()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config.AppSettings);
                Assert.Empty(config.AppSettings.Settings);

                config.AppSettings.Settings.Add("NewKey", "NewValue");
                Assert.NotEmpty(config.AppSettings.Settings);
                Assert.Equal("NewValue", config.AppSettings.Settings["NewKey"].Value);

                config.Save();

                // Make sure we didn't serialize the implicit machine.config
                Assert.False(File.Exists(new ConfigurationFileMap().MachineConfigFilename));

                config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotEmpty(config.AppSettings.Settings);
                Assert.Equal("NewValue", config.AppSettings.Settings["NewKey"].Value);
            }
        }

        [Fact]
        public void AppSettingsCannotLoadFromUser()
        {
            // By default you can't load a section from a user config file- validating that appSettings falls in this bucket
            using (var machine = new TempConfig(TestData.ImplicitMachineConfig))
            using (var exe = new TempConfig(TestData.EmptyConfig))
            using (var user = new TempConfig(TestData.SimpleConfig))
            {
                ExeConfigurationFileMap map = new ExeConfigurationFileMap
                {
                    MachineConfigFilename = machine.ConfigPath,
                    ExeConfigFilename = exe.ConfigPath,
                    RoamingUserConfigFilename = user.ConfigPath
                };

                var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

                Assert.Throws<ConfigurationErrorsException>(() => config.AppSettings);
            }
        }

        public static string AppSettingsConfig =
@"<?xml version='1.0' encoding='utf-8' ?>
<appSettings>
  <add key='AppSettingsKey' value='AppSettingsValue'/>
</appSettings>";

        [Fact]
        public void AppSettingsCannotLoadFromConfigSource()
        {
            using (var tempConfig = new TempConfig(TestData.EmptyConfig))
            {
                const string SubDirectory = "Config";
                const string AppConfigFileName = "tempAppConfig.config";
                string tempConfigDirectory = Path.Combine(Path.GetDirectoryName(tempConfig.ConfigPath), SubDirectory);
                using (var tempDirectory = new TempDirectory(tempConfigDirectory))
                {
                    // set configSource and save the config
                    var config = ConfigurationManager.OpenExeConfiguration(tempConfig.ExePath);
                    config.AppSettings.SectionInformation.ConfigSource = Path.Combine(SubDirectory, AppConfigFileName);
                    config.Save();

                    // write temporary appConfig
                    var tempAppConfigPath = Path.Combine(tempConfigDirectory, AppConfigFileName);
                    File.WriteAllText(tempAppConfigPath, AppSettingsConfig);

                    // load config and test the appSettings
                    config = ConfigurationManager.OpenExeConfiguration(tempConfig.ExePath);
                    Assert.NotEmpty(config.AppSettings.Settings);
                    Assert.Equal("AppSettingsValue", config.AppSettings.Settings["AppSettingsKey"].Value);
                }
            }
        }

        public static IEnumerable<object[]> ConfigPaths = new List<object[]>()
        {
            new object[] { @"../Config/foo.config" },
            new object[] { @"..\Config\foo.config" },
            new object[] { @"\Config\foo.config" },
            new object[] { @"/Config/foo.config" },
            new object[] { @"\..\Config\foo.config" },
            new object[] { @"/../Config/foo.config" },
        };

        [Theory]
        [MemberData(nameof(ConfigPaths))]
        public void AppSettingsInvalidConfigSourcePath_Throws(string configPath)
        {
            using (var tempConfig = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(tempConfig.ExePath);
                Assert.ThrowsAny<Exception>(() => config.AppSettings.SectionInformation.ConfigSource = configPath);
            }
        }
    }
}
