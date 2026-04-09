// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public class InvalidSizeParamIndex
{
    [DllImport("Unused")]
    static extern void SizeParamIndexTooBig(
        out byte arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 20)] out byte[] arrByte);

    [DllImport("Unused")]
    public static extern void SizeParamIndexWrongType(
        out string arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out byte[] arrByte);

    [Fact]
    [SkipOnMono("needs triage")]
    public static void TooBig()
    {
        Assert.Throws<MarshalDirectiveException>(() => SizeParamIndexTooBig(out var _, out var _));
    }

    [Fact]
    [SkipOnMono("needs triage")]
    public static void WrongType()
    {
        Assert.Throws<MarshalDirectiveException>(() => SizeParamIndexWrongType(out var _, out var _));
    }
}
