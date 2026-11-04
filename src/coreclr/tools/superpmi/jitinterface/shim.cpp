// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The native side of the managed SuperPMI shims, which the runtime loads as its JIT. The runtime and the JIT call
// the JIT-EE interface objects here, which forward to the managed shim; and the managed shim calls the real JIT
// and EE through the forwarders here. An exception that the EE (or the JIT) throws is captured where the call is
// made, carried through the managed shim as a CorInfoExceptionClass, and rethrown as it was thrown when the call
// returns to the EE: at the end of compileMethod, and in the function that the EE's error trap runs.

#include <stdint.h>
#include <string.h>
#include <exception>
#include <new>

#ifdef _MSC_VER
#include <excpt.h>
#else
#include <cxxabi.h>
#endif

#include "jitinterface_generated.h"
#include "../../../inc/corjithost.h"

static const WCHAR s_emptyMessage[1] = {};

// An exception that the EE or the JIT threw, or (without an exception) an error of the managed shim itself.
struct CapturedException : CorInfoExceptionClass
{
    std::exception_ptr exception;
    uint32_t code;

    CapturedException(std::exception_ptr exception, uint32_t code)
        : CorInfoExceptionClass(s_emptyMessage, 0), exception(exception), code(code)
    {
    }
};

static CapturedException* AsCaptured(CorInfoExceptionClass* pException)
{
    return static_cast<CapturedException*>(pException);
}

#ifdef _MSC_VER
// Records the code of the structured exception, before the C++ handler of the caller catches it.
template <typename F>
static void RunWithCodeFilter(F& function, uint32_t* code)
{
    __try
    {
        function();
    }
    __except (*code = GetExceptionCode(), EXCEPTION_CONTINUE_SEARCH)
    {
    }
}
#else
// The code of the current exception if the PAL threw it (PAL_SEHException, whose first field points to the
// EXCEPTION_RECORD), and otherwise 1, the code that is recorded for the other exceptions.
static uint32_t GetCurrentExceptionCode()
{
    const std::type_info* type = abi::__cxa_current_exception_type();
    if (type == nullptr || strcmp(type->name(), "16PAL_SEHException") != 0)
        return 1;

    std::exception_ptr exception = std::current_exception();
    void* object = *reinterpret_cast<void**>(&exception);
    return **reinterpret_cast<uint32_t**>(object);
}
#endif

// Runs the function, and returns what it throws in *ppException instead.
template <typename F>
static void RunCapturingException(CorInfoExceptionClass** ppException, F function)
{
    uint32_t code = 1;
    try
    {
#ifdef _MSC_VER
        RunWithCodeFilter(function, &code);
#else
        function();
#endif
    }
    catch (CorInfoExceptionClass* pException)
    {
        // Already captured, and rethrown through the JIT by the JIT-EE interface wrapper.
        *ppException = pException;
    }
    catch (...)
    {
#ifndef _MSC_VER
        code = GetCurrentExceptionCode();
#endif
        *ppException = new CapturedException(std::current_exception(), code);
    }
}

[[noreturn]] static void Rethrow(CorInfoExceptionClass* pException)
{
    CapturedException* captured = AsCaptured(pException);
    if (!captured->exception)
    {
        // An error of the managed shim, which escapes to the runtime.
        throw pException;
    }

    std::exception_ptr exception = captured->exception;
    delete captured;
    std::rethrow_exception(exception);
}

struct TrapParam
{
    ICorJitInfo::errorTrapFunction function;
    void* parameter;
};

// Runs the JIT's function in the EE's error trap, which gets the exceptions as the EE threw them.
static void TrapTrampoline(void* p)
{
    TrapParam* param = static_cast<TrapParam*>(p);
    try
    {
        param->function(param->parameter);
    }
    catch (CorInfoExceptionClass* pException)
    {
        Rethrow(pException);
    }
}

static bool Forward_runWithErrorTrap(ICorJitInfo* jitInfo, CorInfoExceptionClass** ppException, ICorJitInfo::errorTrapFunction function, void* parameter)
{
    TrapParam param = { function, parameter };
    bool result = false;
    RunCapturingException(ppException, [&]() { result = jitInfo->runWithErrorTrap(TrapTrampoline, &param); });
    return result;
}

static bool Forward_runWithSPMIErrorTrap(ICorJitInfo* jitInfo, CorInfoExceptionClass** ppException, ICorJitInfo::errorTrapFunction function, void* parameter)
{
    TrapParam param = { function, parameter };
    bool result = false;
    RunCapturingException(ppException, [&]() { result = jitInfo->runWithSPMIErrorTrap(TrapTrampoline, &param); });
    return result;
}

#include "icorjitinfo_forwarders_generated.h"

bool JitInterfaceWrapper::runWithErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter)
{
    CorInfoExceptionClass* pException = nullptr;
    bool result = _callbacks->runWithErrorTrap(_thisHandle, &pException, function, parameter);
    if (pException != nullptr)
        throw pException;
    return result;
}

bool JitInterfaceWrapper::runWithSPMIErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter)
{
    CorInfoExceptionClass* pException = nullptr;
    bool result = _callbacks->runWithSPMIErrorTrap(_thisHandle, &pException, function, parameter);
    if (pException != nullptr)
        throw pException;
    return result;
}

struct JitCompilerCallbacks
{
    CorJitResult (*compileMethod)(void* thisHandle, CorInfoExceptionClass** ppException, ICorJitInfo* comp,
        CORINFO_METHOD_INFO* info, unsigned flags, uint8_t** nativeEntry, uint32_t* nativeSizeOfCode);
    void (*ProcessShutdownWork)(void* thisHandle, ICorStaticInfo* info);
    void (*getVersionIdentifier)(void* thisHandle, GUID* versionIdentifier);
    void (*setTargetOS)(void* thisHandle, CORINFO_OS os);
};

class JitCompilerWrapper : public ICorJitCompiler
{
    void* _thisHandle;
    const JitCompilerCallbacks* _callbacks;

public:
    JitCompilerWrapper(void* thisHandle, const JitCompilerCallbacks* callbacks)
        : _thisHandle(thisHandle), _callbacks(callbacks)
    {
    }

    CorJitResult compileMethod(ICorJitInfo* comp, CORINFO_METHOD_INFO* info, unsigned flags, uint8_t** nativeEntry,
        uint32_t* nativeSizeOfCode) override
    {
        CorInfoExceptionClass* pException = nullptr;
        CorJitResult result = _callbacks->compileMethod(_thisHandle, &pException, comp, info, flags, nativeEntry, nativeSizeOfCode);
        if (pException != nullptr)
            Rethrow(pException);
        return result;
    }

    void ProcessShutdownWork(ICorStaticInfo* info) override
    {
        _callbacks->ProcessShutdownWork(_thisHandle, info);
    }

    void getVersionIdentifier(GUID* versionIdentifier) override
    {
        _callbacks->getVersionIdentifier(_thisHandle, versionIdentifier);
    }

    void setTargetOS(CORINFO_OS os) override
    {
        _callbacks->setTargetOS(_thisHandle, os);
    }
};

struct JitHostCallbacks
{
    int (*getIntConfigValue)(void* thisHandle, const char* name, int defaultValue);
    const char* (*getStringConfigValue)(void* thisHandle, const char* name);
    void (*freeStringConfigValue)(void* thisHandle, const char* value);
};

class JitHostWrapper : public ICorJitHost
{
    ICorJitHost* _original;
    void* _thisHandle;
    const JitHostCallbacks* _callbacks;

public:
    JitHostWrapper(ICorJitHost* original, void* thisHandle, const JitHostCallbacks* callbacks)
        : _original(original), _thisHandle(thisHandle), _callbacks(callbacks)
    {
    }

    void* allocateMemory(size_t size) override
    {
        return _original->allocateMemory(size);
    }

    void freeMemory(void* block) override
    {
        _original->freeMemory(block);
    }

    int getIntConfigValue(const char* name, int defaultValue) override
    {
        return _callbacks->getIntConfigValue(_thisHandle, name, defaultValue);
    }

    const char* getStringConfigValue(const char* name) override
    {
        return _callbacks->getStringConfigValue(_thisHandle, name);
    }

    void freeStringConfigValue(const char* value) override
    {
        _callbacks->freeStringConfigValue(_thisHandle, value);
    }
};

// Called when the shim is unloaded (at the end of the process).
static void (*s_unloadCallback)();

static struct UnloadNotifier
{
    ~UnloadNotifier()
    {
        if (s_unloadCallback != nullptr)
            s_unloadCallback();
    }
} s_unloadNotifier;

extern "C" void Shim_SetUnloadCallback(void (*callback)())
{
    s_unloadCallback = callback;
}

extern "C" void* const* Shim_GetForwarders()
{
    return s_forwarders;
}

extern "C" CorInfoExceptionClass* Shim_AllocError(uint32_t code)
{
    return new CapturedException(std::exception_ptr(), code);
}

extern "C" uint32_t Shim_GetExceptionCode(CorInfoExceptionClass* pException)
{
    return AsCaptured(pException)->code;
}

extern "C" bool Shim_IsError(CorInfoExceptionClass* pException)
{
    return !AsCaptured(pException)->exception;
}

extern "C" void Shim_FreeException(CorInfoExceptionClass* pException)
{
    delete AsCaptured(pException);
}

extern "C" ICorJitCompiler* Shim_CreateJitCompiler(void* thisHandle, const JitCompilerCallbacks* callbacks)
{
    return new JitCompilerWrapper(thisHandle, callbacks);
}

extern "C" ICorJitHost* Shim_CreateJitHost(ICorJitHost* original, void* thisHandle, const JitHostCallbacks* callbacks)
{
    return new JitHostWrapper(original, thisHandle, callbacks);
}

// Compiles the method with the JIT; an exception that escapes it is returned in *ppException.
extern "C" CorJitResult Shim_CompileMethod(CorInfoExceptionClass** ppException, ICorJitCompiler* jit, ICorJitInfo* comp,
    CORINFO_METHOD_INFO* info, unsigned flags, uint8_t** nativeEntry, uint32_t* nativeSizeOfCode)
{
    CorJitResult result = CORJIT_INTERNALERROR;
    RunCapturingException(ppException, [&]() { result = jit->compileMethod(comp, info, flags, nativeEntry, nativeSizeOfCode); });
    return result;
}

// Compiles the method with the JIT, which calls the managed shim's JIT-EE interface.
extern "C" CorJitResult Shim_CompileMethodWithJitInfo(CorInfoExceptionClass** ppException, ICorJitCompiler* jit, void* thisHandle,
    void** callbacks, CORINFO_METHOD_INFO* info, unsigned flags, uint8_t** nativeEntry, uint32_t* nativeSizeOfCode)
{
    JitInterfaceWrapper jitInfo(thisHandle, callbacks);
    return Shim_CompileMethod(ppException, jit, &jitInfo, info, flags, nativeEntry, nativeSizeOfCode);
}

extern "C" void Shim_ProcessShutdownWork(ICorJitCompiler* jit, ICorStaticInfo* info)
{
    jit->ProcessShutdownWork(info);
}

extern "C" void Shim_GetVersionIdentifier(ICorJitCompiler* jit, GUID* versionIdentifier)
{
    jit->getVersionIdentifier(versionIdentifier);
}

extern "C" void Shim_SetTargetOS(ICorJitCompiler* jit, CORINFO_OS os)
{
    jit->setTargetOS(os);
}

// The error trap for the errors of the managed shim (as opposed to the EE's), for the collector's
// runWithSPMIErrorTrap: it only catches the method context errors, and returns other exceptions in *ppException.
extern "C" bool Shim_RunWithSPMIErrorTrap(CorInfoExceptionClass** ppException, ICorJitInfo::errorTrapFunction function,
    void* parameter, uint32_t errorCode)
{
    RunCapturingException(ppException, [&]() { function(parameter); });
    if (*ppException == nullptr)
        return true;

    if (Shim_IsError(*ppException) && AsCaptured(*ppException)->code == errorCode)
    {
        Shim_FreeException(*ppException);
        *ppException = nullptr;
    }
    return false;
}

extern "C" int Shim_HostGetIntConfigValue(ICorJitHost* host, const char* name, int defaultValue)
{
    return host->getIntConfigValue(name, defaultValue);
}

extern "C" const char* Shim_HostGetStringConfigValue(ICorJitHost* host, const char* name)
{
    return host->getStringConfigValue(name);
}

extern "C" void Shim_HostFreeStringConfigValue(ICorJitHost* host, const char* value)
{
    host->freeStringConfigValue(value);
}
