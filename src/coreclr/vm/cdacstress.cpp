// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CdacStress.cpp
//
// Implements in-process cDAC loading and stack reference verification.
// Enabled via DOTNET_CdacStress (bit flags).
// At each enabled stress point we:
//   1. Ask the cDAC to enumerate stack GC references via ISOSDacInterface::GetStackReferences
//   2. Ask the runtime to enumerate stack GC references via StackWalkFrames + GcInfoDecoder
//      (the GC's own root-reporting machinery — the ground-truth oracle)
//   3. Compare the two sets and report any mismatches
//
// The runtime's GC root enumeration is the single oracle: it is what the collector
// actually consumes, so by definition cDAC must agree with it.
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

// Fixed-size buffer for collecting refs during stack walk.
// No heap allocation inside the promote callback, we're under NOTHROW contracts.
static const int MAX_COLLECTED_REFS = 4096;

// Per-frame cap for both the disposition arrays kept on FrameResult AND the
// number of refs CompareFrameRefs will consider for matching. Frames that
// exceed this cap are marked truncated and emit a WARN log; the comparison
// only examines the FIRST MAX_REFS_PER_FRAME refs on each side, which can
// mask real mismatches AND create spurious ones when the two sides' "first N"
// subsets disagree. Set high enough to cover the largest observed frame
// (CreateManifestAndDescriptors has ~109 untracked stack slots); bump if
// the WARN log fires.
static const int MAX_REFS_PER_FRAME = 256;

// Sentinel flag set on cDAC StackRefData entries by RecordDeferredFrame to
// mark a frame whose ref scan was intentionally skipped (e.g. PromoteCallerStack
// pending the ArgIterator port). Mirrors GcScanFlags.CDAC_DEFERRED_FRAME.
static const unsigned int CDAC_DEFERRED_FRAME = 0x40000000;
static const int MAX_DEFERRED_FRAMES = 64;

// Bit flags for DOTNET_CdacStress configuration.
enum CdacStressFlags : DWORD
{
    // Trigger points (where stress fires)
    CDACSTRESS_ALLOC        = 0x1,    // Verify at allocation points

    // Modifiers
    CDACSTRESS_VERBOSE      = 0x200,  // Rich per-ref diagnostics in the log
};

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
    int             SourceType; // SOS_StackSourceIP or SOS_StackSourceFrame
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

// Single-line file logger. Self-guards on s_logFile, so callers don't need to.
// All structured per-verification output (lines beginning with [PASS] / [FAIL]
// / [FRAME_*] / [STACK_TRACE] / etc.) should go through this macro rather than
// raw fprintf. Summary / shutdown lines that already check s_logFile may keep
// their direct fprintf, but using CDAC_LOG is also fine.
#define CDAC_LOG(...)                                  \
    do {                                               \
        if (s_logFile != nullptr)                      \
            fprintf(s_logFile, __VA_ARGS__);           \
    } while (0)

// Error/diagnostic emitter for messages that must be visible even when the
// per-process log file is not open (e.g. during framework init, before
// s_logFile exists, or on failure paths where the framework disables itself
// silently). Emits to BOTH stderr (always) and the log file (when open) so
// the message is captured in CI output regardless of where the consumer is
// looking. Every line is automatically prefixed with "CDAC GC Stress: ".
// Use for: init success/failure, library-load errors, fatal failures.
// Don't use for: per-verification output (use CDAC_LOG instead).
#define CDAC_ERR(...)                                  \
    do {                                               \
        fprintf(stderr, "CDAC GC Stress: ");           \
        fprintf(stderr, __VA_ARGS__);                  \
        if (s_logFile != nullptr) {                    \
            fprintf(s_logFile, "CDAC GC Stress: ");    \
            fprintf(s_logFile, __VA_ARGS__);           \
        }                                              \
    } while (0)

// Pretty-print a processor-encoding register number for the current target.
// Returns a short interned string. Unknown values render as "?".
// Implementation: see "Rendering helpers" section at bottom of file.
static const char* RegisterName(int reg);

// Format ref Flags as a bit-name list (e.g. "Interior|Pinned" or "-").
// Writes into caller-supplied buffer to avoid TLS / allocation.
// Implementation: see "Rendering helpers" section at bottom of file.
static const char* FormatRefFlags(unsigned int flags, char* buf, size_t bufLen);

// Per-ref disposition coming out of a frame compare. Combined with ref.Side
// to produce the rendered label (e.g. ONLY+SIDE_CDAC -> "ONLY(cDAC)").
enum RefDisposition : uint8_t
{
    REF_MATCHED = 0,    // paired with a ref on the opposite side
    REF_ONLY    = 1,    // present on this side, absent on the other
    REF_NIE     = 2,    // only-side, but Source is on the deferred list
                        // (only meaningful for SIDE_RT)
};

// Forward declarations for the remaining rendering helpers. All definitions
// live in the "Rendering helpers" section at bottom of file.
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

    // Initialize the cDAC reader with in-process callbacks (no alloc_virtual for in-process stress)
    if (init(descriptorAddr, &ReadFromTargetCallback, &WriteToTargetCallback, &ReadThreadContextCallback, nullptr, nullptr, &s_cdacHandle) != 0)
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

    s_initialized = false;
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
    StackRef refs[MAX_COLLECTED_REFS];
    int count;
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
    if (ctx->count >= MAX_COLLECTED_REFS)
    {
        ctx->overflow = true;
        return;
    }

    StackRef& ref = ctx->refs[ctx->count++];

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
}

// Runs the runtime's own ScanStackRoots-equivalent walk and copies the
// resulting refs into the caller's buffer. Returns S_OK on a clean walk,
// or S_FALSE if the per-thread buffer overflowed (the walk still ran to
// completion but the returned ref set is the prefix that fit in
// MAX_COLLECTED_REFS slots).
static HRESULT CollectRuntimeStackRefs(Thread* pThread, PCONTEXT regs, StackRef* outRefs, int* outCount)
{
    RuntimeRefCollectionContext collectCtx;
    collectCtx.count = 0;
    collectCtx.overflow = false;
    collectCtx.currentFrameSource = 0;
    collectCtx.currentFrameSourceType = 0;
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
                collectCtx->currentFrameSourceType = 0; // SOS_StackSourceIP
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
                collectCtx->currentFrameSourceType = 1; // SOS_StackSourceFrame

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
    int SourceType;     // 0 = IP, 1 = Frame
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

static int GroupRefsByFrame(StackRef* refs, int count, FrameRefGroup* groups, int maxGroups)
{
    if (count == 0)
        return 0;

    qsort(refs, count, sizeof(StackRef), CompareBySource);

    int groupCount = 0;
    CLRDATA_ADDRESS currentSource = refs[0].Source;
    int startIdx = 0;

    for (int i = 1; i <= count; i++)
    {
        if (i == count || refs[i].Source != currentSource)
        {
            if (groupCount < maxGroups)
            {
                groups[groupCount].Source = currentSource;
                groups[groupCount].SourceType = refs[startIdx].SourceType;
                groups[groupCount].StartIdx = startIdx;
                groups[groupCount].Count = i - startIdx;
                groupCount++;
            }
            if (i < count)
            {
                currentSource = refs[i].Source;
                startIdx = i;
            }
        }
    }
    return groupCount;
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
// Per-frame comparison + bucketing.
//
// Algorithm: group both ref sets by Source (managed PC for frameless JIT
// frames, Frame* for explicit Frames), merge-walk the two grouped lists, and
// per matching frame compare refs with CompareFrameRefs. Tracks "true" vs
// "deferred-known" mismatches so the caller can decide PASS / KNOWN_ISSUE / FAIL
// in one pass (without a separate flat-comparison + bucketing step).
//
// Mismatch classification:
//   - Frame in A only:                 true mismatch (A reported a Source B didn't)
//   - Frame in B only, Source deferred: known issue (cDAC intentionally skipped)
//   - Frame in B only, Source not deferred: true mismatch
//   - Same Source, refs don't match:   true mismatch (counts/values differ within a frame)
//
// emitLog: when true, also writes structured [COMPARE] / [FRAME_*] / [MATCH]
// lines to s_logFile. When false, the call is verdict-only (no I/O).
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Per-frame comparison.
//
// Algorithm: group both ref sets by Source (managed PC for frameless JIT
// frames, Frame* for explicit Frames), merge-walk the two grouped lists, and
// per matching frame compare refs with CompareFrameRefs. Captures full
// per-frame results (including per-ref disposition arrays) into the caller's
// FrameResult[] buffer. Pure data transform: no I/O, no counter side effects.
//
// Mismatch classification (mirrors RT-as-oracle):
//   - Frame in cDAC only:                    MISMATCH (cDAC reported a Source RT didn't)
//   - Frame in RT only, Source deferred:     KNOWN_NIE (cDAC intentionally skipped)
//   - Frame in RT only, Source not deferred: MISMATCH
//   - Same Source, refs don't match:         MISMATCH (counts/values differ within a frame)
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

    // Per-ref disposition. Indices 0..min(*Count, MAX_REFS_PER_FRAME).
    // If a side's Count exceeds MAX_REFS_PER_FRAME, Truncated is set and the
    // overflow refs are unrendered (but still counted in *Count).
    RefDisposition  CdacDisp[MAX_REFS_PER_FRAME];
    RefDisposition  RtDisp[MAX_REFS_PER_FRAME];
    bool            Truncated;

    FrameOutcome    Outcome;
};

// TODO(debug-only): dump every slot reported by each side for a MISMATCH frame,
// sorted by Address, annotated with disposition. Discriminates slot-table vs
// live-state vs scratch-filter divergence. Impl in "Rendering helpers" section.
static void LogAllSlots(const FrameResult& fr, const StackRef* cdacBuf, const StackRef* runtimeRefsBuf);

// Helper used by CompareFrames to record per-ref disposition into a frame.
static void FillDisposition(RefDisposition* out, const bool* used, int count, bool isNie)
{
    int n = count < MAX_REFS_PER_FRAME ? count : MAX_REFS_PER_FRAME;
    for (int i = 0; i < n; i++)
    {
        if (used[i])
            out[i] = REF_MATCHED;
        else if (isNie)
            out[i] = REF_NIE;
        else
            out[i] = REF_ONLY;
    }
}

// Walks the grouped frames once and fills outResults[] with full per-frame
// data. Returns the number of frames populated (capped at outCap).
//
// Both ref arrays must be in source-order (qsort'd in GroupRefsByFrame).
// The disposition arrays inside each FrameResult are keyed on the ORIGINAL
// ref array indices via CdacStart / RtStart, so callers can index back into
// refsCdac / refsRt to retrieve the ref data when rendering.

// Emits a single WARN line per truncated frame. cDAC / RT counts of -1
// indicate "side not present" (used by the cDAC-only / RT-only paths).
static void LogRefTruncationWarning(CLRDATA_ADDRESS source, int cdacCount, int rtCount)
{
    if (cdacCount > MAX_REFS_PER_FRAME && rtCount > MAX_REFS_PER_FRAME)
    {
        CDAC_LOG("WARN: per-frame ref truncation Source=0x%llx cDAC=%d RT=%d cap=%d -- bump MAX_REFS_PER_FRAME\n",
                 (unsigned long long)source, cdacCount, rtCount, MAX_REFS_PER_FRAME);
    }
    else if (cdacCount > MAX_REFS_PER_FRAME)
    {
        CDAC_LOG("WARN: per-frame ref truncation Source=0x%llx cDAC=%d cap=%d -- bump MAX_REFS_PER_FRAME\n",
                 (unsigned long long)source, cdacCount, MAX_REFS_PER_FRAME);
    }
    else if (rtCount > MAX_REFS_PER_FRAME)
    {
        CDAC_LOG("WARN: per-frame ref truncation Source=0x%llx RT=%d cap=%d -- bump MAX_REFS_PER_FRAME\n",
                 (unsigned long long)source, rtCount, MAX_REFS_PER_FRAME);
    }
}

static int CompareFrames(
    StackRef* refsCdac, int countCdac,
    StackRef* refsRt,   int countRt,
    const CLRDATA_ADDRESS* deferred, int deferredCount,
    FrameResult* outResults, int outCap)
{
    static const int MAX_GROUPS = 256;
    FrameRefGroup groupsCdac[MAX_GROUPS], groupsRt[MAX_GROUPS];
    int numGroupsCdac = GroupRefsByFrame(refsCdac, countCdac, groupsCdac, MAX_GROUPS);
    int numGroupsRt   = GroupRefsByFrame(refsRt,   countRt,   groupsRt,   MAX_GROUPS);

    int idxCdac = 0, idxRt = 0;
    int resultCount = 0;

    auto addResult = [&]() -> FrameResult* {
        if (resultCount >= outCap)
            return nullptr;
        FrameResult* fr = &outResults[resultCount++];
        memset(fr, 0, sizeof(*fr));
        return fr;
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
            bool cUsed[MAX_REFS_PER_FRAME] = {};
            bool rUsed[MAX_REFS_PER_FRAME] = {};
            int truncated = (cC > MAX_REFS_PER_FRAME) || (cR > MAX_REFS_PER_FRAME);
            LogRefTruncationWarning(gC.Source, cC, cR);

            // CompareFrameRefs writes through aUsed/bUsed sized to MAX_REFS_PER_FRAME;
            // if a frame exceeds the cap we still run the matcher (so the counts are
            // correct) but mark the frame truncated for rendering.
            int matchC = cC < MAX_REFS_PER_FRAME ? cC : MAX_REFS_PER_FRAME;
            int matchR = cR < MAX_REFS_PER_FRAME ? cR : MAX_REFS_PER_FRAME;
            int unmatchedA = 0, unmatchedB = 0;
            CompareFrameRefs(&refsCdac[gC.StartIdx], matchC,
                             &refsRt[gR.StartIdx],   matchR,
                             &unmatchedA, &unmatchedB, cUsed, rUsed);

            if (fr != nullptr)
            {
                fr->Source     = gC.Source;
                fr->SourceType = gC.SourceType;
                fr->SP_cdac    = refsCdac[gC.StartIdx].StackPointer;
                fr->SP_rt      = refsRt[gR.StartIdx].StackPointer;
                fr->CdacStart  = gC.StartIdx;
                fr->CdacCount  = cC;
                fr->RtStart    = gR.StartIdx;
                fr->RtCount    = cR;
                fr->Truncated  = truncated;
                fr->Outcome    = (unmatchedA > 0 || unmatchedB > 0)
                                 ? FRAME_OUTCOME_MISMATCH
                                 : FRAME_OUTCOME_MATCH;
                FillDisposition(fr->CdacDisp, cUsed, cC, /*isNie=*/false);
                FillDisposition(fr->RtDisp,   rUsed, cR, /*isNie=*/false);
            }
            idxCdac++;
            idxRt++;
        }
        else if (cdacOnly)
        {
            FrameRefGroup& gC = groupsCdac[idxCdac];
            if (fr != nullptr)
            {
                fr->Source     = gC.Source;
                fr->SourceType = gC.SourceType;
                fr->SP_cdac    = refsCdac[gC.StartIdx].StackPointer;
                fr->SP_rt      = 0;
                fr->CdacStart  = gC.StartIdx;
                fr->CdacCount  = gC.Count;
                fr->RtStart    = 0;
                fr->RtCount    = 0;
                fr->Truncated  = gC.Count > MAX_REFS_PER_FRAME;
                fr->Outcome    = FRAME_OUTCOME_MISMATCH;
                LogRefTruncationWarning(gC.Source, gC.Count, 0);
                int n = gC.Count < MAX_REFS_PER_FRAME ? gC.Count : MAX_REFS_PER_FRAME;
                for (int i = 0; i < n; i++) fr->CdacDisp[i] = REF_ONLY;
            }
            idxCdac++;
        }
        else
        {
            // Frame only in RT. KNOWN_NIE iff Source is on the deferred list.
            FrameRefGroup& gR = groupsRt[idxRt];
            bool isKnownNie = IsDeferredFrame(gR.Source, deferred, deferredCount);
            if (fr != nullptr)
            {
                fr->Source     = gR.Source;
                fr->SourceType = gR.SourceType;
                fr->SP_cdac    = 0;
                fr->SP_rt      = refsRt[gR.StartIdx].StackPointer;
                fr->CdacStart  = 0;
                fr->CdacCount  = 0;
                fr->RtStart    = gR.StartIdx;
                fr->RtCount    = gR.Count;
                fr->Truncated  = gR.Count > MAX_REFS_PER_FRAME;
                fr->Outcome    = isKnownNie ? FRAME_OUTCOME_KNOWN_NIE
                                            : FRAME_OUTCOME_MISMATCH;
                LogRefTruncationWarning(gR.Source, 0, gR.Count);
                int n = gR.Count < MAX_REFS_PER_FRAME ? gR.Count : MAX_REFS_PER_FRAME;
                for (int i = 0; i < n; i++)
                    fr->RtDisp[i] = isKnownNie ? REF_NIE : REF_ONLY;
            }
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

//-----------------------------------------------------------------------------
// Filter interior stack pointers from a ref set in place.
//-----------------------------------------------------------------------------

static int FilterRefs(StackRef* refs, int count, Thread* pThread, uintptr_t stackLimit)
{
    return FilterInteriorStackRefs(refs, count, pThread, stackLimit);
}

//-----------------------------------------------------------------------------
// Extract CDAC_DEFERRED_FRAME sentinel entries from a cDAC ref array.
// Removes them from `refs` (shifting later elements down) and writes their
// Source addresses into `deferredOut`. Returns the new ref count and writes
// the count of extracted sentinels into *pDeferredCount.
//
// Sentinels are emitted by Microsoft.Diagnostics.DataContractReader.Contracts
// GcScanContext.RecordDeferredFrame when an explicit Frame is intentionally
// skipped because the cDAC code path is not implemented yet (typically
// PromoteCallerStack pending the ArgIterator port).
//-----------------------------------------------------------------------------

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
// Stress verification implementation: shared by all trigger-point
// specializations below. Compares cDAC vs runtime stack refs at the captured
// CONTEXT and records per-frame results.
//-----------------------------------------------------------------------------

static void VerifyAtStressPoint(Thread* pThread, PCONTEXT regs)
{
    _ASSERTE(s_initialized);
    _ASSERTE(pThread != nullptr);
    _ASSERTE(regs != nullptr);

    // Serialize cDAC access — the cDAC's ProcessedData cache and COM interfaces
    // are not thread-safe, and GC stress can fire on multiple threads.
    CrstHolder cdacLock(&s_cdacLock);

    DWORD osThreadId = pThread->GetOSThreadId();

    // =====================================================================
    // Phase A: Collect raw refs from both sides.
    //
    // No comparison or filtering happens here - just two independent walks
    // producing two SArray/buffer pairs that we'll reconcile in Phase B/C.
    // =====================================================================

    // A.1: cDAC side. ReadThreadContext callback state is wired here so the
    //      cDAC's IDacMemoryAccess shim can return the captured register
    //      context when it queries the active thread's CONTEXT.
    SArray<StackRef> cdacRefs;
    HRESULT cdacHr;
    {
        s_currentContext = regs;
        s_currentThreadId = osThreadId;

        // Flush only the cDAC's target-state caches (live process state can
        // change) while keeping immutable metadata caches (e.g. CoreLib type
        // info) populated across invocations.
        if (s_cdacProcess != nullptr)
            s_cdacProcess->Request(DACSTRESSPRIV_REQUEST_FLUSH_TARGET_STATE, 0, NULL, 0, NULL);

        cdacHr = CollectCdacStackRefs(s_cdacSosDac, osThreadId, &cdacRefs);

        s_currentContext = nullptr;
        s_currentThreadId = 0;
    }

    // A.2: Runtime side - the oracle. This is the GC's own ScanStackRoots
    //      walk that the cDAC must agree with.
    StackRef runtimeRefsBuf[MAX_COLLECTED_REFS];
    int runtimeCount = 0;
    HRESULT rtHr = CollectRuntimeStackRefs(pThread, regs, runtimeRefsBuf, &runtimeCount);

    // Early-exit reasons.
    if (FAILED(cdacHr))
    {
        InterlockedIncrement(&s_failCount);
        CDAC_LOG("[FAIL] Thread=0x%x IP=0x%p - cDAC GetStackReferences failed (hr=0x%08x)\n",
            osThreadId, (void*)GetIP(regs), cdacHr);
        return;
    }
    if (rtHr == S_FALSE)
    {
        CDAC_LOG("[WARN] Thread=0x%x IP=0x%p - RT overflow (>%d refs); comparison may be partial\n",
            osThreadId, (void*)GetIP(regs), MAX_COLLECTED_REFS);
    }

    // =====================================================================
    // Phase B: Normalize.
    //
    // Both sides report semantically equivalent data but with several
    // representational differences. Each step below addresses one such
    // difference; the goal is two ref sets that can be compared directly
    // in Phase C.
    // =====================================================================

    // B.1: Compute the live-stack upper bound. PromoteCarefully (siginfo.cpp)
    //      drops interior pointers whose value lies in the live stack region
    //      [topStack, ...). We need the same threshold to mirror that filter
    //      on the cDAC side in step B.3.
    Frame* pTopFrame = pThread->GetFrame();
    Object** topStack = (Object**)pTopFrame;
    if (InlinedCallFrame::FrameHasActiveCall(pTopFrame))
    {
        InlinedCallFrame* pInlinedFrame = dac_cast<PTR_InlinedCallFrame>(pTopFrame);
        topStack = (Object**)pInlinedFrame->GetCallSiteSP();
    }
    uintptr_t stackLimit = (uintptr_t)topStack;

    // B.2: Extract CDAC_DEFERRED_FRAME sentinels from the cDAC ref set.
    //      These are markers (not real refs) emitted when the cDAC intentionally
    //      skips a Frame whose scan code path is not implemented yet (e.g.
    //      ArgIterator-dependent paths). Their Source addresses are used in
    //      Phase C to re-classify per-frame diffs as known issues.
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

    // B.3: Filter the cDAC side.
    //
    //      - Interior-into-stack filter: the runtime's PromoteCarefully drops
    //        interior pointers whose value lies in the live stack. The cDAC
    //        reports raw GcInfo slots (no such filter), so we mirror it here.
    if (cdacCount > 0)
    {
        StackRef* buf = cdacRefs.OpenRawBuffer();
        cdacCount = FilterRefs(buf, cdacCount, pThread, stackLimit);
        cdacRefs.CloseRawBuffer();
    }

    // B.4: No dedup on the runtime side. The runtime's GcInfo decoder
    //      reports each slot exactly once per frame walk, and per-frame
    //      grouping in Phase C scopes comparisons correctly without
    //      collapsing distinct (Addr, Obj, Flags) tuples that legitimately
    //      appear in multiple frames (e.g. caller-saved area overlap).

    // =====================================================================
    // Phase C: Compare per-frame.
    //
    // Group both sides by Source (PC for frameless, Frame* for explicit),
    // merge-walk the grouped lists, run CompareFrameRefs within each frame.
    // Mismatches confined to a deferred Source (B.2) downgrade to
    // KNOWN_NIE rather than MISMATCH.
    //
    // CompareFrames is a pure data transform — no I/O, no counter updates.
    // ComputeVerdict walks the result array once to bump global counters
    // and derive the pass/known/fail verdict.
    // =====================================================================
    static const int MAX_FRAMES = 256;
    FrameResult frameResults[MAX_FRAMES];
    StackRef* cdacBuf = cdacRefs.OpenRawBuffer();
    int frameCount = CompareFrames(
        cdacBuf, cdacCount,
        runtimeRefsBuf, runtimeCount,
        deferredFrames, deferredFrameCount,
        frameResults, MAX_FRAMES);
    CompareVerdict verdict = ComputeVerdict(frameResults, frameCount);

    // =====================================================================
    // Phase D: Bucket the outcome + (on mismatch) emit hierarchical
    // diagnostics: a self-contained block per broken frame followed by
    // one stack trace at the end.
    // =====================================================================
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

            CDAC_LOG("  Frame #%d %s [%s] cDAC=%d RT=%d SP_cDAC=0x%llx SP_RT=0x%llx%s%s\n",
                i, methodName, outcomeName, fr.CdacCount, fr.RtCount,
                (unsigned long long)fr.SP_cdac, (unsigned long long)fr.SP_rt,
                spNote, fr.Truncated ? " (truncated)" : "");

            // Per-ref dump. Verbose -> all refs; concise -> only non-MATCHED
            // refs (which is the actionable signal — what diverges).
            int nC = fr.CdacCount < MAX_REFS_PER_FRAME ? fr.CdacCount : MAX_REFS_PER_FRAME;
            for (int j = 0; j < nC; j++)
            {
                if (!verbose && fr.CdacDisp[j] == REF_MATCHED)
                    continue;
                LogRef(fr.CdacDisp[j], cdacBuf[fr.CdacStart + j]);
            }
            int nR = fr.RtCount < MAX_REFS_PER_FRAME ? fr.RtCount : MAX_REFS_PER_FRAME;
            for (int j = 0; j < nR; j++)
            {
                if (!verbose && fr.RtDisp[j] == REF_MATCHED)
                    continue;
                LogRef(fr.RtDisp[j], runtimeRefsBuf[fr.RtStart + j]);
            }

            // TODO(debug-only): drill into the unmatched stack-slot arithmetic.
            // Dump every slot reported by each side for the mismatch frame,
            // sorted by Address. Discriminates slot-table vs live-state vs
            // scratch-filter divergence.
            if (verbose && fr.Outcome == FRAME_OUTCOME_MISMATCH && fr.RtCount > 0)
            {
                LogAllSlots(fr, cdacBuf, runtimeRefsBuf);
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

    if (sourceType != 0) // SOS_StackSourceFrame
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

// Dump EVERY slot reported by both cDAC and RT for a single MISMATCH frame,
// each list independently sorted by Address. Annotates each slot with its
// disposition (MATCH / ONLY / NIE) so the reader can spot:
//   - same positional rank, different addresses -> slot-table decode bug
//   - addresses interleaved differently         -> live-state decode bug
//   - one side reports a slot in [SP, SP+scratch) the other skipped
//                                                -> scratch-filter bug
// Caller gates on (verbose && Outcome == MISMATCH).
static void LogAllSlots(const FrameResult& fr, const StackRef* cdacBuf, const StackRef* runtimeRefsBuf)
{
    auto emitSide = [&](const char* sideTag, const StackRef* base, int start, int count,
                        const RefDisposition* disp)
    {
        int n = count < MAX_REFS_PER_FRAME ? count : MAX_REFS_PER_FRAME;
        if (n <= 0) return;

        int idx[MAX_REFS_PER_FRAME];
        for (int i = 0; i < n; i++) idx[i] = i;

        // Insertion sort by Address (bounded n, debug-only path)
        for (int i = 1; i < n; i++)
        {
            int key = idx[i];
            CLRDATA_ADDRESS keyAddr = base[start + key].Address;
            int j = i - 1;
            while (j >= 0 && base[start + idx[j]].Address > keyAddr)
            {
                idx[j + 1] = idx[j];
                j--;
            }
            idx[j + 1] = key;
        }

        for (int i = 0; i < n; i++)
        {
            const StackRef& r = base[start + idx[i]];
            char flagBuf[64];
            FormatRefFlags(r.Flags, flagBuf, ARRAY_SIZE(flagBuf));
            const char* regName = RegisterName(r.Register);
            CDAC_LOG("    [ALL_SLOTS %s] #%d %-9s Addr=0x%llx Obj=0x%llx Flags=%s Reg=%d(%s) Off=%d\n",
                sideTag, idx[i], DispositionName(disp[idx[i]]),
                (unsigned long long)r.Address, (unsigned long long)r.Object,
                flagBuf, r.Register, regName, r.Offset);
        }
    };

    CDAC_LOG("    [ALL_SLOTS] cDAC=%d RT=%d (sorted by Address; #N = original slot index)\n",
        fr.CdacCount, fr.RtCount);
    emitSide("cDAC", cdacBuf, fr.CdacStart, fr.CdacCount, fr.CdacDisp);
    emitSide("RT  ", runtimeRefsBuf, fr.RtStart, fr.RtCount, fr.RtDisp);
}

#endif // CDAC_STRESS
