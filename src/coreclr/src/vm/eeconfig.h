// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

    HRESULT Init(__in_z LPCWSTR str);
    bool IsInList(LPCUTF8 typeName);
};
#endif

typedef struct _ConfigStringKeyValuePair
{
    WCHAR * key;
    WCHAR * value;
    
    _ConfigStringKeyValuePair()
    {
        key = NULL;
        value = NULL;
    }
    
    WCHAR * GetKey()
    {
        return key;
    }
} ConfigStringKeyValuePair;

typedef WStringSHash<ConfigStringKeyValuePair> ConfigStringHashtable;

class ConfigList;

//
// Holds a pointer to a hashtable that is populated with data from config files.
// Also acts as a node for a circular doubly-linked list.
//
class ConfigSource
{
    friend class ConfigList;
public:
    ConfigSource();
    ~ConfigSource();

    ConfigStringHashtable* Table();

    //
    // Connect this node into the list that prev is in.
    //
    void Add(ConfigSource* prev);

    ConfigSource* Next()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pNext;
    }

    ConfigSource* Previous()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pPrev;
    }


private:
    ConfigStringHashtable m_Table;
    ConfigSource *m_pNext;
    ConfigSource *m_pPrev;
};

//
// Wrapper around the ConfigSource circular doubly-linked list.
//
class ConfigList
{
public:
    //
    // Iterator  for traversing through a ConfigList.
    //
    class ConfigIter
    {
    public:
        ConfigIter(ConfigList* pList)
        {
            CONTRACTL {
                NOTHROW;
                GC_NOTRIGGER;
                // MODE_ANY;
                FORBID_FAULT;
                SO_TOLERANT; 
            } CONTRACTL_END;
            
            pEnd = &(pList->m_pElement);
            pCurrent = pEnd;
        }

        //
        // TODO: Check if iterating through the list once skips an element.
        // Returns the next node. If the next node is the head, returns null.
        // Note that iteration can be resumed by calling next again.
        //
        ConfigStringHashtable* Next()
        {
            CONTRACT (ConfigStringHashtable*) {
                NOTHROW;
                GC_NOTRIGGER;
                // MODE_ANY;
                FORBID_FAULT;
                SO_TOLERANT; 
                POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            } CONTRACT_END;
        
            pCurrent = pCurrent->Next();;
            if(pCurrent == pEnd)
                RETURN NULL;
            else
                RETURN pCurrent->Table();
        }

        ConfigStringHashtable* Previous()
        {
            CONTRACT (ConfigStringHashtable*) {
                NOTHROW;
                GC_NOTRIGGER;
                FORBID_FAULT;
                // MODE_ANY;
                SO_TOLERANT; 
                POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            } CONTRACT_END;

            pCurrent = pCurrent->Previous();
            if(pCurrent == pEnd)
                RETURN NULL;
            else
                RETURN pCurrent->Table();
        }

    private:
        ConfigSource* pEnd;
        ConfigSource* pCurrent;
    };

    ConfigStringHashtable* Add()
    {
        CONTRACT (ConfigStringHashtable*) {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        } CONTRACT_END;

        ConfigSource* pEntry = new (nothrow) ConfigSource();

        if (pEntry == NULL)
            RETURN NULL;
        
        pEntry->Add(&m_pElement);
        RETURN pEntry->Table();
    }

    ConfigStringHashtable* Append()
    {
        CONTRACT (ConfigStringHashtable*) {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        } CONTRACT_END;
        
        ConfigSource* pEntry = new (nothrow) ConfigSource();
        if (pEntry == NULL)
            RETURN NULL;
        
        pEntry->Add(m_pElement.Previous());
        RETURN pEntry->Table();
    }

    void Append(ConfigSource * pEntry)
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(CheckPointer(pEntry));

        pEntry->Add(m_pElement.Previous());
    }

    ~ConfigList()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            FORBID_FAULT;
        } CONTRACTL_END;

        ConfigSource* pNext = m_pElement.Next();
        while(pNext != &m_pElement) {
            ConfigSource *last = pNext;
            pNext = pNext->m_pNext;
            delete last;
        }
    }

friend class ConfigIter;

private:
    ConfigSource m_pElement;
};

enum { OPT_BLENDED,
    OPT_SIZE,
    OPT_SPEED,
    OPT_RANDOM,
    OPT_DEFAULT = OPT_BLENDED };

/* Control of impersonation flow:
    FASTFLOW means that impersonation is flowed only if it has been achieved through managed means. This is the default and avoids a kernel call.
    NOFLOW is the Everett default where we don't flow the impersonation at all
    ALWAYSFLOW is the (potentially) slow mode where we will always flow the impersonation, regardless of how it was achieved (managed or p/invoke). Includes
    a kernel call.
    Keep in sync with values in SecurityContext.cs
    */
enum { 
    IMP_FASTFLOW = 0,
    IMP_NOFLOW = 1,
    IMP_ALWAYSFLOW = 2,
    IMP_DEFAULT = IMP_FASTFLOW };

enum ParseCtl {
    parseAll,               // parse entire config file
    stopAfterRuntimeSection // stop after <runtime>...</runtime> section
};

extern CorHostProtectionManager s_CorHostProtectionManager;

class EEConfig
{
public:
    typedef enum {
        CONFIG_SYSTEM,
        CONFIG_APPLICATION,
        CONFIG_SYSTEMONLY
    } ConfigSearch;

    static HRESULT Setup();

    void *operator new(size_t size);

    HRESULT Init();
    HRESULT Cleanup();

    // Spinning heuristics

    DWORD         SpinInitialDuration(void)       const {LIMITED_METHOD_CONTRACT;  return dwSpinInitialDuration; }
    DWORD         SpinBackoffFactor(void)         const {LIMITED_METHOD_CONTRACT;  return dwSpinBackoffFactor; }
    DWORD         SpinLimitProcCap(void)          const {LIMITED_METHOD_CONTRACT;  return dwSpinLimitProcCap; }
    DWORD         SpinLimitProcFactor(void)       const {LIMITED_METHOD_CONTRACT;  return dwSpinLimitProcFactor; }
    DWORD         SpinLimitConstant(void)         const {LIMITED_METHOD_CONTRACT;  return dwSpinLimitConstant; }
    DWORD         SpinRetryCount(void)            const {LIMITED_METHOD_CONTRACT;  return dwSpinRetryCount; }

    // Jit-config
    
    unsigned int  GenOptimizeType(void)             const {LIMITED_METHOD_CONTRACT;  return iJitOptimizeType; }
    bool          JitFramed(void)                   const {LIMITED_METHOD_CONTRACT;  return fJitFramed; }
    bool          JitAlignLoops(void)               const {LIMITED_METHOD_CONTRACT;  return fJitAlignLoops; }
    bool          AddRejitNops(void)                const {LIMITED_METHOD_DAC_CONTRACT;  return fAddRejitNops; }
    bool          JitMinOpts(void)                  const {LIMITED_METHOD_CONTRACT;  return fJitMinOpts; }
    
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

    bool LegacyNullReferenceExceptionPolicy(void)   const {LIMITED_METHOD_CONTRACT;  return fLegacyNullReferenceExceptionPolicy; }
    bool LegacyUnhandledExceptionPolicy(void)       const {LIMITED_METHOD_CONTRACT;  return fLegacyUnhandledExceptionPolicy; }
    bool LegacyVirtualMethodCallVerification(void)  const {LIMITED_METHOD_CONTRACT;  return fLegacyVirtualMethodCallVerification; }

    bool LegacyApartmentInitPolicy(void)            const {LIMITED_METHOD_CONTRACT;  return fLegacyApartmentInitPolicy; }
    bool LegacyComHierarchyVisibility(void)         const {LIMITED_METHOD_CONTRACT;  return fLegacyComHierarchyVisibility; }
    bool LegacyComVTableLayout(void)                const {LIMITED_METHOD_CONTRACT;  return fLegacyComVTableLayout; }
    bool NewComVTableLayout(void)                   const {LIMITED_METHOD_CONTRACT;  return fNewComVTableLayout; }

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // Returns a bool to indicate if the legacy CSE (pre-v4) behaviour is enabled or not
    bool LegacyCorruptedStateExceptionsPolicy(void) const {LIMITED_METHOD_CONTRACT;  return fLegacyCorruptedStateExceptionsPolicy; }
#endif // FEATURE_CORRUPTING_EXCEPTIONS
    
    // SECURITY
    unsigned    ImpersonationMode(void)           const 
    { 
        CONTRACTL 
        {
            NOTHROW;
            GC_NOTRIGGER;
            // MODE_ANY;
            SO_TOLERANT;
        } CONTRACTL_END;
        return iImpersonationPolicy ; 
    }
    void    SetLegacyImpersonationPolicy()              { LIMITED_METHOD_CONTRACT; iImpersonationPolicy = IMP_NOFLOW; }
    void    SetAlwaysFlowImpersonationPolicy()              { LIMITED_METHOD_CONTRACT; iImpersonationPolicy = IMP_ALWAYSFLOW; }

#ifdef _DEBUG
    bool LogTransparencyErrors() const { LIMITED_METHOD_CONTRACT; return fLogTransparencyErrors; }
    bool DisableTransparencyEnforcement() const { LIMITED_METHOD_CONTRACT; return fLogTransparencyErrors; }
#endif // _DEBUG

    void SetLegacyLoadMscorsnOnStartup(bool val) { LIMITED_METHOD_CONTRACT; fLegacyLoadMscorsnOnStartup = val; }
    bool LegacyLoadMscorsnOnStartup(void) const { LIMITED_METHOD_CONTRACT; return fLegacyLoadMscorsnOnStartup; }
    bool BypassTrustedAppStrongNames() const { LIMITED_METHOD_CONTRACT; return fBypassStrongNameVerification; } // See code:AssemblySecurityDescriptor::ResolveWorker#StrongNameBypass
    bool GeneratePublisherEvidence(void) const { LIMITED_METHOD_CONTRACT; return fGeneratePublisherEvidence; }
    bool EnforceFIPSPolicy() const { LIMITED_METHOD_CONTRACT; return fEnforceFIPSPolicy; }
    bool LegacyHMACMode() const { LIMITED_METHOD_CONTRACT; return fLegacyHMACMode; }

#ifdef FEATURE_COMINTEROP
    bool ComInsteadOfManagedRemoting()              const {LIMITED_METHOD_CONTRACT;  return m_fComInsteadOfManagedRemoting; } 
#endif //FEATURE_COMINTEROP
    bool InteropValidatePinnedObjects()             const { LIMITED_METHOD_CONTRACT;  return m_fInteropValidatePinnedObjects; }
    bool InteropLogArguments()                      const { LIMITED_METHOD_CONTRACT;  return m_fInteropLogArguments; }

#ifdef _DEBUG
    bool GenDebuggableCode(void)                    const {LIMITED_METHOD_CONTRACT;  return fDebuggable; }
    bool IsStressOn(void)                           const {LIMITED_METHOD_CONTRACT;  return fStressOn; }
    int GetAPIThreadStressCount(void)               const {LIMITED_METHOD_CONTRACT;  return apiThreadStressCount; }
    bool TlbImpSkipLoading()                        const {LIMITED_METHOD_CONTRACT;  return fTlbImpSkipLoading; }

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
    static HRESULT ParseTypeList(__in_z LPWSTR str, TypeNamesList** out);
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

#ifdef WIN64EXCEPTIONS
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
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_CORECLR
    bool VerifyModulesOnLoad(void) const { LIMITED_METHOD_CONTRACT; return fVerifyAllOnLoad; }
#endif
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

#ifdef FEATURE_LOADER_OPTIMIZATION
    inline DWORD DefaultSharePolicy() const
    {
        LIMITED_METHOD_CONTRACT;
        return dwSharePolicy;
    }
#endif

    inline bool CacheBindingFailures() const
    {
        LIMITED_METHOD_CONTRACT;
        return fCacheBindingFailures;
    }

    inline bool UseLegacyIdentityFormat() const
    {
        LIMITED_METHOD_CONTRACT;
        return fUseLegacyIdentityFormat;
    }

    inline bool DisableFusionUpdatesFromADManager() const
    {
        LIMITED_METHOD_CONTRACT;
        return fDisableFusionUpdatesFromADManager;
    }

    inline void SetDisableCommitThreadStack(bool val)
    {
        LIMITED_METHOD_CONTRACT; 
        fDisableCommitThreadStack = val;
    }

    inline bool GetDisableCommitThreadStack() const
    {
        LIMITED_METHOD_CONTRACT; 
        return fDisableCommitThreadStack;
    }

    inline bool ProbeForStackOverflow() const
    {
        LIMITED_METHOD_CONTRACT;
        return fProbeForStackOverflow;
    }

    inline bool AppDomainUnload() const
    {LIMITED_METHOD_CONTRACT;  return fAppDomainUnload; }

    inline DWORD AppDomainUnloadRetryCount() const
    {LIMITED_METHOD_CONTRACT;  return dwADURetryCount; }
    

#ifdef _DEBUG
    inline bool AppDomainLeaks() const
    {LIMITED_METHOD_DAC_CONTRACT;  return fAppDomainLeaks; }
#endif

    inline bool DeveloperInstallation() const
    {LIMITED_METHOD_CONTRACT;  return m_fDeveloperInstallation; }

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

    inline bool Do_AllowUntrustedCaller_Checks()
    {LIMITED_METHOD_CONTRACT;  return fDoAllowUntrustedCallerChecks; }

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

#ifdef _DEBUG // TRACE_GC

    int     GetGCtraceStart()               const {LIMITED_METHOD_CONTRACT; return iGCtraceStart;  }
    int     GetGCtraceEnd  ()               const {LIMITED_METHOD_CONTRACT;  return iGCtraceEnd;   }
    int     GetGCtraceFac  ()               const {LIMITED_METHOD_CONTRACT;  return iGCtraceFac;   }
    int     GetGCprnLvl    ()               const {LIMITED_METHOD_CONTRACT;  return iGCprnLvl;     }
    
#endif

#ifdef STRESS_HEAP

    bool    IsGCStressMix  ()               const {LIMITED_METHOD_CONTRACT;  return iGCStressMix != 0;}
    int     GetGCStressStep()               const {LIMITED_METHOD_CONTRACT;  return iGCStressStep; }
#endif

    bool    IsGCBreakOnOOMEnabled()         const {LIMITED_METHOD_CONTRACT;  return fGCBreakOnOOM; }

    size_t  GetGCgen0size  ()               const {LIMITED_METHOD_CONTRACT;  return iGCgen0size;   }
    void    SetGCgen0size  (size_t iSize)   {LIMITED_METHOD_CONTRACT; iGCgen0size = iSize;   }
    size_t  GetSegmentSize ()               const {LIMITED_METHOD_CONTRACT;  return iGCSegmentSize; }
    void    SetSegmentSize (size_t iSize)   {LIMITED_METHOD_CONTRACT;  iGCSegmentSize = iSize; }

    int     GetGCconcurrent()               const {LIMITED_METHOD_CONTRACT;  return iGCconcurrent; }
    void    SetGCconcurrent(int val)              {LIMITED_METHOD_CONTRACT;  iGCconcurrent = val;  }
#ifdef _DEBUG
    int     GetGCLatencyMode()              const {LIMITED_METHOD_CONTRACT;  return iGCLatencyMode; }
#endif //_DEBUG
    int     GetGCForceCompact()             const {LIMITED_METHOD_CONTRACT; return iGCForceCompact; }
    int     GetGCRetainVM ()                const {LIMITED_METHOD_CONTRACT; return iGCHoardVM;}
    int     GetGCLOHCompactionMode()        const {LIMITED_METHOD_CONTRACT; return iGCLOHCompactionMode;}

#ifdef GCTRIMCOMMIT

    int     GetGCTrimCommit()               const {LIMITED_METHOD_CONTRACT; return iGCTrimCommit;}

#endif

#ifdef FEATURE_CONSERVATIVE_GC
    bool    GetGCConservative()             const {LIMITED_METHOD_CONTRACT; return iGCConservative;}
#endif
#ifdef _WIN64
    bool    GetGCAllowVeryLargeObjects()    const {LIMITED_METHOD_CONTRACT; return iGCAllowVeryLargeObjects;}
#endif
#ifdef _DEBUG
    bool    SkipGCCoverage(LPCUTF8 assemblyName) const {WRAPPER_NO_CONTRACT; return (pSkipGCCoverageList != NULL 
                                                                                    && pSkipGCCoverageList->IsInList(assemblyName));}
#endif


    // thread stress: number of threads to run
#ifdef STRESS_THREAD
    DWORD GetStressThreadCount ()           const {LIMITED_METHOD_CONTRACT; return dwStressThreadCount;}
#endif

#ifdef _DEBUG
    inline DWORD FastGCStressLevel() const
    {LIMITED_METHOD_CONTRACT;  return iFastGCStress;}

    inline DWORD InjectFatalError() const
    {
        LIMITED_METHOD_CONTRACT;
        return iInjectFatalError;
    }

    inline BOOL SaveThreadInfo() const
    {
        return fSaveThreadInfo;
    }

    inline DWORD SaveThreadInfoMask() const
    {
        return dwSaveThreadInfoMask;
    }
#endif


#ifdef _DEBUG
    // Interop config
    IUnknown* GetTraceIUnknown()            const {LIMITED_METHOD_CONTRACT;  return m_pTraceIUnknown; }
    int     GetTraceWrapper()               const {LIMITED_METHOD_CONTRACT;  return m_TraceWrapper;      }
#endif

    // Loader

    enum RequireZapsType
    {
        REQUIRE_ZAPS_NONE,      // Dont care if native image is used or not
        REQUIRE_ZAPS_ALL,       // All assemblies must have native images
        REQUIRE_ZAPS_ALL_JIT_OK,// All assemblies must have native images, but its OK if the JIT-compiler also gets used (if some function was not ngenned)
        REQUIRE_ZAPS_SUPPORTED, // All assemblies must have native images, unless the loader does not support the scenario. Its OK if the JIT-compiler also gets used
        
        REQUIRE_ZAPS_COUNT
    };
    RequireZapsType RequireZaps()           const {LIMITED_METHOD_CONTRACT;  return iRequireZaps; }
    bool    RequireZap(LPCUTF8 assemblyName) const;
#ifdef _DEBUG
    bool    ForbidZap(LPCUTF8 assemblyName) const;
#endif

#ifdef _TARGET_AMD64_
    bool    DisableNativeImageLoad(LPCUTF8 assemblyName) const;
    bool    IsDisableNativeImageLoadListNonEmpty() const { LIMITED_METHOD_CONTRACT; return (pDisableNativeImageLoadList != NULL); }
#endif
    
    LPCWSTR ZapSet()                        const { LIMITED_METHOD_CONTRACT; return pZapSet; }

    bool    NgenBindOptimizeNonGac()        const { LIMITED_METHOD_CONTRACT; return fNgenBindOptimizeNonGac; }

    LPUTF8  GetZapBBInstr()                 const { LIMITED_METHOD_CONTRACT; return szZapBBInstr; }
    LPWSTR  GetZapBBInstrDir()              const { LIMITED_METHOD_CONTRACT; return szZapBBInstrDir; }
    DWORD   DisableStackwalkCache()         const {LIMITED_METHOD_CONTRACT;  return dwDisableStackwalkCache; }
    DWORD   UseNewCrossDomainRemoting()     const { LIMITED_METHOD_CONTRACT; return fUseNewCrossDomainRemoting; }

    bool    StressLog()                     const { LIMITED_METHOD_CONTRACT; return fStressLog; }
    bool    ForceEnc()                      const { LIMITED_METHOD_CONTRACT; return fForceEnc; }

    // Optimizations to improve working set

    HRESULT sync();    // check the registry again and update local state

    // Helpers to read configuration
    
    // This function exposes the config file data to CLRConfig. A pointer to this function is passed into CLRConfig on EEConfig::init.
    // We are using BOOLs instead of ConfigSearch for direction since CLRConfig isn't always linked to EEConfig.
    static HRESULT GetConfigValueCallback(__in_z LPCWSTR pKey, __deref_out_opt LPCWSTR* value, BOOL systemOnly, BOOL applicationFirst);

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead. 
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    // 
    static HRESULT GetConfigString_DontUse_(__in_z LPCWSTR name, __deref_out_z LPWSTR*out, BOOL fPrependCOMPLUS = TRUE,
                                  ConfigSearch direction = CONFIG_SYSTEM); // Note that you own the returned string!

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead. 
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    // 
    static DWORD GetConfigDWORD_DontUse_(__in_z LPCWSTR name, DWORD defValue,
                                DWORD level=(DWORD) REGUTIL::COR_CONFIG_ALL,
                                BOOL fPrependCOMPLUS = TRUE,
                                ConfigSearch direction = CONFIG_SYSTEM);

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead. 
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    // 
    static ULONGLONG GetConfigULONGLONG_DontUse_(__in_z LPCWSTR name, ULONGLONG defValue,
                                             DWORD level=(DWORD) REGUTIL::COR_CONFIG_ALL,
                                             BOOL fPrependCOMPLUS = TRUE,
                                             ConfigSearch direction = CONFIG_SYSTEM);
    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead. 
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    // 
    static DWORD GetConfigDWORDFavoringConfigFile_DontUse_(__in_z LPCWSTR name, DWORD defValue,
                                                  DWORD level=(DWORD) REGUTIL::COR_CONFIG_ALL,
                                                  BOOL fPrependCOMPLUS = TRUE,
                                                  ConfigSearch direction = CONFIG_SYSTEM);

    //
    // NOTE: The following function is deprecated; use the CLRConfig class instead. 
    // To access a configuration value through CLRConfig, add an entry in file:../inc/CLRConfigValues.h.
    // 
    static DWORD GetConfigFlag_DontUse_(__in_z LPCWSTR name, DWORD bitToSet, bool defValue = FALSE);

#ifdef _DEBUG
    // GC alloc logging
    bool ShouldLogAlloc(const char *pClass) const { LIMITED_METHOD_CONTRACT; return pPerfTypesToLog && pPerfTypesToLog->IsInList(pClass);}
    int AllocSizeThreshold()                const {LIMITED_METHOD_CONTRACT;  return iPerfAllocsSizeThreshold; }
    int AllocNumThreshold()                 const { LIMITED_METHOD_CONTRACT; return iPerfNumAllocsThreshold;  }

#endif // _DEBUG

    enum NgenHardBindType
    {
        NGEN_HARD_BIND_NONE,    // Do not hardbind at all
        NGEN_HARD_BIND_LIST,    // Only hardbind to what is specified by CustomAttributes (and any default assemblies specified by the CLR)
        NGEN_HARD_BIND_ALL,     // Hardbind to any existing ngen images if possible
        NGEN_HARD_BIND_COUNT,
        
        NGEN_HARD_BIND_DEFAULT = NGEN_HARD_BIND_LIST,
    };
    
    NgenHardBindType NgenHardBind()   { LIMITED_METHOD_CONTRACT; return iNgenHardBind;    }

#ifdef _DEBUG
    DWORD  NgenForceFailureMask()     { LIMITED_METHOD_CONTRACT; return dwNgenForceFailureMask; }
    DWORD  NgenForceFailureCount()    { LIMITED_METHOD_CONTRACT; return dwNgenForceFailureCount; }
    DWORD  NgenForceFailureKind()     { LIMITED_METHOD_CONTRACT; return dwNgenForceFailureKind;  }
#endif
    enum GCPollType
    {
        GCPOLL_TYPE_DEFAULT,    // Use the default gc poll for the platform
        GCPOLL_TYPE_HIJACK,     // Depend on thread hijacking for gc suspension
        GCPOLL_TYPE_POLL,       // Emit function calls to a helper for GC Poll
        GCPOLL_TYPE_INLINE,     // Emit inlined tests to the helper for GC Poll
        GCPOLL_TYPE_COUNT
    };
    GCPollType GetGCPollType() { LIMITED_METHOD_CONTRACT; return iGCPollType; }

#ifdef _DEBUG
    BOOL ShouldGenerateStubForHost() const {LIMITED_METHOD_CONTRACT; return fGenerateStubForHost;}
    void DisableGenerateStubForHost() {LIMITED_METHOD_CONTRACT; fGenerateStubForHost = FALSE;}

    DWORD GetHostTestADUnload() const {LIMITED_METHOD_CONTRACT; return testADUnload;}

    DWORD GetHostTestThreadAbort() const {LIMITED_METHOD_CONTRACT; return testThreadAbort;}

#define INJECTFAULT_LOADERHEAP      0x1
#define INJECTFAULT_HANDLETABLE     0x1
#define INJECTFAULT_GCHEAP          0x2
#define INJECTFAULT_SO              0x4
#define INJECTFAULT_GMHEAP          0x8
#define INJECTFAULT_DYNAMICCODEHEAP 0x10
#define INJECTFAULT_MAPVIEWOFFILE   0x20
#define INJECTFAULT_JITHEAP         0x40

    DWORD ShouldInjectFault(DWORD faultType) const {LIMITED_METHOD_CONTRACT; return fShouldInjectFault & faultType;}
    
#endif

private: //----------------------------------------------------------------

    // @TODO - Fusion needs to be able to read this value, but they are unable to
    // pull in all of the appropriate headers for all of the #defines found below.
    // As long as this is defined at the top of the object, the "incorrect offsets" that
    // will come as a result won't matter.
    bool fCacheBindingFailures;
    bool fUseLegacyIdentityFormat;
    bool fDisableFusionUpdatesFromADManager;
    bool fInited;                   // have we synced to the registry at least once?

    // Jit-config

    bool fJitFramed;           // Enable/Disable EBP based frames
    bool fJitAlignLoops;       // Enable/Disable loop alignment
    bool fAddRejitNops;        // Enable/Disable nop padding for rejit.          default is true
    bool fJitMinOpts;          // Enable MinOpts for all jitted methods

    unsigned iJitOptimizeType; // 0=Blended,1=SmallCode,2=FastCode,              default is 0=Blended
    
    unsigned fPInvokeRestoreEsp;  // -1=Default, 0=Never, Else=Always

    bool fLegacyNullReferenceExceptionPolicy; // Old AV's as NullRef behavior
    bool fLegacyUnhandledExceptionPolicy;     // Old unhandled exception policy (many are swallowed)
    bool fLegacyVirtualMethodCallVerification;  // Old (pre-whidbey) policy for call (nonvirt) of virtual function

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    bool fLegacyCorruptedStateExceptionsPolicy;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    bool fLegacyApartmentInitPolicy;          // Old nondeterministic COM apartment initialization switch
    bool fLegacyComHierarchyVisibility;       // Old behavior allowing QIs for classes with invisible parents
    bool fLegacyComVTableLayout;              // Old behavior passing out IClassX interface for IUnknown and IDispatch.
    bool fNewComVTableLayout;                 // New behavior passing out Basic interface for IUnknown and IDispatch.
    
    // SECURITY
    unsigned  iImpersonationPolicy; //control flow of impersonation in the SecurityContext. 0=FASTFLOW 1=
#ifdef _DEBUG
    bool fLogTransparencyErrors;            // don't throw on transparency errors, instead log to the CLR log file
#endif // _DEBUG
    bool fLegacyLoadMscorsnOnStartup; // load mscorsn.dll when starting up the runtime.
    bool fBypassStrongNameVerification;     // bypass strong name verification of trusted app assemblies
    bool fGeneratePublisherEvidence;        // verify Authenticode signatures of assemblies during load, generating publisher evidence for them
    bool fEnforceFIPSPolicy;                // enforce that only FIPS certified crypto algorithms are created if the FIPS machine settting is enabled
    bool fLegacyHMACMode;                   // HMACSHA384 and HMACSHA512 should default to the Whidbey block size

    LPUTF8 pszBreakOnClassLoad;         // Halt just before loading this class

#ifdef TEST_DATA_CONSISTENCY
    bool fTestDataConsistency;         // true if we are testing locks for data consistency in the debugger--
                                       // If a lock is held during inspection, we assume the data under the lock
                                       // is inconsistent. We have a special code path for testing this
                                       // which we will follow if this is set. The value is determined by
                                       // the environment variable TestDataConsistency
#endif

#ifdef FEATURE_COMINTEROP
    bool   m_fComInsteadOfManagedRemoting; // When communicating with a cross app domain CCW, use COM instead of managed remoting.
#endif
    bool   m_fInteropValidatePinnedObjects; // After returning from a M->U interop call, validate GC heap around objects pinned by IL stubs.
    bool   m_fInteropLogArguments; // Log all pinned arguments passed to an interop call

#ifdef _DEBUG
    static HRESULT ParseMethList(__in_z LPWSTR str, MethodNamesList* * out);
    static void DestroyMethList(MethodNamesList* list);
    static bool IsInMethList(MethodNamesList* list, MethodDesc* pMD);

    bool fDebuggable;
    bool fStressOn;
    int apiThreadStressCount;

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

    bool   fAppDomainLeaks;             // Enable appdomain leak detection for object refs

    bool   m_fAssertOnBadImageFormat;   // If false, don't assert on invalid IL (for testing)
    bool   m_fAssertOnFailFast;         // If false, don't assert if we detect a stack corruption

    bool   fConditionalContracts;       // Conditional contracts (off inside asserts)
    bool   fSuppressChecks;             // Disable checks (including contracts)

    DWORD  iExposeExceptionsInCOM;      // Should we exposed exceptions that will be transformed into HRs?

    // Tlb Tools
    bool fTlbImpSkipLoading;

    unsigned m_SuspendThreadDeadlockTimeoutMs;  // Used in Thread::SuspendThread()
    unsigned m_SuspendDeadlockTimeout; // Used in Thread::SuspendRuntime. 

    bool fEnableFullDebug;
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    bool bLogCCWRefCountChange;           // Is CCW logging on 
    LPCUTF8 pszLogCCWRefCountChange;      // OutputDebugString when AddRef/Release is called on a CCW
                                          // for the specified type(s)
    bool fEnableRCWCleanupOnSTAShutdown;  // Register our IInitializeSpy even in classic processes
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_DOUBLE_ALIGNMENT_HINT
    unsigned int DoubleArrayToLargeObjectHeapThreshold;  // double arrays of more than this number of elems go in large object heap
#endif

#ifdef FEATURE_LOADER_OPTIMIZATION
    DWORD  dwSharePolicy;               // Default policy for loading assemblies into the domain neutral area
#endif

    // Only developer machines are allowed to use DEVPATH. This value is set when there is an appropriate entry
    // in the machine configuration file. This should not be sent out in the redist.
    bool   m_fDeveloperInstallation;      // We are on a developers machine
    bool   fAppDomainUnload;            // Enable appdomain unloading
    
#ifdef FEATURE_CORECLR
    bool fVerifyAllOnLoad;              // True if we want to verify all methods in an assembly at load time.
#endif //FEATURE_CORECLR

    DWORD  dwADURetryCount;

#ifdef _DEBUG
    bool fExpandAllOnLoad;              // True if we want to load all types/jit all methods in an assembly
                                        // at load time.
    bool fJitVerificationDisable;       // Turn off jit verification (for testing purposes only)


    // Verifier
    bool fVerifierOff;

    bool fDoAllowUntrustedCallerChecks; // do AllowUntrustedCallerChecks

#ifdef WIN64EXCEPTIONS
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

#ifdef VERIFY_HEAP
    int  iGCHeapVerify;
#endif

#ifdef _DEBUG // TRACE_GC

    int  iGCtraceStart;
    int  iGCtraceEnd;
    int  iGCtraceFac;
    int  iGCprnLvl;
    
#endif

#if defined(STRESS_HEAP) || defined(_DEBUG)
    int  iGCStress;
#endif

#ifdef STRESS_HEAP
    int  iGCStressMix;
    int  iGCStressStep;
#endif

#define DEFAULT_GC_PRN_LVL 3
    size_t iGCgen0size;
    size_t iGCSegmentSize;
    int  iGCconcurrent;
#ifdef _DEBUG
    int  iGCLatencyMode;
#endif //_DEBUG
    int  iGCForceCompact;
    int  iGCHoardVM;
    int  iGCLOHCompactionMode;

#ifdef GCTRIMCOMMIT

    int  iGCTrimCommit;

#endif

#ifdef FEATURE_CONSERVATIVE_GC
    bool iGCConservative;
#endif // FEATURE_CONSERVATIVE_GC
#ifdef _WIN64
    bool iGCAllowVeryLargeObjects;
#endif // _WIN64

    bool fGCBreakOnOOM;

#ifdef  STRESS_THREAD
    DWORD dwStressThreadCount;
#endif

#ifdef _DEBUG
    DWORD iFastGCStress;
    LPUTF8 pszGcCoverageOnMethod;

    DWORD iInjectFatalError;

    BOOL fSaveThreadInfo;
    DWORD dwSaveThreadInfoMask;
    
    AssemblyNamesList *pSkipGCCoverageList;
#endif

    RequireZapsType iRequireZaps;
    // Assemblies which need to have native images. 
    // This is only used if iRequireZaps!=REQUIRE_ZAPS_NONE
    // This can be used to enforce that ngen images are used only selectively for some assemblies
    AssemblyNamesList * pRequireZapsList;
    // assemblies which need NOT have native images.
    // This is only used if iRequireZaps!=REQUIRE_ZAPS_NONE
    // This overrides pRequireZapsList.
    AssemblyNamesList * pRequireZapsExcludeList;

#ifdef _DEBUG
    // Exact opposite of require zaps
    BOOL iForbidZaps;
    AssemblyNamesList * pForbidZapsList;
    AssemblyNamesList * pForbidZapsExcludeList;
#endif

#ifdef _TARGET_AMD64_
    // Assemblies for which we will not load a native image. This is from the COMPLUS_DisableNativeImageLoadList
    // variable / reg key. It performs the same function as the config file key "<disableNativeImageLoad>" (except
    // that is it just a list of assembly names, which the config file key can specify full assembly identities).
    // This was added to support COMPLUS_UseLegacyJit, to support the rollout of RyuJIT to replace JIT64, where
    // the user can cause the CLR to fall back to JIT64 for JITting but not for NGEN. This allows the user to
    // force JITting for a specified list of NGEN assemblies.
    AssemblyNamesList * pDisableNativeImageLoadList;
#endif

    LPCWSTR pZapSet;

    bool fNgenBindOptimizeNonGac;

    bool fStressLog;
    bool fForceEnc;
    bool fDisableCommitThreadStack;
    bool fProbeForStackOverflow;
    
    // Stackwalk optimization flag
    DWORD dwDisableStackwalkCache;

    // New cross domain remoting
    DWORD fUseNewCrossDomainRemoting;
    
    LPUTF8 szZapBBInstr;
    LPWSTR szZapBBInstrDir;

#ifdef _DEBUG
    // interop logging
    IUnknown* m_pTraceIUnknown;
    int       m_TraceWrapper;
#endif

    // Flag to keep track of memory
    int     m_fFreepZapSet;

#ifdef _DEBUG
    // GC Alloc perf flags
    int iPerfNumAllocsThreshold;        // Start logging after this many allocations are made
    int iPerfAllocsSizeThreshold;       // Log allocations of this size or above
    TypeNamesList* pPerfTypesToLog;     // List of types whose allocations are to be logged

#endif // _DEBUG

    // New configuration
    ConfigList  m_Configuration;

    BOOL fEnableHardbinding;
    NgenHardBindType iNgenHardBind;
#ifdef _DEBUG
    DWORD dwNgenForceFailureMask;
    DWORD dwNgenForceFailureCount;
    DWORD dwNgenForceFailureKind;
#endif

    GCPollType iGCPollType;

#ifdef _DEBUG
    BOOL fGenerateStubForHost;
    DWORD fShouldInjectFault;
    DWORD testADUnload;
    DWORD testThreadAbort;
#endif

public:
#ifndef FEATURE_CORECLR // unimpactful install --> no config files
    HRESULT ImportConfigurationFile(
        ConfigStringHashtable* pTable,
        LPCWSTR pszFileName,
        LPCWSTR version,
        ParseCtl parseCtl = parseAll);

    HRESULT AppendConfigurationFile(
        LPCWSTR pszFileName,
        LPCWSTR version,
        ParseCtl parseCtl = parseAll); 

    HRESULT SetupConfiguration();
#endif // FEATURE_CORECLR

    HRESULT GetConfiguration_DontUse_(__in_z LPCWSTR pKey, ConfigSearch direction, __deref_out_opt LPCWSTR* value);
    LPCWSTR  GetProcessBindingFile();  // All flavors must support this method
    SIZE_T  GetSizeOfProcessBindingFile();  // All flavors must support this method

    DWORD GetConfigDWORDInternal_DontUse_ (__in_z LPCWSTR name, DWORD defValue,    //for getting data in the constructor of EEConfig
                                    DWORD level=(DWORD) REGUTIL::COR_CONFIG_ALL,
                                    BOOL fPrependCOMPLUS = TRUE,
                                    ConfigSearch direction = CONFIG_SYSTEM);

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
#if defined(_TARGET_AMD64_)
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
#endif // _TARGET_AMD64_
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

#if FEATURE_APPX
private:
    DWORD dwWindows8ProfileAPICheckFlag;

public:
    DWORD GetWindows8ProfileAPICheckFlag() { return dwWindows8ProfileAPICheckFlag; }
#endif
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

    // STRESS_ASSERT is meant to be temperary additions to the code base that stop the
    // runtime quickly when running stress
#define STRESS_ASSERT(cond)   do { if (!(cond) && g_pConfig->IsStressOn())  DebugBreak();    } while(0)

#define FILE_FORMAT_CHECK_MSG(_condition, _message)                     \
    do {                                                                \
        if (g_pConfig != NULL && g_pConfig->fAssertOnBadImageFormat())  \
             ASSERT_CHECK(_condition, _message, "Bad file format");     \
        else if (!(_condition))                                         \
            DebugBreak();                                               \
    } while (0)

#define FILE_FORMAT_CHECK(_condition)  FILE_FORMAT_CHECK_MSG(_condition, "")

#else

#define STRESS_ASSERT(cond)
#define BAD_FORMAT_NOTHROW_ASSERT(str)

#define FILE_FORMAT_CHECK_MSG(_condition, _message)
#define FILE_FORMAT_CHECK(_condition)

#endif

void InitHostProtectionManager();

extern BYTE g_CorHostProtectionManagerInstance[];

inline CorHostProtectionManager* GetHostProtectionManager()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
//        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    return (CorHostProtectionManager*)g_CorHostProtectionManagerInstance;
}

extern BOOL g_CLRPolicyRequested;

// NGENImagesAllowed is the safe way to determine if NGEN Images are allowed to be loaded. (Defined as
// a macro instead of an inlined function to avoid compilation errors due to dependent
// definitions not being available to this header.)
#ifdef PROFILING_SUPPORTED
#define NGENImagesAllowed()                                                                                     \
    (g_fAllowNativeImages &&                /* No one disabled use of native images */                          \
    !(CORProfilerDisableAllNGenImages()))   /* Profiler didn't explicitly refuse NGEN images */
#else
#define NGENImagesAllowed()                                                                                     \
    (g_fAllowNativeImages)
#endif

#endif // EECONFIG_H
