// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
