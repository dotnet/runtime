// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
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

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[8] { float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN }))
                {
                    var vf1 = Avx.SetAllVector256<float>(-5);
                    Unsafe.Write(floatTable.outArrayPtr, vf1);

                    if (!floatTable.CheckResult((x) => (x == -5)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<double> doubleTable = new TestTable<double>(new double[4] { double.NaN, double.NaN, double.NaN, double.NaN }))
                {
                    var vf1 = Avx.SetAllVector256<double>(3);
                    Unsafe.Write(doubleTable.outArrayPtr, vf1);

                    if (!doubleTable.CheckResult((x) => (x == 3)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<sbyte> sbyteTable = new TestTable<sbyte>(new sbyte[32] { sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue,
                                                                                          sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue,
                                                                                          sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue,
                                                                                          sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue, sbyte.MaxValue }))
                {
                    var vf1 = Avx.SetAllVector256<sbyte>(100);
                    Unsafe.Write(sbyteTable.outArrayPtr, vf1);

                    if (!sbyteTable.CheckResult((x) => (x == 100)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on sbyte:");
                        foreach (var item in sbyteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<byte> byteTable = new TestTable<byte>(new byte[32] { byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                                                                      byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                                                                      byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
                                                                                      byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue }))
                {
                    Vector256<byte> vf1 = Avx.SetAllVector256<byte>(4);
                    Unsafe.Write(byteTable.outArrayPtr, vf1);

                    if (!byteTable.CheckResult((x) => (x == 4)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on byte:");
                        foreach (var item in byteTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<short> shortTable = new TestTable<short>(new short[16] { short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue,
                                                                                          short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue, short.MaxValue }))
                {
                    var vf1 = Avx.SetAllVector256<short>(-5);
                    Unsafe.Write(shortTable.outArrayPtr, vf1);

                    if (!shortTable.CheckResult((x) => (x == -5)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on short:");
                        foreach (var item in shortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> ushortTable = new TestTable<ushort>(new ushort[16] { ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue,
                                                                                              ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue }))
                {
                    Vector256<ushort> vf1 = Avx.SetAllVector256<ushort>(2);
                    Unsafe.Write(ushortTable.outArrayPtr, vf1);

                    if (!ushortTable.CheckResult((x) => (x == 2)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on ushort:");
                        foreach (var item in ushortTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<int> intTable = new TestTable<int>(new int[8] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue }))
                {
                    var vf1 = Avx.SetAllVector256<int>(-5);
                    Unsafe.Write(intTable.outArrayPtr, vf1);

                    if (!intTable.CheckResult((x) => (x == -5)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on int:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<uint> uintTable = new TestTable<uint>(new uint[8] { uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue }))
                {
                    Vector256<uint> vf1 = Avx.SetAllVector256<uint>(3);
                    Unsafe.Write(uintTable.outArrayPtr, vf1);

                    if (!uintTable.CheckResult((x) => (x == 3)))
                    {
                        Console.WriteLine("AVX SetAllVector256 failed on uint:");
                        foreach (var item in uintTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                if (Environment.Is64BitProcess)
                {
                    using (TestTable<long> longTable = new TestTable<long>(new long[4] { long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue }))
                    {
                        var vf1 = Avx.SetAllVector256<long>(-199);
                        Unsafe.Write(longTable.outArrayPtr, vf1);

                        if (!longTable.CheckResult((x) => (x == -199)))
                        {
                            Console.WriteLine("AVX SetAllVector256 failed on long:");
                            foreach (var item in longTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            testResult = Fail;
                        }
                    }

                    using (TestTable<ulong> ulongTable = new TestTable<ulong>(new ulong[4] { ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue }))
                    {
                        Vector256<ulong> vf1 = Avx.SetAllVector256<ulong>(34);
                       Unsafe.Write(ulongTable.outArrayPtr, vf1);
    
                        if (!ulongTable.CheckResult((x) => (x == 34)))
                        {
                            Console.WriteLine("AVX SetAllVector256 failed on ulong:");
                            foreach (var item in ulongTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            testResult = Fail;
                        }
                    }
                }
            }

            return testResult;
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
        {
            public T[] outArray;

            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle outHandle;
            public TestTable(T[] a)
            {
                this.outArray = a;

                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T, bool> check)
            {
                foreach (var item in outArray)
                {
                    if (!check(item))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                outHandle.Free();
            }
        }
    }
}
