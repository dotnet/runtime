// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CorHost.cpp
//
// Implementation for the meta data dispenser code.
//

//*****************************************************************************

#include "common.h"

#include "mscoree.h"
#include "corhost.h"
#include "excep.h"
#include "threads.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "ceemain.h"
#include "eepolicy.h"
#include "clrex.h"
#include "comcallablewrapper.h"
#include "invokeutil.h"
#include "appdomain.inl"
#include "vars.hpp"
#include "comdelegate.h"
#include "dllimportcallback.h"
#include "eventtrace.h"

#include "win32threadpool.h"
#include "eventtrace.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#ifndef TARGET_UNIX
#include "dwreport.h"
#endif // !TARGET_UNIX

#ifndef DACCESS_COMPILE

extern void STDMETHODCALLTYPE EEShutDown(BOOL fIsDllUnloading);
extern void PrintToStdOutA(const char *pszString);
extern void PrintToStdOutW(const WCHAR *pwzString);

//***************************************************************************

// *** ICorRuntimeHost methods ***

CorHost2::CorHost2() :
    m_cRef(0), m_fFirstToLoadCLR(FALSE), m_fStarted(FALSE), m_fAppDomainCreated(FALSE)
{
    LIMITED_METHOD_CONTRACT;
}

static DangerousNonHostedSpinLock lockOnlyOneToInvokeStart;

STDMETHODIMP CorHost2::Start()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
    }CONTRACTL_END;

    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    // Ensure that only one thread at a time gets in here
    DangerousNonHostedSpinLockHolder lockHolder(&lockOnlyOneToInvokeStart);

    // To provide the complete semantic of Start/Stop in context of a given host, we check m_fStarted and let
    // them invoke the Start only if they have not already. Likewise, they can invoke the Stop method
    // only if they have invoked Start prior to that.
    //
    // This prevents a host from invoking Stop twice and hitting the refCount to zero, when another
    // host is using the CLR, as CLR instance sharing across hosts is a scenario for CoreCLR.

    if (g_fEEStarted)
    {
        hr = S_OK;
        // CoreCLR is already running - but was Start already invoked by this host?
        if (m_fStarted)
        {
            // This host had already invoked the Start method - return them an error
            hr = HOST_E_INVALIDOPERATION;
        }
        else
        {
            // Increment the global (and dynamic) refCount...
            FastInterlockIncrement(&m_RefCount);

            // And set our flag that this host has invoked the Start...
            m_fStarted = TRUE;
        }
    }
    else
    {
        hr = EnsureEEStarted();
        if (SUCCEEDED(hr))
        {
            // Set our flag that this host invoked the Start method.
            m_fStarted = TRUE;

            // And they also loaded the CoreCLR DLL in the memory (for this version).
            // This is a special flag as the host that has got this flag set will be allowed
            // to repeatedly invoke Stop method (without corresponding Start method invocations).
            // This is to support scenarios like that of Office where they need to bring down
            // the CLR at any cost.
            //
            // So, if you want to do that, just make sure you are the first host to load the
            // specific version of CLR in memory AND start it.
            m_fFirstToLoadCLR = TRUE;
            FastInterlockIncrement(&m_RefCount);
        }
    }

    END_ENTRYPOINT_NOTHROW;
    return hr;
}



HRESULT CorHost2::Stop()
{
    CONTRACTL
    {
        NOTHROW;
        ENTRY_POINT;    // We're bringing the EE down, so no point in probing
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;
    if (!g_fEEStarted)
    {
        return E_UNEXPECTED;
    }
    HRESULT hr=S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    // Is this host eligible to invoke the Stop method?
    if ((!m_fStarted) && (!m_fFirstToLoadCLR))
    {
        // Well - since this host never invoked Start, it is not eligible to invoke Stop.
        // Semantically, for such a host, CLR is not available in the process. The only
        // exception to this condition is the host that first loaded this version of the
        // CLR and invoked Start method. For details, refer to comments in CorHost2::Start implementation.
        hr = HOST_E_CLRNOTAVAILABLE;
    }
    else
    {
        while (TRUE)
        {
            LONG refCount = m_RefCount;
            if (refCount == 0)
            {
                hr = HOST_E_CLRNOTAVAILABLE;
                break;
            }
            else
            if (FastInterlockCompareExchange(&m_RefCount, refCount - 1, refCount) == refCount)
            {
                // Indicate that we have got a Stop for a corresponding Start call from the
                // Host. Semantically, CoreCLR has stopped for them.
                m_fStarted = FALSE;

                if (refCount > 1)
                {
                    hr=S_FALSE;
                    break;
                }
                else
                {
                    break;
                }
            }
        }
    }
    END_ENTRYPOINT_NOTHROW;


    return hr;
}


HRESULT CorHost2::GetCurrentAppDomainId(DWORD *pdwAppDomainId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive())
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(pdwAppDomainId == NULL)
    {
        hr = E_POINTER;
    }
    else
    {
        Thread *pThread = GetThreadNULLOk();
        if (!pThread)
        {
            hr = E_UNEXPECTED;
        }
        else
        {
            *pdwAppDomainId = DefaultADID;
        }
    }

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CorHost2::ExecuteApplication(LPCWSTR   pwzAppFullName,
                                     DWORD     dwManifestPaths,
                                     LPCWSTR   *ppwzManifestPaths,
                                     DWORD     dwActivationData,
                                     LPCWSTR   *ppwzActivationData,
                                     int       *pReturnValue)
{
    return E_NOTIMPL;
}

/*
 * This method processes the arguments sent to the host which are then used
 * to invoke the main method.
 * Note -
 * [0] - points to the assemblyName that has been sent by the host.
 * The rest are the arguments sent to the assembly.
 * Also note, this might not always return the exact same identity as the cmdLine
 * used to invoke the method.
 *
 * For example :-
 * ActualCmdLine - Foo arg1 arg2.
 * (Host1)       - Full_path_to_Foo arg1 arg2
*/
void SetCommandLineArgs(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR* argv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Record the command line.
    SaveManagedCommandLine(pwzAssemblyPath, argc, argv);

    // Send the command line to System.Environment.
    struct _gc
    {
        PTRARRAYREF cmdLineArgs;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    gc.cmdLineArgs = (PTRARRAYREF)AllocateObjectArray(argc + 1 /* arg[0] should be the exe name*/, g_pStringClass);
    OBJECTREF orAssemblyPath = StringObject::NewString(Bundle::AppIsBundle() ? static_cast<LPCWSTR>(Bundle::AppBundle->Path()) : pwzAssemblyPath);
    gc.cmdLineArgs->SetAt(0, orAssemblyPath);

    for (int i = 0; i < argc; ++i)
    {
        OBJECTREF argument = StringObject::NewString(argv[i]);
        gc.cmdLineArgs->SetAt(i + 1, argument);
    }

    MethodDescCallSite setCmdLineArgs(METHOD__ENVIRONMENT__SET_COMMAND_LINE_ARGS);

    ARG_SLOT args[] =
    {
        ObjToArgSlot(gc.cmdLineArgs),
    };
    setCmdLineArgs.Call(args);

    GCPROTECT_END();
}

HRESULT CorHost2::ExecuteAssembly(DWORD dwAppDomainId,
                                      LPCWSTR pwzAssemblyPath,
                                      int argc,
                                      LPCWSTR* argv,
                                      DWORD *pReturnValue)
{
    CONTRACTL
    {
        THROWS; // Throws...as we do not want it to swallow the managed exception
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // This is currently supported in default domain only
    if (dwAppDomainId != DefaultADID)
        return HOST_E_INVALIDOPERATION;

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive())
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    if(!pwzAssemblyPath)
        return E_POINTER;

    if(argc < 0)
    {
        return E_INVALIDARG;
    }

    if(argc > 0 && argv == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    AppDomain *pCurDomain = SystemDomain::GetCurrentDomain();

    Thread *pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            goto ErrExit;
        }
    }

    INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    _ASSERTE (!pThread->PreemptiveGCDisabled());

    Assembly *pAssembly = AssemblySpec::LoadAssembly(pwzAssemblyPath);

#if defined(FEATURE_MULTICOREJIT)
    pCurDomain->GetMulticoreJitManager().AutoStartProfile(pCurDomain);
#endif // defined(FEATURE_MULTICOREJIT)

    {
        GCX_COOP();

        // Here we call the managed method that gets the cmdLineArgs array.
        SetCommandLineArgs(pwzAssemblyPath, argc, argv);

        PTRARRAYREF arguments = NULL;
        GCPROTECT_BEGIN(arguments);

        arguments = (PTRARRAYREF)AllocateObjectArray(argc, g_pStringClass);
        for (int i = 0; i < argc; ++i)
        {
            STRINGREF argument = StringObject::NewString(argv[i]);
            arguments->SetAt(i, argument);
        }

        if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_Corhost_Swallow_Uncaught_Exceptions))
        {
            EX_TRY
                DWORD retval = pAssembly->ExecuteMainMethod(&arguments, TRUE /* waitForOtherThreads */);
                if (pReturnValue)
                {
                    *pReturnValue = retval;
                }
            EX_CATCH_HRESULT (hr)
        }
        else
        {
            DWORD retval = pAssembly->ExecuteMainMethod(&arguments, TRUE /* waitForOtherThreads */);
            if (pReturnValue)
            {
                *pReturnValue = retval;
            }
        }

        GCPROTECT_END();
    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;

ErrExit:

    return hr;
}

HRESULT CorHost2::ExecuteInDefaultAppDomain(LPCWSTR pwzAssemblyPath,
                                            LPCWSTR pwzTypeName,
                                            LPCWSTR pwzMethodName,
                                            LPCWSTR pwzArgument,
                                            DWORD   *pReturnValue)
{
    CONTRACTL
    {
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive())
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    if(! (pwzAssemblyPath && pwzTypeName && pwzMethodName) )
        return E_POINTER;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    Thread *pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            goto ErrExit;
        }
    }

    _ASSERTE (!pThread->PreemptiveGCDisabled());

    INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    EX_TRY
    {
        Assembly *pAssembly = AssemblySpec::LoadAssembly(pwzAssemblyPath);

        SString szTypeName(pwzTypeName);
        StackScratchBuffer buff1;
        const char* szTypeNameUTF8 = szTypeName.GetUTF8(buff1);
        MethodTable *pMT = ClassLoader::LoadTypeByNameThrowing(pAssembly,
                                                            NULL,
                                                            szTypeNameUTF8).AsMethodTable();

        SString szMethodName(pwzMethodName);
        StackScratchBuffer buff;
        const char* szMethodNameUTF8 = szMethodName.GetUTF8(buff);
        MethodDesc *pMethodMD = MemberLoader::FindMethod(pMT, szMethodNameUTF8, &gsig_SM_Str_RetInt);

        if (!pMethodMD)
        {
            hr = COR_E_MISSINGMETHOD;
        }
        else
        {
            GCX_COOP();

            MethodDescCallSite method(pMethodMD);

            STRINGREF sref = NULL;
            GCPROTECT_BEGIN(sref);

            if (pwzArgument)
                sref = StringObject::NewString(pwzArgument);

            ARG_SLOT MethodArgs[] =
            {
                ObjToArgSlot(sref)
            };
            DWORD retval = method.Call_RetI4(MethodArgs);
            if (pReturnValue)
            {
                *pReturnValue = retval;
            }

            GCPROTECT_END();
        }
    }
    EX_CATCH_HRESULT(hr);

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;

ErrExit:

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT ExecuteInAppDomainHelper(FExecuteInAppDomainCallback pCallback,
                                 void * cookie)
{
    STATIC_CONTRACT_THROWS;

    return pCallback(cookie);
}

HRESULT CorHost2::ExecuteInAppDomain(DWORD dwAppDomainId,
                                     FExecuteInAppDomainCallback pCallback,
                                     void * cookie)
{

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive())
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    // Moved this here since no point validating the pointer
    // if the basic checks [above] fail
    if( pCallback == NULL)
        return E_POINTER;

    // This is currently supported in default domain only
    if (dwAppDomainId != DefaultADID)
        return HOST_E_INVALIDOPERATION;

    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());

    // We are calling an unmanaged function pointer, either an unmanaged function, or a marshaled out delegate.
    // The thread should be in preemptive mode.
    {
        GCX_PREEMP();
        hr=ExecuteInAppDomainHelper (pCallback, cookie);
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#define EMPTY_STRING_TO_NULL(s) {if(s && s[0] == 0) {s=NULL;};}

HRESULT CorHost2::CreateAppDomainWithManager(
    LPCWSTR wszFriendlyName,
    DWORD  dwFlags,
    LPCWSTR wszAppDomainManagerAssemblyName,
    LPCWSTR wszAppDomainManagerTypeName,
    int nProperties,
    LPCWSTR* pPropertyNames,
    LPCWSTR* pPropertyValues,
    DWORD* pAppDomainID)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr=S_OK;

    //cannot call the function more than once when single appDomain is allowed
    if (m_fAppDomainCreated)
    {
        return HOST_E_INVALIDOPERATION;
    }

    //normalize empty strings
    EMPTY_STRING_TO_NULL(wszFriendlyName);
    EMPTY_STRING_TO_NULL(wszAppDomainManagerAssemblyName);
    EMPTY_STRING_TO_NULL(wszAppDomainManagerTypeName);

    if (pAppDomainID==NULL)
        return E_POINTER;

    if (!m_fStarted)
        return HOST_E_INVALIDOPERATION;

    if (wszFriendlyName == NULL)
        return E_INVALIDARG;

    if ((wszAppDomainManagerAssemblyName != NULL) || (wszAppDomainManagerTypeName != NULL))
        return E_INVALIDARG;

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);

    AppDomain* pDomain = SystemDomain::System()->DefaultDomain();

    pDomain->SetFriendlyName(wszFriendlyName);

    ETW::LoaderLog::DomainLoad(pDomain, (LPWSTR)wszFriendlyName);

    if (dwFlags & APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS)
        pDomain->SetIgnoreUnhandledExceptions();

    if (dwFlags & APPDOMAIN_FORCE_TRIVIAL_WAIT_OPERATIONS)
        pDomain->SetForceTrivialWaitOperations();

    pDomain->CreateBinderContext();

    {
        GCX_COOP();

        MethodDescCallSite setup(METHOD__APPCONTEXT__SETUP);

        ARG_SLOT args[3];
        args[0] = PtrToArgSlot(pPropertyNames);
        args[1] = PtrToArgSlot(pPropertyValues);
        args[2] = PtrToArgSlot(nProperties);

        setup.Call(args);
    }

    LPCWSTR pwzNativeDllSearchDirectories = NULL;
    LPCWSTR pwzTrustedPlatformAssemblies = NULL;
    LPCWSTR pwzPlatformResourceRoots = NULL;
    LPCWSTR pwzAppPaths = NULL;
    LPCWSTR pwzAppNiPaths = NULL;

    for (int i = 0; i < nProperties; i++)
    {
        if (wcscmp(pPropertyNames[i], W("NATIVE_DLL_SEARCH_DIRECTORIES")) == 0)
        {
            pwzNativeDllSearchDirectories = pPropertyValues[i];
        }
        else
        if (wcscmp(pPropertyNames[i], W("TRUSTED_PLATFORM_ASSEMBLIES")) == 0)
        {
            pwzTrustedPlatformAssemblies = pPropertyValues[i];
        }
        else
        if (wcscmp(pPropertyNames[i], W("PLATFORM_RESOURCE_ROOTS")) == 0)
        {
            pwzPlatformResourceRoots = pPropertyValues[i];
        }
        else
        if (wcscmp(pPropertyNames[i], W("APP_PATHS")) == 0)
        {
            pwzAppPaths = pPropertyValues[i];
        }
        else
        if (wcscmp(pPropertyNames[i], W("APP_NI_PATHS")) == 0)
        {
            pwzAppNiPaths = pPropertyValues[i];
        }
        else
        if (wcscmp(pPropertyNames[i], W("DEFAULT_STACK_SIZE")) == 0)
        {
            extern void ParseDefaultStackSize(LPCWSTR value);
            ParseDefaultStackSize(pPropertyValues[i]);
        }
        else
        if (wcscmp(pPropertyNames[i], W("USE_ENTRYPOINT_FILTER")) == 0)
        {
            extern void ParseUseEntryPointFilter(LPCWSTR value);
            ParseUseEntryPointFilter(pPropertyValues[i]);
        }
    }

    pDomain->SetNativeDllSearchDirectories(pwzNativeDllSearchDirectories);

    {
        SString sTrustedPlatformAssemblies(pwzTrustedPlatformAssemblies);
        SString sPlatformResourceRoots(pwzPlatformResourceRoots);
        SString sAppPaths(pwzAppPaths);
        SString sAppNiPaths(pwzAppNiPaths);

        CLRPrivBinderCoreCLR *pBinder = pDomain->GetTPABinderContext();
        _ASSERTE(pBinder != NULL);
        IfFailThrow(pBinder->SetupBindingPaths(
            sTrustedPlatformAssemblies,
            sPlatformResourceRoots,
            sAppPaths,
            sAppNiPaths));
    }

    *pAppDomainID=DefaultADID;

    m_fAppDomainCreated = TRUE;

    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CorHost2::CreateDelegate(
    DWORD appDomainID,
    LPCWSTR wszAssemblyName,
    LPCWSTR wszClassName,
    LPCWSTR wszMethodName,
    INT_PTR* fnPtr)
{

    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr=S_OK;

    EMPTY_STRING_TO_NULL(wszAssemblyName);
    EMPTY_STRING_TO_NULL(wszClassName);
    EMPTY_STRING_TO_NULL(wszMethodName);

    if (fnPtr == NULL)
       return E_POINTER;
    *fnPtr = NULL;

    if(wszAssemblyName == NULL)
        return E_INVALIDARG;

    if(wszClassName == NULL)
        return E_INVALIDARG;

    if(wszMethodName == NULL)
        return E_INVALIDARG;

    // This is currently supported in default domain only
    if (appDomainID != DefaultADID)
        return HOST_E_INVALIDOPERATION;

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());

    MAKE_UTF8PTR_FROMWIDE(szAssemblyName, wszAssemblyName);
    MAKE_UTF8PTR_FROMWIDE(szClassName, wszClassName);
    MAKE_UTF8PTR_FROMWIDE(szMethodName, wszMethodName);

    {
        GCX_PREEMP();

        AssemblySpec spec;
        spec.Init(szAssemblyName);
        Assembly* pAsm=spec.LoadAssembly(FILE_ACTIVE);

        TypeHandle th=pAsm->GetLoader()->LoadTypeByNameThrowing(pAsm,NULL,szClassName);
        MethodDesc* pMD=NULL;

        if (!th.IsTypeDesc())
        {
            pMD = MemberLoader::FindMethodByName(th.GetMethodTable(), szMethodName, MemberLoader::FM_Unique);
            if (pMD == NULL)
            {
                // try again without the FM_Unique flag (error path)
                pMD = MemberLoader::FindMethodByName(th.GetMethodTable(), szMethodName, MemberLoader::FM_Default);
                if (pMD != NULL)
                {
                    // the method exists but is overloaded
                    ThrowHR(COR_E_AMBIGUOUSMATCH);
                }
            }
        }

        if (pMD==NULL || !pMD->IsStatic() || pMD->HasClassOrMethodInstantiation())
            ThrowHR(COR_E_MISSINGMETHOD);

        if (pMD->HasUnmanagedCallersOnlyAttribute())
        {
            *fnPtr = pMD->GetMultiCallableAddrOfCode();
        }
        else
        {
            UMEntryThunk* pUMEntryThunk = pMD->GetLoaderAllocator()->GetUMEntryThunkCache()->GetUMEntryThunk(pMD);
            *fnPtr = (INT_PTR)pUMEntryThunk->GetCode();
        }
    }

    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CorHost2::Authenticate(ULONGLONG authKey)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    // Host authentication was used by Silverlight. It is no longer relevant for CoreCLR.
    return S_OK;
}

HRESULT CorHost2::RegisterMacEHPort()
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    return S_OK;
}

HRESULT CorHost2::SetStartupFlags(STARTUP_FLAGS flag)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    if (g_fEEStarted)
    {
        return HOST_E_INVALIDOPERATION;
    }

    m_dwStartupFlags = flag;

    return S_OK;
}

#endif //!DACCESS_COMPILE

#ifndef DACCESS_COMPILE
SVAL_IMPL(STARTUP_FLAGS, CorHost2, m_dwStartupFlags = STARTUP_CONCURRENT_GC);
#else
SVAL_IMPL(STARTUP_FLAGS, CorHost2, m_dwStartupFlags);
#endif

STARTUP_FLAGS CorHost2::GetStartupFlags()
{
    return m_dwStartupFlags;
}

#ifndef DACCESS_COMPILE


extern "C"
DLLEXPORT
HRESULT  GetCLRRuntimeHost(REFIID riid, IUnknown **ppUnk)
{
    WRAPPER_NO_CONTRACT;

    return CorHost2::CreateObject(riid, (void**)ppUnk);
}


STDMETHODIMP CorHost2::UnloadAppDomain(DWORD dwDomainId, BOOL fWaitUntilDone)
{
    return UnloadAppDomain2(dwDomainId, fWaitUntilDone, nullptr);
}

STDMETHODIMP CorHost2::UnloadAppDomain2(DWORD dwDomainId, BOOL fWaitUntilDone, int *pLatchedExitCode)
{
    WRAPPER_NO_CONTRACT;

    if (!m_fStarted)
        return HOST_E_INVALIDOPERATION;

    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    if(!m_fAppDomainCreated)
    {
        return HOST_E_INVALIDOPERATION;
    }

    HRESULT hr=S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    if (!m_fFirstToLoadCLR)
    {
        _ASSERTE(!"Not reachable");
        hr = HOST_E_CLRNOTAVAILABLE;
    }
    else
    {
        LONG refCount = m_RefCount;
        if (refCount == 0)
        {
            hr = HOST_E_CLRNOTAVAILABLE;
        }
        else
        if (1 == refCount)
        {
            // Stop coreclr on unload.
            EEShutDown(FALSE);
        }
        else
        {
            _ASSERTE(!"Not reachable");
            hr = S_FALSE;
        }
    }
    END_ENTRYPOINT_NOTHROW;

    if (pLatchedExitCode)
    {
        *pLatchedExitCode = GetLatchedExitCode();
    }

    return hr;
}

//*****************************************************************************
// IUnknown
//*****************************************************************************

ULONG CorHost2::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return InterlockedIncrement(&m_cRef);
}

ULONG CorHost2::Release()
{
    LIMITED_METHOD_CONTRACT;

    ULONG cRef = InterlockedDecrement(&m_cRef);
    if (!cRef) {
        delete this;
    }

    return (cRef);
}

HRESULT CorHost2::QueryInterface(REFIID riid, void **ppUnk)
{
    if (!ppUnk)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (ppUnk == NULL)
    {
        return E_POINTER;
    }

    *ppUnk = 0;

    // Deliberately do NOT hand out ICorConfiguration.  They must explicitly call
    // GetConfiguration to obtain that interface.
    if (riid == IID_IUnknown)
    {
        *ppUnk = static_cast<IUnknown *>(static_cast<ICLRRuntimeHost *>(this));
    }
    else if (riid == IID_ICLRRuntimeHost)
    {
        *ppUnk = static_cast<ICLRRuntimeHost *>(this);
    }
    else if (riid == IID_ICLRRuntimeHost2)
    {
        *ppUnk = static_cast<ICLRRuntimeHost2 *>(this);
    }
    else if (riid == IID_ICLRRuntimeHost4)
    {
        *ppUnk = static_cast<ICLRRuntimeHost4 *>(this);
    }
#ifndef TARGET_UNIX
    else if (riid == IID_IPrivateManagedExceptionReporting)
    {
        *ppUnk = static_cast<IPrivateManagedExceptionReporting *>(this);
    }
#endif // !TARGET_UNIX
    else
        return (E_NOINTERFACE);
    AddRef();
    return (S_OK);
}


#ifndef TARGET_UNIX
HRESULT CorHost2::GetBucketParametersForCurrentException(BucketParameters *pParams)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    // To avoid confusion, clear the buckets.
    memset(pParams, 0, sizeof(BucketParameters));

    // Defer to Watson helper.
    hr = ::GetBucketParametersForCurrentException(pParams);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}
#endif // !TARGET_UNIX

HRESULT CorHost2::CreateObject(REFIID riid, void **ppUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    CorHost2 *pCorHost = new (nothrow) CorHost2();
    if (!pCorHost)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        hr = pCorHost->QueryInterface(riid, ppUnk);
        if (FAILED(hr))
            delete pCorHost;
    }
    return (hr);
}

LONG CorHost2::m_RefCount = 0;

///////////////////////////////////////////////////////////////////////////////
// ICLRRuntimeHost::SetHostControl
///////////////////////////////////////////////////////////////////////////////
HRESULT CorHost2::SetHostControl(IHostControl* pHostControl)
{
    return E_NOTIMPL;
}

///////////////////////////////////////////////////////////////////////////////
// ICLRRuntimeHost::GetCLRControl
HRESULT CorHost2::GetCLRControl(ICLRControl** pCLRControl)
{
    return E_NOTIMPL;
}

///////////////////////////////////////////////////////////////////////////////

// Note: Sampling profilers also use this function to initialize TLS for a unmanaged
// sampling thread so that initialization can be done in advance to avoid deadlocks.
// See ProfToEEInterfaceImpl::InitializeCurrentThread for more details.
void SetupTLSForThread()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

#ifdef STRESS_LOG
    if (StressLog::StressLogOn(~0u, 0))
    {
        StressLog::CreateThreadStressLog();
    }
#endif

    // Make sure ThreadType can be seen by SOS
    ClrFlsSetThreadType((TlsThreadTypeFlag)0);

#ifdef ENABLE_CONTRACTS
    // Profilers need the side effect of GetClrDebugState() to perform initialization
    // in advance to avoid deadlocks. Refer to ProfToEEInterfaceImpl::InitializeCurrentThread
    ::GetClrDebugState();
#endif
}

#ifdef ENABLE_CONTRACTS_IMPL
// Fls callback to deallocate ClrDebugState when our FLS block goes away.
void FreeClrDebugState(LPVOID pTlsData);
#endif

// Called here from a thread detach or from destruction of a Thread object.
void ThreadDetaching()
{
    // Can not cause memory allocation during thread detach, so no real contracts.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    // This function may be called twice:
    // 1. When a physical thread dies, our DLL_THREAD_DETACH calls this function with pTlsData = NULL
    // 2. When a fiber is destroyed, or OS calls FlsCallback after DLL_THREAD_DETACH process.
    // We will null the FLS and TLS entry if it matches the deleted one.

    if (StressLog::t_pCurrentThreadLog != NULL)
    {
#ifdef STRESS_LOG
        StressLog::ThreadDetach();
#else
        _ASSERTE (!"should not have StressLog");
#endif
    }

#ifdef ENABLE_CONTRACTS_IMPL
    if (t_pClrDebugState != NULL)
    {
        void* pData = t_pClrDebugState;
        t_pClrDebugState = 0;
        FreeClrDebugState(pData);
    }
#endif // ENABLE_CONTRACTS_IMPL
}


HRESULT CorHost2::DllGetActivationFactory(DWORD appDomainID, LPCWSTR wszTypeName, IActivationFactory ** factory)
{
    return E_NOTIMPL;
}

#endif // !DACCESS_COMPILE
