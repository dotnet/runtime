// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetSyncRootListDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            ListDictionary ld;
            ListDictionary ld1;

            object root;         // returned SyncRoot for ld
            object root1;         // returned SyncRoot for ld1

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

            // [] ListDictionary SyncRoot
            //-----------------------------------------------------------------

            int len = values.Length;
            ld = new ListDictionary();
            ld1 = new ListDictionary();


            //  [] for empty dictionary
            //
            root = ld.SyncRoot;
            root1 = ld1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different dictionaries are equal"));
            }

            root1 = ld.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one dictionary are not equal"));
            }

            //
            //  [] for filled dictionary
            //
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            for (int i = 0; i < len; i++)
            {
                ld1.Add(keys[i], values[i]);
            }
            root = ld.SyncRoot;
            root1 = ld1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different dictionaries are equal"));
            }

            root1 = ld.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one dictionary are not equal"));
            }


            //
            // Type should be Object
            //
            root = ld.SyncRoot;
            Type type = root.GetType();
            if (type != typeof(Object))
            {
                Assert.False(true, string.Format("Error, syncroot is not type Object"));
            }
        }
    }
}
