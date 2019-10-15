// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;

            if (Sse.IsSupported)
            {
                float* inArray = stackalloc float[4];
                byte* outBuffer = stackalloc byte[32];
                float* outArray = Align(outBuffer, 16);

                var vf = Unsafe.Read<Vector128<float>>(inArray);
                Sse.StoreAlignedNonTemporal(outArray, vf);

                for (var i = 0; i < 4; i++)
                {
                    if (BitConverter.SingleToInt32Bits(inArray[i]) != BitConverter.SingleToInt32Bits(outArray[i]))
                    {
                        Console.WriteLine("SSE StoreAlignedNonTemporal failed on float:");
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

            return testResult;
        }

        static unsafe float* Align(byte* buffer, byte expectedAlignment)
        {
            // Compute how bad the misalignment is, which is at most (expectedAlignment - 1).
            // Then subtract that from the expectedAlignment and add it to the original address
            // to compute the aligned address.

            var misalignment = expectedAlignment - ((ulong)(buffer) % expectedAlignment);
            return (float*)(buffer + misalignment);
        }
    }
}
