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
        public static unsafe void UnpackLow()
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
                    var vb3 = Avx512BW.UnpackLow(vb1, vb2);
                    Unsafe.Write(byteTable.outArrayPtr, vb3);

                    var vsb1 = Unsafe.Read<Vector512<sbyte>>(sbyteTable.inArray1Ptr);
                    var vsb2 = Unsafe.Read<Vector512<sbyte>>(sbyteTable.inArray2Ptr);
                    var vsb3 = Avx512BW.UnpackLow(vsb1, vsb2);
                    Unsafe.Write(sbyteTable.outArrayPtr, vsb3);

                    var vs1 = Unsafe.Read<Vector512<short>>(shortTable.inArray1Ptr);
                    var vs2 = Unsafe.Read<Vector512<short>>(shortTable.inArray2Ptr);
                    var vs3 = Avx512BW.UnpackLow(vs1, vs2);
                    Unsafe.Write(shortTable.outArrayPtr, vs3);

                    var vus1 = Unsafe.Read<Vector512<ushort>>(ushortTable.inArray1Ptr);
                    var vus2 = Unsafe.Read<Vector512<ushort>>(ushortTable.inArray2Ptr);
                    var vus3 = Avx512BW.UnpackLow(vus1, vus2);
                    Unsafe.Write(ushortTable.outArrayPtr, vus3);

                    if ((byteTable.inArray1[0]  != byteTable.outArray[0])  || (byteTable.inArray2[0]  != byteTable.outArray[1]) ||
                        (byteTable.inArray1[1]  != byteTable.outArray[2])  || (byteTable.inArray2[1]  != byteTable.outArray[3]) ||
                        (byteTable.inArray1[2]  != byteTable.outArray[4])  || (byteTable.inArray2[2]  != byteTable.outArray[5]) ||
                        (byteTable.inArray1[3]  != byteTable.outArray[6])  || (byteTable.inArray2[3]  != byteTable.outArray[7]) ||
                        (byteTable.inArray1[4]  != byteTable.outArray[8])  || (byteTable.inArray2[4]  != byteTable.outArray[9]) ||
                        (byteTable.inArray1[5]  != byteTable.outArray[10]) || (byteTable.inArray2[5]  != byteTable.outArray[11]) ||
                        (byteTable.inArray1[6]  != byteTable.outArray[12]) || (byteTable.inArray2[6]  != byteTable.outArray[13]) ||
                        (byteTable.inArray1[7]  != byteTable.outArray[14]) || (byteTable.inArray2[7]  != byteTable.outArray[15]) ||
                        (byteTable.inArray1[16] != byteTable.outArray[16]) || (byteTable.inArray2[16] != byteTable.outArray[17]) ||
                        (byteTable.inArray1[17] != byteTable.outArray[18]) || (byteTable.inArray2[17] != byteTable.outArray[19]) ||
                        (byteTable.inArray1[18] != byteTable.outArray[20]) || (byteTable.inArray2[18] != byteTable.outArray[21]) ||
                        (byteTable.inArray1[19] != byteTable.outArray[22]) || (byteTable.inArray2[19] != byteTable.outArray[23]) ||
                        (byteTable.inArray1[20] != byteTable.outArray[24]) || (byteTable.inArray2[20] != byteTable.outArray[25]) ||
                        (byteTable.inArray1[21] != byteTable.outArray[26]) || (byteTable.inArray2[21] != byteTable.outArray[27]) ||
                        (byteTable.inArray1[22] != byteTable.outArray[28]) || (byteTable.inArray2[22] != byteTable.outArray[29]) ||
                        (byteTable.inArray1[23] != byteTable.outArray[30]) || (byteTable.inArray2[23] != byteTable.outArray[31]) ||
                        (byteTable.inArray1[32] != byteTable.outArray[32]) || (byteTable.inArray2[32] != byteTable.outArray[33]) ||
                        (byteTable.inArray1[33] != byteTable.outArray[34]) || (byteTable.inArray2[33] != byteTable.outArray[35]) ||
                        (byteTable.inArray1[34] != byteTable.outArray[36]) || (byteTable.inArray2[34] != byteTable.outArray[37]) ||
                        (byteTable.inArray1[35] != byteTable.outArray[38]) || (byteTable.inArray2[35] != byteTable.outArray[39]) ||
                        (byteTable.inArray1[36] != byteTable.outArray[40]) || (byteTable.inArray2[36] != byteTable.outArray[41]) ||
                        (byteTable.inArray1[37] != byteTable.outArray[42]) || (byteTable.inArray2[37] != byteTable.outArray[43]) ||
                        (byteTable.inArray1[38] != byteTable.outArray[44]) || (byteTable.inArray2[38] != byteTable.outArray[45]) ||
                        (byteTable.inArray1[39] != byteTable.outArray[46]) || (byteTable.inArray2[39] != byteTable.outArray[47]) ||
                        (byteTable.inArray1[48] != byteTable.outArray[48]) || (byteTable.inArray2[48] != byteTable.outArray[49]) ||
                        (byteTable.inArray1[49] != byteTable.outArray[50]) || (byteTable.inArray2[49] != byteTable.outArray[51]) ||
                        (byteTable.inArray1[50] != byteTable.outArray[52]) || (byteTable.inArray2[50] != byteTable.outArray[53]) ||
                        (byteTable.inArray1[51] != byteTable.outArray[54]) || (byteTable.inArray2[51] != byteTable.outArray[55]) ||
                        (byteTable.inArray1[52] != byteTable.outArray[56]) || (byteTable.inArray2[52] != byteTable.outArray[57]) ||
                        (byteTable.inArray1[53] != byteTable.outArray[58]) || (byteTable.inArray2[53] != byteTable.outArray[59]) ||
                        (byteTable.inArray1[54] != byteTable.outArray[60]) || (byteTable.inArray2[54] != byteTable.outArray[61]) ||
                        (byteTable.inArray1[55] != byteTable.outArray[62]) || (byteTable.inArray2[55] != byteTable.outArray[63]))
                        {
                            Console.WriteLine("Avx512BW UnpackLow failed on byte:");
                            Console.WriteLine($"    left: ({string.Join(", ", byteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", byteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", byteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((sbyteTable.inArray1[0]  != sbyteTable.outArray[0])  || (sbyteTable.inArray2[0]  != sbyteTable.outArray[1]) ||
                        (sbyteTable.inArray1[1]  != sbyteTable.outArray[2])  || (sbyteTable.inArray2[1]  != sbyteTable.outArray[3]) ||
                        (sbyteTable.inArray1[2]  != sbyteTable.outArray[4])  || (sbyteTable.inArray2[2]  != sbyteTable.outArray[5]) ||
                        (sbyteTable.inArray1[3]  != sbyteTable.outArray[6])  || (sbyteTable.inArray2[3]  != sbyteTable.outArray[7]) ||
                        (sbyteTable.inArray1[4]  != sbyteTable.outArray[8])  || (sbyteTable.inArray2[4]  != sbyteTable.outArray[9]) ||
                        (sbyteTable.inArray1[5]  != sbyteTable.outArray[10]) || (sbyteTable.inArray2[5]  != sbyteTable.outArray[11]) ||
                        (sbyteTable.inArray1[6]  != sbyteTable.outArray[12]) || (sbyteTable.inArray2[6]  != sbyteTable.outArray[13]) ||
                        (sbyteTable.inArray1[7]  != sbyteTable.outArray[14]) || (sbyteTable.inArray2[7]  != sbyteTable.outArray[15]) ||
                        (sbyteTable.inArray1[16] != sbyteTable.outArray[16]) || (sbyteTable.inArray2[16] != sbyteTable.outArray[17]) ||
                        (sbyteTable.inArray1[17] != sbyteTable.outArray[18]) || (sbyteTable.inArray2[17] != sbyteTable.outArray[19]) ||
                        (sbyteTable.inArray1[18] != sbyteTable.outArray[20]) || (sbyteTable.inArray2[18] != sbyteTable.outArray[21]) ||
                        (sbyteTable.inArray1[19] != sbyteTable.outArray[22]) || (sbyteTable.inArray2[19] != sbyteTable.outArray[23]) ||
                        (sbyteTable.inArray1[20] != sbyteTable.outArray[24]) || (sbyteTable.inArray2[20] != sbyteTable.outArray[25]) ||
                        (sbyteTable.inArray1[21] != sbyteTable.outArray[26]) || (sbyteTable.inArray2[21] != sbyteTable.outArray[27]) ||
                        (sbyteTable.inArray1[22] != sbyteTable.outArray[28]) || (sbyteTable.inArray2[22] != sbyteTable.outArray[29]) ||
                        (sbyteTable.inArray1[23] != sbyteTable.outArray[30]) || (sbyteTable.inArray2[23] != sbyteTable.outArray[31]) ||
                        (sbyteTable.inArray1[32] != sbyteTable.outArray[32]) || (sbyteTable.inArray2[32] != sbyteTable.outArray[33]) ||
                        (sbyteTable.inArray1[33] != sbyteTable.outArray[34]) || (sbyteTable.inArray2[33] != sbyteTable.outArray[35]) ||
                        (sbyteTable.inArray1[34] != sbyteTable.outArray[36]) || (sbyteTable.inArray2[34] != sbyteTable.outArray[37]) ||
                        (sbyteTable.inArray1[35] != sbyteTable.outArray[38]) || (sbyteTable.inArray2[35] != sbyteTable.outArray[39]) ||
                        (sbyteTable.inArray1[36] != sbyteTable.outArray[40]) || (sbyteTable.inArray2[36] != sbyteTable.outArray[41]) ||
                        (sbyteTable.inArray1[37] != sbyteTable.outArray[42]) || (sbyteTable.inArray2[37] != sbyteTable.outArray[43]) ||
                        (sbyteTable.inArray1[38] != sbyteTable.outArray[44]) || (sbyteTable.inArray2[38] != sbyteTable.outArray[45]) ||
                        (sbyteTable.inArray1[39] != sbyteTable.outArray[46]) || (sbyteTable.inArray2[39] != sbyteTable.outArray[47]) ||
                        (sbyteTable.inArray1[48] != sbyteTable.outArray[48]) || (sbyteTable.inArray2[48] != sbyteTable.outArray[49]) ||
                        (sbyteTable.inArray1[49] != sbyteTable.outArray[50]) || (sbyteTable.inArray2[49] != sbyteTable.outArray[51]) ||
                        (sbyteTable.inArray1[50] != sbyteTable.outArray[52]) || (sbyteTable.inArray2[50] != sbyteTable.outArray[53]) ||
                        (sbyteTable.inArray1[51] != sbyteTable.outArray[54]) || (sbyteTable.inArray2[51] != sbyteTable.outArray[55]) ||
                        (sbyteTable.inArray1[52] != sbyteTable.outArray[56]) || (sbyteTable.inArray2[52] != sbyteTable.outArray[57]) ||
                        (sbyteTable.inArray1[53] != sbyteTable.outArray[58]) || (sbyteTable.inArray2[53] != sbyteTable.outArray[59]) ||
                        (sbyteTable.inArray1[54] != sbyteTable.outArray[60]) || (sbyteTable.inArray2[54] != sbyteTable.outArray[61]) ||
                        (sbyteTable.inArray1[55] != sbyteTable.outArray[62]) || (sbyteTable.inArray2[55] != sbyteTable.outArray[63]))
                        {
                            Console.WriteLine("Avx512BW UnpackLow failed on sbyte:");
                            Console.WriteLine($"    left: ({string.Join(", ", sbyteTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", sbyteTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", sbyteTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((shortTable.inArray1[0]  != shortTable.outArray[0])  || (shortTable.inArray2[0]  != shortTable.outArray[1]) ||
                        (shortTable.inArray1[1]  != shortTable.outArray[2])  || (shortTable.inArray2[1]  != shortTable.outArray[3]) ||
                        (shortTable.inArray1[2]  != shortTable.outArray[4])  || (shortTable.inArray2[2]  != shortTable.outArray[5]) ||
                        (shortTable.inArray1[3]  != shortTable.outArray[6])  || (shortTable.inArray2[3]  != shortTable.outArray[7]) ||
                        (shortTable.inArray1[8]  != shortTable.outArray[8])  || (shortTable.inArray2[8]  != shortTable.outArray[9]) ||
                        (shortTable.inArray1[9]  != shortTable.outArray[10]) || (shortTable.inArray2[9]  != shortTable.outArray[11]) ||
                        (shortTable.inArray1[10] != shortTable.outArray[12]) || (shortTable.inArray2[10] != shortTable.outArray[13]) ||
                        (shortTable.inArray1[11] != shortTable.outArray[14]) || (shortTable.inArray2[11] != shortTable.outArray[15]) ||
                        (shortTable.inArray1[16] != shortTable.outArray[16]) || (shortTable.inArray2[16] != shortTable.outArray[17]) ||
                        (shortTable.inArray1[17] != shortTable.outArray[18]) || (shortTable.inArray2[17] != shortTable.outArray[19]) ||
                        (shortTable.inArray1[18] != shortTable.outArray[20]) || (shortTable.inArray2[18] != shortTable.outArray[21]) ||
                        (shortTable.inArray1[19] != shortTable.outArray[22]) || (shortTable.inArray2[19] != shortTable.outArray[23]) ||
                        (shortTable.inArray1[24] != shortTable.outArray[24]) || (shortTable.inArray2[24] != shortTable.outArray[25]) ||
                        (shortTable.inArray1[25] != shortTable.outArray[26]) || (shortTable.inArray2[25] != shortTable.outArray[27]) ||
                        (shortTable.inArray1[26] != shortTable.outArray[28]) || (shortTable.inArray2[26] != shortTable.outArray[29]) ||
                        (shortTable.inArray1[27] != shortTable.outArray[30]) || (shortTable.inArray2[27] != shortTable.outArray[31]))
                        {
                            Console.WriteLine("Avx512BW UnpackLow failed on short:");
                            Console.WriteLine($"    left: ({string.Join(", ", shortTable.inArray1)})");
                            Console.WriteLine($"   right: ({string.Join(", ", shortTable.inArray2)})");
                            Console.WriteLine($"  result: ({string.Join(", ", shortTable.outArray)})");
                            Console.WriteLine();

                            testResult = Fail;
                        }

                    if ((ushortTable.inArray1[0]  != ushortTable.outArray[0])  || (ushortTable.inArray2[0]  != ushortTable.outArray[1]) ||
                        (ushortTable.inArray1[1]  != ushortTable.outArray[2])  || (ushortTable.inArray2[1]  != ushortTable.outArray[3]) ||
                        (ushortTable.inArray1[2]  != ushortTable.outArray[4])  || (ushortTable.inArray2[2]  != ushortTable.outArray[5]) ||
                        (ushortTable.inArray1[3]  != ushortTable.outArray[6])  || (ushortTable.inArray2[3]  != ushortTable.outArray[7]) ||
                        (ushortTable.inArray1[8]  != ushortTable.outArray[8])  || (ushortTable.inArray2[8]  != ushortTable.outArray[9]) ||
                        (ushortTable.inArray1[9]  != ushortTable.outArray[10]) || (ushortTable.inArray2[9]  != ushortTable.outArray[11]) ||
                        (ushortTable.inArray1[10] != ushortTable.outArray[12]) || (ushortTable.inArray2[10] != ushortTable.outArray[13]) ||
                        (ushortTable.inArray1[11] != ushortTable.outArray[14]) || (ushortTable.inArray2[11] != ushortTable.outArray[15]) ||
                        (ushortTable.inArray1[16] != ushortTable.outArray[16]) || (ushortTable.inArray2[16] != ushortTable.outArray[17]) ||
                        (ushortTable.inArray1[17] != ushortTable.outArray[18]) || (ushortTable.inArray2[17] != ushortTable.outArray[19]) ||
                        (ushortTable.inArray1[18] != ushortTable.outArray[20]) || (ushortTable.inArray2[18] != ushortTable.outArray[21]) ||
                        (ushortTable.inArray1[19] != ushortTable.outArray[22]) || (ushortTable.inArray2[19] != ushortTable.outArray[23]) ||
                        (ushortTable.inArray1[24] != ushortTable.outArray[24]) || (ushortTable.inArray2[24] != ushortTable.outArray[25]) ||
                        (ushortTable.inArray1[25] != ushortTable.outArray[26]) || (ushortTable.inArray2[25] != ushortTable.outArray[27]) ||
                        (ushortTable.inArray1[26] != ushortTable.outArray[28]) || (ushortTable.inArray2[26] != ushortTable.outArray[29]) ||
                        (ushortTable.inArray1[27] != ushortTable.outArray[30]) || (ushortTable.inArray2[27] != ushortTable.outArray[31]))
                        {
                            Console.WriteLine("Avx512BW UnpackLow failed on ushort:");
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
