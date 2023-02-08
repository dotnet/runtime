// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

struct S0
{
    public ulong F0;
    public ulong F1;
    public long F2;
    public uint F4;
    public uint F6;
}

public class Runtime_55141
{
    // UDIV is lowered to the MULHI/BITCAST nodes and they are stored in field (STORE_LCL_FLD).
    // BITCAST is marked as contained so the value to be stored can be used from MULHI, but marking
    // the containment of BITCAST is not supported in codegen for STORE_LCL_FLD.
    [Fact]
    public static int TestEntryPoint()
    {
        return (uint)Run(0) == 0 ? 100 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Run(long x)
    {
        S0 vr1 = default(S0);
        vr1.F4 = (uint)x / 254;
        return vr1.F6;
    }
}
