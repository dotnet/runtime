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
        public static unsafe void MultiplyHigh()
        {
            int testResult = Pass;
            int testsCount = 21;
            string methodUnderTestName = nameof(Sse2.MultiplyHigh);

            if (Sse2.IsSupported)
            {
                using (var shortTable = TestTableSse2<short>.Create(testsCount))
                using (var ushortTable = TestTableSse2<ushort>.Create(testsCount))
                {
                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<short>, Vector128<short>, Vector128<short>) value = shortTable[i];
                        Vector128<short> result = Sse2.MultiplyHigh(value.Item1, value.Item2);
                        shortTable.SetOutArray(result);
                    }

                    for (int i = 0; i < testsCount; i++)
                    {
                        (Vector128<ushort>, Vector128<ushort>, Vector128<ushort>) value = ushortTable[i];
                        Vector128<ushort> result = Sse2.MultiplyHigh(value.Item1, value.Item2);
                        ushortTable.SetOutArray(result);
                    }

                    CheckMethod<short> checkInt16 = (short x, short y, short z, ref short a) => (a = (short)((x * y) >> 16)) == z;

                    if (!shortTable.CheckResult(checkInt16))
                    {
                        PrintError(shortTable, methodUnderTestName, "(short x, short y, short z, ref short a) => (a = (short)((x * y) >> 16)) == z", checkInt16);
                        testResult = Fail;
                    }

                    CheckMethod<ushort> checkUInt16 = (ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)((x * y) >> 16)) == z;

                    if (!ushortTable.CheckResult(checkUInt16))
                    {
                        PrintError(ushortTable, methodUnderTestName, "(ushort x, ushort y, ushort z, ref ushort a) => (a = (ushort)((x * y) >> 16)) == z", checkUInt16);
                        testResult = Fail;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Sse2.IsSupported: {Sse2.IsSupported}, skipped tests of {typeof(Sse2)}.{methodUnderTestName}");
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
