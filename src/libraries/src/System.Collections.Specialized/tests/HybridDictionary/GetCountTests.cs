// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetCountTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
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

            //
            // [] Clear empty dictionary and check Count
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


            //  [] Add simple strings (list) - Count -  Clear() - Count
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

            //   Hashtable
            //  [] Add simple strings (hashtable) - Count -  Clear() - Count
            //
            cnt = hd.Count;
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
        }
    }
}
