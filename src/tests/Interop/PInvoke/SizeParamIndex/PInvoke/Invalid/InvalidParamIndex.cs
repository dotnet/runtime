// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class Program
{
    [DllImport("Unused")]
    static extern void SizeParamIndexTooBig(
        out byte arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 20)] out byte[] arrByte);

    [DllImport("Unused")]
    public static extern void SizeParamIndexWrongType(
        out string arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out byte[] arrByte);

    static int Main()
    {
        Assert.Throws<MarshalDirectiveException>(() => SizeParamIndexTooBig(out var _, out var _));
        Assert.Throws<MarshalDirectiveException>(() => SizeParamIndexWrongType(out var _, out var _));

        return 100;
    }
}
