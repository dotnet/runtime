// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//*****************************************************************************
// CorHost.h
//
// Class factories are used by the pluming in COM to activate new objects.
// This module contains the class factory code to instantiate the debugger
// objects described in <cordb.h>.
//
//*****************************************************************************

#ifndef __CorHost__h__
#define __CorHost__h__


#include "windows.h" // worth to include before mscoree.h so we are guaranteed to pick few definitions
#ifdef CreateSemaphore
#undef CreateSemaphore
#endif

#include "mscoree.h"

#include "clrinternal.h"

#include "ivehandler.h"
#include "ivalidator.h"
#ifdef FEATURE_FUSION
#include "fusion.h"
#endif
#include "holder.h"

#if defined(FEATURE_HOSTED_BINDER)
#include "clrprivhosting.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "activation.h" // WinRT activation.
#endif

class DangerousNonHostedSpinLock;

#define INVALID_STACK_BASE_MARKER_FOR_CHECK_STATE 2

class AppDomain;
class Assembly;

#if !defined(FEATURE_CORECLR)
class CorThreadpool : public ICorThreadpool
{
public:
    HRESULT STDMETHODCALLTYPE  CorRegisterWaitForSingleObject(PHANDLE phNewWaitObject,
                                                              HANDLE hWaitObject,
                                                              WAITORTIMERCALLBACK Callback,
                                                              PVOID Context,
                                                              ULONG timeout,
                                                              BOOL  executeOnlyOnce,
                                                              BOOL* pResult);

    HRESULT STDMETHODCALLTYPE  CorBindIoCompletionCallback(HANDLE fileHandle, LPOVERLAPPED_COMPLETION_ROUTINE callback);

    HRESULT STDMETHODCALLTYPE  CorUnregisterWait(HANDLE hWaitObject,HANDLE CompletionEvent, BOOL* pResult);

    HRESULT STDMETHODCALLTYPE  CorQueueUserWorkItem(LPTHREAD_START_ROUTINE Function,PVOID Context,BOOL executeOnlyOnce, BOOL* pResult );

    HRESULT STDMETHODCALLTYPE  CorCallOrQueueUserWorkItem(LPTHREAD_START_ROUTINE Function,PVOID Context,BOOL* pResult );

    HRESULT STDMETHODCALLTYPE  CorCreateTimer(PHANDLE phNewTimer,
                                              WAITORTIMERCALLBACK Callback,
                                              PVOID Parameter,
                                              DWORD DueTime,
                                              DWORD Period,
                                              BOOL* pResult);

    HRESULT STDMETHODCALLTYPE  CorDeleteTimer(HANDLE Timer, HANDLE CompletionEvent, BOOL* pResult);

    HRESULT STDMETHODCALLTYPE  CorChangeTimer(HANDLE Timer,ULONG DueTime,ULONG Period, BOOL* pResult);

    HRESULT STDMETHODCALLTYPE CorSetMaxThreads(DWORD MaxWorkerThreads,
                                               DWORD MaxIOCompletionThreads);

    HRESULT STDMETHODCALLTYPE CorGetMaxThreads(DWORD *MaxWorkerThreads,
                                               DWORD *MaxIOCompletionThreads);

    HRESULT STDMETHODCALLTYPE CorGetAvailableThreads(DWORD *AvailableWorkerThreads,
                                                  DWORD *AvailableIOCompletionThreads);
};

class CorGCHost : public IGCHost2
{
public:
    // IGCHost
    STDMETHODIMP STDMETHODCALLTYPE SetGCStartupLimits(
        DWORD SegmentSize,
        DWORD MaxGen0Size);

    STDMETHODIMP STDMETHODCALLTYPE Collect(
        LONG Generation);

    STDMETHODIMP STDMETHODCALLTYPE GetStats(
        COR_GC_STATS *pStats);

    STDMETHODIMP STDMETHODCALLTYPE GetThreadStats(
        DWORD *pFiberCookie,
        COR_GC_THREAD_STATS *pStats);

    STDMETHODIMP STDMETHODCALLTYPE SetVirtualMemLimit(
        SIZE_T sztMaxVirtualMemMB);

    // IGCHost2
    STDMETHODIMP STDMETHODCALLTYPE SetGCStartupLimitsEx(
        SIZE_T SegmentSize,
        SIZE_T MaxGen0Size);

private:

    HRESULT _SetGCSegmentSize(SIZE_T SegmentSize);
    HRESULT _SetGCMaxGen0Size(SIZE_T MaxGen0Size);
};

class CorConfiguration : public ICorConfiguration
{
public:
    virtual HRESULT STDMETHODCALLTYPE SetGCThreadControl(
        /* [in] */ IGCThreadControl __RPC_FAR *pGCThreadControl);

    virtual HRESULT STDMETHODCALLTYPE SetGCHostControl(
        /* [in] */ IGCHostControl __RPC_FAR *pGCHostControl);

    virtual HRESULT STDMETHODCALLTYPE SetDebuggerThreadControl(
        /* [in] */ IDebuggerThreadControl __RPC_FAR *pDebuggerThreadControl);

    virtual HRESULT STDMETHODCALLTYPE AddDebuggerSpecialThread(
        /* [in] */ DWORD dwSpecialThreadId);

    // This mechanism isn't thread-safe with respect to reference counting, because
    // the runtime will use the cached pointer without adding extra refcounts to protect
    // itself.  So if one thread calls GetGCThreadControl & another thread calls
    // ICorHost::SetGCThreadControl, we have a race.
    static IGCThreadControl *GetGCThreadControl()
    {
        LIMITED_METHOD_CONTRACT;

        return m_CachedGCThreadControl;
    }

    static IGCHostControl *GetGCHostControl()
    {
        LIMITED_METHOD_CONTRACT;

        return m_CachedGCHostControl;
    }

    static IDebuggerThreadControl *GetDebuggerThreadControl()
    {
        LIMITED_METHOD_CONTRACT;

        return m_CachedDebuggerThreadControl;
    }

    static DWORD GetDebuggerSpecialThreadCount()
    {
        LIMITED_METHOD_CONTRACT;

        return m_DSTCount;
    }

    static DWORD *GetDebuggerSpecialThreadArray()
    {
        LIMITED_METHOD_CONTRACT;

        return m_DSTArray;
    }

    // Helper function that returns true if the thread is in the debugger special thread list
    static BOOL IsDebuggerSpecialThread(DWORD dwThreadId);

    // Helper function to update the thread list in the debugger control block
    static HRESULT RefreshDebuggerSpecialThreadList();

    // Clean up debugger thread control object, called at shutdown
    static void CleanupDebuggerThreadControl();

private:
    // Cache the IGCThreadControl interface until the EE is started, at which point
    // we pass it through.
    static IGCThreadControl *m_CachedGCThreadControl;
    static IGCHostControl *m_CachedGCHostControl;
    static IDebuggerThreadControl *m_CachedDebuggerThreadControl;

    // Array of ID's of threads that should be considered "special" to
    // the debugging services.
    static DWORD *m_DSTArray;
    static DWORD  m_DSTArraySize;
    static DWORD  m_DSTCount;
};

class CorValidator : public IValidator
{
protected:
    CorValidator() {LIMITED_METHOD_CONTRACT;}

public:
    STDMETHODIMP STDMETHODCALLTYPE Validate(
            IVEHandler        *veh,
            IUnknown          *pAppDomain,
            unsigned long      ulFlags,
            unsigned long      ulMaxError,
            unsigned long      token,
            __in_z LPWSTR             fileName,
            BYTE               *pe,
            unsigned long      ulSize);

    STDMETHODIMP STDMETHODCALLTYPE FormatEventInfo(
            HRESULT            hVECode,
            VEContext          Context,
            __out_ecount(ulMaxLength) LPWSTR             msg,
            unsigned long      ulMaxLength,
            SAFEARRAY         *psa);
};

class CLRValidator : public ICLRValidator
{
protected:
    CLRValidator() {LIMITED_METHOD_CONTRACT;}

public:
    STDMETHODIMP STDMETHODCALLTYPE Validate(
            IVEHandler        *veh,
            unsigned long      ulAppDomainId,
            unsigned long      ulFlags,
            unsigned long      ulMaxError,
            unsigned long      token,
            __in_z LPWSTR             fileName,
            BYTE               *pe,
            unsigned long      ulSize);

    STDMETHODIMP STDMETHODCALLTYPE FormatEventInfo(
            HRESULT            hVECode,
            VEContext          Context,
            __out_ecount(ulMaxLength) LPWSTR             msg,
            unsigned long      ulMaxLength,
            SAFEARRAY         *psa);
};

class CorDebuggerInfo : public IDebuggerInfo
{
public:
    STDMETHODIMP IsDebuggerAttached(BOOL *pbAttached);
};
#endif // !defined(FEATURE_CORECLR)

class CorExecutionManager
    : public ICLRExecutionManager
{
public:
    CorExecutionManager();

    STDMETHODIMP STDMETHODCALLTYPE Pause(DWORD dwAppDomainId, DWORD dwFlags);
    STDMETHODIMP STDMETHODCALLTYPE Resume(DWORD dwAppDomainId);

private:
    DWORD m_dwFlags; //flags passed to the last Pause call.
    INT64 m_pauseStartTime;
};

class CorRuntimeHostBase
{
protected:
    CorRuntimeHostBase()
    :m_Started(FALSE),
     m_cRef(0)
#ifdef FEATURE_CORECLR
    , m_fStarted(FALSE)
#endif // FEATURE_CORECLR
    {LIMITED_METHOD_CONTRACT;}

    STDMETHODIMP_(ULONG) AddRef(void);

    // Starts the runtime. This is equivalent to CoInitializeCor()
    STDMETHODIMP Start();

#ifdef FEATURE_COMINTEROP
    // Creates a domain in the runtime. The identity array is
    // a pointer to an array TYPE containing IIdentity objects defining
    // the security identity.
    STDMETHODIMP CreateDomain(LPCWSTR pwzFriendlyName,   // Optional
                              IUnknown* pIdentityArray, // Optional
                              IUnknown ** pAppDomain);

    // Returns the default domain.
    STDMETHODIMP GetDefaultDomain(IUnknown ** pAppDomain);

    // Enumerate currently existing domains.
    STDMETHODIMP EnumDomains(HDOMAINENUM *hEnum);

    // Returns S_FALSE when there are no more domains. A domain
    // is passed out only when S_OK is returned.
    STDMETHODIMP NextDomain(HDOMAINENUM hEnum,
                            IUnknown** pAppDomain);

    // Close the enumeration releasing resources
    STDMETHODIMP CloseEnum(HDOMAINENUM hEnum);

    STDMETHODIMP CreateDomainEx(LPCWSTR pwzFriendlyName,
                                IUnknown* pSetup, // Optional
                                IUnknown* pEvidence, // Optional
                                IUnknown ** pAppDomain);

    // Create appdomain setup object that can be passed into CreateDomainEx
    STDMETHODIMP CreateDomainSetup(IUnknown** pAppDomainSetup);

    // Create Evidence object that can be passed into CreateDomainEx
    STDMETHODIMP CreateEvidence(IUnknown** pEvidence);

    // Unload a domain, releasing the reference will only release the
    // the wrapper to the domain not unload the domain.
    STDMETHODIMP UnloadDomain(IUnknown* pAppDomain);

    // Returns the threads domain if there is one.
    STDMETHODIMP CurrentDomain(IUnknown ** pAppDomain);
#endif // FEATURE_COMINTEROP

    STDMETHODIMP MapFile(                       // Return code.
        HANDLE     hFile,                       // [in]  Handle for file
        HMODULE   *hMapAddress                  // [out] HINSTANCE for mapped file
        );

    STDMETHODIMP LocksHeldByLogicalThread(      // Return code.
        DWORD *pCount                           // [out] Number of locks that the current thread holds.
        );

protected:
    BOOL        m_Started;              // Has START been called?

    LONG        m_cRef;                 // Ref count.

#ifdef FEATURE_CORECLR
    // This flag will be used to ensure that a CoreCLR host can invoke Start/Stop in pairs only.
    BOOL m_fStarted; 
    BOOL m_fAppDomainCreated; // this flag is used when an appdomain can only create a single appdomain
#endif // FEATURE_CORECLR

    static ULONG       m_Version;              // Version of ICorRuntimeHost.
                                        // Some functions are only available in ICLRRuntimeHost.
                                        // Some functions are no-op in ICLRRuntimeHost.

    STDMETHODIMP UnloadAppDomain(DWORD dwDomainId, BOOL fWaitUntilDone);

public:
    static ULONG GetHostVersion()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (m_Version != 0);
        return m_Version;
    }

};

#if !defined(FEATURE_CORECLR) // simple hosting
class CorHost :
    public CorRuntimeHostBase, public ICorRuntimeHost, public CorThreadpool
    , public CorGCHost, public CorConfiguration
    , public CorValidator, public CorDebuggerInfo
    , public CorExecutionManager
{
public:
    CorHost() {WRAPPER_NO_CONTRACT;}

    // *** IUnknown methods ***
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::AddRef();
    }
    STDMETHODIMP_(ULONG) Release(void);


    // *** ICorRuntimeHost methods ***
    // Returns an object for configuring the runtime prior to
    // it starting. If the runtime has been initialized this
    // routine returns an error. See ICorConfiguration.
    STDMETHODIMP GetConfiguration(ICorConfiguration** pConfiguration);


    // Starts the runtime. This is equivalent to CoInitializeCor();
    STDMETHODIMP Start(void);

    STDMETHODIMP Stop();

    // Creates a domain in the runtime. The identity array is
    // a pointer to an array TYPE containing IIdentity objects defining
    // the security identity.
    STDMETHODIMP CreateDomain(LPCWSTR pwzFriendlyName,   // Optional
                              IUnknown* pIdentityArray, // Optional
                              IUnknown ** pAppDomain)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::CreateDomain(pwzFriendlyName,pIdentityArray,pAppDomain);
    }

    // Returns the default domain.
    STDMETHODIMP GetDefaultDomain(IUnknown ** pAppDomain)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::GetDefaultDomain(pAppDomain);
    }

    // Enumerate currently existing domains.
    STDMETHODIMP EnumDomains(HDOMAINENUM *hEnum)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::EnumDomains(hEnum);
    }

    // Returns S_FALSE when there are no more domains. A domain
    // is passed out only when S_OK is returned.
    STDMETHODIMP NextDomain(HDOMAINENUM hEnum,
                            IUnknown** pAppDomain)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::NextDomain(hEnum,pAppDomain);
    }

    // Close the enumeration releasing resources
    STDMETHODIMP CloseEnum(HDOMAINENUM hEnum)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::CloseEnum(hEnum);
    }

    STDMETHODIMP CreateDomainEx(LPCWSTR pwzFriendlyName,
                                IUnknown* pSetup, // Optional
                                IUnknown* pEvidence, // Optional
                                IUnknown ** pAppDomain)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::CreateDomainEx(pwzFriendlyName,pSetup,pEvidence,pAppDomain);
    }

    // Create appdomain setup object that can be passed into CreateDomainEx
    STDMETHODIMP CreateDomainSetup(IUnknown** pAppDomainSetup)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::CreateDomainSetup(pAppDomainSetup);
    }

    // Create Evidence object that can be passed into CreateDomainEx
    STDMETHODIMP CreateEvidence(IUnknown** pEvidence)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::CreateEvidence(pEvidence);
    }

    // Unload a domain, releasing the reference will only release the
    // the wrapper to the domain not unload the domain.
    STDMETHODIMP UnloadDomain(IUnknown* pAppDomain)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::UnloadDomain(pAppDomain);
    }

    // Returns the threads domain if there is one.
    STDMETHODIMP CurrentDomain(IUnknown ** pAppDomain)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::CurrentDomain(pAppDomain);
    }

    // TODO: Following 4 APIs should be move to CorHost for V1.
    STDMETHODIMP CreateLogicalThreadState();    // Return code.
    STDMETHODIMP DeleteLogicalThreadState();    // Return code.
    STDMETHODIMP SwitchInLogicalThreadState(    // Return code.
        DWORD *pFiberCookie                     // [in] Cookie that indicates the fiber to use.
        );

    STDMETHODIMP SwitchOutLogicalThreadState(   // Return code.
        DWORD **pFiberCookie                    // [out] Cookie that indicates the fiber being switched out.
        );

    STDMETHODIMP LocksHeldByLogicalThread(      // Return code.
        DWORD *pCount                           // [out] Number of locks that the current thread holds.
        )
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::LocksHeldByLogicalThread(pCount);
    }

    // Class factory hook-up.
    static HRESULT CreateObject(REFIID riid, void **ppUnk);

    STDMETHODIMP MapFile(                       // Return code.
        HANDLE     hFile,                       // [in]  Handle for file
        HMODULE   *hMapAddress                  // [out] HINSTANCE for mapped file
        )
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return CorRuntimeHostBase::MapFile(hFile,hMapAddress);
    }
};
#endif // !defined(FEATURE_CORECLR)

class ConnectionNameTable;
typedef DPTR(class ConnectionNameTable) PTR_ConnectionNameTable;

class CrstStatic;

// Defines the precedence (in increading oder) of the two symbol reading knobs
enum ESymbolReadingSetBy
{
    eSymbolReadingSetByDefault,
    eSymbolReadingSetByConfig,  // EEConfig - config file, env var, etc.
    eSymbolReadingSetByHost,    // Hosting API - highest precedence
    eSymbolReadingSetBy_COUNT
};

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
// Hash table entry to keep track <connection, name> for SQL fiber support
typedef DPTR(struct ConnectionNameHashEntry) PTR_ConnectionNameHashEntry;
struct ConnectionNameHashEntry
{
    FREEHASHENTRY   entry;
    CONNID          m_dwConnectionId;
    PTR_WSTR        m_pwzName;
    ICLRTask        **m_ppCLRTaskArray;
    UINT            m_CLRTaskCount;
};


class CCLRDebugManager : public ICLRDebugManager
{
public:
    CCLRDebugManager() {LIMITED_METHOD_CONTRACT;};

    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
    STDMETHODIMP_(ULONG) Release(void);

    // ICLRTDebugManager's interface
    STDMETHODIMP BeginConnection(
        CONNID  dwConnectionId,
        __in_z wchar_t *szConnectionName);
    STDMETHODIMP SetConnectionTasks(
        DWORD id,
        DWORD dwCount,
        ICLRTask **ppCLRTask);
    STDMETHODIMP EndConnection(
        CONNID  dwConnectionId);

    // Set ACL on shared section, events, and process
    STDMETHODIMP SetDacl(PACL pacl);

    // Returning the current ACL that CLR is using
    STDMETHODIMP GetDacl(PACL *pacl);

    STDMETHODIMP IsDebuggerAttached(BOOL *pbAttached);

    // symbol reading policy - include file line info when getting a call stack etc.
    STDMETHODIMP SetSymbolReadingPolicy(ESymbolReadingPolicy policy);

#ifdef DACCESS_COMPILE
    // Expose iterators for DAC. Debugger can use this on attach to find existing Connections.
    //
    // Example usage:
    //   HASHFIND h;
    //   ConnectionNameHashEntry * pConnection = FindFirst(&h);
    //   while(pConnection != NULL) {
    //       DoSomething(pConnection);
    //       pConnection = FindNext(&h);
    //   }
    static ConnectionNameHashEntry * FindFirst(HASHFIND * pHashfind);
    static ConnectionNameHashEntry * FindNext(HASHFIND * pHashfind);
#endif

    static void ProcessInit();
    static void ProcessCleanup();

    // Get the current symbol reading policy setting
    static ESymbolReadingPolicy GetSymbolReadingPolicy()
    {
        return m_symbolReadingPolicy;
    }

    // Set the symbol reading policy if the setter has higher precendence than the current setting
    static void SetSymbolReadingPolicy( ESymbolReadingPolicy policy, ESymbolReadingSetBy setBy );
    
private:
    static CrstStatic m_lockConnectionNameTable;
    SPTR_DECL(ConnectionNameTable, m_pConnectionNameHash);

    static ESymbolReadingPolicy m_symbolReadingPolicy;
    static ESymbolReadingSetBy m_symbolReadingSetBy;
};

#endif // FEATURE_INCLUDE_ALL_INTERFACES

#if defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)
class CCLRErrorReportingManager :
#ifdef FEATURE_WINDOWSPHONE
    public ICLRErrorReportingManager2
#else
    public ICLRErrorReportingManager
#endif // FEATURE_WINDOWSPHONE
{
    friend class ClrDataAccess;
    friend struct _DacGlobals;

    SVAL_DECL(ECustomDumpFlavor, g_ECustomDumpFlavor);
    
#ifdef FEATURE_WINDOWSPHONE
    WCHAR* m_pApplicationId;
    WCHAR* m_pInstanceId;
    
    class BucketParamsCache
    {
    private:
        WCHAR** m_pParams;
        DWORD const m_cMaxParams;
    public:
        BucketParamsCache(DWORD maxNumParams);
        ~BucketParamsCache();
        
        WCHAR const* GetAt(BucketParameterIndex index);
        HRESULT SetAt(BucketParameterIndex index, WCHAR const* val);
    };
    
    BucketParamsCache* m_pBucketParamsCache;
    
    HRESULT CopyToDataCache(_In_ WCHAR** pTarget, WCHAR const* pSource);
#endif // FEATURE_WINDOWSPHONE

public:
    CCLRErrorReportingManager();
    ~CCLRErrorReportingManager();

    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void);
    STDMETHODIMP_(ULONG) Release(void);

    // ICLRErrorReportingManager APIs //
    
    // Get Watson bucket parameters for "current" exception (on calling thread).
    STDMETHODIMP GetBucketParametersForCurrentException(BucketParameters *pParams);
    STDMETHODIMP BeginCustomDump(   ECustomDumpFlavor dwFlavor,
                                        DWORD dwNumItems,
                                        CustomDumpItem items[],
                                        DWORD dwReserved);
    STDMETHODIMP EndCustomDump();
    
#ifdef FEATURE_WINDOWSPHONE
    // ICLRErrorReportingManager2 APIs //
    
    STDMETHODIMP SetApplicationData(ApplicationDataKey key, WCHAR const* pValue);
    STDMETHODIMP SetBucketParametersForUnhandledException(BucketParameters const* pBucketParams, DWORD* pCountParams);
    
    // internal APIs
    
    // returns the application data for the specified key if available, else returns NULL.
    WCHAR const* GetApplicationData(ApplicationDataKey key);
    
    // returns bucket parameter override data if available, else returns NULL.
    WCHAR const* GetBucketParamOverride(BucketParameterIndex bucketParamId);
#endif // FEATURE_WINDOWSPHONE
};

extern CCLRErrorReportingManager g_CLRErrorReportingManager;
#endif // defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)

#ifdef FEATURE_IPCMAN
// @TODO:: a-meicht
// consolidate the following class with DebuggerManager.
//
class CCLRSecurityAttributeManager
{
public:

    // Set ACL on shared section, events, and process
    STDMETHODIMP SetDACL(PACL pacl);

    // Returning the current ACL that CLR is using
    STDMETHODIMP GetDACL(PACL *pacl);

    static void ProcessInit();
    static void ProcessCleanUp();

    // retrieving Host security attribute setting. If host does not set it, default to
    // our default policy.
    static HRESULT GetHostSecurityAttributes(SECURITY_ATTRIBUTES **ppSA);
    static void DestroyHostSecurityAttributes(SECURITY_ATTRIBUTES *pSA);

    static CrstStatic          m_hostSAMutex;

private:
    static PACL                m_pACL;

    // Security attributes cached for the current process.
    static SECURITY_ATTRIBUTES m_hostSA;
    static SECURITY_DESCRIPTOR m_hostSD;

    static HRESULT CopyACL(PACL pAclOriginal, PACL ppAclNew);
};
#endif // FEATURE_IPCMAN

class CorHost2 :
    public CorRuntimeHostBase
#ifndef FEATURE_PAL    
    , public IPrivateManagedExceptionReporting /* This interface is for internal Watson testing only*/
#endif // FEATURE_PAL    
#ifdef FEATURE_CORECLR
    , public ICLRRuntimeHost2
#else
    , public CorThreadpool
    , public CorGCHost
    , public CorConfiguration
    , public CLRValidator
    , public CorDebuggerInfo
    , public ICLRRuntimeHost
#endif // FEATURE_CORECLR
#if defined(FEATURE_HOSTED_BINDER) && !defined(FEATURE_CORECLR)
    , public ICLRPrivRuntime
#endif
    , public CorExecutionManager
{
    friend struct _DacGlobals;

public:
    CorHost2();
    virtual ~CorHost2() {}

    // *** IUnknown methods ***
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void)
    {
        WRAPPER_NO_CONTRACT;
        return CorRuntimeHostBase::AddRef();
    }
    STDMETHODIMP_(ULONG) Release(void);


    // *** ICorRuntimeHost methods ***
#ifndef FEATURE_CORECLR
    // Returns an object for configuring the runtime prior to
    // it starting. If the runtime has been initialized this
    // routine returns an error. See ICorConfiguration.
    STDMETHODIMP GetConfiguration(ICorConfiguration** pConfiguration);
#endif // FEATURE_CORECLR

#ifndef FEATURE_PAL    
    // defined in IPrivateManagedExceptionReporting interface for internal Watson testing only
    STDMETHODIMP GetBucketParametersForCurrentException(BucketParameters *pParams);
#endif // FEATURE_PAL    

    // Starts the runtime. This is equivalent to CoInitializeCor().
    STDMETHODIMP Start();
    STDMETHODIMP Stop();

    STDMETHODIMP ExecuteInAppDomain(DWORD dwAppDomainId,
                                    FExecuteInAppDomainCallback pCallback,
                                    void * cookie);

    STDMETHODIMP LocksHeldByLogicalThread(      // Return code.
        DWORD *pCount                           // [out] Number of locks that the current thread holds.
        )
    {
        WRAPPER_NO_CONTRACT;
        return CorRuntimeHostBase::LocksHeldByLogicalThread(pCount);
    }

    // Class factory hook-up.
    static HRESULT CreateObject(REFIID riid, void **ppUnk);

    STDMETHODIMP MapFile(                       // Return code.
        HANDLE     hFile,                       // [in]  Handle for file
        HMODULE   *hMapAddress                  // [out] HINSTANCE for mapped file
        )
    {
        WRAPPER_NO_CONTRACT;
        return CorRuntimeHostBase::MapFile(hFile,hMapAddress);
    }

    STDMETHODIMP STDMETHODCALLTYPE SetHostControl(
        IHostControl* pHostControl);

    STDMETHODIMP STDMETHODCALLTYPE GetCLRControl(
        ICLRControl** pCLRControl);

    STDMETHODIMP UnloadAppDomain(DWORD dwDomainId, BOOL fWaitUntilDone);

    STDMETHODIMP GetCurrentAppDomainId(DWORD *pdwAppDomainId);

    STDMETHODIMP ExecuteApplication(LPCWSTR  pwzAppFullName,
                                    DWORD    dwManifestPaths,
                                    LPCWSTR  *ppwzManifestPaths,
                                    DWORD    dwActivationData,
                                    LPCWSTR  *ppwzActivationData,
                                    int      *pReturnValue);

    STDMETHODIMP ExecuteInDefaultAppDomain(LPCWSTR pwzAssemblyPath,
                                           LPCWSTR pwzTypeName,
                                           LPCWSTR pwzMethodName,
                                           LPCWSTR pwzArgument,
                                           DWORD   *pReturnValue);

#ifdef FEATURE_CORECLR
    // *** ICLRRuntimeHost2 methods ***
    STDMETHODIMP CreateAppDomainWithManager(
        LPCWSTR wszFriendlyName,
        DWORD  dwSecurityFlags,
        LPCWSTR wszAppDomainManagerAssemblyName, 
        LPCWSTR wszAppDomainManagerTypeName, 
        int nProperties, 
        LPCWSTR* pPropertyNames, 
        LPCWSTR* pPropertyValues, 
        DWORD* pAppDomainID);

    STDMETHODIMP CreateDelegate(
        DWORD appDomainID,
        LPCWSTR wszAssemblyName,     
        LPCWSTR wszClassName,     
        LPCWSTR wszMethodName,
        INT_PTR* fnPtr);

    STDMETHODIMP Authenticate(ULONGLONG authKey);

    STDMETHODIMP RegisterMacEHPort();
    STDMETHODIMP SetStartupFlags(STARTUP_FLAGS flag);
    STDMETHODIMP DllGetActivationFactory(
        DWORD appDomainID, 
        LPCWSTR wszTypeName,
        IActivationFactory ** factory);

    STDMETHODIMP ExecuteAssembly(
        DWORD dwAppDomainId,
        LPCWSTR pwzAssemblyPath,
        int argc,
        LPCWSTR* argv,
        DWORD* pReturnValue);

#endif // !FEATURE_CORECLR

#if defined(FEATURE_HOSTED_BINDER) && !defined(FEATURE_CORECLR)
    /**********************************************************************************
     ** ICLRPrivRuntime Methods
     **********************************************************************************/
    STDMETHODIMP GetInterface(
        REFCLSID rclsid,
        REFIID   riid,
        LPVOID * ppUnk);

    STDMETHODIMP CreateAppDomain(
        LPCWSTR pwzFriendlyName,
        ICLRPrivBinder * pBinder,
        LPDWORD pdwAppDomainId);

    STDMETHODIMP CreateDelegate(
        DWORD appDomainID,
        LPCWSTR wszAssemblyName,
        LPCWSTR wszClassName,
        LPCWSTR wszMethodName,
        LPVOID * ppvDelegate);

    STDMETHODIMP ExecuteMain(
        ICLRPrivBinder * pBinder,
        int * pRetVal);

#endif // FEATURE_HOSTED_BINDER && !FEATURE_CORECLR

    static IHostControl *GetHostControl ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostControl;
#endif // FEATURE_CORECLR
    }

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    static IHostMemoryManager *GetHostMemoryManager ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostMemoryManager;
#endif // FEATURE_CORECLR
    }

    static IHostMalloc *GetHostMalloc ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostMalloc;
#endif // FEATURE_CORECLR
    }

    static IHostTaskManager *GetHostTaskManager ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostTaskManager;
#endif // FEATURE_CORECLR
    }

    static IHostThreadpoolManager *GetHostThreadpoolManager ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostThreadpoolManager;
#endif // FEATURE_CORECLR
    }

    static IHostIoCompletionManager *GetHostIoCompletionManager ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostIoCompletionManager;
#endif // FEATURE_CORECLR
    }

    static IHostSyncManager *GetHostSyncManager ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostSyncManager;
#endif // FEATURE_CORECLR
    }

    static IHostAssemblyManager *GetHostAssemblyManager()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostAssemblyManager;
#endif // FEATURE_CORECLR
    }

    static IHostGCManager *GetHostGCManager()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostGCManager;
#endif // FEATURE_CORECLR
    }

    static IHostSecurityManager *GetHostSecurityManager()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostSecurityManager;
#endif // FEATURE_CORECLR
    }

    static IHostPolicyManager *GetHostPolicyManager ()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_HostPolicyManager;
#endif // FEATURE_CORECLR
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#ifdef FEATURE_LEGACYNETCF_DBG_HOST_CONTROL
    static IHostNetCFDebugControlManager *GetHostNetCFDebugControlManager ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_HostNetCFDebugControlManager;
    }
#endif

    static int GetHostOverlappedExtensionSize()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return 0;
#else // FEATURE_CORECLR
        _ASSERTE (m_HostOverlappedExtensionSize != -1);
        return m_HostOverlappedExtensionSize;
#endif // FEATURE_CORECLR
    }

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    static ICLRAssemblyReferenceList *GetHostDomainNeutralAsms()
    {
        LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
        return NULL;
#else // FEATURE_CORECLR
        return m_pHostDomainNeutralAsms;
#endif // FEATURE_CORECLR
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#ifndef FEATURE_CORECLR
    static HRESULT SetFlagsAndHostConfig(STARTUP_FLAGS dwStartupFlags, LPCWSTR pwzHostConfigFile, BOOL fFinalize);
    static LPCWSTR GetHostConfigFile();

    static void GetDefaultAppDomainProperties(StringArrayList **ppPropertyNames, StringArrayList **ppPropertyValues);
#endif // !FEATURE_CORECLR

    static STARTUP_FLAGS GetStartupFlags();

#ifndef FEATURE_CORECLR
    static HRESULT SetPropertiesForDefaultAppDomain(DWORD nProperties,
                                                    __in_ecount(nProperties) LPCWSTR *pwszPropertyNames,
                                                    __in_ecount(nProperties) LPCWSTR *pwszPropertyValues);

    static HRESULT SetAppDomainManagerType(LPCWSTR wszAppDomainManagerAssembly,
                                           LPCWSTR wszAppDomainManagerType,
                                           EInitializeNewDomainFlags dwInitializeDomainFlags);
#endif // FEATURE_CORECLR

    static LPCWSTR GetAppDomainManagerAsm();

    static LPCWSTR GetAppDomainManagerType();

    static EInitializeNewDomainFlags GetAppDomainManagerInitializeNewDomainFlags();

    static BOOL HasAppDomainManagerInfo()
    {
        LIMITED_METHOD_CONTRACT;
        return GetAppDomainManagerAsm() != NULL && GetAppDomainManagerType() != NULL;
    }

    static BOOL HasStarted()
    {
        return m_RefCount != 0;
    }
    
    static BOOL IsLoadFromBlocked(); // LoadFrom, LoadFile and Load(byte[]) are blocked in certain hosting scenarios

private:
#ifdef FEATURE_CORECLR
    // This flag indicates if this instance was the first to load and start CoreCLR
    BOOL m_fFirstToLoadCLR;

    // This flag indicates if the host has authenticated with us or not
    BOOL m_fIsHostAuthenticated;

#endif // FEATURE_CORECLR

#if defined(FEATURE_CORECLR) || defined(FEATURE_HOSTED_BINDER)
    // Helpers for both ICLRRuntimeHost2 and ICLRPrivRuntime
    HRESULT _CreateAppDomain(
        LPCWSTR wszFriendlyName,
        DWORD  dwFlags,
        LPCWSTR wszAppDomainManagerAssemblyName, 
        LPCWSTR wszAppDomainManagerTypeName, 
        int nProperties, 
        LPCWSTR* pPropertyNames, 
        LPCWSTR* pPropertyValues,
#if defined(FEATURE_HOSTED_BINDER) && !defined(FEATURE_CORECLR)
        ICLRPrivBinder* pBinder,
#endif
        DWORD* pAppDomainID);

    HRESULT _CreateDelegate(
        DWORD appDomainID,
        LPCWSTR wszAssemblyName,     
        LPCWSTR wszClassName,     
        LPCWSTR wszMethodName,
        INT_PTR* fnPtr);
#endif // defined(FEATURE_CORECLR) || defined(FEATURE_HOSTED_BINDER)

#ifdef FEATURE_HOSTED_BINDER
    // entrypoint helper to be wrapped in a filter to process unhandled exceptions
    VOID ExecuteMainInner(Assembly* pRootAssembly);
#endif // FEATURE_HOSTED_BINDER

    static LONG  m_RefCount;

    static IHostControl *m_HostControl;

    static LPCWSTR s_wszAppDomainManagerAsm;
    static LPCWSTR s_wszAppDomainManagerType;
    static EInitializeNewDomainFlags s_dwDomainManagerInitFlags;

#if !defined(FEATURE_CORECLR)
    static StringArrayList s_defaultDomainPropertyNames;
    static StringArrayList s_defaultDomainPropertyValues;

protected:
    static IHostMemoryManager *m_HostMemoryManager;
    static IHostMalloc *m_HostMalloc;
    static IHostTaskManager  *m_HostTaskManager;
    static IHostThreadpoolManager *m_HostThreadpoolManager;
    static IHostIoCompletionManager *m_HostIoCompletionManager;
    static IHostSyncManager *m_HostSyncManager;
    static IHostAssemblyManager *m_HostAssemblyManager;
    static IHostGCManager *m_HostGCManager;
    static IHostSecurityManager *m_HostSecurityManager;
    static IHostPolicyManager *m_HostPolicyManager;
    static int m_HostOverlappedExtensionSize;
    static ICLRAssemblyReferenceList *m_pHostDomainNeutralAsms;

    static WCHAR m_wzHostConfigFile[_MAX_PATH];

    static BOOL m_dwFlagsFinalized;
    static DangerousNonHostedSpinLock m_FlagsLock; // protects the flags and host config
#endif // !defined(FEATURE_CORECLR)

#ifdef FEATURE_LEGACYNETCF_DBG_HOST_CONTROL
protected:
    static IHostNetCFDebugControlManager *m_HostNetCFDebugControlManager;
#endif

    SVAL_DECL(STARTUP_FLAGS, m_dwStartupFlags);
};

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
class CorHostProtectionManager : public ICLRHostProtectionManager
#else // !FEATURE_INCLUDE_ALL_INTERFACES
class CorHostProtectionManager
#endif // FEATURE_INCLUDE_ALL_INTERFACES
{
private:
    EApiCategories m_eProtectedCategories;
    bool m_fEagerSerializeGrantSet;
    bool m_fFrozen;

public:
    CorHostProtectionManager();

    // IUnknown methods
    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID id,
        void **pInterface);
    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();

    // Interface methods
    virtual HRESULT STDMETHODCALLTYPE SetProtectedCategories(/* [in] */ EApiCategories eFullTrustOnlyResources);
    virtual HRESULT STDMETHODCALLTYPE SetEagerSerializeGrantSets();

    // Getters
    EApiCategories GetProtectedCategories();
    bool GetEagerSerializeGrantSets() const;

    void Freeze();
};

#ifdef FEATURE_COMINTEROP
extern "C"
HRESULT STDMETHODCALLTYPE DllGetActivationFactoryImpl(
                                                      LPCWSTR wszAssemblyName, 
                                                      LPCWSTR wszTypeName, 
                                                      LPCWSTR wszCodeBase,
                                                      IActivationFactory ** factory);

#endif // defined(FEATURE_COMINTEROP)

extern SIZE_T Host_SegmentSize;
extern SIZE_T Host_MaxGen0Size;
extern BOOL  Host_fSegmentSizeSet;
extern BOOL  Host_fMaxGen0SizeSet;

#define PARTIAL_TRUST_VISIBLE_ASSEMBLIES_PROPERTY W("PARTIAL_TRUST_VISIBLE_ASSEMBLIES")
#endif // __CorHost__h__
