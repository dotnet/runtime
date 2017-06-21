// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public class PropertyCollectionTests
    {
        [Fact]
        public void Indexer_NullPropertyName_ThrowsArgumentNullException()
        {
            using (var entry = new DirectoryEntry())
            {
                AssertExtensions.Throws<ArgumentNullException>("propertyName", () => entry.Properties[null]);
                AssertExtensions.Throws<ArgumentNullException>("propertyName", () => ((IDictionary)entry.Properties)[null]);
            }
        }

        [Fact]
        public void Indexer_ObjectNotString_ThrowsInvalidCastException()
        {
            using (var entry = new DirectoryEntry())
            {
                IDictionary properties = entry.Properties;
                Assert.Throws<InvalidCastException>(() => properties[1]);
            }
        }

        [Fact]
        public void IDictionary_ModifyDictionary_ThrowsNotSupportedException()
        {
            using (var entry = new DirectoryEntry())
            {
                IDictionary properties = entry.Properties;
                Assert.Throws<NotSupportedException>(() => properties["name"] = 2);
                Assert.Throws<NotSupportedException>(() => properties.Add("key", "value"));
                Assert.Throws<NotSupportedException>(() => properties.Remove("key"));
                Assert.Throws<NotSupportedException>(() => properties.Clear());
            }
        }

        [Fact]
        public void IDictionary_GetProperties_ReturnsExpected()
        {
            using (var entry = new DirectoryEntry())
            {
                IDictionary properties = entry.Properties;
                Assert.True(properties.IsFixedSize);
                Assert.True(properties.IsReadOnly);
                Assert.False(properties.IsSynchronized);
                Assert.Same(properties, properties.SyncRoot);
            }
        }

        [Fact]
        public void CopyTo_NullArray_ThrowsArgumentNullException()
        {
            using (var entry = new DirectoryEntry())
            {
                PropertyCollection properties = entry.Properties;
                AssertExtensions.Throws<ArgumentNullException>("array", () => properties.CopyTo(null, 0));
                AssertExtensions.Throws<ArgumentNullException>("array", () => ((ICollection)properties).CopyTo(null, 0));
            }
        }

        [Fact]
        public void CopyTo_InvalidArrayRank_ThrowsArgumentException()
        {
            using (var entry = new DirectoryEntry())
            {
                PropertyCollection properties = entry.Properties;
                AssertExtensions.Throws<ArgumentException>("array", () => ((ICollection)properties).CopyTo(new int[1, 1], 0));
            }
        }

        [Fact]
        public void CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            using (var entry = new DirectoryEntry())
            {
                PropertyCollection properties = entry.Properties;
                AssertExtensions.Throws<ArgumentOutOfRangeException>("Number was less than the array's lower bound in the first dimension.", () => properties.CopyTo(new PropertyValueCollection[0], -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("Number was less than the array's lower bound in the first dimension.", () => ((ICollection)properties).CopyTo(new PropertyValueCollection[0], -1));
            }
        }

        [Fact]
        public void Values_Get_ReturnsUniqueObject()
        {
            using (var entry = new DirectoryEntry())
            {
                PropertyCollection properties = entry.Properties;
                Assert.NotSame(properties.Values, properties.Values);
            }
        }

        [Fact]
        public void IDictionaryValues_Get_ReturnsUniqueObject()
        {
            using (var entry = new DirectoryEntry())
            {
                IDictionary properties = entry.Properties;
                Assert.NotSame(properties.Values, properties.Values);
            }
        }
    }
}
