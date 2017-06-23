// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToDictionaryTests : EnumerableTests
    {
        private class CustomComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) { return EqualityComparer<T>.Default.Equals(x, y); }
            public int GetHashCode(T obj) { return EqualityComparer<T>.Default.GetHashCode(obj); }
        }

        [Fact]
        public void ToDictionary_AlwaysCreateACopy()
        {
            Dictionary<int, int> source = new Dictionary<int, int>() { { 1, 1 }, { 2, 2 }, { 3, 3 } };
            Dictionary<int, int> result = source.ToDictionary(key => key.Key, val => val.Value);

            Assert.NotSame(source, result);
            Assert.Equal(source, result);
        }


        private void RunToDictionaryOnAllCollectionTypes<T>(T[] items, Action<Dictionary<T, T>> validation)
        {
            validation(Enumerable.ToDictionary(items, key => key));
            validation(Enumerable.ToDictionary(items, key => key, value => value));
            validation(Enumerable.ToDictionary(new List<T>(items), key => key));
            validation(Enumerable.ToDictionary(new List<T>(items), key => key, value => value));
            validation(new TestEnumerable<T>(items).ToDictionary(key => key));
            validation(new TestEnumerable<T>(items).ToDictionary(key => key, value => value));
            validation(new TestReadOnlyCollection<T>(items).ToDictionary(key => key));
            validation(new TestReadOnlyCollection<T>(items).ToDictionary(key => key, value => value));
            validation(new TestCollection<T>(items).ToDictionary(key => key));
            validation(new TestCollection<T>(items).ToDictionary(key => key, value => value));
        }


        [Fact]
        public void ToDictionary_WorkWithEmptyCollection()
        {
            RunToDictionaryOnAllCollectionTypes(new int[0],
                resultDictionary =>
                {
                    Assert.NotNull(resultDictionary);
                    Assert.Equal(0, resultDictionary.Count);
                });
        }


        [Fact]
        public void ToDictionary_ProduceCorrectDictionary()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            RunToDictionaryOnAllCollectionTypes(sourceArray,
                resultDictionary =>
                {
                    Assert.Equal(sourceArray.Length, resultDictionary.Count);
                    Assert.Equal(sourceArray, resultDictionary.Keys);
                    Assert.Equal(sourceArray, resultDictionary.Values);
                });

            string[] sourceStringArray = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            RunToDictionaryOnAllCollectionTypes(sourceStringArray,
                resultDictionary =>
                {
                    Assert.Equal(sourceStringArray.Length, resultDictionary.Count);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultDictionary[sourceStringArray[i]]);
                });
        }

        [Fact]
        public void RunOnce()
        {
            Assert.Equal(
                new Dictionary<int, string> {{1, "0"}, {2, "1"}, {3, "2"}, {4, "3"}},
                Enumerable.Range(0, 4).RunOnce().ToDictionary(i => i + 1, i => i.ToString()));
        }

        [Fact]
        public void ToDictionary_PassCustomComparer()
        {
            CustomComparer<int> comparer = new CustomComparer<int>();
            TestCollection<int> collection = new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result1 = collection.ToDictionary(key => key, comparer);
            Assert.Same(comparer, result1.Comparer);

            Dictionary<int, int> result2 = collection.ToDictionary(key => key, val => val, comparer);
            Assert.Same(comparer, result2.Comparer);
        }

        [Fact]
        public void ToDictionary_UseDefaultComparerOnNull()
        {
            CustomComparer<int> comparer = null;
            TestCollection<int> collection = new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result1 = collection.ToDictionary(key => key, comparer);
            Assert.Same(EqualityComparer<int>.Default, result1.Comparer);

            Dictionary<int, int> result2 = collection.ToDictionary(key => key, val => val, comparer);
            Assert.Same(EqualityComparer<int>.Default, result2.Comparer);
        }

        [Fact]
        public void ToDictionary_KeyValueSelectorsWork()
        {
            TestCollection<int> collection = new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result = collection.ToDictionary(key => key + 10, val => val + 100);

            Assert.Equal(collection.Items.Select(o => o + 10), result.Keys);
            Assert.Equal(collection.Items.Select(o => o + 100), result.Values);
        }


        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToDictionary(key => key));
        }


        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenKeySelectorIsNull()
        {
            int[] source = new int[0];
            Func<int, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenValueSelectorIsNull()
        {
            int[] source = new int[0];
            Func<int, int> keySelector = key => key;
            Func<int, int> valueSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => source.ToDictionary(keySelector, valueSelector));
        }

        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenSourceIsNullElementSelector()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToDictionary(key => key, e => e));
        }


        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenKeySelectorIsNullElementSelector()
        {
            int[] source = new int[0];
            Func<int, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.ToDictionary(keySelector, e => e));
        }

        [Fact]
        public void ToDictionary_KeySelectorThrowException()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, int> keySelector = key =>
            {
                if (key == 1)
                    throw new InvalidOperationException();
                return key;
            };


            Assert.Throws<InvalidOperationException>(() => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ThrowWhenKeySelectorReturnNull()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, string> keySelector = key => null;

            AssertExtensions.Throws<ArgumentNullException>("key", () => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ThrowWhenKeySelectorReturnSameValueTwice()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, int> keySelector = key => 1;

            AssertExtensions.Throws<ArgumentException>(null, () => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ValueSelectorThrowException()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, int> keySelector = key => key;
            Func<int, int> valueSelector = value =>
            {
                if (value == 1)
                    throw new InvalidOperationException();
                return value;
            };

            Assert.Throws<InvalidOperationException>(() => source.ToDictionary(keySelector, valueSelector));
        }
        
        [Fact]
        public void ThrowsOnNullKey()
        {
            var source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = "null", Score = 55 }
            };

            source.ToDictionary(e => e.Name); // Doesn't throw;

            source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = default(string), Score = 55 }
            };

            AssertExtensions.Throws<ArgumentNullException>("key", () => source.ToDictionary(e => e.Name));
        }

        [Fact]
        public void ThrowsOnNullKeyCustomComparer()
        {
            var source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = "null", Score = 55 }
            };

            source.ToDictionary(e => e.Name, new AnagramEqualityComparer()); // Doesn't throw;

            source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = default(string), Score = 55 }
            };

            AssertExtensions.Throws<ArgumentNullException>("key", () => source.ToDictionary(e => e.Name, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ThrowsOnNullKeyValueSelector()
        {
            var source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = "null", Score = 55 }
            };

            source.ToDictionary(e => e.Name, e => e); // Doesn't throw;

            source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = default(string), Score = 55 }
            };

            AssertExtensions.Throws<ArgumentNullException>("key", () => source.ToDictionary(e => e.Name, e => e));
        }

        [Fact]
        public void ThrowsOnNullKeyCustomComparerValueSelector()
        {
            var source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = "null", Score = 55 }
            };

            source.ToDictionary(e => e.Name, e => e, new AnagramEqualityComparer()); // Doesn't throw;

            source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = default(string), Score = 55 }
            };

            AssertExtensions.Throws<ArgumentNullException>("key", () => source.ToDictionary(e => e.Name, e => e, new AnagramEqualityComparer()));
        }
        
        [Fact]
        public void ThrowsOnDuplicateKeys()
        {
            var source = new[]
            {
                new { Name = "Chris", Score = 50 },
                new { Name = "Bob", Score = 95 },
                new { Name = "Bob", Score = 55 }
            };

            AssertExtensions.Throws<ArgumentException>(null, () => source.ToDictionary(e => e.Name, e => e, new AnagramEqualityComparer()));
        }
        
        private static void AssertMatches<K, E>(IEnumerable<K> keys, IEnumerable<E> values, Dictionary<K, E> dict)
        {
            Assert.NotNull(dict);
            Assert.NotNull(keys);
            Assert.NotNull(values);
            using (var ke = keys.GetEnumerator())
            {
                foreach(var value in values)
                {
                    Assert.True(ke.MoveNext()); 
                    var key = ke.Current;
                    E dictValue;
                    Assert.True(dict.TryGetValue(key, out dictValue));
                    Assert.Equal(value, dictValue);
                    dict.Remove(key);
                }
                Assert.False(ke.MoveNext());
                Assert.Equal(0, dict.Count());
            }
        }

        [Fact]
        public void EmtpySource()
        {
            int[] elements = new int[] { };
            string[] keys = new string[] { };
            var source = keys.Zip(elements, (k, e) => new { Name = k, Score = e });

            AssertMatches(keys, elements, source.ToDictionary(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }
        
        [Fact]
        public void OneElementNullComparer()
        {
            int[] elements = new int[] { 5 };
            string[] keys = new string[] { "Bob" };
            var source = new [] { new { Name = keys[0], Score = elements[0] } };

            AssertMatches(keys, elements, source.ToDictionary(e => e.Name, e => e.Score, null));
        }
        
        [Fact]
        public void SeveralElementsCustomComparerer()
        {
            string[] keys = new string[] { "Bob", "Zen", "Prakash", "Chris", "Sachin" };
            var source = new []
            {
                new { Name = "Bbo", Score = 95 },
                new { Name = keys[1], Score = 45 },
                new { Name = keys[2], Score = 100 },
                new { Name = keys[3], Score = 90 },
                new { Name = keys[4], Score = 45 }
            };
            
            AssertMatches(keys, source, source.ToDictionary(e => e.Name, new AnagramEqualityComparer()));
        }
        
        [Fact]
        public void NullCoalescedKeySelector()
        {
            string[] elements = new string[] { null };
            string[] keys = new string[] { string.Empty };
            string[] source = new string[] { null };

            AssertMatches(keys, elements, source.ToDictionary(e => e ?? string.Empty, e => e, EqualityComparer<string>.Default));

        }
    }
}
