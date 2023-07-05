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
    public partial class Program { public class GatherMaskVector128
    {
        const int N = 64;

        static byte Four;
        static byte Eight;
        static byte invalid;

        static readonly float[] floatSourceTable = new float[N];
        static readonly double[] doubleSourceTable = new double[N];
        static readonly int[] intSourceTable = new int[N];
        static readonly long[] longSourceTable = new long[N];

        static readonly int[] intIndexTable = new int[4] {8, 16, 32, 63};
        static readonly long[] longIndexTable = new long[2] {16, 32};
        static readonly long[] vector256longIndexTable = new long[4] {8, 16, 32, 63};

        static readonly int[] intMaskTable = new int[4] {-1, 0, -1, 0};
        static readonly long[] longMaskTable = new long[2] {-1, 0};

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

                Vector128<int> indexi;
                Vector128<long> indexl;
                Vector256<long> indexl256;

                fixed (int* iptr = intIndexTable)
                fixed (long* lptr = longIndexTable)
                fixed (long* l256ptr = vector256longIndexTable)
                {
                    indexi = Sse2.LoadVector128(iptr);
                    indexl = Sse2.LoadVector128(lptr);
                    indexl256 = Avx.LoadVector256(l256ptr);
                }

                Vector128<int> maski;
                Vector128<uint> maskui;
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
                }

                Vector128<int> sourcei = Vector128<int>.Zero;
                Vector128<uint> sourceui = Vector128<uint>.Zero;
                Vector128<long> sourcel = Vector128<long>.Zero;
                Vector128<ulong> sourceul = Vector128<ulong>.Zero;
                Vector128<float> sourcef = Vector128<float>.Zero;
                Vector128<double> sourced = Vector128<double>.Zero;


                // public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<int> index, Vector128<float> mask, byte scale)
                using (TestTable<float, int> floatTable = new TestTable<float, int>(floatSourceTable, new float[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<float>), typeof(float*), typeof(Vector128<int>), typeof(Vector128<float>), typeof(byte)}).
                            Invoke(null, new object[] { sourcef, Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexi, maskf, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with non-const scale (IMM):");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexi, maskf, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }


                // public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<int> index, Vector128<double> mask, byte scale)
                using (TestTable<double, int> doubletTable = new TestTable<double, int>(doubleSourceTable, new double[2]))
                {
                    var vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexi, maskd, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector128<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<double>), typeof(double*), typeof(Vector128<int>), typeof(Vector128<double>), typeof(byte)}).
                            Invoke(null, new object[] { sourced, Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexi, maskd, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexi, maskd, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexi, maskd,  Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with non-const scale (IMM):");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexi, maskd,  invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<int> index, Vector128<int> mask, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<int>), typeof(int*), typeof(Vector128<int>), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { sourcei, Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexi, maski, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexi, maski, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<int> index, Vector128<uint> mask, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<uint>), typeof(uint*), typeof(Vector128<int>), typeof(Vector128<uint>), typeof(byte)}).
                            Invoke(null, new object[] { sourceui, Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexi, maskui, (byte)4});
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexi, maskui, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<int> index, Vector128<long> mask, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexi, maskl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<long>), typeof(long*), typeof(Vector128<int>), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { sourcel, Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexi, maskl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexi, maskl, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexi, maskl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexi, maskl, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<int> index, Vector128<ulong> mask, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexi, maskul, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<ulong>), typeof(ulong*), typeof(Vector128<int>), typeof(Vector128<ulong>), typeof(byte)}).
                            Invoke(null, new object[] { sourceul, Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexi, maskul, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexi, maskul, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexi, maskul, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexi, maskul, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<long> index, Vector128<int> mask, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl, maski, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<int>), typeof(int*), typeof(Vector128<long>), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { sourcei, Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexl, maski, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on int with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl, maski,  3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl, maski,  Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl, maski,  invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<long> index, Vector128<uint> mask, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl, maskui, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<uint>), typeof(uint*), typeof(Vector128<long>), typeof(Vector128<uint>), typeof(byte)}).
                            Invoke(null, new object[] { sourceui, Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexl, maskui, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on uint with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl, maskui, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl, maskui, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl, maskui, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<long> index, Vector128<long> mask, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<long>), typeof(long*), typeof(Vector128<long>), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { sourcel, Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexl, maskl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on long with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcel, (long*)(longTable.inArrayPtr), indexl, maskl, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<long> index, Vector128<ulong> mask, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<ulong>), typeof(ulong*), typeof(Vector128<long>), typeof(Vector128<ulong>), typeof(byte)}).
                            Invoke(null, new object[] { sourceul, Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexl, maskul, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on ulong with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on ulong with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceul, (ulong*)(longTable.inArrayPtr), indexl, maskul, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on long with invalid non-const scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<long> index, Vector128<float> mask, byte scale)
                using (TestTable<float, long> floatTable = new TestTable<float, long>(floatSourceTable, new float[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl, maskf, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with Vector128 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<float>), typeof(float*), typeof(Vector128<long>), typeof(Vector128<float>), typeof(byte)}).
                            Invoke(null, new object[] { sourcef, Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexl, maskf, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on float with Vector128 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl, maskf, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl, maskf, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl, maskf, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with invalid non-const scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<long> index, Vector128<double> mask, byte scale)
                using (TestTable<double, long> doubletTable = new TestTable<double, long>(doubleSourceTable, new double[2]))
                {
                    var vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with Vector128 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector128<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<double>), typeof(double*), typeof(Vector128<long>), typeof(Vector128<double>), typeof(byte)}).
                            Invoke(null, new object[] { sourced, Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexl, maskd, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on double with Vector128 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherMaskVector128(sourced, (double*)(doubletTable.inArrayPtr), indexl, maskd, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on double with invalid non-const scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector256<long> index, Vector128<int> mask, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl256, maski, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<int>), typeof(int*), typeof(Vector256<long>), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { sourcei, Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexl256, maski, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on int with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl256, maski, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl256, maski, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcei, (int*)(intTable.inArrayPtr), indexl256, maski, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on int with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector256<long> index, Vector128<uint> mask, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl256, maskui, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<uint>), typeof(uint*), typeof(Vector256<long>), typeof(Vector128<uint>), typeof(byte)}).
                            Invoke(null, new object[] { sourceui, Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexl256, maskui, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on uint with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl256, maskui, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl256, maskui, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourceui, (uint*)(intTable.inArrayPtr), indexl256, maskui, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on uint with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector256<long> index, Vector128<float> mask, byte scale)
                using (TestTable<float, long> floatTable = new TestTable<float, long>(floatSourceTable, new float[4]))
                {
                    var vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl256, maskf, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with Vector256 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherMaskVector128), new Type[] {typeof(Vector128<float>), typeof(float*), typeof(Vector256<long>), typeof(Vector128<float>), typeof(byte)}).
                            Invoke(null, new object[] { sourcef, Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexl256, maskf, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed with reflection on float with Vector256 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl256, maskf, 3);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl256, maskf, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherMaskVector128(sourcef, (float*)(floatTable.inArrayPtr), indexl256, maskf, invalid);
                        Console.WriteLine("AVX2 GatherMaskVector128 failed on float with invalid non-const scale (IMM) and Vector256 long index");
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
