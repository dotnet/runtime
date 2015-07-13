// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoRFC1123Pattern
    {
        // PosTest1: Call RFC1123Pattern getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
            VerificationHelper(new CultureInfo("en-us").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
            VerificationHelper(new CultureInfo("ja-jp").DateTimeFormat, "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected)
        {
            string actual = info.RFC1123Pattern;
            Assert.Equal(expected, actual);
        }
    }
}
