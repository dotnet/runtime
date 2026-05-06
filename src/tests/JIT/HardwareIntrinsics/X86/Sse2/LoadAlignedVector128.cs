// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void LoadAlignedVector128()
        {
            int testResult = Pass;

            if (Sse2.IsSupported)
            {
                using (AlignedTestTable<double> doubleTable = new AlignedTestTable<double>(new double[2] { 1, -5 }, new double[2]))
                {
                    var vf = Sse2.LoadAlignedVector128((double*)(doubleTable.inArrayPtr));
                    Unsafe.Write(doubleTable.outArrayPtr, vf);

                    if (!doubleTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y)))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<int> intTable = new AlignedTestTable<int>(new int[4] { 1, -5, 100, 0 }, new int[4]))
                {
                    var vf = Sse2.LoadAlignedVector128((int*)(intTable.inArrayPtr));
                    Unsafe.Write(intTable.outArrayPtr, vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<long> longTable = new AlignedTestTable<long>(new long[2] { 1, -5 }, new long[2]))
                {
                    var vf = Sse2.LoadAlignedVector128((long*)(longTable.inArrayPtr));
                    Unsafe.Write(longTable.outArrayPtr, vf);

                    if (!longTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on long:");
                        foreach (var item in longTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<uint> uintTable = new AlignedTestTable<uint>(new uint[4] { 1, 5, 100, 0 }, new uint[4]))
                {
                    var vf = Sse2.LoadAlignedVector128((uint*)(uintTable.inArrayPtr));
                    Unsafe.Write(uintTable.outArrayPtr, vf);

                    if (!uintTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on uint:");
                        foreach (var item in uintTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<ulong> ulongTable = new AlignedTestTable<ulong>(new ulong[2] { 1, 5 }, new ulong[2]))
                {
                    var vf = Sse2.LoadAlignedVector128((ulong*)(ulongTable.inArrayPtr));
                    Unsafe.Write(ulongTable.outArrayPtr, vf);

                    if (!ulongTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on ulong:");
                        foreach (var item in ulongTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<short> shortTable = new AlignedTestTable<short>(new short[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new short[8]))
                {
                    var vf = Sse2.LoadAlignedVector128((short*)(shortTable.inArrayPtr));
                    Unsafe.Write(shortTable.outArrayPtr, vf);

                    if (!shortTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on short:");
                        foreach (var item in shortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<ushort> ushortTable = new AlignedTestTable<ushort>(new ushort[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new ushort[8]))
                {
                    var vf = Sse2.LoadAlignedVector128((ushort*)(ushortTable.inArrayPtr));
                    Unsafe.Write(ushortTable.outArrayPtr, vf);

                    if (!ushortTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on ushort:");
                        foreach (var item in ushortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<sbyte> sbyteTable = new AlignedTestTable<sbyte>(new sbyte[16] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new sbyte[16]))
                {
                    var vf = Sse2.LoadAlignedVector128((sbyte*)(sbyteTable.inArrayPtr));
                    Unsafe.Write(sbyteTable.outArrayPtr, vf);

                    if (!sbyteTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (AlignedTestTable<byte> byteTable = new AlignedTestTable<byte>(new byte[16] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new byte[16]))
                {
                    var vf = Sse2.LoadAlignedVector128((byte*)(byteTable.inArrayPtr));
                    Unsafe.Write(byteTable.outArrayPtr, vf);

                    if (!byteTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Sse2 LoadAlignedVector128 failed on byte:");
                        foreach (var item in byteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }

            if (testResult != Pass)
                Assert.Fail("");
        }
    }
}
