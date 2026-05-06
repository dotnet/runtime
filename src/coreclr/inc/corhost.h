// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

#ifdef FEATURE_COMINTEROP
#include "activation.h" // WinRT activation.
#endif

class AppDomain;
class Assembly;

class CorHost2 : ICLRRuntimeHost4
#ifndef TARGET_UNIX
    , public IPrivateManagedExceptionReporting /* This interface is for internal Watson testing only*/
#endif // TARGET_UNIX
{
    friend struct _DacGlobals;

public:
    CorHost2();
    virtual ~CorHost2() {}

    // *** IUnknown methods ***
    STDMETHODIMP    QueryInterface(REFIID riid, void** ppv);
    STDMETHODIMP_(ULONG) AddRef(void);
    STDMETHODIMP_(ULONG) Release(void);


    // *** ICorRuntimeHost methods ***

#ifndef TARGET_UNIX
    // defined in IPrivateManagedExceptionReporting interface for internal Watson testing only
    STDMETHODIMP GetBucketParametersForCurrentException(BucketParameters *pParams);
#endif // TARGET_UNIX

    // Starts the runtime. This is equivalent to CoInitializeCor().
    STDMETHODIMP Start();
    STDMETHODIMP Stop();

    STDMETHODIMP ExecuteInAppDomain(DWORD dwAppDomainId,
                                    FExecuteInAppDomainCallback pCallback,
                                    void * cookie);

    // Class factory hook-up.
    static HRESULT CreateObject(REFIID riid, void **ppUnk);

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
    LONG        m_cRef;                 // COM ref count.

    // This flag indicates if this instance was the first to load and start CoreCLR
    BOOL m_fFirstToLoadCLR;

    // This flag will be used to ensure that a CoreCLR host can invoke Start/Stop in pairs only.
    BOOL m_fStarted;
    BOOL m_fAppDomainCreated; // this flag is used when an appdomain can only create a single appdomain

    // entrypoint helper to be wrapped in a filter to process unhandled exceptions
    VOID ExecuteMainInner(Assembly* pRootAssembly);

    static LONG  m_RefCount;

    SVAL_DECL(STARTUP_FLAGS, m_dwStartupFlags);
};

#endif // __CorHost__h__
