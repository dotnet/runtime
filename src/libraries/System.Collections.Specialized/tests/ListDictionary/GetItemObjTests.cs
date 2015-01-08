// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetItemObjListDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            ListDictionary ld;
            Object itm;

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

            //  initialize IntStrings
            intl = new IntlStrings();


            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            //  [] on empty dictionary
            //
            cnt = ld.Count;
            Assert.Throws<ArgumentNullException>(() => { itm = ld[null]; });

            cnt = ld.Count;
            itm = ld["some_string"];
            if (itm != null)
            {
                Assert.False(true, string.Format("Error, returned non=null"));
            }

            cnt = ld.Count;
            itm = ld[new Hashtable()];
            if (itm != null)
            {
                Assert.False(true, string.Format("Error, returned non-null"));
            }

            //  [] get Item() on dictionary filled with simple strings
            //

            cnt = ld.Count;
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, values.Length));
            }
            //

            for (int i = 0; i < len; i++)
            {
                if (!ld.Contains(keys[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain key", i));
                }
                if (String.Compare(ld[keys[i]].ToString(), values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong value", i));
                }
            }


            //
            // Intl strings
            //  [] get Item() on dictionary filled with Intl strings
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

            Boolean caseInsensitive = false;
            for (int i = 0; i < len * 2; i++)
            {
                if (intlValues[i].Length != 0 && intlValues[i].ToLower() == intlValues[i].ToUpper())
                    caseInsensitive = true;
            }

            cnt = ld.Count;
            for (int i = 0; i < len; i++)
            {
                ld.Add(intlValues[i + len], intlValues[i]);
            }
            if (ld.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, cnt + len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                if (!ld.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain key", i));
                }
                if (String.Compare(ld[intlValues[i + len]].ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong value", i));
                }
            }


            //
            // [] Case sensitivity
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
                ld.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                if (!ld.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain key", i));
                }

                if (String.Compare(ld[intlValues[i + len]].ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong value", i));
                }
            }

            ld.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                ld.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
            }

            //  LD is case-sensitive by default
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                cnt = ld.Count;
                if (!caseInsensitive && ld[intlValuesLower[i + len]] != null)
                {
                    Assert.False(true, string.Format("Error, failed: returned non-null for lowercase key", i));
                }
            }

            //
            //  [] get Item() on filled dictionary with case-insensitive comparer

            ld = new ListDictionary(new InsensitiveComparer());

            len = values.Length;
            ld.Clear();
            string kk = "key";
            for (int i = 0; i < len; i++)
            {
                ld.Add(kk + i, values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                if (ld[kk.ToUpper() + i] == null)
                {
                    Assert.False(true, string.Format("Error, returned null for differently cased key", i));
                }
                else
                {
                    if (String.Compare(ld[kk.ToUpper() + i].ToString(), values[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, returned wrong value", i));
                    }
                }
            }


            //
            //   [] Item(null) for dilled dictionary
            //
            ld = new ListDictionary();
            cnt = ld.Count;
            if (ld.Count < len)
            {
                ld.Clear();
                for (int i = 0; i < len; i++)
                {
                    ld.Add(keys[i], values[i]);
                }
            }

            Assert.Throws<ArgumentNullException>(() => { itm = ld[null]; });

            //  [] get Item(special_object)
            //
            ld = new ListDictionary();

            ld.Clear();
            ArrayList b = new ArrayList();
            ArrayList b1 = new ArrayList();
            Hashtable lbl = new Hashtable();
            Hashtable lbl1 = new Hashtable();

            ld.Add(lbl, b);
            ld.Add(lbl1, b1);
            if (ld.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, 2));
            }

            if (!ld[lbl].Equals(b))
            {
                Assert.False(true, string.Format("Error, failed to access special object"));
            }
            if (!ld[lbl1].Equals(b1))
            {
                Assert.False(true, string.Format("Error, failed to access special object"));
            }
        }
    }
}
