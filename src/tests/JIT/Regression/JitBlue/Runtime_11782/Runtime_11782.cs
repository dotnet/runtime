// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

namespace Runtime_11782
{

    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe uint mulx(uint a, uint b)
        {
            uint r;
            return Bmi2.MultiplyNoFlags(a, b, &r) + r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ulong mulx_64(ulong a, ulong b)
        {
            ulong r;
            return Bmi2.X64.MultiplyNoFlags(a, b, &r) + r;
        }

        static int Main()
        {
            int returnVal = 100;

            if (Bmi2.IsSupported)
            {
                uint u1 = mulx(0xf0000000, 0x10);
                if (u1 != 0xf)
                {
                    Console.WriteLine("32-bit mulx failed: u1 = 0x{0:X}", u1 );
                    returnVal = -1;
                }
                if (Bmi2.X64.IsSupported)
                {
                    ulong ul1 = mulx_64(0xf00000000, 0x10);
                    if (ul1 != 0xf000000000UL)
                    {
                        Console.WriteLine("64-bit mulx failed: ul1 = 0x{0:X}", ul1);
                        returnVal = -1;
                    }
                }
            }
            return returnVal;
        }
    }
}
