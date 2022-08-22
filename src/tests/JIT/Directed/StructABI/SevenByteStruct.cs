// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

// On ARM32 the following has S0 passed in two registers, which requires passing 3 bytes in the last register.
// We cannot do that in a single load from an arbitrary source and must copy it to a local first.

public struct S0
{
    public byte F0;
    public byte F1;
    public byte F2;
    public byte F3;
    public byte F4;
    public byte F5;
    public byte F6;
}

public class SevenByteStruct
{
    public static S0 s_4 = new S0 { F0 = 1, F1 = 2, F2 = 3, F3 = 4, F4 = 5, F5 = 6, F6 = 7 };
    public static int Main()
    {
        ref S0 vr0 = ref s_4;
        int sum = M35(vr0);
        return sum == 28 ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int M35(S0 arg0)
    {
        return arg0.F0 + arg0.F1 + arg0.F2 + arg0.F3 + arg0.F4 + arg0.F5 + arg0.F6;
    }
}
