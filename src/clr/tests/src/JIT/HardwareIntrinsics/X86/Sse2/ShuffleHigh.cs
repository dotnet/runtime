// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IntelHardwareIntrinsicTest
{
    internal static partial class Program
    {
        const short Pass = 100;
        const short Fail = 0;

        static unsafe int Main(string[] args)
        {
            short testResult = Pass;
            short testsCount = 16;
            string methodUnderTestName = nameof(Sse2.ShuffleHigh);



            if (Sse2.IsSupported)
            {
                string[] permuteData = new string[]
                {
                    "0b11100100",         // identity
                    "0b00011011",         // invert
                    "0b00000000",         // broadcast element 0
                    "0b11111111",         // broadcast element 3
                    "0b01010101",         // broadcast element 1
                    "0b10101010",         // broadcast element 2
                    "0b11011000",         // swap middle elements
                    "0b00100111",         // swap external elements
                    "0b10110001",         // swap internal with external elements
                    "0b11110000",         // divide everything between external elements
                    "0b10100101",         // divide everything between internal elements
                    "0b00010100",         // pattern (0, 1, 1, 0)
                    "0b10000010",         // pattern (2, 0, 0, 2)
                    "0b11001100",         // pattern (3, 0, 3, 0)
                    "0b01100110",         // pattern (1, 2, 1, 2)
                    "0b10011001"          // pattern (2, 1, 2, 1)
                };

                using (var shortTable = TestTableTuvImmSse2<short, short, byte>.Create(testsCount))
                using (var ushortTable = TestTableTuvImmSse2<ushort, ushort, byte>.Create(testsCount))
                {

                    // Vector128<short> tests

                    TestUtilities.InitializeWithElementNumberingModuloVectorLength<short>(
                        shortTable.inArray1, 16, (int i, int elNo) =>
                        {
                            return (short)(i % 8);
                        });

                    TestUtilities.InitializeWithConstValue<short>(0, shortTable.inArray2);

                    (Vector128<short>, Vector128<short>) valueInt16_0 = shortTable[0];
                    Vector128<short> resultInt16_0 = Sse2.ShuffleHigh(valueInt16_0.Item1, (byte) 0b11100100);
                    shortTable.SetOutArray(resultInt16_0, 0, (byte) 0b11100100);

                    (Vector128<short>, Vector128<short>) valueInt16_1 = shortTable[1];
                    Vector128<short> resultInt16_1 = Sse2.ShuffleHigh(valueInt16_1.Item1, (byte) 0b00011011);
                    shortTable.SetOutArray(resultInt16_1, 1, (byte) 0b00011011);

                    (Vector128<short>, Vector128<short>) valueInt16_2 = shortTable[2];
                    Vector128<short> resultInt16_2 = Sse2.ShuffleHigh(valueInt16_2.Item1, (byte) 0b00000000);
                    shortTable.SetOutArray(resultInt16_2, 2, (byte) 0b00000000);

                    (Vector128<short>, Vector128<short>) valueInt16_3 = shortTable[3];
                    Vector128<short> resultInt16_3 = Sse2.ShuffleHigh(valueInt16_3.Item1, (byte) 0b11111111);
                    shortTable.SetOutArray(resultInt16_3, 3, (byte) 0b11111111);

                    (Vector128<short>, Vector128<short>) valueInt16_4 = shortTable[4];
                    Vector128<short> resultInt16_4 = Sse2.ShuffleHigh(valueInt16_4.Item1, (byte) 0b01010101);
                    shortTable.SetOutArray(resultInt16_4, 4, (byte) 0b01010101);

                    (Vector128<short>, Vector128<short>) valueInt16_5 = shortTable[5];
                    Vector128<short> resultInt16_5 = Sse2.ShuffleHigh(valueInt16_5.Item1, (byte) 0b10101010);
                    shortTable.SetOutArray(resultInt16_5, 5, (byte) 0b10101010);

                    (Vector128<short>, Vector128<short>) valueInt16_6 = shortTable[6];
                    Vector128<short> resultInt16_6 = Sse2.ShuffleHigh(valueInt16_6.Item1, (byte) 0b11011000);
                    shortTable.SetOutArray(resultInt16_6, 6, (byte) 0b11011000);

                    (Vector128<short>, Vector128<short>) valueInt16_7 = shortTable[7];
                    Vector128<short> resultInt16_7 = Sse2.ShuffleHigh(valueInt16_7.Item1, (byte) 0b00100111);
                    shortTable.SetOutArray(resultInt16_7, 7, (byte) 0b00100111);

                    (Vector128<short>, Vector128<short>) valueInt16_8 = shortTable[8];
                    Vector128<short> resultInt16_8 = Sse2.ShuffleHigh(valueInt16_8.Item1, (byte) 0b10110001);
                    shortTable.SetOutArray(resultInt16_8, 8, (byte) 0b10110001);

                    (Vector128<short>, Vector128<short>) valueInt16_9 = shortTable[9];
                    Vector128<short> resultInt16_9 = Sse2.ShuffleHigh(valueInt16_9.Item1, (byte) 0b11110000);
                    shortTable.SetOutArray(resultInt16_9, 9, (byte) 0b11110000);

                    (Vector128<short>, Vector128<short>) valueInt16_10 = shortTable[10];
                    Vector128<short> resultInt16_10 = Sse2.ShuffleHigh(valueInt16_10.Item1, (byte) 0b10100101);
                    shortTable.SetOutArray(resultInt16_10, 10, (byte) 0b10100101);

                    (Vector128<short>, Vector128<short>) valueInt16_11 = shortTable[11];
                    Vector128<short> resultInt16_11 = Sse2.ShuffleHigh(valueInt16_11.Item1, (byte) 0b00010100);
                    shortTable.SetOutArray(resultInt16_11, 11, (byte) 0b00010100);

                    (Vector128<short>, Vector128<short>) valueInt16_12 = shortTable[12];
                    Vector128<short> resultInt16_12 = Sse2.ShuffleHigh(valueInt16_12.Item1, (byte) 0b10000010);
                    shortTable.SetOutArray(resultInt16_12, 12, (byte) 0b10000010);

                    (Vector128<short>, Vector128<short>) valueInt16_13 = shortTable[13];
                    Vector128<short> resultInt16_13 = Sse2.ShuffleHigh(valueInt16_13.Item1, (byte) 0b11001100);
                    shortTable.SetOutArray(resultInt16_13, 13, (byte) 0b11001100);

                    (Vector128<short>, Vector128<short>) valueInt16_14 = shortTable[14];
                    Vector128<short> resultInt16_14 = Sse2.ShuffleHigh(valueInt16_14.Item1, (byte) 0b01100110);
                    shortTable.SetOutArray(resultInt16_14, 14, (byte) 0b01100110);

                    (Vector128<short>, Vector128<short>) valueInt16_15 = shortTable[15];
                    Vector128<short> resultInt16_15 = Sse2.ShuffleHigh(valueInt16_15.Item1, (byte) 0b10011001);
                    shortTable.SetOutArray(resultInt16_15, 15, (byte) 0b10011001);


                    // Vector128<ushort> tests

                    TestUtilities.InitializeWithElementNumberingModuloVectorLength<ushort>(
                        ushortTable.inArray1, 16, (int i, int elNo) =>
                        {
                            return (ushort)(i % 8);
                        });

                    TestUtilities.InitializeWithConstValue<ushort>(0, ushortTable.inArray2);


                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_0 = ushortTable[0];
                    Vector128<ushort> resultUInt16_0 = Sse2.ShuffleHigh(valueUInt16_0.Item1, (byte) 0b11100100);
                    ushortTable.SetOutArray(resultUInt16_0, 0, (byte) 0b11100100);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_1 = ushortTable[1];
                    Vector128<ushort> resultUInt16_1 = Sse2.ShuffleHigh(valueUInt16_1.Item1, (byte) 0b00011011);
                    ushortTable.SetOutArray(resultUInt16_1, 1, (byte) 0b00011011);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_2 = ushortTable[2];
                    Vector128<ushort> resultUInt16_2 = Sse2.ShuffleHigh(valueUInt16_2.Item1, (byte) 0b00000000);
                    ushortTable.SetOutArray(resultUInt16_2, 2, (byte) 0b00000000);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_3 = ushortTable[3];
                    Vector128<ushort> resultUInt16_3 = Sse2.ShuffleHigh(valueUInt16_3.Item1, (byte) 0b11111111);
                    ushortTable.SetOutArray(resultUInt16_3, 3, (byte) 0b11111111);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_4 = ushortTable[4];
                    Vector128<ushort> resultUInt16_4 = Sse2.ShuffleHigh(valueUInt16_4.Item1, (byte) 0b01010101);
                    ushortTable.SetOutArray(resultUInt16_4, 4, (byte) 0b01010101);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_5 = ushortTable[5];
                    Vector128<ushort> resultUInt16_5 = Sse2.ShuffleHigh(valueUInt16_5.Item1, (byte) 0b10101010);
                    ushortTable.SetOutArray(resultUInt16_5, 5, (byte) 0b10101010);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_6 = ushortTable[6];
                    Vector128<ushort> resultUInt16_6 = Sse2.ShuffleHigh(valueUInt16_6.Item1, (byte) 0b11011000);
                    ushortTable.SetOutArray(resultUInt16_6, 6, (byte) 0b11011000);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_7 = ushortTable[7];
                    Vector128<ushort> resultUInt16_7 = Sse2.ShuffleHigh(valueUInt16_7.Item1, (byte) 0b00100111);
                    ushortTable.SetOutArray(resultUInt16_7, 7, (byte) 0b00100111);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_8 = ushortTable[8];
                    Vector128<ushort> resultUInt16_8 = Sse2.ShuffleHigh(valueUInt16_8.Item1, (byte) 0b10110001);
                    ushortTable.SetOutArray(resultUInt16_8, 8, (byte) 0b10110001);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_9 = ushortTable[9];
                    Vector128<ushort> resultUInt16_9 = Sse2.ShuffleHigh(valueUInt16_9.Item1, (byte) 0b11110000);
                    ushortTable.SetOutArray(resultUInt16_9, 9, (byte) 0b11110000);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_10 = ushortTable[10];
                    Vector128<ushort> resultUInt16_10 = Sse2.ShuffleHigh(valueUInt16_10.Item1, (byte) 0b10100101);
                    ushortTable.SetOutArray(resultUInt16_10, 10, (byte) 0b10100101);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_11 = ushortTable[11];
                    Vector128<ushort> resultUInt16_11 = Sse2.ShuffleHigh(valueUInt16_11.Item1, (byte) 0b00010100);
                    ushortTable.SetOutArray(resultUInt16_11, 11, (byte) 0b00010100);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_12 = ushortTable[12];
                    Vector128<ushort> resultUInt16_12 = Sse2.ShuffleHigh(valueUInt16_12.Item1, (byte) 0b10000010);
                    ushortTable.SetOutArray(resultUInt16_12, 12, (byte) 0b10000010);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_13 = ushortTable[13];
                    Vector128<ushort> resultUInt16_13 = Sse2.ShuffleHigh(valueUInt16_13.Item1, (byte) 0b11001100);
                    ushortTable.SetOutArray(resultUInt16_13, 13, (byte) 0b11001100);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_14 = ushortTable[14];
                    Vector128<ushort> resultUInt16_14 = Sse2.ShuffleHigh(valueUInt16_14.Item1, (byte) 0b01100110);
                    ushortTable.SetOutArray(resultUInt16_14, 14, (byte) 0b01100110);

                    (Vector128<ushort>, Vector128<ushort>) valueUInt16_15 = ushortTable[15];
                    Vector128<ushort> resultUInt16_15 = Sse2.ShuffleHigh(valueUInt16_15.Item1, (byte) 0b10011001);
                    ushortTable.SetOutArray(resultUInt16_15, 15, (byte) 0b10011001);


                    CheckMethodFive<short, short, byte> checkInt16 = (Span<short> x, byte imm, Span<short> z, Span<short> a) =>
                    {
                        bool result = true;
                        int halfLength = x.Length/2;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if  (i < halfLength)
                            {
                                a[i] = x[i];
                            }
                            else
                            {
                                a[i] = x[(imm & 0x03) + 4];
                                imm = (byte) (imm >> 2);
                            }

                            if (z[i] != a[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!shortTable.CheckResultShuffle(checkInt16))
                    {
                        PrintError8(shortTable, methodUnderTestName, "CheckResultShuffleHigh", checkInt16);
                        testResult = Fail;
                    }

                   CheckMethodFive<ushort, ushort, byte> checkUInt16 = (Span<ushort> x, byte imm, Span<ushort> z, Span<ushort> a) =>
                    {
                        bool result = true;
                        int halfLength = x.Length/2;
                        for (int i = 0; i < x.Length; i++)
                        {
                            if  (i < halfLength)
                            {
                                a[i] = x[i];
                            }
                            else
                            {
                                a[i] = x[(imm & 0x03) + 4];
                                imm = (byte) (imm >> 2);
                            }

                            if (z[i] != a[i])
                                result = false;
                        }
                        return result;
                    };

                    if (!ushortTable.CheckResultShuffle(checkUInt16))
                    {
                        PrintError8(ushortTable, methodUnderTestName, "CheckResultShuffleHigh", checkUInt16);
                        testResult = Fail;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Sse2.IsSupported: {Sse2.IsSupported}, skipped tests of {typeof(Sse2)}.{methodUnderTestName}");
            }
            return testResult;
        }
    }
}
