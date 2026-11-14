// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The native side of the managed SuperPMI shims, which the runtime loads as its JIT. The JIT-EE interface objects
// here make the calls to the real JIT and to the EE themselves, and only call the managed shim to record (or count)
// them: managed code is never between the runtime and the JIT, or between the JIT and the EE. So the EE's exceptions
// propagate through native frames only, as they were thrown, which the runtime depends on (for example, to get the
// managed exception object of a type load failure).
//
// The managed collector also calls the EE itself, to record more data, through the forwarders here. An exception
// of such a call is caught where the call is made, and only its code goes to the managed collector, which records
// it: the call that the JIT made succeeded, so the exception doesn't propagate.

#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <new>

#ifdef _MSC_VER
#include <excpt.h>
#endif

#include "jitfaultguard.h"
#include "jitinterface_generated.h"
#include "../../../inc/corjithost.h"

static const WCHAR s_emptyMessage[1] = {};

// The code of an exception that the EE threw for a call of the managed collector, or an error of the managed shim
// itself. The errors are thrown as CapturedException*, so that the shim doesn't take the CorInfoExceptionClass* that
// crossgen2's JIT-EE interface throws for its own.
struct CapturedException : CorInfoExceptionClass
{
    uint32_t code;
    bool isError;

    CapturedException(uint32_t code, bool isError)
        : CorInfoExceptionClass(s_emptyMessage, 0), code(code), isError(isError)
    {
    }
};

static CapturedException* AsCaptured(CorInfoExceptionClass* pException)
{
    return static_cast<CapturedException*>(pException);
}

extern "C" bool Shim_IsError(CorInfoExceptionClass* pException);
extern "C" void Shim_FreeException(CorInfoExceptionClass* pException);

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
#endif

// Runs the function, and returns what it throws in *ppException instead: the exception itself is discarded. Elsewhere
// than on Windows, the code of a C++ exception isn't known, so it is recorded as 1.
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
    catch (CapturedException* pException)
    {
        // An error of the collector, which RecordingJitInfo threw through the JIT.
        *ppException = pException;
    }
    catch (...)
    {
        *ppException = new CapturedException(code, false);
    }
}

static bool Forward_runWithErrorTrap(ICorJitInfo* jitInfo, CorInfoExceptionClass** ppException, ICorJitInfo::errorTrapFunction function, void* parameter)
{
    bool result = false;
    RunCapturingException(ppException, [&]() { result = jitInfo->runWithErrorTrap(function, parameter); });
    return result;
}

static bool Forward_runWithSPMIErrorTrap(ICorJitInfo* jitInfo, CorInfoExceptionClass** ppException, ICorJitInfo::errorTrapFunction function, void* parameter)
{
    bool result = false;
    RunCapturingException(ppException, [&]() { result = jitInfo->runWithSPMIErrorTrap(function, parameter); });
    return result;
}

#include "icorjitinfo_forwarders_generated.h"

#ifdef _MSC_VER
// Runs 'call'. If it throws, calls 'recordException' with the code of the exception while the exception propagates:
// the filter only records the code, so the exception isn't caught, and the frames that the exception unwinds are
// those from the call up to its handler, as without the shim.
template <typename FCall, typename FRecord>
static void CallRecordingException(FCall& call, FRecord& recordException)
{
    uint32_t code = 0;
    __try
    {
        __try
        {
            call();
        }
        __except (code = GetExceptionCode(), EXCEPTION_CONTINUE_SEARCH)
        {
        }
    }
    __finally
    {
        if (AbnormalTermination())
            recordException(code);
    }
}
#else
// Elsewhere than on Windows, the EE's exceptions are C++ exceptions, whose code isn't known: it is recorded as 1.
template <typename FCall, typename FRecord>
static void CallRecordingException(FCall& call, FRecord& recordException)
{
    try
    {
        call();
    }
    catch (...)
    {
        recordException(1);
        throw;
    }
}
#endif

// The call that the JIT made, which the managed collector records: the managed OriginalJitInfo returns its result
// (or throws, if it threw) instead of calling the EE again. The method of the call must take it, once, before it
// calls any other method of OriginalJitInfo (see ShimInterop.PendingCall).
struct PendingCall
{
    // The index of the method, or -1 when there is no pending call.
    int32_t index;
    int32_t taken;
    uint32_t exceptionCode;
    void* result;
};

// Called when the managed collector didn't take the pending call: it would have recorded something else than what
// the JIT got. Doesn't return.
typedef void (*PendingCallNotTakenCallback)(int32_t index);

// Records updateEntryPointForTailCall, with the entry point from before the call, which the callback of the managed
// JIT-EE interface doesn't get. Takes the pending call, and doesn't call the EE.
typedef void (*RecordUpdateEntryPointForTailCallCallback)(void* thisHandle, CorInfoExceptionClass** ppException,
    CORINFO_CONST_LOOKUP* origEntryPoint, CORINFO_CONST_LOOKUP* entryPoint);

// Counts a call of the forwarding shims, with the name of the method.
typedef void (*OnCallCallback)(const char* name);

// The ICorJitInfo that the JIT gets from the shims. Each method calls the EE's, and then (Recording) the managed
// collector to record the call; or (otherwise) only forwards the call, after counting it with onCall if any
// (superpmi-shim-simple and superpmi-shim-counter). The EE's exceptions propagate from the EE to the JIT (and then to
// the EE again) through native frames only: the managed collector records the call while the exception propagates,
// but doesn't get it.
template <bool Recording>
class ShimJitInfo : public ICorJitInfo
{
    ICorJitInfo* _original;
    void* _thisHandle;
    const JitInterfaceCallbacks* _callbacks;
    PendingCallNotTakenCallback _notTaken;
    RecordUpdateEntryPointForTailCallCallback _recordUpdateEntryPointForTailCall;
    OnCallCallback _onCall;

public:
    PendingCall pending;

    ShimJitInfo(ICorJitInfo* original, PendingCallNotTakenCallback notTaken,
        RecordUpdateEntryPointForTailCallCallback recordUpdateEntryPointForTailCall)
        : _original(original), _thisHandle(nullptr), _callbacks(nullptr), _notTaken(notTaken),
          _recordUpdateEntryPointForTailCall(recordUpdateEntryPointForTailCall), _onCall(nullptr), pending{-1, 0, 0, nullptr}
    {
    }

    ShimJitInfo(ICorJitInfo* original, OnCallCallback onCall)
        : _original(original), _thisHandle(nullptr), _callbacks(nullptr), _notTaken(nullptr),
          _recordUpdateEntryPointForTailCall(nullptr), _onCall(onCall), pending{-1, 0, 0, nullptr}
    {
    }

    void SetCollector(void* thisHandle, const JitInterfaceCallbacks* callbacks)
    {
        _thisHandle = thisHandle;
        _callbacks = callbacks;
    }

private:
    void OnCall(const char* name)
    {
        if (_onCall != nullptr)
            _onCall(name);
    }

    template <typename FCallback>
    void Record(int index, void* result, uint32_t exceptionCode, FCallback& callback)
    {
        pending = {index, 0, exceptionCode, result};
        CorInfoExceptionClass* pException = nullptr;
        callback(&pException);
        bool taken = pending.taken != 0;
        pending.index = -1;

        if (pException == nullptr)
        {
            if (!taken)
                _notTaken(index);
            return;
        }

        // An error of the collector itself (which may come before it took the call) escapes to the JIT, as the
        // errors of the managed JIT-EE interface do; but not while an exception of the EE propagates. The exception
        // of a call that the collector made to the EE itself, to record more data, is dropped: the call that the JIT
        // made succeeded.
        if (exceptionCode == 0 && Shim_IsError(pException))
            throw AsCaptured(pException);
        Shim_FreeException(pException);
    }

    template <typename FCall, typename FCallback>
    void CallThenRecord(int index, const char* name, void* result, FCall call, FCallback callback)
    {
        if constexpr (!Recording)
        {
            OnCall(name);
            call();
        }
        else
        {
            auto recordException = [&](uint32_t code) { Record(index, result, code, callback); };
            CallRecordingException(call, recordException);
            Record(index, result, 0, callback);
        }
    }

    // Records the call, which the caller then makes.
    template <typename FCallback>
    void RecordBefore(int index, const char* name, void* result, FCallback callback)
    {
        if constexpr (!Recording)
            OnCall(name);
        else
            Record(index, result, 0, callback);
    }

public:
#include "icorjitinfo_shim_generated.h"

    CorInfoHelpFunc getNewHelper(CORINFO_CLASS_HANDLE classHandle, bool* pHasSideEffects) override
    {
        if constexpr (!Recording)
        {
            OnCall("getNewHelper");
            return _original->getNewHelper(classHandle, pHasSideEffects);
        }
        else
        {
            CorInfoHelpFunc result{};
            bool hasSideEffects = false;
            CallThenRecord(Index_getNewHelper, "getNewHelper", &result,
                [&]() { result = _original->getNewHelper(classHandle, &hasSideEffects); },
                [&](CorInfoExceptionClass** ppException) { _callbacks->getNewHelper(_thisHandle, ppException, classHandle, &hasSideEffects); });
            if (pHasSideEffects != nullptr)
                *pHasSideEffects = hasSideEffects;
            return result;
        }
    }

    TypeCompareState isEnum(CORINFO_CLASS_HANDLE cls, CORINFO_CLASS_HANDLE* underlyingType) override
    {
        if constexpr (!Recording)
        {
            OnCall("isEnum");
            return _original->isEnum(cls, underlyingType);
        }
        else
        {
            TypeCompareState result{};
            CORINFO_CLASS_HANDLE tempUnderlyingType = nullptr;
            CallThenRecord(Index_isEnum, "isEnum", &result,
                [&]() { result = _original->isEnum(cls, &tempUnderlyingType); },
                [&](CorInfoExceptionClass** ppException) { _callbacks->isEnum(_thisHandle, ppException, cls, &tempUnderlyingType); });
            if (underlyingType != nullptr)
                *underlyingType = tempUnderlyingType;
            return result;
        }
    }

    void getHelperFtn(CorInfoHelpFunc ftnNum, CORINFO_CONST_LOOKUP* pNativeEntrypoint, CORINFO_METHOD_HANDLE* pMethod) override
    {
        if constexpr (!Recording)
        {
            OnCall("getHelperFtn");
            _original->getHelperFtn(ftnNum, pNativeEntrypoint, pMethod);
        }
        else
        {
            CORINFO_CONST_LOOKUP nativeEntrypoint{};
            CORINFO_METHOD_HANDLE method = nullptr;
            CallThenRecord(Index_getHelperFtn, "getHelperFtn", nullptr,
                [&]() { _original->getHelperFtn(ftnNum, &nativeEntrypoint, &method); },
                [&](CorInfoExceptionClass** ppException) { _callbacks->getHelperFtn(_thisHandle, ppException, ftnNum, &nativeEntrypoint, &method); });
            if (pNativeEntrypoint != nullptr)
                *pNativeEntrypoint = nativeEntrypoint;
            if (pMethod != nullptr)
                *pMethod = method;
        }
    }

    // The collector records the entry point from before the call too, with a callback of its own.
    void updateEntryPointForTailCall(CORINFO_CONST_LOOKUP* entryPoint) override
    {
        CORINFO_CONST_LOOKUP origEntryPoint = *entryPoint;
        CallThenRecord(Index_updateEntryPointForTailCall, "updateEntryPointForTailCall", nullptr,
            [&]() { _original->updateEntryPointForTailCall(entryPoint); },
            [&](CorInfoExceptionClass** ppException) { _recordUpdateEntryPointForTailCall(_thisHandle, ppException, &origEntryPoint, entryPoint); });
    }

    // The collector's error trap for its own errors, which the EE's exceptions go through.
    bool runWithSPMIErrorTrap(ICorJitInfo::errorTrapFunction function, void* parameter) override
    {
        if constexpr (!Recording)
        {
            OnCall("runWithSPMIErrorTrap");
            return _original->runWithSPMIErrorTrap(function, parameter);
        }
        else
        {
            try
            {
                function(parameter);
            }
            catch (CapturedException* pException)
            {
                Shim_FreeException(pException);
                return false;
            }
            return true;
        }
    }
};

using RecordingJitInfo = ShimJitInfo<true>;
using ForwardingJitInfo = ShimJitInfo<false>;

class ForwardingJitCompiler : public ICorJitCompiler
{
    ICorJitCompiler* _jit;
    OnCallCallback _onCall;

    void OnCall(const char* name)
    {
        if (_onCall != nullptr)
            _onCall(name);
    }

public:
    ForwardingJitCompiler(ICorJitCompiler* jit, OnCallCallback onCall)
        : _jit(jit), _onCall(onCall)
    {
    }

    CorJitResult compileMethod(ICorJitInfo* comp, CORINFO_METHOD_INFO* info, unsigned flags, uint8_t** nativeEntry,
        uint32_t* nativeSizeOfCode) override
    {
        OnCall("compileMethod");
        ForwardingJitInfo jitInfo(comp, _onCall);
        return _jit->compileMethod(&jitInfo, info, flags, nativeEntry, nativeSizeOfCode);
    }

    void ProcessShutdownWork(ICorStaticInfo* info) override
    {
        OnCall("ProcessShutdownWork");
        _jit->ProcessShutdownWork(info);
    }

    void getVersionIdentifier(GUID* versionIdentifier) override
    {
        OnCall("getVersionIdentifier");
        _jit->getVersionIdentifier(versionIdentifier);
    }

    void setTargetOS(CORINFO_OS os) override
    {
        _jit->setTargetOS(os);
    }
};

class ForwardingJitHost : public ICorJitHost
{
    ICorJitHost* _original;
    OnCallCallback _onCall;

    void OnCall(const char* name)
    {
        if (_onCall != nullptr)
            _onCall(name);
    }

public:
    ForwardingJitHost(ICorJitHost* original, OnCallCallback onCall)
        : _original(original), _onCall(onCall)
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
        OnCall("getIntConfigValue");
        return _original->getIntConfigValue(name, defaultValue);
    }

    const char* getStringConfigValue(const char* name) override
    {
        OnCall("getStringConfigValue");
        return _original->getStringConfigValue(name);
    }

    void freeStringConfigValue(const char* value) override
    {
        OnCall("freeStringConfigValue");
        _original->freeStringConfigValue(value);
    }
};

struct RecordingCompilerCallbacks
{
    // Starts recording the compilation: returns the handle of the managed JIT-EE interface, with its callbacks in
    // *pJitInfoCallbacks; or null not to record it.
    void* (*beginCompile)(void* thisHandle, ICorJitInfo* comp, PendingCall* pending, CORINFO_METHOD_INFO* info,
        unsigned flags, const JitInterfaceCallbacks** pJitInfoCallbacks);
    // Ends recording the compilation, also when an exception escapes the JIT.
    void (*endCompile)(void* thisHandle, void* jitInfoHandle, CorJitResult result, uint8_t* nativeEntry, uint32_t nativeSizeOfCode);
    void (*setTargetOS)(void* thisHandle, CORINFO_OS os);
    PendingCallNotTakenCallback pendingCallNotTaken;
    RecordUpdateEntryPointForTailCallCallback recordUpdateEntryPointForTailCall;
};

// The ICorJitCompiler of the collector, which compiles with the real JIT and records the JIT-EE calls of the
// compilation with RecordingJitInfo. Managed code is never on the stack while the JIT compiles.
class RecordingJitCompiler : public ICorJitCompiler
{
    ICorJitCompiler* _jit;
    void* _thisHandle;
    const RecordingCompilerCallbacks* _callbacks;

    struct EndCompile
    {
        const RecordingJitCompiler* compiler;
        void* jitInfoHandle;
        uint8_t** nativeEntry;
        uint32_t* nativeSizeOfCode;
        CorJitResult result;

        ~EndCompile()
        {
            compiler->_callbacks->endCompile(compiler->_thisHandle, jitInfoHandle, result, *nativeEntry, *nativeSizeOfCode);
        }
    };

public:
    RecordingJitCompiler(ICorJitCompiler* jit, void* thisHandle, const RecordingCompilerCallbacks* callbacks)
        : _jit(jit), _thisHandle(thisHandle), _callbacks(callbacks)
    {
    }

    CorJitResult compileMethod(ICorJitInfo* comp, CORINFO_METHOD_INFO* info, unsigned flags, uint8_t** nativeEntry,
        uint32_t* nativeSizeOfCode) override
    {
        RecordingJitInfo jitInfo(comp, _callbacks->pendingCallNotTaken, _callbacks->recordUpdateEntryPointForTailCall);
        const JitInterfaceCallbacks* jitInfoCallbacks = nullptr;
        void* jitInfoHandle = _callbacks->beginCompile(_thisHandle, comp, &jitInfo.pending, info, flags, &jitInfoCallbacks);
        if (jitInfoHandle == nullptr)
            return _jit->compileMethod(comp, info, flags, nativeEntry, nativeSizeOfCode);

        jitInfo.SetCollector(jitInfoHandle, jitInfoCallbacks);
        *nativeEntry = nullptr;
        *nativeSizeOfCode = 0;

        // Ends the recording when the JIT returns, or when an exception escapes it (as the destructors run while
        // the exception unwinds, also for the structured exceptions with /EHa).
#ifdef JIT_FAULT_GUARD
        // Elsewhere than on Windows, a fault of the JIT (such as a segmentation fault) doesn't unwind: the recording
        // ends when the JIT is abandoned, and then the process ends, as it would without the collector.
        int signal;
        {
            EndCompile end = {this, jitInfoHandle, nativeEntry, nativeSizeOfCode, CORJIT_INTERNALERROR};
            auto call = [&]() { end.result = _jit->compileMethod(&jitInfo, info, flags, nativeEntry, nativeSizeOfCode); };
            int code = 0;
            signal = JitFaultGuard::CallCatchingJitFaults(_jit, call, &code);
            if (signal == 0)
                return end.result;
        }
        fprintf(stderr, "SuperPMI collector: the JIT failed with signal %d (%s); the method context was saved.\n", signal, strsignal(signal));
        abort();
#else
        EndCompile end = {this, jitInfoHandle, nativeEntry, nativeSizeOfCode, CORJIT_INTERNALERROR};
        end.result = _jit->compileMethod(&jitInfo, info, flags, nativeEntry, nativeSizeOfCode);
        return end.result;
#endif
    }

    void ProcessShutdownWork(ICorStaticInfo* info) override
    {
        _jit->ProcessShutdownWork(info);
    }

    void getVersionIdentifier(GUID* versionIdentifier) override
    {
        _jit->getVersionIdentifier(versionIdentifier);
    }

    void setTargetOS(CORINFO_OS os) override
    {
        _callbacks->setTargetOS(_thisHandle, os);
        _jit->setTargetOS(os);
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
    return new CapturedException(code, true);
}

extern "C" uint32_t Shim_GetExceptionCode(CorInfoExceptionClass* pException)
{
    return AsCaptured(pException)->code;
}

extern "C" bool Shim_IsError(CorInfoExceptionClass* pException)
{
    return AsCaptured(pException)->isError;
}

extern "C" void Shim_FreeException(CorInfoExceptionClass* pException)
{
    delete AsCaptured(pException);
}

extern "C" ICorJitCompiler* Shim_CreateForwardingJitCompiler(ICorJitCompiler* jit, OnCallCallback onCall)
{
    return new ForwardingJitCompiler(jit, onCall);
}

extern "C" ICorJitHost* Shim_CreateForwardingJitHost(ICorJitHost* original, OnCallCallback onCall)
{
    return new ForwardingJitHost(original, onCall);
}

extern "C" ICorJitCompiler* Shim_CreateRecordingJitCompiler(ICorJitCompiler* jit, void* thisHandle, const RecordingCompilerCallbacks* callbacks)
{
    return new RecordingJitCompiler(jit, thisHandle, callbacks);
}

extern "C" ICorJitHost* Shim_CreateJitHost(ICorJitHost* original, void* thisHandle, const JitHostCallbacks* callbacks)
{
    return new JitHostWrapper(original, thisHandle, callbacks);
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
