#ifndef X86_BUILTIN_CTZ_H
#define X86_BUILTIN_CTZ_H

#if defined(_MSC_VER) && !defined(__clang__)
#if defined(_M_IX86) || defined(_M_AMD64) || defined(_M_IA64) ||  defined(_M_ARM) || defined(_M_ARM64)

#include <intrin.h>
#ifdef X86_FEATURES
#  include "arch/x86/x86.h"
#endif

/* This is not a general purpose replacement for __builtin_ctz. The function expects that value is != 0
 * Because of that assumption trailing_zero is not initialized and the return value of _BitScanForward is not checked
 */
static __forceinline unsigned long __builtin_ctz(uint32_t value) {
#ifdef X86_FEATURES
    if (x86_cpu_has_tzcnt)
        return _tzcnt_u32(value);
#endif
    unsigned long trailing_zero;
    _BitScanForward(&trailing_zero, value);
    return trailing_zero;
}
#define HAVE_BUILTIN_CTZ

#ifdef _M_AMD64
/* This is not a general purpose replacement for __builtin_ctzll. The function expects that value is != 0
 * Because of that assumption trailing_zero is not initialized and the return value of _BitScanForward64 is not checked
 */
static __forceinline unsigned long long __builtin_ctzll(uint64_t value) {
#ifdef X86_FEATURES
    if (x86_cpu_has_tzcnt)
        return _tzcnt_u64(value);
#endif
    unsigned long trailing_zero;
    _BitScanForward64(&trailing_zero, value);
    return trailing_zero;
}
#define HAVE_BUILTIN_CTZLL
#endif

#endif
#endif
#endif
