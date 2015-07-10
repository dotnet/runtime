// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoDateTimeFormat
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            DateTimeFormatInfo myDateTimeFormat = new DateTimeFormatInfo();
            myDateTimeFormat.AMDesignator = "a.m.";
            myDateTimeFormat.MonthDayPattern = "MMMM-dd";
            myDateTimeFormat.ShortTimePattern = "HH|mm";
            myCultureInfo.DateTimeFormat = myDateTimeFormat;
            Assert.Equal("a.m.", myCultureInfo.DateTimeFormat.AMDesignator);
            Assert.Equal("MMMM-dd", myCultureInfo.DateTimeFormat.MonthDayPattern);
            Assert.Equal("HH|mm", myCultureInfo.DateTimeFormat.ShortTimePattern);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultureInfo = new CultureInfo("fr");
            myCultureInfo.DateTimeFormat.AMDesignator = "a.m.";
            Assert.Equal("a.m.", myCultureInfo.DateTimeFormat.AMDesignator);

            myCultureInfo.DateTimeFormat.MonthDayPattern = "MMMM-dd";
            Assert.Equal("MMMM-dd", myCultureInfo.DateTimeFormat.MonthDayPattern);

            myCultureInfo.DateTimeFormat.ShortTimePattern = "HH|mm";
            Assert.Equal("HH|mm", myCultureInfo.DateTimeFormat.ShortTimePattern);
        }

        [Fact]
        public void NegTest1()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.Throws<ArgumentNullException>(() =>
            {
                DateTimeFormatInfo myDateTimeFormat = null;
                myCultureInfo.DateTimeFormat = myDateTimeFormat;
            });
        }

        [Fact]
        public void NegTest2()
        {
            CultureInfo myCultureInfo = CultureInfo.InvariantCulture; // InvariantCulture is a Read-Only culture
            Assert.True(myCultureInfo.IsReadOnly);
            DateTimeFormatInfo myDateTimeFormat = new DateTimeFormatInfo();
            myDateTimeFormat.AMDesignator = "a.m.";
            Assert.Throws<InvalidOperationException>(() =>
            {
                myCultureInfo.DateTimeFormat = myDateTimeFormat;
            });
        }
    }
}