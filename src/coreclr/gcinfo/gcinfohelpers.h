#ifndef _GCINFOHELPERS_H_
#define _GCINFOHELPERS_H_

#if defined(_DEBUG)
// NOTE: This needs to actually do something with expr or crossgen will crash in safemath.h
//  because safemath verifies that you've actually performed overflow checks and there's a load-bearing assert somewhere in GcInfoEncoder
// It is not possible to use libc assert here without linker errors.
static bool __gcinfo_assert_hack_global = false;
#ifdef _MSC_VER
#define GCINFO_ASSERT(expr) if (!(__gcinfo_assert_hack_global = (expr))) { \
    __debugbreak(); \
}
#else // _MSC_VER
#define GCINFO_ASSERT(expr) if (!(__gcinfo_assert_hack_global = (expr))) { \
    __builtin_trap(); \
}
#endif // _MSC_VER
#else // _DEBUG
#define GCINFO_ASSERT(expr) (void)0
#endif // _DEBUG

// If you want GcInfoEncoder logging to work, replace this macro with an appropriate definition.
// This previously relied on our common logging infrastructure, but that caused linker failures in the interpreter.
// Example implementation:
// #define GCINFO_LOG(level, format, ...) (printf(format, __VA_ARGS__), true)
#define GCINFO_LOG(level, format, ...) false

// If you want to enable GcInfoSize::Log to work, replace this macro with an appropriate definition.
#define GCINFO_LOGSPEW(level, format, ...) false

// Duplicated from log.h
// NOTE: ICorJitInfo::logMsg appears to accept these same levels and is accessible from GcInfoEncoder.
#define LL_EVERYTHING  10
#define LL_INFO1000000  9       // can be expected to generate 1,000,000 logs per small but not trivial run
#define LL_INFO100000   8       // can be expected to generate 100,000 logs per small but not trivial run
#define LL_INFO10000    7       // can be expected to generate 10,000 logs per small but not trivial run
#define LL_INFO1000     6       // can be expected to generate 1,000 logs per small but not trivial run
#define LL_INFO100      5       // can be expected to generate 100 logs per small but not trivial run
#define LL_INFO10       4       // can be expected to generate 10 logs per small but not trivial run
#define LL_WARNING      3
#define LL_ERROR        2
#define LL_FATALERROR   1
#define LL_ALWAYS   	0		// impossible to turn off (log level never negative)

// Needed by bitposition.h
#define _BITPOSITION_ASSERTE(x) GCINFO_ASSERT(x)

#endif // _GCINFOHELPERS_H_
