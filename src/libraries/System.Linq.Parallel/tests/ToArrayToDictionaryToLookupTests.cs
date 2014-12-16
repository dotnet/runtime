// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    public class ToArrayToDictionaryToLookupTests
    {
        //
        // ToArray
        //

        [Fact]
        public static void RunToArrayTests()
        {
            RunToArray(1);
            RunToArray(8);
            RunToArray(1297);
        }

        private static void RunToArray(int size)
        {
            string methodFailed = string.Format("RunToArray(size={0}):  FAILED.  ", size);

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            int[] a = xx.AsParallel().AsOrdered().Select(x => x).ToArray<int>();

            bool passed = (size == a.Length);
            if (!passed)
                Assert.True(false, string.Format(methodFailed + "  > Resulting array size is {0} -- expected {1}", a.Length, size));

            int prev = -1;
            foreach (int x in a)
            {
                if (x != prev + 1)
                {
                    Assert.True(false, string.Format(methodFailed + "  > Missing element in sequence - went from {0} to {1}", prev, x));
                }
                prev = x;
            }
        }

        //
        // ToDictionary
        //

        // Tests ToDictionary<TSource,TKey> with the default equality comparator
        [Fact]
        public static void RunToDictionary1()
        {
            int size = 10000;
            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            Dictionary<int, int> a = xx.AsParallel().ToDictionary<int, int>(v => (2 * v));

            bool passed = (size == a.Count);
            if (!passed)
                Assert.True(false, string.Format("RunToDictionary1:  FAILED.  > Resulting Dictionary size is {0} -- expected {1}", a.Count, size));

            for (int i = 0; i < size; i++)
            {
                if (a[2 * i] != i)
                {
                    Assert.True(false, string.Format("RunToDictionary1:  FAILED.  > Value a[{0}] is {1} -- expected {2}", 2 * i, a[2 * i], i));
                }
            }
        }

        // Tests ToDictionary<TSource,TKey,TElement> with a custom equality comparator
        [Fact]
        public static void RunToDictionary2()
        {
            int size = 1009;
            int p = 7; // GCD(size,p) = 1
            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            Dictionary<int, int> a = xx.AsParallel().ToDictionary<int, int, int>(
                v => ((p * v) % size),
                v => (v + 1),
                new ModularCongruenceComparer(size));

            bool passed = (size == a.Count);
            if (!passed)
                Assert.True(false, string.Format("RunToDictionary2: FAILED.  > Resulting Dictionary size is {0} -- expected {1}", a.Count, size));

            // Since GCD(size,p) = 1, we know that no two elements of xx should be mapped to the same index in a[]
            for (int i = 0; i < size; i++)
            {
                if (a[(p * i) % size] != i + 1)
                {
                    Assert.True(false, string.Format("RunToDictionary2: FAILED.  > Value a[{0}] is {1} -- expected {2}", (p * i) % size, a[(p * i) % size], i + 1));
                }
            }
        }

        //
        // ToLookup
        //
        [Fact]
        public static void RunToLookupOnInput()
        {
            RunToLookupOnInputCore(new string[] { });
            RunToLookupOnInputCore(new string[] { null, null, null });
            RunToLookupOnInputCore(new string[] { "aaa", "aaa", "aaa" });
            RunToLookupOnInputCore(new string[] { "aaa", null, "abb", "abc", null, "aaa" });
        }

        // Tests ToLookup<TSource,TKey> with the default equality comparator
        [Fact]
        public static void RunToLookup1()
        {
            int M = 1000;
            int size = 2 * M;

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            ILookup<int, int> lookup = xx.AsParallel().ToLookup<int, int>(v => (v % M));

            if (M != lookup.Count)
                Assert.True(false, string.Format("RunToLookup1:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", lookup.Count, M));

            // Test this[] on Lookup
            for (int i = 0; i < M; i++)
            {
                int[] vals = lookup[i].ToArray();

                if (vals.Length != 2)
                {
                    Assert.True(false, string.Format("RunToLookup1:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", vals.Length, 2));
                }

                int mn = vals[0] >= vals[1] ? vals[1] : vals[0];
                int mx = vals[0] >= vals[1] ? vals[0] : vals[1];
                if (mn != i || mx != i + M)
                {
                    Assert.True(false, string.Format("RunToLookup1:  FAILED.  > For key {0} got values ({0},{1}) -- expected ({2},{3})", mn, mx, i, i + M));
                }
            }

            // Test GetEnumerator() on Lookup
            HashSet<int> seen = new HashSet<int>();
            foreach (IGrouping<int, int> grouping in lookup)
            {
                if (grouping.Key < 0 || grouping.Key >= M)
                {
                    Assert.True(false, string.Format("RunToLookup1:  FAILED.  > Lookup enumerator returned key {0}, when expected range is [{0},{1})", grouping.Key, 0, M));
                }

                if (seen.Contains(grouping.Key))
                {
                    Assert.True(false, string.Format("RunToLookup1:  FAILED.  > Lookup enumerator returned duplicate (key,value) pairs with the same key = {0}", grouping.Key));
                }
                seen.Add(grouping.Key);
            }

            // Test that invalid key access returns an empty enumerable.
            IEnumerable<int> ie = lookup[-1];
            if (ie.Count() != 0)
            {
                Assert.True(false, string.Format("RunToLookup1:  FAILED.  > Lookup did not return empty sequence for an invalid key"));
            }
        }

        // Tests ToLookup<TSource,TKey,TElement> with a custom equality comparator
        [Fact]
        public static void RunToLookup2()
        {
            int M = 1000; // M is prime
            int size = 2 * M;

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            ILookup<int, int> lookup = xx.AsParallel().ToLookup<int, int, int>(
                v => (v % M),
                v => (v + 1),
                new ModularCongruenceComparer(M));

            if (M != lookup.Count)
                Assert.True(false, string.Format("RunToLookup2:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", lookup.Count, M));

            for (int i = 0; i < M; i++)
            {
                int[] vals = lookup[i].ToArray();

                if (vals.Length != 2)
                {
                    Assert.True(false, string.Format("RunToLookup2:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", vals.Length, 2));
                }

                int mn = vals[0] >= vals[1] ? vals[1] : vals[0];
                int mx = vals[0] >= vals[1] ? vals[0] : vals[1];
                if (mn != 1 + i || mx != 1 + i + M)
                {
                    Assert.True(false, string.Format("RunToLookup2:  FAILED.  > For key {0} got values ({0},{1}) -- expected ({2},{3})", mn, mx, 1 + i, 1 + i + M));
                }
            }
        }

        // Tests ToLookup<TSource,TKey> with the default equality comparator, obtained via passing in null comparator
        [Fact]
        public static void RunToLookup3()
        {
            int M = 1000;
            int size = 2 * M;

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            ILookup<int, int> lookup = xx.AsParallel().ToLookup<int, int>(v => (v % M), null);

            bool passed = (M == lookup.Count);
            if (!(M == lookup.Count))
                Assert.True(false, string.Format("RunToLookup3_nullComparator:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", lookup.Count, M));

            // second variant.
            lookup = xx.AsParallel().ToLookup<int, int, int>(v => (v % M), x => x, null);
            if (M != lookup.Count)
                Assert.True(false, string.Format("RunToLookup3_nullComparator:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", lookup.Count, M));
        }

        private static void RunToLookupOnInputCore(string[] list)
        {
            ILookup<string, string> lookupPlinq = list.AsParallel().Select(i => i).ToLookup<string, string, string>(
                i => i, i => i);
            ILookup<string, string> lookupLinq = Enumerable.ToLookup<string, string, string>(list, i => i, i => i);

            if (lookupPlinq.Count != lookupLinq.Count)
            {
                Assert.True(false, string.Format("RunToLookupOnInput:  FAILED.  > Resulting Lookup size is {0} -- expected {1}", lookupPlinq.Count, list.Length));
            }

            foreach (IGrouping<string, string> grouping in lookupLinq)
            {
                if (!lookupPlinq.Contains(grouping.Key))
                {
                    Assert.True(false, string.Format("RunToLookupOnInput:  FAILED.  > Lookup does not contain key {0}", grouping.Key));
                }

                if (!lookupPlinq[grouping.Key].OrderBy(i => i).SequenceEqual(grouping.OrderBy(i => i)))
                {
                    Assert.True(false, string.Format("RunToLookupOnInput:  FAILED.  > Lookup sequence for key {0} is not correct.", grouping.Key));
                }
            }

            int count = 0;
            foreach (IGrouping<string, string> grouping in lookupPlinq)
            {
                if (!lookupLinq.Contains(grouping.Key))
                {
                    Assert.True(false, string.Format("RunToLookupOnInput:  FAILED.  > Lookup contains extra key {0}", grouping.Key));
                }
                count++;
            }

            if (count != lookupLinq.Count)
            {
                Assert.True(false, string.Format("RunToLookupOnInput:  FAILED.  > Lookup enumerator iterated over {0} elements -- {1} expected", count, lookupLinq.Count));
            }
        }

        // A helper class: a custom IEqualityComparer
        private class ModularCongruenceComparer : IEqualityComparer<int>
        {
            private int _Mod;

            public ModularCongruenceComparer(int mod)
            {
                _Mod = mod;
            }

            private int leastPositiveResidue(int x)
            {
                return ((x % _Mod) + _Mod) % _Mod;
            }

            public bool Equals(int x, int y)
            {
                return leastPositiveResidue(x) == leastPositiveResidue(y);
            }

            public int GetHashCode(int x)
            {
                return leastPositiveResidue(x).GetHashCode();
            }

            public int GetHashCode(object obj)
            {
                return GetHashCode((int)obj);
            }
        }
    }
}
