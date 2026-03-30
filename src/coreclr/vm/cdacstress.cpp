// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CdacStress.cpp
//
// Implements in-process cDAC loading and stack reference verification.
// Enabled via DOTNET_CdacStress (bit flags) or legacy DOTNET_GCStress=0x20.
// At each enabled stress point we:
//   1. Ask the cDAC to enumerate stack GC references via ISOSDacInterface::GetStackReferences
//   2. Ask the runtime to enumerate stack GC references via StackWalkFrames + GcInfoDecoder
//   3. Compare the two sets and report any mismatches
//

#include "common.h"

#ifdef HAVE_GCCOVER

#include "cdacstress.h"
#include "../../native/managed/cdac/inc/cdac_reader.h"
#include "../../debug/datadescriptor-shared/inc/contract-descriptor.h"
#include <xclrdata.h>
#include <sospriv.h>
#include "threads.h"
#include "eeconfig.h"
#include "gccover.h"
#include "sstring.h"
#include "exinfo.h"

// Forward-declare the 3-param GcEnumObject used as a GCEnumCallback.
// Defined in gcenv.ee.common.cpp; not exposed in any header.
extern void GcEnumObject(LPVOID pData, OBJECTREF *pObj, uint32_t flags);

#define CDAC_LIB_NAME MAKEDLLNAME_W(W("mscordaccore_universal"))

// Represents a single GC stack reference for comparison purposes.
struct StackRef
{
    CLRDATA_ADDRESS Address;    // Location on stack holding the ref
    CLRDATA_ADDRESS Object;     // The object pointer value
    unsigned int    Flags;      // SOSRefFlags (interior, pinned)
    CLRDATA_ADDRESS Source;     // IP or Frame that owns this ref
    int             SourceType; // SOS_StackSourceIP or SOS_StackSourceFrame
    int             Register;   // Register number (cDAC only)
    int             Offset;     // Register offset (cDAC only)
    CLRDATA_ADDRESS StackPointer; // Stack pointer at this ref (cDAC only)
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

// Static state — legacy DAC (for three-way comparison)
static HMODULE              s_dacModule = NULL;
static ISOSDacInterface*    s_dacSosDac = nullptr;
static IXCLRDataProcess*    s_dacProcess = nullptr;

// Static state — common
static bool             s_initialized = false;
static bool             s_failFast = true;
static DWORD            s_step = 1;       // Verify every Nth stress point (1=every point)
static DWORD            s_cdacStressLevel = 0; // Resolved CdacStressFlags
static FILE*            s_logFile = nullptr;
static CrstStatic       s_cdacLock;       // Serializes cDAC access from concurrent GC stress threads

// Unique-stack filtering: hash set of previously seen stack traces.
// Protected by s_cdacLock (already held during VerifyAtStressPoint).

static SHash<NoRemoveSHashTraits<SetSHashTraits<SIZE_T>>>* s_seenStacks = nullptr;

// Thread-local reentrancy guard — prevents infinite recursion when
// allocations inside VerifyAtStressPoint trigger VerifyAtAllocPoint.
thread_local bool       t_inVerification = false;

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
// Minimal ICLRDataTarget implementation for loading the legacy DAC in-process.
// Routes ReadVirtual/GetThreadContext to the same callbacks as the cDAC.
//-----------------------------------------------------------------------------
class InProcessDataTarget : public ICLRDataTarget, public ICLRRuntimeLocator
{
    volatile LONG m_refCount;
public:
    InProcessDataTarget() : m_refCount(1) {}
    virtual ~InProcessDataTarget() = default;

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppObj) override
    {
        if (riid == IID_IUnknown || riid == __uuidof(ICLRDataTarget))
        {
            *ppObj = static_cast<ICLRDataTarget*>(this);
            AddRef();
            return S_OK;
        }
        if (riid == __uuidof(ICLRRuntimeLocator))
        {
            *ppObj = static_cast<ICLRRuntimeLocator*>(this);
            AddRef();
            return S_OK;
        }
        *ppObj = nullptr;
        return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return InterlockedIncrement(&m_refCount); }
    ULONG STDMETHODCALLTYPE Release() override
    {
        ULONG c = InterlockedDecrement(&m_refCount);
        if (c == 0) delete this;
        return c;
    }

    // ICLRRuntimeLocator — provides the CLR base address directly so the DAC
    // does not fall back to GetImageBase (which needs GetModuleHandleW, unavailable on Linux).
    HRESULT STDMETHODCALLTYPE GetRuntimeBase(CLRDATA_ADDRESS* baseAddress) override
    {
        *baseAddress = (CLRDATA_ADDRESS)GetCurrentModuleBase();
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetMachineType(ULONG32* machineType) override
    {
#ifdef TARGET_AMD64
        *machineType = IMAGE_FILE_MACHINE_AMD64;
#elif defined(TARGET_ARM64)
        *machineType = IMAGE_FILE_MACHINE_ARM64;
#elif defined(TARGET_X86)
        *machineType = IMAGE_FILE_MACHINE_I386;
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
        // Not needed — the DAC uses ICLRRuntimeLocator::GetRuntimeBase() instead.
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE ReadVirtual(CLRDATA_ADDRESS address, BYTE* buffer, ULONG32 bytesRequested, ULONG32* bytesRead) override
    {
        int hr = ReadFromTargetCallback((uint64_t)address, buffer, bytesRequested, nullptr);
        if (hr == S_OK && bytesRead != nullptr)
            *bytesRead = bytesRequested;
        return hr;
    }

    HRESULT STDMETHODCALLTYPE WriteVirtual(CLRDATA_ADDRESS, BYTE*, ULONG32, ULONG32*) override { return E_NOTIMPL; }

    HRESULT STDMETHODCALLTYPE GetTLSValue(ULONG32 threadId, ULONG32 index, CLRDATA_ADDRESS* value) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE SetTLSValue(ULONG32 threadId, ULONG32 index, CLRDATA_ADDRESS value) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE GetCurrentThreadID(ULONG32* threadId) override
    {
        *threadId = ::GetCurrentThreadId();
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetThreadContext(ULONG32 threadId, ULONG32 contextFlags, ULONG32 contextSize, BYTE* contextBuffer) override
    {
        return ReadThreadContextCallback(threadId, contextFlags, contextSize, contextBuffer, nullptr);
    }

    HRESULT STDMETHODCALLTYPE SetThreadContext(ULONG32, ULONG32, BYTE*) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE Request(ULONG32, ULONG32, BYTE*, ULONG32, BYTE*) override { return E_NOTIMPL; }
};

//-----------------------------------------------------------------------------
// Initialization / Shutdown
//-----------------------------------------------------------------------------

bool CdacStress::IsEnabled()
{
    // Check DOTNET_CdacStress first (new config)
    DWORD cdacStress = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStress);
    if (cdacStress != 0)
        return true;

    // Fall back to legacy DOTNET_GCStress=0x20
    return (g_pConfig->GetGCStressLevel() & EEConfig::GCSTRESS_CDAC) != 0;
}

bool CdacStress::IsInitialized()
{
    return s_initialized;
}

DWORD GetCdacStressLevel()
{
    return s_cdacStressLevel;
}

bool CdacStress::IsUniqueEnabled()
{
    return (s_cdacStressLevel & CDACSTRESS_UNIQUE) != 0;
}

bool CdacStress::Initialize()
{
    if (!IsEnabled())
        return false;

    // Resolve the stress level from DOTNET_CdacStress or legacy GCSTRESS_CDAC
    DWORD cdacStress = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStress);
    if (cdacStress != 0)
    {
        s_cdacStressLevel = cdacStress;
    }
    else
    {
        // Legacy: GCSTRESS_CDAC maps to allocation-point + reference verification
        s_cdacStressLevel = CDACSTRESS_ALLOC | CDACSTRESS_REFS;
    }

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
    s_failFast = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStressFailFast) != 0;

    // Read step interval for throttling verifications
    s_step = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStressStep);
    if (s_step == 0)
        s_step = 1;

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
            LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Failed to QI for ISOSDacInterface (hr=0x%08x) - cannot verify\n", hr));
            if (s_cdacProcess != nullptr)
            {
                s_cdacProcess->Release();
                s_cdacProcess = nullptr;
            }
            auto freeFn = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(s_cdacModule, "cdac_reader_free"));
            if (freeFn != nullptr)
                freeFn(s_cdacHandle);
            ::FreeLibrary(s_cdacModule);
            s_cdacModule = NULL;
            s_cdacHandle = 0;
            return false;
        }
    }

    // Open log file if configured
    CLRConfigStringHolder logFilePath(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStressLogFile));
    if (logFilePath != nullptr)
    {
        SString sLogPath(logFilePath);
        fopen_s(&s_logFile, sLogPath.GetUTF8(), "w");
        if (s_logFile != nullptr)
        {
            fprintf(s_logFile, "=== cDAC GC Stress Verification Log ===\n");
            fprintf(s_logFile, "FailFast: %s\n", s_failFast ? "true" : "false");
            fprintf(s_logFile, "Step: %u (verify every %u stress points)\n\n", s_step, s_step);
        }
    }

    s_cdacLock.Init(CrstGCCover, CRST_DEFAULT);

    if (IsUniqueEnabled())
    {
        s_seenStacks = new SHash<NoRemoveSHashTraits<SetSHashTraits<SIZE_T>>>();
    }

    // Load the legacy DAC for three-way comparison (optional — non-fatal if it fails).
    {
        PathString dacPath;
        if (WszGetModuleFileName(reinterpret_cast<HMODULE>(GetCurrentModuleBase()), dacPath) != 0)
        {
            SString::Iterator dacIter = dacPath.End();
            if (dacPath.FindBack(dacIter, DIRECTORY_SEPARATOR_CHAR_W))
            {
                dacIter++;
                dacPath.Truncate(dacIter);
                dacPath.Append(W("mscordaccore.dll"));

                s_dacModule = CLRLoadLibrary(dacPath.GetUnicode());
                if (s_dacModule != NULL)
                {
                    typedef HRESULT (STDAPICALLTYPE *PFN_CLRDataCreateInstance)(REFIID, ICLRDataTarget*, void**);
                    auto pfnCreate = reinterpret_cast<PFN_CLRDataCreateInstance>(
                        ::GetProcAddress(s_dacModule, "CLRDataCreateInstance"));
                    if (pfnCreate != nullptr)
                    {
                        InProcessDataTarget* pTarget = new (nothrow) InProcessDataTarget();
                        if (pTarget != nullptr)
                        {
                            IUnknown* pDacUnk = nullptr;
                            HRESULT hr = pfnCreate(__uuidof(IUnknown), pTarget, (void**)&pDacUnk);
                            pTarget->Release();
                            if (SUCCEEDED(hr) && pDacUnk != nullptr)
                            {
                                pDacUnk->QueryInterface(__uuidof(ISOSDacInterface), (void**)&s_dacSosDac);
                                pDacUnk->QueryInterface(__uuidof(IXCLRDataProcess), (void**)&s_dacProcess);
                                pDacUnk->Release();
                            }
                        }
                    }
                    if (s_dacSosDac == nullptr)
                    {
                        LOG((LF_GCROOTS, LL_WARNING, "CDAC GC Stress: Legacy DAC loaded but QI for ISOSDacInterface failed\n"));
                    }
                }
                else
                {
                    LOG((LF_GCROOTS, LL_INFO10, "CDAC GC Stress: Legacy DAC not found (three-way comparison disabled)\n"));
                }
            }
        }
    }

    s_initialized = true;
    LOG((LF_GCROOTS, LL_INFO10, "CDAC GC Stress: Initialized successfully (failFast=%d, logFile=%s)\n",
        s_failFast, s_logFile != nullptr ? "yes" : "no"));
    return true;
}

void CdacStress::Shutdown()
{
    if (!s_initialized)
        return;

    // Print summary to stderr so results are always visible
    LONG actualVerifications = s_verifyPass + s_verifyFail + s_verifySkip;
    fprintf(stderr, "CDAC GC Stress: %ld stress points, %ld verifications (%ld pass / %ld fail, %ld skipped)\n",
        (long)s_verifyCount, (long)actualVerifications, (long)s_verifyPass, (long)s_verifyFail, (long)s_verifySkip);
    STRESS_LOG3(LF_GCROOTS, LL_ALWAYS,
        "CDAC GC Stress shutdown: %d verifications (%d pass / %d fail)\n",
        (int)actualVerifications, (int)s_verifyPass, (int)s_verifyFail);

    if (s_logFile != nullptr)
    {
        fprintf(s_logFile, "\n=== Summary ===\n");
        fprintf(s_logFile, "Total stress points: %ld\n", (long)s_verifyCount);
        fprintf(s_logFile, "Total verifications: %ld\n", (long)actualVerifications);
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

    // Legacy DAC cleanup
    if (s_dacSosDac != nullptr) { s_dacSosDac->Release(); s_dacSosDac = nullptr; }
    if (s_dacProcess != nullptr) { s_dacProcess->Release(); s_dacProcess = nullptr; }

    if (s_seenStacks != nullptr)
    {
        delete s_seenStacks;
        s_seenStacks = nullptr;
    }

    s_initialized = false;
    LOG((LF_GCROOTS, LL_INFO10, "CDAC GC Stress: Shutdown complete\n"));
}

//-----------------------------------------------------------------------------
// Collect stack refs from the cDAC
//-----------------------------------------------------------------------------

static bool CollectStackRefs(ISOSDacInterface* pSosDac, DWORD osThreadId, SArray<StackRef>* pRefs)
{
    if (pSosDac == nullptr)
        return false;

    ISOSStackRefEnum* pEnum = nullptr;
    HRESULT hr = pSosDac->GetStackReferences(osThreadId, &pEnum);

    if (FAILED(hr) || pEnum == nullptr)
        return false;

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
        ref.Register = refData.Register;
        ref.Offset = refData.Offset;
        ref.StackPointer = refData.StackPointer;
        pRefs->Append(ref);
    }

    // Release twice: once for the normal ref, and once for the extra ref-count
    // leaked by SOSDacImpl.GetStackReferences for COM compat (see ConvertToUnmanaged call).
    pEnum->Release();
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

    // Always report the real ppObj address. For register-based refs, ppObj points
    // into the REGDISPLAY/CONTEXT on the native stack — we can't reliably distinguish
    // these from managed stack slots on the runtime side. The comparison logic handles
    // this by matching register refs (cDAC Address=0) by (Object, Flags) only.
    ref.Address = reinterpret_cast<CLRDATA_ADDRESS>(ppObj);
    ref.Object = reinterpret_cast<CLRDATA_ADDRESS>(*ppObj);

    ref.Flags = 0;
    if (flags & GC_CALL_INTERIOR)
        ref.Flags |= SOSRefInterior;
    if (flags & GC_CALL_PINNED)
        ref.Flags |= SOSRefPinned;
    ref.Source = 0;
    ref.SourceType = 0;
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

    // Use a callback that matches DAC behavior (DacStackReferenceWalker::Callback):
    // Only call EnumGcRefs for frameless frames and GcScanRoots for explicit frames.
    // Deliberately skip the post-scan logic (LCG resolver promotion,
    // GcReportLoaderAllocator, generic param context) that GcStackCrawlCallBack
    // includes — the DAC's callback has that logic disabled (#if 0).
    struct DiagContext { GCCONTEXT* gcctx; RuntimeRefCollectionContext* collectCtx; };
    DiagContext diagCtx = { &gcctx, &collectCtx };

    auto dacLikeCallback = [](CrawlFrame* pCF, VOID* pData) -> StackWalkAction
    {
        DiagContext* dCtx = (DiagContext*)pData;
        GCCONTEXT* gcctx = dCtx->gcctx;

        ResetPointerHolder<CrawlFrame*> rph(&gcctx->cf);
        gcctx->cf = pCF;

        bool fReportGCReferences = pCF->ShouldCrawlframeReportGCReferences();

        if (fReportGCReferences)
        {
            if (pCF->IsFrameless())
            {
                ICodeManager* pCM = pCF->GetCodeManager();
                _ASSERTE(pCM != NULL);
                unsigned flags = pCF->GetCodeManagerFlags();
                pCM->EnumGcRefs(pCF->GetRegisterSet(),
                                pCF->GetCodeInfo(),
                                flags,
                                GcEnumObject,
                                gcctx);
            }
            else
            {
                Frame* pFrame = pCF->GetFrame();
                pFrame->GcScanRoots(gcctx->f, gcctx->sc);
            }
        }

        return SWA_CONTINUE;
    };

    pThread->StackWalkFrames(dacLikeCallback, &diagCtx, flagsStackWalk);

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

static int __cdecl CompareStackRefKey(const void* a, const void* b)
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
// Compare IXCLRDataStackWalk frame-by-frame between cDAC and legacy DAC.
// Creates a stack walk on each, advances in lockstep, and compares
// GetContext + Request(FRAME_DATA) at each step.
//-----------------------------------------------------------------------------

static void CompareStackWalks(Thread* pThread, PCONTEXT regs)
{
    if (s_cdacProcess == nullptr || s_dacProcess == nullptr)
        return;

    DWORD osThreadId = pThread->GetOSThreadId();

    // Get IXCLRDataTask for the thread from both processes
    IXCLRDataTask* cdacTask = nullptr;
    IXCLRDataTask* dacTask = nullptr;

    HRESULT hr1 = s_cdacProcess->GetTaskByOSThreadID(osThreadId, &cdacTask);
    HRESULT hr2 = s_dacProcess->GetTaskByOSThreadID(osThreadId, &dacTask);

    if (FAILED(hr1) || cdacTask == nullptr || FAILED(hr2) || dacTask == nullptr)
    {
        if (cdacTask) cdacTask->Release();
        if (dacTask) dacTask->Release();
        return;
    }

    // Create stack walks
    IXCLRDataStackWalk* cdacWalk = nullptr;
    IXCLRDataStackWalk* dacWalk = nullptr;

    hr1 = cdacTask->CreateStackWalk(0xF /* CLRDATA_SIMPFRAME_MANAGED_METHOD | ... */, &cdacWalk);
    hr2 = dacTask->CreateStackWalk(0xF, &dacWalk);

    cdacTask->Release();
    dacTask->Release();

    if (FAILED(hr1) || cdacWalk == nullptr || FAILED(hr2) || dacWalk == nullptr)
    {
        if (cdacWalk) cdacWalk->Release();
        if (dacWalk) dacWalk->Release();
        return;
    }

    // Walk in lockstep comparing each frame
    int frameIdx = 0;
    bool mismatch = false;
    while (frameIdx < 200) // safety limit
    {
        // Compare GetContext
        BYTE cdacCtx[4096] = {};
        BYTE dacCtx[4096] = {};
        ULONG32 cdacCtxSize = 0, dacCtxSize = 0;

        hr1 = cdacWalk->GetContext(0, sizeof(cdacCtx), &cdacCtxSize, cdacCtx);
        hr2 = dacWalk->GetContext(0, sizeof(dacCtx), &dacCtxSize, dacCtx);

        if (hr1 != hr2)
        {
            if (s_logFile)
                fprintf(s_logFile, "  [WALK_MISMATCH] Frame %d: GetContext hr mismatch cDAC=0x%x DAC=0x%x\n",
                    frameIdx, hr1, hr2);
            mismatch = true;
            break;
        }
        if (hr1 != S_OK)
            break; // both finished

        if (cdacCtxSize != dacCtxSize)
        {
            if (s_logFile)
                fprintf(s_logFile, "  [WALK_MISMATCH] Frame %d: Context size differs cDAC=%u DAC=%u\n",
                    frameIdx, cdacCtxSize, dacCtxSize);
            mismatch = true;
        }
        else if (cdacCtxSize >= sizeof(CONTEXT))
        {
            // Compare IP and SP — these are what matter for stack walk parity.
            // Other CONTEXT fields (floating-point, debug registers, xstate) may
            // differ between cDAC and DAC without affecting the walk.
            PCODE cdacIP = GetIP((CONTEXT*)cdacCtx);
            PCODE dacIP = GetIP((CONTEXT*)dacCtx);
            TADDR cdacSP = GetSP((CONTEXT*)cdacCtx);
            TADDR dacSP = GetSP((CONTEXT*)dacCtx);

            if (cdacIP != dacIP || cdacSP != dacSP)
            {
                if (s_logFile)
                    fprintf(s_logFile, "  [WALK_MISMATCH] Frame %d: Context differs cDAC_IP=0x%llx cDAC_SP=0x%llx DAC_IP=0x%llx DAC_SP=0x%llx\n",
                        frameIdx,
                        (unsigned long long)cdacIP, (unsigned long long)cdacSP,
                        (unsigned long long)dacIP, (unsigned long long)dacSP);
                mismatch = true;
            }
        }

        // Compare Request(FRAME_DATA)
        ULONG64 cdacFrameAddr = 0, dacFrameAddr = 0;
        hr1 = cdacWalk->Request(0xf0000000, 0, nullptr, sizeof(cdacFrameAddr), (BYTE*)&cdacFrameAddr);
        hr2 = dacWalk->Request(0xf0000000, 0, nullptr, sizeof(dacFrameAddr), (BYTE*)&dacFrameAddr);

        if (hr1 == S_OK && hr2 == S_OK && cdacFrameAddr != dacFrameAddr)
        {
            if (s_logFile)
            {
                PCODE cdacIP = 0, dacIP = 0;
                if (cdacCtxSize >= sizeof(CONTEXT))
                    cdacIP = GetIP((CONTEXT*)cdacCtx);
                if (dacCtxSize >= sizeof(CONTEXT))
                    dacIP = GetIP((CONTEXT*)dacCtx);
                fprintf(s_logFile, "  [WALK_MISMATCH] Frame %d: FrameAddr cDAC=0x%llx DAC=0x%llx (cDAC_IP=0x%llx DAC_IP=0x%llx)\n",
                    frameIdx, (unsigned long long)cdacFrameAddr, (unsigned long long)dacFrameAddr,
                    (unsigned long long)cdacIP, (unsigned long long)dacIP);
            }
            mismatch = true;
        }

        // Advance both
        hr1 = cdacWalk->Next();
        hr2 = dacWalk->Next();

        if (hr1 != hr2)
        {
            if (s_logFile)
                fprintf(s_logFile, "  [WALK_MISMATCH] Frame %d: Next hr mismatch cDAC=0x%x DAC=0x%x\n",
                    frameIdx, hr1, hr2);
            mismatch = true;
            break;
        }
        if (hr1 != S_OK)
            break; // both finished

        frameIdx++;
    }

    if (!mismatch && s_logFile)
        fprintf(s_logFile, "  [WALK_OK] %d frames matched between cDAC and DAC\n", frameIdx);

    cdacWalk->Release();
    dacWalk->Release();
}

//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Compare two ref sets using two-phase matching.
// Phase 1: Match stack refs (Address != 0) by exact (Address, Object, Flags).
// Phase 2: Match register refs (Address == 0) by (Object, Flags) only.
// Returns true if all refs in setA have a match in setB and counts are equal.
//-----------------------------------------------------------------------------

static bool CompareRefSets(StackRef* refsA, int countA, StackRef* refsB, int countB)
{
    if (countA != countB)
        return false;
    if (countA == 0)
        return true;
    if (countA > MAX_COLLECTED_REFS)
        return false;

    bool matched[MAX_COLLECTED_REFS] = {};

    for (int i = 0; i < countA; i++)
    {
        if (refsA[i].Address == 0)
            continue;
        bool found = false;
        for (int j = 0; j < countB; j++)
        {
            if (matched[j]) continue;
            if (refsA[i].Address == refsB[j].Address &&
                refsA[i].Object == refsB[j].Object &&
                refsA[i].Flags == refsB[j].Flags)
            {
                matched[j] = true;
                found = true;
                break;
            }
        }
        if (!found) return false;
    }

    for (int i = 0; i < countA; i++)
    {
        if (refsA[i].Address != 0)
            continue;
        bool found = false;
        for (int j = 0; j < countB; j++)
        {
            if (matched[j]) continue;
            if (refsA[i].Object == refsB[j].Object &&
                refsA[i].Flags == refsB[j].Flags)
            {
                matched[j] = true;
                found = true;
                break;
            }
        }
        if (!found) return false;
    }

    return true;
}

//-----------------------------------------------------------------------------
// Filter interior stack pointers and deduplicate a ref set in place.
//-----------------------------------------------------------------------------

static int FilterAndDedup(StackRef* refs, int count, Thread* pThread, uintptr_t stackLimit)
{
    count = FilterInteriorStackRefs(refs, count, pThread, stackLimit);
    count = DeduplicateRefs(refs, count);
    return count;
}

//-----------------------------------------------------------------------------
// Main entry point: verify at a GC stress point
//-----------------------------------------------------------------------------

bool CdacStress::ShouldSkipStressPoint()
{
    LONG count = InterlockedIncrement(&s_verifyCount);

    if (s_step <= 1)
        return false;

    return (count % s_step) != 0;
}

void CdacStress::VerifyAtAllocPoint()
{
    if (!s_initialized)
        return;

    // Reentrancy guard: allocations inside VerifyAtStressPoint (e.g., SArray)
    // would trigger this function again, causing deadlock on s_cdacLock.
    if (t_inVerification)
        return;

    Thread* pThread = GetThreadNULLOk();
    if (pThread == nullptr || !pThread->PreemptiveGCDisabled())
        return;

    CONTEXT ctx;
    RtlCaptureContext(&ctx);
    VerifyAtStressPoint(pThread, &ctx);
}

void CdacStress::VerifyAtStressPoint(Thread* pThread, PCONTEXT regs)
{
    _ASSERTE(s_initialized);
    _ASSERTE(pThread != nullptr);
    _ASSERTE(regs != nullptr);

    // RAII guard: set t_inVerification=true on entry, false on exit.
    // Prevents infinite recursion when allocations inside this function
    // trigger VerifyAtAllocPoint again (which would deadlock on s_cdacLock).
    struct ReentrancyGuard {
        ReentrancyGuard() { t_inVerification = true; }
        ~ReentrancyGuard() { t_inVerification = false; }
    } reentrancyGuard;

    // Serialize cDAC access — the cDAC's ProcessedData cache and COM interfaces
    // are not thread-safe, and GC stress can fire on multiple threads.
    CrstHolder cdacLock(&s_cdacLock);

    // Unique-stack filtering: use IP + SP as a stack identity.
    // This skips re-verification at the same code location with the same stack depth.
    if (IsUniqueEnabled() && s_seenStacks != nullptr)
    {
        SIZE_T stackHash = GetIP(regs) ^ (GetSP(regs) * 2654435761u);
        if (s_seenStacks->LookupPtr(stackHash) != nullptr)
            return;
        s_seenStacks->Add(stackHash);
    }

    // Set the thread context for the cDAC's ReadThreadContext callback.
    s_currentContext = regs;
    s_currentThreadId = pThread->GetOSThreadId();

    // Flush the cDAC's ProcessedData cache so it re-reads from the live process.
    if (s_cdacProcess != nullptr)
    {
        s_cdacProcess->Flush();
    }

    // Flush the legacy DAC cache too.
    if (s_dacProcess != nullptr)
    {
        s_dacProcess->Flush();
    }

    // Compare IXCLRDataStackWalk frame-by-frame between cDAC and legacy DAC.
    if (s_cdacStressLevel & CDACSTRESS_WALK)
    {
        CompareStackWalks(pThread, regs);
    }

    // Compare GC stack references.
    if (!(s_cdacStressLevel & CDACSTRESS_REFS))
    {
        s_currentContext = nullptr;
        s_currentThreadId = 0;
        return;
    }

    // Step 1: Collect raw refs from cDAC (always) and DAC (if USE_DAC).
    DWORD osThreadId = pThread->GetOSThreadId();

    SArray<StackRef> cdacRefs;
    bool haveCdac = CollectStackRefs(s_cdacSosDac, osThreadId, &cdacRefs);

    SArray<StackRef> dacRefs;
    bool haveDac = false;
    if (s_cdacStressLevel & CDACSTRESS_USE_DAC)
    {
        haveDac = (s_dacSosDac != nullptr) && CollectStackRefs(s_dacSosDac, osThreadId, &dacRefs);
    }

    s_currentContext = nullptr;
    s_currentThreadId = 0;

    StackRef runtimeRefsBuf[MAX_COLLECTED_REFS];
    int runtimeCount = 0;
    bool haveRuntime = CollectRuntimeStackRefs(pThread, regs, runtimeRefsBuf, &runtimeCount);

    if (!haveCdac || !haveRuntime)
    {
        InterlockedIncrement(&s_verifySkip);
        if (s_logFile != nullptr)
        {
            if (!haveCdac)
                fprintf(s_logFile, "[SKIP] Thread=0x%x IP=0x%p - cDAC GetStackReferences failed\n",
                    osThreadId, (void*)GetIP(regs));
            else
                fprintf(s_logFile, "[SKIP] Thread=0x%x IP=0x%p - runtime CollectRuntimeStackRefs overflowed\n",
                    osThreadId, (void*)GetIP(regs));
        }
        return;
    }

    // Step 2: Compare cDAC vs DAC raw (before any filtering).
    int rawCdacCount = (int)cdacRefs.GetCount();
    int rawDacCount = haveDac ? (int)dacRefs.GetCount() : -1;
    bool dacMatch = true;
    if (haveDac)
    {
        StackRef* cdacBuf = cdacRefs.OpenRawBuffer();
        StackRef* dacBuf = dacRefs.OpenRawBuffer();
        dacMatch = CompareRefSets(cdacBuf, rawCdacCount, dacBuf, rawDacCount);
        cdacRefs.CloseRawBuffer();
        dacRefs.CloseRawBuffer();
    }

    // Step 3: Filter cDAC refs and compare vs RT (always).
    Frame* pTopFrame = pThread->GetFrame();
    Object** topStack = (Object**)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        InlinedCallFrame* pInlinedFrame = dac_cast<PTR_InlinedCallFrame>(pTopFrame);
        topStack = (Object**)pInlinedFrame->GetCallSiteSP();
    }
    uintptr_t stackLimit = (uintptr_t)topStack;

    int filteredCdacCount = rawCdacCount;
    if (filteredCdacCount > 0)
    {
        StackRef* cdacBuf = cdacRefs.OpenRawBuffer();
        filteredCdacCount = FilterAndDedup(cdacBuf, filteredCdacCount, pThread, stackLimit);
        cdacRefs.CloseRawBuffer();
    }
    runtimeCount = DeduplicateRefs(runtimeRefsBuf, runtimeCount);

    StackRef* cdacBuf = cdacRefs.OpenRawBuffer();
    bool rtMatch = CompareRefSets(cdacBuf, filteredCdacCount, runtimeRefsBuf, runtimeCount);
    cdacRefs.CloseRawBuffer();

    // Step 4: Pass requires cDAC vs RT match.
    // DAC mismatch is logged separately but doesn't affect pass/fail.
    bool pass = rtMatch;

    if (pass)
        InterlockedIncrement(&s_verifyPass);
    else
        InterlockedIncrement(&s_verifyFail);

    // Step 5: Log results.
    if (s_logFile != nullptr)
    {
        const char* label = pass ? "PASS" : "FAIL";
        if (pass && !dacMatch)
            label = "DAC_MISMATCH";
        fprintf(s_logFile, "[%s] Thread=0x%x IP=0x%p cDAC=%d DAC=%d RT=%d\n",
            label, osThreadId, (void*)GetIP(regs),
            rawCdacCount, rawDacCount, runtimeCount);

        if (!pass || !dacMatch)
        {
            for (int i = 0; i < rawCdacCount; i++)
                fprintf(s_logFile, "  cDAC [%d]: Address=0x%llx Object=0x%llx Flags=0x%x Source=0x%llx SourceType=%d SP=0x%llx\n",
                    i, (unsigned long long)cdacRefs[i].Address, (unsigned long long)cdacRefs[i].Object,
                    cdacRefs[i].Flags, (unsigned long long)cdacRefs[i].Source, cdacRefs[i].SourceType,
                    (unsigned long long)cdacRefs[i].StackPointer);
            if (haveDac)
            {
                for (int i = 0; i < rawDacCount; i++)
                    fprintf(s_logFile, "  DAC  [%d]: Address=0x%llx Object=0x%llx Flags=0x%x Source=0x%llx\n",
                        i, (unsigned long long)dacRefs[i].Address, (unsigned long long)dacRefs[i].Object,
                        dacRefs[i].Flags, (unsigned long long)dacRefs[i].Source);
            }
            for (int i = 0; i < runtimeCount; i++)
                fprintf(s_logFile, "  RT   [%d]: Address=0x%llx Object=0x%llx Flags=0x%x\n",
                    i, (unsigned long long)runtimeRefsBuf[i].Address, (unsigned long long)runtimeRefsBuf[i].Object,
                    runtimeRefsBuf[i].Flags);

            fflush(s_logFile);
        }
    }
}

#endif // HAVE_GCCOVER
