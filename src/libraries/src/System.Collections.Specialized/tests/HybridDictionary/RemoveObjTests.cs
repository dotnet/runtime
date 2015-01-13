// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class RemoveObjTests
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

            hd = new HybridDictionary();

            // [] Remove() on empty dictionary
            //
            cnt = hd.Count;
            try
            {
                hd.Remove(null);
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
            hd.Remove("some_string");
            if (hd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, changed dictionary after Remove(some_string)"));
            }

            cnt = hd.Count;
            hd.Remove(new Hashtable());
            if (hd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, changed dictionary after Remove(some_string)"));
            }

            //
            // [] Remove() on short dictionary with simple strings
            //

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
                hd.Remove(keysShort[i]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(keysShort[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
                // remove second time
                hd.Remove(keysShort[i]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed when Remove() second time", i));
                }
            }

            // [] Remove() on long dictionary with simple strings
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
                hd.Remove(keysLong[i]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(keysLong[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
                // remove second time
                hd.Remove(keysLong[i]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed when Remove() second time", i));
                }
            }

            //
            // [] Remove() on long dictionary with Intl strings
            // Intl strings
            //

            len = valuesLong.Length;
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
                hd.Remove(intlValues[i + len]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }

            // [] Remove() on short dictionary with Intl strings
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
                hd.Remove(intlValues[i + len]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }


            //
            //  Case sensitivity
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

                cnt = hd.Count;
                hd.Remove(keysLong[i].ToUpper());
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(keysLong[i].ToUpper()))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
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

            //  LD is case-sensitive by default
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                cnt = hd.Count;
                hd.Remove(keysLong[i].ToLower());
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, failed: removed item using lowercase key", i));
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

                cnt = hd.Count;
                hd.Remove(keysLong[i].ToUpper());
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(keysLong[i].ToUpper()))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
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

            //  LD is case-sensitive by default
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                cnt = hd.Count;
                hd.Remove(keysLong[i].ToLower());
                if (hd.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, failed: removed item using lowercase key", i));
                }
            }

            //
            // [] Remove() on case-insensitive HD - list
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
                cnt = hd.Count;
                hd.Remove(keysLong[i].ToUpper());
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(keysLong[i].ToLower()))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }

            //
            // [] Remove() on case-insensitive HD - hashtable
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
                cnt = hd.Count;
                hd.Remove(keysLong[i].ToUpper());
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (hd.Contains(keysLong[i].ToLower()))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }


            //
            //  [] Remove(null)  from filled HD - list
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
                hd.Remove(null);
                Assert.False(true, string.Format("Error, no exception"));
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            //
            //  [] Remove(null)  from filled HD - hashtable
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
                hd.Remove(null);
                Assert.False(true, string.Format("Error, no exception"));
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            //
            //  [] Remove(special_object)  from filled HD - list
            //
            hd = new HybridDictionary();

            hd.Clear();
            len = 2;
            ArrayList[] b = new ArrayList[len];
            Hashtable[] lbl = new Hashtable[len];
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
                hd.Remove(lbl[i]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove special object"));
                }
                if (hd.Contains(lbl[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong special object"));
                }
            }

            //
            //  [] Remove(special_object)  from filled HD - hashtable
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
                hd.Remove(lbl[i]);
                if (hd.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove special object"));
                }
                if (hd.Contains(lbl[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong special object"));
                }
            }
        }
    }
}
