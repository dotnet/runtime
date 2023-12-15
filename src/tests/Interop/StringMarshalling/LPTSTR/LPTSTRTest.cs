// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

using static LPTStrTestNative;

[OuterLoop]
[SkipOnMono("needs triage")]
public partial class StringTests
{
}


[OuterLoop]
[SkipOnMono("needs triage")]
public partial class StringBuilderTests
{
}


[OuterLoop]
[SkipOnMono("needs triage")]
public partial class StringInStructTests
{
}


[OuterLoop]
[SkipOnMono("needs triage")]
[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class LPTStrTest
{
    private static readonly string InitialString = "Hello World";
    private static readonly string LongString = "0123456789abcdefghi";
    private static readonly string LongUnicodeString = "\uD83D\uDC68\u200D\uD83D\uDC68\u200D\uD83D\uDC67\u200D\uD83D\uDC67\uD83D\uDC31\u200D\uD83D\uDC64";

    [Fact]
    public static void RunStringBuilderTests()
    {
        int length = 10;
        StringBuilder nullTerminatorBuilder = new StringBuilder(length);
        Assert.True(Verify_NullTerminators_PastEnd(nullTerminatorBuilder, length));
        Assert.True(Verify_NullTerminators_PastEnd_Out(nullTerminatorBuilder, length));
    }

    [Fact]
    public static void RunByValTStrTests()
    {
        Assert.True(MatchFuncNameAnsi(new ByValStringInStructAnsi { str = nameof(MatchFuncNameAnsi)}));

        var ansiStr = new ByValStringInStructAnsi
        {
            str = InitialString
        };

        ReverseByValStringAnsi(ref ansiStr);

        Assert.Equal(Helpers.Reverse(InitialString), ansiStr.str);

        Assert.True(MatchFuncNameUni(new ByValStringInStructUnicode { str = nameof(MatchFuncNameUni)}));

        var uniStr = new ByValStringInStructUnicode
        {
            str = InitialString
        };

        ReverseByValStringUni(ref uniStr);
        Assert.Equal(Helpers.Reverse(InitialString), uniStr.str);

        ReverseCopyByValStringAnsi(new ByValStringInStructAnsi { str = LongString }, out ByValStringInStructSplitAnsi ansiStrSplit);

        Assert.Equal(Helpers.Reverse(LongString[^10..]), ansiStrSplit.str1);
        Assert.Equal(Helpers.Reverse(LongString[..^10]), ansiStrSplit.str2);

        ReverseCopyByValStringUni(new ByValStringInStructUnicode { str = LongString }, out ByValStringInStructSplitUnicode uniStrSplit);

        Assert.Equal(Helpers.Reverse(LongString[^10..]), uniStrSplit.str1);
        Assert.Equal(Helpers.Reverse(LongString[..^10]), uniStrSplit.str2);

        ReverseCopyByValStringUni(new ByValStringInStructUnicode { str = LongUnicodeString }, out ByValStringInStructSplitUnicode uniStrSplit2);

        Assert.Equal(Helpers.Reverse(LongUnicodeString[^10..]), uniStrSplit2.str1);
        Assert.Equal(Helpers.Reverse(LongUnicodeString[..^10]), uniStrSplit2.str2);
    }
}
