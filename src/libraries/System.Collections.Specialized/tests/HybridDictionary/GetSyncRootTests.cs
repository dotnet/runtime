// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetSyncRootTests
    {
        public const int MAX_LEN = 50;          // max length of random strings


        [Fact]
        public void Test01()
        {
            HybridDictionary hd;
            HybridDictionary hd1;

            object root;         // returned SyncRoot for hd
            object root1;         // returned SyncRoot for hd1

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

            // [] HybridDictionary SyncRoot
            //-----------------------------------------------------------------

            int len = values.Length;
            hd = new HybridDictionary();
            hd1 = new HybridDictionary();


            // [] for empty dictionary
            //
            root = hd.SyncRoot;
            root1 = hd1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different dictionaries are equal"));
            }

            root1 = hd.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one dictionary are not equal"));
            }

            // [] for filled dictionary
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keys[i], values[i]);
            }
            for (int i = 0; i < len; i++)
            {
                hd1.Add(keys[i], values[i]);
            }
            root = hd.SyncRoot;
            root1 = hd1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different dictionaries are equal"));
            }

            root1 = hd.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one dictionary are not equal"));
            }
        }
    }
}
