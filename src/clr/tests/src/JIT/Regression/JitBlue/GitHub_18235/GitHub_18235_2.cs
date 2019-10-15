// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

struct S0
{
    public uint F0;
    public byte F2;
    public int F3;
    public sbyte F5;
    public long F8;
}

public class Program
{
    static uint s_0;
    public static int Main()
    {
        S0 vr3 = new S0();
        vr3.F0 = 0x10001;
        // The JIT was giving the RHS below the same value-number as
        // 0x10001 above, which was then constant propagated to
        // usages of vr4, even though it should be narrowed.
        uint vr4 = (ushort)vr3.F0;
        s_0 = vr4;
        return vr4 == 1 ? 100 : 0;
    }
}
