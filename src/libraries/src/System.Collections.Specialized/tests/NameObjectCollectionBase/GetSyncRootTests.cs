// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetSyncRootNameObjectCollectionBaseTests
    {
        private String _strErr = "Error!";

        [Fact]
        public void Test01()
        {
            // [] Get SyncRoot, same instance returns same object
            MyNameObjectCollection noc1 = new MyNameObjectCollection();
            MyNameObjectCollection noc2;
            noc1.Add("key1", new Foo());
            noc1.Add("key2", new Foo());

            if (((ICollection)noc1).SyncRoot == null)
            {
                Assert.False(true, _strErr + "SyncRoot object is null");
            }

            noc2 = noc1;
            if (((ICollection)noc1).SyncRoot != ((ICollection)noc2).SyncRoot)
            {
                Assert.False(true, _strErr + "Different SyncRoot objects for same collection");
            }

            // [] Different instances return different SyncRoot objects
            noc1 = new MyNameObjectCollection();
            noc2 = new MyNameObjectCollection();

            if (((ICollection)noc1).SyncRoot == ((ICollection)noc2).SyncRoot)
            {
                Assert.False(true, _strErr + "SyncRoot for 2 different collections is the same");
            }

            // [] IsSynchronized returns false
            noc1 = new MyNameObjectCollection();

            if (((ICollection)noc1).IsSynchronized != false)
            {
                Assert.False(true, _strErr + "IsSynchronized should be false!");
            }

            // [] SyncRoot is of type Object
            noc1 = new MyNameObjectCollection();
            if (((ICollection)noc1).SyncRoot.GetType() != typeof(Object))
            {
                Assert.False(true, _strErr + "SyncRoot is not of type Object");
            }
        }
    }
}


