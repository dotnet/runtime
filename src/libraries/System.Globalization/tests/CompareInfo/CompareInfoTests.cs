// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("en")]
        [InlineData("zh-Hans")]
        [InlineData("zh-Hant")]
        public void GetCompareInfo(string name)
        {
            CompareInfo compare = CompareInfo.GetCompareInfo(name);
            Assert.Equal(name, compare.Name);
        }

        [Fact]
        public void GetCompareInfo_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("name", () => CompareInfo.GetCompareInfo(null));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.CompareInfo, CultureInfo.InvariantCulture.CompareInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.CompareInfo, CompareInfo.GetCompareInfo(""), true };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, CompareInfo.GetCompareInfo("en-US"), true };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, CompareInfo.GetCompareInfo("fr-FR"), false };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, new object(), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(CompareInfo compare1, object value, bool expected)
        {
            Assert.Equal(expected, compare1.Equals(value));
            if (value is CompareInfo)
            {
                Assert.Equal(expected, compare1.GetHashCode().Equals(value.GetHashCode()));
            }
        }

        [Theory]
        [InlineData("abc", CompareOptions.OrdinalIgnoreCase, "ABC", CompareOptions.OrdinalIgnoreCase, true)]
        [InlineData("abc", CompareOptions.Ordinal, "ABC", CompareOptions.Ordinal, false)]
        [InlineData("abc", CompareOptions.Ordinal, "abc", CompareOptions.Ordinal, true)]
        [InlineData("abc", CompareOptions.None, "abc", CompareOptions.None, true)]
        public void GetHashCode(string source1, CompareOptions options1, string source2, CompareOptions options2, bool expected)
        {
            CompareInfo invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
            Assert.Equal(expected, invariantCompare.GetHashCode(source1, options1).Equals(invariantCompare.GetHashCode(source2, options2)));
        }

        [Fact]
        public void GetHashCode_EmptyString()
        {
            Assert.Equal(0, CultureInfo.InvariantCulture.CompareInfo.GetHashCode("", CompareOptions.None));
        }

        [Fact]
        public void GetHashCode_Invalid()
        {
            Assert.Throws<ArgumentNullException>("source", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode(null, CompareOptions.None));

            Assert.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", CompareOptions.StringSort));
            Assert.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", CompareOptions.Ordinal | CompareOptions.IgnoreSymbols));
            Assert.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", (CompareOptions)(-1)));
        }

        [Theory]
        [InlineData("", "CompareInfo - ")]
        [InlineData("en-US", "CompareInfo - en-US")]
        [InlineData("EN-US", "CompareInfo - en-US")]
        public void ToString(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).CompareInfo.ToString());
        }
    }
}
