// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthGenitiveNames
    {
        // PosTest1: Call MonthGenitiveNames getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    new string[] {
                    "January",
                    "February",
                    "March",
                    "April",
                    "May",
                    "June",
                    "July",
                    "August",
                    "September",
                    "October",
                    "November",
                    "December",
                    ""
                    },
                    false);
        }

        // PosTest2: Call MonthGenitiveNames setter method should return correct value
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

        // PosTest3: Call MonthGenitiveNames getter method should return correct value for ru-ru culture
        [Fact]
        public void TestRuCulture()
        {
            DateTimeFormatInfo info = new CultureInfo("ru-RU").DateTimeFormat;

            string[] expected = new string[] {
                    "\u044F\u043D\u0432\u0430\u0440\u044F",
                    "\u0444\u0435\u0432\u0440\u0430\u043B\u044F",
                    "\u043C\u0430\u0440\u0442\u0430",
                    "\u0430\u043F\u0440\u0435\u043B\u044F",
                    "\u043C\u0430\u044F",
                    "\u0438\u044E\u043D\u044F",
                    "\u0438\u044E\u043B\u044F",
                    "\u0430\u0432\u0433\u0443\u0441\u0442\u0430",
                    "\u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044F",
                    "\u043E\u043A\u0442\u044F\u0431\u0440\u044F",
                    "\u043D\u043E\u044F\u0431\u0440\u044F",
                    "\u0434\u0435\u043A\u0430\u0431\u0440\u044F",
                    ""
                    };
            VerificationHelper(info, expected, false);
        }

        // NegTest1: ArgumentNullException should be thrown when The property is being set to a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().MonthGenitiveNames = null;
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().MonthGenitiveNames = new string[] {
                    "1",
                    "2",
                    "3",
                    null,
                    "5",
                    "6",
                    "7",
                    "8",
                    null,
                    "10",
                    "11",
                    "12",
                    ""
                    };
            });
        }

        // NegTest2: ArgumentException should be thrown when The property is being set to an array that is multidimensional or whose length is not exactly 7
        [Fact]
        public void TestInvalidArray()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new DateTimeFormatInfo().MonthGenitiveNames = new string[] { "sun" };
            });
        }

        // NegTest3: InvalidOperationException should be thrown when The property is being set and the DateTimeFormatInfo is read-only
        [Fact]
        public void TestReadOnly()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.MonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" };
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string[] expected, bool setter)
        {
            if (setter)
            {
                info.MonthGenitiveNames = expected;
            }

            string[] actual = info.MonthGenitiveNames;
            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }
    }
}
