// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoGetHashCode
    {
        [Theory]
        [InlineData("en-US")]
        [InlineData("en")]
        [InlineData("")]
        public void GetHashCodeTest(string name)
        {
            // The only guarantee that can be made about HashCodes is that they will be the same across calls
            CultureInfo culture = new CultureInfo(name);
            Assert.Equal(culture.GetHashCode(), culture.GetHashCode());
        }
    }
}
