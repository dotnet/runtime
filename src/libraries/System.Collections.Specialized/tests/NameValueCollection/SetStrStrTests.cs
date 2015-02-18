// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class SetStrStrTests
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

            // initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            //  [] Set() - new simple strings
            //
            for (int i = 0; i < values.Length; i++)
            {
                cnt = nvc.Count;
                nvc.Set(keys[i], values[i]);
                if (nvc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, nvc.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (Array.IndexOf(nvc.AllKeys, keys[i]) < 0)
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }

                //  access the item
                //
                if (String.Compare(nvc[keys[i]], values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, nvc[keys[i]], values[i]));
                }
            }

            //
            // Intl strings
            // [] Set() - new Intl strings
            //
            int len = values.Length;
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


            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                cnt = nvc.Count;

                nvc.Set(intlValues[i + len], intlValues[i]);
                if (nvc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, nvc.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (Array.IndexOf(nvc.AllKeys, intlValues[i + len]) < 0)
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }

                //  access the item
                //
                if (String.Compare(nvc[intlValues[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, nvc[intlValues[i + len]], intlValues[i]));
                }
            }

            //
            // [] Case sensitivity
            // Casing doesn't change ( keys are not converted to lower!)
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
                cnt = nvc.Count;
                // add uppercase items
                nvc.Set(intlValues[i + len], intlValues[i]);
                if (nvc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, nvc.Count, cnt + 1));
                }

                // verify that collection contains newly added uppercase item
                //
                if (Array.IndexOf(nvc.AllKeys, intlValues[i + len]) < 0)
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }

                //  access the item
                //
                if (String.Compare(nvc[intlValues[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, nvc[intlValues[i + len]], intlValues[i]));
                }

                // verify that collection doesn't contains lowercase item
                //
                if (!caseInsensitive && String.Compare(nvc[intlValuesLower[i + len]], intlValuesLower[i]) == 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" is lowercase after adding uppercase", i, nvc[intlValuesLower[i + len]]));
                }

                // key is not converted to lower
                if (!caseInsensitive && Array.IndexOf(nvc.AllKeys, intlValuesLower[i + len]) >= 0)
                {
                    Assert.False(true, string.Format("Error, key was converted to lower", i));
                }

                // but search among keys is case-insensitive
                if (String.Compare(nvc[intlValuesLower[i + len]], intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, could not find item using differently cased key", i));
                }
            }

            //
            //   [] Set multiple values with the same key
            //

            nvc.Clear();
            len = values.Length;
            string k = "keykey";

            for (int i = 0; i < len; i++)
            {
                nvc.Set(k, "Value" + i);
                // should replace previous value
                if (nvc.Count != 1)
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of 1", nvc.Count, i));
                }
                if (String.Compare(nvc[k], "Value" + i) != 0)
                {
                    Assert.False(true, string.Format("Error, didn't replace value", i));
                }
            }

            if (nvc.AllKeys.Length != 1)
            {
                Assert.False(true, "Error, should contain only 1 key");
            }

            // verify that collection contains newly added item
            //
            if (Array.IndexOf(nvc.AllKeys, k) < 0)
            {
                Assert.False(true, "Error, collection doesn't contain key of new item");
            }

            //  access the item
            //
            string[] vals = nvc.GetValues(k);
            if (vals.Length != 1)
            {
                Assert.False(true, string.Format("Error, number of values at given key is {0} instead of {1}", vals.Length, 1));
            }

            if (Array.IndexOf(vals, "Value" + (len - 1).ToString()) < 0)
            {
                Assert.False(true, string.Format("Error, value is not {0}", "Value" + (len - 1)));
            }

            //
            // [] Set(string, null)
            //

            k = "kk";

            nvc.Remove(k);      // make sure there is no such item already
            cnt = nvc.Count;
            nvc.Set(k, null);

            if (nvc.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, cnt + 1));
            }

            if (Array.IndexOf(nvc.AllKeys, k) < 0)
            {
                Assert.False(true, "Error, collection doesn't contain key of new item");
            }

            // verify that collection contains null
            //
            if (nvc[k] != null)
            {
                Assert.False(true, "Error, returned non-null on place of null");
            }

            nvc.Remove(k);      // make sure there is no such item already
            nvc.Add(k, "kItem");
            cnt = nvc.Count;
            nvc.Set(k, null);

            if (nvc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count has changed: {0} instead of {1}", nvc.Count, cnt));
            }

            if (Array.IndexOf(nvc.AllKeys, k) < 0)
            {
                Assert.False(true, "Error, collection doesn't contain key of new item");
            }

            // verify that item at k-key was replaced with null
            //
            if (nvc[k] != null)
            {
                Assert.False(true, "Error, non-null was not replaced with null");
            }

            //
            // Set item with null key - no NullReferenceException expected
            // [] Set(null, string)
            //

            nvc.Remove(null);
            cnt = nvc.Count;

            nvc.Set(null, "item");

            if (nvc.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, cnt + 1));
            }

            if (Array.IndexOf(nvc.AllKeys, null) < 0)
            {
                Assert.False(true, "Error, collection doesn't contain null key ");
            }

            // verify that collection contains null
            //
            if (nvc[null] != "item")
            {
                Assert.False(true, "Error, returned wrong value at null key");
            }


            // replace item with null key
            cnt = nvc.Count;
            nvc.Set(null, "newItem");

            if (nvc.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count has changed: {0} instead of {1}", nvc.Count, cnt));
            }

            if (Array.IndexOf(nvc.AllKeys, null) < 0)
            {
                Assert.False(true, "Error, collection doesn't contain null key ");
            }

            // verify that item with null key was replaced
            //
            if (nvc[null] != "newItem")
            {
                Assert.False(true, "Error, didn't replace value at null key");
            }
        }
    }
}
