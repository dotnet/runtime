// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Xunit;

namespace System.ConfigurationTests
{
    [Collection(nameof(DisableParallelization))]
    public class LocalFileSettingsProviderTests
    {
        private readonly SettingsContext _testContext = new SettingsContext
        {
            ["GroupName"] = "GroupNameFoo",
            ["SettingsKey"] = "SettingsKeyFoo"
        };

        [Fact]
        public void GetPropertyValues_NotStoredProperty_ValueEqualsNull()
        {
            var property = new SettingsProperty("PropertyName");
            property.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
            var properties = new SettingsPropertyCollection();
            properties.Add(property);
            var localFileSettingsProvider = new LocalFileSettingsProvider();

            SettingsPropertyValueCollection propertyValues = localFileSettingsProvider.GetPropertyValues(_testContext, properties);

            Assert.Equal(1, propertyValues.Count);
            Assert.Null(propertyValues["PropertyName"].PropertyValue);
        }

        [Fact]
        public void GetPropertyValues_NotStoredConnectionStringProperty_ValueEqualsEmptyString()
        {
            var property = new SettingsProperty("PropertyName");
            property.PropertyType = typeof (string);
            property.Attributes.Add(typeof(ApplicationScopedSettingAttribute), new ApplicationScopedSettingAttribute());
            property.Attributes.Add(typeof(SpecialSettingAttribute), new SpecialSettingAttribute(SpecialSetting.ConnectionString));
            var properties = new SettingsPropertyCollection();
            properties.Add(property);
            var localFileSettingsProvider = new LocalFileSettingsProvider();

            SettingsPropertyValueCollection propertyValues = localFileSettingsProvider.GetPropertyValues(_testContext, properties);

            Assert.Equal(1, propertyValues.Count);
            Assert.Equal(string.Empty, propertyValues["PropertyName"].PropertyValue);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(42)]
        [InlineData(867.5309)]
        [InlineData(StringComparison.Ordinal)]
        public void GetPropertyValues_DefaultValueApplied(object defaultValue)
        {
            var provider = new LocalFileSettingsProvider();
            var property = new SettingsProperty(
                "Test",
                defaultValue.GetType(),
                provider,
                false,
                defaultValue,
                SettingsSerializeAs.Xml,
                new SettingsAttributeDictionary(),
                false,
                false);
            property.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());

            var properties = new SettingsPropertyCollection() { property };
            var propertyValues = provider.GetPropertyValues(_testContext, properties);

            Assert.Equal(1, propertyValues.Count);
            Assert.Equal(defaultValue, propertyValues["Test"].PropertyValue);
        }

        [Fact]
        public void AppleMobileIdentity_UsesStableBoundedBundleIdentifierHash()
        {
            const string FirstPath = "/var/containers/Bundle/Application/F67E5161-EBAA-4084-B89C-2D17C837D315/Test.app/Test.dll";
            const string SecondPath = "/var/containers/Bundle/Application/9293AD65-BCC7-453C-8D42-8902B64FF19E/Test.app/Test.dll";

            string first = GetApplicationIdentitySuffix(
                FirstPath,
                isSingleFile: false,
                isAppleMobile: true,
                bundleIdentifier: "com.contoso.test");
            string second = GetApplicationIdentitySuffix(
                SecondPath,
                isSingleFile: false,
                isAppleMobile: true,
                bundleIdentifier: "com.contoso.test");

            Assert.Equal(first, second);
            Assert.StartsWith("_BundleIdentifier_", first);
            Assert.Equal("_BundleIdentifier_".Length + 32, first.Length);
        }

        [Fact]
        public void NonAppleMobileIdentity_PreservesExistingBehavior()
        {
            const string ApplicationPath = "/Applications/Test/Test.dll";

            string expected = GetApplicationIdentitySuffix(
                ApplicationPath,
                isSingleFile: false,
                isAppleMobile: false,
                bundleIdentifier: null);
            string actual = GetApplicationIdentitySuffix(
                ApplicationPath,
                isSingleFile: false,
                isAppleMobile: false,
                bundleIdentifier: "com.contoso.ignored");

            Assert.Equal(expected, actual);
            Assert.DoesNotContain("BundleIdentifier", actual);
        }

        [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.tvOS)]
        [Fact]
        public void AppleMobileBundleIdentifier_IsAvailable()
        {
            Type appleApplication = typeof(LocalFileSettingsProvider).Assembly.GetType("System.Configuration.AppleApplication");
            MethodInfo getMainBundleIdentifier = appleApplication.GetMethod(
                "GetMainBundleIdentifier",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.False(string.IsNullOrEmpty((string)getMainBundleIdentifier.Invoke(null, null)));
        }

        [Fact]
        public void FindPreviousConfigFile_StableHierarchyTakesPrecedence()
        {
            using var temp = new TempDirectory();
            string companyDirectory = temp.Path;
            string stableIdentity = Path.Combine(companyDirectory, StableIdentityName);
            string currentDirectory = CreateVersion(stableIdentity, "3.0.0.0", createConfig: false);
            string expected = Path.Combine(CreateVersion(stableIdentity, "1.0.0.0"), UserConfigFilename);
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('a')), "2.0.0.0");

            string actual = FindPreviousConfigFile(
                currentDirectory,
                "3.0.0.0",
                UserConfigFilename,
                LegacyPrefix);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FindPreviousConfigFile_WithoutLegacyPrefixPreservesExistingSelection()
        {
            using var temp = new TempDirectory();
            string identityDirectory = Path.Combine(temp.Path, "TestApp_Url_" + new string('a', 32));
            string currentDirectory = CreateVersion(identityDirectory, "3.0.0.0", createConfig: false);
            CreateVersion(identityDirectory, "1.0.0.0");
            CreateVersion(identityDirectory, "2.0.0.0", createConfig: false);

            string actual = FindPreviousConfigFile(
                currentDirectory,
                "3.0.0.0",
                UserConfigFilename,
                legacyDirectoryPrefix: null);

            Assert.Null(actual);
        }

        [Fact]
        public void FindPreviousConfigFile_LegacyHierarchySelectsHighestValidPriorVersion()
        {
            using var temp = new TempDirectory();
            string companyDirectory = temp.Path;
            string currentDirectory = CreateVersion(
                Path.Combine(companyDirectory, StableIdentityName),
                "4.0.0.0",
                createConfig: false);
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('a')), "1.0.0.0");
            string expected = Path.Combine(
                CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('b')), "3.0.0.0"),
                UserConfigFilename);
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('c')), "4.0.0.0");
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('d')), "5.0.0.0");

            string actual = FindPreviousConfigFile(
                currentDirectory,
                "4.0.0.0",
                UserConfigFilename,
                LegacyPrefix);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FindPreviousConfigFile_ExcludesMalformedAndUnrelatedDirectories()
        {
            using var temp = new TempDirectory();
            string companyDirectory = temp.Path;
            string currentDirectory = CreateVersion(
                Path.Combine(companyDirectory, StableIdentityName),
                "3.0.0.0",
                createConfig: false);
            CreateVersion(Path.Combine(companyDirectory, "OtherApp_Url_" + new string('a', 32)), "2.0.0.0");
            CreateVersion(Path.Combine(companyDirectory, LegacyPrefix + "Url_" + new string('6', 32)), "2.0.0.0");
            CreateVersion(Path.Combine(companyDirectory, LegacyPrefix + "Unknown_" + new string('a', 32)), "2.0.0.0");
            CreateVersion(Path.Combine(companyDirectory, LegacyPrefix + "Url_short"), "2.0.0.0");
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('a')), "not-a-version");
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('b')), "2.0.0.0", createConfig: false);

            string actual = FindPreviousConfigFile(
                currentDirectory,
                "3.0.0.0",
                UserConfigFilename,
                LegacyPrefix);

            Assert.Null(actual);
        }

        [Fact]
        public void FindPreviousConfigFile_AmbiguousHighestLegacyVersionIsNotSelected()
        {
            using var temp = new TempDirectory();
            string companyDirectory = temp.Path;
            string currentDirectory = CreateVersion(
                Path.Combine(companyDirectory, StableIdentityName),
                "3.0.0.0",
                createConfig: false);
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('a')), "2.0.0.0");
            CreateVersion(Path.Combine(companyDirectory, LegacyIdentityName('b')), "2.0.0.0");

            string actual = FindPreviousConfigFile(
                currentDirectory,
                "3.0.0.0",
                UserConfigFilename,
                LegacyPrefix);

            Assert.Null(actual);
        }

        private const string LegacyPrefix = "TestApp_";
        private const string StableIdentityName = LegacyPrefix + "BundleIdentifier_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string UserConfigFilename = "user.config";

        private static string GetApplicationIdentitySuffix(
            string applicationPath,
            bool isSingleFile,
            bool isAppleMobile,
            string bundleIdentifier)
        {
            Type clientConfigPaths = typeof(LocalFileSettingsProvider).Assembly.GetType("System.Configuration.ClientConfigPaths");
            MethodInfo getApplicationIdentitySuffix = clientConfigPaths.GetMethod(
                "GetApplicationIdentitySuffix",
                BindingFlags.NonPublic | BindingFlags.Static);

            return (string)getApplicationIdentitySuffix.Invoke(
                null,
                new object[] { applicationPath, isSingleFile, isAppleMobile, bundleIdentifier });
        }

        private static string FindPreviousConfigFile(
            string currentConfigDirectory,
            string currentVersion,
            string userConfigFilename,
            string legacyDirectoryPrefix)
        {
            MethodInfo findPreviousConfigFile = typeof(LocalFileSettingsProvider).GetMethod(
                "FindPreviousConfigFile",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(string) },
                modifiers: null);

            return (string)findPreviousConfigFile.Invoke(
                null,
                new object[] { currentConfigDirectory, currentVersion, userConfigFilename, legacyDirectoryPrefix });
        }

        private static string LegacyIdentityName(char hashCharacter)
        {
            return LegacyPrefix + "Url_" + new string(hashCharacter, 32);
        }

        private static string CreateVersion(string identityDirectory, string version, bool createConfig = true)
        {
            string versionDirectory = Directory.CreateDirectory(Path.Combine(identityDirectory, version)).FullName;
            if (createConfig)
            {
                File.WriteAllText(Path.Combine(versionDirectory, UserConfigFilename), "<configuration />");
            }

            return versionDirectory;
        }
    }
}
