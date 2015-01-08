// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Text;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class IndexOfStrTests
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

            // [] for empty collection
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (sc.IndexOf(values[i]) != -1)
                {
                    Assert.False(true, string.Format("Error, returned {1} for empty collection", i, sc.IndexOf(values[i])));
                }
            }

            //
            // [] add simple strings and verify IndexOf()


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
                if (sc.IndexOf(values[i]) != i)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {0}", i, sc.IndexOf(values[i])));
                }
            }

            //
            // Intl strings
            // [] add Intl strings and verify IndexOf()
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

            int len = values.Length;
            Boolean caseInsensitive = false;
            for (int i = 0; i < len; i++)
            {
                if (intlValues[i].Length != 0 && intlValues[i].ToLower() == intlValues[i].ToUpper())
                    caseInsensitive = true;
            }

            cnt = sc.Count;
            sc.AddRange(intlValues);
            if (sc.Count != (cnt + intlValues.Length))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, cnt + intlValues.Length));
            }

            for (int i = 0; i < intlValues.Length; i++)
            {
                // verify that collection contains all added items
                //
                if (sc.IndexOf(intlValues[i]) != values.Length + i)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {2}", i, sc.IndexOf(intlValues[i]), values.Length + i));
                }
            }

            //
            // [] duplicate strings
            //
            sc.Clear();
            string intlStr = intlValues[0];

            sc.Add(intlStr);        // index 0
            sc.AddRange(values);
            sc.AddRange(intlValues);        // second index values.Length + 1
            cnt = values.Length + 1 + intlValues.Length;
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sc.Count, cnt));
            }

            // verify Index of newly added item
            //
            if (sc.IndexOf(intlStr) != 0)
            {
                Assert.False(true, string.Format("Error, IndexOf returned {0} instead of {1}", sc.IndexOf(intlStr), 0));
            }

            sc.Clear();

            sc.AddRange(values);
            sc.AddRange(intlValues);        // second index values.Length + 1
            sc.Add(intlStr);        // index values.Length + intlValues.Length
            cnt = values.Length + 1 + intlValues.Length;
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sc.Count, cnt));
            }

            // verify Index of newly added item
            //
            if (sc.IndexOf(intlStr) != values.Length)
            {
                Assert.False(true, string.Format("Error, IndexOf returned {0} instead of {1}", sc.IndexOf(intlStr), values.Length));
            }

            //
            // [] Case sensitivity: search should be case-sensitive
            //

            sc.Clear();
            sc.Add(intlValues[0].ToUpper());
            sc.AddRange(values);
            sc.Add(intlValues[0].ToLower());
            cnt = values.Length + 2;
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2} ", sc.Count, cnt));
            }

            // look for lowercase item - should be (values.Length  + 1) index
            //
            intlStr = intlValues[0].ToLower();

            if (!caseInsensitive && (sc.IndexOf(intlStr) != values.Length + 1))
            {
                Assert.False(true, string.Format("Error, IndexOf() returned {0} instead of {1} ", sc.IndexOf(intlStr), values.Length + 1));
            }

            // string half_upper+half_lower should not be found
            //
            if (intlStr.Length > 2)
            {
                int mid = intlStr.Length / 2;
                String strTemp = intlStr.Substring(0, mid).ToLower() + intlStr.Substring(mid, intlStr.Length - mid).ToUpper();
                if (strTemp != intlStr.ToUpper() &&
                    strTemp != intlStr.ToLower() &&
                    !caseInsensitive)
                {
                    intlStr = strTemp;

                    if (sc.IndexOf(intlStr) != -1)
                    {
                        Assert.False(true, string.Format("Error, IndexOf() returned {0} instead of -1 ", sc.IndexOf(intlStr)));
                    }
                }
            }
        }
    }
}


