// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;


namespace System.Collections.SortedListTests
{
    public class GetKeyTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }


        [Fact]
        public void TestGetKeyBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);

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

            //    Testcase: GetKey - key at index 0 , ArgExc expected
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    sl2.GetKey(0);
                });

            //   Testcase: GetKey - null val, should pass
            sl2["first key"] = (string)null;
            Assert.Equal(1, sl2.Count);

            //   Testcase: vanila Set
            sl2.Clear();
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

            //
            //   now get their keys using GetKey
            //
            for (i = 0; i < 50; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();
                Assert.True(((string)sl2.GetKey(sl2.IndexOfKey(s1))).Equals(s1));
            }

            Assert.Equal(50, sl2.Count);

        }
    }
}
