// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Lowering elides transparent scalar/vector reinterprets (CreateScalarUnsafe, GetLower, ...).
// Codegen for the dual-overload x64 intrinsics below must therefore distinguish the vector and
// pointer/index overloads using stable node metadata rather than the post-lowering operand type:
//   * ConvertTo*Int* picked the pointer (memory-load) overload from the operand type, so an elided
//     CreateScalarUnsafe made it load from the scalar value as if it were an address.
//   * An AVX2 gather selects its VSIB width (xmm vs ymm index) from the index operand's width, so an
//     elided GetLower on the index widened it and gathered too many elements.

namespace Runtime_131137;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static class Runtime_131137
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> ConvertI32(int packed)
        => Sse41.ConvertToVector128Int32(Vector128.CreateScalarUnsafe(packed).AsByte());

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> ConvertI16(int packed)
        => Sse41.ConvertToVector128Int16(Vector128.CreateScalarUnsafe(packed).AsSByte());

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> ConvertI64(int packed)
        => Sse41.ConvertToVector128Int64(Vector128.CreateScalarUnsafe(packed).AsByte());

    [ConditionalFact(typeof(Sse41), nameof(Sse41.IsSupported))]
    public static void ConvertToVectorFromScalar()
    {
        Assert.Equal(Vector128.Create(1, 2, 3, 4), ConvertI32(0x04030201));
        Assert.Equal(Vector128.Create((short)1, 2, 3, 4, 0, 0, 0, 0), ConvertI16(0x04030201));
        Assert.Equal(Vector128.Create(1L, 2), ConvertI64(0x00000201));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> ConvertI32x8(long packed)
        => Avx2.ConvertToVector256Int32(Vector128.CreateScalarUnsafe(packed).AsByte());

    [ConditionalFact(typeof(Avx2), nameof(Avx2.IsSupported))]
    public static void ConvertToVector256FromScalar()
    {
        Assert.Equal(Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8), ConvertI32x8(0x0807060504030201L));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector128<int> GatherQD(int* baseAddr, Vector256<long> index)
        => Avx2.GatherVector128(baseAddr, index.GetLower(), 4);

    [ConditionalFact(typeof(Avx2), nameof(Avx2.IsSupported))]
    public static unsafe void GatherWithNarrowedIndex()
    {
        int* buf = stackalloc int[4] { 20, 21, 22, 23 };
        Vector256<long> index = Vector256.Create(0L, 2, 1, 3);

        // Only the low 128 bits of the index ({0, 2}) are in play, so lanes 2 and 3 stay zero.
        Assert.Equal(Vector128.Create(20, 22, 0, 0), GatherQD(buf, index));
    }
}
