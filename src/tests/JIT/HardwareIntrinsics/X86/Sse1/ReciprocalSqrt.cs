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
        public static unsafe void ReciprocalSqrt()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4]))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var vf2 = Sse.ReciprocalSqrt(vf1);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);

                    if (!floatTable.CheckResult((x, y) => {
                        var expected = 1 / MathF.Sqrt(x);
                        return (Math.Abs(expected - y) <= 0.0003662109375f) // |Relative Error| <= 1.5 * 2^-12
                            || (float.IsNaN(expected) && float.IsNaN(y))
                            || (float.IsNegativeInfinity(expected) && float.IsNegativeInfinity(y))
                            || (float.IsPositiveInfinity(expected) && float.IsPositiveInfinity(y));
                    }))
                    {
                        Console.WriteLine("SSE ReciprocalSqrt failed on float:");
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
