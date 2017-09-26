// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class LocalFileSettingsProviderTests
    {
        private readonly SettingsContext _testContext = new SettingsContext
        {
            ["GroupName"] = "GroupNameFoo",
            ["SettingsKey"] = "SettingsKeyFoo"
        };

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void GetPropertyValues_NotStoredProperty_ValueEqualsNull()
        {
            var property = new SettingsProperty("PropertyName");
            property.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
            var properties = new SettingsPropertyCollection();
            properties.Add(property);
            var localFileSettingsProvider = new LocalFileSettingsProvider();

            SettingsPropertyValueCollection propertyValues = localFileSettingsProvider.GetPropertyValues(_testContext, properties);

            Assert.Equal(1, propertyValues.Count);
            Assert.Equal(null, propertyValues["PropertyName"].PropertyValue);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
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
    }
}