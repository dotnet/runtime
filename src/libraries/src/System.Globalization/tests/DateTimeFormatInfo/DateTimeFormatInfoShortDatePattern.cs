// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortDatePattern
    {
        // PosTest1: Call ShortDatePattern getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo, "MM/dd/yyyy", false);
        }

        // PosTest2: Call ShortDatePattern setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            VerificationHelper(new DateTimeFormatInfo(), "MM/dd/yyyy", true);
            VerificationHelper(new DateTimeFormatInfo(), "MM-DD-yyyy", true);
            VerificationHelper(new DateTimeFormatInfo(), "d", true);
            VerificationHelper(new DateTimeFormatInfo(), TestLibrary.Generator.GetString(-55, false, 1, 256), true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().ShortDatePattern = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void TestReadOnly()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.ShortDatePattern = "MM/dd/yyyy";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.ShortDatePattern = expected;
            }

            string actual = info.ShortDatePattern;
            Assert.Equal(expected, actual);
        }
    }
}
