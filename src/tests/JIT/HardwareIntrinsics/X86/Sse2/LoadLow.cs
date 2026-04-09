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
        public static unsafe void LoadLow()
        {
            if (Sse2.IsSupported)
            {
                using (TestTable_2Input<double> doubleTable = new TestTable_2Input<double>(new double[2] { 1, -5 }, new double[2] { 22, -1 }, new double[2]))
                {
                    var vf1 = Unsafe.Read<Vector128<double>>(doubleTable.inArray1Ptr);
                    var vf2 = Sse2.LoadLow(vf1, (double*)(doubleTable.inArray2Ptr));
                    Unsafe.Write(doubleTable.outArrayPtr, vf2);

                    if (!doubleTable.CheckResult((x, y, z) => z[0] == y[0] && z[1] == x[1]))
                    {
                        Console.WriteLine("SSE2 LoadLow failed on double:");
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
