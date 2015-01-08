// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetKeysListDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings


        [Fact]
        public void Test01()
        {
            IntlStrings intl;


            ListDictionary ld;

            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aa",
                "tExt",
                "     spAces",
                "1",
                "$%^#",
                "2222222222222222222222222",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // keys for simple string values
            string[] keys =
            {
                "zero",
                "one",
                " ",
                "",
                "aa",
                "1",
                System.DateTime.Today.ToString(),
                "$%^#",
                Int32.MaxValue.ToString(),
                "     spaces",
                "2222222222222222222222222"
            };

            Array arr;
            ICollection ks;         // Keys collection
            int ind;

            // initialize IntStrings
            intl = new IntlStrings();


            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            //  [] get Keys for empty dictionary
            //
            if (ld.Count > 0)
                ld.Clear();

            if (ld.Keys.Count != 0)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ld.Keys.Count));
            }

            //  [] get Keys for filled dictionary
            //
            int len = values.Length;
            ld.Clear();
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len));
            }

            ks = ld.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, keys[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key. Search result: {2}", i, keys[i], ind));
                }
            }



            //
            //  get Keys on dictionary with identical values
            // 
            //  [] get Keys for filled dictionary with identical value
            //

            ld.Clear();
            string intlStr = intl.GetRandomString(MAX_LEN);

            ld.Add("keykey", intlStr);        // 1st key
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            ld.Add("keyKey", intlStr);        // 2nd key
            if (ld.Count != len + 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len + 2));
            }

            // get Keys 
            //

            ks = ld.Keys;
            if (ks.Count != ld.Count)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len + 2);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, keys[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key", i, keys[i]));
                }
            }
            ind = Array.IndexOf(arr, "keykey");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain {0} key", "keykey"));
            }

            ind = Array.IndexOf(arr, "keyKey");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain \"{0}\" key", "keyKey"));
            }

            //
            // Intl strings
            //
            //  [] get Keys for dictionary filled with Intl strings
            //

            string[] intlValues = new string[len * 2];
            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                string val = intl.GetRandomString(MAX_LEN);
                while (Array.IndexOf(intlValues, val) != -1)
                    val = intl.GetRandomString(MAX_LEN);
                intlValues[i] = val;
            }

            //
            // will use first half of array as values and second half as keys
            //
            ld.Clear();
            for (int i = 0; i < len; i++)
            {
                ld.Add(intlValues[i + len], intlValues[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len));
            }

            ks = ld.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < arr.Length; i++)
            {
                ind = Array.IndexOf(arr, intlValues[i + len]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key", i, intlValues[i + len]));
                }
            }

            //
            //   [] Change dictionary and verify Keys
            // 

            ld.Clear();
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len));
            }

            ks = ld.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }

            ld.Remove(keys[0]);
            if (ld.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove element"));
            }
            if (ks.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after removal"));
            }
            arr = Array.CreateInstance(typeof(Object), ld.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keys[0]);
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Keys still contains removed key " + ind));
            }

            ld.Add(keys[0], "new item");
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, didn't add element"));
            }
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after addition"));
            }
            arr = Array.CreateInstance(typeof(Object), ld.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keys[0]);
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain added key "));
            }

            ICollection icol1;
            ListDictionary Hd = new ListDictionary();
            for (int i = 0; i < 10; i++)
                Hd.Add("key_" + i, "val_" + i);

            icol1 = Hd.Keys;

            if (Hd.SyncRoot != icol1.SyncRoot)
            {
                Assert.False(true, string.Format("Error, SyncRoot is not the same for ListDictionary's SyncRoot and ICollection.SyncRoot"));
            }

            // [] Run IEnumerable tests
            Hd = new ListDictionary();
            String[] expectedKeys = new String[10];
            for (int i = 0; i < 10; i++)
            {
                Hd.Add("key_" + i, "val_" + i);
                expectedKeys[i] = "key_" + i;
            }
            TestSupport.Collections.IEnumerable_Test iEnumerableTest;
            iEnumerableTest = new TestSupport.Collections.IEnumerable_Test(Hd.Keys, expectedKeys);
            if (!iEnumerableTest.RunAllTests())
            {
                Assert.False(true, string.Format("Err_98382apeuie System.Collections.IEnumerable tests FAILED"));
            }
        }
    }
}
