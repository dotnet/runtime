// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;

namespace Webcil;

// Exercises the wasm v128 calling convention (SIMD passed/returned/stored by value)
// without relying on any SIMD arithmetic intrinsics, so only the ABI/materialization
// paths are covered.
public static class WasmSimdModule
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> Echo(Vector128<int> value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> ThroughLocal(Vector128<int> value)
    {
        Vector128<int> local = value;
        return local;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Store(Vector128<int> value, ref Vector128<int> destination)
    {
        destination = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> CallEcho(Vector128<int> value)
    {
        return Echo(value);
    }

    // Vector<T> is 16 bytes on wasm (128-bit vectors), so it uses the same v128 ABI as Vector128<T>.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<int> EchoVectorT(Vector<int> value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<int> CallEchoVectorT(Vector<int> value)
    {
        return EchoVectorT(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<byte> RelaxedSwizzle(Vector128<byte> value, Vector128<byte> indices)
    {
        return RelaxedSimd.Swizzle(value, indices);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> RelaxedConvertF32ToInt32(Vector128<float> value)
    {
        return RelaxedSimd.ConvertToInt32(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<uint> RelaxedConvertF32ToUInt32(Vector128<float> value)
    {
        return RelaxedSimd.ConvertToUInt32(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> RelaxedConvertF64ToInt32(Vector128<double> value)
    {
        return RelaxedSimd.ConvertToInt32(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<uint> RelaxedConvertF64ToUInt32(Vector128<double> value)
    {
        return RelaxedSimd.ConvertToUInt32(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<float> RelaxedMultiplyAddF32(
        Vector128<float> left, Vector128<float> right, Vector128<float> addend)
    {
        return RelaxedSimd.MultiplyAddEstimate(left, right, addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<float> RelaxedMultiplyAddNegatedF32(
        Vector128<float> left, Vector128<float> right, Vector128<float> addend)
    {
        return RelaxedSimd.MultiplyAddNegatedEstimate(left, right, addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<double> RelaxedMultiplyAddF64(
        Vector128<double> left, Vector128<double> right, Vector128<double> addend)
    {
        return RelaxedSimd.MultiplyAddEstimate(left, right, addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<double> RelaxedMultiplyAddNegatedF64(
        Vector128<double> left, Vector128<double> right, Vector128<double> addend)
    {
        return RelaxedSimd.MultiplyAddNegatedEstimate(left, right, addend);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<byte> RelaxedLaneSelectI8(
        Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask)
    {
        return RelaxedSimd.LaneSelect(left, right, mask);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<short> RelaxedLaneSelectI16(
        Vector128<short> left, Vector128<short> right, Vector128<short> mask)
    {
        return RelaxedSimd.LaneSelect(left, right, mask);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> RelaxedLaneSelectI32(
        Vector128<int> left, Vector128<int> right, Vector128<int> mask)
    {
        return RelaxedSimd.LaneSelect(left, right, mask);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<long> RelaxedLaneSelectI64(
        Vector128<long> left, Vector128<long> right, Vector128<long> mask)
    {
        return RelaxedSimd.LaneSelect(left, right, mask);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<float> RelaxedMinF32(Vector128<float> left, Vector128<float> right)
    {
        return RelaxedSimd.Min(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<float> RelaxedMaxF32(Vector128<float> left, Vector128<float> right)
    {
        return RelaxedSimd.Max(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<double> RelaxedMinF64(Vector128<double> left, Vector128<double> right)
    {
        return RelaxedSimd.Min(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<double> RelaxedMaxF64(Vector128<double> left, Vector128<double> right)
    {
        return RelaxedSimd.Max(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<short> RelaxedMultiplyRoundedQ15(Vector128<short> left, Vector128<short> right)
    {
        return RelaxedSimd.MultiplyRoundedQ15(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<short> RelaxedDotProduct(Vector128<sbyte> left, Vector128<byte> right)
    {
        return RelaxedSimd.DotProduct(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> RelaxedDotProductAdd(
        Vector128<sbyte> left, Vector128<byte> right, Vector128<int> accumulator)
    {
        return RelaxedSimd.DotProductAdd(left, right, accumulator);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<byte> RelaxedSwizzleIfSupported(Vector128<byte> value, Vector128<byte> indices)
    {
        return RelaxedSimd.IsSupported ? RelaxedSimd.Swizzle(value, indices) : value;
    }

    // A single-field struct wrapping a v128 is itself passed/returned as a v128, matching emscripten.
    public struct WrappedVector128
    {
        public Vector128<int> Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVector128 EchoWrapped(WrappedVector128 value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVector128 CallEchoWrapped(WrappedVector128 value)
    {
        return EchoWrapped(value);
    }

    // The same unwrapping applies to a struct wrapping a 128-bit Vector<T>.
    public struct WrappedVectorT
    {
        public Vector<int> Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVectorT EchoWrappedVectorT(WrappedVectorT value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVectorT CallEchoWrappedVectorT(WrappedVectorT value)
    {
        return EchoWrappedVectorT(value);
    }
}
