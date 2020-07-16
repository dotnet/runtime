// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;

public class TestAsm
{
    public static int Sum(int a, int b)
    {
        return NativeSum(a, b);
    }

    [DllImport(NativeLibraryToLoad.InvalidName)]
    static extern int NativeSum(int arg1, int arg2);
}
