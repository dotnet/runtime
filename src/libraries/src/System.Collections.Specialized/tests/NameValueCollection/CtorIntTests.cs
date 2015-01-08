// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorIntNameValueCollectionTests
    {
        [Fact]
        public void Test01()
        {
            NameValueCollection nvc;


            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aa",
                "tExt",
                "     SPaces",
                "1",
                "$%^#",
                "2222222222222222222222222",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // names(keys) for simple string values
            string[] names =
            {
                "zero",
                "oNe",
                " ",
                "",
                "aA",
                "1",
                System.DateTime.Today.ToString(),
                "$%^#",
                Int32.MaxValue.ToString(),
                "     spaces",
                "2222222222222222222222222"
            };

            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------


            //
            //  [] create with capacity 10
            //
            nvc = new NameValueCollection(10);

            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} for empty with capacity 10", nvc.Count));
            }


            int len = values.Length;

            for (int i = 0; i < 10; i++)
            {
                nvc.Add(names[i], values[i]);
            }
            if (nvc.Count != 10)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of 10", nvc.Count));
            }

            for (int i = 0; i < 5; i++)
            {
                nvc.Add("key" + i, "item" + i);
            }
            if (nvc.Count != 15)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of 15", nvc.Count));
            }

            //////////////////////////////////////////////////////////////  
            //
            //  [] create with capacity 100
            //
            nvc = new NameValueCollection(100);

            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} for empty with capacity 100", nvc.Count));
            }

            for (int i = 0; i < 100; i++)
            {
                nvc.Add(names[i / 10] + i, values[i / 10] + i);
            }
            if (nvc.Count != 100)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of 100", nvc.Count));
            }

            for (int i = 0; i < 70; i++)
            {
                nvc.Add("key" + i, "item" + i);
            }
            if (nvc.Count != 170)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of 170", nvc.Count));
            }

            //
            //  [] invalid parameter
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => { nvc = new NameValueCollection(-1); });
        }
    }
}
