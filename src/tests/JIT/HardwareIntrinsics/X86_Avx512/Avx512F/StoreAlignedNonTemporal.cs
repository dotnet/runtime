// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx512F
{
    public partial class Program
    {
        [Fact]
        public static unsafe void StoreAlignedNonTemporal()
        {
            int testResult = Pass;

            if (Avx512F.IsSupported)
            {
                {
                    double* inArray = stackalloc double[8];
                    byte* outBuffer = stackalloc byte[128];
                    double* outArray = (double*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<double>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (BitConverter.DoubleToInt64Bits(inArray[i]) != BitConverter.DoubleToInt64Bits(outArray[i]))
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on double:");
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
                    float* inArray = stackalloc float[16];
                    byte* outBuffer = stackalloc byte[128];
                    float* outArray = (float*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<float>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (BitConverter.SingleToInt32Bits(inArray[i]) != BitConverter.SingleToInt32Bits(outArray[i]))
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on float:");
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
                    long* inArray = stackalloc long[8];
                    byte* outBuffer = stackalloc byte[128];
                    long* outArray = (long*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<long>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on long:");
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
                    ulong* inArray = stackalloc ulong[8];
                    byte* outBuffer = stackalloc byte[128];
                    ulong* outArray = (ulong*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<ulong>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on ulong:");
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
                    int* inArray = stackalloc int[16];
                    byte* outBuffer = stackalloc byte[128];
                    int* outArray = (int*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<int>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on int:");
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
                    uint* inArray = stackalloc uint[16];
                    byte* outBuffer = stackalloc byte[128];
                    uint* outArray = (uint*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<uint>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on uint:");
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
                    short* inArray = stackalloc short[32];
                    byte* outBuffer = stackalloc byte[128];
                    short* outArray = (short*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<short>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on short:");
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
                    ushort* inArray = stackalloc ushort[32];
                    byte* outBuffer = stackalloc byte[128];
                    ushort* outArray = (ushort*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<ushort>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on ushort:");
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
                    byte* inArray = stackalloc byte[64];
                    byte* outBuffer = stackalloc byte[128];
                    byte* outArray = (byte*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<byte>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 64; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on byte:");
                            for (var n = 0; n < 64; n++)
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
                    sbyte* inArray = stackalloc sbyte[64];
                    byte* outBuffer = stackalloc byte[128];
                    sbyte* outArray = (sbyte*)Align(outBuffer, 64);

                    var vf = Unsafe.Read<Vector512<sbyte>>(inArray);
                    Avx512F.StoreAlignedNonTemporal(outArray, vf);

                    for (var i = 0; i < 64; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("AVX512F StoreAlignedNonTemporal failed on byte:");
                            for (var n = 0; n < 64; n++)
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
