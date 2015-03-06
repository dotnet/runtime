// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class TrimToSizeTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestTrimToSizeBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            //

            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            String s1 = null;
            String s2 = null;
            String s3 = null;

            int i = 0;
            //
            // 	Constructor: Create SortedList using this as IComparer and default settings.
            //
            sl2 = new SortedList(this);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);
            Assert.Null(s3);

            //   Testcase: add few key-val pairs

            // start adding elements
            for (i = 0; i < 32; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg.Length = 0;
                sblMsg.Append("val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                sl2.Add(s1, s2);
            }

            //
            //  add one more elemnt now the capacity should be doubled
            //
            sl2.Add("key_32", "val_32");
            Assert.Equal(33, sl2.Count);

            //
            //   Testcase: now Remove few elements and TrimToSize
            //
            for (i = 0; i < 10; i++)
            {
                sl2.Remove("key_" + i.ToString()); // remove the current object
            }

            //   Testcase:  validate the Count and capacity
            Assert.Equal(23, sl2.Count);

            //  now TrimToSize
            sl2.TrimToSize();

            //  clear the list
            sl2.Clear();

            //  now TrimToSize
            sl2.TrimToSize();
        }
    }
}
