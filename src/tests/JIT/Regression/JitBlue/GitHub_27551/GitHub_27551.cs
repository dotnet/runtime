// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_27551
{
    static int returnVal = 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte GetByte()
    {
        return 0xaa;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ValidateResult(Byte[] resultElements, Byte[] valueElements)
    {
        bool succeeded = true;

        if (resultElements.Length <= valueElements.Length)
        {
            for (var i = 0; i < resultElements.Length; i++)
            {
                if (resultElements[i] != valueElements[i])
                {
                    returnVal = -1;
                    break;
                }
            }
        }
        else
        {
            for (var i = 0; i < valueElements.Length; i++)
            {
                if (resultElements[i] != valueElements[i])
                {
                    returnVal = -1;
                    break;
                }
            }

            for (var i = valueElements.Length; i < resultElements.Length; i++)
            {
                if (resultElements[i] != default)
                {
                    returnVal = -1;
                    break;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateResult(Vector<Byte> result, Vector256<Byte> value)
    {
        Byte[] resultElements = new Byte[Vector<byte>.Count];
        Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);

        Byte[] valueElements = new Byte[Vector256<byte>.Count];
        Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref valueElements[0]), value);

        ValidateResult(resultElements, valueElements);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateResult(Vector256<Byte> result, Vector<Byte> value)
    {
        Byte[] resultElements = new Byte[Vector256<byte>.Count];
        Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);

        Byte[] valueElements = new Byte[Vector<byte>.Count];
        Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref valueElements[0]), value);

        ValidateResult(resultElements, valueElements);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test(Vector256<byte> value)
    {
        Vector<Byte> result = value.AsVector();
        ValidateResult(result, value);

        value = result.AsVector256();
        ValidateResult(value, result);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Vector256<Byte> value = Vector256.Create((byte)GetByte());
        Test(value);

        return returnVal;
    }
}
