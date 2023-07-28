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
        public static unsafe void LoadAlignedVector128()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                byte* inBuffer = stackalloc byte[32];
                float* inArray = (float*)Align(inBuffer, 16);
                float* outArray = stackalloc float[4];

                var vf = Sse.LoadAlignedVector128(inArray);
                Unsafe.Write(outArray, vf);

                for (var i = 0; i < 4; i++)
                {
                    if (BitConverter.SingleToInt32Bits(inArray[i]) != BitConverter.SingleToInt32Bits(outArray[i]))
                    {
                        Console.WriteLine("SSE LoadAlignedVector128 failed on float:");
                        for (var n = 0; n < 4; n++)
                        {
                            Console.Write(outArray[n] + ", ");
                        }
                        Console.WriteLine();

                        testResult = Fail;
                        break;
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
