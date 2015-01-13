// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetValuesStringDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            StringDictionary sd;
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


            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] get Values on empty dictionary
            //
            if (sd.Count > 0)
                sd.Clear();

            if (sd.Values.Count != 0)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", sd.Values.Count));
            }

            // [] get Values on filled dictionary
            //
            int len = values.Length;
            sd.Clear();
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            vs = sd.Values;
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }
            arr = Array.CreateInstance(typeof(string), len);
            vs.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, values[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Values doesn't contain \"{1}\" value. Search result: {2}", i, keys[i], ind));
                }
            }



            //
            // [] get Values on dictionary with identical values
            //

            sd.Clear();
            string intlStr = intl.GetRandomString(MAX_LEN);

            sd.Add("keykey1", intlStr);        // 1st duplicate
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            sd.Add("keykey2", intlStr);        // 2nd duplicate
            if (sd.Count != len + 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len + 2));
            }

            // get Values
            //

            vs = sd.Values;
            if (vs.Count != sd.Count)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }
            arr = Array.CreateInstance(typeof(string), len + 2);
            vs.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, values[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Values doesn't contain \"{1}\" value", i, keys[i]));
                }
            }
            ind = Array.IndexOf(arr, intlStr);
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Values doesn't contain {0} value", intlStr));
            }

            //
            // Intl strings
            // [] get Values on dictionary filled with Intl strings
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
            sd.Clear();
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            vs = sd.Values;
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }
            arr = Array.CreateInstance(typeof(string), len);
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
            //  Case sensitivity: values are case sensitive
            // and changing Dictionary
            // [] change dictionary and verify case sensitivity
            //

            sd.Clear();
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            vs = sd.Values;
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Values.Count = {0}", vs.Count));
            }

            sd.Remove(keys[0]);
            if (sd.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove element"));
            }
            if (vs.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, Values were not updated after removal"));
            }
            arr = Array.CreateInstance(typeof(string), sd.Count);
            vs.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, values[0]);
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Values still contains removed value " + ind));
            }

            sd.Add(keys[0], "new item");
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, didn't add element"));
            }
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, Values were not updated after addition"));
            }
            arr = Array.CreateInstance(typeof(string), sd.Count);
            vs.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, "new item");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Values doesn't contain added value "));
            }

            sd[keys[0]] = "UPPERCASE";
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, Count changed after set"));
            }
            if (vs.Count != len)
            {
                Assert.False(true, string.Format("Error, Values.Count changed after set"));
            }
            arr = Array.CreateInstance(typeof(string), sd.Count);
            vs.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, "UPPERCASE");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Values doesn't contain value we just set "));
            }

            ind = Array.IndexOf(arr, "uppercase");
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Values contains lowercase version of value we just set "));
            }
        }
    }
}
