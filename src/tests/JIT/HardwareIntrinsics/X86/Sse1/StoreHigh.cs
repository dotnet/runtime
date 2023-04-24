// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Sse1
{
    public partial class Program
    {
        [Fact]
        public static unsafe void StoreHigh()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4]))
                {
                    var vf = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    Sse.StoreHigh((float*)(floatTable.outArrayPtr), vf);

                    if (!floatTable.CheckResult((x, y) => y[0] == x[2] && y[1] == x[3] &&
                                                          y[2] == 0    && y[3] == 0))
                    {
                        Console.WriteLine("SSE StoreHigh failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
