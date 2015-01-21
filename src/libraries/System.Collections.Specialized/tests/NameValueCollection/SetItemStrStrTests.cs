// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class SetItemStrStrTests
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

            // [] initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            // [] set Item() on empty collection
            //
            nvc.Clear();
            nvc[null] = "nullItem";
            if (nvc.Count != 1)
            {
                Assert.False(true, "Error, failed to add item");
            }
            if (nvc[null] == null)
            {
                Assert.False(true, "Error, returned null");
            }
            else
            {
                if (String.Compare(nvc[null], "nullItem") != 0)
                {
                    Assert.False(true, "Error, wrong value");
                }
            }

            nvc.Clear();
            nvc["some_string"] = "someItem";
            if (nvc.Count != 1)
            {
                Assert.False(true, "Error, failed to add item");
            }
            if (nvc["some_string"] == null)
            {
                Assert.False(true, "Error, returned null");
            }
            else
            {
                if (String.Compare(nvc["some_string"], "someItem") != 0)
                {
                    Assert.False(true, "Error, wrong value");
                }
            }

            // [] set Item(string) on collection filled with simple strings
            //

            nvc.Clear();
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
            //
            for (int i = 0; i < len; i++)
            {
                nvc[keys[i]] = "Item" + i;
                if (String.Compare(nvc[keys[i]], "Item" + i) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[keys[i]], "Item" + i));
                }
            }


            //
            // Intl strings
            // [] set Item(string) on collection filled with Intl strings
            //

            string[] intlValues = new string[len * 3];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 3; i++)
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
                nvc[intlValues[i + len]] = intlValues[i + len * 2];
                if (String.Compare(nvc[intlValues[i + len]], intlValues[i + len * 2]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc[intlValues[i + len]], intlValues[i + len * 2]));
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
                // uppercase key
                if (String.Compare(nvc[intlValues[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc[intlValues[i + len]], intlValues[i]));
                }

                // lowercase key
                if (String.Compare(nvc[intlValuesLower[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc[intlValuesLower[i + len]], intlValues[i]));
                }

                if (!caseInsensitive && String.Compare(nvc[intlValues[i + len]], intlValuesLower[i]) == 0)
                {
                    Assert.False(true, string.Format("Error, returned lowercase when added uppercase", i));
                }

                // set to lowercase value
                nvc[intlValues[i + len]] = intlValuesLower[i];
                // uppercase key
                if (!caseInsensitive && String.Compare(nvc[intlValues[i + len]], intlValues[i]) == 0)
                {
                    Assert.False(true, string.Format("Error, failed to set to uppercase value", i));
                }

                // lowercase key
                if (!caseInsensitive && String.Compare(nvc[intlValuesLower[i + len]], intlValues[i]) == 0)
                {
                    Assert.False(true, string.Format("Error, failed to set to lowercase value", i));
                }

                if (String.Compare(nvc[intlValues[i + len]], intlValuesLower[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned uppercase when set to lowercase", i));
                }
            }

            // [] set Item(string) on filled collection - multiple item swith the same key
            //

            nvc.Clear();
            len = values.Length;
            string k = "keykey";
            string k1 = "hm1";
            string exp = "";
            string exp1 = "";
            string newVal = "nEw1,nEw2";
            string newVal1 = "Hello,hello,hELLo";
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

            if (String.Compare(nvc[k], exp) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[k], exp));
            }

            nvc[k] = newVal;
            if (String.Compare(nvc[k], newVal) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[k], newVal));
            }
            // Values array should contain 1 item
            if (nvc.GetValues(k).Length != 1)
            {
                Assert.False(true, string.Format("Error, number of values is {0} instead of 1", nvc.GetValues(k).Length));
            }

            if (String.Compare(nvc[k1], exp1) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[k1], exp1));
            }
            nvc[k1] = newVal1;
            if (String.Compare(nvc[k1], newVal1) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[k1], newVal1));
            }
            // Values array should contain 1 item
            if (nvc.GetValues(k1).Length != 1)
            {
                Assert.False(true, string.Format("Error, number of values is {0} instead of 1", nvc.GetValues(k).Length));
            }


            //
            // [] set Item(null) - when there is an item with null key
            //
            cnt = nvc.Count;
            nvc.Add(null, "nullValue");
            if (String.Compare(nvc[null], "nullValue") != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[null], "nullValue"));
            }

            nvc[null] = "newnewValue";
            if (String.Compare(nvc[null], "newnewValue") != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc[null], "newnewValue"));
            }

            //
            //  [] set Item(null)   - when no item with null key
            //
            nvc.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            nvc[null] = "newNullValue";

            itm = nvc[null];
            if (String.Compare(itm, "newNullValue") != 0)
            {
                Assert.False(true, "Error, returned unexpected value ");
            }
        }
    }
}
