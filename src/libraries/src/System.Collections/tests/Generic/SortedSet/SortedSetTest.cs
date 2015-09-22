// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SortedSetTests
{
    public class SortedSetTest
    {
        #region Helper Methods

        private static void ContainsExpected(string testName, SortedSet<int> theSet1, int[] expectedArray, int expectedCount)
        {
            SortedSet<int> expected = new SortedSet<int>(expectedArray);
            Assert.Equal(expectedCount, theSet1.Count); //"FAILED:  " + testName + ". Expected both lengths be the same."
            Assert.True(theSet1.SetEquals(expected)); //"FAILED:  " + testName + ". Both sets are not equal to each other."
        }

        private static void ContainsExpected(string testName, SortedSet<DummyClass> theSet1, DummyClass[] expectedArray, int expectedCount)
        {
            SortedSet<DummyClass> expected = new SortedSet<DummyClass>(expectedArray);
            Assert.Equal(expectedCount, theSet1.Count); //"FAILED:  " + testName + ". Expected both lengths be the same."
            Assert.True(theSet1.SetEquals(expected)); //"FAILED:  " + testName + ". Both sets are not equal to each other."
        }

        #endregion

        #region Regression tests / these were once bugs...make sure they don't break again

        [Fact]
        public static void TestEmptyCtor()
        {
            SortedSet<int> SortedSet;
            SortedSet = new SortedSet<int>(new int[0]);
        }

        [Fact]
        public static void TestCopyToEmpty()
        {
            SortedSet<int> set = new SortedSet<int>();
            int[] array = new int[0];
            set.CopyTo(array);
            set.CopyTo(array, 0);
            set.CopyTo(array, 0, 0);
        }

        [Fact]
        public static void TestSortedSetCountAssumptions()
        {
            SortedSet<int> set1 = new SortedSet<int>(new IntModEqualityComparer(5));
            SortedSet<int> set2 = new SortedSet<int>();

            //Symmetric Except With
            set1.Add(5);
            set2.Add(5);
            set2.Add(10);
            set1.SymmetricExceptWith(set2);
            Assert.Equal(0, set1.Count);
            set1.Clear();
            set2.Clear();

            //IsSubSet
            set1.Add(5);
            set1.Add(7);
            set2.Add(5);
            set2.Add(10);
            Assert.False(set1.IsSubsetOf(set2));
            set1.Clear();
            set2.Clear();

            //IsProperSubSet
            set1.Add(5);
            set1.Add(7);
            set2.Add(5);
            set2.Add(10);
            set2.Add(11);
            Assert.False(set1.IsProperSubsetOf(set2));

            set1.Clear();
            set2.Clear();

            //IsSuperSet
            set1.Add(5);
            set2.Add(5);
            set2.Add(10);
            Assert.True(set1.IsSupersetOf(set2));
            set1.Clear();
            set2.Clear();


            //IsProperSuperSet
            set1.Add(5);
            set1.Add(6);
            set2.Add(5);
            set2.Add(10);
            Assert.True(set1.IsProperSupersetOf(set2));
            set1.Clear();
            set2.Clear();

            //SetEquals (1)
            set1.Add(5);
            set2.Add(5);
            set2.Add(10);
            Assert.True(set1.SetEquals(set2));
            set1.Clear();
            set2.Clear();

            //SetEquals (2)
            set1.Add(5);
            set1.Add(7);
            set2.Add(5);
            set2.Add(10);
            Assert.False(set1.SetEquals(set2));

            set1.Clear();
            set2.Clear();
        }

        [Fact]
        public static void TestSymmetricExceptThrow()
        {
            SortedSet<int> SortedSet = new SortedSet<int>(new int[] { 3, 4, 5 });
            List<int> list = new List<int>(new int[] { 1, 2, 3, 4, 5 });
            SortedSet.SymmetricExceptWith(list);
        }

        [Fact]
        public static void TestFragmentedTrimExcess()
        {
            SortedSet<int> theSet = new SortedSet<int>();
            List<int> expectedList = new List<int>();
            for (int i = 0; i < 11; i++)
            {
                theSet.Add(i);
                if (i != 4)
                    expectedList.Add(i);
            }
            theSet.Remove(4);
            //theSet.TrimExcess();

            string testName = "TestFragmentedTrimExcess";
            int[] expectedArray = expectedList.ToArray();
            int expectedCount = 10;
            ContainsExpected(testName, theSet, expectedArray, expectedCount);
        }

        private static SortedSet<int> s_sortedSet;

        private static bool s_foundZero = false;

        [Fact]
        public static void TestRemoveWhereZero()
        {
            int[] elements = new int[] { -4, -3, -2, -1, 0, 1, 2, 3, 4 };

            s_sortedSet = new SortedSet<int>(elements);
            Assert.Equal(0, s_sortedSet.RemoveWhere(RemoveIt));
            Assert.True(s_foundZero);
        }

        private static bool RemoveIt(int i)
        {
            if (i == 0)
            {
                s_foundZero = true;
                s_sortedSet.Clear();
            }
            s_sortedSet.Remove(i);
            return true;
        }

        [Fact]
        public static void TestTrimExcessPostCondition()
        {
            SortedSet<int> set = new SortedSet<int>();
            set.Add(3);
            set.Add(4);
            set.Clear();
            //set.TrimExcess();

            bool result = set.Contains(3);
            result = set.Overlaps(new int[] { 7, 8 });
            result = set.IsProperSubsetOf(new int[] { 7, 8 });
            result = set.IsSubsetOf(new int[] { 7, 8 });
            result = set.IsProperSupersetOf(new int[] { 7, 8 });
            result = set.IsSupersetOf(new int[] { 7, 8 });
            set.IntersectWith(new int[] { 7, 8 });
            set.UnionWith(new int[] { 7, 8 });

            set.Clear();
            //set.TrimExcess();
            set.SymmetricExceptWith(new int[] { 7, 8 });

            set.Clear();
            //set.TrimExcess();
            set.ExceptWith(new int[] { 7, 8 });

            set.Clear();
            //set.TrimExcess();
            set.Remove(4);

            set.Clear();
            //set.TrimExcess();
            set.Add(9);

            set.Clear();
            //set.TrimExcess();
            set.Clear();

            Assert.Equal(0, set.Count);
        }

        [Fact]
        public static void TestSetEqualsThrows()
        {
            SortedSet<ValueItem> SortedSet = new SortedSet<ValueItem>();
            SortedSet.Add(new ValueItem(34));
            Assert.False(SortedSet.SetEquals(new ValueItem(-43)));

            SortedSet.Add(new ValueItem(56));
            SortedSet.Add(new ValueItem(-12));
            SortedSet.Add(new ValueItem(3));

            HashSet<ValueItem> HashSet = new HashSet<ValueItem>(SortedSet);

            Assert.True(SortedSet.SetEquals(HashSet));

            IList<ValueItem> List = new List<ValueItem>(HashSet);

            Assert.True(SortedSet.SetEquals(List));
        }

        // the following empty set tests check whether set throws
        [Fact]
        public static void TestContainsEmptySet()
        {
            SortedSet<int> SortedSet = new SortedSet<int>();
            Assert.False(SortedSet.Contains(3));
        }

        [Fact]
        public static void TestProperSupersetEmptySet()
        {
            List<int> list = new List<int>();
            list.Add(3);
            SortedSet<int> SortedSet = new SortedSet<int>();
            Assert.False(SortedSet.IsProperSupersetOf(list));
        }

        [Fact]
        public static void TestSupersetEmptySet()
        {
            List<int> list = new List<int>();
            list.Add(3);
            SortedSet<int> SortedSet = new SortedSet<int>();
            Assert.False(SortedSet.IsSupersetOf(list));
        }

        [Fact]
        public static void TestProperSubsetEmptySet()
        {
            List<int> list = new List<int>();
            list.Add(3);
            SortedSet<int> SortedSet = new SortedSet<int>();
            Assert.True(SortedSet.IsProperSubsetOf(list));
        }

        [Fact]
        public static void TestSymmetricExceptEmptySet()
        {
            List<int> list = new List<int>();
            list.Add(3);
            SortedSet<int> SortedSet = new SortedSet<int>();
            SortedSet.SymmetricExceptWith(list);
        }

        [Fact]
        public static void TestSubsetEmptySet()
        {
            List<int> list = new List<int>();
            list.Add(3);
            SortedSet<int> SortedSet = new SortedSet<int>();
            Assert.True(SortedSet.IsSubsetOf(list));
        }

        [Fact]
        public static void TestSetEqualsEmptySet()
        {
            List<int> list = new List<int>();
            list.Add(3);
            SortedSet<int> SortedSet = new SortedSet<int>();
            Assert.False(SortedSet.SetEquals(list));
        }

        [Fact]
        public static void TestCopyToWithSetOfSet_Int()
        {
            SortedSet<int> junk_set = new SortedSet<int>(new int[] { 0 });
            SortedSet<int> empty_set = new SortedSet<int>();

            List<SortedSet<int>> list_of_empty_set = new List<SortedSet<int>>();
            list_of_empty_set.Add(empty_set);

            HashSet<SortedSet<int>> set_of_sets = new HashSet<SortedSet<int>>();
            set_of_sets.Add(junk_set);
            set_of_sets.Add(empty_set);

            set_of_sets.IntersectWith(list_of_empty_set);

            set_of_sets.CopyTo(new SortedSet<int>[1], 0, 1);
        }

        [Fact]
        public static void TestUnionWithEmpty_Int()
        {
            SortedSet<int> SortedSet, SortedSet2;

            SortedSet = new SortedSet<int>();
            SortedSet2 = new SortedSet<int>(new int[] { -123, -12, -1, 0, 1, 12, 123 });
            SortedSet.UnionWith(SortedSet2);
            Assert.Equal(7, SortedSet.Count);
            Assert.True(SortedSet.SetEquals(SortedSet2));
        }

        [Fact]
        public static void TestUnionWithEmpty_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");
            DummyClass c7 = new DummyClass("c7");

            SortedSet<DummyClass> SortedSet, SortedSet2;

            SortedSet = new SortedSet<DummyClass>();
            SortedSet2 = new SortedSet<DummyClass>(new DummyClass[] { c1, c2, c3, c4, c5, c6, c7 });
            SortedSet.UnionWith(SortedSet2);
            Assert.Equal(7, SortedSet.Count);

            Assert.True(SortedSet.SetEquals(SortedSet2));
        }

        [Fact]
        public static void TestUnionWithSelf()
        {
            SortedSet<int> SortedSet;

            SortedSet = new SortedSet<int>();
            SortedSet.UnionWith(new int[] { -123, -12, -1, 0, 1, 12, 123 });
            int count = SortedSet.Count;

            SortedSet.UnionWith(SortedSet);
            Assert.Equal(count, SortedSet.Count);
        }

        [Fact]
        public static void TestExceptWithSelf()
        {
            SortedSet<int> SortedSet;

            SortedSet = new SortedSet<int>();
            SortedSet.UnionWith(new int[] { -123, -12, -1, 0, 1, 12, 123 });

            SortedSet.ExceptWith(SortedSet);
            Assert.Equal(0, SortedSet.Count);
        }

        [Fact]
        public static void TestSymmetricExceptWithSelf()
        {
            SortedSet<int> SortedSet;

            SortedSet = new SortedSet<int>();
            SortedSet.UnionWith(new int[] { -123, -12, -1, 0, 1, 12, 123 });

            SortedSet.SymmetricExceptWith(SortedSet);
            Assert.Equal(0, SortedSet.Count);
        }

        [Fact]
        public static void TestIsProperSubsetOfEmptySet()
        {
            SortedSet<int> SortedSet;

            SortedSet = new SortedSet<int>();
            int[] empty = new int[] { };

            Assert.False(SortedSet.IsProperSubsetOf(empty));
        }

        [Fact]
        public static void TestSymmetricExceptEC()
        {
            List<int> list = new List<int>();
            list.Add(-5);
            list.Add(5);

            SortedSet<int> SortedSet = new SortedSet<int>(new IntAbsComparer());
            SortedSet.Add(-7);
            SortedSet.SymmetricExceptWith(list);
            Assert.Equal(2, SortedSet.Count);
        }

        [Fact]
        public static void TestIntersectEC()
        {
            SortedSet<int> other = new SortedSet<int>(new int[] { -5, 7 });

            SortedSet<int> SortedSet = new SortedSet<int>(new IntAbsComparer());
            SortedSet.Add(-7);
            SortedSet.Add(5);
            SortedSet.IntersectWith(other);
            Assert.Equal(2, SortedSet.Count);
        }

        [Fact]
        public static void TestSubsetEC()
        {
            SortedSet<int> other = new SortedSet<int>(new int[] { -5, 7 });

            SortedSet<int> SortedSet = new SortedSet<int>(new IntAbsComparer());
            SortedSet.Add(5);
            SortedSet.Add(7);
            Assert.True(SortedSet.IsSubsetOf(other));
        }

        [Fact]
        public static void TestProperSubsetEC()
        {
            SortedSet<int> other = new SortedSet<int>(new int[] { -5, 7 });

            SortedSet<int> SortedSet = new SortedSet<int>(new IntAbsComparer());
            SortedSet.Add(5);
            Assert.True(SortedSet.IsSubsetOf(other));
        }

        private class IntAbsComparer : IEqualityComparer<int>, IComparer<int>
        {
            public IntAbsComparer()
            {
            }
            public bool Equals(int x, int y)
            {
                return (Abs(x) == Abs(y));
            }

            public int GetHashCode(int x)
            {
                return Abs(x);
            }

            public int Compare(int x, int y)
            {
                return Abs(x) - Abs(y);
            }
        }

        private class ModComparer : IEqualityComparer<int>, IComparer<int>
        {
            public ModComparer()
            {
            }
            public bool Equals(int x, int y)
            {
                long div1, div2;
                long rem1, rem2;
                div1 = DivRem(Abs(x), 7, out rem1);
                div2 = DivRem(Abs(y), 7, out rem2);
                return rem1 == rem2;
            }

            public int GetHashCode(int x)
            {
                long div1;
                long rem1;
                div1 = DivRem(x, 7, out rem1);
                return (int)rem1;
            }

            public int Compare(int x, int y)
            {
                long div1, div2;
                long rem1, rem2;
                div1 = DivRem(Abs(x), 7, out rem1);
                div2 = DivRem(Abs(y), 7, out rem2);
                if (rem1 == rem2)
                    return 0;
                if (rem1 > rem2)
                    return 1;
                else
                    return -1;
            }
        }

        private static int DivRem(int number, int div, out long remainder)
        {
            remainder = number % div;
            return (int)(number / div);
        }

        private class OddEvenComparer : IEqualityComparer<int>, IComparer<int>
        {
            public OddEvenComparer()
            {
            }
            public bool Equals(int x, int y)
            {
                return (x & 1) == (y & 1);
            }

            public int GetHashCode(int x)
            {
                return (x & 1);
            }

            public int Compare(int x, int y)
            {
                return (x & 1) - (y & 1);
            }
        }

        #endregion

        #region SortedSet of int tests

        [Fact]
        public static void TestReverse_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();
            int[] numsToAdd = new int[] { 6, 4, 8, 6, 7, 7, 1, 7, 8, 8, };
            foreach (var num in numsToAdd)
                theSet.Add(num);

            int[] expected = new int[] { 1, 4, 6, 7, 8 };
            int index = expected.Length - 1;
            foreach (int val in theSet.Reverse())
            {
                Assert.Equal(expected[index], val); //"Expected them to be equal."
                index--;
            }
        }

        [Fact]
        public static void TestTrimExcess_Int()
        {
            // make sure count remains the same after TrimExcess is called

            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(1);
            theSet.Add(2);
            theSet.Add(3);

            //theSet.TrimExcess();
            Assert.Equal(3, theSet.Count);

            theSet = new SortedSet<int>();
            for (int i = 0; i < 200; i++)
            {
                theSet.Add(i);
            }

            Assert.Equal(200, theSet.Count);
        }

        [Fact]
        public static void TestOverlaps_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(1);
            theSet.Add(2);
            theSet.Add(3);

            Assert.True(theSet.Overlaps(new int[] { 1, 2, 3 }));
            Assert.True(theSet.Overlaps(new int[] { 2 }));
            Assert.False(theSet.Overlaps(new int[] { 4, 8, 9 }));
        }

        [Fact]
        public static void TestSetsOfSortedSets_Int()
        {
            SortedSet<int> set1 = new SortedSet<int>();
            set1.Add(1);
            set1.Add(2);
            set1.Add(3);
            SortedSet<int> set11 = new SortedSet<int>();
            set11.Add(4);
            set11.Add(5);
            set11.Add(6);

            IComparer<SortedSet<int>> myEQ = new SortedSetComparer<int>();
            SortedSet<SortedSet<int>> setOfSets1 = new SortedSet<SortedSet<int>>(myEQ);

            setOfSets1.Add(set1);
            setOfSets1.Add(set11);

            SortedSet<SortedSet<int>> setOfSets2 = new SortedSet<SortedSet<int>>(myEQ);
            SortedSet<int> set2 = new SortedSet<int>();
            set2.Add(1);
            set2.Add(2);
            set2.Add(3);
            SortedSet<int> set22 = new SortedSet<int>();
            set22.Add(4);
            set22.Add(5);
            set22.Add(6);

            setOfSets2.Add(set2);
            setOfSets2.Add(set22);

            Assert.True(setOfSets1.IsSubsetOf(setOfSets2));
            Assert.False(setOfSets1.IsProperSubsetOf(setOfSets2));
            Assert.True(setOfSets1.SetEquals(setOfSets2));
        }

        [Fact]
        public static void TestCtors_Int()
        {
            SortedSet<int> set1 = new SortedSet<int>();
            set1.Add(9);
            set1.Add(853);
            set1.Add(111222333);
            set1.Add(-1);
            set1.Add(173);
            set1.Add(653);
            set1.Add(2);
            set1.Add(5);

            Assert.Equal(8, set1.Count);
            SortedSet<int> set2 = new SortedSet<int>(set1);
            Assert.Equal(8, set2.Count);
            Assert.True(set2.SetEquals(set1));
            Assert.True(set1.SetEquals(set2));
        }

        [Fact]
        public static void TestCtors_Int_IComparer()
        {
            var comparer = new IntAbsComparer();
            SortedSet<int> sortedSet = new SortedSet<int>(new int[] { -5, 7, 5, 10, -10 }, comparer);
            Assert.Equal(3, sortedSet.Count); //"Expect them to be the same."
            Assert.All(new[] { 5, 7, 10 }, item => Assert.Contains(item, sortedSet, comparer));

            Assert.True(sortedSet.Add(-13)); //"Should be able to add item not in set."
            Assert.False(sortedSet.Add(13)); //"Should not be able to add item in set."
        }

        [Fact]
        public static void TestMaxMin()
        {
            // Uses the default comparer to sort the list.
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(15);
            theSet1.Add(15);
            theSet1.Add(42);
            theSet1.Add(666);
            theSet1.Add(1);

            Assert.Equal(1, theSet1.Min); //"Expected the right min"
            Assert.Equal(777777, theSet1.Max); //"Expected the right max"

            theSet1.Add(-15);
            Assert.Equal(-15, theSet1.Min); //"Expected the right min"

            theSet1.Add(777777 + 1);
            Assert.Equal(777778, theSet1.Max); //"Expected the right max"

            // Tests that it uses the given Comparer to sort the list to find Max/Min.
            var comparer = new IntAbsComparer();
            theSet1 = new SortedSet<int>(comparer);
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(-15);
            theSet1.Add(-42);
            theSet1.Add(28);
            theSet1.Add(42);
            theSet1.Add(1);
            theSet1.Add(-40);
            theSet1.Add(-777778);

            int[] expectedOrder = new int[] { 1, -15, 28, -40, -42, 666, 777777, -777778 };
            Assert.Equal(expectedOrder.Length, theSet1.Count); //"Expected to be the same size."
            //Checking that the items in the set are sorted correctly using the Comparer given.
            Assert.Equal(expectedOrder, theSet1);

            Assert.Equal(1, theSet1.Min); //"Expected the correct min."
            Assert.Equal(-777778, theSet1.Max); //"Expected the correct Max."

            theSet1.Add(0);
            Assert.Equal(0, theSet1.Min); //"Expected the correct min."

            theSet1.Add(777780);
            Assert.Equal(777780, theSet1.Max); //"Expected the right max."
        }

        [Fact]
        public static void TestAddAndRemove_Int()
        {
            // "expected" is used to compare 
            List<int> expected = new List<int>();
            expected.Add(1);
            expected.Add(2);
            expected.Add(3);

            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(1);
            theSet.Add(2);
            theSet.Add(3);
            Assert.True(theSet.SetEquals(expected));

            theSet.Remove(2);
            expected.Remove(2);
            // check
            Assert.True(theSet.SetEquals(expected));

            theSet.Add(2);
            expected.Add(2);
            // check
            Assert.True(theSet.SetEquals(expected));
        }

        [Fact]
        public static void TestDuplicateAdds_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            theSet.Add(33);
            theSet.Add(-44);
            theSet.Add(234);

            Assert.False(theSet.Add(-44));

            SortedSet<int> theSet2 = new SortedSet<int>();

            theSet2.Add(33);
            theSet2.Add(-44);
            theSet2.Add(234);

            Assert.True(theSet.SetEquals(theSet2));
        }

        [Fact]
        public static void TestClear_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            // add some elements
            theSet.Add(15);
            theSet.Add(20);
            theSet.Add(30);

            theSet.Clear();
            Assert.Equal(0, theSet.Count);
        }

        [Fact]
        public static void TestCount_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            // add some elements
            theSet.Add(60);
            theSet.Add(666);
            theSet.Add(-17);

            Assert.Equal(3, theSet.Count);

            theSet.Clear();
            Assert.Equal(0, theSet.Count);
        }

        [Fact]
        public static void TestCopyToSimple_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            // add some elements
            theSet.Add(15);
            theSet.Add(777777);
            theSet.Add(666);
            theSet.Remove(15);
            theSet.Remove(666);
            theSet.Add(890);
            theSet.Add(666);

            int[] copyToArray = new int[3];

            theSet.CopyTo(copyToArray);

            SortedSet<int> expected = new SortedSet<int>(new int[] { 666, 890, 777777 });
            Assert.True(expected.SetEquals(copyToArray));
        }

        [Fact]
        public static void TestCopyToWithStartIndex_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            // add some elements
            theSet.Add(15);
            theSet.Add(777777);
            theSet.Add(666);
            theSet.Add(890);

            int[] copyToArray = new int[5];

            theSet.CopyTo(copyToArray, 1);

            // should have 0 (default int) in first element
            int[] expectedArray = { 0, 15, 777777, 666, 890 };

            SortedSet<int> expected = new SortedSet<int>(expectedArray);
            Assert.True(expected.SetEquals(expectedArray));
            Assert.Equal(0, copyToArray[0]);
        }

        [Fact]
        public static void TestCopyToErrors_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            // add some elements
            theSet.Add(15);
            theSet.Add(777777);
            theSet.Add(666);
            theSet.Add(890);
            theSet.Add(-17);

            int[] copyToArray = new int[5];

            Assert.Throws<ArgumentException>(() => theSet.CopyTo(copyToArray, 1)); //" should have thrown exception!"
            Assert.Throws<ArgumentException>(() => theSet.CopyTo(copyToArray, 10, 10)); //"should have thrown exception!"
        }

        [Fact]
        public static void TestRemoveWhere_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();

            // add some elements
            theSet.Add(60);
            theSet.Add(666);
            theSet.Add(-17);

            Assert.Equal(1, theSet.RemoveWhere(i => { return i > 100; }));
            string testName = "TestRemoveWhere_Int 2";
            int[] expectedArray = { 60, -17 };
            int expectedCount = 2;
            ContainsExpected(testName, theSet, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestSuperset_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(15);
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(42);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(666);
            theSet2.Add(15);
            Assert.True(theSet1.IsSupersetOf(theSet2));

            SortedSet<int> theSet3 = new SortedSet<int>();
            theSet3.Add(666);
            theSet3.Add(17);

            Assert.False(theSet1.IsSupersetOf(theSet3));
        }

        [Fact]
        public static void TestProperSuperset_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(15);
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(42);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(666);
            theSet2.Add(15);

            Assert.True(theSet1.IsProperSupersetOf(theSet2));

            SortedSet<int> theSet3 = new SortedSet<int>();
            theSet3.Add(666);
            theSet3.Add(17);

            Assert.False(theSet1.IsProperSupersetOf(theSet3));

            SortedSet<int> theSet4 = new SortedSet<int>();
            theSet4.Add(15);
            theSet4.Add(666);
            theSet4.Add(777777);
            theSet4.Add(42);

            Assert.False(theSet1.IsProperSupersetOf(theSet4));
        }

        [Fact]
        public static void TestProperSupersetWithEnumerable_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(15);
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(42);

            List<int> theList = new List<int>();
            theList.Add(666);
            theList.Add(15);
            theList.Add(15);

            Assert.True(theSet1.IsProperSupersetOf(theList));

            theList.Add(42);
            theList.Add(42);
            Assert.True(theSet1.IsProperSupersetOf(theList));

            theList.Add(777777);
            Assert.False(theSet1.IsProperSupersetOf(theList));

            theList.Add(2342325);
            theList.Add(352);
            Assert.False(theSet1.IsProperSupersetOf(theList));
        }

        [Fact]
        public static void TestEquality_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(15);
            theSet1.Add(666);
            theSet1.Add(777);
            theSet1.Add(42);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(15);
            theSet2.Add(42);
            theSet2.Add(666);
            theSet2.Add(777);

            Assert.True(theSet1.SetEquals(theSet2));

            List<int> theList = new List<int>();
            theList.Add(15);
            theList.Add(666);
            theList.Add(777);
            theList.Add(42);

            Assert.True(theSet1.SetEquals(theList));
        }

        [Fact]
        public static void TestUnion_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(15);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(15);
            theSet2.Add(42);
            theSet2.Add(666);

            theSet1.UnionWith(theSet2);

            int[] expectedArray = { 15, 42, 666, 777777 };
            int expectedCount = 4;
            string testName = "TestUnion_Int";

            ContainsExpected(testName, theSet1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestIntersection_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(15);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(15);
            theSet2.Add(42);
            theSet2.Add(666);

            theSet1.IntersectWith(theSet2);

            int[] expectedArray = { 15, 666 };
            int expectedCount = 2;

            ContainsExpected("TestIntersection_Int", theSet1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestExcept_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(15);
            theSet1.Add(-234);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(15);
            theSet2.Add(42);
            theSet2.Add(666);

            theSet1.ExceptWith(theSet2);

            SortedSet<int> expected = new SortedSet<int>();
            expected.Add(777777);
            expected.Add(-234);

            Assert.True(theSet1.SetEquals(expected));

            string testName = "TestExcept_Int 2";
            int expectedCount = 2;
            int[] expectedArray = { 777777, -234 };
            ContainsExpected(testName, theSet1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestSymmetricExcept_Int()
        {
            SortedSet<int> set1 = new SortedSet<int>();
            set1.Add(4);
            set1.Add(7);
            set1.Add(-5);
            set1.Add(54);

            SortedSet<int> set2 = new SortedSet<int>();
            set2.Add(92);
            set2.Add(7);
            set2.Add(-5);
            set2.Add(293);

            set1.SymmetricExceptWith(set2);
            // set1 should now have 4, 54, 92, 293
            Assert.Equal(4, set1.Count);

            // make sure set2 count didn't change, don't want to modify it.
            Assert.Equal(4, set2.Count);

            string testName = "TestSymmetricExcept_Int 3";
            int expectedCount = 4;
            int[] expectedArray = { 4, 54, 92, 293 };
            ContainsExpected(testName, set1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestSymmetricWithEnumerable_Int()
        {
            SortedSet<int> set1 = new SortedSet<int>();
            set1.Add(4);
            set1.Add(7);
            set1.Add(-5);
            set1.Add(54);

            List<int> theList = new List<int>();
            theList.Add(8);
            theList.Add(8);
            theList.Add(7);
            theList.Add(54);
            theList.Add(54);
            theList.Add(7);
            theList.Add(8);

            set1.SymmetricExceptWith(theList);
            // set1 should now have 4, 8, -5
            int[] expectedArray = { 4, 8, -5 };
            int expectedCount = 3;
            string testName = "TestSymmetricWithEnumerable_Int 1";

            ContainsExpected(testName, set1, expectedArray, expectedCount);

            SortedSet<int> set2 = new SortedSet<int>();
            set2.Add(4);
            set2.Add(7);
            set2.Add(-5);
            set2.Add(54);

            theList.Clear();
            theList.Add(4);
            theList.Add(7);
            theList.Add(54);
            theList.Add(-5);
            theList.Add(7);

            set2.SymmetricExceptWith(theList);
            Assert.Equal(0, set2.Count);

            SortedSet<int> set3 = new SortedSet<int>();
            set3.Add(4);
            set3.Add(7);
            set3.Add(-5);
            set3.Add(54);

            theList.Clear();
            theList.Add(7);
            theList.Add(54);
            theList.Add(-5);
            theList.Add(7);

            set3.SymmetricExceptWith(theList);

            int[] expectedArray2 = { 4 };
            int expectedCount2 = 1;
            string testName2 = "TestSymmetricWithEnumerable_Int 2";

            ContainsExpected(testName2, set3, expectedArray2, expectedCount2);
        }

        [Fact]
        public static void TestSubset_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(15);
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(42);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(666);
            theSet2.Add(15);

            Assert.True(theSet2.IsSubsetOf(theSet1));

            SortedSet<int> theSet3 = new SortedSet<int>();
            theSet3.Add(666);
            theSet3.Add(17);

            Assert.False(theSet3.IsSubsetOf(theSet1));
        }

        [Fact]
        public static void TestSubsetWithEnumerable_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(15);
            theSet.Add(666);

            List<int> theList = new List<int>();
            theList.Add(666);
            theList.Add(15);
            theList.Add(777777);
            theList.Add(42);

            Assert.True(theSet.IsSubsetOf(theList));

            List<int> theList2 = new List<int>();
            theList.Add(222);
            theList.Add(15);
            theList.Add(777777);
            theList.Add(42);

            Assert.False(theSet.IsSubsetOf(theList2));
        }

        [Fact]
        public static void TestProperSubset_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            theSet1.Add(15);
            theSet1.Add(666);
            theSet1.Add(777777);
            theSet1.Add(42);

            SortedSet<int> theSet2 = new SortedSet<int>();
            theSet2.Add(666);
            theSet2.Add(15);

            Assert.True(theSet2.IsProperSubsetOf(theSet1));

            SortedSet<int> theSet3 = new SortedSet<int>();
            theSet3.Add(666);
            theSet3.Add(17);

            Assert.False(theSet3.IsProperSubsetOf(theSet1));

            SortedSet<int> theSet4 = new SortedSet<int>();
            theSet4.Add(15);
            theSet4.Add(666);
            theSet4.Add(777777);
            theSet4.Add(42);

            Assert.False(theSet4.IsProperSubsetOf(theSet1));
        }

        [Fact]
        public static void TestProperSubsetWithEnumerable_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(15);
            theSet.Add(666);
            theSet.Add(777777);
            theSet.Add(42);

            List<int> theList = new List<int>();
            theList.Add(666);
            theList.Add(15);
            theList.Add(777777);
            theList.Add(42);
            theList.Add(42);    // duplicate

            Assert.False(theSet.IsProperSubsetOf(theList));

            theList.Add(943);
            theList.Add(15);    // duplicate

            Assert.True(theSet.IsProperSubsetOf(theList));

            theList.Remove(15);
            theList.Remove(666);

            Assert.False(theSet.IsProperSubsetOf(theList));
        }

        [Fact]
        public static void TestEmptyBehavior_Int()
        {
            SortedSet<int> theSet1 = new SortedSet<int>();
            Assert.False(theSet1.Remove(333));
            Assert.False(theSet1.Contains(213));

            SortedSet<int> anotherSet = new SortedSet<int>();
            anotherSet.Add(-23);
            anotherSet.Add(543);

            theSet1.IntersectWith(anotherSet);
            Assert.Equal(0, theSet1.Count);
        }

        [Fact]
        public static void TestResizing_Int()
        {
            // add a bunch of elements and see if resizes and see if
            // resulting size is expected. Do spot check for elements too.
            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(5);
            theSet.Add(75);
            theSet.Add(2445);
            theSet.Add(1234);
            theSet.Add(-2342);
            theSet.Add(-1);
            theSet.Add(875);
            theSet.Add(-532);
            theSet.Add(-452);
            theSet.Add(0);

            Assert.Equal(10, theSet.Count);
            Assert.All(new[] { -1, 75, -452, 0 }, item => Assert.Contains(item, theSet));
        }

        [Fact]
        public static void TestMultipleSubsets_Int()
        {
            SortedSet<int> theSet = new SortedSet<int>();
            theSet.Add(1);
            theSet.Add(2);
            theSet.Add(3);

            List<int> list1 = new List<int>();
            list1.Add(4);
            list1.Add(2);
            list1.Add(3);

            Assert.False(theSet.IsSubsetOf(list1));

            List<int> list2 = new List<int>();
            list2.Add(1);
            list2.Add(2);

            Assert.False(theSet.IsSubsetOf(list2));
        }

        [Fact]
        public static void StressTestBitArray()
        {
            SortedSet<int> set1 = new SortedSet<int>();
            for (int i = 0; i < 4001; i++)
            {
                set1.Add(i);
            }
            // set1 now has 0...4000

            List<int> list2 = new List<int>();
            for (int i = 3000; i < 7001; i++)
            {
                list2.Add(i);
            }
            // list2 has 3000...7000

            set1.SymmetricExceptWith(list2);

            //symmetric diff should be 0...2999 and 4001...7000

            List<int> expectedList = new List<int>();
            for (int i = 0; i < 3000; i++)
            {
                expectedList.Add(i);
            }
            for (int i = 4001; i < 7001; i++)
            {
                expectedList.Add(i);
            }

            string testName = "StressTestBitArray";
            int[] expectedArray = expectedList.ToArray();
            int expectedCount = expectedArray.Length;
            ContainsExpected(testName, set1, expectedArray, expectedCount);
        }

        #endregion

        #region SortedSet of object tests

        [Fact]
        public static void TestOverlaps_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            Assert.True(theSet.Overlaps(new DummyClass[] { c1, c2, c3 }));
            Assert.True(theSet.Overlaps(new DummyClass[] { c2 }));
            Assert.False(theSet.Overlaps(new DummyClass[] { c4, c5, c6 }));
        }

        [Fact]
        public static void TestSetsOfSortedSets_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");

            SortedSet<DummyClass> set1 = new SortedSet<DummyClass>();
            set1.Add(c1);
            set1.Add(c2);
            set1.Add(c3);
            SortedSet<DummyClass> set11 = new SortedSet<DummyClass>();
            set11.Add(c4);
            set11.Add(c5);
            set11.Add(c6);

            IComparer<SortedSet<DummyClass>> myEQ = new SortedSetComparer<DummyClass>();
            SortedSet<SortedSet<DummyClass>> setOfSets1 = new SortedSet<SortedSet<DummyClass>>(myEQ);

            setOfSets1.Add(set1);
            setOfSets1.Add(set11);

            SortedSet<SortedSet<DummyClass>> setOfSets2 = new SortedSet<SortedSet<DummyClass>>(myEQ);
            SortedSet<DummyClass> set2 = new SortedSet<DummyClass>();
            set2.Add(c1);
            set2.Add(c2);
            set2.Add(c3);
            SortedSet<DummyClass> set22 = new SortedSet<DummyClass>();
            set22.Add(c4);
            set22.Add(c5);
            set22.Add(c6);

            setOfSets2.Add(set2);
            setOfSets2.Add(set22);

            Assert.True(setOfSets1.IsSubsetOf(setOfSets2));
            Assert.False(setOfSets1.IsProperSubsetOf(setOfSets2));
            Assert.True(setOfSets1.SetEquals(setOfSets2));
        }

        [Fact]
        public static void TestCtors_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");
            DummyClass c7 = new DummyClass("c7");
            DummyClass c8 = new DummyClass("c8");

            SortedSet<DummyClass> set1 = new SortedSet<DummyClass>();
            set1.Add(c1);
            set1.Add(c2);
            set1.Add(c3);
            set1.Add(c4);
            set1.Add(c5);
            set1.Add(c6);
            set1.Add(c7);
            set1.Add(c8);

            Assert.Equal(8, set1.Count);

            SortedSet<DummyClass> set2 = new SortedSet<DummyClass>(set1);

            Assert.Equal(8, set2.Count);
            Assert.True(set2.SetEquals(set1));
            Assert.True(set1.SetEquals(set2));
        }

        [Fact]
        public static void TestAddAndRemove_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            // "expected" is used to compare 
            List<DummyClass> expected = new List<DummyClass>();
            expected.Add(c1);
            expected.Add(c2);
            expected.Add(c3);

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            Assert.True(theSet.SetEquals(expected));

            theSet.Remove(c2);
            expected.Remove(c2);
            // check
            Assert.True(theSet.SetEquals(expected));

            theSet.Add(c2);
            expected.Add(c2);
            // check
            Assert.True(theSet.SetEquals(expected));
        }

        [Fact]
        public static void TestDuplicateAdds_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            Assert.False(theSet.Add(c2));

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();

            theSet2.Add(c1);
            theSet2.Add(c2);
            theSet2.Add(c3);

            Assert.True(theSet.SetEquals(theSet2));
        }

        [Fact]
        public static void TestNullAdds_Object()
        {
            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(null);
            Assert.True(theSet.Contains(null));

            String expectedStr = ",,";

            int count = 0;
            String resultStr = ",";
            foreach (DummyClass o in theSet)
            {
                resultStr += o;
                resultStr += ",";
                count++;
            }
            Assert.Equal(1, count);
            Assert.Equal(expectedStr, resultStr);
        }

        [Fact]
        public static void TestClear_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            theSet.Clear();
            Assert.Equal(0, theSet.Count);
        }

        [Fact]
        public static void TestCount_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            Assert.Equal(3, theSet.Count);

            theSet.Clear();
            Assert.Equal(0, theSet.Count);
        }

        [Fact]
        public static void TestReverse_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c1Copy = new DummyClass("c1");
            c1Copy.Name = "Bump";
            c1.Name = "De Bump";
            c2.Name = "Bump";
            c3.Name = "De Bump";

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);
            theSet.Add(c1Copy);

            List<DummyClass> reversed = new List<DummyClass>(theSet.Reverse());
            String final = "";
            foreach (DummyClass d in reversed)
            {
                final += d.Name + " ";
            }

            Assert.Equal("De Bump Bump De Bump Bump ", final);
        }

        [Fact]
        public static void TestCopyToSimple_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);
            theSet.Remove(c1);
            theSet.Remove(c3);
            theSet.Add(c4);
            theSet.Add(c3);

            DummyClass[] expectedArray = { c2, c3, c4 };
            int expectedCount = 3;
            string testName = "TestCopyToSimple_Object";

            ContainsExpected(testName, theSet, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestCopyToWithStartIndex_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);
            theSet.Add(c4);

            DummyClass[] expectedArray = { c1, c2, c3, c4 };
            int expectedCount = 4;
            string testName = "TestCopyToWithStartIndex_Object";

            ContainsExpected(testName, theSet, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestCopyToErrors_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);
            theSet.Add(c4);
            theSet.Add(c5);

            DummyClass[] copyToArray = new DummyClass[5];

            Assert.Throws<ArgumentException>(() => theSet.CopyTo(copyToArray, 1)); //"should have thrown ArgumentException!"
            Assert.Throws<ArgumentException>(() => theSet.CopyTo(copyToArray, 10, 10)); //"should have thrown ArgumentException!"
        }

        [Fact]
        public static void TestRemoveWhere_Object()
        {
            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();

            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");

            // add some elements
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            Assert.Equal(1, theSet.RemoveWhere(MyPredicate_Object));

            DummyClass[] expectedArray = { c2, c3 };
            int expectedCount = 2;
            string testName = "TestRemoveWhere_Object 2";

            ContainsExpected(testName, theSet, expectedArray, expectedCount);
        }

        private static bool MyPredicate_Object(DummyClass dc)
        {
            return (dc.Id.Equals("c1")) ? true : false;
        }

        [Fact]
        public static void TestSuperset_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c2);
            theSet2.Add(c1);

            Assert.True(theSet1.IsSupersetOf(theSet2));

            SortedSet<DummyClass> theSet3 = new SortedSet<DummyClass>();
            theSet3.Add(c2);
            theSet3.Add(c5);

            Assert.False(theSet1.IsSupersetOf(theSet3));
        }

        [Fact]
        public static void TestProperSuperset_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");


            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c2);
            theSet2.Add(c1);

            Assert.True(theSet1.IsProperSupersetOf(theSet2));

            SortedSet<DummyClass> theSet3 = new SortedSet<DummyClass>();
            theSet3.Add(c2);
            theSet3.Add(c5);

            Assert.False(theSet1.IsProperSupersetOf(theSet3));

            SortedSet<DummyClass> theSet4 = new SortedSet<DummyClass>();
            theSet4.Add(c1);
            theSet4.Add(c2);
            theSet4.Add(c3);
            theSet4.Add(c4);

            Assert.False(theSet1.IsProperSupersetOf(theSet4));
        }

        [Fact]
        public static void TestProperSupersetWithEnumerable_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            List<DummyClass> theList = new List<DummyClass>();
            theList.Add(c2);
            theList.Add(c1);
            theList.Add(c1);

            Assert.True(theSet1.IsProperSupersetOf(theList));

            theList.Add(c4);
            theList.Add(c4);
            Assert.True(theSet1.IsProperSupersetOf(theList));

            theList.Add(c3);
            Assert.False(theSet1.IsProperSupersetOf(theList));

            theList.Add(c5);
            theList.Add(c6);
            Assert.False(theSet1.IsProperSupersetOf(theList));
        }

        [Fact]
        public static void TestEquality_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c1);
            theSet2.Add(c4);
            theSet2.Add(c2);
            theSet2.Add(c3);

            Assert.True(theSet1.SetEquals(theSet2));

            List<DummyClass> theList = new List<DummyClass>();
            theList.Add(c1);
            theList.Add(c2);
            theList.Add(c3);
            theList.Add(c4);

            Assert.True(theSet1.SetEquals(theList));
        }

        [Fact]
        public static void TestUnion_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c3);
            theSet2.Add(c4);
            theSet2.Add(c1);

            theSet1.UnionWith(theSet2);

            DummyClass[] expectedArray = { c1, c2, c3, c4 };
            int expectedCount = 4;
            string testName = "TestUnion_Object";

            ContainsExpected(testName, theSet1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestIntersection_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c3);
            theSet2.Add(c4);
            theSet2.Add(c1);

            theSet1.IntersectWith(theSet2);

            DummyClass[] expectedArray = { c1, c3 };
            int expectedCount = 2;
            string testName = "TestIntersection_Object";

            ContainsExpected(testName, theSet1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestExcept_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c3);
            theSet2.Add(c5);
            theSet2.Add(c1);

            theSet1.ExceptWith(theSet2);

            SortedSet<DummyClass> expected = new SortedSet<DummyClass>();
            expected.Add(c2);
            expected.Add(c4);

            Assert.True(theSet1.SetEquals(expected));
        }

        [Fact]
        public static void TestSymmetricExcept_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");

            SortedSet<DummyClass> set1 = new SortedSet<DummyClass>();
            set1.Add(c1);
            set1.Add(c2);
            set1.Add(c3);
            set1.Add(c4);

            SortedSet<DummyClass> set2 = new SortedSet<DummyClass>();
            set2.Add(c5);
            set2.Add(c2);
            set2.Add(c3);
            set2.Add(c6);

            set1.SymmetricExceptWith(set2);
            // set1 should now have c1, c4, c5, c6
            Assert.Equal(4, set1.Count);

            // make sure set2 count didn't change, don't want to modify it.
            Assert.Equal(4, set2.Count);

            DummyClass[] expectedArray = { c1, c4, c5, c6 };
            int expectedCount = 4;
            string testName = "TestSymmetricExcept_Object 3";

            ContainsExpected(testName, set1, expectedArray, expectedCount);
        }

        [Fact]
        public static void TestSymmetricWithEnumerator_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> set1 = new SortedSet<DummyClass>();
            set1.Add(c1);
            set1.Add(c2);
            set1.Add(c3);
            set1.Add(c4);

            List<DummyClass> theList = new List<DummyClass>();
            theList.Add(c5);
            theList.Add(c5);
            theList.Add(c2);
            theList.Add(c4);
            theList.Add(c4);
            theList.Add(c2);
            theList.Add(c5);


            set1.SymmetricExceptWith(theList);
            // set1 should now have c1, c5, c3

            SortedSet<DummyClass> controlSet = new SortedSet<DummyClass>();
            controlSet.Add(c1);
            controlSet.Add(c5);
            controlSet.Add(c3);
            Assert.True(set1.SetEquals(controlSet));

            DummyClass[] expectedArray = { c1, c5, c3 };
            int expectedCount = 3;
            string testName = "TestSymmetricWithEnumerator_Object 2";

            ContainsExpected(testName, set1, expectedArray, expectedCount);

            SortedSet<DummyClass> set2 = new SortedSet<DummyClass>();
            set2.Add(c1);
            set2.Add(c2);
            set2.Add(c3);
            set2.Add(c4);

            theList.Clear();
            theList.Add(c1);
            theList.Add(c2);
            theList.Add(c4);
            theList.Add(c3);
            theList.Add(c2);

            set2.SymmetricExceptWith(theList);
            Assert.Equal(0, set2.Count);

            SortedSet<DummyClass> set3 = new SortedSet<DummyClass>();
            set3.Add(c1);
            set3.Add(c2);
            set3.Add(c3);
            set3.Add(c4);

            theList.Clear();
            theList.Add(c2);
            theList.Add(c4);
            theList.Add(c3);
            theList.Add(c2);

            set3.SymmetricExceptWith(theList);

            SortedSet<DummyClass> controlSet2 = new SortedSet<DummyClass>();
            controlSet2.Add(c1);
            Assert.True(set3.SetEquals(controlSet2));
        }

        [Fact]
        public static void TestSubset_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c2);
            theSet2.Add(c1);

            Assert.True(theSet2.IsSubsetOf(theSet1));

            SortedSet<DummyClass> theSet3 = new SortedSet<DummyClass>();
            theSet3.Add(c2);
            theSet3.Add(c5);

            Assert.False(theSet3.IsSubsetOf(theSet1));
        }

        [Fact]
        public static void TestSubsetWithEnumerable_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(c1);
            theSet.Add(c2);

            List<DummyClass> theList = new List<DummyClass>();
            theList.Add(c2);
            theList.Add(c1);
            theList.Add(c3);
            theList.Add(c4);

            Assert.True(theSet.IsSubsetOf(theList));

            List<DummyClass> theList2 = new List<DummyClass>();
            theList.Add(c5);
            theList.Add(c1);
            theList.Add(c3);
            theList.Add(c4);

            Assert.False(theSet.IsSubsetOf(theList2));
        }

        [Fact]
        public static void TestProperSubset_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            theSet1.Add(c1);
            theSet1.Add(c2);
            theSet1.Add(c3);
            theSet1.Add(c4);

            SortedSet<DummyClass> theSet2 = new SortedSet<DummyClass>();
            theSet2.Add(c2);
            theSet2.Add(c1);

            Assert.True(theSet2.IsProperSubsetOf(theSet1));

            SortedSet<DummyClass> theSet3 = new SortedSet<DummyClass>();
            theSet3.Add(c2);
            theSet3.Add(c5);

            Assert.False(theSet3.IsProperSubsetOf(theSet1));

            SortedSet<DummyClass> theSet4 = new SortedSet<DummyClass>();
            theSet4.Add(c1);
            theSet4.Add(c2);
            theSet4.Add(c3);
            theSet4.Add(c4);

            Assert.False(theSet4.IsProperSubsetOf(theSet1));
        }

        [Fact]
        public static void TestProperSubsetWithEnumerable_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);
            theSet.Add(c4);

            List<DummyClass> theList = new List<DummyClass>();
            theList.Add(c2);
            theList.Add(c1);
            theList.Add(c3);
            theList.Add(c4);
            theList.Add(c4);    // duplicate

            Assert.False(theSet.IsProperSubsetOf(theList));

            theList.Add(c5);
            theList.Add(c1);    // duplicate

            Assert.True(theSet.IsProperSubsetOf(theList));

            theList.Remove(c1);
            theList.Remove(c2);

            Assert.False(theSet.IsProperSubsetOf(theList));
        }

        [Fact]
        public static void TestEmptyBehavior_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet1 = new SortedSet<DummyClass>();
            Assert.False(theSet1.Remove(c1));

            Assert.False(theSet1.Contains(c4));

            SortedSet<DummyClass> anotherSet = new SortedSet<DummyClass>();
            anotherSet.Add(c2);
            anotherSet.Add(c3);


            theSet1.IntersectWith(anotherSet);
            Assert.Equal(0, theSet1.Count);
        }

        [Fact]
        public static void TestResizing_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");
            DummyClass c5 = new DummyClass("c5");
            DummyClass c6 = new DummyClass("c6");
            DummyClass c7 = new DummyClass("c7");
            DummyClass c8 = new DummyClass("c8");
            DummyClass c9 = new DummyClass("c9");
            DummyClass c10 = new DummyClass("c10");

            // add a bunch of elements and see if resizes to handle
            // using fact that defaultCapacity is now 7...keep this updated
            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);
            theSet.Add(c4);
            theSet.Add(c5);
            theSet.Add(c6);
            theSet.Add(c7);
            theSet.Add(c8);
            theSet.Add(c9);
            theSet.Add(c10);

            Assert.Equal(10, theSet.Count);

            Assert.All(new[] { c6, c2, c7, c10 }, item => Assert.Contains(item, theSet));
        }

        [Fact]
        public static void TestMultipleSubsets_Object()
        {
            DummyClass c1 = new DummyClass("c1");
            DummyClass c2 = new DummyClass("c2");
            DummyClass c3 = new DummyClass("c3");
            DummyClass c4 = new DummyClass("c4");

            SortedSet<DummyClass> theSet = new SortedSet<DummyClass>();
            theSet.Add(c1);
            theSet.Add(c2);
            theSet.Add(c3);

            List<DummyClass> list1 = new List<DummyClass>();
            list1.Add(c4);
            list1.Add(c2);
            list1.Add(c3);

            Assert.False(theSet.IsSubsetOf(list1));

            List<DummyClass> list2 = new List<DummyClass>();
            list2.Add(c1);
            list2.Add(c2);

            Assert.False(theSet.IsSubsetOf(list2));
        }

        #endregion

        #region DummyClass / helpers

        private class IntModEqualityComparer : IEqualityComparer<int>, IComparer<int>
        {
            private int _mod;

            public IntModEqualityComparer(int mod)
            {
                _mod = mod;
            }

            public bool Equals(int x, int y)
            {
                return ((x % _mod) == (y % _mod));
            }

            public int GetHashCode(int x)
            {
                return (x % _mod);
            }

            public int Compare(int x, int y)
            {
                return ((x % _mod) - (y % _mod));
            }
        }

        private class AbsInt : IEquatable<AbsInt>, IComparable<AbsInt>
        {
            public int value;

            public AbsInt(int x)
            {
                value = x;
            }

            public bool Equals(AbsInt y)
            {
                if (y == null) return false;

                return Abs(y.value) == Abs(value);
            }

            public int CompareTo(AbsInt o)
            {
                if (o == null) return 1;
                return Abs(value) - Abs(o.value);
            }
        }

        private class AbsIntNormalComparer : IEqualityComparer<AbsInt>, IComparer<AbsInt>
        {
            public AbsIntNormalComparer()
            {
            }

            public bool Equals(AbsInt x, AbsInt y)
            {
                if (x == null)
                    return (y == null);
                if (y == null)
                    return false;
                return (x.value == y.value);
            }

            public int GetHashCode(AbsInt x)
            {
                if (x == null)
                    return 0;
                return (x.value);
            }

            public int Compare(AbsInt x, AbsInt y)
            {
                if (x == null)
                    if (y == null)
                        return 0;
                    else
                        return -1;
                if (y == null)
                    return 1;

                return x.value - y.value;
            }
        }

        public class ValueItem : IEnumerable<ValueItem>, IEnumerable, IComparable
        {
            public int x;

            public ValueItem(int x2)
            {
                x = x2;
            }

            public IEnumerator<ValueItem> GetEnumerator()
            {
                return new ValueItemEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ValueItemEnumerator(this);
            }

            public int CompareTo(object o)
            {
                ValueItem obj = o as ValueItem;

                if (obj == null)
                    return -1;

                int absOBJ = Abs(obj.x);
                int absX = Abs(x);
                if (absX > absOBJ)
                    return 1;
                if (absX < absOBJ)
                    return -1;

                return 0;
                //compare distance from origin
                //return (int)Round(Sqrt(x * x + y * y) - Sqrt((obj.x * obj.x) + (obj.y * obj.y)));
            }
        }

        public class ValueItemEnumerator : IEnumerator<ValueItem>, IEnumerator
        {
            public ValueItem _item;
            private int _position = -1;

            public ValueItemEnumerator(ValueItem item)
            {
                _item = item;
            }

            public bool MoveNext()
            {
                _position++;
                return (_position < 1);
            }

            public void Reset()
            {
                _position = -1;
            }

            public void Dispose()
            {
                _item = null;
            }

            public ValueItem Current
            {
                get
                {
                    if (_position == 0) return _item;
                    else throw new InvalidOperationException();
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_position == 0) return _item;
                    throw new InvalidOperationException();
                }
            }
        }

        // DummyClass is used for SortedSet of object tests below
        public class DummyClass : IComparable
        {
            private string _id;
            private string _name;
            private double _percent;

            // initialize random generators
            static DummyClass()
            {
            }

            public DummyClass(string id)
            {
                _id = id;
                _name = id;
                _percent = 24.23;
            }

            public string Id
            {
                get { return _id; }
                set { _id = value; }
            }

            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            public double Percent
            {
                get { return _percent; }
                set { _percent = value; }
            }

            public override string ToString()
            {
                return _id;
            }

            // Generates a random-ish string 
            private string RandomString()
            {
                int size = 10;

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < size; i++)
                {
                    builder.Append((char)(i + 50));
                }
                return builder.ToString();
            }

            public int CompareTo(object o)
            {
                DummyClass obj = o as DummyClass;
                if (obj == null)
                    return -1;
                //sorts first by id, then name, then percent

                int comp = Id.CompareTo(obj.Id);
                if (comp != 0)
                    return comp;
                comp = Name.CompareTo(obj.Name);
                if (comp != 0)
                    return comp;
                comp = Percent.CompareTo(obj.Percent);
                return comp;
            }
        }

        /// <summary>
        /// An ordering for sorted set that checks elements lexicographically
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class SortedSetComparer<T> : IComparer<SortedSet<T>>
        {
            public int Compare(SortedSet<T> x, SortedSet<T> y)
            {
                if (x == null || x.Count == 0)
                    if (y == null || y.Count == 0)
                        return 0;
                    else
                        return -1;
                if (y == null || y.Count == 0)
                    return 1;

                IEnumerator<T> one = x.GetEnumerator();
                IEnumerator<T> two = y.GetEnumerator();

                IComparer<T> comparer = (x.Comparer.Equals(y.Comparer)) ? x.Comparer : Comparer<T>.Default;

                int diff = 0;
                while (diff == 0)
                {
                    bool a = one.MoveNext();
                    bool b = two.MoveNext();
                    if (!a && !b) return 0;
                    if (!b) return 1;
                    if (!a) return -1;

                    diff = comparer.Compare(one.Current, two.Current);
                }

                return diff;
            }
        }

        private static int Abs(int value)
        {
            if (value < 0)
                return -value;
            else
                return value;
        }

        // Dummy equality comparer for some DummyClass test cases where we want
        // DummyClass value equality instead of reference
        private class DummyClassEqualityComparer : IEqualityComparer<DummyClass>, IComparer<DummyClass>
        {
            // Don't worry about boundary cases like null, etc; this is just for DummyClass test cases below
            public bool Equals(DummyClass x, DummyClass y)
            {
                return x.Id.Equals(y.Id) && x.Name.Equals(y.Name) && x.Percent.Equals(y.Percent);
            }

            public int GetHashCode(DummyClass obj)
            {
                return obj.Id.GetHashCode() | obj.Name.GetHashCode() | obj.Percent.GetHashCode();
            }

            public int Compare(DummyClass a, DummyClass b)
            {
                //sorts first by id, then name, then percent

                int comp = a.Id.CompareTo(b.Id);
                if (comp != 0)
                    return comp;
                comp = a.Name.CompareTo(b.Name);
                if (comp != 0)
                    return comp;
                comp = a.Percent.CompareTo(b.Percent);
                return comp;
            }
        }

        #endregion
    }
}
