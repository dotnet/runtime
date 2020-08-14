// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

            if (Sse2.IsSupported)
            {
                {
                    int* inArray = stackalloc int[4];
                    inArray[0] = -784561;
                    int* outBuffer = stackalloc int[4];

                    Sse2.StoreNonTemporal(outBuffer, inArray[0]);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outBuffer[i])
                        {
                            Console.WriteLine("Sse2 StoreNonTemporal failed on int:");
                            for (var n = 0; n < 4; n++)
                            {
                                Console.Write(outBuffer[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    uint* inArray = stackalloc uint[4];
                    inArray[0] = 0xffffff02u;
                    uint* outBuffer = stackalloc uint[4];

                    Sse2.StoreNonTemporal(outBuffer, inArray[0]);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outBuffer[i])
                        {
                            Console.WriteLine("Sse2 StoreNonTemporal failed on uint:");
                            for (var n = 0; n < 4; n++)
                            {
                                Console.Write(outBuffer[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }
            }

            return testResult;
        }
    }
}

