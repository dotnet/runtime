// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetSyncRootStringDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringDictionary sd;
            StringDictionary sd1;
            object root;         // returned SyncRoot for sd
            object root1;         // returned SyncRoot for sd1
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

            // [] StringDictionary SyncRoot
            //-----------------------------------------------------------------

            int len = values.Length;
            sd = new StringDictionary();
            sd1 = new StringDictionary();


            // [] on empty dictionary
            //
            root = sd.SyncRoot;
            root1 = sd1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different dictionaries are equal"));
            }

            root1 = sd.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one dictionary are not equal"));
            }

            // [] on filled dictionary
            //
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            for (int i = 0; i < len; i++)
            {
                sd1.Add(keys[i], values[i]);
            }
            root = sd.SyncRoot;
            root1 = sd1.SyncRoot;
            if (root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots of two different dictionaries are equal"));
            }

            root1 = sd.SyncRoot;
            if (!root.Equals(root1))
            {
                Assert.False(true, string.Format("Error, roots for one dictionary are not equal"));
            }


            //
            // Type should be Object
            //
            root = sd.SyncRoot;
            Type type = root.GetType();
            if (type != typeof(Object))
            {
                Assert.False(true, string.Format("Error, syncroot is not type Object"));
            }

            root = sd.SyncRoot;
            Assert.Throws<InvalidCastException>(() => { Hashtable ht = (Hashtable)root; });
        }
    }
}
