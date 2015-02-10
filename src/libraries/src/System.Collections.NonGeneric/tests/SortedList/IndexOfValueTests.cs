// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;


namespace System.Collections.SortedListTests
{
    public class IndexOfValueTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            return string.Compare(obj1.ToString(), obj2.ToString());
        }


        [Fact]
        public void TestIndexOfValueBasic()
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
            string s4 = null;

            int i = 0;
            int j = 0;
            //
            // 	Constructor: Create SortedList using this as IComparer and default settings.
            //
            sl2 = new SortedList(this);

            //  Verify that the SortedList is not null.
            Assert.NotNull(sl2);

            //  Verify that the SortedList is empty.
            Assert.Equal(0, sl2.Count);

            //  with null - should return -1
            // null val
            j = sl2.IndexOfValue((string)null);
            Assert.Equal(-1, j);

            //  invalid val - should return -1
            j = sl2.IndexOfValue("No_Such_Val");
            Assert.Equal(-1, j);

            // null is a valid value
            sl2.Add("Key_0", null);
            j = sl2.IndexOfValue(null);
            Assert.NotEqual(-1, j);

            // first occurrence check
            sl2.Add("Key_1", "Val_Same");
            sl2.Add("Key_2", "Val_Same");

            j = sl2.IndexOfValue("Val_Same");
            Assert.Equal(1, j);

            sl2.Clear();

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
            //   Testcase: test IndexOfVal 
            //
            for (i = 0; i < sl2.Count; i++)
            {
                sblMsg.Length = 0;
                sblMsg.Append("key_"); //key
                sblMsg.Append(i);
                s1 = sblMsg.ToString();

                sblMsg.Length = 0;
                sblMsg.Append("val_"); //value
                sblMsg.Append(i);
                s2 = sblMsg.ToString();

                //  now use IndexOfKey and IndexOfVal to obtain the same object and check
                s3 = (string)sl2.GetByIndex(sl2.IndexOfKey(s1)); //Get the index of this key and then get object
                s4 = (string)sl2.GetByIndex(sl2.IndexOfValue(s2)); //Get the index of this val and then get object
                Assert.True(s3.Equals(s4));

                // now Get using the index obtained thru IndexOfKey () and compare it with s2
                s3 = (string)sl2.GetByIndex(sl2.IndexOfKey(s1));
                Assert.True(s3.Equals(s2));
            }

            //
            // Remove a key and then check
            //
            sblMsg.Length = 0;
            sblMsg.Append("key_50");
            s1 = sblMsg.ToString();

            sl2.Remove(s1); //removes "Key_50"
            j = sl2.IndexOfValue(s1);
            Assert.Equal(-1, j);
        }
    }
}
