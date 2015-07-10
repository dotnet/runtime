// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoUniversalSortableDateTimePattern
    {
        // PosTest1: Call UniversalSortableDateTimePattern getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "yyyy'-'MM'-'dd HH':'mm':'ss'Z'");
            VerificationHelper(new CultureInfo("en-us").DateTimeFormat, "yyyy'-'MM'-'dd HH':'mm':'ss'Z'");
            VerificationHelper(new CultureInfo("ja-jp").DateTimeFormat, "yyyy'-'MM'-'dd HH':'mm':'ss'Z'");
            VerificationHelper(new CultureInfo("fr-fr").DateTimeFormat, "yyyy'-'MM'-'dd HH':'mm':'ss'Z'");
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected)
        {
            string actual = info.UniversalSortableDateTimePattern;
            Assert.Equal(expected, actual);
        }
    }
}
