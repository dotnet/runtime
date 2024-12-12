// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class SettingsPropertyTests
    {
        [Fact]
        public void SettingsProperty_WithNoArguments()
        {
            var settingsProperty = new SettingsProperty("TestName");
            Assert.Equal("TestName", settingsProperty.Name);
            Assert.False(settingsProperty.IsReadOnly);
            Assert.Null(settingsProperty.DefaultValue);
            Assert.Null(settingsProperty.PropertyType);
            Assert.Equal(SettingsSerializeAs.String, settingsProperty.SerializeAs);
            Assert.Null(settingsProperty.Provider);
            Assert.NotNull(settingsProperty.Attributes);
            Assert.False(settingsProperty.ThrowOnErrorDeserializing);
            Assert.False(settingsProperty.ThrowOnErrorSerializing);
        }

        [Theory]
        [InlineData(SettingsSerializeAs.String)]
        [InlineData(SettingsSerializeAs.Xml)]
        [InlineData(SettingsSerializeAs.ProviderSpecific)]
        public void SettingsProperty_WithArguments(SettingsSerializeAs serializeAs)
        {
            var settingsProperty = new SettingsProperty(
                "TestName",
                typeof(string),
                provider: null,
                isReadOnly: true,
                "TestDefaultValue",
                serializeAs,
                new SettingsAttributeDictionary(),
                throwOnErrorDeserializing: true,
                throwOnErrorSerializing: false);
            Assert.Equal("TestName", settingsProperty.Name);
            Assert.True(settingsProperty.IsReadOnly);
            Assert.Equal("TestDefaultValue", settingsProperty.DefaultValue);
            Assert.Equal(typeof(string), settingsProperty.PropertyType);
            Assert.Equal(serializeAs, settingsProperty.SerializeAs);
            Assert.Null(settingsProperty.Provider);
            Assert.NotNull(settingsProperty.Attributes);
            Assert.True(settingsProperty.ThrowOnErrorDeserializing);
            Assert.False(settingsProperty.ThrowOnErrorSerializing);
        }
    }
}
