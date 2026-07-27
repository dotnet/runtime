// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>

#ifndef FEATURE_NATIVEAOT
#include "gchelpers.inl"
#include "gcheaputilities.h"
#endif

#ifdef FEATURE_MULTITHREADING
#error The current assembly implementation of write barriers assumes single-threaded Wasm
#endif

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#error The current assembly implementation of write barriers does not implement card bundles
#endif

// To simplify integration with the rest of the codebase and avoid complicating the build
// system, Wasm write barriers are implemented using inline assembly inside C functions, below.
// These write barriers use the Wasm-specific FCDECL2_RAW for a native calling convention instead
// of a managed calling convention (to omit the sp and pep parameters). This reduces code size
// considerably and simplifies the JIT. In order to make it safe to call these write barriers from
// inside of a managed function without manually updating the __stack_pointer global, the barriers
// *must* be implemented without use of the linear memory stack, and the best way to guarantee that
// is to implement the barriers by hand in assembly.

#define ASM_HELPER_2(rettype, funcname, a1, a2) \
    EXTERN_C rettype __attribute__((naked)) F_CALL_CONV funcname(a1, a2)

// Helper to make relevant GC globals and constants visible inside of inline assembly
#define GC_ASM(text) \
    asm(text \
        :: [g_lowest_address] "i" (&g_lowest_address), \
        [g_highest_address] "i" (&g_highest_address), \
        [g_ephemeral_low] "i" (&g_ephemeral_low), \
        [g_ephemeral_high] "i" (&g_ephemeral_high), \
        [g_card_table] "i" (&g_card_table), \
        [card_byte_shift] "i" (card_byte_shift) \
    )

EXTERN_C FCDECL2_RAW(VOID, RhpAssignRef, Object **dst, Object *ref);
ASM_HELPER_2(VOID, RhpAssignRef, Object **dst, Object *ref)
{
    GC_ASM(
        /* *dst = ref */
        "local.get 0\n"
        "local.get 1\n"
        "i32.store 0\n"
        /* if ((ref < ephemeral_low) || (ref >= ephemeral_high)) return */
        "local.get 1\n"
        "i32.const 0\n i32.load %[g_ephemeral_low]\n"
        "i32.lt_u\n"
        "local.get 1\n"
        "i32.const 0\n i32.load %[g_ephemeral_high]\n"
        "i32.ge_u\n"
        "i32.or\n"
        "if\n return\n end_if\n"
        /* dst = &g_card_table[(dst >> card_byte_shift)] */
        "local.get 0\n"
        "i32.const %[card_byte_shift]\n"
        "i32.shr_u\n"
        "i32.const 0\n"
        "i32.load %[g_card_table]\n"
        "i32.add\n"
        "local.tee 0\n"
        /* if (*dst == 255) return */
        "i32.load8_u 0\n"
        "i32.const 255\n"
        "i32.eq\n"
        "if\n return\n end_if\n"
        /* *dst = 255 */
        "local.get 0\n"
        "i32.const 255\n"
        "i32.store8 0\n"
        "return\n"
    );
}

EXTERN_C FCDECL2_RAW(VOID, RhpCheckedAssignRef, Object **dst, Object *ref);
ASM_HELPER_2(VOID, RhpCheckedAssignRef, Object **dst, Object *ref)
{
    GC_ASM(
        /* *dst = ref */
        "local.get 0\n"
        "local.get 1\n"
        "i32.store 0\n"
        /* if ((dst < lowest) || (dst >= highest)) return */
        "local.get 0\n"
        "i32.const 0\n i32.load %[g_lowest_address]\n"
        "i32.lt_u\n"
        "local.get 0\n"
        "i32.const 0\n i32.load %[g_highest_address]\n"
        "i32.ge_u\n"
        "i32.or\n"
        "if\n return\n end_if\n"
        /* if ((ref < ephemeral_low) || (ref >= ephemeral_high)) return */
        "local.get 1\n"
        "i32.const 0\n i32.load %[g_ephemeral_low]\n"
        "i32.lt_u\n"
        "local.get 1\n"
        "i32.const 0\n i32.load %[g_ephemeral_high]\n"
        "i32.ge_u\n"
        "i32.or\n"
        "if\n return\n end_if\n"
        /* dst = &g_card_table[(dst >> card_byte_shift)] */
        "local.get 0\n"
        "i32.const %[card_byte_shift]\n"
        "i32.shr_u\n"
        "i32.const 0\n"
        "i32.load %[g_card_table]\n"
        "i32.add\n"
        "local.tee 0\n"
        /* if (*dst == 255) return */
        "i32.load8_u 0\n"
        "i32.const 255\n"
        "i32.eq\n"
        "if\n return\n end_if\n"
        /* *dst = 255 */
        "local.get 0\n"
        "i32.const 255\n"
        "i32.store8 0\n"
        "return\n"
    );
}
