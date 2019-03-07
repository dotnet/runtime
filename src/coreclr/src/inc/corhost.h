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


class CorRuntimeHostBase
{
protected:
    CorRuntimeHostBase()
    :m_Started(FALSE),
     m_cRef(0)
    {LIMITED_METHOD_CONTRACT;}

    STDMETHODIMP_(ULONG) AddRef(void);

    // Starts the runtime. This is equivalent to CoInitializeCor()
    STDMETHODIMP Start();

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

class CorHost2 :
    public CorRuntimeHostBase
#ifndef FEATURE_PAL    
    , public IPrivateManagedExceptionReporting /* This interface is for internal Watson testing only*/
#endif // FEATURE_PAL    
    , public ICLRRuntimeHost4
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

    static BOOL HasStarted()
    {
        return m_RefCount != 0;
    }

private:
    // This flag indicates if this instance was the first to load and start CoreCLR
    BOOL m_fFirstToLoadCLR;

    // This flag will be used to ensure that a CoreCLR host can invoke Start/Stop in pairs only.
    BOOL m_fStarted;
    BOOL m_fAppDomainCreated; // this flag is used when an appdomain can only create a single appdomain

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

    SVAL_DECL(STARTUP_FLAGS, m_dwStartupFlags);
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
