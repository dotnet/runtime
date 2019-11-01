// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class KeyedCollectionTests
    {
        public static IEnumerable<object[]> TryGetValue_TestData()
        {
            yield return new object[] { null, "first_key", true, "first" };
            yield return new object[] { null, "FIRST_KEY", false, null };
            yield return new object[] { null, "NoSuchKey", false, null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "first_key", true, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "FIRST_KEY", true, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "NoSuchKey", false, null };
        }

        [Theory]
        [MemberData(nameof(TryGetValue_TestData))]
        public void TryGetValue_Invoke_ReturnsExpected(IEqualityComparer<string> comparer, string key, bool expected, string expectedItem)
        {
            var collection = new StringKeyedCollection<string>(comparer, 3);
            collection.GetKeyForItemHandler = i => i + "_key";

            // Without dictionary.
            collection.InsertItem(0, "first");
            Assert.Equal(expected, collection.TryGetValue(key, out string item));
            Assert.Equal(expectedItem, item);

            // With dictionary.
            collection.InsertItem(0, "second");
            collection.InsertItem(0, "third");
            collection.InsertItem(0, "fourth");
            Assert.Equal(expected, collection.TryGetValue(key, out item));
            Assert.Equal(expectedItem, item);
        }

        [Theory]
        [InlineData(3, true, 2)]
        [InlineData(4, false, 0)]
        public void TryGetValue_NullKeyForItemResult_Success(int dictionaryCreationThreshold, bool expected, int expectedItem)
        {
            var collection = new StringKeyedCollection<int>(null, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = i => i.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.Add(4);

            // Don't get even numbers.
            collection.GetKeyForItemHandler = i => i % 2 == 0 ? null : i.ToString();

            // Get null key.
            Assert.Equal(expected, collection.TryGetValue("2", out int item));
            Assert.Equal(expectedItem, item);

            // Get non null key.
            Assert.True(collection.TryGetValue("1", out item));
            Assert.Equal(1, item);
        }

        [Theory]
        [InlineData(3, false, 0)]
        [InlineData(4, true, 3)]
        public void TryGetValue_DifferentKeyForItemResult_Success(int dictionaryCreationThreshold, bool expected, int expectedItem)
        {
            var collection = new StringKeyedCollection<int>(null, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = i => i.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.Add(4);

            collection.GetKeyForItemHandler = i => (i * 2).ToString();

            Assert.Equal(expected, collection.TryGetValue("6", out int item));
            Assert.Equal(expectedItem, item);
        }

        [Fact]
        public void TryGetValue_NullKey_ThrowsArgumentNullException()
        {
            var collection = new StringKeyedCollection<string>();
            string item = "item";
            AssertExtensions.Throws<ArgumentNullException>("key", () => collection.TryGetValue(null, out item));
            Assert.Equal("item", item);
        }
    }
}
