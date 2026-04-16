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

#ifdef HOST_ANDROID

#include "debug/crashreport/inproccrashreporter.h"
#include <sys/syscall.h>
#include <signal.h>
#include <unistd.h>

extern "C" void PROCEnableInProcCrashReport();

// ---------------------------------------------------------------------------
// Crash-time thread suspension using a dedicated signal and pipe.
//
// At init time we install a handler for SIGUSR2 that, when armed, parks
// the receiving thread on a pipe read.  At crash time the reporter arms
// the gate, sends SIGUSR2 to every non-crashing managed thread, waits
// briefly, walks stacks, then closes the pipe to release everyone.
//
// This approach keeps the PAL's activation signal handler untouched.
// ---------------------------------------------------------------------------
static volatile int s_crashSuspendArmed = 0;
static int s_crashResumePipe[2] = { -1, -1 };

static void CrashSuspendSignalHandler(int sig, siginfo_t* info, void* context)
{
    (void)sig;
    (void)info;
    (void)context;

    if (!__atomic_load_n(&s_crashSuspendArmed, __ATOMIC_ACQUIRE))
        return;

    // Block until the crash reporter closes the write end.
    // read() is async-signal-safe.
    char buf;
    while (read(s_crashResumePipe[0], &buf, 1) == -1 && errno == EINTR)
        ;
}

static void CrashSuspendInstallHandler()
{
    struct sigaction sa;
    memset(&sa, 0, sizeof(sa));
    sa.sa_sigaction = CrashSuspendSignalHandler;
    sa.sa_flags = SA_SIGINFO | SA_RESTART;
    sigemptyset(&sa.sa_mask);
    sigaction(SIGUSR2, &sa, NULL);
}

static void CrashSuspendArm()
{
    if (pipe(s_crashResumePipe) != 0)
    {
        s_crashResumePipe[0] = -1;
        s_crashResumePipe[1] = -1;
    }
    __atomic_store_n(&s_crashSuspendArmed, 1, __ATOMIC_RELEASE);
}

static void CrashSuspendRelease()
{
    if (s_crashResumePipe[1] != -1)
    {
        close(s_crashResumePipe[1]);
        s_crashResumePipe[1] = -1;
    }
}

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
    uint32_t ilOffset = 0;
    uint64_t ip = 0;
    uint64_t stackPointer = 0;
    PREGDISPLAY pRD = pCF->GetRegisterSet();
    if (pRD != NULL)
    {
        ip = static_cast<uint64_t>(GetControlPC(pRD));
        stackPointer = static_cast<uint64_t>(GetRegdisplaySP(pRD));
    }

    if (ip == 0 && stackPointer == 0)
    {
        return SWA_CONTINUE;
    }

    if (g_pDebugInterface != NULL && pMD != NULL)
    {
        DWORD resolvedILOffset = 0;
        if (g_pDebugInterface->GetILOffsetFromNative(
            pMD,
            reinterpret_cast<LPCBYTE>(static_cast<TADDR>(ip)),
            nativeOffset,
            &resolvedILOffset))
        {
            ilOffset = resolvedILOffset;
        }
    }

    uint32_t moduleTimestamp = 0;
    uint32_t moduleSize = 0;
    char moduleGuid[MINIPAL_GUID_BUFFER_LEN];
    moduleGuid[0] = '\0';

    if (pModule != NULL)
    {
        PEAssembly* pPEAssembly = pModule->GetPEAssembly();
        if (pPEAssembly != NULL && pPEAssembly->HasLoadedPEImage())
        {
            moduleTimestamp = pPEAssembly->GetLoadedLayout()->GetTimeDateStamp();
            moduleSize = static_cast<uint32_t>(pPEAssembly->GetLoadedLayout()->GetSize());
        }

        IMDInternalImport* pImport = pModule->GetMDImport();
        if (pImport != NULL)
        {
            GUID mvid;
            if (SUCCEEDED(pImport->GetScopeProps(NULL, &mvid)))
            {
                minipal_guid_as_string(mvid, moduleGuid, MINIPAL_GUID_BUFFER_LEN);
            }
        }
    }

    ctx->callback(ip, stackPointer, methodName, classNameBuf, moduleName, nativeOffset, static_cast<uint32_t>(token), ilOffset, moduleTimestamp, moduleSize, moduleGuid, ctx->userCtx);
    return SWA_CONTINUE;
}

static
void
CrashReportWalkThread(
    Thread* pThread,
    InProcCrashReportFrameCallback frameCallback,
    void* ctx)
{
    if (pThread == NULL || frameCallback == NULL)
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
int
CrashReportIsCurrentThreadManaged()
{
    return GetThreadAsyncSafe() != NULL;
}

static
int
CrashReportGetExceptionForThread(
    Thread* pThread,
    char* exceptionTypeBuf,
    int exceptionTypeBufSize,
    uint32_t* hresult)
{
    if (exceptionTypeBufSize > 0)
    {
        exceptionTypeBuf[0] = '\0';
    }

    if (hresult != NULL)
    {
        *hresult = 0;
    }

    // Only inspect the managed throwable when the thread is already in cooperative mode.
    if (!pThread->PreemptiveGCDisabled())
    {
        return 0;
    }

    int result = 0;

    GCX_COOP();

    OBJECTREF throwable = pThread->GetThrowable();
    GCPROTECT_BEGIN(throwable);

    if (throwable != NULL)
    {
        MethodTable* pMT = throwable->GetMethodTable();
        if (pMT != NULL)
        {
            mdTypeDef cl = pMT->GetCl();
            Module* pModule = pMT->GetModule();
            if (pModule != NULL)
            {
                IMDInternalImport* pImport = pModule->GetMDImport();
                if (pImport != NULL && cl != mdTypeDefNil)
                {
                    LPCUTF8 className = NULL;
                    LPCUTF8 namespaceName = NULL;
                    pImport->GetNameOfTypeDef(cl, &className, &namespaceName);

                    int index = 0;
                    if (namespaceName != NULL)
                    {
                        while (*namespaceName != '\0' && index < exceptionTypeBufSize - 1)
                        {
                            exceptionTypeBuf[index++] = *namespaceName++;
                        }
                    }

                    if (className != NULL)
                    {
                        if (index > 0 && index < exceptionTypeBufSize - 1)
                        {
                            exceptionTypeBuf[index++] = '.';
                        }

                        while (*className != '\0' && index < exceptionTypeBufSize - 1)
                        {
                            exceptionTypeBuf[index++] = *className++;
                        }
                    }

                    exceptionTypeBuf[index] = '\0';
                }
            }
        }

        if (hresult != NULL)
        {
            *hresult = static_cast<uint32_t>(((EXCEPTIONREF)throwable)->GetHResult());
        }

        result = 1;
    }

    GCPROTECT_END();

    return result;
}

static
int
CrashReportGetException(
    char* exceptionTypeBuf,
    int exceptionTypeBufSize,
    char* exceptionMsgBuf,
    int exceptionMsgBufSize,
    uint32_t* hresult)
{
    Thread* pThread = GetThreadAsyncSafe();
    if (pThread == NULL)
    {
        return 0;
    }

    if (exceptionMsgBufSize > 0)
    {
        exceptionMsgBuf[0] = '\0';
    }

    return CrashReportGetExceptionForThread(pThread, exceptionTypeBuf, exceptionTypeBufSize, hresult);
}

// Suspend non-crashing threads so their managed stacks can be walked
// reliably.  Sends SIGUSR2 to every non-crashing managed thread; the
// handler (installed at init) parks them on a pipe read.
static
void
CrashReportSuspendThreads(Thread* pCrashThread)
{
    CrashSuspendArm();

    pid_t pid = getpid();
    Thread* pThread = ThreadStore::GetThreadList(NULL);
    while (pThread != NULL)
    {
        if (pThread != pCrashThread)
        {
            DWORD tid = pThread->GetOSThreadId();
            if (tid != 0)
            {
                syscall(SYS_tgkill, pid, static_cast<pid_t>(tid), SIGUSR2);
            }
        }
        pThread = ThreadStore::GetThreadList(pThread);
    }

    // Brief wait for threads to park in the signal handler.
    usleep(50000);
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
    bool crashThreadHandled = false;

    CrashReportSuspendThreads(pCrashThread);

    // Emit the crashing thread first so the report keeps the most important
    // thread even if later enumeration is incomplete.
    if (pCrashThread != NULL)
    {
        uint64_t crashOsId = static_cast<uint64_t>(pCrashThread->GetOSThreadId());
        if (crashOsId == crashingTid)
        {
            char exceptionType[256];
            uint32_t hresult = 0;
            int hasException = CrashReportGetExceptionForThread(pCrashThread, exceptionType, sizeof(exceptionType), &hresult);

            threadCallback(crashOsId, 1, hasException ? exceptionType : "", hresult, ctx);

            CrashReportWalkThread(pCrashThread, frameCallback, ctx);
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
        char exceptionType[256];
        uint32_t hresult = 0;
        int hasException = 0;

        if (isCrashThread)
        {
            hasException = CrashReportGetExceptionForThread(pThread, exceptionType, sizeof(exceptionType), &hresult);
        }

        threadCallback(osThreadId, isCrashThread ? 1 : 0, hasException ? exceptionType : "", hresult, ctx);

        if (isCrashThread)
        {
            CrashReportWalkThread(pThread, frameCallback, ctx);
            crashThreadHandled = true;
        }
        else
        {
            // Non-crashing threads have been parked by the crash-suspend
            // signal handler.  Their managed stacks are frozen and safe
            // to walk regardless of their original GC mode.
            CrashReportWalkThread(pThread, frameCallback, ctx);
        }

        pThread = ThreadStore::GetThreadList(pThread);
    }

    CrashSuspendRelease();
}

void
CrashReportRegisterStackWalker()
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

    const char* defaultReportDirectory = getenv("HOME");
    if (defaultReportDirectory == nullptr || defaultReportDirectory[0] == '\0')
    {
        defaultReportDirectory = getenv("TMPDIR");
    }
    if (defaultReportDirectory == nullptr || defaultReportDirectory[0] == '\0')
    {
        defaultReportDirectory = "/data/local/tmp";
    }

    InProcCrashReportInitialize(1, dumpName, defaultReportDirectory);

    // Set the PAL flag so PROCCreateCrashDumpIfEnabled knows to call the reporter.
    PROCEnableInProcCrashReport();

    // Install the SIGUSR2 handler for crash-time thread suspension.
    CrashSuspendInstallHandler();

    InProcCrashReportSetCurrentThreadManagedResolver(CrashReportIsCurrentThreadManaged);
    InProcCrashReportSetStackWalker(CrashReportWalkStack);
    InProcCrashReportSetExceptionResolver(CrashReportGetException);
    InProcCrashReportSetThreadEnumerator(CrashReportEnumerateThreads);
}

#endif // HOST_ANDROID
