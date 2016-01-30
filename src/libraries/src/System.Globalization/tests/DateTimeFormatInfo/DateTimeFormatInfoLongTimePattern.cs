// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongTimePattern
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Call LongTimePattern getter method should return correct value for InvariantInfo
        [Fact]
        public void PosTest1()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "HH:mm:ss", false);
        }

        // PosTest2: Call LongTimePattern setter method should return correct value
        [Fact]
        public void PosTest2()
        {
            VerificationHelper(new DateTimeFormatInfo(), "dddd, dd MMMM yyyy HH:mm:ss", true);
            VerificationHelper(new DateTimeFormatInfo(), "HH", true);
            VerificationHelper(new DateTimeFormatInfo(), "T", true);
            VerificationHelper(new DateTimeFormatInfo(), "HH:mm:ss dddd, dd MMMM yyyy", true);
            VerificationHelper(new DateTimeFormatInfo(), _generator.GetString(-55, false, 1, 256), true);
            VerificationHelper(new DateTimeFormatInfo(), "HH:mm:ss", true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().LongTimePattern = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void TestInvalid()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.LongTimePattern = "HH:mm:ss";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.LongTimePattern = expected;
            }

            string actual = info.LongTimePattern;
            Assert.Equal(expected, actual);
        }
    }
}
