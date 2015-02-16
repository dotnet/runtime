// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace List_List_RemoveReverseTests
{
    public class Driver<T>
    {
        #region Remove

        public void BasicRemove(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(true, list.Remove(items[i])); //String.Format("Err_12702ahpba Expected Remove to return true with item={0}", items[i])
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.Contains(items[i]), false); //String.Format("Err_1707ahspb!!! Expected item={0} not to exist in the collection", items[i])
            }
            Assert.Equal(list.Count, 0); //"Err_21181ahied Expected Count=0 after removing all items actual:{1}" + list.Count
        }

        public void ExtendedRemove(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));
            list.AddRange(new TestCollection<T>(itemsY));
            for (int i = 0; i < itemsX.Length; i++)
            {
                Assert.Equal(true, list.Remove(itemsX[i])); //String.Format("Err_586123ahpba Expected Remove to return true with item={0}", itemsX[i])
            }
            Assert.Equal(list.Count, itemsY.Length); //"Expect to be same."
            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(list[i], itemsY[i]); //"Err_485585ahied Expected at index:" + i
            }
        }

        public void RemoveMultipleExistingValue(T[] items, int sameFactor, int sameIndex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            int sameCountExpected = (sameFactor * 2) + 1;
            T sameVal = items[sameIndex];
            for (int i = 0; i < sameFactor; i++)
            {
                list.AddRange(new TestCollection<T>(items));
                list.Add(sameVal);
            }
            for (int i = 0; i < sameCountExpected; i++)
            {
                Assert.Equal(true, list.Remove(sameVal)); //String.Format("Err_181585aeahp Expected Remove to return true with item={0}", sameVal)
            }

            Assert.Equal(list.Contains(sameVal), false); //"Expect to be same."

            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(false, list.Remove(sameVal)); //String.Format("Err_55861anhpba Expected Remove to return false with item={0}", sameVal)
            }
            Assert.Equal(list.Contains(sameVal), false); //"Expect to be same."
            Assert.Equal(list.Count, (items.Length - 1) * (sameFactor + 1)); //"Expect to be same."
        }

        public void RemoveEmptyCollection(T item)
        {
            List<T> list = new List<T>();

            Assert.Equal(false, list.Remove(item)); //String.Format("Err_44896!!! Expected Remove to return false with item={0}", item)

            Assert.Equal(list.Count, 0); //"Expect to be same."
        }

        public void RemoveNullValueWhenReference(T value)
        {
            if ((object)value != null)
            {
                throw new ArgumentException("invalid argument passed to testcase");
            }

            List<T> list = new List<T>();
            list.Add(value);

            Assert.Equal(true, list.Remove(value)); //String.Format("Err_55861anhpba Expected Remove to return true with item={0}", value)
            Assert.Equal(false, list.Remove(value)); //String.Format("Err_88484ahbpz Expected Remove to return false with item={0}", value)

            Assert.Equal(list.Contains(value), false); //"Expect to be same."
        }

        public void NonGenericIListBasicRemove(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            for (int i = 0; i < items.Length; i++)
            {
                _ilist.Remove(items[i]);
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.Contains(items[i]), false); //"Expect to be same."
            }
            Assert.Equal(list.Count, 0); //"Expect to be same."
        }

        public void NonGenericIListExtendedRemove(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));
            IList _ilist = list;
            list.AddRange(new TestCollection<T>(itemsY));
            for (int i = 0; i < itemsX.Length; i++)
            {
                _ilist.Remove(itemsX[i]);
            }
            Assert.Equal(list.Count, itemsY.Length); //"Expect to be same."
            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(list[i], itemsY[i]); //"Expect to be same."
            }
        }

        public void NonGenericIListRemoveMultipleExistingValue(T[] items, int sameFactor, int sameIndex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            int sameCountExpected = (sameFactor * 2) + 1;
            T sameVal = items[sameIndex];
            for (int i = 0; i < sameFactor; i++)
            {
                list.AddRange(new TestCollection<T>(items));
                list.Add(sameVal);
            }
            for (int i = 0; i < sameCountExpected; i++)
            {
                _ilist.Remove(sameVal);
            }
            Assert.Equal(list.Contains(sameVal), false); //"Expect to be same."
            for (int i = 0; i < 100; i++)
            {
                _ilist.Remove(sameVal);
            }
            Assert.Equal(list.Contains(sameVal), false); //"Expect to be same."
            Assert.Equal(list.Count, (items.Length - 1) * (sameFactor + 1)); //"Expect to be same."
        }

        public void NonGenericIListRemoveNullValueWhenReference(T value)
        {
            if ((object)value != null)
            {
                throw new ArgumentException("invalid argument passed to testcase");
            }

            List<T> list = new List<T>();
            IList _ilist = list;
            list.Add(value);
            _ilist.Remove(value);
            Assert.Equal(list.Contains(value), false); //"Expect to be same."
        }

        public void NonGenericIListRemoveTestParams()
        {
            List<T> list = new List<T>();
            IList _ilist = list;

            list.Add(default(T));
            _ilist.Remove(new LinkedListNode<string>("item"));

            Assert.Equal(1, list.Count); //"Expect to be same."
        }

        #endregion

        #region RemoveAll(Pred<T>)

        public void RemoveAll_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            Predicate<T> expectedItemDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };
            bool typeNullable = default(T) == null;
            T[] expectedItems, matchingItems;
            int removeAllReturnValue;

            //[] Verify RemoveAll works correcty when removing every item
            list.Clear();
            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];

                Assert.Equal(1, (removeAllReturnValue = list.RemoveAll(expectedItemDelegate))); //"Err_56488ajodoe Expected RemoveAll"

                expectedItems = new T[items.Length - 1 - i];
                Array.Copy(items, i + 1, expectedItems, 0, expectedItems.Length);

                VerifyList(list, expectedItems);
            }

            //[] Verify RemoveAll removes every item when  the match returns true on every item
            list.Clear();
            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            Assert.Equal(items.Length, (removeAllReturnValue = list.RemoveAll(delegate (T item) { return true; }))); //"Err_2988092jaid Expected RemoveAll"
            VerifyList(list, new T[0]);

            //[] Verify RemoveAll removes no items when  the match returns false on every item
            list.Clear();
            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            Assert.Equal(0, (removeAllReturnValue = list.RemoveAll(delegate (T item) { return false; }))); //"Err_4848ajod Expected RemoveAll"
            VerifyList(list, items);

            //[] Verify RemoveAll that removes every other item
            list.Clear();
            matchingItems = new T[items.Length - (items.Length / 2)];
            expectedItems = new T[items.Length - matchingItems.Length];
            for (int i = 0; i < items.Length; ++i)
            {
                list.Add(items[i]);

                if ((i & 1) == 0)
                    matchingItems[i / 2] = items[i];
                else
                    expectedItems[i / 2] = items[i];
            }

            Assert.Equal(matchingItems.Length, (removeAllReturnValue = list.RemoveAll(delegate (T item) { return -1 != Array.IndexOf(matchingItems, item); }))); //"Err_50898ajhoid Expected RemoveAll"
            VerifyList(list, expectedItems);
        }

        public void RemoveAll_VerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();
            Predicate<T> predicate = delegate (T item) { return true; };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.RemoveAll(null)); //"Err_858ahia Expected null match to throw ArgumentNullException"
        }

        private void VerifyList(List<T> list, T[] expectedItems)
        {
            Assert.Equal(list.Count, expectedItems.Length); //"Err_2828ahid Expected the same"

            //Only verify the indexer. List should be in a good enough state that we
            //do not have to verify consistancy with any other method.
            for (int i = 0; i < list.Count; ++i)
            {
                Assert.Equal(expectedItems[i], list[i]); //"Err_19818ayiadb Expceted same stuff in list."
            }
        }

        #endregion

        #region RemoveAT

        public void BasicRemoveAt(T[] items, int startindex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            for (int i = items.Length - 1; i >= startindex; i--)
            {
                list.RemoveAt(i);
            }
            Assert.Equal(list.Count, items.Length - (items.Length - startindex)); //"Expect to be the same."
            for (int i = 0; i < startindex; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expect to be the same."
            }

            list = new List<T>(new TestCollection<T>(items));
            for (int i = 0; i < items.Length - startindex; i++)
            {
                list.RemoveAt(startindex);
            }
            Assert.Equal(list.Count, items.Length - (items.Length - startindex)); //"Expect to be the same."
            for (int i = 0; i < startindex; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expect to be the same."
            }

            list = new List<T>(new TestCollection<T>(items));
            for (int i = 0; i < startindex; i++)
            {
                list.RemoveAt(0);
            }
            Assert.Equal(list.Count, items.Length - startindex); //"Expect to be the same."
            for (int i = 0; i < items.Length - startindex; i++)
            {
                Assert.Equal(list[i], items[i + startindex]); //"Expect to be the same."
            }

            list = new List<T>(new TestCollection<T>(items));
            for (int i = startindex - 1; i >= 0; i--)
            {
                list.RemoveAt(i);
            }
            Assert.Equal(list.Count, items.Length - startindex); //"Expect to be the same."
            for (int i = 0; i < items.Length - startindex; i++)
            {
                Assert.Equal(list[i], items[i + startindex]); //"Expect to be the same."
            }
        }

        public void RemoveAtValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            int[] bad = new int[] { items.Length, items.Length + 1, int.MaxValue, -1, -2, int.MinValue };
            for (int i = 0; i < bad.Length; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(bad[i])); //"ArgumentOutOfRangeException expected."
            }
        }

        public void RemoveAtNullValueWhenReference(T value)
        {
            if ((object)value != null)
            {
                throw new ArgumentException("invalid argument passed to testcase");
            }

            List<T> list = new List<T>();
            list.Add(value);
            list.RemoveAt(0);
            Assert.Equal(list.Contains(value), false); //"Expected to be the same."
        }

        public void NonGenericIListBasicRemoveAt(T[] items, int startindex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            for (int i = items.Length - 1; i >= startindex; i--)
            {
                _ilist.RemoveAt(i);
            }
            Assert.Equal(list.Count, items.Length - (items.Length - startindex)); //"Expected to be the same."
            for (int i = 0; i < startindex; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expected to be the same."
            }

            list = new List<T>(new TestCollection<T>(items));
            _ilist = list;
            for (int i = 0; i < items.Length - startindex; i++)
            {
                _ilist.RemoveAt(startindex);
            }
            Assert.Equal(list.Count, items.Length - (items.Length - startindex)); //"Expected to be the same."
            for (int i = 0; i < startindex; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expected to be the same."
            }

            list = new List<T>(new TestCollection<T>(items));
            _ilist = list;
            for (int i = 0; i < startindex; i++)
            {
                _ilist.RemoveAt(0);
            }
            Assert.Equal(list.Count, items.Length - startindex); //"Expected to be the same."
            for (int i = 0; i < items.Length - startindex; i++)
            {
                Assert.Equal(list[i], items[i + startindex]); //"Expected to be the same."
            }

            list = new List<T>(new TestCollection<T>(items));
            _ilist = list;
            for (int i = startindex - 1; i >= 0; i--)
            {
                _ilist.RemoveAt(i);
            }
            Assert.Equal(list.Count, items.Length - startindex); //"Expected to be the same."
            for (int i = 0; i < items.Length - startindex; i++)
            {
                Assert.Equal(list[i], items[i + startindex]); //"Expected to be the same."
            }
        }

        public void NonGenericIListRemoveAtValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            int[] bad = new int[] { items.Length, items.Length + 1, int.MaxValue, -1, -2, int.MinValue };
            for (int i = 0; i < bad.Length; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => _ilist.RemoveAt(bad[i])); //"ArgumentOutOfRangeException expected."
            }
        }

        public void NonGenericIListRemoveAtNullValueWhenReference(T value)
        {
            if ((object)value != null)
            {
                throw new ArgumentException("invalid argument passed to testcase");
            }

            List<T> list = new List<T>();
            IList _ilist = list;
            list.Add(value);
            _ilist.RemoveAt(0);
            Assert.Equal(list.Contains(value), false); //"Expected to be the same."
        }

        #endregion

        #region RemoveRange

        public void BasicRemoveRange(T[] items, int index, int count)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            list.RemoveRange(index, count);
            Assert.Equal(list.Count, items.Length - count); //"Expected them to be the same."
            for (int i = 0; i < index; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expected them to be the same."
            }

            for (int i = index; i < items.Length - (index + count); i++)
            {
                Assert.Equal(list[i], items[i + count]); //"Expected them to be the same."
            }
        }

        public void RemoveRangeValidations(T[] items)
        {
            //
            //Always send items.Length is even
            //
            List<T> list = new List<T>(new TestCollection<T>(items));
            int[] bad = new int[]
            {
                items.Length,1,
                items.Length+1,0,
                items.Length+1,1,
                items.Length,2,
                items.Length/2,items.Length/2+1,
                items.Length-1,2,
                items.Length-2,3,
                1,items.Length,
                0,items.Length+1,
                1,items.Length+1,
                2,items.Length,
                items.Length/2+1,items.Length/2,
                2,items.Length-1,
                3,items.Length-2,
            };
            int[] negBad = new int[]
            {
                -1,-1,
                -1,0,
                -1,1,
                -1,2,
                0,-1,
                1,-1,
                2,-1
            };

            for (int i = 0; i < bad.Length; i++)
            {
                Assert.Throws<ArgumentException>(() => list.RemoveRange(bad[i], bad[++i])); //"Expect ArgumentException."
            }

            for (int i = 0; i < negBad.Length; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveRange(negBad[i], negBad[++i])); //"Expect ArgumentOutOfRangeException"
            }
        }

        #endregion

        #region Reverse

        public void Reverse(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.Reverse();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list[i], items[items.Length - (i + 1)]); //"Expect them to be teh same."
            }
        }

        public void Reverse(T item, int repeat)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < repeat; i++)
                list.Add(item);

            list.Reverse();

            for (int i = 0; i < repeat; i++)
            {
                Assert.Equal(list[i], item); //"Expect them to be teh same."
            }
        }

        #endregion

        #region Reverse(int, int)

        public void Reverse(T[] items, int index, int count)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.Reverse(index, count);

            for (int i = 0; i < index; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expect them to be the same."
            }

            int j = 0;
            for (int i = index; i < index + count; i++)
            {
                Assert.Equal(list[i], items[index + count - (j + 1)]); //"Expect them to be the same."
                j++;
            }

            for (int i = index + count; i < items.Length; i++)
            {
                Assert.Equal(list[i], items[i]); //"Expect them to be the same."
            }
        }

        public void ReverseValidations(T[] items)
        {
            //
            //Always send items.Length is even
            //
            List<T> list = new List<T>(new TestCollection<T>(items));
            int[] bad = new int[]
            {
                items.Length,1,
                items.Length+1,0,
                items.Length+1,1,
                items.Length,2,
                items.Length/2,items.Length/2+1,
                items.Length-1,2,
                items.Length-2,3,
                1,items.Length,
                0,items.Length+1,
                1,items.Length+1,
                2,items.Length,
                items.Length/2+1,items.Length/2,
                2,items.Length-1,
                3,items.Length-2,
            };
            int[] negBad = new int[]
            {
                -1,-1,
                -1,0,
                -1,1,
                -1,2,
                0,-1,
                1,-1,
                2,-1
            };

            for (int i = 0; i < bad.Length; i++)
            {
                Assert.Throws<ArgumentException>(() => list.Reverse(bad[i], bad[++i])); //"Expect ArgumentException."
            }

            for (int i = 0; i < negBad.Length; i++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.Reverse(negBad[i], negBad[++i])); //"Expect ArgumentOutOfRangeException"
            }
        }

        #endregion
    }

    public class List_RemoveReverse
    {
        [Fact]
        public static void RemoveTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = i;
            }

            int[] intArr2 = new int[100];
            for (int i = 0; i < 100; i++)
            {
                intArr2[i] = i + 100;
            }

            IntDriver.BasicRemove(intArr1);
            IntDriver.ExtendedRemove(intArr1, intArr2);
            IntDriver.RemoveMultipleExistingValue(intArr1, 3, 5);
            IntDriver.RemoveMultipleExistingValue(intArr1, 17, 99);
            IntDriver.RemoveMultipleExistingValue(intArr1, 7, 0);
            IntDriver.RemoveEmptyCollection(0);
            IntDriver.NonGenericIListBasicRemove(intArr1);
            IntDriver.NonGenericIListExtendedRemove(intArr1, intArr2);
            IntDriver.NonGenericIListRemoveMultipleExistingValue(intArr1, 3, 5);
            IntDriver.NonGenericIListRemoveMultipleExistingValue(intArr1, 17, 99);
            IntDriver.NonGenericIListRemoveMultipleExistingValue(intArr1, 7, 0);
            IntDriver.NonGenericIListRemoveTestParams();

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            string[] stringArr2 = new string[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr2[i] = "SomeTestString" + (i + 100).ToString();
            }

            StringDriver.BasicRemove(stringArr1);
            StringDriver.ExtendedRemove(stringArr1, stringArr2);
            StringDriver.RemoveMultipleExistingValue(stringArr1, 3, 5);
            StringDriver.RemoveMultipleExistingValue(stringArr1, 17, 99);
            StringDriver.RemoveMultipleExistingValue(stringArr1, 7, 0);
            StringDriver.RemoveNullValueWhenReference(null);
            StringDriver.RemoveEmptyCollection(null);
            StringDriver.RemoveEmptyCollection(String.Empty);
            StringDriver.NonGenericIListBasicRemove(stringArr1);
            StringDriver.NonGenericIListExtendedRemove(stringArr1, stringArr2);
            StringDriver.NonGenericIListRemoveMultipleExistingValue(stringArr1, 3, 5);
            StringDriver.NonGenericIListRemoveMultipleExistingValue(stringArr1, 17, 99);
            StringDriver.NonGenericIListRemoveMultipleExistingValue(stringArr1, 7, 0);
            StringDriver.NonGenericIListRemoveNullValueWhenReference(null);
            StringDriver.NonGenericIListRemoveTestParams();
        }

        [Fact]
        public static void RemoveAllTests()
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

            intDriver.RemoveAll_VerifyVanilla(new int[0]);
            intDriver.RemoveAll_VerifyVanilla(new int[] { 1 });
            intDriver.RemoveAll_VerifyVanilla(intArray);

            stringDriver.RemoveAll_VerifyVanilla(new string[0]);
            stringDriver.RemoveAll_VerifyVanilla(new string[] { "1" });
            stringDriver.RemoveAll_VerifyVanilla(stringArray);
        }

        [Fact]
        public static void RemoveAtTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = i;
            }

            int[] intArr2 = new int[100];
            for (int i = 0; i < 100; i++)
            {
                intArr2[i] = i + 100;
            }

            IntDriver.BasicRemoveAt(intArr1, 50);
            IntDriver.BasicRemoveAt(intArr1, 0);
            IntDriver.BasicRemoveAt(intArr1, 99);
            IntDriver.NonGenericIListBasicRemoveAt(intArr1, 50);
            IntDriver.NonGenericIListBasicRemoveAt(intArr1, 0);
            IntDriver.NonGenericIListBasicRemoveAt(intArr1, 99);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            string[] stringArr2 = new string[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr2[i] = "SomeTestString" + (i + 100).ToString();
            }

            StringDriver.BasicRemoveAt(stringArr1, 50);
            StringDriver.BasicRemoveAt(stringArr1, 0);
            StringDriver.BasicRemoveAt(stringArr1, 99);
            StringDriver.RemoveAtNullValueWhenReference(null);
            StringDriver.NonGenericIListBasicRemoveAt(stringArr1, 50);
            StringDriver.NonGenericIListBasicRemoveAt(stringArr1, 0);
            StringDriver.NonGenericIListBasicRemoveAt(stringArr1, 99);
            StringDriver.NonGenericIListRemoveAtNullValueWhenReference(null);
        }

        [Fact]
        public static void RemoveAtTests_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = i;
            }
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            IntDriver.NonGenericIListRemoveAtValidations(intArr1);
            IntDriver.RemoveAtValidations(intArr1);
            StringDriver.RemoveAtValidations(stringArr1);
            StringDriver.NonGenericIListRemoveAtValidations(stringArr1);
        }

        [Fact]
        public static void RemoveRangeTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;

            IntDriver.BasicRemoveRange(intArr1, 50, 50);
            IntDriver.BasicRemoveRange(intArr1, 0, 50);
            IntDriver.BasicRemoveRange(intArr1, 50, 25);
            IntDriver.BasicRemoveRange(intArr1, 0, 25);
            IntDriver.BasicRemoveRange(intArr1, 75, 25);
            IntDriver.BasicRemoveRange(intArr1, 0, 100);
            IntDriver.BasicRemoveRange(intArr1, 0, 99);
            IntDriver.BasicRemoveRange(intArr1, 1, 1);
            IntDriver.BasicRemoveRange(intArr1, 99, 1);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            StringDriver.BasicRemoveRange(stringArr1, 50, 50);
            StringDriver.BasicRemoveRange(stringArr1, 0, 50);
            StringDriver.BasicRemoveRange(stringArr1, 50, 25);
            StringDriver.BasicRemoveRange(stringArr1, 0, 25);
            StringDriver.BasicRemoveRange(stringArr1, 75, 25);
            StringDriver.BasicRemoveRange(stringArr1, 0, 100);
            StringDriver.BasicRemoveRange(stringArr1, 0, 99);
            StringDriver.BasicRemoveRange(stringArr1, 1, 1);
            StringDriver.BasicRemoveRange(stringArr1, 99, 1);
        }

        [Fact]
        public static void RemoveRangeTests_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            IntDriver.RemoveRangeValidations(intArr1);
            StringDriver.RemoveRangeValidations(stringArr1);
        }

        [Fact]
        public static void ReverseTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            IntDriver.Reverse(intArr);
            IntDriver.Reverse(intArr[0], 5);
            IntDriver.Reverse(new int[] { });
            IntDriver.Reverse(new int[] { 1 });

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr[i] = "SomeTestString" + i.ToString();

            StringDriver.Reverse(stringArr);
            StringDriver.Reverse(stringArr[0], 5);
            StringDriver.Reverse(new string[] { });
            StringDriver.Reverse(new string[] { "string" });
            StringDriver.Reverse(new string[] { (string)null });
            StringDriver.Reverse(null, 5);
        }

        [Fact]
        public static void ReverseIntIntTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            IntDriver.Reverse(intArr, 3, 3);
            IntDriver.Reverse(intArr, 0, 10);
            IntDriver.Reverse(intArr, 10, 0);
            IntDriver.Reverse(intArr, 5, 5);
            IntDriver.Reverse(intArr, 0, 5);
            IntDriver.Reverse(intArr, 1, 9);
            IntDriver.Reverse(intArr, 9, 1);
            IntDriver.Reverse(intArr, 2, 8);
            IntDriver.Reverse(intArr, 8, 2);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr[i] = "SomeTestString" + i.ToString();

            StringDriver.Reverse(stringArr, 3, 3);
            StringDriver.Reverse(stringArr, 0, 10);
            StringDriver.Reverse(stringArr, 10, 0);
            StringDriver.Reverse(stringArr, 5, 5);
            StringDriver.Reverse(stringArr, 0, 5);
            StringDriver.Reverse(stringArr, 1, 9);
            StringDriver.Reverse(stringArr, 9, 1);
            StringDriver.Reverse(stringArr, 2, 8);
            StringDriver.Reverse(stringArr, 8, 2);
            StringDriver.Reverse(new string[] { "str1", null, "str2", }, 1, 2);
            StringDriver.Reverse(new string[] { "str1", null, "str2", }, 1, 1);
            StringDriver.Reverse(new string[] { null, null, "str2", null, null }, 1, 4);
        }

        [Fact]
        public static void ReverseIntIntTests_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            IntDriver.ReverseValidations(intArr);

            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr[i] = "SomeTestString" + i.ToString();

            StringDriver.ReverseValidations(stringArr);
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
    #endregion
}
