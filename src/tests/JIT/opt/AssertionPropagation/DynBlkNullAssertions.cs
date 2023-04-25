// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// "cpblk/initblk" should not generate non-null assertions because the
// indirections they represent may not be realized (in case the "size"
// was zero).

using System;
using System.Runtime.CompilerServices;

public class DynBlkNullAssertions
{
    public static int Main()
    {
        if (!TestCpBlk(ref Unsafe.NullRef<byte>(), ref Unsafe.NullRef<byte>(), 0))
        {
            return 101;
        }
        if (!TestInitBlk(ref Unsafe.NullRef<byte>(), 0, 0))
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestCpBlk(ref byte dst, ref byte src, uint size)
    {
        Unsafe.CopyBlock(ref dst, ref src, size);

        return Unsafe.AreSame(ref dst, ref Unsafe.NullRef<byte>()) && Unsafe.AreSame(ref src, ref Unsafe.NullRef<byte>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestInitBlk(ref byte dst, byte value, uint size)
    {
        Unsafe.InitBlock(ref dst, value, size);

        return Unsafe.AreSame(ref dst, ref Unsafe.NullRef<byte>());
    }
}
