// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetIsFixedSizeTests
    {
        public const int MAX_LEN = 50;          // max length of random strings


        [Fact]
        public void Test01()
        {
            HybridDictionary hd;
            int cnt;

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

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary.IsFixedSize should return false
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            // [] on empty dictionary
            //
            if (hd.IsFixedSize)
            {
                Assert.False(true, string.Format("Error, returned true for empty dictionary"));
            }

            //  [] on short filled dictionary
            //

            hd.Clear();
            cnt = valuesShort.Length;
            for (int i = 0; i < cnt; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            if (hd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt));
            }
            if (hd.IsFixedSize)
            {
                Assert.False(true, string.Format("Error, returned true for short filled dictionary"));
            }

            //  [] on long filled dictionary
            //

            hd.Clear();
            cnt = valuesLong.Length;
            for (int i = 0; i < cnt; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt));
            }
            if (hd.IsFixedSize)
            {
                Assert.False(true, string.Format("Error, returned true for long filled dictionary"));
            }
        }
    }
}
