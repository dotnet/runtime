// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationPathTests : FileCleanupTestBase
    {
        public ConfigurationPathTests() : base(AppDomain.CurrentDomain.BaseDirectory) // We do not want the files go to temporary directory as that will not test the relative paths correctly
        {
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomAppConfigIsUsedWhenSpecifiedAsRelativePath()
        {
            const string SettingName = "test_CustomAppConfigIsUsedWhenSpecified";
            string expectedSettingValue = Guid.NewGuid().ToString();
            string configFilePath = Path.Combine(GetTestDirectoryName(), CreateAppConfigFileWithSetting(SettingName, expectedSettingValue));

            RemoteExecutor.Invoke((string configFilePath, string expectedSettingValue) => {
                // We change directory so that if product tries to read from the current directory which usually happens to be same as BaseDirectory the test will fail
                Environment.CurrentDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFilePath);
                Assert.Equal(expectedSettingValue, ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath, expectedSettingValue).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomAppConfigIsUsedWhenSpecifiedAsAbsolutePath()
        {
            const string SettingName = "test_CustomAppConfigIsUsedWhenSpecified";
            string expectedSettingValue = Guid.NewGuid().ToString();
            string configFilePath = Path.Combine(TestDirectory, CreateAppConfigFileWithSetting(SettingName, expectedSettingValue));

            RemoteExecutor.Invoke((string configFilePath, string expectedSettingValue) => {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFilePath);
                Assert.Equal(expectedSettingValue, ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath, expectedSettingValue).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fails when path contains #")]
        public void CustomAppConfigIsUsedWhenSpecifiedAsAbsoluteUri()
        {
            const string SettingName = "test_CustomAppConfigIsUsedWhenSpecified";
            string expectedSettingValue = Guid.NewGuid().ToString();
            string configFilePath = new Uri(Path.Combine(TestDirectory, CreateAppConfigFileWithSetting(SettingName, expectedSettingValue))).ToString();

            RemoteExecutor.Invoke((string configFilePath, string expectedSettingValue) => {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFilePath);
                Assert.Equal(expectedSettingValue, ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath, expectedSettingValue).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void NoErrorWhenCustomAppConfigIsSpecifiedAndItDoesNotExist()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", "non-existing-file.config");
                Assert.Null(ConfigurationManager.AppSettings["AnySetting"]);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MalformedAppConfigCausesException()
        {
            const string SettingName = "AnySetting";

            // Following will cause malformed config file
            string configFilePath = Path.Combine(TestDirectory, CreateAppConfigFileWithSetting(SettingName, "\""));

            RemoteExecutor.Invoke((string configFilePath) => {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFilePath);
                Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.AppSettings[SettingName]);
            }, configFilePath).Dispose();
        }

        private string GetTestDirectoryName()
        {
            string dir = TestDirectory;
            if (dir.EndsWith("\\") || dir.EndsWith("/"))
                dir = dir.Substring(0, dir.Length - 1);

            return Path.GetFileName(dir);
        }

        private string CreateAppConfigFileWithSetting(string key, string rawUnquotedValue, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string fileName = GetTestFileName(null, memberName, lineNumber) + ".config";
            File.WriteAllText(Path.Combine(TestDirectory, fileName),
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
