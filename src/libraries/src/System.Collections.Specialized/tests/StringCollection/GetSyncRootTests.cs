// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetSyncRootStringCollectionTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringCollection sc;
            StringCollection sc1;

            object root;         // returned SyncRoot for sc
            object root1;         // returned SyncRoot for sc1

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

            // [] StringCollection SyncRoot
            //-----------------------------------------------------------------

            sc = new StringCollection();
            sc1 = new StringCollection();


            // [] for empty collection
            //
            root = sc.SyncRoot;
            root1 = sc1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different collections are equal"));
            }

            root1 = sc.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one collections are not equal"));
            }

            // [] for filled collection
            //
            sc.AddRange(values);
            sc1.AddRange(values);
            root = sc.SyncRoot;
            root1 = sc1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different collections are equal"));
            }

            root1 = sc.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one collections are not equal"));
            }


            //
            // Type should be Object
            //
            root = sc.SyncRoot;
            Type type = root.GetType();
            if (type != typeof(Object))
            {
                Assert.False(true, string.Format("Error, syncroot is not type Object"));
            }
        }
    }
}
