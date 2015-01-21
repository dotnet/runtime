// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class RemoveAtTests
    {
        private String _strErr = "Error!";

        [Fact]
        public void Test01()
        {
            MyNameObjectCollection noc = new MyNameObjectCollection();
            ArrayList keys = new ArrayList();
            ArrayList values = new ArrayList();


            // [] Set up collection, including some null keys and values
            for (int i = 0; i < 10; i++)
            {
                String k = "key_" + i.ToString();
                Foo v = new Foo();
                noc.Add(k, v);
                keys.Add(k);
                values.Add(v);
            }
            Foo val = new Foo();
            noc.Add(null, val);
            keys.Add(null);
            values.Add(val);
            noc.Add(null, null);
            keys.Add(null);
            values.Add(null);
            noc.Add("repeatedkey", null);
            keys.Add("repeatedkey");
            values.Add(null);
            noc.Add("repeatedkey", val);
            keys.Add("repeatedkey");
            values.Add(val);
            CheckCollection(noc, keys, values);

            // [] Remove from middle of collection
            noc.RemoveAt(3);
            keys.RemoveAt(3);
            values.RemoveAt(3);
            CheckCollection(noc, keys, values);

            // [] Remove first element
            noc.RemoveAt(0);
            keys.RemoveAt(0);
            values.RemoveAt(0);
            CheckCollection(noc, keys, values);

            // [] Remove last element
            noc.RemoveAt(noc.Count - 1);
            keys.RemoveAt(keys.Count - 1);
            values.RemoveAt(values.Count - 1);
            CheckCollection(noc, keys, values);

            // [] Index < 0
            Assert.Throws<ArgumentOutOfRangeException>(() => { noc.RemoveAt(-1); });

            // [] Index = Count
            Assert.Throws<ArgumentOutOfRangeException>(() => { noc.RemoveAt(noc.Count); });

            // [] Remove all elements
            int n = noc.Count;
            for (int i = 0; i < n; i++)
            {
                noc.RemoveAt(0);
                keys.RemoveAt(0);
                values.RemoveAt(0);
                CheckCollection(noc, keys, values);
            }

            // [] RemoveAt on an empty collection
            Assert.Throws<ArgumentOutOfRangeException>(() => { noc.RemoveAt(0); });

            // [] RemoveAt on a ReadOnly collection
            noc.Add("key", new Foo());
            noc.IsReadOnly = true;
            Assert.Throws<NotSupportedException>(() => { noc.RemoveAt(0); });
        }

        void CheckCollection(MyNameObjectCollection noc, ArrayList keys, ArrayList values)
        {
            // Check counts
            if ((noc.Count != keys.Count) || (noc.Count != values.Count))
            {
                Assert.False(true, _strErr + "Wrong number of elements, or mismatched keys and values.");
                Assert.False(true, string.Format(_strErr + "noc.Count={0}, keys.Count={1}, values.Count={2}", noc.Count, keys.Count, values.Count));
            }

            // Check keys/values
            for (int i = 0; i < noc.Count; i++)
            {
                // keys
                if (noc.GetKey(i) != (String)keys[i])
                {
                    Assert.False(true, string.Format(_strErr + "key #{0} not as expected.  noc.GetKey({0}) = {1}, should be {2}", i, noc.GetKey(i), keys[i]));
                }
                // values
                if (values[i] == null)
                {
                    if (noc[i] != null)
                    {
                        Assert.False(true, string.Format(_strErr + "value #{0} not as expected.  noc[{0}] = {1}, should be {2}", i, noc[i], values[i]));
                    }
                }
                else
                {
                    if ((noc[i] == null) || ((Foo)noc[i] != (Foo)values[i]))
                    {
                        Assert.False(true, string.Format(_strErr + "value #{0} not as expected.  noc[{0}] = {1}, should be {2}", i, noc[i], values[i]));
                    }
                }
            }
        }
    }
}


