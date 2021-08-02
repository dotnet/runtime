// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationPathTests
    {
        private const string ConfigName = "APP_CONFIG_FILE";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomAppConfigIsUsedWhenSpecifiedAsRelativePath()
        {
            const string SettingName = "test_CustomAppConfigIsUsedWhenSpecified";
            string expectedSettingValue = Guid.NewGuid().ToString();
            string configFilePath = CreateAppConfigFileWithSetting(SettingName, expectedSettingValue);

            RemoteExecutor.Invoke((string configFilePath, string expectedSettingValue) => {
                AppDomain.CurrentDomain.SetData(ConfigName, configFilePath);
                Assert.Equal(expectedSettingValue, ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath, expectedSettingValue).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomAppConfigIsUsedWhenSpecifiedAsAbsolutePath()
        {
            const string SettingName = "test_CustomAppConfigIsUsedWhenSpecified";
            string expectedSettingValue = Guid.NewGuid().ToString();
            string configFilePath = Path.GetFullPath(CreateAppConfigFileWithSetting(SettingName, expectedSettingValue));

            RemoteExecutor.Invoke((string configFilePath, string expectedSettingValue) => {
                AppDomain.CurrentDomain.SetData(ConfigName, configFilePath);
                Assert.Equal(expectedSettingValue, ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath, expectedSettingValue).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomAppConfigIsUsedWhenSpecifiedAsAbsoluteUri()
        {
            const string SettingName = "test_CustomAppConfigIsUsedWhenSpecified";
            string expectedSettingValue = Guid.NewGuid().ToString();
            string configFilePath = new Uri(Path.GetFullPath(CreateAppConfigFileWithSetting(SettingName, expectedSettingValue))).ToString();

            RemoteExecutor.Invoke((string configFilePath, string expectedSettingValue) => {
                AppDomain.CurrentDomain.SetData(ConfigName, configFilePath);
                Assert.Equal(expectedSettingValue, ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath, expectedSettingValue).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void NoErrorWhenCustomAppConfigIsSpecifiedAndItDoesNotExist()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetData(ConfigName, "non-existing-file.config");
                Assert.Null(ConfigurationManager.AppSettings["AnySetting"]);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MalformedAppConfigCausesException()
        {
            const string SettingName = "AnySetting";

            // Following will cause malformed config file
            string configFilePath = CreateAppConfigFileWithSetting(SettingName, "\"");

            RemoteExecutor.Invoke((string configFilePath) => {
                AppDomain.CurrentDomain.SetData(ConfigName, configFilePath);
                Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath).Dispose();
        }

        private static string CreateAppConfigFileWithSetting(string key, string rawUnquotedValue)
        {
            string fileName = Path.GetRandomFileName() + ".config";
            File.WriteAllText(fileName,
                @$"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
    <add key=""{key}"" value=""{rawUnquotedValue}""/>
  </appSettings>
</configuration>");

            return fileName;
        }
    }
}
