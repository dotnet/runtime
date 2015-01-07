// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetIsSynchronizedStringCollectionTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
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

            // [] StringCollection.IsSynchronized should return false
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] on empty collection
            //
            if (sc.IsSynchronized)
            {
                Assert.False(true, string.Format("Error, returned true for empty collection"));
            }


            // [] on filled collection
            //

            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }
            if (sc.IsSynchronized)
            {
                Assert.False(true, string.Format("Error, returned true for filled collection"));
            }
        }
    }
}
