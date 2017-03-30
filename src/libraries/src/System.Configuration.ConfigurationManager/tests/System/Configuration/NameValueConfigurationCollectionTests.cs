// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class NameValueConfigurationCollectionTests
    {
        [Fact]
        public void CollectionType_EqualsAddRemoveClearMap()
        {
            Assert.Equal(ConfigurationElementCollectionType.AddRemoveClearMap, new NameValueConfigurationCollection().CollectionType);
        }

        [Fact]
        public void AllKeys_EmptyCollection()
        {
            Assert.Empty(new NameValueConfigurationCollection().AllKeys);
        }

        [Fact]
        public void Add_NullKeyValue_Throws()
        {
            var element = new NameValueConfigurationElement(null, null);
            Assert.Throws<ConfigurationErrorsException>(() => new NameValueConfigurationCollection().Add(element));
        }

        [Fact]
        public void Add_NullValue_Ok()
        {
            var collection = new NameValueConfigurationCollection();
            var element = new NameValueConfigurationElement("foo", null);
            collection.Add(element);
            Assert.Null(collection["foo"].Value);
        }

        [Fact]
        public void SetElement_NoElementWithGivenName_Ok()
        {
            var collection = new NameValueConfigurationCollection();
            collection["foo"] = new NameValueConfigurationElement("foo", "bar");
            Assert.Equal("bar", collection["foo"].Value);
        }

        [Fact]
        public void SetElement_ElementWithGivenNameExists_Ok()
        {
            var collection = new NameValueConfigurationCollection();
            collection["foo"] = new NameValueConfigurationElement("foo", "bar");
            Assert.Equal("bar", collection["foo"].Value);
            collection["foo"] = new NameValueConfigurationElement("foo", "barModified");
            Assert.Equal("barModified", collection["foo"].Value);
        }

        [Fact]
        public void SetElement_IndexAndElementNameMismatch_ElementNameIsUsed()
        {
            var collection = new NameValueConfigurationCollection();
            collection["foo"] = new NameValueConfigurationElement("fooMismatched", "bar");
            Assert.Equal("bar", collection["fooMismatched"].Value);
            Assert.Null(collection["foo"]);
        }

        [Fact]
        public void Add_ElementWithExistingName_Throws()
        {
            var collection = new NameValueConfigurationCollection();
            var element1 = new NameValueConfigurationElement("foo", "foo");
            var element2 = new NameValueConfigurationElement("foo", "bar");
            collection.Add(element1);
            Assert.Throws<ConfigurationErrorsException>(() => collection.Add(element2));
        }

        [Fact]
        public void Value_AssignNewValue_Ok()
        {
            var collection = new NameValueConfigurationCollection();
            var element = new NameValueConfigurationElement("foo", "foo");
            collection.Add(element);
            collection["foo"].Value = "bar";
            Assert.Equal("bar", collection["foo"].Value);
        }

        [Fact]
        public void Remove_ByName_ElementIsNull()
        {
            var collection = new NameValueConfigurationCollection();
            var element = new NameValueConfigurationElement("foo", "foo");
            collection.Add(element);
            Assert.Equal("foo", collection["foo"].Value);
            collection.Remove("foo");
            Assert.Null(collection["foo"]);
        }

        [Fact]
        public void Remove_ByElement_ElementIsNull()
        {
            var collection = new NameValueConfigurationCollection();
            var element = new NameValueConfigurationElement("foo", "foo");
            collection.Add(element);
            Assert.Equal("foo", collection["foo"].Value);
            collection.Remove(element);
            Assert.Null(collection["foo"]);
        }

        [Fact]
        public void Clear_NotEmptyCollection_EmptyCollection()
        {
            var collection = new NameValueConfigurationCollection();
            var element = new NameValueConfigurationElement("foo", "foo");
            collection.Add(element);
            Assert.NotEmpty(collection);
            collection.Clear();
            Assert.Empty(collection);
        }
    }
}