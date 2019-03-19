// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoPMDesignator
    {
        [Fact]
        public void PMDesignator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("PM", DateTimeFormatInfo.InvariantInfo.PMDesignator);
        }

        [Theory]
        [InlineData("")]
        [InlineData("PP")]
        [InlineData("P.M")]
        public void PMDesignator_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.PMDesignator = value;
            Assert.Equal(value, format.PMDesignator);
        }

        [Fact]
        public void PMDesignator_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.PMDesignator = null);
        }
        
        [Fact]
        public void PMDesignator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.PMDesignator = "PP");
        }
    }
}
