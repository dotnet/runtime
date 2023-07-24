// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub_19910
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SwapNonGeneric(ref Vector128<uint> a, ref Vector128<uint> b)
        {
            Vector128<uint> tmp = a; a = b; b = tmp;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int TestEntryPoint()
        {
            if (Sse2.IsSupported)
            {
                Vector128<uint> a = Sse2.ConvertScalarToVector128UInt32(0xA);
                Vector128<uint> b = Sse2.ConvertScalarToVector128UInt32(0xB);

                Vector128<uint> tmp = a; a = b; b = tmp;    // in-place version
                SwapNonGeneric(ref a, ref b);               // inlined version

                if ((Sse2.ConvertToUInt32(a) != 0xA) || (Sse2.ConvertToUInt32(b) != 0xB))
                {
                    Console.WriteLine("A={0}, B={1}", Sse2.ConvertToUInt32(a), Sse2.ConvertToUInt32(b));
                    return -1;
                }
            }
            return 100;
        }
    }
}
