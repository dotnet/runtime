// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class CopyToTests
    {
        [Fact]
        public void TestCopyToBasic()
        {
            Hashtable hash = null;        // the hashtable object which will be used in the tests
            object[] objArr = null;        // the object array corresponding to arr
            object[] objArr2 = null;        // helper object array
            object[,] objArrRMDim = null;       // multi dimensional object array

            // these are the keys and values which will be added to the hashtable in the tests
            object[] keys = new object[] {
                new object(),
                "Hello" ,
                "my array" ,
                new DateTime(),
                new SortedList(),
                typeof( System.Environment ),
                5
            };

            object[] values = new object[] {
                "Somestring" ,
                new object(),
                new int [] { 1, 2, 3, 4, 5 },
                new Hashtable(),
                new Exception(),
                new CopyToTests(),
                null
            };

            //[]test normal conditions, array is large enough to hold all elements

            // make new hashtable
            hash = new Hashtable();

            // put in values and keys
            for (int i = 0; i < values.Length; i++)
            {
                hash.Add(keys[i], values[i]);
            }

            // now try getting out the values using CopyTo method
            objArr = new object[values.Length + 2];

            // put a sentinal in first position, and make sure it is not overriden
            objArr[0] = "startstring";

            // put a sentinal in last position, and make sure it is not overriden
            objArr[values.Length + 1] = "endstring";
            hash.Values.CopyTo((Array)objArr, 1);

            // make sure sentinal character is still there
            Assert.Equal("startstring", objArr[0]);
            Assert.Equal("endstring", objArr[values.Length + 1]);

            // check to make sure arr is filled up with the correct elements

            objArr2 = new object[values.Length];
            Array.Copy(objArr, 1, objArr2, 0, values.Length);
            objArr = objArr2;

            Assert.True(CompareArrays(objArr, values));

            //[] This is the same test as before but now we are going to used Hashtable.CopyTo instead of Hasthabe.Values.CopyTo
            // now try getting out the values using CopyTo method
            objArr = new object[values.Length + 2];

            // put a sentinal in first position, and make sure it is not overriden
            objArr[0] = "startstring";

            // put a sentinal in last position, and make sure it is not overriden
            objArr[values.Length + 1] = "endstring";
            hash.CopyTo((Array)objArr, 1);

            // make sure sentinal character is still there

            Assert.Equal("startstring", objArr[0]);
            Assert.Equal("endstring", objArr[values.Length + 1]);

            // check to make sure arr is filled up with the correct elements
            BitArray bitArray = new BitArray(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                DictionaryEntry entry = (DictionaryEntry)objArr[i + 1];
                int valueIndex = Array.IndexOf(values, entry.Value);
                int keyIndex = Array.IndexOf(keys, entry.Key);

                Assert.NotEqual(-1, valueIndex);
                Assert.NotEqual(-1, keyIndex);
                Assert.Equal(valueIndex, keyIndex);

                bitArray[i] = true;
            }

            for (int i = 0; i < ((ICollection)bitArray).Count; i++)
            {
                Assert.True(bitArray[i]);
            }

            //[] Parameter validation

            //[] Null array

            Assert.Throws<ArgumentNullException>(() =>
                         {
                             hash = new Hashtable();
                             objArr = new object[0];
                             hash.CopyTo(null, 0);
                         }
            );

            //[] Multidimentional array
            Assert.Throws<ArgumentException>(() =>
                         {
                             hash = new Hashtable();
                             objArrRMDim = new object[16, 16];
                             hash.CopyTo(objArrRMDim, 0);
                         }
            );

            //[] Array not large enough
            Assert.Throws<ArgumentException>(() =>
                         {
                             hash = new Hashtable();
                             for (int i = 0; i < 256; i++)
                             {
                                 hash.Add(i.ToString(), i);
                             }

                             objArr = new object[hash.Count + 8];
                             hash.CopyTo(objArr, 9);
                         }
            );


            Assert.Throws<ArgumentException>(() =>
                         {
                             hash = new Hashtable();
                             objArr = new object[0];
                             hash.CopyTo(objArr, Int32.MaxValue);
                         }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                         {
                             hash = new Hashtable();
                             objArr = new object[0];
                             hash.CopyTo(objArr, Int32.MinValue);
                         }
            );

            //[]copy should throw because of outofrange
            Random random = new Random(-55);
            for (int iii = 0; iii < 20; iii++)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                                 {
                                     hash = new Hashtable();
                                     objArr = new object[0];
                                     hash.CopyTo(objArr, random.Next(-1000, 0));
                                 }
                );
            }

            //[]test when array is to small to hold all values hashtable has more values then array can hold

            hash = new Hashtable();

            // put in values and keys
            for (int i = 0; i < values.Length; i++)
            {
                hash.Add(keys[i], values[i]);
            }

            // now try getting out the values using CopyTo method into a small array
            objArr = new object[values.Length - 1];
            Assert.Throws<ArgumentException>(() =>
                         {
                             hash.Values.CopyTo((Array)objArr, 0);
                         }
            );

            //[]test when array is size 0
            // now try getting out the values using CopyTo method into a 0 sized array
            objArr = new object[0];
            Assert.Throws<ArgumentException>(() =>
                         {
                             hash.Values.CopyTo((Array)objArr, 0);
                         }
            );

            //[]test when array is null
            Assert.Throws<ArgumentNullException>(() =>
                         {
                             hash.Values.CopyTo(null, 0);
                         }
            );
        }

        [Fact]
        public void TestCopyToWithValidIndex()
        {
            //[]test when hashtable has no elements in it
            var hash = new Hashtable();
            // make an array of 100 size to hold elements
            var objArr = new object[100];
            hash.Values.CopyTo(objArr, 0);

            objArr = new object[100];
            hash.Values.CopyTo(objArr, 99);

            objArr = new object[100];
            // valid now
            hash.Values.CopyTo(objArr, 100);

            // make an array of 0 size to hold elements
            objArr = new object[0];
            hash.Values.CopyTo(objArr, 0);

            int key = 123;
            int val = 456;
            hash.Add(key, val);

            objArr = new object[100];
            objArr[0] = 0;
            hash.Values.CopyTo(objArr, 0);
            Assert.Equal(val, objArr[0]);

            hash.Values.CopyTo(objArr, 99);
            Assert.Equal(val, objArr[99]);
        }

        [Fact]
        public void TestCopyToWithInvalidIndex()
        {
            object[] objArr = null;
            object[][] objArrMDim = null;       // multi dimensional object array
            object[,] objArrRMDim = null;       // multi dimensional object array

            //[]test when hashtable has no elements in it and index is out of range (negative)
            var hash = new Hashtable();

            // put no elements in hashtable
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                         {
                             // make an array of 100 size to hold elements
                             objArr = new object[0];
                             hash.Values.CopyTo(objArr, -1);
                         }
            );

            //[]test when array is multi dimensional and array is large enough
            hash = new Hashtable();
            // put elements into hashtable
            for (int i = 0; i < 100; i++)
            {
                hash.Add(i.ToString(), i.ToString());
            }

            Assert.Throws<InvalidCastException>(() =>
                         {
                             // make an array of 100 size to hold elements
                             objArrMDim = new object[100][];
                             for (int i = 0; i < 100; i++)
                             {
                                 objArrMDim[i] = new object[i + 1];
                             }

                             hash.Values.CopyTo(objArrMDim, 0);
                         }
            );

            Assert.Throws<ArgumentException>(() =>
                         {
                             // make an array of 100 size to hold elements
                             objArrRMDim = new object[100, 100];
                             hash.Values.CopyTo(objArrRMDim, 0);
                         }
            );

            //[]test when array is multi dimensional and array is small
            hash = new Hashtable();

            // put elements into hashtable
            for (int i = 0; i < 100; i++)
            {
                hash.Add(i.ToString(), i.ToString());
            }

            Assert.Throws<ArgumentException>(() =>
                {
                    // make an array of 100 size to hold elements
                    objArrMDim = new object[99][];
                    for (int i = 0; i < 99; i++)
                    {
                        objArrMDim[i] = new object[i + 1];
                    }

                    hash.Values.CopyTo(objArrMDim, 0);
                }
            );

            //[]test to see if CopyTo throws correct exception
            hash = new Hashtable();

            Assert.Throws<ArgumentException>(() =>
                {
                    string[] str = new string[100];
                    // i will be calling CopyTo with the str array and index 101 it should throw an exception
                    // since the array index 101 is not valid
                    hash.Values.CopyTo(str, 101);
                }
            );
        }

        /////////////////////////// HELPER FUNCTIONS
        // this is pretty slow algorithm but it works
        // returns true if arr1 has the same elements as arr2
        // arrays can have nulls in them, but arr1 and arr2 should not be null
        public static bool CompareArrays(object[] arr1, object[] arr2)
        {
            if (arr1.Length != arr2.Length)
            {
                return false;
            }

            int i, j;
            bool fPresent = false;

            for (i = 0; i < arr1.Length; i++)
            {
                fPresent = false;
                for (j = 0; j < arr2.Length && (fPresent == false); j++)
                {
                    if ((arr1[i] == null && arr2[j] == null)
                    ||
                    (arr1[i] != null && arr1[i].Equals(arr2[j])))
                    {
                        fPresent = true;
                    }
                }
                if (fPresent == false)
                {
                    return false;
                }
            }

            // now do the same thing but the other way around
            for (i = 0; i < arr2.Length; i++)
            {
                fPresent = false;
                for (j = 0; j < arr1.Length && (fPresent == false); j++)
                {
                    if ((arr2[i] == null && arr1[j] == null) || (arr2[i] != null && arr2[i].Equals(arr1[j])))
                    {
                        fPresent = true;
                    }
                }
                if (fPresent == false)
                {
                    return false;
                }
            }

            return true;
        }
    }
}