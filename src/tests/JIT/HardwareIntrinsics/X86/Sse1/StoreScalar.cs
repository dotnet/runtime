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
        public static unsafe void StoreScalar()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 3 }, new float[4]))
                {
                    var vf = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    Sse.StoreScalar((float*)(floatTable.outArrayPtr), vf);

                    if (!floatTable.CheckResult((x, y) => BitConverter.SingleToInt32Bits(x[0]) == BitConverter.SingleToInt32Bits(y[0])
                                                       && BitConverter.SingleToInt32Bits(y[1]) == 0
                                                       && BitConverter.SingleToInt32Bits(y[2]) == 0
                                                       && BitConverter.SingleToInt32Bits(y[3]) == 0))
                    {
                        Console.WriteLine("SSE StoreScalar failed on float:");
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
