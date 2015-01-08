// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class RemoveStrTests
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
            string[] ks;           // Keys array

            // initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            //  [] Remove() on empty collection
            //
            cnt = nvc.Count;
            nvc.Remove(null);
            if (nvc.Count != cnt)
            {
                Assert.False(true, "Error, changed collection after Remove(null)");
            }
            cnt = nvc.Count;
            nvc.Remove("some_string");
            if (nvc.Count != cnt)
            {
                Assert.False(true, "Error, changed collection after Remove(some_string)");
            }

            //  [] Remove() on collection filled with simple strings
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
            //

            for (int i = 0; i < len; i++)
            {
                cnt = nvc.Count;
                nvc.Remove(keys[i]);
                if (nvc.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item", i));
                }
                ks = nvc.AllKeys;
                if (Array.IndexOf(ks, keys[i]) > -1)
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
            }


            //
            // Intl strings
            //  [] Remove() on collection filled with Intl strings
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


            cnt = nvc.Count;
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);
            }
            if (nvc.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, cnt + len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                cnt = nvc.Count;
                nvc.Remove(intlValues[i + len]);
                if (nvc.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item", i));
                }
                ks = nvc.AllKeys;
                if (Array.IndexOf(ks, intlValues[i + len]) > -1)
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

                cnt = nvc.Count;
                nvc.Remove(intlValues[i + len]);
                if (nvc.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item", i));
                }
                ks = nvc.AllKeys;
                if (Array.IndexOf(ks, intlValues[i + len]) > -1)
                {
                    Assert.False(true, string.Format("Error, removed wrong item", i));
                }
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
                // lowercase key

                cnt = nvc.Count;
                nvc.Remove(intlValuesLower[i + len]);
                if (nvc.Count != cnt - 1)
                {
                    Assert.False(true, string.Format("Error, returned: failed to remove item using lowercase key", i));
                }
                ks = nvc.AllKeys;
                if (Array.IndexOf(ks, intlValues[i + len]) > -1)
                {
                    Assert.False(true, string.Format("Error, removed wrong item using lowercase key", i));
                }
            }


            //  [] Remove() on filled collection - with multiple items with the same key
            //

            nvc.Clear();
            len = values.Length;
            string k = "keykey";
            string k1 = "hm1";
            for (int i = 0; i < len; i++)
            {
                nvc.Add(k, "Value" + i);
                nvc.Add(k1, "iTem" + i);
            }
            if (nvc.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, 2));
            }

            nvc.Remove(k);
            if (nvc.Count != 1)
            {
                Assert.False(true, "Error, failed to remove item");
            }
            ks = nvc.AllKeys;
            if (Array.IndexOf(ks, k) > -1)
            {
                Assert.False(true, "Error, removed wrong item");
            }

            nvc.Remove(k1);
            if (nvc.Count != 0)
            {
                Assert.False(true, "Error, failed to remove item");
            }
            ks = nvc.AllKeys;
            if (Array.IndexOf(ks, k1) > -1)
            {
                Assert.False(true, "Error, removed wrong item");
            }


            //
            //  [] Remove(null) - when there is an item with null-key
            //
            cnt = nvc.Count;
            nvc.Add(null, "nullValue");
            if (nvc.Count != cnt + 1)
            {
                Assert.False(true, "Error, failed to add item with null-key");
            }
            if (nvc[null] == null)
            {
                Assert.False(true, "Error, didn't add item with null-key");
            }

            cnt = nvc.Count;
            nvc.Remove(null);
            if (nvc.Count != cnt - 1)
            {
                Assert.False(true, "Error, failed to remove item");
            }
            if (nvc[null] != null)
            {
                Assert.False(true, "Error, didn't remove item with null-key");
            }

            //
            //  [] Remove(null)   - when no item with null key
            //
            nvc.Clear();
            for (int i = 0; i < len; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            cnt = nvc.Count;

            nvc.Remove(null);
            if (nvc.Count != cnt)
            {
                Assert.False(true, "Error, removed something ");
            }
        }
    }
}
