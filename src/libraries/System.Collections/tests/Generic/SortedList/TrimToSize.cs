// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using SortedList_SortedListUtils;

namespace SortedListTrim
{
    public class Driver<K, V>
    {
        private Test m_test;

        public Driver(Test test)
        {
            m_test = test;
        }

        public void DefaultCtor(K[] keys, V[] values, bool expectResize)
        {
            SortedList<K, V> collection = new SortedList<K, V>();
            K[] existingKeys, newKeys;
            V[] existingValues, newValues;

            SplitArray<K>(keys, out existingKeys, out newKeys);
            SplitArray<V>(values, out existingValues, out newValues);

            for (int i = 0; i < existingKeys.Length; ++i)
            {
                collection.Add(existingKeys[i], existingValues[i]);
            }

            m_test.Eval(Verify(collection, existingKeys, existingValues, newKeys, newValues, expectResize), "Err_025891anhoy Verify with default ctor FAILD");
        }

        public void InitialSizeCtor(K[] keys, V[] values, int initialSize, bool expectResize)
        {
            SortedList<K, V> collection = new SortedList<K, V>(initialSize);
            K[] existingKeys, newKeys;
            V[] existingValues, newValues;

            SplitArray<K>(keys, out existingKeys, out newKeys);
            SplitArray<V>(values, out existingValues, out newValues);

            for (int i = 0; i < existingKeys.Length; ++i)
            {
                collection.Add(existingKeys[i], existingValues[i]);
            }

            m_test.Eval(Verify(collection, existingKeys, existingValues, newKeys, newValues, expectResize), "Err_58949ajhba Verify with intial capacity ctor FAILD");
        }

        public void EmptySortedList(K[] keys, V[] values, bool expectResize)
        {
            SortedList<K, V> collection = new SortedList<K, V>();

            m_test.Eval(Verify(collection, new K[0], new V[0], keys, values, expectResize), "Err_45648ahpba Verify with empty SortedList FAILD");
        }


        public void AddRemoveSome(K[] keys, V[] values, bool expectResize)
        {
            SortedList<K, V> collection = new SortedList<K, V>();
            K[] existingKeys, newKeys;
            V[] existingValues, newValues;

            SplitArray<K>(keys, out existingKeys, out newKeys);
            SplitArray<V>(values, out existingValues, out newValues);

            for (int i = 0; i < existingKeys.Length; ++i)
            {
                collection.Add(existingKeys[i], existingValues[i]);
            }

            for (int i = 0; i < newKeys.Length; ++i)
            {
                collection.Add(newKeys[i], newValues[i]);
            }


            for (int i = 0; i < newKeys.Length; ++i)
            {
                collection.Remove(newKeys[i]);
            }

            m_test.Eval(Verify(collection, existingKeys, existingValues, newKeys, newValues, expectResize), "Err_70712bas Add then Remove some of the items Test FAILED");
        }

        public void AddRemoveAll(K[] keys, V[] values, bool expectResize)
        {
            SortedList<K, V> collection = new SortedList<K, V>();

            for (int i = 0; i < keys.Length; ++i)
            {
                collection.Add(keys[i], values[i]);
            }

            for (int i = 0; i < keys.Length; ++i)
            {
                collection.RemoveAt(0);
            }

            m_test.Eval(Verify(collection, new K[0], new V[0], keys, values, expectResize), "Err_56498ahpba Add then Remove all of the items Test FAILED");
        }

        public void AddClear(K[] keys, V[] values, bool expectResize)
        {
            SortedList<K, V> collection = new SortedList<K, V>();

            for (int i = 0; i < keys.Length; ++i)
            {
                collection.Add(keys[i], values[i]);
            }

            collection.Clear();

            m_test.Eval(Verify(collection, new K[0], new V[0], keys, values, expectResize), "Err_46598ahpas Add then Clear Test FAILED");
        }


        public bool Verify(SortedList<K, V> collection, K[] originalKeys, V[] originalValues, K[] newKeys, V[] newValues, bool expectResize)
        {
            bool retValue = true;
            int expectedCapacity = expectResize ? originalKeys.Length : GetCapacity(collection);


            retValue &= m_test.Eval(originalKeys.Length == collection.Count, "Err_238897ahpba Expected s.Length=" + originalKeys.Length + " actual=" + collection.Count);
            collection.TrimExcess();
            collection.TrimExcess();
            collection.TrimExcess();
            retValue &= m_test.Eval(originalKeys.Length == collection.Count, "Err_77107ahbpa Expected s.Length=" + originalKeys.Length + " actual=" + collection.Count);
            retValue &= m_test.Eval(expectedCapacity == GetCapacity(collection), "Err_9649ahpba Expected capacity=" + originalKeys.Length + " actual=" + GetCapacity(collection));

            for (int i = 0; i < newKeys.Length; ++i)
            {
                collection.Add(newKeys[i], newValues[i]);
            }

            for (int i = 0; i < newKeys.Length; ++i)
            {
                collection.Remove(newKeys[i]);
            }

            for (int i = 0; i < originalKeys.Length; ++i)
            {
                retValue &= m_test.Eval(originalValues[i].Equals(collection[originalKeys[i]]), "Err_70171ahpba Expected s.Remove=" + originalValues[i] + " actual=" + collection[originalKeys[i]]);
            }

            for (int i = 0; i < originalKeys.Length; ++i)
            {
                collection.RemoveAt(0);
            }

            collection.TrimExcess();
            collection.TrimExcess();
            collection.TrimExcess();
            retValue &= m_test.Eval(0 == collection.Count, "Err_887894ahpba Expected q.Length=" + 0 + " actual=" + collection.Count);

            return retValue;
        }

        private int GetCapacity(SortedList<K, V> collection)
        {
            //System.Reflection.FieldInfo arrayField = collection.GetType().GetField("keys", System.Reflection.BindingFlags.Instance |System.Reflection.BindingFlags.NonPublic);

            //if(null == collection) {
            //    throw new ArgumentNullException("s");
            //}
            //if(null == arrayField) {
            //    throw new Exception("Could not get the underlying array that represents SortedList. The dev might have changed the name of the field");
            //}

            //K[] array = (K[])arrayField.GetValue(collection);

            //return array.Length;
            return collection.Keys.Count;
        }

        private static void SplitArray<T>(T[] array, out T[] firstHalfArray, out T[] secondHalfArray)
        {
            firstHalfArray = new T[array.Length / 2];
            secondHalfArray = new T[array.Length - firstHalfArray.Length];

            Array.Copy(array, 0, firstHalfArray, 0, firstHalfArray.Length);
            Array.Copy(array, firstHalfArray.Length, secondHalfArray, 0, secondHalfArray.Length);
        }
    }

    public class Meth_TrimExcess
    {
        [Fact]
        [ActiveIssue(754)]
        public static void TrimToSizeMain()
        {
            Test test = new Test();

            Driver<int, int> IntDriver = new Driver<int, int>(test);

            IntDriver.DefaultCtor(GenRndIntArray(10), GenRndIntArray(10), true);
            IntDriver.InitialSizeCtor(GenRndIntArray(1000), GenRndIntArray(1000), 1000, true);
            IntDriver.EmptySortedList(GenRndIntArray(10), GenRndIntArray(10), true);
            IntDriver.AddRemoveSome(GenRndIntArray(10), GenRndIntArray(10), true);
            IntDriver.AddRemoveAll(GenRndIntArray(10), GenRndIntArray(10), true);
            IntDriver.AddClear(GenRndIntArray(10), GenRndIntArray(10), true);

            IntDriver.DefaultCtor(GenRndIntArray(15), GenRndIntArray(15), false);
            IntDriver.InitialSizeCtor(GenRndIntArray(40), GenRndIntArray(40), 21, false);
            IntDriver.InitialSizeCtor(GenRndIntArray(20), GenRndIntArray(20), 20, true);
            IntDriver.AddRemoveSome(GenRndIntArray(15), GenRndIntArray(15), true);
            IntDriver.AddRemoveAll(GenRndIntArray(15), GenRndIntArray(15), true);
            IntDriver.AddClear(GenRndIntArray(15), GenRndIntArray(15), true);

            Driver<string, string> StringDriver = new Driver<string, string>(test);
            StringDriver.DefaultCtor(GenRndStrArray(10), GenRndStrArray(10), true);
            StringDriver.InitialSizeCtor(GenRndStrArray(1000), GenRndStrArray(1000), 1000, true);
            StringDriver.EmptySortedList(GenRndStrArray(10), GenRndStrArray(10), true);
            StringDriver.AddRemoveSome(GenRndStrArray(10), GenRndStrArray(10), true);
            StringDriver.AddRemoveAll(GenRndStrArray(10), GenRndStrArray(10), true);
            StringDriver.AddClear(GenRndStrArray(10), GenRndStrArray(10), true);

            StringDriver.DefaultCtor(GenRndStrArray(15), GenRndStrArray(15), false);
            StringDriver.InitialSizeCtor(GenRndStrArray(40), GenRndStrArray(40), 21, false);
            StringDriver.InitialSizeCtor(GenRndStrArray(20), GenRndStrArray(20), 20, true);
            StringDriver.AddRemoveSome(GenRndStrArray(15), GenRndStrArray(15), true);
            StringDriver.AddRemoveAll(GenRndStrArray(15), GenRndStrArray(15), true);
            StringDriver.AddClear(GenRndStrArray(15), GenRndStrArray(15), true);

            Assert.True(test.result);
        }



        //The maximum length a string can be when generating random strings
        public static readonly int MAX_RND_STRING_LENGTH = 256;

        private static string[] GenRndStrArray(int size)
        {
            string[] rndElements = new string[size];
            Random rndGen = new Random(-55);
            StringBuilder strBldr = new StringBuilder();

            for (int i = 0; i < rndElements.Length; ++i)
            {
                string value;

                do
                {
                    strBldr.Length = 0;

                    int strLength = rndGen.Next(1, MAX_RND_STRING_LENGTH);

                    for (int j = 0; j < strLength; j++)
                    {
                        strBldr.Append((char)rndGen.Next(32, 217));
                    }

                    value = strBldr.ToString();
                } while (-1 != Array.IndexOf(rndElements, value));

                rndElements[i] = value;
            }

            return rndElements;
        }

        private static int[] GenRndIntArray(int size)
        {
            int[] rndElements = new int[size];
            Random rndGen = new Random(-55);

            for (int i = 0; i < rndElements.Length; ++i)
            {
                int value;

                do
                {
                    value = rndGen.Next(Int32.MinValue, Int32.MaxValue);
                } while (-1 != Array.IndexOf(rndElements, value));

                rndElements[i] = value;
            }

            return rndElements;
        }
    }
}