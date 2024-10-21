// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoTwoLetterISOLanguageName
    {
        [Theory]
        [InlineData("de-DE", "de")]
        [InlineData("en", "en")]
        [InlineData("", "iv")]
        public void TwoLetterISOLanguageName(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TwoLetterISOLanguageName);
        }
    }
}
