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

#include "holder.h"

#include "clrprivhosting.h"

#ifdef FEATURE_COMINTEROP
#include "activation.h" // WinRT activation.
#endif

class DangerousNonHostedSpinLock;

#define INVALID_STACK_BASE_MARKER_FOR_CHECK_STATE 2

class AppDomain;
class Assembly;


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
    , m_fStarted(FALSE)
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

    // This flag will be used to ensure that a CoreCLR host can invoke Start/Stop in pairs only.
    BOOL m_fStarted; 
    BOOL m_fAppDomainCreated; // this flag is used when an appdomain can only create a single appdomain

    static ULONG       m_Version;              // Version of ICorRuntimeHost.
                                        // Some functions are only available in ICLRRuntimeHost.
                                        // Some functions are no-op in ICLRRuntimeHost.

    STDMETHODIMP UnloadAppDomain(DWORD dwDomainId, BOOL fWaitUntilDone);

    STDMETHODIMP UnloadAppDomain2(DWORD dwDomainId, BOOL fWaitUntilDone, int *pLatchedExitCode);
public:
    static ULONG GetHostVersion()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (m_Version != 0);
        return m_Version;
    }

};


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


#if defined(FEATURE_WINDOWSPHONE)
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
#endif // defined(FEATURE_WINDOWSPHONE)

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
    , public ICLRRuntimeHost4
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

    STDMETHODIMP UnloadAppDomain2(DWORD dwDomainId, BOOL fWaitUntilDone, int *pLatchedExitCode);

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

    static STARTUP_FLAGS GetStartupFlags();

    static EInitializeNewDomainFlags GetAppDomainManagerInitializeNewDomainFlags();

    static BOOL HasStarted()
    {
        return m_RefCount != 0;
    }

private:
    // This flag indicates if this instance was the first to load and start CoreCLR
    BOOL m_fFirstToLoadCLR;

    // This flag indicates if the host has authenticated with us or not
    BOOL m_fIsHostAuthenticated;


    // Helpers for both ICLRRuntimeHost2 and ICLRPrivRuntime
    HRESULT _CreateAppDomain(
        LPCWSTR wszFriendlyName,
        DWORD  dwFlags,
        LPCWSTR wszAppDomainManagerAssemblyName, 
        LPCWSTR wszAppDomainManagerTypeName, 
        int nProperties, 
        LPCWSTR* pPropertyNames, 
        LPCWSTR* pPropertyValues,
        DWORD* pAppDomainID);

    HRESULT _CreateDelegate(
        DWORD appDomainID,
        LPCWSTR wszAssemblyName,     
        LPCWSTR wszClassName,     
        LPCWSTR wszMethodName,
        INT_PTR* fnPtr);

    // entrypoint helper to be wrapped in a filter to process unhandled exceptions
    VOID ExecuteMainInner(Assembly* pRootAssembly);

    static LONG  m_RefCount;

    static IHostControl *m_HostControl;

    SVAL_DECL(STARTUP_FLAGS, m_dwStartupFlags);
};

class CorHostProtectionManager
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
