// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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


// Code Storage

class MulticoreJitCodeStorage
{
private:
    MapSHashWithRemove<PVOID,PCODE> m_nativeCodeMap;
    CrstExplicitInit                m_crstCodeMap;  // protecting m_nativeCodeMap
    unsigned                        m_nStored;
    unsigned                        m_nReturned;

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

    void StoreMethodCode(MethodDesc * pMethod, PCODE pCode);

    PCODE QueryMethodCode(MethodDesc * pMethod, BOOL shouldRemoveCode);

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
    bool                    m_fAppxMode;
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
        m_fAppxMode             = false;
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
    void SetProfileRoot(AppDomain * pDomain, const WCHAR * pProfilePath);

    // Multicore JIT API function: StartProfile
    void StartProfile(AppDomain * pDomain, ICLRPrivBinder * pBinderContext, const WCHAR * pProfile, int suffix = -1);

    // Multicore JIT API function (internal): AbortProfile
    void AbortProfile();

    // Called at AppDomain shut down to automatically shut down remaining profiling
    void StopProfile(bool appDomainShutdown);

    static void StopProfileAll();

    // Track module loading event for recording
    void RecordModuleLoad(Module * pModule, FileLoadLevel loadLevel);

    static bool IsMethodSupported(MethodDesc * pMethod);

    PCODE RequestMethodCode(MethodDesc * pMethod);

    void RecordMethodJit(MethodDesc * pMethod);

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

    static bool IsSupportedModule(Module * pModule, bool fMethodJit, bool fAppx);

    static FileLoadLevel GetModuleFileLoadLevel(Module * pModule);

    static bool ModuleHasNoCode(Module * pModule);


};


// For ecall.cpp

class MultiCoreJITNative
{
public:
    static void QCALLTYPE InternalSetProfileRoot(__in_z LPCWSTR directoryPath);

    static void QCALLTYPE InternalStartProfile(__in_z LPCWSTR wszProfile, INT_PTR ptrNativeAssemblyLoadContext);
};

#endif // __MULTICORE_JIT_H__
