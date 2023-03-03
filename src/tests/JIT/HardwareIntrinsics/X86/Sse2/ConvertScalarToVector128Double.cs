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
        public static unsafe void ConvertScalarToVector128Double()
        {
            if (Sse2.IsSupported)
            {

                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2]))
                using (TestTable<float> floatTable = new TestTable<float>(new float[4] { 3, -11, 7, 49 }, new float[4]))
                {
                    var vd0 = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var vf2 = Sse2.ConvertScalarToVector128Double(vd0, vf1);
                    Unsafe.Write(doubleTable.outArrayPtr, vf2);

                    if (!doubleTable.CheckResult((x, y) => (y[0] == 3) && (y[1] == x[1])))
                    {
                        Console.WriteLine("SSE2 ConvertScalarToVector128Double failed on float:");
                        foreach (var item in doubleTable.outArray)
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
