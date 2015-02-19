// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class CopyToStrAIntTests
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

            string[] destination;

            int cnt = 0;            // Count

            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringCollection is constructed as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] Copy empty collection into empty array
            //
            destination = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                destination[i] = "";
            }
            sc.CopyTo(destination, 0);
            if (destination.Length != values.Length)
            {
                Assert.False(true, string.Format("Error, altered array after copying empty collection"));
            }
            if (destination.Length == values.Length)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (String.Compare(destination[i], "") != 0)
                    {
                        Assert.False(true, string.Format("Error, item = \"{1}\" instead of \"{2}\" after copying empty collection", i, destination[i], ""));
                    }
                }
            }

            // [] Copy empty collection into non-empty array
            //
            destination = values;
            sc.CopyTo(destination, 0);
            if (destination.Length != values.Length)
            {
                Assert.False(true, string.Format("Error, altered array after copying empty collection"));
            }
            if (destination.Length == values.Length)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (String.Compare(destination[i], values[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, altered item {0} after copying empty collection", i));
                    }
                }
            }


            //
            // [] add simple strings and CopyTo([], 0)


            cnt = sc.Count;
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            destination = new string[values.Length];
            sc.CopyTo(destination, 0);

            for (int i = 0; i < values.Length; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(sc[i], destination[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i], sc[i]));
                }
            }

            // [] add simple strings and CopyTo([], middle_index)
            //

            sc.Clear();
            cnt = sc.Count;
            sc.AddRange(values);
            if (sc.Count != values.Length)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, values.Length));
            }

            destination = new string[values.Length * 2];
            sc.CopyTo(destination, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(sc[i], destination[i + values.Length]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i + values.Length], sc[i]));
                }
            }

            //
            // Intl strings
            // [] add intl strings and CopyTo([], 0)
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
            if (sc.Count != (intlValues.Length))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }

            destination = new string[intlValues.Length];
            sc.CopyTo(destination, 0);

            for (int i = 0; i < intlValues.Length; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(sc[i], destination[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i], sc[i]));
                }
            }

            //
            // Intl strings
            // [] add intl strings and CopyTo([], middle_index)
            //

            sc.Clear();
            sc.AddRange(intlValues);
            if (sc.Count != (intlValues.Length))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }

            destination = new string[intlValues.Length * 2];
            sc.CopyTo(destination, intlValues.Length);

            for (int i = 0; i < intlValues.Length; i++)
            {
                // verify that collection is copied correctly
                //
                if (String.Compare(sc[i], destination[i + intlValues.Length]) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, destination[i + intlValues.Length], sc[i]));
                }
            }

            //
            //  [] CopyTo(null, int)
            //
            sc.Clear();
            sc.AddRange(values);
            if (sc.Count != (intlValues.Length))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
            }

            destination = null;
            Assert.Throws<ArgumentNullException>(() => { sc.CopyTo(destination, 0); });

            //
            // [] CopyTo(string[], -1)
            //
            if (sc.Count != values.Length)
            {
                sc.Clear();
                sc.AddRange(values);
                if (sc.Count != (intlValues.Length))
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
                }
            }

            destination = new string[values.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() => { sc.CopyTo(destination, -1); });

            //
            //  [] CopyTo(string[], upperBound+1)
            //
            if (sc.Count != values.Length)
            {
                sc.Clear();
                sc.AddRange(values);
                if (sc.Count != (intlValues.Length))
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
                }
            }

            destination = new string[values.Length];
            Assert.Throws<ArgumentException>(() => { sc.CopyTo(destination, values.Length); });

            //
            // [] CopyTo(string[], upperBound+2)
            //
            if (sc.Count != values.Length)
            {
                sc.Clear();
                sc.AddRange(values);
                if (sc.Count != (intlValues.Length + 1))
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
                }
            }

            destination = new string[values.Length];
            Assert.Throws<ArgumentException>(() => { sc.CopyTo(destination, values.Length); });

            //
            //  [] CopyTo(string[], not_enough_space)
            //
            if (sc.Count != values.Length)
            {
                sc.Clear();
                sc.AddRange(values);
                if (sc.Count != (intlValues.Length))
                {
                    Assert.False(true, string.Format("Error, count is {0} instead of {1}", sc.Count, intlValues.Length));
                }
            }

            destination = new string[values.Length];
            Assert.Throws<ArgumentException>(() => { sc.CopyTo(destination, values.Length / 2); });
        }
    }
}
