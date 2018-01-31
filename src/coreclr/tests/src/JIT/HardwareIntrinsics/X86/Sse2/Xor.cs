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
            string methodUnderTestName = nameof(Sse2.Xor);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double>.Create(testsCount))
                using (var longTable = TestTableSse2<long>.Create(testsCount))
                using (var ulongTable = TestTableSse2<ulong>.Create(testsCount))
                using (var intTable = TestTableSse2<int>.Create(testsCount))
                using (var uintTable = TestTableSse2<uint>.Create(testsCount))
                using (var shortTable = TestTableSse2<short>.Create(testsCount))
                using (var ushortTable = TestTableSse2<ushort>.Create(testsCount))
                using (var sbyteTable = TestTableSse2<sbyte>.Create(testsCount))
                using (var byteTable = TestTableSse2<byte>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>, Vector128<double>) value = doubleTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        doubleTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<long>, Vector128<long>, Vector128<long>) value = longTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        longTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ulong>, Vector128<ulong>, Vector128<ulong>) value = ulongTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        ulongTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<int>, Vector128<int>, Vector128<int>) value = intTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        intTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<uint>, Vector128<uint>, Vector128<uint>) value = uintTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        uintTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>, Vector128<short>) value = shortTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        shortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ushort>, Vector128<ushort>, Vector128<ushort>) value = ushortTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        ushortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<sbyte>, Vector128<sbyte>, Vector128<sbyte>) value = sbyteTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        sbyteTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<byte>, Vector128<byte>, Vector128<byte>) value = byteTable[i];
                        var result = Sse2.Xor(value.Item1, value.Item2);
                        byteTable.SetOutArray(result);
                    }

                    CheckMethod<double> checkDouble = (double x, double y, double z, ref double a) => (a = BitwiseXor(x, y)) == z;

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(double x, double y, double z, ref double a) => (a = BitwiseXor(x, y)) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethod<long> checkLong = (long x, long y, long z, ref long a) => (a = x ^ y) == z;

                    if (!longTable.CheckResult(checkLong))
                    {
                        PrintError(longTable, methodUnderTestName, "(long x, long y, long z, ref long a) => (a = x ^ y) == z", checkLong);
                        testResult = Fail;
                    }

                    CheckMethod<ulong> checkUlong = (ulong x, ulong y, ulong z, ref ulong a) => (a = x ^ y) == z;

                    if (!longTable.CheckResult(checkLong))
                    {
                        PrintError(ulongTable, methodUnderTestName, "(ulong x, ulong y, ulong z, ref ulong a) => (a = x ^ y) == z", checkUlong);
                        testResult = Fail;
                    }

                    CheckMethod<int> checkInt32 = (int x, int y, int z, ref int a) => (a = x ^ y) == z;

                    if (!intTable.CheckResult(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(int x, int y, int z, ref int a) => (a = x ^ y) == z", checkInt32);
                        testResult = Fail;
                    }

                    CheckMethod<uint> checkUInt32 = (uint x, uint y, uint z, ref uint a) => (a = x ^ y) == z;

                    if (!uintTable.CheckResult(checkUInt32))
                    {
                        PrintError(uintTable, methodUnderTestName, "(uint x, uint y, uint z, ref uint a) => (a = x ^ y) == z", checkUInt32);
                        testResult = Fail;
                    }

                    CheckMethod<short> checkInt16 = (short x, short y, short z, ref short a) => (a = (short)(x ^ y)) == z;

                    if (!shortTable.CheckResult(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(short x, short y, short z, ref short a) => (a = (short)(x ^ y)) == z", checkInt16);
                        testResult = Fail;
                    }

                    CheckMethod<ushort> checkUInt16 = (ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)(x ^ y)) == z;

                    if (!ushortTable.CheckResult(checkUInt16))
                    {
                        PrintError(ushortTable, methodUnderTestName, "(ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)(x ^ y)) == z", checkUInt16);
                        testResult = Fail;
                    }

                    CheckMethod<sbyte> checkSByte = (sbyte x, sbyte y, sbyte z, ref sbyte a) => (a = (sbyte)(x ^ y)) == z;

                    if (!sbyteTable.CheckResult(checkSByte))
                    {
                        PrintError(sbyteTable, methodUnderTestName, "(sbyte x, sbyte y, sbyte z, ref sbyte a) => (a = (sbyte)(x ^ y)) == z", checkSByte);
                        testResult = Fail;
                    }

                    CheckMethod<byte> checkByte = (byte x, byte y, byte z, ref byte a) => (a = (byte)(x ^ y)) == z;

                    if (!byteTable.CheckResult(checkByte))
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

        public static unsafe double BitwiseXor(double x, double y)
        {
            var xUlong = BitConverter.ToUInt64(BitConverter.GetBytes(x));
            var yUlong = BitConverter.ToUInt64(BitConverter.GetBytes(y));
            var longAnd = xUlong ^ yUlong;
            return BitConverter.ToDouble(BitConverter.GetBytes(longAnd));
        }
    }
}
