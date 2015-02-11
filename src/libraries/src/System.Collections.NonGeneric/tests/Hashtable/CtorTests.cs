// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class CtorTests
    {
        [Fact]
        public void TestCtorDefault()
        {
            Hashtable hash = null;
            int nAttempts = 100;
            int[] iIntArray = new int[nAttempts];

            //
            // [] Constructor: Create Hashtable using default settings.
            //
            Console.Out.WriteLine("Create Hashtable using default settings");
            hash = new Hashtable();
            Assert.NotNull(hash);

            // Verify that the hash tabe is empty.
            Assert.Equal(hash.Count, 0);
        }

        [Fact]
        public void TestCtorDictionarySingle()
        {
            // No exception
            var hash = new Hashtable(new Hashtable(), 1f);
            // No exception
            hash = new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable(new Hashtable()), 1f), 1f), 1f), 1f);

            // []test to see if elements really get copied from old dictionary to new hashtable
            Hashtable tempHash = new Hashtable();
            // this for assumes that MinValue is a negative!
            for (long i = long.MinValue; i < long.MinValue + 100; i++)
            {
                tempHash.Add(i, i);
            }

            hash = new Hashtable(tempHash, 1f);

            // make sure that new hashtable has the elements in it that old hashtable had
            for (long i = long.MinValue; i < long.MinValue + 100; i++)
            {
                Assert.True(hash.ContainsKey(i));
                Assert.True(hash.ContainsValue(i));
            }

            //[]make sure that there are no connections with the old and the new hashtable
            tempHash.Clear();
            for (long i = long.MinValue; i < long.MinValue + 100; i++)
            {
                Assert.True(hash.ContainsKey(i));
                Assert.True(hash.ContainsValue(i));
            }
        }

        [Fact]
        public void TestCtorDictionarySingleNegative()
        {
            // variables used for tests
            Hashtable hash = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                hash = new Hashtable(null, 1);
            }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(new Hashtable(), Int32.MinValue);
            }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(new Hashtable(), Single.NaN);
            }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(new Hashtable(), 100.1f);
            }
            );
        }

        [Fact]
        public void TestCtorDictionary()
        {
            Hashtable hash = null;
            Hashtable hash2 = null;
            Int32 i4a;

            //
            // [] Constructor: null
            //
            Assert.Throws<ArgumentNullException>(() =>
            {
                hash = new Hashtable((IDictionary)null);
            }
            );

            //
            // []Constructor: empty
            //
            hash2 = new Hashtable(); //empty dictionary
            // No exception
            hash = new Hashtable(hash2);

            //
            // []Constructor: dictionary with 100 entries...
            //
            hash2 = new Hashtable();
            for (int i = 0; i < 100; i++)
            {
                hash2.Add("key_" + i, "val_" + i);
            }
            // No exception
            hash = new Hashtable(hash2);

            //Confirming the values
            Hashtable hash3 = new Hashtable(200);
            for (int ii = 0; ii < 100; ii++)
            {
                i4a = ii;
                hash3.Add("key_" + ii, i4a);
            }

            hash = new Hashtable(hash3);
            Assert.Equal(100, hash.Count);

            for (int ii = 0; ii < 100; ii++)
            {
                Assert.Equal(ii, (int)hash3["key_" + ii]);
                Assert.True(hash3.ContainsKey("key_" + ii));
            }

            Assert.False(hash3.ContainsKey("key_100"));
        }

        [Fact]
        public void TestCtorIntSingle()
        {
            // variables used for tests
            Hashtable hash = null;

            // [] should get ArgumentException if trying to have large num of entries
            Assert.Throws<ArgumentException>(() =>
            {
                hash = new Hashtable(int.MaxValue, .1f);
            }
            );

            // []should not get any exceptions for valid values - we also check that the HT works here
            hash = new Hashtable(100, .1f);

            int iNumberOfElements = 100;
            for (int i = 0; i < iNumberOfElements; i++)
            {
                hash.Add("Key_" + i, "Value_" + i);
            }

            //Count
            Assert.Equal(hash.Count, iNumberOfElements);

            DictionaryEntry[] strValueArr = new DictionaryEntry[hash.Count];
            hash.CopyTo(strValueArr, 0);

            Hashtable hsh3 = new Hashtable();
            for (int i = 0; i < iNumberOfElements; i++)
            {
                Assert.True(hash.Contains("Key_" + i), "Error, Expected value not returned, " + hash.Contains("Key_" + i));
                Assert.True(hash.ContainsKey("Key_" + i), "Error, Expected value not returned, " + hash.ContainsKey("Key_" + i));
                Assert.True(hash.ContainsValue("Value_" + i), "Error, Expected value not returned, " + hash.ContainsValue("Value_" + i));

                //we still need a way to make sure that there are all these unique values here -see below code for that
                Assert.True(hash.ContainsValue(((DictionaryEntry)strValueArr[i]).Value), "Error, Expected value not returned, " + ((DictionaryEntry)strValueArr[i]).Value);

                hsh3.Add(((DictionaryEntry)strValueArr[i]).Value, null);
            }
        }

        [Fact]
        public void TestCtorIntSingleNegative()
        {
            Hashtable hash = null;
            // []should get ArgumentOutOfRangeException if capacity range is not correct
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(5, .01f);
            }
            );

            // should get ArgumentOutOfRangeException if range is not correct
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(5, 100.1f);
            }
            );

            // should get OutOfMemoryException if Dictionary is null
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(int.MaxValue, 100.1f);
            }
            );

            // []ArgumentOutOfRangeException if capacity is less than zero.
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(int.MinValue, 10.1f);
            }
            );
        }

        [Fact]
        public void TestCtorIntCapacity()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            Hashtable hash = null;
            int nCapacity = 100;

            //
            // [] Constructor: Create Hashtable using a capacity value.
            //
            hash = new Hashtable(nCapacity);

            Assert.NotNull(hash);

            // Verify that the hash tabe is empty.

            Assert.Equal(0, hash.Count);

            //
            // [] Constructor: Create Hashtablewith zero capacity value - valid.
            //
            hash = new Hashtable(0);

            //
            // []Constructor: Create Hashtable using a invalid capacity value.
            //
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(-1);
            }
            );

            Assert.Throws<ArgumentException>(() => { hash = new Hashtable(Int32.MaxValue); });
        }

        [Fact]
        public void TestCtorIntFloat()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            Hashtable hash = null;
            int nCapacity = 100;
            float fltLoadFactor = (float).5;  // Note: default load factor is .73

            //
            // []Constructor: Create Hashtable using a capacity and load factor.
            //
            hash = new Hashtable(nCapacity, fltLoadFactor);
            Assert.NotNull(hash);

            // Verify that the hash tabe is empty.
            Assert.Equal(0, hash.Count);

            //
            // [] Constructor: Create Hashtable using a zero capacity and some load factor.
            //
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(-1, fltLoadFactor);
            });

            //
            // [] Constructor: Create Hashtable using a invalid capacity and valid load factor.
            //
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(nCapacity, .09f); // min lf allowed is .01
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(nCapacity, 1.1f);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                hash = new Hashtable(-1, -1f);
            });

            Assert.Throws<OutOfMemoryException>(() =>
            {
                hash = new Hashtable((int)100000000, .5f);
            });
        }
    }
}
