// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetEnumeratorNameObjectCollectionBaseTests
    {
        private String _strErr = "Error!";

        [Fact]
        public void Test01()
        {
            MyNameObjectCollection noc = new MyNameObjectCollection();
            IEnumerator en = null;
            bool res;


            // [] Enumerator for empty collection
            // Get enumerator
            en = noc.GetEnumerator();

            // MoveNext should return false
            res = en.MoveNext();
            if (res)
            {
                Assert.False(true, _strErr + "MoveNext returned true");
            }

            //  Attempt to get Current should result in exception
            Assert.Throws<InvalidOperationException>(() => { String curr = (String)en.Current; });

            // [] Enumerator for non-empty collection
            // Add items
            for (int i = 0; i < 10; i++)
            {
                noc.Add("key_" + i.ToString(), new Foo());
            }

            // Get enumerator
            en = noc.GetEnumerator();

            //  Attempt to get Current should result in exception
            Assert.Throws<InvalidOperationException>(() => { String curr = (String)en.Current; });

            // Iterate over collection
            for (int i = 0; i < noc.Count; i++)
            {
                // MoveNext should return true
                res = en.MoveNext();
                if (!res)
                {
                    Assert.False(true, string.Format(_strErr + "#{0}, MoveNext returned false", i));
                }

                // Check current
                String curr = (String)en.Current;
                if (noc[curr] == null)
                {
                    Assert.False(true, string.Format(_strErr + "#{0}, Current={1}, key not found in collection", i, curr));
                }

                // Check current again
                String current1 = (String)en.Current;
                if (current1 != curr)
                {
                    Assert.False(true, string.Format(_strErr + "#{0}, Value of Current changed!  Was {1}, now {2}", i, curr, current1));
                }
            }

            // next MoveNext should bring us outside of the collection, return false
            res = en.MoveNext();
            if (res)
            {
                Assert.False(true, _strErr + "MoveNext returned true");
            }

            // Attempt to get Current should result in exception
            Assert.Throws<InvalidOperationException>(() => { String curr = (String)en.Current; });

            // Reset
            en.Reset();

            // Attempt to get Current should result in exception
            Assert.Throws<InvalidOperationException>(() => { String curr = (String)en.Current; });

            // Modify collection and then then try MoveNext, Current, Reset
            // new collection
            noc = new MyNameObjectCollection();
            noc.Add("key1", new Foo());
            noc.Add("key2", new Foo());
            noc.Add("key3", new Foo());
            en = noc.GetEnumerator();

            // MoveNext
            if (!en.MoveNext())
            {
                Assert.False(true, _strErr + "MoveNext returned false");
            }

            // Current
            String current = (String)en.Current;

            // Modify collection
            noc.RemoveAt(0);
            if (noc.Count != 2)
            {
                Assert.False(true, string.Format(_strErr + "Collection Count wrong.  Expected {0}, got {1}", 2, noc.Count));
            }

            //  Current should not throw, but no guarantee is made on the return value
            string curr1 = (String)en.Current;

            //  MoveNext should throw exception
            Assert.Throws<InvalidOperationException>(() => { en.MoveNext(); });

            //  Reset should throw exception
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });

            //  Current should not throw, but no guarantee is made on the return value
            curr1 = (String)en.Current;

            //  MoveNext should still throw exception if collection is ReadOnly
            noc.IsReadOnly = true;
            Assert.Throws<InvalidOperationException>(() => { en.MoveNext(); });

            // Clear collection and then then try MoveNext, Current, Reset
            // new collection
            noc = new MyNameObjectCollection();
            noc.Add("key1", new Foo());
            noc.Add("key2", new Foo());
            noc.Add("key3", new Foo());
            en = noc.GetEnumerator();

            // MoveNext
            if (!en.MoveNext())
            {
                Assert.False(true, _strErr + "MoveNext returned false");
            }

            // Current
            current = (String)en.Current;

            // Modify collection
            noc.Clear();
            if (noc.Count != 0)
            {
                Assert.False(true, string.Format(_strErr + "Collection Count wrong.  Expected {0}, got {1}", 2, noc.Count));
            }

            //  Current throws.  Should it throw here?!
            Assert.Throws<InvalidOperationException>(() => { String curr = (String)en.Current; });

            //  MoveNext should throw exception
            Assert.Throws<InvalidOperationException>(() => { en.MoveNext(); });

            //  Reset should throw exception
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });
        }
    }
}


