// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAMDesignator
    {
        [Fact]
        public void AMDesignator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal("AM", DateTimeFormatInfo.InvariantInfo.AMDesignator);
        }

        [Theory]
        [InlineData("")]
        [InlineData("AA")]
        [InlineData("A.M")]
        public void AMDesignator_Set_GetReturnsExpected(string value)
        {
            var format = new DateTimeFormatInfo();
            format.AMDesignator = value;
            Assert.Equal(value, format.AMDesignator);
        }

        [Fact]
        public void AMDesignator_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AMDesignator = null);
        }
        
        [Fact]
        public void AMDesignator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AMDesignator = "AA");
        }
    }
}
