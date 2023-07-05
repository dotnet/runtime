// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_61510
{
    [FixedAddressValueType]
    private static byte s_field;

    [Fact]
    public static int TestEntryPoint()
    {
        ref byte result = ref AddZeroByrefToNativeInt((nint)Unsafe.AsPointer(ref s_field));

        return Unsafe.AreSame(ref s_field, ref result) ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref byte AddZeroByrefToNativeInt(nint addr)
    {
        return ref Unsafe.Add(ref Unsafe.NullRef<byte>(), addr);
    }
}
