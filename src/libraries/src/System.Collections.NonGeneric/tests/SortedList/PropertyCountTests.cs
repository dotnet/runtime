// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class CountTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestGetCountBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            //

            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            string s1 = null;
            string s2 = null;

            int i = 0;

            //
            // 	Constructor: Create SortedList using this as IComparer and default settings.
            //
            sl2 = new SortedList(this);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);

            //   Testcase: Set - null val, should pass
            sl2["first key"] = (string)null;
            Assert.Equal(1, sl2.Count);

            //   Testcase: vanila Set
            sl2["first key"] = "first value";
            Assert.Equal(1, sl2.Count);

            sl2.Clear();
            Assert.Equal(0, sl2.Count);

            //   Testcase: add key-val pairs
            for (i = 0; i < 50; i++)
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

            Assert.Equal(50, sl2.Count);

            //
            //   Testcase:  now set 100 key-val pairs, this includes the first 50 pairs that
            //				   we just added, their values must be changed to the new values set
            //
            for (i = 0; i < 100; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg.Length = 0;
                sblMsg.Append("new_val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                sl2[s1] = s2;
            }

            Assert.Equal(100, sl2.Count);

            //  clear and check the Count == 0
            sl2.Clear();
            Assert.Equal(0, sl2.Count);
        }
    }
}
