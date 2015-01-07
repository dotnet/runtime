// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetIsReadOnlyListDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            ListDictionary ld;
            int cnt;

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

            // [] ListDictionary.IsReadOnly should return false
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            //  [] on empty dictionary
            //
            if (ld.IsReadOnly)
            {
                Assert.False(true, string.Format("Error, returned true for empty dictionary"));
            }

            //  [] on filled dictionary
            //

            ld.Clear();
            cnt = values.Length;
            for (int i = 0; i < cnt; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, cnt));
            }
            if (ld.IsReadOnly)
            {
                Assert.False(true, string.Format("Error, returned true for filled dictionary"));
            }
        }
    }
}
