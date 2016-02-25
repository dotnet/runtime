// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoCultureName
    {
        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("EN-us")]
        [InlineData("FR-fr")]
        public void CultureName(string name)
        {
            CultureInfo culture = new CultureInfo(name);
            Assert.Equal(culture.Name, culture.TextInfo.CultureName);
        }
    }
}
