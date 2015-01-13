// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// These are all wrapper methods, so they will not be tested extensively here.
// KeysCollection.CopyTo is tested separately.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class KeysCollectionTests
    {
        private String _strErr = "Error!";

        [Fact]
        public void Test01()
        {
            MyNameObjectCollection noc = new MyNameObjectCollection();
            NameObjectCollectionBase.KeysCollection keys;

            // Set up initial collection
            for (int i = 0; i < 20; i++)
            {
                noc.Add("key_" + i.ToString(), new Foo());
            }

            // Get the KeysCollection
            _strErr = "Err_001, ";
            keys = noc.Keys;

            // Check Count
            if (keys.Count != noc.Count)
            {
                Assert.False(true, string.Format(_strErr + "keys.Count is wrong.  expected {0}, got {1}", noc.Count, keys.Count));
            }

            // Compare - test Get, Item
            for (int i = 0; i < noc.Count; i++)
            {
                if (keys.Get(i) != "key_" + i.ToString())
                {
                    Assert.False(true, string.Format(_strErr + "keys.Get({0}) is wrong.  expected {1}, got {2}", i, "key_" + i.ToString(), keys.Get(i)));
                }
                if (keys[i] != "key_" + i.ToString())
                {
                    Assert.False(true, string.Format(_strErr + "keys[{0}] is wrong.  expected {1}, got {2}", i, "key_" + i.ToString(), keys[i]));
                }
            }

            // Get enumerator - it's the same enumerator as the original collection, so don't
            // need to test it again here.
            IEnumerator en = keys.GetEnumerator();

            // Get SyncRoot - just a cursory test
            if (((ICollection)keys).SyncRoot != ((ICollection)noc).SyncRoot)
            {
                Assert.False(true, _strErr + "keys.SyncRoot was not the same as noc.SyncRoot");
            }

            // Get IsSynchronized
            if (((ICollection)keys).IsSynchronized)
            {
                Assert.False(true, _strErr + "keys.SyncRoot was not the same as noc.SyncRoot");
            }

            // Check empty collection
            noc = new MyNameObjectCollection();
            keys = noc.Keys;

            // Check Count
            if (keys.Count != 0)
            {
                Assert.False(true, string.Format(_strErr + "keys.Count is wrong.  expected {0}, got {1}", 0, keys.Count));
            }
        }
    }
}


