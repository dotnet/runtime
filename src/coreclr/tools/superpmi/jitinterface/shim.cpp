// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The native side of the managed SuperPMI shims, which the runtime loads as its JIT. The JIT-EE interface objects
// here make the calls to the real JIT and to the EE themselves, and only call the managed shim to record (or count)
// them: managed code is never between the runtime and the JIT, or between the JIT and the EE. So the EE's exceptions
// propagate through native frames only, as they were thrown, which the runtime depends on (for example, to get the
// managed exception object of a type load failure).
//
// The managed collector only records what it gets: the arguments and the result of each call, and the code of the
// exception that the EE threw, if any. The few other queries of the EE that the recording needs are made here.

#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <map>
#include <mutex>
#include <new>
#include <string>
#include <unordered_map>

#ifdef _MSC_VER
#include <excpt.h>
#endif

#include "jitfaultguard.h"
#include "jitinterface_generated.h"
#include "../../../inc/corjithost.h"

static const WCHAR s_emptyMessage[1] = {};

// The code of an exception that the EE threw for a query of the shim itself, or an error of the managed shim. The errors are thrown as CapturedException*, so that the shim doesn't take the CorInfoExceptionClass* that
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

// Records updateEntryPointForTailCall, with the entry point from before the call too.
typedef void (*RecordUpdateEntryPointForTailCallCallback)(void* thisHandle, CorInfoExceptionClass** ppException,
    CORINFO_CONST_LOOKUP* origEntryPoint, CORINFO_CONST_LOOKUP* entryPoint, uint32_t exceptionCode);

#include "icorjitinfo_recorder_generated.h"

// Counts a call of the forwarding shims, with the name of the method.
typedef void (*OnCallCallback)(const char* name);

// The ICorJitInfo that the JIT gets from the shims. Each method calls the EE's, and then (Recording) passes the
// arguments and the result to the managed collector's recorder; or (otherwise) only forwards the call, after counting
// it with onCall if any (superpmi-shim-simple and superpmi-shim-counter). The EE's exceptions propagate from the EE to
// the JIT (and then to the EE again) through native frames only: the recorder gets the code of the exception while it
// propagates, but not the exception.
template <bool Recording>
class ShimJitInfo : public ICorJitInfo
{
    ICorJitInfo* _original;
    void* _thisHandle;
    const JitInfoRecorder* _recorder;
    RecordUpdateEntryPointForTailCallCallback _recordUpdateEntryPointForTailCall;
    OnCallCallback _onCall;

public:
    ShimJitInfo(ICorJitInfo* original, RecordUpdateEntryPointForTailCallCallback recordUpdateEntryPointForTailCall)
        : _original(original), _thisHandle(nullptr), _recorder(nullptr),
          _recordUpdateEntryPointForTailCall(recordUpdateEntryPointForTailCall), _onCall(nullptr)
    {
    }

    ShimJitInfo(ICorJitInfo* original, OnCallCallback onCall)
        : _original(original), _thisHandle(nullptr), _recorder(nullptr), _recordUpdateEntryPointForTailCall(nullptr),
          _onCall(onCall)
    {
    }

    void SetCollector(void* thisHandle, const JitInfoRecorder* recorder)
    {
        _thisHandle = thisHandle;
        _recorder = recorder;
    }

private:
    void OnCall(const char* name)
    {
        if (_onCall != nullptr)
            _onCall(name);
    }

    // Has the managed collector record the call, with the code of the exception that the EE threw (0 if none).
    template <typename FCallback>
    void Record(uint32_t exceptionCode, FCallback& callback)
    {
        CorInfoExceptionClass* pException = nullptr;
        callback(&pException, exceptionCode);
        if (pException == nullptr)
            return;

        // An error of the collector escapes to the JIT, as the errors of the managed JIT-EE interface do; but not
        // while an exception of the EE propagates.
        if (exceptionCode == 0 && Shim_IsError(pException))
            throw AsCaptured(pException);
        Shim_FreeException(pException);
    }

    template <typename FCall, typename FCallback>
    void CallThenRecord(const char* name, FCall call, FCallback callback)
    {
        if constexpr (!Recording)
        {
            OnCall(name);
            call();
        }
        else
        {
            auto recordException = [&](uint32_t code) { Record(code, callback); };
            CallRecordingException(call, recordException);
            Record(0, callback);
        }
    }

    // Records the call, which the caller then makes.
    template <typename FCallback>
    void RecordBefore(const char* name, FCallback callback)
    {
        if constexpr (!Recording)
            OnCall(name);
        else
            Record(0, callback);
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
            CallThenRecord("getNewHelper",
                [&]() { result = _original->getNewHelper(classHandle, &hasSideEffects); },
                [&](CorInfoExceptionClass** ppException, uint32_t exceptionCode) { _recorder->getNewHelper(_thisHandle, ppException, classHandle, &hasSideEffects, result, exceptionCode); });
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
            CallThenRecord("isEnum",
                [&]() { result = _original->isEnum(cls, &tempUnderlyingType); },
                [&](CorInfoExceptionClass** ppException, uint32_t exceptionCode) { _recorder->isEnum(_thisHandle, ppException, cls, &tempUnderlyingType, result, exceptionCode); });
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
            CallThenRecord("getHelperFtn",
                [&]() { _original->getHelperFtn(ftnNum, &nativeEntrypoint, &method); },
                [&](CorInfoExceptionClass** ppException, uint32_t exceptionCode) { _recorder->getHelperFtn(_thisHandle, ppException, ftnNum, &nativeEntrypoint, &method, exceptionCode); });
            if (pNativeEntrypoint != nullptr)
                *pNativeEntrypoint = nativeEntrypoint;
            if (pMethod != nullptr)
                *pMethod = method;
        }
    }

    CorInfoTypeWithMod getArgType(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_HANDLE args, CORINFO_CLASS_HANDLE* vcTypeRet) override
    {
        CorInfoTypeWithMod result{};
        CallThenRecord("getArgType",
            [&]() { result = _original->getArgType(sig, args, vcTypeRet); },
            [&](CorInfoExceptionClass** ppException, uint32_t exceptionCode) { _recorder->getArgType(_thisHandle, ppException, sig, args, vcTypeRet, result, exceptionCode); });

        if constexpr (Recording)
        {
            // Records getArgClass too, for a "fat" method context. What it throws is dropped: the JIT didn't call it.
            CorInfoExceptionClass* pException = nullptr;
            RunCapturingException(&pException, [&]() { getArgClass(sig, args); });
            if (pException != nullptr)
                Shim_FreeException(pException);
        }
        return result;
    }

    // The collector records the entry point from before the call too, with a callback of its own.
    void updateEntryPointForTailCall(CORINFO_CONST_LOOKUP* entryPoint) override
    {
        CORINFO_CONST_LOOKUP origEntryPoint = *entryPoint;
        CallThenRecord("updateEntryPointForTailCall",
            [&]() { _original->updateEntryPointForTailCall(entryPoint); },
            [&](CorInfoExceptionClass** ppException, uint32_t exceptionCode) { _recordUpdateEntryPointForTailCall(_thisHandle, ppException, &origEntryPoint, entryPoint, exceptionCode); });
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
    // Starts recording the compilation: returns the handle of the managed recorder, with its callbacks in *pRecorder;
    // or null not to record it.
    void* (*beginCompile)(void* thisHandle, ICorJitInfo* comp, CORINFO_METHOD_INFO* info, unsigned flags,
        const JitInfoRecorder** pRecorder);
    // Ends recording the compilation, also when an exception escapes the JIT.
    void (*endCompile)(void* thisHandle, void* jitInfoHandle, CorJitResult result, uint8_t* nativeEntry, uint32_t nativeSizeOfCode);
    void (*setTargetOS)(void* thisHandle, CORINFO_OS os);
    RecordUpdateEntryPointForTailCallCallback recordUpdateEntryPointForTailCall;
};

// Prints the name in full, with a buffer of the size that the first call requires, so that the method context
// records all of it.
template <typename FPrint>
static void PrintInFull(FPrint print)
{
    char buffer[256];
    size_t requiredSize = 0;
    print(buffer, sizeof(buffer), &requiredSize);
    if (requiredSize > sizeof(buffer))
    {
        std::string fullBuffer(requiredSize, '\0');
        print(&fullBuffer[0], requiredSize, nullptr);
    }
}

// Forces some extra data into the method context ("fat" method contexts), which ildump needs. What the EE throws
// for these queries is dropped: the JIT didn't make them.
static void RecordExtraData(ICorJitInfo* jitInfo, CORINFO_METHOD_INFO* info)
{
    CorInfoExceptionClass* pException = nullptr;
    RunCapturingException(&pException, [&]() {
        static const CorInfoClassId classIds[] = {
            CLASSID_SYSTEM_OBJECT, CLASSID_TYPED_BYREF, CLASSID_TYPE_HANDLE, CLASSID_FIELD_HANDLE,
            CLASSID_METHOD_HANDLE, CLASSID_STRING, CLASSID_RUNTIME_TYPE,
        };
        for (CorInfoClassId classId : classIds)
            jitInfo->getBuiltinClass(classId);

        CORINFO_CLASS_HANDLE ourClass = jitInfo->getMethodClass(info->ftn);
        jitInfo->getClassAttribs(ourClass);
        jitInfo->isValueClass(ourClass);
        jitInfo->asCorInfoType(ourClass);

        PrintInFull([&](char* buffer, size_t bufferSize, size_t* pRequiredBufferSize) {
            return jitInfo->printClassName(ourClass, buffer, bufferSize, pRequiredBufferSize); });
        PrintInFull([&](char* buffer, size_t bufferSize, size_t* pRequiredBufferSize) {
            return jitInfo->printMethodName(info->ftn, buffer, bufferSize, pRequiredBufferSize); });
    });
    if (pException != nullptr)
        Shim_FreeException(pException);
}

// The ICorJitCompiler of the collector, which compiles with the real JIT and records the JIT-EE calls of the
// compilation with RecordingJitInfo. RecordingJitInfo calls the EE from native code before the managed collector
// records the call, so the EE's exceptions never unwind through managed frames.
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
        RecordingJitInfo jitInfo(comp, _callbacks->recordUpdateEntryPointForTailCall);
        const JitInfoRecorder* recorder = nullptr;
        void* jitInfoHandle = _callbacks->beginCompile(_thisHandle, comp, info, flags, &recorder);
        if (jitInfoHandle == nullptr)
            return _jit->compileMethod(comp, info, flags, nativeEntry, nativeSizeOfCode);

        jitInfo.SetCollector(jitInfoHandle, recorder);
        *nativeEntry = nullptr;
        *nativeSizeOfCode = 0;
        RecordExtraData(&jitInfo, info);

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

// superpmi-shim-counter: counts the calls of the forwarding shims by the name of the method, and writes the counts to a
// .csv file when the shim is unloaded (at the end of the process), without running managed code then.
namespace CallCounter
{
    static std::mutex s_lock;
    // By the address of the name, a string literal of this file; never freed, as it is used until the end.
    static std::unordered_map<const char*, long>* s_counts;
    static std::string s_fileName;

    static void Count(const char* name)
    {
        std::lock_guard<std::mutex> lock(s_lock);
        (*s_counts)[name]++;
    }

    static OnCallCallback GetCallback()
    {
        return s_counts != nullptr ? &Count : nullptr;
    }

    static struct Writer
    {
        ~Writer()
        {
            if (s_counts == nullptr)
                return;

            std::lock_guard<std::mutex> lock(s_lock);
            FILE* file = fopen(s_fileName.c_str(), "w");
            if (file == nullptr)
            {
                fprintf(stderr, "ERROR: Couldn't open file '%s'\n", s_fileName.c_str());
                return;
            }

            // Ordered by name, as the names of the different call sites of a method are different literals.
            std::map<std::string, long> byName;
            for (const auto& count : *s_counts)
                byName[count.first] += count.second;

            fprintf(file, "FunctionName,Count\n");
            for (const auto& count : byName)
                fprintf(file, "%s,%ld\n", count.first.c_str(), count.second);
            fclose(file);
        }
    } s_writer;
}

// Called once, by superpmi-shim-counter, before the forwarding JIT and JIT host are created.
extern "C" void Shim_EnableCallCounting(const char* fileName)
{
    CallCounter::s_fileName = fileName;
    CallCounter::s_counts = new std::unordered_map<const char*, long>();
}

extern "C" CorInfoExceptionClass* Shim_AllocError(uint32_t code)
{
    return new CapturedException(code, true);
}

extern "C" bool Shim_IsError(CorInfoExceptionClass* pException)
{
    return AsCaptured(pException)->isError;
}

extern "C" void Shim_FreeException(CorInfoExceptionClass* pException)
{
    delete AsCaptured(pException);
}

extern "C" ICorJitCompiler* Shim_CreateForwardingJitCompiler(ICorJitCompiler* jit)
{
    return new ForwardingJitCompiler(jit, CallCounter::GetCallback());
}

extern "C" ICorJitHost* Shim_CreateForwardingJitHost(ICorJitHost* original)
{
    return new ForwardingJitHost(original, CallCounter::GetCallback());
}

extern "C" ICorJitCompiler* Shim_CreateRecordingJitCompiler(ICorJitCompiler* jit, void* thisHandle, const RecordingCompilerCallbacks* callbacks)
{
    return new RecordingJitCompiler(jit, thisHandle, callbacks);
}

extern "C" ICorJitHost* Shim_CreateJitHost(ICorJitHost* original, void* thisHandle, const JitHostCallbacks* callbacks)
{
    return new JitHostWrapper(original, thisHandle, callbacks);
}

// The name of the method and of its class (in *className), for the collection filter of the collector; null if
// the EE throws. Not recorded.
extern "C" const char* Shim_GetMethodNameFromMetadata(ICorJitInfo* comp, CORINFO_METHOD_HANDLE ftn, const char** className)
{
    const char* result = nullptr;
    *className = nullptr;
    CorInfoExceptionClass* pException = nullptr;
    RunCapturingException(&pException, [&]() { result = comp->getMethodNameFromMetadata(ftn, className, nullptr, nullptr, 0); });
    if (pException == nullptr)
        return result;

    Shim_FreeException(pException);
    *className = nullptr;
    return nullptr;
}

// The length of the GC layout of the class, in pointers of the target (not of the host, so that cross-target
// collections, such as wasm32 on an x64 host, record all of it), for the collector's getClassGClayout. Not recorded.
// The EE answered getClassGClayout for the class, so these don't throw; if they do, the recording can't go on.
extern "C" uint32_t Shim_GetClassGCLayoutLength(ICorJitInfo* comp, CORINFO_CLASS_HANDLE cls)
{
    uint32_t result = 0;
    CorInfoExceptionClass* pException = nullptr;
    RunCapturingException(&pException, [&]() {
        uint32_t architecture = comp->getExpectedTargetArchitecture();
        uint32_t pointerSize = (architecture == CORINFO_ARCH_X86 || architecture == CORINFO_ARCH_ARM ||
            architecture == CORINFO_ARCH_WASM32) ? 4 : 8;
        uint32_t size = comp->isValueClass(cls) ? comp->getClassSize(cls) : comp->getHeapClassSize(cls);
        result = (size + pointerSize - 1) / pointerSize;
    });
    if (pException != nullptr)
    {
        fprintf(stderr, "SuperPMI collector: the EE threw for the GC layout length of a class.\n");
        abort();
    }
    return result;
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
