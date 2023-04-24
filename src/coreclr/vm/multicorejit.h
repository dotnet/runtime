// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: MultiCoreJIT.h
//

//
// Multicore JIT interface to other part of the VM (Thread, AppDomain, JIT)
//
// ======================================================================================

#ifndef __MULTICORE_JIT_H__
#define __MULTICORE_JIT_H__

class MulticoreJitRecorder;
class MulticoreJitProfilePlayer;


class MulticoreJitCounter
{
    volatile LONG m_nValue;

public:
    MulticoreJitCounter()
    {
        LIMITED_METHOD_CONTRACT;

        m_nValue = 0;
    }

    inline LONG GetValue() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_nValue;
    }

    LONG Increment()
    {
        LIMITED_METHOD_CONTRACT;

        return InterlockedIncrement(& m_nValue);
    }

    LONG Decrement()
    {
        LIMITED_METHOD_CONTRACT;

        return InterlockedDecrement(& m_nValue);
    }
};


// Statistics, information shared by recorder and player
struct MulticoreJitPlayerStat
{
    unsigned short    m_nTotalMethod;
    unsigned short    m_nHasNativeCode;
    unsigned short    m_nTryCompiling;
    unsigned short    m_nFilteredMethods;
    unsigned short    m_nMissingModuleSkip;
    unsigned short    m_nTotalDelay;
    unsigned short    m_nDelayCount;
    unsigned short    m_nWalkBack;

    HRESULT           m_hr;

    void Clear()
    {
        LIMITED_METHOD_CONTRACT;

        memset(this, 0, sizeof(MulticoreJitPlayerStat));
    }
};


#ifndef DACCESS_COMPILE
class MulticoreJitPrepareCodeConfig;
#endif

class MulticoreJitCodeInfo
{
private:
    enum class TierInfo : TADDR
    {
        None = 0,
        WasTier0 = 1 << 0,
        JitSwitchedToOptimized = 1 << 1,
        Mask = None | WasTier0 | JitSwitchedToOptimized
    };

    TADDR m_entryPointAndTierInfo;

public:
    MulticoreJitCodeInfo() : m_entryPointAndTierInfo(NULL)
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifndef DACCESS_COMPILE
public:
    MulticoreJitCodeInfo(PCODE entryPoint, const MulticoreJitPrepareCodeConfig *pConfig);
#endif

private:
    void VerifyIsNotNull() const;

public:
    bool IsNull() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_entryPointAndTierInfo == NULL;
    }

    PCODE GetEntryPoint() const
    {
        WRAPPER_NO_CONTRACT;
        return IsNull() ? NULL : PINSTRToPCODE(m_entryPointAndTierInfo & ~(TADDR)TierInfo::Mask);
    }

    bool WasTier0() const
    {
        WRAPPER_NO_CONTRACT;
        VerifyIsNotNull();

        return (m_entryPointAndTierInfo & (TADDR)TierInfo::WasTier0) != 0;
    }

    bool JitSwitchedToOptimized() const
    {
        WRAPPER_NO_CONTRACT;
        VerifyIsNotNull();

        return (m_entryPointAndTierInfo & (TADDR)TierInfo::JitSwitchedToOptimized) != 0;
    }
};


// Code Storage

class MulticoreJitCodeStorage
{
private:
    MapSHashWithRemove<PVOID, MulticoreJitCodeInfo> m_nativeCodeMap;
    CrstExplicitInit                                m_crstCodeMap;  // protecting m_nativeCodeMap
    unsigned                                        m_nStored;
    unsigned                                        m_nReturned;

public:

    void Init();

#ifdef DACCESS_COMPILE

    ~MulticoreJitCodeStorage()
    {
        LIMITED_METHOD_CONTRACT;
    }

#else

    ~MulticoreJitCodeStorage();

#endif

    void StoreMethodCode(MethodDesc * pMethod, MulticoreJitCodeInfo codeInfo);

    bool LookupMethodCode(MethodDesc * pMethod);

    MulticoreJitCodeInfo QueryAndRemoveMethodCode(MethodDesc * pMethod);

    inline unsigned GetRemainingMethodCount() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_nativeCodeMap.GetCount();
    }

    inline unsigned GetStored() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_nStored;
    }

    inline unsigned GetReturned() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_nReturned;
    }

};


const LONG SETPROFILEROOTCALLED = 1;


// Multicore JIT attachment to AppDomain class
class MulticoreJitManager
{
private:
    MulticoreJitCounter     m_ProfileSession;          // Sequential profile session within the domain,
                                                       // incremented for every StartProfile/StopProfile/AbortProfile call to signal older players to quit
                                                       // We're just afraid of keeping pointer to player

    MulticoreJitRecorder  * m_pMulticoreJitRecorder;   // pointer to current recorder
    SString                 m_profileRoot;             // profile root string
    LONG                    m_fSetProfileRootCalled;   // SetProfileRoot has been called
    LONG                    m_fAutoStartCalled;
    bool                    m_fRecorderActive;         // Manager open for recording/event, turned on when initialized properly, turned off when at full capacity
    CrstExplicitInit        m_playerLock;              // Thread protection (accessing m_pMulticoreJitRecorder)
    MulticoreJitPlayerStat  m_stats;                   // Statistics: normally gathered by player, written to profile

    MulticoreJitCodeStorage m_MulticoreJitCodeStorage;

public:

#ifndef DACCESS_COMPILE
    MulticoreJitManager();

    ~MulticoreJitManager();
#else

    MulticoreJitManager()
    {
        LIMITED_METHOD_CONTRACT;

        m_pMulticoreJitRecorder = NULL;
        m_fSetProfileRootCalled = 0;
        m_fAutoStartCalled      = 0;
        m_fRecorderActive       = false;
    }

    ~MulticoreJitManager()
    {
        LIMITED_METHOD_CONTRACT;
    }

#endif

    inline bool IsRecorderActive() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_fRecorderActive;
    }

    inline MulticoreJitCounter & GetProfileSession()
    {
        LIMITED_METHOD_CONTRACT;

        return m_ProfileSession;
    }

    // Check for environment variable to automatically start multicore JIT
    void AutoStartProfile(AppDomain * pDomain);

    // Multicore JIT API function: SetProfileRoot
    void SetProfileRoot(const WCHAR * pProfilePath);

    // Multicore JIT API function: StartProfile
    void StartProfile(AppDomain * pDomain, AssemblyBinder * pBinder, const WCHAR * pProfile, int suffix = -1);

    // Multicore JIT API function (internal): AbortProfile
    void AbortProfile();

    // Called at AppDomain shut down to automatically shut down remaining profiling
    void StopProfile(bool appDomainShutdown);

    static void StopProfileAll();

#ifndef TARGET_UNIX
    void WriteMulticoreJitProfiler();
#endif // !TARGET_UNIX

    // Track module loading event for recording
    void RecordModuleLoad(Module * pModule, FileLoadLevel loadLevel);

    static bool IsMethodSupported(MethodDesc * pMethod);

    MulticoreJitCodeInfo RequestMethodCode(MethodDesc * pMethod);

    void RecordMethodJitOrLoad(MethodDesc * pMethod);

    MulticoreJitPlayerStat & GetStats()
    {
        LIMITED_METHOD_CONTRACT;

        return m_stats;
    }

    MulticoreJitCodeStorage & GetMulticoreJitCodeStorage()
    {
        LIMITED_METHOD_CONTRACT;

        return m_MulticoreJitCodeStorage;
    }

    static void DisableMulticoreJit();

    static bool IsSupportedModule(Module * pModule, bool fMethodJit);

    static FileLoadLevel GetModuleFileLoadLevel(Module * pModule);

    static bool ModuleHasNoCode(Module * pModule);

    static DWORD EncodeModuleHelper(void * pModuleContext,
                                    Module * pReferencedModule);

    static Module * DecodeModuleFromIndex(void * pModuleContext,
                                          DWORD  ix);
};


// For qcallentrypoints.cpp

extern "C" void QCALLTYPE MultiCoreJIT_InternalSetProfileRoot(_In_z_ LPCWSTR directoryPath);
extern "C" void QCALLTYPE MultiCoreJIT_InternalStartProfile(_In_z_ LPCWSTR wszProfile, INT_PTR ptrNativeAssemblyBinder);

#endif // __MULTICORE_JIT_H__
