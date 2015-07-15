// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoLongDatePattern
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Call LongDatePattern getter method should return correct value for InvariantInfo
        [Fact]
        public void PosTest1()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "dddd, dd MMMM yyyy", false);
        }

        // PosTest2: Call LongDatePattern setter method should return correct value
        [Fact]
        public void PosTest2()
        {
            VerificationHelper(new DateTimeFormatInfo(), "dddd, dd MMMM yyyy HH:mm:ss", true);
            VerificationHelper(new DateTimeFormatInfo(), "dddd", true);
            VerificationHelper(new DateTimeFormatInfo(), "D", true);
            VerificationHelper(new DateTimeFormatInfo(), "HH:mm:ss dddd, dd MMMM yyyy", true);
            VerificationHelper(new DateTimeFormatInfo(), _generator.GetString(-55, false, 1, 256), true);
            VerificationHelper(new DateTimeFormatInfo(), "dddd, dd MMMM yyyy", true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void NegTest1()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().LongDatePattern = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void NegTest2()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.LongDatePattern = "dddd, dd MMMM yyyy";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.LongDatePattern = expected;
            }

            string actual = info.LongDatePattern;
            Assert.Equal(expected, actual);
        }
    }
}
