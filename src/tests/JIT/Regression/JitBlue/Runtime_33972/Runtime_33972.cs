// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Program
{
    // CompareEqual

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareEqual_Vector64_Byte_Zero(Vector64<byte> left)
    {
        // ARM64-FULL-LINE: cmeq v0.8b, v0.8b, #0
        return AdvSimd.CompareEqual(left, Vector64<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<sbyte> AdvSimd_CompareEqual_Vector64_SByte_Zero(Vector64<sbyte> left)
    {
        // ARM64-FULL-LINE: cmeq v0.8b, v0.8b, #0
        return AdvSimd.CompareEqual(left, Vector64<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<ushort> AdvSimd_CompareEqual_Vector64_UInt16_Zero(Vector64<ushort> left)
    {
        // ARM64-FULL-LINE: cmeq v0.4h, v0.4h, #0
        return AdvSimd.CompareEqual(left, Vector64<ushort>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<short> AdvSimd_CompareEqual_Vector64_Int16_Zero(Vector64<short> left)
    {
        // ARM64-FULL-LINE: cmeq v0.4h, v0.4h, #0
        return AdvSimd.CompareEqual(left, Vector64<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<uint> AdvSimd_CompareEqual_Vector64_UInt32_Zero(Vector64<uint> left)
    {
        // ARM64-FULL-LINE: cmeq v0.2s, v0.2s, #0
        return AdvSimd.CompareEqual(left, Vector64<uint>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AdvSimd_CompareEqual_Vector64_Int32_Zero(Vector64<int> left)
    {
        // ARM64-FULL-LINE: cmeq v0.2s, v0.2s, #0
        return AdvSimd.CompareEqual(left, Vector64<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareEqual_Vector64_Single_Zero(Vector64<float> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.2s, v0.2s, #0.0
        return AdvSimd.CompareEqual(left, Vector64<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AdvSimd_CompareEqual_Vector64_Int32_CreateZero(Vector64<int> left)
    {
        // ARM64-FULL-LINE: cmeq v0.2s, v0.2s, #0
        return AdvSimd.CompareEqual(left, Vector64.Create(0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AdvSimd_CompareEqual_Vector64_Int32_CreateZeroZero(Vector64<int> left)
    {
        // ARM64-FULL-LINE: cmeq v0.2s, v0.2s, #0
        return AdvSimd.CompareEqual(left, Vector64.Create(0, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareEqual_Vector64_Single_CreateZero(Vector64<float> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.2s, v0.2s, #0.0
        return AdvSimd.CompareEqual(left, Vector64.Create(0f));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareEqual_Vector64_Single_CreateZeroZero(Vector64<float> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.2s, v0.2s, #0.0
        return AdvSimd.CompareEqual(left, Vector64.Create(0f, 0f));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<byte> AdvSimd_CompareEqual_Vector128_Byte_Zero(Vector128<byte> left)
    {
        // ARM64-FULL-LINE: cmeq v0.16b, v0.16b, #0
        return AdvSimd.CompareEqual(left, Vector128<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> AdvSimd_CompareEqual_Vector128_SByte_Zero(Vector128<sbyte> left)
    {
        // ARM64-FULL-LINE: cmeq v0.16b, v0.16b, #0
        return AdvSimd.CompareEqual(left, Vector128<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<ushort> AdvSimd_CompareEqual_Vector128_UInt16_Zero(Vector128<ushort> left)
    {
        // ARM64-FULL-LINE: cmeq v0.8h, v0.8h, #0
        return AdvSimd.CompareEqual(left, Vector128<ushort>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> AdvSimd_CompareEqual_Vector128_Int16_Zero(Vector128<short> left)
    {
        // ARM64-FULL-LINE: cmeq v0.8h, v0.8h, #0
        return AdvSimd.CompareEqual(left, Vector128<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<uint> AdvSimd_CompareEqual_Vector128_UInt32_Zero(Vector128<uint> left)
    {
        // ARM64-FULL-LINE: cmeq v0.4s, v0.4s, #0
        return AdvSimd.CompareEqual(left, Vector128<uint>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AdvSimd_CompareEqual_Vector128_Int32_Zero(Vector128<int> left)
    {
        // ARM64-FULL-LINE: cmeq v0.4s, v0.4s, #0
        return AdvSimd.CompareEqual(left, Vector128<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_Zero(Vector128<float> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.4s, v0.4s, #0.0
        return AdvSimd.CompareEqual(left, Vector128<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AdvSimd_CompareEqual_Vector128_Int32_CreateZero(Vector128<int> left)
    {
        // ARM64-FULL-LINE: cmeq v0.4s, v0.4s, #0
        return AdvSimd.CompareEqual(left, Vector128.Create(0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AdvSimd_CompareEqual_Vector128_Int32_CreateZeroZeroZeroZero(Vector128<int> left)
    {
        // ARM64-FULL-LINE: cmeq v0.4s, v0.4s, #0
        return AdvSimd.CompareEqual(left, Vector128.Create(0, 0, 0, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_CreateZero(Vector128<float> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.4s, v0.4s, #0.0
        return AdvSimd.CompareEqual(left, Vector128.Create(0f));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_CreateZeroZeroZeroZero(Vector128<float> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.4s, v0.4s, #0.0
        return AdvSimd.CompareEqual(left, Vector128.Create(0f, 0f, 0f, 0f));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_CreateZeroZeroZeroZero_AsVariable(Vector128<float> left)
    {
        // 'asVar' should be propagated.
        // ARM64-FULL-LINE: fcmeq v0.4s, v0.4s, #0.0
        var asVar = Vector128.Create(0f, 0f, 0f, 0f);
        return AdvSimd.CompareEqual(left, asVar);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_CreateZeroZeroZeroZero_AsVariableLoop(Vector128<float> left)
    {
        Vector128<float> result = default;
        var asVar = Vector128.Create(0f, 0f, 0f, 0f);

        for (var i = 0; i < 4; i++)
        {
            result = AdvSimd.CompareEqual(left, asVar);
            result = AdvSimd.CompareEqual(left, asVar);
            result = AdvSimd.CompareEqual(left, asVar);
            result = AdvSimd.CompareEqual(left, asVar);

            for (var j = 0; j < 4; j++)
            {
                result = AdvSimd.CompareEqual(left, asVar);
                result = AdvSimd.CompareEqual(left, asVar);
                result = AdvSimd.CompareEqual(left, asVar);
                result = AdvSimd.CompareEqual(left, asVar);
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector128<long> AdvSimd_Arm64_CompareEqual_Vector128_Long_AsVariableLoop(Vector128<long> left)
    {
        Vector128<long> result = default;
        Vector128<long> asVar = Vector128.Create((long)0);
        Vector128<nint> asVar2 = Vector128.Create((nint)0);
        Vector128<long> asVar3 = asVar2.AsInt64();

        for (var i = 0; i < 4; i++)
        {
            result = AdvSimd.Arm64.CompareEqual(left, asVar);

            for (var j = 0; j < 4; j++)
            {
                result = AdvSimd.Arm64.CompareEqual(left, asVar3);
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> AdvSimd_Arm64_CompareEqual_Vector128_Double_Zero(Vector128<double> left)
    {
        // ARM64-FULL-LINE: fcmeq v0.2d, v0.2d, #0.0
        return AdvSimd.Arm64.CompareEqual(left, Vector128<double>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<ulong> AdvSimd_Arm64_CompareEqual_Vector128_UInt64_Zero(Vector128<ulong> left)
    {
        // ARM64-FULL-LINE: cmeq v0.2d, v0.2d, #0
        return AdvSimd.Arm64.CompareEqual(left, Vector128<ulong>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> AdvSimd_Arm64_CompareEqual_Vector128_Int64_Zero(Vector128<long> left)
    {
        // ARM64-FULL-LINE: cmeq v0.2d, v0.2d, #0
        return AdvSimd.Arm64.CompareEqual(left, Vector128<long>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_Arm64_CompareEqualScalar_Vector64_Single_Zero(Vector64<float> left)
    {
        // ARM64-FULL-LINE: fcmeq s0, s0, #0.0
        return AdvSimd.Arm64.CompareEqualScalar(left, Vector64<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<double> AdvSimd_Arm64_CompareEqualScalar_Vector64_Double_Zero(Vector64<double> left)
    {
        // ARM64-FULL-LINE: fcmeq d0, d0, #0.0
        return AdvSimd.Arm64.CompareEqualScalar(left, Vector64<double>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<ulong> AdvSimd_Arm64_CompareEqualScalar_Vector64_UInt64_Zero(Vector64<ulong> left)
    {
        // ARM64-FULL-LINE: cmeq d0, d0, #0
        return AdvSimd.Arm64.CompareEqualScalar(left, Vector64<ulong>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<long> AdvSimd_Arm64_CompareEqualScalar_Vector64_Int64_Zero(Vector64<long> left)
    {
        // ARM64-FULL-LINE: cmeq d0, d0, #0
        return AdvSimd.Arm64.CompareEqualScalar(left, Vector64<long>.Zero);
    }

    // CompareEqual Swapped

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareEqual_Vector64_Byte_Zero_Swapped(Vector64<byte> right)
    {
        // ARM64-FULL-LINE: cmeq v0.8b, v0.8b, #0
        return AdvSimd.CompareEqual(Vector64<byte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<sbyte> AdvSimd_CompareEqual_Vector64_SByte_Zero_Swapped(Vector64<sbyte> right)
    {
        // ARM64-FULL-LINE: cmeq v0.8b, v0.8b, #0
        return AdvSimd.CompareEqual(Vector64<sbyte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<ushort> AdvSimd_CompareEqual_Vector64_UInt16_Zero_Swapped(Vector64<ushort> right)
    {
        // ARM64-FULL-LINE: cmeq v0.4h, v0.4h, #0
        return AdvSimd.CompareEqual(Vector64<ushort>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<short> AdvSimd_CompareEqual_Vector64_Int16_Zero_Swapped(Vector64<short> right)
    {
        // ARM64-FULL-LINE: cmeq v0.4h, v0.4h, #0
        return AdvSimd.CompareEqual(Vector64<short>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<uint> AdvSimd_CompareEqual_Vector64_UInt32_Zero_Swapped(Vector64<uint> right)
    {
        // ARM64-FULL-LINE: cmeq v0.2s, v0.2s, #0
        return AdvSimd.CompareEqual(Vector64<uint>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AdvSimd_CompareEqual_Vector64_Int32_Zero_Swapped(Vector64<int> right)
    {
        // ARM64-FULL-LINE: cmeq v0.2s, v0.2s, #0
        return AdvSimd.CompareEqual(Vector64<int>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareEqual_Vector64_Single_Zero_Swapped(Vector64<float> right)
    {
        // ARM64-FULL-LINE: fcmeq v0.2s, v0.2s, #0.0
        return AdvSimd.CompareEqual(Vector64<float>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<byte> AdvSimd_CompareEqual_Vector128_Byte_Zero_Swapped(Vector128<byte> right)
    {
        // ARM64-FULL-LINE: cmeq v0.16b, v0.16b, #0
        return AdvSimd.CompareEqual(Vector128<byte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> AdvSimd_CompareEqual_Vector128_SByte_Zero_Swapped(Vector128<sbyte> right)
    {
        // ARM64-FULL-LINE: cmeq v0.16b, v0.16b, #0
        return AdvSimd.CompareEqual(Vector128<sbyte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<ushort> AdvSimd_CompareEqual_Vector128_UInt16_Zero_Swapped(Vector128<ushort> right)
    {
        // ARM64-FULL-LINE: cmeq v0.8h, v0.8h, #0
        return AdvSimd.CompareEqual(Vector128<ushort>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> AdvSimd_CompareEqual_Vector128_Int16_Zero_Swapped(Vector128<short> right)
    {
        // ARM64-FULL-LINE: cmeq v0.8h, v0.8h, #0
        return AdvSimd.CompareEqual(Vector128<short>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<uint> AdvSimd_CompareEqual_Vector128_UInt32_Zero_Swapped(Vector128<uint> right)
    {
        // ARM64-FULL-LINE: cmeq v0.4s, v0.4s, #0
        return AdvSimd.CompareEqual(Vector128<uint>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AdvSimd_CompareEqual_Vector128_Int32_Zero_Swapped(Vector128<int> right)
    {
        // ARM64-FULL-LINE: cmeq v0.4s, v0.4s, #0
        return AdvSimd.CompareEqual(Vector128<int>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_Zero_Swapped(Vector128<float> right)
    {
        // ARM64-FULL-LINE: fcmeq v0.4s, v0.4s, #0.0
        return AdvSimd.CompareEqual(Vector128<float>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> AdvSimd_Arm64_CompareEqual_Vector128_Double_Zero_Swapped(Vector128<double> right)
    {
        // ARM64-FULL-LINE: fcmeq v0.2d, v0.2d, #0.0
        return AdvSimd.Arm64.CompareEqual(Vector128<double>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<ulong> AdvSimd_Arm64_CompareEqual_Vector128_UInt64_Zero_Swapped(Vector128<ulong> right)
    {
        // ARM64-FULL-LINE: cmeq v0.2d, v0.2d, #0
        return AdvSimd.Arm64.CompareEqual(Vector128<ulong>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> AdvSimd_Arm64_CompareEqual_Vector128_Int64_Zero_Swapped(Vector128<long> right)
    {
        // ARM64-FULL-LINE: cmeq v0.2d, v0.2d, #0
        return AdvSimd.Arm64.CompareEqual(Vector128<long>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_Arm64_CompareEqualScalar_Vector64_Single_Zero_Swapped(Vector64<float> right)
    {
        // ARM64-FULL-LINE: fcmeq s0, s0, #0.0
        return AdvSimd.Arm64.CompareEqualScalar(Vector64<float>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<double> AdvSimd_Arm64_CompareEqualScalar_Vector64_Double_Zero_Swapped(Vector64<double> right)
    {
        // ARM64-FULL-LINE: fcmeq d0, d0, #0.0
        return AdvSimd.Arm64.CompareEqualScalar(Vector64<double>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<ulong> AdvSimd_Arm64_CompareEqualScalar_Vector64_UInt64_Zero_Swapped(Vector64<ulong> right)
    {
        // ARM64-FULL-LINE: cmeq d0, d0, #0
        return AdvSimd.Arm64.CompareEqualScalar(Vector64<ulong>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<long> AdvSimd_Arm64_CompareEqualScalar_Vector64_Int64_Zero_Swapped(Vector64<long> right)
    {
        // ARM64-FULL-LINE: cmeq d0, d0, #0
        return AdvSimd.Arm64.CompareEqualScalar(Vector64<long>.Zero, right);
    }

    // CompareGreaterThan

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareGreaterThan_Vector64_Byte_Zero(Vector64<byte> left)
    {
        // ARM64-FULL-LINE:      movi {{v[0-9]+}}.2s, #0
        // ARM64-FULL-LINE-NEXT: cmhi {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
        return AdvSimd.CompareGreaterThan(left, Vector64<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareGreaterThan_Vector64_Single_Zero(Vector64<float> left)
    {
        // ARM64-FULL-LINE: fcmgt v0.2s, v0.2s, #0.0
        return AdvSimd.CompareGreaterThan(left, Vector64<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<byte> AdvSimd_CompareGreaterThan_Vector128_Byte_Zero(Vector128<byte> left)
    {
        // ARM64-FULL-LINE:      movi {{v[0-9]+}}.4s, #0
        // ARM64-FULL-LINE-NEXT: cmhi {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
        return AdvSimd.CompareGreaterThan(left, Vector128<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareGreaterThan_Vector128_Single_Zero(Vector128<float> left)
    {
        // ARM64-FULL-LINE: fcmgt v0.4s, v0.4s, #0.0
        return AdvSimd.CompareGreaterThan(left, Vector128<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> AdvSimd_Arm64_CompareGreaterThan_Vector128_Double_Zero(Vector128<double> left)
    {
        // ARM64-FULL-LINE: fcmgt v0.2d, v0.2d, #0.0
        return AdvSimd.Arm64.CompareGreaterThan(left, Vector128<double>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> AdvSimd_Arm64_CompareGreaterThan_Vector128_Int64_Zero(Vector128<long> left)
    {
        // ARM64-FULL-LINE: cmgt v0.2d, v0.2d, #0
        return AdvSimd.Arm64.CompareGreaterThan(left, Vector128<long>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<double> AdvSimd_Arm64_CompareGreaterThanScalar_Vector64_Double_Zero(Vector64<double> left)
    {
        // ARM64-FULL-LINE: fcmgt d0, d0, #0.0
        return AdvSimd.Arm64.CompareGreaterThanScalar(left, Vector64<double>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<long> AdvSimd_Arm64_CompareGreaterThanScalar_Vector64_Int64_Zero(Vector64<long> left)
    {
        // ARM64-FULL-LINE: cmgt d0, d0, #0
        return AdvSimd.Arm64.CompareGreaterThanScalar(left, Vector64<long>.Zero);
    }

    // CompareGreaterThanOrEqual

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareGreaterThanOrEqual_Vector64_Byte_Zero(Vector64<byte> left)
    {
        // ARM64-FULL-LINE:      movi {{v[0-9]+}}.2s, #0
        // ARM64-FULL-LINE-NEXT: cmhs {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
        return AdvSimd.CompareGreaterThanOrEqual(left, Vector64<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareGreaterThanOrEqual_Vector64_Single_Zero(Vector64<float> left)
    {
        // ARM64-FULL-LINE: fcmge v0.2s, v0.2s, #0.0
        return AdvSimd.CompareGreaterThanOrEqual(left, Vector64<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<byte> AdvSimd_CompareGreaterThanOrEqual_Vector128_Byte_Zero(Vector128<byte> left)
    {
        // ARM64-FULL-LINE:      movi {{v[0-9]+}}.4s, #0
        // ARM64-FULL-LINE-NEXT: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
        return AdvSimd.CompareGreaterThanOrEqual(left, Vector128<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareGreaterThanOrEqual_Vector128_Single_Zero(Vector128<float> left)
    {
        // ARM64-FULL-LINE: fcmge v0.4s, v0.4s, #0.0
        return AdvSimd.CompareGreaterThanOrEqual(left, Vector128<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> AdvSimd_Arm64_CompareGreaterThanOrEqual_Vector128_Double_Zero(Vector128<double> left)
    {
        // ARM64-FULL-LINE: fcmge v0.2d, v0.2d, #0.0
        return AdvSimd.Arm64.CompareGreaterThanOrEqual(left, Vector128<double>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<long> AdvSimd_Arm64_CompareGreaterThanOrEqual_Vector128_Int64_Zero(Vector128<long> left)
    {
        // ARM64-FULL-LINE: cmge v0.2d, v0.2d, #0
        return AdvSimd.Arm64.CompareGreaterThanOrEqual(left, Vector128<long>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<double> AdvSimd_Arm64_CompareGreaterThanOrEqualScalar_Vector64_Double_Zero(Vector64<double> left)
    {
        // ARM64-FULL-LINE: fcmge d0, d0, #0.0
        return AdvSimd.Arm64.CompareGreaterThanOrEqualScalar(left, Vector64<double>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<long> AdvSimd_Arm64_CompareGreaterThanOrEqualScalar_Vector64_Int64_Zero(Vector64<long> left)
    {
        // ARM64-FULL-LINE: cmge d0, d0, #0
        return AdvSimd.Arm64.CompareGreaterThanOrEqualScalar(left, Vector64<long>.Zero);
    }

    // Validation

    unsafe static bool ValidateResult_Vector64<T>(Vector64<T> result, T expectedElementValue) where T : unmanaged
    {
        var succeeded = true;

        for (var i = 0; i < (8 / sizeof(T)); i++)
        {
            if (!result.GetElement(i).Equals(expectedElementValue))
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    unsafe static bool ValidateResult_Vector64<T>(Vector64<T> result, Vector64<T> expectedElementValue) where T : unmanaged
    {
        var succeeded = true;

        for (var i = 0; i < (8 / sizeof(T)); i++)
        {
            if (!result.GetElement(i).Equals(expectedElementValue.GetElement(i)))
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    unsafe static bool ValidateResult_Vector128<T>(Vector128<T> result, T expectedElementValue) where T : unmanaged
    {
        var succeeded = true;

        for (var i = 0; i < (16 / sizeof(T)); i++)
        {
            if (!result.GetElement(i).Equals(expectedElementValue))
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    unsafe static bool ValidateResult_Vector128<T>(Vector128<T> result, Vector128<T> expectedElementValue) where T : unmanaged
    {
        var succeeded = true;

        for (var i = 0; i < (16 / sizeof(T)); i++)
        {
            if (!result.GetElement(i).Equals(expectedElementValue.GetElement(i)))
            {
                succeeded = false;
            }
        }

        return succeeded;
    }

    static int Tests_AdvSimd()
    {
        var result = 100;

        // Begin CompareEqual Tests

        // Vector64

        if (!ValidateResult_Vector64<byte>(AdvSimd_CompareEqual_Vector64_Byte_Zero(Vector64<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<sbyte>(AdvSimd_CompareEqual_Vector64_SByte_Zero(Vector64<sbyte>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<ushort>(AdvSimd_CompareEqual_Vector64_UInt16_Zero(Vector64<ushort>.Zero), UInt16.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<short>(AdvSimd_CompareEqual_Vector64_Int16_Zero(Vector64<short>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<uint>(AdvSimd_CompareEqual_Vector64_UInt32_Zero(Vector64<uint>.Zero), UInt32.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<int>(AdvSimd_CompareEqual_Vector64_Int32_Zero(Vector64<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareEqual_Vector64_Single_Zero(Vector64<float>.Zero), Single.NaN))
            result = -1;

        // Vector64.Create

        if (!ValidateResult_Vector64<int>(AdvSimd_CompareEqual_Vector64_Int32_CreateZero(Vector64<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareEqual_Vector64_Single_CreateZero(Vector64<float>.Zero), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector64<int>(AdvSimd_CompareEqual_Vector64_Int32_CreateZeroZero(Vector64<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareEqual_Vector64_Single_CreateZeroZero(Vector64<float>.Zero), Single.NaN))
            result = -1;

        // Vector128

        if (!ValidateResult_Vector128<byte>(AdvSimd_CompareEqual_Vector128_Byte_Zero(Vector128<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<sbyte>(AdvSimd_CompareEqual_Vector128_SByte_Zero(Vector128<sbyte>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<ushort>(AdvSimd_CompareEqual_Vector128_UInt16_Zero(Vector128<ushort>.Zero), UInt16.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<short>(AdvSimd_CompareEqual_Vector128_Int16_Zero(Vector128<short>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<uint>(AdvSimd_CompareEqual_Vector128_UInt32_Zero(Vector128<uint>.Zero), UInt32.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<int>(AdvSimd_CompareEqual_Vector128_Int32_Zero(Vector128<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareEqual_Vector128_Single_Zero(Vector128<float>.Zero), Single.NaN))
            result = -1;

        // Vector128.Create

        if (!ValidateResult_Vector128<int>(AdvSimd_CompareEqual_Vector128_Int32_CreateZero(Vector128<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareEqual_Vector128_Single_CreateZero(Vector128<float>.Zero), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<int>(AdvSimd_CompareEqual_Vector128_Int32_CreateZeroZeroZeroZero(Vector128<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareEqual_Vector128_Single_CreateZeroZeroZeroZero(Vector128<float>.Zero), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareEqual_Vector128_Single_CreateZeroZeroZeroZero_AsVariable(Vector128<float>.Zero), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareEqual_Vector128_Single_CreateZeroZeroZeroZero_AsVariableLoop(Vector128<float>.Zero), Single.NaN))
            result = -1;

        // End CompareEqual Tests

        // Begin CompareGreaterThan Tests

        if (!ValidateResult_Vector64<byte>(AdvSimd_CompareGreaterThan_Vector64_Byte_Zero(Vector64.Create((byte)1)), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareGreaterThan_Vector64_Single_Zero(Vector64.Create(1.0f)), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<byte>(AdvSimd_CompareGreaterThan_Vector128_Byte_Zero(Vector128.Create((byte)1)), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareGreaterThan_Vector128_Single_Zero(Vector128.Create(1.0f)), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<double>(AdvSimd_Arm64_CompareGreaterThan_Vector128_Double_Zero(Vector128.Create(1.0)), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareGreaterThan_Vector128_Int64_Zero(Vector128.Create(1L)), -1))
            result = -1;

        if (!ValidateResult_Vector64<double>(AdvSimd_Arm64_CompareGreaterThanScalar_Vector64_Double_Zero(Vector64.Create(1.0)), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector64<long>(AdvSimd_Arm64_CompareGreaterThanScalar_Vector64_Int64_Zero(Vector64.Create(1L)), -1))
            result = -1;

        if (ValidateResult_Vector64<byte>(AdvSimd_CompareGreaterThan_Vector64_Byte_Zero(Vector64<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (ValidateResult_Vector64<float>(AdvSimd_CompareGreaterThan_Vector64_Single_Zero(Vector64<float>.Zero), Single.NaN))
            result = -1;

        if (ValidateResult_Vector128<byte>(AdvSimd_CompareGreaterThan_Vector128_Byte_Zero(Vector128<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (ValidateResult_Vector128<float>(AdvSimd_CompareGreaterThan_Vector128_Single_Zero(Vector128<float>.Zero), Single.NaN))
            result = -1;

        if (ValidateResult_Vector128<double>(AdvSimd_Arm64_CompareGreaterThan_Vector128_Double_Zero(Vector128<double>.Zero), Double.NaN))
            result = -1;

        if (ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareGreaterThan_Vector128_Int64_Zero(Vector128<long>.Zero), -1))
            result = -1;

        if (ValidateResult_Vector64<double>(AdvSimd_Arm64_CompareGreaterThanScalar_Vector64_Double_Zero(Vector64<double>.Zero), Double.NaN))
            result = -1;

        if (ValidateResult_Vector64<long>(AdvSimd_Arm64_CompareGreaterThanScalar_Vector64_Int64_Zero(Vector64<long>.Zero), -1))
            result = -1;

        // End CompareGreaterThan Tests

        // Begin CompareGreaterThanOrEqual Tests

        if (!ValidateResult_Vector64<byte>(AdvSimd_CompareGreaterThanOrEqual_Vector64_Byte_Zero(Vector64.Create((byte)1)), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareGreaterThanOrEqual_Vector64_Single_Zero(Vector64.Create(1.0f)), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<byte>(AdvSimd_CompareGreaterThanOrEqual_Vector128_Byte_Zero(Vector128.Create((byte)1)), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareGreaterThanOrEqual_Vector128_Single_Zero(Vector128.Create(1.0f)), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<double>(AdvSimd_Arm64_CompareGreaterThanOrEqual_Vector128_Double_Zero(Vector128.Create(1.0)), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareGreaterThanOrEqual_Vector128_Int64_Zero(Vector128.Create(1L)), -1))
            result = -1;

        if (!ValidateResult_Vector64<double>(AdvSimd_Arm64_CompareGreaterThanOrEqualScalar_Vector64_Double_Zero(Vector64.Create(1.0)), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector64<long>(AdvSimd_Arm64_CompareGreaterThanOrEqualScalar_Vector64_Int64_Zero(Vector64.Create(1L)), -1))
            result = -1;

        if (!ValidateResult_Vector64<byte>(AdvSimd_CompareGreaterThanOrEqual_Vector64_Byte_Zero(Vector64<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareGreaterThanOrEqual_Vector64_Single_Zero(Vector64<float>.Zero), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<byte>(AdvSimd_CompareGreaterThanOrEqual_Vector128_Byte_Zero(Vector128<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareGreaterThanOrEqual_Vector128_Single_Zero(Vector128<float>.Zero), Single.NaN))
            result = -1;

        if (!ValidateResult_Vector128<double>(AdvSimd_Arm64_CompareGreaterThanOrEqual_Vector128_Double_Zero(Vector128<double>.Zero), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareGreaterThanOrEqual_Vector128_Int64_Zero(Vector128<long>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<double>(AdvSimd_Arm64_CompareGreaterThanOrEqualScalar_Vector64_Double_Zero(Vector64<double>.Zero), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector64<long>(AdvSimd_Arm64_CompareGreaterThanOrEqualScalar_Vector64_Int64_Zero(Vector64<long>.Zero), -1))
            result = -1;

        // End CompareGreaterThanOrEqual Tests

        return result;
    }

    static int Tests_AdvSimd_Swapped()
    {
        var result = 100;

        // Begin CompareEqual Tests

        // Vector64

        if (!ValidateResult_Vector64<byte>(AdvSimd_CompareEqual_Vector64_Byte_Zero_Swapped(Vector64<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<sbyte>(AdvSimd_CompareEqual_Vector64_SByte_Zero_Swapped(Vector64<sbyte>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<ushort>(AdvSimd_CompareEqual_Vector64_UInt16_Zero_Swapped(Vector64<ushort>.Zero), UInt16.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<short>(AdvSimd_CompareEqual_Vector64_Int16_Zero_Swapped(Vector64<short>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<uint>(AdvSimd_CompareEqual_Vector64_UInt32_Zero_Swapped(Vector64<uint>.Zero), UInt32.MaxValue))
            result = -1;

        if (!ValidateResult_Vector64<int>(AdvSimd_CompareEqual_Vector64_Int32_Zero_Swapped(Vector64<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector64<float>(AdvSimd_CompareEqual_Vector64_Single_Zero_Swapped(Vector64<float>.Zero), Single.NaN))
            result = -1;

        // Vector128

        if (!ValidateResult_Vector128<byte>(AdvSimd_CompareEqual_Vector128_Byte_Zero_Swapped(Vector128<byte>.Zero), Byte.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<sbyte>(AdvSimd_CompareEqual_Vector128_SByte_Zero_Swapped(Vector128<sbyte>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<ushort>(AdvSimd_CompareEqual_Vector128_UInt16_Zero_Swapped(Vector128<ushort>.Zero), UInt16.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<short>(AdvSimd_CompareEqual_Vector128_Int16_Zero_Swapped(Vector128<short>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<uint>(AdvSimd_CompareEqual_Vector128_UInt32_Zero_Swapped(Vector128<uint>.Zero), UInt32.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<int>(AdvSimd_CompareEqual_Vector128_Int32_Zero_Swapped(Vector128<int>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<float>(AdvSimd_CompareEqual_Vector128_Single_Zero_Swapped(Vector128<float>.Zero), Single.NaN))
            result = -1;

        // End CompareEqual Tests

        return result;
    }

    static int Tests_AdvSimd_Arm64()
    {
        var result = 100;

        // Begin CompareEqual Tests

        // Vector128

        if (!ValidateResult_Vector128<double>(AdvSimd_Arm64_CompareEqual_Vector128_Double_Zero(Vector128<double>.Zero), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector128<ulong>(AdvSimd_Arm64_CompareEqual_Vector128_UInt64_Zero(Vector128<ulong>.Zero), UInt64.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareEqual_Vector128_Int64_Zero(Vector128<long>.Zero), -1))
            result = -1;

        if (!ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareEqual_Vector128_Long_AsVariableLoop(Vector128<long>.Zero), -1))
            result = -1;

        // Vector64

        if (!ValidateResult_Vector64<float>(AdvSimd_Arm64_CompareEqualScalar_Vector64_Single_Zero(Vector64<float>.Zero), Vector64.CreateScalar(Single.NaN)))
            result = -1;

        if (!ValidateResult_Vector64<double>(AdvSimd_Arm64_CompareEqualScalar_Vector64_Double_Zero(Vector64<double>.Zero), Vector64.CreateScalar(Double.NaN)))
            result = -1;

        if (!ValidateResult_Vector64<ulong>(AdvSimd_Arm64_CompareEqualScalar_Vector64_UInt64_Zero(Vector64<ulong>.Zero), Vector64.CreateScalar(UInt64.MaxValue)))
            result = -1;

        if (!ValidateResult_Vector64<long>(AdvSimd_Arm64_CompareEqualScalar_Vector64_Int64_Zero(Vector64<long>.Zero), Vector64.CreateScalar(-1L)))
            result = -1;

        // End CompareEqual Tests

        return result;
    }

    static int Tests_AdvSimd_Arm64_Swapped()
    {
        var result = 100;

        // Begin CompareEqual Tests

        // Vector128

        if (!ValidateResult_Vector128<double>(AdvSimd_Arm64_CompareEqual_Vector128_Double_Zero_Swapped(Vector128<double>.Zero), Double.NaN))
            result = -1;

        if (!ValidateResult_Vector128<ulong>(AdvSimd_Arm64_CompareEqual_Vector128_UInt64_Zero_Swapped(Vector128<ulong>.Zero), UInt64.MaxValue))
            result = -1;

        if (!ValidateResult_Vector128<long>(AdvSimd_Arm64_CompareEqual_Vector128_Int64_Zero_Swapped(Vector128<long>.Zero), -1))
            result = -1;

        // Vector64

        if (!ValidateResult_Vector64<float>(AdvSimd_Arm64_CompareEqualScalar_Vector64_Single_Zero_Swapped(Vector64<float>.Zero), Vector64.CreateScalar(Single.NaN)))
            result = -1;

        if (!ValidateResult_Vector64<double>(AdvSimd_Arm64_CompareEqualScalar_Vector64_Double_Zero_Swapped(Vector64<double>.Zero), Vector64.CreateScalar(Double.NaN)))
            result = -1;

        if (!ValidateResult_Vector64<ulong>(AdvSimd_Arm64_CompareEqualScalar_Vector64_UInt64_Zero_Swapped(Vector64<ulong>.Zero), Vector64.CreateScalar(UInt64.MaxValue)))
            result = -1;

        if (!ValidateResult_Vector64<long>(AdvSimd_Arm64_CompareEqualScalar_Vector64_Int64_Zero_Swapped(Vector64<long>.Zero), Vector64.CreateScalar(-1L)))
            result = -1;

        // End CompareEqual Tests

        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var result = 100;

        if (AdvSimd.IsSupported)
        {
            Console.WriteLine("Testing AdvSimd");

            if (result != -1)
            {
                result = Tests_AdvSimd();
            }
            if (result != -1)
            {
                result = Tests_AdvSimd_Swapped();
            }

            if (result == -1)
            {
                Console.WriteLine("AdvSimd Tests Failed");
            }
            else
            {
                Console.WriteLine("AdvSimd Tests Passed");
            }
        }
        else
        {
            Console.WriteLine("Skipped AdvSimd Tests");
        }

        if (AdvSimd.Arm64.IsSupported)
        {
            Console.WriteLine("Testing AdvSimd_Arm64");

            if (result != -1)
            {
                result = Tests_AdvSimd_Arm64();
            }
            if (result != -1)
            {
                result = Tests_AdvSimd_Arm64_Swapped();
            }

            if (result == -1)
            {
                Console.WriteLine("AdvSimd_Arm64 Tests Failed");
            }
            else
            {
                Console.WriteLine("AdvSimd_Arm64 Tests Passed");
            }
        }
        else
        {
            Console.WriteLine("Skipped AdvSimd_Arm64 Tests");
        }

        return result;
    }
}
