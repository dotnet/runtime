// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
internal struct AA
{
    public short tmp1;
    public short q;

    public ushort tmp2;
    public int tmp3;

    public AA(short qq)
    {
        tmp1 = 106;
        tmp2 = 107;
        tmp3 = 108;
        q = qq;
    }
    
    // The test verifies that we accurately update the byref variable that is a field of struct.
    public static short call_target_ref(ref short arg) { arg = 100; return arg; }
}

   
public class Runtime_57912
{

    [Fact]
    public static int TestEntryPoint()
    {
        return (int)test_0_17(100, new AA(100), new AA(0));
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static short test_0_17(int num, AA init, AA zero)
    {
        return AA.call_target_ref(ref init.q);
    }
}