// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace GB18030.Tests;

public class CharUnicodeInfoTests
{
    [Theory]
    [MemberData(nameof(TestHelper.GB18030CharUnicodeInfoMemberData), MemberType = typeof(TestHelper))]
    public void GetUnicodeCategory(CharUnicodeInfoTestCase testCase)
    {
        if (testCase.Utf32CodeValue.Length == 1)
        {
            Assert.Equal(UnicodeCategory.OtherLetter, CharUnicodeInfo.GetUnicodeCategory(testCase.Utf32CodeValue[0]));
        }

        Assert.Equal(UnicodeCategory.OtherLetter, CharUnicodeInfo.GetUnicodeCategory(testCase.Utf32CodeValue, 0));
#if !NETFRAMEWORK
        Assert.Equal(UnicodeCategory.OtherLetter, CharUnicodeInfo.GetUnicodeCategory(testCase.CodePoint));
#endif
        Assert.Equal(UnicodeCategory.OtherLetter, testCase.GeneralCategory);
    }
}
