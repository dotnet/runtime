// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Configuration
{
    public class SettingElementCollectionTests
    {
        [Fact]
        public void DefaultCollectionTypeBasicMap()
        {
            var collection = new SettingElementCollection();
            Assert.Equal(ConfigurationElementCollectionType.BasicMap, collection.CollectionType);
        }

        [Fact]
        public void SuccessfulRetrieveSettingElement()
        {
            var settingElement = new SettingElement("TestElementName", SettingsSerializeAs.String);
            var collection = new SettingElementCollection();
            collection.Add(settingElement);
            var retrievedElement = collection.Get("TestElementName");
            Assert.Same(settingElement, retrievedElement);
        }

        [Fact]
        public void SuccessfullyRemoveElement()
        {
            var settingElement = new SettingElement("TestElementName", SettingsSerializeAs.String);
            var collection = new SettingElementCollection();
            collection.Add(settingElement);
            collection.Remove(settingElement);
            var retrievedElement = collection.Get("TestElementName");
            Assert.Equal(null, retrievedElement);
        }

        [Fact]
        public void ClearNoElementsLeft()
        {
            var settingElement = new SettingElement("TestElementName", SettingsSerializeAs.String);
            var collection = new SettingElementCollection();
            collection.Add(settingElement);
            collection.Clear();
            var retrievedElement = collection.Get("TestElementName");
            Assert.Equal(null, retrievedElement);
        }
    }
}
