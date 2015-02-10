// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class GetEnumeratorTests : IComparer
    {
        public virtual int Compare(object obj1, object obj2)  // ICompare satisfier.
        {
            int mrI4_1 = (Int32)obj1;
            int mrI4_2 = (Int32)obj2;

            if (mrI4_1 == mrI4_2)
                return 0;
            else
                return (mrI4_1 - mrI4_2);
        }

        [Fact]
        public void TestGetEnumeratorBasic()
        {
            StringBuilder sblMsg = new StringBuilder(99);
            //

            SortedList sl2 = null;

            IDictionaryEnumerator dicen = null;

            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);
            StringBuilder sblWork1 = new StringBuilder(99);

            int i3 = 0;
            int i2 = 0;

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

            //   Testcase: Set - null key, ArgExc expected
            Assert.Throws<ArgumentNullException>(() =>
                {
                    sl2[null] = 0;
                });

            Assert.Equal(0, sl2.Count);

            //   Testcase: Set - null val
            sl2[(object)100] = (object)null;
            Assert.Equal(1, sl2.Count);

            //   Testcase: vanila Set
            sl2[(object)100] = 1;
            Assert.Equal(1, sl2.Count);

            sl2.Clear();
            Assert.Equal(0, sl2.Count);

            //   Testcase: add key-val pairs
            for (i = 0; i < 100; i++)
            {
                sl2.Add(i + 100, i);
            }

            Assert.Equal(100, sl2.Count);

            for (i = 0; i < 100; i++)
            {
                j = i + 100;

                Assert.True(sl2.ContainsKey((int)j));
                Assert.True(sl2.ContainsValue(i));

                object o2 = sl2[(object)(j)]; //need to cast: Get (object key)
                Assert.NotNull(o2);
                i2 = (int)o2;

                i3 = (int)sl2.GetByIndex(i); //Get (index i)
                Assert.False((i3 != i) || (i2 != i));

                //  testcase: GetEnumerator
                dicen = (IDictionaryEnumerator)sl2.GetEnumerator();

                //  Boundary for Current
                Assert.Throws<InvalidOperationException>(() =>
                                 {
                                     object throwaway = dicen.Current;
                                 }
                );

                j = 0;
                //  go over the enumarator
                while (dicen.MoveNext())
                {
                    //  Current to see the order
                    i3 = (int)dicen.Value;
                    Assert.True(j.Equals(i3));

                    //  Current again to see the same order
                    i3 = (int)dicen.Value;
                    Assert.Equal(i3, j);

                    j++;
                }

                //  Boundary for Current
                Assert.Throws<InvalidOperationException>(() =>
                                 {
                                     object throwaway = dicen.Current;
                                 }
                );
                //  Boundary for MoveNext: call MoveNext to make sure it returns false
                Assert.False((dicen.MoveNext()) || (j != 100));

                //  call again MoveNext to make sure it still returns false
                Assert.False(dicen.MoveNext());
            }
        }
    }
}
