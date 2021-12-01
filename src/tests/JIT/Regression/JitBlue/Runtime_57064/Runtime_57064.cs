// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

struct S
{
    public uint F0;
    public ushort F1;
    public uint F2;
}

public class Runtime_57064
{
    public static int Main()
    {
        S val = Create();
        val.F0 = 0xF0;
        val.F1 = 0xF1;
        val.F2 = 0xF2;
        // This call splits S between registers and stack on ARM32.
        // The issue was that we were writing S.F2 at stack+2
        // instead of stack+4 when val was promoted.
        return Split(null, false, null, val) == 0xF2 ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint Split(ushort[] arg0, bool arg1, bool[] arg2, S arg3)
    {
        return arg3.F2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S Create() => default;
}
