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
            string methodUnderTestName = nameof(Sse2.AddSaturate);

            if (Sse2.IsSupported)
            {
                using (var shortTable = TestTableSse2<short>.Create(testsCount))
                using (var ushortTable = TestTableSse2<ushort>.Create(testsCount))
                using (var sbyteTable = TestTableSse2<sbyte>.Create(testsCount))
                using (var byteTable = TestTableSse2<byte>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>, Vector128<short>) value = shortTable[i];
                        var result = Sse2.AddSaturate(value.Item1, value.Item2);
                        shortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ushort>, Vector128<ushort>, Vector128<ushort>) value = ushortTable[i];
                        var result = Sse2.AddSaturate(value.Item1, value.Item2);
                        ushortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<sbyte>, Vector128<sbyte>, Vector128<sbyte>) value = sbyteTable[i];
                        var result = Sse2.AddSaturate(value.Item1, value.Item2);
                        sbyteTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<byte>, Vector128<byte>, Vector128<byte>) value = byteTable[i];
                        var result = Sse2.AddSaturate(value.Item1, value.Item2);
                        byteTable.SetOutArray(result);
                    }

                    CheckMethod<short> checkInt16 = (short x, short y, short z, ref short a) =>
                    {
                        int value = x + y;
                        value = Math.Max(value, short.MinValue);
                        value = Math.Min(value, short.MaxValue);
                        a = (short) value;
                        return a == z;
                    };

                    if (!shortTable.CheckResult(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(short x, short y, short z, ref short a) => (a = (short)(x & y)) == z", checkInt16);
                        testResult = Fail;
                    }

                    CheckMethod<ushort> checkUInt16 = (ushort x, ushort y, ushort z, ref ushort a) =>
                    {
                        int value = x + y;
                        value = Math.Max(value, 0);
                        value = Math.Min(value, ushort.MaxValue);
                        a = (ushort) value;
                        return a == z;
                    };

                    if (!ushortTable.CheckResult(checkUInt16))
                    {
                        PrintError(ushortTable, methodUnderTestName, "(ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)(x & y)) == z", checkUInt16);
                        testResult = Fail;
                    }

                    CheckMethod<sbyte> checkSByte = (sbyte x, sbyte y, sbyte z, ref sbyte a) =>
                    {
                        int value = x + y;
                        value = Math.Max(value, sbyte.MinValue);
                        value = Math.Min(value, sbyte.MaxValue);
                        a = (sbyte) value;
                        return a == z;
                    };

                    if (!sbyteTable.CheckResult(checkSByte))
                    {
                        PrintError(sbyteTable, methodUnderTestName, "(sbyte x, sbyte y, sbyte z, ref sbyte a) => (a = (sbyte)(x & y)) == z", checkSByte);
                        testResult = Fail;
                    }

                    CheckMethod<byte> checkByte = (byte x, byte y, byte z, ref byte a) =>
                    {
                        int value = x + y;
                        value = Math.Max(value, 0);                        
                        value = Math.Min(value, byte.MaxValue);
                        a = (byte) value;
                        return a == z;
                    };

                    if (!byteTable.CheckResult(checkByte))
                    {
                        PrintError(byteTable, methodUnderTestName, "(byte x, byte y, byte z, ref byte a) => (a = (byte)(x & y)) == z", checkByte);
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
