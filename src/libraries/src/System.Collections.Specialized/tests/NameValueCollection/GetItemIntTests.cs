// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetItemIntNameValueCollectionTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            NameValueCollection nvc;

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
            string itm;         // item
            // initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            // [] get Item() on empty collection
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = nvc[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = nvc[0]; });

            // [] get Item() on collection filled with simple strings
            //

            cnt = nvc.Count;
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            if (nvc.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, values.Length));
            }

            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc[i], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[i], values[i]));
                }
            }


            //
            // Intl strings
            // [] get Item() on collection filled with Intl strings
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
                if (intlValues[i].Length != 0 && intlValues[i].ToLowerInvariant() == intlValues[i].ToUpperInvariant())
                    caseInsensitive = true;
            }


            nvc.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);
            }
            if (nvc.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                if (String.Compare(nvc[i], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc[i], intlValues[i]));
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
                intlValues[i] = intlValues[i].ToUpperInvariant();
            }

            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValues[i].ToLowerInvariant();
            }

            nvc.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                //
                if (String.Compare(nvc[i], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc[i], intlValues[i]));
                }

                if (!caseInsensitive && String.Compare(nvc[i], intlValuesLower[i]) == 0)
                {
                    Assert.False(true, string.Format("Error, returned lowercase when added uppercase", i));
                }
            }

            // [] get Item() on filled collection - multiple items with the same key
            //

            nvc.Clear();
            len = values.Length;
            string k = "keykey";
            string k1 = "hm1";
            string exp = "";
            string exp1 = "";
            for (int i = 0; i < len; i++)
            {
                nvc.Add(k, "Value" + i);
                nvc.Add(k1, "iTem" + i);
                if (i < len - 1)
                {
                    exp += "Value" + i + ",";
                    exp1 += "iTem" + i + ",";
                }
                else
                {
                    exp += "Value" + i;
                    exp1 += "iTem" + i;
                }
            }
            if (nvc.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, 2));
            }

            if (String.Compare(nvc[0], exp) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[0], exp));
            }
            if (String.Compare(nvc[1], exp1) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[1], exp1));
            }


            //
            //  [] Item(-1)
            //
            cnt = nvc.Count;

            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = nvc[-1]; });

            //
            //  [] Item(count)
            //
            if (nvc.Count < 1)
            {
                for (int i = 0; i < len; i++)
                {
                    nvc.Add(keys[i], values[i]);
                }
            }
            cnt = nvc.Count;
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = nvc[cnt]; });

            //
            //  [] Item(count+1)
            //
            if (nvc.Count < 1)
            {
                for (int i = 0; i < len; i++)
                {
                    nvc.Add(keys[i], values[i]);
                }
            }
            cnt = nvc.Count;
            Assert.Throws<ArgumentOutOfRangeException>(() => { itm = nvc[cnt + 1]; });

            //
            //  [] Item(null)
            //   Item(null)   - calls other overloaded version of Get - Get(string) - no exception
            //
            itm = nvc[null];
            if (itm != null)
            {
                Assert.False(true, "Error, returned non-null ");
            }
        }
    }
}
