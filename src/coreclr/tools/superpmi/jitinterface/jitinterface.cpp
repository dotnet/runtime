// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The native side of the managed SuperPMI replay host. The JIT calls the ICorJitInfo methods of crossgen2's
// generated JitInterfaceWrapper, which forwards them to the managed implementation. Managed exceptions are
// rethrown through the JIT as C++ exceptions, and caught here. This also has the parts of the near differ and
// of the throughput measurements that need native code.

#include <stdarg.h>
#include <stdint.h>
#include <stdio.h>
#include <new>

#ifdef _MSC_VER
#include <excpt.h>
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE __attribute__((noinline))
#endif

#include "dllexport.h"
#include "jitfaultguard.h"
#include "jitinterface_generated.h"
#include "../../../inc/coredistools.h"

// The exception code for C++ exceptions, which other exceptions are reported with on non-Windows platforms.
static const uint32_t CppExceptionCode = 0xE06D7363;

static const WCHAR s_emptyMessage[1] = {};

// The kinds of exceptions thrown on behalf of the managed host, which decide the error traps that catch them.
enum ExceptionKind
{
    // An exception that the EE threw during recording: caught by runWithErrorTrap.
    EEException,
    // A SuperPMI exception, for example for data missing from the method context: caught by
    // runWithSPMIErrorTrap, and otherwise reported by the replay loop.
    SuperPmiException,
    // A failure of the host itself: always reported by the replay loop.
    HostFailure,
};

struct HostException
{
    CorInfoExceptionClass base;
    ExceptionKind kind;

    HostException(ExceptionKind kind)
        : base(s_emptyMessage, 0), kind(kind)
    {
    }
};

DLL_EXPORT CorInfoExceptionClass* AllocException(int kind)
{
    return &(new HostException((ExceptionKind)kind))->base;
}

DLL_EXPORT void FreeException(CorInfoExceptionClass* pException)
{
    delete reinterpret_cast<HostException*>(pException);
}

static ExceptionKind GetKind(CorInfoExceptionClass* pException)
{
    return reinterpret_cast<HostException*>(pException)->kind;
}

// Catches the exceptions other than SuperPMI exceptions and host failures.
static bool RunWithErrorTrapCpp(ICorJitInfo::errorTrapFunction function, void* parameter)
{
    try
    {
        (*function)(parameter);
    }
    catch (CorInfoExceptionClass* pException)
    {
        if (GetKind(pException) != EEException)
            throw;
        return false;
    }
    catch (...)
    {
        return false;
    }
    return true;
}

bool JitInterfaceWrapper::runWithErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter)
{
#ifdef _MSC_VER
    // Also catch the structured exceptions, such as access violations; the C++ ones are handled above.
    __try
    {
        return RunWithErrorTrapCpp(function, parameter);
    }
    __except (GetExceptionCode() == CppExceptionCode ? EXCEPTION_CONTINUE_SEARCH : EXCEPTION_EXECUTE_HANDLER)
    {
        return false;
    }
#else
    return RunWithErrorTrapCpp(function, parameter);
#endif
}

bool JitInterfaceWrapper::runWithSPMIErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter)
{
    try
    {
        (*function)(parameter);
    }
    catch (CorInfoExceptionClass* pException)
    {
        if (GetKind(pException) != SuperPmiException)
            throw;
        return false;
    }
    return true;
}

static int JitCompileMethodCpp(CorInfoExceptionClass** ppException, uint32_t* pExceptionCode, ICorJitCompiler* pJit, void* thisHandle,
    void** callbacks, CORINFO_METHOD_INFO* methodInfo, unsigned flags, uint8_t** entryAddress, uint32_t* nativeSizeOfCode)
{
    try
    {
        JitInterfaceWrapper jitInterfaceWrapper(thisHandle, callbacks);
        return pJit->compileMethod(&jitInterfaceWrapper, methodInfo, flags, entryAddress, nativeSizeOfCode);
    }
    catch (CorInfoExceptionClass* pException)
    {
        *ppException = pException;
    }
    catch (...)
    {
        *pExceptionCode = CppExceptionCode;
    }
    return CORJIT_INTERNALERROR;
}

// Compiles the method. An exception that escapes the JIT is returned in *ppException if the host threw it,
// and otherwise reported by its code in *pExceptionCode.
DLL_EXPORT int JitCompileMethod(CorInfoExceptionClass** ppException, uint32_t* pExceptionCode, ICorJitCompiler* pJit, void* thisHandle,
    void** callbacks, CORINFO_METHOD_INFO* methodInfo, unsigned flags, uint8_t** entryAddress, uint32_t* nativeSizeOfCode)
{
    *ppException = nullptr;
    *pExceptionCode = 0;

#ifdef _MSC_VER
    // Recover from JIT crashes, such as access violations.
    __try
    {
        return JitCompileMethodCpp(ppException, pExceptionCode, pJit, thisHandle, callbacks, methodInfo, flags, entryAddress, nativeSizeOfCode);
    }
    __except (*pExceptionCode = GetExceptionCode(), EXCEPTION_EXECUTE_HANDLER)
    {
        return CORJIT_INTERNALERROR;
    }
#elif defined(JIT_FAULT_GUARD)
    // Recover from JIT crashes, such as segmentation faults, which are reported with the codes of the corresponding
    // Windows exceptions.
    int result = CORJIT_INTERNALERROR;
    auto call = [&]() { result = JitCompileMethodCpp(ppException, pExceptionCode, pJit, thisHandle, callbacks, methodInfo, flags, entryAddress, nativeSizeOfCode); };
    int code = 0;
    int signal = JitFaultGuard::CallCatchingJitFaults(pJit, call, &code);
    if (signal != 0)
    {
        *pExceptionCode = JitFaultGuard::ExceptionCode(signal, code);
        return CORJIT_INTERNALERROR;
    }
    return result;
#else
    return JitCompileMethodCpp(ppException, pExceptionCode, pJit, thisHandle, callbacks, methodInfo, flags, entryAddress, nativeSizeOfCode);
#endif
}

DLL_EXPORT void JitGetVersionIdentifier(ICorJitCompiler* pJit, GUID* versionIdentifier)
{
    pJit->getVersionIdentifier(versionIdentifier);
}

DLL_EXPORT void JitSetOs(ICorJitCompiler* pJit, CORINFO_OS os)
{
    pJit->setTargetOS(os);
}

typedef void (*LogCallback)(int level, const char* message);
static LogCallback s_logCallback;

static void LogFormatted(int level, const char* format, va_list args)
{
    va_list argsCopy;
    va_copy(argsCopy, args);
    int length = vsnprintf(nullptr, 0, format, argsCopy);
    va_end(argsCopy);
    if (length < 0)
        return;

    char* message = new char[length + 1];
    vsnprintf(message, length + 1, format, args);
    s_logCallback(level, message);
    delete[] message;
}

#define PRINTER(name, level)                          \
    static void __cdecl name(const char* format, ...) \
    {                                                 \
        va_list args;                                 \
        va_start(args, format);                       \
        LogFormatted(level, format, args);            \
        va_end(args);                                 \
    }

PRINTER(PrintError, 0)
PRINTER(PrintWarning, 1)
PRINTER(PrintVerbose, 2)

// The printers for coredistools, which take printf-style arguments. They pass the formatted messages to
// 'callback', with the level 0 for errors, 1 for warnings, and 2 for everything else.
DLL_EXPORT const PrintControl* GetCoreDisToolsPrinters(LogCallback callback)
{
    static const PrintControl s_printControl = {PrintError, PrintWarning, PrintVerbose, PrintVerbose};
    s_logCallback = callback;
    return &s_printControl;
}

// The types of the metrics that the JIT reports through reportMetadata. Matches JitMetrics.MetricType.
enum JitMetricType
{
    JitMetricInt,
    JitMetricInt64,
    JitMetricDouble,
};

template <typename T> struct JitMetricTypeOf;
template <> struct JitMetricTypeOf<int> { static const JitMetricType value = JitMetricInt; };
template <> struct JitMetricTypeOf<int64_t> { static const JitMetricType value = JitMetricInt64; };
template <> struct JitMetricTypeOf<double> { static const JitMetricType value = JitMetricDouble; };

struct JitMetricInfo
{
    const char* name;
    JitMetricType type;
};

static const JitMetricInfo s_jitMetrics[] = {
#define JITMETADATAINFO(name, type, flags)
#define JITMETADATAMETRIC(name, type, flags) {#name, JitMetricTypeOf<type>::value},
#include "../../../jit/jitmetadatalist.h"
};

DLL_EXPORT int GetJitMetricCount()
{
    return sizeof(s_jitMetrics) / sizeof(s_jitMetrics[0]);
}

// The metrics are in the order that the JIT reports them.
DLL_EXPORT const char* GetJitMetric(int index, JitMetricType* type)
{
    *type = s_jitMetrics[index].type;
    return s_jitMetrics[index].name;
}

static volatile uint64_t s_globalZero = 0;

// A dynamic instrumentor can rewrite this function to provide the precise instruction counts that SuperPMI
// reports in its metrics. The volatile read tells the compiler that something might actually happen here.
DLL_EXPORT NOINLINE void Instrumentor_GetInsCount(uint64_t* result)
{
    // The instrumentor may have written the value already.
    if (*result == 0)
    {
        *result = s_globalZero;
    }
}
