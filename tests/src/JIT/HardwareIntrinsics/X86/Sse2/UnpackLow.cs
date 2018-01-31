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
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.UnpackLow);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double, double>.Create(testsCount, 1.0))
                using (var longTable = TestTableSse2<long, long>.Create(testsCount, 1.0))
                using (var ulongTable = TestTableSse2<ulong, ulong>.Create(testsCount, 1.0))
                using (var intTable = TestTableSse2<int, int>.Create(testsCount, 1.0))
                using (var uintTable = TestTableSse2<uint, uint>.Create(testsCount, 1.0))
                using (var shortTable = TestTableSse2<short, short>.Create(testsCount, 1.0))
                using (var ushortTable = TestTableSse2<ushort, ushort>.Create(testsCount, 1.0))
                using (var sbyteTable = TestTableSse2<sbyte, sbyte>.Create(testsCount, 1.0))
                using (var byteTable = TestTableSse2<byte, byte>.Create(testsCount, 1.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>) value = doubleTable[i];
                        Vector128<double> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        doubleTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<long>, Vector128<long>) value = longTable[i];
                        Vector128<long> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        longTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ulong>, Vector128<ulong>) value = ulongTable[i];
                        Vector128<ulong> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        ulongTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<int>, Vector128<int>) value = intTable[i];
                        Vector128<int> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        intTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<uint>, Vector128<uint>) value = uintTable[i];
                        Vector128<uint> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        uintTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>) value = shortTable[i];
                        Vector128<short> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        shortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ushort>, Vector128<ushort>) value = ushortTable[i];
                        Vector128<ushort> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        ushortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<sbyte>, Vector128<sbyte>) value = sbyteTable[i];
                        Vector128<sbyte> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        sbyteTable.SetOutArray(result, i);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<byte>, Vector128<byte>) value = byteTable[i];
                        Vector128<byte> result = Sse2.UnpackLow(value.Item1, value.Item2);
                        byteTable.SetOutArray(result, i);
                    }

                    CheckMethodFive<double, double> checkDouble = (double x1, double x2, double y1, double y2, double z1, double z2, ref double a1, ref double a2) =>
                    {
                        return (a1 = x1) == z1 && (a2 = y1) == z2;
                    };

                    if (!doubleTable.CheckUnpackHiDouble(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(double x, double y, double z, ref double a) => (a = BitwiseXor(x, y)) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethodFive<long, long> checkLong = (long x1, long x2, long y1, long y2, long z1, long z2, ref long a1, ref long a2) =>
                    {
                        return (a1 = x1) == z1 && (a2 = y1) == z2;
                    };

                    if (!longTable.CheckUnpackHiDouble(checkLong))
                    {
                        PrintError(longTable, methodUnderTestName, "(long x, long y, long z, ref long a) => (a = x ^ y) == z", checkLong);
                        testResult = Fail;
                    }

                    CheckMethodFive<ulong, ulong> checkUlong = (ulong x1, ulong x2, ulong y1, ulong y2, ulong z1, ulong z2, ref ulong a1, ref ulong a2) =>
                    {
                        return (a1 = x1) == z1 && (a2 = y1) == z2;
                    };

                    if (!longTable.CheckUnpackHiDouble(checkLong))
                    {
                        PrintError(ulongTable, methodUnderTestName, "(ulong x1, ulong x2, ulong y1, ulong y2, ulong z1, ulong z2, ref ulong a1, ref ulong a2) => (a1 = x2) == z1 && (a2 = y2) == z2", checkUlong);
                        testResult = Fail;
                    }

                    CheckMethodFourTFourU<int, int> checkInt32 =
                        (ValueTuple<int, int, int, int> x, ValueTuple<int, int, int, int> y,
                        ValueTuple<int, int, int, int> z,
                        ref int a1, ref int a2, ref int a3, ref int a4) =>
                        {
                            a1 = x.Item1;
                            a2 = y.Item1;
                            a3 = x.Item2;
                            a4 = y.Item2;
                            return a1 == z.Item1 && a2 == z.Item2 && a3 == z.Item3 && a4 == z.Item4;
                        };

                    if (!intTable.CheckUnpack(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(int x, int y, int z, ref int a) => (a = x ^ y) == z", checkInt32);
                        testResult = Fail;
                    }

                    CheckMethodFourTFourU<uint, uint> checkUInt32 =
                        (ValueTuple<uint, uint, uint, uint> x, ValueTuple<uint, uint, uint, uint> y,
                        ValueTuple<uint, uint, uint, uint> z,
                        ref uint a1, ref uint a2, ref uint a3, ref uint a4) =>
                        {
                            a1 = x.Item1;
                            a2 = y.Item1;
                            a3 = x.Item2;
                            a4 = y.Item2;
                            return a1 == z.Item1 && a2 == z.Item2 && a3 == z.Item3 && a4 == z.Item4;
                        };

                    if (!uintTable.CheckUnpack(checkUInt32))
                    {
                        PrintError(uintTable, methodUnderTestName, "(uint x, uint y, uint z, ref uint a) => (a = x ^ y) == z", checkUInt32);
                        testResult = Fail;
                    }

                    CheckMethodEightOfTEightOfU<short, short> checkInt16 =
                        (ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> x,
                        ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> y,
                        ValueTuple<short, short, short, short, short, short, short, ValueTuple<short>> z,
                        ref short a1, ref short a2, ref short a3, ref short a4, ref short a5, ref short a6, ref short a7, ref short a8) =>
                        {
                            a1 = x.Item1;
                            a2 = y.Item1;
                            a3 = x.Item2;
                            a4 = y.Item2;
                            a5 = x.Item3;
                            a6 = y.Item3;
                            a7 = x.Item4;
                            a8 = y.Item4;
                            return a1 == z.Item1 && a2 == z.Item2 && a3 == z.Item3 && a4 == z.Item4 &&
                                a5 == z.Item5 && a6 == z.Item6 && a7 == z.Item7 && a8 == z.Item8;
                        };

                    if (!shortTable.CheckUnpack(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "CheckUnpack(CheckMethodEightOfTEightOfU<short, short>)", checkInt16);
                        testResult = Fail;
                    }

                    CheckMethodEightOfTEightOfU<ushort, ushort> checkUInt16 =
                        (ValueTuple<ushort, ushort, ushort, ushort, ushort, ushort, ushort, ValueTuple<ushort>> x,
                        ValueTuple<ushort, ushort, ushort, ushort, ushort, ushort, ushort, ValueTuple<ushort>> y,
                        ValueTuple<ushort, ushort, ushort, ushort, ushort, ushort, ushort, ValueTuple<ushort>> z,
                        ref ushort a1, ref ushort a2, ref ushort a3, ref ushort a4, ref ushort a5, ref ushort a6, ref ushort a7, ref ushort a8) =>
                        {
                            a1 = x.Item1;
                            a2 = y.Item1;
                            a3 = x.Item2;
                            a4 = y.Item2;
                            a5 = x.Item3;
                            a6 = y.Item3;
                            a7 = x.Item4;
                            a8 = y.Item4;
                            return a1 == z.Item1 && a2 == z.Item2 && a3 == z.Item3 && a4 == z.Item4 &&
                                a5 == z.Item5 && a6 == z.Item6 && a7 == z.Item7 && a8 == z.Item8;
                        };

                    if (!ushortTable.CheckUnpack(checkUInt16))
                    {
                        PrintError(ushortTable, methodUnderTestName, "(ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)(x ^ y)) == z", checkUInt16);
                        testResult = Fail;
                    }

                    CheckMethodSixteenOfAll<sbyte, sbyte> checkSByte =
                        (ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> x,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> x1,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> y,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> y1,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> z1,
                        ValueTuple<sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, sbyte, ValueTuple<sbyte>> z2,
                        ref sbyte a1, ref sbyte a2, ref sbyte a3, ref sbyte a4, ref sbyte a5, ref sbyte a6, ref sbyte a7, ref sbyte a8,
                        ref sbyte a9, ref sbyte a10, ref sbyte a11, ref sbyte a12, ref sbyte a13, ref sbyte a14, ref sbyte a15, ref sbyte a16) =>
                        {
                            a1 = x.Item1;
                            a2 = y.Item1;
                            a3 = x.Item2;
                            a4 = y.Item2;
                            a5 = x.Item3;
                            a6 = y.Item3;
                            a7 = x.Item4;
                            a8 = y.Item4;
                            a9 = x.Item5;
                            a10 = y.Item5;
                            a11 = x.Item6;
                            a12 = y.Item6;
                            a13 = x.Item7;
                            a14 = y.Item7;
                            a15 = x.Item8;
                            a16 = y.Item8;

                            return a1 == z1.Item1 && a2 == z1.Item2 && a3 == z1.Item3 && a4 == z1.Item4 &&
                                a5 == z1.Item5 && a6 == z1.Item6 && a7 == z1.Item7 && a8 == z1.Item8 &&
                                a9 == z2.Item1 && a10 == z2.Item2 && a11 == z2.Item3 && a12 == z2.Item4 &&
                                a13 == z2.Item5 && a14 == z2.Item6 && a15 == z2.Item7 && a16 == z2.Item8;
                        };

                    if (!sbyteTable.CheckUnpack(checkSByte))
                    {
                        PrintError(sbyteTable, methodUnderTestName, "(sbyte x, sbyte y, sbyte z, ref sbyte a) => (a = (sbyte)(x ^ y)) == z", checkSByte);
                        testResult = Fail;
                    }

                    CheckMethodSixteenOfAll<byte, byte> checkByte =
                        (ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> x,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> x1,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> y,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> y1,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> z1,
                        ValueTuple<byte, byte, byte, byte, byte, byte, byte, ValueTuple<byte>> z2,
                        ref byte a1, ref byte a2, ref byte a3, ref byte a4, ref byte a5, ref byte a6, ref byte a7, ref byte a8,
                        ref byte a9, ref byte a10, ref byte a11, ref byte a12, ref byte a13, ref byte a14, ref byte a15, ref byte a16) =>
                        {
                            a1 = x.Item1;
                            a2 = y.Item1;
                            a3 = x.Item2;
                            a4 = y.Item2;
                            a5 = x.Item3;
                            a6 = y.Item3;
                            a7 = x.Item4;
                            a8 = y.Item4;
                            a9 = x.Item5;
                            a10 = y.Item5;
                            a11 = x.Item6;
                            a12 = y.Item6;
                            a13 = x.Item7;
                            a14 = y.Item7;
                            a15 = x.Item8;
                            a16 = y.Item8;

                            return a1 == z1.Item1 && a2 == z1.Item2 && a3 == z1.Item3 && a4 == z1.Item4 &&
                                a5 == z1.Item5 && a6 == z1.Item6 && a7 == z1.Item7 && a8 == z1.Item8 &&
                                a9 == z2.Item1 && a10 == z2.Item2 && a11 == z2.Item3 && a12 == z2.Item4 &&
                                a13 == z2.Item5 && a14 == z2.Item6 && a15 == z2.Item7 && a16 == z2.Item8;
                        };

                    if (!byteTable.CheckUnpack(checkByte))
                    {
                        PrintError(byteTable, methodUnderTestName, "(byte x, byte y, byte z, ref byte a) => (a = (byte)(x ^ y)) == z", checkByte);
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
