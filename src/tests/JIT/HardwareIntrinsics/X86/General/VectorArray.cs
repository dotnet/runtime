// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test case is ported from S.N.Vector counterpart
// https://github.com/dotnet/runtime/blob/main/src/tests/JIT/SIMD/VectorArray.cs

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

namespace IntelHardwareIntrinsicTest.General;
public partial class Program
{
    private class Vector128ArrayTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        private static void Move(Vector128<T>[] pos, ref Vector128<T> delta)
        {
            for (int i = 0; i < pos.Length; ++i)
            {
                pos[i] = Vector128Add<T>(pos[i], delta);
            }
        }

        static public unsafe int Vector128Array()
        {
            Vector128<T>[] v = new Vector128<T>[3];
            int elementSize = Unsafe.SizeOf<T>();
            const int vectorSize = 16;
            int elementCount = vectorSize / elementSize;

            for (int i = 0; i < v.Length; ++i)
                v[i] = CreateVector128<T>(GetValueFromInt<T>(i + 1));

            Vector128<T> delta = CreateVector128<T>(GetValueFromInt<T>(1));
            Move(v, ref delta);

            byte* buffer = stackalloc byte[vectorSize * v.Length];
            for (int i = 0; i < v.Length; ++i)
                Unsafe.Write<Vector128<T>>(buffer + i * vectorSize, v[i]);

            for (int i = 0; i < v.Length; i++)
            {
                T checkValue = GetValueFromInt<T>(i + 2);
                for (int j = 0; j < elementCount; j++)
                {
                    if (!CheckValue<T>(Unsafe.Read<T>(&buffer[i * vectorSize + j * elementSize]), checkValue)) return Fail;
                }
            }

            return Pass;
        }
    }

    private class Vector256ArrayTest<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        private static void Move(Vector256<T>[] pos, ref Vector256<T> delta)
        {
            for (int i = 0; i < pos.Length; ++i)
            {
                pos[i] = Vector256Add<T>(pos[i], delta);
            }
        }

        static public unsafe int Vector256Array()
        {
            Vector256<T>[] v = new Vector256<T>[3];
            int elementSize = Unsafe.SizeOf<T>();
            const int vectorSize = 32;
            int elementCount = vectorSize / elementSize;

            for (int i = 0; i < v.Length; ++i)
                v[i] = CreateVector256<T>((T)Convert.ChangeType(i + 1, typeof(T)));

            Vector256<T> delta = CreateVector256((T)Convert.ChangeType(1, typeof(T)));
            Move(v, ref delta);

            byte* buffer = stackalloc byte[vectorSize * v.Length];
            for (int i = 0; i < v.Length; ++i)
                Unsafe.Write<Vector256<T>>(buffer + i * vectorSize, v[i]);

            for (int i = 0; i < v.Length; i++)
            {
                T checkValue = GetValueFromInt<T>(i + 2);
                for (int j = 0; j < elementCount; j++)
                {
                    if (!CheckValue<T>(Unsafe.Read<T>(&buffer[i * vectorSize + j * elementSize]), checkValue)) return Fail;
                }
            }

            return Pass;
        }
    }

    [Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/75767", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoLLVMAOT))]
    [Fact]
    public unsafe static void VectorArray()
    {
        int returnVal = Pass;
        try
        {
            if (Sse2.IsSupported)
            {
                if (Vector128ArrayTest<float>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<double>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<byte>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<sbyte>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<short>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<ushort>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<int>.Vector128Array() != Pass) returnVal = Fail;
                if (Vector128ArrayTest<uint>.Vector128Array() != Pass) returnVal = Fail;
                if (Environment.Is64BitProcess)
                {
                    if (Vector128ArrayTest<long>.Vector128Array() != Pass) returnVal = Fail;
                    if (Vector128ArrayTest<ulong>.Vector128Array() != Pass) returnVal = Fail;
                }
            }

            if (Avx2.IsSupported)
            {
                if (Vector256ArrayTest<float>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<double>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<byte>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<sbyte>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<short>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<ushort>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<int>.Vector256Array() != Pass) returnVal = Fail;
                if (Vector256ArrayTest<uint>.Vector256Array() != Pass) returnVal = Fail;
                if (Environment.Is64BitProcess)
                {
                    if (Vector256ArrayTest<long>.Vector256Array() != Pass) returnVal = Fail;
                    if (Vector256ArrayTest<ulong>.Vector256Array() != Pass) returnVal = Fail;
                }
            }
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine("NotSupportedException was raised");
            Console.WriteLine(ex.StackTrace);
            Assert.Fail("");
        }

        Assert.Equal(Pass, returnVal);
    }
}
