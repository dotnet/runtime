// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class KeyValueConfigurationCollectionTests
    {
        [Fact]
        public void MsdnRootSample()
        {
            // https://msdn.microsoft.com/en-us/library/system.configuration.keyvalueconfigurationcollection(v=vs.110).aspx
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                AppSettingsSection configSection = (AppSettingsSection)config.GetSection("appSettings");
                Assert.Equal("appSettings", configSection.SectionInformation.Name);
                KeyValueConfigurationElement myAdminKeyVal = new KeyValueConfigurationElement("myAdminTool", "admin.aspx");
                KeyValueConfigurationCollection configSettings = config.AppSettings.Settings;
                Assert.Equal(0, configSettings.AllKeys.Length);
                config.AppSettings.Settings.Add(myAdminKeyVal);
                Assert.False(configSection.SectionInformation.IsLocked);
                config.Save();

                KeyValueConfigurationCollection settings =
                  config.AppSettings.Settings;
                foreach (KeyValueConfigurationElement keyValueElement in settings)
                {
                    Assert.Equal("myAdminTool", keyValueElement.Key);
                    Assert.Equal("admin.aspx", keyValueElement.Value);
                }
            }
        }

        [Fact]
        public void CollectionTypeIsAddRemoveMap()
        {
            Assert.Equal(ConfigurationElementCollectionType.AddRemoveClearMap, new KeyValueConfigurationCollection().CollectionType);
        }

        [Fact]
        public void ThrowOnDuplicateIsFalse()
        {
            Assert.False(new TestKeyValueCollection().TestThrowOnDuplicate);
        }

        [Fact]
        public void CreateNewElement()
        {
            var element = new TestKeyValueCollection().TestCreateNewElement();
            Assert.IsType<KeyValueConfigurationElement>(element);
            Assert.Equal("", ((KeyValueConfigurationElement)element).Key);
            Assert.Equal("", ((KeyValueConfigurationElement)element).Value);
        }

        [Fact]
        public void EmptyAllKeys()
        {
            Assert.Equal(0, new KeyValueConfigurationCollection().AllKeys.Length);
        }

        [Fact]
        public void AddNullKeyValueThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new KeyValueConfigurationCollection().Add(null, null));
        }

        [Fact]
        public void AddNullKeyValueElementThrows()
        {
            var element = new KeyValueConfigurationElement(null, null);
            Assert.Throws<ConfigurationErrorsException>(() => new KeyValueConfigurationCollection().Add(element));
        }

        [Fact]
        public void AddNullKeyThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new KeyValueConfigurationCollection().Add(null, "foo"));
        }

        [Fact]
        public void AddNullValue()
        {
            var collection = new KeyValueConfigurationCollection();
            collection.Add("foo", null);
            Assert.Null(collection["foo"].Value);
        }

        [Fact]
        public void AddOverValue()
        {
            var collection = new KeyValueConfigurationCollection();
            collection.Add("foo", "foo");
            Assert.Equal("foo", collection["foo"].Value);
            collection.Add("foo", "bar");
            Assert.Equal("foo,bar", collection["foo"].Value);
        }

        [Fact]
        public void ModifyElementValue()
        {
            var collection = new KeyValueConfigurationCollection();
            collection.Add("foo", "foo");
            collection["foo"].Value = "bar";
            Assert.Equal("bar", collection["foo"].Value);
        }

        [Fact]
        public void RemoveKey()
        {
            var collection = new KeyValueConfigurationCollection();
            collection.Add("foo", "foo");
            Assert.Equal("foo", collection["foo"].Value);
            collection.Remove("foo");
            Assert.Null(collection["foo"]);
        }

        [Fact]
        public void Clear()
        {
            var collection = new KeyValueConfigurationCollection();
            collection.Add("foo", "foo");
            Assert.Equal("foo", collection["foo"].Value);
            collection.Clear();
            Assert.Null(collection["foo"]);
        }

        private class TestKeyValueCollection : KeyValueConfigurationCollection
        {
            public bool TestThrowOnDuplicate => ThrowOnDuplicate;
            public ConfigurationElement TestCreateNewElement() => CreateNewElement();
        }
    }
}
