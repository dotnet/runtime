// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    //
    // The TransitionBlock that RhpInterfaceDispatchSlow passes to RhpCidResolve. Except on x86 and WASM, it is
    // created by PROLOG_WITH_TRANSITION_BLOCK and has the same layout as the TransitionBlock in CoreCLR's
    // callingconvention.h.
    //
    internal static class TransitionBlock
    {
        // Offset of the 'this' argument from the TransitionBlock pointer
        public static int GetThisOffset()
        {
#if TARGET_AMD64 && TARGET_UNIX
            // rdi is the first of the argument registers at the base of the TransitionBlock
            return 0;
#elif TARGET_AMD64
            // rcx is homed above the callee-saved registers (rdi, rsi, rbx, rbp, r12-r15) and the return address
            return 9 * 8;
#elif TARGET_ARM64
            // x0 follows the callee-saved registers (fp, lr, x19-x28), the padding and x8
            return 14 * 8;
#elif TARGET_ARM
            // r0 follows the callee-saved registers (r4-r11) and lr
            return 9 * 4;
#elif TARGET_LOONGARCH64
            // a0 follows the callee-saved registers (fp, ra, s0-s8) and the padding
            return 12 * 8;
#elif TARGET_RISCV64
            // a0 follows the callee-saved registers (fp, ra, s1-s11, tp, gp) and the padding
            return 16 * 8;
#elif TARGET_X86
            // ecx follows edx at the base of the transition frame
            return 4;
#elif TARGET_WASM
            return 4;
#else
#error Unknown architecture!
#endif
        }
    }
}
