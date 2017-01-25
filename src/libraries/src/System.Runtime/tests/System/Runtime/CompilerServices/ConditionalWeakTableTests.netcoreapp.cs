// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public partial class ConditionalWeakTableTests
    {
        [Fact]
        public static void AddOrUpdateDataTest()
        {
            var cwt = new ConditionalWeakTable<string, string>();
            string key = "key1";
            cwt.AddOrUpdate(key, "value1");

            string value;
            Assert.True(cwt.TryGetValue(key, out value));
            Assert.Equal(value, "value1");
            Assert.Equal(value, cwt.GetOrCreateValue(key));
            Assert.Equal(value, cwt.GetValue(key, k => "value1"));

            Assert.Throws<ArgumentNullException>(() => cwt.AddOrUpdate(null, "value2"));

            cwt.AddOrUpdate(key, "value2");
            Assert.True(cwt.TryGetValue(key, out value));
            Assert.Equal(value, "value2");
            Assert.Equal(value, cwt.GetOrCreateValue(key));
            Assert.Equal(value, cwt.GetValue(key, k => "value1"));
        }

        [Fact]
        public static void Clear_EmptyTable()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            cwt.Clear(); // no exception
            cwt.Clear();
        }

        [Fact]
        public static void Clear_AddThenEmptyRepeatedly_ItemsRemoved()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            object key = new object(), value = new object();
            object result;
            for (int i = 0; i < 3; i++)
            {
                cwt.Add(key, value);

                Assert.True(cwt.TryGetValue(key, out result));
                Assert.Same(value, result);

                cwt.Clear();

                Assert.False(cwt.TryGetValue(key, out result));
                Assert.Null(result);
            }
        }

        [Fact]
        public static void Clear_AddMany_Clear_AllItemsRemoved()
        {
            var cwt = new ConditionalWeakTable<object, object>();

            object[] keys = Enumerable.Range(0, 33).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, keys.Length).Select(_ => new object()).ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                cwt.Add(keys[i], values[i]);
            }

            Assert.Equal(keys.Length, ((IEnumerable<KeyValuePair<object, object>>)cwt).Count());

            cwt.Clear();

            Assert.Equal(0, ((IEnumerable<KeyValuePair<object, object>>)cwt).Count());

            GC.KeepAlive(keys);
            GC.KeepAlive(values);
        }

        [Fact]
        public static void GetEnumerator_Empty_ReturnsEmptyEnumerator()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;
            Assert.Equal(0, enumerable.Count());
        }

        [Fact]
        public static void GetEnumerator_AddedAndRemovedItems_AppropriatelyShowUpInEnumeration()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object key1 = new object(), value1 = new object();

            for (int i = 0; i < 20; i++) // adding and removing multiple times, across internal container boundary
            {
                cwt.Add(key1, value1);
                Assert.Equal(1, enumerable.Count());
                Assert.Equal(new KeyValuePair<object, object>(key1, value1), enumerable.First());

                Assert.True(cwt.Remove(key1));
                Assert.Equal(0, enumerable.Count());
            }

            GC.KeepAlive(key1);
            GC.KeepAlive(value1);
        }

        [Fact]
        public static void GetEnumerator_CollectedItemsNotEnumerated()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            // Delegate to add collectible items to the table, separated out 
            // to avoid the JIT extending the lifetimes of the temporaries
            Action<ConditionalWeakTable<object, object>> addItem = 
                t => t.Add(new object(), new object());

            for (int i = 0; i < 10; i++) addItem(cwt);
            GC.Collect();
            Assert.Equal(0, enumerable.Count());
        }

        [Fact]
        public static void GetEnumerator_MultipleEnumeratorsReturnSameResults()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object[] keys = Enumerable.Range(0, 33).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, keys.Length).Select(_ => new object()).ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                cwt.Add(keys[i], values[i]);
            }

            using (IEnumerator<KeyValuePair<object, object>> enumerator1 = enumerable.GetEnumerator())
            using (IEnumerator<KeyValuePair<object, object>> enumerator2 = enumerable.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    Assert.True(enumerator2.MoveNext());
                    Assert.Equal(enumerator1.Current, enumerator2.Current);
                }
                Assert.False(enumerator2.MoveNext());
            }

            GC.KeepAlive(keys);
            GC.KeepAlive(values);
        }

        [Fact]
        public static void GetEnumerator_RemovedItems_RemovedFromResults()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object[] keys = Enumerable.Range(0, 33).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, keys.Length).Select(_ => new object()).ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                cwt.Add(keys[i], values[i]);
            }

            for (int i = 0; i < keys.Length; i++)
            {
                Assert.Equal(keys.Length - i, enumerable.Count());
                Assert.Equal(
                    Enumerable.Range(i, keys.Length - i).Select(j => new KeyValuePair<object, object>(keys[j], values[j])),
                    enumerable);
                cwt.Remove(keys[i]);
            }
            Assert.Equal(0, enumerable.Count());

            GC.KeepAlive(keys);
            GC.KeepAlive(values);
        }

        [Fact]
        public static void GetEnumerator_ItemsAddedAfterGetEnumeratorNotIncluded()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object key1 = new object(), key2 = new object(), value1 = new object(), value2 = new object();

            cwt.Add(key1, value1);
            IEnumerator<KeyValuePair<object, object>> enumerator1 = enumerable.GetEnumerator();
            cwt.Add(key2, value2);
            IEnumerator<KeyValuePair<object, object>> enumerator2 = enumerable.GetEnumerator();

            Assert.True(enumerator1.MoveNext());
            Assert.Equal(new KeyValuePair<object, object>(key1, value1), enumerator1.Current);
            Assert.False(enumerator1.MoveNext());

            Assert.True(enumerator2.MoveNext());
            Assert.Equal(new KeyValuePair<object, object>(key1, value1), enumerator2.Current);
            Assert.True(enumerator2.MoveNext());
            Assert.Equal(new KeyValuePair<object, object>(key2, value2), enumerator2.Current);
            Assert.False(enumerator2.MoveNext());

            enumerator1.Dispose();
            enumerator2.Dispose();

            GC.KeepAlive(key1);
            GC.KeepAlive(key2);
            GC.KeepAlive(value1);
            GC.KeepAlive(value2);
        }

        [Fact]
        public static void GetEnumerator_ItemsRemovedAfterGetEnumeratorNotIncluded()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object key1 = new object(), key2 = new object(), value1 = new object(), value2 = new object();

            cwt.Add(key1, value1);
            cwt.Add(key2, value2);
            IEnumerator<KeyValuePair<object, object>> enumerator1 = enumerable.GetEnumerator();
            cwt.Remove(key1);
            IEnumerator<KeyValuePair<object, object>> enumerator2 = enumerable.GetEnumerator();

            Assert.True(enumerator1.MoveNext());
            Assert.Equal(new KeyValuePair<object, object>(key2, value2), enumerator1.Current);
            Assert.False(enumerator1.MoveNext());

            Assert.True(enumerator2.MoveNext());
            Assert.Equal(new KeyValuePair<object, object>(key2, value2), enumerator2.Current);
            Assert.False(enumerator2.MoveNext());

            enumerator1.Dispose();
            enumerator2.Dispose();

            GC.KeepAlive(key1);
            GC.KeepAlive(key2);
            GC.KeepAlive(value1);
            GC.KeepAlive(value2);
        }

        [Fact]
        public static void GetEnumerator_ItemsClearedAfterGetEnumeratorNotIncluded()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object key1 = new object(), key2 = new object(), value1 = new object(), value2 = new object();

            cwt.Add(key1, value1);
            cwt.Add(key2, value2);
            IEnumerator<KeyValuePair<object, object>> enumerator1 = enumerable.GetEnumerator();
            cwt.Clear();
            IEnumerator<KeyValuePair<object, object>> enumerator2 = enumerable.GetEnumerator();

            Assert.False(enumerator1.MoveNext());
            Assert.False(enumerator2.MoveNext());

            enumerator1.Dispose();
            enumerator2.Dispose();

            GC.KeepAlive(key1);
            GC.KeepAlive(key2);
            GC.KeepAlive(value1);
            GC.KeepAlive(value2);
        }

        [Fact]
        public static void GetEnumerator_Current_ThrowsOnInvalidUse()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var enumerable = (IEnumerable<KeyValuePair<object, object>>)cwt;

            object key1 = new object(), value1 = new object();
            cwt.Add(key1, value1);

            using (IEnumerator<KeyValuePair<object, object>> enumerator = enumerable.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current); // before first MoveNext
            }

            GC.KeepAlive(key1);
            GC.KeepAlive(value1);
        }
    }
}
