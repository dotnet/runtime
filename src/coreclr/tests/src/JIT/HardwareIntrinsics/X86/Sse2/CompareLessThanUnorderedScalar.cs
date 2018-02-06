// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

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
            string methodUnderTestName = nameof(Sse2.CompareLessThanUnorderedScalar);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableScalarSse2<double, bool>.Create(testsCount, 2.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>) value = doubleTable[i];
                        var result = Sse2.CompareLessThanUnorderedScalar(value.Item1, value.Item2);
                        doubleTable.SetOutArray(result);
                    }

                    CheckMethodEightOne<double, bool> checkDouble = (Span<double> x, Span<double> y, bool z, ref bool a) =>
                    {
                        a = x[0] < y[0] ? true : false;
                        return a == z;
                    };

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(Span<double> x, Span<double> y, bool z, ref bool a) => CompareLessThanUnorderedScalar", checkDouble);
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
