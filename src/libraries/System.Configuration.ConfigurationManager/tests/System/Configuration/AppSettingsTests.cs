// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
