// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoGetFormat
    {
        [Theory]
        [InlineData(typeof(NumberFormatInfo), typeof(NumberFormatInfo))]
        [InlineData(typeof(DateTimeFormatInfo), typeof(DateTimeFormatInfo))]
        [InlineData(typeof(string), null)]
        public void GetFormat(Type formatType, Type expectedFormatType)
        {
            object format = new CultureInfo("en-US").GetFormat(formatType);
            if (expectedFormatType == null)
            {
                Assert.Null(format);
            }
            else
            {
                Assert.Equal(expectedFormatType, format.GetType());
            }
        }
    }
}
