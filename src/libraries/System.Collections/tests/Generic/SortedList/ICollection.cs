// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using SortedList_SortedListUtils;
using SortedList_ICollection;

namespace SortedList_ICollection
{
    public class ICollectionTester<T>
    {
        private static ICollection s_pCollectionToTest;

        private static void CheckCount(int expectedCount)
        {
            Test.Eval(s_pCollectionToTest.Count == expectedCount, "Expected Count on ICollection to be " + expectedCount + ", but found " + s_pCollectionToTest.Count);
        }

        private static void CheckIsSynchronized(bool expectedIsSynchronized)
        {
            Test.Eval(s_pCollectionToTest.IsSynchronized == expectedIsSynchronized, "Expected IsSynchronized on ICollection to be " + expectedIsSynchronized + ", but found " + s_pCollectionToTest.IsSynchronized);
        }

        private static void CheckSyncRoot(object expectedSyncRoot)
        {
            Test.Eval(s_pCollectionToTest.SyncRoot != null, "Expected SyncRoot on ICollection to be non null actual " + s_pCollectionToTest.SyncRoot);

            Test.Eval(s_pCollectionToTest.SyncRoot == expectedSyncRoot, "Expected SyncRoot on ICollection to be " + expectedSyncRoot + ", but found " + s_pCollectionToTest.SyncRoot);

            try
            {
                lock (s_pCollectionToTest.SyncRoot)
                {
                }
            }
            catch (Exception e)
            {
                Test.Eval(false, "The following exception was thrown while trying to lock on SyncRoot\n" + e.ToString());
            }
        }


        private static void CheckCopyTo(Array expectedCopyTo)
        {
            CheckCopyTo(expectedCopyTo, false, false);
        }

        private static void CheckCopyTo(Array expectedCopyTo, bool expectCopyToItemsOutOfOrder)
        {
            CheckCopyTo(expectedCopyTo, expectCopyToItemsOutOfOrder, false);
        }

        private static void CheckCopyTo(Array expectedCopyTo, bool expectCopyToItemsOutOfOrder, bool copyToOnlySupportsZeroLowerBounds)
        {
            Object[] arrayToCopyTo = null;
            T[] tArrayToCopyTo = new T[expectedCopyTo.Length];
            Object[,] arrayToCopyToMulti = new Object[1, 1];
            MyFooType[] badCastReferenceTypeArray = new MyFooType[expectedCopyTo.Length];
            MyValueType[] badCastValueTypeArray = new MyValueType[expectedCopyTo.Length];

            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyTo, 0);
                Test.Eval(false, "Expected ArgumentNullException when attempting to copy to null Array from ICollection.");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to null Array from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyToMulti, 0);
                Test.Eval(false, "Expected ArgumentException when attempting to copy to Multidimensional Array from ICollection.");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to Multidimensional Array from ICollection: " + E);
            }

            arrayToCopyTo = new Object[expectedCopyTo.Length - 1];
            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyTo, 0);
                Test.Eval(false, "Expected ArgumentException when attempting to copy to smaller Array from ICollection.");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to smaller Array from ICollection: " + E);
            }

            arrayToCopyTo = new Object[expectedCopyTo.Length];
            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyTo, expectedCopyTo.Length);
                Test.Eval(false, "Expected ArgumentException when attempting to copy to array at index >= Length of Array from ICollection.");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to array at index >= Length of Array from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyTo, -1);
                Test.Eval(false, "Expected ArgumentOutOfRangeException when attempting to copy to array at index < 0 from ICollection.");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to array at index < 0 from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyTo, 1);
                Test.Eval(false, "Expected ArgumentException when attempting to copy to array without enough room between index and end of array from ICollection.");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to array without enough room between index and end of array from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(badCastReferenceTypeArray, 0);
                Test.Eval(false, "Expected ArrayTypeMismatchException when attempting to copy to array that cannot be cast to from ICollection.");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to reference type array that cannot be cast to from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(badCastValueTypeArray, 0);
                Test.Eval(false, "Err_292haied Expected ArrayTypeMismatchException when attempting to copy to value type array that cannot be cast to from ICollection.");
            }
#if WINCORESYS
		catch ( System.InvalidCastException) 
		{		
		}
#endif
            catch (ArgumentException)
            {
            }
            catch (Exception E)
            {
                Test.Eval(false, "Unknown Exception when attempting to copy to array that cannot be cast to from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(arrayToCopyTo, 0);

                if (arrayToCopyTo.Length == expectedCopyTo.Length)
                {
                    if (expectCopyToItemsOutOfOrder)
                    {
                        Test.Eval(VerifyItemsOutOfOrder(expectedCopyTo, arrayToCopyTo, 0), "Err_70928ahpg Expected items and actual item differ");
                    }
                    else
                    {
                        Test.Eval(VerifyItemsInOrder(expectedCopyTo, arrayToCopyTo, 0), "Err_5688pqygb Expected items and actual item differ");
                    }
                }
                else
                {
                    Test.Eval(false, "Expected copied array length of " + expectedCopyTo.Length + ", but found a length of " + arrayToCopyTo.Length);
                }
            }
            catch (Exception E)
            {
                Test.Eval(false, "Err_550578oqpg Unknown Exception when attempting to copy to array without enough room between index and end of array from ICollection: " + E);
            }

            try
            {
                s_pCollectionToTest.CopyTo(tArrayToCopyTo, 0);

                if (tArrayToCopyTo.Length == expectedCopyTo.Length)
                {
                    if (expectCopyToItemsOutOfOrder)
                    {
                        Test.Eval(VerifyItemsOutOfOrderT(expectedCopyTo, tArrayToCopyTo, 0), "Err_336879pqicbx Expected items and actual item differ");
                    }
                    else
                    {
                        Test.Eval(VerifyItemsInOrderT(expectedCopyTo, tArrayToCopyTo, 0), "Err_35488qpag Expected items and actual item differ");
                    }
                }
                else
                {
                    Test.Eval(false, "Expected copied array length of " + expectedCopyTo.Length + ", but found a length of " + tArrayToCopyTo.Length);
                }
            }
            catch (Exception E)
            {
                Test.Eval(false, "Err_7839hpqg Unknown Exception when attempting to copy to T[] using CopyTo from ICollection: " + E);
            }

            //[] Non zero index
            try
            {
                tArrayToCopyTo = new T[expectedCopyTo.Length + 1];
                s_pCollectionToTest.CopyTo(tArrayToCopyTo, 1);

                if (expectCopyToItemsOutOfOrder)
                {
                    Test.Eval(VerifyItemsOutOfOrderT(expectedCopyTo, tArrayToCopyTo, 1), "Err_56888apahpg Expected items and actual item differ");
                }
                else
                {
                    Test.Eval(VerifyItemsInOrderT(expectedCopyTo, tArrayToCopyTo, 1), "Err_00289aogs Expected items and actual item differ");
                }
            }
            catch (Exception E)
            {
                Test.Eval(false, "Err_78928pqyb Unknown Exception when attempting to copy to T[] using CopyTo from ICollection: " + E);
            }

            if (copyToOnlySupportsZeroLowerBounds)
            {
                //[] Non zero lower bounds
                try
                {
                    Array tempArray = Array.CreateInstance(typeof(Object), new int[] { expectedCopyTo.Length + 8 }, new int[] { -4 });
                    s_pCollectionToTest.CopyTo(tempArray, 0);

                    Test.Eval(false, "Expected Argument when attempting to copy to array that has a non zero lower bound");
                }
                catch (ArgumentException)
                {
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (Exception E)
                {
                    Test.Eval(false, "Unknown Exception when attempting to copy to array that has a non zero lower bound: " + E);
                }
            }
        }

        private static bool VerifyItemsInOrder(Array expectedItems, Object[] actualItems, int actualItemsIndex)
        {
            for (int i = 0; i < expectedItems.Length; ++i)
            {
                if (!Test.Eval(expectedItems.GetValue(i).Equals(actualItems[i + actualItemsIndex]),
                    String.Format("Err_787892opyes Items differ expected={0} actual={1} at {2}",
                    expectedItems.GetValue(i), actualItems[i + actualItemsIndex], i)))
                {
                    return false;
                }
            }

            return true;
        }

#if !DESKTOP
        private static List<Object> cloneArray(Array alst)
        {
            List<Object> result = new List<Object>();
            foreach (Object obj in alst)
            {
                result.Add(obj);
            }
            return result;
        }
#endif
        private static bool VerifyItemsOutOfOrder(Array expectedItems, Object[] actualItems, int actualItemsIndex)
        {
#if DESKTOP
		ArrayList expectedItemsArrayList = new ArrayList(expectedItems);
#else
            List<Object> expectedItemsArrayList = cloneArray(expectedItems);
#endif
            int itemIndex;

            for (int i = 0; i < expectedItems.Length; ++i)
            {
                if (!Test.Eval(-1 != (itemIndex = expectedItemsArrayList.IndexOf(actualItems[i + actualItemsIndex])),
                    "Err_07092apqcv Unexpected item in actualItems " + actualItems[i + actualItemsIndex]))
                {
                    return false;
                }

                expectedItemsArrayList.RemoveAt(itemIndex);
            }

            if (!Test.Eval(expectedItemsArrayList.Count == 0, "Err_87092pqytb Unexpected items in expectedItems that were not actualItems"))
            {
                for (int i = 0; i < expectedItemsArrayList.Count; ++i)
                {
                    Console.WriteLine("\t" + expectedItemsArrayList[i]);
                }
                return false;
            }

            return true;
        }

        private static bool VerifyItemsInOrderT(Array expectedItems, T[] actualItems, int actualItemsIndex)
        {
            for (int i = 0; i < expectedItems.Length; ++i)
            {
                if (!Test.Eval(expectedItems.GetValue(i).Equals(actualItems[i + actualItemsIndex]),
                    String.Format("Err_787892opyes Items differ expected={0} actual={1} at {2}",
                    expectedItems.GetValue(i), actualItems[i + actualItemsIndex], i)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool VerifyItemsOutOfOrderT(Array expectedItems, T[] actualItems, int actualItemsIndex)
        {
#if DESKTOP
		ArrayList expectedItemsArrayList = new ArrayList(expectedItems);
#else
            List<Object> expectedItemsArrayList = cloneArray(expectedItems);
#endif
            int itemIndex;

            for (int i = 0; i < expectedItems.Length; ++i)
            {
                if (!Test.Eval(-1 != (itemIndex = expectedItemsArrayList.IndexOf(actualItems[i + actualItemsIndex])),
                    "Err_07092apqcv Unexpected item in actualItems " + actualItems[i + actualItemsIndex]))
                {
                    return false;
                }

                expectedItemsArrayList.RemoveAt(itemIndex);
            }

            if (!Test.Eval(expectedItemsArrayList.Count == 0, "Err_87092pqytb Unexpected items in expectedItems that were not actualItems"))
            {
                for (int i = 0; i < expectedItemsArrayList.Count; ++i)
                {
                    Console.WriteLine("\t" + expectedItemsArrayList[i]);
                }
                return false;
            }

            return true;
        }


        public static void RunTest(ICollection collectionToTest, int expectedCount, bool expectedIsSynchronized, object expectedSyncRoot,
            Array expectedCopyTo)
        {
            RunTest(collectionToTest, expectedCount, expectedIsSynchronized, expectedSyncRoot, expectedCopyTo, false);
        }

        public static void RunTest(ICollection collectionToTest, int expectedCount, bool expectedIsSynchronized, object expectedSyncRoot,
            Array expectedCopyTo, bool expectCopyToItemsOutOfOrder)
        {
            s_pCollectionToTest = collectionToTest;
            CheckCount(expectedCount);
            CheckIsSynchronized(expectedIsSynchronized);
            CheckSyncRoot(expectedSyncRoot);
            CheckCopyTo(expectedCopyTo, expectCopyToItemsOutOfOrder);
        }

        public static void RunTest(ICollection collectionToTest, int expectedCount, bool expectedIsSynchronized, object expectedSyncRoot,
            Array expectedCopyTo, bool expectCopyToItemsOutOfOrder, bool copyToOnlySupportsZeroLowerBounds)
        {
            s_pCollectionToTest = collectionToTest;
            CheckCount(expectedCount);
            CheckIsSynchronized(expectedIsSynchronized);
            CheckSyncRoot(expectedSyncRoot);
            CheckCopyTo(expectedCopyTo, expectCopyToItemsOutOfOrder, copyToOnlySupportsZeroLowerBounds);
        }

        public class MyFooType
        {
        }

        public struct MyValueType
        {
        }
    }
}