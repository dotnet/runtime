// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class ClearListDictionaryTests
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
                "aA",
                "text",
                "     SPaces",
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
                "oNe",
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

            int cnt = 0;            // Count

            // initialize IntStrings
            intl = new IntlStrings();


            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();
            cnt = ld.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after default ctor", ld.Count, 0));
            }

            //  [] Clear() on empty dictionary
            // 
            ld.Clear();
            cnt = ld.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", ld.Count, 0));
            }

            cnt = ld.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = ld.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }


            //  [] Add simple strings and Clear()
            //
            cnt = ld.Count;
            for (int i = 0; i < values.Length; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, values.Length));
            }

            ld.Clear();
            cnt = ld.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", ld.Count, 0));
            }

            cnt = ld.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = ld.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }


            //
            // [] Add Intl strings and Clear()
            //
            int len = values.Length;
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

            //   Add items
            //
            cnt = ld.Count;
            for (int i = 0; i < len; i++)
            {
                ld.Add(intlValues[i + len], intlValues[i]);
            }
            if (ld.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, cnt + len));
            }

            ld.Clear();
            cnt = ld.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", ld.Count, 0));
            }

            cnt = ld.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = ld.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
        }
    }
}
