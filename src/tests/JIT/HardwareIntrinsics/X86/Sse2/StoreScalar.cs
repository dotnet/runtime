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
        public static unsafe void StoreScalar()
        {
            int testResult = Pass;

            if (Sse2.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2]))
                {
                    var vf = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    Sse2.StoreScalar((double*)(doubleTable.outArrayPtr), vf);

                    if (!doubleTable.CheckResult((x, y) => BitConverter.DoubleToInt64Bits(x[0]) == BitConverter.DoubleToInt64Bits(y[0])
                                                        && BitConverter.DoubleToInt64Bits(y[1]) == 0))
                    {
                        Console.WriteLine("Sse2 StoreScalar failed on double:");
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
                    Sse2.StoreScalar((long*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => y[0] == x[0] && y[1] == 0))
                    {
                        Console.WriteLine("Sse2 StoreScalar failed on long:");
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
                    Sse2.StoreScalar((ulong*)(intTable.outArrayPtr), vf);

                    if (!intTable.CheckResult((x, y) => y[0] == x[0] && y[1] == 0))
                    {
                        Console.WriteLine("Sse2 StoreScalar failed on ulong:");
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
