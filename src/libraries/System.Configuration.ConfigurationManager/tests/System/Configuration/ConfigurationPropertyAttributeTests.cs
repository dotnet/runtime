// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationPropertyAttributeTests
    {
        [Fact]
        public void DefaultValueIsNullObject()
        {
            // It isn't publicly exposed anywhere else, first check that it is the same object instance here
            ConfigurationPropertyAttribute one = new ConfigurationPropertyAttribute("one");
            ConfigurationPropertyAttribute two = new ConfigurationPropertyAttribute("two");

            Assert.IsType<object>(one.DefaultValue);
            Assert.Same(one.DefaultValue, two.DefaultValue);
        }

        [Fact]
        public void DefaultOptionsIsNone()
        {
            Assert.Equal(ConfigurationPropertyOptions.None, new ConfigurationPropertyAttribute("foo").Options);
        }

        [Fact]
        public void IsDefaultCollection()
        {
            ConfigurationPropertyAttribute attribute = new ConfigurationPropertyAttribute("foo");
            Assert.False(attribute.IsDefaultCollection);

            attribute.Options = ConfigurationPropertyOptions.IsDefaultCollection;
            Assert.True(attribute.IsDefaultCollection);
            attribute.IsDefaultCollection = false;
            Assert.False(attribute.IsDefaultCollection);

            Assert.Equal(ConfigurationPropertyOptions.None, attribute.Options);
        }

        [Fact]
        public void IsRequired()
        {
            ConfigurationPropertyAttribute attribute = new ConfigurationPropertyAttribute("foo");
            Assert.False(attribute.IsDefaultCollection);

            attribute.Options = ConfigurationPropertyOptions.IsRequired;
            Assert.True(attribute.IsRequired);
            attribute.IsRequired = false;
            Assert.False(attribute.IsRequired);

            Assert.Equal(ConfigurationPropertyOptions.None, attribute.Options);
        }

        [Fact]
        public void IsKey()
        {
            ConfigurationPropertyAttribute attribute = new ConfigurationPropertyAttribute("foo");
            Assert.False(attribute.IsDefaultCollection);

            attribute.Options = ConfigurationPropertyOptions.IsKey;
            Assert.True(attribute.IsKey);
            attribute.IsKey = false;
            Assert.False(attribute.IsKey);

            Assert.Equal(ConfigurationPropertyOptions.None, attribute.Options);
        }

        public static string AttributeConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
  <system.diagnostics>
    <trace>
      <listeners>
        <add name=""delimited"" type=""System.Diagnostics.DelimitedListTraceListener"" delimiter="":"" />
      </listeners>
    </trace>
  </system.diagnostics>
</configuration>";

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "ListenerElement uses HashTable not StringDictionary")]
        public void CustomAttributeIsPresent()
        {
            using (var temp = new TempConfig(AttributeConfiguration))
            {
                ConfigurationSection section = ConfigurationManager.OpenExeConfiguration(temp.ExePath).GetSection("system.diagnostics");
                ConfigurationElementCollection listeners = (ConfigurationElementCollection)GetPropertyValue(GetPropertyValue(section, "Trace"), "Listeners");
                Assert.Equal(2, listeners.Count);

                ConfigurationElement[] items = new ConfigurationElement[2];
                listeners.CopyTo(items, 0);
                Assert.Equal("delimited", GetPropertyValue(items[1], "Name"));
                StringDictionary attributes = (StringDictionary)GetPropertyValue(items[1], "Attributes");
                Assert.Equal(1, attributes.Count);

                // Verify the attribute is present.
                Assert.Equal(":", attributes["delimiter"]);

                static object GetPropertyValue(object obj, string propertyName) => obj.GetType().
                    GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).
                    GetValue(obj);
            }
        }
    }
}
