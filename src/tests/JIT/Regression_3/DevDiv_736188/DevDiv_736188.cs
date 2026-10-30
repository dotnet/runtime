// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test exposed an issue in the prolog generation for arm64. The jit did not expect any holes in the mask
// of registers that needed to be zero initialized in the prolog. 
// The proj file sets 2 stress modes for JitStressRegs: 0x4 and 0x200.
// 0x4 forces Jit to choose registers starting from the end of the register window (like R27, R28);
// 0x200 forces different register windows for different blocks and creates resolution move that uses a temp int register (R19).
// It ends up with the mask {R19 | R27 | R28} that hits assert in `genSaveCalleeSavedRegistersHelp` that R19 and R27 must be paired.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace DevDiv_736188
{
    public class Program
    {
        private static object InternalSyncObject = new object();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T CallWith3Args<T>(ref T field, ref object syncObject, Func<T> initializer) => default;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T CallWith2Args<T>(ref T field, Func<T> initializer) where T : class =>
    CallWith3Args(ref field, ref InternalSyncObject, initializer);

        [Fact]
        public static void TestEntryPoint()
        {
            var args = new string[0];
            CallWith2Args(ref args, null);
        }
    }
}
