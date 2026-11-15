// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace System.Collections.Tests
{
    public static class HashSet_NonGeneric_Tests
    {
        [Fact]
        public static void HashSet_CopyConstructor_ShouldWorkWithRandomizedEffectiveComparer()
        {
            HashSet<string> set = CreateCopyWithRandomizedComparer(new HashSet<string>() { "a", "b" });
            Assert.True(set.Contains("a"));

            HashSet<string> copiedSet = new(set);
            Assert.True(copiedSet.Contains("a"));

            static HashSet<string> CreateCopyWithRandomizedComparer(HashSet<string> set)
            {
                // To reproduce the bug, we need a HashSet<string> instance that has fallen back to
                // the randomized comparer. This typically happens when there are many collisions but
                // it can also happen when the set is serialized and deserialized via ISerializable.
                // For consistent results and to avoid brute forcing collisions, use the latter approach.

                SerializationInfo info = new(typeof(HashSet<string>), new FormatterConverter());
                StreamingContext context = new(StreamingContextStates.All);
                set.GetObjectData(info, context);
  
                HashSet<string> copiedSet = (HashSet<string>)Activator.CreateInstance(typeof(HashSet<string>), BindingFlags.NonPublic | BindingFlags.Instance, null, [info, context], null);
                copiedSet.OnDeserialization(null);
                return copiedSet;
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(1000)]
        [InlineData(1000_000)]
        public static void InsertionOpsOnly_Enumeration_PreservesInsertionOrder(int count)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                set.Add(i.ToString());
            }

            int j = 0;
            foreach (string elem in set)
            {
                Assert.Equal(j.ToString(), elem);
                j++;
            }
        }

        #region Clear

        [Fact]
        public static void Clear_AfterRemoval_DoesNotExposeOldItems()
        {
            var comparer = EqualityComparer<int>.Create((x, y) => x == y, x => x % 2);
            var set = new HashSet<int>(comparer) { 1, 2, 3 };
            Assert.True(set.Remove(2));

            set.Clear();
            Assert.Empty(set);

            Assert.True(set.Add(4));
            Assert.True(set.Add(5));
            Assert.Equal(new[] { 4, 5 }, set.OrderBy(item => item));
            foreach (int item in new[] { 4, 5 })
            {
                Assert.True(set.TryGetValue(item, out int actual));
                Assert.Equal(item, actual);
            }
            foreach (int item in new[] { 1, 2, 3 })
            {
                Assert.False(set.Contains(item));
            }
        }

        #endregion
    }
}
