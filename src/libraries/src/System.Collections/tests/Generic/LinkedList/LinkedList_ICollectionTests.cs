// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace LinkedList_LinkedList_ICollectionTests
{
    public class LinkedList_ICollectionTests
    {
        [Fact]
        public static void ModifiedCollection_Test()
        {
            int arraySize = 16;
            Object[] intItems = new Object[arraySize];
            Object[] stringItems = new Object[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = i;
                stringItems[i] = i.ToString();
            }
            LinkedList<Object> linkedList = new LinkedList<Object>();
            for (int i = 0; i < intItems.Length; ++i)
                linkedList.AddLast(intItems[i]);

            LinkedList_T_Tests<Object> helper = new LinkedList_T_Tests<Object>();
            helper.ModifiedCollection_Test(linkedList, intItems);

            linkedList = new LinkedList<Object>();
            for (int i = 0; i < stringItems.Length; ++i)
                linkedList.AddLast(stringItems[i]);

            helper.ModifiedCollection_Test(linkedList, stringItems);
        }

        [Fact]
        public static void CopyTo_Tests_Negative()
        {
            int arraySize = 16;
            LinkedList<Object> linkedList = new LinkedList<Object>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(i.ToString());
            LinkedList_T_Tests<Object> helper = new LinkedList_T_Tests<Object>();
            helper.CopyTo_Tests_Negative(linkedList);
        }

        [Fact]
        public static void CopyTo_Tests_Negative_Rank1Multidim1()
        {
            int arraySize = 16;
            LinkedList<Object> linkedList = new LinkedList<Object>();
            for (int i = 0; i < arraySize; ++i)
                linkedList.AddLast(i.ToString());
            LinkedList_T_Tests<Object> helper = new LinkedList_T_Tests<Object>();
            helper.CopyTo_Tests_Negative_Rank1Multidim1(linkedList);
        }

        [Fact]
        public static void CopyTo_Valid_Tests()
        {
            int arraySize = 16;
            Object[] intItems = new Object[arraySize];
            Object[] stringItems = new Object[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = i;
                stringItems[i] = i.ToString();
            }
            LinkedList<Object> linkedList = new LinkedList<Object>();
            for (int i = 0; i < intItems.Length; ++i)
                linkedList.AddLast(intItems[i]);

            LinkedList_T_Tests<Object> helper = new LinkedList_T_Tests<Object>();
            helper.CopyTo_Valid_Tests(linkedList, intItems);

            linkedList = new LinkedList<Object>();
            for (int i = 0; i < stringItems.Length; ++i)
                linkedList.AddLast(stringItems[i]);

            helper.CopyTo_Valid_Tests(linkedList, stringItems);
        }

        [Fact]
        public static void VerifyICollectionTests()
        {
            int arraySize = 16;
            Object[] intItems = new Object[arraySize];
            Object[] stringItems = new Object[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intItems[i] = i;
                stringItems[i] = i.ToString();
            }
            LinkedList<Object> linkedList = new LinkedList<Object>();
            for (int i = 0; i < intItems.Length; ++i)
                linkedList.AddLast(intItems[i]);

            LinkedList_T_Tests<Object> helper = new LinkedList_T_Tests<Object>();
            helper.InitialTests(linkedList, intItems);

            linkedList = new LinkedList<Object>();
            for (int i = 0; i < stringItems.Length; ++i)
                linkedList.AddLast(stringItems[i]);

            helper.InitialTests(linkedList, stringItems);
        }
    }
    /// <summary>
    /// Helper class that verifies some properties of the linked list.
    /// </summary>
    internal class LinkedList_T_Tests<T>
    {
        internal void InitialTests(ICollection _collection, T[] _items)
        {
            IsSynchronized_Tests(_collection);
            Count_Tests(_collection, _items);
            SyncRoot_Tests(_collection);
            VerifyEnumerator(_collection.GetEnumerator(), _items, _items.Length);
        }

        /// <summary>
        /// Runs tests when the collection has been modified after 
        /// the enumerator was created.
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void ModifiedCollection_Test(LinkedList<T> _collection, T[] _items)
        {
            IEnumerator enumerator;
            bool atEnd;
            Object currentItem;
            Action<LinkedList<T>> modifyCollection = (linkedList) =>
            {
                linkedList.AddLast(default(T));
                linkedList.RemoveLast();
            };

            //[] Verify Modifying collecton with new Enumerator
            enumerator = _collection.GetEnumerator();
            atEnd = _items.Length == 0;
            modifyCollection(_collection);

            VerifyModifiedEnumerator(enumerator, null, true, atEnd);

            //[] Verify enumerating to the first item
            //We can only do this test if there is more then 1 item in it
            //If the collection only has one item in it we will enumerate 
            //to the first item in the test "Verify enumerating the entire collection"
            if (1 < _items.Length)
            {
                enumerator = _collection.GetEnumerator();

                VerifyEnumerator(enumerator, _items, 1);

                //[] Verify Modifying collection on an enumerator that has enumerated to the first item in the collection
                currentItem = enumerator.Current;
                modifyCollection(_collection);

                VerifyModifiedEnumerator(enumerator, currentItem, false, false);

                enumerator = _collection.GetEnumerator();

                VerifyEnumerator(enumerator, _items, _items.Length / 2);

                //[] Verify Modifying collection on an enumerator that has enumerated part of the collection
                currentItem = enumerator.Current;
                modifyCollection(_collection);

                VerifyModifiedEnumerator(enumerator, currentItem, false, false);
            }

            //[] Verify enumerating the entire collection
            enumerator = _collection.GetEnumerator();

            VerifyEnumerator(enumerator, _items, _items.Length);

            ////[] Verify Modifying collection on an enumerator that has enumerated the entire collection		
            //currentItem = enumerator.Current;
            //modifyCollection(_collection);

            //VerifyModifiedEnumerator(enumerator, currentItem, false, true);

            //[] Verify enumerating past the end of the collection
            enumerator = _collection.GetEnumerator();
            VerifyEnumerator(enumerator, _items, _items.Length);
            //[] Verify Modifying collection on an enumerator that has enumerated past the end of the collection		
            currentItem = null;
            modifyCollection(_collection);
            VerifyModifiedEnumerator(enumerator, currentItem, true, true);
        }

        /// <summary>
        /// Runs all of the invalid(argument checking) tests on CopyTo(Array).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void CopyTo_Tests_Negative(ICollection _collection)
        {
            LinkedList<Object> linkedList = new LinkedList<Object>();

            linkedList.AddLast(0);
            linkedList.AddLast(1);

            Assert.Throws<ArgumentException>(() => _collection.CopyTo(new int[8], 0)); //"Err_1235081anheid Expected ArgumentException to be thrown"

            Object[] itemObjectArray = null, tempItemObjectArray = null;
            Array itemArray = null, tempItemArray = null;

            //[] Verify CopyTo with null array
            Assert.Throws<ArgumentNullException>(() => _collection.CopyTo(null, 0)); //"Err_2470zsou: Expected ArgumentNullException with null array"

            // [] Verify CopyTo with index=Int32.MinValue
            itemObjectArray = GenerateArray(_collection.Count);
            tempItemObjectArray = (Object[])itemObjectArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(() => _collection.CopyTo(itemObjectArray, Int32.MinValue)); //"Err_68971aehps: Exception not thrown with index=Int32.MinValue"

            VerifyItems(itemObjectArray, tempItemObjectArray);

            // [] Verify CopyTo with index=-1
            itemObjectArray = GenerateArray(_collection.Count);
            tempItemObjectArray = (Object[])itemObjectArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(() => _collection.CopyTo(itemObjectArray, -1)); //"Err_3771zsiap: Exception not thrown with index=-1"

            VerifyItems(itemObjectArray, tempItemObjectArray);

            // [] Verify CopyTo with index=Int32.MaxValue
            itemObjectArray = GenerateArray(_collection.Count);
            tempItemObjectArray = (Object[])itemObjectArray.Clone();

            Assert.Throws<ArgumentException>(() => _collection.CopyTo(itemObjectArray, Int32.MaxValue)); //"Err_39744ahps: Exception not thrown with index=Int32.MaxValue"

            VerifyItems(itemObjectArray, tempItemObjectArray);

            // [] Verify CopyTo with index=array.length
            itemObjectArray = GenerateArray(_collection.Count);
            tempItemObjectArray = (Object[])itemObjectArray.Clone();

            Assert.Throws<ArgumentException>(() => _collection.CopyTo(itemObjectArray, _collection.Count)); //"Err_2078auoz: Exception not thow with index=array.Length"

            VerifyItems(itemObjectArray, tempItemObjectArray);

            // [] Verify CopyTo with collection.Count > array.length - index
            itemObjectArray = GenerateArray(_collection.Count + 1);
            tempItemObjectArray = (Object[])itemObjectArray.Clone();

            Assert.Throws<ArgumentException>(() => _collection.CopyTo(itemObjectArray, 2)); //"Err_1734nmzb: Exception not thrown with collection.Count > array.length - index"

            VerifyItems(itemObjectArray, tempItemObjectArray);

            //// [] Verify CopyTo with array is multidimetional
            //Assert.Throws<ArgumentException>(() => _collection.CopyTo(new Object[1, _collection.Count], 0),
            //    "Err_3708yzha: Exception not thrown with multidimentional array");


            GC.KeepAlive(typeof(MyInvalidValueType[]));
            GC.KeepAlive(typeof(MyInvalidReferenceType[]));

            //[] Verify CopyTo with invalid types
            Type[] invalidArrayTypes = new Type[] { typeof(MyInvalidReferenceType), typeof(MyInvalidValueType) };
            for (int i = 0; i < invalidArrayTypes.Length; ++i)
            {
                itemArray = Array.CreateInstance(invalidArrayTypes[i], _collection.Count);
                tempItemArray = (Array)itemArray.Clone();
                try
                {
                    _collection.CopyTo(itemArray, 0);
                    Assert.True(false, "Err_46387ueiacgz: Exception not thrown invalid array type: " + invalidArrayTypes[i]);
                }
                catch (ArrayTypeMismatchException) { }
                catch (ArgumentException) { }
                VerifyItems(itemArray, tempItemArray);
            }
        }

        /// <summary>
        /// Runs all of the invalid(argument checking) tests on CopyTo(Array).
        ///   (This is the subset of CopyTo_Tests_Negative that involves rank-1 multidim arrays. We split these off
        //    since not all of our products support rank-1 multidim arrays.)
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void CopyTo_Tests_Negative_Rank1Multidim1(ICollection _collection)
        {
            LinkedList<Object> linkedList = new LinkedList<Object>();

            linkedList.AddLast(0);
            linkedList.AddLast(1);

            Array itemArray = null, tempItemArray = null;

            // [] Verify CopyTo array LowerBounds=-5, Length=Collection.Count + 4, and index=-1
            itemArray = Array.CreateInstance(typeof(Object), new int[] { _collection.Count + 4 }, new int[] { -5 });
            tempItemArray = (Array)itemArray.Clone();
            Assert.Throws<ArgumentException>(() => _collection.CopyTo(itemArray, -1)); //"Err_0238188ajied Expected CopyTo to throw Exception wtih negative lowerbounds"

            // [] Verify CopyTo array LowerBounds=-4, Length=Collection.Count + 5, and index=1
            itemArray = Array.CreateInstance(typeof(Object), new int[] { _collection.Count + 5 }, new int[] { -4 });
            tempItemArray = (Array)itemArray.Clone();
            Assert.Throws<ArgumentException>(() => _collection.CopyTo(itemArray, 1)); //"Err_28280aneiud Expected CopyTo to throw Exception wtih negative lowerbounds"

            // [] Verify CopyTo array LowerBounds=-4, Length=Collection.Count + 4, and index=1
            itemArray = Array.CreateInstance(typeof(Object), new int[] { _collection.Count + 8 }, new int[] { -4 });
            tempItemArray = (Array)itemArray.Clone();

            Assert.Throws<ArgumentException>(() => _collection.CopyTo(itemArray, 0)); //"Err_215084aheibcx: Exception not thrown with  array LowerBounds=-4, Length=Collection.Count + 4, and index=0"

            VerifyItems(itemArray, tempItemArray);
        }



        /// <summary>
        /// Runs all of the valid test on CopyTo(Array).
        /// </summary>
        /// <returns>true if all of the test passed else false.</returns>
        internal void CopyTo_Valid_Tests(ICollection _collection, T[] _items)
        {
            Object[] itemObjectArray = null, tempItemObjectArray = null;

            // [] CopyTo with index=0 and the array is 4 items larger then size as the collection
            itemObjectArray = GenerateArrayWithRandomItems(_items.Length + 4);
            tempItemObjectArray = new Object[_items.Length + 4];

            Array.Copy(itemObjectArray, tempItemObjectArray, itemObjectArray.Length);
            Array.Copy(_items, 0, tempItemObjectArray, 0, _items.Length);
            _collection.CopyTo(itemObjectArray, 0);

            VerifyItems(itemObjectArray, tempItemObjectArray);

            // [] CopyTo with index=4 and the array is 4 items larger then size as the collection
            itemObjectArray = GenerateArrayWithRandomItems(_items.Length + 4);
            tempItemObjectArray = new Object[_items.Length + 4];

            Array.Copy(itemObjectArray, tempItemObjectArray, itemObjectArray.Length);
            Array.Copy(_items, 0, tempItemObjectArray, 4, _items.Length);
            _collection.CopyTo(itemObjectArray, 4);

            VerifyItems(itemObjectArray, tempItemObjectArray);

            // [] CopyTo with index=4 and the array is 8 items larger then size as the collection
            itemObjectArray = GenerateArrayWithRandomItems(_items.Length + 8);
            tempItemObjectArray = new Object[_items.Length + 8];

            Array.Copy(itemObjectArray, tempItemObjectArray, itemObjectArray.Length);
            Array.Copy(_items, 0, tempItemObjectArray, 4, _items.Length);
            _collection.CopyTo(itemObjectArray, 4);

            VerifyItems(itemObjectArray, tempItemObjectArray);

            //[] Verify CopyTo with valid types
            Type[] validTypes = new Type[] { typeof(object), typeof(T) };
            for (int i = 0; i < validTypes.Length; i++)
            {
                _collection.CopyTo(Array.CreateInstance(validTypes[i], _collection.Count), 0);
                VerifyItems(itemObjectArray, tempItemObjectArray);
            }
        }

        /// <summary>
        /// Runs all of the tests on Count.
        /// </summary>
        internal void Count_Tests(ICollection collection, T[] expected)
        {
            Assert.Equal(expected.Length, collection.Count); //"Err_6487pqtw Count"
        }

        internal void IsSynchronized_Tests(ICollection collection)
        {
            Assert.False(collection.IsSynchronized); //"Err_3148oyzx  should not be: IsSynchronized"
        }

        internal void SyncRoot_Tests(ICollection collection)
        {
            //[] Verify SyncRoot is not null
            Assert.NotNull(collection.SyncRoot); //"Err_3871aehhd SyncRoot is null"

            //[] Verify SyncRoot returns consistent results
            Object syncRoot1 = collection.SyncRoot;
            Object syncRoot2 = collection.SyncRoot;

            Assert.NotNull(syncRoot1); //"Err_9791aehad SyncRoot is null"

            Assert.Equal(syncRoot1, syncRoot2); //"Err_4894ahea SyncRoot is did not return the same result"

            //[] Verify SyncRoot can be used in a lock statement
            lock (collection.SyncRoot) { }

            //[] Verify that calling SyncRoot on different collections returns different values
            for (int i = 0; i < 3; i++)
            {
                ICollection c = new LinkedList<T>();
                Assert.NotEqual(collection.SyncRoot, c.SyncRoot); //"Err_35689eaps SyncRoot returned on this collection and on another collection returned the same value iteration= " + i
            }

            Assert.Equal(typeof(Object), collection.SyncRoot.GetType()); //"Err_47235fsd! Expected SyncRoot to be an object"
        }

        #region Helper Methods

        /// <summary>
        /// Verifies that the non-generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyEnumerator(IEnumerator enumerator, T[] expectedItems, int expectedCount)
        {
            int iterations = 0;

            //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    object tempCurrent = enumerator.Current;
                }
                catch (InvalidOperationException) { }
            }

            // There is no sequential order to the collection, so we're testing that all the items
            // in the readonlydictionary exist in the array.
            bool[] itemsVisited = new bool[expectedCount];
            bool itemFound;
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                object currentItem = enumerator.Current;
                object tempItem;

                //[] Verify we have not gotten more items then we expected                
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned fromt the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

                //[] Verify Current returned the correct value
                itemFound = false;

                for (int i = 0; i < itemsVisited.Length; ++i)
                {
                    if (!itemsVisited[i] && expectedItems[i].Equals(currentItem))
                    {
                        itemsVisited[i] = true;
                        itemFound = true;
                        break;
                    }
                }
                Assert.True(itemFound, "Err_1432pauy Current returned unexpected value=" + currentItem);

                //[] Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistant results Current."
                }

                iterations++;
            }

            for (int i = 0; i < expectedCount; ++i)
            {
                Assert.True(itemsVisited[i], "Err_052848ahiedoi Expected Current to return true for item: " + expectedItems[i] + "index: " + i);
            }

            Assert.Equal(expectedCount, iterations); //"Err_658805eauz Number of items to iterate through"

            if (expectedCount == expectedItems.Length)
            {
                for (int i = 0; i < 3; i++)
                {
                    Assert.False(enumerator.MoveNext()); //"Err_2929ahiea Expected MoveNext to return false after " + iterations + " iterations"
                }

                //[] Verify non deterministic behavior of current every time it is called after the enumerator is positioned after the last item
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        object tempCurrent = enumerator.Current;
                    }
                    catch (InvalidOperationException) { }
                }
            }
        }

        private void VerifyModifiedEnumerator(IEnumerator enumerator, Object expectedCurrent, bool expectCurrentThrow, bool atEnd)
        {
            //[] Verify Current
            try
            {
                Object currentItem = enumerator.Current;

                if (!expectCurrentThrow)
                {
                    //[] Verify Current always returns the same value every time it is called
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.Equal(expectedCurrent, currentItem); //"Err_67894wphs Current is returning inconsistant results Current, Iteration: " + i
                        currentItem = enumerator.Current;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                Assert.True(expectCurrentThrow); //"Err_98715ajps: Did not expect Current to thow InvalidOperationException"
            }

            //[] Verify MoveNext()
            bool _moveNextAtEndThrowsOnModifiedCollection = true;
            if (!atEnd || _moveNextAtEndThrowsOnModifiedCollection)
            {
                try
                {
                    enumerator.MoveNext();
                    Assert.True(false); //"Err_2507poaq: MoveNext() should have thrown an exception on a modified collection"
                }
                catch (InvalidOperationException) { }
            }
            else
            {
                // The eumerator is positioned at the end of the collection and it shouldn't throw
                Assert.False(enumerator.MoveNext()); //"Err_3923lgtk: MoveNext() should have returned false at the end of the collection"
            }

            //[] Verify Reset()
            try
            {
                enumerator.Reset();
                Assert.True(false); //"Err_1087pypa: Reset() should have thrown an exception on a modified collection"
            }
            catch (InvalidOperationException) { }
        }

        private void VerifyItems(Object[] actualItems, Object[] expectedItems)
        {
            Assert.Equal(expectedItems.Length, actualItems.Length); //"Err_1707ahps The length of the items"
            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.Equal(actualItems[i], expectedItems[i]); //"Err_0722haps The actual item and expected items differ at index:" + i
            }
        }

        private void VerifyItems(Array actualItems, Array expectedItems)
        {
            Assert.Equal(expectedItems.Length, actualItems.Length); //"Err_1707ahps The length of the items"

            int actualItemsLowerBounds = actualItems.GetLowerBound(0);
            int expectedItemsLowerBounds = expectedItems.GetLowerBound(0);

            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.Equal(actualItems.GetValue(i + actualItemsLowerBounds), expectedItems.GetValue(i + expectedItemsLowerBounds)); //"Err_0722haps The actual item and expected items differ at index:" + i
            }
        }

        private Object[] GenerateArray(int length)
        {
            Object[] itemArray = new Object[length];

            for (int i = 0; i < length; ++i)
                itemArray[i] = i ^ length;

            return itemArray;
        }

        private Object[] GenerateArrayWithRandomItems(int length)
        {
            int[] random100Numbers = new int[]
            {
                987059828, 2051597057, 456044278, 638924432, 81065019, 1338449596, 1179280838, 74775441, 1679264085, 1191885267, 1743939948, 873186576, 950191312, 179425883, 1032088947,
                813931278, 2109084517, 204676815, 356594810, 1311812559, 267979139, 733516308, 616709511, 529673752, 193823767, 1935633780, 474790740, 1645097169, 1585981558, 807212128,
                1056347988, 2020143024, 889036798, 789045776, 1820039140, 314723398, 1266169527, 1854222194, 1636375747, 400773037, 1564240445, 2139502697, 1260629066, 1068311962, 1943637451,
                493839823, 105943382, 684688663, 1116646503, 1053620220, 1717854730, 1089314970, 1270224458, 1488094956, 445699286, 253543520, 1434887546, 2073854173, 445100665, 292914886,
                863658856, 1681667316, 636277530, 872051957, 135537279, 1871280571, 2131633425, 161145536, 506870390, 717365549, 1695245398, 254862323, 715784715, 2103305420, 1895055761,
                275960089, 1620370889, 1695881196, 733519948, 1847467591, 1829690398, 1937585724, 528450666, 532361338, 1236841045, 2114516665, 749918566, 1548425489, 343346490, 1566495620,
                1027319499, 1339799001, 1409121529, 1343460861, 1684597828, 2030056776, 1503225167, 388577109, 932774683, 72356880,
            };
            object[] returned = new object[length];
            for (int i = 0; i < length; i++)
                returned[i] = random100Numbers[i % random100Numbers.Length];

            return returned;
        }

        #endregion
    }
    /// <summary>
    /// Test Class to check for invalid type cast.
    /// </summary>
    internal class MyInvalidReferenceType { }
    /// <summary>
    /// Test Class to check for invalid type cast.
    /// </summary>
    internal struct MyInvalidValueType { }
}