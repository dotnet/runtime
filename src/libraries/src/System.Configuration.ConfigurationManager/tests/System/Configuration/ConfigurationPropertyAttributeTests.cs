// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
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
    }
}
