// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    const int N = 64;

    static readonly float[] floatSourceTable = new float[N];
    static readonly double[] doubleSourceTable = new double[N];
    static readonly int[] intSourceTable = new int[N];
    static readonly uint[] uintSourceTable = new uint[N];
    static readonly nint[] nintSourceTable = new nint[N];
    static readonly nuint[] nuintSourceTable = new nuint[N];
    static readonly long[] longSourceTable = new long[N];
    static readonly ulong[] ulongSourceTable = new ulong[N];

    static readonly int[] intIndexTable = new int[4] { 8, 16, 32, 63 };
    static readonly nint[] nintIndexTable = new nint[4] { 8, 16, 32, 63 };
    static readonly nuint[] nuintIndexTable = new nuint[4] { 8, 16, 32, 63 };
    static readonly long[] longIndexTable = new long[2] { 16, 32 };
    static readonly ulong[] ulongIndexTable = new ulong[2] { 16, 32 };
    static readonly long[] vector256longIndexTable = new long[4] { 8, 16, 32, 63 };

    static readonly int[] intMaskTable = new int[4] { -1, 0, -1, 0 };
    static readonly long[] longMaskTable = new long[2] { -1, 0 };

    [Fact]
    public unsafe void GatherVector256()
    {
        Vector128<int> indexi;
        Vector128<nint> indexn;
        Vector128<nuint> indexnu;

        fixed (int* iptr = intIndexTable)
        fixed (long* lptr = longIndexTable)
        fixed (long* l256ptr = vector256longIndexTable)
        {
            indexi = Sse2.LoadVector128(iptr);
            indexn = indexi.AsNInt();
            indexnu = indexi.AsNUInt();
        }

        Vector128<int> maski;
        Vector128<uint> maskui;
        Vector128<nint> maskn;
        Vector128<nuint> masknu;
        Vector128<long> maskl;
        Vector128<ulong> maskul;
        Vector128<float> maskf;
        Vector128<double> maskd;

        fixed (int* iptr = intMaskTable)
        fixed (long* lptr = longMaskTable)
        {
            maski = Sse2.LoadVector128(iptr);
            maskl = Sse2.LoadVector128(lptr);

            maskui = maski.AsUInt32();
            maskul = maskl.AsUInt64();
            maskf = maski.AsSingle();
            maskd = maskl.AsDouble();
            maskn = maskl.AsNInt();
            masknu = maskl.AsNUInt();
        }

        Vector128<int> sourcei = Vector128<int>.Zero;
        Vector128<uint> sourceui = Vector128<uint>.Zero;
        Vector128<long> sourcel = Vector128<long>.Zero;
        Vector128<ulong> sourceul = Vector128<ulong>.Zero;
        Vector128<float> sourcef = Vector128<float>.Zero;
        Vector128<double> sourced = Vector128<double>.Zero;
        Vector128<nint> sourcen = Vector128<nint>.Zero;
        Vector128<nuint> sourcenu = Vector128<nuint>.Zero;

        // public static unsafe Vector128<nint> GatherMaskVector128(Vector128<nint> source, nint* baseAddress, Vector128<int> index, Vector128<nint> mask, byte scale);
        using (TestTable<nint, int> nintTable = new(nintSourceTable, new nint[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcen, (nint*)nintTable.InArrayPtr, indexi, maskn, 4);
            Unsafe.Write(nintTable.OutArrayPtr, vf);
            Assert.True(nintTable.CheckResult(
                (x, y) => x == y, intIndexTable));

            vf = (Vector128<nint>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<nint>), typeof(nint*), typeof(Vector128<int>), typeof(Vector128<nint>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcen, Pointer.Box(nintTable.InArrayPtr, typeof(nint*)), indexi, maskn, (byte)4
                    })!;
            Unsafe.Write(nintTable.OutArrayPtr, vf);

            Assert.True(nintTable.CheckResult(
                (x, y) => x == y, intIndexTable));
        }

        // public static unsafe Vector128<nuint> GatherMaskVector128(Vector128<nuint> source, nuint* baseAddress, Vector128<int> index, Vector128<nuint> mask, byte scale);
        using (TestTable<nuint, int> nuintTable = new(nuintSourceTable, new nuint[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcenu, (nuint*)nuintTable.InArrayPtr, indexi, masknu, 4);
            Unsafe.Write(nuintTable.OutArrayPtr, vf);
            Assert.True(nuintTable.CheckResult(
                (x, y) => x == y, intIndexTable));

            vf = (Vector128<nuint>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<nuint>), typeof(nuint*), typeof(Vector128<int>), typeof(Vector128<nuint>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcenu, Pointer.Box(nuintTable.InArrayPtr, typeof(nuint*)), indexi, masknu, (byte)4
                    })!;
            Unsafe.Write(nuintTable.OutArrayPtr, vf);

            Assert.True(nuintTable.CheckResult(
                (x, y) => x == y, intIndexTable));
        }

        // public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<nint> index, Vector128<int> mask, byte scale);
        using (TestTable<int, nint> intTable = new(intSourceTable, new int[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcei, (int*)intTable.InArrayPtr, indexn, maski, 4);
            Unsafe.Write(intTable.OutArrayPtr, vf);
            Assert.True(intTable.CheckResult(
                (x, y) => x == y, nintIndexTable));

            vf = (Vector128<int>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<int>), typeof(int*), typeof(Vector128<nint>), typeof(Vector128<int>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcei, Pointer.Box(intTable.InArrayPtr, typeof(int*)), indexn, maski, (byte)4
                    })!;
            Unsafe.Write(intTable.OutArrayPtr, vf);

            Assert.True(intTable.CheckResult(
                (x, y) => x == y, nintIndexTable));
        }

        // public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector256<nint> index, Vector128<uint> mask, byte scale);
        using (TestTable<uint, nint> uintTable = new(uintSourceTable, new uint[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourceui, (uint*)uintTable.InArrayPtr, indexn, maskui, 4);
            Unsafe.Write(uintTable.OutArrayPtr, vf);
            Assert.True(uintTable.CheckResult(
                (x, y) => x == y, nintIndexTable));

            vf = (Vector128<uint>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<uint>), typeof(uint*), typeof(Vector128<nint>), typeof(Vector128<uint>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourceui, Pointer.Box(uintTable.InArrayPtr, typeof(uint*)), indexn, maskui, (byte)4
                    })!;
            Unsafe.Write(uintTable.OutArrayPtr, vf);

            Assert.True(uintTable.CheckResult(
                (x, y) => x == y, nintIndexTable));
        }

        // public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<nint> index, Vector128<long> mask, byte scale);
        using (TestTable<long, nint> longTable = new(longSourceTable, new long[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcel, (long*)longTable.InArrayPtr, indexn, maskl, 4);
            Unsafe.Write(longTable.OutArrayPtr, vf);
            Assert.True(longTable.CheckResult(
                (x, y) => x == y, nintIndexTable));

            vf = (Vector128<long>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<long>), typeof(long*), typeof(Vector128<nint>), typeof(Vector128<long>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcel, Pointer.Box(longTable.InArrayPtr, typeof(long*)), indexn, maskl, (byte)4
                    })!;
            Unsafe.Write(longTable.OutArrayPtr, vf);

            Assert.True(longTable.CheckResult(
                (x, y) => x == y, nintIndexTable));
        }

        // public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<nint> index, Vector128<long> mask, byte scale);
        using (TestTable<ulong, nint> ulongTable = new(ulongSourceTable, new ulong[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourceul, (ulong*)ulongTable.InArrayPtr, indexn, maskul, 4);
            Unsafe.Write(ulongTable.OutArrayPtr, vf);
            Assert.True(ulongTable.CheckResult(
                (x, y) => x == y, nintIndexTable));

            vf = (Vector128<ulong>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<ulong>), typeof(ulong*), typeof(Vector128<nint>), typeof(Vector128<long>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourceul, Pointer.Box(ulongTable.InArrayPtr, typeof(ulong*)), indexn, maskul, (byte)4
                    })!;
            Unsafe.Write(ulongTable.OutArrayPtr, vf);

            Assert.True(ulongTable.CheckResult(
                (x, y) => x == y, nintIndexTable));
        }

        // public static unsafe Vector128<nint> GatherMaskVector128(Vector128<nint> source, nint* baseAddress, Vector128<nint> index, Vector128<nint> mask, byte scale);
        using (TestTable<nint, nint> nintTable = new(nintSourceTable, new nint[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcen, (nint*)nintTable.InArrayPtr, indexn, maskn, 4);
            Unsafe.Write(nintTable.OutArrayPtr, vf);
            Assert.True(nintTable.CheckResult(
                (x, y) => x == y, nintIndexTable));

            vf = (Vector128<nint>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<nint>), typeof(nint*), typeof(Vector128<nint>), typeof(Vector128<nint>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcen, Pointer.Box(nintTable.InArrayPtr, typeof(nint*)), indexn, maskn, (byte)4
                    })!;
            Unsafe.Write(nintTable.OutArrayPtr, vf);

            Assert.True(nintTable.CheckResult(
                (x, y) => x == y, nintIndexTable));
        }

        // public static unsafe Vector128<nuint> GatherMaskVector128(Vector128<nuint> source, nuint* baseAddress, Vector128<nint> index, Vector128<nuint> mask, byte scale);
        using (TestTable<nuint, nuint> nuintTable = new(nuintSourceTable, new nuint[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcen, (nuint*)nuintTable.InArrayPtr, indexn, maskn, 4);
            Unsafe.Write(nuintTable.OutArrayPtr, vf);
            Assert.True(nuintTable.CheckResult(
                (x, y) => x == y, nuintIndexTable));

            vf = (Vector128<nuint>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<nint>), typeof(nint*), typeof(Vector128<nint>), typeof(Vector128<nint>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcen, Pointer.Box(nuintTable.InArrayPtr, typeof(nint*)), indexn, maskn, (byte)4
                    })!;
            Unsafe.Write(nuintTable.OutArrayPtr, vf);

            Assert.True(nuintTable.CheckResult(
                (x, y) => x == y, nuintIndexTable));
        }

        // public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<nint> index, Vector128<float> mask, byte scale);
        using (TestTable<float, nint> floatTable = new(floatSourceTable, new float[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourcef, (float*)floatTable.InArrayPtr, indexn, maskf, 4);
            Unsafe.Write(floatTable.OutArrayPtr, vf);
            Assert.True(floatTable.CheckResult(
                (x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), nintIndexTable));

            vf = (Vector128<float>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<float>), typeof(float*), typeof(Vector128<nint>), typeof(Vector128<float>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcef, Pointer.Box(floatTable.InArrayPtr, typeof(float*)), indexn, maskf, (byte)4
                    })!;
            Unsafe.Write(floatTable.OutArrayPtr, vf);

            Assert.True(floatTable.CheckResult(
                (x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), nintIndexTable));
        }

        // public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<nint> index, Vector128<double> mask, byte scale);
        using (TestTable<double, nint> doubleTable = new(doubleSourceTable, new double[4]))
        {
            var vf = Avx2.GatherMaskVector128(sourced, (double*)doubleTable.InArrayPtr, indexn, maskd, 4);
            Unsafe.Write(doubleTable.OutArrayPtr, vf);
            Assert.True(doubleTable.CheckResult(
                (x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), nintIndexTable));

            vf = (Vector128<double>)typeof(Avx2)
                .GetMethod(nameof(Avx2.GatherMaskVector128),
                    new[]
                    {
                        typeof(Vector128<double>), typeof(double*), typeof(Vector128<nint>), typeof(Vector128<double>),
                        typeof(byte)
                    })!.Invoke(null,
                    new[]
                    {
                        sourcef, Pointer.Box(doubleTable.InArrayPtr, typeof(double*)), indexi, maskf, (byte)4
                    })!;
            Unsafe.Write(doubleTable.OutArrayPtr, vf);

            Assert.True(doubleTable.CheckResult(
                (x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), nintIndexTable));
        }
    }
}
