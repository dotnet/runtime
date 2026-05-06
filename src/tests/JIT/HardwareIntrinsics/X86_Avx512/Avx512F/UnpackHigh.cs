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
        public static unsafe void UnpackHigh()
        {
            int testResult = Pass;

            if (Avx512F.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[16] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new float[16] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new float[16]))
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new double[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new double[8]))
                {
                    var vf1 = Unsafe.Read<Vector512<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector512<float>>(floatTable.inArray2Ptr);
                    var vf3 = Avx512F.UnpackHigh(vf1, vf2);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((left, right, result) =>
                                                              (left[2]  == result[0])  && (right[2]  == result[1]) &&
                                                              (left[3]  == result[2])  && (right[3]  == result[3]) &&
                                                              (left[6]  == result[4])  && (right[6]  == result[5]) &&
                                                              (left[7]  == result[6])  && (right[7]  == result[7]) &&
                                                              (left[10] == result[8])  && (right[10] == result[9]) &&
                                                              (left[11] == result[10]) && (right[11] == result[11]) &&
                                                              (left[14] == result[12]) && (right[14] == result[13]) &&
                                                              (left[15] == result[14]) && (right[15] == result[15])))
                    {
                        Console.WriteLine("Avx512F UnpackHigh failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    var vd1 = Unsafe.Read<Vector512<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector512<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx512F.UnpackHigh(vd1, vd2);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    if (!doubleTable.CheckResult((left, right, result) =>
                                                              (left[1] == result[0]) && (right[1] == result[1]) &&
                                                              (left[3] == result[2]) && (right[3] == result[3]) &&
                                                              (left[5] == result[4]) && (right[5] == result[5]) &&
                                                              (left[7] == result[6]) && (right[7] == result[7])))
                    {
                        Console.WriteLine("Avx512F UnpackHigh failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable<int, int, int> intTable = new TestTable<int, int, int>(new int[16] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new int[16] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new int[16]))
                using (TestTable<uint, uint, uint> uintTable = new TestTable<uint, uint, uint>(new uint[16] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new uint[16] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new uint[16]))
                using (TestTable<long, long, long> longTable = new TestTable<long, long, long>(new long[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new long[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new long[8]))
                using (TestTable<ulong, ulong, ulong> ulongTable = new TestTable<ulong, ulong, ulong>(new ulong[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new ulong[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new ulong[8]))
                {
                    var vi1 = Unsafe.Read<Vector512<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector512<int>>(intTable.inArray2Ptr);
                    var vi3 = Avx512F.UnpackHigh(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector512<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector512<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx512F.UnpackHigh(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl1 = Unsafe.Read<Vector512<long>>(longTable.inArray1Ptr);
                    var vl2 = Unsafe.Read<Vector512<long>>(longTable.inArray2Ptr);
                    var vl3 = Avx512F.UnpackHigh(vl1, vl2);
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul1 = Unsafe.Read<Vector512<ulong>>(ulongTable.inArray1Ptr);
                    var vul2 = Unsafe.Read<Vector512<ulong>>(ulongTable.inArray2Ptr);
                    var vul3 = Avx512F.UnpackHigh(vul1, vul2);
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);

                    if ((intTable.inArray1[2]  != intTable.outArray[0])  || (intTable.inArray2[2]  != intTable.outArray[1]) ||
                        (intTable.inArray1[3]  != intTable.outArray[2])  || (intTable.inArray2[3]  != intTable.outArray[3]) ||
                        (intTable.inArray1[6]  != intTable.outArray[4])  || (intTable.inArray2[6]  != intTable.outArray[5]) ||
                        (intTable.inArray1[7]  != intTable.outArray[6])  || (intTable.inArray2[7]  != intTable.outArray[7]) ||
                        (intTable.inArray1[10] != intTable.outArray[8])  || (intTable.inArray2[10] != intTable.outArray[9]) ||
                        (intTable.inArray1[11] != intTable.outArray[10]) || (intTable.inArray2[11] != intTable.outArray[11]) ||
                        (intTable.inArray1[14] != intTable.outArray[12]) || (intTable.inArray2[14] != intTable.outArray[13]) ||
                        (intTable.inArray1[15] != intTable.outArray[14]) || (intTable.inArray2[15] != intTable.outArray[15]))
                        {
                            Console.WriteLine("Avx512F UnpackHigh failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", intTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", intTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", intTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((uintTable.inArray1[2]  != uintTable.outArray[0])  || (uintTable.inArray2[2]  != uintTable.outArray[1]) ||
                        (uintTable.inArray1[3]  != uintTable.outArray[2])  || (uintTable.inArray2[3]  != uintTable.outArray[3]) ||
                        (uintTable.inArray1[6]  != uintTable.outArray[4])  || (uintTable.inArray2[6]  != uintTable.outArray[5]) ||
                        (uintTable.inArray1[7]  != uintTable.outArray[6])  || (uintTable.inArray2[7]  != uintTable.outArray[7]) ||
                        (uintTable.inArray1[10] != uintTable.outArray[8])  || (uintTable.inArray2[10] != uintTable.outArray[9]) ||
                        (uintTable.inArray1[11] != uintTable.outArray[10]) || (uintTable.inArray2[11] != uintTable.outArray[11]) ||
                        (uintTable.inArray1[14] != uintTable.outArray[12]) || (uintTable.inArray2[14] != uintTable.outArray[13]) ||
                        (uintTable.inArray1[15] != uintTable.outArray[14]) || (uintTable.inArray2[15] != uintTable.outArray[15]))
                        {
                            Console.WriteLine("Avx512F UnpackHigh failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", uintTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", uintTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", uintTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((longTable.inArray1[1] != longTable.outArray[0]) || (longTable.inArray2[1] != longTable.outArray[1]) ||
                        (longTable.inArray1[3] != longTable.outArray[2]) || (longTable.inArray2[3] != longTable.outArray[3]) ||
                        (longTable.inArray1[5] != longTable.outArray[4]) || (longTable.inArray2[5] != longTable.outArray[5]) ||
                        (longTable.inArray1[7] != longTable.outArray[6]) || (longTable.inArray2[7] != longTable.outArray[7]))
                         {
                            Console.WriteLine("Avx512F UnpackHigh failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", longTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", longTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", longTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((ulongTable.inArray1[1] != ulongTable.outArray[0]) || (ulongTable.inArray2[1] != ulongTable.outArray[1]) ||
                        (ulongTable.inArray1[3] != ulongTable.outArray[2]) || (ulongTable.inArray2[3] != ulongTable.outArray[3]) ||
                        (ulongTable.inArray1[5] != ulongTable.outArray[4]) || (ulongTable.inArray2[5] != ulongTable.outArray[5]) ||
                        (ulongTable.inArray1[7] != ulongTable.outArray[6]) || (ulongTable.inArray2[7] != ulongTable.outArray[7]))
                        {
                            Console.WriteLine("Avx512F UnpackHigh failed on ulong:");
                            Console.WriteLine($"    left: ({string.Join(", ", ulongTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", ulongTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", ulongTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
