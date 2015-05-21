// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Globalization.Extensions.Tests
{
    public class GlobalizationExtensionsTests
    {
        [Fact]
        public static void CompareInfoThrows()
        {
            Assert.Throws<ArgumentNullException>("compareInfo", () => { CompareInfo info = null; info.GetStringComparer(CompareOptions.None); });

            Assert.Throws<ArgumentException>("options", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer((CompareOptions)0xFFFF));
            Assert.Throws<ArgumentException>("options", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer(CompareOptions.Ordinal | CompareOptions.IgnoreCase));
            Assert.Throws<ArgumentException>("options", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer(CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreCase));

            Assert.Throws<ArgumentNullException>("obj", () => new CultureInfo("tr-TR").CompareInfo.GetStringComparer(CompareOptions.None).GetHashCode(null));
        }

        [Fact]
        [ActiveIssue(810, PlatformID.AnyUnix)]
        public static void CompareInfoBasicTests()
        {
            string one = "A test string";
            string aCopyOfOne = one;

            StringComparer comp = new CultureInfo("fr-FR").CompareInfo.GetStringComparer(CompareOptions.IgnoreCase);

            Assert.Equal(0, comp.Compare(one, aCopyOfOne));
            Assert.True(comp.Equals(one, aCopyOfOne));

            Assert.Equal(-1, comp.Compare(null, one));
            Assert.Equal(0, comp.Compare(null, null));
            Assert.Equal(1, comp.Compare(one, null));

            Assert.False(comp.Equals(null, one));
            Assert.True(comp.Equals(null, null));
            Assert.False(comp.Equals(one, null));

            Assert.Equal(comp.GetHashCode("abc"), comp.GetHashCode("ABC"));
        }

        [Theory]
        [ActiveIssue(810, PlatformID.AnyUnix)]
        [InlineData("abc", "def", -1, "fr-FR", CompareOptions.IgnoreCase)]
        [InlineData("abc", "ABC", 0, "fr-FR", CompareOptions.IgnoreCase)]
        [InlineData("def", "ABC", 1, "fr-FR", CompareOptions.IgnoreCase)]
        [InlineData("abc", "ABC", 32, "en-US", CompareOptions.Ordinal)]     // this test generates a 32 for some reason
        [InlineData("abc", "ABC", 0, "en-US", CompareOptions.OrdinalIgnoreCase)]
        [InlineData("Cot\u00E9", "cot\u00E9", 0, "fr-FR", CompareOptions.IgnoreCase)]
        [InlineData("cot\u00E9", "c\u00F4te", 1, "fr-FR", CompareOptions.None)]
        public static void CompareVarying(string one, string two, int compareValue, string culture, CompareOptions compareOptions)
        {
            StringComparer comp = new CultureInfo(culture).CompareInfo.GetStringComparer(compareOptions);

            Assert.Equal(compareValue, comp.Compare(one, two));
            if (compareValue == 0)
            {
                Assert.True(comp.Equals(one, two));
            }
            else
            {
                Assert.False(comp.Equals(one, two));
            }
        }

        [Fact]
        [ActiveIssue(810, PlatformID.AnyUnix)]
        public static void CompareInfoIdentityTests()
        {
            StringComparer us = new CultureInfo("en-US").CompareInfo.GetStringComparer(CompareOptions.IgnoreCase);
            StringComparer us2 = new CultureInfo("en-US").CompareInfo.GetStringComparer(CompareOptions.IgnoreCase);
            StringComparer usNoSym = new CultureInfo("en-US").CompareInfo.GetStringComparer(CompareOptions.IgnoreSymbols);
            StringComparer fr = new CultureInfo("fr-FR").CompareInfo.GetStringComparer(CompareOptions.IgnoreCase);
            StringComparer frOrdinal = new CultureInfo("fr-FR").CompareInfo.GetStringComparer(CompareOptions.Ordinal);

            Assert.True(us.Equals(us2));
            Assert.False(us.Equals(usNoSym));
            Assert.False(us.Equals(fr));
            Assert.False(us.Equals(frOrdinal));

            Assert.Equal(us.GetHashCode(), us2.GetHashCode());
            Assert.NotEqual(us.GetHashCode(), usNoSym.GetHashCode());
            Assert.NotEqual(us.GetHashCode(), fr.GetHashCode());
            Assert.NotEqual(frOrdinal.GetHashCode(), fr.GetHashCode());
        }
    }
}
