// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetItemStrStringDictionaryTests
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
                "text",
                "     spaces",
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

            string itm;         // returned Item
            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] get Item() on empty dictionary
            //
            if (sd.Count > 0)
                sd.Clear();

            for (int i = 0; i < keys.Length; i++)
            {
                itm = sd[keys[i]];
                if (itm != null)
                {
                    Assert.False(true, string.Format("Error, returned non-null for {0} key", i));
                }
            }

            // [] get Item() on filled dictionary
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

            for (int i = 0; i < len; i++)
            {
                if (String.Compare(sd[keys[i]], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, sd[keys[i]], values[i]));
                }
            }



            //
            //  [] get Item on dictionary with identical values
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

            // get Item
            //
            if (String.Compare(sd["keykey1"], intlStr) != 0)
            {
                Assert.False(true, string.Format("Error, returned {1} instead of {2}", sd["keykey1"], intlStr));
            }

            if (String.Compare(sd["keykey2"], intlStr) != 0)
            {
                Assert.False(true, string.Format("Error, returned {1} instead of {2}", sd["keykey2"], intlStr));
            }

            //
            // Intl strings
            // [] get Item() on dictionary filled with Intl strings
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

            // get item
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(sd[intlValues[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, sd[intlValues[i + len]], intlValues[i]));
                }
            }

            //
            // [] Case sensitivity: keys are always lowercased
            //

            sd.Clear();

            string[] intlValuesUpper = new string[len];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                intlValues[i] = intlValues[i].ToLowerInvariant();
            }

            // array of uppercase keys
            for (int i = 0; i < len; i++)
            {
                intlValuesUpper[i] = intlValues[i + len].ToUpperInvariant();
            }

            sd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);     // adding lowercase strings
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            // get Item
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(sd[intlValuesUpper[i]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, sd[intlValuesUpper[i]], intlValues[i]));
                }
            }

            //
            // [] Invalid parameter
            //
            Assert.Throws<ArgumentNullException>(() => { itm = sd[null]; });
        }
    }
}
