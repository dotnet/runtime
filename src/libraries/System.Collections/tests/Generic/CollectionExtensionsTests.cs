// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        [Fact]
        public void GetAlternateLookup_ThrowsWhenNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => CollectionExtensions.GetAlternateLookup<int, int, long>((Dictionary<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => CollectionExtensions.TryGetAlternateLookup<int, int, long>((Dictionary<int, int>)null, out _));

            AssertExtensions.Throws<ArgumentNullException>("set", () => CollectionExtensions.GetAlternateLookup<int, long>((HashSet<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("set", () => CollectionExtensions.TryGetAlternateLookup<int, long>((HashSet<int>)null, out _));
        }

        [Fact]
        public void GetAlternateLookup_FailsWhenIncompatible()
        {
            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            var hashSet = new HashSet<string>(StringComparer.Ordinal);

            dictionary.GetAlternateLookup<string, string, ReadOnlySpan<char>>();
            Assert.True(dictionary.TryGetAlternateLookup<string, string, ReadOnlySpan<char>>(out _));

            hashSet.GetAlternateLookup<string, ReadOnlySpan<char>>();
            Assert.True(hashSet.TryGetAlternateLookup<string, ReadOnlySpan<char>>(out _));

            Assert.Throws<InvalidOperationException>(() => dictionary.GetAlternateLookup<string, string, ReadOnlySpan<byte>>());
            Assert.Throws<InvalidOperationException>(() => dictionary.GetAlternateLookup<string, string, string>());
            Assert.Throws<InvalidOperationException>(() => dictionary.GetAlternateLookup<string, string, int>());

            Assert.False(dictionary.TryGetAlternateLookup<string, string, ReadOnlySpan<byte>>(out _));
            Assert.False(dictionary.TryGetAlternateLookup<string, string, string>(out _));
            Assert.False(dictionary.TryGetAlternateLookup<string, string, int>(out _));

            Assert.Throws<InvalidOperationException>(() => hashSet.GetAlternateLookup<string, ReadOnlySpan<byte>>());
            Assert.Throws<InvalidOperationException>(() => hashSet.GetAlternateLookup<string, string>());
            Assert.Throws<InvalidOperationException>(() => hashSet.GetAlternateLookup<string, int>());

            Assert.False(hashSet.TryGetAlternateLookup<string, ReadOnlySpan<byte>>(out _));
            Assert.False(hashSet.TryGetAlternateLookup<string, string>(out _));
            Assert.False(hashSet.TryGetAlternateLookup<string, int>(out _));
        }

        public static IEnumerable<object[]> Dictionary_GetAlternateLookup_OperationsMatchUnderlyingDictionary_MemberData()
        {
            yield return new object[] { EqualityComparer<string>.Default };
            yield return new object[] { StringComparer.Ordinal };
            yield return new object[] { StringComparer.OrdinalIgnoreCase };
            yield return new object[] { StringComparer.InvariantCulture };
            yield return new object[] { StringComparer.InvariantCultureIgnoreCase };
            yield return new object[] { StringComparer.CurrentCulture };
            yield return new object[] { StringComparer.CurrentCultureIgnoreCase };
        }

        [Theory]
        [MemberData(nameof(Dictionary_GetAlternateLookup_OperationsMatchUnderlyingDictionary_MemberData))]
        public void Dictionary_GetAlternateLookup_OperationsMatchUnderlyingDictionary(IEqualityComparer<string> comparer)
        {
            // Test with a variety of comparers to ensure that the alternate lookup is consistent with the underlying dictionary
            Dictionary<string, int> dictionary = new(comparer);
            Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup = dictionary.GetAlternateLookup<string, int, ReadOnlySpan<char>>();
            Assert.Same(dictionary, lookup.Dictionary);
            Assert.Same(lookup.Dictionary, lookup.Dictionary);

            string actualKey;
            int value;

            // Add to the dictionary and validate that the lookup reflects the changes
            dictionary["123"] = 123;
            Assert.True(lookup.ContainsKey("123".AsSpan()));
            Assert.True(lookup.TryGetValue("123".AsSpan(), out value));
            Assert.Equal(123, value);
            Assert.Equal(123, lookup["123".AsSpan()]);
            Assert.False(lookup.TryAdd("123".AsSpan(), 321));
            Assert.True(lookup.Remove("123".AsSpan()));
            Assert.False(dictionary.ContainsKey("123"));
            Assert.Throws<KeyNotFoundException>(() => lookup["123".AsSpan()]);

            // Add via the lookup and validate that the dictionary reflects the changes
            Assert.True(lookup.TryAdd("123".AsSpan(), 123));
            Assert.True(dictionary.ContainsKey("123"));
            lookup.TryGetValue("123".AsSpan(), out value);
            Assert.Equal(123, value);
            Assert.False(lookup.Remove("321".AsSpan(), out actualKey, out value));
            Assert.Null(actualKey);
            Assert.Equal(0, value);
            Assert.True(lookup.Remove("123".AsSpan(), out actualKey, out value));
            Assert.Equal("123", actualKey);
            Assert.Equal(123, value);

            // Ensure that case-sensitivity of the comparer is respected
            lookup["a".AsSpan()] = 42;
            if (dictionary.Comparer.Equals(EqualityComparer<string>.Default) ||
                dictionary.Comparer.Equals(StringComparer.Ordinal) ||
                dictionary.Comparer.Equals(StringComparer.InvariantCulture) ||
                dictionary.Comparer.Equals(StringComparer.CurrentCulture))
            {
                Assert.True(lookup.TryGetValue("a".AsSpan(), out actualKey, out value));
                Assert.Equal("a", actualKey);
                Assert.Equal(42, value);
                Assert.True(lookup.TryAdd("A".AsSpan(), 42));
                Assert.True(lookup.Remove("a".AsSpan()));
                Assert.False(lookup.Remove("a".AsSpan()));
                Assert.True(lookup.Remove("A".AsSpan()));
            }
            else
            {
                Assert.True(lookup.TryGetValue("A".AsSpan(), out actualKey, out value));
                Assert.Equal("a", actualKey);
                Assert.Equal(42, value);
                Assert.False(lookup.TryAdd("A".AsSpan(), 42));
                Assert.True(lookup.Remove("A".AsSpan()));
                Assert.False(lookup.Remove("a".AsSpan()));
                Assert.False(lookup.Remove("A".AsSpan()));
            }

            // Validate overwrites
            lookup["a".AsSpan()] = 42;
            Assert.Equal(42, dictionary["a"]);
            lookup["a".AsSpan()] = 43;
            Assert.True(lookup.Remove("a".AsSpan(), out actualKey, out value));
            Assert.Equal("a", actualKey);
            Assert.Equal(43, value);

            // Test adding multiple entries via the lookup
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, dictionary.Count);
                Assert.True(lookup.TryAdd(i.ToString().AsSpan(), i));
                Assert.False(lookup.TryAdd(i.ToString().AsSpan(), i));
            }

            Assert.Equal(10, dictionary.Count);

            // Test that the lookup and the dictionary agree on what's in and not in
            for (int i = -1; i <= 10; i++)
            {
                Assert.Equal(dictionary.TryGetValue(i.ToString(), out int dv), lookup.TryGetValue(i.ToString().AsSpan(), out int lv));
                Assert.Equal(dv, lv);
            }

            // Test removing multiple entries via the lookup
            for (int i = 9; i >= 0; i--)
            {
                Assert.True(lookup.Remove(i.ToString().AsSpan(), out actualKey, out value));
                Assert.Equal(i.ToString(), actualKey);
                Assert.Equal(i, value);
                Assert.False(lookup.Remove(i.ToString().AsSpan(), out actualKey, out value));
                Assert.Null(actualKey);
                Assert.Equal(0, value);
                Assert.Equal(i, dictionary.Count);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void HashSet_GetAlternateLookup_OperationsMatchUnderlyingSet(int mode)
        {
            // Test with a variety of comparers to ensure that the alternate lookup is consistent with the underlying set
            HashSet<string> set = new(mode switch
            {
                0 => StringComparer.Ordinal,
                1 => StringComparer.OrdinalIgnoreCase,
                2 => StringComparer.InvariantCulture,
                3 => StringComparer.InvariantCultureIgnoreCase,
                4 => StringComparer.CurrentCulture,
                5 => StringComparer.CurrentCultureIgnoreCase,
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            });
            HashSet<string>.AlternateLookup<ReadOnlySpan<char>> lookup = set.GetAlternateLookup<string, ReadOnlySpan<char>>();
            Assert.Same(set, lookup.Set);
            Assert.Same(lookup.Set, lookup.Set);

            // Add to the set and validate that the lookup reflects the changes
            Assert.True(set.Add("123"));
            Assert.True(lookup.Contains("123".AsSpan()));
            Assert.False(lookup.Add("123".AsSpan()));
            Assert.True(lookup.Remove("123".AsSpan()));
            Assert.False(set.Contains("123"));

            // Add via the lookup and validate that the set reflects the changes
            Assert.True(lookup.Add("123".AsSpan()));
            Assert.True(set.Contains("123"));
            lookup.TryGetValue("123".AsSpan(), out string value);
            Assert.Equal("123", value);
            Assert.False(lookup.Remove("321".AsSpan()));
            Assert.True(lookup.Remove("123".AsSpan()));

            // Ensure that case-sensitivity of the comparer is respected
            Assert.True(lookup.Add("a"));
            if (set.Comparer.Equals(StringComparer.Ordinal) ||
                set.Comparer.Equals(StringComparer.InvariantCulture) ||
                set.Comparer.Equals(StringComparer.CurrentCulture))
            {
                Assert.True(lookup.Add("A".AsSpan()));
                Assert.True(lookup.Remove("a".AsSpan()));
                Assert.False(lookup.Remove("a".AsSpan()));
                Assert.True(lookup.Remove("A".AsSpan()));
            }
            else
            {
                Assert.False(lookup.Add("A".AsSpan()));
                Assert.True(lookup.Remove("A".AsSpan()));
                Assert.False(lookup.Remove("a".AsSpan()));
                Assert.False(lookup.Remove("A".AsSpan()));
            }

            // Test the behavior of null vs "" in the set and lookup
            Assert.True(set.Add(null));
            Assert.True(set.Add(string.Empty));
            Assert.True(set.Contains(null));
            Assert.True(set.Contains(""));
            Assert.True(lookup.Contains("".AsSpan()));
            Assert.True(lookup.Remove("".AsSpan()));
            Assert.Equal(1, set.Count);
            Assert.False(lookup.Remove("".AsSpan()));
            Assert.True(set.Remove(null));
            Assert.Equal(0, set.Count);

            // Test adding multiple entries via the lookup
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, set.Count);
                Assert.True(lookup.Add(i.ToString().AsSpan()));
                Assert.False(lookup.Add(i.ToString().AsSpan()));
            }

            Assert.Equal(10, set.Count);

            // Test that the lookup and the set agree on what's in and not in
            for (int i = -1; i <= 10; i++)
            {
                Assert.Equal(set.TryGetValue(i.ToString(), out string dv), lookup.TryGetValue(i.ToString().AsSpan(), out string lv));
                Assert.Equal(dv, lv);
            }

            // Test removing multiple entries via the lookup
            for (int i = 9; i >= 0; i--)
            {
                Assert.True(lookup.Remove(i.ToString().AsSpan()));
                Assert.False(lookup.Remove(i.ToString().AsSpan()));
                Assert.Equal(i, set.Count);
            }
        }

        [Fact]
        public void Dictionary_NotCorruptedByThrowingComparer()
        {
            Dictionary<string, string> dict = new(new CreateThrowsComparer());

            Assert.Equal(0, dict.Count);

            Assert.Throws<FormatException>(() => dict.GetAlternateLookup<string, string, ReadOnlySpan<char>>().TryAdd("123".AsSpan(), "123"));
            Assert.Equal(0, dict.Count);

            dict.Add("123", "123");
            Assert.Equal(1, dict.Count);
        }

        [Fact]
        public void Dictionary_NotCorruptedByNullReturningComparer()
        {
            Dictionary<string, string> dict = new(new NullReturningComparer());

            Assert.Equal(0, dict.Count);

            Assert.ThrowsAny<ArgumentException>(() => dict.GetAlternateLookup<string, string, ReadOnlySpan<char>>().TryAdd("123".AsSpan(), "123"));
            Assert.Equal(0, dict.Count);

            dict.Add("123", "123");
            Assert.Equal(1, dict.Count);
        }

        [Fact]
        public void HashSet_NotCorruptedByThrowingComparer()
        {
            HashSet<string> set = new(new CreateThrowsComparer());

            Assert.Equal(0, set.Count);

            Assert.Throws<FormatException>(() => set.GetAlternateLookup<string, ReadOnlySpan<char>>().Add("123".AsSpan()));
            Assert.Equal(0, set.Count);

            set.Add("123");
            Assert.Equal(1, set.Count);
        }

        private sealed class CreateThrowsComparer : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
        {
            public bool Equals(string? x, string? y) => EqualityComparer<string>.Default.Equals(x, y);
            public int GetHashCode(string obj) => EqualityComparer<string>.Default.GetHashCode(obj);

            public bool Equals(ReadOnlySpan<char> span, string target) => span.SequenceEqual(target);
            public int GetHashCode(ReadOnlySpan<char> span) => string.GetHashCode(span);
            public string Create(ReadOnlySpan<char> span) => throw new FormatException();
        }

        private sealed class NullReturningComparer : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
        {
            public bool Equals(string? x, string? y) => EqualityComparer<string>.Default.Equals(x, y);
            public int GetHashCode(string obj) => EqualityComparer<string>.Default.GetHashCode(obj);

            public bool Equals(ReadOnlySpan<char> span, string target) => span.SequenceEqual(target);
            public int GetHashCode(ReadOnlySpan<char> span) => string.GetHashCode(span);
            public string Create(ReadOnlySpan<char> span) => null!;
        }
    }
}
