// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

class Program
{
    // CompareEqual

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareEqual_Vector64_Byte_Zero(Vector64<byte> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<sbyte> AdvSimd_CompareEqual_Vector64_SByte_Zero(Vector64<sbyte> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<ushort> AdvSimd_CompareEqual_Vector64_UInt16_Zero(Vector64<ushort> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<ushort>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<short> AdvSimd_CompareEqual_Vector64_Int16_Zero(Vector64<short> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<uint> AdvSimd_CompareEqual_Vector64_UInt32_Zero(Vector64<uint> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<uint>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AdvSimd_CompareEqual_Vector64_Int32_Zero(Vector64<int> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareEqual_Vector64_Single_Zero(Vector64<float> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<float>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<byte> AdvSimd_CompareEqual_Vector128_Byte_Zero(Vector128<byte> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<byte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> AdvSimd_CompareEqual_Vector128_SByte_Zero(Vector128<sbyte> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<sbyte>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<ushort> AdvSimd_CompareEqual_Vector128_UInt16_Zero(Vector128<ushort> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<ushort>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> AdvSimd_CompareEqual_Vector128_Int16_Zero(Vector128<short> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<uint> AdvSimd_CompareEqual_Vector128_UInt32_Zero(Vector128<uint> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<uint>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AdvSimd_CompareEqual_Vector128_Int32_Zero(Vector128<int> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_Zero(Vector128<float> left)
    {
        return AdvSimd.CompareEqual(left, Vector128<float>.Zero);
    }

    // CompareEqual Swapped

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareEqual_Vector64_Byte_Zero_Swapped(Vector64<byte> right)
    {
        return AdvSimd.CompareEqual(Vector64<byte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<sbyte> AdvSimd_CompareEqual_Vector64_SByte_Zero_Swapped(Vector64<sbyte> right)
    {
        return AdvSimd.CompareEqual(Vector64<sbyte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<ushort> AdvSimd_CompareEqual_Vector64_UInt16_Zero_Swapped(Vector64<ushort> right)
    {
        return AdvSimd.CompareEqual(Vector64<ushort>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<short> AdvSimd_CompareEqual_Vector64_Int16_Zero_Swapped(Vector64<short> right)
    {
        return AdvSimd.CompareEqual(Vector64<short>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<uint> AdvSimd_CompareEqual_Vector64_UInt32_Zero_Swapped(Vector64<uint> right)
    {
        return AdvSimd.CompareEqual(Vector64<uint>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AdvSimd_CompareEqual_Vector64_Int32_Zero_Swapped(Vector64<int> right)
    {
        return AdvSimd.CompareEqual(Vector64<int>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> AdvSimd_CompareEqual_Vector64_Single_Zero_Swapped(Vector64<float> right)
    {
        return AdvSimd.CompareEqual(Vector64<float>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<byte> AdvSimd_CompareEqual_Vector128_Byte_Zero_Swapped(Vector128<byte> right)
    {
        return AdvSimd.CompareEqual(Vector128<byte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<sbyte> AdvSimd_CompareEqual_Vector128_SByte_Zero_Swapped(Vector128<sbyte> right)
    {
        return AdvSimd.CompareEqual(Vector128<sbyte>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<ushort> AdvSimd_CompareEqual_Vector128_UInt16_Zero_Swapped(Vector128<ushort> right)
    {
        return AdvSimd.CompareEqual(Vector128<ushort>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<short> AdvSimd_CompareEqual_Vector128_Int16_Zero_Swapped(Vector128<short> right)
    {
        return AdvSimd.CompareEqual(Vector128<short>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<uint> AdvSimd_CompareEqual_Vector128_UInt32_Zero_Swapped(Vector128<uint> right)
    {
        return AdvSimd.CompareEqual(Vector128<uint>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AdvSimd_CompareEqual_Vector128_Int32_Zero_Swapped(Vector128<int> right)
    {
        return AdvSimd.CompareEqual(Vector128<int>.Zero, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AdvSimd_CompareEqual_Vector128_Single_Zero_Swapped(Vector128<float> right)
    {
        return AdvSimd.CompareEqual(Vector128<float>.Zero, right);
    }

    // Validation

    unsafe static bool ValidateResult_Vector64<T>(Vector64<T> result, T expectedElementValue) where T: unmanaged
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

    unsafe static bool ValidateResult_Vector128<T>(Vector128<T> result, T expectedElementValue) where T: unmanaged
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

    static int Tests()
    {
        var result = 100;

        // Begin CompareEqual Tests

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

        // End CompareEqual Tests

        return result;
    }

    static int Tests_Swapped()
    {
        var result = 100;

        // Begin CompareEqual Tests

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

    static int Main(string[] args)
    {
        var result = 100;

        if (AdvSimd.IsSupported)
        {
            Console.WriteLine("Testing AdvSimd");

            result = Tests();
            if(result != -1)
            {
                result = Tests_Swapped();
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

        return result;
    }
}
