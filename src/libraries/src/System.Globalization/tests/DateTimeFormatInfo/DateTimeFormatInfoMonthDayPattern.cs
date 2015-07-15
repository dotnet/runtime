// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthDayPattern
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Call MonthDayPattern getter method should return correct value for InvariantInfo
        [Fact]
        public void PosTest1()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "MMMM dd", false);
        }

        // PosTest2: Call MonthDayPattern setter method should return correct value
        [Fact]
        public void PosTest2()
        {
            VerificationHelper(new DateTimeFormatInfo(), "MMMM", true);
            VerificationHelper(new DateTimeFormatInfo(), "MMM dd", true);
            VerificationHelper(new DateTimeFormatInfo(), "M", true);
            VerificationHelper(new DateTimeFormatInfo(), "dd MMMM", true);
            VerificationHelper(new DateTimeFormatInfo(), _generator.GetString(-55, false, 1, 256), true);
            VerificationHelper(new DateTimeFormatInfo(), "MMMM dd", true);
            VerificationHelper(new DateTimeFormatInfo(), "m", true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().MonthDayPattern = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void TestInvalid()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.MonthDayPattern = "MMMM dd";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.MonthDayPattern = expected;
            }

            string actual = info.MonthDayPattern;
            Assert.Equal(expected, actual);
        }
    }
}
