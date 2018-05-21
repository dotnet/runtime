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
            string methodUnderTestName = nameof(Sse2.ConvertToInt32WithTruncation);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableScalarSse2<double, int>.Create(testsCount, 2.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>) value = doubleTable[i];
                        int result = Sse2.ConvertToInt32WithTruncation(value.Item1);
                        doubleTable.SetOutArray(result);
                    }

                    CheckMethodEightOne<double, int> checkDouble = (Span<double> x, Span<double> y, int z, ref int a) =>
                    {
                        a = (int) x[0];
                        return a == z;
                    };

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(Span<double> x, Span<double> y, int z, ref int a) => ConvertToInt32", checkDouble);
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
