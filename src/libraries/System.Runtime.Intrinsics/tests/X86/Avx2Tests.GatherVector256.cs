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
    [InlineData(0, 0, 0, 0, 4, 0, 0, 0, 0)]
    [InlineData(4, 8, 16, 32, 4, 20, 0, 40, 0)]
    [InlineData(8, 16, 32, 64, 4, 40, 0, 80, 0)]
    [InlineData(16, 32, 64, 128, 4, 80, 0, 160, 0)]
    public void GatherVector256_64Bit(long index1, long index2, long index3, long index4, byte scale, long expected1,
        long expected2, long expected3, long expected4)
    {
        unsafe
        {
            Vector256<nint> nativeIndex = Vector256.Create(index1, index2, index3, index4).AsNInt();
            nint[] indexTable = { nativeIndex[0], nativeIndex[1], nativeIndex[2], nativeIndex[3] };

            Vector256<int> index = Vector256.Create(index1, index2, index3, index4).AsInt32();

            int count = (int)indexTable.Max() * scale;
            var baseData = new nint[count];
            for (int i = 0; i < count; i++)
            {
                baseData[i] = i * 10;
            }

            Vector256<long> expected = Vector256.Create(expected1, expected2, expected3, expected4);

            // public static unsafe Vector256<nint> GatherVector256(nint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nint, nint> table = new(baseData, new nint[scale]))
            {
                var actual = Avx2.GatherVector256((nint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsNInt(), actual);
                
                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nint*), typeof(Vector256<int>), typeof(byte) })!;
                
                actual = (Vector256<nint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), index, scale })!;
                Assert.Equal(expected.AsNInt(), actual);
            }

            // public static unsafe Vector256<nuint> GatherVector256(nuint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nuint, nint> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector256((nuint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsNUInt(), actual);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nuint*), typeof(Vector256<int>), typeof(byte) })!;

                actual = (Vector256<nuint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), index, scale })!;
                Assert.Equal(expected.AsNUInt(), actual);
            }
            
            // public static unsafe Vector256<nint> GatherVector256(long* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<int, nint> table = new(Unsafe.As<nint[], int[]>(ref baseData), new int[scale]))
            {
                var actual = Avx2.GatherVector256((long*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(long*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(long*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNInt()[0], actual[0]);
            }
            
            // public static unsafe Vector256<nuint> GatherVector256(ulong* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<ulong, nint> table = new(Unsafe.As<nint[], ulong[]>(ref baseData), new ulong[scale]))
            {
                var actual = Avx2.GatherVector256((ulong*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNUInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(ulong*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nuint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(ulong*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNUInt()[0], actual[0]);
            }
            
            // public static unsafe Vector256<nint> GatherVector256(nint* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<nint, nint> table = new(Unsafe.As<nint[], nint[]>(ref baseData), new nint[scale]))
            {
                var actual = Avx2.GatherVector256((nint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nint*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNInt()[0], actual[0]);
            }
            
            // public static unsafe Vector256<nuint> GatherVector256(nuint* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<nuint, nint> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector256((nuint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNUInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nuint*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nuint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nuint*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNUInt()[0], actual[0]);
            }
            
            // public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<double, nint> table = new(Unsafe.As<nint[], double[]>(ref baseData), new double[scale]))
            {
                var actual = Avx2.GatherVector256((double*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsDouble()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(double*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<double>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(double*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsDouble()[0], actual[0]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 4, 0, 0, 0, 0)]
    [InlineData(4, 8, 16, 32, 4, 20, 0, 40, 0)]
    [InlineData(8, 16, 32, 64, 4, 40, 0, 80, 0)]
    [InlineData(16, 32, 64, 128, 4, 80, 0, 160, 0)]
    public void GatherVector256_32Bit(int index1, int index2, int index3, int index4, byte scale,
            int expected1, int expected2, int expected3, int expected4)
    {
        unsafe
        {
            Vector256<nint> nativeIndex = Vector256.Create(index1, index2, index3, index4).AsNInt();
            nint[] indexTable = { nativeIndex[0], nativeIndex[1], nativeIndex[2], nativeIndex[3] };

            Vector256<int> index = Vector256.Create(index1, index2, index3, index4).AsInt32();

            int count = (int)indexTable.Max() * scale;
            var baseData = new nint[count];
            for (int i = 0; i < count; i++)
            {
                baseData[i] = i * 10;
            }

            Vector256<long> expected = Vector256.Create(expected1, expected2, expected3, expected4);

            // public static unsafe Vector256<nint> GatherVector256(nint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nint, nint> table = new(baseData, new nint[scale]))
            {
                var actual = Avx2.GatherVector256((nint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsNInt(), actual);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nint*), typeof(Vector256<int>), typeof(byte) })!;

                actual = (Vector256<nint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), index, scale })!;
                Assert.Equal(expected.AsNInt(), actual);
            }

            // public static unsafe Vector256<nuint> GatherVector256(nuint* baseAddress, Vector128<int> index, byte scale);
            using (TestTable<nuint, nint> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector256((nuint*)table.InArrayPtr, index, scale);
                Assert.Equal(expected.AsNUInt(), actual);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nuint*), typeof(Vector256<int>), typeof(byte) })!;

                actual = (Vector256<nuint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), index, scale })!;
                Assert.Equal(expected.AsNUInt(), actual);
            }

            // public static unsafe Vector256<nint> GatherVector256(long* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<int, nint> table = new(Unsafe.As<nint[], int[]>(ref baseData), new int[scale]))
            {
                var actual = Avx2.GatherVector256((long*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(long*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(long*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNInt()[0], actual[0]);
            }

            // public static unsafe Vector256<nuint> GatherVector256(ulong* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<ulong, nint> table = new(Unsafe.As<nint[], ulong[]>(ref baseData), new ulong[scale]))
            {
                var actual = Avx2.GatherVector256((ulong*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNUInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(ulong*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nuint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(ulong*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNUInt()[0], actual[0]);
            }

            // public static unsafe Vector256<nint> GatherVector256(nint* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<nint, nint> table = new(Unsafe.As<nint[], nint[]>(ref baseData), new nint[scale]))
            {
                var actual = Avx2.GatherVector256((nint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nint*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nint*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNInt()[0], actual[0]);
            }

            // public static unsafe Vector256<nuint> GatherVector256(nuint* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<nuint, nint> table = new(Unsafe.As<nint[], nuint[]>(ref baseData), new nuint[scale]))
            {
                var actual = Avx2.GatherVector256((nuint*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsNUInt()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(nuint*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<nuint>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(nuint*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsNUInt()[0], actual[0]);
            }

            // public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector256<nint> index, byte scale);
            using (TestTable<double, nint> table = new(Unsafe.As<nint[], double[]>(ref baseData),
                       new double[scale]))
            {
                var actual = Avx2.GatherVector256((double*)table.InArrayPtr, nativeIndex, scale);
                Assert.Equal(expected.AsDouble()[0], actual[0]);

                var method = typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256),
                    new[] { typeof(double*), typeof(Vector256<nint>), typeof(byte) })!;

                actual = (Vector256<double>)method.Invoke(null,
                    new[] { Pointer.Box(table.InArrayPtr, typeof(double*)), nativeIndex, scale })!;
                Assert.Equal(expected.AsDouble()[0], actual[0]);
            }
        }
    }
}
