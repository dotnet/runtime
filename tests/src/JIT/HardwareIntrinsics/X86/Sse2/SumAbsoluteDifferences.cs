// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Linq;
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
            string methodUnderTestName = nameof(Sse2.SumAbsoluteDifferences);

            if (Sse2.IsSupported)
            {
                using (var byteTable = TestTableSse2<byte, long>.Create(testsCount, 8.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<byte>, Vector128<byte>) value = byteTable[i];
                        var result = Sse2.SumAbsoluteDifferences(value.Item1, value.Item2);
                        byteTable.SetOutArrayU(result);
                    }

                    CheckMethodEightOne<byte, long> checkByte = (Span<byte> x, Span<byte> y, long z, ref long a) =>
                    {
                        short[] tmpArray = new short[8];
                        for (int i = 0; i < 8; i++)
                        {
                            tmpArray[i] = (short)Math.Abs(x[i] - y[i]);
                        }

                        foreach (short s in tmpArray) a += s;
                        return a == z;
                    };

                    if (!byteTable.CheckResult(checkByte))
                    {
                        PrintError(byteTable, methodUnderTestName, "(Span<byte> x, Span<byte> y, long z, ref long a) => SumAbsoluteDifferences", checkByte);
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
