// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class AddRangeStrATests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            StringCollection sc;

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

            int cnt = 0;            // Count
            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringCollection is constructed as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            //  [] AddRange() of simple strings
            //
            cnt = sc.Count;
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            for (int i = 0; i < values.Length; i++)
            {
                // verify that collection contains all added items
                //
                if (!sc.Contains(values[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain new item", i));
                }
            }

            //
            // Intl strings
            // [] AddRange() of Intl strings
            //
            string[] intlValues = new string[values.Length];

            // fill array with unique strings
            //
            for (int i = 0; i < values.Length; i++)
            {
                string val = intl.GetRandomString(MAX_LEN);
                while (Array.IndexOf(intlValues, val) != -1)
                    val = intl.GetRandomString(MAX_LEN);
                intlValues[i] = val;
            }

            //   AddRange
            //
            cnt = sc.Count;
            sc.AddRange(intlValues);
            if (sc.Count != (cnt + intlValues.Length))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, cnt + intlValues.Length));
            }

            for (int i = 0; i < intlValues.Length; i++)
            {
                // verify that collection contains all newly added items
                //
                if (!sc.Contains(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain new item", i));
                }
            }


            //  [] AddRange() - empty range
            //
            cnt = sc.Count;
            string[] empty = { };
            sc.AddRange(empty);
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, cnt));
            }

            //  [] AddRange() - null
            //
            cnt = sc.Count;
            Assert.Throws<ArgumentNullException>(() => { sc.AddRange((string[])null); });

            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, cnt));
            }
        }
    }
}
