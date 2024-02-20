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
    uint32_t NumCodeBytes = 0;
    uint64_t NumExecutedInstructions = 0;
};

class JitInstance
{
private:
    char*          PathToOriginalJit;
    char*          PathToTempJit;
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

    HRESULT StartUp(char* PathToJit, bool copyJit, bool breakOnDebugBreakorAV, MethodContext* firstContext);
    bool reLoad(MethodContext* firstContext);

    bool callJitStartup(ICorJitHost* newHost);

    bool resetConfig(MethodContext* firstContext);

    ReplayResults CompileMethod(MethodContext* MethodToCompile, int mcIndex, bool collectThroughput);

    const WCHAR* getForceOption(const WCHAR* key);
    const WCHAR* getOption(const WCHAR* key);
    const WCHAR* getOption(const WCHAR* key, LightWeightMap<DWORD, DWORD>* options);

    const MethodContext::Environment& getEnvironment();

    void* allocateArray(size_t size);
    void* allocateLongLivedArray(size_t size);
    void freeArray(void* array);
    void freeLongLivedArray(void* array);

    void updateForceOptions(LightWeightMap<DWORD, DWORD>* newForceOptions);
};

#endif
