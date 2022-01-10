// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// EEConfig.H
//

//
// Fetched configuration data from the registry (should we Jit, run GC checks ...)
//
//



#ifndef EECONFIG_H
#define EECONFIG_H

class MethodDesc;

#include "shash.h"
#include "corhost.h"

#ifdef _DEBUG
class TypeNamesList
{
    class TypeName
    {
        LPUTF8      typeName;
        TypeName *next;           // Next name

        friend class TypeNamesList;
    };

    TypeName     *pNames;         // List of names

public:
    TypeNamesList();
    ~TypeNamesList();

    HRESULT Init(_In_z_ LPCWSTR str);
    bool IsInList(LPCUTF8 typeName);
};
#endif

enum { OPT_BLENDED,
    OPT_SIZE,
    OPT_SPEED,
    OPT_RANDOM,
    OPT_DEFAULT = OPT_BLENDED };

enum ParseCtl {
    parseAll,               // parse entire config file
    stopAfterRuntimeSection // stop after <runtime>...</runtime> section
};

class EEConfig
{
public:
    static HRESULT Setup();

    HRESULT Init();
    HRESULT Cleanup();

    // Spinning heuristics

    DWORD         SpinInitialDuration(void)       const {LIMITED_METHOD_CONTRACT;  return dwSpinInitialDuration; }
    DWORD         SpinBackoffFactor(void)         const {LIMITED_METHOD_CONTRACT;  return dwSpinBackoffFactor; }
    DWORD         SpinLimitProcCap(void)          const {LIMITED_METHOD_CONTRACT;  return dwSpinLimitProcCap; }
    DWORD         SpinLimitProcFactor(void)       const {LIMITED_METHOD_CONTRACT;  return dwSpinLimitProcFactor; }
    DWORD         SpinLimitConstant(void)         const {LIMITED_METHOD_CONTRACT;  return dwSpinLimitConstant; }
    DWORD         SpinRetryCount(void)            const {LIMITED_METHOD_CONTRACT;  return dwSpinRetryCount; }
    DWORD         MonitorSpinCount(void)          const {LIMITED_METHOD_CONTRACT;  return dwMonitorSpinCount; }

    // Jit-config

    DWORD         JitHostMaxSlabCache(void)                 const {LIMITED_METHOD_CONTRACT;  return dwJitHostMaxSlabCache; }
    bool          GetTrackDynamicMethodDebugInfo(void)      const {LIMITED_METHOD_CONTRACT;  return fTrackDynamicMethodDebugInfo; }
    unsigned int  GenOptimizeType(void)                     const {LIMITED_METHOD_CONTRACT;  return iJitOptimizeType; }
    bool          JitFramed(void)                           const {LIMITED_METHOD_CONTRACT;  return fJitFramed; }
    bool          JitMinOpts(void)                          const {LIMITED_METHOD_CONTRACT;  return fJitMinOpts; }

    // Tiered Compilation config
#if defined(FEATURE_TIERED_COMPILATION)
    bool          TieredCompilation(void)           const { LIMITED_METHOD_CONTRACT;  return fTieredCompilation; }
    bool          TieredCompilation_QuickJit() const { LIMITED_METHOD_CONTRACT; return fTieredCompilation_QuickJit; }
    bool          TieredCompilation_QuickJitForLoops() const { LIMITED_METHOD_CONTRACT; return fTieredCompilation_QuickJitForLoops; }
    DWORD         TieredCompilation_BackgroundWorkerTimeoutMs() const { LIMITED_METHOD_CONTRACT; return tieredCompilation_BackgroundWorkerTimeoutMs; }
    bool          TieredCompilation_CallCounting()  const { LIMITED_METHOD_CONTRACT; return fTieredCompilation_CallCounting; }
    UINT16        TieredCompilation_CallCountThreshold() const { LIMITED_METHOD_CONTRACT; return tieredCompilation_CallCountThreshold; }
    DWORD         TieredCompilation_CallCountingDelayMs() const { LIMITED_METHOD_CONTRACT; return tieredCompilation_CallCountingDelayMs; }
    bool          TieredCompilation_UseCallCountingStubs() const { LIMITED_METHOD_CONTRACT; return fTieredCompilation_UseCallCountingStubs; }
    DWORD         TieredCompilation_DeleteCallCountingStubsAfter() const { LIMITED_METHOD_CONTRACT; return tieredCompilation_DeleteCallCountingStubsAfter; }
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT)
    // OSR Config
    DWORD         OSR_CounterBump() const { LIMITED_METHOD_CONTRACT; return dwOSR_CounterBump; }
    DWORD         OSR_HitLimit() const { LIMITED_METHOD_CONTRACT; return dwOSR_HitLimit; }
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT) && defined(_DEBUG)
    DWORD         OSR_LowId() const { LIMITED_METHOD_CONTRACT; return dwOSR_LowId; }
    DWORD         OSR_HighId() const { LIMITED_METHOD_CONTRACT; return dwOSR_HighId; }
#endif

    bool          BackpatchEntryPointSlots() const { LIMITED_METHOD_CONTRACT; return backpatchEntryPointSlots; }

#if defined(FEATURE_GDBJIT) && defined(_DEBUG)
    inline bool ShouldDumpElfOnMethod(LPCUTF8 methodName) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(methodName, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszGDBJitElfDump, methodName);
    }
#endif // FEATURE_GDBJIT && _DEBUG

#if defined(FEATURE_GDBJIT_FRAME)
    inline bool ShouldEmitDebugFrame(void) const {LIMITED_METHOD_CONTRACT; return fGDBJitEmitDebugFrame;}
#endif // FEATURE_GDBJIT_FRAME
    BOOL PInvokeRestoreEsp(BOOL fDefault) const
    {
        LIMITED_METHOD_CONTRACT;

        switch (fPInvokeRestoreEsp)
        {
            case (unsigned)-1: return fDefault;
            case            0: return FALSE;
            default          : return TRUE;
        }
    }

    bool InteropValidatePinnedObjects()             const { LIMITED_METHOD_CONTRACT;  return m_fInteropValidatePinnedObjects; }
    bool InteropLogArguments()                      const { LIMITED_METHOD_CONTRACT;  return m_fInteropLogArguments; }

#ifdef _DEBUG
    bool GenDebuggableCode(void)                    const {LIMITED_METHOD_CONTRACT;  return fDebuggable; }

    bool ShouldExposeExceptionsInCOMToConsole()     const {LIMITED_METHOD_CONTRACT;  return (iExposeExceptionsInCOM & 1) != 0; }
    bool ShouldExposeExceptionsInCOMToMsgBox()      const {LIMITED_METHOD_CONTRACT;  return (iExposeExceptionsInCOM & 2) != 0; }

    static bool RegexOrExactMatch(LPCUTF8 regex, LPCUTF8 input);

    inline bool ShouldPrestubHalt(MethodDesc* pMethodInfo) const
    {
        WRAPPER_NO_CONTRACT;
        return IsInMethList(pPrestubHalt, pMethodInfo);
    }

    inline bool ShouldInvokeHalt(MethodDesc* pMethodInfo) const
    {
        WRAPPER_NO_CONTRACT;
        return IsInMethList(pInvokeHalt, pMethodInfo);
    }


    inline bool ShouldPrestubGC(MethodDesc* pMethodInfo) const
    {
        WRAPPER_NO_CONTRACT;
        return IsInMethList(pPrestubGC, pMethodInfo);
    }
    inline bool ShouldBreakOnClassLoad(LPCUTF8 className) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(className, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnClassLoad, className);
    }
    inline bool ShouldBreakOnClassBuild(LPCUTF8 className) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(className, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnClassBuild, className);
    }
    inline bool BreakOnInstantiationEnabled() const
    {
        LIMITED_METHOD_CONTRACT;
        return pszBreakOnInstantiation != NULL;
    }
    inline bool ShouldBreakOnInstantiation(LPCUTF8 className) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(className, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnInstantiation, className);
    }
    inline bool ShouldBreakOnMethod(LPCUTF8 methodName) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(methodName, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnMethodName, methodName);
    }
    inline bool ShouldDumpOnClassLoad(LPCUTF8 className) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(className, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszDumpOnClassLoad, className);
    }
    inline bool ShouldBreakOnInteropStubSetup(LPCUTF8 methodName) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(methodName, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnInteropStubSetup, methodName);
    }
    inline bool ShouldBreakOnComToClrNativeInfoInit(LPCUTF8 methodName) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(methodName, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnComToClrNativeInfoInit, methodName);
    }
    inline bool ShouldBreakOnStructMarshalSetup(LPCUTF8 className) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(className, NULL_OK));
        } CONTRACTL_END
        return RegexOrExactMatch(pszBreakOnStructMarshalSetup, className);
    }
    static HRESULT ParseTypeList(_In_z_ LPWSTR str, TypeNamesList** out);
    static void DestroyTypeList(TypeNamesList* list);

    inline bool ShouldGcCoverageOnMethod(LPCUTF8 methodName) const
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            PRECONDITION(CheckPointer(methodName, NULL_OK));
        } CONTRACTL_END
        return (pszGcCoverageOnMethod == 0 || methodName == 0 || RegexOrExactMatch(pszGcCoverageOnMethod, methodName));
    }

    bool IsJitVerificationDisabled(void)    const {LIMITED_METHOD_CONTRACT;  return fJitVerificationDisable; }

#ifdef FEATURE_EH_FUNCLETS
    bool SuppressLockViolationsOnReentryFromOS() const {LIMITED_METHOD_CONTRACT;  return fSuppressLockViolationsOnReentryFromOS; }
#endif

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    bool IsStubLinkerUnwindInfoVerificationOn() const { LIMITED_METHOD_CONTRACT; return fStubLinkerUnwindInfoVerificationOn; }
#endif

#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    inline bool LogCCWRefCountChangeEnabled()
    {
        LIMITED_METHOD_CONTRACT;
        return bLogCCWRefCountChange;
    }

    void SetLogCCWRefCountChangeEnabled(bool newVal);
    bool ShouldLogCCWRefCountChange(LPCUTF8 pszClassName, LPCUTF8 pszNamespace) const;

    inline bool EnableRCWCleanupOnSTAShutdown()
    {
        LIMITED_METHOD_CONTRACT;
        return fEnableRCWCleanupOnSTAShutdown;
    }

    bool IsBuiltInCOMSupported() const { LIMITED_METHOD_CONTRACT;  return m_fBuiltInCOMInteropSupported; }
#endif // FEATURE_COMINTEROP

#ifdef _DEBUG
    bool ExpandModulesOnLoad(void) const { LIMITED_METHOD_CONTRACT; return fExpandAllOnLoad; }
#endif //_DEBUG

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    // Because the large object heap is 8 byte aligned, we want to put
    // arrays of doubles there more agressively than normal objects.
    // This is the threshold for this.  It is the number of doubles,
    // not the number of bytes in the array.
    unsigned int  GetDoubleArrayToLargeObjectHeapThreshold() const { LIMITED_METHOD_CONTRACT; return DoubleArrayToLargeObjectHeapThreshold; }
#endif

    inline bool ProbeForStackOverflow() const
    {
        LIMITED_METHOD_CONTRACT;
        return fProbeForStackOverflow;
    }

#ifdef TEST_DATA_CONSISTENCY
    // get the value of fTestDataConsistency, which controls whether we test that we can correctly detect
    // held locks in DAC builds. This is determined by an environment variable.
    inline bool TestDataConsistency() const { LIMITED_METHOD_DAC_CONTRACT; return fTestDataConsistency; }
#endif

#ifdef _DEBUG

    unsigned SuspendThreadDeadlockTimeoutMs() const
    {LIMITED_METHOD_CONTRACT; return m_SuspendThreadDeadlockTimeoutMs; }

    unsigned SuspendDeadlockTimeout() const
    {LIMITED_METHOD_CONTRACT; return m_SuspendDeadlockTimeout; }

    // Verifier
    bool    IsVerifierOff()                 const {LIMITED_METHOD_CONTRACT;  return fVerifierOff; }

    inline bool fAssertOnBadImageFormat() const
    {LIMITED_METHOD_CONTRACT;  return m_fAssertOnBadImageFormat; }

    inline bool fAssertOnFailFast() const
    {LIMITED_METHOD_CONTRACT;  return m_fAssertOnFailFast; }

    inline bool SuppressChecks() const
    {LIMITED_METHOD_CONTRACT;  return fSuppressChecks; }

    inline bool EnableFullDebug() const
    {LIMITED_METHOD_CONTRACT;  return fEnableFullDebug; }

#endif
#ifdef ENABLE_STARTUP_DELAY
    inline int StartupDelayMS()
    { LIMITED_METHOD_CONTRACT; return iStartupDelayMS; }
#endif

#ifdef VERIFY_HEAP
    // GC config
    enum HeapVerifyFlags {
        HEAPVERIFY_NONE             = 0,
        HEAPVERIFY_GC               = 1,   // Verify the heap at beginning and end of GC
        HEAPVERIFY_BARRIERCHECK     = 2,   // Verify the brick table
        HEAPVERIFY_SYNCBLK          = 4,   // Verify sync block scanning

        // the following options can be used to mitigate some of the overhead introduced
        // by heap verification.  some options might cause heap verifiction to be less
        // effective depending on the scenario.

        HEAPVERIFY_NO_RANGE_CHECKS  = 0x10,   // Excludes checking if an OBJECTREF is within the bounds of the managed heap
        HEAPVERIFY_NO_MEM_FILL      = 0x20,   // Excludes filling unused segment portions with fill pattern
        HEAPVERIFY_POST_GC_ONLY     = 0x40,   // Performs heap verification post-GCs only (instead of before and after each GC)
        HEAPVERIFY_DEEP_ON_COMPACT  = 0x80    // Performs deep object verfication only on compacting GCs.
    };

    int     GetHeapVerifyLevel()                  {LIMITED_METHOD_CONTRACT;  return iGCHeapVerify;  }

    bool    IsHeapVerifyEnabled()           const {LIMITED_METHOD_CONTRACT;  return iGCHeapVerify != 0; }
#endif

#if defined(STRESS_HEAP) || defined(_DEBUG)
    void    SetGCStressLevel(int val)             {LIMITED_METHOD_CONTRACT;  iGCStress = val;  }

    enum  GCStressFlags {
        GCSTRESS_NONE               = 0,
        GCSTRESS_ALLOC              = 1,    // GC on all allocs and 'easy' places
        GCSTRESS_TRANSITION         = 2,    // GC on transitions to preemtive GC
        GCSTRESS_INSTR_JIT          = 4,    // GC on every allowable JITed instr
        GCSTRESS_INSTR_NGEN         = 8,    // GC on every allowable NGEN instr
        GCSTRESS_UNIQUE             = 16,   // GC only on a unique stack trace
    };

    GCStressFlags GetGCStressLevel()        const { WRAPPER_NO_CONTRACT; SUPPORTS_DAC; return GCStressFlags(iGCStress); }
#endif

    bool    IsGCBreakOnOOMEnabled()         const {LIMITED_METHOD_CONTRACT; return fGCBreakOnOOM; }

    int     GetGCconcurrent()               const {LIMITED_METHOD_CONTRACT; return iGCconcurrent; }
    void    SetGCconcurrent(int val)              {LIMITED_METHOD_CONTRACT; iGCconcurrent = val;  }
    int     GetGCRetainVM ()                const {LIMITED_METHOD_CONTRACT; return iGCHoardVM;}
    DWORD   GetGCLOHThreshold()             const {LIMITED_METHOD_CONTRACT; return iGCLOHThreshold;}

#ifdef FEATURE_CONSERVATIVE_GC
    bool    GetGCConservative()             const {LIMITED_METHOD_CONTRACT; return iGCConservative;}
#endif
#ifdef HOST_64BIT
    bool    GetGCAllowVeryLargeObjects()    const {LIMITED_METHOD_CONTRACT; return iGCAllowVeryLargeObjects;}
#endif
#ifdef _DEBUG
    bool    SkipGCCoverage(LPCUTF8 assemblyName) const {WRAPPER_NO_CONTRACT; return (pSkipGCCoverageList != NULL
                                                                                    && pSkipGCCoverageList->IsInList(assemblyName));}
#endif

#ifdef _DEBUG
    inline DWORD FastGCStressLevel() const
    {LIMITED_METHOD_CONTRACT;  return iFastGCStress;}

    inline DWORD InjectFatalError() const
    {
        LIMITED_METHOD_CONTRACT;
        return iInjectFatalError;
    }
#endif


#ifdef _DEBUG
    // Interop config
    int     GetTraceWrapper()               const {LIMITED_METHOD_CONTRACT;  return m_TraceWrapper;      }
#endif

    // Loader
    bool    ExcludeReadyToRun(LPCUTF8 assemblyName) const;

    bool    NgenBindOptimizeNonGac()        const { LIMITED_METHOD_CONTRACT; return fNgenBindOptimizeNonGac; }

    LPUTF8  GetZapBBInstr()                 const { LIMITED_METHOD_CONTRACT; return szZapBBInstr; }
    LPWSTR  GetZapBBInstrDir()              const { LIMITED_METHOD_CONTRACT; return szZapBBInstrDir; }
    DWORD   DisableStackwalkCache()         const {LIMITED_METHOD_CONTRACT;  return dwDisableStackwalkCache; }

    bool    StressLog()                     const { LIMITED_METHOD_CONTRACT; return fStressLog; }
    bool    ForceEnc()                      const { LIMITED_METHOD_CONTRACT; return fForceEnc; }
    bool    DebugAssembliesModifiable()     const { LIMITED_METHOD_CONTRACT; return fDebugAssembliesModifiable; }

    // Optimizations to improve working set

    HRESULT sync();    // check the registry again and update local state

#ifdef _DEBUG
    // GC alloc logging
    bool ShouldLogAlloc(const char *pClass) const { LIMITED_METHOD_CONTRACT; return pPerfTypesToLog && pPerfTypesToLog->IsInList(pClass);}
    int AllocSizeThreshold()                const {LIMITED_METHOD_CONTRACT;  return iPerfAllocsSizeThreshold; }
    int AllocNumThreshold()                 const { LIMITED_METHOD_CONTRACT; return iPerfNumAllocsThreshold;  }

#endif // _DEBUG

#ifdef _DEBUG
    DWORD  NgenForceFailureMask()     { LIMITED_METHOD_CONTRACT; return dwNgenForceFailureMask; }
    DWORD  NgenForceFailureCount()    { LIMITED_METHOD_CONTRACT; return dwNgenForceFailureCount; }
    DWORD  NgenForceFailureKind()     { LIMITED_METHOD_CONTRACT; return dwNgenForceFailureKind;  }
#endif

#ifdef _DEBUG

    DWORD GetHostTestThreadAbort() const {LIMITED_METHOD_CONTRACT; return testThreadAbort;}

#define INJECTFAULT_LOADERHEAP      0x1
#define INJECTFAULT_GCHEAP          0x2
#define INJECTFAULT_SO              0x4
#define INJECTFAULT_GMHEAP          0x8
#define INJECTFAULT_DYNAMICCODEHEAP 0x10
#define INJECTFAULT_MAPVIEWOFFILE   0x20
#define INJECTFAULT_JITHEAP         0x40

    DWORD ShouldInjectFault(DWORD faultType) const {LIMITED_METHOD_CONTRACT; return fShouldInjectFault & faultType;}

#endif

private: //----------------------------------------------------------------

    bool fInited;                   // have we synced to the registry at least once?

    // Jit-config

    DWORD dwJitHostMaxSlabCache;       // max size for jit host slab cache
    bool fTrackDynamicMethodDebugInfo; //  Enable/Disable tracking dynamic method debug info
    bool fJitFramed;                   // Enable/Disable EBP based frames
    bool fJitMinOpts;                  // Enable MinOpts for all jitted methods

    unsigned iJitOptimizeType; // 0=Blended,1=SmallCode,2=FastCode,              default is 0=Blended

    unsigned fPInvokeRestoreEsp;  // -1=Default, 0=Never, Else=Always

    LPUTF8 pszBreakOnClassLoad;         // Halt just before loading this class

#ifdef TEST_DATA_CONSISTENCY
    bool fTestDataConsistency;         // true if we are testing locks for data consistency in the debugger--
                                       // If a lock is held during inspection, we assume the data under the lock
                                       // is inconsistent. We have a special code path for testing this
                                       // which we will follow if this is set. The value is determined by
                                       // the environment variable TestDataConsistency
#endif

    bool   m_fInteropValidatePinnedObjects; // After returning from a M->U interop call, validate GC heap around objects pinned by IL stubs.
    bool   m_fInteropLogArguments; // Log all pinned arguments passed to an interop call

#ifdef _DEBUG
    static HRESULT ParseMethList(_In_z_ LPWSTR str, MethodNamesList* * out);
    static void DestroyMethList(MethodNamesList* list);
    static bool IsInMethList(MethodNamesList* list, MethodDesc* pMD);

    bool fDebuggable;

    MethodNamesList* pPrestubHalt;      // list of methods on which to break when hit prestub
    MethodNamesList* pPrestubGC;        // list of methods on which to cause a GC when hit prestub
    MethodNamesList* pInvokeHalt;      // list of methods on which to break when hit prestub


    LPUTF8 pszBreakOnClassBuild;         // Halt just before loading this class
    LPUTF8 pszBreakOnInstantiation;      // Halt just before instantiating a non-canonical generic type
    LPUTF8 pszBreakOnMethodName;         // Halt when doing something with this method in the class defined in ClassBuild
    LPUTF8 pszDumpOnClassLoad;           // Dump the class to the log

    LPUTF8 pszBreakOnInteropStubSetup;   // Halt before we set up the interop stub for a method
    LPUTF8 pszBreakOnComToClrNativeInfoInit; // Halt before we init the native info for a COM to CLR call
    LPUTF8 pszBreakOnStructMarshalSetup; // Halt before the field marshallers are set up for a struct

    bool   m_fAssertOnBadImageFormat;   // If false, don't assert on invalid IL (for testing)
    bool   m_fAssertOnFailFast;         // If false, don't assert if we detect a stack corruption

    bool   fConditionalContracts;       // Conditional contracts (off inside asserts)
    bool   fSuppressChecks;             // Disable checks (including contracts)

    DWORD  iExposeExceptionsInCOM;      // Should we exposed exceptions that will be transformed into HRs?

    unsigned m_SuspendThreadDeadlockTimeoutMs;  // Used in Thread::SuspendThread()
    unsigned m_SuspendDeadlockTimeout; // Used in Thread::SuspendRuntime.

    bool fEnableFullDebug;
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    bool bLogCCWRefCountChange;           // Is CCW logging on
    LPCUTF8 pszLogCCWRefCountChange;      // OutputDebugString when AddRef/Release is called on a CCW
                                          // for the specified type(s)
    bool fEnableRCWCleanupOnSTAShutdown;  // Register our IInitializeSpy even in classic processes
    bool m_fBuiltInCOMInteropSupported;   // COM built-in support
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    unsigned int DoubleArrayToLargeObjectHeapThreshold;  // double arrays of more than this number of elems go in large object heap
#endif

#ifdef _DEBUG
    bool fExpandAllOnLoad;              // True if we want to load all types/jit all methods in an assembly
                                        // at load time.
    bool fJitVerificationDisable;       // Turn off jit verification (for testing purposes only)


    // Verifier
    bool fVerifierOff;

#ifdef FEATURE_EH_FUNCLETS
    bool fSuppressLockViolationsOnReentryFromOS;
#endif

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    bool fStubLinkerUnwindInfoVerificationOn;
#endif
#endif // _DEBUG
#ifdef ENABLE_STARTUP_DELAY
    int iStartupDelayMS; //Adds sleep to startup.
#endif

    // Spinning heuristics
    DWORD dwSpinInitialDuration;
    DWORD dwSpinBackoffFactor;
    DWORD dwSpinLimitProcCap;
    DWORD dwSpinLimitProcFactor;
    DWORD dwSpinLimitConstant;
    DWORD dwSpinRetryCount;
    DWORD dwMonitorSpinCount;

#ifdef VERIFY_HEAP
    int  iGCHeapVerify;
#endif

#if defined(STRESS_HEAP) || defined(_DEBUG)
    int  iGCStress;
#endif

    int  iGCconcurrent;
    int  iGCHoardVM;
    DWORD iGCLOHThreshold;

#ifdef FEATURE_CONSERVATIVE_GC
    bool iGCConservative;
#endif // FEATURE_CONSERVATIVE_GC
#ifdef HOST_64BIT
    bool iGCAllowVeryLargeObjects;
#endif // HOST_64BIT

    bool fGCBreakOnOOM;

#ifdef _DEBUG
    DWORD iFastGCStress;
    LPUTF8 pszGcCoverageOnMethod;

    DWORD iInjectFatalError;

    AssemblyNamesList *pSkipGCCoverageList;
#endif

    // Assemblies which cannot use Ready to Run images.
    AssemblyNamesList * pReadyToRunExcludeList;

    bool fNgenBindOptimizeNonGac;

    bool fStressLog;
    bool fForceEnc;
    bool fDebugAssembliesModifiable;
    bool fProbeForStackOverflow;

    // Stackwalk optimization flag
    DWORD dwDisableStackwalkCache;

    LPUTF8 szZapBBInstr;
    LPWSTR szZapBBInstrDir;

#ifdef _DEBUG
    // interop logging
    int       m_TraceWrapper;
#endif

#ifdef _DEBUG
    // GC Alloc perf flags
    int iPerfNumAllocsThreshold;        // Start logging after this many allocations are made
    int iPerfAllocsSizeThreshold;       // Log allocations of this size or above
    TypeNamesList* pPerfTypesToLog;     // List of types whose allocations are to be logged

#endif // _DEBUG

#ifdef _DEBUG
    DWORD dwNgenForceFailureMask;
    DWORD dwNgenForceFailureCount;
    DWORD dwNgenForceFailureKind;
#endif

#ifdef _DEBUG
    DWORD fShouldInjectFault;
    DWORD testThreadAbort;
#endif

#if defined(FEATURE_TIERED_COMPILATION)
    bool fTieredCompilation;
    bool fTieredCompilation_QuickJit;
    bool fTieredCompilation_QuickJitForLoops;
    bool fTieredCompilation_CallCounting;
    bool fTieredCompilation_UseCallCountingStubs;
    UINT16 tieredCompilation_CallCountThreshold;
    DWORD tieredCompilation_BackgroundWorkerTimeoutMs;
    DWORD tieredCompilation_CallCountingDelayMs;
    DWORD tieredCompilation_DeleteCallCountingStubsAfter;
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT)
    DWORD dwOSR_HitLimit;
    DWORD dwOSR_CounterBump;
#endif

#if defined(FEATURE_ON_STACK_REPLACEMENT) && defined(_DEBUG)
    DWORD dwOSR_LowId;
    DWORD dwOSR_HighId;
#endif

    bool backpatchEntryPointSlots;

#if defined(FEATURE_GDBJIT) && defined(_DEBUG)
    LPCUTF8 pszGDBJitElfDump;
#endif // FEATURE_GDBJIT && _DEBUG

#if defined(FEATURE_GDBJIT_FRAME)
    bool fGDBJitEmitDebugFrame;
#endif
public:

    enum BitForMask {
        CallSite_1 = 0x0001,
        CallSite_2 = 0x0002,
        CallSite_3 = 0x0004,
        CallSite_4 = 0x0008,
        CallSite_5 = 0x0010,
        CallSite_6 = 0x0020,
        CallSite_7 = 0x0040,
        CallSite_8 = 0x0080,
    };

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    void DebugCheckAndForceIBCFailure(BitForMask bitForMask);
#endif

#if defined(_DEBUG)
#if defined(TARGET_AMD64)
private:

    // Defaults to 0, which means we will not generate long jump dispatch stubs.
    // But if this is set to a positive integer, then this
    // will be 1/x ration of stubs we generate as long jump. So if x is 4, then
    // every 1 in 4 dispatch stubs will be long jump stubs.
    size_t m_cGenerateLongJumpDispatchStubRatio;

    // Total count of stubs generated, used with above variable to determine if
    // the next stub should be a long jump.
    size_t m_cDispatchStubsGenerated;

public:
    BOOL ShouldGenerateLongJumpDispatchStub()
    {
        return (m_cDispatchStubsGenerated++ % m_cGenerateLongJumpDispatchStubRatio) == 0;
    }
#else
public:
    // Just return false when we're in DEBUG but not on AMD64
    BOOL ShouldGenerateLongJumpDispatchStub()
    {
        return FALSE;
    }
#endif // TARGET_AMD64
#endif // _DEBUG

#if defined(_DEBUG)
private:
    bool bDiagnosticSuspend;

public:
    bool GetDiagnosticSuspend()
    { return bDiagnosticSuspend; }
#endif

private:
    DWORD dwSleepOnExit;

public:
    DWORD GetSleepOnExit()
    { return dwSleepOnExit; }
};



#ifdef _DEBUG_IMPL

    // We actually want our asserts for illegal IL, but testers need to test that
    // we fail gracefully under those conditions.  Thus we have to hide them for those runs.
#define BAD_FORMAT_NOTHROW_ASSERT(str)                                  \
    do {                                                                \
        if (g_pConfig->fAssertOnBadImageFormat()) {                     \
            _ASSERTE(str);                                              \
        }                                                               \
        else if (!(str)) {                                              \
            if (IsDebuggerPresent()) DebugBreak();                      \
        }                                                               \
    } while(0)

#define FILE_FORMAT_CHECK_MSG(_condition, _message)                     \
    do {                                                                \
        if (g_pConfig != NULL && g_pConfig->fAssertOnBadImageFormat())  \
             ASSERT_CHECK(_condition, _message, "Bad file format");     \
        else if (!(_condition))                                         \
            DebugBreak();                                               \
    } while (0)

#define FILE_FORMAT_CHECK(_condition)  FILE_FORMAT_CHECK_MSG(_condition, "")

#else

#define BAD_FORMAT_NOTHROW_ASSERT(str)

#define FILE_FORMAT_CHECK_MSG(_condition, _message)
#define FILE_FORMAT_CHECK(_condition)

#endif

#endif // EECONFIG_H
