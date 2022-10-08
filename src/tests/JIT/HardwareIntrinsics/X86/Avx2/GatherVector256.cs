// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

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

        static unsafe int Main(string[] args)
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

                // public static unsafe Vector256<float> GatherVector256(float* baseAddress, Vector256<int> index, byte scale)
                using (TestTable<float, int> floatTable = new TestTable<float, int>(floatSourceTable, new float[8]))
                {
                    var vf = Avx2.GatherVector256((float*)(floatTable.inArrayPtr), indexi, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(float*), typeof(Vector256<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexi, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((float*)(floatTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on float with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((float*)(floatTable.inArrayPtr), indexi, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on float with non-const scale (IMM):");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((float*)(floatTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on float with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<double, int> doubletTable = new TestTable<double, int>(doubleSourceTable, new double[4]))
                {
                    var vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexi128, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector256<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(double*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexi128, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexi128, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on double with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexi128, Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on double with non-const scale (IMM):");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexi128, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on double with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<int> GatherVector256(int* baseAddress, Vector256<int> index, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[8]))
                {
                    var vf = Avx2.GatherVector256((int*)(intTable.inArrayPtr), indexi, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(int*), typeof(Vector256<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexi, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((int*)(intTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on int with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((int*)(intTable.inArrayPtr), indexi, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on int with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((int*)(intTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on int with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<uint> GatherVector256(uint* baseAddress, Vector256<int> index, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[8]))
                {
                    var vf = Avx2.GatherVector256((uint*)(intTable.inArrayPtr), indexi, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(uint*), typeof(Vector256<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexi, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((uint*)(intTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on uint with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((uint*)(intTable.inArrayPtr), indexi, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on uint with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((uint*)(intTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on uint with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexi128, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(long*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexi128, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexi128, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on long with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexi128, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on long with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexi128, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on long with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexi128, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(ulong*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexi128, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexi128, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexi128, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, vector128intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexi128, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector256<long> index, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on long with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(long*), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on long with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on long with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on long with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((long*)(longTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on long with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector256<long> index, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[4]))
                {
                    var vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector256<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(ulong*), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on ulong with Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on ulong with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector256((ulong*)(longTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on long with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector256<long> index, byte scale)
                using (TestTable<double, long> doubletTable = new TestTable<double, long>(doubleSourceTable, new double[4]))
                {
                    var vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexl, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on double with Vector256 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector256<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector256), new Type[] {typeof(double*), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexl, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed with reflection on double with Vector256 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector256 failed on double with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexl, Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector256 failed on double with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector256((double*)(doubletTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector256 failed on double with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

            }

            return testResult;
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
                    if (!check(inArray[Convert.ToInt32(indexArray[i])], outArray[i]))
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

    }
}
