// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._Sse41
{
    public partial class Program
    {
        [Fact]
        public static unsafe void LoadAlignedVector128NonTemporal()
        {
            int testResult = Pass;

            if (Sse41.IsSupported)
            {
                {
                    byte* inBuffer = stackalloc byte[32];
                    int* inArray = (int*)Align(inBuffer, 16);
                    int* outArray = stackalloc int[4];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on int:");
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
                    byte* inBuffer = stackalloc byte[32];
                    long* inArray = (long*)Align(inBuffer, 16);
                    long* outArray = stackalloc long[2];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 2; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on long:");
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
                    byte* inBuffer = stackalloc byte[32];
                    uint* inArray = (uint*)Align(inBuffer, 16);
                    uint* outArray = stackalloc uint[4];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 4; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on uint:");
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
                    byte* inBuffer = stackalloc byte[32];
                    ulong* inArray = (ulong*)Align(inBuffer, 16);
                    ulong* outArray = stackalloc ulong[2];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 2; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on ulong:");
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
                    byte* inBuffer = stackalloc byte[32];
                    short* inArray = (short*)Align(inBuffer, 16);
                    short* outArray = stackalloc short[8];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on short:");
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
                    byte* inBuffer = stackalloc byte[32];
                    ushort* inArray = (ushort*)Align(inBuffer, 16);
                    ushort* outArray = stackalloc ushort[8];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 8; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on ushort:");
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
                    byte* inBuffer = stackalloc byte[32];
                    sbyte* inArray = (sbyte*)Align(inBuffer, 16);
                    sbyte* outArray = stackalloc sbyte[16];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on sbyte:");
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
                    byte* inBuffer = stackalloc byte[32];
                    byte* inArray = (byte*)Align(inBuffer, 16);
                    byte* outArray = stackalloc byte[16];
                    var vf = Sse41.LoadAlignedVector128NonTemporal(inArray);
                    Unsafe.Write(outArray, vf);

                    for (var i = 0; i < 16; i++)
                    {
                        if (inArray[i] != outArray[i])
                        {
                            Console.WriteLine("Sse41 LoadAlignedVector128NonTemporal failed on byte:");
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
