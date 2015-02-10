// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class RemoveTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestRemoveBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            //

            SortedList sl2 = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            string s1 = null;
            string s2 = null;
            string s3 = null;

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

            //  Verify that the SortedList is empty.
            Assert.Equal(100, sl2.Count);

            //
            //   Testcase: test Remove (Object)
            //
            for (i = 0; i < 100; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sl2.Remove(s1); // remove the current object
                Assert.Equal(sl2.Count, (100 - (i + 1)));
            }

            //
            // Boundary - Remove a invalid key: No_Such_Key
            //
            sblMsg.Length = 0;
            sblMsg.Append("No_Such_Key");
            s1 = sblMsg.ToString();
            sl2.Remove(s1);

            //
            // Boundary - null key - pk
            //
            Assert.Throws<ArgumentNullException>(() => { sl2.Remove((string)null); });
        }
    }
}
