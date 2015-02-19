// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class ContainsKeyStrTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            StringDictionary sd;
            string ind;
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

            //  [] check for empty dictionary
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (sd.ContainsKey(keys[i]))
                {
                    Assert.False(true, string.Format("Error, returned true for empty dictionary", i));
                }
            }


            // [] add simple strings and verify ContainsKey()
            //

            cnt = values.Length;
            for (int i = 0; i < cnt; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, cnt));
            }

            for (int i = 0; i < cnt; i++)
            {
                // verify that collection contains all added items
                //
                if (!sd.ContainsValue(values[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain value \"{1}\"", i, values[i]));
                }
                if (!sd.ContainsKey(keys[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key \"{1}\"", i, keys[i]));
                }
            }

            //
            // Intl strings
            // [] add Intl strings and verify ContainsKey()
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

            Boolean caseInsensitive = false;
            for (int i = 0; i < len * 2; i++)
            {
                if (intlValues[i].Length != 0 && intlValues[i].ToLowerInvariant() == intlValues[i].ToUpperInvariant())
                    caseInsensitive = true;
            }


            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                cnt = sd.Count;

                sd.Add(intlValues[i + len], intlValues[i]);
                if (sd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, sd.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (!sd.ContainsValue(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain value of new item", i));
                }

                if (!sd.ContainsKey(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }

                //  access the item
                //
                ind = intlValues[i + len];
                if (String.Compare(sd[ind], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, sd[ind], intlValues[i]));
                }
            }

            //
            // add null string with non-null key
            // [] add null string with non-null key and verify ContainsKey()
            //
            cnt = sd.Count;
            string k = "keykey";

            sd.Add(k, null);
            if (sd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sd.Count, cnt + 1));
            }

            // verify that collection contains newly added item
            //
            if (!sd.ContainsKey(k))
            {
                Assert.False(true, string.Format("Error, dictionary doesn't contain new key"));
            }

            //
            // [] Case sensitivity: search should be case-sensitive
            //

            sd.Clear();
            if (sd.Count != 0)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2} after Clear()", sd.Count, 0));
            }

            string[] intlValuesLower = new string[len * 2];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                intlValues[i] = intlValues[i].ToUpperInvariant();
            }

            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValues[i].ToLowerInvariant();
            }

            sd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                cnt = sd.Count;

                sd.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
                if (sd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, sd.Count, cnt + 1));
                }

                // verify that collection contains newly added uppercase item
                //
                if (!sd.ContainsValue(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain value of new item", i));
                }

                if (!sd.ContainsKey(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }

                // verify that collection doesn't contains lowercase item
                //
                if (!caseInsensitive && sd.ContainsValue(intlValuesLower[i]))
                {
                    Assert.False(true, string.Format("Error, collection contains lowercase value of new item", i));
                }

                // key is case insensitive
                if (!sd.ContainsKey(intlValuesLower[i + len]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain lowercase key of new item", i));
                }
            }

            //
            // call ContainsKey with null - ArgumentNullException expected
            // [] ContainsKey (null)
            //
            Assert.Throws<ArgumentNullException>(() => { sd.ContainsKey(null); });
        }
    }
}
