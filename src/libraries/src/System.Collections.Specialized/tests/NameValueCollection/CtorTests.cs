// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorNameValueCollectionTests
    {
        [Fact]
        public void Test01()
        {
            NameValueCollection nvc;

            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc = new NameValueCollection();

            //  [] comapre to null
            //
            if (nvc == null)
            {
                Assert.False(true, "Error, collection is null after default ctor");
            }

            // [] check Count
            //
            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} after default ctor", nvc.Count));
            }

            if (nvc.Get("key") != null)
            {
                Assert.False(true, "Error, Get(some_key) returned non-null after default ctor");
            }

            // [] check AllKeys
            //
            string[] keys = nvc.AllKeys;
            if (keys.Length != 0)
            {
                Assert.False(true, string.Format("Error, AllKeys contains {0} keys after default ctor", keys.Length));
            }

            //
            // Item(some_key) should return null
            //
            if (nvc["key"] != null)
            {
                Assert.False(true, "Error, Item(some_key) returned non-null after default ctor");
            }


            //
            // [] Add(string, string) 
            //
            nvc.Add("Name", "Value");
            if (nvc.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 1", nvc.Count));
            }
            if (String.Compare(nvc["Name"], "Value") != 0)
            {
                Assert.False(true, "Error, Item() returned unexpected value");
            }

            //
            // [] Clear() 
            //
            nvc.Clear();
            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count returned {0} instead of 0 after Clear()", nvc.Count));
            }
            if (nvc["Name"] != null)
            {
                Assert.False(true, "Error, Item() returned non-null value after Clear()");
            }
        }
    }
}
