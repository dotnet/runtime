// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void ConvertScalarToVector128Single()
        {
            if (Sse2.IsSupported)
            {
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 1, -5, 100, 0 }, new float[4]))
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { -11, 7 }, new double[2]))
                {
                    var vd0 = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var vf2 = Sse2.ConvertScalarToVector128Single(vf1, vd0);
                    Unsafe.Write(floatTable.outArrayPtr, vf2);

                    if (!floatTable.CheckResult((x, y) => (y[0] == -11)
                                                       && (y[1] == x[1]) && (y[2] == x[2]) && (y[3] == x[3])))
                    {
                        Console.WriteLine("SSE ConvertScalarToVector128Single failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        Assert.Fail("");
                    }
                }
            }
        }
    }
}
