// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoSortableDateTimePattern
    {
        // PosTest1: Call SortableDateTimePattern getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            VerificationHelper(new CultureInfo("en-us").DateTimeFormat, "yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            VerificationHelper(new CultureInfo("ja-JP").DateTimeFormat, "yyyy'-'MM'-'dd'T'HH':'mm':'ss");
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected)
        {
            string actual = info.SortableDateTimePattern;
            Assert.Equal(expected, actual);
        }
    }
}
