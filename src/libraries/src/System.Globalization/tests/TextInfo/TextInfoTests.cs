// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoMiscTests
    {
        public static IEnumerable<object[]> TextInfo_TestData()
        {
            yield return new object[] { "", 0x7f, 0x4e4, 0x25, 0x2710, 0x1b5, false };
            yield return new object[] { "en-US", 0x409, 0x4e4, 0x25, 0x2710, 0x1b5, false };
            yield return new object[] { "ja-JP", 0x411, 0x3a4, 0x4f42, 0x2711, 0x3a4, false };
            yield return new object[] { "zh-CN", 0x804, 0x3a8, 0x1f4, 0x2718, 0x3a8, false };
            yield return new object[] { "ar-SA", 0x401, 0x4e8, 0x4fc4, 0x2714, 0x2d0, true };
            yield return new object[] { "ko-KR", 0x412, 0x3b5, 0x5161, 0x2713, 0x3b5, false };
            yield return new object[] { "he-IL", 0x40d, 0x4e7, 0x1f4, 0x2715, 0x35e, true };
        }

        [Theory]
        [MemberData(nameof(TextInfo_TestData))]
        public void MiscTest(string cultureName, int lcid, int ansiCodePage, int ebcdiCCodePage, int macCodePage, int oemCodePage, bool isRightToLeft)
        {
            TextInfo ti = CultureInfo.GetCultureInfo(cultureName).TextInfo;
            Assert.Equal(lcid, ti.LCID);
            Assert.Equal(ansiCodePage, ti.ANSICodePage);
            Assert.Equal(ebcdiCCodePage, ti.EBCDICCodePage);
            Assert.Equal(macCodePage, ti.MacCodePage);
            Assert.Equal(oemCodePage, ti.OEMCodePage);
            Assert.Equal(isRightToLeft, ti.IsRightToLeft);
        }

        [Fact]
        public void ReadOnlyTest()
        {
            TextInfo ti = CultureInfo.GetCultureInfo("en-US").TextInfo;
            Assert.True(ti.IsReadOnly, "IsReadOnly should be true with cached TextInfo object");

            ti = (TextInfo) ti.Clone();
            Assert.False(ti.IsReadOnly, "IsReadOnly should be false with cloned TextInfo object");
            
            ti = TextInfo.ReadOnly(ti);
            Assert.True(ti.IsReadOnly, "IsReadOnly should be true with created read-nly TextInfo object");
        }

        [Fact]
        public void ToTitleCaseTest()
        {
            TextInfo ti = CultureInfo.GetCultureInfo("en-US").TextInfo;
            Assert.Equal("A Tale Of Two Cities", ti.ToTitleCase("a tale of two cities"));
            Assert.Equal("Growl To The Rescue", ti.ToTitleCase("gROWL to the rescue"));
            Assert.Equal("Inside The US Government", ti.ToTitleCase("inside the US government"));
            Assert.Equal("Sports And MLB Baseball", ti.ToTitleCase("sports and MLB baseball"));
            Assert.Equal("The Return Of Sherlock Holmes", ti.ToTitleCase("The Return of Sherlock Holmes"));
            Assert.Equal("UNICEF And Children", ti.ToTitleCase("UNICEF and children"));

            AssertExtensions.Throws<ArgumentNullException>("str", () => ti.ToTitleCase(null));
        }

        public static IEnumerable<object[]> DutchTitleCaseInfo_TestData()
        {
            yield return new object[] { "nl-NL", "IJ IJ IJ IJ", "ij iJ Ij IJ" };
            yield return new object[] { "nl-be", "IJzeren Eigenschappen", "ijzeren eigenschappen" };
            yield return new object[] { "NL-NL", "Lake IJssel", "lake iJssel" };
            yield return new object[] { "NL-BE", "Boba N' IJango Fett PEW PEW", "Boba n' Ijango fett PEW PEW" };
            yield return new object[] { "en-us", "Ijill And Ijack", "ijill and ijack" };
            yield return new object[] { "de-DE", "Ij Ij IJ Ij", "ij ij IJ ij" };
            yield return new object[] { "he-il", "Ijon't Know What Will Happen.", "Ijon't know what Will happen." };
        }

        [Theory]
        [MemberData(nameof(DutchTitleCaseInfo_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Desktop Framework hasn't received the fix for dotnet/corefx#16770 yet.")]
        public void ToTitleCaseDutchTest(string cultureName, string expected, string actual)
        {
            TextInfo ti = CultureInfo.GetCultureInfo(cultureName).TextInfo;
            Assert.Equal(expected, ti.ToTitleCase(actual));
        }
    }
}
