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
            string methodUnderTestName = nameof(Sse2.Average);

            if (Sse2.IsSupported)
            {
                using (var ushortTable = TestTableSse2<ushort>.Create(testsCount))
                using (var byteTable = TestTableSse2<byte>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ushort>, Vector128<ushort>, Vector128<ushort>) value = ushortTable[i];
                        Vector128<ushort> result = Sse2.Average(value.Item1, value.Item2);
                        ushortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<byte>, Vector128<byte>, Vector128<byte>) value = byteTable[i];
                        Vector128<byte> result = Sse2.Average(value.Item1, value.Item2);
                        byteTable.SetOutArray(result);
                    }

                    CheckMethod<ushort> checkUshort = (ushort x, ushort y, ushort z, ref ushort a) =>
                    (a = (ushort)((x + y + 1) >> 1)) == z;

                    if (!ushortTable.CheckResult(checkUshort))
                    {
                        PrintError(ushortTable, methodUnderTestName, "(x, y, z, ref a) => (a = (x + y + 1) >> 1) == z", checkUshort);
                        testResult = Fail;
                    }

                    CheckMethod<byte> checkByte = (byte x, byte y, byte z, ref byte a) =>
                    (a = (byte)((x + y + 1) >> 1)) == z;

                    if (!byteTable.CheckResult(checkByte))
                    {
                        PrintError(byteTable, methodUnderTestName, "(x, y, z, ref a) => (a = (x + y + 1) >> 1) == z", checkByte);
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
