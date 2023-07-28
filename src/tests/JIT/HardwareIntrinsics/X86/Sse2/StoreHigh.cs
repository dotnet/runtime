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
        public static unsafe void StoreHigh()
        {
            if (Sse2.IsSupported)
            {
                using (TestTable<double> doubleTable = new TestTable<double>(new double[2] { 1, -5 }, new double[2]))
                {
                    var vf = Unsafe.Read<Vector128<double>>(doubleTable.inArrayPtr);
                    Sse2.StoreHigh((double*)(doubleTable.outArrayPtr), vf);

                    if (!doubleTable.CheckResult((x, y) => y[0] == x[1] && y[1] == 0))
                    {
                        Console.WriteLine("SSE2 StoreHigh failed on double:");
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
