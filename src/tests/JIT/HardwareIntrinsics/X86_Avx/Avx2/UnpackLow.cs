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
        public static unsafe void UnpackLow()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                using (TestTable<byte, byte, byte> byteTable = new TestTable<byte, byte, byte>(new byte[32] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new byte[32] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new byte[32]))
                using (TestTable<sbyte, sbyte, sbyte> sbyteTable = new TestTable<sbyte, sbyte, sbyte>(new sbyte[32] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new sbyte[32] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new sbyte[32]))
                using (TestTable<short, short, short> shortTable = new TestTable<short, short, short>(new short[16] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new short[16] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0}, new short[16]))
                using (TestTable<ushort, ushort, ushort> ushortTable = new TestTable<ushort, ushort, ushort>(new ushort[16] { 1, 5, 100, 0, 1, 5, 100, 0,  1, 5, 100, 0, 1, 5, 100, 0 }, new ushort[16] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new ushort[16]))
                using (TestTable<int, int, int> intTable = new TestTable<int, int, int>(new int[8] { 1, 5, 100, 0, 1, 5, 100, 0}, new int[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new int[8]))
                using (TestTable<uint, uint, uint> uintTable = new TestTable<uint, uint, uint>(new uint[8] { 1, 5, 100, 0, 1, 5, 100, 0 }, new uint[8] { 22, 1, 50, 0, 22, 1, 50, 0 }, new uint[8]))
                using (TestTable<long, long, long> longTable = new TestTable<long, long, long>(new long[4] { 1, -5, 100, 0 }, new long[4] { 22, -1, -50, 0}, new long[4]))
                using (TestTable<ulong, ulong, ulong> ulongTable = new TestTable<ulong, ulong, ulong>(new ulong[4] { 1, 5, 100, 0 }, new ulong[4] { 22, 1, 50, 0 }, new ulong[4]))

                {

                    var vb1 = Unsafe.Read<Vector256<byte>>(byteTable.inArray1Ptr);
                    var vb2 = Unsafe.Read<Vector256<byte>>(byteTable.inArray2Ptr);
                    var vb3 = Avx2.UnpackLow(vb1, vb2);
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    var vsb1 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray1Ptr);
                    var vsb2 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray2Ptr);
                    var vsb3 = Avx2.UnpackLow(vsb1, vsb2);
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    var vs1 = Unsafe.Read<Vector256<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector256<short>>(shortTable.inArray2Ptr);
                    var vs3 = Avx2.UnpackLow(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    var vus1 = Unsafe.Read<Vector256<ushort>>(ushortTable.inArray1Ptr);
                    var vus2 = Unsafe.Read<Vector256<ushort>>(ushortTable.inArray2Ptr);
                    var vus3 = Avx2.UnpackLow(vus1, vus2);
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);
                    
                    var vi1 = Unsafe.Read<Vector256<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector256<int>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.UnpackLow(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector256<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector256<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx2.UnpackLow(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl1 = Unsafe.Read<Vector256<long>>(longTable.inArray1Ptr);
                    var vl2 = Unsafe.Read<Vector256<long>>(longTable.inArray2Ptr);
                    var vl3 = Avx2.UnpackLow(vl1, vl2);
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul1 = Unsafe.Read<Vector256<ulong>>(ulongTable.inArray1Ptr);
                    var vul2 = Unsafe.Read<Vector256<ulong>>(ulongTable.inArray2Ptr);
                    var vul3 = Avx2.UnpackLow(vul1, vul2);
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);
                    
                    if((byteTable.inArray1[0] != byteTable.outArray[0]) || (byteTable.inArray2[0] != byteTable.outArray[1]) ||
                        (byteTable.inArray1[1] != byteTable.outArray[2]) || (byteTable.inArray2[1] != byteTable.outArray[3]) ||
                        (byteTable.inArray1[2] != byteTable.outArray[4]) || (byteTable.inArray2[2] != byteTable.outArray[5]) ||
                        (byteTable.inArray1[3] != byteTable.outArray[6]) || (byteTable.inArray2[3] != byteTable.outArray[7]) ||
                        (byteTable.inArray1[4] != byteTable.outArray[8]) || (byteTable.inArray2[4] != byteTable.outArray[9]) ||
                        (byteTable.inArray1[5] != byteTable.outArray[10]) || (byteTable.inArray2[5] != byteTable.outArray[11]) ||
                        (byteTable.inArray1[6] != byteTable.outArray[12]) || (byteTable.inArray2[6] != byteTable.outArray[13]) ||
                        (byteTable.inArray1[7] != byteTable.outArray[14]) || (byteTable.inArray2[7] != byteTable.outArray[15]) ||
                        (byteTable.inArray1[16] != byteTable.outArray[16]) || (byteTable.inArray2[16] != byteTable.outArray[17]) ||
                        (byteTable.inArray1[17] != byteTable.outArray[18]) || (byteTable.inArray2[17] != byteTable.outArray[19]) ||
                        (byteTable.inArray1[18] != byteTable.outArray[20]) || (byteTable.inArray2[18] != byteTable.outArray[21]) ||
                        (byteTable.inArray1[19] != byteTable.outArray[22]) || (byteTable.inArray2[19] != byteTable.outArray[23]) ||
                        (byteTable.inArray1[20] != byteTable.outArray[24]) || (byteTable.inArray2[20] != byteTable.outArray[25]) ||
                        (byteTable.inArray1[21] != byteTable.outArray[26]) || (byteTable.inArray2[21] != byteTable.outArray[27]) ||
                        (byteTable.inArray1[22] != byteTable.outArray[28]) || (byteTable.inArray2[22] != byteTable.outArray[29]) ||
                        (byteTable.inArray1[23] != byteTable.outArray[30]) || (byteTable.inArray2[23] != byteTable.outArray[31]))
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on byte:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if((sbyteTable.inArray1[0] != sbyteTable.outArray[0]) || (sbyteTable.inArray2[0] != sbyteTable.outArray[1]) ||
                        (sbyteTable.inArray1[1] != sbyteTable.outArray[2]) || (sbyteTable.inArray2[1] != sbyteTable.outArray[3]) ||
                        (sbyteTable.inArray1[2] != sbyteTable.outArray[4]) || (sbyteTable.inArray2[2] != sbyteTable.outArray[5]) ||
                        (sbyteTable.inArray1[3] != sbyteTable.outArray[6]) || (sbyteTable.inArray2[3] != sbyteTable.outArray[7]) ||
                        (sbyteTable.inArray1[4] != sbyteTable.outArray[8]) || (sbyteTable.inArray2[4] != sbyteTable.outArray[9]) ||
                        (sbyteTable.inArray1[5] != sbyteTable.outArray[10]) || (sbyteTable.inArray2[5] != sbyteTable.outArray[11]) ||
                        (sbyteTable.inArray1[6] != sbyteTable.outArray[12]) || (sbyteTable.inArray2[6] != sbyteTable.outArray[13]) ||
                        (sbyteTable.inArray1[7] != sbyteTable.outArray[14]) || (sbyteTable.inArray2[7] != sbyteTable.outArray[15]) ||
                        (sbyteTable.inArray1[16] != sbyteTable.outArray[16]) || (sbyteTable.inArray2[16] != sbyteTable.outArray[17]) ||
                        (sbyteTable.inArray1[17] != sbyteTable.outArray[18]) || (sbyteTable.inArray2[17] != sbyteTable.outArray[19]) ||
                        (sbyteTable.inArray1[18] != sbyteTable.outArray[20]) || (sbyteTable.inArray2[18] != sbyteTable.outArray[21]) ||
                        (sbyteTable.inArray1[19] != sbyteTable.outArray[22]) || (sbyteTable.inArray2[19] != sbyteTable.outArray[23]) ||
                        (sbyteTable.inArray1[20] != sbyteTable.outArray[24]) || (sbyteTable.inArray2[20] != sbyteTable.outArray[25]) ||
                        (sbyteTable.inArray1[21] != sbyteTable.outArray[26]) || (sbyteTable.inArray2[21] != sbyteTable.outArray[27]) ||
                        (sbyteTable.inArray1[22] != sbyteTable.outArray[28]) || (sbyteTable.inArray2[22] != sbyteTable.outArray[29]) ||
                        (sbyteTable.inArray1[23] != sbyteTable.outArray[30]) || (sbyteTable.inArray2[23] != sbyteTable.outArray[31]))
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on sbyte:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    
                    if((shortTable.inArray1[0] != shortTable.outArray[0]) || (shortTable.inArray2[0] != shortTable.outArray[1]) ||
                        (shortTable.inArray1[1] != shortTable.outArray[2]) || (shortTable.inArray2[1] != shortTable.outArray[3]) ||
                        (shortTable.inArray1[2] != shortTable.outArray[4]) || (shortTable.inArray2[2] != shortTable.outArray[5]) ||
                        (shortTable.inArray1[3] != shortTable.outArray[6]) || (shortTable.inArray2[3] != shortTable.outArray[7]) ||
                        (shortTable.inArray1[8] != shortTable.outArray[8]) || (shortTable.inArray2[8] != shortTable.outArray[9]) ||
                        (shortTable.inArray1[9] != shortTable.outArray[10]) || (shortTable.inArray2[9] != shortTable.outArray[11]) ||
                        (shortTable.inArray1[10] != shortTable.outArray[12]) || (shortTable.inArray2[10] != shortTable.outArray[13]) ||
                        (shortTable.inArray1[11] != shortTable.outArray[14]) || (shortTable.inArray2[11] != shortTable.outArray[15]))
                       
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on short:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    
                    if((ushortTable.inArray1[0] !=   ushortTable.outArray[0]) || (ushortTable.inArray2[0] != ushortTable.outArray[1]) ||
                        (ushortTable.inArray1[1] !=  ushortTable.outArray[2]) || (ushortTable.inArray2[1] != ushortTable.outArray[3]) ||
                        (ushortTable.inArray1[2] !=  ushortTable.outArray[4]) || (ushortTable.inArray2[2] != ushortTable.outArray[5]) ||
                        (ushortTable.inArray1[3] !=  ushortTable.outArray[6]) || (ushortTable.inArray2[3] != ushortTable.outArray[7]) ||
                        (ushortTable.inArray1[8] != ushortTable.outArray[8]) || (ushortTable.inArray2[8] != ushortTable.outArray[9]) ||
                        (ushortTable.inArray1[9] != ushortTable.outArray[10]) || (ushortTable.inArray2[9] != ushortTable.outArray[11]) ||
                        (ushortTable.inArray1[10] != ushortTable.outArray[12]) || (ushortTable.inArray2[10] != ushortTable.outArray[13]) ||
                        (ushortTable.inArray1[11] != ushortTable.outArray[14]) || (ushortTable.inArray2[11] != ushortTable.outArray[15]))
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on ushort:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((intTable.inArray1[0] != intTable.outArray[0]) || (intTable.inArray2[0] != intTable.outArray[1]) ||
                        (intTable.inArray1[1] != intTable.outArray[2]) || (intTable.inArray2[1] != intTable.outArray[3]) ||
                        (intTable.inArray1[4] != intTable.outArray[4]) || (intTable.inArray2[4] != intTable.outArray[5]) ||
                        (intTable.inArray1[5] != intTable.outArray[6]) || (intTable.inArray2[5] != intTable.outArray[7]))
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((uintTable.inArray1[0] != uintTable.outArray[0]) || (uintTable.inArray2[0] != uintTable.outArray[1]) ||
                        (uintTable.inArray1[1] != uintTable.outArray[2]) || (uintTable.inArray2[1] != uintTable.outArray[3]) ||
                        (uintTable.inArray1[4] != uintTable.outArray[4]) || (uintTable.inArray2[4] != uintTable.outArray[5]) ||
                        (uintTable.inArray1[5] != uintTable.outArray[6]) || (uintTable.inArray2[5] != uintTable.outArray[7]))   
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((longTable.inArray1[0] != longTable.outArray[0]) || (longTable.inArray2[0] != longTable.outArray[1]) ||
                        (longTable.inArray1[2] != longTable.outArray[2]) || (longTable.inArray2[2] != longTable.outArray[3]) )
                         {
                            Console.WriteLine("AVX2 UnpackLow failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((ulongTable.inArray1[0] != ulongTable.outArray[0]) || (ulongTable.inArray2[0] != ulongTable.outArray[1]) ||
                        (ulongTable.inArray1[2] != ulongTable.outArray[2]) || (ulongTable.inArray2[2] != ulongTable.outArray[3]) )
                        {
                            Console.WriteLine("AVX2 UnpackLow failed on ulong:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }    
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
