// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetCountStringCollectionTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            StringCollection sc;

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


            // initialize IntStrings
            intl = new IntlStrings();

            // [] StringCollection.Count is as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] Count on empty collection
            //
            if (sc.Count != 0)
            {
                Assert.False(true, string.Format("Error, returned {0} for empty collection", sc.Count));
            }

            // [] Count on collection filled with simple strings
            //

            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }
            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }


            //
            // Intl strings
            // [] Count on collection filled with Intl strings
            //

            string[] intlValues = new string[values.Length];

            // fill array with unique strings
            //
            for (int i = 0; i < values.Length; i++)
            {
                string val = intl.GetRandomString(MAX_LEN);
                while (Array.IndexOf(intlValues, val) != -1)
                    val = intl.GetRandomString(MAX_LEN);
                intlValues[i] = val;
            }

            sc.Clear();
            sc.AddRange(intlValues);
            if (sc.Count != intlValues.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }

            sc.Clear();
            sc.AddRange(intlValues);
            if (sc.Count != intlValues.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }
        }
    }
}
