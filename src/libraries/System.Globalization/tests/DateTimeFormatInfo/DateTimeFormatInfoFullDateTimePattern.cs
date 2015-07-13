// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFullDateTimePattern
    {
        // PosTest1: Call FullDateTimePattern getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "dddd, dd MMMM yyyy HH:mm:ss", false);
        }

        // PosTest2: Call FullDateTimePattern setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            VerificationHelper(new DateTimeFormatInfo(), "dddd, dd MMMM yyyy HH:mm:ss", true);
            VerificationHelper(new DateTimeFormatInfo(), "dddd", true);
            VerificationHelper(new DateTimeFormatInfo(), "F", true);
            VerificationHelper(new DateTimeFormatInfo(), "HH:mm:ss dddd, dd MMMM yyyy", true);
            VerificationHelper(new DateTimeFormatInfo(), TestLibrary.Generator.GetString(-55, false, 1, 256), true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().FullDateTimePattern = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void TestReadOnly()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.FullDateTimePattern = "dddd, dd MMMM yyyy HH:mm:ss";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.FullDateTimePattern = expected;
            }

            string actual = info.FullDateTimePattern;
            Assert.Equal(expected, actual);
        }
    }
}
