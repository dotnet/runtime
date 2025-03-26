#ifndef _GCINFOHELPERS_H_
#define _GCINFOHELPERS_H_

// NOTE: This needs to actually do something with expr or crossgen will crash in safemath.h
//  because safemath verifies that you've actually performed overflow checks.
#define GCINFO_ASSERT(expr) (__gcinfo_assert_hack_global = expr)

static bool __gcinfo_assert_hack_global = false;

// If you want to enable general GCINFO logging you'll need to replace this macro with an appropriate definition.
// This previously relied on our common logging infrastructure, but that caused linker failures in the interpreter.
#define GCINFO_LOG(arglist) (void)0;

// If you want to enable GcInfoSize::Log to work, replace these two macros with appropriate definitions.
// These previously relied on our common logging infrastructure, but that causes linker failures in the interpreter.
#define GCINFO_LOGSPEW(arglist) (void)0;
// TODO: Can we use ICorJitInfo::logMsg's return value to sense whether logging is enabled, and remove this macro?
#define GCINFO_LOGGINGON(level) false

// Duplicated from log.h
// ICorJitInfo::logMsg appears to accept these same levels
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

#endif // _GCINFOHELPERS_H_
