// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Collections.Generic;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program { public class GatherMaskVector256
    {
        const int N = 128;

        static byte Four;
        static byte Eight;
        static byte invalid;

        static readonly float[] floatSourceTable = new float[N];
        static readonly double[] doubleSourceTable = new double[N];
        static readonly int[] intSourceTable = new int[N];
        static readonly long[] longSourceTable = new long[N];

        static readonly int[] intIndexTable = new int[8] {1, 2, 4, 8, 16, 32, 40, 63};
        static readonly long[] longIndexTable = new long[4] {2, 8, 16, 32};
        static readonly int[] vector128intIndexTable = new int[4] {8, 16, 32, 63};

        static readonly int[] intMaskTable = new int[8] {-1, 0, -1, 0, -1, 0, -1, 0};
        static readonly long[] longMaskTable = new long[4] {-1, 0, -1, 0};

        [Fact]
        public static unsafe void Test()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                Four = 4;
                Eight = 8;
                invalid = 15;

                for (int i = 0; i < N; i++)
                {
                    floatSourceTable[i] = (float)i * 10.0f;
                    doubleSourceTable[i] = (double)i * 10.0;
                    intSourceTable[i] = i * 10;
                    longSourceTable[i] = i * 10;
                }

                Vector256<int> indexi;
                Vector256<long> indexl;
                Vector128<int> indexi128;

                fixed (int* iptr = intIndexTable)
                fixed (long* lptr = longIndexTable)
                fixed (int* i128ptr = vector128intIndexTable)
                {
                    indexi = Avx.LoadVector256(iptr);
                    indexl = Avx.LoadVector256(lptr);
                    indexi128 = Sse2.LoadVector128(i128ptr);
                }

                Vector256<int> maski;
                Vector256<uint> maskui;
                Vector256<long> maskl;
                Vector256<ulong> maskul;
                Vector256<float> maskf;
                Vector256<double> maskd;

                fixed (int* iptr = intMaskTable)
                fixed (long* lptr = longMaskTable)
                {
                    maski = Avx.LoadVector256(iptr);
                    maskl = Avx.LoadVector256(lptr);

                    maskui = maski.AsUInt32();
                    maskul = maskl.AsUInt64();
                    maskf = maski.AsSingle();
                    maskd = maskl.AsDouble();
                }

                Vector256<int> sourcei = Vector256<int>.Zero;
                Vector256<uint> sourceui = Vector256<uint>.Zero;
                Vector256<long> sourcel = Vector256<long>.Zero;
                Vector256<ulong> sourceul = Vector256<ulong>.Zero;
                Vector256<float> sourcef = Vector256<float>.Zero;
                Vector256<double> sourced = Vector256<double>.Zero;

                // public static unsafe Vector256<float> GatherMaskVector256(Vector256<float> source, float* baseAddress, Vector256<int> index, Vector256<float> mask, byte scale)
                using (TestTable<float, int> floatTable = new TestTable<float, int>(floatSourceTable, new float[8]))
                {
                    var vf = Avx2.GatherMaskVector256(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<float>), typeof(float*), typeof(Vector256<int>), typeof(Vector256<float>), typeof(byte)}).
                            Invoke(null, new object[] { sourcef, Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexi, maskf, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf,  3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on float with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf,  Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on float with non-const scale (IMM):");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf,  invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on float with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector128<int> index, Vector256<double> mask, byte scale)
                using (TestTable<double, int> doubletTable = new TestTable<double, int>(doubleSourceTable, new double[4]))
                {
                    var vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexi128, maskd, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector256<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<double>), typeof(double*), typeof(Vector128<int>), typeof(Vector256<double>), typeof(byte)}).
                            Invoke(null, new object[] { sourced, Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexi128, maskd, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexi128, maskd,  3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexi128, maskd,  Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with non-const scale (IMM):");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexi128, maskd,  invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<int> GatherMaskVector256(Vector256<int> source, int* baseAddress, Vector256<int> index, Vector256<int> mask, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[8]))
                {
                    var vf = Avx2.GatherMaskVector256(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<int>), typeof(int*), typeof(Vector256<int>), typeof(Vector256<int>), typeof(byte)}).
                            Invoke(null, new object[] { sourcei, Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexi, maski, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, 3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on int with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on int with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on int with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<uint> GatherMaskVector256(Vector256<uint> source, uint* baseAddress, Vector256<int> index, Vector256<uint> mask, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[8]))
                {
                    var vf = Avx2.GatherMaskVector256(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<uint>), typeof(uint*), typeof(Vector256<int>), typeof(Vector256<uint>), typeof(byte)}).
                            Invoke(null, new object[] { sourceui, Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexi, maskui, (byte)4 });
                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, 3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on uint with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on uint with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on uint with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector128<int> index, Vector256<long> mask, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexi128, maskl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<long>), typeof(long*), typeof(Vector128<int>), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { sourcel, Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexi128, maskl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexi128, maskl, 3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexi128, maskl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexi128, maskl, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector128<int> index, Vector256<ulong> mask, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexi128, maskul, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<ulong>), typeof(ulong*), typeof(Vector128<int>), typeof(Vector256<ulong>), typeof(byte)}).
                            Invoke(null, new object[] { sourceul, Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexi128, maskul, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexi128, maskul,  3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexi128, maskul,  Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexi128, maskul,  invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector256<long> index, Vector256<long> mask, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<long>), typeof(long*), typeof(Vector256<long>), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { sourcel, Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexl, maskl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on long with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, 3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector256<long> index, Vector256<ulong> mask, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<ulong>), typeof(ulong*), typeof(Vector256<long>), typeof(Vector256<ulong>), typeof(byte)}).
                            Invoke(null, new object[] { sourceul, Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexl, maskul, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on ulong with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul,  3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul,  Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on ulong with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector256(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul,  invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on long with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector256<long> index, Vector256<double> mask, byte scale)
                using (TestTable<double, long> doubletTable = new TestTable<double, long>(doubleSourceTable, new double[4]))
                {
                    var vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with Vector256 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector256<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector256), new Type[] {typeof(Vector256<double>), typeof(double*), typeof(Vector256<long>), typeof(Vector256<double>), typeof(byte)}).
                            Invoke(null, new object[] { sourced, Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexl, maskd, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed with reflection on double with Vector256 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, 3);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector256(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector256 failed on double with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

            }

            Assert.Equal(Pass, testResult);
        }

        public unsafe struct TestTable<T, U> : IDisposable where T : struct where U : struct
        {
            public T[] inArray;
            public T[] outArray;

            public void* inArrayPtr => inHandle.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle;
            GCHandle outHandle;
            public TestTable(T[] a, T[] b)
            {
                this.inArray = a;
                this.outArray = b;

                inHandle = GCHandle.Alloc(inArray, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, T, bool> check, U[] indexArray)
            {
                int length = Math.Min(indexArray.Length, outArray.Length);
                for (int i = 0; i < length; i++)
                {
                    bool take = i % 2 == 0;
                    if ((take && !check(inArray[Convert.ToInt32(indexArray[i])], outArray[i])) ||
                        (!take && !EqualityComparer<T>.Default.Equals(outArray[i], default(T))))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }

    } }
}
