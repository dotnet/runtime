// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoString
    {
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("\uD800\uDC00")]
        public void String_Set(string value)
        {
            StringInfo stringInfo = new StringInfo();
            stringInfo.String = value;
            Assert.Same(value, stringInfo.String);
        }
        
        [Fact]
        public void String_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("String", () => new StringInfo().String = null);
        }
    }
}
