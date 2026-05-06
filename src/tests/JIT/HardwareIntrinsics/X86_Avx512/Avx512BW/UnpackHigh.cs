// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx512BW
{
    public partial class Program
    {
        [Fact]
        public static unsafe void UnpackHigh()
        {
            int testResult = Pass;

            if (Avx512BW.IsSupported)
            {
                using (TestTable<byte, byte, byte> byteTable = new TestTable<byte, byte, byte>(new byte[64] { 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0 }, new byte[64] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new byte[64]))
                using (TestTable<sbyte, sbyte, sbyte> sbyteTable = new TestTable<sbyte, sbyte, sbyte>(new sbyte[64] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new sbyte[64] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new sbyte[64]))
                using (TestTable<short, short, short> shortTable = new TestTable<short, short, short>(new short[32] { 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0, 1, -5, 100, 0 }, new short[32] { 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0, 22, -1, -50, 0 }, new short[32]))
                using (TestTable<ushort, ushort, ushort> ushortTable = new TestTable<ushort, ushort, ushort>(new ushort[32] { 1, 5, 100, 0, 1, 5, 100, 0,  1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0, 1, 5, 100, 0,  1, 5, 100, 0, 1, 5, 100, 0 }, new ushort[32] { 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0, 22, 1, 50, 0 }, new ushort[32]))
                {
                    var vb1 = Unsafe.Read<Vector512<byte>>(byteTable.inArray1Ptr);
                    var vb2 = Unsafe.Read<Vector512<byte>>(byteTable.inArray2Ptr);
                    var vb3 = Avx512BW.UnpackHigh(vb1, vb2);
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    var vsb1 = Unsafe.Read<Vector512<sbyte>>(sbyteTable.inArray1Ptr);
                    var vsb2 = Unsafe.Read<Vector512<sbyte>>(sbyteTable.inArray2Ptr);
                    var vsb3 = Avx512BW.UnpackHigh(vsb1, vsb2);
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    var vs1 = Unsafe.Read<Vector512<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector512<short>>(shortTable.inArray2Ptr);
                    var vs3 = Avx512BW.UnpackHigh(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    var vus1 = Unsafe.Read<Vector512<ushort>>(ushortTable.inArray1Ptr);
                    var vus2 = Unsafe.Read<Vector512<ushort>>(ushortTable.inArray2Ptr);
                    var vus3 = Avx512BW.UnpackHigh(vus1, vus2);
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);

                    if ((byteTable.inArray1[8]  != byteTable.outArray[0])  || (byteTable.inArray2[8]  != byteTable.outArray[1]) ||
                        (byteTable.inArray1[9]  != byteTable.outArray[2])  || (byteTable.inArray2[9]  != byteTable.outArray[3]) ||
                        (byteTable.inArray1[10] != byteTable.outArray[4])  || (byteTable.inArray2[10] != byteTable.outArray[5]) ||
                        (byteTable.inArray1[11] != byteTable.outArray[6])  || (byteTable.inArray2[11] != byteTable.outArray[7]) ||
                        (byteTable.inArray1[12] != byteTable.outArray[8])  || (byteTable.inArray2[12] != byteTable.outArray[9]) ||
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
                        (byteTable.inArray1[31] != byteTable.outArray[30]) || (byteTable.inArray2[31] != byteTable.outArray[31]) ||
                        (byteTable.inArray1[40] != byteTable.outArray[32]) || (byteTable.inArray2[40] != byteTable.outArray[33]) ||
                        (byteTable.inArray1[41] != byteTable.outArray[34]) || (byteTable.inArray2[41] != byteTable.outArray[35]) ||
                        (byteTable.inArray1[42] != byteTable.outArray[36]) || (byteTable.inArray2[42] != byteTable.outArray[37]) ||
                        (byteTable.inArray1[43] != byteTable.outArray[38]) || (byteTable.inArray2[43] != byteTable.outArray[39]) ||
                        (byteTable.inArray1[44] != byteTable.outArray[40]) || (byteTable.inArray2[44] != byteTable.outArray[41]) ||
                        (byteTable.inArray1[45] != byteTable.outArray[42]) || (byteTable.inArray2[45] != byteTable.outArray[43]) ||
                        (byteTable.inArray1[46] != byteTable.outArray[44]) || (byteTable.inArray2[46] != byteTable.outArray[45]) ||
                        (byteTable.inArray1[47] != byteTable.outArray[46]) || (byteTable.inArray2[47] != byteTable.outArray[47]) ||
                        (byteTable.inArray1[56] != byteTable.outArray[48]) || (byteTable.inArray2[56] != byteTable.outArray[49]) ||
                        (byteTable.inArray1[57] != byteTable.outArray[50]) || (byteTable.inArray2[57] != byteTable.outArray[51]) ||
                        (byteTable.inArray1[58] != byteTable.outArray[52]) || (byteTable.inArray2[58] != byteTable.outArray[53]) ||
                        (byteTable.inArray1[59] != byteTable.outArray[54]) || (byteTable.inArray2[59] != byteTable.outArray[55]) ||
                        (byteTable.inArray1[60] != byteTable.outArray[56]) || (byteTable.inArray2[60] != byteTable.outArray[57]) ||
                        (byteTable.inArray1[61] != byteTable.outArray[58]) || (byteTable.inArray2[61] != byteTable.outArray[59]) ||
                        (byteTable.inArray1[62] != byteTable.outArray[60]) || (byteTable.inArray2[62] != byteTable.outArray[61]) ||
                        (byteTable.inArray1[63] != byteTable.outArray[62]) || (byteTable.inArray2[63] != byteTable.outArray[63]))
                        {
                            Console.WriteLine("Avx512BW UnpackHigh failed on byte:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                         }

                    if ((sbyteTable.inArray1[8]  != sbyteTable.outArray[0])  || (sbyteTable.inArray2[8]  != sbyteTable.outArray[1]) ||
                        (sbyteTable.inArray1[9]  != sbyteTable.outArray[2])  || (sbyteTable.inArray2[9]  != sbyteTable.outArray[3]) ||
                        (sbyteTable.inArray1[10] != sbyteTable.outArray[4])  || (sbyteTable.inArray2[10] != sbyteTable.outArray[5]) ||
                        (sbyteTable.inArray1[11] != sbyteTable.outArray[6])  || (sbyteTable.inArray2[11] != sbyteTable.outArray[7]) ||
                        (sbyteTable.inArray1[12] != sbyteTable.outArray[8])  || (sbyteTable.inArray2[12] != sbyteTable.outArray[9]) ||
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
                        (sbyteTable.inArray1[31] != sbyteTable.outArray[30]) || (sbyteTable.inArray2[31] != sbyteTable.outArray[31]) ||
                        (sbyteTable.inArray1[40] != sbyteTable.outArray[32]) || (sbyteTable.inArray2[40] != sbyteTable.outArray[33]) ||
                        (sbyteTable.inArray1[41] != sbyteTable.outArray[34]) || (sbyteTable.inArray2[41] != sbyteTable.outArray[35]) ||
                        (sbyteTable.inArray1[42] != sbyteTable.outArray[36]) || (sbyteTable.inArray2[42] != sbyteTable.outArray[37]) ||
                        (sbyteTable.inArray1[43] != sbyteTable.outArray[38]) || (sbyteTable.inArray2[43] != sbyteTable.outArray[39]) ||
                        (sbyteTable.inArray1[44] != sbyteTable.outArray[40]) || (sbyteTable.inArray2[44] != sbyteTable.outArray[41]) ||
                        (sbyteTable.inArray1[45] != sbyteTable.outArray[42]) || (sbyteTable.inArray2[45] != sbyteTable.outArray[43]) ||
                        (sbyteTable.inArray1[46] != sbyteTable.outArray[44]) || (sbyteTable.inArray2[46] != sbyteTable.outArray[45]) ||
                        (sbyteTable.inArray1[47] != sbyteTable.outArray[46]) || (sbyteTable.inArray2[47] != sbyteTable.outArray[47]) ||
                        (sbyteTable.inArray1[56] != sbyteTable.outArray[48]) || (sbyteTable.inArray2[56] != sbyteTable.outArray[49]) ||
                        (sbyteTable.inArray1[57] != sbyteTable.outArray[50]) || (sbyteTable.inArray2[57] != sbyteTable.outArray[51]) ||
                        (sbyteTable.inArray1[58] != sbyteTable.outArray[52]) || (sbyteTable.inArray2[58] != sbyteTable.outArray[53]) ||
                        (sbyteTable.inArray1[59] != sbyteTable.outArray[54]) || (sbyteTable.inArray2[59] != sbyteTable.outArray[55]) ||
                        (sbyteTable.inArray1[60] != sbyteTable.outArray[56]) || (sbyteTable.inArray2[60] != sbyteTable.outArray[57]) ||
                        (sbyteTable.inArray1[61] != sbyteTable.outArray[58]) || (sbyteTable.inArray2[61] != sbyteTable.outArray[59]) ||
                        (sbyteTable.inArray1[62] != sbyteTable.outArray[60]) || (sbyteTable.inArray2[62] != sbyteTable.outArray[61]) ||
                        (sbyteTable.inArray1[63] != sbyteTable.outArray[62]) || (sbyteTable.inArray2[63] != sbyteTable.outArray[63]))
                        {
                            Console.WriteLine("Avx512BW UnpackHigh failed on sbyte:");
                            Console.WriteLine($"    left: ({string.Join(", ", sbyteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", sbyteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", sbyteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((shortTable.inArray1[4]  != shortTable.outArray[0])  || (shortTable.inArray2[4]  != shortTable.outArray[1]) ||
                        (shortTable.inArray1[5]  != shortTable.outArray[2])  || (shortTable.inArray2[5]  != shortTable.outArray[3]) ||
                        (shortTable.inArray1[6]  != shortTable.outArray[4])  || (shortTable.inArray2[6]  != shortTable.outArray[5]) ||
                        (shortTable.inArray1[7]  != shortTable.outArray[6])  || (shortTable.inArray2[7]  != shortTable.outArray[7]) ||
                        (shortTable.inArray1[12] != shortTable.outArray[8])  || (shortTable.inArray2[12] != shortTable.outArray[9]) ||
                        (shortTable.inArray1[13] != shortTable.outArray[10]) || (shortTable.inArray2[13] != shortTable.outArray[11]) ||
                        (shortTable.inArray1[14] != shortTable.outArray[12]) || (shortTable.inArray2[14] != shortTable.outArray[13]) ||
                        (shortTable.inArray1[15] != shortTable.outArray[14]) || (shortTable.inArray2[15] != shortTable.outArray[15]) ||
                        (shortTable.inArray1[20] != shortTable.outArray[16]) || (shortTable.inArray2[20] != shortTable.outArray[17]) ||
                        (shortTable.inArray1[21] != shortTable.outArray[18]) || (shortTable.inArray2[21] != shortTable.outArray[19]) ||
                        (shortTable.inArray1[22] != shortTable.outArray[20]) || (shortTable.inArray2[22] != shortTable.outArray[21]) ||
                        (shortTable.inArray1[23] != shortTable.outArray[22]) || (shortTable.inArray2[23] != shortTable.outArray[23]) ||
                        (shortTable.inArray1[28] != shortTable.outArray[24]) || (shortTable.inArray2[28] != shortTable.outArray[25]) ||
                        (shortTable.inArray1[29] != shortTable.outArray[26]) || (shortTable.inArray2[29] != shortTable.outArray[27]) ||
                        (shortTable.inArray1[30] != shortTable.outArray[28]) || (shortTable.inArray2[30] != shortTable.outArray[29]) ||
                        (shortTable.inArray1[31] != shortTable.outArray[30]) || (shortTable.inArray2[31] != shortTable.outArray[31]))
                        {
                            Console.WriteLine("Avx512BW UnpackHigh failed on short:");
                            Console.WriteLine($"    left: ({string.Join(", ", shortTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", shortTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", shortTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((ushortTable.inArray1[4]  != ushortTable.outArray[0])  || (ushortTable.inArray2[4]  != ushortTable.outArray[1]) ||
                        (ushortTable.inArray1[5]  != ushortTable.outArray[2])  || (ushortTable.inArray2[5]  != ushortTable.outArray[3]) ||
                        (ushortTable.inArray1[6]  != ushortTable.outArray[4])  || (ushortTable.inArray2[6]  != ushortTable.outArray[5]) ||
                        (ushortTable.inArray1[7]  != ushortTable.outArray[6])  || (ushortTable.inArray2[7]  != ushortTable.outArray[7]) ||
                        (ushortTable.inArray1[12] != ushortTable.outArray[8])  || (ushortTable.inArray2[12] != ushortTable.outArray[9]) ||
                        (ushortTable.inArray1[13] != ushortTable.outArray[10]) || (ushortTable.inArray2[13] != ushortTable.outArray[11]) ||
                        (ushortTable.inArray1[14] != ushortTable.outArray[12]) || (ushortTable.inArray2[14] != ushortTable.outArray[13]) ||
                        (ushortTable.inArray1[15] != ushortTable.outArray[14]) || (ushortTable.inArray2[15] != ushortTable.outArray[15]) ||
                        (ushortTable.inArray1[20] != ushortTable.outArray[16]) || (ushortTable.inArray2[20] != ushortTable.outArray[17]) ||
                        (ushortTable.inArray1[21] != ushortTable.outArray[18]) || (ushortTable.inArray2[21] != ushortTable.outArray[19]) ||
                        (ushortTable.inArray1[22] != ushortTable.outArray[20]) || (ushortTable.inArray2[22] != ushortTable.outArray[21]) ||
                        (ushortTable.inArray1[23] != ushortTable.outArray[22]) || (ushortTable.inArray2[23] != ushortTable.outArray[23]) ||
                        (ushortTable.inArray1[28] != ushortTable.outArray[24]) || (ushortTable.inArray2[28] != ushortTable.outArray[25]) ||
                        (ushortTable.inArray1[29] != ushortTable.outArray[26]) || (ushortTable.inArray2[29] != ushortTable.outArray[27]) ||
                        (ushortTable.inArray1[30] != ushortTable.outArray[28]) || (ushortTable.inArray2[30] != ushortTable.outArray[29]) ||
                        (ushortTable.inArray1[31] != ushortTable.outArray[30]) || (ushortTable.inArray2[31] != ushortTable.outArray[31]))
                        {
                            Console.WriteLine("Avx512BW UnpackHigh failed on ushort:");
                            Console.WriteLine($"    left: ({string.Join(", ", ushortTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", ushortTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", ushortTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
