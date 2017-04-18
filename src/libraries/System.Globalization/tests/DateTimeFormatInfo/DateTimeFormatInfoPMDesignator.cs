// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoPMDesignator
    {
        [Fact]
        public void PMDesignator_InvariantInfo()
        {
            Assert.Equal("PM", DateTimeFormatInfo.InvariantInfo.PMDesignator);
        }

        [Theory]
        [InlineData("AA")]
        [InlineData("P.M.")]
        public void PMDesignator_Set(string newPMDesignator)
        {
            var format = new DateTimeFormatInfo();
            format.PMDesignator = newPMDesignator;
            Assert.Equal(newPMDesignator, format.PMDesignator);
        }

        [Fact]
        public void PMDesignator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().PMDesignator = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.PMDesignator = "AA"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
