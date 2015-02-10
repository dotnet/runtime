// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class CtorTestClass : IComparable
    {
        internal string str = null;

        public CtorTestClass() { }

        public CtorTestClass(string tstr)
        {
            str = tstr;
        }

        public virtual int CompareTo(object obj2)
        {
            return str.CompareTo(obj2.ToString());
        }

        public override bool Equals(object obj)
        {
            return str.Equals(obj);
        }

        public override int GetHashCode()
        {
            return str.GetHashCode();
        }

        public override string ToString()
        {
            return str.ToString();
        }

        /// <summary>
        /// named it as GetString() to distinguish it from ToString() even though the purpose is same
        /// </summary>
        /// <returns></returns>
        public virtual string GetString()
        {
            return str.ToString();
        }
    }

    public class CtorTests
    {
        [Fact]
        public void TestCtorDefault()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            //
            // 	Constructor: Create a default SortedList
            //
            sl2 = new SortedList();

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);

            //
            // 	Constructor: few more tests
            //
            sl2 = new SortedList();

            var k0 = new CtorTestClass("cde");
            var k1 = new CtorTestClass("abc");
            var k2 = new CtorTestClass("bcd");

            sl2.Add(k0, null);
            sl2.Add(k1, null);
            sl2.Add(k2, null);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList Count is right.
            Assert.Equal(3, sl2.Count);

            //  Verify that the SortedList actually sorted the hashtable.
            Assert.Equal(2, sl2.IndexOfKey(k0));

            Assert.Equal(0, sl2.IndexOfKey(k1));

            Assert.Equal(1, sl2.IndexOfKey(k2));

            //  Verify that the SortedList contains the right keys.

            Assert.True(((CtorTestClass)sl2.GetKey(0)).GetString().Equals("abc"));

            Assert.True(((CtorTestClass)sl2.GetKey(1)).GetString().Equals("bcd"));

            Assert.True(((CtorTestClass)sl2.GetKey(2)).GetString().Equals("cde"));
        }
    }
}
