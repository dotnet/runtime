// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class GetStringComparerTests
    {
        private static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [Fact]
        public void GetStringComparer_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("compareInfo", () => ((CompareInfo)null).GetStringComparer(CompareOptions.None));

            AssertExtensions.Throws<ArgumentException>("options", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer((CompareOptions)0xFFFF));
            AssertExtensions.Throws<ArgumentException>("options", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer(CompareOptions.Ordinal | CompareOptions.IgnoreCase));
            AssertExtensions.Throws<ArgumentException>("options", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer(CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreCase));
        }

        [Theory]
        [InlineData("hello", "hello", "fr-FR", CompareOptions.IgnoreCase, 0, 0)]
        [InlineData("hello", "HELLo", "fr-FR", CompareOptions.IgnoreCase, 0, 0)]
        [InlineData("hello", null, "fr-FR", CompareOptions.IgnoreCase, 1, 1)]
        [InlineData(null, "hello", "fr-FR", CompareOptions.IgnoreCase, -1, -1)]
        [InlineData(null, null, "fr-FR", CompareOptions.IgnoreCase, 0, 0)]
        [InlineData("abc", "def", "fr-FR", CompareOptions.IgnoreCase, -1, -1)]
        [InlineData("abc", "ABC", "fr-FR", CompareOptions.IgnoreCase, 0, 0)]
        [InlineData("def", "ABC", "fr-FR", CompareOptions.IgnoreCase, 1, 1)]
        [InlineData("abc", "ABC", "en-US", CompareOptions.Ordinal, 1, 1)]
        [InlineData("abc", "ABC", "en-US", CompareOptions.OrdinalIgnoreCase, 0, 0)]
        [InlineData("Cot\u00E9", "cot\u00E9", "fr-FR", CompareOptions.IgnoreCase, 0, 0)]
        [InlineData("cot\u00E9", "c\u00F4te", "fr-FR", CompareOptions.None, 1, -1)]
        public static void Compare(string x, string y, string cultureName, CompareOptions options, int expectedWindows, int expectedICU)
        {
            int expected = s_isWindows ? expectedWindows : expectedICU;
            StringComparer comparer = new CultureInfo(cultureName).CompareInfo.GetStringComparer(options);

            Assert.Equal(expected, Math.Sign(comparer.Compare(x, y)));
            Assert.Equal((expected == 0), comparer.Equals(x, y));

            if (x != null && y != null)
            {
                Assert.Equal((expected == 0), comparer.GetHashCode(x).Equals(comparer.GetHashCode(y)));
            }
        }

        [Fact]
        public void GetHashCode_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer(CompareOptions.None).GetHashCode(null));
        }

        [Theory]
        [InlineData("en-US", CompareOptions.IgnoreCase, "en-US", CompareOptions.IgnoreCase, true)]
        [InlineData("en-US", CompareOptions.IgnoreCase, "en-US", CompareOptions.IgnoreSymbols, false)]
        [InlineData("en-US", CompareOptions.IgnoreCase, "fr-FR", CompareOptions.IgnoreCase, false)]
        [InlineData("en-US", CompareOptions.IgnoreCase, "fr-FR", CompareOptions.Ordinal, false)]
        public void Equals(string cultureName1, CompareOptions options1, string cultureName2, CompareOptions options2, bool expected)
        {
            StringComparer comparer1 = new CultureInfo(cultureName1).CompareInfo.GetStringComparer(options1);
            StringComparer comparer2 = new CultureInfo(cultureName2).CompareInfo.GetStringComparer(options2);

            Assert.Equal(expected, comparer1.Equals(comparer2));
            Assert.Equal(expected, comparer1.GetHashCode().Equals(comparer2.GetHashCode()));
        }
    }
}
