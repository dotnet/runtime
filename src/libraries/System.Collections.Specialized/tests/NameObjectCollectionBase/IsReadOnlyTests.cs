// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class IsReadOnlyTests
    {
        private String _strErr = "Error!";

        [Fact]
        public void Test01()
        {
            MyNameObjectCollection noc = new MyNameObjectCollection();
            String key = "key1";
            Foo value = new Foo();


            // [] IsReadOnly is initially false
            if (noc.IsReadOnly != false)
            {
                Assert.False(true, _strErr + "IsReadOnly should be false initially");
            }

            // [] Set IsReadOnly to true
            noc.IsReadOnly = true;
            if (noc.IsReadOnly != true)
            {
                Assert.False(true, _strErr + "IsReadOnly should be true");
            }

            // Now we are going to verify that methods that change the collection throw an exception
            // if IsReadOnly is true.

            // [] Set IsReadOnly to false and add an element
            noc.IsReadOnly = false;
            if (noc.IsReadOnly != false)
            {
                Assert.False(true, _strErr + "IsReadOnly should be false");
            }

            noc.Add(key, value);
            noc.IsReadOnly = true;

            // [] Add fails
            Assert.Throws<NotSupportedException>(() => { noc.Add("new key", new Foo()); });

            // [] Remove fails
            Assert.Throws<NotSupportedException>(() => { noc.Remove(key); });

            // [] RemoveAt fails
            Assert.Throws<NotSupportedException>(() => { noc.RemoveAt(0); });

            // [] Clear fails
            Assert.Throws<NotSupportedException>(() => { noc.Clear(); });

            // [] Get by key succeeds
            if (noc[key] != value)
            {
                Assert.False(true, string.Format(_strErr + "Wrong value returned.  Expected {0}, got {1}", value, noc[key]));
            }

            // [] Set by key fails
            Assert.Throws<NotSupportedException>(() => { noc[key] = new Foo(); });

            // [] Get by index succeeds
            if (noc[0] != value)
            {
                Assert.False(true, string.Format(_strErr + "Wrong value returned.  Expected {0}, got {1}", value, noc[0]));
            }

            // [] Set by index fails
            Assert.Throws<NotSupportedException>(() => { noc[0] = new Foo(); });

            // [] GetKey succeeds
            if (noc.GetKey(0) != key)
            {
                Assert.False(true, string.Format(_strErr + "Wrong value returned.  Expected {0}, got {1}", key, noc.GetKey(0)));
            }
        }
    }
}


