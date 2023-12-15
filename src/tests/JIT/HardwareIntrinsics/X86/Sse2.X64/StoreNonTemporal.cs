// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2.X64
{
    public class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        [Fact]
        public static unsafe void StoreNonTemporal()
        {
            int testResult = Pass;

            if (Sse2.X64.IsSupported)
            {
                {
                    long* inArray = stackalloc long[2];
                    inArray[0] = 0xffffffff01l;
                    long* outBuffer = stackalloc long[2];

                    Sse2.X64.StoreNonTemporal(outBuffer, inArray[0]);

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

                    Sse2.X64.StoreNonTemporal(outBuffer, inArray[0]);

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

                    Sse2.X64.StoreNonTemporal(outBuffer, inArray[0]);
                    testResult = Fail;
                    Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.X64.StoreNonTemporal)} failed on long: expected PlatformNotSupportedException exception.");
                }
                catch (PlatformNotSupportedException)
                {

                }
                catch (Exception ex)
                {
                    testResult = Fail;
                    Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.X64.StoreNonTemporal)}-{ex} failed on long: expected PlatformNotSupportedException exception.");
                }

                try
                {
                    ulong* inArray = stackalloc ulong[2];
                    inArray[0] = 0xffffffffff01ul;
                    ulong* outBuffer = stackalloc ulong[2];

                    Sse2.X64.StoreNonTemporal(outBuffer, inArray[0]);
                    testResult = Fail;
                    Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.X64.StoreNonTemporal)} failed on ulong: expected PlatformNotSupportedException exception.");
                }
                catch (PlatformNotSupportedException)
                {

                }
                catch (Exception ex)
                {
                    testResult = Fail;
                    Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.X64.StoreNonTemporal)}-{ex} failed on ulong: expected PlatformNotSupportedException exception.");
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}

