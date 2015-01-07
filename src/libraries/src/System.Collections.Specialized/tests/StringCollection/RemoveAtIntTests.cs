// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class RemoveAtIntTests
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

            // [] initialize IntStrings
            intl = new IntlStrings();


            // [] StringCollection is constructed as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] RemoveAt() for empty collection
            //
            if (sc.Count > 0)
                sc.Clear();
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.RemoveAt(0); });

            // [] RemoveAt() on filled collection
            //


            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            sc.RemoveAt(0);

            if (sc.Count != values.Length - 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", sc.Count, values.Length - 1));
            }

            if (sc.Contains(values[0]))
            {
                Assert.False(true, string.Format("Error, removed wrong item"));
            }

            // check that all init items were moved
            for (int i = 0; i < values.Length; i++)
            {
                if (sc.IndexOf(values[i]) != i - 1)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {2}", i, sc.IndexOf(values[i]), i - 1));
                }
            }

            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            sc.RemoveAt(values.Length - 1);

            if (sc.Count != values.Length - 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", sc.Count, values.Length - 1));
            }

            if (sc.Contains(values[values.Length - 1]))
            {
                Assert.False(true, string.Format("Error, removed wrong item"));
            }

            // check that all init items were moved
            for (int i = 0; i < values.Length - 1; i++)
            {
                if (sc.IndexOf(values[i]) != i)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {2}", i, sc.IndexOf(values[i]), i));
                }
            }


            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            sc.RemoveAt(values.Length / 2);

            if (sc.Count != values.Length - 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", sc.Count, values.Length - 1));
            }

            if (sc.Contains(values[values.Length / 2]))
            {
                Assert.False(true, string.Format("Error, removed wrong item"));
            }

            // check that all init items were moved
            for (int i = 0; i < values.Length; i++)
            {
                int expected = i;
                if (i == values.Length / 2)
                    expected = -1;
                else
                    if (i > values.Length / 2)
                    expected = i - 1;
                if (sc.IndexOf(values[i]) != expected)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {2}", i, sc.IndexOf(values[i]), expected));
                }
            }


            //
            // [] RemoveAt() on collection with identical items
            //

            sc.Clear();
            string intlStr = intl.GetRandomString(MAX_LEN);

            sc.Add(intlStr);        // index 0
            sc.AddRange(values);
            sc.Add(intlStr);        // second index values.Length + 1
            if (sc.Count != values.Length + 2)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sc.Count, values.Length + 2));
            }

            // remove  
            //
            sc.RemoveAt(values.Length + 1);
            if (!sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, removed both duplicates"));
            }
            // second string should still be present
            if (sc.IndexOf(intlStr) != 0)
            {
                Assert.False(true, string.Format("Error, removed 1st instance"));
            }


            //
            // [] Invalid parameter
            //


            sc.Clear();
            sc.AddRange(values);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.RemoveAt(-1); });
            sc.Clear();
            sc.AddRange(values);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.RemoveAt(sc.Count); });
            sc.Clear();
            sc.AddRange(values);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.RemoveAt(sc.Count + 1); });
        }
    }
}
