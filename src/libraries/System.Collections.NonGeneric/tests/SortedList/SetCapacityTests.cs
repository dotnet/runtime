// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Globalization;

namespace System.Collections.SortedListTests
{
    public class PropertyCapacityTests01
    {
        [Fact]
        public void TestSetCapacityBasic()
        {
            string strValue = string.Empty;

            SortedList list;
            Int32 iCurrentDefaultCapacity;
            Int32 capacity;

            iCurrentDefaultCapacity = 0;

            list = new SortedList();
            Assert.Equal(list.Capacity, iCurrentDefaultCapacity);

            list.Capacity = 3;
            Assert.Equal(3, list.Capacity);

            list = new SortedList(0);
            capacity = 0;
            list.Capacity = capacity;

            Assert.Equal(list.Capacity, capacity);

            capacity = 5000;
            list.Capacity = capacity;

            Assert.Equal(list.Capacity, capacity);

            //If the capacity is greater than zero, then Capcity cannot set it to zero
            list = new SortedList();
            capacity = 0;
            list.Capacity = capacity;

            Assert.Equal(list.Capacity, capacity);

            list = new SortedList(5000);
            capacity = 0;
            list.Capacity = capacity;

            Assert.Equal(list.Capacity, capacity);

            list = new SortedList(5000);
            capacity = 5;
            list.Capacity = capacity;

            Assert.Equal(list.Capacity, capacity);

            //If the SortedList.Count is greater than the value of the capacity, this will throw
            list = new SortedList();
            for (int i = 0; i < 5000; i++)
                list.Add(i, i);
            capacity = 0;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                         {
                             list.Capacity = capacity;
                         }
            );

            list = new SortedList(0);
            capacity = -1;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                         {
                             list.Capacity = capacity;
                         }
            );

            //Trying to set to Int32.MaxValue
            list = new SortedList(0);
            capacity = Int32.MaxValue;

            Assert.Throws<OutOfMemoryException>(() =>
                         {
                             list.Capacity = capacity;
                         }
            );
        }
    }
}
