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
        public static unsafe void LoadAlignedVector512NonTemporal()
        {
            int testResult = Pass;

            if (Avx512F.IsSupported)
            {
                {
                    byte* inBuffer = stackalloc byte[128];
                    int* inArray = (int*)Align(inBuffer, 64);
                    int* outArray = stackalloc int[16];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on int:");
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
                    byte* inBuffer = stackalloc byte[128];
                    long* inArray = (long*)Align(inBuffer, 64);
                    long* outArray = stackalloc long[8];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on long:");
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
                    byte* inBuffer = stackalloc byte[128];
                    uint* inArray = (uint*)Align(inBuffer, 64);
                    uint* outArray = stackalloc uint[16];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on uint:");
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
                    byte* inBuffer = stackalloc byte[128];
                    ulong* inArray = (ulong*)Align(inBuffer, 64);
                    ulong* outArray = stackalloc ulong[8];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on ulong:");
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
                    byte* inBuffer = stackalloc byte[128];
                    short* inArray = (short*)Align(inBuffer, 64);
                    short* outArray = stackalloc short[32];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on short:");
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
                    byte* inBuffer = stackalloc byte[128];
                    ushort* inArray = (ushort*)Align(inBuffer, 64);
                    ushort* outArray = stackalloc ushort[32];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on ushort:");
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
                    byte* inBuffer = stackalloc byte[128];
                    sbyte* inArray = (sbyte*)Align(inBuffer, 64);
                    sbyte* outArray = stackalloc sbyte[64];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 64; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on sbyte:");
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
                    byte* inBuffer = stackalloc byte[128];
                    byte* inArray = (byte*)Align(inBuffer, 64);
                    byte* outArray = stackalloc byte[64];
                    var vf = Avx512F.LoadAlignedVector512NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 64; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx512F LoadAlignedVector512NonTemporal failed on byte:");
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
