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
        public static unsafe void StoreAligned()
        {
            int testResult = Pass;

            if (Sse2.IsSupported)
            {
                {
                    double* inArray = stackalloc double[2];
                    byte* outBuffer = stackalloc byte[32];
                    double* outArray = (double*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<double>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 2; i++)
                    {
                        if (BitConverter.DoubleToInt64Bits(inArray[i]) != BitConverter.DoubleToInt64Bits(outArray[i]))
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on double:");
                            for (var n = 0; n < 2; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    long* inArray = stackalloc long[2];
                    byte* outBuffer = stackalloc byte[32];
                    long* outArray = (long*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<long>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 2; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on long:");
                            for (var n = 0; n < 2; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    ulong* inArray = stackalloc ulong[2];
                    byte* outBuffer = stackalloc byte[32];
                    ulong* outArray = (ulong*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<ulong>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 2; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on ulong:");
                            for (var n = 0; n < 2; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    int* inArray = stackalloc int[4];
                    byte* outBuffer = stackalloc byte[32];
                    int* outArray = (int*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<int>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on int:");
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

                {
                    uint* inArray = stackalloc uint[4];
                    byte* outBuffer = stackalloc byte[32];
                    uint* outArray = (uint*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<uint>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on uint:");
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

                {
                    short* inArray = stackalloc short[8];
                    byte* outBuffer = stackalloc byte[32];
                    short* outArray = (short*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<short>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on short:");
                            for (var n = 0; n < 8; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    ushort* inArray = stackalloc ushort[8];
                    byte* outBuffer = stackalloc byte[32];
                    ushort* outArray = (ushort*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<ushort>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on ushort:");
                            for (var n = 0; n < 8; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    byte* inArray = stackalloc byte[16];
                    byte* outBuffer = stackalloc byte[32];
                    byte* outArray = (byte*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<byte>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on byte:");
                            for (var n = 0; n < 16; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }

                {
                    sbyte* inArray = stackalloc sbyte[16];
                    byte* outBuffer = stackalloc byte[32];
                    sbyte* outArray = (sbyte*)Align(outBuffer, 16);

                    var vf = Unsafe.Read<Vector128<sbyte>>(inArray);
                    Sse2.StoreAligned(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse2 StoreAligned failed on byte:");
                            for (var n = 0; n < 16; n++)
                            {
                                Console.Write(outArray[n] + ", ");
                            }
                            Console.WriteLine();

                            testResult = Fail;
                            break;
                        }
                    }
                }
            }

            Assert.Equal(Pass, testResult);
        }
    }
}
