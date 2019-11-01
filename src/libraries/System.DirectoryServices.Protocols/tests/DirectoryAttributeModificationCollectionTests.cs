// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryAttributeModificationCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var collection = new DirectoryAttributeModificationCollection();
            Assert.Equal(0, collection.Count);
            Assert.Equal(0, collection.Capacity);
        }

        [Fact]
        public void Indexer_Set_GetReturnsExpected()
        {
            var attribute = new DirectoryAttributeModification { "value" };
            var collection = new DirectoryAttributeModificationCollection { new DirectoryAttributeModification() };
            collection[0] = attribute;
            Assert.Equal(attribute, collection[0]);
        }

        [Fact]
        public void Indexer_SetNull_ThrowsArgumentException()
        {
            var collection = new DirectoryAttributeModificationCollection();
            AssertExtensions.Throws<ArgumentException>(null, () => collection[0] = null);
        }

        [Fact]
        public void Add_ValidAttribute_AppendsToList()
        {
            var attribute1 = new DirectoryAttributeModification { "value1" };
            var attribute2 = new DirectoryAttributeModification { "value2" };
            var collection = new DirectoryAttributeModificationCollection { attribute1, attribute2 };
            Assert.Equal(2, collection.Count);
            Assert.Equal(new DirectoryAttributeModification[] { attribute1, attribute2 }, collection.Cast<DirectoryAttributeModification>());
        }

        [Fact]
        public void Add_NullAttribute_ThrowsArgumentException()
        {
            var collection = new DirectoryAttributeModificationCollection();
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Add(null));
        }

        [Fact]
        public void AddRange_ValidAttributes_AddsToCollection()
        {
            DirectoryAttributeModification[] attributes = new DirectoryAttributeModification[] { new DirectoryAttributeModification(), new DirectoryAttributeModification() };

            var collection = new DirectoryAttributeModificationCollection();
            collection.AddRange(attributes);

            Assert.Equal(attributes, collection.Cast<DirectoryAttributeModification>());
        }

        [Fact]
        public void AddRange_NullAttributes_ThrowsArgumentNullException()
        {
            var collection = new DirectoryAttributeModificationCollection();
            AssertExtensions.Throws<ArgumentNullException>("attributes", () => collection.AddRange((DirectoryAttributeModification[])null));
        }

        [Fact]
        public void AddRange_NullObjectInValues_ThrowsArgumentException()
        {
            DirectoryAttributeModification[] attributes = new DirectoryAttributeModification[] { new DirectoryAttributeModification(), null, new DirectoryAttributeModification() };
            var collection = new DirectoryAttributeModificationCollection();

            AssertExtensions.Throws<ArgumentException>(null, () => collection.AddRange(attributes));
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void AddRange_ValidAttributeCollection_AddsToCollection()
        {
            DirectoryAttributeModification[] attributes = new DirectoryAttributeModification[] { new DirectoryAttributeModification(), new DirectoryAttributeModification() };
            var attributeCollection = new DirectoryAttributeModificationCollection();
            attributeCollection.AddRange(attributes);

            var collection = new DirectoryAttributeModificationCollection();
            collection.AddRange(attributeCollection);

            Assert.Equal(attributes, collection.Cast<DirectoryAttributeModification>());
        }

        [Fact]
        public void AddRange_NullAttributeCollection_ThrowsArgumentNullException()
        {
            var collection = new DirectoryAttributeModificationCollection();
            AssertExtensions.Throws<ArgumentNullException>("attributeCollection", () => collection.AddRange((DirectoryAttributeModificationCollection)null));
        }

        [Fact]
        public void Contains_Valid_ReturnsExpected()
        {
            var attribute = new DirectoryAttributeModification { "value" };
            var collection = new DirectoryAttributeModificationCollection { attribute };
            Assert.True(collection.Contains(attribute));
            Assert.False(collection.Contains(new DirectoryAttributeModification()));
            Assert.False(collection.Contains(null));
        }

        [Fact]
        public void CopyTo_MultipleTypes_Success()
        {
            DirectoryAttributeModification[] array = new DirectoryAttributeModification[3];
            var attribute = new DirectoryAttributeModification { "value" };

            var collection = new DirectoryAttributeModificationCollection { attribute };
            collection.CopyTo(array, 1);
            Assert.Equal(new DirectoryAttributeModification[] { null, attribute, null }, array);
        }

        [Fact]
        public void Insert_ValidDirectoryAttribute_Success()
        {
            var attribute1 = new DirectoryAttributeModification { "value1" };
            var attribute2 = new DirectoryAttributeModification { "value2" };
            var collection = new DirectoryAttributeModificationCollection();
            collection.Insert(0, attribute1);
            collection.Insert(1, attribute2);

            Assert.Equal(new DirectoryAttributeModification[] { attribute1, attribute2 }, collection.Cast<DirectoryAttributeModification>());
        }

        [Fact]
        public void Insert_NullValue_ThrowsArgumentException()
        {
            var collection = new DirectoryAttributeModificationCollection();
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Insert(0, null));
        }

        [Fact]
        public void IndexOf_Valid_ReturnsExpected()
        {
            var attribute = new DirectoryAttributeModification { "value" };
            var collection = new DirectoryAttributeModificationCollection { attribute };
            Assert.Equal(0, collection.IndexOf(attribute));
            Assert.Equal(-1, collection.IndexOf(new DirectoryAttributeModification()));
            Assert.Equal(-1, collection.IndexOf(null));
        }

        [Fact]
        public void Remove_Valid_Success()
        {
            var attribute = new DirectoryAttributeModification { "value" };
            var collection = new DirectoryAttributeModificationCollection { attribute };
            collection.Remove(attribute);
            Assert.Empty(collection);
        }

        public static IEnumerable<object[]> Remove_InvalidValue_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new DirectoryAttributeModification(), null };
            yield return new object[] { 1, "value" };
        }

        [Theory]
        [MemberData(nameof(Remove_InvalidValue_TestData))]
        public void Remove_InvalidValue_ThrowsArgumentException(object value, string paramName)
        {
            IList collection = new DirectoryAttributeModificationCollection { new DirectoryAttributeModification() };
            AssertExtensions.Throws<ArgumentException>(paramName, () => collection.Remove(value));
        }
    }
}
