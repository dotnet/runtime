// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetValuesListDictionaryTests
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
            ICollection vs;         // Values collection
            int ind;

            // initialize IntStrings
            intl = new IntlStrings();


            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            // [] get Values for empty dictionary
            //
            if (ld.Count > 0)
                ld.Clear();

            if (ld.Values.Count != 0)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", ld.Values.Count));
            }

            // [] get Values for filled dictionary
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

            vs = ld.Values;
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len);
            vs.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, values[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Values doesn't contain \"{1}\" value. Search result: {2}", i, values[i], ind));
                }
            }



            //
            //  [] get Values on dictionary with identical values
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

            // get Values
            //

            vs = ld.Values;
            if (vs.Count != ld.Count)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len + 2);
            vs.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, values[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Values doesn't contain \"{1}\" value", i, values[i]));
                }
            }
            ind = Array.IndexOf(arr, intlStr);
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Values doesn't contain {0} value", intlStr));
            }

            //
            // Intl strings
            // [] get Values for dictionary filled with Intl strings
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

            vs = ld.Values;
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len);
            vs.CopyTo(arr, 0);
            for (int i = 0; i < arr.Length; i++)
            {
                ind = Array.IndexOf(arr, intlValues[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Values doesn't contain \"{1}\" value", i, intlValues[i]));
                }
            }

            //
            //  [] Change dictionary and check Values
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

            vs = ld.Values;
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }

            ld.Remove(keys[0]);
            if (ld.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove element"));
            }
            if (vs.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, Values were not updated after removal"));
            }
            arr = Array.CreateInstance(typeof(Object), ld.Count);
            vs.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, values[0]);
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Values still contains removed value " + ind));
            }

            ld.Add(keys[0], "new item");
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, didn't add element"));
            }
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, Values were not updated after addition"));
            }
            arr = Array.CreateInstance(typeof(Object), ld.Count);
            vs.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, "new item");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Values doesn't contain added value "));
            }

            // [] Run IEnumerable tests
            ListDictionary Ld = new ListDictionary();
            String[] expectedValues = new String[10];
            for (int i = 0; i < 10; i++)
            {
                Ld.Add("key_" + i, "val_" + i);
                expectedValues[i] = "val_" + i;
            }
            TestSupport.Collections.IEnumerable_Test iEnumerableTest;
            iEnumerableTest = new TestSupport.Collections.IEnumerable_Test(Ld.Values, expectedValues);
            if (!iEnumerableTest.RunAllTests())
            {
                Assert.False(true, string.Format("Err_98382apeuie System.Collections.IEnumerable tests FAILED"));
            }
        }
    }
}