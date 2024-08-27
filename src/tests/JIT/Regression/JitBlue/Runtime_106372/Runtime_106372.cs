// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using Xunit;

// Found by Antigen
// Reduced from 161.42 KB to 874 B.
// Further reduced by hand.
//
// Assert failure(PID 35056 [0x000088f0], Thread: 28500 [0x6f54]): Assertion failed 'unreached' in 'TestClass:Method0():this' during 'Importation' (IL size 116; hash 0x46e9aa75; Tier0)
//     File: C:\wk\runtime\src\coreclr\jit\gentree.cpp:22552
//     Image: C:\wk\runtime\artifacts\tests\coreclr\windows.x86.Checked\Tests\Core_Root\corerun.exe

public class Runtime_106372
{
    static long s_long_11 = 1;

    public static void TestEntryPoint()
    {
        Vector128<long> result = Vector128.CreateSequence(s_long_11, -2);
        Assert(Vector128.Create(+1L, -1L), result);
    }
}
