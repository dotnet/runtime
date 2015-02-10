// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class IndexcAccessTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }

        [Fact]
        public void TestIndexcAccessBasic()
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

            //   Testcase: Set - null key, ArgExc expected
            Assert.Throws<ArgumentNullException>(() =>
                {
                    sl2[null] = "first value";
                });

            Assert.Equal(0, sl2.Count);

            //   Testcase: Set - null val, should pass
                sl2["first key"] = (string)null;
            Assert.Equal(1, sl2.Count);

            //   Testcase: vanila Set
            sl2[(int)0] = "first value";
            Assert.Equal(2, sl2.Count);

            //   Testcase: check to see whether the key is there
            Assert.True(sl2.ContainsKey("first key"));

            //   Testcase: Get and check the value
            sl2["first key"] = "first value";
            s2 = (string)sl2["first key"];
            Assert.True(s2.Equals("first value"));

            //   Testcase: Set again with a diff value
            sl2["first key"] = "second value";
            Assert.Equal(2, sl2.Count);

            //   Testcase: now, Get again and check the value set
            s2 = (string)sl2["first key"];
            Assert.True(s2.Equals("second value"));
            sl2.Clear();

            //   Testcase: add 50 key-val pairs
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
            //   Testcase:  now set their val again using Set (index, newVal)
            //
            for (i = 0; i < 50; i++)
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

            Assert.Equal(50, sl2.Count);

            //   Testcase:  check the values
            for (i = 0; i < 50; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_");
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg.Length = 0;
                sblMsg.Append("new_val_");
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                s3 = (string)sl2[s1];
                Assert.True(s3.Equals(s2));
            }
        }
    }
}
