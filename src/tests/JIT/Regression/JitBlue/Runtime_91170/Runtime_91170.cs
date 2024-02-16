// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class TestClass
{
    public struct S1
    {
    }

    static short s_short_8 = 5;
    static int s_int_9 = -2;
    long long_59 = 4;
    uint uint_64 = 1;
    Vector256<int> v256_int_90 = Vector256.Create(2, -5, 4, 4, 5, 0, -1, 5);
    S1 s1_99 = new S1();

    private uint Method4(out short p_short_161, S1 p_s1_162, bool p_bool_163, ref int p_int_164)
    {
        unchecked
        {
            p_short_161 = 15|4;
            if ((long_59 *= 15>>4)!= (long_59 |= 15^4))
            {
            }
            else
            {
                Vector128.CreateScalarUnsafe(Vector256.Sum(v256_int_90));
            }
            return 15|4;
        }
    }

    private void Method0()
    {
        unchecked
        {
            uint_64 = Method4(out s_short_8, s1_99, 15<4, ref s_int_9);
            return;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        new TestClass().Method0();
    }
}
/*

Assert failure(PID 34336 [0x00008620], Thread: 38576 [0x96b0]): Assertion failed '!childNode->isContainableHWIntrinsic()' in 'TestClass:Method4(byref,TestClass+S1,bool,byref):uint:this' during 'Lowering nodeinfo' (IL size 63; hash 0xa4e6dede; Tier0)
    File: D:\git\runtime\src\coreclr\jit\lowerxarch.cpp Line: 8201
    Image: D:\git\runtime\artifacts\tests\coreclr\windows.x64.Checked\tests\Core_Root\CoreRun.exe
*/
