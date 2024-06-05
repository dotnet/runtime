// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Collections.Tests
{
    public class CollectionExtensionsTests
    {
        [Fact]
        public void GetValueOrDefault_KeyExistsInIReadOnlyDictionary_ReturnsValue()
        {
            IReadOnlyDictionary<string, string> dictionary = new SortedDictionary<string, string>() { { "key", "value" } };
            Assert.Equal("value", dictionary.GetValueOrDefault("key"));
            Assert.Equal("value", dictionary.GetValueOrDefault("key", null));
        }

        [Fact]
        public void GetValueOrDefault_KeyDoesntExistInIReadOnlyDictionary_ReturnsDefaultValue()
        {
            IReadOnlyDictionary<string, string> dictionary = new SortedDictionary<string, string>() { { "key", "value" } };
            Assert.Null(dictionary.GetValueOrDefault("anotherKey"));
            Assert.Equal("anotherValue", dictionary.GetValueOrDefault("anotherKey", "anotherValue"));
        }

        [Fact]
        public void GetValueOrDefault_NullKeyIReadOnlyDictionary_ThrowsArgumentNullException()
        {
            IReadOnlyDictionary<string, string> dictionary = new SortedDictionary<string, string>() { { "key", "value" } };
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictionary.GetValueOrDefault(null));
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictionary.GetValueOrDefault(null, "anotherValue"));
        }

        [Fact]
        public void GetValueOrDefault_NullIReadOnlyDictionary_ThrowsArgumentNullException()
        {
            IReadOnlyDictionary<string, string> dictionary = null;
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => dictionary.GetValueOrDefault("key"));
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => dictionary.GetValueOrDefault("key", "value"));
        }

        [Fact]
        public void TryAdd_NullIDictionary_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dictionary = null;
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => dictionary.TryAdd("key", "value"));
        }

        [Fact]
        public void TryAdd_NullKeyIDictionary_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>();
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictionary.TryAdd(null, "value"));
        }

        [Fact]
        public void TryAdd_KeyDoesntExistInIDictionary_ReturnsTrue()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>();
            Assert.True(dictionary.TryAdd("key", "value"));
            Assert.Equal("value", dictionary["key"]);
        }

        [Fact]
        public void TryAdd_KeyExistsInIDictionary_ReturnsFalse()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>() { ["key"] = "value" };
            Assert.False(dictionary.TryAdd("key", "value2"));
            Assert.Equal("value", dictionary["key"]);
        }

        [Fact]
        public void Remove_NullIDictionary_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dictionary = null;
            string value = null;
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => dictionary.Remove("key", out value));
            Assert.Null(value);
        }

        [Fact]
        public void Remove_NullKeyIDictionary_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>();
            string value = null;
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictionary.Remove(null, out value));
            Assert.Null(value);
        }

        [Fact]
        public void Remove_KeyExistsInIDictionary_ReturnsTrue()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>() { ["key"] = "value" };
            Assert.True(dictionary.Remove("key", out var value));
            Assert.Equal("value", value);
            Assert.Throws<KeyNotFoundException>(() => dictionary["key"]);
        }

        [Fact]
        public void Remove_KeyDoesntExistInIDictionary_ReturnsFalse()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>();
            Assert.False(dictionary.Remove("key", out var value));
            Assert.Equal(default(string), value);
        }

        [Fact]
        public void AsReadOnly_TurnsIListIntoReadOnlyCollection()
        {
            IList<string> list = new List<string> { "A", "B" };
            ReadOnlyCollection<string> readOnlyCollection = list.AsReadOnly();
            Assert.NotNull(readOnlyCollection);
            CollectionAsserts.Equal(list, readOnlyCollection);
        }

        [Fact]
        public void AsReadOnly_TurnsIDictionaryIntoReadOnlyDictionary()
        {
            IDictionary<string, string> dictionary = new Dictionary<string, string> { ["key1"] = "value1", ["key2"] = "value2" };
            ReadOnlyDictionary<string, string> readOnlyDictionary = dictionary.AsReadOnly();
            Assert.NotNull(readOnlyDictionary);
            Assert.Equal(dictionary["key1"], readOnlyDictionary["key1"]);
            Assert.Equal(dictionary["key2"], readOnlyDictionary["key2"]);
            Assert.Equal(dictionary.Count, readOnlyDictionary.Count);
        }

        [Fact]
        public void AsReadOnly_NullIList_ThrowsArgumentNullException()
        {
            IList<string> list = null;
            Assert.Throws<ArgumentNullException>("list", () => list.AsReadOnly());
        }

        [Fact]
        public void AsReadOnly_NullIDictionary_ThrowsArgumentNullException()
        {
            IDictionary<string, string> dictionary = null;
            Assert.Throws<ArgumentNullException>("dictionary", () => dictionary.AsReadOnly());
        }
    }
}
