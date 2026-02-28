// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace GB18030.Tests;

[SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
public class CharUnicodeInfoTests
{
    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void GetUnicodeCategory(CharUnicodeInfoTestCase testCase)
    {
        UnicodeCategory expected = PlatformDetection.IsNetFramework ? UnicodeCategory.OtherNotAssigned : UnicodeCategory.OtherLetter;

        if (testCase.Utf32CodeValue.Length == 1)
        {
            Assert.Equal(expected, CharUnicodeInfo.GetUnicodeCategory(testCase.Utf32CodeValue[0]));
        }

        Assert.Equal(expected, CharUnicodeInfo.GetUnicodeCategory(testCase.Utf32CodeValue, 0));
#if !NETFRAMEWORK
        Assert.Equal(expected, CharUnicodeInfo.GetUnicodeCategory(testCase.CodePoint));
#endif
        Assert.True(PlatformDetection.IsNetFramework || testCase.GeneralCategory == expected);
    }
}
