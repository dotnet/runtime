// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program { public class GatherVector128
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

                // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<float, int> floatTable = new TestTable<float, int>(floatSourceTable, new float[4]))
                {
                    var vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexi, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(float*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexi, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on float with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexi, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on float with non-const scale (IMM):");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on float with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<double, int> doubletTable = new TestTable<double, int>(doubleSourceTable, new double[2]))
                {
                    var vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexi, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector128<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(double*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexi, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on double:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on double with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexi, Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on double with non-const scale (IMM):");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on double with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexi, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(int*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexi, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on int with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexi, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on int with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on int with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<int, int> intTable = new TestTable<int, int>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexi, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(uint*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexi, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexi, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with non-const scale (IMM):");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexi, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(long*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexi, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on long with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexi, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on long with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on long with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<int> index, byte scale)
                using (TestTable<long, int> longTable = new TestTable<long, int>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexi, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(ulong*), typeof(Vector128<int>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexi, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexi, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong with invalid scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexi, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, intIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong with non-const scale (IMM):");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexi, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<long> index, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on int with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(int*), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexl, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on int with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on int with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on int with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on int with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<long> index, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(uint*), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexl, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on uint with Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<long> index, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on long with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<long>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(long*), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(long*)), indexl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on long with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on long with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on long with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((long*)(longTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on long with invalid non-const scale (IMM)");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<long> index, byte scale)
                using (TestTable<long, long> longTable = new TestTable<long, long>(longSourceTable, new long[2]))
                {
                    var vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexl, 8);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<ulong>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(ulong*), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(longTable.inArrayPtr, typeof(ulong*)), indexl, (byte)8 });
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on ulong with Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexl, Eight);
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y, longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on ulong with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((ulong*)(longTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on long with invalid non-const scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<long> index, byte scale)
                using (TestTable<float, long> floatTable = new TestTable<float, long>(floatSourceTable, new float[4]))
                {
                    var vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on float with Vector128 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(float*), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexl, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on float with Vector128 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on float with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on float with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on float with invalid non-const scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<long> index, byte scale)
                using (TestTable<double, long> doubletTable = new TestTable<double, long>(doubleSourceTable, new double[2]))
                {
                    var vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexl, 8);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on double with Vector128 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vd = (Vector128<double>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(double*), typeof(Vector128<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(doubletTable.inArrayPtr, typeof(double*)), indexl, (byte)8 });
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on double with Vector128 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexl, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on double with invalid scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexl, Eight);
                    Unsafe.Write(doubletTable.outArrayPtr, vd);

                    if (!doubletTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y), longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on double with non-const scale (IMM) and Vector128 long index:");
                        foreach (var item in doubletTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vd = Avx2.GatherVector128((double*)(doubletTable.inArrayPtr), indexl, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on double with invalid non-const scale (IMM) and Vector128 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector256<long> index, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl256, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on int with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<int>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(int*), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(int*)), indexl256, (byte)4 });
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on int with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl256, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on int with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl256, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on int with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((int*)(intTable.inArrayPtr), indexl256, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on int with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector256<long> index, byte scale)
                using (TestTable<int, long> intTable = new TestTable<int, long>(intSourceTable, new int[4]))
                {
                    var vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl256, 4);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<uint>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(uint*), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(intTable.inArrayPtr, typeof(uint*)), indexl256, (byte)4 });
                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on uint with Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl256, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl256, Four);
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y, vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((uint*)(intTable.inArrayPtr), indexl256, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on uint with invalid non-const scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }
                }

                // public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector256<long> index, byte scale)
                using (TestTable<float, long> floatTable = new TestTable<float, long>(floatSourceTable, new float[4]))
                {
                    var vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl256, 4);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on float with Vector256 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    vf = (Vector128<float>)typeof(Avx2).GetMethod(nameof(Avx2.GatherVector128), new Type[] {typeof(float*), typeof(Vector256<long>), typeof(byte)}).
                            Invoke(null, new object[] { Pointer.Box(floatTable.inArrayPtr, typeof(float*)), indexl256, (byte)4 });
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed with reflection on float with Vector256 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl256, 3);
                        Console.WriteLine("AVX2 GatherVector128 failed on float with invalid scale (IMM) and Vector256 long index");
                        testResult = Fail;
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        // success
                    }

                    vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl256, Four);
                    Unsafe.Write(floatTable.outArrayPtr, vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y), vector256longIndexTable))
                    {
                        Console.WriteLine("AVX2 GatherVector128 failed on float with non-const scale (IMM) and Vector256 long index:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    try
                    {
                        vf = Avx2.GatherVector128((float*)(floatTable.inArrayPtr), indexl256, invalid);
                        Console.WriteLine("AVX2 GatherVector128 failed on float with invalid non-const scale (IMM) and Vector256 long index");
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

    } }
}
