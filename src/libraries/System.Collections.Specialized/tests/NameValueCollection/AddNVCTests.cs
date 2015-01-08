// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class AddNVCTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;


            NameValueCollection nvc;
            NameValueCollection nvc1;

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


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();
            nvc1 = new NameValueCollection();

            // [] Add(empty_coll) to empty collection
            //
            nvc.Clear();
            nvc1.Clear();
            nvc.Add(nvc1);
            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of 0", nvc.Count));
            }

            // [] Add(simple_strings_coll) to empty collection
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

            nvc1.Clear();
            nvc1.Add(nvc);
            if (nvc1.Count != nvc.Count)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc1.Count, nvc.Count));
            }
            //
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1[keys[i]], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[keys[i]], values[i]));
                }
            }

            // [] Add(simple_strings_coll) to simple_string_collection
            //

            len = values.Length;
            if (nvc.Count < len)
            {
                nvc.Clear();
                for (int i = 0; i < len; i++)
                {
                    nvc.Add(keys[i], values[i]);
                }
            }
            nvc1.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc1.Add("k" + i, "v" + i);
            }

            cnt = nvc1.Count;
            nvc1.Add(nvc);
            if (nvc1.Count != cnt + nvc.Count)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc1.Count, cnt + nvc.Count));
            }
            //
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1[keys[i]], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[keys[i]], values[i]));
                }
                if (String.Compare(nvc1["k" + i], "v" + i) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc["k" + i], "v" + i));
                }
            }


            //
            // Intl strings
            // [] Add(intl_strings_coll) to empty collection 
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


            // fill init collection with intl strings
            nvc.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);
            }
            if (nvc.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, len));
            }

            // add filled collection to tested empty collection
            nvc1.Clear();
            nvc1.Add(nvc);

            for (int i = 0; i < len; i++)
            {
                //
                if (String.Compare(nvc1[intlValues[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc1[intlValues[i + len]], intlValues[i]));
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

            // add uppercase to tested collection
            nvc1.Clear();
            nvc1.Add(nvc);

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                if (String.Compare(nvc1[intlValues[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc1[intlValues[i + len]], intlValues[i]));
                }

                // lowercase key
                if (String.Compare(nvc1[intlValuesLower[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned \"{1}\" instead of \"{2}\"", i, nvc1[intlValuesLower[i + len]], intlValues[i]));
                }

                if (!caseInsensitive && (String.Compare(nvc1[intlValues[i + len]], intlValuesLower[i]) == 0))
                {
                    Assert.False(true, string.Format("Error, returned lowercase when added uppercase", i));
                }
            }

            //
            // when adding values with existing keys, values should be added to existing values
            // [] Add(NVC) with already existing keys
            //

            nvc.Clear();
            cnt = nvc.Count;
            len = values.Length;
            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            if (nvc.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, values.Length));
            }

            nvc1.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc1.Add(keys[i], values[i]);
            }
            if (nvc1.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc1.Count, values.Length));
            }
            cnt = nvc1.Count;

            nvc1.Add(nvc);
            // count should not change
            if (nvc1.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc1.Count, cnt));
            }
            //  values should be added to previously existed values
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1[keys[i]], values[i] + "," + values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[keys[i]], values[i] + "," + values[i]));
                }
            }

            // [] multiple items with same key
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

            // add NVC
            nvc1.Clear();
            nvc1.Add(nvc);

            if (String.Compare(nvc1[k], exp) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc1[k], exp));
            }

            // Values array should contain len items
            if (nvc1.GetValues(k).Length != len)
            {
                Assert.False(true, string.Format("Error, number of values is {0} instead of {1}", nvc1.GetValues(k).Length, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1.GetValues(k)[i], "Value" + i) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, nvc1.GetValues(k)[i], "Value" + i));
                }
            }

            if (String.Compare(nvc1[k1], exp1) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc1[k1], exp1));
            }
            // Values array should contain len items
            if (nvc1.GetValues(k1).Length != len)
            {
                Assert.False(true, string.Format("Error, number of values is {0} instead of {1}", nvc1.GetValues(k1).Length, len));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1.GetValues(k1)[i], "iTem" + i) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {1} instead of {2}", i, nvc1.GetValues(k1)[i], "iTem" + i));
                }
            }


            //
            //  [] Add(with_null_key) when there is item with null key
            //
            cnt = nvc.Count;
            nvc.Add(null, "nullValue");
            nvc1[null] = "nullValue1";

            nvc1.Add(nvc);
            if (String.Compare(nvc1[null], "nullValue1," + nvc[null]) != 0)
            {
                Assert.False(true, string.Format("Error, returned \"{0}\" instead of \"{1}\"", nvc1[null], "nullValue1," + nvc[null]));
            }

            //
            //  [] Add(with_null_key) when there is no item with null key
            //
            nvc.Clear();
            nvc1.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc1.Add(keys[i], values[i]);
            }
            nvc[null] = "newNullValue";

            nvc1.Add(nvc);
            if (String.Compare(nvc1[null], "newNullValue") != 0)
            {
                Assert.False(true, "Error, returned unexpected value ");
            }

            //
            //  [] Add(null_collection)
            //
            try
            {
                nvc1.Add(null);
                Assert.False(true, "Error, no exception");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Error, unexpected exception: {0}", e.ToString()));
            }

            //
            //  [] Add(empty_coll) to filled collection
            //

            nvc.Clear();
            if (nvc1.Count < len)
            {
                nvc1.Clear();
                for (int i = 0; i < len; i++)
                {
                    nvc1.Add(keys[i], values[i]);
                }
                if (nvc1.Count != len)
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc1.Count, values.Length));
                }
            }

            cnt = nvc1.Count;
            nvc1.Add(nvc);
            if (nvc1.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count has changed: {0} instead of {1}", nvc1.Count, cnt));
            }
            //   verify that collection has not changed
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1[keys[i]], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[keys[i]], values[i]));
                }
            }


            //
            //  [] Add collection with null values
            //
            nvc.Clear();
            nvc1.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], null);
            }

            nvc1.Add(nvc);
            if (nvc1.Count != nvc.Count)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc1.Count, nvc.Count));
            }
            //
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc1[keys[i]], null) != 0)
                {
                    Assert.False(true, string.Format("Error, returned: \"{1}\", expected \"{2}\"", i, nvc[keys[i]], values[i]));
                }
            }
        }
    }
}
