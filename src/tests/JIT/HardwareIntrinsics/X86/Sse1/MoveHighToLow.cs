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
        public static unsafe void MoveHighToLow()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[4] { 1, -5, 100, 0 }, new float[4] { 22, -1, -50, 0 }, new float[4]))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vf3 = Sse.MoveHighToLow(vf1, vf2);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => (z[0] == y[2]) && (z[1] == y[3]) &&
                                                             (z[2] == x[2]) && (z[3] == x[3])))
                    {
                        Console.WriteLine("SSE MoveHighToLow failed on float:");
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
