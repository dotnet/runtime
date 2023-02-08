// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public unsafe class Runtime_54956
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool succeeded = true;

        succeeded &= Test(() => SideEffects_Vector_AndNot(Array.Empty<Vector<int>>(), null));

        succeeded &= Test(() => SideEffects_Avx2_PermuteVar8x32(Array.Empty<Vector256<int>>(), null));
        succeeded &= Test(() => SideEffects_Avx2_ShiftLeftLogical(Array.Empty<Vector256<int>>(), null));
        succeeded &= Test(() => SideEffects_Avx2_ShiftRightArithmetic(Array.Empty<Vector256<int>>(), null));
        succeeded &= Test(() => SideEffects_Avx2_ShiftRightLogical(Array.Empty<Vector256<int>>(), null));

        succeeded &= Test(() => SideEffects_Sse2_CompareScalarNotGreaterThanOrEqual(Array.Empty<Vector128<double>>(), null));
        succeeded &= Test(() => SideEffects_Sse2_ShiftLeftLogical(Array.Empty<Vector128<int>>(), null));
        succeeded &= Test(() => SideEffects_Sse2_ShiftRightArithmetic(Array.Empty<Vector128<int>>(), null));
        succeeded &= Test(() => SideEffects_Sse2_ShiftRightLogical(Array.Empty<Vector128<int>>(), null));

        succeeded &= Test(() => SideEffects_Vector64_Store(Array.Empty<Vector64<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector64_StoreAligned(Array.Empty<Vector64<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector64_StoreAlignedNonTemporal(Array.Empty<Vector64<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector64_StoreUnsafe(Array.Empty<Vector64<int>>(), ref Unsafe.NullRef<int>()));

        succeeded &= Test(() => SideEffects_Vector128_Store(Array.Empty<Vector128<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector128_StoreAligned(Array.Empty<Vector128<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector128_StoreAlignedNonTemporal(Array.Empty<Vector128<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector128_StoreUnsafe(Array.Empty<Vector128<int>>(), ref Unsafe.NullRef<int>()));

        succeeded &= Test(() => SideEffects_Vector256_Store(Array.Empty<Vector256<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector256_StoreAligned(Array.Empty<Vector256<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector256_StoreAlignedNonTemporal(Array.Empty<Vector256<int>>(), null));
        succeeded &= Test(() => SideEffects_Vector256_StoreUnsafe(Array.Empty<Vector256<int>>(), ref Unsafe.NullRef<int>()));

        return succeeded ? 100 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test(Action action, [CallerArgumentExpression("action")] string expr = null)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            if (exception is IndexOutOfRangeException)
            {
                return true;
            }

            Console.WriteLine($"{expr} failed. Expected {nameof(IndexOutOfRangeException)} but caught {exception.GetType().Name} instead.");
            return false;
        }

        Console.WriteLine($"{expr} failed. Expected {nameof(IndexOutOfRangeException)} but none was thrown.");
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> SideEffects_Vector_AndNot(Vector<int>[] left, Vector<int>* right)
    {
        return Vector.AndNot(left[0], *right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> SideEffects_Avx2_PermuteVar8x32(Vector256<int>[] left, Vector256<int>* right)
    {
        if (Avx2.IsSupported)
        {
            return Avx2.PermuteVar8x32(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> SideEffects_Avx2_ShiftLeftLogical(Vector256<int>[] left, byte* right)
    {
        if (Avx2.IsSupported)
        {
            return Avx2.ShiftLeftLogical(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> SideEffects_Avx2_ShiftRightArithmetic(Vector256<int>[] left, byte* right)
    {
        if (Avx2.IsSupported)
        {
            return Avx2.ShiftRightArithmetic(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> SideEffects_Avx2_ShiftRightLogical(Vector256<int>[] left, byte* right)
    {
        if (Avx2.IsSupported)
        {
            return Avx2.ShiftRightLogical(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> SideEffects_Sse2_CompareScalarNotGreaterThanOrEqual(Vector128<double>[] left, Vector128<double>* right)
    {
        if (Sse2.IsSupported)
        {
            return Sse2.CompareScalarNotGreaterThanOrEqual(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> SideEffects_Sse2_ShiftLeftLogical(Vector128<int>[] left, byte* right)
    {
        if (Sse2.IsSupported)
        {
            return Sse2.ShiftLeftLogical(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> SideEffects_Sse2_ShiftRightArithmetic(Vector128<int>[] left, byte* right)
    {
        if (Sse2.IsSupported)
        {
            return Sse2.ShiftRightArithmetic(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> SideEffects_Sse2_ShiftRightLogical(Vector128<int>[] left, byte* right)
    {
        if (Sse2.IsSupported)
        {
            return Sse2.ShiftRightLogical(left[0], *right);
        }
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector64_Store(Vector64<int>[] source, int* destination)
    {
        source[0].Store(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector64_StoreAligned(Vector64<int>[] source, int* destination)
    {
        source[0].StoreAligned(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector64_StoreAlignedNonTemporal(Vector64<int>[] source, int* destination)
    {
        source[0].StoreAlignedNonTemporal(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector64_StoreUnsafe(Vector64<int>[] source, ref int destination)
    {
        source[0].StoreUnsafe(ref destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector128_Store(Vector128<int>[] source, int* destination)
    {
        source[0].Store(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector128_StoreAligned(Vector128<int>[] source, int* destination)
    {
        source[0].StoreAligned(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector128_StoreAlignedNonTemporal(Vector128<int>[] source, int* destination)
    {
        source[0].StoreAlignedNonTemporal(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector128_StoreUnsafe(Vector128<int>[] source, ref int destination)
    {
        source[0].StoreUnsafe(ref destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector256_Store(Vector256<int>[] source, int* destination)
    {
        source[0].Store(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector256_StoreAligned(Vector256<int>[] source, int* destination)
    {
        source[0].StoreAligned(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector256_StoreAlignedNonTemporal(Vector256<int>[] source, int* destination)
    {
        source[0].StoreAlignedNonTemporal(destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffects_Vector256_StoreUnsafe(Vector256<int>[] source, ref int destination)
    {
        source[0].StoreUnsafe(ref destination);
    }
}
