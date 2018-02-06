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

            if (Sse2.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2]))
                {
                    var vf = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    Sse2.StoreLow((double*)(doubleTable.outArrayPtr), vf);

                    if (!doubleTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x[0]) == BitConverter.DoubleToInt64Bits(y[0])
                                                        && BitConverter.DoubleToInt64Bits(y[1]) == 0))
                    {
                        Console.WriteLine("Sse2 StoreLow failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<long> intTable = new TestTable<long>(new long[2] { 1, -5 }, new long[2]))
                {
                    var vf = Unsafe.Read<Vector128<long>>(intTable.inArrayPtr);
                    Sse2.StoreLow((long*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => y[0] == x[0] && y[1] == 0))
                    {
                        Console.WriteLine("Sse2 StoreLow failed on long:");
                        foreach (var item in intTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<ulong> intTable = new TestTable<ulong>(new ulong[2] { 1, 5 }, new ulong[2]))
                {
                    var vf = Unsafe.Read<Vector128<ulong>>(intTable.inArrayPtr);
                    Sse2.StoreLow((ulong*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => y[0] == x[0] && y[1] == 0))
                    {
                        Console.WriteLine("Sse2 StoreLow failed on ulong:");
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
            public bool CheckResult(Func<T[], T[], bool> check)
            {
                return check(inArray, outArray);
            }

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }
        }

    }
}
