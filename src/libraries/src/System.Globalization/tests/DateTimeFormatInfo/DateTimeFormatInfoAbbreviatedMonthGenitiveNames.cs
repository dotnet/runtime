// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedMonthGenitiveNames
    {
        // PosTest1: Call AbbreviatedMonthGenitiveNames getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    new string[] {
                    "Jan",
                    "Feb",
                    "Mar",
                    "Apr",
                    "May",
                    "Jun",
                    "Jul",
                    "Aug",
                    "Sep",
                    "Oct",
                    "Nov",
                    "Dec",
                    ""
                    },
                    false);
        }

        // PosTest2: Call AbbreviatedMonthGenitiveNames setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            VerificationHelper(new DateTimeFormatInfo(),
                    new string[] {
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "10",
                    "11",
                    "12",
                    ""
                    },
                    true);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().AbbreviatedMonthGenitiveNames = null;
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().AbbreviatedMonthGenitiveNames = new string[] {
                    "1",
                    "2",
                    "3",
                    null,
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "10",
                    "11",
                    "12",
                    ""
                    };
            });
        }

        // NegTest2: ArgumentException should be thrown when The property is being set to an array that is 
        // multidimensional or whose length is not exactly 7
        [Fact]
        public void TestInvalidArray()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new DateTimeFormatInfo().AbbreviatedMonthGenitiveNames = new string[] { "Jan" };
            });
        }

        // NegTest3: InvalidOperationException should be thrown when The property is being set and the 
        // DateTimeFormatInfo is read-only
        [Fact]
        public void NegTest3()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" };
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string[] expected, bool setter)
        {
            if (setter)
            {
                info.AbbreviatedMonthGenitiveNames = expected;
            }

            string[] actual = info.AbbreviatedMonthGenitiveNames;
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }
    }
}
