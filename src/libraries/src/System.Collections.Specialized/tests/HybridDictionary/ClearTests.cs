// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class ClearTests
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
            cnt = hd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after default ctor", hd.Count, 0));
            }

            // [] Clear() on empty dictionary
            // 
            hd.Clear();
            cnt = hd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", hd.Count, 0));
            }

            cnt = hd.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = hd.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }


            //  [] Add simple strings and Clear()
            //
            cnt = hd.Count;
            for (int i = 0; i < valuesShort.Length; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            if (hd.Count != valuesShort.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, valuesShort.Length));
            }

            hd.Clear();
            cnt = hd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", hd.Count, 0));
            }

            cnt = hd.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = hd.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }


            //
            // [] Add Intl strings and Clear()
            //
            int len = valuesShort.Length;
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

            //   Add items
            //
            cnt = hd.Count;
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt + len));
            }

            hd.Clear();
            cnt = hd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", hd.Count, 0));
            }

            cnt = hd.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = hd.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }

            //
            //  [] Add many simple strings and Clear()
            //
            hd.Clear();
            for (int i = 0; i < valuesLong.Length; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != valuesLong.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, valuesLong.Length));
            }

            hd.Clear();
            cnt = hd.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after Clear()", hd.Count, 0));
            }

            cnt = hd.Keys.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Keys.Count is {0} instead of {1} after Clear()", cnt, 0));
            }
            cnt = hd.Values.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, Values.Count is {0} instead of {1} after Clear()", cnt, 0));
            }

            // Clear should clear underlying collection's items
            hd = new HybridDictionary();

            for (int i = 0; i < 100; i++)
                hd.Add("key_" + i, "val_" + i);
            ICollection icol1 = hd.Keys;
            ICollection icol2 = hd.Values;
            hd.Clear();

            if (icol1.Count != 0)
            {
                Assert.False(true, string.Format("Error, icol1.Count wrong, expected {0} got {1}", 0, icol1.Count));
            }
            if (icol2.Count != 0)
            {
                Assert.False(true, string.Format("Error, icol2.Count wrong, expected {0} got {1}", 0, icol2.Count));
            }
        }
    }
}