// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include "asansupport.h"

#ifndef HOST_WINDOWS
#define WEAK_SYMBOL __attribute__((weak))
#define HOST_SYMBOL __attribute__((weak))
#define HOST_SYMBOL_CALLCONV 
#else
#include <windows.h>
#define WEAK_SYMBOL 
#define HOST_SYMBOL __declspec(dllexport)
#define HOST_SYMBOL_CALLCONV __cdecl
#endif

#ifdef ASAN_SUPPORT_EXPOSE_SHADOW
// Specify use_sigaltstack=0 as coreclr uses own alternate stack for signal handlers
extern "C" const char *__asan_default_options() {
  return "symbolize=1 use_sigaltstack=0 detect_leaks=0";
}
namespace __asan
{
    extern uintptr_t kHighMemEnd, kMidMemBeg, kMidMemEnd;
}

extern "C" HOST_SYMBOL void HOST_SYMBOL_CALLCONV get_asan_shadow_range(uintptr_t* pHighMemEnd, uintptr_t* pMidMemBeg, uintptr_t* pMidMemEnd)
{
    *pHighMemEnd = __asan::kHighMemEnd;
    *pMidMemBeg = __asan::kMidMemBeg;
    *pMidMemEnd = __asan::kMidMemEnd;
}

namespace
{
    void initialize_asan_shadow_range()
    {
    }
}
#else

namespace __asan
{
    static uintptr_t kHighMemEnd, kMidMemBeg, kMidMemEnd;
}

#ifdef HOST_WINDOWS
namespace
{
    typedef void(*get_asan_shadow_range_ptr)(uintptr_t* pHighMemEnd, uintptr_t* pMidMemBeg, uintptr_t* pMidMemEnd);
    void get_asan_shadow_range(uintptr_t* pHighMemEnd, uintptr_t* pMidMemBeg, uintptr_t* pMidMemEnd)
    {
        static get_asan_shadow_range_ptr get_asan_shadow_range_func = nullptr;
        if (!get_asan_shadow_range_func)
        {
            HMODULE entryHandle = GetModuleHandleW(NULL);
            get_asan_shadow_range_func = (get_asan_shadow_range_ptr)GetProcAddress(entryHandle, "get_asan_shadow_range");
        }
        get_asan_shadow_range_func(pHighMemEnd, pMidMemBeg, pMidMemEnd);
    }
    void initialize_asan_shadow_range()
    {
        static bool initialized = false;
        if (initialized)
        {
            return;
        }
        get_asan_shadow_range(&__asan::kHighMemEnd, &__asan::kMidMemBeg, &__asan::kMidMemEnd);
        initialized = true;
    }
}
#else
extern "C" void __attribute__((weak)) get_asan_shadow_range(uintptr_t* pHighMemEnd, uintptr_t* pMidMemBeg, uintptr_t* pMidMemEnd);
namespace
{
    void initialize_asan_shadow_range()
    {
        static bool initialized = false;
        if (initialized)
        {
            return;
        }
        get_asan_shadow_range(&__asan::kHighMemEnd, &__asan::kMidMemBeg, &__asan::kMidMemEnd);
        initialized = true;
    }
}
#endif
#endif

// Ported from address sanitizer
extern "C"
{
    extern WEAK_SYMBOL uintptr_t __asan_shadow_memory_dynamic_address;
}

namespace __asan
{
    const uint32_t SHADOW_SCALE = 3;

#define SHADOW_OFFSET __asan_shadow_memory_dynamic_address

#define MEM_TO_SHADOW(mem) (((mem) >> SHADOW_SCALE) + (SHADOW_OFFSET))
#define kLowMemBeg      0
#define kLowMemEnd      (SHADOW_OFFSET ? SHADOW_OFFSET - 1 : 0)

#define kLowShadowBeg   SHADOW_OFFSET
#define kLowShadowEnd   MEM_TO_SHADOW(kLowMemEnd)

#define kHighMemBeg     (MEM_TO_SHADOW(kHighMemEnd) + 1)

#define kHighShadowBeg  MEM_TO_SHADOW(kHighMemBeg)
#define kHighShadowEnd  MEM_TO_SHADOW(kHighMemEnd)

# define kMidShadowBeg MEM_TO_SHADOW(kMidMemBeg)
# define kMidShadowEnd MEM_TO_SHADOW(kMidMemEnd)

// With the zero shadow base we can not actually map pages starting from 0.
// This constant is somewhat arbitrary.
#define kZeroBaseShadowStart 0
#define kZeroBaseMaxShadowStart (1 << 18)

#define kShadowGapBeg   (kLowShadowEnd ? kLowShadowEnd + 1 \
                                       : kZeroBaseShadowStart)
#define kShadowGapEnd   ((kMidMemBeg ? kMidShadowBeg : kHighShadowBeg) - 1)

#define kShadowGap2Beg (kMidMemBeg ? kMidShadowEnd + 1 : 0)
#define kShadowGap2End (kMidMemBeg ? kMidMemBeg - 1 : 0)

#define kShadowGap3Beg (kMidMemBeg ? kMidMemEnd + 1 : 0)
#define kShadowGap3End (kMidMemBeg ? kHighShadowBeg - 1 : 0)

    static inline bool AddrIsInLowMem(uintptr_t a) {
        return a <= kLowMemEnd;
    }

    static inline bool AddrIsInLowShadow(uintptr_t a) {
        return a >= kLowShadowBeg && a <= kLowShadowEnd;
    }

    static inline bool AddrIsInMidMem(uintptr_t a) {
        return kMidMemBeg && a >= kMidMemBeg && a <= kMidMemEnd;
    }

    static inline bool AddrIsInMidShadow(uintptr_t a) {
        return kMidMemBeg && a >= kMidShadowBeg && a <= kMidShadowEnd;
    }

    static inline bool AddrIsInHighMem(uintptr_t a) {
        return kHighMemBeg && a >= kHighMemBeg && a <= kHighMemEnd;
    }

    static inline bool AddrIsInHighShadow(uintptr_t a) {
        return kHighMemBeg && a >= kHighShadowBeg && a <= kHighShadowEnd;
    }

    static inline bool AddrIsInShadowGap(uintptr_t a) {
        if (kMidMemBeg) {
            if (a <= kShadowGapEnd)
                return SHADOW_OFFSET == 0 || a >= kShadowGapBeg;
            return (a >= kShadowGap2Beg && a <= kShadowGap2End) ||
                  (a >= kShadowGap3Beg && a <= kShadowGap3End);
        }
        // In zero-based shadow mode we treat addresses near zero as addresses
        // in shadow gap as well.
        if (SHADOW_OFFSET == 0)
            return a <= kShadowGapEnd;
        return a >= kShadowGapBeg && a <= kShadowGapEnd;
    }
}

bool WINAPI isAsanShadowAddress(void* address)
{
    initialize_asan_shadow_range();
    uintptr_t ptr = (uintptr_t)address;
    return __asan::AddrIsInLowShadow(ptr) || __asan::AddrIsInMidShadow(ptr) || __asan::AddrIsInHighShadow(ptr);
}
