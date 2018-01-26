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
            string methodUnderTestName = nameof(Sse2.ConvertToVector128Int32WithTruncation);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double, int>.Create(testsCount, 0.5))
                using (var floatTable = TestTableSse2<float, int>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>) value = doubleTable[i];
                        Vector128<int> result = Sse2.ConvertToVector128Int32WithTruncation(value.Item1);
                        doubleTable.SetOutArrayU(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<float>, Vector128<float>) value = floatTable[i];
                        Vector128<int> result = Sse2.ConvertToVector128Int32WithTruncation(value.Item1);
                        floatTable.SetOutArrayU(result);
                    }

                    CheckMethodFour<double, int> checkDouble = (double x1, double x2, int z1, int z2, ref int a1, ref int a2) =>
                    {
                        return (a1 = (int)(x1 > 0 ? Math.Floor(x1) : Math.Ceiling(x1))) == z1 &&
                            (a2 = (int)(x2 > 0 ? Math.Floor(x2) : Math.Ceiling(x2))) == z2;
                    };

                    if (!doubleTable.CheckConvertDoubleToVector128Single(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(double x, double y, int z, ref int a) => (a = convertWithTruncation((int)x)) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethodTwo<float, int> checkFloat = (float x, float y, int z, ref int a) => (a = (int)(x > 0 ? Math.Floor(x) : Math.Ceiling(x))) == z;

                    if (!floatTable.CheckResult(checkFloat))
                    {
                        PrintError(floatTable, methodUnderTestName, "(float x, float y, int z, ref int a) => (a1 = (int)(x > 0 ? Math.Floor(x) : Math.Ceiling(x))) == z", checkFloat);
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
