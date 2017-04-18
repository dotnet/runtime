// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAMDesignator
    {
        [Fact]
        public void AMDesignator_InvariantInfo()
        {
            Assert.Equal("AM", DateTimeFormatInfo.InvariantInfo.AMDesignator);
        }

        [Fact]
        public void AMDesignator_Set()
        {
            string newAMDesignator = "AA";
            var format = new DateTimeFormatInfo();
            format.AMDesignator = newAMDesignator;
            Assert.Equal(newAMDesignator, format.AMDesignator);
        }

        [Fact]
        public void AMDesignator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().AMDesignator = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AMDesignator = "AA"); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
