// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
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

        public class SettingsWithProvider : ApplicationSettingsBase
        {
            [Setting]
            [SettingsProvider(typeof(CustomProvider))]
            public string StringPropertyWithProvider
            {
                get
                {
                    return (string)this[nameof(StringPropertyWithProvider)];
                }
                set
                {
                    this[nameof(StringPropertyWithProvider)] = value;
                }
            }

            [UserScopedSetting]
            public string StringProperty
            {
                get
                {
                    return (string)this[nameof(StringProperty)];
                }
                set
                {
                    this[nameof(StringProperty)] = value;
                }
            }

            public class CustomProvider : SettingsProvider
            {
                public const string DefaultStringPropertyValue = "stringPropertySet";
                public override string ApplicationName { get; set; }

                public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
                {
                    SettingsPropertyValueCollection result = new SettingsPropertyValueCollection();
                    SettingsProperty property = new SettingsProperty("StringPropertyWithProvider", typeof(string), this, false, DefaultStringPropertyValue, SettingsSerializeAs.String, new SettingsAttributeDictionary(), false, false);
                    result.Add(new SettingsPropertyValue(new SettingsProperty(property)));
                    return result;
                }

                public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
                {
                }

                public override void Initialize(string name, NameValueCollection config)
                {
                    base.Initialize(name ?? "CustomProvider", config ?? new NameValueCollection());
                }
            }

        }

        private class PersistedSimpleSettings : SimpleSettings
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Context_SimpleSettings_InNotNull(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings())
                : new SimpleSettings();

            Assert.NotNull(settings.Context);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Providers_SimpleSettings_Empty(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings())
                : new SimpleSettings();

            Assert.Equal(1, settings.Providers.Count);
            Assert.NotNull(settings.Providers[typeof(LocalFileSettingsProvider).Name]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetSetStringProperty_SimpleSettings_Ok(bool isSynchronized)
        {
            SimpleSettings settings = isSynchronized
                ? (SimpleSettings)SettingsBase.Synchronized(new SimpleSettings())
                : new SimpleSettings();

            Assert.Equal(default, settings.StringProperty);
            settings.StringProperty = "Foo";
            Assert.Equal("Foo", settings.StringProperty);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/28833")]
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

        [Fact]
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
#pragma warning disable CS0618 // Type or member is obsolete
        [SettingsSerializeAs(SettingsSerializeAs.Binary)]
#pragma warning restore CS0618 // Type or member is obsolete
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

        [Fact]
        public void SettingsProperty_SettingsWithAttributes_Ok()
        {
            SettingsWithAttributes settings = new SettingsWithAttributes();

            Assert.Equal(1, settings.Properties.Count);
            SettingsProperty property = settings.Properties["StringProperty"];
            Assert.Equal(typeof(TestProvider), property.Provider.GetType());
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal(SettingsSerializeAs.Binary, property.SerializeAs);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void SettingsChanging_Success()
        {
            SimpleSettings settings = new SimpleSettings();
            bool changingFired = false;
            int newValue = 1976;

            settings.SettingChanging += (object sender, SettingChangingEventArgs e)
                =>
                {
                    changingFired = true;
                    Assert.Equal(nameof(SimpleSettings.IntProperty), e.SettingName);
                    Assert.Equal(typeof(SimpleSettings).FullName, e.SettingClass);
                    Assert.Equal(newValue, e.NewValue);
                };

            settings.IntProperty = newValue;

            Assert.True(changingFired);
            Assert.Equal(newValue, settings.IntProperty);
        }

        [Fact]
        public void SettingsChanging_Canceled()
        {
            int oldValue = 1776;
            SimpleSettings settings = new SimpleSettings
            {
                IntProperty = oldValue
            };

            bool changingFired = false;
            int newValue = 1976;

            settings.SettingChanging += (object sender, SettingChangingEventArgs e)
                =>
            {
                changingFired = true;
                e.Cancel = true;
            };

            settings.IntProperty = newValue;

            Assert.True(changingFired);
            Assert.Equal(oldValue, settings.IntProperty);
        }

        [Fact]
        public void OnSettingsLoaded_QueryProperty()
        {
            SettingsWithProvider settings = new SettingsWithProvider();
            bool loadedFired = false;
            string newStringPropertyValue = nameof(SettingsWithProvider.StringProperty);
            settings.SettingsLoaded += (object s, SettingsLoadedEventArgs e)
                =>
            {
                loadedFired = true;
                Assert.Equal(SettingsWithProvider.CustomProvider.DefaultStringPropertyValue, settings.StringPropertyWithProvider);
                if (string.IsNullOrEmpty(settings.StringProperty))
                    settings.StringProperty = newStringPropertyValue;
            };

            Assert.Equal(newStringPropertyValue, settings.StringProperty);
            Assert.True(loadedFired);
        }
    }
}
