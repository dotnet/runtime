// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class RemoveObjListDictionaryTests
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

            // [] Remove() on empty dictionary
            //
            cnt = ld.Count;
            Assert.Throws<ArgumentNullException>(() => { ld.Remove(null); });

            cnt = ld.Count;
            ld.Remove("some_string");
            if (ld.Count != cnt)
            {
                Assert.False(true, string.Format("Error, changed dictionary after Remove(some_string)"));
            }

            cnt = ld.Count;
            ld.Remove(new Hashtable());
            if (ld.Count != cnt)
            {
                Assert.False(true, string.Format("Error, changed dictionary after Remove(some_string)"));
            }

            // [] add simple strings and Remove()
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
                cnt = ld.Count;
                ld.Remove(keys[i]);
                if (ld.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item", i));
                }
                if (ld.Contains(keys[i]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }


            //
            // Intl strings
            // [] Add Intl strings and Remove()
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
                cnt = ld.Count;
                ld.Remove(intlValues[i + len]);
                if (ld.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item", i));
                }
                if (ld.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
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

                cnt = ld.Count;
                ld.Remove(intlValues[i + len]);
                if (ld.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item", i));
                }
                if (ld.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
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
                ld.Remove(intlValuesLower[i + len]);
                if (!caseInsensitive && ld.Count != cnt)
                {
                    Assert.False(true, string.Format("Error, failed: removed item using lowercase key", i));
                }
            }


            //
            // [] Remove() on LD with case-insensitive comparer
            //
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
                cnt = ld.Count;
                ld.Remove(kk.ToUpper() + i);
                if (ld.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, failed to remove item", i));
                }
                if (ld.Contains(kk + i))
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }


            //
            //   [] Remove(null)
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

            Assert.Throws<ArgumentNullException>(() => { ld.Remove(null); });

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

            cnt = ld.Count;
            ld.Remove(lbl);
            if (ld.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, failed to remove special object"));
            }
            if (ld.Contains(lbl))
            {
                Assert.False(true, string.Format("Error, removed wrong special object"));
            }
        }
    }
}
