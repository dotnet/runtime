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
                using (TestTable<double> doubleTable = new TestTable<double>(new double[4] { 1, -5, 100, 0 }, new double[4]))
                {
                    var vf = Unsafe.Read<Vector256<double>>(doubleTable.inArrayPtr);
                    Avx.Store((double*)(doubleTable.outArrayPtr), vf);

                    if (!doubleTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y)))
                    {
                        Console.WriteLine("Avx Store failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<float> floatTable = new TestTable<float>(new float[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new float[8]))
                {
                    var vf = Unsafe.Read<Vector256<float>>(floatTable.inArrayPtr);
                    Avx.Store((float*)(floatTable.outArrayPtr), vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y)))
                    {
                        Console.WriteLine("Avx Store failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<long> intTable = new TestTable<long>(new long[4] { 1, -5, 100, 0 }, new long[4]))
                {
                    var vf = Unsafe.Read<Vector256<long>>(intTable.inArrayPtr);
                    Avx.Store((long*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on long:");
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
                    var vf = Unsafe.Read<Vector256<ulong>>(intTable.inArrayPtr);
                    Avx.Store((ulong*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on ulong:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<int> intTable = new TestTable<int>(new int[8] { 1, -5, 100, 0, 1, 2, 3, 4 }, new int[8]))
                {
                    var vf = Unsafe.Read<Vector256<int>>(intTable.inArrayPtr);
                    Avx.Store((int*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on int:");
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
                    var vf = Unsafe.Read<Vector256<uint>>(intTable.inArrayPtr);
                    Avx.Store((uint*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on uint:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<short> intTable = new TestTable<short>(new short[16] { 1, -5, 100, 0, 1, 2, 3, 4, 1, -5, 100, 0, 1, 2, 3, 4  }, new short[16]))
                {
                    var vf = Unsafe.Read<Vector256<short>>(intTable.inArrayPtr);
                    Avx.Store((short*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on short:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ushort> intTable = new TestTable<ushort>(new ushort[16] { 1, 5, 100, 0, 1, 2, 3, 4, 1, 5, 100, 0, 1, 2, 3, 4  }, new ushort[16]))
                {
                    var vf = Unsafe.Read<Vector256<ushort>>(intTable.inArrayPtr);
                    Avx.Store((ushort*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on ushort:");
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
                    var vf = Unsafe.Read<Vector256<sbyte>>(intTable.inArrayPtr);
                    Avx.Store((sbyte*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on sbyte:");
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
                    var vf = Unsafe.Read<Vector256<byte>>(intTable.inArrayPtr);
                    Avx.Store((byte*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => x == y))
                    {
                        Console.WriteLine("Avx Store failed on byte:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

            }

            return testResult;
        }

        public unsafe struct TestTable<T> : IDisposable where T : struct
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
            public bool CheckResult(Func<T, T, bool> check)
            {
                for (int i = 0; i < inArray.Length; i++)
                {
                    if (!check(inArray[i], outArray[i]))
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
