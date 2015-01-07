// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class SetItemObjObjListDictionaryTests
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

            // [] set Item() on empty dictionary
            //
            cnt = ld.Count;
            Assert.Throws<ArgumentNullException>(() => { ld[null] = "item"; });

            cnt = ld.Count;
            ld["some_string"] = "item";
            if (ld.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add item"));
            }
            if (String.Compare(ld["some_string"].ToString(), "item") != 0)
            {
                Assert.False(true, string.Format("Error, failed to set item"));
            }

            cnt = ld.Count;
            Hashtable lbl = new Hashtable();
            ArrayList b = new ArrayList();
            ld[lbl] = b;
            if (ld.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add item"));
            }
            if (!ld[lbl].Equals(b))
            {
                Assert.False(true, string.Format("Error, failed to set object-item"));
            }

            // [] set Item() on dictionary filled with simple strings
            //

            cnt = ld.Count;
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != cnt + len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, cnt + len));
            }
            //

            for (int i = 0; i < len; i++)
            {
                if (!ld.Contains(keys[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain key", i));
                }
                ld[keys[i]] = "newValue" + i;
                if (String.Compare(ld[keys[i]].ToString(), "newValue" + i) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set value", i));
                }
                ld[keys[i]] = b;
                if (!ld[keys[i]].Equals(b))
                {
                    Assert.False(true, string.Format("Error, failed to set object-value", i));
                }
            }


            //
            // Intl strings
            // [] set Item() on dictionary filled with Intl strings
            //

            string[] intlValues = new string[len * 2 + 1];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2 + 1; i++)
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
                ld[intlValues[i + len]] = intlValues[len * 2];
                if (String.Compare(ld[intlValues[i + len]].ToString(), intlValues[len * 2]) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set value", i));
                }
                ld[intlValues[i + len]] = b;
                if (!ld[intlValues[i + len]].Equals(b))
                {
                    Assert.False(true, string.Format("Error, failed to set object-value", i));
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

                ld[intlValues[i + len]] = b;
                if (!ld[intlValues[i + len]].Equals(b))
                {
                    Assert.False(true, string.Format("Error, failed to set via uppercase key", i));
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

            //  LD is case-sensitive by default  - new entries should be added
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                cnt = ld.Count;
                ld[intlValuesLower[i + len]] = "item";
                if (ld[intlValuesLower[i + len]] == null)
                {
                    Assert.False(true, string.Format("Error, failed: returned non-null for lowercase key", i));
                }
                if (!caseInsensitive && String.Compare(ld[intlValues[i + len]].ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, failed: changed value via lowercase key", i));
                }
                // lowercase itemshould be added to the dictionary
                if (String.Compare(ld[intlValuesLower[i + len]].ToString(), "item") != 0)
                {
                    Assert.False(true, string.Format("Error, failed: didn't add when set via lowercase key", i));
                }
            }

            //
            // [] set Item() on filled dictionary with case-insensitive comparer

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
                    ld[kk.ToUpper() + i] = "Item" + i;
                    if (String.Compare(ld[kk.ToUpper() + i].ToString(), "Item" + i) != 0)
                    {
                        Assert.False(true, string.Format("Error, failed to set value", i));
                    }
                    ld[kk.ToUpper() + i] = b;
                    if (!ld[kk.ToUpper() + i].Equals(b))
                    {
                        Assert.False(true, string.Format("Error, failed to set object-value", i));
                    }
                }
            }


            //
            //   [] set Item(null) on filled LD
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

            Assert.Throws<ArgumentNullException>(() => { ld[null] = "item"; });

            //  [] set Item(special_object)
            //
            ld = new ListDictionary();

            ld.Clear();
            b = new ArrayList();
            ArrayList b1 = new ArrayList();
            lbl = new Hashtable();
            Hashtable lbl1 = new Hashtable();

            ld.Add(lbl, b);
            ld.Add(lbl1, b1);
            if (ld.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, 2));
            }

            ld[lbl] = b1;
            if (!ld[lbl].Equals(b1))
            {
                Assert.False(true, string.Format("Error, failed to set special object"));
            }
            ld[lbl1] = b;
            if (!ld[lbl1].Equals(b))
            {
                Assert.False(true, string.Format("Error, failed to set special object"));
            }

            //
            //  [] set to null
            //
            ld = new ListDictionary();
            cnt = ld.Count;
            ld["null_key"] = null;
            if (ld.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add entry"));
            }
            if (ld["null_key"] != null)
            {
                Assert.False(true, string.Format("Error, failed to add entry with null value"));
            }

            cnt = ld.Count;
            ld.Add("key", "value");
            if (ld.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add entry"));
            }

            cnt = ld.Count;
            ld["key"] = null;
            if (ld["key"] != null)
            {
                Assert.False(true, string.Format("Error, failed to set entry to null "));
            }
        }
    }
}
