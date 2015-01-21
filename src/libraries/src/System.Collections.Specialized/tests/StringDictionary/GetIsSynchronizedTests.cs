// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetIsSynchronizedStringDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringDictionary sd;
            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aa",
                "text",
                "     spaces",
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
                "one",
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

            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] on empty dictionary
            //
            if (sd.IsSynchronized)
            {
                Assert.False(true, string.Format("Error, IsSynchronized returned true"));
            }


            // [] on filled dictionary
            //
            for (int i = 0; i < values.Length; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, values.Length));
            }
            if (sd.IsSynchronized)
            {
                Assert.False(true, string.Format("Error, IsSynchronized returned true"));
            }
        }
    }
}
