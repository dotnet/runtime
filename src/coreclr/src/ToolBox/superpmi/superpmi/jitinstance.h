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
    static JitInstance* InitJit(char* nameOfJit, bool breakOnAssert, SimpleTimer* st1, MethodContext* firstContext);

    HRESULT StartUp(char* PathToJit, bool copyJit, bool breakOnDebugBreakorAV, MethodContext* firstContext);
    bool reLoad(MethodContext* firstContext);

    Result CompileMethod(MethodContext* MethodToCompile, int mcIndex, bool collectThroughput);

    void* allocateArray(ULONG size);
    void* allocateLongLivedArray(ULONG size);
    void freeArray(void* array);
    void freeLongLivedArray(void* array);
};

#endif
