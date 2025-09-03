// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._AvxVnniInt8
{
    public partial class Program
    {
        const float EPS = Single.Epsilon * 5;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Vector128<ulong> getAbs128(Vector128<long> val)
        {
            return Avx10v2.Abs(val);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Vector256<ulong> getAbs256(Vector256<long> val)
        {
            return Avx10v2.Abs(val);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Vector128<uint> getMWA(Vector128<uint> v1, Vector128<byte> v2, Vector128<byte> v3)
        {
            return AvxVnniInt8.MultiplyWideningAndAdd(v1, v2, v3);
        }

        [Fact]
        public static unsafe void AvxVnniInt8SampleTest ()
        {
            Console.WriteLine("Test executed");
            if (AvxVnniInt8.IsSupported)
            {
                Console.WriteLine("AvxVnniInt8 supported");
            }
            else {
                Console.WriteLine("AvxVnniInt8 not supported");
            }
            if (AvxVnniInt8.V512.IsSupported)
            {
                Console.WriteLine("AvxVnniInt8_V512 supported");
            }
            else {
                Console.WriteLine("AvxVnniInt8_V512 not supported");
            }
        }
    }
}
