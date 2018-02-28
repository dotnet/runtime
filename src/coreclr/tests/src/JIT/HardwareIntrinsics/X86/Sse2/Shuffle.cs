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
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;
            int testCount = 16;
            string methodUnderTestName = nameof(Sse2.Shuffle);


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

                string[] permuteDouble = new string[]
                {
                    "0b00",
                    "0b01",
                    "0b10",
                    "0b11",
                };

                using (var doubleTable = TestTableTuvImmSse2<double, double, byte>.Create(permuteDouble.Length))
                using (var intTable = TestTableTuvImmSse2<int, int, byte>.Create(permuteData.Length))
                using (var uintTable = TestTableTuvImmSse2<uint, uint, byte>.Create(permuteData.Length))
                {

                    // Vector128<double> tests

                    TestUtilities.InitializeWithElementNumberingModuloVectorLength<double>(
                        doubleTable.inArray1, 16, (int i, int elNo) =>
                        {
                            return (uint) i % 2;
                        });

                    TestUtilities.InitializeWithElementNumberingModuloVectorLength<double>(
                        doubleTable.inArray2, 16, (int i, int elNo) =>
                        {
                            return (uint) i % 2 + 10;
                        });

                    (Vector128<double>, Vector128<double>) valueDouble_0 = doubleTable[0];
                    Vector128<double> resultDouble_0 = Sse2.Shuffle(valueDouble_0.Item1, valueDouble_0.Item2, (byte) 0b00);
                    doubleTable.SetOutArray(resultDouble_0, 0, (byte) 0b00);

                    (Vector128<double>, Vector128<double>) valueDouble_1 = doubleTable[1];
                    Vector128<double> resultDouble_1 = Sse2.Shuffle(valueDouble_1.Item1, valueDouble_1.Item2, (byte) 0b01);
                    doubleTable.SetOutArray(resultDouble_1, 1, (byte) 0b01);

                    (Vector128<double>, Vector128<double>) valueDouble_2 = doubleTable[2];
                    Vector128<double> resultDouble_2 = Sse2.Shuffle(valueDouble_2.Item1, valueDouble_2.Item2, (byte) 0b10);
                    doubleTable.SetOutArray(resultDouble_2, 2, (byte) 0b10);

                    (Vector128<double>, Vector128<double>) valueDouble_3 = doubleTable[3];
                    Vector128<double> resultDouble_3 = Sse2.Shuffle(valueDouble_3.Item1, valueDouble_3.Item2, (byte) 0b11);
                    doubleTable.SetOutArray(resultDouble_3, 3, (byte) 0b11);


                    // Vector128<int> tests

                    TestUtilities.InitializeWithElementNumberingModuloVectorLength<uint>(
                        uintTable.inArray1, 16, (int i, int elNo) =>
                        {
                            return (uint) i % 4;
                        });

                    (Vector128<int>, Vector128<int>) valueInt32_0 = intTable[0];
                    Vector128<int> resultInt32_0 = Sse2.Shuffle(valueInt32_0.Item1, (byte) 0b11100100);
                    intTable.SetOutArray(resultInt32_0, 0, (byte) 0b11100100);

                    (Vector128<int>, Vector128<int>) valueInt32_1 = intTable[1];
                    Vector128<int> resultInt32_1 = Sse2.Shuffle(valueInt32_1.Item1, (byte) 0b00011011);
                    intTable.SetOutArray(resultInt32_1, 1, (byte) 0b00011011);

                    (Vector128<int>, Vector128<int>) valueInt32_2 = intTable[2];
                    Vector128<int> resultInt32_2 = Sse2.Shuffle(valueInt32_2.Item1, (byte) 0b00000000);
                    intTable.SetOutArray(resultInt32_2, 2, (byte) 0b00000000);

                    (Vector128<int>, Vector128<int>) valueInt32_3 = intTable[3];
                    Vector128<int> resultInt32_3 = Sse2.Shuffle(valueInt32_3.Item1, (byte) 0b11111111);
                    intTable.SetOutArray(resultInt32_3, 3, (byte) 0b11111111);

                    (Vector128<int>, Vector128<int>) valueInt32_4 = intTable[4];
                    Vector128<int> resultInt32_4 = Sse2.Shuffle(valueInt32_4.Item1, (byte) 0b01010101);
                    intTable.SetOutArray(resultInt32_4, 4, (byte) 0b01010101);

                    (Vector128<int>, Vector128<int>) valueInt32_5 = intTable[5];
                    Vector128<int> resultInt32_5 = Sse2.Shuffle(valueInt32_5.Item1, (byte) 0b10101010);
                    intTable.SetOutArray(resultInt32_5, 5, (byte) 0b10101010);

                    (Vector128<int>, Vector128<int>) valueInt32_6 = intTable[6];
                    Vector128<int> resultInt32_6 = Sse2.Shuffle(valueInt32_6.Item1, (byte) 0b11011000);
                    intTable.SetOutArray(resultInt32_6, 6, (byte) 0b11011000);

                    (Vector128<int>, Vector128<int>) valueInt32_7 = intTable[7];
                    Vector128<int> resultInt32_7 = Sse2.Shuffle(valueInt32_7.Item1, (byte) 0b00100111);
                    intTable.SetOutArray(resultInt32_7, 7, (byte) 0b00100111);

                    (Vector128<int>, Vector128<int>) valueInt32_8 = intTable[8];
                    Vector128<int> resultInt32_8 = Sse2.Shuffle(valueInt32_8.Item1, (byte) 0b10110001);
                    intTable.SetOutArray(resultInt32_8, 8, (byte) 0b10110001);

                    (Vector128<int>, Vector128<int>) valueInt32_9 = intTable[9];
                    Vector128<int> resultInt32_9 = Sse2.Shuffle(valueInt32_9.Item1, (byte) 0b11110000);
                    intTable.SetOutArray(resultInt32_9, 9, (byte) 0b11110000);

                    (Vector128<int>, Vector128<int>) valueInt32_10 = intTable[10];
                    Vector128<int> resultInt32_10 = Sse2.Shuffle(valueInt32_10.Item1, (byte) 0b10100101);
                    intTable.SetOutArray(resultInt32_10, 10, (byte) 0b10100101);

                    (Vector128<int>, Vector128<int>) valueInt32_11 = intTable[11];
                    Vector128<int> resultInt32_11 = Sse2.Shuffle(valueInt32_11.Item1, (byte) 0b00010100);
                    intTable.SetOutArray(resultInt32_11, 11, (byte) 0b00010100);

                    (Vector128<int>, Vector128<int>) valueInt32_12 = intTable[12];
                    Vector128<int> resultInt32_12 = Sse2.Shuffle(valueInt32_12.Item1, (byte) 0b10000010);
                    intTable.SetOutArray(resultInt32_12, 12, (byte) 0b10000010);

                    (Vector128<int>, Vector128<int>) valueInt32_13 = intTable[13];
                    Vector128<int> resultInt32_13 = Sse2.Shuffle(valueInt32_13.Item1, (byte) 0b11001100);
                    intTable.SetOutArray(resultInt32_13, 13, (byte) 0b11001100);

                    (Vector128<int>, Vector128<int>) valueInt32_14 = intTable[14];
                    Vector128<int> resultInt32_14 = Sse2.Shuffle(valueInt32_14.Item1, (byte) 0b01100110);
                    intTable.SetOutArray(resultInt32_14, 14, (byte) 0b01100110);

                    (Vector128<int>, Vector128<int>) valueInt32_15 = intTable[15];
                    Vector128<int> resultInt32_15 = Sse2.Shuffle(valueInt32_15.Item1, (byte) 0b10011001);
                    intTable.SetOutArray(resultInt32_15, 15, (byte) 0b10011001);


                    // Vector128<uint> tests

                    TestUtilities.InitializeWithElementNumberingModuloVectorLength<uint>(
                        uintTable.inArray1, 16, (int i, int elNo) =>
                        {
                            return (uint) i % 4;
                        });

                    (Vector128<uint>, Vector128<uint>) valueUInt32_0 = uintTable[0];
                    Vector128<uint> resultUInt32_0 = Sse2.Shuffle(valueUInt32_0.Item1, (byte) 0b11100100);
                    uintTable.SetOutArray(resultUInt32_0, 0, (byte) 0b11100100);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_1 = uintTable[1];
                    Vector128<uint> resultUInt32_1 = Sse2.Shuffle(valueUInt32_1.Item1, (byte) 0b00011011);
                    uintTable.SetOutArray(resultUInt32_1, 1, (byte) 0b00011011);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_2 = uintTable[2];
                    Vector128<uint> resultUInt32_2 = Sse2.Shuffle(valueUInt32_2.Item1, (byte) 0b00000000);
                    uintTable.SetOutArray(resultUInt32_2, 2, (byte) 0b00000000);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_3 = uintTable[3];
                    Vector128<uint> resultUInt32_3 = Sse2.Shuffle(valueUInt32_3.Item1, (byte) 0b11111111);
                    uintTable.SetOutArray(resultUInt32_3, 3, (byte) 0b11111111);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_4 = uintTable[4];
                    Vector128<uint> resultUInt32_4 = Sse2.Shuffle(valueUInt32_4.Item1, (byte) 0b01010101);
                    uintTable.SetOutArray(resultUInt32_4, 4, (byte) 0b01010101);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_5 = uintTable[5];
                    Vector128<uint> resultUInt32_5 = Sse2.Shuffle(valueUInt32_5.Item1, (byte) 0b10101010);
                    uintTable.SetOutArray(resultUInt32_5, 5, (byte) 0b10101010);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_6 = uintTable[6];
                    Vector128<uint> resultUInt32_6 = Sse2.Shuffle(valueUInt32_6.Item1, (byte) 0b11011000);
                    uintTable.SetOutArray(resultUInt32_6, 6, (byte) 0b11011000);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_7 = uintTable[7];
                    Vector128<uint> resultUInt32_7 = Sse2.Shuffle(valueUInt32_7.Item1, (byte) 0b00100111);
                    uintTable.SetOutArray(resultUInt32_7, 7, (byte) 0b00100111);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_8 = uintTable[8];
                    Vector128<uint> resultUInt32_8 = Sse2.Shuffle(valueUInt32_8.Item1, (byte) 0b10110001);
                    uintTable.SetOutArray(resultUInt32_8, 8, (byte) 0b10110001);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_9 = uintTable[9];
                    Vector128<uint> resultUInt32_9 = Sse2.Shuffle(valueUInt32_9.Item1, (byte) 0b11110000);
                    uintTable.SetOutArray(resultUInt32_9, 9, (byte) 0b11110000);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_10 = uintTable[10];
                    Vector128<uint> resultUInt32_10 = Sse2.Shuffle(valueUInt32_10.Item1, (byte) 0b10100101);
                    uintTable.SetOutArray(resultUInt32_10, 10, (byte) 0b10100101);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_11 = uintTable[11];
                    Vector128<uint> resultUInt32_11 = Sse2.Shuffle(valueUInt32_11.Item1, (byte) 0b00010100);
                    uintTable.SetOutArray(resultUInt32_11, 11, (byte) 0b00010100);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_12 = uintTable[12];
                    Vector128<uint> resultUInt32_12 = Sse2.Shuffle(valueUInt32_12.Item1, (byte) 0b10000010);
                    uintTable.SetOutArray(resultUInt32_12, 12, (byte) 0b10000010);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_13 = uintTable[13];
                    Vector128<uint> resultUInt32_13 = Sse2.Shuffle(valueUInt32_13.Item1, (byte) 0b11001100);
                    uintTable.SetOutArray(resultUInt32_13, 13, (byte) 0b11001100);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_14 = uintTable[14];
                    Vector128<uint> resultUInt32_14 = Sse2.Shuffle(valueUInt32_14.Item1, (byte) 0b01100110);
                    uintTable.SetOutArray(resultUInt32_14, 14, (byte) 0b01100110);

                    (Vector128<uint>, Vector128<uint>) valueUInt32_15 = uintTable[15];
                    Vector128<uint> resultUInt32_15 = Sse2.Shuffle(valueUInt32_15.Item1, (byte) 0b10011001);
                    uintTable.SetOutArray(resultUInt32_15, 15, (byte) 0b10011001);


                    CheckMethodFiveDouble<double, double, byte> checkDouble =
                    (Span<double> x, Span<double> y, byte imm, Span<double> z, Span<double> a) =>
                    {
                        a[0] = (0x01 & imm) > 0 ? x[1] : x[0];
                        a[1] = (0x02 & imm) > 0 ? y[1] : y[0];
                        return a[0] == z[0] && a[1] == z[1];
                    };

                    if (!doubleTable.CheckResultShuffle(checkDouble))
                    {
                        PrintError8(doubleTable, methodUnderTestName, "(double x, byte y, double z, ref double a) => (a = x * y) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethodFive<int, int, byte> checkInt32 = (Span<int> x, byte imm, Span<int> z, Span<int> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            a[i] = x[imm & 0x03];
                            if (z[i] != a[i])
                                result = false;
                            imm = (byte) (imm >> 2);
                        }
                        return result;
                    };

                    if (!intTable.CheckResultShuffle(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(int x, byte y, int z, ref int a) => (a = x << y) == z", checkInt32);
                        testResult = Fail;
                    }

                    CheckMethodFive<uint, uint, byte> checkUInt32 = (Span<uint> x, byte imm, Span<uint> z, Span<uint> a) =>
                    {
                        bool result = true;
                        for (int i = 0; i < x.Length; i++)
                        {
                            a[i] = x[imm & 0x03];
                            if (z[i] != a[i])
                                result = false;
                            imm = (byte) (imm >> 2);
                        }
                        return result;
                    };

                    if (!uintTable.CheckResultShuffle(checkUInt32))
                    {
                        PrintError(uintTable, methodUnderTestName, "(uint x, byte y, uint z, ref uint a) => (a = x << y) == z", checkUInt32);
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
