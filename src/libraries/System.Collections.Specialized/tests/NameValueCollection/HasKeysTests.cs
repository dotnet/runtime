// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class HasKeysTests
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

            // [] initialize IntStrings
            intl = new IntlStrings();


            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();
            cnt = nvc.Count;
            if (cnt != 0)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1} after default ctor", nvc.Count, 0));
            }

            //
            //  [] on empty collection
            //
            if (nvc.HasKeys())
            {
                Assert.False(true, "Error, HasKeys returned true after default ctor");
            }


            //  [] Add simple strings and HasKeys()
            //
            cnt = nvc.Count;
            for (int i = 0; i < values.Length; i++)
            {
                nvc.Add(keys[i], values[i]);
            }
            if (nvc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, values.Length));
            }

            if (!nvc.HasKeys())
            {
                Assert.False(true, string.Format("Error, returned false for collection with {0} items", nvc.Count));
            }


            //
            // [] Add Intl strings and HasKeys()
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

            //   Add items
            //
            cnt = nvc.Count;
            for (int i = 0; i < len; i++)
            {
                nvc.Add(intlValues[i + len], intlValues[i]);
            }
            if (nvc.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, cnt + len));
            }

            if (!nvc.HasKeys())
            {
                Assert.False(true, string.Format("Error, returned false for collection with {0} items", nvc.Count));
            }

            //
            //  [] Add item with null-key and call HasKeys()
            //
            nvc.Clear();
            cnt = nvc.Count;
            for (int i = 0; i < values.Length; i++)
            {
                nvc.Add(null, values[i]);
            }
            if (nvc.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", nvc.Count, 1));
            }

            if (nvc.HasKeys())
            {
                Assert.False(true, "Error, returned true for collection null-key");
            }
        }
    }
}
