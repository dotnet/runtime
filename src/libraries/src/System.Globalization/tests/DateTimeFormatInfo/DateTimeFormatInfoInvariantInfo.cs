// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoInvariantInfo
    {
        // PosTest1: InvariantInfo should return a read-only DateTimeFormatInfo
        [Fact]
        public void TestReadOnly()
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.InvariantInfo;
            Assert.True(info.IsReadOnly);
        }
    }
}
