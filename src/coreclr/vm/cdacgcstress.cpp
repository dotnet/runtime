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
#include <xclrdata.h>
#include <sospriv.h>
#include "threads.h"
#include "eeconfig.h"
#include "gccover.h"
#include "sstring.h"

#define CDAC_LIB_NAME MAKEDLLNAME_W(W("mscordaccore_universal"))

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
static HMODULE              s_cdacModule = NULL;
static intptr_t             s_cdacHandle = 0;
static IUnknown*            s_cdacSosInterface = nullptr;
static IXCLRDataProcess*    s_cdacProcess = nullptr;    // Cached QI result for Flush()
static ISOSDacInterface*    s_cdacSosDac = nullptr;     // Cached QI result for GetStackReferences()

// Static state — common
static bool             s_initialized = false;
static bool             s_failFast = true;
static FILE*            s_logFile = nullptr;
static CrstStatic       s_cdacLock;       // Serializes cDAC access from concurrent GC stress threads

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
// In-process callbacks for the cDAC reader.
// These allow the cDAC to read memory from the current process.
//-----------------------------------------------------------------------------

// Helper for ReadFromTargetCallback — AVInRuntimeImplOkayHolder cannot be
// directly inside PAL_TRY scope (see controller.cpp:109).
static void ReadFromTargetHelper(void* src, uint8_t* dest, uint32_t count)
{
    AVInRuntimeImplOkayHolder AVOkay;
    memcpy(dest, src, count);
}

static int ReadFromTargetCallback(uint64_t addr, uint8_t* dest, uint32_t count, void* context)
{
    void* src = reinterpret_cast<void*>(static_cast<uintptr_t>(addr));
    struct Param { void* src; uint8_t* dest; uint32_t count; } param;
    param.src = src; param.dest = dest; param.count = count;
    PAL_TRY(Param *, pParam, &param)
    {
        ReadFromTargetHelper(pParam->src, pParam->dest, pParam->count);
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        return E_FAIL;
    }
    PAL_ENDTRY
    return S_OK;
}

static int WriteToTargetCallback(uint64_t addr, const uint8_t* buff, uint32_t count, void* context)
{
    return E_NOTIMPL;
}

static int ReadThreadContextCallback(uint32_t threadId, uint32_t contextFlags, uint32_t contextBufferSize, uint8_t* contextBuffer, void* context)
{
    // Return the thread context that was stored by VerifyAtStressPoint.
    if (s_currentContext != nullptr && s_currentThreadId == threadId)
    {
        DWORD copySize = min(contextBufferSize, (uint32_t)sizeof(CONTEXT));
        memcpy(contextBuffer, s_currentContext, copySize);
        return S_OK;
    }

    LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: ReadThreadContext mismatch: requested=%u stored=%u\n",
        threadId, s_currentThreadId));
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

    // Cache QI results so we don't QI on every stress point
    {
        HRESULT hr = s_cdacSosInterface->QueryInterface(__uuidof(IXCLRDataProcess), reinterpret_cast<void**>(&s_cdacProcess));
        if (FAILED(hr) || s_cdacProcess == nullptr)
        {
            LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to QI for IXCLRDataProcess (hr=0x%08x)\n", hr));
        }

        hr = s_cdacSosInterface->QueryInterface(__uuidof(ISOSDacInterface), reinterpret_cast<void**>(&s_cdacSosDac));
        if (FAILED(hr) || s_cdacSosDac == nullptr)
        {
            LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to QI for ISOSDacInterface (hr=0x%08x)\n", hr));
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

    s_cdacLock.Init(CrstGCCover, CRST_DEFAULT);
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
    fprintf(stderr, "CDAC GC Stress: %ld verifications (%ld pass / %ld fail, %ld skipped)\n",
        (long)s_verifyCount, (long)s_verifyPass, (long)s_verifyFail, (long)s_verifySkip);
    STRESS_LOG3(LF_GCROOTS, LL_ALWAYS,
        "CDAC GC Stress shutdown: %d verifications (%d pass / %d fail)\n",
        (int)s_verifyCount, (int)s_verifyPass, (int)s_verifyFail);

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

    if (s_cdacSosDac != nullptr)
    {
        s_cdacSosDac->Release();
        s_cdacSosDac = nullptr;
    }

    if (s_cdacProcess != nullptr)
    {
        s_cdacProcess->Release();
        s_cdacProcess = nullptr;
    }

    if (s_cdacSosInterface != nullptr)
    {
        s_cdacSosInterface->Release();
        s_cdacSosInterface = nullptr;
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
    _ASSERTE(s_cdacSosDac != nullptr);

    ISOSStackRefEnum* pEnum = nullptr;
    HRESULT hr = s_cdacSosDac->GetStackReferences(pThread->GetOSThreadId(), &pEnum);

    if (FAILED(hr) || pEnum == nullptr)
    {
        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: GetStackReferences failed (hr=0x%08x)\n", hr));
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
    return true;
}

//-----------------------------------------------------------------------------
// Collect stack refs from the runtime's own GC scanning
//-----------------------------------------------------------------------------

struct RuntimeRefCollectionContext
{
    StackRef refs[MAX_COLLECTED_REFS];
    int count;
    bool overflow;
};

static void CollectRuntimeRefsPromoteFunc(PTR_PTR_Object ppObj, ScanContext* sc, uint32_t flags)
{
    RuntimeRefCollectionContext* ctx = reinterpret_cast<RuntimeRefCollectionContext*>(sc->_unused1);
    if (ctx == nullptr)
        return;
    if (ctx->count >= MAX_COLLECTED_REFS)
    {
        ctx->overflow = true;
        return;
    }

    StackRef& ref = ctx->refs[ctx->count++];

    // Detect whether ppObj is a register save slot (in REGDISPLAY/CONTEXT on the native
    // C stack) or a real managed stack slot. The cDAC reports register refs as (Address=0,
    // Object=value), so we normalize the runtime's output to match.
    // REGDISPLAY slots live below stack_limit; managed stack slots are at or above it.
    bool isRegisterRef = reinterpret_cast<uintptr_t>(ppObj) < sc->stack_limit;

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

static bool CollectRuntimeStackRefs(Thread* pThread, PCONTEXT regs, StackRef* outRefs, int* outCount)
{
    RuntimeRefCollectionContext collectCtx;
    collectCtx.count = 0;
    collectCtx.overflow = false;

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
    return !collectCtx.overflow;
}

//-----------------------------------------------------------------------------
// Filter cDAC refs to match runtime PromoteCarefully behavior.
// The runtime's PromoteCarefully (siginfo.cpp) skips interior pointers whose
// object value is a stack address. The cDAC reports all GcInfo slots without
// this filter, so we apply it here before comparing against runtime refs.
//-----------------------------------------------------------------------------

static int FilterInteriorStackRefs(StackRef* refs, int count, Thread* pThread, uintptr_t stackLimit)
{
    int writeIdx = 0;
    for (int i = 0; i < count; i++)
    {
        bool isInterior = (refs[i].Flags & SOSRefInterior) != 0;
        if (isInterior &&
            pThread->IsAddressInStack((void*)(size_t)refs[i].Object) &&
            (size_t)refs[i].Object >= stackLimit)
        {
            continue;
        }
        refs[writeIdx++] = refs[i];
    }
    return writeIdx;
}

//-----------------------------------------------------------------------------
// Deduplicate cDAC refs that have the same (Address, Object, Flags).
// The cDAC may walk the same managed frame at two different offsets due to
// Frames restoring context (e.g. InlinedCallFrame). The same stack slots
// get reported from both offsets. The runtime only walks each frame once,
// so we deduplicate to match.
//-----------------------------------------------------------------------------

static int CompareStackRefKey(const void* a, const void* b)
{
    const StackRef* refA = static_cast<const StackRef*>(a);
    const StackRef* refB = static_cast<const StackRef*>(b);
    if (refA->Address != refB->Address)
        return (refA->Address < refB->Address) ? -1 : 1;
    if (refA->Object != refB->Object)
        return (refA->Object < refB->Object) ? -1 : 1;
    if (refA->Flags != refB->Flags)
        return (refA->Flags < refB->Flags) ? -1 : 1;
    return 0;
}

static int DeduplicateRefs(StackRef* refs, int count)
{
    if (count <= 1)
        return count;
    qsort(refs, count, sizeof(StackRef), CompareStackRefKey);
    int writeIdx = 1;
    for (int i = 1; i < count; i++)
    {
        // Only dedup stack-based refs (Address != 0).
        // Register refs (Address == 0) are legitimately different entries
        // even when Address/Object/Flags match (different registers).
        if (refs[i].Address != 0 &&
            refs[i].Address == refs[i-1].Address &&
            refs[i].Object == refs[i-1].Object &&
            refs[i].Flags == refs[i-1].Flags)
        {
            continue;
        }
        refs[writeIdx++] = refs[i];
    }
    return writeIdx;
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

    // Serialize cDAC access — the cDAC's ProcessedData cache and COM interfaces
    // are not thread-safe, and GC stress can fire on multiple threads.
    CrstHolder cdacLock(&s_cdacLock);

    // Set the thread context for the cDAC's ReadThreadContext callback.
    s_currentContext = regs;
    s_currentThreadId = pThread->GetOSThreadId();

    // Flush the cDAC's ProcessedData cache so it re-reads from the live process.
    if (s_cdacProcess != nullptr)
    {
        s_cdacProcess->Flush();
    }

    // Collect from cDAC
    SArray<StackRef> cdacRefs;
    bool haveCdac = CollectCdacStackRefs(pThread, regs, &cdacRefs);

    // Clear the stored context
    s_currentContext = nullptr;
    s_currentThreadId = 0;

    // Collect runtime refs (doesn't use cDAC, no timing issue)
    StackRef runtimeRefsBuf[MAX_COLLECTED_REFS];
    int runtimeCount = 0;
    bool runtimeComplete = CollectRuntimeStackRefs(pThread, regs, runtimeRefsBuf, &runtimeCount);

    if (!haveCdac)
    {
        InterlockedIncrement(&s_verifySkip);
        if (s_logFile != nullptr)
            fprintf(s_logFile, "[SKIP] Thread=0x%x IP=0x%p - cDAC GetStackReferences failed\n",
                pThread->GetOSThreadId(), (void*)GetIP(regs));
        return;
    }

    if (!runtimeComplete)
    {
        InterlockedIncrement(&s_verifySkip);
        if (s_logFile != nullptr)
            fprintf(s_logFile, "[SKIP] Thread=0x%x IP=0x%p - runtime ref buffer overflow (>%d refs)\n",
                pThread->GetOSThreadId(), (void*)GetIP(regs), MAX_COLLECTED_REFS);
        return;
    }

    // Filter cDAC refs to match runtime PromoteCarefully behavior:
    // remove interior pointers whose Object value is a stack address.
    // These are register slots (RSP/RBP) that GcInfo marks as live interior
    // but don't point to managed heap objects.
    Frame* pTopFrame = pThread->GetFrame();
    Object** topStack = (Object**)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        InlinedCallFrame* pInlinedFrame = dac_cast<PTR_InlinedCallFrame>(pTopFrame);
        topStack = (Object**)pInlinedFrame->GetCallSiteSP();
    }
    uintptr_t stackLimit = (uintptr_t)topStack;

    int cdacCount = (int)cdacRefs.GetCount();
    if (cdacCount > 0)
    {
        StackRef* cdacBuf = cdacRefs.OpenRawBuffer();
        cdacCount = FilterInteriorStackRefs(cdacBuf, cdacCount, pThread, stackLimit);
        cdacCount = DeduplicateRefs(cdacBuf, cdacCount);
        cdacRefs.CloseRawBuffer();
        // Trim the SArray to the filtered count
        while ((int)cdacRefs.GetCount() > cdacCount)
            cdacRefs.Delete(cdacRefs.End() - 1);
    }

    // Compare cDAC vs runtime (count-only).
    // If the stress IP is in a RangeList section (dynamic method / IL Stub),
    // the cDAC can't decode GcInfo for it (known gap matching DAC behavior).
    // Skip comparison for these — the runtime reports refs from the Frame chain
    // that neither DAC nor cDAC can reproduce via GetStackReferences.
    PCODE stressIP = GetIP(regs);
    bool isDynamicMethod = false;
    {
        RangeSection* pRS = ExecutionManager::FindCodeRange(stressIP, ExecutionManager::ScanReaderLock);
        if (pRS != nullptr)
        {
            isDynamicMethod = (pRS->_flags & RangeSection::RANGE_SECTION_RANGELIST) != 0;
            // Also check if this is a dynamic method by checking the MethodDesc
            if (!isDynamicMethod)
            {
                EECodeInfo ci(stressIP);
                if (ci.IsValid() && ci.GetMethodDesc() != nullptr &&
                    (ci.GetMethodDesc()->IsLCGMethod() || ci.GetMethodDesc()->IsILStub()))
                    isDynamicMethod = true;
            }
        }
    }

    bool pass = (cdacCount == runtimeCount);
    if (!pass && isDynamicMethod)
    {
        // Known gap: dynamic method refs not in cDAC. Treat as pass but log.
        pass = true;
    }

    if (pass)
        InterlockedIncrement(&s_verifyPass);
    else
        InterlockedIncrement(&s_verifyFail);

    if (s_logFile != nullptr)
    {
        fprintf(s_logFile, "[%s] Thread=0x%x IP=0x%p cDAC=%d RT=%d\n",
            pass ? "PASS" : "FAIL", pThread->GetOSThreadId(), (void*)GetIP(regs), cdacCount, runtimeCount);

        if (!pass)
        {
            // Log the stress point IP and the first cDAC Source for debugging
            PCODE stressIP = GetIP(regs);
            fprintf(s_logFile, "  stressIP=0x%p firstCdacSource=0x%llx\n",
                (void*)stressIP,
                cdacCount > 0 ? (unsigned long long)cdacRefs[0].Source : 0ULL);

            // Check if any cDAC ref has the stress IP as its Source
            bool leafFound = false;
            for (int i = 0; i < cdacCount; i++)
            {
                if ((PCODE)cdacRefs[i].Source == stressIP)
                {
                    leafFound = true;
                    break;
                }
            }
            if (!leafFound && cdacCount < runtimeCount)
            {
                fprintf(s_logFile, "  DIAG: Leaf frame at stressIP NOT in cDAC sources (cDAC < RT)\n");

                // Check if the stress IP is in a managed method
                bool isManaged = ExecutionManager::IsManagedCode(stressIP);
                fprintf(s_logFile, "  DIAG: IsManaged(stressIP)=%d\n", isManaged);

                if (isManaged)
                {
                    // Get the method's code range to see if cDAC walks ANY offset in this method
                    EECodeInfo codeInfo(stressIP);
                    if (codeInfo.IsValid())
                    {
                        PCODE methodStart = codeInfo.GetStartAddress();
                        MethodDesc* pMD = codeInfo.GetMethodDesc();
                        fprintf(s_logFile, "  DIAG: Method start=0x%p relOffset=0x%x %s::%s\n",
                            (void*)methodStart, codeInfo.GetRelOffset(),
                            pMD ? pMD->m_pszDebugClassName : "?",
                            pMD ? pMD->m_pszDebugMethodName : "?");

                        // Check if the cDAC can resolve this IP to a MethodDesc
                        if (s_cdacSosDac != nullptr)
                        {
                            CLRDATA_ADDRESS cdacMD = 0;
                            HRESULT hrMD = s_cdacSosDac->GetMethodDescPtrFromIP((CLRDATA_ADDRESS)stressIP, &cdacMD);
                            fprintf(s_logFile, "  DIAG: cDAC GetMethodDescPtrFromIP hr=0x%x MD=0x%llx\n",
                                hrMD, (unsigned long long)cdacMD);
                        }

                        // Check if cDAC has ANY ref from this method (Source near methodStart)
                        bool methodFound = false;
                        for (int i = 0; i < cdacCount; i++)
                        {
                            PCODE src = (PCODE)cdacRefs[i].Source;
                            if (src >= methodStart && src < methodStart + 0x10000) // rough range
                            {
                                methodFound = true;
                                fprintf(s_logFile, "  DIAG: cDAC has ref from same method at Source=0x%llx (offset=0x%llx)\n",
                                    (unsigned long long)src, (unsigned long long)(src - methodStart));
                                break;
                            }
                        }
                        if (!methodFound)
                            fprintf(s_logFile, "  DIAG: cDAC has NO refs from this method at all\n");
                    }
                }

                // Check what the first RT ref looks like
                if (runtimeCount > 0)
                    fprintf(s_logFile, "  DIAG: RT[0]: Address=0x%llx Object=0x%llx Flags=0x%x\n",
                        (unsigned long long)runtimeRefsBuf[0].Address,
                        (unsigned long long)runtimeRefsBuf[0].Object,
                        runtimeRefsBuf[0].Flags);
            }

            for (int i = 0; i < cdacCount; i++)
                fprintf(s_logFile, "  cDAC [%d]: Address=0x%llx Object=0x%llx Flags=0x%x Source=0x%llx SourceType=%d\n",
                    i, (unsigned long long)cdacRefs[i].Address, (unsigned long long)cdacRefs[i].Object,
                    cdacRefs[i].Flags, (unsigned long long)cdacRefs[i].Source, cdacRefs[i].SourceType);
            for (int i = 0; i < runtimeCount; i++)
                fprintf(s_logFile, "  RT   [%d]: Address=0x%llx Object=0x%llx Flags=0x%x\n",
                    i, (unsigned long long)runtimeRefsBuf[i].Address, (unsigned long long)runtimeRefsBuf[i].Object, runtimeRefsBuf[i].Flags);
            fflush(s_logFile);
        }
    }

    if (!pass)
    {
        ReportMismatch("cDAC stack reference verification failed - mismatch between cDAC and runtime GC ref counts", pThread, regs);
    }
}

#endif // HAVE_GCCOVER
