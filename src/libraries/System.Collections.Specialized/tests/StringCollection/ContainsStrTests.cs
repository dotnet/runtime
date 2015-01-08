// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class ContainsStrTests
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

            // [] on empty collection
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (sc.Contains(values[i]))
                {
                    Assert.False(true, string.Format("Error, returned true for empty collection", i));
                }
            }


            // [] add simple strings and verify Contains()
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
                    Assert.False(true, string.Format("Error, collection doesn't contain item \"{1}\"", i, values[i]));
                }
            }

            //
            // Intl strings
            // [] add Intl strings and verify Contains()
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
                if (!sc.Contains(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain item \"{1}\"", i, intlValues[i]));
                }
            }

            //
            // add very long string
            // [] add very long string and verify Contains()
            //
            cnt = sc.Count;
            string intlStr = intlValues[0];
            while (intlStr.Length < 10000)
                intlStr += intlStr;

            sc.Add(intlStr);
            if (sc.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sc.Count, cnt + 1));
            }

            // verify that collection contains newly added item
            //
            if (!sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, collection doesn't contain new item"));
            }

            //
            //  [] Case sensitivity: search should be case-sensitive
            //

            sc.Clear();
            if (sc.Count != 0)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2} after Clear()", sc.Count, 0));
            }

            // add uppercase item
            //
            intlStr = intlValues[0].ToUpper();
            sc.Add(intlStr);
            if (sc.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sc.Count, 1));
            }

            // verify that Contains returns true for newly added uppercase item
            //
            if (!sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, Contains() returned false "));
            }
            // verify that Contains returns false for lowercase version
            //
            intlStr = intlValues[0].ToLower();
            if (!caseInsensitive && sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, Contains() returned true for lowercase version"));
            }
        }
    }
}
