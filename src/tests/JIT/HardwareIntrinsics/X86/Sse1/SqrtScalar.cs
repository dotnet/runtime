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
        public static unsafe void SqrtScalar()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[4] { 1, -5, 100, 0 }, new float[4]))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Sse.SqrtScalar(vf1);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);

                    if (!floatTable.CheckResult((x, y, z) => {
                        var expected = MathF.Sqrt(y[0]);
                        return ((Math.Abs(expected - z[0]) <= 0.0003662109375f) // |Relative Error| <= 1.5 * 2^-12
                             || (float.IsNaN(expected) && float.IsNaN(z[0]))
                             || (float.IsNegativeInfinity(expected) && float.IsNegativeInfinity(z[0]))
                             || (float.IsPositiveInfinity(expected) && float.IsPositiveInfinity(z[0])))
                            && (z[1] == x[1]) && (z[2] == x[2]) && (z[3] == x[3]);
                    }))
                    {
                        Console.WriteLine("SSE SqrtScalar failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }

                using (TestTable_2Input<float> floatTable = new TestTable_2Input<float>(new float[4] { 1, -5, 100, 0 }, new float[4] { 22, -1, -50, 0 }, new float[4]))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector128<float>>(floatTable.inArray2Ptr);
                    var vf3 = Sse.SqrtScalar(vf1, vf2);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    if (!floatTable.CheckResult((x, y, z) => {
                        var expected = MathF.Sqrt(y[0]);
                        return ((Math.Abs(expected - z[0]) <= 0.0003662109375f) // |Relative Error| <= 1.5 * 2^-12
                             || (float.IsNaN(expected) && float.IsNaN(z[0]))
                             || (float.IsNegativeInfinity(expected) && float.IsNegativeInfinity(z[0]))
                             || (float.IsPositiveInfinity(expected) && float.IsPositiveInfinity(z[0])))
                            && (z[1] == x[1]) && (z[2] == x[2]) && (z[3] == x[3]);
                    }))
                    {
                        Console.WriteLine("SSE SqrtScalar failed on float:");
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
