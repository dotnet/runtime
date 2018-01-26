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
            string methodUnderTestName = nameof(Sse2.ConvertToVector128Single);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double, float>.Create(testsCount, 0.5))
                using (var intTable = TestTableSse2<int, float>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>) value = doubleTable[i];
                        Vector128<float> result = Sse2.ConvertToVector128Single(value.Item1);
                        doubleTable.SetOutArrayU(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<int>, Vector128<int>) value = intTable[i];
                        Vector128<float> result = Sse2.ConvertToVector128Single(value.Item1);
                        intTable.SetOutArrayU(result);
                    }

                    CheckMethodFour<double, float> checkDouble = (double x1, double x2, float z1, float z2, ref float a1, ref float a2) =>
                    {
                        return (a1 = (float)x1) == z1 && (a2 = (float)x2) == z2;
                    };

                    if (!doubleTable.CheckConvertDoubleToVector128Single(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(double x1, double x2, float z1, float z2, ref float a1, ref float a2) => (a = (float)x) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethodFour<int, float> checkInt32 = (int x1, int x2, float z1, float z2, ref float a1, ref float a2) =>
                    {
                        return (a1 = (float)x1) == z1 && (a2 = (float)x2) == z2;
                    };

                    if (!intTable.CheckConvertInt32ToVector128Single(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(int x1, int x2, float z1, float z2, ref float a1, ref float a2) => (a = (float)x) == z", checkInt32);
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
