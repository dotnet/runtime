// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class ClearStringDictionaryTests
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

            int cnt = 0;            // Count
            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();
            cnt = sd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after default ctor", sd.Count, 0));
            }

            // [] Clear() empty dictionary
            // 
            sd.Clear();
            cnt = sd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", sd.Count, 0));
            }


            // [] Add simple strings and Clear()
            //
            cnt = sd.Count;
            for (int i = 0; i < values.Length; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, values.Length));
            }

            sd.Clear();
            cnt = sd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", sd.Count, 0));
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
            cnt = sd.Count;
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);
            }
            if (sd.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, cnt + len));
            }

            sd.Clear();
            cnt = sd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", sd.Count, 0));
            }
        }
    }
}
