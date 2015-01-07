// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class InsertIntStrTests
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


            // [] StringCollection is constructed as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] Insert into empty collection
            //
            for (int i = 0; i < values.Length; i++)
            {
                if (sc.Count > 0)
                    sc.Clear();
                sc.Insert(0, values[i]);
                if (sc.Count != 1)
                {
                    Assert.False(true, string.Format("Error, Count {1} instead of 1", i, sc.Count));
                }
                if (!sc.Contains(values[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain just inserted item", i));
                }
            }

            //
            // [] Insert into filled collection



            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            // string to insert
            string val = intl.GetRandomString(MAX_LEN);

            sc.Insert(0, val);

            if (sc.Count != values.Length + 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", sc.Count, values.Length + 1));
            }

            if (sc.IndexOf(val) != 0)
            {
                Assert.False(true, string.Format("Error, IndexOf returned {0} instead of {1}", sc.IndexOf(val), 0));
            }

            // check that all init items were moved
            for (int i = 0; i < values.Length; i++)
            {
                if (sc.IndexOf(values[i]) != i + 1)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {2}", i, sc.IndexOf(values[i]), i + 1));
                }
            }

            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            sc.Insert(values.Length, val);

            if (sc.Count != values.Length + 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", sc.Count, values.Length + 1));
            }

            if (sc.IndexOf(val) != values.Length)
            {
                Assert.False(true, string.Format("Error, IndexOf returned {0} instead of {1}", sc.IndexOf(val), values.Length));
            }

            // check that all init items were moved
            for (int i = 0; i < values.Length; i++)
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

            sc.Insert(values.Length / 2, val);

            if (sc.Count != values.Length + 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of {1}", sc.Count, values.Length + 1));
            }

            if (sc.IndexOf(val) != values.Length / 2)
            {
                Assert.False(true, string.Format("Error, IndexOf returned {0} instead of {1}", sc.IndexOf(val), values.Length / 2));
            }

            // check that all init items were moved
            for (int i = 0; i < values.Length; i++)
            {
                int expected = i;
                if (i >= values.Length / 2)
                    expected = i + 1;
                if (sc.IndexOf(values[i]) != expected)
                {
                    Assert.False(true, string.Format("Error, IndexOf returned {1} instead of {2}", i, sc.IndexOf(values[i]), expected));
                }
            }

            //
            // [] Invalid parameter
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.Insert(-1, val); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.Insert(sc.Count + 1, val); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.Insert(sc.Count + 2, val); });
        }
    }
}
