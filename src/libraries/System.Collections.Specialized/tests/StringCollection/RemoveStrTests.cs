// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class RemoveStrStringCollectionTests
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

            // [] Remove() from empty collection
            //
            for (int i = 0; i < values.Length; i++)
            {
                sc.Remove(values[i]);
                if (sc.Count != 0)
                {
                    Assert.False(true, string.Format("Error, Remove changed Count for empty collection", i));
                }
            }


            // [] Remove() from collection filled with simple strings
            //

            sc.Clear();
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
                    Assert.False(true, string.Format("Error, doesn't contain {0} item", i));
                }

                cnt = sc.Count;

                // Remove each item
                //
                sc.Remove(values[i]);

                if (sc.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, didn't remove anything", i));
                }

                if (sc.Contains(values[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }

            //
            // Intl strings
            // [] Remove() from collection filled with Intl strings
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
            sc.AddRange(intlValues);
            if (sc.Count != intlValues.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }

            for (int i = 0; i < intlValues.Length; i++)
            {
                // verify that collection contains all added items
                //
                if (!sc.Contains(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain {0} item", i));
                }

                cnt = sc.Count;

                // Remove each item
                //
                sc.Remove(intlValues[i]);

                if (sc.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, didn't remove anything", i));
                }

                if (sc.Contains(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }


            //
            // duplicate strings
            // [] Remove() from filled collection with duplicate strings
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

            // remove
            //
            sc.Remove(intlStr);
            if (!sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, removed both duplicates"));
            }
            // second string should still be present
            if (sc.IndexOf(intlStr) != values.Length)
            {
                Assert.False(true, string.Format("Error, IndexOf returned {0} instead of {1}", sc.IndexOf(intlStr), values.Length));
            }

            // verify that items were moved
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (sc.IndexOf(values[i]) != i)
                {
                    Assert.False(true, string.Format("Error, IndexOf {0} item returned {1} ", i, sc.IndexOf(values[i])));
                }

                if (sc.IndexOf(intlValues[i]) != i + values.Length)
                {
                    Assert.False(true, string.Format("Error, IndexOf {1} item returned {2} ", i, i + values.Length, sc.IndexOf(intlValues[i])));
                }
            }


            //
            // [] Case sensitivity: search should be case-sensitive
            //

            sc.Clear();
            sc.Add(intlStr.ToUpper());
            sc.AddRange(values);
            sc.Add(intlStr.ToLower());
            cnt = values.Length + 2;
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2} ", sc.Count, cnt));
            }

            // remove lowercase item
            //
            intlStr = intlStr.ToLower();

            cnt = sc.Count;
            sc.Remove(intlStr);
            if (sc.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove anything"));
            }

            if (!caseInsensitive && sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, didn't remove lowercase "));
            }

            // but should still contain Uppercase
            if (!sc.Contains(intlValues[0].ToUpper()))
            {
                Assert.False(true, string.Format("Error, removed uppercase "));
            }

            //
            //  remove item that is not in the collection
            //

            sc.Clear();
            sc.AddRange(values);
            cnt = values.Length;
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2} ", sc.Count, cnt));
            }

            // remove non-existing item
            //
            intlStr = "Hello";
            cnt = sc.Count;
            sc.Remove(intlStr);
            if (sc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, removed something"));
            }
        }
    }
}
