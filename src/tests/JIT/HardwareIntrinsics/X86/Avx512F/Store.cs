// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx512F
{
    public partial class Program
    {
        [Fact]
        public static unsafe void Store()
        {
            int testResult = Pass;

            if (Avx512F.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new double[8]))
                {
                    var vf = Unsafe.Read<Vector512<double>>(doubleTable.inArrayPtr);
                    Avx512F.Store((double*)(doubleTable.outArrayPtr), vf);

                    if (!doubleTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y)))
                    {
                        Console.WriteLine("AVX512F Store failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<float> floatTable = new TestTable<float>(new float[16] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new float[16]))
                {
                    var vf = Unsafe.Read<Vector512<float>>(floatTable.inArrayPtr);
                    Avx512F.Store((float*)(floatTable.outArrayPtr), vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y)))
                    {
                        Console.WriteLine("AVX512F Store failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<long> intTable = new TestTable<long>(new long[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new long[8]))
                {
                    var vf = Unsafe.Read<Vector512<long>>(intTable.inArrayPtr);
                    Avx512F.Store((long*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((long x, long y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on long:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ulong> intTable = new TestTable<ulong>(new ulong[8] { 1, 5, 100, 0, 1, 2, 3, 4 }, new ulong[8]))
                {
                    var vf = Unsafe.Read<Vector512<ulong>>(intTable.inArrayPtr);
                    Avx512F.Store((ulong*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((ulong x, ulong y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on ulong:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<int> intTable = new TestTable<int>(new int[16] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new int[16]))
                {
                    var vf = Unsafe.Read<Vector512<int>>(intTable.inArrayPtr);
                    Avx512F.Store((int*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((int x, int y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<uint> intTable = new TestTable<uint>(new uint[16] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new uint[16]))
                {
                    var vf = Unsafe.Read<Vector512<uint>>(intTable.inArrayPtr);
                    Avx512F.Store((uint*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((uint x, uint y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<short> intTable = new TestTable<short>(new short[32] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new short[32]))
                {
                    var vf = Unsafe.Read<Vector512<short>>(intTable.inArrayPtr);
                    Avx512F.Store((short*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((short x, short y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on short:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> intTable = new TestTable<ushort>(new ushort[32] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new ushort[32]))
                {
                    var vf = Unsafe.Read<Vector512<ushort>>(intTable.inArrayPtr);
                    Avx512F.Store((ushort*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((ushort x, ushort y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on ushort:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> intTable = new TestTable<sbyte>(new sbyte[64] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new sbyte[64]))
                {
                    var vf = Unsafe.Read<Vector512<sbyte>>(intTable.inArrayPtr);
                    Avx512F.Store((sbyte*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((sbyte x, sbyte y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on sbyte:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> intTable = new TestTable<byte>(new byte[64] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new byte[64]))
                {
                    var vf = Unsafe.Read<Vector512<byte>>(intTable.inArrayPtr);
                    Avx512F.Store((byte*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((byte x, byte y) => x == y))
                    {
                        Console.WriteLine("AVX512F Store failed on byte:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

            }

            Assert.Equal(Pass, testResult);
        }
    }
}
