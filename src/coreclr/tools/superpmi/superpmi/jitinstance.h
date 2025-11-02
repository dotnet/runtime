// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _JitInstance
#define _JitInstance

#include "superpmi.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "cycletimer.h"

enum class ReplayResult
{
    Success,
    Error,
    Miss,
};

struct ReplayResults
{
    ReplayResult Result = ReplayResult::Success;
    bool IsMinOpts = false;
    uint64_t NumExecutedInstructions = 0;
    CompileResult* CompileResults = nullptr;
};

class JitInstance
{
private:
    HMODULE        hLib;
    PgetJit        pngetJit;
    PjitStartup    pnjitStartup;
    ICorJitHost*   jitHost;
    ICorJitInfo*   icji;
    SimpleTimer    stj;

    LightWeightMap<DWORD, DWORD>* forceOptions;
    LightWeightMap<DWORD, DWORD>* options;

    MethodContext::Environment environment;

    JitInstance(){};
    void timeResult(CORINFO_METHOD_INFO info, unsigned flags);

public:

    bool forceClearAltJitFlag;
    bool forceSetAltJitFlag;

    CycleTimer       lt;
    MethodContext*   mc;
    ULONGLONG        times[2];
    ICorJitCompiler* pJitInstance;

    // Allocate and initialize the jit provided
    static JitInstance* InitJit(char*          nameOfJit,
                                bool           breakOnAssert,
                                SimpleTimer*   st1,
                                MethodContext* firstContext,
                                LightWeightMap<DWORD, DWORD>* forceOptions,
                                LightWeightMap<DWORD, DWORD>* options);

    HRESULT StartUp(char* PathToJit, bool breakOnDebugBreakorAV, MethodContext* firstContext);

    bool callJitStartup(ICorJitHost* newHost);

    bool resetConfig(MethodContext* firstContext);

    ReplayResults CompileMethod(MethodContext* MethodToCompile, int mcIndex, bool collectThroughput);

    const char* getForceOption(const char* key);
    const char* getOption(const char* key);
    const char* getOption(const char* key, LightWeightMap<DWORD, DWORD>* options);

    uint32_t getJitFlags(CORJIT_FLAGS* jitFlags, uint32_t sizeInBytes);

    const MethodContext::Environment& getEnvironment();

    void* allocateArray(size_t size);
    void* allocateLongLivedArray(size_t size);
    void freeArray(void* array);
    void freeLongLivedArray(void* array);

    void updateForceOptions(LightWeightMap<DWORD, DWORD>* newForceOptions);
};

#endif
