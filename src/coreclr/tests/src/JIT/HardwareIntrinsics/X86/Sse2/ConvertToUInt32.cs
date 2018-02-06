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
            string methodUnderTestName = nameof(Sse2.ConvertToUInt32);

            if (Sse2.IsSupported)
            {
                using (var uintTable = TestTableScalarSse2<uint, uint>.Create(testsCount, 4.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<uint>, Vector128<uint>) value = uintTable[i];
                        var result = Sse2.ConvertToUInt32(value.Item1);
                        uintTable.SetOutArray(result);
                    }

                    CheckMethodEightOne<uint, uint> checkUInt32 = (Span<uint> x, Span<uint> y, uint z, ref uint a) =>
                    {
                        a = x[0];
                        return a == z;
                    };

                    if (!uintTable.CheckResult(checkUInt32))
                    {
                        PrintError(uintTable, methodUnderTestName, "(Span<int> x, Span<int> y, int z, ref int a) => ConvertToInt32", checkUInt32);
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
