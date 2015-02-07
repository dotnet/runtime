// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class DerivedClassTest
    {
        [Fact]
        public void TestEnumerator()
        {
            ArrayList al;
            IEnumerator enumerator;
            //[] Make sure Enumerator works
            al = new MySimpleArrayList();
            al.Add(5);

            enumerator = al.GetEnumerator();
            enumerator.MoveNext();
            Assert.Equal(5, (int)enumerator.Current);
        }
    }

    public class MySimpleArrayList : ArrayList
    {
        public MySimpleArrayList() : base() { }

        private Object _val;
        private bool _filled;

        public override int Add(Object o)
        {
            if (_filled)
            {
                throw new Exception();
            }

            _filled = true;
            _val = o;

            return 1;
        }

        public override int Count
        {
            get
            {
                return _filled ? 1 : 0;
            }
        }

        public override Object this[int x]
        {
            get
            {
                if (x != 0 || !_filled) throw new Exception();
                return _val;
            }
        }
    }
}
