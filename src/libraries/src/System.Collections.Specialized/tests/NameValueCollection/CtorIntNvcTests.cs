// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CtorIntNvcTests
    {
        [Fact]
        public void Test01()
        {
            NameValueCollection nvc;
            NameValueCollection nvc1;         // argument NameValueCollection

            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aa",
                "tExt",
                "     SPaces",
                "1",
                "$%^#",
                "2222222222222222222222222",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // names(keys) for simple string values
            string[] names =
            {
                "zero",
                "oNe",
                " ",
                "",
                "aA",
                "1",
                System.DateTime.Today.ToString(),
                "$%^#",
                Int32.MaxValue.ToString(),
                "     spaces",
                "2222222222222222222222222"
            };

            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            nvc1 = new NameValueCollection(10);

            //
            //  [] Create w/o capacity from empty w capacity
            //
            nvc = new NameValueCollection(nvc1);
            if (nvc == null)
            {
                Assert.False(true, "Error, collection is null");
            }

            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} ", nvc.Count));
            }

            // [] Create w capacity from empty w same capacity

            nvc = new NameValueCollection(10, nvc1);
            if (nvc == null)
            {
                Assert.False(true, "Error, collection is null");
            }

            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} ", nvc.Count));
            }

            //
            //
            //  [] Create w capacity from empty w greater capacity

            nvc = new NameValueCollection(5, nvc1);
            if (nvc == null)
            {
                Assert.False(true, "Error, collection is null");
            }

            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} ", nvc.Count));
            }

            // [] Create w capacity from empty w smaller capacity

            nvc = new NameValueCollection(50, nvc1);
            if (nvc == null)
            {
                Assert.False(true, "Error, collection is null");
            }

            if (nvc.Count != 0)
            {
                Assert.False(true, string.Format("Error, Count = {0} ", nvc.Count));
            }

            ///////////////////////////////////////////////////////////////
            //
            //  create from filled collection
            // [] Create from filled collection - smaller capacity
            //
            int len = values.Length;

            for (int i = 0; i < len; i++)
            {
                nvc1.Add(names[i], values[i]);
            }

            if (nvc1.Count != len)
            {
                Assert.False(true, string.Format("Error, Count = {0} after instead of {1}", nvc.Count, len));
            }

            nvc = new NameValueCollection(len / 2, nvc1);

            if (nvc.Count != nvc1.Count)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of {1}", nvc.Count, nvc1.Count));
            }


            string[] keys1 = nvc1.AllKeys;
            string[] keys = nvc.AllKeys;
            if (keys1.Length != keys.Length)
            {
                Assert.False(true, string.Format("Error, new collection  Keys.Length is {0} instead of {1}", keys.Length, keys1.Length));
            }
            else
            {
                for (int i = 0; i < keys1.Length; i++)
                {
                    if (Array.IndexOf(keys, keys1[i]) < 0)
                    {
                        Assert.False(true, string.Format("Error, no key \"{1}\" in AllKeys", i, keys1[i]));
                    }
                }
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string[] val = nvc.GetValues(keys[i]);
                if ((val.Length != 1) || String.Compare(val[0], (nvc1.GetValues(keys[i]))[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, unexpected value at key \"{1}\"", i, keys1[i]));
                }
            }

            //
            // [] Create from filled collection - count capacity
            //
            len = values.Length;

            nvc = new NameValueCollection(len, nvc1);

            if (nvc.Count != nvc1.Count)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of {1}", nvc.Count, nvc1.Count));
            }

            keys1 = nvc1.AllKeys;
            keys = nvc.AllKeys;
            if (keys1.Length != keys.Length)
            {
                Assert.False(true, string.Format("Error, new collection  Keys.Length is {0} instead of {1}", keys.Length, keys1.Length));
            }
            else
            {
                for (int i = 0; i < keys1.Length; i++)
                {
                    if (Array.IndexOf(keys, keys1[i]) < 0)
                    {
                        Assert.False(true, string.Format("Error, no key \"{1}\" in AllKeys", i, keys1[i]));
                    }
                }
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string[] val = nvc.GetValues(keys[i]);
                if ((val.Length != 1) || String.Compare(val[0], (nvc1.GetValues(keys[i]))[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, unexpected value at key \"{1}\"", i, keys1[i]));
                }
            }


            //
            //  [] Create from filled collection - greater capacity capacity
            //
            len = values.Length;

            nvc = new NameValueCollection(len * 2, nvc1);

            if (nvc.Count != nvc1.Count)
            {
                Assert.False(true, string.Format("Error, Count = {0} instead of {1}", nvc.Count, nvc1.Count));
            }


            keys1 = nvc1.AllKeys;
            keys = nvc.AllKeys;
            if (keys1.Length != keys.Length)
            {
                Assert.False(true, string.Format("Error, new collection  Keys.Length is {0} instead of {1}", keys.Length, keys1.Length));
            }
            else
            {
                for (int i = 0; i < keys1.Length; i++)
                {
                    if (Array.IndexOf(keys, keys1[i]) < 0)
                    {
                        Assert.False(true, string.Format("Error, no key \"{1}\" in AllKeys", i, keys1[i]));
                    }
                }
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string[] val = nvc.GetValues(keys[i]);
                if ((val.Length != 1) || String.Compare(val[0], (nvc1.GetValues(keys[i]))[0]) != 0)
                {
                    Assert.False(true, string.Format("Error, unexpected value at key \"{1}\"", i, keys1[i]));
                }
            }


            //
            //  [] change argument collection
            //
            string toChange = keys1[0];
            string init = nvc1[toChange];

            //
            // Change element
            //
            nvc1[toChange] = "new Value";
            if (String.Compare(nvc1[toChange], "new Value") != 0)
            {
                Assert.False(true, "Error, failed to change element");
            }
            if (String.Compare(nvc[toChange], init) != 0)
            {
                Assert.False(true, "Error, changed element in new collection");
            }


            //
            // Remove element
            //
            nvc1.Remove(toChange);

            if (nvc1.Count != len - 1)
            {
                Assert.False(true, "Error, failed to remove element");
            }

            if (nvc.Count != len)
            {
                Assert.False(true, "Error, collection changed after argument change - removed element");
            }
            keys = nvc.AllKeys;
            if (Array.IndexOf(keys, toChange) < 0)
            {
                Assert.False(true, "Error, collection changed after argument change - no key");
            }


            //
            //  [] invalid parameter - negative capacity
            //
            Assert.Throws<ArgumentOutOfRangeException>(() => { nvc = new NameValueCollection(-1, nvc1); });

            //
            // [] invalid parameter - null collection
            //
            Assert.Throws<ArgumentNullException>(() => { nvc = new NameValueCollection(10, (NameValueCollection)null); });
        }
    }
}
