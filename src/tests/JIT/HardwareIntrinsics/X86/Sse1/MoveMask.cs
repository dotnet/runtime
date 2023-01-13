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
        public static unsafe void MoveMask()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable_SingleArray<float> floatTable = new TestTable_SingleArray<float>(new float[4] { 1, -5, 100, 0 }))
                {

                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArray1Ptr);
                    var res = Sse.MoveMask(vf1);

                    if (res != 0b0010)
                    {
                        Console.WriteLine("SSE MoveMask failed on float:");
                        Console.WriteLine(res);
                        testResult = Fail;
                    }
                }
            }


            Assert.Equal(Pass, testResult);
        }
    }
}
