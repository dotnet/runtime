// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************************/

#ifndef _ERROR_H_
#define _ERROR_H_
/*****************************************************************************/

#include <corjit.h>   // for CORJIT_INTERNALERROR
#include <safemath.h> // For FitsIn, used by SafeCvt methods.

#define FATAL_JIT_EXCEPTION 0x02345678
class Compiler;

struct ErrorTrapParam
{
    int                errc;
    ICorJitInfo*       jitInfo;
    EXCEPTION_POINTERS exceptionPointers;
    ErrorTrapParam()
    {
        jitInfo = nullptr;
    }
};

// Only catch JIT internal errors (will not catch EE generated Errors)
extern LONG __JITfilter(PEXCEPTION_POINTERS pExceptionPointers, LPVOID lpvParam);

#define setErrorTrap(compHnd, ParamType, paramDef, paramRef)                                                           \
    struct __JITParam : ErrorTrapParam                                                                                 \
    {                                                                                                                  \
        ParamType param;                                                                                               \
    } __JITparam;                                                                                                      \
    __JITparam.errc    = CORJIT_INTERNALERROR;                                                                         \
    __JITparam.jitInfo = compHnd;                                                                                      \
    __JITparam.param   = paramRef;                                                                                     \
    PAL_TRY(__JITParam*, __JITpParam, &__JITparam)                                                                     \
    {                                                                                                                  \
        ParamType paramDef = __JITpParam->param;

// Only catch JIT internal errors (will not catch EE generated Errors)
#define impJitErrorTrap()                                                                                              \
    }                                                                                                                  \
    PAL_EXCEPT_FILTER(__JITfilter)                                                                                     \
    {                                                                                                                  \
        int __errc = __JITparam.errc;                                                                                  \
        (void)__errc;

#define endErrorTrap()                                                                                                 \
    }                                                                                                                  \
    PAL_ENDTRY

#define finallyErrorTrap()                                                                                             \
    }                                                                                                                  \
    PAL_FINALLY                                                                                                        \
    {

/*****************************************************************************/

// clang-format off

extern void debugError(const char* msg, const char* file, unsigned line);
extern void DECLSPEC_NORETURN badCode();
extern void DECLSPEC_NORETURN badCode3(const char* msg, const char* msg2, int arg, _In_z_ const char* file, unsigned line);
extern void DECLSPEC_NORETURN noWay();
extern void DECLSPEC_NORETURN implLimitation();
extern void DECLSPEC_NORETURN NOMEM();
extern void DECLSPEC_NORETURN fatal(int errCode);

extern void DECLSPEC_NORETURN noWayAssertBody();
extern void DECLSPEC_NORETURN noWayAssertBody(const char* cond, const char* file, unsigned line);

// Conditionally invoke the noway assert body. The conditional predicate is evaluated using a method on the tlsCompiler.
// If a noway_assert is hit, we ask the Compiler whether to raise an exception (i.e., conditionally raise exception.)
// To have backward compatibility between v4.5 and v4.0, in min-opts we take a shot at codegen rather than rethrow.
extern void ANALYZER_NORETURN noWayAssertBodyConditional();

extern void ANALYZER_NORETURN noWayAssertBodyConditional(const char* cond, const char* file, unsigned line);

// Define MEASURE_NOWAY to 1 to enable code to count and rank individual noway_assert calls by occurrence.
// These asserts would be dynamically executed, but not necessarily fail. The provides some insight into
// the dynamic prevalence of these (if not a direct measure of their cost), which exist in non-DEBUG as
// well as DEBUG builds.
#ifdef DEBUG
#define MEASURE_NOWAY 1
#else // !DEBUG
#define MEASURE_NOWAY 0
#endif // !DEBUG

#if MEASURE_NOWAY
extern void RecordNowayAssertGlobal(const char* filename, unsigned line, const char* condStr);
#define RECORD_NOWAY_ASSERT(condStr) RecordNowayAssertGlobal(__FILE__, __LINE__, condStr);
#else
#define RECORD_NOWAY_ASSERT(condStr)
#endif

#ifdef DEBUG

#define BADCODE(msg) (debugError(msg, __FILE__, __LINE__), badCode())
#define BADCODE3(msg, msg2, arg) badCode3(msg, msg2, arg, __FILE__, __LINE__)
// Used for an assert that we want to convert into BADCODE to force minopts, or in minopts to force codegen.
#define noway_assert(cond)                                                                                             \
    do                                                                                                                 \
    {                                                                                                                  \
        RECORD_NOWAY_ASSERT(#cond)                                                                                     \
        if (!(cond))                                                                                                   \
        {                                                                                                              \
            noWayAssertBodyConditional(#cond, __FILE__, __LINE__);                                                     \
        }                                                                                                              \
    } while (0)
#define unreached() noWayAssertBody("unreached", __FILE__, __LINE__)
#define NO_WAY(msg) noWayAssertBody(msg, __FILE__, __LINE__)
// Used for fallback stress mode
#define NO_WAY_NOASSERT(msg) noWay()
#define NOWAY_MSG(msg) noWayAssertBodyConditional(msg, __FILE__, __LINE__)
#define NOWAY_MSG_FILE_AND_LINE(msg, file, line) noWayAssertBodyConditional(msg, file, line)

// IMPL_LIMITATION is called when we encounter valid IL that is not
// supported by our current implementation because of various
// limitations (that could be removed in the future)
#define IMPL_LIMITATION(msg) (debugError(msg, __FILE__, __LINE__), implLimitation())

#else // !DEBUG

#define NO_WAY(msg) noWay()
#define BADCODE(msg) badCode()
#define BADCODE3(msg, msg2, arg) badCode()

// IMPL_LIMITATION is called when we encounter valid IL that is not
// supported by our current implementation because of various
// limitations (that could be removed in the future)
#define IMPL_LIMITATION(msg) implLimitation()

#define noway_assert(cond)                                                                                             \
    do                                                                                                                 \
    {                                                                                                                  \
        RECORD_NOWAY_ASSERT(#cond)                                                                                     \
        if (!(cond))                                                                                                   \
        {                                                                                                              \
            noWayAssertBodyConditional();                                                                              \
        }                                                                                                              \
    } while (0)
#define unreached() noWayAssertBody()

#define NOWAY_MSG(msg) noWayAssertBodyConditional()
#define NOWAY_MSG_FILE_AND_LINE(msg, file, line) noWayAssertBodyConditional()

#endif // !DEBUG


#if 1 // All platforms currently enable NYI; this should be a tighter condition to exclude some platforms from NYI

// This can return based on Config flag/Debugger
extern void notYetImplemented(const char* msg, const char* file, unsigned line);
#define NYIRAW(msg) notYetImplemented(msg, __FILE__, __LINE__)

#define NYI(msg)                    NYIRAW("NYI: " msg)
#define NYI_IF(cond, msg) if (cond) NYIRAW("NYI: " msg)

#ifdef TARGET_AMD64

#define NYI_AMD64(msg)  NYIRAW("NYI_AMD64: " msg)
#define NYI_X86(msg)    do { } while (0)
#define NYI_ARM(msg)    do { } while (0)
#define NYI_ARM64(msg)  do { } while (0)
#define NYI_LOONGARCH64(msg) do { } while (0)
#define NYI_RISCV64(msg) do { } while (0)

#elif defined(TARGET_X86)

#define NYI_AMD64(msg)  do { } while (0)
#define NYI_X86(msg)    NYIRAW("NYI_X86: " msg)
#define NYI_ARM(msg)    do { } while (0)
#define NYI_ARM64(msg)  do { } while (0)
#define NYI_LOONGARCH64(msg) do { } while (0)
#define NYI_RISCV64(msg) do { } while (0)

#elif defined(TARGET_ARM)

#define NYI_AMD64(msg)  do { } while (0)
#define NYI_X86(msg)    do { } while (0)
#define NYI_ARM(msg)    NYIRAW("NYI_ARM: " msg)
#define NYI_ARM64(msg)  do { } while (0)
#define NYI_LOONGARCH64(msg) do { } while (0)
#define NYI_RISCV64(msg) do { } while (0)

#elif defined(TARGET_ARM64)

#define NYI_AMD64(msg)  do { } while (0)
#define NYI_X86(msg)    do { } while (0)
#define NYI_ARM(msg)    do { } while (0)
#define NYI_ARM64(msg)  NYIRAW("NYI_ARM64: " msg)
#define NYI_LOONGARCH64(msg) do { } while (0)
#define NYI_RISCV64(msg) do { } while (0)

#elif defined(TARGET_LOONGARCH64)
#define NYI_AMD64(msg)  do { } while (0)
#define NYI_X86(msg)    do { } while (0)
#define NYI_ARM(msg)    do { } while (0)
#define NYI_ARM64(msg)  do { } while (0)
#define NYI_LOONGARCH64(msg) NYIRAW("NYI_LOONGARCH64: " msg)
#define NYI_RISCV64(msg) do { } while (0)

#elif defined(TARGET_RISCV64)
#define NYI_AMD64(msg)  do { } while (0)
#define NYI_X86(msg)    do { } while (0)
#define NYI_ARM(msg)    do { } while (0)
#define NYI_ARM64(msg)  do { } while (0)
#define NYI_LOONGARCH64(msg) do { } while (0)
#define NYI_RISCV64(msg) NYIRAW("NYI_RISCV64: " msg)

#else

#error "Unknown platform, not x86, ARM, LOONGARCH64, AMD64, or RISCV64?"

#endif

#else // NYI not available; make it an assert.

#define NYI(msg)        assert(!(msg))
#define NYI_AMD64(msg)  do { } while (0)
#define NYI_ARM(msg)    do { } while (0)
#define NYI_ARM64(msg)  do { } while (0)

#endif // NYI not available

// clang-format on

#if defined(HOST_X86) && !defined(HOST_UNIX)

// While debugging in an Debugger, the "int 3" will cause the program to break
// Outside, the exception handler will just filter out the "int 3".

#define BreakIfDebuggerPresent()                                                                                       \
    do                                                                                                                 \
    {                                                                                                                  \
        __try                                                                                                          \
        {                                                                                                              \
            __asm {int 3}                                                                                              \
        }                                                                                                              \
        __except (EXCEPTION_EXECUTE_HANDLER)                                                                           \
        {                                                                                                              \
        }                                                                                                              \
    } while (0)

#else
#define BreakIfDebuggerPresent()                                                                                       \
    do                                                                                                                 \
    {                                                                                                                  \
        if (IsDebuggerPresent())                                                                                       \
            DebugBreak();                                                                                              \
    } while (0)
#endif

#ifdef DEBUG
DWORD getBreakOnBadCode();
#endif

// For narrowing numeric conversions, the following two methods ensure that the
// source value fits in the destination type, using either "assert" or
// "noway_assert" to validate the conversion.  Obviously, each returns the source value as
// the destination type.

// (There is an argument that these should be macros, to let the preprocessor capture
// a more useful file/line for the error message.  But then we have to use comma expressions
// so that these can be used in expressions, etc., which is ugly.  So I propose we rely on
// getting stack traces in other ways.)
template <typename Dst, typename Src>
inline Dst SafeCvtAssert(Src val)
{
    assert(FitsIn<Dst>(val));
    return static_cast<Dst>(val);
}

template <typename Dst, typename Src>
inline Dst SafeCvtNowayAssert(Src val)
{
    noway_assert(FitsIn<Dst>(val));
    return static_cast<Dst>(val);
}

#endif
