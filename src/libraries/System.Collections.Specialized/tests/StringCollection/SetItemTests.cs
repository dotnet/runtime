// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class SetItemTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            StringCollection sc;
            string itm;         // returned value of Item
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

            intl = new IntlStrings();


            // [] StringCollection Item is returned as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // exception expected
            // all indexes should be invalid for empty collection

            //
            // [] Invalid parameter - set Item() on empty collection
            //
            itm = intl.GetRandomString(MAX_LEN);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[-1] = itm; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[0] = itm; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[0] = null; });

            // [] set Item() on collection filled with simple strings
            //

            sc.Clear();
            sc.AddRange(values);
            int cnt = values.Length;
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, cnt));
            }
            for (int i = 0; i < cnt; i++)
            {
                sc[i] = values[cnt - i - 1];
                if (String.Compare(sc[i], values[cnt - i - 1]) != 0)
                {
                    Assert.False(true, string.Format("Error, value is {1} instead of {2}", i, sc[i], values[cnt - i - 1]));
                }
            }


            //
            // Intl strings
            // [] set Item() on collection filled with Intl strings
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



            sc.Clear();
            cnt = intlValues.Length;
            sc.AddRange(intlValues);
            if (sc.Count != intlValues.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }

            for (int i = cnt; i < cnt; i++)
            {
                sc[i] = intlValues[cnt - i - 1];
                if (String.Compare(sc[i], intlValues[cnt - i - 1]) != 0)
                {
                    Assert.False(true, string.Format("Error, actual item is {1} instead of {2}", i, sc[i], intlValues[cnt - i - 1]));
                }
            }

            //
            //  [] Case sensitivity
            //

            string intlStrUpper = intlValues[0];
            intlStrUpper = intlStrUpper.ToUpper();
            string intlStrLower = intlStrUpper.ToLower();

            sc.Clear();
            sc.AddRange(values);
            sc.Add(intlStrUpper);

            if (sc.Count != values.Length + 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length + 1));
            }
            // verify that Item returns intl string in a case sensitive way
            //
            // set simple to intl lower
            //
            sc[0] = intlStrLower;
            if (!caseInsensitive && (String.Compare(sc[0], intlStrUpper) == 0))
            {
                Assert.False(true, string.Format("Error, set to uppercase when should have to lower"));
            }
            if (String.Compare(sc[0], intlStrLower) != 0)
            {
                Assert.False(true, string.Format("Error, didn't set to lower"));
            }

            //
            // set intlUpper to intl lower
            //
            sc[sc.Count - 1] = intlStrLower;
            if (!caseInsensitive && (String.Compare(sc[sc.Count - 1], intlStrUpper) == 0))
            {
                Assert.False(true, string.Format("Error, didn't set from uppercase to lowercase "));
            }
            if (String.Compare(sc[sc.Count - 1], intlStrLower) != 0)
            {
                Assert.False(true, string.Format("Error, didn't set to lower"));
            }


            //
            //    set to null  - it's legal - return value will be null
            // [] set Item() to null
            //


            if (sc.Count < 1)
                sc.AddRange(values);

            //
            // set middle item to null
            //
            int ind = sc.Count / 2;
            sc[ind] = null;
            if (sc[ind] != null)
            {
                Assert.False(true, string.Format("Error, failed to set to null"));
            }
            if (!sc.Contains(null))
            {
                Assert.False(true, string.Format("Error, Contains() didn't return true for null"));
            }

            if (sc.IndexOf(null) != ind)
            {
                Assert.False(true, string.Format("Error, IndexOf() returned {0} instead of {1}", sc.IndexOf(null), ind));
            }

            //
            // [] Invalid parameter - for filled collection
            //


            sc.Clear();
            sc.AddRange(intlValues);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[-1] = intlStrUpper; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[sc.Count] = intlStrUpper; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[sc.Count + 1] = intlStrUpper; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc[sc.Count] = null; });
        }
    }
}
