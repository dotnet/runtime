//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _JitInstance
#define _JitInstance

#include "superpmi.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "cycletimer.h"

class JitInstance
{
private:
    char*          PathToOriginalJit;
    char*          PathToTempJit;
    HANDLE         ourHeap;
    HMODULE        hLib;
    PgetJit        pngetJit;
    PjitStartup    pnjitStartup;
    PsxsJitStartup pnsxsJitStartup;
    ICorJitHost*   jitHost;
    ICorJitInfo*   icji;
    SimpleTimer    stj;

    LightWeightMap<DWORD, DWORD>* forceOptions;
    LightWeightMap<DWORD, DWORD>* options;

    MethodContext::Environment environment;

    JitInstance(){};
    void timeResult(CORINFO_METHOD_INFO info, unsigned flags);

public:
    enum Result
    {
        RESULT_ERROR,
        RESULT_SUCCESS,
        RESULT_MISSING
    };
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

    Result CompileMethod(MethodContext* MethodToCompile, int mcIndex, bool collectThroughput);

    const wchar_t* getForceOption(const wchar_t* key);
    const wchar_t* getOption(const wchar_t* key);
    const wchar_t* getOption(const wchar_t* key, LightWeightMap<DWORD, DWORD>* options);

    const MethodContext::Environment& getEnvironment();

    void* allocateArray(ULONG size);
    void* allocateLongLivedArray(ULONG size);
    void freeArray(void* array);
    void freeLongLivedArray(void* array);
};

#endif
