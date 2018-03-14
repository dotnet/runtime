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

            if (Sse2.IsSupported)
            {
                if (Environment.Is64BitProcess)
                {
                    {
                        long* inArray = stackalloc long[2];
                        inArray[0] = 0xffffffff01l;
                        long* outBuffer = stackalloc long[2];

                        Sse2.StoreNonTemporal(outBuffer, inArray[0]);

                        for (var i = 0; i < 2; i++)
                        {
                            if (inArray[i] != outBuffer[i])
                            {
                                Console.WriteLine("Sse2 StoreNonTemporal failed on long:");
                                for (var n = 0; n < 2; n++)
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
                        ulong* inArray = stackalloc ulong[2];
                        inArray[0] = 0xffffffffff01ul;
                        ulong* outBuffer = stackalloc ulong[2];

                        Sse2.StoreNonTemporal(outBuffer, inArray[0]);

                        for (var i = 0; i < 2; i++)
                        {
                            if (inArray[i] != outBuffer[i])
                            {
                                Console.WriteLine("Sse2 StoreNonTemporal failed on ulong:");
                                for (var n = 0; n < 2; n++)
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
                else
                {
                    try
                    {
                        long* inArray = stackalloc long[2];
                        inArray[0] = 0xffffffff01l;
                        long* outBuffer = stackalloc long[2];

                        Sse2.StoreNonTemporal(outBuffer, inArray[0]);
                        testResult = Fail;
                        Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.StoreNonTemporal)} failed on long: expected PlatformNotSupportedException exception.");
                    }
                    catch (PlatformNotSupportedException)
                    {

                    }
                    catch(Exception ex)
                    {
                        testResult = Fail;
                        Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.StoreNonTemporal)}-{ex} failed on long: expected PlatformNotSupportedException exception.");
                    }

                    try
                    {
                        ulong* inArray = stackalloc ulong[2];
                        inArray[0] = 0xffffffffff01ul;
                        ulong* outBuffer = stackalloc ulong[2];

                        Sse2.StoreNonTemporal(outBuffer, inArray[0]);
                        testResult = Fail;
                        Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.StoreNonTemporal)} failed on ulong: expected PlatformNotSupportedException exception.");
                    }
                    catch (PlatformNotSupportedException)
                    {

                    }
                    catch(Exception ex)
                    {
                        testResult = Fail;
                        Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.StoreNonTemporal)}-{ex} failed on ulong: expected PlatformNotSupportedException exception.");                            
                    }                    
                }

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

