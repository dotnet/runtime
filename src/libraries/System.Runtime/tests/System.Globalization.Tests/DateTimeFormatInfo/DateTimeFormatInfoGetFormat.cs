// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetFormat
    {
        [Fact]
        public void GetFormat_Invoke_ReturnsExpected()
        {
            DateTimeFormatInfo expected = new DateTimeFormatInfo();
            DateTimeFormatInfo format = (DateTimeFormatInfo)expected.GetFormat(typeof(DateTimeFormatInfo));
            Assert.Same(expected, format);
        }

        [Fact]
        public void GetFormat_UnsupportedFormatType_ReturnsNull()
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            Assert.Null(info.GetFormat(typeof(object)));
        }
    }
}
