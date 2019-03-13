// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class ApplicationSettingsBaseTests
    {
        private const int DefaultIntPropertyValue = 42;

        private class SimpleSettings : ApplicationSettingsBase
        {
            [ApplicationScopedSetting]
            public string StringProperty
            {
                get
                {
                    return (string) this[nameof(StringProperty)];
                }
                set
                {
                    this[nameof(StringProperty)] = value;
                }
            }

            [UserScopedSetting]
            [DefaultSettingValue("42")]
            public int IntProperty
            {
                get
                {
                    return (int)this[nameof(IntProperty)];
                }
                set
                {
                    this[nameof(IntProperty)] = value;
                }
            }
        }

        private class PersistedSimpleSettings : SimpleSettings
        {
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp)),
            InlineData(true),
            InlineData(false)
            ]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        public void Context_SimpleSettings_InNotNull(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings())
                : new SimpleSettings();

            Assert.NotNull(settings.Context);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp)),
            InlineData(true),
            InlineData(false)
            ]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        public void Providers_SimpleSettings_Empty(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings())
                : new SimpleSettings();

            Assert.Equal(1, settings.Providers.Count);
            Assert.NotNull(settings.Providers[typeof(LocalFileSettingsProvider).Name]);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp)),
            InlineData(true),
            InlineData(false)
            ]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        public void GetSetStringProperty_SimpleSettings_Ok(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized 
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings()) 
                : new SimpleSettings();

            Assert.Equal(default, settings.StringProperty);
            settings.StringProperty = "Foo";
            Assert.Equal("Foo", settings.StringProperty);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp)),
            InlineData(true),
            InlineData(false)
            ]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        public void GetSetIntProperty_SimpleSettings_Ok(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings())
                : new SimpleSettings();

            Assert.Equal(DefaultIntPropertyValue, settings.IntProperty);
            settings.IntProperty = 10;
            Assert.Equal(10, settings.IntProperty);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer)),
            InlineData(true),
            InlineData(false)]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/35668", TestPlatforms.AnyUnix)]
        public void Save_SimpleSettings_Ok(bool isSynchronized)
        {
            PersistedSimpleSettings settings = isSynchronized
                ? (PersistedSimpleSettings)SettingsBase.Synchronized(new PersistedSimpleSettings())
                : new PersistedSimpleSettings();

            // Make sure we're clean
            settings.Reset();
            settings.Save();
            Assert.Equal(DefaultIntPropertyValue, settings.IntProperty);
            Assert.Equal(default, settings.StringProperty);

            // Change settings and save
            settings.IntProperty = 12;
            settings.StringProperty = "Bar";
            Assert.Equal("Bar", settings.StringProperty);
            Assert.Equal(12, settings.IntProperty);
            settings.Save();

            // Create a new instance and validate persisted settings
            settings = isSynchronized
                            ? (PersistedSimpleSettings)SettingsBase.Synchronized(new PersistedSimpleSettings())
                            : new PersistedSimpleSettings();
            Assert.Equal(default, settings.StringProperty); // [ApplicationScopedSetting] isn't persisted
            Assert.Equal(12, settings.IntProperty);

            // Reset and save
            settings.Reset();
            settings.Save();
            Assert.Equal(DefaultIntPropertyValue, settings.IntProperty);
            Assert.Equal(default, settings.StringProperty);

            // Create a new instance and validate persisted settings
            settings = isSynchronized
                            ? (PersistedSimpleSettings)SettingsBase.Synchronized(new PersistedSimpleSettings())
                            : new PersistedSimpleSettings();
            Assert.Equal(default, settings.StringProperty); // [ApplicationScopedSetting] isn't persisted
            Assert.Equal(DefaultIntPropertyValue, settings.IntProperty);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        public void Reload_SimpleSettings_Ok()
        {
            var settings = new SimpleSettings
            {
                IntProperty = 10
            };

            Assert.NotEqual(DefaultIntPropertyValue, settings.IntProperty);
            settings.Reload();
            Assert.Equal(DefaultIntPropertyValue, settings.IntProperty);
        }

        [ReadOnly(false)]
        [SettingsGroupName("TestGroup")]
        [SettingsProvider(typeof(TestProvider))]
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
        private class SettingsWithAttributes : ApplicationSettingsBase
        {
            [ApplicationScopedSetting]
            [SettingsProvider(typeof(TestProvider))]
            public string StringProperty
            {
                get
                {
                    return (string)this["StringProperty"];
                }
                set
                {
                    this["StringProperty"] = value;
                }
            }
        }

        private class TestProvider : LocalFileSettingsProvider
        {
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        [ActiveIssue("dotnet/corefx #18832", TargetFrameworkMonikers.NetFramework)]
        public void SettingsProperty_SettingsWithAttributes_Ok()
        {
            SettingsWithAttributes settings = new SettingsWithAttributes();

            Assert.Equal(1, settings.Properties.Count);
            SettingsProperty property = settings.Properties["StringProperty"];
            Assert.Equal(typeof(TestProvider), property.Provider.GetType());
            Assert.Equal(SettingsSerializeAs.Binary, property.SerializeAs);
        }
    }
}
