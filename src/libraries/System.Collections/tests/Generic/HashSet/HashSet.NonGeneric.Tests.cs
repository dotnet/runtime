// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
    }
}
