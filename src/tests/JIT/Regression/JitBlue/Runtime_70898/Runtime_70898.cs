// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public unsafe class Runtime_70898
{
    public static int Main()
    {
        return Problem(default) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(StructWithIndex a)
    {
        var x = a;
        Use(&x);
        Unsafe.InitBlock(ref Unsafe.As<StructWithIndex, StructWithBytes>(ref x).ByteTwo, 1, 2);

        return (byte)x.Index == 1;
    }

    public static void Use<T>(T* arg) where T : unmanaged { }

    struct StructWithIndex
    {
        public int Index;
        public int Value;
    }

    struct StructWithBytes
    {
        public byte ByteOne;
        public byte ByteTwo;
    }
}
