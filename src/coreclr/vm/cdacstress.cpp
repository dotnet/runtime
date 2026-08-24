// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CdacStress.cpp
//
// At each enabled stress point, asks the cDAC and the runtime to enumerate
// the current thread's stack GC refs and compares them. The runtime's own
// GC root enumeration (what the collector actually consumes) is the oracle.
//
// Enabled via DOTNET_CdacStress.
//

#include "common.h"

#ifdef CDAC_STRESS

#include "cdacstress.h"
#include "dacprivate.h"
#include "../../native/managed/cdac/inc/cdac_reader.h"
#include "../../debug/datadescriptor-shared/inc/contract-descriptor.h"
#include <xclrdata.h>
#include <sospriv.h>
#include "threads.h"
#include "eeconfig.h"
#include "gccover.h"
#include "sstring.h"
#include "exinfo.h"
#include "gcrefmap.h"

#ifdef TARGET_LINUX
// process_vm_readv is the safe in-process read path on Linux. See
// ReadFromTargetCallback below for why PAL_TRY around memcpy is not viable.
#include <sys/uio.h>
#include <unistd.h>
#endif

//-----------------------------------------------------------------------------
// Constants and configuration
//-----------------------------------------------------------------------------

#define CDAC_LIB_NAME MAKEDLLNAME_W(W("mscordaccore_universal"))

// Sentinel flag set on cDAC StackRefData entries by RecordDeferredFrame to
// mark a frame whose ref scan was intentionally skipped (e.g. PromoteCallerStack
// pending the ArgIterator port). Mirrors GcScanFlags.CDAC_DEFERRED_FRAME.
static const unsigned int CDAC_DEFERRED_FRAME = 0x40000000;
static const int MAX_DEFERRED_FRAMES = 64;

// Bit flags for DOTNET_CdacStress configuration.
//
// Layout (little-endian DWORD):
//   byte 0 (0x000000FF) -- WHERE: trigger points the stress harness fires at
//   byte 1 (0x0000FF00) -- WHAT: which sub-checks run when a trigger fires
//   byte 2 (0x00FF0000) -- MODIFIERS: output / behavior knobs
//
// A useful configuration combines at least one WHERE and at least one WHAT
// (e.g. 0x0101 = ALLOC + GCREFS, 0x0301 = ALLOC + GCREFS + ARGITER).
enum CdacStressFlags : DWORD
{
    // WHERE -- trigger points
    CDACSTRESS_ALLOC        = 0x00000001,  // Verify at allocation points (gchelpers.cpp)

    // WHAT -- sub-checks (require a WHERE bit to be set as well)
    CDACSTRESS_GCREFS       = 0x00000100,  // Compare cDAC GetStackReferences vs runtime GC root oracle
    CDACSTRESS_ARGITER      = 0x00000200,  // Compare CallingConvention.EnumerateArguments vs runtime ComputeCallRefMap

    // MODIFIERS
    CDACSTRESS_VERBOSE      = 0x00010000,  // Rich per-ref diagnostics in the log
};

// Convenience masks.
static const DWORD CDACSTRESS_WHERE_MASK = 0x000000FF;
static const DWORD CDACSTRESS_WHAT_MASK  = 0x0000FF00;

//-----------------------------------------------------------------------------
// Types
//-----------------------------------------------------------------------------

// Identifies which collector produced a ref. Lets the logger derive its
// own side label (no need to thread "cDAC"/"RT" strings down through the
// comparison code).
enum RefSide : uint8_t
{
    SIDE_CDAC = 0,
    SIDE_RT   = 1,
};

// Represents a single GC stack reference for comparison purposes.
struct StackRef
{
    CLRDATA_ADDRESS Address;    // Location on stack holding the ref
    CLRDATA_ADDRESS Object;     // The object pointer value
    unsigned int    Flags;      // SOSRefFlags (interior, pinned)
    CLRDATA_ADDRESS Source;     // IP or Frame that owns this ref
    int             SourceType; // SOS_StackSourceIP, SOS_StackSourceFrame, or SOS_StackSourceOther
    int             Register;   // Processor-encoding reg number, -1 for stack slots
                                // (cDAC populates from GcInfo; runtime populates
                                // by inverting GetRegisterSlot on supported arches)
    int             Offset;     // Register offset (cDAC only)
    CLRDATA_ADDRESS StackPointer; // Stack pointer at this ref (cDAC only)
    RefSide         Side;       // Producer of this ref (cDAC vs runtime)
};

static int IdentifyRegisterFromPpObj(REGDISPLAY* pRD, void* ppObj)
{
#if defined(FEATURE_NATIVEAOT)
    (void)pRD; (void)ppObj;
    return -1;
#else
    if (pRD == nullptr || pRD->pCurrentContextPointers == nullptr)
        return -1;
    KNONVOLATILE_CONTEXT_POINTERS* p = pRD->pCurrentContextPointers;

#if defined(TARGET_AMD64)
    PDWORD64* slots = (PDWORD64*)&p->Rax;
    for (int r = 0; r < 16; r++)
    {
        if (r == 4) continue;  // rsp
        if ((void*)slots[r] == ppObj)
            return r;
    }
#elif defined(TARGET_ARM64)
    // gcinfo encoding for ARM64: X0..X28 = 0..28, FP = 29, LR = 30, SP = 31.
    // pCurrentContextPointers exposes only callee-saved (X19..X28, Fp, Lr).
    if ((void*)p->X19 == ppObj) return 19;
    if ((void*)p->X20 == ppObj) return 20;
    if ((void*)p->X21 == ppObj) return 21;
    if ((void*)p->X22 == ppObj) return 22;
    if ((void*)p->X23 == ppObj) return 23;
    if ((void*)p->X24 == ppObj) return 24;
    if ((void*)p->X25 == ppObj) return 25;
    if ((void*)p->X26 == ppObj) return 26;
    if ((void*)p->X27 == ppObj) return 27;
    if ((void*)p->X28 == ppObj) return 28;
    if ((void*)p->Fp  == ppObj) return 29;
    if ((void*)p->Lr  == ppObj) return 30;
#elif defined(TARGET_ARM)
    // gcinfo encoding for ARM: R0..R12 = 0..12, SP = 13, LR = 14, PC = 15.
    // pCurrentContextPointers exposes only callee-saved (R4..R11, Lr).
    if ((void*)p->R4  == ppObj) return 4;
    if ((void*)p->R5  == ppObj) return 5;
    if ((void*)p->R6  == ppObj) return 6;
    if ((void*)p->R7  == ppObj) return 7;
    if ((void*)p->R8  == ppObj) return 8;
    if ((void*)p->R9  == ppObj) return 9;
    if ((void*)p->R10 == ppObj) return 10;
    if ((void*)p->R11 == ppObj) return 11;
    if ((void*)p->Lr  == ppObj) return 14;
#elif defined(TARGET_X86)
    // gcinfo encoding for x86: EAX=0, ECX=1, EDX=2, EBX=3, ESP=4, EBP=5, ESI=6, EDI=7.
    if ((void*)p->Eax == ppObj) return 0;
    if ((void*)p->Ecx == ppObj) return 1;
    if ((void*)p->Edx == ppObj) return 2;
    if ((void*)p->Ebx == ppObj) return 3;
    if ((void*)p->Ebp == ppObj) return 5;
    if ((void*)p->Esi == ppObj) return 6;
    if ((void*)p->Edi == ppObj) return 7;
#elif defined(TARGET_LOONGARCH64)
    // gcinfo encoding for LoongArch64: Ra=1, Fp=22, S0..S8 = 23..31
    // (see GetRegName in src/coreclr/gcdump/gcdumpnonx86.cpp).
    if ((void*)p->Ra == ppObj) return 1;
    if ((void*)p->Fp == ppObj) return 22;
    if ((void*)p->S0 == ppObj) return 23;
    if ((void*)p->S1 == ppObj) return 24;
    if ((void*)p->S2 == ppObj) return 25;
    if ((void*)p->S3 == ppObj) return 26;
    if ((void*)p->S4 == ppObj) return 27;
    if ((void*)p->S5 == ppObj) return 28;
    if ((void*)p->S6 == ppObj) return 29;
    if ((void*)p->S7 == ppObj) return 30;
    if ((void*)p->S8 == ppObj) return 31;
#elif defined(TARGET_RISCV64)
    // gcinfo encoding for RISCV64: Ra=1, Gp=3, Tp=4, Fp=8, S1=9, S2..S11 = 18..27
    // (see GetRegName in src/coreclr/gcdump/gcdumpnonx86.cpp).
    if ((void*)p->Ra  == ppObj) return 1;
    if ((void*)p->Gp  == ppObj) return 3;
    if ((void*)p->Tp  == ppObj) return 4;
    if ((void*)p->Fp  == ppObj) return 8;
    if ((void*)p->S1  == ppObj) return 9;
    if ((void*)p->S2  == ppObj) return 18;
    if ((void*)p->S3  == ppObj) return 19;
    if ((void*)p->S4  == ppObj) return 20;
    if ((void*)p->S5  == ppObj) return 21;
    if ((void*)p->S6  == ppObj) return 22;
    if ((void*)p->S7  == ppObj) return 23;
    if ((void*)p->S8  == ppObj) return 24;
    if ((void*)p->S9  == ppObj) return 25;
    if ((void*)p->S10 == ppObj) return 26;
    if ((void*)p->S11 == ppObj) return 27;
#endif
    return -1;
#endif // !FEATURE_NATIVEAOT
}

//-----------------------------------------------------------------------------
// External symbols
//-----------------------------------------------------------------------------

// Contract descriptor symbol exported from coreclr (consumed by the cDAC).
extern "C" struct ContractDescriptor DotNetRuntimeContractDescriptor;

// 3-param GcEnumObject used as a GCEnumCallback.
// Defined in gcenv.ee.common.cpp; not exposed in any header.
extern void GcEnumObject(LPVOID pData, OBJECTREF *pObj, uint32_t flags);

//-----------------------------------------------------------------------------
// Forward declarations
//-----------------------------------------------------------------------------

static bool IsDeferredFrame(CLRDATA_ADDRESS source, const CLRDATA_ADDRESS* deferred, int deferredCount);
static void ResolveMethodName(CLRDATA_ADDRESS source, int sourceType, char* buf, int bufLen);
static void VerifyGcRefsAtStressPoint(Thread* pThread, PCONTEXT regs, DWORD osThreadId);
static void VerifyArgIteratorOnStack(Thread* pThread);
static void LogArgIteratorMismatch(MethodDesc* pMD, CLRDATA_ADDRESS mdAddr,
                                   LPCSTR frameName, const char* methodName,
                                   const BYTE* rtBlob, int rtLen,
                                   const BYTE* cdacBlob, int cdacLen);

//-----------------------------------------------------------------------------
// Static state — cDAC reader
//-----------------------------------------------------------------------------

static HMODULE              s_cdacModule = NULL;
static intptr_t             s_cdacHandle = 0;
static IUnknown*            s_cdacSosInterface = nullptr;
static IXCLRDataProcess*    s_cdacProcess = nullptr;    // Cached QI result for Flush()
static ISOSDacInterface*    s_cdacSosDac = nullptr;     // Cached QI result for GetStackReferences()

//-----------------------------------------------------------------------------
// Static state — framework
//-----------------------------------------------------------------------------

static bool             s_initialized = false;
static bool             s_failFast = true;
static DWORD            s_cdacStressLevel = 0; // Resolved CdacStressFlags
static FILE*            s_logFile = nullptr;
static CrstStatic       s_cdacLock;       // Serializes cDAC access from concurrent GC stress threads

//-----------------------------------------------------------------------------
// Static state — verification counters (reported at shutdown)
//-----------------------------------------------------------------------------

// Verification outcome counters. (Pass + Fail + KnownIssue) is the total
// number of stress points the harness ran to completion.
static volatile LONG    s_passCount = 0;
static volatile LONG    s_failCount = 0;
static volatile LONG    s_knownIssueCount = 0;

// Frame-level counters. Updated once per frame encountered during compare.
// frameTotal = frameMatch + frameMismatch + frameKnownNie.
static volatile LONG    s_frameTotal = 0;
static volatile LONG    s_frameMatch = 0;
static volatile LONG    s_frameMismatch = 0;
static volatile LONG    s_frameKnownNie = 0;

// ArgIterator (sub-trigger CDACSTRESS_ARGITER) counters. Distinct MDs only;
// per-MD dedup means each MD contributes exactly once across the run.
static volatile LONG    s_argIterPass = 0;
static volatile LONG    s_argIterFail = 0;
static volatile LONG    s_argIterSkip = 0;
static volatile LONG    s_argIterError = 0;

// Per-MD dedup for ArgIterator verification. Lazily allocated on first use,
// freed in Shutdown. Protected by s_cdacLock acquired in VerifyAtStressPoint.
class MethodDesc;
static SetSHash<MethodDesc*, PtrSetSHashTraits<MethodDesc*>>* s_argIterVerifiedMDs = nullptr;

//-----------------------------------------------------------------------------
// Thread-local state
//-----------------------------------------------------------------------------

// Current thread context at the stress point, consumed by the cDAC's
// ReadThreadContext callback.
static thread_local PCONTEXT s_currentContext = nullptr;
static thread_local DWORD    s_currentThreadId = 0;

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
#ifdef TARGET_LINUX
    // On Linux the PAL signal handler refuses to dispatch hardware exceptions
    // when the faulting PC is in non-runtime code (see IsSafeToHandleHardwareException
    // in exceptionhandling.cpp -- only managed code, virtual stubs, and marked JIT
    // helpers qualify). If the cDAC asks us to read from an invalid address, the
    // memcpy below would AV inside libc's __memcpy_advsimd, the signal handler
    // would bail, and the whole process would abort -- a PAL_TRY around memcpy
    // cannot catch it.
    //
    // process_vm_readv performs the copy in the kernel, returning EFAULT for
    // unmapped pages instead of raising a signal. Same pattern used by
    // createdump (crashinfounix.cpp:523).
    void* src = reinterpret_cast<void*>(static_cast<uintptr_t>(addr));
    iovec local = { dest, count };
    iovec remote = { src, count };
    ssize_t bytesRead = process_vm_readv(getpid(), &local, 1, &remote, 1, 0);
    return (bytesRead == (ssize_t)count) ? S_OK : E_FAIL;
#else
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
#endif
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

static bool IsCdacStressVerboseEnabled()
{
    return (s_cdacStressLevel & CDACSTRESS_VERBOSE) != 0;
}

static bool IsCdacStressGcRefsEnabled()
{
    return (s_cdacStressLevel & CDACSTRESS_GCREFS) != 0;
}

static bool IsCdacStressArgIterEnabled()
{
    return (s_cdacStressLevel & CDACSTRESS_ARGITER) != 0;
}

// Single-line file logger. Self-guards on s_logFile, so callers don't need to.
#define CDAC_LOG(...)                                  \
    do {                                               \
        if (s_logFile != nullptr)                      \
            fprintf(s_logFile, __VA_ARGS__);           \
    } while (0)

// Diagnostic emitter that always reaches stderr (and the log file when open).
// Use for init / library-load errors visible in CI. Every line is prefixed
// with "CDAC GC Stress: ".
#define CDAC_ERR(...)                                  \
    do {                                               \
        fprintf(stderr, "CDAC GC Stress: ");           \
        fprintf(stderr, __VA_ARGS__);                  \
        if (s_logFile != nullptr) {                    \
            fprintf(s_logFile, "CDAC GC Stress: ");    \
            fprintf(s_logFile, __VA_ARGS__);           \
        }                                              \
    } while (0)

// Forward declarations for helpers defined later. Implementations live in
// the "Rendering helpers" section at the bottom of the file.
static const char* RegisterName(int reg);
static const char* FormatRefFlags(unsigned int flags, char* buf, size_t bufLen);

// Per-ref disposition coming out of a frame compare.
enum RefDisposition : uint8_t
{
    REF_MATCHED = 0,    // paired with a ref on the opposite side
    REF_ONLY    = 1,    // present on this side, absent on the other
    REF_NIE     = 2,    // only-side, but Source is on the deferred list
                        // (only meaningful for SIDE_RT)
};

static const char* SideName(RefSide s);
static const char* DispositionName(RefDisposition d);
static void LogRefConcise(RefDisposition disp, const StackRef& r);
static void LogRefVerbose(RefDisposition disp, const StackRef& r);
static void LogRef(RefDisposition disp, const StackRef& r);

void CdacStressPolicy::Initialize()
{
    if (s_initialized)
        return;
    DWORD cdacStressLevel = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStress);
    if (cdacStressLevel == 0)
        return;

    // Record the requested stress level early so internal helpers
    // (e.g. IsCdacStressVerboseEnabled) work during the rest of init.
    // Triggers (CdacStress<tp>::IsEnabled) are gated by s_initialized,
    // which is set only after init completes successfully.
    s_cdacStressLevel = cdacStressLevel;

    // Load mscordaccore_universal from next to coreclr
    PathString path;
    // On Unix, GetCurrentModuleBase() returns a raw dladdr base address, not a
    // PAL HMODULE -- WszGetModuleFileName will return 0 for it. The DAC has
    // the same problem and uses PAL_GetPalHostModule() (which is the coreclr
    // host module, exactly where cdacstress.cpp lives). Mirror that pattern.
#ifdef HOST_UNIX
    HMODULE hCoreclr = PAL_GetPalHostModule();
#else
    HMODULE hCoreclr = reinterpret_cast<HMODULE>(GetCurrentModuleBase());
#endif
    if (hCoreclr == NULL || WszGetModuleFileName(hCoreclr, path) == 0)
    {
        CDAC_ERR("Failed to get coreclr module file name (WszGetModuleFileName returned 0).\n");
        return;
    }

    SString::Iterator iter = path.End();
    if (!path.FindBack(iter, DIRECTORY_SEPARATOR_CHAR_W))
    {
        MAKE_UTF8PTR_FROMWIDE_NOTHROW(pathUtf8Sep, path.GetUnicode());
        CDAC_ERR("Failed to find directory separator in module path '%s'.\n",
                 pathUtf8Sep != nullptr ? pathUtf8Sep : "<unknown>");
        return;
    }

    iter++;
    path.Truncate(iter);
    path.Append(CDAC_LIB_NAME);

    s_cdacModule = CLRLoadLibrary(path.GetUnicode());
    if (s_cdacModule == NULL)
    {
        MAKE_UTF8PTR_FROMWIDE_NOTHROW(pathUtf8, path.GetUnicode());
        CDAC_ERR("Failed to load cDAC library at '%s' "
                 "(check that mscordaccore_universal is shipped next to coreclr).\n",
                 pathUtf8 != nullptr ? pathUtf8 : "<unknown>");
        return;
    }

    // Resolve cdac_reader_init
    auto init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(s_cdacModule, "cdac_reader_init"));
    if (init == nullptr)
    {
        CDAC_ERR("Failed to resolve cdac_reader_init symbol.\n");
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        return;
    }

    // Get the address of the contract descriptor in our own process
    uint64_t descriptorAddr = reinterpret_cast<uint64_t>(&DotNetRuntimeContractDescriptor);

    // Initialize the cDAC reader with in-process callbacks (no write_thread_context or alloc_virtual for in-process stress)
    if (init(descriptorAddr, &ReadFromTargetCallback, &WriteToTargetCallback, &ReadThreadContextCallback, nullptr, nullptr, nullptr, &s_cdacHandle) != 0)
    {
        CDAC_ERR("cdac_reader_init failed (descriptorAddr=0x%llx).\n",
                 (unsigned long long)descriptorAddr);
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        return;
    }

    // Create the SOS interface
    auto createSos = reinterpret_cast<decltype(&cdac_reader_create_sos_interface)>(
        ::GetProcAddress(s_cdacModule, "cdac_reader_create_sos_interface"));
    if (createSos == nullptr)
    {
        CDAC_ERR("Failed to resolve cdac_reader_create_sos_interface symbol.\n");
        auto freeFn = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(s_cdacModule, "cdac_reader_free"));
        if (freeFn != nullptr)
            freeFn(s_cdacHandle);
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        s_cdacHandle = 0;
        return;
    }

    if (createSos(s_cdacHandle, nullptr, &s_cdacSosInterface) != 0)
    {
        CDAC_ERR("cdac_reader_create_sos_interface failed.\n");
        auto freeFn = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(s_cdacModule, "cdac_reader_free"));
        if (freeFn != nullptr)
            freeFn(s_cdacHandle);
        ::FreeLibrary(s_cdacModule);
        s_cdacModule = NULL;
        s_cdacHandle = 0;
        return;
    }

    // Read configuration for fail-fast behavior
    s_failFast = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_CdacStressFailFast) != 0;

    // Cache QI results so we don't QI on every stress point
    {
        HRESULT hr = s_cdacSosInterface->QueryInterface(__uuidof(IXCLRDataProcess), reinterpret_cast<void**>(&s_cdacProcess));
        if (FAILED(hr) || s_cdacProcess == nullptr)
        {
            CDAC_ERR("Failed to QI for IXCLRDataProcess (hr=0x%08x)\n", hr);
        }

        hr = s_cdacSosInterface->QueryInterface(__uuidof(ISOSDacInterface), reinterpret_cast<void**>(&s_cdacSosDac));
        if (FAILED(hr) || s_cdacSosDac == nullptr)
        {
            CDAC_ERR("Failed to QI for ISOSDacInterface (hr=0x%08x) - cannot verify\n", hr);
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
            return;
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
            fprintf(s_logFile, "FailFast: %s\n\n", s_failFast ? "true" : "false");
        }
        else
        {
            CDAC_ERR("Failed to open log file '%s' (errno may indicate missing directory).\n",
                     sLogPath.GetUTF8());
        }
    }

    s_cdacLock.Init(CrstGCCover, CRST_DEFAULT);

    // Activate triggers only after everything is fully initialized.
    s_initialized = true;
}

void CdacStressPolicy::Shutdown()
{
    if (!s_initialized)
        return;

    CrstHolder cdacLock(&s_cdacLock);
    s_initialized = false;

    // Print summary to stderr so results are always visible
    LONG totalVerifications = s_passCount + s_failCount + s_knownIssueCount;
    fprintf(stderr,
        "CDAC GC Stress: %ld verifications "
        "(%ld pass / %ld fail / %ld known-issue)\n",
        (long)totalVerifications,
        (long)s_passCount, (long)s_failCount, (long)s_knownIssueCount);
    fprintf(stderr,
        "CDAC GC Stress: %ld frames examined "
        "(%ld matched / %ld mismatched / %ld known-NIE)\n",
        (long)s_frameTotal, (long)s_frameMatch, (long)s_frameMismatch, (long)s_frameKnownNie);
    if (IsCdacStressArgIterEnabled())
    {
        fprintf(stderr,
            "CDAC GC Stress: ArgIter: %ld pass / %ld fail / %ld skip / %ld error\n",
            (long)s_argIterPass, (long)s_argIterFail, (long)s_argIterSkip, (long)s_argIterError);
    }
    STRESS_LOG3(LF_GCROOTS, LL_ALWAYS,
        "CDAC GC Stress shutdown: %d verifications (%d pass / %d fail)\n",
        (int)totalVerifications, (int)s_passCount, (int)s_failCount);

    if (s_logFile != nullptr)
    {
        fprintf(s_logFile, "\n=== Summary ===\n");
        fprintf(s_logFile, "Total verifications: %ld\n", (long)totalVerifications);
        fprintf(s_logFile, "  Passed:        %ld\n", (long)s_passCount);
        fprintf(s_logFile, "  Failed:        %ld\n", (long)s_failCount);
        fprintf(s_logFile, "  Known issues:  %ld\n", (long)s_knownIssueCount);
        fprintf(s_logFile, "Frames examined:     %ld\n", (long)s_frameTotal);
        fprintf(s_logFile, "  Matched:       %ld\n", (long)s_frameMatch);
        fprintf(s_logFile, "  Mismatched:    %ld\n", (long)s_frameMismatch);
        fprintf(s_logFile, "  Known NIE:     %ld\n", (long)s_frameKnownNie);
        // Machine-readable sub-check markers. Mirrors the existing [ARG_STATS]
        // line below: each is emitted only when its sub-check was enabled, so
        // CdacStressResults can distinguish "GCREFS / ARGITER did not run"
        // from "ran but produced zero results" (which the surrounding
        // human-readable counters cannot, since they are always printed and
        // always zero-initialized).
        if (IsCdacStressGcRefsEnabled())
        {
            fprintf(s_logFile, "[GC_STATS] verifications=%ld pass=%ld fail=%ld known_issue=%ld\n",
                (long)totalVerifications, (long)s_passCount,
                (long)s_failCount, (long)s_knownIssueCount);
        }
        if (IsCdacStressArgIterEnabled())
        {
            fprintf(s_logFile, "[ARG_STATS] pass=%ld fail=%ld skip=%ld error=%ld\n",
                (long)s_argIterPass, (long)s_argIterFail,
                (long)s_argIterSkip, (long)s_argIterError);
        }
        fclose(s_logFile);
        s_logFile = nullptr;
    }

    if (s_argIterVerifiedMDs != nullptr)
    {
        delete s_argIterVerifiedMDs;
        s_argIterVerifiedMDs = nullptr;
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

    s_cdacStressLevel = 0;
    LOG((LF_GCROOTS, LL_INFO10, "CDAC GC Stress: Shutdown complete\n"));
}

//-----------------------------------------------------------------------------
// Trigger gates -- one specialization per cdac_trigger_points value.
//
// IsEnabled is also gated on s_initialized so the patch-installing call sites
//-----------------------------------------------------------------------------

bool CdacStress<cdac_on_alloc>::IsEnabled()
{
    return s_initialized && (s_cdacStressLevel & CDACSTRESS_ALLOC) != 0;
}


//-----------------------------------------------------------------------------
// Collect stack refs from the cDAC
//-----------------------------------------------------------------------------

static HRESULT CollectCdacStackRefs(ISOSDacInterface* pSosDac, DWORD osThreadId, SArray<StackRef>* pRefs)
{
    if (pSosDac == nullptr)
        return E_POINTER;

    ISOSStackRefEnum* pEnum = nullptr;
    HRESULT hr = pSosDac->GetStackReferences(osThreadId, &pEnum);
    if (FAILED(hr))
        return hr;
    if (pEnum == nullptr)
        return E_POINTER;

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
        ref.Register = refData.HasRegisterInformation ? (int)refData.Register : -1;
        ref.Offset = refData.Offset;
        ref.StackPointer = refData.StackPointer;
        ref.Side = SIDE_CDAC;
        pRefs->Append(ref);
    }

    pEnum->Release();
    return S_OK;
}

//-----------------------------------------------------------------------------
// Collect stack refs from the runtime's own GC scanning
//-----------------------------------------------------------------------------

struct RuntimeRefCollectionContext
{
    SArray<StackRef>* refs;   // caller-owned, appended during walk
    bool overflow;

    // Per-frame attribution: updated by the crawl callback before each
    // EnumGcRefs/GcScanRoots call so the inner promote callback can stamp
    // every ref with the producing frame.
    //
    // Convention matches DAC (DacStackReferenceWalker, daccess.cpp:7488-7498)
    // and cDAC (GcScanContext.cs:89-97):
    //   - Frameless JIT frame: Source = native PC at the safepoint, SourceType = 0 (IP)
    //   - Explicit Frame:      Source = Frame*,                     SourceType = 1 (Frame)
    CLRDATA_ADDRESS currentFrameSource;
    int             currentFrameSourceType;

    // REGDISPLAY for the current frame (frameless only). Used by the promote
    // callback to invert GetRegisterSlot and recover the register number for
    // register-resident refs. nullptr for explicit Frames.
    REGDISPLAY*     currentRegDisplay;
};

static void CollectRuntimeRefsPromoteFunc(PTR_PTR_Object ppObj, ScanContext* sc, uint32_t flags)
{
    RuntimeRefCollectionContext* ctx = reinterpret_cast<RuntimeRefCollectionContext*>(sc->_unused1);
    if (ctx == nullptr)
        return;
    if (ctx->overflow)
        return;

    StackRef ref;
    ref.Object = reinterpret_cast<CLRDATA_ADDRESS>(*ppObj);

    ref.Flags = 0;
    if (flags & GC_CALL_INTERIOR)
        ref.Flags |= SOSRefInterior;
    if (flags & GC_CALL_PINNED)
        ref.Flags |= SOSRefPinned;

    // Per-frame attribution from the enclosing crawl callback.
    ref.Source = ctx->currentFrameSource;
    ref.SourceType = ctx->currentFrameSourceType;

    int recoveredReg = IdentifyRegisterFromPpObj(ctx->currentRegDisplay, (void*)ppObj);
    if (recoveredReg >= 0)
    {
        ref.Address = 0;
        ref.Register = recoveredReg;
    }
    else
    {
        ref.Address = reinterpret_cast<CLRDATA_ADDRESS>(ppObj);
        ref.Register = -1;
    }
    ref.Offset = 0;
    ref.StackPointer = 0;
    ref.Side = SIDE_RT;

    // SArray::Append can throw OutOfMemoryException. The runtime stack walker
    // CONTRACTs NOTHROW so we must not let an exception escape this callback.
    // On OOM, set the overflow flag so the caller treats the run as FAIL.
    EX_TRY
    {
        ctx->refs->Append(ref);
    }
    EX_CATCH
    {
        ctx->overflow = true;
    }
    EX_END_CATCH
}

// Runs the runtime's own ScanStackRoots-equivalent walk and Appends the
// resulting refs into the caller's SArray. Returns S_OK on a clean walk,
// or S_FALSE if any Append failed (OOM); the SArray will be non-empty but
// missing the tail in that case.
static HRESULT CollectRuntimeStackRefs(Thread* pThread, PCONTEXT regs, SArray<StackRef>* outRefs)
{
    RuntimeRefCollectionContext collectCtx;
    collectCtx.refs = outRefs;
    collectCtx.overflow = false;
    collectCtx.currentFrameSource = 0;
    collectCtx.currentFrameSourceType = SOS_StackSourceIP;
    collectCtx.currentRegDisplay = nullptr;

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
        RuntimeRefCollectionContext* collectCtx = dCtx->collectCtx;

        ResetPointerHolder<CrawlFrame*> rph(&gcctx->cf);
        gcctx->cf = pCF;

        bool fReportGCReferences = pCF->ShouldCrawlframeReportGCReferences();

        if (fReportGCReferences)
        {
            if (pCF->IsFrameless())
            {
                // Frameless JIT frame: attribute refs to the native PC at the
                // safepoint (matches DAC SOS_StackSourceIP convention).
                collectCtx->currentFrameSource =
                    (CLRDATA_ADDRESS)PCODEToPINSTR(GetControlPC(pCF->GetRegisterSet()));
                collectCtx->currentFrameSourceType = SOS_StackSourceIP;
                collectCtx->currentRegDisplay = pCF->GetRegisterSet();

                ICodeManager* pCM = pCF->GetCodeManager();
                _ASSERTE(pCM != NULL);
                unsigned flags = pCF->GetCodeManagerFlags();
                pCM->EnumGcRefs(pCF->GetRegisterSet(),
                                pCF->GetCodeInfo(),
                                flags,
                                GcEnumObject,
                                gcctx);

                collectCtx->currentRegDisplay = nullptr;
            }
            else
            {
                // Explicit Frame: attribute refs to the Frame address (matches
                // DAC SOS_StackSourceFrame convention). Explicit Frames don't
                // emit register-resident refs, so leave currentRegDisplay null.
                Frame* pFrame = pCF->GetFrame();
                collectCtx->currentFrameSource = (CLRDATA_ADDRESS)dac_cast<TADDR>(pFrame);
                collectCtx->currentFrameSourceType = SOS_StackSourceFrame;

                pFrame->GcScanRoots(gcctx->f, gcctx->sc);
            }
        }

        return SWA_CONTINUE;
    };

    pThread->StackWalkFrames(dacLikeCallback, &diagCtx, flagsStackWalk);

    // ScanStackRoots also scans two root sets that are not part of the frame walk: the
    // GCFrame (GCPROTECT) chain and the in-flight ExInfo chain. GetStackReferences reports
    // both, so mirror them here to keep the runtime-side collection in parity. See
    // ScanStackRoots in gcenv.ee.cpp.
    GCFrame* pGCFrame = pThread->GetGCFrame();
    while (pGCFrame != nullptr)
    {
        // A GCFrame node is a separate chain from the explicit Frame chain, so it is not a
        // capital-F Frame. Report it with the Other source type and the GCFrame node address as
        // the Source, matching cDAC (GcScanContext stamps Source = GCFrame node, SourceType = Other).
        collectCtx.currentFrameSource = (CLRDATA_ADDRESS)dac_cast<TADDR>(pGCFrame);
        collectCtx.currentFrameSourceType = SOS_StackSourceOther;
        collectCtx.currentRegDisplay = nullptr;
        pGCFrame->GcScanRoots(gcctx.f, gcctx.sc);
        pGCFrame = pGCFrame->PtrNextFrame();
    }

    PTR_ExInfo pExInfo = pThread->GetExceptionState()->GetCurrentExceptionTracker();
    while (pExInfo != NULL)
    {
        // The ExInfo is not a Frame either; GetStackReferences surfaces the in-flight exception
        // object with the Other source type and the ExInfo node address as the Source, the same
        // way it reports a GCFrame root. Mirror that here so the runtime-side collection matches cDAC.
        collectCtx.currentFrameSource = (CLRDATA_ADDRESS)dac_cast<TADDR>(pExInfo);
        collectCtx.currentFrameSourceType = SOS_StackSourceOther;
        collectCtx.currentRegDisplay = nullptr;
        PTR_PTR_Object pRef = dac_cast<PTR_PTR_Object>(&pExInfo->m_exception);
        gcctx.f(pRef, gcctx.sc, 0);
        pExInfo = pExInfo->GetPreviousExceptionTracker();
    }

    return collectCtx.overflow ? S_FALSE : S_OK;
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
// FrameRefGroup helpers used by CompareFrames below.
//-----------------------------------------------------------------------------

// Represents a group of refs from the same Source (managed frame or explicit Frame).
struct FrameRefGroup
{
    CLRDATA_ADDRESS Source;
    int SourceType;     // 0 = IP, 1 = Frame, 2 = Other
    int StartIdx;       // Index into the original ref array
    int Count;          // Number of refs in this group
};

// Build a sorted list of unique Sources with their ref index ranges.
// The refs array is sorted by Source as a side effect.
static int __cdecl CompareBySource(const void* a, const void* b)
{
    const StackRef* ra = static_cast<const StackRef*>(a);
    const StackRef* rb = static_cast<const StackRef*>(b);
    if (ra->Source != rb->Source)
        return (ra->Source < rb->Source) ? -1 : 1;
    return 0;
}

static void GroupRefsByFrame(StackRef* refs, int count, SArray<FrameRefGroup>* groups)
{
    if (count == 0)
        return;

    qsort(refs, count, sizeof(StackRef), CompareBySource);

    CLRDATA_ADDRESS currentSource = refs[0].Source;
    int startIdx = 0;

    for (int i = 1; i <= count; i++)
    {
        if (i == count || refs[i].Source != currentSource)
        {
            FrameRefGroup g;
            g.Source = currentSource;
            g.SourceType = refs[startIdx].SourceType;
            g.StartIdx = startIdx;
            g.Count = i - startIdx;
            groups->Append(g);
            if (i < count)
            {
                currentSource = refs[i].Source;
                startIdx = i;
            }
        }
    }
}

// Compare refs within a single frame using exact matching on a canonical key.
// Returns the number of unmatched refs in each set.
//
// Canonical key per ref:
//   - Address == 0  -> register-resident ref. Key = (Register, Object, Flags).
//                      cDAC reports Address=0 + Register set; runtime mirrors
//                      this convention by clearing Address=0 when
//                      IdentifyRegisterFromPpObj recovers a register number.
//   - Address != 0  -> stack-slot ref. Key = (Address, Object, Flags).
//                      Register/Offset are metadata describing how the JIT
//                      addressed the slot (e.g. [rbp-0x10]) and are NOT
//                      part of the matching key.
//
// A ref is considered matched iff it has an unused partner with an identical
// canonical key. There is no fuzzy fallback - any unmatched ref is a real
// disagreement that needs a clear diagnostic.
static void CompareFrameRefs(StackRef* refsA, int countA, StackRef* refsB, int countB,
                            int* unmatchedA, int* unmatchedB,
                            bool* aUsed, bool* bUsed)
{
    for (int i = 0; i < countA; i++)
    {
        bool aIsReg = refsA[i].Address == 0;
        for (int j = 0; j < countB; j++)
        {
            if (bUsed[j]) continue;
            bool bIsReg = refsB[j].Address == 0;
            if (aIsReg != bIsReg) continue;
            if (refsA[i].Object != refsB[j].Object) continue;
            if (refsA[i].Flags != refsB[j].Flags) continue;
            if (aIsReg)
            {
                if (refsA[i].Register != refsB[j].Register) continue;
            }
            else
            {
                if (refsA[i].Address != refsB[j].Address) continue;
            }
            aUsed[i] = bUsed[j] = true;
            break;
        }
    }

    *unmatchedA = 0;
    *unmatchedB = 0;
    for (int i = 0; i < countA; i++)
        if (!aUsed[i]) (*unmatchedA)++;
    for (int j = 0; j < countB; j++)
        if (!bUsed[j]) (*unmatchedB)++;
}

//-----------------------------------------------------------------------------
// Per-frame comparison.
//
// Group both ref sets by Source (managed PC for frameless JIT frames,
// Frame* for explicit Frames), merge-walk the two grouped lists, and per
// matching frame compare refs with CompareFrameRefs. Captures full per-frame
// results (including per-ref dispositions in a shared SArray) into the
// caller-owned FrameResult SArray. Pure data transform: no I/O, no counter
// side effects.
//
// Mismatch classification (runtime is the oracle):
//   - cDAC-only frame:                  MISMATCH
//   - RT-only frame, Source deferred:   KNOWN_NIE
//   - RT-only frame, not deferred:      MISMATCH
//   - Same Source, refs don't match:    MISMATCH
//-----------------------------------------------------------------------------

struct CompareVerdict
{
    bool pass;      // every frame's refs matched (no mismatches at all)
    bool allKnown;  // !pass, but every mismatching frame is a deferred Source
};

enum FrameOutcome : unsigned char
{
    FRAME_OUTCOME_MATCH      = 0,  // both sides emitted this frame, all refs matched
    FRAME_OUTCOME_MISMATCH   = 1,  // real disagreement (ref-level or frame-only)
    FRAME_OUTCOME_KNOWN_NIE  = 2,  // RT-only frame, Source on cDAC deferred list
};

// Result of comparing one frame. Carries enough state for the renderer to
// reconstruct the whole frame (counts, SPs, disposition of each ref) without
// re-walking the comparison.
//
// Per-ref dispositions are stored in a shared SArray<RefDisposition> owned
// by the caller of CompareFrames. CdacDispStart/RtDispStart index into that
// buffer; the disposition for ref i on the cDAC side is at
// dispBuf[CdacDispStart + i]. Storing them out-of-band keeps FrameResult
// trivially copyable (SArray<FrameResult>::Append memcpys it) and avoids
// any per-frame ref count cap.
struct FrameResult
{
    CLRDATA_ADDRESS Source;
    int             SourceType;

    CLRDATA_ADDRESS SP_cdac;        // 0 if cDAC didn't have this frame
    CLRDATA_ADDRESS SP_rt;          // 0 if RT didn't have this frame

    int             CdacStart;      // Index into the original cDAC ref array
    int             CdacCount;      // Refs cDAC reported for this frame
    int             RtStart;        // Index into the original RT ref array
    int             RtCount;        // Refs RT reported for this frame

    int             CdacDispStart;  // Index into shared disp buffer
    int             RtDispStart;    // Index into shared disp buffer

    FrameOutcome    Outcome;
};

// Helper used by CompareFrames to append per-ref dispositions into the shared
// disposition buffer. Returns the start index where the dispositions were
// written, suitable for storing in FrameResult::*DispStart.
static int AppendDispositions(SArray<RefDisposition>* dispBuf,
                              const bool* used, int count, bool isNie)
{
    int start = (int)dispBuf->GetCount();
    for (int i = 0; i < count; i++)
    {
        RefDisposition d;
        if (used[i])      d = REF_MATCHED;
        else if (isNie)   d = REF_NIE;
        else              d = REF_ONLY;
        dispBuf->Append(d);
    }
    return start;
}

// Walks the grouped frames once and appends per-frame results into outResults.
// Both ref arrays must be in source-order (qsort'd in GroupRefsByFrame).
// Per-ref dispositions are appended into the caller-owned dispBuf SArray;
// FrameResult::CdacDispStart / RtDispStart index back into dispBuf, and
// CdacStart / RtStart index back into refsCdac / refsRt for the underlying
// ref data.
static int CompareFrames(
    StackRef* refsCdac, int countCdac,
    StackRef* refsRt,   int countRt,
    const CLRDATA_ADDRESS* deferred, int deferredCount,
    SArray<FrameResult>* outResults,
    SArray<RefDisposition>* dispBuf)
{
    SArray<FrameRefGroup> groupsCdac, groupsRt;
    GroupRefsByFrame(refsCdac, countCdac, &groupsCdac);
    GroupRefsByFrame(refsRt,   countRt,   &groupsRt);

    int numGroupsCdac = (int)groupsCdac.GetCount();
    int numGroupsRt   = (int)groupsRt.GetCount();

    int idxCdac = 0, idxRt = 0;
    int resultCount = 0;

    auto addResult = [&]() -> FrameResult* {
        FrameResult fr;
        memset(&fr, 0, sizeof(fr));
        outResults->Append(fr);
        resultCount++;
        return &(*outResults)[outResults->GetCount() - 1];
    };

    while (idxCdac < numGroupsCdac || idxRt < numGroupsRt)
    {
        bool bothHave = idxCdac < numGroupsCdac && idxRt < numGroupsRt
                     && groupsCdac[idxCdac].Source == groupsRt[idxRt].Source;
        bool cdacOnly = idxRt >= numGroupsRt
                     || (idxCdac < numGroupsCdac
                         && groupsCdac[idxCdac].Source < groupsRt[idxRt].Source);

        FrameResult* fr = addResult();

        if (bothHave)
        {
            FrameRefGroup& gC = groupsCdac[idxCdac];
            FrameRefGroup& gR = groupsRt[idxRt];

            int cC = gC.Count;
            int cR = gR.Count;
            NewArrayHolder<bool> cUsed(new bool[cC]());
            NewArrayHolder<bool> rUsed(new bool[cR]());

            int unmatchedA = 0, unmatchedB = 0;
            CompareFrameRefs(&refsCdac[gC.StartIdx], cC,
                             &refsRt[gR.StartIdx],   cR,
                             &unmatchedA, &unmatchedB, cUsed, rUsed);

            fr->Source     = gC.Source;
            fr->SourceType = gC.SourceType;
            fr->SP_cdac    = refsCdac[gC.StartIdx].StackPointer;
            fr->SP_rt      = refsRt[gR.StartIdx].StackPointer;
            fr->CdacStart  = gC.StartIdx;
            fr->CdacCount  = cC;
            fr->RtStart    = gR.StartIdx;
            fr->RtCount    = cR;
            fr->Outcome    = (unmatchedA > 0 || unmatchedB > 0)
                             ? FRAME_OUTCOME_MISMATCH
                             : FRAME_OUTCOME_MATCH;
            fr->CdacDispStart = AppendDispositions(dispBuf, cUsed, cC, /*isNie=*/false);
            fr->RtDispStart   = AppendDispositions(dispBuf, rUsed, cR, /*isNie=*/false);
            idxCdac++;
            idxRt++;
        }
        else if (cdacOnly)
        {
            FrameRefGroup& gC = groupsCdac[idxCdac];
            fr->Source     = gC.Source;
            fr->SourceType = gC.SourceType;
            fr->SP_cdac    = refsCdac[gC.StartIdx].StackPointer;
            fr->SP_rt      = 0;
            fr->CdacStart  = gC.StartIdx;
            fr->CdacCount  = gC.Count;
            fr->RtStart    = 0;
            fr->RtCount    = 0;
            fr->Outcome    = FRAME_OUTCOME_MISMATCH;
            fr->CdacDispStart = (int)dispBuf->GetCount();
            for (int i = 0; i < gC.Count; i++) dispBuf->Append(REF_ONLY);
            fr->RtDispStart = (int)dispBuf->GetCount();
            idxCdac++;
        }
        else
        {
            // Frame only in RT. KNOWN_NIE iff Source is on the deferred list.
            FrameRefGroup& gR = groupsRt[idxRt];
            bool isKnownNie = IsDeferredFrame(gR.Source, deferred, deferredCount);
            fr->Source     = gR.Source;
            fr->SourceType = gR.SourceType;
            fr->SP_cdac    = 0;
            fr->SP_rt      = refsRt[gR.StartIdx].StackPointer;
            fr->CdacStart  = 0;
            fr->CdacCount  = 0;
            fr->RtStart    = gR.StartIdx;
            fr->RtCount    = gR.Count;
            fr->Outcome    = isKnownNie ? FRAME_OUTCOME_KNOWN_NIE
                                        : FRAME_OUTCOME_MISMATCH;
            fr->CdacDispStart = (int)dispBuf->GetCount();
            fr->RtDispStart   = (int)dispBuf->GetCount();
            RefDisposition d = isKnownNie ? REF_NIE : REF_ONLY;
            for (int i = 0; i < gR.Count; i++) dispBuf->Append(d);
            idxRt++;
        }
    }

    return resultCount;
}

// Walks FrameResult[] once and derives the verdict + advances global frame
// counters. Counters are bumped exactly once per call.
static CompareVerdict ComputeVerdict(const FrameResult* frames, int frameCount)
{
    int trueDiff = 0, knownDiff = 0;
    for (int i = 0; i < frameCount; i++)
    {
        InterlockedIncrement(&s_frameTotal);
        switch (frames[i].Outcome)
        {
            case FRAME_OUTCOME_MATCH:
                InterlockedIncrement(&s_frameMatch);
                break;
            case FRAME_OUTCOME_MISMATCH:
                InterlockedIncrement(&s_frameMismatch);
                trueDiff++;
                break;
            case FRAME_OUTCOME_KNOWN_NIE:
                InterlockedIncrement(&s_frameKnownNie);
                knownDiff++;
                break;
        }
    }
    CompareVerdict v;
    v.pass = (trueDiff == 0 && knownDiff == 0);
    v.allKnown = (trueDiff == 0 && knownDiff > 0);
    return v;
}

// Extract CDAC_DEFERRED_FRAME sentinel entries from a cDAC ref array.
// Removes them in-place (shifting later elements down), writes their Source
// addresses into `deferredOut`, and returns the new ref count. Sentinels are
// emitted by GcScanContext.RecordDeferredFrame for explicit Frames whose cDAC
// scan path is not implemented yet (typically PromoteCallerStack pending the
// ArgIterator port).
static int ExtractDeferredFrames(
    StackRef* refs, int count,
    CLRDATA_ADDRESS* deferredOut, int* pDeferredCount, int deferredMax)
{
    int dst = 0;
    int deferred = 0;
    for (int i = 0; i < count; i++)
    {
        if ((refs[i].Flags & CDAC_DEFERRED_FRAME) != 0)
        {
            if (deferred < deferredMax)
                deferredOut[deferred++] = refs[i].Source;
            continue;
        }
        if (dst != i)
            refs[dst] = refs[i];
        dst++;
    }
    *pDeferredCount = deferred;
    return dst;
}

static bool IsDeferredFrame(CLRDATA_ADDRESS source, const CLRDATA_ADDRESS* deferred, int deferredCount)
{
    for (int i = 0; i < deferredCount; i++)
    {
        if (deferred[i] == source)
            return true;
    }
    return false;
}

//-----------------------------------------------------------------------------
// ArgIterator sub-check: compare the cDAC's encoded GCRefMap blob against
// the runtime's ComputeCallRefMap output, byte-for-byte, for every MD on a
// transition Frame on the active thread.
//-----------------------------------------------------------------------------

// Per-MD dedup. Protected by s_cdacLock (held by VerifyAtStressPoint).

// Resolve a MethodDesc address to a human-readable name via the cDAC.
static void ResolveMethodNameFromMD(CLRDATA_ADDRESS mdAddr, char* buf, int bufLen)
{
    if (bufLen <= 0)
        return;

    if (s_cdacSosDac != nullptr)
    {
        WCHAR wname[256] = {};
        unsigned int nameLen = 0;
        if (SUCCEEDED(s_cdacSosDac->GetMethodDescName(mdAddr, ARRAY_SIZE(wname), wname, &nameLen)) && nameLen > 0)
        {
            WideCharToMultiByte(CP_UTF8, 0, wname, -1, buf, bufLen, NULL, NULL);
            return;
        }
    }
    snprintf(buf, bufLen, "<unknown 0x%llx>", (unsigned long long)mdAddr);
}

// Compute the runtime's authoritative GCRefMap blob for `pMD` and copy it
// into the caller's buffer (up to `bufSize` bytes). Returns the actual blob
// length on success, or a negative HRESULT-coded value on failure:
//   -1                  ComputeCallRefMap threw (signature couldn't be classified)
//   -2                  blob exceeded `bufSize` (caller should treat as oracle skip)
// A return >= 0 means `*pBufOut` has `return-value` valid bytes.
static int ComputeRuntimeArgGCRefMap(MethodDesc* pMD, BYTE* pBufOut, int bufSize)
{
    GCRefMapBuilder builder;
    bool threw = false;

    // ComputeCallRefMap chains down to FakeGcScanRoots which declares
    // STANDARD_VM_CONTRACT (MODE_PREEMPTIVE, GC_TRIGGERS, THROWS). The cdacstress
    // hook fires from inside the allocator while the thread is in cooperative
    // GC mode, so the strict mode/GC contract would assert. The work is
    // signature-walking + metadata loads, both of which are safe to perform
    // here (the runtime itself loads metadata in cooperative mode during JIT,
    // and we hold s_cdacLock around the whole call). Acknowledge the contract
    // violation explicitly so Checked builds don't false-fire.
    CONTRACT_VIOLATION(ModeViolation | GCViolation);

    EX_TRY
    {
        ComputeCallRefMap(pMD, &builder, /*isDispatchCell*/ false);
    }
    EX_CATCH
    {
        threw = true;
    }
    EX_END_CATCH

    if (threw)
        return -1;

    DWORD blobLen = 0;
    PVOID blob = builder.GetBlob(&blobLen);
    if ((int)blobLen > bufSize)
        return -2;

    if (blobLen > 0)
        memcpy(pBufOut, blob, blobLen);
    return (int)blobLen;
}

// Hex-dump a blob into `buf` ("aa bb cc ...") for diagnostic output.
// On overflow the dump is truncated with a trailing "..." marker.
static void FormatBlobHex(const BYTE* blob, int len, char* buf, size_t bufLen)
{
    if (bufLen == 0)
        return;
    buf[0] = '\0';
    size_t used = 0;
    for (int i = 0; i < len; i++)
    {
        // Each byte needs 3 chars ("xx ") plus null and trailing "...".
        if (used + 8 >= bufLen)
        {
            snprintf(buf + used, bufLen - used, "...");
            return;
        }
        int n = snprintf(buf + used, bufLen - used, "%02x ", blob[i]);
        if (n <= 0) break;
        used += (size_t)n;
    }
}

// Token name for log output. Matches CORCOMPILE_GCREFMAP_TOKENS in corcompile.h.
static const char* GCRefMapTokenName(int token)
{
    switch (token)
    {
        case GCREFMAP_SKIP:          return "SKIP";
        case GCREFMAP_REF:           return "REF";
        case GCREFMAP_INTERIOR:      return "INTERIOR";
        case GCREFMAP_METHOD_PARAM:  return "METHOD_PARAM";
        case GCREFMAP_TYPE_PARAM:    return "TYPE_PARAM";
        case GCREFMAP_VASIG_COOKIE:  return "VASIG_COOKIE";
        default:                     return "?";
    }
}

// Per-slot location label for the ARG_FAIL table. On the architectures the
// runtime supports, the first NUM_ARGUMENT_REGISTERS positions cover the
// integer-arg registers and the rest are caller-stack slots. Naming the
// registers (vs printing raw offsets) is the difference between "I can read
// this" and "let me go grep the ABI doc".
static void FormatSlotLocation(int pos, int byteOffset, char* buf, size_t bufLen)
{
#if defined(TARGET_AMD64)
#  if defined(UNIX_AMD64_ABI)
    static const char* regNames[] = { "RDI", "RSI", "RDX", "RCX", "R8", "R9" };
#  else
    static const char* regNames[] = { "RCX", "RDX", "R8", "R9" };
#  endif
#elif defined(TARGET_ARM64)
    static const char* regNames[] = { "X0", "X1", "X2", "X3", "X4", "X5", "X6", "X7" };
#elif defined(TARGET_ARM)
    static const char* regNames[] = { "R0", "R1", "R2", "R3" };
#elif defined(TARGET_X86)
    // x86 has 2 arg regs (ECX, EDX) and a non-monotonic pos->offset mapping;
    // print pos+offset rather than guess the wrong register name.
    static const char* regNames[] = { "ECX", "EDX" };
#endif

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_X86)
    const int numRegs = (int)(sizeof(regNames) / sizeof(regNames[0]));
    if (pos >= 0 && pos < numRegs)
    {
        snprintf(buf, bufLen, "%-6s", regNames[pos]);
        return;
    }
#endif

    int stackByteOffset = byteOffset - (int)sizeof(TransitionBlock);
    snprintf(buf, bufLen, "[sp+%d]", stackByteOffset);
}

// Decode a GCRefMap blob into an offset->token map (sparse) plus the
// max pos seen. On x86 we consume the leading WriteStackPop prefix into
// `StackPop` so the remaining bitstream is the token stream proper, matching
// the runtime's GCInfoDecoder.ReadStackPop()-then-ReadToken() ordering.
struct DecodedBlob
{
    static const int MaxSlots = 64;
    int Pos[MaxSlots];
    int Tok[MaxSlots];
    int Count;
    int MaxPos;
    int StackPop;   // x86 only; 0 on other arches and on x86 VarArgs
};

static void DecodeBlob(const BYTE* blob, int len, DecodedBlob& out, bool isX86)
{
    out.Count = 0;
    out.MaxPos = -1;
    out.StackPop = 0;
    if (blob == nullptr || len == 0)
        return;

    GCRefMapDecoder decoder(const_cast<BYTE*>(blob));
#ifdef TARGET_X86
    if (isX86)
        out.StackPop = (int)decoder.ReadStackPop();
#else
    (void)isX86;
#endif
    while (!decoder.AtEnd() && out.Count < DecodedBlob::MaxSlots)
    {
        int token = decoder.ReadToken();
        int afterPos = decoder.CurrentPos();

        if (token == GCREFMAP_SKIP)
        {
            // A skip token bumps pos but emits no entry.
            if (afterPos - 1 > out.MaxPos)
                out.MaxPos = afterPos - 1;
            continue;
        }

        // ReadToken stores the result at the position BEFORE the increment.
        int slotPos = afterPos - 1;
        out.Pos[out.Count] = slotPos;
        out.Tok[out.Count] = token;
        out.Count++;
        if (slotPos > out.MaxPos)
            out.MaxPos = slotPos;
    }
}

static int LookupTokenAtPos(const DecodedBlob& blob, int pos)
{
    for (int i = 0; i < blob.Count; i++)
    {
        if (blob.Pos[i] == pos)
            return blob.Tok[i];
    }
    return GCREFMAP_SKIP;
}

// Compute the byte offset within the TransitionBlock for a given GCRefMap pos,
// mirroring ComputeCallRefMap (frames.cpp:2155-2163).
static int OffsetFromGCRefMapPos(int pos)
{
#ifdef TARGET_X86
    if (pos < NUM_ARGUMENT_REGISTERS)
        return TransitionBlock::GetOffsetOfArgumentRegisters() + ARGUMENTREGISTERS_SIZE - (pos + 1) * sizeof(TADDR);
    return TransitionBlock::GetOffsetOfArgs() + (pos - NUM_ARGUMENT_REGISTERS) * sizeof(TADDR);
#else
    return TransitionBlock::GetOffsetOfFirstGCRefMapSlot() + pos * TARGET_POINTER_SIZE;
#endif
}

// Emit a per-slot comparison table when the runtime and cDAC GCRefMap blobs
// differ. Each row is one position; only positions with a non-skip token on
// at least one side are shown, and rows where the two tokens differ are
// flagged. Reads enormously better than two hex-strings when triaging a port
// bug ("oh, the cDAC missed the byref at stack[+0]" vs squinting at "85 04").
static void LogArgIteratorMismatch(MethodDesc* pMD, CLRDATA_ADDRESS mdAddr,
                                   LPCSTR frameName, const char* methodName,
                                   const BYTE* rtBlob, int rtLen,
                                   const BYTE* cdacBlob, int cdacLen)
{
#ifdef TARGET_X86
    const bool isX86 = true;
#else
    const bool isX86 = false;
#endif

    DecodedBlob rt, cdac;
    DecodeBlob(rtBlob, rtLen, rt, isX86);
    DecodeBlob(cdacBlob, cdacLen, cdac, isX86);

    int maxPos = rt.MaxPos > cdac.MaxPos ? rt.MaxPos : cdac.MaxPos;
    if (maxPos < 0) maxPos = 0;

    char rtHex[256], cdacHex[256];
    FormatBlobHex(rtBlob, rtLen, rtHex, sizeof(rtHex));
    FormatBlobHex(cdacBlob, cdacLen, cdacHex, sizeof(cdacHex));

    CDAC_LOG("[ARG_FAIL] MD=0x%llx frame=%s rtSize=%d cdacSize=%d %s\n",
        (unsigned long long)mdAddr, frameName, rtLen, cdacLen, methodName);
    CDAC_LOG("    RT:   %s\n", rtHex);
    CDAC_LOG("    cDAC: %s\n", cdacHex);
    if (isX86)
    {
        const char* popDiff = (rt.StackPop != cdac.StackPop) ? " <-- DIFF" : "";
        CDAC_LOG("    stack_pop  RT=%d  cDAC=%d%s\n", rt.StackPop, cdac.StackPop, popDiff);
    }
    CDAC_LOG("    pos  location  RT token       cDAC token       diff\n");

    for (int pos = 0; pos <= maxPos; pos++)
    {
        int rtTok = LookupTokenAtPos(rt, pos);
        int cdacTok = LookupTokenAtPos(cdac, pos);
        if (rtTok == GCREFMAP_SKIP && cdacTok == GCREFMAP_SKIP)
            continue;

        char loc[24];
        FormatSlotLocation(pos, OffsetFromGCRefMapPos(pos), loc, sizeof(loc));

        const char* diff = (rtTok != cdacTok) ? " <-- DIFF" : "";
        CDAC_LOG("    %3d  %-8s  %-13s  %-15s%s\n",
            pos, loc, GCRefMapTokenName(rtTok), GCRefMapTokenName(cdacTok), diff);
    }
}

// Verify ArgIterator output for a single MD. Computes the runtime oracle
// blob (via ComputeCallRefMap), asks the cDAC for the same blob via the
// private Request opcode, and compares byte-for-byte.
static void VerifyArgIteratorForMD(MethodDesc* pMD, FrameIdentifier frameId)
{
    char methodName[256];
    ResolveMethodNameFromMD((CLRDATA_ADDRESS)(LONG_PTR)pMD, methodName, sizeof(methodName));
    LPCSTR frameName = Frame::GetFrameTypeName(frameId);
    if (frameName == nullptr)
        frameName = "<unknown>";

    // Stack-allocated buffer for both the runtime oracle blob and the cDAC
    // first-attempt response. Typical blobs are 1-4 bytes, so 64 covers
    // nearly every signature in one call. The cDAC side falls back to a
    // heap buffer via the ERROR_INSUFFICIENT_BUFFER two-call pattern below
    // when an outlier exceeds it; for the runtime oracle, an overflow
    // surfaces as an ARG_SKIP ("runtime-blob-too-large").
    const int kStackBufSize = 64;

    // 1. Runtime oracle. If the runtime itself can't classify this MD there's
    //    nothing for the cDAC to be wrong about, so silently skip --
    //    counted as ARG_SKIP for visibility in stats.
    BYTE rtBlob[kStackBufSize];
    int rtLen = ComputeRuntimeArgGCRefMap(pMD, rtBlob, (int)sizeof(rtBlob));
    if (rtLen < 0)
    {
        InterlockedIncrement(&s_argIterSkip);
        const char* reason = (rtLen == -1) ? "runtime-threw" : "runtime-blob-too-large";
        CDAC_LOG("[ARG_SKIP] MD=0x%llx frame=%s reason=%s %s\n",
            (unsigned long long)(LONG_PTR)pMD, frameName, reason, methodName);
        return;
    }

    // 2. cDAC side via the private Request opcode. outBuffer is unused;
    //    the request descriptor carries an [in,out] buffer descriptor that
    //    the handler writes through. Two-call shape: try the stack guess
    //    first; if it's too small, the handler returns
    //    ERROR_INSUFFICIENT_BUFFER with cbFilled = needed size, and we retry
    //    with a heap buffer.
    BYTE stackBuf[kStackBufSize];

    DacStressArgGCRefMapRequest req = {};
    req.MethodDesc    = (CLRDATA_ADDRESS)(LONG_PTR)pMD;
    req.BlobBuffer    = (CLRDATA_ADDRESS)(LONG_PTR)stackBuf;
    req.BlobBufferLen = sizeof(stackBuf);

    HRESULT cdacHr = s_cdacProcess->Request(
        DACSTRESSPRIV_REQUEST_COMPUTE_ARG_GCREFMAP,
        sizeof(req), (BYTE*)&req,
        0, nullptr);

    const BYTE* cdacBlob = stackBuf;
    NewArrayHolder<BYTE> heapBuf;
    if (cdacHr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        ULONG32 need = req.cbNeeded;
        heapBuf = new (nothrow) BYTE[need];
        if (heapBuf == nullptr)
        {
            InterlockedIncrement(&s_argIterSkip);
            CDAC_LOG("[ARG_SKIP] MD=0x%llx frame=%s reason=oom-retry-buffer rtBlobSize=%d %s\n",
                (unsigned long long)req.MethodDesc, frameName, rtLen, methodName);
            return;
        }
        req.BlobBuffer    = (CLRDATA_ADDRESS)(LONG_PTR)(BYTE*)heapBuf;
        req.BlobBufferLen = need;
        cdacHr = s_cdacProcess->Request(
            DACSTRESSPRIV_REQUEST_COMPUTE_ARG_GCREFMAP,
            sizeof(req), (BYTE*)&req,
            0, nullptr);
        cdacBlob = heapBuf;
    }

    if (cdacHr == E_NOTIMPL)
    {
        InterlockedIncrement(&s_argIterSkip);
        CDAC_LOG("[ARG_SKIP] MD=0x%llx frame=%s reason=0x%08x rtBlobSize=%d %s\n",
            (unsigned long long)req.MethodDesc, frameName,
            (unsigned int)cdacHr, rtLen, methodName);
        return;
    }
    if (FAILED(cdacHr))
    {
        InterlockedIncrement(&s_argIterError);
        CDAC_LOG("[ARG_ERROR] MD=0x%llx frame=%s cdacHr=0x%08x %s\n",
            (unsigned long long)req.MethodDesc, frameName,
            (unsigned int)cdacHr, methodName);
        return;
    }

    // 3. Byte-for-byte comparison.
    if ((int)req.cbFilled == rtLen && memcmp(cdacBlob, rtBlob, rtLen) == 0)
    {
        InterlockedIncrement(&s_argIterPass);
        CDAC_LOG("[ARG_PASS] MD=0x%llx frame=%s blobSize=%d %s\n",
            (unsigned long long)req.MethodDesc, frameName, rtLen, methodName);
        return;
    }

    InterlockedIncrement(&s_argIterFail);
    LogArgIteratorMismatch(pMD, req.MethodDesc, frameName, methodName,
                           rtBlob, rtLen, cdacBlob, (int)req.cbFilled);
}

static void VerifyArgIteratorOnStack(Thread* pThread)
{
    _ASSERTE(s_cdacProcess != nullptr);

    // Lazily allocate the dedup set on first use. Bounded by the count of
    // distinct MDs hitting frames during this run, so growing without bound is fine.
    if (s_argIterVerifiedMDs == nullptr)
    {
        s_argIterVerifiedMDs = new (nothrow) SetSHash<MethodDesc*, PtrSetSHashTraits<MethodDesc*>>();
        if (s_argIterVerifiedMDs == nullptr)
            return;  // OOM: skip ArgIter verification entirely this run.
    }

    // Walk every stack frame (both frameless JIT frames and explicit "F" Frames).
    // For each frame that resolves to a MethodDesc, verify it. The ArgIterator
    // port produces a result for any MD regardless of which kind of frame surfaced
    // it, so the only filter is "does this frame have an MD". Per-MD dedup keeps
    // cost flat across long stress runs.
    struct WalkCtx
    {
        FrameIdentifier lastFrameId;
    };
    WalkCtx ctx;
    ctx.lastFrameId = FrameIdentifier::None;

    auto callback = [](CrawlFrame* pCF, VOID* pData) -> StackWalkAction
    {
        WalkCtx* c = (WalkCtx*)pData;

        MethodDesc* pMD = pCF->GetFunction();
        if (pMD == nullptr)
            return SWA_CONTINUE;

        // Frame identifier for logging context: explicit Frames carry their
        // class id; frameless JIT frames have no Frame*, so report "None"
        // (the cDAC walker treats it as just another managed frame).
        FrameIdentifier id = FrameIdentifier::None;
        if (!pCF->IsFrameless())
        {
            Frame* pFrame = pCF->GetFrame();
            if (pFrame != nullptr)
                id = pFrame->GetFrameIdentifier();
        }

        if (s_argIterVerifiedMDs->Lookup(pMD) != nullptr)
            return SWA_CONTINUE;

        EX_TRY
        {
            s_argIterVerifiedMDs->Add(pMD);
        }
        EX_CATCH
        {
            // OOM adding to the dedup set: skip this MD and try again later.
            return SWA_CONTINUE;
        }
        EX_END_CATCH

        VerifyArgIteratorForMD(pMD, id);
        c->lastFrameId = id;
        return SWA_CONTINUE;
    };

    GCForbidLoaderUseHolder forbidLoaderUse;
    unsigned flags = ALLOW_ASYNC_STACK_WALK | ALLOW_INVALID_OBJECTS | GC_FUNCLET_REFERENCE_REPORTING;
    pThread->StackWalkFrames(callback, &ctx, flags);
}

//-----------------------------------------------------------------------------
// Stress verification implementation: shared by all trigger-point
// specializations below. Compares cDAC vs runtime stack refs at the captured
// CONTEXT and records per-frame results.
//-----------------------------------------------------------------------------

static void VerifyAtStressPoint(Thread* pThread, PCONTEXT regs)
{
    _ASSERTE(pThread != nullptr);
    _ASSERTE(regs != nullptr);

    // Serialize cDAC access — the cDAC's ProcessedData cache and COM interfaces
    // are not thread-safe, and GC stress can fire on multiple threads.
    CrstHolder cdacLock(&s_cdacLock);

    if (!s_initialized)
        return;

    DWORD osThreadId = pThread->GetOSThreadId();

    // Each sub-check below is gated independently on its CDACSTRESS_* WHAT bit.
    if (IsCdacStressGcRefsEnabled())
    {
        VerifyGcRefsAtStressPoint(pThread, regs, osThreadId);
    }

    if (IsCdacStressArgIterEnabled() && s_cdacProcess != nullptr)
    {
        s_currentContext = regs;
        s_currentThreadId = osThreadId;

        // Flush target-state caches before walking. The GCREFS sub-check
        // does this implicitly via its A.1 phase; if ARGITER runs without
        // GCREFS, the cDAC's ProcessedData cache can be stale (or empty),
        // which causes ValidateMethodDescPointer to fail for live MDs.
        s_cdacProcess->Request(DACSTRESSPRIV_REQUEST_FLUSH_TARGET_STATE, 0, NULL, 0, NULL);

        VerifyArgIteratorOnStack(pThread);
        s_currentContext = nullptr;
        s_currentThreadId = 0;
    }
}

// GC-refs sub-check: compare cDAC GetStackReferences output against the
// runtime's own GC root enumeration at the captured CONTEXT.
static void VerifyGcRefsAtStressPoint(Thread* pThread, PCONTEXT regs, DWORD osThreadId)
{

    // Phase A: Collect raw refs from both sides (independent walks).

    // A.1: cDAC side. ReadThreadContext callback state is wired here so the
    // cDAC can return the captured CONTEXT for the active thread.
    SArray<StackRef> cdacRefs;
    HRESULT cdacHr;
    {
        s_currentContext = regs;
        s_currentThreadId = osThreadId;

        // Flush only target-state caches (process state can change), keep
        // immutable metadata caches (e.g. CoreLib type info) populated.
        if (s_cdacProcess != nullptr)
            s_cdacProcess->Request(DACSTRESSPRIV_REQUEST_FLUSH_TARGET_STATE, 0, NULL, 0, NULL);

        cdacHr = CollectCdacStackRefs(s_cdacSosDac, osThreadId, &cdacRefs);

        s_currentContext = nullptr;
        s_currentThreadId = 0;
    }

    // A.2: Runtime side -- the oracle (GC's own ScanStackRoots-equivalent walk).
    SArray<StackRef> runtimeRefs;
    HRESULT rtHr = CollectRuntimeStackRefs(pThread, regs, &runtimeRefs);
    int runtimeCount = (int)runtimeRefs.GetCount();

    if (FAILED(cdacHr))
    {
        InterlockedIncrement(&s_failCount);
        CDAC_LOG("[FAIL] Thread=0x%x IP=0x%p - cDAC GetStackReferences failed (hr=0x%08x)\n",
            osThreadId, (void*)GetIP(regs), cdacHr);
        return;
    }
    if (rtHr == S_FALSE)
    {
        // OOM mid-Append; comparing a truncated set risks a false PASS.
        InterlockedIncrement(&s_failCount);
        CDAC_LOG("[FAIL] Thread=0x%x IP=0x%p - RT collection OOM after %d refs\n",
            osThreadId, (void*)GetIP(regs), runtimeCount);
        return;
    }

    // Phase B: Normalize the cDAC side so it can compare directly with RT.

    // B.1: Live-stack upper bound. PromoteCarefully (siginfo.cpp) drops
    // interior pointers whose value lies in the live stack [topStack, ...).
    // We mirror that filter on the cDAC side in B.3.
    Frame* pTopFrame = pThread->GetFrame();
    Object** topStack = (Object**)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        InlinedCallFrame* pInlinedFrame = dac_cast<PTR_InlinedCallFrame>(pTopFrame);
        topStack = (Object**)pInlinedFrame->GetCallSiteSP();
    }
    uintptr_t stackLimit = (uintptr_t)topStack;

    // B.2: Extract CDAC_DEFERRED_FRAME sentinels from the cDAC ref set.
    // These are markers (not real refs) emitted when the cDAC intentionally
    // skips a Frame whose scan path is not implemented yet. Their Source
    // addresses are used in Phase C to re-classify diffs as known issues.
    CLRDATA_ADDRESS deferredFrames[MAX_DEFERRED_FRAMES];
    int deferredFrameCount = 0;
    int cdacCount = (int)cdacRefs.GetCount();
    if (cdacCount > 0)
    {
        StackRef* buf = cdacRefs.OpenRawBuffer();
        cdacCount = ExtractDeferredFrames(
            buf, cdacCount,
            deferredFrames, &deferredFrameCount, MAX_DEFERRED_FRAMES);
        cdacRefs.CloseRawBuffer();
    }

    // B.3: Mirror PromoteCarefully's interior-into-stack filter on the cDAC
    // side. The cDAC reports raw GcInfo slots without this filter.
    if (cdacCount > 0)
    {
        StackRef* buf = cdacRefs.OpenRawBuffer();
        cdacCount = FilterInteriorStackRefs(buf, cdacCount, pThread, stackLimit);
        cdacRefs.CloseRawBuffer();
    }

    // Phase C: Compare per-frame. CompareFrames is a pure data transform;
    // ComputeVerdict bumps the global counters once.
    SArray<FrameResult> frameResults;
    SArray<RefDisposition> dispBuf;
    StackRef* cdacBuf = cdacRefs.OpenRawBuffer();
    StackRef* runtimeBuf = runtimeRefs.OpenRawBuffer();
    int frameCount = CompareFrames(
        cdacBuf, cdacCount,
        runtimeBuf, runtimeCount,
        deferredFrames, deferredFrameCount,
        &frameResults, &dispBuf);
    const RefDisposition* dispPtr = dispBuf.GetElements();
    CompareVerdict verdict = ComputeVerdict(frameResults.GetElements(), frameCount);

    // Phase D: Bucket the outcome and (on mismatch) emit hierarchical
    // diagnostics: one block per broken frame, then one stack trace.
    if (verdict.pass)
        InterlockedIncrement(&s_passCount);
    else if (verdict.allKnown)
        InterlockedIncrement(&s_knownIssueCount);
    else
        InterlockedIncrement(&s_failCount);

    if (verdict.pass)
    {
        CDAC_LOG("[PASS] Thread=0x%x IP=0x%p cDAC=%d RT=%d frames=%d\n",
            osThreadId, (void*)GetIP(regs), cdacCount, runtimeCount, frameCount);
    }
    else if (s_logFile != nullptr)
    {
        const char* label = verdict.allKnown ? "KNOWN_ISSUE" : "FAIL";

        // Per-trigger-point frame breakdown — lets a reader confirm at a
        // glance that a KNOWN_ISSUE has zero real mismatch frames.
        int fMatch = 0, fMismatch = 0, fNie = 0;
        for (int i = 0; i < frameCount; i++)
        {
            switch (frameResults[i].Outcome)
            {
                case FRAME_OUTCOME_MATCH:     fMatch++; break;
                case FRAME_OUTCOME_MISMATCH:  fMismatch++; break;
                case FRAME_OUTCOME_KNOWN_NIE: fNie++; break;
            }
        }

        CDAC_LOG("[%s] Thread=0x%x IP=0x%p cDAC=%d RT=%d frames=%d (match=%d mismatch=%d known_nie=%d)\n",
            label, osThreadId, (void*)GetIP(regs), cdacCount, runtimeCount,
            frameCount, fMatch, fMismatch, fNie);

        bool verbose = IsCdacStressVerboseEnabled();

        // Per-broken-frame blocks. Matched frames are omitted entirely in
        // concise mode; verbose mode still emits matched refs under their
        // [STACK_TRACE] entry. Frame numbering matches the stack trace
        // emitted at the end.
        for (int i = 0; i < frameCount; i++)
        {
            const FrameResult& fr = frameResults[i];
            if (fr.Outcome == FRAME_OUTCOME_MATCH)
                continue;

            char methodName[256];
            ResolveMethodName(fr.Source, fr.SourceType, methodName, sizeof(methodName));

            const char* outcomeName =
                fr.Outcome == FRAME_OUTCOME_MISMATCH  ? "MISMATCH" :
                fr.Outcome == FRAME_OUTCOME_KNOWN_NIE ? "KNOWN_NIE" : "?";

            const char* spNote = "";
            if (fr.SP_cdac != 0 && fr.SP_rt != 0 && fr.SP_cdac != fr.SP_rt)
                spNote = " <-- SP MISMATCH";

            CDAC_LOG("  Frame #%d %s [%s] cDAC=%d RT=%d SP_cDAC=0x%llx SP_RT=0x%llx%s\n",
                i, methodName, outcomeName, fr.CdacCount, fr.RtCount,
                (unsigned long long)fr.SP_cdac, (unsigned long long)fr.SP_rt,
                spNote);

            // Per-ref dump. Verbose -> all refs; concise -> only non-MATCHED
            // refs (which is the actionable signal — what diverges).
            for (int j = 0; j < fr.CdacCount; j++)
            {
                RefDisposition d = dispPtr[fr.CdacDispStart + j];
                if (!verbose && d == REF_MATCHED)
                    continue;
                LogRef(d, cdacBuf[fr.CdacStart + j]);
            }
            for (int j = 0; j < fr.RtCount; j++)
            {
                RefDisposition d = dispPtr[fr.RtDispStart + j];
                if (!verbose && d == REF_MATCHED)
                    continue;
                LogRef(d, runtimeBuf[fr.RtStart + j]);
            }
        }

        // One stack trace at the end of the stress-point block, with markers
        // on the broken frames so a reader can correlate Frame #N above to
        // the same #N here.
        CDAC_LOG("  [STACK_TRACE] (cDAC=%d RT=%d frames=%d)\n",
            cdacCount, runtimeCount, frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            char methodName[256];
            ResolveMethodName(frameResults[i].Source, frameResults[i].SourceType,
                methodName, sizeof(methodName));

            const char* marker = "";
            switch (frameResults[i].Outcome)
            {
                case FRAME_OUTCOME_MATCH:     marker = "";                                           break;
                case FRAME_OUTCOME_MISMATCH:  marker = " <-- MISMATCH";                              break;
                case FRAME_OUTCOME_KNOWN_NIE: marker = " <-- KNOWN_NIE (PromoteCallerStack deferred)"; break;
            }
            CDAC_LOG("    #%d %s (cDAC=%d RT=%d)%s\n",
                i, methodName, frameResults[i].CdacCount, frameResults[i].RtCount, marker);
        }

        fflush(s_logFile);
    }

    cdacRefs.CloseRawBuffer();
    runtimeRefs.CloseRawBuffer();
}

//-----------------------------------------------------------------------------
// Trigger-point specializations: each MaybeVerify is invoked at the wired
// runtime site. They gate on IsEnabled, capture the caller's CONTEXT, and
// hand off to VerifyAtStressPoint for the shared work.
//-----------------------------------------------------------------------------

void CdacStress<cdac_on_alloc>::MaybeVerify()
{
    if (!IsEnabled())
        return;

    Thread* pThread = GetThreadNULLOk();
    if (pThread == nullptr || !pThread->PreemptiveGCDisabled())
        return;

    // The walk will start from inside MaybeVerify itself; the comparison
    // treats this frame as just another frame (no need to skip it).
    CONTEXT ctx;
    RtlCaptureContext(&ctx);

    VerifyAtStressPoint(pThread, &ctx);
}

//=============================================================================
// Rendering helpers
//
// All textual formatting / log emission for cdacstress lives here. Forward
// declarations near the top of the file allow the main logic to call into
// these helpers without inlining the formatting code into the algorithm.
// Adding new log shapes (e.g. new [DEBUG_*] blocks) belongs in this section.
//=============================================================================

static const char* SideName(RefSide s)
{
    return s == SIDE_CDAC ? "cDAC" : "RT";
}

// Pretty-print a processor-encoding register number for the current target.
// Returns a short interned string. Unknown values render as "?".
//
// Register numbering matches the GcInfo encoding for each architecture
// (i.e. what gcdump's GetRegName / RegName produces). Negative values are
// rendered as "-" (meaning "ref is stack-resident, not register-resident").
static const char* RegisterName(int reg)
{
#if defined(TARGET_AMD64)
    static const char* names[16] = {
        "rax","rcx","rdx","rbx","rsp","rbp","rsi","rdi",
        "r8","r9","r10","r11","r12","r13","r14","r15"
    };
    if (reg >= 0 && reg < 16) return names[reg];
#elif defined(TARGET_ARM64)
    static const char* names[32] = {
        "x0","x1","x2","x3","x4","x5","x6","x7",
        "x8","x9","x10","x11","x12","x13","x14","x15",
        "x16","x17","x18","x19","x20","x21","x22","x23",
        "x24","x25","x26","x27","x28","fp","lr","sp"
    };
    if (reg >= 0 && reg < 32) return names[reg];
#elif defined(TARGET_X86)
    static const char* names[8] = {
        "eax","ecx","edx","ebx","esp","ebp","esi","edi"
    };
    if (reg >= 0 && reg < 8) return names[reg];
#elif defined(TARGET_ARM)
    static const char* names[16] = {
        "r0","r1","r2","r3","r4","r5","r6","r7",
        "r8","r9","r10","r11","r12","sp","lr","pc"
    };
    if (reg >= 0 && reg < 16) return names[reg];
#elif defined(TARGET_LOONGARCH64)
    static const char* names[33] = {
        "r0","ra","tp","sp","a0","a1","a2","a3",
        "a4","a5","a6","a7","t0","t1","t2","t3",
        "t4","t5","t6","t7","t8","x0","fp","s0",
        "s1","s2","s3","s4","s5","s6","s7","s8",
        "pc"
    };
    if (reg >= 0 && reg < 33) return names[reg];
#elif defined(TARGET_RISCV64)
    static const char* names[33] = {
        "r0","ra","sp","gp","tp","t0","t1","t2",
        "fp","s1","a0","a1","a2","a3","a4","a5",
        "a6","a7","s2","s3","s4","s5","s6","s7",
        "s8","s9","s10","s11","t3","t4","t5","t6",
        "pc"
    };
    if (reg >= 0 && reg < 33) return names[reg];
#endif
    if (reg < 0) return "-";
    return "?";
}

// Format ref Flags as a bit-name list (e.g. "Interior|Pinned" or "-").
// Writes into caller-supplied buffer to avoid TLS / allocation.
static const char* FormatRefFlags(unsigned int flags, char* buf, size_t bufLen)
{
    if (flags == 0) { strncpy_s(buf, bufLen, "-", _TRUNCATE); return buf; }
    buf[0] = '\0';
    bool first = true;
    auto append = [&](const char* s) {
        if (!first) strncat_s(buf, bufLen, "|", _TRUNCATE);
        strncat_s(buf, bufLen, s, _TRUNCATE);
        first = false;
    };
    if (flags & SOSRefInterior) append("Interior");
    if (flags & SOSRefPinned)   append("Pinned");
    unsigned int known = SOSRefInterior | SOSRefPinned;
    if (flags & ~known)
    {
        char other[24];
        sprintf_s(other, ARRAY_SIZE(other), "0x%x", flags & ~known);
        append(other);
    }
    return buf;
}

static const char* DispositionName(RefDisposition d)
{
    switch (d)
    {
        case REF_MATCHED: return "MATCHED";
        case REF_ONLY:    return "ONLY";
        case REF_NIE:     return "NIE";
        default:          return "?";
    }
}

// Concise per-ref line. Side label is derived from ref.Side; disposition is
// supplied by the comparison layer. No-op if s_logFile is nullptr.
static void LogRefConcise(RefDisposition disp, const StackRef& r)
{
    CDAC_LOG("      [%s(%s)] Addr=0x%llx Obj=0x%llx Flags=0x%x Reg=%d Off=%d\n",
        DispositionName(disp), SideName(r.Side),
        (unsigned long long)r.Address, (unsigned long long)r.Object, r.Flags,
        r.Register, r.Offset);
}

// Verbose per-ref line — emitted when CDACSTRESS_VERBOSE is on. No-op if
// s_logFile is nullptr.
static void LogRefVerbose(RefDisposition disp, const StackRef& r)
{
    char flagBuf[64];
    FormatRefFlags(r.Flags, flagBuf, ARRAY_SIZE(flagBuf));

    const char* regName = RegisterName(r.Register);
    bool hasReg = r.Register >= 0;

    CDAC_LOG(
        "      [%s(%s)] Addr=0x%llx Obj=0x%llx Flags=%s HasReg=%s Reg=%s(%d) Off=%d SP=0x%llx\n",
        DispositionName(disp), SideName(r.Side),
        (unsigned long long)r.Address,
        (unsigned long long)r.Object,
        flagBuf,
        hasReg ? "Y" : "N",
        regName, r.Register,
        r.Offset,
        (unsigned long long)r.StackPointer);
}

// Dispatch to verbose or concise based on the global flag.
static void LogRef(RefDisposition disp, const StackRef& r)
{
    if (IsCdacStressVerboseEnabled())
        LogRefVerbose(disp, r);
    else
        LogRefConcise(disp, r);
}

// Resolve a managed IP (or Frame*) to a printable name for log output.
// Falls back to "<unknown 0x...>" or "<frame 0x...>" if resolution fails.
// Uses the cDAC's ISOSDacInterface by default; we're running in-process
// against live pointers, so dereferencing Frame* directly is safe (no DAC
// marshaling needed).
static void ResolveMethodName(CLRDATA_ADDRESS source, int sourceType, char* buf, int bufLen)
{
    if (bufLen <= 0)
        return;

    if (sourceType == SOS_StackSourceOther)
    {
        // A root reported outside the frame walk (GCFrame/GCPROTECT or ExInfo chain). Source is a
        // node address, not a capital-F Frame, so do not dereference it as a Frame*.
        snprintf(buf, bufLen, "<other 0x%llx>", (unsigned long long)source);
        return;
    }

    if (sourceType == SOS_StackSourceFrame)
    {
        Frame* pFrame = reinterpret_cast<Frame*>(source);
        LPCSTR typeName = Frame::GetFrameTypeName(pFrame->GetFrameIdentifier());
        if (typeName != nullptr)
            snprintf(buf, bufLen, "<frame %s 0x%llx>", typeName, (unsigned long long)source);
        else
            snprintf(buf, bufLen, "<frame 0x%llx>", (unsigned long long)source);
        return;
    }

    ISOSDacInterface* pSos = s_cdacSosDac;

    if (pSos != nullptr)
    {
        CLRDATA_ADDRESS mdAddr = 0;
        if (SUCCEEDED(pSos->GetMethodDescPtrFromIP(source, &mdAddr)) && mdAddr != 0)
        {
            WCHAR wname[256] = {};
            unsigned int nameLen = 0;
            if (SUCCEEDED(pSos->GetMethodDescName(mdAddr, ARRAY_SIZE(wname), wname, &nameLen)) && nameLen > 0)
            {
                WideCharToMultiByte(CP_UTF8, 0, wname, -1, buf, bufLen, NULL, NULL);
                return;
            }
        }
    }

    snprintf(buf, bufLen, "<unknown 0x%llx>", (unsigned long long)source);
}

#endif // CDAC_STRESS
