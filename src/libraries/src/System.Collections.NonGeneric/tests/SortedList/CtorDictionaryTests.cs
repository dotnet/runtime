// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class SortedListCtorTestClass : IComparable
    {
        internal string str = null; //key value

        public SortedListCtorTestClass() { }

        public SortedListCtorTestClass(string tstr)
        {
            str = tstr;
        }

        public virtual int CompareTo(Object obj)
        {
            return str.CompareTo(obj.ToString());
        }

        public override bool Equals(Object obj)
        {
            return str.Equals(obj.ToString());
        }

        public override int GetHashCode()
        {
            return str.GetHashCode();
        }

        public override string ToString()
        {
            return str.ToString();
        }
    }

    public class CtorDictionaryTests
    {
        [Fact]
        public void Test01()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            SortedList sl2 = null;
            Hashtable ht = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            //
            // Construct a hashtable with 3 elements in an unsorted order
            //
            ht = new Hashtable();
            var k0 = new SortedListCtorTestClass("cde");
            var k1 = new SortedListCtorTestClass("abc");
            var k2 = new SortedListCtorTestClass("bcd");

            ht.Add(k0, null);
            ht.Add(k1, null);
            ht.Add(k2, null);

            //
            // Constructor: Create a SortedList using the hashtable (dictionary) created
            //
            sl2 = new SortedList(ht);

            // Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            // Verify that the SortedList Count is right.
            Assert.Equal(3, sl2.Count);

            // Verify that the SortedList actually sorted the hashtable.
            Assert.Equal(2, sl2.IndexOfKey(k0));

            Assert.Equal(0, sl2.IndexOfKey(k1));

            Assert.Equal(1, sl2.IndexOfKey(k2));

            // Verify that the SortedList contains the right keys.
            Assert.True(((SortedListCtorTestClass)sl2.GetKey(0)).ToString().Equals("abc"));

            Assert.True(((SortedListCtorTestClass)sl2.GetKey(1)).ToString().Equals("bcd"));

            Assert.True(((SortedListCtorTestClass)sl2.GetKey(2)).ToString().Equals("cde"));

            ht = new Hashtable();
            sl2 = new SortedList(ht);
            Assert.Equal(0, sl2.Count);
        }
    }
}
