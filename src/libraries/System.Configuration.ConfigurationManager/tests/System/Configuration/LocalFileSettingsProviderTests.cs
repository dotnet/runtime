// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;
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
    }
}
