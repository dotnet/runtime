// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// VM-side implementation of the in-proc crash report thread callbacks.

#include "common.h"
#include "codeman.h"
#include "method.hpp"

#ifdef HOST_ANDROID

#include "../pal/src/crashreport/inproccrashreporter.h"

struct WalkContext
{
    InProcCrashReportFrameCallback callback;
    void* userCtx;
};

static
StackWalkAction
FrameCallbackAdapter(
    CrawlFrame* pCF,
    VOID* pData)
{
    WalkContext* ctx = static_cast<WalkContext*>(pData);
    MethodDesc* pMD = pCF->GetFunction();
    if (pMD == NULL)
    {
        return SWA_CONTINUE;
    }

    LPCUTF8 methodName = pMD->GetName();
    mdMethodDef token = pMD->GetMemberDef();

    LPCUTF8 className = NULL;
    LPCUTF8 namespaceName = NULL;
    MethodTable* pMT = pMD->GetMethodTable();
    if (pMT != NULL)
    {
        mdTypeDef cl = pMT->GetCl();
        IMDInternalImport* pImport = pMD->GetMDImport();
        if (pImport != NULL && cl != mdTypeDefNil)
        {
            pImport->GetNameOfTypeDef(cl, &className, &namespaceName);
        }
    }

    char classNameBuf[256] = { 0 };
    int index = 0;
    if (namespaceName != NULL)
    {
        while (*namespaceName != '\0' && index < static_cast<int>(sizeof(classNameBuf)) - 1)
        {
            classNameBuf[index++] = *namespaceName++;
        }
    }

    if (className != NULL)
    {
        if (index > 0 && index < static_cast<int>(sizeof(classNameBuf)) - 1)
        {
            classNameBuf[index++] = '.';
        }

        while (*className != '\0' && index < static_cast<int>(sizeof(classNameBuf)) - 1)
        {
            classNameBuf[index++] = *className++;
        }
    }
    classNameBuf[index] = '\0';

    const char* moduleName = NULL;
    Module* pModule = pMD->GetModule();
    if (pModule != NULL)
    {
        Assembly* pAssembly = pModule->GetAssembly();
        if (pAssembly != NULL)
        {
            moduleName = pAssembly->GetSimpleName();
        }
    }

    uint32_t nativeOffset = pCF->HasFaulted() ? 0 : pCF->GetRelOffset();
    uint64_t ip = 0;
    uint64_t stackPointer = 0;
    PREGDISPLAY pRD = pCF->GetRegisterSet();
    if (pRD != NULL)
    {
        ip = static_cast<uint64_t>(GetControlPC(pRD));
        stackPointer = static_cast<uint64_t>(GetRegdisplaySP(pRD));
    }

    ctx->callback(ip, stackPointer, methodName, classNameBuf, moduleName, nativeOffset, static_cast<uint32_t>(token), ctx->userCtx);
    return SWA_CONTINUE;
}

static
void
CrashReportWalkStack(
    InProcCrashReportFrameCallback frameCallback,
    void* ctx)
{
    Thread* pThread = GetThreadAsyncSafe();
    if (pThread == NULL)
    {
        return;
    }

    WalkContext walkContext = { frameCallback, ctx };
    pThread->StackWalkFrames(FrameCallbackAdapter, &walkContext,
        QUICKUNWIND | FUNCTIONSONLY | ALLOW_ASYNC_STACK_WALK);
}

static
int
CrashReportIsCurrentThreadManaged()
{
    return GetThreadAsyncSafe() != NULL;
}

static
void
CrashReportEnumerateThreads(
    uint64_t crashingTid,
    InProcCrashReportThreadCallback threadCallback,
    InProcCrashReportFrameCallback frameCallback,
    void* ctx)
{
    // This minimal lift intentionally reuses the existing ThreadStore traversal
    // and StackWalkFrames as a best-effort source for managed thread state.
    // The later strict-safety slices replace this with the signal-safe thread
    // registry and pre-published frame snapshots.
    Thread* pCrashThread = GetThreadAsyncSafe();
    bool crashThreadHandled = false;

    // Emit the crashing thread first so the report keeps the most important
    // thread even if later enumeration is incomplete.
    if (pCrashThread != NULL)
    {
        uint64_t crashOsId = static_cast<uint64_t>(pCrashThread->GetOSThreadId());
        if (crashOsId == crashingTid)
        {
            threadCallback(crashOsId, 1, ctx);

            WalkContext walkContext = { frameCallback, ctx };
            pCrashThread->StackWalkFrames(FrameCallbackAdapter, &walkContext,
                QUICKUNWIND | FUNCTIONSONLY | ALLOW_ASYNC_STACK_WALK);
            crashThreadHandled = true;
        }
    }

    Thread* pThread = ThreadStore::GetThreadList(NULL);
    while (pThread != NULL)
    {
        if (crashThreadHandled && pThread == pCrashThread)
        {
            pThread = ThreadStore::GetThreadList(pThread);
            continue;
        }

        uint64_t osThreadId = static_cast<uint64_t>(pThread->GetOSThreadId());
        if (osThreadId == 0)
        {
            pThread = ThreadStore::GetThreadList(pThread);
            continue;
        }

        bool isCrashThread = !crashThreadHandled && osThreadId == crashingTid;
        threadCallback(osThreadId, isCrashThread ? 1 : 0, ctx);
        if (isCrashThread)
        {
            crashThreadHandled = true;
        }

        if (pThread->PreemptiveGCDisabled() == FALSE)
        {
            Frame* pFrame = pThread->GetFrame();
            if (pFrame != NULL && pFrame != FRAME_TOP)
            {
                WalkContext walkContext = { frameCallback, ctx };
                pThread->StackWalkFrames(FrameCallbackAdapter, &walkContext,
                    QUICKUNWIND | FUNCTIONSONLY | ALLOW_ASYNC_STACK_WALK);
            }
        }

        pThread = ThreadStore::GetThreadList(pThread);
    }
}

void
CrashReportRegisterStackWalker()
{
    InProcCrashReportSetCurrentThreadManagedResolver(CrashReportIsCurrentThreadManaged);
    InProcCrashReportSetStackWalker(CrashReportWalkStack);
    InProcCrashReportSetThreadEnumerator(CrashReportEnumerateThreads);
}

#endif // HOST_ANDROID
