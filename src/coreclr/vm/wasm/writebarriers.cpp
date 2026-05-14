// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>
#include "..\gchelpers.inl"
#include "gcheaputilities.h"

#define ASM_HELPER_2(rettype, funcname, a1, a2) \
    EXTERN_C rettype __attribute__((naked)) F_CALL_CONV funcname(a1, a2)
#define GC_ASM(text) \
    asm(text \
        :: [g_lowest_address] "i" (&g_lowest_address), \
        [g_highest_address] "i" (&g_highest_address), \
        [g_ephemeral_low] "i" (&g_ephemeral_low), \
        [g_ephemeral_high] "i" (&g_ephemeral_high), \
        [g_card_table] "i" (&g_card_table), \
        [card_byte_shift] "i" (card_byte_shift) \
    )

EXTERN_C FCDECL2_RAW(VOID, JIT_WriteBarrier, Object **dst, Object *ref);
ASM_HELPER_2(VOID, JIT_WriteBarrier, Object **dst, Object *ref)
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
        /* ref = ref >> card_byte_shift */
        "local.get 1\n"
        "i32.const %[card_byte_shift]\n"
        "i32.shr_u\n"
        "local.tee 1\n"
        /* if (g_card_table[ref] == 255) return */
        "i32.load8_u %[g_card_table]\n"
        "i32.const 255\n"
        "i32.eq\n"
        "if\n return\n end_if\n"
        /* g_card_table[ref] = 255 */
        "local.get 1\n"
        "i32.const 255\n"
        "i32.store8 %[g_card_table]\n"
        "return\n"
    );
}

EXTERN_C FCDECL2_RAW(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref);
ASM_HELPER_2(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref)
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
        /* ref = ref >> card_byte_shift */
        "local.get 1\n"
        "i32.const %[card_byte_shift]\n"
        "i32.shr_u\n"
        "local.tee 1\n"
        /* if (g_card_table[ref] == 255) return */
        "i32.load8_u %[g_card_table]\n"
        "i32.const 255\n"
        "i32.eq\n"
        "if\n return\n end_if\n"
        /* g_card_table[ref] = 255 */
        "local.get 1\n"
        "i32.const 255\n"
        "i32.store8 %[g_card_table]\n"
        "return\n"
    );
}

EXTERN_C FCDECL2_RAW(VOID, RhpAssignRef, Object **dst, Object *ref);
ASM_HELPER_2(VOID, RhpAssignRef, Object **dst, Object *ref)
__attribute__((alias("JIT_WriteBarrier")));

EXTERN_C FCDECL2_RAW(VOID, RhpCheckedAssignRef, Object **dst, Object *ref);
ASM_HELPER_2(VOID, RhpCheckedAssignRef, Object **dst, Object *ref)
__attribute__((alias("JIT_CheckedWriteBarrier")));

EXTERN_C FCDECL2_RAW(VOID, RhpByRefAssignRef, Object **dst, Object **ref);
ASM_HELPER_2(VOID, RhpByRefAssignRef, Object **dst, Object **ref)
{
    GC_ASM("unreachable"
    );
}
