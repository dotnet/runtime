// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoTests
    {
        [Fact]
        public void TestCompareInfo()
        {
            CompareInfo ciENG = CompareInfo.GetCompareInfo("en-US");
            CompareInfo ciFR = CompareInfo.GetCompareInfo("fr-FR");

            Assert.True(ciENG.Name.Equals("en-US", StringComparison.CurrentCultureIgnoreCase));
            Assert.NotEqual(ciENG.GetHashCode(), ciFR.GetHashCode());
            Assert.NotEqual(ciENG, ciFR);
        }

        [Theory]
        [InlineData("de-DE", "Ü", "UE", -1)]
        [InlineData("de-DE_phoneb", "Ü", "UE", 0)]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void TestLocaleAlternateSortOrder(string locale, string string1, string string2, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            CompareInfo ci = myTestCulture.CompareInfo;
            Assert.Equal(expected, ci.Compare(string1, string2));
        }
    }
}
