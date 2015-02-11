// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class ContainsKeyTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestContainsKeyBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            String s1 = null;
            String s2 = null;

            int i = 0;
            //
            // 	Constructor: Create SortedList using this as IComparer and default settings.
            //
            sl2 = new SortedList(this);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);

            //   Testcase: No_Such_Key
            Assert.False(sl2.ContainsKey("No_Such_Key"));

            //   Testcase: add few key-val pairs
            for (i = 0; i < 100; i++)
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
            //   Testcase: test ContainsKey
            //
            for (i = 0; i < sl2.Count; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                Assert.True(sl2.ContainsKey(s1));
            }

            //
            // Remove a key and then check
            //
            sblMsg.Length = 0;
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            sl2.Remove(s1); //removes "Key_50"
            Assert.False(sl2.ContainsKey(s1));
        }
    }
}
