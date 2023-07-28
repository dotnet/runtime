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
        public static unsafe void StoreAligned()
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                float* inArray = stackalloc float[4];
                byte* outBuffer = stackalloc byte[32];
                float* outArray = (float*)Align(outBuffer, 16);

                var vf = Unsafe.Read<Vector128<float>>(inArray);
                Sse.StoreAligned(outArray, vf);

                for (var i = 0; i < 4; i++)
                {
                    if (BitConverter.SingleToInt32Bits(inArray[i]) != BitConverter.SingleToInt32Bits(outArray[i]))
                    {
                        Console.WriteLine("SSE StoreAligned failed on float:");
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
