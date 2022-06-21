// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public unsafe class Runtime_70954
{
    public static int Main()
    {
        return Problem(default) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(ImplicitByRefStruct a)
    {
        return CallForThreeByteStruct(a.ThreeByteStruct) != 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte CallForThreeByteStruct(ThreeByteStruct arg0) => arg0.Bytes[0];

    struct ImplicitByRefStruct
    {
        public ThreeByteStruct ThreeByteStruct;
        public fixed byte Fill[16];
    }

    struct ThreeByteStruct
    {
        public fixed byte Bytes[3];
    }
}
