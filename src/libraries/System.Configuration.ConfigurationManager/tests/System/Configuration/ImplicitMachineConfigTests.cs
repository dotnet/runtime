// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Configuration;
using System.Configuration.Internal;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class ImplicitMachineConfigTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void RuntimeAppSettingsAccessible()
        {
            var appSettings = ConfigurationManager.AppSettings;
            Assert.NotNull(appSettings);
        }

        [Fact]
        public void DesignTimeAppSettingsAccessible()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config);
                Assert.NotNull(config.AppSettings);
            }
        }

        [Fact]
        public void DesignTimeAppSettingsFailWithMissingMachineConfig_1()
        {
            // ConfigurationFileMap checks for existence in the constructor
            string missingFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".config");
            AssertExtensions.Throws<ArgumentException>("machineConfigFilename", () => new ConfigurationFileMap(missingFile));
        }

        [Fact]
        public void DesignTimeAppSettingsFailWithMissingMachineConfig_2()
        {
            // Get around the existence check by using the default constructor
            string missingFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".config");
            ConfigurationFileMap map = new ConfigurationFileMap();
            map.MachineConfigFilename = missingFile;
            var config = ConfigurationManager.OpenMappedMachineConfiguration(map);
            Assert.NotNull(config);
            Assert.Null(config.AppSettings);
        }

        [Fact]
        public void DesignTimeAppSettingsFailWithEmptyMachineConfig()
        {
            // If we've explicitly specified a machine config and it doesn't define the AppSettingsType we shouldn't be able
            // to get it (e.g. we haven't stubbed in overtop).
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenMappedMachineConfiguration(new ConfigurationFileMap(temp.ConfigPath));
                Assert.NotNull(config);

                Assert.Null(config.AppSettings);
            }
        }

        [Fact]
        public void EnsureInitWithDifferentOrderHostParams() 
        {
            string assemblyName = PlatformDetection.IsFullFramework ? "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" : "System.Configuration.ConfigurationManager";

            // InternalConfigFactory allows you to specify your own host / hostInitParams
            // Ensure ImplictMachineConfigHost can init within this process and not throw an Invalid cast exception 
            using (var temp = new TempConfig(TestData.EmptyConfig)) 
            {
                string typeName = "System.Configuration.Internal.InternalConfigConfigurationFactory, " + assemblyName;

                Type type = Type.GetType(typeName, true);
                var configFactory = (IInternalConfigConfigurationFactory) Activator.CreateInstance(type, true);
                var config = configFactory.Create(typeof(TempConfigurationHost), "test", new ConfigurationFileMap(temp.ConfigPath), "test");

                Assert.NotNull(config);
            }
        }
    }
}
