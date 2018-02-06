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
            string methodUnderTestName = nameof(Sse2.ConvertToUInt64);

            if (Sse2.IsSupported)
            {
                using (var uintTable = TestTableScalarSse2<ulong, ulong>.Create(testsCount, 2.0))
                {
                    if (Environment.Is64BitProcess)
                    {
                        for (int i = 0; i < testsCount; i++)
                        {
                            (Vector128<ulong>, Vector128<ulong>) value = uintTable[i];
                            var result = Sse2.ConvertToUInt64(value.Item1);
                            uintTable.SetOutArray(result);
                        }

                        CheckMethodEightOne<ulong, ulong> checkUInt64 = (Span<ulong> x, Span<ulong> y, ulong z, ref ulong a) =>
                        {
                            a = x[0];
                            return a == z;
                        };

                        if (!uintTable.CheckResult(checkUInt64))
                        {
                            PrintError(uintTable, methodUnderTestName, "(Span<int> x, Span<int> y, int z, ref int a) => ConvertToInt32", checkUInt64);
                            testResult = Fail;
                        }
                    }
                    else
                    {
                        try
                        {
                            for (int i = 0; i < testsCount; i++)
                            {
                                (Vector128<ulong>, Vector128<ulong>) value = uintTable[i];
                                var result = Sse2.ConvertToUInt64(value.Item1);
                                uintTable.SetOutArray(result);
                            }
                            testResult = Fail;
                            Console.WriteLine($"{nameof(Sse2)}.{methodUnderTestName} failed: expected PlatformNotSupportedException exception.");
                        }
                        catch (PlatformNotSupportedException)
                        {

                        }
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
