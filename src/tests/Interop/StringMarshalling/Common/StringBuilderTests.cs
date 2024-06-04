// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Text;
using Xunit;

using static StringMarshalingTestNative;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public partial class StringBuilderTests
{
    private static readonly string InitialString = "Hello World";

    [Fact]
    public static void ByValue()
    {
        var builder = new StringBuilder(InitialString);
        ReverseInplace(builder);
        Assert.Equal(Helpers.Reverse(InitialString), builder.ToString());
    }

    [Fact]
    public static void ByRef()
    {
        var builder = new StringBuilder(InitialString);
        ReverseInplaceByref(ref builder);
        Assert.Equal(Helpers.Reverse(InitialString), builder.ToString());
    }

    [Fact]
    public static void ReversePInvoke()
    {
        var builder = new StringBuilder(InitialString);
        Assert.True(ReverseInplaceInCallback(builder, b =>
        {
            string reversed = Helpers.Reverse(b.ToString());
            b.Clear();
            b.Append(reversed);
        }));
    }
}
