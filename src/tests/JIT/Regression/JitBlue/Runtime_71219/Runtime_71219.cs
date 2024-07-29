// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_71219
{
    [Fact]
    public static int TestEntryPoint()
    {
        Vector4 vtor = new Vector4(1, 2, 3, 4);

        if (ProblemWithNonFloatField(vtor))
        {
            return 101;
        }

        if (ProblemWithMisalignedField(vtor.AsVector128().AsInt64()))
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithNonFloatField(Vector4 vtor)
    {
        vtor += vtor;
        return Unsafe.As<Vector4, StructWithIndex>(ref vtor).Value != Unsafe.As<float, int>(ref vtor.Y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithMisalignedField(Vector128<long> vtor)
    {
        vtor += vtor;
        return
            Unsafe.As<Vector128<long>, StructWithLng>(ref vtor).Long !=
            Unsafe.As<byte, long>(ref Unsafe.Add(ref Unsafe.As<Vector128<long>, byte>(ref vtor), 4));
    }

    struct StructWithIndex
    {
        public int Index;
        public int Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct StructWithLng
    {
        [FieldOffset(4)]
        public long Long;
    }
}
