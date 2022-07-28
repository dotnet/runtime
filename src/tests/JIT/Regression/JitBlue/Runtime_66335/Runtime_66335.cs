// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public class Runtime_66335
{
    private static S0 s_24;

    public static int Main()
    {
        return Problem() == 1 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem()
    {
        S0 vr7 = new S0(1);
        s_24.F4 += (sbyte)-vr7.F2;

        return vr7.F2;
    }

    public struct S0
    {
        public ushort F0;
        public uint F1;
        public sbyte F2;
        public sbyte F4;
        public ulong F5;
        public S0(sbyte f2) : this()
        {
            F2 = f2;
        }
    }
}

