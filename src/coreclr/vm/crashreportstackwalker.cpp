// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// VM-side implementation of the in-proc crash report thread callbacks.

#include "common.h"
#include "codeman.h"
#include "dbginterface.h"
#include "method.hpp"
#include "peassembly.h"
#include <clrconfignocache.h>
#include <minipal/guid.h>
#include <minipal/tempdir.h>

#ifdef FEATURE_INPROC_CRASHREPORT

#include "inproccrashreporter.h"
#include "threadsuspend.h"
#include "gcenv.h"

struct WalkContext
{
    InProcCrashReportFrameCallback callback;
    void* userCtx;
};

static void BuildTypeName(LPUTF8 buffer, size_t bufferSize, LPCUTF8 namespaceName, LPCUTF8 className);

static
StackWalkAction
FrameCallbackAdapter(
    CrawlFrame* pCF,
    VOID* pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    WalkContext* ctx = static_cast<WalkContext*>(pData);
    MethodDesc* pMD = pCF->GetFunction();
    if (pMD == nullptr)
    {
        return SWA_CONTINUE;
    }

    LPCUTF8 methodName = pMD->GetName();
    mdMethodDef token = pMD->GetMemberDef();

    LPCUTF8 className = nullptr;
    LPCUTF8 namespaceName = nullptr;
    MethodTable* pMT = pMD->GetMethodTable();
    if (pMT != nullptr)
    {
        mdTypeDef cl = pMT->GetCl();
        IMDInternalImport* pImport = pMD->GetMDImport();
        if (pImport != nullptr && cl != mdTypeDefNil)
        {
            pImport->GetNameOfTypeDef(cl, &className, &namespaceName);
        }
    }

    char classNameBuf[CRASHREPORT_STRING_BUFFER_SIZE];
    BuildTypeName(classNameBuf, sizeof(classNameBuf), namespaceName, className);

    LPCUTF8 moduleName = nullptr;
    Module* pModule = pMD->GetModule();
    if (pModule != nullptr)
    {
        Assembly* pAssembly = pModule->GetAssembly();
        if (pAssembly != nullptr)
        {
            moduleName = pAssembly->GetSimpleName();
        }
    }

    uint32_t nativeOffset = pCF->HasFaulted() ? 0 : pCF->GetRelOffset();
    uint32_t ilOffset = 0;
    PCODE ip = (PCODE)0;
    TADDR stackPointer = (TADDR)0;
    PREGDISPLAY pRD = pCF->GetRegisterSet();
    if (pRD != nullptr)
    {
        ip = GetControlPC(pRD);
        stackPointer = GetRegdisplaySP(pRD);
    }

    if (ip == (PCODE)0 && stackPointer == (TADDR)0)
    {
        return SWA_CONTINUE;
    }

    if (g_pDebugInterface != nullptr && pMD != nullptr)
    {
        DWORD resolvedILOffset = 0;
        BOOL haveILOffset = FALSE;
        EX_TRY
        {
            haveILOffset = g_pDebugInterface->GetILOffsetFromNative(
                pMD,
                reinterpret_cast<LPCBYTE>(ip),
                nativeOffset,
                &resolvedILOffset);
        }
        EX_CATCH
        {
            // Best-effort: if IL-offset resolution throws, leave ilOffset = 0
            // and continue with the native frame metadata we already have.
        }
        EX_END_CATCH
        if (haveILOffset)
        {
            ilOffset = resolvedILOffset;
        }
    }

    uint32_t moduleTimestamp = 0;
    uint32_t moduleSize = 0;
    char moduleGuid[MINIPAL_GUID_BUFFER_LEN];
    moduleGuid[0] = '\0';

    if (pModule != nullptr)
    {
        PEAssembly* pPEAssembly = pModule->GetPEAssembly();
        if (pPEAssembly != nullptr && pPEAssembly->HasLoadedPEImage())
        {
            moduleTimestamp = pPEAssembly->GetLoadedLayout()->GetTimeDateStamp();
            moduleSize = static_cast<uint32_t>(pPEAssembly->GetLoadedLayout()->GetSize());
        }

        IMDInternalImport* pImport = pModule->GetMDImport();
        if (pImport != nullptr)
        {
            GUID mvid;
            if (SUCCEEDED(pImport->GetScopeProps(nullptr, &mvid)))
            {
                minipal_guid_as_string(mvid, moduleGuid, MINIPAL_GUID_BUFFER_LEN);
            }
        }
    }

    className = classNameBuf[0] == '\0' ? nullptr : classNameBuf;
    ctx->callback(static_cast<uint64_t>(ip), static_cast<uint64_t>(stackPointer), methodName, className, moduleName, nativeOffset, static_cast<uint32_t>(token), ilOffset, moduleTimestamp, moduleSize, moduleGuid, ctx->userCtx);
    return SWA_CONTINUE;
}

static
void
CrashReportWalkThread(
    Thread* pThread,
    InProcCrashReportFrameCallback frameCallback,
    void* ctx)
{
    if (pThread == nullptr || frameCallback == nullptr)
    {
        return;
    }

    WalkContext walkContext = { frameCallback, ctx };
    pThread->StackWalkFrames(FrameCallbackAdapter, &walkContext,
        QUICKUNWIND | FUNCTIONSONLY | ALLOW_ASYNC_STACK_WALK);
}

static
void
CrashReportWalkStack(
    InProcCrashReportFrameCallback frameCallback,
    void* ctx)
{
    CrashReportWalkThread(GetThreadAsyncSafe(), frameCallback, ctx);
}

static
bool
CrashReportIsCurrentThreadManaged()
{
    return GetThreadAsyncSafe() != nullptr;
}

// Copy a type's namespace-qualified name (namespace + '.' + class) into
// |buffer|, truncating if needed. Always null-terminates when bufferSize > 0.
static
void
BuildTypeName(
    LPUTF8 buffer,
    size_t bufferSize,
    LPCUTF8 namespaceName,
    LPCUTF8 className)
{
    if (bufferSize == 0)
    {
        return;
    }

    size_t index = 0;
    if (namespaceName != nullptr)
    {
        while (*namespaceName != '\0' && index + 1 < bufferSize)
        {
            buffer[index++] = *namespaceName++;
        }
    }

    if (className != nullptr)
    {
        if (index > 0 && index + 1 < bufferSize)
        {
            buffer[index++] = '.';
        }

        while (*className != '\0' && index + 1 < bufferSize)
        {
            buffer[index++] = *className++;
        }
    }

    buffer[index] = '\0';
}

static
bool
CrashReportGetExceptionForThread(
    Thread* pThread,
    char* exceptionTypeBuf,
    size_t exceptionTypeBufSize,
    uint32_t* hresult)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (exceptionTypeBufSize > 0)
    {
        exceptionTypeBuf[0] = '\0';
    }

    if (hresult != nullptr)
    {
        *hresult = 0;
    }

    // Only inspect the managed throwable when the thread is already in cooperative mode.
    if (!pThread->PreemptiveGCDisabled())
    {
        return false;
    }

    bool result = false;

    GCX_COOP();

    OBJECTREF throwable = pThread->GetThrowable();
    GCPROTECT_BEGIN(throwable);

    if (throwable != nullptr)
    {
        MethodTable* pMT = throwable->GetMethodTable();
        if (pMT != nullptr)
        {
            mdTypeDef cl = pMT->GetCl();
            Module* pModule = pMT->GetModule();
            if (pModule != nullptr)
            {
                IMDInternalImport* pImport = pModule->GetMDImport();
                if (pImport != nullptr && cl != mdTypeDefNil)
                {
                    LPCUTF8 className = nullptr;
                    LPCUTF8 namespaceName = nullptr;
                    pImport->GetNameOfTypeDef(cl, &className, &namespaceName);

                    BuildTypeName(exceptionTypeBuf, exceptionTypeBufSize, namespaceName, className);
                }
            }
        }

        if (hresult != nullptr)
        {
            *hresult = static_cast<uint32_t>(((EXCEPTIONREF)throwable)->GetHResult());
        }

        result = true;
    }

    GCPROTECT_END();

    return result;
}

static
bool
CrashReportGetException(
    char* exceptionTypeBuf,
    size_t exceptionTypeBufSize,
    uint32_t* hresult)
{
    Thread* pThread = GetThreadAsyncSafe();
    if (pThread == nullptr)
    {
        return false;
    }

    return CrashReportGetExceptionForThread(pThread, exceptionTypeBuf, exceptionTypeBufSize, hresult);
}

// Suspend non-crashing managed threads via SuspendEE so their stacks
// can be walked from runtime-known safe points. SuspendEE acquires the
// thread store lock and waits for every other managed thread to reach a
// safe point (and for any in-progress GC to complete), so skip it when
// a known pre-condition would prevent forward progress:
//
//  * g_fFatalErrorOccurredOnGCThread: GC thread faulted mid-GC, so GC
//    will never finish and SuspendEE's GC wait would hang.
//  * GCHeapUtilities::IsGCInProgress(): a GC is already running; if it
//    is wedged (common in runtime-internal crashes) SuspendEE hangs.
//  * IsGCSpecialThread(): we are a GC thread ourselves; the GC wait
//    would wait on us.
//  * ThreadStore::HoldingThreadStore(pCrashThread): SuspendEE's
//    LockThreadStore asserts the holder is unknown, so it would
//    assert-fail in checked builds (undefined in release).
//
// The crash reporter is best-effort; on hang the Android watchdog
// kills the process and we keep whatever crash report JSON was flushed
// beforehand.

static
bool
CrashReportSuspendThreads(Thread* pCrashThread)
{
    if (g_fFatalErrorOccurredOnGCThread
        || GCHeapUtilities::IsGCInProgress()
        || IsGCSpecialThread()
        || ThreadStore::HoldingThreadStore(pCrashThread))
    {
        return false;
    }

    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
    return true;
}

static
void
CrashReportResumeThreads(bool runtimeSuspended)
{
    if (runtimeSuspended)
    {
        ThreadSuspend::RestartEE(FALSE /* bFinishedGC */, TRUE /* SuspendSucceeded */);
    }
}

static
void
CrashReportEnumerateThreads(
    uint64_t crashingTid,
    InProcCrashReportThreadCallback threadCallback,
    InProcCrashReportFrameCallback frameCallback,
    void* ctx)
{
    Thread* pCrashThread = GetThreadAsyncSafe();

    bool runtimeSuspended = CrashReportSuspendThreads(pCrashThread);

    // Emit the crashing thread first so the report keeps the most important
    // thread even if later enumeration is incomplete.
    if (pCrashThread != nullptr)
    {
        uint64_t crashOsId = static_cast<uint64_t>(pCrashThread->GetOSThreadId());
        if (crashOsId == crashingTid)
        {
            char exceptionType[CRASHREPORT_STRING_BUFFER_SIZE];
            uint32_t hresult = 0;
            bool hasException = CrashReportGetExceptionForThread(pCrashThread, exceptionType, sizeof(exceptionType), &hresult);

            threadCallback(crashOsId, true, hasException ? exceptionType : "", hresult, ctx);

            CrashReportWalkThread(pCrashThread, frameCallback, ctx);
        }
    }

    // Walk the remaining managed threads only when the runtime was
    // successfully suspended; otherwise the walker is not guaranteed
    // to be at a safe point for them.
    if (runtimeSuspended)
    {
        Thread* pThread = nullptr;
        while ((pThread = ThreadStore::GetThreadList(pThread)) != nullptr)
        {
            if (pThread == pCrashThread)
                continue;

            uint64_t osThreadId = static_cast<uint64_t>(pThread->GetOSThreadId());
            if (osThreadId == 0 || osThreadId == crashingTid)
                continue;

            threadCallback(osThreadId, false, "", 0, ctx);
            CrashReportWalkThread(pThread, frameCallback, ctx);
        }
    }

    CrashReportResumeThreads(runtimeSuspended);
}

void
CrashReportConfigure()
{
    // Read crash report configuration here rather than in PROCAbortInitialize
    // because on Android the DOTNET_* environment variables are set via JNI
    // after PAL_Initialize has already run.
    CLRConfigNoCache enabledReportCfg = CLRConfigNoCache::Get("EnableCrashReport", /*noprefix*/ false, &getenv);
    DWORD reportEnabled = 0;
    bool enableCrashReport = enabledReportCfg.IsSet() && enabledReportCfg.TryAsInteger(10, reportEnabled) && reportEnabled == 1;

    CLRConfigNoCache enabledReportOnlyCfg = CLRConfigNoCache::Get("EnableCrashReportOnly", /*noprefix*/ false, &getenv);
    DWORD reportOnlyEnabled = 0;
    bool enableCrashReportOnly = enabledReportOnlyCfg.IsSet() && enabledReportOnlyCfg.TryAsInteger(10, reportOnlyEnabled) && reportOnlyEnabled == 1;

    if (!enableCrashReport && !enableCrashReportOnly)
    {
        return;
    }

    CLRConfigNoCache dmpNameCfg = CLRConfigNoCache::Get("DbgMiniDumpName", /*noprefix*/ false, &getenv);
    const char* dumpName = dmpNameCfg.IsSet() ? dmpNameCfg.AsString() : nullptr;
    if (dumpName == nullptr || dumpName[0] == '\0')
    {
        return;
    }

    // If DbgMiniDumpName is just a filename (no directory component), write
    // the crash report under the platform temp directory.
    char dumpPathBuf[CRASHREPORT_STRING_BUFFER_SIZE];
    if (strchr(dumpName, DIRECTORY_SEPARATOR_CHAR_A) == nullptr)
    {
        if (!minipal_get_tempdir(dumpPathBuf, sizeof(dumpPathBuf)))
        {
            return;
        }
        size_t tmpLen = strlen(dumpPathBuf);
        size_t dumpLen = strlen(dumpName);
        if (tmpLen + dumpLen + 1 > sizeof(dumpPathBuf))
        {
            return;
        }
        memcpy(dumpPathBuf + tmpLen, dumpName, dumpLen);
        dumpPathBuf[tmpLen + dumpLen] = '\0';
        dumpName = dumpPathBuf;
    }

    InProcCrashReporterSettings settings = {};
    settings.reportPath = dumpName;
    settings.isManagedThreadCallback = CrashReportIsCurrentThreadManaged;
    settings.walkStackCallback = CrashReportWalkStack;
    settings.getExceptionCallback = CrashReportGetException;
    settings.enumerateThreadsCallback = CrashReportEnumerateThreads;

    // Initialize the reporter and register the PAL signal-path callback last
    // so PAL only observes the reporter after all VM callbacks are wired in.
    InProcCrashReportInitialize(settings);
}

#endif // FEATURE_INPROC_CRASHREPORT
