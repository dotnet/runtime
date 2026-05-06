// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void Multiply()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                using (TestTable<int, int, long> intTable = new TestTable<int, int, long>(new int[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new int[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new long[4]))
                using (TestTable<uint, uint, ulong> uintTable = new TestTable<uint, uint, ulong>(new uint[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new uint[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new ulong[4]))
                {

                    var vi1 = Unsafe.Read<Vector256<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector256<int>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.Multiply(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector256<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector256<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx2.Multiply(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    for (int i = 0; i < intTable.outArray.Length; i++)
                    {
                        if (intTable.inArray1[i * 2] * intTable.inArray2[i * 2] != intTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 Multiply failed on int:");
                            foreach (var item in intTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }

                    for (int i = 0; i < uintTable.outArray.Length; i++)
                    {
                        if (uintTable.inArray1[i * 2] * uintTable.inArray2[i * 2] != uintTable.outArray[i])
                        {
                            Console.WriteLine("AVX2 Multiply failed on uint:");
                            foreach (var item in uintTable.outArray)
                            {
                                Console.Write(item + ", ");
                            }
                            Console.WriteLine();
                            Assert.Fail("");
                        }
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
