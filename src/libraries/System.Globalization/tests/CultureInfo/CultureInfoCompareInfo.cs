// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoCompareInfo
    {
        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        public void CompareInfo_Name(string name)
        {
            CultureInfo culture = new CultureInfo(name);
            Assert.Equal(name, culture.CompareInfo.Name);
        }
    }
}
