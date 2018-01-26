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
        private const int Pass = 100;
        private const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.CompareGreaterThan);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double>.Create(testsCount))
                using (var intTable = TestTableSse2<int>.Create(testsCount))
                using (var shortTable = TestTableSse2<short>.Create(testsCount))
                using (var sbyteTable = TestTableSse2<sbyte>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>, Vector128<double>) value = doubleTable[i];
                        var result = Sse2.CompareGreaterThan(value.Item1, value.Item2);
                        doubleTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<int>, Vector128<int>, Vector128<int>) value = intTable[i];
                        var result = Sse2.CompareGreaterThan(value.Item1, value.Item2);
                        intTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>, Vector128<short>) value = shortTable[i];
                        var result = Sse2.CompareGreaterThan(value.Item1, value.Item2);
                        shortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<sbyte>, Vector128<sbyte>, Vector128<sbyte>) value = sbyteTable[i];
                        var result = Sse2.CompareGreaterThan(value.Item1, value.Item2);
                        sbyteTable.SetOutArray(result);
                    }

                    CheckMethod<double> checkDouble = (double x, double y, double z, ref double a) =>
                    {
                        a = x > y ? BitConverter.Int64BitsToDouble(-1) : 0;
                        return BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(z);
                    };

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(x, y, z, ref a) => (a = x > y ? double.NaN : 0) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethod<int> checkInt32 = (int x, int y, int z, ref int a) => (a = x > y ? -1 : 0) == z;

                    if (!intTable.CheckResult(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(x, y, z, a) => (a = x > y ? -1 : 0) == z", checkInt32);
                        testResult = Fail;
                    }

                    CheckMethod<short> checkInt16 = (short x, short y, short z, ref short a)
                        => (a = (short)(x > y ? -1 : 0)) == z;

                    if (!shortTable.CheckResult(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(x, y, z) => (x > y ? -1 : 0) == z", checkInt16);
                        testResult = Fail;
                    }

                    CheckMethod<sbyte> checkSByte = (sbyte x, sbyte y, sbyte z, ref sbyte a)
                        => (a = (sbyte)(x > y ? -1 : 0)) == z;

                    if (!sbyteTable.CheckResult(checkSByte))
                    {
                        PrintError(sbyteTable, methodUnderTestName, "(x, y, z) => (x > y ? -1 : 0) == z", checkSByte);
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
