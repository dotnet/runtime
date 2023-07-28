// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace IntrinsicsMisoptimizationTest {
    public class Program {
        unsafe static void WriteArray (float* ptr, int count)
        {
            Console.Write ("[");
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    Console.Write (", ");

                Console.Write (ptr [i]);
            }
            Console.WriteLine ("]");
        }

        unsafe static bool TestXmm_NoCSE()
        {
            const int VecLen = 4;

            int result = -1;
            var mem = stackalloc float [VecLen];
            var memSpan = new Span<float> (mem, VecLen);
            for (int i = 0; i < 1; i++)
            {
                if (Avx.IsSupported)
                {
                    Vector128<float> x1, x2, x3;

                    memSpan.Fill (25);
                    x1 = Avx.LoadVector128 (mem);

                    memSpan.Fill (75);
                    x2 = Avx.LoadVector128 (mem);

                    x3 = Avx.Add (x1, x2);

                    Avx.Store (mem, x3);
                    WriteArray (mem, VecLen);
                }
                else if (AdvSimd.IsSupported)
                {
                    Vector128<float> x1, x2, x3;

                    memSpan.Fill (25);
                    x1 = AdvSimd.LoadVector128 (mem);

                    memSpan.Fill (75);
                    x2 = AdvSimd.LoadVector128 (mem);

                    x3 = AdvSimd.Add (x1, x2);

                    AdvSimd.Store (mem, x3);
                    WriteArray (mem, VecLen);
                }
                else
                {
                    Console.WriteLine("Hardware Intrinsics not supported");
                    return true;
                }
            }

            if (mem[0] != 100.00)
            {
                Console.WriteLine("XMM_NoCSE Test Failed");
                return false;
            }
            return true;
        }

        unsafe static bool TestXmm_CanCSE()
        {
            const int VecLen = 4;

            int result = -1;
            var mem = stackalloc float [VecLen];
            var memSpan = new Span<float> (mem, VecLen);
            for (int i = 0; i < 1; i++)
            {
                if (Avx.IsSupported)
                {
                    Vector128<float> x1, x2, x3, x4;
                    Vector128<float> x5, x6, x7;

                    memSpan.Fill (25);
                    x1 = Avx.LoadVector128 (mem);
                    x2 = Avx.LoadVector128 (mem);
                    x3 = Avx.LoadVector128 (mem);
                    x4 = Avx.LoadVector128 (mem);

                    x5 = Avx.Add (x1, x2);
                    x6 = Avx.Add (x3, x4);
                    x7 = Avx.Add (x5, x6);

                    Avx.Store (mem, x7);
                    WriteArray (mem, VecLen);
                }
                else if (AdvSimd.IsSupported)
                {
                    Vector128<float> x1, x2, x3, x4;
                    Vector128<float> x5, x6, x7;

                    memSpan.Fill (25);
                    x1 = AdvSimd.LoadVector128 (mem);
                    x2 = AdvSimd.LoadVector128 (mem);
                    x3 = AdvSimd.LoadVector128 (mem);
                    x4 = AdvSimd.LoadVector128 (mem);

                    x5 = AdvSimd.Add (x1, x2);
                    x6 = AdvSimd.Add (x3, x4);
                    x7 = AdvSimd.Add (x5, x6);

                    AdvSimd.Store (mem, x7);
                    WriteArray (mem, VecLen);
                }
                else
                {
                    Console.WriteLine("Hardware Intrinsics not supported");
                    return true;
                }
            }

            if (mem[0] != 100.00)
            {
                Console.WriteLine("XMM_CanCSE Test Failed");
                return false;
            }
            return true;
        }

        unsafe static bool TestYmm_NoCSE()
        {
            const int VecLen = 8;

            int result = -1;
            var mem = stackalloc float [VecLen];
            var memSpan = new Span<float> (mem, VecLen);
            for (int i = 0; i < 1; i++)
            {
                if (Avx.IsSupported)
                {
                    Vector256<float> x1, x2, x3;

                    memSpan.Fill (25);
                    x1 = Avx.LoadVector256 (mem);

                    memSpan.Fill (75);
                    x2 = Avx.LoadVector256 (mem);
                    
                    x3 = Avx.Add (x1, x2);

                    Avx.Store (mem, x3);
                    WriteArray (mem, VecLen);
                }
                else if (AdvSimd.IsSupported)
                {
                    Console.WriteLine("Vector256 not supported");
                    return true;
                }
                else
                {
                    Console.WriteLine("Hardware Intrinsics not supported");
                    return true;
                }
            }

            if (mem[0] != 100.00)
            {
                Console.WriteLine("YMM_NoCSE Test Failed");
                return false;
            }
            return true;
        }

        unsafe static bool TestYmm_CanCSE()
        {
            const int VecLen = 8;

            int result = -1;
            var mem = stackalloc float [VecLen];
            var memSpan = new Span<float> (mem, VecLen);
            for (int i = 0; i < 1; i++)
            {
                if (Avx.IsSupported)
                {
                    Vector256<float> x1, x2, x3, x4;
                    Vector256<float> x5, x6, x7;

                    memSpan.Fill (25);
                    x1 = Avx.LoadVector256 (mem);
                    x2 = Avx.LoadVector256 (mem);
                    x3 = Avx.LoadVector256 (mem);
                    x4 = Avx.LoadVector256 (mem);

                    x5 = Avx.Add (x1, x2);
                    x6 = Avx.Add (x3, x4);
                    x7 = Avx.Add (x5, x6);

                    Avx.Store (mem, x7);
                    WriteArray (mem, VecLen);
                }
                else if (AdvSimd.IsSupported)
                {
                    Console.WriteLine("Vector256 not supported");
                    return true;
                }
                else
                {
                    Console.WriteLine("Hardware Intrinsics not supported");
                    return true;
                }
            }

            if (mem[0] != 100.00)
            {
                Console.WriteLine("YMM_CanCSE Test Failed");
                return false;
            }
            return true;
        }

	[Fact]
        public static int TestEntryPoint()
        {
            bool result = true;
            result &= TestXmm_NoCSE();
            result &= TestYmm_NoCSE();
            result &= TestXmm_CanCSE();
            result &= TestYmm_CanCSE();

            if (result == true)
            {
                return 100;
            }
            else
            {
                return -1;
            }
        }
    }
}
