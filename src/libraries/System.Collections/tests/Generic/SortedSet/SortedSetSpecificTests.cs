// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace SortedSetTests
{
    public class SortedSetSpecificTests
    {
        [Fact]
        public static void TestCopyConstructor()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();

            List<int> listOfItems = new List<int>();
            for (int i = 0; i < 10000; i++)
            {
                if (!sortedSet.Contains(i))
                {
                    sortedSet.Add(i);
                    listOfItems.Add(i);
                }
            }

            SortedSet<int> newTree1 = new SortedSet<int>(listOfItems);

            Assert.True(newTree1.SetEquals(listOfItems)); //"Expected to be the same set."

            SortedSet<int> newTree2 = new SortedSet<int>(sortedSet);

            Assert.True(sortedSet.SetEquals(newTree2)); //"Expected to be the same set."

            Assert.Equal(sortedSet.Count, newTree1.Count); //"Should be equal."
            Assert.Equal(sortedSet.Count, newTree2.Count); //"Copied tree not the same as base"
        }

        [Fact]
        public static void TestCopyConstructor2()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();

            List<int> listOfItems = new List<int>();
            int c = 0;
            while (sortedSet.Count < 100000)
            {
                c++;
                if (!sortedSet.Contains(50000 - c))
                {
                    sortedSet.Add(50000 - c);
                    listOfItems.Add(50000 - c);
                }
            }

            SortedSet<int> newTree1 = new SortedSet<int>(listOfItems);

            Assert.True(newTree1.SetEquals(listOfItems)); //"Expected to be the same set."

            SortedSet<int> newTree2 = new SortedSet<int>(sortedSet);

            Assert.True(newTree2.SetEquals(sortedSet)); //"Expected to be the same set."

            IEnumerator<int> t1 = sortedSet.GetEnumerator();
            IEnumerator<int> t2 = newTree1.GetEnumerator();
            IEnumerator<int> t3 = newTree2.GetEnumerator();

            while (t1.MoveNext())
            {
                t2.MoveNext();
                t3.MoveNext();

                Assert.Equal(t1.Current, t2.Current); //"Not fully constructed"
                Assert.Equal(t2.Current, t3.Current); //"Not fullu constructed."
            }

            sortedSet.Clear();
        }

        [Fact]
        public static void TestCopyConstructor3()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();

            List<int> listOfItems = new List<int>();
            int c = 0;
            while (sortedSet.Count < 100000)
            {
                c++;
                if (!sortedSet.Contains(50000 - c))
                {
                    sortedSet.Add(50000 - c);
                    listOfItems.Add(50000 - c);
                }
            }

            SortedSet<int> newTree1 = new SortedSet<int>(listOfItems);

            Assert.True(newTree1.SetEquals(listOfItems)); //"Expected to be the same set."

            SortedSet<int> newTree2 = new SortedSet<int>(sortedSet);

            Assert.True(newTree2.SetEquals(sortedSet)); //"Expected to be the same set."

            SortedSet<int>.Enumerator t1 = sortedSet.GetEnumerator();
            SortedSet<int>.Enumerator t2 = newTree1.GetEnumerator();
            SortedSet<int>.Enumerator t3 = newTree2.GetEnumerator();

            while (t1.MoveNext())
            {
                t2.MoveNext();
                t3.MoveNext();

                Assert.Equal(t1.Current, t2.Current); //"Not fully constructed"
                Assert.Equal(t2.Current, t3.Current); //"Not fullu constructed."
            }

            sortedSet.Clear();
            t1.Dispose();
            t2.Dispose();
            t3.Dispose();
        }

        [Fact]
        public static void TestReverseEnumerator()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();
            sortedSet.Clear();
            for (int j = 5000; j > 0; j--)
            {
                if (!sortedSet.Contains(j))
                    sortedSet.Add(j);
            }

            int[] output = new int[5000];
            sortedSet.CopyTo(output, 0);

            int index = 0;
            IEnumerator<int> e = sortedSet.Reverse().GetEnumerator();

            while (e.MoveNext())
            {
                int recd = e.Current;
                Assert.Equal(recd, output[sortedSet.Count - 1 - index]); //"mismatched reversal"
                index++;
            }
        }

        [Fact]
        public static void TestRemoveWhere()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();
            for (int i = 0; i < 5000; i++)
            {
                sortedSet.Add(i);
            }

            int removed = sortedSet.RemoveWhere(delegate (int x) { if (x < 2500) return true; else return false; });

            Assert.Equal(2500, removed); //"Did not remove according to predicate"
            Assert.Equal(2500, sortedSet.Count); //"Did not remove according to predicate"
        }

        [Fact]
        public static void TestSubSetEnumerator()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();
            for (int i = 0; i < 10000; i++)
            {
                if (!sortedSet.Contains(i))
                    sortedSet.Add(i);
            }
            SortedSet<int> mySubSet = sortedSet.GetViewBetween(45, 90);

            Assert.Equal(46, mySubSet.Count); //"not all elements were encountered"

            IEnumerable<int> en = mySubSet.Reverse();
            Assert.True(mySubSet.SetEquals(en)); //"Expected to be the same set."
        }

        [Fact]
        public static void TestTreeCopyTo1()
        {
            var sortedSet = GetSortedSet();
            int[] arr = new int[sortedSet.Count];
            sortedSet.CopyTo(arr, 0);

            for (int i = 0; i < sortedSet.Count; i++)
                Assert.Equal(arr[i], i); //"Should have equal contents."
        }

        [Fact]
        public static void TestHashCopyTo1()
        {
            var hashSet = GetHashSet();
            int[] arr = new int[hashSet.Count];
            hashSet.CopyTo(arr, 0);

            for (int i = 0; i < hashSet.Count; i++)
            {
                Assert.Equal(arr[i], i); //"Should have equal contents."
            }
        }

        [Fact]
        public static void TestEquals1()
        {
            var sortedSet = GetSortedSet();
            var hashSet = GetHashSet();

            if (!sortedSet.SetEquals(hashSet))
                Assert.True(false); //"Expected: Equal sets"

            if (!hashSet.SetEquals(sortedSet))
                Assert.True(false); //"Expected: Equal sets"
        }

        [Fact]
        public static void TestTreeRemove1()
        {
            var sortedSet = GetSortedSet();
            int a = 10000;
            while (sortedSet.Count > 0)
            {
                sortedSet.Remove(a);
                a--;
            }

            Assert.Equal(-1, a); //"Should have been able to remove 10000 items."
            Assert.Equal(0, sortedSet.Count); //"Should have no items left"
            Assert.True(sortedSet.SetEquals(new int[] { })); //"Should be empty."
        }

        [Fact]
        public static void TestHashRemove1()
        {
            var hashSet = GetHashSet();
            int a = 10000;
            while (hashSet.Count > 0)
            {
                hashSet.Remove(a);
                a--;
            }
            Assert.Equal(-1, a); //"Should have been able to remove 10000 items."
            Assert.Equal(0, hashSet.Count); //"Should have no items left"
            Assert.True(hashSet.SetEquals(new int[] { })); //"Should be empty."
        }

        [Fact]
        public static void TestUnionWithSortedSet()
        {
            var sortedSet = new SortedSet<int>();
            int[] itemsToAdd = new int[] { 5, 13, 8, 11, 5, 1, 12, 9, 14, 4, };
            foreach (var item in itemsToAdd)
                sortedSet.Add(item);

            SortedSet<int> meow = new SortedSet<int>();
            int[] itemsToAdd2 = new int[] { 5, 3, 7, 12, 0 };
            foreach (var item in itemsToAdd2)
                meow.Add(item);

            List<int> expectedUnion = new List<int>();
            foreach (var item in itemsToAdd)
            {
                if (!expectedUnion.Contains(item))
                    expectedUnion.Add(item);
            }
            foreach (var item in itemsToAdd2)
            {
                if (!expectedUnion.Contains(item))
                    expectedUnion.Add(item);
            }

            sortedSet.UnionWith(meow);
            Assert.True(sortedSet.SetEquals(expectedUnion)); //"Expected to be the same set."
        }

        [Fact]
        public static void TestUnionWithHashSet()
        {
            var hashSet = new HashSet<int>();
            int[] itemsToAdd = new int[] { 5, 13, 8, 11, 5, 9, 12, 9, 14, 4, };
            foreach (var item in itemsToAdd)
                hashSet.Add(item);

            HashSet<int> growl = new HashSet<int>();
            int[] itemsToAdd2 = new int[] { 5, 3, 7, 12, 0 };
            foreach (var item in itemsToAdd2)
                growl.Add(item);

            List<int> expectedUnion = new List<int>();
            foreach (var item in itemsToAdd)
            {
                if (!expectedUnion.Contains(item))
                    expectedUnion.Add(item);
            }
            foreach (var item in itemsToAdd2)
            {
                if (!expectedUnion.Contains(item))
                    expectedUnion.Add(item);
            }

            hashSet.UnionWith(growl);
            Assert.True(hashSet.SetEquals(expectedUnion)); //"Expected to be the same set."
        }

        [Fact]
        public static void TestUnionWithCompare()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();
            HashSet<int> hashSet = new HashSet<int>();
            SortedSet<int> dummy1 = new SortedSet<int>();
            SortedSet<int> dummy2 = new SortedSet<int>();
            int[] dummyNums = new int[]
            {
                10, 29, 45, 93, 8, 50, 14, 27, 66, 3, 86, 41, 96, 34, 62,
                30, 50, 14, 82, 61, 85, 46, 94, 84, 33, 98, 82, 9, 56, 11,
                7, 60, 74, 94, 17, 58, 51, 78, 3, 79, 44, 53, 65, 8, 75,
                43, 58, 19, 22, 97, 69, 3, 11, 44, 17, 64, 34, 61, 59, 9,
                67, 5, 71, 55, 96, 25, 66, 1, 16, 4, 78, 72, 11, 3, 16,
                31, 80, 86, 9, 90, 39, 62, 87, 58, 41, 3, 48, 29, 77, 53,
                24, 90, 18, 93, 11, 39, 81, 9, 12, 49,
            };
            int[] setNums = new int[]
            {
                73, 7, 60, 80, 48, 37, 82, 89, 54, 63, 34, 93, 49, 21, 79,
                79, 99, 59, 66, 68, 23, 70, 46, 83, 16, 0, 64, 85, 20, 92,
                40, 8, 43, 89, 47, 19, 67, 36, 77, 25, 49, 53, 84, 38, 18,
                3, 77, 10, 96, 71, 75, 93, 62, 23, 28, 3, 61, 77, 64, 47,
                73, 96, 68, 62, 23, 25, 50, 60, 74, 59, 11, 62, 81, 40, 18,
                69, 85, 7, 64, 12, 23, 54, 89, 49, 16, 46, 46, 20, 60, 43,
                58, 90, 72, 29, 52, 52, 85, 21, 15, 92,
            };
            foreach (var item in dummyNums)
            {
                dummy1.Add(item);
                dummy2.Add(item);
            }
            foreach (var item in setNums)
            {
                sortedSet.Add(item);
                hashSet.Add(item);
            }

            dummy1.UnionWith(sortedSet);
            dummy2.UnionWith(hashSet);
            int[] expectedUnion = new int[]
            {
                10, 29, 45, 93, 8, 50, 14, 27, 66, 3, 86, 41, 96, 34, 62,
                30, 82, 61, 85, 46, 94, 84, 33, 98, 9, 56, 11, 7, 60, 74,
                17, 58, 51, 78, 79, 44, 53, 65, 75, 43, 19, 22, 97, 69, 64,
                59, 67, 5, 71, 55, 25, 1, 16, 4, 72, 31, 80, 90, 39, 87,
                48, 77, 24, 18, 81, 12, 49, 73, 37, 89, 54, 63, 21, 99, 68,
                23, 70, 83, 0, 20, 92, 40, 47, 36, 38, 28, 52, 15,
            };
            Assert.True(dummy1.SetEquals(expectedUnion)); //"Expected to be the same set."
            Assert.True(dummy2.SetEquals(expectedUnion)); //"Expected to be the same set."
        }

        [Fact]
        public static void TestIntersectWithSortedSet()
        {
            var sortedSet = new SortedSet<int>();
            int[] itemsToAdd = new int[] { 5, 13, 8, 11, 5, 1, 12, 9, 14, 4, };
            foreach (var item in itemsToAdd)
                sortedSet.Add(item);

            SortedSet<int> meow = new SortedSet<int>();
            int[] itemsToAdd2 = new int[] { 5, 3, 7, 12, 8 };
            foreach (var item in itemsToAdd2)
                meow.Add(item);

            int[] expectedIntersect = new int[] { 5, 12, 8 };
            sortedSet.IntersectWith(meow);
            Assert.True(sortedSet.SetEquals(expectedIntersect)); //"Expected to be the same set."
        }

        [Fact]
        public static void TestIntersectWithHashSet()
        {
            var hashSet = new HashSet<int>();
            int[] itemsToAdd = new int[] { 5, 13, 8, 11, 5, 9, 12, 9, 14, 4, };
            foreach (var item in itemsToAdd)
                hashSet.Add(item);

            HashSet<int> growl = new HashSet<int>();
            int[] itemsToAdd2 = new int[] { 5, 3, 7, 12, 8 };
            foreach (var item in itemsToAdd2)
                growl.Add(item);

            int[] expectedIntersect = new int[] { 5, 12, 8 };
            hashSet.IntersectWith(growl);

            Assert.True(hashSet.SetEquals(expectedIntersect)); //"Expected to be the same set."
        }

        #region Helper Methods

        private static SortedSet<int> GetSortedSet()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();
            int count = 10000;
            for (int i = 0; i < count; i++)
            {
                if (!sortedSet.Contains(i))
                    sortedSet.Add(i);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.True(sortedSet.Contains(i)); //"Adding did not produce the right result"
            }

            Assert.Equal(count, sortedSet.Count); //"Adding did not produce the right result"
            return sortedSet;
        }

        private static HashSet<int> GetHashSet()
        {
            int count = 10000;
            HashSet<int> hashSet = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                if (!hashSet.Contains(i))
                    hashSet.Add(i);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.True(hashSet.Contains(i), "Should contain value at index: " + i);
            }
            Assert.Equal(count, hashSet.Count); //"Should have the same number of items"

            return hashSet;
        }

        #endregion
    }
}
