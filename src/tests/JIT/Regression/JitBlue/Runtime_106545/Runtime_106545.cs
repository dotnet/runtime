// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
Found by Fuzzlyn

Assertion failed '(emitThisGCrefRegs & regMask) == 0' in 'TestClass:Method4(short,System.Runtime.Intrinsics.Vector512`1[long],byref,short,byref,byref,TestClass+S1,byref):byte:this' during 'Emit code' (IL size 47; hash 0x0a275b75; FullOpts)

    File: D:\a\_work\1\s\src\coreclr\jit\emitxarch.cpp Line: 1498
*/
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_106545
{   
    Vector512<byte> v512_byte_97 = Vector512.CreateScalar((byte)1);

    public ulong Method0()
    {
        v512_byte_97 = Vector512<byte>.AllBitsSet;
        return (0 - Vector512.ExtractMostSignificantBits(v512_byte_97));
    }
    
    [Fact]
    public static void TestEntryPoint()
    {
        if (Avx2.IsSupported)
        {
            new Runtime_106545().Method0();
        }
    }
}
