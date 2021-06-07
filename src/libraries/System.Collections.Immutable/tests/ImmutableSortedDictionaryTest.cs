// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableSortedDictionaryTest : ImmutableDictionaryTestBase
    {
        private enum Operation
        {
            Add,
            Set,
            Remove,
            Last,
        }

        [Fact]
        public void RandomOperationsTest()
        {
            int operationCount = this.RandomOperationsCount;
            var expected = new SortedDictionary<int, bool>();
            var actual = ImmutableSortedDictionary<int, bool>.Empty;

            int seed = unchecked((int)DateTime.Now.Ticks);
            Debug.WriteLine("Using random seed {0}", seed);
            var random = new Random(seed);

            for (int iOp = 0; iOp < operationCount; iOp++)
            {
                switch ((Operation)random.Next((int)Operation.Last))
                {
                    case Operation.Add:
                        int key;
                        do
                        {
                            key = random.Next();
                        }
                        while (expected.ContainsKey(key));
                        bool value = random.Next() % 2 == 0;
                        Debug.WriteLine("Adding \"{0}\"={1} to the set.", key, value);
                        expected.Add(key, value);
                        actual = actual.Add(key, value);
                        break;

                    case Operation.Set:
                        bool overwrite = expected.Count > 0 && random.Next() % 2 == 0;
                        if (overwrite)
                        {
                            int position = random.Next(expected.Count);
                            key = expected.Skip(position).First().Key;
                        }
                        else
                        {
                            do
                            {
                                key = random.Next();
                            }
                            while (expected.ContainsKey(key));
                        }

                        value = random.Next() % 2 == 0;
                        Debug.WriteLine("Setting \"{0}\"={1} to the set (overwrite={2}).", key, value, overwrite);
                        expected[key] = value;
                        actual = actual.SetItem(key, value);
                        break;

                    case Operation.Remove:
                        if (expected.Count > 0)
                        {
                            int position = random.Next(expected.Count);
                            key = expected.Skip(position).First().Key;
                            Debug.WriteLine("Removing element \"{0}\" from the set.", key);
                            Assert.True(expected.Remove(key));
                            actual = actual.Remove(key);
                        }

                        break;
                }

                Assert.Equal<KeyValuePair<int, bool>>(expected.ToList(), actual.ToList());
            }
        }

        [Fact]
        public void AddExistingKeySameValueTest()
        {
            AddExistingKeySameValueTestHelper(Empty(StringComparer.Ordinal, StringComparer.Ordinal), "Company", "Microsoft", "Microsoft");
            AddExistingKeySameValueTestHelper(Empty(StringComparer.Ordinal, StringComparer.OrdinalIgnoreCase), "Company", "Microsoft", "MICROSOFT");
        }

        [Fact]
        public void AddExistingKeyDifferentValueTest()
        {
            AddExistingKeyDifferentValueTestHelper(Empty(StringComparer.Ordinal, StringComparer.Ordinal), "Company", "Microsoft", "MICROSOFT");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void ToUnorderedTest()
        {
            var sortedMap = Empty<int, GenericParameterHelper>().AddRange(Enumerable.Range(1, 100).Select(n => new KeyValuePair<int, GenericParameterHelper>(n, new GenericParameterHelper(n))));
            var unsortedMap = sortedMap.ToImmutableDictionary();
            Assert.IsAssignableFrom<ImmutableDictionary<int, GenericParameterHelper>>(unsortedMap);
            Assert.Equal(sortedMap.Count, unsortedMap.Count);
            Assert.Equal<KeyValuePair<int, GenericParameterHelper>>(sortedMap.ToList(), unsortedMap.ToList());
        }

        [Fact]
        public void SortChangeTest()
        {
            var map = Empty<string, string>(StringComparer.Ordinal)
                .Add("Johnny", "Appleseed")
                .Add("JOHNNY", "Appleseed");
            Assert.Equal(2, map.Count);
            Assert.True(map.ContainsKey("Johnny"));
            Assert.False(map.ContainsKey("johnny"));
            var newMap = map.ToImmutableSortedDictionary(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, newMap.Count);
            Assert.True(newMap.ContainsKey("Johnny"));
            Assert.True(newMap.ContainsKey("johnny")); // because it's case insensitive
        }

        [Fact]
        public void InitialBulkAddUniqueTest()
        {
            var uniqueEntries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("a", "b"),
                new KeyValuePair<string,string>("c", "d"),
            };

            var map = Empty<string, string>(StringComparer.Ordinal, StringComparer.Ordinal);
            var actual = map.AddRange(uniqueEntries);
            Assert.Equal(2, actual.Count);
        }

        [Fact]
        public void InitialBulkAddWithExactDuplicatesTest()
        {
            var uniqueEntries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("a", "b"),
                new KeyValuePair<string,string>("a", "b"),
            };

            var map = Empty<string, string>(StringComparer.Ordinal, StringComparer.Ordinal);
            var actual = map.AddRange(uniqueEntries);
            Assert.Equal(1, actual.Count);
        }

        [Fact]
        public void ContainsValueTest()
        {
            this.ContainsValueTestHelper(ImmutableSortedDictionary<int, GenericParameterHelper>.Empty, 1, new GenericParameterHelper());
        }

        [Fact]
        public void InitialBulkAddWithKeyCollisionTest()
        {
            var uniqueEntries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("a", "b"),
                new KeyValuePair<string,string>("a", "d"),
            };

            var map = Empty<string, string>(StringComparer.Ordinal, StringComparer.Ordinal);
            AssertExtensions.Throws<ArgumentException>(null, () => map.AddRange(uniqueEntries));
        }

        [Fact]
        public void Create()
        {
            IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string> { { "a", "b" } };
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = StringComparer.CurrentCulture;

            var dictionary = ImmutableSortedDictionary.Create<string, string>();
            Assert.Equal(0, dictionary.Count);
            Assert.Same(Comparer<string>.Default, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = ImmutableSortedDictionary.Create<string, string>(keyComparer);
            Assert.Equal(0, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = ImmutableSortedDictionary.Create(keyComparer, valueComparer);
            Assert.Equal(0, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(valueComparer, dictionary.ValueComparer);

            dictionary = ImmutableSortedDictionary.CreateRange(pairs);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(Comparer<string>.Default, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = ImmutableSortedDictionary.CreateRange(keyComparer, pairs);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = ImmutableSortedDictionary.CreateRange(keyComparer, valueComparer, pairs);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(valueComparer, dictionary.ValueComparer);
        }

        [Fact]
        public void ToImmutableSortedDictionary()
        {
            IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string> { { "a", "B" } };
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = StringComparer.CurrentCulture;

            ImmutableSortedDictionary<string, string> dictionary = pairs.ToImmutableSortedDictionary();
            Assert.Equal(1, dictionary.Count);
            Assert.Same(Comparer<string>.Default, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = pairs.ToImmutableSortedDictionary(keyComparer);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = pairs.ToImmutableSortedDictionary(keyComparer, valueComparer);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(valueComparer, dictionary.ValueComparer);

            dictionary = pairs.ToImmutableSortedDictionary(p => p.Key.ToUpperInvariant(), p => p.Value.ToLowerInvariant());
            Assert.Equal(1, dictionary.Count);
            Assert.Equal("A", dictionary.Keys.Single());
            Assert.Equal("b", dictionary.Values.Single());
            Assert.Same(Comparer<string>.Default, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = pairs.ToImmutableSortedDictionary(p => p.Key.ToUpperInvariant(), p => p.Value.ToLowerInvariant(), keyComparer);
            Assert.Equal(1, dictionary.Count);
            Assert.Equal("A", dictionary.Keys.Single());
            Assert.Equal("b", dictionary.Values.Single());
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(EqualityComparer<string>.Default, dictionary.ValueComparer);

            dictionary = pairs.ToImmutableSortedDictionary(p => p.Key.ToUpperInvariant(), p => p.Value.ToLowerInvariant(), keyComparer, valueComparer);
            Assert.Equal(1, dictionary.Count);
            Assert.Equal("A", dictionary.Keys.Single());
            Assert.Equal("b", dictionary.Values.Single());
            Assert.Same(keyComparer, dictionary.KeyComparer);
            Assert.Same(valueComparer, dictionary.ValueComparer);
        }

        [Fact]
        public void WithComparers()
        {
            var map = ImmutableSortedDictionary.Create<string, string>().Add("a", "1").Add("B", "1");
            Assert.Same(Comparer<string>.Default, map.KeyComparer);
            Assert.True(map.ContainsKey("a"));
            Assert.False(map.ContainsKey("A"));

            map = map.WithComparers(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
            Assert.Equal(2, map.Count);
            Assert.True(map.ContainsKey("a"));
            Assert.True(map.ContainsKey("A"));
            Assert.True(map.ContainsKey("b"));

            var cultureComparer = StringComparer.CurrentCulture;
            map = map.WithComparers(StringComparer.OrdinalIgnoreCase, cultureComparer);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
            Assert.Same(cultureComparer, map.ValueComparer);
            Assert.Equal(2, map.Count);
            Assert.True(map.ContainsKey("a"));
            Assert.True(map.ContainsKey("A"));
            Assert.True(map.ContainsKey("b"));
        }

        [Fact]
        public void WithComparersCollisions()
        {
            // First check where collisions have matching values.
            var map = ImmutableSortedDictionary.Create<string, string>()
                .Add("a", "1").Add("A", "1");
            map = map.WithComparers(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
            Assert.Equal(1, map.Count);
            Assert.True(map.ContainsKey("a"));
            Assert.Equal("1", map["a"]);

            // Now check where collisions have conflicting values.
            map = ImmutableSortedDictionary.Create<string, string>()
              .Add("a", "1").Add("A", "2").Add("b", "3");
            AssertExtensions.Throws<ArgumentException>(null, () => map.WithComparers(StringComparer.OrdinalIgnoreCase));

            // Force all values to be considered equal.
            map = map.WithComparers(StringComparer.OrdinalIgnoreCase, EverythingEqual<string>.Default);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
            Assert.Same(EverythingEqual<string>.Default, map.ValueComparer);
            Assert.Equal(2, map.Count);
            Assert.True(map.ContainsKey("a"));
            Assert.True(map.ContainsKey("b"));
        }

        [Fact]
        public void CollisionExceptionMessageContainsKey()
        {
            var map = ImmutableSortedDictionary.Create<string, string>()
                .Add("firstKey", "1").Add("secondKey", "2");
            var exception = AssertExtensions.Throws<ArgumentException>(null, () => map.Add("firstKey", "3"));
            Assert.Contains("firstKey", exception.Message);
        }

        [Fact]
        public void WithComparersEmptyCollection()
        {
            var map = ImmutableSortedDictionary.Create<string, string>();
            Assert.Same(Comparer<string>.Default, map.KeyComparer);
            map = map.WithComparers(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, map.KeyComparer);
        }

        [Fact]
        public void EnumeratorRecyclingMisuse()
        {
            var collection = ImmutableSortedDictionary.Create<int, int>().Add(3, 5);
            var enumerator = collection.GetEnumerator();
            var enumeratorCopy = enumerator;
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();
            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Current);

            enumerator.Dispose(); // double-disposal should not throw
            enumeratorCopy.Dispose();

            // We expect that acquiring a new enumerator will use the same underlying Stack<T> object,
            // but that it will not throw exceptions for the new enumerator.
            enumerator = collection.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            enumerator.Dispose();
        }

        [Fact]
        public void Remove_KeyExists_RemovesKeyValuePair()
        {
            ImmutableSortedDictionary<int, string>  dictionary = new Dictionary<int, string>
            {
                { 1, "a" }
            }.ToImmutableSortedDictionary();
            Assert.Equal(0, dictionary.Remove(1).Count);
        }

        [Fact]
        public void Remove_FirstKey_RemovesKeyValuePair()
        {
            ImmutableSortedDictionary<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" },
                { 2, "b" }
            }.ToImmutableSortedDictionary();
            Assert.Equal(1, dictionary.Remove(1).Count);
        }

        [Fact]
        public void Remove_SecondKey_RemovesKeyValuePair()
        {
            ImmutableSortedDictionary<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" },
                { 2, "b" }
            }.ToImmutableSortedDictionary();
            Assert.Equal(1, dictionary.Remove(2).Count);
        }

        [Fact]
        public void Remove_KeyDoesntExist_DoesNothing()
        {
            ImmutableSortedDictionary<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" }
            }.ToImmutableSortedDictionary();
            Assert.Equal(1, dictionary.Remove(2).Count);
            Assert.Equal(1, dictionary.Remove(-1).Count);
        }

        [Fact]
        public void Remove_EmptyDictionary_DoesNothing()
        {
            ImmutableSortedDictionary<int, string> dictionary = ImmutableSortedDictionary<int, string>.Empty;
            Assert.Equal(0, dictionary.Remove(2).Count);
        }

        [Fact]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableSortedDictionary.Create<string, int>());
            ImmutableSortedDictionary<int, int> dict = ImmutableSortedDictionary.Create<int, int>().Add(1, 2).Add(2, 3).Add(3, 4);
            var info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(dict);

            object rootNode = DebuggerAttributes.GetFieldValue(ImmutableSortedDictionary.Create<string, string>(), "_root");
            DebuggerAttributes.ValidateDebuggerDisplayReferences(rootNode);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            KeyValuePair<int, int>[] items = itemProperty.GetValue(info.Instance) as KeyValuePair<int, int>[];
            Assert.Equal(dict, items);
        }

        [Fact]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableSortedDictionary.Create<int, int>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void ValueRef()
        {
            var dictionary = new Dictionary<string, int>()
            {
                { "a", 1 },
                { "b", 2 }
            }.ToImmutableSortedDictionary();

            ref readonly var safeRef = ref dictionary.ValueRef("a");
            ref var unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(1, dictionary.ValueRef("a"));

            unsafeRef = 5;

            Assert.Equal(5, dictionary.ValueRef("a"));
        }

        [Fact]
        public void ValueRef_NonExistentKey()
        {
            var dictionary = new Dictionary<string, int>()
            {
                { "a", 1 },
                { "b", 2 }
            }.ToImmutableSortedDictionary();

            Assert.Throws<KeyNotFoundException>(() => dictionary.ValueRef("c"));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50579", TestPlatforms.Android)]
        public void Indexer_KeyNotFoundException_ContainsKeyInMessage()
        {
            var map = ImmutableSortedDictionary.Create<string, string>()
                .Add("a", "1").Add("b", "2");
            var exception = Assert.Throws<KeyNotFoundException>(() => map["c"]);
            Assert.Contains("'c'", exception.Message);
        }

        protected override IImmutableDictionary<TKey, TValue> Empty<TKey, TValue>()
        {
            return ImmutableSortedDictionaryTest.Empty<TKey, TValue>();
        }

        protected override IImmutableDictionary<string, TValue> Empty<TValue>(StringComparer comparer)
        {
            return ImmutableSortedDictionary.Create<string, TValue>(comparer);
        }

        protected override IEqualityComparer<TValue> GetValueComparer<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
        {
            return ((ImmutableSortedDictionary<TKey, TValue>)dictionary).ValueComparer;
        }

        protected void ContainsValueTestHelper<TKey, TValue>(ImmutableSortedDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            Assert.False(map.ContainsValue(value));
            Assert.True(map.Add(key, value).ContainsValue(value));
        }

        private static IImmutableDictionary<TKey, TValue> Empty<TKey, TValue>(IComparer<TKey> keyComparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer);
        }
    }
}
