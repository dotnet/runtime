// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace List_List_AsReadOnlyTests
{
    public class VerifyReadOnlyIList<T>
    {
        public static void Verify(IList<T> list, T[] items, Func<T> generateItem)
        {
            VerifyIsReadOnly(list, items);
            VerifyIndexOf(list, items);
            VerifyContains(list, items);
            VerifyItem_Get(list, items);
            CopyTo_Tests(list, items, generateItem);

            // IEnumerable<T> interface
            MoveNext_Tests(list, items);
            Current_Tests(list, items);
            Reset_Tests(list, items);
        }

        public static void VerifyExceptions(IList<T> list, T[] items, Func<T> generateItem)
        {
            VerifyAdd(list, items);
            VerifyClear(list, items);
            VerifyInsert(list, items);
            VerifyRemove(list, items);
            VerifyRemoveAt(list, items);
            VerifyItem_Set(list, items);
            CopyTo_Tests_Negative(list, items, generateItem);
        }

        private static void VerifyAdd(IList<T> list, T[] items)
        {
            int origCount = list.Count;

            //[]Try adding an item to the colleciton and verify Add throws NotSupportedException
            Assert.Throws<NotSupportedException>(() => list.Add(default(T))); //"Err_27027ahbz!!! Not supported Exception should have been thrown when calling Add on a readonly collection"
            Assert.Equal(origCount, list.Count); //string.Format("Err_7072habpo!!! Expected Count={0} actual={1} after calling Add on a readonly collection", origCount, list.Count)
        }

        private static void VerifyInsert(IList<T> list, T[] items)
        {
            int origCount = list.Count;

            //[]Verify Insert throws NotSupportedException
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(T))); //"Err_558449ahpba!!! Not supported Exception should have been thrown when calling Insert on a readonly collection"
            Assert.Equal(origCount, list.Count); //string.Format("Err_7072habpo!!! Expected Count={0} actual={1} after calling Add on a readonly collection", origCount, list.Count)
        }

        private static void VerifyClear(IList<T> list, T[] items)
        {
            int origCount = list.Count;

            //[]Verify Clear throws NotSupportedException
            Assert.Throws<NotSupportedException>(() => list.Clear()); //"Err_7027qhpa!!! Not supported Exception should have been thrown when calling Clear on a readonly collection"
            Assert.Equal(origCount, list.Count); //string.Format("Err_7072habpo!!! Expected Count={0} actual={1} after calling Add on a readonly collection", origCount, list.Count)
        }

        private static void VerifyRemove(IList<T> list, T[] items)
        {
            int origCount = list.Count;

            //[]Verify Remove throws NotSupportedException
            if (null != items && items.Length != 0)
            {
                Assert.Throws<NotSupportedException>(() => list.Remove(items[0])); //"Err_8207aahpb!!! Not supported Exception should have been thrown when calling Remove on a readonly collection"
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => list.Remove(default(T))); //"Err_8207aahpb!!! Not supported Exception should have been thrown when calling Remove on a readonly collection"
            }

            Assert.Equal(origCount, list.Count); //string.Format("Err_7072habpo!!! Expected Count={0} actual={1} after calling Add on a readonly collection", origCount, list.Count)
        }

        private static void VerifyRemoveAt(IList<T> list, T[] items)
        {
            int origCount = list.Count;

            //[]Verify RemoveAt throws NotSupportedException
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0)); //"Err_77894ahpba!!! Not supported Exception should have been thrown when calling RemoveAt on a readonly collection"

            Assert.Equal(origCount, list.Count); //string.Format("Err_7072habpo!!! Expected Count={0} actual={1} after calling Add on a readonly collection", origCount, list.Count)
        }

        private static void VerifyItem_Set(IList<T> list, T[] items)
        {
            //[]Verify Item_Set throws NotSupportedException
            Assert.Throws<NotSupportedException>(() => list[0] = default(T)); //"Err_77894ahpba!!! Not supported Exception should have been thrown when calling Item_Set on a readonly collection"
        }

        private static void VerifyIsReadOnly(IList<T> list, T[] items)
        {
            Assert.True(list.IsReadOnly); //"Err_44894phkni!!! Expected IsReadOnly to be false"
        }

        private static void VerifyIndexOf(IList<T> list, T[] items)
        {
            int index;
            for (int i = 0; i < items.Length; ++i)
            {
                Assert.True(i == (index = list.IndexOf(items[i])) || items[i].Equals(items[index]),
                    string.Format("Err_331697ahpba Expect IndexOf to return an index to item equal to={0} actual={1} IndexReturned={2} items Index={3}",
                    items[i], items[index], index, i));
            }
        }

        private static void VerifyContains(IList<T> list, T[] items)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                Assert.True(list.Contains(items[i]),
                    string.Format("Err_1568ahpa Expected Contains to return true with item={0} items index={1}",
                    items[i], i));
            }
        }

        private static void VerifyItem_Get(IList<T> list, T[] items)
        {
            Assert.Equal(items.Length, list.Count); //"Should have the same number of items in each list."

            for (int i = 0; i < items.Length; ++i)
            {
                Assert.Equal(items[i], list[i]); //string.Format("Err_70717ahbpa Expected list[{0}]={1} actual={2}", i, items[i], list[i])
            }
        }

        private static void Verify_IndexOf(IList<T> collection, T[] items)
        {
            Assert.Equal(items.Length, collection.Count); //"Err_669713ahzp Verifying IndexOf the count of the collection"
            for (int i = 0; i < items.Length; i++)
            {
                int indexOfRetVal = collection.IndexOf(items[i]);

                Assert.NotEqual(-1, indexOfRetVal); //"Err_1634pnyan Verifying IndexOf and expected item: " + items[i] + " to be in the colleciton. Index:" + i

                Assert.Equal(items[indexOfRetVal], items[i]); //string.Format("Err_88489apps Verifying IndexOf and the index returned is wrong expected to find {0} at {1} actually found {2} at {3}", items[i], i, items[indexOfRetVal], indexOfRetVal)

                Assert.Equal(items[i], collection[indexOfRetVal]); //string.Format("Err_32198ahps Verifying IndexOf and the index returned is wrong expected to find {0} at {1} actually found {2} at {3}", items[i], i, items[indexOfRetVal], indexOfRetVal)
            }
        }

        /// <summary>
        /// Runs all of the tests on CopyTo(Array).
        /// </summary>
        private static void CopyTo_Tests(IList<T> collection, T[] items, Func<T> generateItem)
        {
            T[] itemArray = null, tempItemsArray = null;

            // [] CopyTo with index=0 and the array is the same size as the collection
            itemArray = GenerateArray(items.Length, generateItem);

            collection.CopyTo(itemArray, 0);

            VerifyItem_Get(itemArray, items);

            // [] CopyTo with index=0 and the array is 4 items larger then size as the collection
            itemArray = GenerateArray(items.Length + 4, generateItem);
            tempItemsArray = new T[items.Length + 4];

            Array.Copy(itemArray, tempItemsArray, itemArray.Length);
            Array.Copy(items, 0, tempItemsArray, 0, items.Length);
            collection.CopyTo(itemArray, 0);

            VerifyItem_Get(itemArray, tempItemsArray);

            // [] CopyTo with index=4 and the array is 4 items larger then size as the collection
            itemArray = GenerateArray(items.Length + 4, generateItem);
            tempItemsArray = new T[items.Length + 4];

            Array.Copy(itemArray, tempItemsArray, itemArray.Length);
            Array.Copy(items, 0, tempItemsArray, 4, items.Length);
            collection.CopyTo(itemArray, 4);

            VerifyItem_Get(itemArray, tempItemsArray);

            // [] CopyTo with index=4 and the array is 8 items larger then size as the collection
            itemArray = GenerateArray(items.Length + 8, generateItem);
            tempItemsArray = new T[items.Length + 8];

            Array.Copy(itemArray, tempItemsArray, itemArray.Length);
            Array.Copy(items, 0, tempItemsArray, 4, items.Length);
            collection.CopyTo(itemArray, 4);

            VerifyItem_Get(itemArray, tempItemsArray);
        }

        private static void CopyTo_Tests_Negative(IList<T> collection, T[] items, Func<T> generateItem)
        {
            T[] itemArray = null, tempItemsArray = null;

            //[] Verify CopyTo with null array
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0)); //"Err_2470zsou: Exception not thrown with null array"

            // [] Verify CopyTo with index=Int32.MinValue
            itemArray = GenerateArray(items.Length, generateItem);

            tempItemsArray = (T[])itemArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.CopyTo(new T[collection.Count], Int32.MinValue)); //"Err_68971aehps: Exception not thrown with index=Int32.MinValue"

            //Verify that the array was not mutated 
            VerifyItem_Get(itemArray, tempItemsArray);

            // [] Verify CopyTo with index=-1
            itemArray = GenerateArray(items.Length, generateItem);
            tempItemsArray = (T[])itemArray.Clone();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => collection.CopyTo(new T[collection.Count], -1)); //"Err_3771zsiap: Exception not thrown with index=-1"

            //Verify that the array was not mutated 
            VerifyItem_Get(itemArray, tempItemsArray);

            // [] Verify CopyTo with index=Int32.MaxValue
            itemArray = GenerateArray(items.Length, generateItem);
            tempItemsArray = (T[])itemArray.Clone();

            Assert.Throws<ArgumentException>(() => collection.CopyTo(new T[collection.Count], Int32.MaxValue)); //"Err_39744ahps: Exception not thrown with index=Int32.MaxValue"

            //Verify that the array was not mutated 
            VerifyItem_Get(itemArray, tempItemsArray);

            if (items.Length > 0)
            {
                // [] Verify CopyTo with index=array.length
                itemArray = GenerateArray(items.Length, generateItem);
                tempItemsArray = (T[])itemArray.Clone();
                T[] output = new T[collection.Count];
                Assert.Throws<ArgumentException>(
                    () => collection.CopyTo(output, collection.Count)); //"Err_2078auoz: Exception not thow with index=array.Length"

                //Verify that the array was not mutated 
                VerifyItem_Get(itemArray, tempItemsArray);

                // [] Verify CopyTo with collection.Count > array.length - index
                itemArray = GenerateArray(items.Length + 1, generateItem);
                tempItemsArray = (T[])itemArray.Clone();
                Assert.Throws<ArgumentException>(() => collection.CopyTo(new T[items.Length + 1], 2)); //"Err_1734nmzb: Correct exception not thrown with collection.Count > array.length - index"

                //Verify that the array was not mutated 
                VerifyItem_Get(itemArray, tempItemsArray);
            }
        }

        /// <summary>
        /// Runs all of the tests on MoveNext().
        /// </summary>
        private static void MoveNext_Tests(IList<T> collection, T[] items)
        {
            int iterations = 0;
            IEnumerator<T> enumerator = collection.GetEnumerator();
            //[] Call MoveNext() untill the end of the collection has been reached

            while (enumerator.MoveNext())
                iterations++;

            Assert.Equal(items.Length, iterations); //"Err_64897adhs Number of items to iterate through"

            //[] Call MoveNext() several times after the end of the collection has been reached
            for (int j = 0; j < 3; j++)
            {
                try
                {
                    T tempCurrent = enumerator.Current;
                }
                catch (InvalidOperationException) { }//Behavior of Current here is undefined 

                Assert.False(enumerator.MoveNext(),
                    "Err_1081adohs Expected MoveNext() to return false on the " + (j + 1) + " after the end of the collection has been reached\n");
            }
        }

        /// <summary>
        /// Runs all of the tests on Current.
        /// </summary>
        private static void Current_Tests(IList<T> collection, T[] items)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();
            //[] Call MoveNext() untill the end of the collection has been reached

            VerifyEnumerator(enumerator, items, 0, items.Length);

            //[] Enumerate only part of the collection
            enumerator = collection.GetEnumerator();

            VerifyEnumerator(enumerator, items, 0, items.Length / 2);
        }

        /// <summary>
        /// Runs all of the tests on Reset().
        /// </summary>
        private static void Reset_Tests(IList<T> collection, T[] items)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();

            //[] Call Reset() several times on a new Enumerator then enumerate the collection
            VerifyEnumerator(enumerator, items, 0, items.Length);
            enumerator.Reset();

            //[] Enumerate part of the collection then call Reset() several times

            VerifyEnumerator(enumerator, items, 0, items.Length / 2);

            enumerator.Reset();
            enumerator.Reset();
            enumerator.Reset();

            //[] After Enumerating only part of the collection and Reset() was called several times and enumerate through the entire collection
            VerifyEnumerator(enumerator, items, 0, items.Length);

            enumerator.Reset();

            //[] Enumerate the entire collection then call Reset() several times
            VerifyEnumerator(enumerator, items, 0, items.Length);

            enumerator.Reset();
            enumerator.Reset();
            enumerator.Reset();

            //[] After Enumerating the entire collection and Reset() was called several times and enumerate through the entire collection
            VerifyEnumerator(enumerator, items, 0, items.Length);
            enumerator.Reset();
        }

        private static void VerifyEnumerator(IEnumerator<T> enumerator, T[] expectedItems, int startIndex, int count)
        {
            int iterations = 0;

            //[] Verify non deterministic behavior of current every time it is called before a call to MoveNext() has been made			
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    T tempCurrent = enumerator.Current;
                }
                catch (InvalidOperationException) { }
            }

            while ((iterations < count) && enumerator.MoveNext())
            {
                T currentItem = enumerator.Current;
                T tempItem;

                //[] Verify we have not gotten more items then we expected
                Assert.True(iterations < count, "Err_9844awpa More items have been returned fromt the enumerator(" + iterations
                    + " items) then are " + "in the expectedElements(" + count + " items)");

                //[] Verify Current returned the correct value
                Assert.Equal(expectedItems[startIndex + iterations], currentItem); //"Err_1432pauy Current returned unexpected value"

                //[] Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem); //"Err_8776phaw Current is returning inconsistant results Current."
                }

                iterations++;
            }

            Assert.Equal(count, iterations); //"Err_658805eauz Number of items to iterate through"

            if (expectedItems.Length == (count - startIndex))
            {
                //[] Verify non deterministic behavior of current every time it is called after the enumerator is positioned after the last item
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        T tempCurrent = enumerator.Current;
                    }
                    catch (InvalidOperationException) { }
                    Assert.False(enumerator.MoveNext()); //"Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations"
                }
            }
        }

        private static T[] GenerateArray(int length, Func<T> generateItem)
        {
            T[] items = new T[length];

            if (null != generateItem)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = generateItem();
                }
            }

            return items;
        }
    }
    public class Driver<T>
    {
        public void CheckType()
        {
            // VSWhidbey #378658
            List<T> list = new List<T>();
            ReadOnlyCollection<T> readOnlyList = new ReadOnlyCollection<T>(list);
            Assert.Equal(typeof(ReadOnlyCollection<T>), readOnlyList.GetType()); //"Err_1703r38abhpx Read Only Collection Type Test FAILED"
        }

        public void EmptyCollection(Func<T> generateItem, bool testExceptions)
        {
            List<T> list = new List<T>();
            if (testExceptions)
                VerifyReadOnlyIList<T>.VerifyExceptions(new ReadOnlyCollection<T>(list), new T[0], generateItem);
            else
                VerifyReadOnlyIList<T>.Verify(new ReadOnlyCollection<T>(list), new T[0], generateItem);
        }

        public void NonEmptyCollectionIEnumerableCtor(T[] items, Func<T> generateItem, bool testExceptions)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            if (testExceptions)
                VerifyReadOnlyIList<T>.VerifyExceptions(new ReadOnlyCollection<T>(list), items, generateItem);
            else
                VerifyReadOnlyIList<T>.Verify(new ReadOnlyCollection<T>(list), items, generateItem);
            //, "Err_884964ahbz NON Empty Collection using the IEnumerable constructor to populate the list Test FAILED");
        }

        public void NonEmptyCollectionAdd(T[] items, Func<T> generateItem, bool testExceptions)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            if (testExceptions)
                VerifyReadOnlyIList<T>.VerifyExceptions(new ReadOnlyCollection<T>(list), items, generateItem);
            else
                VerifyReadOnlyIList<T>.Verify(new ReadOnlyCollection<T>(list), items, generateItem);
        }

        public void AddRemoveSome(T[] items, T[] itemsToAdd, T[] itemsToRemove, Func<T> generateItem, bool testExceptions)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < itemsToAdd.Length; ++i)
                list.Add(itemsToAdd[i]);

            for (int i = 0; i < itemsToRemove.Length; ++i)
                list.Remove(itemsToRemove[i]);

            if (testExceptions)
                VerifyReadOnlyIList<T>.VerifyExceptions(new ReadOnlyCollection<T>(list), items, generateItem);
            else
                VerifyReadOnlyIList<T>.Verify(new ReadOnlyCollection<T>(list), items, generateItem);
        }

        public void AddRemoveAll(T[] items, Func<T> generateItem, bool testExceptions)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            for (int i = 0; i < items.Length; ++i)
                list.RemoveAt(0);
            if (testExceptions)
                VerifyReadOnlyIList<T>.VerifyExceptions(new ReadOnlyCollection<T>(list), new T[0], generateItem);
            else
                VerifyReadOnlyIList<T>.Verify(new ReadOnlyCollection<T>(list), new T[0], generateItem);
        }

        public void AddClear(T[] items, Func<T> generateItem, bool testExceptions)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            list.Clear();

            if (testExceptions)
                VerifyReadOnlyIList<T>.VerifyExceptions(new ReadOnlyCollection<T>(list), new T[0], generateItem);
            else
                VerifyReadOnlyIList<T>.Verify(new ReadOnlyCollection<T>(list), new T[0], generateItem);
        }
    }
    public class List_AsReadOnlyTests
    {
        [Fact]
        public static void CheckType()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            IntDriver.CheckType();
            stringDriver.CheckType();
        }

        [Fact]
        public static void EmptyCollection()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();

            IntDriver.EmptyCollection(refX1IntGenerator.NextValue, false);
            stringDriver.EmptyCollection(valX1stringGenerator.NextValue, false);
        }
        [Fact]
        public static void EmptyCollection_Negative()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();

            IntDriver.EmptyCollection(refX1IntGenerator.NextValue, true);
            stringDriver.EmptyCollection(valX1stringGenerator.NextValue, true);
        }

        [Fact]
        public static void NonEmptyCollectionIEnumerableCtor()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.NonEmptyCollectionIEnumerableCtor(intArr, refX1IntGenerator.NextValue, false);
            stringDriver.NonEmptyCollectionIEnumerableCtor(stringArr, valX1stringGenerator.NextValue, false);
        }
        [Fact]
        public static void NonEmptyCollectionIEnumerableCtor_Negative()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.NonEmptyCollectionIEnumerableCtor(intArr, refX1IntGenerator.NextValue, true);
            stringDriver.NonEmptyCollectionIEnumerableCtor(stringArr, valX1stringGenerator.NextValue, true);
        }

        [Fact]
        public static void NonEmptyCollectionAdd()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.NonEmptyCollectionAdd(intArr, refX1IntGenerator.NextValue, false);
            stringDriver.NonEmptyCollectionAdd(stringArr, valX1stringGenerator.NextValue, false);
        }
        [Fact]
        public static void NonEmptyCollectionAdd_Negative()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.NonEmptyCollectionAdd(intArr, refX1IntGenerator.NextValue, true);
            stringDriver.NonEmptyCollectionAdd(stringArr, valX1stringGenerator.NextValue, true);
        }

        [Fact]
        public static void AddRemoveSome()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.AddRemoveSome(intArrAfterRemove, intArr, intArrToRemove, refX1IntGenerator.NextValue, false);
            stringDriver.AddRemoveSome(stringArrAfterRemove, stringArr, stringArrToRemove, valX1stringGenerator.NextValue, false);
        }
        [Fact]
        public static void AddRemoveSome_Negative()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.AddRemoveSome(intArrAfterRemove, intArr, intArrToRemove, refX1IntGenerator.NextValue, true);
            stringDriver.AddRemoveSome(stringArrAfterRemove, stringArr, stringArrToRemove, valX1stringGenerator.NextValue, true);
        }

        [Fact]
        public static void AddRemoveAll()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.AddRemoveAll(intArr, refX1IntGenerator.NextValue, false);
            stringDriver.AddRemoveAll(stringArr, valX1stringGenerator.NextValue, false);
        }

        [Fact]
        public static void AddRemoveAll_Negative()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.AddRemoveAll(intArr, refX1IntGenerator.NextValue, true);
            stringDriver.AddRemoveAll(stringArr, valX1stringGenerator.NextValue, true);
        }

        [Fact]
        public static void AddClear()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.AddClear(intArr, refX1IntGenerator.NextValue, false);
            stringDriver.AddClear(stringArr, valX1stringGenerator.NextValue, false);
        }

        [Fact]
        public static void AddClear_Negative()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1IntGenerator refX1IntGenerator = new RefX1IntGenerator();
            RefX1<int>[] intArrToRemove, intArrAfterRemove;
            RefX1<int>[] intArr = PrepTests(refX1IntGenerator, 100, out intArrToRemove, out intArrAfterRemove);

            Driver<ValX1<string>> stringDriver = new Driver<ValX1<string>>();
            ValX1stringGenerator valX1stringGenerator = new ValX1stringGenerator();
            ValX1<string>[] stringArrToRemove, stringArrAfterRemove;
            ValX1<string>[] stringArr = PrepTests(valX1stringGenerator, 100, out stringArrToRemove, out stringArrAfterRemove);

            IntDriver.AddClear(intArr, refX1IntGenerator.NextValue, true);
            stringDriver.AddClear(stringArr, valX1stringGenerator.NextValue, true);
        }

        private static ValX1<string>[] PrepTests(ValX1stringGenerator generator, int count,
            out ValX1<string>[] toRemove, out ValX1<string>[] afterRemove)
        {
            ValX1<string>[] entireArray = new ValX1<string>[count];
            toRemove = new ValX1<string>[count / 2];
            afterRemove = new ValX1<string>[count / 2];
            for (int i = 0; i < 100; i++)
            {
                entireArray[i] = generator.NextValue();

                if ((i & 1) != 0)
                    toRemove[i / 2] = entireArray[i];
                else
                    afterRemove[i / 2] = entireArray[i];
            }

            return entireArray;
        }

        private static RefX1<int>[] PrepTests(RefX1IntGenerator generator, int count,
            out RefX1<int>[] toRemove, out RefX1<int>[] afterRemove)
        {
            RefX1<int>[] entireArray = new RefX1<int>[count];
            toRemove = new RefX1<int>[count / 2];
            afterRemove = new RefX1<int>[count / 2];
            for (int i = 0; i < 100; i++)
            {
                entireArray[i] = generator.NextValue();

                if ((i & 1) != 0)
                    toRemove[i / 2] = entireArray[i];
                else
                    afterRemove[i / 2] = entireArray[i];
            }

            return entireArray;
        }
    }
    #region Helper Classes

    /// <summary>
    /// Helps tests reference types.
    /// </summary>
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
    public class RefX1IntGenerator
    {
        private int _index;

        public RefX1IntGenerator()
        {
            _index = 1;
        }

        public RefX1<int> NextValue()
        {
            return new RefX1<int>(_index++);
        }
    }
    /// <summary>
    /// Helps test value types.
    /// </summary>
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
    public class ValX1stringGenerator
    {
        private int _index;

        public ValX1stringGenerator()
        {
            _index = 1;
        }

        public ValX1<string> NextValue()
        {
            return new ValX1<string>((_index++).ToString());
        }
    }
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
