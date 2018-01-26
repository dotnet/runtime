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
            string methodUnderTestName = nameof(Sse2.ConvertToVector128Double);

            if (Sse2.IsSupported)
            {
                using (var floatTable = TestTableSse2<float, double>.Create(testsCount, 2.0))
                using (var intTable = TestTableSse2<int, double>.Create(testsCount, 2.0))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<float>, Vector128<float>) value = floatTable[i];
                        var result = Sse2.ConvertToVector128Double(value.Item1);
                        floatTable.SetOutArrayU(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<int>, Vector128<int>) value = intTable[i];
                        var result = Sse2.ConvertToVector128Double(value.Item1);
                        intTable.SetOutArrayU(result);
                    }

                    CheckMethodFour<float, double> checkDouble = (float x1, float x2, double z1, double z2, ref double a1, ref double a2) =>
                    {
                        return (a1 = (double)x1) == z1 && (a2 = (double)x2) == z2;
                    };

                    if (!floatTable.CheckConvertToVector128Double(checkDouble))
                    {
                        PrintError(floatTable, methodUnderTestName, "(float x, float y, double z, ref double a) => (a = (double)x) == z", checkDouble);
                        testResult = Fail;
                    }

                    CheckMethodFour<int, double> checkInt32 = (int x1, int x2, double z1, double z2, ref double a1, ref double a2) =>
                    {
                        return (a1 = (double)x1) == z1 && (a2 = (double)x2) == z2;
                    };

                    if (!intTable.CheckConvertToVector128Double(checkInt32))
                    {
                        PrintError(intTable, methodUnderTestName, "(int x, int y, double z, ref double a) =>  (a = (double)x) == z", checkInt32);
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
