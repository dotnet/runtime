// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.Avx1
{
    public partial class Program
    {
        [Fact]
        public static unsafe void LoadDquVector256()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable<int> intTable = new TestTable<int>(new int[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new int[8]))
                {
                    var vf = Avx.LoadDquVector256((int*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((int x, int y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<uint> intTable = new TestTable<uint>(new uint[8] { 1, 5, 100, 0, 1, 2, 3, 4 }, new uint[8]))
                {
                    var vf = Avx.LoadDquVector256((uint*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((uint x, uint y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<long> intTable = new TestTable<long>(new long[4] { 1, -5, 100, 0 }, new long[4]))
                {
                    var vf = Avx.LoadDquVector256((long*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((long x, long y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on long:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ulong> intTable = new TestTable<ulong>(new ulong[4] { 1, 5, 100, 0 }, new ulong[4]))
                {
                    var vf = Avx.LoadDquVector256((ulong*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((ulong x, ulong y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on ulong:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<short> intTable = new TestTable<short>(new short[16] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new short[16]))
                {
                    var vf = Avx.LoadDquVector256((short*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((short x, short y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on short:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> intTable = new TestTable<ushort>(new ushort[16] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new ushort[16]))
                {
                    var vf = Avx.LoadDquVector256((ushort*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((ushort x, ushort y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on ushort:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> intTable = new TestTable<byte>(new byte[32] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4 }, new byte[32]))
                {
                    var vf = Avx.LoadDquVector256((byte*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((byte x, byte y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on byte:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> intTable = new TestTable<sbyte>(new sbyte[32] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4 }, new sbyte[32]))
                {
                    var vf = Avx.LoadDquVector256((sbyte*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((sbyte x, sbyte y) => x == y))
                    {
                        Console.WriteLine("AVX LoadDquVector256 failed on sbyte:");
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
