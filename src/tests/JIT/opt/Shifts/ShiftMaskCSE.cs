// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace ShiftMaskCSE
{
    // Verify that the redundant AND mask on shift amounts (emitted by Roslyn)
    // does not interfere with CSE. When two shifts share the same variable
    // shift amount, CSE can hoist the (shift & 31) expression. Stripping the
    // mask early in the importer prevents the AND from being CSE'd, avoiding
    // the extra AND instruction at runtime.
    public class ShiftMaskCSETests
    {
        [Fact]
        public static int TestEntryPoint()
        {
            bool fail = false;

            // 32-bit unsigned right shift then left shift
            if (ShiftAndCSE(0xDEADBEEF, 16) != 0xDEAD0000)
            {
                Console.WriteLine($"ShiftAndCSE failed: expected 0xDEAD0000, got 0x{ShiftAndCSE(0xDEADBEEF, 16):X}");
                fail = true;
            }

            if (ShiftAndCSE(0x12345678, 0) != 0x12345678)
            {
                Console.WriteLine($"ShiftAndCSE with shift=0 failed");
                fail = true;
            }

            if (ShiftAndCSE(0x12345678, 31) != 0)
            {
                Console.WriteLine($"ShiftAndCSE with shift=31 failed");
                fail = true;
            }

            // 32-bit signed left shift then right shift
            if (ShiftAndCSESigned(0x12345678, 8) != 0x00345678)
            {
                Console.WriteLine($"ShiftAndCSESigned failed: expected 0x345678, got 0x{ShiftAndCSESigned(0x12345678, 8):X}");
                fail = true;
            }

            // 64-bit unsigned right shift then left shift
            if (ShiftAndCSE64(0xDEADBEEFCAFEBABE, 32) != 0xDEADBEEF00000000)
            {
                Console.WriteLine($"ShiftAndCSE64 failed: expected 0xDEADBEEF00000000, got 0x{ShiftAndCSE64(0xDEADBEEFCAFEBABE, 32):X}");
                fail = true;
            }

            // Three shifts sharing the same amount
            if (TripleShift(0xFFFF0000, 8) != 0)
            {
                Console.WriteLine($"TripleShift failed: expected 0x0, got 0x{TripleShift(0xFFFF0000, 8):X}");
                fail = true;
            }

            return fail ? -1 : 100;
        }

        // Pattern from the issue: the two shifts share the same variable shift amount.
        // Roslyn emits (shift & 31) for both, which CSE can hoist.
        // With early mask stripping, no AND instruction should be generated.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ShiftAndCSE(uint foo, int shift)
        {
            uint res = (foo >> shift);
            res <<= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ShiftAndCSESigned(int foo, int shift)
        {
            int res = (foo << shift);
            res >>= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ShiftAndCSE64(ulong foo, int shift)
        {
            ulong res = (foo >> shift);
            res <<= shift;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint TripleShift(uint foo, int shift)
        {
            uint a = foo >> shift;
            uint b = foo << shift;
            uint c = a & b;
            c >>= shift;
            return c;
        }
    }
}
