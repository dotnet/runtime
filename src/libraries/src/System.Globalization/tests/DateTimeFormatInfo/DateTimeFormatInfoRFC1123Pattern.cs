// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
