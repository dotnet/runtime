// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class AddStrTests
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

            int cnt = 0;            // Count
            int ind = 0;            // Index

            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringCollection is constructed as expected
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] Add() simple strings
            //
            for (int i = 0; i < values.Length; i++)
            {
                cnt = sc.Count;
                sc.Add(values[i]);
                if (sc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, sc.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (!sc.Contains(values[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain new item", i));
                }

                // verify that item was added at the end
                //
                ind = sc.IndexOf(values[i]);
                if (ind != sc.Count - 1)
                {
                    Assert.False(true, string.Format("Error, returned index {1} instead of {2}", i, ind, sc.Count - 1));
                }

                //  access the item
                //
                if (ind != -1)
                {
                    if (String.Compare(sc[ind], values[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, sc[ind], values[i]));
                    }
                }
            }

            //
            // Intl strings
            // [] Add() Intl strings
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

            for (int i = 0; i < intlValues.Length; i++)
            {
                cnt = sc.Count;

                sc.Add(intlValues[i]);
                if (sc.Count != cnt + 1)
                {
                    Assert.False(true, string.Format("Error, count is {1} instead of {2}", i, sc.Count, cnt + 1));
                }

                // verify that collection contains newly added item
                //
                if (!sc.Contains(intlValues[i]))
                {
                    Assert.False(true, string.Format("Error, collection doesn't contain new item", i));
                }

                // verify that item was added at the end
                //
                ind = sc.IndexOf(intlValues[i]);
                if (ind != sc.Count - 1)
                {
                    Assert.False(true, string.Format("Error, returned index {1} instead of {2}", i, ind, sc.Count - 1));
                }

                //  access the item
                //
                if (ind != -1)
                {
                    if (String.Compare(sc[ind], intlValues[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", i, sc[ind], intlValues[i]));
                    }
                }
            }

            //
            // add very long string
            // [] Add() very long string
            //
            cnt = sc.Count;
            string intlStr = intlValues[0];
            while (intlStr.Length < 10000)
                intlStr += intlStr;

            sc.Add(intlStr);
            if (sc.Count != cnt + 1)
            {
                Assert.False(true, string.Format("Error, count is {1} instead of {2}", sc.Count, cnt + 1));
            }

            // verify that collection contains newly added item
            //
            if (!sc.Contains(intlStr))
            {
                Assert.False(true, string.Format("Error, collection doesn't contain new item"));
            }

            // verify that item was added at the end
            //
            ind = sc.IndexOf(intlStr);
            if (ind != sc.Count - 1)
            {
                Assert.False(true, string.Format("Error, returned index {1} instead of {2}", ind, sc.Count - 1));
            }

            //  access the item
            //
            if (ind != -1)
            {
                if (String.Compare(sc[ind], intlStr) != 0)
                {
                    Assert.False(true, string.Format("Error, returned item \"{1}\" instead of \"{2}\"", sc[ind], intlStr));
                }
            }
        }
    }
}
