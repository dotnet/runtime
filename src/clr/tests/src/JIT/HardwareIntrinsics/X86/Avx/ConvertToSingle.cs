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

        static unsafe int Main()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                fixed (float* ptr = new float[8] { 1, -5, 100, 0, 2, 30, -6, 42 })
                {
                    var v = Unsafe.Read<Vector256<float>>(ptr);
                    var f = Avx.ConvertToSingle(v);

                    if (f != ptr[0])
                    {
                        Console.WriteLine("AVX ConvertToSingle failed on float:");
                        Console.WriteLine(f);
                        testResult = Fail;
                    }
                }
            }

            return testResult;
        }
    }
}
