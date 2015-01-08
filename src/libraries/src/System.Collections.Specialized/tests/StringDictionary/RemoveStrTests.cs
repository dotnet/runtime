// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class RemoveStrStringDictionaryTests
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

            int cnt = 0;
            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] Remove() from empty dictionary
            //
            if (sd.Count > 0)
                sd.Clear();

            for (int i = 0; i < keys.Length; i++)
            {
                sd.Remove(keys[0]);
            }

            // [] Remove() from filled dictionary
            //
            int len = values.Length;
            sd.Clear();
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                cnt = sd.Count;
                sd.Remove(keys[i]);

                if (sd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, didn't remove element with {0} key", i));
                }

                // verify that indeed element with given key was removed
                if (sd.ContainsValue(values[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong value", i));
                }
                if (sd.ContainsKey(keys[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong value", i));
                }
            }


            //
            // [] Remove() on dictionary with identical values
            //

            sd.Clear();
            string intlStr = intl.GetRandomString(MAX_LEN);

            sd.Add("keykey1", intlStr);        // 1st duplicate
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            sd.Add("keykey2", intlStr);        // 2nd duplicate
            if (sd.Count != len + 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len + 2));
            }

            // remove
            //
            sd.Remove("keykey2");
            if (!sd.ContainsValue(intlStr))
            {
                Assert.False(true, string.Format("Error, removed both duplicates"));
            }
            // second string should still be present
            if (sd.ContainsKey("keykey2"))
            {
                Assert.False(true, string.Format("Error, removed not given instance"));
            }
            if (!sd.ContainsKey("keykey1"))
            {
                Assert.False(true, string.Format("Error, removed wrong instance"));
            }

            //
            // Intl strings
            // [] Remove() from dictionary filled with Intl strings
            //

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

            //
            // will use first half of array as values and second half as keys
            //
            sd.Clear();
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            // remove
            for (int i = 0; i < len; i++)
            {
                cnt = sd.Count;
                sd.Remove(intlValues[i + len]);

                if (sd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, didn't remove element with {0} key", i + len));
                }

                // verify that indeed element with given key was removed
                if (sd.ContainsValue(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong value", i));
                }
                if (sd.ContainsKey(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, removed wrong key", i));
                }
            }

            //
            // [] Case sensitivity: keys are always lowercased
            //

            sd.Clear();

            string[] intlValuesUpper = new string[len];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                intlValues[i] = intlValues[i].ToLowerInvariant();
            }

            // array of uppercase keys
            for (int i = 0; i < len; i++)
            {
                intlValuesUpper[i] = intlValues[i + len].ToUpperInvariant();
            }

            sd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);     // adding lowercase strings
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            // remove
            for (int i = 0; i < len; i++)
            {
                cnt = sd.Count;
                sd.Remove(intlValuesUpper[i]);

                if (sd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, didn't remove element with {0} lower key", i + len));
                }

                // verify that indeed element with given key was removed
                if (sd.ContainsValue(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong value", i));
                }
                if (sd.ContainsKey(intlValuesUpper[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong key", i));
                }
            }

            //
            // [] Invalid parameter
            //
            Assert.Throws<ArgumentNullException>(() => { sd.Remove(null); });
        }
    }
}
