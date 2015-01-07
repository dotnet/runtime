// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetKeysStringDictionaryTests
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
            ICollection ks;         // Keys collection
            int ind;

            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] get Keys on empty dictionary
            //
            if (sd.Count > 0)
                sd.Clear();

            if (sd.Keys.Count != 0)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", sd.Keys.Count));
            }

            //
            // [] get Keys on filled dictionary
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

            ks = sd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(string), len);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, keys[i].ToLowerInvariant());
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key. Search result: {2}", i, keys[i], ind));
                }
            }



            //
            // [] get Keys on dictionary with identical values
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

            // get Keys 
            //

            ks = sd.Keys;
            if (ks.Count != sd.Count)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(string), len + 2);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, keys[i].ToLowerInvariant());
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key", i, keys[i]));
                }
            }
            ind = Array.IndexOf(arr, "keykey1");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain {0} key", "keykey1"));
            }

            ind = Array.IndexOf(arr, "keykey2");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain \"{0}\" key", "keykey2"));
            }

            //
            // Intl strings
            // [] get Keys on dictionary filled with Intl strings
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

            ks = sd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(string), len);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < arr.Length; i++)
            {
                ind = Array.IndexOf(arr, intlValues[i + len].ToLowerInvariant());
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key", i, intlValues[i + len]));
                }
            }

            //
            //  Case sensitivity: keys are always lowercased - not doing it
            // [] Change dictionary and check Keys
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

            ks = sd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }

            sd.Remove(keys[0]);
            if (sd.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove element"));
            }
            if (ks.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after removal"));
            }
            arr = Array.CreateInstance(typeof(string), sd.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keys[0].ToLowerInvariant());
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Keys still contains removed key " + ind));
            }

            sd.Add(keys[0], "new item");
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, didn't add element"));
            }
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after addition"));
            }
            arr = Array.CreateInstance(typeof(string), sd.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keys[0].ToLowerInvariant());
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain added key "));
            }
        }
    }
}
