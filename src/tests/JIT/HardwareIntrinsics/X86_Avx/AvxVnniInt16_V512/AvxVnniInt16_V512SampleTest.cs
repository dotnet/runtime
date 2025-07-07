// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest._AvxVnniInt16_V512
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

        [Fact]
        public static unsafe void AvxVnniInt16_V512SampleTest ()
        {
            Console.WriteLine("Test executed");
            if (AvxVnniInt16.IsSupported)
            {
                Console.WriteLine("AvxVnniInt16 supported");
            }
            else {
                Console.WriteLine("AvxVnniInt16 not supported");
            }
            if (AvxVnniInt16.V512.IsSupported)
            {
                Console.WriteLine("AvxVnniInt16_V512 supported");
            }
            else {
                Console.WriteLine("AvxVnniInt16_V512 not supported");
            }
        }
    }
}