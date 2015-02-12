// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_SortTests
{
    public class Driver<T> where T : IComparable<T>
    {
        #region Sort

        public void Sort1(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            //search for each item
            list.Sort();
            EnsureSorted(list, 0, list.Count);
        }

        public RefX1<int>[] MakeRefX1ArrayInt(int[] items)
        {
            RefX1<int>[] arr = new RefX1<int>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                arr[i] = new RefX1<int>(items[i]);
            }
            return arr;
        }

        public ValX1<int>[] MakeValX1ArrayInt(int[] items)
        {
            ValX1<int>[] arr = new ValX1<int>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                arr[i] = new ValX1<int>(items[i]);
            }
            return arr;
        }

        private void EnsureSorted(List<T> list, int index, int count)
        {
            for (int i = index; i < index + count - 1; i++)
            {
                Assert.True(
                    ((null == (object)list[i]) && (null != (object)list[i + 1]) && (-1 < list[i + 1].CompareTo(list[i])))
                    || (1 > list[i].CompareTo(list[i + 1])),
                    "The list is not sorted at index: " + i);
            }
        }

        #endregion

        #region Sort(Comparison)

        public void VerifyVanilla(T[] items)
        {
            List<T> list = new List<T>();

            int[] numbers = new int[] { -1, 0, 1 };
            int currentIndex = 0;

            T[] tempArray;

            //[] Verify Sort with random comparison
            list.Clear();
            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            list.Sort((T x, T y) =>
            {
                int index = currentIndex % numbers.Length;
                currentIndex++;
                return numbers[index];
            });

            VerifyListOutOfOrder(list, items);

            //[] Verify Sort with comparison that reverses the order
            list.Clear();
            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            list.Sort(delegate (T x, T y) { return Array.IndexOf(items, y) - Array.IndexOf(items, x); });
            tempArray = new T[items.Length];
            Array.Copy(items, 0, tempArray, 0, items.Length);
            Array.Reverse(tempArray);

            VerifyList(list, tempArray);

            //[] Verify Sort with comparison that reverses the order AGAIN
            //[] We are using the reversed list from the previous scenario
            list.Sort(delegate (T x, T y) { return Array.IndexOf(items, x) - Array.IndexOf(items, y); });

            VerifyList(list, items);
        }

        public void VerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null Comparison
            Assert.Throws<ArgumentNullException>(() => list.Sort((Comparison<T>)null)); //"Err_858ahia Expected null match to throw ArgumentNullException"
        }

        private void VerifyList(List<T> list, T[] expectedItems)
        {
            Assert.Equal(list.Count, expectedItems.Length); //"Err_2828ahid Expected"

            //Only verify the indexer. List should be in a good enough state that we
            //do not have to verify consistancy with any other method.
            for (int i = 0; i < list.Count; ++i)
            {
                Assert.Equal(list[i], expectedItems[i]); //"Err_19818ayiadb Expceted"
            }
        }

        private void VerifyListOutOfOrder(List<T> list, T[] expectedItems)
        {
            Assert.Equal(list.Count, expectedItems.Length); //"Err_5808ajhdo Expected"

            //do not have to verify consistancy with any other method.
            for (int i = 0; i < list.Count; ++i)
            {
                Assert.True(list.Contains(expectedItems[i])); //"Err_51108ajdiu Expceted"
            }
        }

        public void CallSortAndVerify(T[] items, T[] expectedItems, Comparison<T> comparison)
        {
            List<T> list = new List<T>();
            List<T> visitedItems = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            list.Sort(comparison);

            VerifyList(list, expectedItems);
        }

        #endregion
    }

    public class Driver2<T> where T : IComparableValue
    {
        #region Sort(IComparer<T>)

        public void SortIComparer(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            //search for each item
            list.Sort(new ValueComparer<T>());
            EnsureSorted(list, 0, list.Count);
        }

        public void SortIComparerValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Throws<InvalidOperationException>(() => list.Sort((IComparer<T>)null)); //"InvalidOperationException expected."
        }

        public RefX1_IC<int>[] MakeRefX1ArrayInt(int[] items)
        {
            RefX1_IC<int>[] arr = new RefX1_IC<int>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                arr[i] = new RefX1_IC<int>(items[i]);
            }
            return arr;
        }

        public ValX1_IC<int>[] MakeValX1ArrayInt(int[] items)
        {
            ValX1_IC<int>[] arr = new ValX1_IC<int>[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                arr[i] = new ValX1_IC<int>(items[i]);
            }
            return arr;
        }

        #endregion

        #region Sort(int, int, IComparer<T>)

        public void SortIntIntIComparer(T[] items, int index, int count)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            //search for each item
            list.Sort(index, count, new ValueComparer<T>());
            EnsureSorted(list, index, count);
            for (int i = 0; i < index; i++)
            {
                Assert.Equal(((Object)list[i]), items[i]); //"Expected them to be the same."
            }
            for (int i = index + count; i < items.Length; i++)
            {
                Assert.Equal(((Object)list[i]), items[i]); //"Expected them to be the same."
            }
        }

        public void SortIntIntIComparerValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Throws<InvalidOperationException>(() => list.Sort((IComparer<T>)null)); //"InvalidOperationException expected"

            int[] bad = new int[] {
            items.Length,1,
            items.Length+1,0,
            int.MaxValue,0
            };

            for (int i = 0; i < bad.Length; i++)
            {
                Assert.Throws<ArgumentException>(() => list.Sort(bad[i], bad[++i], new ValueComparer<T>())); //"ArgumentException expected."
            }

            bad = new int[] {
            -1,0,
            -2,0,
            int.MinValue,0,
            0,-1,
            0,-2,
            0,int.MinValue
            };

            for (int i = 0; i < bad.Length; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.Sort(bad[i], bad[++i], new ValueComparer<T>())); //"ArgumentOutOfRangeException expected."
            }
        }

        #endregion

        private void EnsureSorted(List<T> list, int index, int count)
        {
            for (int i = index; i < index + count - 1; i++)
            {
                Assert.True(
                    ((null == (object)list[i]) && (null != (object)list[i + 1]) && (-1 < list[i + 1].CompareTo(list[i])))
                    || (1 > list[i].CompareTo(list[i + 1])),
                    "The list is not sorted at index: " + i);
            }
        }
    }
    public class List_SortTests
    {
        #region Static Member Variables

        private static readonly int[] s_arr1 = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        private static readonly int[] s_arr2 = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly int[] s_arr3 = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        private static readonly int[] s_arr4 = new int[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 };

        private static readonly int[] s_arr5 = new int[] { 9, 1, 2, 3, 4, 5, 6, 7, 8, 0 };
        private static readonly int[] s_arr6 = new int[] { 8, 1, 2, 3, 4, 5, 6, 7, 0 };
        private static readonly int[] s_arr7 = new int[] { 0, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        private static readonly int[] s_arr8 = new int[] { 1, 9, 8, 7, 6, 5, 4, 3, 2 };

        private static readonly int[] s_arr9 = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
        private static readonly int[] s_arr10 = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
        private static readonly int[] s_arr11 = new int[] { 8, 7, 6, 5, 4, 3, 2, 1, 0, 9 };
        private static readonly int[] s_arr12 = new int[] { 7, 6, 5, 4, 3, 2, 1, 0, 8 };

        private static readonly int[] s_arr13 = new int[] { 5, 1, 2, 3, 4, 9, 6, 7, 8, 0 };
        private static readonly int[] s_arr14 = new int[] { 4, 1, 2, 3, 8, 5, 6, 7, 0 };
        private static readonly int[] s_arr15 = new int[] { 0, 9, 8, 7, 1, 5, 4, 3, 2, 6 };
        private static readonly int[] s_arr16 = new int[] { 1, 9, 8, 7, 6, 2, 4, 3, 5 };

        private static readonly int[] s_arr17 = new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2 };
        private static readonly int[] s_arr18 = new int[] { 1, 1, 2, 0, 2, 2, 0, 0, 2, 0, 1, 1 };
        private static readonly int[] s_arr19 = new int[] { 2, 1, 3 };
        private static readonly int[] s_arr20 = new int[] { 2, 3, 1 };
        private static readonly int[] s_arr21 = new int[] { 1, 2, 3 };
        private static readonly int[] s_arr22 = new int[] { 1, 3, 2 };
        private static readonly int[] s_arr23 = new int[] { 3, 2, 1 };
        private static readonly int[] s_arr24 = new int[] { 3, 1, 2 };

        private static readonly int[] s_arr25 = new int[] { 2, 1 };
        private static readonly int[] s_arr26 = new int[] { 0, 1 };
        private static readonly int[] s_arr27 = new int[] { 1 };
        private static readonly int[] s_arr28 = new int[] { };

        #endregion

        [Fact]
        public static void SortTest()
        {
            Driver<RefX1<int>> RefDriver = new Driver<RefX1<int>>();

            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr1));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr2));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr3));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr4));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr5));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr6));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr7));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr8));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr9));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr10));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr11));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr12));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr13));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr14));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr15));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr16));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr17));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr18));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr19));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr20));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr21));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr22));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr23));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr24));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr25));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr26));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr27));
            RefDriver.Sort1(RefDriver.MakeRefX1ArrayInt(s_arr28));

            Driver<ValX1<int>> ValDriver = new Driver<ValX1<int>>();

            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr1));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr2));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr3));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr4));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr5));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr6));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr7));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr8));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr9));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr10));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr11));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr12));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr13));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr14));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr15));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr16));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr17));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr18));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr19));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr20));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr21));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr22));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr23));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr24));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr25));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr26));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr27));
            ValDriver.Sort1(ValDriver.MakeValX1ArrayInt(s_arr28));
        }

        [Fact]
        public static void ComparerReturns0YieldsStableSort()
        {
            var originalList = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                originalList.Add(i);
            }

            var sortedList = new List<int>(originalList);

            sortedList.Sort(new Comparison<int>((a, b) => 0));

            Assert.Equal(originalList.Count, sortedList.Count);
            for (int i = 0; i < originalList.Count; i++)
            {
                Assert.Equal(originalList[i], sortedList[i]);
            }
        }

        [Fact]
        public static void SortIComparer_Tests()
        {
            Driver2<RefX1_IC<int>> RefDriver = new Driver2<RefX1_IC<int>>();

            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr2));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr3));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr4));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr5));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr6));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr7));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr8));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr9));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr10));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr11));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr12));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr13));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr14));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr15));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr16));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr17));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr18));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr19));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr20));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr21));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr22));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr23));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr24));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr25));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr26));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr27));
            RefDriver.SortIComparer(RefDriver.MakeRefX1ArrayInt(s_arr28));


            Driver2<ValX1_IC<int>> ValDriver = new Driver2<ValX1_IC<int>>();

            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr1));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr2));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr3));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr4));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr5));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr6));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr7));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr8));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr9));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr10));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr11));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr12));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr13));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr14));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr15));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr16));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr17));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr18));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr19));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr20));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr21));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr22));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr23));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr24));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr25));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr26));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr27));
            ValDriver.SortIComparer(ValDriver.MakeValX1ArrayInt(s_arr28));
        }

        [Fact]
        public static void SortIComparer_Tests_Negative()
        {
            int[] arr21 = new int[] { 1, 2, 3 };

            Driver2<RefX1_IC<int>> RefDriver = new Driver2<RefX1_IC<int>>();
            RefDriver.SortIComparerValidations(RefDriver.MakeRefX1ArrayInt(arr21));

            Driver2<ValX1_IC<int>> ValDriver = new Driver2<ValX1_IC<int>>();
            ValDriver.SortIComparerValidations(ValDriver.MakeValX1ArrayInt(arr21));
        }

        [Fact]
        public static void SortIntIntIComparer_Tests()
        {
            Driver2<RefX1_IC<int>> RefDriver = new Driver2<RefX1_IC<int>>();

            RefDriver.SortIntIntIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1), 0, 10);
            RefDriver.SortIntIntIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1), 1, 9);
            RefDriver.SortIntIntIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1), 9, 1);
            RefDriver.SortIntIntIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1), 5, 5);
            RefDriver.SortIntIntIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1), 0, 5);
            RefDriver.SortIntIntIComparer(RefDriver.MakeRefX1ArrayInt(s_arr1), 3, 3);

            Driver2<ValX1_IC<int>> ValDriver = new Driver2<ValX1_IC<int>>();

            ValDriver.SortIntIntIComparer(ValDriver.MakeValX1ArrayInt(s_arr1), 0, 10);
            ValDriver.SortIntIntIComparer(ValDriver.MakeValX1ArrayInt(s_arr1), 1, 9);
            ValDriver.SortIntIntIComparer(ValDriver.MakeValX1ArrayInt(s_arr1), 9, 1);
            ValDriver.SortIntIntIComparer(ValDriver.MakeValX1ArrayInt(s_arr1), 5, 5);
            ValDriver.SortIntIntIComparer(ValDriver.MakeValX1ArrayInt(s_arr1), 0, 5);
            ValDriver.SortIntIntIComparer(ValDriver.MakeValX1ArrayInt(s_arr1), 3, 3);
        }
        [Fact]
        public static void SortIntIntIComparer_Tests_Negative()
        {
            Driver2<RefX1_IC<int>> RefDriver = new Driver2<RefX1_IC<int>>();
            Driver2<ValX1_IC<int>> ValDriver = new Driver2<ValX1_IC<int>>();

            RefDriver.SortIntIntIComparerValidations(RefDriver.MakeRefX1ArrayInt(s_arr1));
            ValDriver.SortIntIntIComparerValidations(ValDriver.MakeValX1ArrayInt(s_arr1));
        }

        [Fact]
        public static void SortComparison_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.VerifyVanilla(new int[0]);
            intDriver.VerifyVanilla(new int[] { 1 });
            intDriver.VerifyVanilla(intArray);
            intDriver.CallSortAndVerify(
                new int[] { 1, 0, 5, 4, 2 },
                new int[] { 0, 1, 2, 4, 5 },
                (int x, int y) => { return x - y; });

            stringDriver.VerifyVanilla(new string[0]);
            stringDriver.VerifyVanilla(new string[] { "1" });
            stringDriver.VerifyVanilla(stringArray);
            stringDriver.CallSortAndVerify(
                new string[] { "1", "", "12345", "1234", "12" },
                new string[] { "", "1", "12", "1234", "12345" },
                (string x, string y) => { return x.Length - y.Length; });
        }

        [Fact]
        public static void SortComparison_Tests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }
            intDriver.VerifyExceptions(intArray);
            stringDriver.VerifyExceptions(stringArray);
        }
    }

    #region Helper Classes

    /// <summary>
    /// Helper class that implements ICollection.
    /// </summary>
    public class TestCollection<T> : ICollection<T>
    {
        /// <summary>
        /// Expose the Items in Array to give more test flexibility...
        /// </summary>
        public readonly T[] m_items;

        public TestCollection(T[] items)
        {
            m_items = items;
        }

        public void CopyTo(T[] array, int index)
        {
            Array.Copy(m_items, 0, array, index, m_items.Length);
        }

        public int Count
        {
            get
            {
                if (m_items == null)
                    return 0;
                else
                    return m_items.Length;
            }
        }

        public Object SyncRoot { get { return this; } }

        public bool IsSynchronized { get { return false; } }

        public IEnumerator<T> GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        private class TestCollectionEnumerator<T1> : IEnumerator<T1>
        {
            private TestCollection<T1> _col;
            private int _index;

            public void Dispose() { }

            public TestCollectionEnumerator(TestCollection<T1> col)
            {
                _col = col;
                _index = -1;
            }

            public bool MoveNext()
            {
                return (++_index < _col.m_items.Length);
            }

            public T1 Current
            {
                get { return _col.m_items[_index]; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _col.m_items[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        #region Non Implemented methods

        public void Add(T item) { throw new NotSupportedException(); }

        public void Clear() { throw new NotSupportedException(); }
        public bool Contains(T item) { throw new NotSupportedException(); }

        public bool Remove(T item) { throw new NotSupportedException(); }

        public bool IsReadOnly { get { throw new NotSupportedException(); } }

        #endregion
    }

    public class RefX1<T> : IComparable<RefX1<T>> where T : IComparable
    {
        private T _val;
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
        public RefX1(T t) { _val = t; }
        public int CompareTo(RefX1<T> obj)
        {
            if (null == obj)
                return 1;
            if (null == _val)
                if (null == obj.Val)
                    return 0;
                else
                    return -1;
            return _val.CompareTo(obj.Val);
        }
        public override bool Equals(object obj)
        {
            if (obj is RefX1<T>)
            {
                RefX1<T> v = (RefX1<T>)obj;
                return (CompareTo(v) == 0);
            }
            return false;
        }
        public override int GetHashCode() { return base.GetHashCode(); }

        public bool Equals(RefX1<T> x)
        {
            return 0 == CompareTo(x);
        }
    }

    public struct ValX1<T> : IComparable<ValX1<T>> where T : IComparable
    {
        private T _val;
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
        public ValX1(T t) { _val = t; }
        public int CompareTo(ValX1<T> obj)
        {
            if (Object.ReferenceEquals(_val, obj._val)) return 0;

            if (null == _val)
                return -1;

            return _val.CompareTo(obj.Val);
        }
        public override bool Equals(object obj)
        {
            if (obj is ValX1<T>)
            {
                ValX1<T> v = (ValX1<T>)obj;
                return (CompareTo(v) == 0);
            }
            return false;
        }
        public override int GetHashCode() { return ((object)this).GetHashCode(); }

        public bool Equals(ValX1<T> x)
        {
            return 0 == CompareTo(x);
        }
    }

    public interface IComparableValue
    {
        IComparable Val { get; set; }
        int CompareTo(IComparableValue obj);
    }

    public class RefX1_IC<T> : IComparableValue where T : IComparable
    {
        private T _val;
        public System.IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }
        public RefX1_IC(T t) { _val = t; }
        public int CompareTo(IComparableValue obj)
        {
            if (null == (object)obj)
                return 1;
            if (null == (object)_val)
                if (null == (object)obj.Val)
                    return 0;
                else
                    return -1;
            return _val.CompareTo(obj.Val);
        }
    }

    public struct ValX1_IC<T> : IComparableValue where T : IComparable
    {
        private T _val;
        public System.IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }
        public ValX1_IC(T t) { _val = t; }
        public int CompareTo(IComparableValue obj)
        {
            return _val.CompareTo(obj.Val);
        }
    }

    public class ValueComparer<T> : IComparer<T> where T : IComparableValue
    {
        public int Compare(T x, T y)
        {
            if (null == (object)x)
                if (null == (object)y)
                    return 0;
                else
                    return -1;
            if (null == (object)y)
                return 1;
            if (null == (object)x.Val)
                if (null == (object)y.Val)
                    return 0;
                else
                    return -1;
            return x.Val.CompareTo(y.Val);
        }

        public bool Equals(T x, T y)
        {
            return 0 == Compare(x, y);
        }

        public int GetHashCode(T x)
        {
            return x.GetHashCode();
        }
    }
    #endregion
}
