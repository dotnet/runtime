// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.SortedListTests
{
    public class IsFixedSizeTests
    {
        [Fact]
        public void TestGetIsFixedSizeBasic()
        {
            SortedList srt1;
            //vanila - IsFixedSize should return false and we should be able to add items to the HT
            srt1 = new SortedList();

            Assert.False(srt1.IsFixedSize);
            for (int i = 0; i < 100; i++)
                srt1.Add(i, i);

            Assert.Equal(100, srt1.Count);
            for (int i = 0; i < srt1.Count; i++)
            {
                Assert.True(srt1.ContainsKey(i));
                Assert.Equal((int)srt1[i], i);
            }
        }
    }
}
