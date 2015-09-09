// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoYearMonthPattern
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Call YearMonthPattern getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "yyyy MMMM", false);
            VerificationHelper(new CultureInfo("fr-fr").DateTimeFormat, "MMMM yyyy", false);
        }

        // PosTest2: Call YearMonthPattern setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            VerificationHelper(new DateTimeFormatInfo(), "yyyy MMMM", true);
            VerificationHelper(new DateTimeFormatInfo(), "y", true);
            VerificationHelper(new DateTimeFormatInfo(), "Y", true);
            VerificationHelper(new DateTimeFormatInfo(), _generator.GetString(-55, false, 1, 256), true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().YearMonthPattern = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void TestReadOnly()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.YearMonthPattern = "yyyy MMMM";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.YearMonthPattern = expected;
            }

            string actual = info.YearMonthPattern;
            Assert.Equal(expected, actual);
        }
    }
}
