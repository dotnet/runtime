// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 4, 0, 0)]
    [InlineData(16, 32, 4, 80, 0)]
    [InlineData(32, 64, 4, 160, 0)]
    [InlineData(64, 128, 4, 320, 0)]
    public void GatherVector128_64Bit(long leftIndex, long rightIndex, byte scale, long leftExpected, long rightExpected)
    {
        unsafe
        {
            Vector128<int> index = Vector128.Create(leftIndex, rightIndex).AsInt32();
            int[] indexTable = { index[0], index[1], index[2], index[3] };
            nint[] nativeIndexTable = { index[0], index[1], index[2], index[3] };
            Vector128<nint> nativeIndex = Vector128.Create(nativeIndexTable);

            int count = indexTable.Max() * scale;
            var baseData = new nint[count];
            for (int i = 0; i < count; i++)
            {
                baseData[i] = i * 10;
            }

            Vector128<long> expected = Vector128.Create(leftExpected, rightExpected);

            //
            // Test all Vector128<T>.GatherVector128 overloads with index of type Vector128
            //

            // public static unsafe Vector128<nint> GatherVector128(nint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nint, int> table = new(baseData, new nint[scale]))
            {
                var actual = Avx2.GatherVector128((nint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nint*), typeof(Vector128<int>), typeof(byte) })!;

                actual = (Vector128<nint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), index, scale })!;
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<nuint> GatherVector128(nuint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nuint, int> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector128((nuint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nuint*), typeof(Vector128<int>), typeof(byte) })!;

                actual = (Vector128<nuint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nuint*)), index, scale })!;
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<int, nint> table = new(Unsafe.As<nint[], int[]>(ref baseData), new int[scale]))
            {
                var actual = Avx2.GatherVector128((int*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(int*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<int>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(int*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<uint, nint> table = new(Unsafe.As<nint[], uint[]>(ref baseData), new uint[scale]))
            {
                var actual = Avx2.GatherVector128((uint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(uint*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<uint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(uint*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<nint> GatherVector128(nint* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<nint, nint> table = new(Unsafe.As<nint[], nint[]>(ref baseData), new nint[scale]))
            {
                var actual = Avx2.GatherVector128((nint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nint*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<nint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<nuint> GatherVector128(nuint* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<nuint, nint> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector128((nuint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nuint*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<nuint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<float, nint> table = new(Unsafe.As<nint[], float[]>(ref baseData), new float[scale]))
            {
                var actual = Avx2.GatherVector128((float*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(float*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<float>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(float*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<double, nint> table = new(Unsafe.As<nint[], double[]>(ref baseData), new double[scale]))
            {
                var actual = Avx2.GatherVector128((double*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(double*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<double>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(double*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            //
            // Test all Vector128<T>.GatherVector128 overloads with index of type Vector256
            //

            Vector256<nint> index256 = Vector256.Create(nativeIndex.AsInt64(), nativeIndex.AsInt64()).AsNInt();

            // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<int, nint> table = new(Unsafe.As<nint[], int[]>(ref baseData), new int[scale]))
            {
                var actual = Avx2.GatherVector128((int*)table.InArrayPtr, index256, scale);
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(int*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector128<int>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(int*)), index256, scale })!;
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<uint, nint> table = new(Unsafe.As<nint[], uint[]>(ref baseData), new uint[scale]))
            {
                var actual = Avx2.GatherVector128((uint*)table.InArrayPtr, index256, scale);
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(uint*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector128<uint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(uint*)), index256, scale })!;
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<float, nint> table = new(Unsafe.As<nint[], float[]>(ref baseData), new float[scale]))
            {
                var actual = Avx2.GatherVector128((float*)table.InArrayPtr, index256, scale);
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(float*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector128<float>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(float*)), index256, scale })!;
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 4, 0, 0)]
    [InlineData(16, 32, 4, 80, 0)]
    [InlineData(32, 64, 4, 160, 0)]
    [InlineData(64, 128, 4, 320, 0)]
    public void GatherVector128_32Bit(int leftIndex, int rightIndex, byte scale, int leftExpected, int rightExpected)
    {
        unsafe
        {
            Vector128<int> index = Vector128.Create(leftIndex, rightIndex).AsInt32();
            int[] indexTable = { index[0], index[1], index[2], index[3] };
            nint[] nativeIndexTable = { index[0], index[1], index[2], index[3] };
            Vector128<nint> nativeIndex = Vector128.Create(nativeIndexTable);

            int count = indexTable.Max() * scale;
            var baseData = new nint[count];
            for (int i = 0; i < count; i++)
            {
                baseData[i] = i * 10;
            }

            Vector128<long> expected = Vector128.Create(leftExpected, rightExpected);

            //
            // Test all Vector128<T>.GatherVector128 overloads with index of type Vector128
            //

            // public static unsafe Vector128<nint> GatherVector128(nint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nint, int> table = new(baseData, new nint[scale]))
            {
                var actual = Avx2.GatherVector128((nint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nint*), typeof(Vector128<int>), typeof(byte) })!;

                actual = (Vector128<nint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), index, scale })!;
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<nuint> GatherVector128(nuint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nuint, int> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector128((nuint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nuint*), typeof(Vector128<int>), typeof(byte) })!;

                actual = (Vector128<nuint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nuint*)), index, scale })!;
                Assert.Equal(expected.AsInt64().GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<int, nint> table = new(Unsafe.As<nint[], int[]>(ref baseData), new int[scale]))
            {
                var actual = Avx2.GatherVector128((int*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(int*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<int>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(int*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<uint, nint> table = new(Unsafe.As<nint[], uint[]>(ref baseData), new uint[scale]))
            {
                var actual = Avx2.GatherVector128((uint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(uint*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<uint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(uint*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<nint> GatherVector128(nint* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<nint, nint> table = new(Unsafe.As<nint[], nint[]>(ref baseData), new nint[scale]))
            {
                var actual = Avx2.GatherVector128((nint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nint*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<nint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<nuint> GatherVector128(nuint* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<nuint, nint> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector128((nuint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(nuint*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<nuint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<float, nint> table = new(Unsafe.As<nint[], float[]>(ref baseData), new float[scale]))
            {
                var actual = Avx2.GatherVector128((float*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(float*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<float>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(float*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<nint> index, byte scale);
            using (TestTable<double, nint> table = new(Unsafe.As<nint[], double[]>(ref baseData), new double[scale]))
            {
                var actual = Avx2.GatherVector128((double*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected, actual.AsInt64());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(double*), typeof(Vector128<nint>), typeof(byte) })!;

                actual = (Vector128<double>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(double*)), nativeIndex, scale })!;
                Assert.Equal(expected, actual.AsInt64());
            }

            //
            // Test all Vector128<T>.GatherVector128 overloads with index of type Vector256
            //

            Vector256<nint> index256 = Vector256.Create(nativeIndex.AsInt64(), nativeIndex.AsInt64()).AsNInt();

            // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<int, nint> table = new(Unsafe.As<nint[], int[]>(ref baseData), new int[scale]))
            {
                var actual = Avx2.GatherVector128((int*)table.InArrayPtr, index256, scale);
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(int*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector128<int>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(int*)), index256, scale })!;
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<uint, nint> table = new(Unsafe.As<nint[], uint[]>(ref baseData), new uint[scale]))
            {
                var actual = Avx2.GatherVector128((uint*)table.InArrayPtr, index256, scale);
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(uint*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector128<uint>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(uint*)), index256, scale })!;
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());
            }

            // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<float, nint> table = new(Unsafe.As<nint[], float[]>(ref baseData), new float[scale]))
            {
                var actual = Avx2.GatherVector128((float*)table.InArrayPtr, index256, scale);
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128),
                    new[] { typeof(float*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector128<float>)method.Invoke(null, new[] { Pointer.Box(table.InArrayPtr, typeof(float*)), index256, scale })!;
                Assert.Equal(expected.GetLower(), actual.AsInt64().GetLower());
            }
        }
    }
}
