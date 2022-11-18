// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public unsafe class Runtime_69965
{
    public static int Main()
    {
        const int Value = 10;
        var vtor = Vector128.Create(Value, Value, Value, Value);
        var vtors = new StructWithOverlappedVtor128[] { new StructWithOverlappedVtor128 { Vtor = vtor } };

        return Problem(vtors) != Value ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem(StructWithOverlappedVtor128[] a)
    {
        static Vector128<int> Tunnel(StructWithOverlappedVtor128[] a) => a[0].Vtor;

        return CallForVtor(Tunnel(a));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallForVtor(Vector128<int> value) => value.GetElement(0);

    [StructLayout(LayoutKind.Explicit)]
    struct StructWithOverlappedVtor128
    {
        [FieldOffset(16)]
        public Vector128<int> Vtor;
        [FieldOffset(16)]
        public Vector128<uint> AnotherVtor;
    }
}
