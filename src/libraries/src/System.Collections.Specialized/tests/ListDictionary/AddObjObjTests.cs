// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class AddObjObjListDictionaryTests
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


            // [] Add simple strings
            //
            for (int i = 0; i < values.Length; i++)
            {
                cnt = ld.Count;
                ld.Add(keys[i], values[i]);
                if (ld.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, ld.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(ld[keys[i]].ToString(), values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, ld[keys[i]], values[i]));
                }
            }

            //
            // [] Add Intl strings
            // Intl strings
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
                if (intlValues[i].Length != 0 && intlValues[i].ToLower() == intlValues[i].ToUpper())
                    caseInsensitive = true;
            }

            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                cnt = ld.Count;

                ld.Add(intlValues[i + len], intlValues[i]);
                if (ld.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, ld.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(ld[intlValues[i + len]].ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, ld[intlValues[i + len]], intlValues[i]));
                }
            }

            //
            // [] Case sensitivity
            // Casing doesn't change ( keys are not converted to lower!)
            //
            string[] intlValuesLower = new string[len * 2];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                intlValues[i] = intlValues[i].ToUpper();
            }

            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValues[i].ToLower();
            }

            ld.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                cnt = ld.Count;

                ld.Add(intlValues[i + len], intlValues[i]);
                if (ld.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, ld.Count, cnt + 1));
                }

                //  access the item
                //
                if (ld[intlValues[i + len]] == null)
                {
                    Assert.False(true, string.Format("Error, returned null", i));
                }
                else
                {
                    if (!ld[intlValues[i + len]].Equals(intlValues[i]))
                    {
                        Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, ld[intlValues[i + len]], intlValues[i]));
                    }
                }

                // verify that dictionary doesn't contains lowercase item
                //
                if (!caseInsensitive && ld[intlValuesLower[i + len]] != null)
                {
                    Assert.False(true, string.Format("Error, returned non-null", i));
                }
            }

            //
            //   [] Add multiple values with the same key
            //   Add multiple values with the same key - ArgumentException expected
            //

            ld.Clear();
            len = values.Length;
            string k = "keykey";
            ld.Add(k, "value");
            Assert.Throws<ArgumentException>(() => { ld.Add(k, "newvalue"); });

            //
            // [] Add(string, null)
            // Add null value
            //

            cnt = ld.Count;
            k = "kk";
            ld.Add(k, null);

            if (ld.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, cnt + 1));
            }

            // verify that dictionary contains null
            //
            if (ld[k] != null)
            {
                Assert.False(true, string.Format("Error, returned non-null on place of null"));
            }

            //
            // [] Add(null, string)
            // Add item with null key - ArgumentNullException expected
            //

            cnt = ld.Count;

            Assert.Throws<ArgumentNullException>(() => { ld.Add(null, "item"); });

            //
            // [] Add duplicate values
            //
            ld.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                cnt = ld.Count;
                ld.Add(keys[i], "value");
                if (ld.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, ld.Count, cnt + 1));
                }

                //  access the item
                //
                if (!ld[keys[i]].Equals("value"))
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, ld[keys[i]], "value"));
                }
            }
            // verify Keys and Values

            if (ld.Keys.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, Keys contains {0} instead of {1}", ld.Keys.Count, values.Length));
            }
            if (ld.Values.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, Values contains {0} instead of {1}", ld.Values.Count, values.Length));
            }
        }
    }
}
