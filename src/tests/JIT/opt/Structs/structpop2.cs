// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Optimization of pop with struct types
// Codegen for TestByPtr should be minimal
//
// See https://github.com/dotnet/runtime/issues/10607

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
struct VT
{
    public static readonly int Size = Marshal.SizeOf<VT>();

    public int F1, F2, F3, F4, F5, F6, F7, F8;
}

public class P
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Do(int v1)
    {
        Console.WriteLine("v1={0}", v1);
        return v1;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    unsafe static int TestByPtr(VT* pVT)
    {
        int v1, v2, v3, v4, v5, v6, v7, v8;
        v1 = pVT->F1;
        v2 = pVT->F2;
        v3 = pVT->F3;
        v4 = pVT->F4;
        v5 = pVT->F5;
        v6 = pVT->F6;
        v7 = pVT->F7;
        v8 = pVT->F8;
        return Do(v1);
    }
    
    [Fact]
    public unsafe static int TestEntryPoint()
    {
        byte* pDataBytes = stackalloc byte[VT.Size];
        VT* pVT = (VT*)pDataBytes;
        pVT->F1 = 1;
        pVT->F2 = 2;
        pVT->F3 = 3;
        pVT->F4 = 4;
        pVT->F5 = 5;
        pVT->F6 = 6;
        pVT->F7 = 7;
        pVT->F8 = 8;
        int result = TestByPtr(pVT);
        return result + 99;
    }
}


