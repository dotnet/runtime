// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoIsRightToLeft
    {
        [Theory]
        [InlineData("en-US", false)]
        [InlineData("ar", true)]
        public void IsRightToLeft(string name, bool expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.IsRightToLeft);
        }
    }
}
