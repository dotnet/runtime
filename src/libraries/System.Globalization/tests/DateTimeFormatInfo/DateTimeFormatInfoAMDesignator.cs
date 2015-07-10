// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAMDesignator
    {
        // PosTest1: Call AMDesignator getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    "AM",
                    false);
        }

        // PosTest2: Call AMDesignator setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            VerificationHelper(new DateTimeFormatInfo(),
                    "AA",
                    true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().AMDesignator = null;
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and 
        // the DateTimeFormatInfo is read-only
        [Fact]
        public void TestReadOnly()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.AMDesignator = "1";
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string expected, bool setter)
        {
            if (setter)
            {
                info.AMDesignator = expected;
            }

            string actual = info.AMDesignator;
            Assert.Equal(expected, actual);
        }
    }
}
