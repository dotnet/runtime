// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoCompareInfo
    {
        [Theory]
        [InlineData("es-ES", "llegar", "lugar", -1)]
        public void CompareInfo_Compare(string name, string string1, string string2, int expected)
        {
            CultureInfo culture = new CultureInfo(name);
            Assert.Equal(expected, Math.Sign(culture.CompareInfo.Compare(string1, string2)));
        }

        [Fact]
        [ActiveIssue(5463, PlatformID.AnyUnix)]
        public void CompareInfo_EsESTraditional()
        {
            // TOOD: Once #5463 is fixed, combine this into the InlineData for CompareInfo_Compare
            CompareInfo_Compare("es-ES_tradnl", "llegar", "lugar", 1);
        }

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
