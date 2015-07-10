// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
