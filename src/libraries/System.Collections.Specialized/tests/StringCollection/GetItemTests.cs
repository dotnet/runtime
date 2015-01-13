// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetItemTests
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

            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringCollection Item is returned as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // exception expected
            // all indexes should be invalid for empty collection

            //
            // [] Invalid parameter - get Item from empty collection
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = sc[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = sc[0]; });

            // [] get Item() on collection filled with simple strings
            //

            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }
            for (int i = 0; i < values.Length; i++)
            {
                if (String.Compare(sc[i], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, sc[i], values[i]));
                }
            }


            //
            // Intl strings
            // [] get Item() on collection filled with Intl strings
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


            int cnt = sc.Count;
            sc.AddRange(intlValues);
            if (sc.Count != (cnt + intlValues.Length))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, cnt + intlValues.Length));
            }

            cnt = values.Length;
            for (int i = cnt; i < cnt + intlValues.Length; i++)
            {
                // verify that Item returns all intl string as expected
                //
                if (String.Compare(sc[i], intlValues[i - cnt]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, sc[i], intlValues[i - cnt]));
                }
            }

            //
            //  [] Case sensitivity
            //

            string intlStr = intlValues[0];
            intlStr = intlStr.ToUpper();

            sc.Clear();
            sc.Add(intlStr);

            if (sc.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of 1", sc.Count));
            }
            // verify that Item returns intl string in a case sensitive way
            //
            if (!caseInsensitive && (String.Compare(sc[0], intlValues[0].ToLower()) == 0))
            {
                Assert.False(true, string.Format("Error, returned unexpected result: {0} when should have return all upper", sc[0]));
            }
            if (String.Compare(sc[0], intlStr) != 0)
            {
                Assert.False(true, string.Format("Error, returned {0} instead of {1}", sc[0], intlStr));
            }

            //
            // [] Invalid parameter for filled collection
            //


            sc.Clear();
            sc.AddRange(intlValues);
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = sc[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = sc[sc.Count]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = sc[sc.Count + 1]; });
        }
    }
}
