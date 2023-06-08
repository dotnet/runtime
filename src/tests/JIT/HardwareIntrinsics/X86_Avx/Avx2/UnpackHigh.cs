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
        public static unsafe void UnpackHigh()
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
                    var vb3 = Avx2.UnpackHigh(vb1, vb2);
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    var vsb1 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray1Ptr);
                    var vsb2 = Unsafe.Read<Vector256<sbyte>>(sbyteTable.inArray2Ptr);
                    var vsb3 = Avx2.UnpackHigh(vsb1, vsb2);
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    var vs1 = Unsafe.Read<Vector256<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector256<short>>(shortTable.inArray2Ptr);
                    var vs3 = Avx2.UnpackHigh(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    var vus1 = Unsafe.Read<Vector256<ushort>>(ushortTable.inArray1Ptr);
                    var vus2 = Unsafe.Read<Vector256<ushort>>(ushortTable.inArray2Ptr);
                    var vus3 = Avx2.UnpackHigh(vus1, vus2);
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);
                    
                    var vi1 = Unsafe.Read<Vector256<int>>(intTable.inArray1Ptr);
                    var vi2 = Unsafe.Read<Vector256<int>>(intTable.inArray2Ptr);
                    var vi3 = Avx2.UnpackHigh(vi1, vi2);
                    Unsafe.Write(intTable.outArrayPtr, vi3);

                    var vui1 = Unsafe.Read<Vector256<uint>>(uintTable.inArray1Ptr);
                    var vui2 = Unsafe.Read<Vector256<uint>>(uintTable.inArray2Ptr);
                    var vui3 = Avx2.UnpackHigh(vui1, vui2);
                    Unsafe.Write(uintTable.outArrayPtr, vui3);

                    var vl1 = Unsafe.Read<Vector256<long>>(longTable.inArray1Ptr);
                    var vl2 = Unsafe.Read<Vector256<long>>(longTable.inArray2Ptr);
                    var vl3 = Avx2.UnpackHigh(vl1, vl2);
                    Unsafe.Write(longTable.outArrayPtr, vl3);

                    var vul1 = Unsafe.Read<Vector256<ulong>>(ulongTable.inArray1Ptr);
                    var vul2 = Unsafe.Read<Vector256<ulong>>(ulongTable.inArray2Ptr);
                    var vul3 = Avx2.UnpackHigh(vul1, vul2);
                    Unsafe.Write(ulongTable.outArrayPtr, vul3);
                    
                    if((byteTable.inArray1[8] != byteTable.outArray[0]) || (byteTable.inArray2[8] != byteTable.outArray[1]) ||
                        (byteTable.inArray1[9] != byteTable.outArray[2]) || (byteTable.inArray2[9] != byteTable.outArray[3]) ||
                        (byteTable.inArray1[10] != byteTable.outArray[4]) || (byteTable.inArray2[10] != byteTable.outArray[5]) ||
                        (byteTable.inArray1[11] != byteTable.outArray[6]) || (byteTable.inArray2[11] != byteTable.outArray[7]) ||
                        (byteTable.inArray1[12] != byteTable.outArray[8]) || (byteTable.inArray2[12] != byteTable.outArray[9]) ||
                        (byteTable.inArray1[13] != byteTable.outArray[10]) || (byteTable.inArray2[13] != byteTable.outArray[11]) ||
                        (byteTable.inArray1[14] != byteTable.outArray[12]) || (byteTable.inArray2[14] != byteTable.outArray[13]) ||
                        (byteTable.inArray1[15] != byteTable.outArray[14]) || (byteTable.inArray2[15] != byteTable.outArray[15]) ||
                        (byteTable.inArray1[24] != byteTable.outArray[16]) || (byteTable.inArray2[24] != byteTable.outArray[17]) ||
                        (byteTable.inArray1[25] != byteTable.outArray[18]) || (byteTable.inArray2[25] != byteTable.outArray[19]) ||
                        (byteTable.inArray1[26] != byteTable.outArray[20]) || (byteTable.inArray2[26] != byteTable.outArray[21]) ||
                        (byteTable.inArray1[27] != byteTable.outArray[22]) || (byteTable.inArray2[27] != byteTable.outArray[23]) ||
                        (byteTable.inArray1[28] != byteTable.outArray[24]) || (byteTable.inArray2[28] != byteTable.outArray[25]) ||
                        (byteTable.inArray1[29] != byteTable.outArray[26]) || (byteTable.inArray2[29] != byteTable.outArray[27]) ||
                        (byteTable.inArray1[30] != byteTable.outArray[28]) || (byteTable.inArray2[30] != byteTable.outArray[29]) ||
                        (byteTable.inArray1[31] != byteTable.outArray[30]) || (byteTable.inArray2[31] != byteTable.outArray[31]))
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on byte:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                         }
                        
                    if((sbyteTable.inArray1[8] != sbyteTable.outArray[0]) || (sbyteTable.inArray2[8] != sbyteTable.outArray[1]) ||
                        (sbyteTable.inArray1[9] != sbyteTable.outArray[2]) || (sbyteTable.inArray2[9] != sbyteTable.outArray[3]) ||
                        (sbyteTable.inArray1[10] != sbyteTable.outArray[4]) || (sbyteTable.inArray2[10] != sbyteTable.outArray[5]) ||
                        (sbyteTable.inArray1[11] != sbyteTable.outArray[6]) || (sbyteTable.inArray2[11] != sbyteTable.outArray[7]) ||
                        (sbyteTable.inArray1[12] != sbyteTable.outArray[8]) || (sbyteTable.inArray2[12] != sbyteTable.outArray[9]) ||
                        (sbyteTable.inArray1[13] != sbyteTable.outArray[10]) || (sbyteTable.inArray2[13] != sbyteTable.outArray[11]) ||
                        (sbyteTable.inArray1[14] != sbyteTable.outArray[12]) || (sbyteTable.inArray2[14] != sbyteTable.outArray[13]) ||
                        (sbyteTable.inArray1[15] != sbyteTable.outArray[14]) || (sbyteTable.inArray2[15] != sbyteTable.outArray[15]) ||
                        (sbyteTable.inArray1[24] != sbyteTable.outArray[16]) || (sbyteTable.inArray2[24] != sbyteTable.outArray[17]) ||
                        (sbyteTable.inArray1[25] != sbyteTable.outArray[18]) || (sbyteTable.inArray2[25] != sbyteTable.outArray[19]) ||
                        (sbyteTable.inArray1[26] != sbyteTable.outArray[20]) || (sbyteTable.inArray2[26] != sbyteTable.outArray[21]) ||
                        (sbyteTable.inArray1[27] != sbyteTable.outArray[22]) || (sbyteTable.inArray2[27] != sbyteTable.outArray[23]) ||
                        (sbyteTable.inArray1[28] != sbyteTable.outArray[24]) || (sbyteTable.inArray2[28] != sbyteTable.outArray[25]) ||
                        (sbyteTable.inArray1[29] != sbyteTable.outArray[26]) || (sbyteTable.inArray2[29] != sbyteTable.outArray[27]) ||
                        (sbyteTable.inArray1[30] != sbyteTable.outArray[28]) || (sbyteTable.inArray2[30] != sbyteTable.outArray[29]) ||
                        (sbyteTable.inArray1[31] != sbyteTable.outArray[30]) || (sbyteTable.inArray2[31] != sbyteTable.outArray[31]))
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on sbyte:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    
                    if((shortTable.inArray1[4] != shortTable.outArray[0]) || (shortTable.inArray2[4] != shortTable.outArray[1]) ||
                        (shortTable.inArray1[5] != shortTable.outArray[2]) || (shortTable.inArray2[5] != shortTable.outArray[3]) ||
                        (shortTable.inArray1[6] != shortTable.outArray[4]) || (shortTable.inArray2[6] != shortTable.outArray[5]) ||
                        (shortTable.inArray1[7] != shortTable.outArray[6]) || (shortTable.inArray2[7] != shortTable.outArray[7]) ||
                        (shortTable.inArray1[12] != shortTable.outArray[8]) || (shortTable.inArray2[12] != shortTable.outArray[9]) ||
                        (shortTable.inArray1[13] != shortTable.outArray[10]) || (shortTable.inArray2[13] != shortTable.outArray[11]) ||
                        (shortTable.inArray1[14] != shortTable.outArray[12]) || (shortTable.inArray2[14] != shortTable.outArray[13]) ||
                        (shortTable.inArray1[15] != shortTable.outArray[14]) || (shortTable.inArray2[15] != shortTable.outArray[15]))
                       
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on short:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                    
                    if((ushortTable.inArray1[4] !=   ushortTable.outArray[0]) || (ushortTable.inArray2[4] != ushortTable.outArray[1]) ||
                        (ushortTable.inArray1[5] !=  ushortTable.outArray[2]) || (ushortTable.inArray2[5] != ushortTable.outArray[3]) ||
                        (ushortTable.inArray1[6] !=  ushortTable.outArray[4]) || (ushortTable.inArray2[6] != ushortTable.outArray[5]) ||
                        (ushortTable.inArray1[7] !=  ushortTable.outArray[6]) || (ushortTable.inArray2[7] != ushortTable.outArray[7]) ||
                        (ushortTable.inArray1[12] != ushortTable.outArray[8]) || (ushortTable.inArray2[12] != ushortTable.outArray[9]) ||
                        (ushortTable.inArray1[13] != ushortTable.outArray[10]) || (ushortTable.inArray2[13] != ushortTable.outArray[11]) ||
                        (ushortTable.inArray1[14] != ushortTable.outArray[12]) || (ushortTable.inArray2[14] != ushortTable.outArray[13]) ||
                        (ushortTable.inArray1[15] != ushortTable.outArray[14]) || (ushortTable.inArray2[15] != ushortTable.outArray[15]))
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on ushort:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((intTable.inArray1[2] != intTable.outArray[0]) || (intTable.inArray2[2] != intTable.outArray[1]) ||
                        (intTable.inArray1[3] != intTable.outArray[2]) || (intTable.inArray2[3] != intTable.outArray[3]) ||
                        (intTable.inArray1[6] != intTable.outArray[4]) || (intTable.inArray2[6] != intTable.outArray[5]) ||
                        (intTable.inArray1[7] != intTable.outArray[6]) || (intTable.inArray2[7] != intTable.outArray[7]))
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on int:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((uintTable.inArray1[2] != uintTable.outArray[0]) || (uintTable.inArray2[2] != uintTable.outArray[1]) ||
                        (uintTable.inArray1[3] != uintTable.outArray[2]) || (uintTable.inArray2[3] != uintTable.outArray[3]) ||
                        (uintTable.inArray1[6] != uintTable.outArray[4]) || (uintTable.inArray2[6] != uintTable.outArray[5]) ||
                        (uintTable.inArray1[7] != uintTable.outArray[6]) || (uintTable.inArray2[7] != uintTable.outArray[7]))   
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on uint:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((longTable.inArray1[1] != longTable.outArray[0]) || (longTable.inArray2[1] != longTable.outArray[1]) ||
                        (longTable.inArray1[3] != longTable.outArray[2]) || (longTable.inArray2[3] != longTable.outArray[3]) )
                         {
                            Console.WriteLine("AVX2 UnpackHigh failed on long:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                        
                    if ((ulongTable.inArray1[1] != ulongTable.outArray[0]) || (ulongTable.inArray2[1] != ulongTable.outArray[1]) ||
                        (ulongTable.inArray1[3] != ulongTable.outArray[2]) || (ulongTable.inArray2[3] != ulongTable.outArray[3]) )
                        {
                            Console.WriteLine("AVX2 UnpackHigh failed on ulong:");
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
