// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class CtorIComparerIntTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestCtorIComparerIntBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);

            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            int nCapacity = 100;
            //
            // 	Constructor: Create SortedList using a capacity value.
            sl2 = new SortedList(this, nCapacity);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            // Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);

            //
            // 	Constructor: Create SortedList with zero capacity value - valid.
            //

            sl2 = new SortedList(this, 0);
            //
            // 	Constructor: Create SortedList using a invalid capacity value.
            //
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                             {
                                 sl2 = new SortedList(this, -1);
                             }
            );
        }
    }
}
