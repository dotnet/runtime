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
            string methodUnderTestName = nameof(Sse2.MultiplyLow);

            if (Sse2.IsSupported)
            {
                using (var shortTable = TestTableSse2<short>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>, Vector128<short>) value = shortTable[i];
                        Vector128<short> result = Sse2.MultiplyLow(value.Item1, value.Item2);
                        shortTable.SetOutArray(result);
                    }

                    CheckMethod<short> checkInt16 = (short x, short y, short z, ref short a) =>
                    {
                        int tmp = (((int)x * (int)y) & 0x0000ffff);
                        a = unchecked((short)tmp);
                        return a == z;
                    };

                    if (!shortTable.CheckResult(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(short x, short y, int z, ref int a) => (a = (int)x * y >> 16 | sign) == z", checkInt16);
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
