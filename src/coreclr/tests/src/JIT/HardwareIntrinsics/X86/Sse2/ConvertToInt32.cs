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
            string methodUnderTestName = nameof(Sse2.ConvertToInt32);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableScalarSse2<double, int>.Create(testsCount, 2.0))
                using (var intTable = TestTableScalarSse2<int, int>.Create(testsCount, 4.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        doubleTable.SetIndex(i);
                        int result = Sse2.ConvertToInt32(doubleTable.Vector1);
                        doubleTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        intTable.SetIndex(i);
                        int result = Sse2.ConvertToInt32(intTable.Vector1);
                        intTable.SetOutArray(result);
                    }

                    CheckMethodEightOne<double, int> checkDouble = (Span<double> x, Span<double> y, int z, ref int a) =>
                    {
                        a = (int) Math.Round(x[0], 0);
                        return a == z;
                    };

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(Span<double> x, Span<double> y, int z, ref int a) => ConvertToInt32", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethodEightOne<int, int> checkInt32 = (Span<int> x, Span<int> y, int z, ref int a) =>
                    {
                        a = x[0];
                        return a == z;
                    };

                    if (!intTable.CheckResult(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(Span<int> x, Span<int> y, int z, ref int a) => ConvertToInt32", checkInt32);
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
