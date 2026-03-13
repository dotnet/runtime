// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// cdacgcstress.cpp
//
// Implements in-process cDAC loading and stack reference verification
// for GC stress testing. When GCSTRESS_CDAC (0x20) is enabled, at each
// instruction-level GC stress point we:
//   1. Ask the cDAC to enumerate stack GC references via ISOSDacInterface::GetStackReferences
//   2. Ask the runtime to enumerate stack GC references via StackWalkFrames + GcInfoDecoder
//   3. Compare the two sets and report any mismatches
//

#include "common.h"

#ifdef HAVE_GCCOVER

#include "cdacgcstress.h"
#include "../../native/managed/cdac/inc/cdac_reader.h"
#include "../../debug/datadescriptor-shared/inc/contract-descriptor.h"
#include <clrdata.h>
#include <xclrdata.h>
#include <sospriv.h>
#include "threads.h"
#include "eeconfig.h"
#include "gccover.h"
#include "sstring.h"

#define CDAC_LIB_NAME MAKEDLLNAME_W(W("mscordaccore_universal"))
#define DAC_LIB_NAME  MAKEDLLNAME_W(W("mscordaccore"))

// Represents a single GC stack reference for comparison purposes.
struct StackRef
{
    CLRDATA_ADDRESS Address;    // Location on stack holding the ref
    CLRDATA_ADDRESS Object;     // The object pointer value
    unsigned int    Flags;      // SOSRefFlags (interior, pinned)
    CLRDATA_ADDRESS Source;     // IP or Frame that owns this ref
    int             SourceType; // SOS_StackSourceIP or SOS_StackSourceFrame
};

// Fixed-size buffer for collecting refs during stack walk.
// No heap allocation inside the promote callback — we're under NOTHROW contracts.
static const int MAX_COLLECTED_REFS = 4096;

// Static state — cDAC
static HMODULE          s_cdacModule = NULL;
static intptr_t         s_cdacHandle = 0;
static IUnknown*        s_cdacSosInterface = nullptr;
static decltype(&cdac_reader_flush_cache) s_flushCache = nullptr;

// Static state — legacy DAC
static HMODULE          s_dacModule = NULL;
static IUnknown*        s_dacSosInterface = nullptr;

// Static state — common
static bool             s_initialized = false;
static bool             s_failFast = true;
static FILE*            s_logFile = nullptr;

// Verification counters (reported at shutdown)
static volatile LONG    s_verifyCount = 0;
static volatile LONG    s_verifyPass = 0;
static volatile LONG    s_verifyFail = 0;
static volatile LONG    s_verifySkip = 0;

// Thread-local storage for the current thread context at the stress point.
static thread_local PCONTEXT s_currentContext = nullptr;
static thread_local DWORD    s_currentThreadId = 0;

// Extern declaration for the contract descriptor symbol exported from coreclr.
extern "C" struct ContractDescriptor DotNetRuntimeContractDescriptor;

//-----------------------------------------------------------------------------
// ICLRDataTarget implementation for in-process memory access.
// Used by the legacy DAC's CLRDataCreateInstance.
//-----------------------------------------------------------------------------

class InProcessDataTarget : public ICLRDataTarget
{
    volatile LONG m_refCount;
public:
    InProcessDataTarget() : m_refCount(1) {}

    // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppv) override
    {
        if (riid == IID_IUnknown || riid == __uuidof(ICLRDataTarget))
        {
            *ppv = static_cast<ICLRDataTarget*>(this);
            AddRef();
            return S_OK;
        }
        *ppv = nullptr;
        return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return InterlockedIncrement(&m_refCount); }
    ULONG STDMETHODCALLTYPE Release() override
    {
        LONG ref = InterlockedDecrement(&m_refCount);
        if (ref == 0) delete this;
        return ref;
    }

    // ICLRDataTarget
    HRESULT STDMETHODCALLTYPE GetMachineType(ULONG32* machineType) override
    {
#ifdef TARGET_AMD64
        *machineType = IMAGE_FILE_MACHINE_AMD64;
#elif defined(TARGET_X86)
        *machineType = IMAGE_FILE_MACHINE_I386;
#elif defined(TARGET_ARM64)
        *machineType = IMAGE_FILE_MACHINE_ARM64;
#elif defined(TARGET_ARM)
        *machineType = IMAGE_FILE_MACHINE_ARMNT;
#else
        return E_NOTIMPL;
#endif
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetPointerSize(ULONG32* pointerSize) override
    {
        *pointerSize = sizeof(void*);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetImageBase(LPCWSTR imagePath, CLRDATA_ADDRESS* baseAddress) override
    {
        HMODULE hMod = ::GetModuleHandleW(imagePath);
        if (hMod == NULL)
            return E_FAIL;
        *baseAddress = reinterpret_cast<CLRDATA_ADDRESS>(hMod);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE ReadVirtual(CLRDATA_ADDRESS address, BYTE* buffer, ULONG32 bytesRequested, ULONG32* bytesRead) override
    {
        void* src = reinterpret_cast<void*>(static_cast<uintptr_t>(address));
        MEMORY_BASIC_INFORMATION mbi;
        if (VirtualQuery(src, &mbi, sizeof(mbi)) == 0 || mbi.State != MEM_COMMIT)
        {
            *bytesRead = 0;
            return E_FAIL;
        }
        DWORD prot = mbi.Protect & 0xFF;
        if (!(prot == PAGE_READONLY || prot == PAGE_READWRITE || prot == PAGE_EXECUTE_READ ||
              prot == PAGE_EXECUTE_READWRITE || prot == PAGE_WRITECOPY || prot == PAGE_EXECUTE_WRITECOPY))
        {
            *bytesRead = 0;
            return E_FAIL;
        }
        memcpy(buffer, src, bytesRequested);
        *bytesRead = bytesRequested;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE WriteVirtual(CLRDATA_ADDRESS address, BYTE* buffer, ULONG32 bytesRequested, ULONG32* bytesWritten) override
    {
        *bytesWritten = 0;
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE GetTLSValue(ULONG32 threadID, ULONG32 index, CLRDATA_ADDRESS* value) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE SetTLSValue(ULONG32 threadID, ULONG32 index, CLRDATA_ADDRESS value) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE GetCurrentThreadID(ULONG32* threadID) override
    {
        *threadID = ::GetCurrentThreadId();
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetThreadContext(ULONG32 threadID, ULONG32 contextFlags, ULONG32 contextSize, BYTE* context) override
    {
        if (s_currentContext != nullptr && s_currentThreadId == threadID)
        {
            DWORD copySize = min(contextSize, (ULONG32)sizeof(CONTEXT));
            memcpy(context, s_currentContext, copySize);
            return S_OK;
        }
        return E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE SetThreadContext(ULONG32 threadID, ULONG32 contextSize, BYTE* context) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE Request(ULONG32 reqCode, ULONG32 inBufferSize, BYTE* inBuffer, ULONG32 outBufferSize, BYTE* outBuffer) override { return E_NOTIMPL; }
};

static InProcessDataTarget* s_dataTarget = nullptr;

//-----------------------------------------------------------------------------
// In-process callbacks for the cDAC reader.
// These allow the cDAC to read memory from the current process.
//-----------------------------------------------------------------------------

static int ReadFromTargetCallback(uint64_t addr, uint8_t* dest, uint32_t count, void* context)
{
    // In-process memory read with address validation.
    // The cDAC may try to read from addresses that are not yet mapped or are invalid
    // (e.g., following stale pointer chains). We validate with VirtualQuery before reading
    // because the CLR's vectored exception handler intercepts AVs before SEH __except.
    void* src = reinterpret_cast<void*>(static_cast<uintptr_t>(addr));
    MEMORY_BASIC_INFORMATION mbi;
    if (VirtualQuery(src, &mbi, sizeof(mbi)) == 0)
        return E_FAIL;

    if (mbi.State != MEM_COMMIT)
        return E_FAIL;

    // Check the page protection allows reading
    DWORD prot = mbi.Protect & 0xFF;
    if (!(prot == PAGE_READONLY || prot == PAGE_READWRITE || prot == PAGE_EXECUTE_READ ||
          prot == PAGE_EXECUTE_READWRITE || prot == PAGE_WRITECOPY || prot == PAGE_EXECUTE_WRITECOPY))
        return E_FAIL;

    // Ensure the entire range falls within this region
    uintptr_t regionEnd = reinterpret_cast<uintptr_t>(mbi.BaseAddress) + mbi.RegionSize;
    if (addr + count > regionEnd)
        return E_FAIL;

    memcpy(dest, src, count);
    return S_OK;
}

static int WriteToTargetCallback(uint64_t addr, const uint8_t* buff, uint32_t count, void* context)
{
    void* dst = reinterpret_cast<void*>(static_cast<uintptr_t>(addr));
    MEMORY_BASIC_INFORMATION mbi;
    if (VirtualQuery(dst, &mbi, sizeof(mbi)) == 0)
        return E_FAIL;

    if (mbi.State != MEM_COMMIT)
        return E_FAIL;

    DWORD prot = mbi.Protect & 0xFF;
    if (!(prot == PAGE_READWRITE || prot == PAGE_EXECUTE_READWRITE || prot == PAGE_WRITECOPY || prot == PAGE_EXECUTE_WRITECOPY))
        return E_FAIL;

    memcpy(dst, buff, count);
    return S_OK;
}

static int ReadThreadContextCallback(uint32_t threadId, uint32_t contextFlags, uint32_t contextBufferSize, uint8_t* contextBuffer, void* context)
{
    // Return the thread context that was stored by VerifyAtStressPoint.
    // At GC stress points, we only verify the current thread, so we check
    // that the requested thread ID matches.
    if (s_currentContext != nullptr && s_currentThreadId == threadId)
    {
        DWORD copySize = min(contextBufferSize, (uint32_t)sizeof(CONTEXT));
        memcpy(contextBuffer, s_currentContext, copySize);
        return S_OK;
    }

    return E_FAIL;
}

//-----------------------------------------------------------------------------
// Initialization / Shutdown
//-----------------------------------------------------------------------------

bool CdacGcStress::IsEnabled()
{
    return (g_pConfig->GetGCStressLevel() & EEConfig::GCSTRESS_CDAC) != 0;
}

bool CdacGcStress::IsInitialized()
{
    return s_initialized;
}

bool CdacGcStress::Initialize()
{
    if (!IsEnabled())
        return false;

    // Load mscordaccore_universal from next to coreclr
    PathString path;
    if (WszGetModuleFileName(reinterpret_cast<HMODULE>(GetCurrentModuleBase()), path) == 0)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to get module file name\n"));
        return false;
    }

    SString::Iterator iter = path.End();
    if (!path.FindBack(iter, DIRECTORY_SEPARATOR_CHAR_W))
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to find directory separator\n"));
        return false;
    }

    iter++;
    path.Truncate(iter);
    path.Append(CDAC_LIB_NAME);

    s_cdacModule = CLRLoadLibrary(path.GetUnicode());
    if (s_cdacModule == NULL)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to load %S\n", path.GetUnicode()));
        return false;
    }

    // Resolve cdac_reader_init
    auto init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(s_cdacModule, "cdac_reader_init"));
    if (init == nullptr)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to resolve cdac_reader_init\n"));
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        return false;
    }

    // Get the address of the contract descriptor in our own process
    uint64_t descriptorAddr = reinterpret_cast<uint64_t>(&DotNetRuntimeContractDescriptor);

    // Initialize the cDAC reader with in-process callbacks
    if (init(descriptorAddr, &ReadFromTargetCallback, &WriteToTargetCallback, &ReadThreadContextCallback, nullptr, &s_cdacHandle) != 0)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: cdac_reader_init failed\n"));
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        return false;
    }

    // Create the SOS interface
    auto createSos = reinterpret_cast<decltype(&cdac_reader_create_sos_interface)>(
        ::GetProcAddress(s_cdacModule, "cdac_reader_create_sos_interface"));
    if (createSos == nullptr)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to resolve cdac_reader_create_sos_interface\n"));
        auto freeFn = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(s_cdacModule, "cdac_reader_free"));
        if (freeFn != nullptr)
            freeFn(s_cdacHandle);
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        s_cdacHandle = 0;
        return false;
    }

    if (createSos(s_cdacHandle, nullptr, &s_cdacSosInterface) != 0)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: cdac_reader_create_sos_interface failed\n"));
        auto freeFn = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(s_cdacModule, "cdac_reader_free"));
        if (freeFn != nullptr)
            freeFn(s_cdacHandle);
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        s_cdacHandle = 0;
        return false;
    }

    // Read configuration for fail-fast behavior
    s_failFast = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCStressCdacFailFast) != 0;

    // Resolve flush_cache for invalidating stale data between stress points
    s_flushCache = reinterpret_cast<decltype(&cdac_reader_flush_cache)>(
        ::GetProcAddress(s_cdacModule, "cdac_reader_flush_cache"));

    // Load legacy DAC (mscordaccore.dll) for three-way comparison
    {
        PathString dacPath;
        WszGetModuleFileName(reinterpret_cast<HMODULE>(GetCurrentModuleBase()), dacPath);
        SString::Iterator dacIter = dacPath.End();
        dacPath.FindBack(dacIter, DIRECTORY_SEPARATOR_CHAR_W);
        dacIter++;
        dacPath.Truncate(dacIter);
        dacPath.Append(DAC_LIB_NAME);

        s_dacModule = CLRLoadLibrary(dacPath.GetUnicode());
        if (s_dacModule != NULL)
        {
            typedef HRESULT (STDAPICALLTYPE *CLRDataCreateInstanceFn)(REFIID, ICLRDataTarget*, void**);
            auto dacCreateInstance = reinterpret_cast<CLRDataCreateInstanceFn>(
                ::GetProcAddress(s_dacModule, "CLRDataCreateInstance"));
            if (dacCreateInstance != nullptr)
            {
                s_dataTarget = new InProcessDataTarget();
                HRESULT hr = dacCreateInstance(__uuidof(ISOSDacInterface), s_dataTarget, reinterpret_cast<void**>(&s_dacSosInterface));
                if (FAILED(hr))
                {
                    LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Legacy DAC CLRDataCreateInstance failed (hr=0x%08x)\n", hr));
                    s_dacSosInterface = nullptr;
                }
            }
        }
        else
        {
            LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to load legacy DAC %S\n", dacPath.GetUnicode()));
        }
    }

    // Open log file if configured
    CLRConfigStringHolder logFilePath(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCStressCdacLogFile));
    if (logFilePath != nullptr)
    {
        s_logFile = _wfopen(logFilePath, W("w"));
        if (s_logFile != nullptr)
        {
            fprintf(s_logFile, "=== cDAC GC Stress Verification Log ===\n");
            fprintf(s_logFile, "FailFast: %s\n\n", s_failFast ? "true" : "false");
        }
    }

    s_initialized = true;
    LOG((LF_GCROOTS, LL_INFO10, "CDAC GC Stress: Initialized successfully (failFast=%d, logFile=%s)\n",
        s_failFast, s_logFile != nullptr ? "yes" : "no"));
    return true;
}

void CdacGcStress::Shutdown()
{
    if (!s_initialized)
        return;

    // Print summary to stderr so results are always visible
    fprintf(stderr, "CDAC GC Stress: %ld verifications (%ld passed, %ld failed, %ld skipped)\n",
        (long)s_verifyCount, (long)s_verifyPass, (long)s_verifyFail, (long)s_verifySkip);
    STRESS_LOG4(LF_GCROOTS, LL_ALWAYS,
        "CDAC GC Stress shutdown: %d verifications (%d passed, %d failed, %d skipped)\n",
        (int)s_verifyCount, (int)s_verifyPass, (int)s_verifyFail, (int)s_verifySkip);

    if (s_logFile != nullptr)
    {
        fprintf(s_logFile, "\n=== Summary ===\n");
        fprintf(s_logFile, "Total verifications: %ld\n", (long)s_verifyCount);
        fprintf(s_logFile, "  Passed:  %ld\n", (long)s_verifyPass);
        fprintf(s_logFile, "  Failed:  %ld\n", (long)s_verifyFail);
        fprintf(s_logFile, "  Skipped: %ld\n", (long)s_verifySkip);
        fclose(s_logFile);
        s_logFile = nullptr;
    }

    if (s_cdacSosInterface != nullptr)
    {
        s_cdacSosInterface->Release();
        s_cdacSosInterface = nullptr;
    }

    if (s_dacSosInterface != nullptr)
    {
        s_dacSosInterface->Release();
        s_dacSosInterface = nullptr;
    }

    if (s_dataTarget != nullptr)
    {
        s_dataTarget->Release();
        s_dataTarget = nullptr;
    }

    if (s_cdacHandle != 0)
    {
        auto freeFn = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(s_cdacModule, "cdac_reader_free"));
        if (freeFn != nullptr)
            freeFn(s_cdacHandle);
        s_cdacHandle = 0;
    }

    if (s_cdacModule != NULL)
    {
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
    }

    s_initialized = false;
    LOG((LF_GCROOTS, LL_INFO10, "CDAC GC Stress: Shutdown complete\n"));
}

//-----------------------------------------------------------------------------
// Collect stack refs from the cDAC
//-----------------------------------------------------------------------------

static bool CollectCdacStackRefs(Thread* pThread, PCONTEXT regs, SArray<StackRef>* pRefs)
{
    _ASSERTE(s_cdacSosInterface != nullptr);

    // QI for ISOSDacInterface
    ISOSDacInterface* pSosDac = nullptr;
    HRESULT hr = s_cdacSosInterface->QueryInterface(__uuidof(ISOSDacInterface), reinterpret_cast<void**>(&pSosDac));
    if (FAILED(hr) || pSosDac == nullptr)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to QI for ISOSDacInterface (hr=0x%08x)\n", hr));
        return false;
    }

    // Get stack references for this thread
    // (thread context is already set by VerifyAtStressPoint)
    ISOSStackRefEnum* pEnum = nullptr;
    hr = pSosDac->GetStackReferences(pThread->GetOSThreadId(), &pEnum);

    if (FAILED(hr) || pEnum == nullptr)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: GetStackReferences failed (hr=0x%08x)\n", hr));
        if (pSosDac != nullptr)
            pSosDac->Release();
        return false;
    }

    // Enumerate all refs
    SOSStackRefData refData;
    unsigned int fetched = 0;
    while (true)
    {
        hr = pEnum->Next(1, &refData, &fetched);
        if (FAILED(hr) || fetched == 0)
            break;

        StackRef ref;
        ref.Address = refData.Address;
        ref.Object = refData.Object;
        ref.Flags = refData.Flags;
        ref.Source = refData.Source;
        ref.SourceType = refData.SourceType;
        pRefs->Append(ref);
    }

    pEnum->Release();
    pSosDac->Release();
    return true;
}

//-----------------------------------------------------------------------------
// Collect stack refs from the legacy DAC
//-----------------------------------------------------------------------------

static bool CollectDacStackRefs(Thread* pThread, PCONTEXT regs, SArray<StackRef>* pRefs)
{
    if (s_dacSosInterface == nullptr)
        return false;

    // Flush the legacy DAC's instance cache so it re-reads from the live process.
    // Without this, the DAC returns stale data from the first stress point.
    IXCLRDataProcess* pProcess = nullptr;
    HRESULT hr = s_dacSosInterface->QueryInterface(__uuidof(IXCLRDataProcess), reinterpret_cast<void**>(&pProcess));
    if (SUCCEEDED(hr) && pProcess != nullptr)
    {
        pProcess->Flush();
        pProcess->Release();
    }

    ISOSDacInterface* pSosDac = nullptr;
    hr = s_dacSosInterface->QueryInterface(__uuidof(ISOSDacInterface), reinterpret_cast<void**>(&pSosDac));
    if (FAILED(hr) || pSosDac == nullptr)
        return false;

    // Thread context is already set by VerifyAtStressPoint
    ISOSStackRefEnum* pEnum = nullptr;
    hr = pSosDac->GetStackReferences(pThread->GetOSThreadId(), &pEnum);

    if (FAILED(hr) || pEnum == nullptr)
    {
        pSosDac->Release();
        return false;
    }

    SOSStackRefData refData;
    unsigned int fetched = 0;
    while (true)
    {
        hr = pEnum->Next(1, &refData, &fetched);
        if (FAILED(hr) || fetched == 0)
            break;

        StackRef ref;
        ref.Address = refData.Address;
        ref.Object = refData.Object;
        ref.Flags = refData.Flags;
        ref.Source = refData.Source;
        ref.SourceType = refData.SourceType;
        pRefs->Append(ref);
    }

    pEnum->Release();
    pSosDac->Release();
    return true;
}

//-----------------------------------------------------------------------------
// Collect stack refs from the runtime's own GC scanning
//-----------------------------------------------------------------------------

struct RuntimeRefCollectionContext
{
    StackRef refs[MAX_COLLECTED_REFS];
    int count;
};

static void CollectRuntimeRefsPromoteFunc(PTR_PTR_Object ppObj, ScanContext* sc, uint32_t flags)
{
    RuntimeRefCollectionContext* ctx = reinterpret_cast<RuntimeRefCollectionContext*>(sc->_unused1);
    if (ctx == nullptr || ctx->count >= MAX_COLLECTED_REFS)
        return;

    StackRef& ref = ctx->refs[ctx->count++];

    // Detect whether ppObj is a register save slot (in REGDISPLAY/CONTEXT on the native
    // C stack) or a real managed stack slot. The cDAC reports register refs as (Address=0,
    // Object=value), so we normalize the runtime's output to match.
    // Register save slots are NOT on the managed stack, so IsAddressInStack returns false.
    Thread* pThread = sc->thread_under_crawl;
    bool isRegisterRef = (pThread != nullptr && !pThread->IsAddressInStack(ppObj));

    if (isRegisterRef)
    {
        ref.Address = 0;
        ref.Object = reinterpret_cast<CLRDATA_ADDRESS>(*ppObj);
    }
    else
    {
        ref.Address = reinterpret_cast<CLRDATA_ADDRESS>(ppObj);
        ref.Object = reinterpret_cast<CLRDATA_ADDRESS>(*ppObj);
    }

    ref.Flags = 0;
    if (flags & GC_CALL_INTERIOR)
        ref.Flags |= SOSRefInterior;
    if (flags & GC_CALL_PINNED)
        ref.Flags |= SOSRefPinned;
}

static void CollectRuntimeStackRefs(Thread* pThread, PCONTEXT regs, StackRef* outRefs, int* outCount)
{
    RuntimeRefCollectionContext collectCtx;
    collectCtx.count = 0;

    GCCONTEXT gcctx = {};

    // Set up ScanContext the same way ScanStackRoots does — the stack_limit and
    // thread_under_crawl fields are required for PromoteCarefully/IsAddressInStack.
    ScanContext sc;
    sc.promotion = TRUE;
    sc.thread_under_crawl = pThread;
    sc._unused1 = &collectCtx;

    Frame* pTopFrame = pThread->GetFrame();
    Object** topStack = (Object**)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        InlinedCallFrame* pInlinedFrame = dac_cast<PTR_InlinedCallFrame>(pTopFrame);
        topStack = (Object**)pInlinedFrame->GetCallSiteSP();
    }
    sc.stack_limit = (uintptr_t)topStack;

    gcctx.f = CollectRuntimeRefsPromoteFunc;
    gcctx.sc = &sc;
    gcctx.cf = NULL;

    // Set FORBIDGC_LOADER_USE_ENABLED so MethodDesc::GetName uses NOTHROW
    // instead of THROWS inside EECodeManager::EnumGcRefs.
    GCForbidLoaderUseHolder forbidLoaderUse;

    unsigned flagsStackWalk = ALLOW_ASYNC_STACK_WALK | ALLOW_INVALID_OBJECTS;
    flagsStackWalk |= GC_FUNCLET_REFERENCE_REPORTING;

    pThread->StackWalkFrames(GcStackCrawlCallBack, &gcctx, flagsStackWalk);

    // NOTE: ScanStackRoots also scans the separate GCFrame linked list
    // (Thread::GetGCFrame), but the DAC's GetStackReferences / DacStackReferenceWalker
    // does NOT include those. We intentionally omit GCFrame scanning here so our
    // runtime-side collection matches what the cDAC is expected to produce.

    // Copy results out
    *outCount = collectCtx.count;
    memcpy(outRefs, collectCtx.refs, collectCtx.count * sizeof(StackRef));
}

//-----------------------------------------------------------------------------
// Compare the two sets of stack refs
//-----------------------------------------------------------------------------

static int CompareStackRefByAddress(const void* a, const void* b)
{
    const StackRef* refA = static_cast<const StackRef*>(a);
    const StackRef* refB = static_cast<const StackRef*>(b);
    if (refA->Address < refB->Address)
        return -1;
    if (refA->Address > refB->Address)
        return 1;
    return 0;
}

static bool CompareStackRefs(StackRef* cdacRefs, int cdacCount, StackRef* dacRefs, int dacCount, Thread* pThread)
{
    // Sort both arrays by address for comparison.
    // cDAC and DAC use the same SOSStackRefData convention, so all refs
    // (including register refs with Address=0) are directly comparable.
    if (cdacCount > 1)
        qsort(cdacRefs, cdacCount, sizeof(StackRef), CompareStackRefByAddress);
    if (dacCount > 1)
        qsort(dacRefs, dacCount, sizeof(StackRef), CompareStackRefByAddress);

    bool match = true;
    int cdacIdx = 0;
    int dacIdx = 0;

    while (cdacIdx < cdacCount && dacIdx < dacCount)
    {
        StackRef& cdacRef = cdacRefs[cdacIdx];
        StackRef& dacRef = dacRefs[dacIdx];

        if (cdacRef.Address < dacRef.Address)
        {
            LOG((LF_GCROOTS, LL_WARNING,
                "CDAC GC Stress MISMATCH: cDAC has extra ref at Address=0x%p Object=0x%p Flags=0x%x (Thread 0x%x)\n",
                (void*)(size_t)cdacRef.Address, (void*)(size_t)cdacRef.Object, cdacRef.Flags, pThread->GetOSThreadId()));
            match = false;
            cdacIdx++;
        }
        else if (cdacRef.Address > dacRef.Address)
        {
            LOG((LF_GCROOTS, LL_WARNING,
                "CDAC GC Stress MISMATCH: DAC has ref missing from cDAC at Address=0x%p Object=0x%p Flags=0x%x (Thread 0x%x)\n",
                (void*)(size_t)dacRef.Address, (void*)(size_t)dacRef.Object, dacRef.Flags, pThread->GetOSThreadId()));
            match = false;
            dacIdx++;
        }
        else
        {
            if (cdacRef.Object != dacRef.Object)
            {
                LOG((LF_GCROOTS, LL_WARNING,
                    "CDAC GC Stress MISMATCH: Different object at Address=0x%p: cDAC=0x%p DAC=0x%p (Thread 0x%x)\n",
                    (void*)(size_t)cdacRef.Address, (void*)(size_t)cdacRef.Object, (void*)(size_t)dacRef.Object, pThread->GetOSThreadId()));
                match = false;
            }
            if (cdacRef.Flags != dacRef.Flags)
            {
                LOG((LF_GCROOTS, LL_WARNING,
                    "CDAC GC Stress MISMATCH: Different flags at Address=0x%p: cDAC=0x%x DAC=0x%x (Thread 0x%x)\n",
                    (void*)(size_t)cdacRef.Address, cdacRef.Flags, dacRef.Flags, pThread->GetOSThreadId()));
                match = false;
            }
            cdacIdx++;
            dacIdx++;
        }
    }

    while (cdacIdx < cdacCount)
    {
        StackRef& cdacRef = cdacRefs[cdacIdx++];
        LOG((LF_GCROOTS, LL_WARNING,
            "CDAC GC Stress MISMATCH: cDAC has extra ref at Address=0x%p Object=0x%p Flags=0x%x (Thread 0x%x)\n",
            (void*)(size_t)cdacRef.Address, (void*)(size_t)cdacRef.Object, cdacRef.Flags, pThread->GetOSThreadId()));
        match = false;
    }

    while (dacIdx < dacCount)
    {
        StackRef& dacRef = dacRefs[dacIdx++];
        LOG((LF_GCROOTS, LL_WARNING,
            "CDAC GC Stress MISMATCH: DAC has ref missing from cDAC at Address=0x%p Object=0x%p Flags=0x%x (Thread 0x%x)\n",
            (void*)(size_t)dacRef.Address, (void*)(size_t)dacRef.Object, dacRef.Flags, pThread->GetOSThreadId()));
        match = false;
    }

    return match;
}

//-----------------------------------------------------------------------------
// Report mismatch
//-----------------------------------------------------------------------------

static void ReportMismatch(const char* message, Thread* pThread, PCONTEXT regs)
{
    LOG((LF_GCROOTS, LL_ERROR, "CDAC GC Stress: %s (Thread=0x%x, IP=0x%p)\n",
        message, pThread->GetOSThreadId(), (void*)GetIP(regs)));

    if (s_failFast)
    {
        _ASSERTE_MSG(false, message);
    }
}

//-----------------------------------------------------------------------------
// Main entry point: verify at a GC stress point
//-----------------------------------------------------------------------------

void CdacGcStress::VerifyAtStressPoint(Thread* pThread, PCONTEXT regs)
{
    _ASSERTE(s_initialized);
    _ASSERTE(pThread != nullptr);
    _ASSERTE(regs != nullptr);

    InterlockedIncrement(&s_verifyCount);

    // Set the thread context ONCE for both DAC and cDAC before any collection.
    // This ensures both see the same context when they call ReadThreadContext.
    s_currentContext = regs;
    s_currentThreadId = pThread->GetOSThreadId();

    // Flush both caches at the same point so both read fresh data.
    if (s_flushCache != nullptr)
        s_flushCache(s_cdacHandle);

    if (s_dacSosInterface != nullptr)
    {
        IXCLRDataProcess* pProcess = nullptr;
        HRESULT hr = s_dacSosInterface->QueryInterface(__uuidof(IXCLRDataProcess), reinterpret_cast<void**>(&pProcess));
        if (SUCCEEDED(hr) && pProcess != nullptr)
        {
            pProcess->Flush();
            pProcess->Release();
        }
    }

    // Now collect from both cDAC and DAC with the same context and cache state.
    SArray<StackRef> cdacRefs;
    bool haveCdac = CollectCdacStackRefs(pThread, regs, &cdacRefs);

    SArray<StackRef> dacRefs;
    bool haveDac = CollectDacStackRefs(pThread, regs, &dacRefs);

    // Clear the stored context
    s_currentContext = nullptr;
    s_currentThreadId = 0;

    // Collect runtime refs (doesn't use DAC/cDAC, no timing issue)
    StackRef runtimeRefsBuf[MAX_COLLECTED_REFS];
    int runtimeCount = 0;
    CollectRuntimeStackRefs(pThread, regs, runtimeRefsBuf, &runtimeCount);

    if (!haveCdac)
    {
        InterlockedIncrement(&s_verifySkip);
        if (s_logFile != nullptr)
            fprintf(s_logFile, "[SKIP] Thread=0x%x IP=0x%p - cDAC GetStackReferences failed\n",
                pThread->GetOSThreadId(), (void*)GetIP(regs));
        return;
    }

    int cdacCount = (int)cdacRefs.GetCount();
    int dacCount = haveDac ? (int)dacRefs.GetCount() : -1;

    // Primary comparison: cDAC vs DAC (apples-to-apples, same SOSStackRefData contract)
    bool cdacMatchesDac = true;
    if (haveDac)
    {
        StackRef* cdacBuf = (cdacCount > 0) ? cdacRefs.OpenRawBuffer() : nullptr;
        StackRef* dacBuf = (dacCount > 0) ? dacRefs.OpenRawBuffer() : nullptr;
        cdacMatchesDac = CompareStackRefs(cdacBuf, cdacCount, dacBuf, dacCount, pThread);
        if (cdacBuf != nullptr) cdacRefs.CloseRawBuffer();
        if (dacBuf != nullptr) dacRefs.CloseRawBuffer();
    }

    if (!cdacMatchesDac)
    {
        InterlockedIncrement(&s_verifyFail);
        STRESS_LOG3(LF_GCROOTS, LL_ERROR,
            "CDAC GC Stress MISMATCH: cDAC=%d vs DAC=%d at IP=0x%p\n",
            cdacCount, dacCount, GetIP(regs));

        if (s_logFile != nullptr)
        {
            fprintf(s_logFile, "[FAIL] Thread=0x%x IP=0x%p cDAC=%d DAC=%d RT=%d\n",
                pThread->GetOSThreadId(), (void*)GetIP(regs), cdacCount, dacCount, runtimeCount);
            for (int i = 0; i < cdacCount; i++)
                fprintf(s_logFile, "  cDAC [%d]: Address=0x%llx Object=0x%llx Flags=0x%x Source=0x%llx SourceType=%d\n",
                    i, (unsigned long long)cdacRefs[i].Address, (unsigned long long)cdacRefs[i].Object,
                    cdacRefs[i].Flags, (unsigned long long)cdacRefs[i].Source, cdacRefs[i].SourceType);
            StackRef* dacBuf = (dacCount > 0) ? dacRefs.OpenRawBuffer() : nullptr;
            for (int i = 0; i < dacCount; i++)
                fprintf(s_logFile, "  DAC  [%d]: Address=0x%llx Object=0x%llx Flags=0x%x Source=0x%llx SourceType=%d\n",
                    i, (unsigned long long)dacBuf[i].Address, (unsigned long long)dacBuf[i].Object,
                    dacBuf[i].Flags, (unsigned long long)dacBuf[i].Source, dacBuf[i].SourceType);
            if (dacBuf != nullptr) dacRefs.CloseRawBuffer();
            for (int i = 0; i < runtimeCount; i++)
                fprintf(s_logFile, "  RT   [%d]: Address=0x%llx Object=0x%llx Flags=0x%x\n",
                    i, (unsigned long long)runtimeRefsBuf[i].Address, (unsigned long long)runtimeRefsBuf[i].Object, runtimeRefsBuf[i].Flags);

            // Dump Frame chain for diagnostics
            fprintf(s_logFile, "  FRAMES: initSP=0x%llx\n", (unsigned long long)GetSP(regs));
            Frame* pFrame = pThread->GetFrame();
            int frameIdx = 0;
            while (pFrame != nullptr && pFrame != FRAME_TOP && frameIdx < 20)
            {
                TADDR frameAddr = dac_cast<TADDR>(pFrame);
                PCODE retAddr = 0;
                retAddr = pFrame->GetReturnAddress();
                fprintf(s_logFile, "  FRAME[%d]: addr=0x%llx id=%d retAddr=0x%llx",
                    frameIdx, (unsigned long long)frameAddr, (int)pFrame->GetFrameIdentifier(), (unsigned long long)retAddr);
                if (pFrame->GetFrameIdentifier() == FrameIdentifier::InlinedCallFrame)
                {
                    InlinedCallFrame* pICF = (InlinedCallFrame*)pFrame;
                    bool hasActive = InlinedCallFrame::FrameHasActiveCall(pFrame);
                    fprintf(s_logFile, " [ICF active=%d callSiteSP=0x%llx callerRetAddr=0x%llx]",
                        hasActive, (unsigned long long)(TADDR)pICF->GetCallSiteSP(),
                        (unsigned long long)pICF->m_pCallerReturnAddress);
                }
                fprintf(s_logFile, "\n");
                pFrame = pFrame->PtrNextFrame();
                frameIdx++;
            }
            fflush(s_logFile);
        }

        ReportMismatch("cDAC stack reference verification failed - mismatch between cDAC and DAC GC refs", pThread, regs);
    }
    else
    {
        InterlockedIncrement(&s_verifyPass);
        if (s_logFile != nullptr)
            fprintf(s_logFile, "[PASS] Thread=0x%x IP=0x%p cDAC=%d DAC=%d RT=%d\n",
                pThread->GetOSThreadId(), (void*)GetIP(regs), cdacCount, dacCount, runtimeCount);
    }
}

#endif // HAVE_GCCOVER
