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
        public static unsafe void ConvertToInt32()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                using (TestTable_SingleArray<float> floatTable = new TestTable_SingleArray<float>(new float[4] { 1, -5, 100, 0 }))
                {
                    var vf1 = Unsafe.Read<Vector128<float>>(floatTable.inArrayPtr);
                    var i2 = Sse.ConvertToInt32(vf1);

                    if (i2 != ((int)floatTable.inArray[0]))
                    {
                        Console.WriteLine("SSE ConvertToInt32 failed on float:");
                        Console.WriteLine(i2);
                        testResult = Fail;
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
