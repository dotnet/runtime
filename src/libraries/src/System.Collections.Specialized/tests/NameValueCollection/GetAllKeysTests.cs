// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetAllKeysTests
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
            string[] ks;          // keys array

            // initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            // [] AllKeys on empty collection
            //
            if (nvc.Count > 0)
                nvc.Clear();
            ks = nvc.AllKeys;
            if (ks.Length != 0)
            {
                Assert.False(true, string.Format("Error, number of keys is {0} instead of 0", ks.Length));
            }

            // [] AllKeys on collection filled with simple strings
            //
            for (int i = 0; i < values.Length; i++)
            {
                cnt = nvc.Count;
                nvc.Add(keys[i], values[i]);
                if (nvc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, nvc.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (nvc.AllKeys.Length != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, incorrects Keys array", i));
                }

                if (Array.IndexOf(nvc.AllKeys, keys[i]) < 0)
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }
            }

            //
            // Intl strings
            // [] AllKeys on collection filled with Intl strings
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

                nvc.Add(intlValues[i + len], intlValues[i]);
                if (nvc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, nvc.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (nvc.AllKeys.Length != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, wrong keys array", i));
                }

                if (Array.IndexOf(nvc.AllKeys, intlValues[i + len]) < 0)
                {
                    Assert.False(true, string.Format("Error, Array doesn't contain key of new item", i));
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
                nvc.Add(intlValues[i + len], intlValues[i]);
                if (nvc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, nvc.Count, cnt + 1));
                }

                // verify that collection contains newly added uppercase item
                //
                if (nvc.AllKeys.Length != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, wrong keys array", i));
                }
                if (Array.IndexOf(nvc.AllKeys, intlValues[i + len]) < 0)
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain key of new item", i));
                }

                // key is not converted to lower
                if (!caseInsensitive && Array.IndexOf(nvc.AllKeys, intlValuesLower[i + len]) >= 0)
                {
                    Assert.False(true, string.Format("Error, key was converted to lower", i));
                }
            }

            //
            //  [] AllKeys for multiple values with the same key
            //

            nvc.Clear();
            len = values.Length;
            string k = "keykey";

            for (int i = 0; i < len; i++)
            {
                nvc.Add(k, "Value" + i);
                if (nvc.Count != 1)
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of 1", nvc.Count, i));
                }
                if (nvc.AllKeys.Length != 1)
                {
                    Assert.False(true, string.Format("Error, AllKeys contains {0} instead of 1", nvc.AllKeys.Length, i));
                }
                if (Array.IndexOf(nvc.AllKeys, k) != 0)
                {
                    Assert.False(true, string.Format("Error, wrong key", i));
                }
            }

            //  access the item
            //
            string[] vals = nvc.GetValues(k);
            if (vals.Length != len)
            {
                Assert.False(true, string.Format("Error, number of values at given key is {0} instead of {1}", vals.Length, 1));
            }

            //
            // [] AllKeys when collection has null value
            //

            k = "kk";

            nvc.Remove(k);      // make sure there is no such item already
            cnt = nvc.Count;
            nvc.Add(k, null);

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

            //
            // [] Allkeys when item with null key is present
            //

            nvc.Remove(null);
            cnt = nvc.Count;

            nvc.Add(null, "item");

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
        }
    }
}
