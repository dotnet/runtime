// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.aa

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class TestClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector256<int> Method0() => Avx2.ShiftRightArithmetic(Vector256<int>.AllBitsSet, Vector128<int>.AllBitsSet);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> Method1() => Sse2.ShiftRightArithmetic(Vector128<int>.AllBitsSet, Vector128<int>.AllBitsSet);

    [Fact]
    public static void TestEntryPoint()
    {
        if (Avx2.IsSupported)
        {
            _ = Method0();
        }

        if (Sse2.IsSupported)
        {
            _ = Method1();
        }
    }
}
