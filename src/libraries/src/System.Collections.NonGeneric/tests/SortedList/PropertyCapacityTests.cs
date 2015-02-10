// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class CapacityTests
    {
        [Fact]
        public void TestGetCapacityBasic()
        {
            String strValue = String.Empty;

            SortedList list;
            Int32 iCurrentDefaultCapacity;
            Int32 capacity;

            iCurrentDefaultCapacity = 0;
            list = new SortedList();
            Assert.Equal(list.Capacity, iCurrentDefaultCapacity);

            list.Capacity = 3;
            Assert.Equal(3, list.Capacity);

            capacity = 0;
            list = new SortedList(capacity);
            Assert.Equal(list.Capacity, capacity);

            capacity = 5000;
            list = new SortedList(capacity);
            Assert.Equal(list.Capacity, capacity);
        }
    }
}

