// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void Sqrt()
        {
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.Sqrt);

            if (Sse2.IsSupported)
            {
                using (var doubleTable = TestTableSse2<double>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<double>, Vector128<double>, Vector128<double>) value = doubleTable[i];
                        var result = Sse2.Sqrt(value.Item1);
                        doubleTable.SetOutArray(result);
                    }

                    CheckMethod<double> checkDouble = (double x, double y, double z, ref double a) => (a = Math.Sqrt(x)) == z;

                    if (!doubleTable.CheckResult(checkDouble))
                    {
                        PrintError(doubleTable, methodUnderTestName, "(double x, double y, double z, ref double a) => (a = x - y) == z", checkDouble);
                        Assert.Fail("");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Sse2.IsSupported: {Sse2.IsSupported}, skipped tests of {typeof(Sse2)}.{methodUnderTestName}");
            }
        }
    }
}
