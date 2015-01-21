// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class AddObjObjTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            HybridDictionary hd;
            const int BIG_LENGTH = 100;

            // simple string values
            string[] valuesShort =
            {
                "",
                " ",
                "$%^#",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // keys for simple string values
            string[] keysShort =
            {
                Int32.MaxValue.ToString(),
                " ",
                System.DateTime.Today.ToString(),
                "",
                "$%^#"
            };

            string[] valuesLong = new string[BIG_LENGTH];
            string[] keysLong = new string[BIG_LENGTH];

            int cnt = 0;            // Count

            // initialize IntStrings
            intl = new IntlStrings();

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary is constructed as expected
            //-----------------------------------------------------------------

            // [] simple strings

            hd = new HybridDictionary();


            for (int i = 0; i < valuesShort.Length; i++)
            {
                cnt = hd.Count;
                hd.Add(keysShort[i], valuesShort[i]);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(hd[keysShort[i]].ToString(), valuesShort[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[keysShort[i]], valuesShort[i]));
                }
            }

            // increase the number of items
            for (int i = 0; i < valuesLong.Length; i++)
            {
                cnt = hd.Count;
                hd.Add(keysLong[i], valuesLong[i]);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[keysLong[i]], valuesLong[i]));
                }
            }

            //
            // [] Intl strings
            //
            int len = valuesShort.Length;
            hd.Clear();
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
            // will use first half of array as valuesShort and second half as keysShort
            //
            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;

                hd.Add(intlValues[i + len], intlValues[i]);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(hd[intlValues[i + len]].ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[intlValues[i + len]], intlValues[i]));
                }
            }

            // increase the number of items
            for (int i = 0; i < valuesLong.Length; i++)
            {
                cnt = hd.Count;
                hd.Add(keysLong[i], intlValues[1]);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(hd[keysLong[i]].ToString(), intlValues[1]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[keysLong[i]], intlValues[1]));
                }
            }

            //
            // [] Case sensitivity
            // Casing doesn't change ( keysShort are not converted to lower!)
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

            hd.Clear();
            //
            // will use first half of array as valuesShort and second half as keysShort
            //
            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;

                hd.Add(intlValues[i + len], intlValues[i]);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (hd[intlValues[i + len]] == null)
                {
                    Assert.False(true, string.Format("Error, returned null", i));
                }
                else
                {
                    if (!hd[intlValues[i + len]].Equals(intlValues[i]))
                    {
                        Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[intlValues[i + len]], intlValues[i]));
                    }
                }

                // verify that dictionary doesn't contains lowercase item
                //
                if (!caseInsensitive && hd[intlValuesLower[i + len]] != null)
                {
                    Assert.False(true, string.Format("Error, returned non-null", i));
                }
            }

            //
            //   [] Add multiple valuesShort with the same key
            //   Add multiple valuesShort with the same key - ArgumentException expected
            //

            hd.Clear();
            len = valuesShort.Length;
            string k = "keykey";
            hd.Add(k, "value");
            Assert.Throws<ArgumentException>(() => { hd.Add(k, "newvalue"); });

            //
            // [] Add null value
            //

            cnt = hd.Count;
            k = "kk";
            hd.Add(k, null);

            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt + 1));
            }

            // verify that dictionary contains null
            //
            if (hd[k] != null)
            {
                Assert.False(true, string.Format("Error, returned non-null on place of null"));
            }

            //
            // [] Add item with null key
            // Add item with null key - ArgumentNullException expected
            //

            cnt = hd.Count;
            Assert.Throws<ArgumentNullException>(() => { hd.Add(null, "item"); });

            //
            // [] Add duplicate values
            //
            hd.Clear();
            for (int i = 0; i < valuesShort.Length; i++)
            {
                cnt = hd.Count;
                hd.Add(keysShort[i], "value");
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (!hd[keysShort[i]].Equals("value"))
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[keysShort[i]], "value"));
                }
            }
            // verify Keys and Values

            if (hd.Keys.Count != valuesShort.Length)
            {
                Assert.False(true, string.Format("Error, Keys contains {0} instead of {1}", hd.Keys.Count, valuesShort.Length));
            }
            if (hd.Values.Count != valuesShort.Length)
            {
                Assert.False(true, string.Format("Error, Values contains {0} instead of {1}", hd.Values.Count, valuesShort.Length));
            }

            //
            //  [] add many simple strings
            //
            hd = new HybridDictionary();
            for (int i = 0; i < valuesLong.Length; i++)
            {
                cnt = hd.Count;
                hd.Add(keysLong[i], valuesLong[i]);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(hd[keysLong[i]].ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[keysLong[i]], valuesLong[i]));
                }
            }

            // increase the number of items
            for (int i = 0; i < valuesLong.Length; i++)
            {
                cnt = hd.Count;
                hd.Add(keysLong[i] + "_", valuesLong[i] + i);
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, hd.Count, cnt + 1));
                }

                //  access the item
                //
                if (String.Compare(hd[keysLong[i] + "_"].ToString(), valuesLong[i] + i) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, hd[keysLong[i] + "_"], valuesLong[i] + i));
                }
            }
        }
    }
}