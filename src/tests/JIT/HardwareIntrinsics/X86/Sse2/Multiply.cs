// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void Multiply()
        {
            int testResult = Pass;
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.Multiply);

            if (Sse2.IsSupported)
            {
                using (var uintTable = TestTableSse2<uint, ulong>.Create(testsCount, 2.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<uint>, Vector128<uint>) value = uintTable[i];
                        Vector128<ulong> result = Sse2.Multiply(value.Item1, value.Item2);
                        uintTable.SetOutArrayU(result);
                    }

                    CheckMethodFive<uint, ulong> checkUInt32 = (uint x1, uint x2, uint y1, uint y2, ulong z1, ulong z2, ref ulong a1, ref ulong a2) =>
                    {
                        a1 = (ulong)x1 * y1;
                        a2 = (ulong)x2 * y2;
                        return a1 == z1 && a2 == z2;
                    };

                    if (!uintTable.CheckMultiplyUInt32ToUInt64(checkUInt32))
                    {
                        PrintError(uintTable, methodUnderTestName, "(uint x1, uint x2, uint y1, uint y2, ulong z1, ulong z2, ref ulong a1, ref ulong a2) => (a1 = (ulong)x1 * y1) == z1 && (a2 = (ulong)x2 * y2) == z2", checkUInt32);
                        Assert.Fail("");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Sse2.IsSupported: {Sse2.IsSupported}, skipped tests of {typeof(Sse2)}.{methodUnderTestName}");
            }
        }
    }
}
