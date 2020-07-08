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
            string methodUnderTestName = nameof(Sse2.MoveScalar);

            if (Sse2.IsSupported)
            {
                using (var longTable = TestTableScalarSse2<long, long>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<long>, Vector128<long>) value = longTable[i];
                        Vector128<long> result = Sse2.MoveScalar(value.Item1);
                        longTable.SetOutArray(result, i);
                    }

                    CheckMethodEight<long, long> checkLong = (Span<long> x, Span<long> y, Span<long> z, Span<long> a) =>
                    {
                        a[0] = x[0];
                        a[1] = 0;
                        return a[0] == z[0] && a[1] == z[1];
                    };

                    if (!longTable.CheckResult(checkLong))
                    {
                        PrintError(longTable, methodUnderTestName, "(Span<long> x, Span<long> y, Span<long> z, Span<long> a) => MoveScalar", checkLong);
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
