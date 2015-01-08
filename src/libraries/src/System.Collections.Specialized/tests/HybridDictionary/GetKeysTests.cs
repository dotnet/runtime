// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetKeysTests
    {
        public const int MAX_LEN = 50;          // max length of random strings


        [Fact]
        public void Test01()
        {
            const int BIG_LENGTH = 100;
            HybridDictionary hd;

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

            Array arr;
            ICollection ks;         // Keys collection
            int ind;

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary is constructed as expected
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            //  [] on empty dictionary
            //
            if (hd.Count > 0)
                hd.Clear();

            if (hd.Keys.Count != 0)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", hd.Keys.Count));
            }

            //  [] on short filled dictionary
            //
            int len = valuesShort.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            ks = hd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, keysShort[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key. Search result: {2}", i, keysShort[i], ind));
                }
            }

            //  [] on long filled dictionary
            //
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            ks = hd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), len);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < len; i++)
            {
                ind = Array.IndexOf(arr, keysLong[i]);
                if (ind < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain \"{1}\" key. Search result: {2}", i, keysLong[i], ind));
                }
            }



            //
            //  [] get Keys on dictionary with different_in_casing_only keys - list
            //

            hd.Clear();
            string intlStr = "intlStr";

            hd.Add("keykey", intlStr);        // 1st key
            hd.Add("keyKey", intlStr);        // 2nd key
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, 2));
            }

            // get Keys
            //

            ks = hd.Keys;
            if (ks.Count != hd.Count)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), 2);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, "keykey");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain {0} value", "keykey"));
            }
            ind = Array.IndexOf(arr, "keyKey");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain {0} value", "keyKey"));
            }

            //
            //  [] get Keys on dictionary with different_in_casing_only keys - hashtable
            //
            hd.Clear();

            hd.Add("keykey", intlStr);        // 1st key
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            hd.Add("keyKey", intlStr);        // 2nd key
            if (hd.Count != BIG_LENGTH + 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, BIG_LENGTH + 2));
            }

            // get Keys
            //

            ks = hd.Keys;
            if (ks.Count != hd.Count)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }
            arr = Array.CreateInstance(typeof(Object), BIG_LENGTH + 2);
            ks.CopyTo(arr, 0);
            for (int i = 0; i < BIG_LENGTH; i++)
            {
                if (Array.IndexOf(arr, keysLong[i]) < 0)
                {
                    Assert.False(true, string.Format("Error, Keys doesn't contain {0} key", keysLong[i], i));
                }
            }
            ind = Array.IndexOf(arr, "keykey");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain {0} key", "keykey"));
            }
            ind = Array.IndexOf(arr, "keyKey");
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain {0} key", "keyKey"));
            }

            //
            //   Change dictionary
            //  [] change long dictionary and check Keys

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

            ks = hd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }

            hd.Remove(keysLong[0]);
            if (hd.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove element"));
            }
            if (ks.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after removal"));
            }
            arr = Array.CreateInstance(typeof(Object), hd.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keysLong[0]);
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Keys still contains removed value " + ind));
            }

            hd.Add(keysLong[0], "new item");
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, didn't add element"));
            }
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after addition"));
            }
            arr = Array.CreateInstance(typeof(Object), hd.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keysLong[0]);
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain added value "));
            }

            //
            //  [] change short dictionary and check Keys
            //
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

            ks = hd.Keys;
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, returned Keys.Count = {0}", ks.Count));
            }

            hd.Remove(keysShort[0]);
            if (hd.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove element"));
            }
            if (ks.Count != len - 1)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after removal"));
            }
            arr = Array.CreateInstance(typeof(Object), hd.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keysShort[0]);
            if (ind >= 0)
            {
                Assert.False(true, string.Format("Error, Keys still contains removed value " + ind));
            }

            hd.Add(keysShort[0], "new item");
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, didn't add element"));
            }
            if (ks.Count != len)
            {
                Assert.False(true, string.Format("Error, Keys were not updated after addition"));
            }
            arr = Array.CreateInstance(typeof(Object), hd.Count);
            ks.CopyTo(arr, 0);
            ind = Array.IndexOf(arr, keysShort[0]);
            if (ind < 0)
            {
                Assert.False(true, string.Format("Error, Keys doesn't contain added value "));
            }
        }
    }
}
