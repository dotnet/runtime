// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class SetItemObjObjTests
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
            Object itm;         // Item

            // initialize IntStrings
            intl = new IntlStrings();

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary is constructed as expected
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            // [] set Item() on empty dictionary
            //
            cnt = hd.Count;
            try
            {
                hd[null] = valuesShort[0];
                Assert.False(true, string.Format("Error, no exception"));
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            cnt = hd.Count;
            hd["some_string"] = valuesShort[0];
            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add Item(some_string)"));
            }
            itm = hd["some_string"];
            if (String.Compare(itm.ToString(), valuesShort[0]) != 0)
            {
                Assert.False(true, string.Format("Error, added wrong Item(some_string)"));
            }

            cnt = hd.Count;
            Hashtable l = new Hashtable();
            ArrayList bb = new ArrayList();
            hd[l] = bb;
            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add Item(some_object)"));
            }
            itm = hd[l];
            if (!itm.Equals(bb))
            {
                Assert.False(true, string.Format("Error, returned wrong Item(some_object)"));
            }

            // [] set Item() on short dictionary with simple strings
            //

            hd.Clear();
            cnt = hd.Count;
            int len = valuesShort.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, valuesShort.Length));
            }
            //

            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;
                hd[keysShort[i]] = valuesLong[0];
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, added item instead of setting", i));
                }
                itm = hd[keysShort[i]];
                if (String.Compare(itm.ToString(), valuesLong[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set item", i));
                }
            }
            // should add non-existing item
            cnt = hd.Count;
            hd[keysLong[0]] = valuesLong[0];
            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, didn't add non-existing item"));
            }
            itm = hd[keysLong[0]];
            if (String.Compare(itm.ToString(), valuesLong[0]) != 0)
            {
                Assert.False(true, string.Format("Error, failed to set item"));
            }

            // [] set Item() on long dictionary with simple strings
            //
            hd.Clear();
            cnt = hd.Count;
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }
            //

            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;
                hd[keysLong[i]] = valuesShort[0];
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, added item instead of setting", i));
                }
                itm = hd[keysLong[i]];
                if (String.Compare(itm.ToString(), valuesShort[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set item", i));
                }
            }
            // should add non-existing item
            cnt = hd.Count;
            hd[keysShort[0]] = valuesShort[0];
            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, didn't add non-existing item"));
            }
            itm = hd[keysShort[0]];
            if (String.Compare(itm.ToString(), valuesShort[0]) != 0)
            {
                Assert.False(true, string.Format("Error, failed to set item"));
            }


            //
            // [] set Item() on long dictionary with Intl strings
            // Intl strings
            //

            len = valuesLong.Length;
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
            string toSet = intlValues[len * 2];      // string to set            

            cnt = hd.Count;
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt + len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                cnt = hd.Count;
                hd[intlValues[i + len]] = toSet;
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, added item instead of setting", i));
                }
                itm = hd[intlValues[i + len]];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set item", i));
                }
            }

            // [] set Item() on short dictionary with Intl strings
            //

            len = valuesShort.Length;

            hd.Clear();
            cnt = hd.Count;
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt + len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                cnt = hd.Count;
                hd[intlValues[i + len]] = toSet;
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, added item instead of setting", i));
                }
                itm = hd[intlValues[i + len]];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
            }


            //
            // Case sensitivity
            // [] Case sensitivity - hashtable
            //

            len = valuesLong.Length;

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                hd[keysLong[i].ToUpper()] = toSet;
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set", i));
                }
            }

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //  LD is case-sensitive by default   - should add lowercase key
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                cnt = hd.Count;
                hd[keysLong[i].ToLower()] = toSet;
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, failed to add when setting", i));
                }
                itm = hd[keysLong[i].ToLower()];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set item", i));
                }
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToUpper()) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to preserve item", i));
                }
            }

            //
            // [] Case sensitivity - list


            len = valuesShort.Length;

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                hd[keysLong[i].ToUpper()] = toSet;
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set", i));
                }
            }

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //  LD is case-sensitive by default   - should add lowercase key
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                cnt = hd.Count;
                hd[keysLong[i].ToLower()] = toSet;
                if (hd.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, failed to add when setting", i));
                }
                itm = hd[keysLong[i].ToLower()];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set item", i));
                }
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToUpper()) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to preserve item", i));
                }
            }

            // [] set Item() on case-insensitive HD - list
            //

            hd = new HybridDictionary(true);

            len = valuesShort.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToLower(), valuesLong[i].ToLower());
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                hd[keysLong[i].ToUpper()] = toSet;
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set via uppercase key", i));
                }
                hd[keysLong[i].ToLower()] = valuesLong[0];
                itm = hd[keysLong[i].ToLower()];
                if (String.Compare(itm.ToString(), valuesLong[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set via lowercase key", i));
                }
            }

            // [] set Item() on case-insensitive HD - hashtable
            //
            hd = new HybridDictionary(true);

            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToLower(), valuesLong[i].ToLower());
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                hd[keysLong[i].ToUpper()] = toSet;
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), toSet) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set via uppercase key", i));
                }
                hd[keysLong[i].ToLower()] = valuesLong[0];
                itm = hd[keysLong[i].ToLower()];
                if (String.Compare(itm.ToString(), valuesLong[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, failed to set via lowercase key", i));
                }
            }


            //
            //  [] set Item(null) on filled HD - list
            //
            hd = new HybridDictionary();
            len = valuesShort.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            try
            {
                hd[null] = toSet;
                Assert.False(true, string.Format("Error, no exception"));
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            //  [] set Item(null) on filled HD - hashtable
            //
            hd = new HybridDictionary();
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            try
            {
                hd[null] = toSet;
                Assert.False(true, string.Format("Error, no exception"));
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            //  [] set Item(special_object) on filled HD - list
            //
            hd = new HybridDictionary();

            hd.Clear();
            len = 2;
            ArrayList[] b = new ArrayList[len];
            Hashtable[] lbl = new Hashtable[len];
            SortedList cb = new SortedList();
            for (int i = 0; i < len; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;
                hd[lbl[i]] = cb;
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, added special object instead of setting"));
                }
                itm = hd[lbl[i]];
                if (!itm.Equals(cb))
                {
                    Assert.False(true, string.Format("Error, failed to set special object"));
                }
            }

            cnt = hd.Count;
            hd[cb] = lbl[0];
            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add non-existing special object"));
            }
            itm = hd[cb];
            if (!itm.Equals(lbl[0]))
            {
                Assert.False(true, string.Format("Error, failed to set special object"));
            }

            //  [] set Item(special_object) on filled HD - hashtable
            //
            hd = new HybridDictionary();

            hd.Clear();
            len = 40;
            b = new ArrayList[len];
            lbl = new Hashtable[len];
            for (int i = 0; i < len; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;
                hd[lbl[i]] = cb;
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, added special object instead of setting"));
                }
                itm = hd[lbl[i]];
                if (!itm.Equals(cb))
                {
                    Assert.False(true, string.Format("Error, failed to set special object"));
                }
            }
            cnt = hd.Count;
            hd[cb] = lbl[0];
            if (hd.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, failed to add non-existing special object"));
            }
            itm = hd[cb];
            if (!itm.Equals(lbl[0]))
            {
                Assert.False(true, string.Format("Error, failed to set special object"));
            }

            //  [] set Item() to null on filled HD - list
            // 
            hd = new HybridDictionary();

            hd.Clear();
            len = valuesShort.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            cnt = hd.Count;
            hd[keysShort[0]] = null;
            if (hd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, added entry instead of setting"));
            }
            itm = hd[keysShort[0]];
            if (itm != null)
            {
                Assert.False(true, string.Format("Error, failed to set to null"));
            }

            //  [] set Item() to null on filled HD - hashtable
            // 
            hd.Clear();

            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            cnt = hd.Count;
            hd[keysLong[0]] = null;
            if (hd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, added entry instead of setting"));
            }
            itm = hd[keysLong[0]];
            if (itm != null)
            {
                Assert.False(true, string.Format("Error, failed to set to null"));
            }
        }
    }
}
