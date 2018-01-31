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
        const int Pass = 100;
        const int Fail = 0;

        internal static unsafe int Main(string[] args)
        {
            int testResult = Pass;
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.MultiplyHorizontalAdd);

            if (Sse2.IsSupported)
            {
                Console.WriteLine($"Test started");

                using (var shortTable = TestTableSse2<short, int>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>) value = shortTable[i];
                        var result = Sse2.MultiplyHorizontalAdd(value.Item1, value.Item2);
                        shortTable.SetOutArrayU(result);
                    }

                    CheckMethodThree<short, int> checkInt16 = (short x1, short x2, short y1, short y2, int z, ref int a) =>
                    (a = (int)x1 * y1 + (int)x2 * y2) == z;

                    if (!shortTable.CheckMultiplyHorizontalAdd(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(short x1, short x2, short y1, short y2, int z, ref int a) => (a = (int)x1 * y1 + (int)x2 * y2) == z", checkInt16);
                        testResult = Fail;
                    }
                }

                Console.WriteLine($"Test finished with result: {testResult}");
            }
            else
            {
                Console.WriteLine($"Sse2.IsSupported: {Sse2.IsSupported}, skipped tests of {typeof(Sse2)}.{methodUnderTestName}");
            }

            return testResult;
        }
    }
}
