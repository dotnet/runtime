// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

using static StringMarshalingTestNative;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public partial class StringInStructTests
{
    private static readonly string InitialString = "Hello World";

    [Fact]
    public static void ByValue()
    {
        Assert.True(MatchFunctionNameInStruct(new StringInStruct { str = nameof(MatchFunctionNameInStruct)}));
    }

    [Fact]
    public static void ByRef()
    {
        var str = new StringInStruct
        {
            str = InitialString
        };

        ReverseInplaceByrefInStruct(ref str);

        Assert.Equal(Helpers.Reverse(InitialString), str.str);
    }
}
