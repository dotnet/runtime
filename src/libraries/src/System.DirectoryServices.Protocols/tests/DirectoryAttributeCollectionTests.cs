// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryAttributeCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var collection = new DirectoryAttributeCollection();
            Assert.Equal(0, collection.Count);
            Assert.Equal(0, collection.Capacity);
        }

        [Fact]
        public void Indexer_Set_GetReturnsExpected()
        {
            var attribute = new DirectoryAttribute { "value" };
            var collection = new DirectoryAttributeCollection { new DirectoryAttribute() };
            collection[0] = attribute;
            Assert.Equal(attribute, collection[0]);
        }

        [Fact]
        public void Indexer_SetNull_ThrowsArgumentException()
        {
            var collection = new DirectoryAttributeCollection();
            AssertExtensions.Throws<ArgumentException>(null, () => collection[0] = null);
        }

        [Fact]
        public void Add_ValidAttribute_AppendsToList()
        {
            var attribute1 = new DirectoryAttribute("name1", "value");
            var attribute2 = new DirectoryAttribute("name2", "value");
            var collection = new DirectoryAttributeCollection { attribute1, attribute2 };
            Assert.Equal(2, collection.Count);
            Assert.Equal(new DirectoryAttribute[] { attribute1, attribute2 }, collection.Cast<DirectoryAttribute>());
        }

        [Fact]
        public void Add_NullAttribute_ThrowsArgumentException()
        {
            var collection = new DirectoryAttributeCollection();
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Add(null));
        }

        [Fact]
        public void AddRange_ValidAttributes_AddsToCollection()
        {
            DirectoryAttribute[] attributes = new DirectoryAttribute[] { new DirectoryAttribute(), new DirectoryAttribute() };

            var collection = new DirectoryAttributeCollection();
            collection.AddRange(attributes);

            Assert.Equal(attributes, collection.Cast<DirectoryAttribute>());
        }

        [Fact]
        public void AddRange_NullAttributes_ThrowsArgumentNullException()
        {
            var collection = new DirectoryAttributeCollection();
            AssertExtensions.Throws<ArgumentNullException>("attributes", () => collection.AddRange((DirectoryAttribute[])null));
        }

        [Fact]
        public void AddRange_NullObjectInValues_ThrowsArgumentException()
        {
            DirectoryAttribute[] attributes = new DirectoryAttribute[] { new DirectoryAttribute(), null, new DirectoryAttribute() };
            var collection = new DirectoryAttributeCollection();

            AssertExtensions.Throws<ArgumentException>(null, () => collection.AddRange(attributes));
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void AddRange_ValidAttributeCollection_AddsToCollection()
        {
            DirectoryAttribute[] attributes = new DirectoryAttribute[] { new DirectoryAttribute(), new DirectoryAttribute() };
            var attributeCollection = new DirectoryAttributeCollection();
            attributeCollection.AddRange(attributes);

            var collection = new DirectoryAttributeCollection();
            collection.AddRange(attributeCollection);

            Assert.Equal(attributes, collection.Cast<DirectoryAttribute>());
        }

        [Fact]
        public void AddRange_NullAttributeCollection_ThrowsArgumentNullException()
        {
            var collection = new DirectoryAttributeCollection();
            AssertExtensions.Throws<ArgumentNullException>("attributeCollection", () => collection.AddRange((DirectoryAttributeCollection)null));
        }

        [Fact]
        public void Contains_Valid_ReturnsExpected()
        {
            var attribute = new DirectoryAttribute { "value" };
            var collection = new DirectoryAttributeCollection { attribute };
            Assert.True(collection.Contains(attribute));
            Assert.False(collection.Contains(new DirectoryAttribute()));
            Assert.False(collection.Contains(null));
        }

        [Fact]
        public void CopyTo_MultipleTypes_Success()
        {
            DirectoryAttribute[] array = new DirectoryAttribute[3];
            var attribute = new DirectoryAttribute { "value" };

            var collection = new DirectoryAttributeCollection { attribute };
            collection.CopyTo(array, 1);
            Assert.Equal(new DirectoryAttribute[] { null, attribute, null }, array);
        }

        [Fact]
        public void Insert_ValidDirectoryAttribute_Success()
        {
            var attribute1 = new DirectoryAttribute { "value1" };
            var attribute2 = new DirectoryAttribute { "value2" };
            var collection = new DirectoryAttributeCollection();
            collection.Insert(0, attribute1);
            collection.Insert(1, attribute2);

            Assert.Equal(new DirectoryAttribute[] { attribute1, attribute2 }, collection.Cast<DirectoryAttribute>());
        }

        [Fact]
        public void Insert_NullValue_ThrowsArgumentException()
        {
            var collection = new DirectoryAttributeCollection();
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Insert(0, null));
        }

        [Fact]
        public void IndexOf_Valid_ReturnsExpected()
        {
            var attribute = new DirectoryAttribute { "value" };
            var collection = new DirectoryAttributeCollection { attribute };
            Assert.Equal(0, collection.IndexOf(attribute));
            Assert.Equal(-1, collection.IndexOf(new DirectoryAttribute()));
            Assert.Equal(-1, collection.IndexOf(null));
        }

        [Fact]
        public void Remove_Valid_Success()
        {
            var attribute = new DirectoryAttribute { "value" };
            var collection = new DirectoryAttributeCollection { attribute };
            collection.Remove(attribute);
            Assert.Empty(collection);
        }

        public static IEnumerable<object[]> Remove_InvalidValue_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new DirectoryAttribute(), null };
            yield return new object[] { 1, "value" };
        }

        [Theory]
        [MemberData(nameof(Remove_InvalidValue_TestData))]
        public void Remove_InvalidValue_ThrowsArgumentException(object value, string paramName)
        {
            IList collection = new DirectoryAttributeCollection { new DirectoryAttribute() };
            AssertExtensions.Throws<ArgumentException>(paramName, () => collection.Remove(value));
        }
    }
}
