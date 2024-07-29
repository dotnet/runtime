// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Avx2
{
    public partial class Program
    {
        [Fact]
        public static unsafe void LoadAlignedVector256NonTemporal()
        {
            int testResult = Pass;

            if (Avx2.IsSupported)
            {
                {
                    byte* inBuffer = stackalloc byte[64];
                    int* inArray = (int*)Align(inBuffer, 32);
                    int* outArray = stackalloc int[8];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on int:");
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
                    byte* inBuffer = stackalloc byte[64];
                    long* inArray = (long*)Align(inBuffer, 32);
                    long* outArray = stackalloc long[4];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on long:");
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
                    byte* inBuffer = stackalloc byte[64];
                    uint* inArray = (uint*)Align(inBuffer, 32);
                    uint* outArray = stackalloc uint[8];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on uint:");
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
                    byte* inBuffer = stackalloc byte[64];
                    ulong* inArray = (ulong*)Align(inBuffer, 32);
                    ulong* outArray = stackalloc ulong[4];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on ulong:");
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
                    byte* inBuffer = stackalloc byte[64];
                    short* inArray = (short*)Align(inBuffer, 32);
                    short* outArray = stackalloc short[16];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on short:");
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
                    byte* inBuffer = stackalloc byte[64];
                    ushort* inArray = (ushort*)Align(inBuffer, 32);
                    ushort* outArray = stackalloc ushort[16];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on ushort:");
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
                    byte* inBuffer = stackalloc byte[64];
                    sbyte* inArray = (sbyte*)Align(inBuffer, 32);
                    sbyte* outArray = stackalloc sbyte[32];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on sbyte:");
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
                    byte* inBuffer = stackalloc byte[64];
                    byte* inArray = (byte*)Align(inBuffer, 32);
                    byte* outArray = stackalloc byte[32];
                    var vf = Avx2.LoadAlignedVector256NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 32; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Avx2 LoadAlignedVector256NonTemporal failed on byte:");
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
