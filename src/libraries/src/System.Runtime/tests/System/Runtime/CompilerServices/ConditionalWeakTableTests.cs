// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public partial class ConditionalWeakTableTests
    {
        [Fact]
        public static void InvalidArgs_Throws()
        {
            var cwt = new ConditionalWeakTable<object, object>();

            object ignored;
            Assert.Throws<ArgumentNullException>("key", () => cwt.Add(null, new object())); // null key
            Assert.Throws<ArgumentNullException>("key", () => cwt.TryGetValue(null, out ignored)); // null key
            Assert.Throws<ArgumentNullException>("key", () => cwt.Remove(null)); // null key
            Assert.Throws<ArgumentNullException>("createValueCallback", () => cwt.GetValue(new object(), null)); // null delegate

            object key = new object();
            cwt.Add(key, key);
            Assert.Throws<ArgumentException>(null, () => cwt.Add(key, key)); // duplicate key
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public static void Add(int numObjects)
        {
            // Isolated to ensure we drop all references even in debug builds where lifetime is extended by the JIT to the end of the method
            Func<int, Tuple<ConditionalWeakTable<object, object>, WeakReference[], WeakReference[]>> body = count =>
            {
                object[] keys = Enumerable.Range(0, count).Select(_ => new object()).ToArray();
                object[] values = Enumerable.Range(0, count).Select(_ => new object()).ToArray();
                var cwt = new ConditionalWeakTable<object, object>();

                for (int i = 0; i < count; i++)
                {
                    cwt.Add(keys[i], values[i]);
                }

                for (int i = 0; i < count; i++)
                {
                    object value;
                    Assert.True(cwt.TryGetValue(keys[i], out value));
                    Assert.Same(values[i], value);
                    Assert.Same(value, cwt.GetOrCreateValue(keys[i]));
                    Assert.Same(value, cwt.GetValue(keys[i], _ => new object()));
                }

                return Tuple.Create(cwt, keys.Select(k => new WeakReference(k)).ToArray(), values.Select(v => new WeakReference(v)).ToArray());
            };

            Tuple<ConditionalWeakTable<object, object>, WeakReference[], WeakReference[]> result = body(numObjects);
            GC.Collect();

            Assert.NotNull(result.Item1);

            for (int i = 0; i < numObjects; i++)
            {
                Assert.False(result.Item2[i].IsAlive, $"Expected not to find key #{i}");
                Assert.False(result.Item3[i].IsAlive, $"Expected not to find value #{i}");
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public static void AddMany_ThenRemoveAll(int numObjects)
        {
            object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            var cwt = new ConditionalWeakTable<object, object>();

            for (int i = 0; i < numObjects; i++)
            {
                cwt.Add(keys[i], values[i]);
            }

            for (int i = 0; i < numObjects; i++)
            {
                Assert.Same(values[i], cwt.GetValue(keys[i], _ => new object()));
            }

            for (int i = 0; i < numObjects; i++)
            {
                Assert.True(cwt.Remove(keys[i]));
                Assert.False(cwt.Remove(keys[i]));
            }

            for (int i = 0; i < numObjects; i++)
            {
                object ignored;
                Assert.False(cwt.TryGetValue(keys[i], out ignored));
            }
        }

        [Theory]
        [InlineData(100)]
        public static void AddRemoveIteratively(int numObjects)
        {
            object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            var cwt = new ConditionalWeakTable<object, object>();

            for (int i = 0; i < numObjects; i++)
            {
                cwt.Add(keys[i], values[i]);
                Assert.Same(values[i], cwt.GetValue(keys[i], _ => new object()));
                Assert.True(cwt.Remove(keys[i]));
                Assert.False(cwt.Remove(keys[i]));
            }
        }

        [Fact]
        public static void Concurrent_AddMany_DropReferences() // no asserts, just making nothing throws
        {
            var cwt = new ConditionalWeakTable<object, object>();
            for (int i = 0; i < 10000; i++)
            {
                cwt.Add(i.ToString(), i.ToString());
                if (i % 1000 == 0) GC.Collect();
            }
        }

        [Fact]
        public static void Concurrent_Add_Read_Remove_DifferentObjects()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(0.25);
            Parallel.For(0, Environment.ProcessorCount, i =>
            {
                while (DateTime.UtcNow < end)
                {
                    object key = new object();
                    object value = new object();
                    cwt.Add(key, value);
                    Assert.Same(value, cwt.GetValue(key, _ => new object()));
                    Assert.True(cwt.Remove(key));
                    Assert.False(cwt.Remove(key));
                }
            });
        }

        [Fact]
        public static void Concurrent_GetValue_Read_Remove_DifferentObjects()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(0.25);
            Parallel.For(0, Environment.ProcessorCount, i =>
            {
                while (DateTime.UtcNow < end)
                {
                    object key = new object();
                    object value = new object();
                    Assert.Same(value, cwt.GetValue(key, _ => value));
                    Assert.True(cwt.Remove(key));
                    Assert.False(cwt.Remove(key));
                }
            });
        }

        [Fact]
        public static void Concurrent_GetValue_Read_Remove_SameObject()
        {
            object key = new object();
            object value = new object();

            var cwt = new ConditionalWeakTable<object, object>();
            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(0.25);
            Parallel.For(0, Environment.ProcessorCount, i =>
            {
                while (DateTime.UtcNow < end)
                {
                    Assert.Same(value, cwt.GetValue(key, _ => value));
                    cwt.Remove(key);
                }
            });
        }

        [Fact]
        public static void AddRemove_DropValue()
        {
            var key = new object();
            var value = new object();

            var cwt = new ConditionalWeakTable<object, object>();

            cwt.Add(key, value);
            cwt.Remove(key);

            // Verify that the removed entry is not keeping the value alive
            var wrValue = new WeakReference(value);
            value = null;

            GC.Collect();
            Assert.False(wrValue.IsAlive);

            GC.KeepAlive(cwt);
            GC.KeepAlive(key);
        }

        [Fact]
        public static void GetOrCreateValue()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var key = new object();
            object obj = null;

            object value = cwt.GetOrCreateValue(key);

            Assert.True(cwt.TryGetValue(key, out value));
            Assert.Equal(value, cwt.GetValue(key, k => new object()));

            var wrValue = new WeakReference<object>(value, false);
            var wrkey = new WeakReference<object>(key, false);
            key = null;
            value = null;

            GC.Collect();

            // key and value must be collected
            Assert.False(wrValue.TryGetTarget(out obj));
            Assert.False(wrkey.TryGetTarget(out obj));
        }

        [Fact]
        public static void GetValue()
        {
            var cwt = new ConditionalWeakTable<object, object>();
            var key = new object();
            object obj = null;

            object value = cwt.GetValue(key, k => new object());

            Assert.True(cwt.TryGetValue(key, out value));
            Assert.Equal(value, cwt.GetOrCreateValue(key));

            WeakReference<object> wrValue = new WeakReference<object>(value, false);
            WeakReference<object> wrkey = new WeakReference<object>(key, false);
            key = null;
            value = null;

            GC.Collect();

            // key and value must be collected
            Assert.False(wrValue.TryGetTarget(out obj));
            Assert.False(wrkey.TryGetTarget(out obj));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void Clear_AllValuesRemoved(int numObjects)
        {
            var cwt = new ConditionalWeakTable<object, object>();

            MethodInfo clear = cwt.GetType().GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clear == null)
            {
                // Couldn't access the Clear method; skip the test.
                return;
            }

            object[] keys = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();
            object[] values = Enumerable.Range(0, numObjects).Select(_ => new object()).ToArray();

            for (int iter = 0; iter < 2; iter++)
            {
                // Add the objects
                for (int i = 0; i < numObjects; i++)
                {
                    cwt.Add(keys[i], values[i]);
                    Assert.Same(values[i], cwt.GetValue(keys[i], _ => new object()));
                }

                // Clear the table
                clear.Invoke(cwt, null);

                // Verify the objects are removed
                for (int i = 0; i < numObjects; i++)
                {
                    object ignored;
                    Assert.False(cwt.TryGetValue(keys[i], out ignored));
                }

                // Do it a couple of times, to make sure the table is still usable after a clear.
            }
        }
    }
}
