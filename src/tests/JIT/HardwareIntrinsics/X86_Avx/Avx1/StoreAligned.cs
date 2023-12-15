// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.Avx1
{
    public partial class Program
    {
        [Fact]
        public static unsafe void StoreAligned()
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                {
                    double* inArray = stackalloc double[4];
                    byte* outBuffer = stackalloc byte[64];
                    double* outArray = (double*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<double>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (BitConverter.DoubleToInt64Bits(inArray[i]) != BitConverter.DoubleToInt64Bits(outArray[i]))
                        {
                            Console.WriteLine("Avx StoreAligned failed on double:");
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
                    float* inArray = stackalloc float[8];
                    byte* outBuffer = stackalloc byte[64];
                    float* outArray = (float*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<float>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (BitConverter.SingleToInt32Bits(inArray[i]) != BitConverter.SingleToInt32Bits(outArray[i]))
                        {
                            Console.WriteLine("Avx StoreAligned failed on float:");
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
                    long* inArray = stackalloc long[4];
                    byte* outBuffer = stackalloc byte[64];
                    long* outArray = (long*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<long>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on long:");
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
                    ulong* inArray = stackalloc ulong[4];
                    byte* outBuffer = stackalloc byte[64];
                    ulong* outArray = (ulong*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<ulong>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on ulong:");
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
                    int* inArray = stackalloc int[8];
                    byte* outBuffer = stackalloc byte[64];
                    int* outArray = (int*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<int>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on int:");
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
                    uint* inArray = stackalloc uint[8];
                    byte* outBuffer = stackalloc byte[64];
                    uint* outArray = (uint*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<uint>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on uint:");
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
                    short* inArray = stackalloc short[16];
                    byte* outBuffer = stackalloc byte[64];
                    short* outArray = (short*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<short>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on short:");
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
                    ushort* inArray = stackalloc ushort[16];
                    byte* outBuffer = stackalloc byte[64];
                    ushort* outArray = (ushort*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<ushort>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on ushort:");
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
                    byte* inArray = stackalloc byte[32];
                    byte* outBuffer = stackalloc byte[64];
                    byte* outArray = (byte*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<byte>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on byte:");
                            for (var n = 0; n < 32; n++)
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
                    sbyte* inArray = stackalloc sbyte[32];
                    byte* outBuffer = stackalloc byte[64];
                    sbyte* outArray = (sbyte*)Align(outBuffer, 32);

                    var vf = Unsafe.Read<Vector256<sbyte>>(inArray);
                    Avx.StoreAligned(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx StoreAligned failed on byte:");
                            for (var n = 0; n < 32; n++)
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
