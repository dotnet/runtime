// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    internal static class CollectionAsserts
    {
        public static void HasCount<T>(ICollection<T> collection, int count)
        {
            Assert.Equal(count, collection.Count);
#if !NETFRAMEWORK
            IReadOnlyCollection<T> readOnlyCollection = collection;
            Assert.Equal(count, readOnlyCollection.Count);
#endif
        }

        public static void EqualAt<T>(IList<T> list, int index, T expected)
        {
            Assert.Equal(expected, list[index]);
#if !NETFRAMEWORK
            IReadOnlyList<T> readOnlyList = list;
            Assert.Equal(expected, readOnlyList[index]);
#endif
        }

        public static void NotEqualAt<T>(IList<T> list, int index, T expected)
        {
            Assert.NotEqual(expected, list[index]);
#if !NETFRAMEWORK
            IReadOnlyList<T> readOnlyList = list;
            Assert.NotEqual(expected, readOnlyList[index]);
#endif
        }

        public static void ThrowsElementAt<T>(IList<T> list, int index, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => list[index]);
#if !NETFRAMEWORK
            IReadOnlyList<T> readOnlyList = list;
            Assert.Throws(exceptionType, () => readOnlyList[index]);
#endif
        }

        public static void ElementAtSucceeds<T>(IList<T> list, int index)
        {
            T result = list[index];
#if !NETFRAMEWORK
            IReadOnlyList<T> readOnlyList = list;
            Assert.Equal(result, readOnlyList[index]);
#endif
        }

        public static void EqualAt<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue expected)
        {
            Assert.Equal(expected, dictionary[key]);
#if !NETFRAMEWORK
            IReadOnlyDictionary<TKey, TValue> readOnlyDictionary = dictionary;
            Assert.Equal(expected, readOnlyDictionary[key]);
#endif
        }

        public static void ContainsKey<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, bool expected)
        {
            Assert.Equal(expected, dictionary.ContainsKey(key));
#if !NETFRAMEWORK
            IReadOnlyDictionary<TKey, TValue> readOnlyDictionary = dictionary;
            Assert.Equal(expected, readOnlyDictionary.ContainsKey(key));
#endif
        }

        public static void TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, bool expected, TValue expectedValue = default)
        {
            Assert.Equal(expected, dictionary.TryGetValue(key, out TValue value));
            if (expected)
            {
                Assert.Equal(expectedValue, value);
            }
#if !NETFRAMEWORK
            IReadOnlyDictionary<TKey, TValue> readOnlyDictionary = dictionary;
            Assert.Equal(expected, readOnlyDictionary.TryGetValue(key, out value));
            if (expected)
            {
                Assert.Equal(expectedValue, value);
            }
#endif
        }

        public static void Contains<T>(ISet<T> set, T expected)
        {
            Assert.True(set.Contains(expected));
#if !NETFRAMEWORK
            ICollection<T> collection = set;
            Assert.True(collection.Contains(expected));
            IReadOnlySet<T> readOnlySet = set;
            Assert.True(readOnlySet.Contains(expected));
#endif
        }

        public static void IsProperSubsetOf<T>(ISet<T> set, IEnumerable<T> enumerable, bool expected)
        {
            Assert.Equal(expected, set.IsProperSubsetOf(enumerable));
#if !NETFRAMEWORK
            IReadOnlySet<T> readOnlySet = set;
            Assert.Equal(expected, readOnlySet.IsProperSubsetOf(enumerable));
#endif
        }

        public static void IsProperSupersetOf<T>(ISet<T> set, IEnumerable<T> enumerable, bool expected)
        {
            Assert.Equal(expected, set.IsProperSupersetOf(enumerable));
#if !NETFRAMEWORK
            IReadOnlySet<T> readOnlySet = set;
            Assert.Equal(expected, readOnlySet.IsProperSupersetOf(enumerable));
#endif
        }

        public static void IsSubsetOf<T>(ISet<T> set, IEnumerable<T> enumerable, bool expected)
        {
            Assert.Equal(expected, set.IsSubsetOf(enumerable));
#if !NETFRAMEWORK
            IReadOnlySet<T> readOnlySet = set;
            Assert.Equal(expected, readOnlySet.IsSubsetOf(enumerable));
#endif
        }

        public static void IsSupersetOf<T>(ISet<T> set, IEnumerable<T> enumerable, bool expected)
        {
            Assert.Equal(expected, set.IsSupersetOf(enumerable));
#if !NETFRAMEWORK
            IReadOnlySet<T> readOnlySet = set;
            Assert.Equal(expected, readOnlySet.IsSupersetOf(enumerable));
#endif
        }

        public static void Overlaps<T>(ISet<T> set, IEnumerable<T> enumerable, bool expected)
        {
            Assert.Equal(expected, set.Overlaps(enumerable));
#if !NETFRAMEWORK
            IReadOnlySet<T> readOnlySet = set;
            Assert.Equal(expected, readOnlySet.Overlaps(enumerable));
#endif
        }

        public static void SetEquals<T>(ISet<T> set, IEnumerable<T> enumerable, bool expected)
        {
            Assert.Equal(expected, set.SetEquals(enumerable));
#if !NETFRAMEWORK
            IReadOnlySet<T> readOnlySet = set;
            Assert.Equal(expected, readOnlySet.SetEquals(enumerable));
#endif
        }

        public static void Equal(ICollection expected, ICollection actual)
        {
            Assert.Equal(expected == null, actual == null);
            if (expected == null)
            {
                return;
            }
            Assert.Equal(expected.Count, actual.Count);
            IEnumerator e = expected.GetEnumerator();
            IEnumerator a = actual.GetEnumerator();
            while (e.MoveNext())
            {
                Assert.True(a.MoveNext(), "actual has fewer elements");
                if (e.Current == null)
                {
                    Assert.Null(a.Current);
                }
                else
                {
                    Assert.IsType(e.Current.GetType(), a.Current);
                    Assert.Equal(e.Current, a.Current);
                }
            }
            Assert.False(a.MoveNext(), "actual has more elements");
        }

        public static void Equal<T>(ICollection<T> expected, ICollection<T> actual)
        {
            Assert.Equal(expected == null, actual == null);
            if (expected == null)
            {
                return;
            }
            Assert.Equal(expected.Count, actual.Count);
#if !NETFRAMEWORK
            IReadOnlyCollection<T> readOnlyExpected = expected;
            Assert.Equal(expected.Count, readOnlyExpected.Count);
            IReadOnlyCollection<T> readOnlyActual = actual;
            Assert.Equal(actual.Count, readOnlyActual.Count);
#endif
            IEnumerator<T> e = expected.GetEnumerator();
            IEnumerator<T> a = actual.GetEnumerator();
            while (e.MoveNext())
            {
                Assert.True(a.MoveNext(), "actual has fewer elements");
                if (e.Current == null)
                {
                    Assert.Null(a.Current);
                }
                else
                {
                    Assert.IsType(e.Current.GetType(), a.Current);
                    Assert.Equal(e.Current, a.Current);
                }
            }
            Assert.False(a.MoveNext(), "actual has more elements");
        }

        public static void EqualUnordered(ICollection expected, ICollection actual)
        {
            Assert.Equal(expected == null, actual == null);
            if (expected == null)
            {
                return;
            }

            // Lookups are an aggregated collections (enumerable contents), but ordered.
            ILookup<object, object> e = expected.Cast<object>().ToLookup(key => key);
            ILookup<object, object> a = actual.Cast<object>().ToLookup(key => key);

            // Dictionaries can't handle null keys, which is a possibility
            Assert.Equal(e.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()), a.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()));

            // Get count of null keys.  Returns an empty sequence (and thus a 0 count) if no null key
            Assert.Equal(e[null].Count(), a[null].Count());
        }

        public static void EqualUnordered<T>(ICollection<T> expected, ICollection<T> actual)
        {
            Assert.Equal(expected == null, actual == null);
            if (expected == null)
            {
                return;
            }

            // Lookups are an aggregated collections (enumerable contents), but ordered.
            ILookup<object, object> e = expected.Cast<object>().ToLookup(key => key);
            ILookup<object, object> a = actual.Cast<object>().ToLookup(key => key);

            // Dictionaries can't handle null keys, which is a possibility
            Assert.Equal(e.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()), a.Where(kv => kv.Key != null).ToDictionary(g => g.Key, g => g.Count()));

            // Get count of null keys.  Returns an empty sequence (and thus a 0 count) if no null key
            Assert.Equal(e[null].Count(), a[null].Count());
        }
    }
}
