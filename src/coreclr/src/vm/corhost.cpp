// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "hosting.h"
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

#ifndef FEATURE_PAL
#include "dwreport.h"
#endif // !FEATURE_PAL

#include "stringarraylist.h"
#ifdef FEATURE_PERFTRACING
#include "eventpipe.h"
#endif // FEATURE_PERFTRACING

#ifdef FEATURE_COMINTEROP
#include "winrttypenameconverter.h"
#endif


GVAL_IMPL_INIT(DWORD, g_fHostConfig, 0);

#ifndef __llvm__
EXTERN_C __declspec(thread) ThreadLocalInfo gCurrentThreadInfo;
#else // !__llvm__
EXTERN_C __thread ThreadLocalInfo gCurrentThreadInfo;
#endif // !__llvm__
#ifndef FEATURE_PAL
EXTERN_C UINT32 _tls_index;
#else // FEATURE_PAL
UINT32 _tls_index = 0;
#endif // FEATURE_PAL

#if defined(FEATURE_WINDOWSPHONE)
SVAL_IMPL_INIT(ECustomDumpFlavor, CCLRErrorReportingManager, g_ECustomDumpFlavor, DUMP_FLAVOR_Default);
#endif

#ifndef DACCESS_COMPILE

extern void STDMETHODCALLTYPE EEShutDown(BOOL fIsDllUnloading);
extern HRESULT STDAPICALLTYPE CoInitializeEE(DWORD fFlags);
extern void PrintToStdOutA(const char *pszString);
extern void PrintToStdOutW(const WCHAR *pwzString);
extern BOOL g_fEEHostedStartup;

INT64 g_PauseTime;         // Total time in millisecond the CLR has been paused
Volatile<BOOL> g_IsPaused;  // True if the runtime is paused (FAS)
CLREventStatic g_ClrResumeEvent; // Event that is fired at FAS Resuming 

extern BYTE g_rbTestKeyBuffer[];

//***************************************************************************

ULONG CorRuntimeHostBase::m_Version = 0;


#if defined(FEATURE_WINDOWSPHONE)
CCLRErrorReportingManager g_CLRErrorReportingManager;
#endif // defined(FEATURE_WINDOWSPHONE)

#endif // !DAC

typedef DPTR(CONNID)   PTR_CONNID;



// Keep track connection id and name

#ifndef DACCESS_COMPILE




// *** ICorRuntimeHost methods ***

CorHost2::CorHost2() : m_fFirstToLoadCLR(FALSE), m_fStarted(FALSE), m_fAppDomainCreated(FALSE)
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
        // Using managed C++ libraries, its possible that when the runtime is already running,
        // MC++ will use CorBindToRuntimeEx to make callbacks into specific appdomain of its
        // choice. Now, CorBindToRuntimeEx results in CorHost2::CreateObject being invoked
        // that will set runtime hosted flag "g_fHostConfig |= CLRHOSTED".
        //
        // For the case when managed code started without CLR hosting and MC++ does a 
        // CorBindToRuntimeEx, setting the CLR hosted flag is incorrect.
        //
        // Thus, before we attempt to start the runtime, we save the status of it being
        // already running or not. Next, if we are able to successfully start the runtime
        // and ONLY if it was not started earlier will we set the hosted flag below.
        if (!g_fEEStarted)
        {
            g_fHostConfig |= CLRHOSTED;
        }

        hr = CorRuntimeHostBase::Start();
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

// Starts the runtime. This is equivalent to CoInitializeEE();
HRESULT CorRuntimeHostBase::Start()
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    {
        m_Started = TRUE;
#ifdef FEATURE_EVENT_TRACE
        g_fEEHostedStartup = TRUE;
#endif // FEATURE_EVENT_TRACE
        hr = InitializeEE(COINITEE_DEFAULT);
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
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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
    // We use CanRunManagedCode() instead of IsRuntimeActive() because this allows us
    // to specify test using the form that does not trigger a GC.
    if (!(g_fEEStarted && CanRunManagedCode(LoaderLockCheck::None)))
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
        Thread *pThread = GetThread();
        if (!pThread)
        {
            hr = E_UNEXPECTED;
        }
        else
        {
            *pdwAppDomainId = SystemDomain::GetCurrentDomain()->GetId().m_dwId;
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

    // Send the command line to EventPipe.
#ifdef FEATURE_PERFTRACING
    EventPipe::SaveCommandLine(pwzAssemblyPath, argc, argv);
#endif // FEATURE_PERFTRACING

    // Send the command line to System.Environment.
    struct _gc
    {
        PTRARRAYREF cmdLineArgs;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    gc.cmdLineArgs = (PTRARRAYREF)AllocateObjectArray(argc + 1 /* arg[0] should be the exe name*/, g_pStringClass);
    OBJECTREF orAssemblyPath = StringObject::NewString(pwzAssemblyPath);
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

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            goto ErrExit;
        }
    }

    if(pCurDomain->GetId().m_dwId != DefaultADID)
    {
        return HOST_E_INVALIDOPERATION;
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

        DWORD retval = pAssembly->ExecuteMainMethod(&arguments, TRUE /* waitForOtherThreads */);
        if (pReturnValue)
        {
            *pReturnValue = retval;
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

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            goto ErrExit;
        }
    }

    _ASSERTE (!pThread->PreemptiveGCDisabled());

    _ASSERTE (SystemDomain::GetCurrentDomain()->GetId().m_dwId == DefaultADID);

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
    STATIC_CONTRACT_SO_INTOLERANT;

    HRESULT hr = S_OK;

    BEGIN_SO_TOLERANT_CODE(GetThread());
    hr = pCallback(cookie);
    END_SO_TOLERANT_CODE;

    return hr;
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

    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());
    ENTER_DOMAIN_ID(ADID(dwAppDomainId))
    {
        // We are calling an unmanaged function pointer, either an unmanaged function, or a marshaled out delegate.
        // The thread should be in preemptive mode, and SO_Tolerant.
        GCX_PREEMP();
        hr=ExecuteInAppDomainHelper (pCallback, cookie);
    }
    END_DOMAIN_TRANSITION;
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#define EMPTY_STRING_TO_NULL(s) {if(s && s[0] == 0) {s=NULL;};}

HRESULT CorHost2::_CreateAppDomain(
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
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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

    pDomain->CreateFusionContext();

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
#ifdef FEATURE_COMINTEROP
    LPCWSTR pwzAppLocalWinMD = NULL;
#endif

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
#ifdef FEATURE_COMINTEROP
        else
        if (wcscmp(pPropertyNames[i], W("APP_LOCAL_WINMETADATA")) == 0)
        {
            pwzAppLocalWinMD = pPropertyValues[i];
        }
#endif
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

#ifdef FEATURE_COMINTEROP
    if (WinRTSupported())
    {
        pDomain->SetWinrtApplicationContext(pwzAppLocalWinMD);
    }
#endif

    *pAppDomainID=pDomain->GetId().m_dwId;

    m_fAppDomainCreated = TRUE;

    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CorHost2::_CreateDelegate(
    DWORD appDomainID,
    LPCWSTR wszAssemblyName,     
    LPCWSTR wszClassName,     
    LPCWSTR wszMethodName,
    INT_PTR* fnPtr)
{

    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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
    
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());

    MAKE_UTF8PTR_FROMWIDE(szAssemblyName, wszAssemblyName);
    MAKE_UTF8PTR_FROMWIDE(szClassName, wszClassName);
    MAKE_UTF8PTR_FROMWIDE(szMethodName, wszMethodName);

    ADID id;
    id.m_dwId=appDomainID;

    ENTER_DOMAIN_ID(id)

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

    if (pMD==NULL || !pMD->IsStatic() || pMD->ContainsGenericVariables()) 
        ThrowHR(COR_E_MISSINGMETHOD);

    UMEntryThunk *pUMEntryThunk = pMD->GetLoaderAllocator()->GetUMEntryThunkCache()->GetUMEntryThunk(pMD);
    *fnPtr = (INT_PTR)pUMEntryThunk->GetCode();

    END_DOMAIN_TRANSITION;

    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

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
    WRAPPER_NO_CONTRACT;

    return _CreateAppDomain(
        wszFriendlyName,
        dwFlags,
        wszAppDomainManagerAssemblyName, 
        wszAppDomainManagerTypeName, 
        nProperties, 
        pPropertyNames, 
        pPropertyValues,
        pAppDomainID);
}

HRESULT CorHost2::CreateDelegate(
    DWORD appDomainID,
    LPCWSTR wszAssemblyName,     
    LPCWSTR wszClassName,     
    LPCWSTR wszMethodName,
    INT_PTR* fnPtr)
{
    WRAPPER_NO_CONTRACT;

    return _CreateDelegate(appDomainID, wszAssemblyName, wszClassName, wszMethodName, fnPtr);
}

HRESULT CorHost2::Authenticate(ULONGLONG authKey)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
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



HRESULT SuspendEEForPause()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // In CoreCLR, we always resume from the same thread that paused.  So we can simply suspend the EE from this thread,
    // knowing we'll restart from the same thread.
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);

    return hr;
}

HRESULT RestartEEFromPauseAndSetResumeEvent()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // see comments in SuspendEEFromPause
    ThreadSuspend::RestartEE(FALSE, TRUE);

    _ASSERTE(g_ClrResumeEvent.IsValid());
    g_ClrResumeEvent.Set();

    return S_OK;
}
    


CorExecutionManager::CorExecutionManager()
    : m_dwFlags(0), m_pauseStartTime(0)
{
    LIMITED_METHOD_CONTRACT;
    g_IsPaused = FALSE;
    g_PauseTime = 0;
}

HRESULT CorExecutionManager::Pause(DWORD dwAppDomainId, DWORD dwFlags)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;


    if(g_IsPaused)
        return E_FAIL;

    EX_TRY
    {
        if(!g_ClrResumeEvent.IsValid())
            g_ClrResumeEvent.CreateManualEvent(FALSE);
        else
            g_ClrResumeEvent.Reset();

    }
    EX_CATCH_HRESULT(hr);
    
    if (FAILED(hr))
        return hr;
    
    BEGIN_ENTRYPOINT_NOTHROW;

    m_dwFlags = dwFlags;


    if (SUCCEEDED(hr))
    {
        g_IsPaused = TRUE;

        hr = SuspendEEForPause();

        // Even though this is named with TickCount, it returns milliseconds
        m_pauseStartTime = (INT64)CLRGetTickCount64(); 
    }

    END_ENTRYPOINT_NOTHROW;

    return hr;
}


HRESULT CorExecutionManager::Resume(DWORD dwAppDomainId)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;


    if(!g_IsPaused)
        return E_FAIL;

    // GCThread is the thread that did the Pause. Resume should also happen on that same thread
    Thread *pThread = GetThread();
    if(pThread != ThreadSuspend::GetSuspensionThread())
    {
        _ASSERTE(!"HOST BUG: The same thread that did Pause should do the Resume");
        return E_FAIL;
    }

    BEGIN_ENTRYPOINT_NOTHROW;

    // Even though this is named with TickCount, it returns milliseconds
    INT64 currTime = (INT64)CLRGetTickCount64(); 
    _ASSERTE(currTime >= m_pauseStartTime);
    _ASSERTE(m_pauseStartTime != 0);

    g_PauseTime += (currTime - m_pauseStartTime);
    g_IsPaused = FALSE;

    hr = RestartEEFromPauseAndSetResumeEvent();


    END_ENTRYPOINT_NOTHROW;

    return hr;
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


#ifdef FEATURE_COMINTEROP

// Enumerate currently existing domains.
HRESULT CorRuntimeHostBase::EnumDomains(HDOMAINENUM *hEnum)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        WRAPPER(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if(hEnum == NULL) return E_POINTER;

    // Thread setup happens in BEGIN_EXTERNAL_ENTRYPOINT below.
    // If the runtime has not started, we have nothing to do.
    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    HRESULT hr = E_OUTOFMEMORY;
    *hEnum = NULL;
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)

    AppDomainIterator *pEnum = new (nothrow) AppDomainIterator(FALSE);
    if(pEnum) {
        *hEnum = (HDOMAINENUM) pEnum;
        hr = S_OK;
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // FEATURE_COMINTEROP

extern "C"
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
    STATIC_CONTRACT_SO_TOLERANT;

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

HRESULT CorRuntimeHostBase::UnloadAppDomain(DWORD dwDomainId, BOOL fWaitUntilDone)
{
    return UnloadAppDomain2(dwDomainId, fWaitUntilDone, nullptr);
}

HRESULT CorRuntimeHostBase::UnloadAppDomain2(DWORD dwDomainId, BOOL fWaitUntilDone, int *pLatchedExitCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        FORBID_FAULT; // Unloading domains cannot fail due to OOM
        ENTRY_POINT;
    }
    CONTRACTL_END;

    return COR_E_CANNOTUNLOADAPPDOMAIN;
}

//*****************************************************************************
// Fiber Methods
//*****************************************************************************

HRESULT CorRuntimeHostBase::LocksHeldByLogicalThread(DWORD *pCount)
{
    if (!pCount)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_NOTHROW;

    Thread* pThread = GetThread();
    if (pThread == NULL)
        *pCount = 0;
    else
        *pCount = pThread->m_dwLockCount;

    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

//*****************************************************************************
// ICorConfiguration
//*****************************************************************************

//*****************************************************************************
// IUnknown
//*****************************************************************************

ULONG CorRuntimeHostBase::AddRef()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        SO_TOLERANT;
    }
    CONTRACTL_END;
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
        SO_TOLERANT;    // no global state updates that need guarding.
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
        ULONG version = 2;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRRuntimeHost2 *>(this);
    }
    else if (riid == IID_ICLRRuntimeHost4)
    {
        ULONG version = 4;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRRuntimeHost4 *>(this);
    }
    else if (riid == IID_ICLRExecutionManager)
    {
        ULONG version = 2;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRExecutionManager *>(this);
    }
#ifndef FEATURE_PAL
    else if (riid == IID_IPrivateManagedExceptionReporting)
    {
        *ppUnk = static_cast<IPrivateManagedExceptionReporting *>(this);
    }
#endif // !FEATURE_PAL
    else
        return (E_NOINTERFACE);
    AddRef();
    return (S_OK);
}


#ifndef FEATURE_PAL
HRESULT CorHost2::GetBucketParametersForCurrentException(BucketParameters *pParams)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
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
#endif // !FEATURE_PAL

HRESULT CorHost2::CreateObject(REFIID riid, void **ppUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW; );
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
    END_SO_INTOLERANT_CODE;
    return (hr);
}


//-----------------------------------------------------------------------------
// MapFile - Maps a file into the runtime in a non-standard way
//-----------------------------------------------------------------------------

static PEImage *MapFileHelper(HANDLE hFile)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    HandleHolder hFileMap(WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL));
    if (hFileMap == NULL)
        ThrowLastError();

    CLRMapViewHolder base(CLRMapViewOfFile(hFileMap, FILE_MAP_READ, 0, 0, 0));
    if (base == NULL)
        ThrowLastError();

    DWORD dwSize = SafeGetFileSize(hFile, NULL);
    if (dwSize == 0xffffffff && GetLastError() != NOERROR)
    {
        ThrowLastError();
    }
    PEImageHolder pImage(PEImage::LoadFlat(base, dwSize));
    return pImage.Extract();
}

HRESULT CorRuntimeHostBase::MapFile(HANDLE hFile, HMODULE* phHandle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        *phHandle = (HMODULE) (MapFileHelper(hFile)->GetLoadedLayout()->GetBase());
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;


    return hr;
}

///////////////////////////////////////////////////////////////////////////////
// IDebuggerInfo::IsDebuggerAttached

LONG CorHost2::m_RefCount = 0;

IHostControl *CorHost2::m_HostControl = NULL;

#ifdef _DEBUG
extern void ValidateHostInterface();
#endif

static Volatile<BOOL> fOneOnly = 0;

///////////////////////////////////////////////////////////////////////////////
// ICLRRuntimeHost::SetHostControl
///////////////////////////////////////////////////////////////////////////////
HRESULT CorHost2::SetHostControl(IHostControl* pHostControl)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    if (m_Version < 2)
        // CLR is hosted with v1 hosting interface.  Some part of v2 hosting API are disabled.
        return HOST_E_INVALIDOPERATION;

    if (pHostControl == 0)
        return E_INVALIDARG;

    // If Runtime has been started, do not allow setting HostMemoryManager
    if (g_fEEStarted)
        return E_ACCESSDENIED;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    
    DWORD dwSwitchCount = 0;

    while (FastInterlockExchange((LONG*)&fOneOnly, 1) == 1)
    {
             __SwitchToThread(0, ++dwSwitchCount);
    }
    

    if (m_HostControl == NULL)
    {
        m_HostControl = pHostControl;
        m_HostControl->AddRef();
    }

    goto ErrExit;

ErrExit:
    fOneOnly = 0;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

class CCLRPolicyManager: public ICLRPolicyManager
{
public:
    virtual HRESULT STDMETHODCALLTYPE SetDefaultAction(EClrOperation operation,
                                                       EPolicyAction action)
    {
        LIMITED_METHOD_CONTRACT;
        return E_NOTIMPL;
    }

    virtual HRESULT STDMETHODCALLTYPE SetTimeout(EClrOperation operation,
                                                 DWORD dwMilliseconds)
    {
        LIMITED_METHOD_CONTRACT;
        return E_NOTIMPL;
    }

    virtual HRESULT STDMETHODCALLTYPE SetActionOnTimeout(EClrOperation operation,
                                                         EPolicyAction action)
    {
        LIMITED_METHOD_CONTRACT;
        return E_NOTIMPL;
    }

    virtual HRESULT STDMETHODCALLTYPE SetTimeoutAndAction(EClrOperation operation, DWORD dwMilliseconds,
                                                          EPolicyAction action)
    {
        LIMITED_METHOD_CONTRACT;
        return E_NOTIMPL;
    }

    virtual HRESULT STDMETHODCALLTYPE SetActionOnFailure(EClrFailure failure,
                                                         EPolicyAction action)
    {
        // This is enabled for CoreCLR since a host can use this to 
        // specify action for handling AV.
        STATIC_CONTRACT_ENTRY_POINT;
        LIMITED_METHOD_CONTRACT;
        HRESULT hr;
        // For CoreCLR, this method just supports FAIL_AccessViolation as a valid
        // failure input arg. The validation of the specified action for the failure
        // will be done in EEPolicy::IsValidActionForFailure.
        if (failure != FAIL_AccessViolation)
        {
            return E_INVALIDARG;
        }
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetActionOnFailure(failure,action);
        END_ENTRYPOINT_NOTHROW;
        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE SetUnhandledExceptionPolicy(EClrUnhandledException policy)
    {
        LIMITED_METHOD_CONTRACT;
        return E_NOTIMPL;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return 1;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return 1;
    }

    BEGIN_INTERFACE HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
                                                             void **ppvObject)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        if (riid != IID_ICLRPolicyManager && riid != IID_IUnknown)
            return (E_NOINTERFACE);

        // Ensure that the out going pointer is not null
        if (ppvObject == NULL)
            return E_POINTER;

        *ppvObject = this;
        return S_OK;
    }
};

static CCLRPolicyManager s_PolicyManager;



void ProcessEventForHost(EClrEvent event, void *data)
{
}

// We do not call ProcessEventForHost for stack overflow, since we have limit stack
// and we should avoid calling GCX_PREEMPT
void ProcessSOEventForHost(EXCEPTION_POINTERS *pExceptionInfo, BOOL fInSoTolerant)
{
}

BOOL IsHostRegisteredForEvent(EClrEvent event)
{
    WRAPPER_NO_CONTRACT;
    return FALSE;
}

inline size_t SizeInKBytes(size_t cbSize)
{
    LIMITED_METHOD_CONTRACT;
    size_t cb = (cbSize % 1024) ? 1 : 0;
    return ((cbSize / 1024) + cb);
}

SIZE_T Host_SegmentSize = 0;
SIZE_T Host_MaxGen0Size = 0;
BOOL  Host_fSegmentSizeSet = FALSE;
BOOL  Host_fMaxGen0SizeSet = FALSE;

void UpdateGCSettingFromHost ()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE (g_pConfig);
    if (Host_fSegmentSizeSet)
    {
        g_pConfig->SetSegmentSize(Host_SegmentSize);
    }
    if (Host_fMaxGen0SizeSet)
    {
        g_pConfig->SetGCgen0size(Host_MaxGen0Size);
    }
}

#if defined(FEATURE_WINDOWSPHONE)
class CCLRGCManager: public ICLRGCManager2
{
public:
    virtual HRESULT STDMETHODCALLTYPE Collect(LONG Generation)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT     hr = S_OK;

        if (Generation > (int) GCHeapUtilities::GetGCHeap()->GetMaxGeneration())
            hr = E_INVALIDARG;

        if (SUCCEEDED(hr))
        {
            // Set up a Thread object if this is called on a native thread.
            Thread *pThread;
            pThread = GetThread();
            if (pThread == NULL)
                pThread = SetupThreadNoThrow(&hr);
            if (pThread != NULL)
            {
                BEGIN_ENTRYPOINT_NOTHROW_WITH_THREAD(pThread);
                GCX_COOP();

                EX_TRY
                {
                    STRESS_LOG0(LF_GC, LL_INFO100, "Host triggers GC\n");
                    hr = GCHeapUtilities::GetGCHeap()->GarbageCollect(Generation);
                }
                EX_CATCH
                {
                    hr = GET_EXCEPTION()->GetHR();
                }
                EX_END_CATCH(SwallowAllExceptions);

                END_ENTRYPOINT_NOTHROW_WITH_THREAD;
            }
        }

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE GetStats(COR_GC_STATS *pStats)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

    #if defined(ENABLE_PERF_COUNTERS)

        Perf_GC     *pgc = &GetPerfCounters().m_GC;

        if (!pStats)
            IfFailGo(E_INVALIDARG);

        if (pStats->Flags & COR_GC_COUNTS)
        {
            pStats->ExplicitGCCount = pgc->cInducedGCs;

            for (int idx=0; idx<3; idx++)
                pStats->GenCollectionsTaken[idx] = pgc->cGenCollections[idx];
        }

        if (pStats->Flags & COR_GC_MEMORYUSAGE)
        {
            pStats->CommittedKBytes = SizeInKBytes(pgc->cTotalCommittedBytes);
            pStats->ReservedKBytes = SizeInKBytes(pgc->cTotalReservedBytes);
            pStats->Gen0HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[0]);
            pStats->Gen1HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[1]);
            pStats->Gen2HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[2]);
            pStats->LargeObjectHeapSizeKBytes = SizeInKBytes(pgc->cLrgObjSize);
            pStats->KBytesPromotedFromGen0 = SizeInKBytes(pgc->cbPromotedMem[0]);
            pStats->KBytesPromotedFromGen1 = SizeInKBytes(pgc->cbPromotedMem[1]);
        }
        hr = S_OK;
ErrExit:
    #else
        hr = E_NOTIMPL;
    #endif // ENABLE_PERF_COUNTERS

        END_ENTRYPOINT_NOTHROW;
        return hr;
    }
    virtual HRESULT STDMETHODCALLTYPE SetGCStartupLimits(
        DWORD SegmentSize,
        DWORD MaxGen0Size)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;

        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        // Set default overrides if specified by caller.
        if (SegmentSize != (DWORD) ~0 && SegmentSize > 0)
        {
            hr = _SetGCSegmentSize(SegmentSize);
        }

        if (SUCCEEDED(hr) && MaxGen0Size != (DWORD) ~0 && MaxGen0Size > 0)
        {
            hr = _SetGCMaxGen0Size(MaxGen0Size);
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE SetGCStartupLimitsEx(
        SIZE_T SegmentSize,
        SIZE_T MaxGen0Size)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;

        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        // Set default overrides if specified by caller.
        if (SegmentSize != (SIZE_T) ~0 && SegmentSize > 0)
        {
            hr = _SetGCSegmentSize(SegmentSize);
        }

        if (SUCCEEDED(hr) && MaxGen0Size != (SIZE_T) ~0 && MaxGen0Size > 0)
        {
            hr = _SetGCMaxGen0Size(MaxGen0Size);
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, OUT PVOID *ppUnk)
    {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRGCManager && riid != IID_ICLRGCManager2 && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppUnk = this;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
private:
    HRESULT _SetGCSegmentSize(SIZE_T SegmentSize);
    HRESULT _SetGCMaxGen0Size(SIZE_T MaxGen0Size);
};


HRESULT CCLRGCManager::_SetGCSegmentSize(SIZE_T SegmentSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Sanity check the value, it must be a power of two and big enough.
    if (!GCHeapUtilities::GetGCHeap()->IsValidSegmentSize(SegmentSize))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        Host_SegmentSize = SegmentSize;
        Host_fSegmentSizeSet = TRUE;
    }

    return (hr);
}

HRESULT CCLRGCManager::_SetGCMaxGen0Size(SIZE_T MaxGen0Size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Sanity check the value is at least large enough.
    if (!GCHeapUtilities::GetGCHeap()->IsValidGen0MaxSize(MaxGen0Size))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        Host_MaxGen0Size = MaxGen0Size;
        Host_fMaxGen0SizeSet = TRUE;
    }

    return (hr);
}

static CCLRGCManager s_GCManager;
#endif //FEATURE_WINDOWSPHONE

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
class CCLRAppDomainResourceMonitor : public ICLRAppDomainResourceMonitor
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetCurrentAllocated(DWORD dwAppDomainId, 
                                                          ULONGLONG* pBytesAllocated)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT     hr = S_OK;

        BEGIN_ENTRYPOINT_NOTHROW;

        SystemDomain::LockHolder lh;

        AppDomain* pAppDomain = SystemDomain::GetAppDomainFromId((ADID)dwAppDomainId, ADV_CURRENTAD);        
        if (pBytesAllocated)
        {
            *pBytesAllocated = pAppDomain->GetAllocBytes();
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE GetCurrentSurvived(DWORD dwAppDomainId, 
                                                         ULONGLONG* pAppDomainBytesSurvived, 
                                                         ULONGLONG* pTotalBytesSurvived)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT     hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        SystemDomain::LockHolder lh;

        AppDomain* pAppDomain = SystemDomain::GetAppDomainFromId((ADID)dwAppDomainId, ADV_CURRENTAD);
        if (pAppDomainBytesSurvived)
        {
            *pAppDomainBytesSurvived = pAppDomain->GetSurvivedBytes();
        }
        if (pTotalBytesSurvived)
        {
            *pTotalBytesSurvived = SystemDomain::GetTotalSurvivedBytes();
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE GetCurrentCpuTime(DWORD dwAppDomainId, 
                                                        ULONGLONG* pMilliseconds)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;

        BEGIN_ENTRYPOINT_NOTHROW;

        {
            SystemDomain::LockHolder lh;
    
            AppDomain* pAppDomain = SystemDomain::GetAppDomainFromId((ADID)dwAppDomainId, ADV_CURRENTAD);
            if (pMilliseconds)
            {
                *pMilliseconds = pAppDomain->QueryProcessorUsage() / 10000;
            }
        }

        END_ENTRYPOINT_NOTHROW;

        return hr;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, OUT PVOID *ppUnk)
    {
        LIMITED_METHOD_CONTRACT;
        *ppUnk = NULL;
        if (riid == IID_IUnknown)
            *ppUnk = (IUnknown*)this;
        else if (riid == IID_ICLRAppDomainResourceMonitor)
            *ppUnk = (ICLRAppDomainResourceMonitor*)this;
        else
            return E_NOINTERFACE;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
};
static CCLRAppDomainResourceMonitor s_Arm;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING


BOOL g_CLRPolicyRequested = FALSE;

class CCorCLRControl: public ICLRControl
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetCLRManager(REFIID riid, void **ppObject)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;    // no global state updates
        }
        CONTRACTL_END;

        // Sanity check.
        if (ppObject == NULL)
            return E_INVALIDARG;

#if defined(FEATURE_WINDOWSPHONE)
        if (riid == IID_ICLRErrorReportingManager2)
        {
            *ppObject = &g_CLRErrorReportingManager;
            return S_OK;
        }
        else
#endif //defined(FEATURE_WINDOWSPHONE)
        if (g_fEEStarted && !m_fFullAccess)
        {
            // If runtime has been started, do not allow user to obtain CLR managers.
            return HOST_E_INVALIDOPERATION;
        }

        // CoreCLR supports ICLRPolicyManager since it allows the host
        // to specify the policy for AccessViolation.  
        else if (riid == IID_ICLRPolicyManager) {
            *ppObject = &s_PolicyManager;
            FastInterlockExchange((LONG*)&g_CLRPolicyRequested, TRUE);
            return S_OK;
        }

#if defined(FEATURE_WINDOWSPHONE)
        else if ((riid == IID_ICLRGCManager) || (riid == IID_ICLRGCManager2))
        {
            *ppObject = &s_GCManager;
            return S_OK;
        }
#endif //FEATURE_WINDOWSPHONE

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
        else if (riid == IID_ICLRAppDomainResourceMonitor)
        {
            EnableARM();
            *ppObject = &s_Arm;
            return S_OK;
        }
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING
        else
            return (E_NOINTERFACE);
    }

    virtual HRESULT STDMETHODCALLTYPE SetAppDomainManagerType(
        LPCWSTR pwzAppDomainManagerAssembly,
        LPCWSTR pwzAppDomainManagerType)
    {
        
        // CoreCLR does not support this method
        return E_NOTIMPL;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    BEGIN_INTERFACE HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
                                                             void **ppvObject)
    {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRControl && riid != IID_IUnknown)
            return (E_NOINTERFACE);

        // Ensure that the out going pointer is not null
        if (ppvObject == NULL)
            return E_POINTER;

        *ppvObject = this;
        return S_OK;
    }

    // This is to avoid having ctor.  We have static objects, and it is
    // difficult to support ctor on certain platform.
    void SetAccess(BOOL fFullAccess)
    {
        LIMITED_METHOD_CONTRACT;
        m_fFullAccess = fFullAccess;
    }
private:
    BOOL m_fFullAccess;
};

// Before CLR starts, we give out s_CorCLRControl which has full access to all managers.
// After CLR starts, we give out s_CorCLRControlLimited which allows limited access to managers.
static CCorCLRControl s_CorCLRControl;


///////////////////////////////////////////////////////////////////////////////
// ICLRRuntimeHost::GetCLRControl
HRESULT CorHost2::GetCLRControl(ICLRControl** pCLRControl)
{
    LIMITED_METHOD_CONTRACT;
    
    // Ensure that we have a valid pointer
    if (pCLRControl == NULL)
    {
        return E_POINTER;
    }

    HRESULT hr = S_OK;
    
    STATIC_CONTRACT_ENTRY_POINT;
    BEGIN_ENTRYPOINT_NOTHROW;
    if (!g_fEEStarted && m_Version >= 2)
    {
        s_CorCLRControl.SetAccess(TRUE);
        *pCLRControl = &s_CorCLRControl;
    }
    else
    {
        // If :
        // 1) request comes for interface other than ICLRControl*, OR
        // 2) runtime has already started, OR
        // 3) version is not 2
        //
        // we will return failure and set the out pointer to NULL
        *pCLRControl = NULL;
        if (g_fEEStarted)
        {
            // Return HOST_E_INVALIDOPERATION as per MSDN if runtime has already started
            hr = HOST_E_INVALIDOPERATION;
        }
        else
        {
            hr = E_NOTIMPL;
        }
    }
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// static
EInitializeNewDomainFlags CorHost2::GetAppDomainManagerInitializeNewDomainFlags()
{
    LIMITED_METHOD_CONTRACT;
    return eInitializeNewDomainFlags_None;
}


#endif // !DAC


#ifdef DACCESS_COMPILE


#endif //DACCESS_COMPILE

#ifndef DACCESS_COMPILE


#if defined(FEATURE_WINDOWSPHONE)

HRESULT CCLRErrorReportingManager::QueryInterface(REFIID riid, void** ppUnk)
{
    if (!ppUnk)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    
    if (ppUnk == NULL)
    {
        return E_POINTER;
    }

    *ppUnk = 0;

    // Deliberately do NOT hand out ICorConfiguration.  They must explicitly call
    // GetConfiguration to obtain that interface.
    if (riid == IID_IUnknown)
    {
        *ppUnk = (IUnknown *) this;
    }
    else if (riid == IID_ICLRErrorReportingManager)
    {
        *ppUnk = (ICLRErrorReportingManager *) this;
    }
#ifdef FEATURE_WINDOWSPHONE
    else if (riid == IID_ICLRErrorReportingManager2)
    {
        *ppUnk = (ICLRErrorReportingManager2 *) this;
    }
#endif // FEATURE_WINDOWSPHONE
    else
    {
        hr = E_NOINTERFACE;
    }

    return hr;

} // HRESULT CCLRErrorReportingManager::QueryInterface()

ULONG CCLRErrorReportingManager::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
} // HRESULT CCLRErrorReportingManager::AddRef()

ULONG CCLRErrorReportingManager::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
} // HRESULT CCLRErrorReportingManager::Release()

// Get Watson bucket parameters for "current" exception (on calling thread).
HRESULT CCLRErrorReportingManager::GetBucketParametersForCurrentException(
    BucketParameters *pParams)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    // To avoid confusion, clear the buckets.
    memset(pParams, 0, sizeof(BucketParameters));

#ifndef FEATURE_PAL   
    // Defer to Watson helper.
    hr = ::GetBucketParametersForCurrentException(pParams);
 #else
    // Watson doesn't exist on non-windows platforms
    hr = E_NOTIMPL;
#endif // !FEATURE_PAL

    END_ENTRYPOINT_NOTHROW;

    return hr;

} // HRESULT CCLRErrorReportingManager::GetBucketParametersForCurrentException()

//
// The BeginCustomDump function configures the custom dump support
//
// Parameters -
// dwFlavor     - The flavor of the dump
// dwNumItems   - The number of items in the CustomDumpItem array.
//                  Should always be zero today, since no custom items are defined
// items        - Array of CustomDumpItem structs specifying items to be added to the dump.
//                  Should always be NULL today, since no custom items are defined.
// dwReserved   - reserved for future use. Must be zero today
//
HRESULT CCLRErrorReportingManager::BeginCustomDump( ECustomDumpFlavor dwFlavor,
                                        DWORD dwNumItems,
                                        CustomDumpItem items[],
                                        DWORD dwReserved)
{
    STATIC_CONTRACT_ENTRY_POINT;
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (dwNumItems != 0 ||  items != NULL || dwReserved != 0)
    {
        IfFailGo(E_INVALIDARG);
    }
    if (g_ECustomDumpFlavor != DUMP_FLAVOR_Default)
    {
        // BeginCustomDump is called without matching EndCustomDump
        IfFailGo(E_INVALIDARG);
    }
    g_ECustomDumpFlavor = dwFlavor;

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

//
// EndCustomDump clears the custom dump configuration
//
HRESULT CCLRErrorReportingManager::EndCustomDump()
{
    STATIC_CONTRACT_ENTRY_POINT;
    // NOT IMPLEMENTED YET
    BEGIN_ENTRYPOINT_NOTHROW;
    g_ECustomDumpFlavor = DUMP_FLAVOR_Default;
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

#ifdef FEATURE_WINDOWSPHONE
HRESULT CopyStringWorker(_Out_ WCHAR** pTarget, WCHAR const* pSource)
{
    LIMITED_METHOD_CONTRACT;

    if (pTarget == NULL || pSource == NULL)
        return E_INVALIDARG;

    if (*pTarget)
        delete[] (*pTarget);

    // allocate space for the data plus one wchar for NULL
    size_t sourceLen = wcslen(pSource);
    *pTarget = new (nothrow) WCHAR[sourceLen + 1];
    
    if (!(*pTarget))
        return E_OUTOFMEMORY;

    errno_t result = wcsncpy_s(*pTarget, sourceLen + 1, pSource, sourceLen);
    _ASSERTE(result == 0);
    
    if (result != 0)
    {
        delete[] (*pTarget);
        *pTarget = NULL;

        return E_FAIL;
    }

    return S_OK;
}

CCLRErrorReportingManager::BucketParamsCache::BucketParamsCache(DWORD maxNumParams) : m_pParams(NULL), m_cMaxParams(maxNumParams)
{
    LIMITED_METHOD_CONTRACT;
}

CCLRErrorReportingManager::BucketParamsCache::~BucketParamsCache()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pParams)
    {
        for (DWORD i = 0; i < m_cMaxParams; ++i)
            if (m_pParams[i]) delete[] m_pParams[i];
    }
}

WCHAR const* CCLRErrorReportingManager::BucketParamsCache::GetAt(BucketParameterIndex index)
{
    LIMITED_METHOD_CONTRACT;

    if (index >= InvalidBucketParamIndex)
    {
        _ASSERTE(!"bad bucket parameter index");
        return NULL;
    }

    if (!m_pParams)
        return NULL;

    return m_pParams[index];
}

HRESULT CCLRErrorReportingManager::BucketParamsCache::SetAt(BucketParameterIndex index, WCHAR const* val)
{
    LIMITED_METHOD_CONTRACT;

    if (index < 0 || index >= InvalidBucketParamIndex)
    {
        _ASSERTE(!"bad bucket parameter index");
        return E_INVALIDARG;
    }

    if (!val)
        return E_INVALIDARG;

    if (!m_pParams)
    {
        m_pParams = new (nothrow) WCHAR*[m_cMaxParams];
        if (!m_pParams)
            return E_OUTOFMEMORY;

        for (DWORD i = 0; i < m_cMaxParams; ++i)
            m_pParams[i] = NULL;
    }

    return CopyStringWorker(&m_pParams[index], val);
}

HRESULT CCLRErrorReportingManager::CopyToDataCache(_Out_ WCHAR** pTarget, WCHAR const* pSource)
{
    LIMITED_METHOD_CONTRACT;

    return CopyStringWorker(pTarget, pSource);
}

HRESULT CCLRErrorReportingManager::SetApplicationData(ApplicationDataKey key, WCHAR const* pValue)
{
    STATIC_CONTRACT_ENTRY_POINT;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(g_fEEStarted)
        return HOST_E_INVALIDOPERATION;

    if (pValue == NULL || wcslen(pValue) > MAX_LONGPATH)
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    switch (key)
    {
    case ApplicationID:
        hr = CopyToDataCache(&m_pApplicationId, pValue);
        break;

    case InstanceID:
        hr = CopyToDataCache(&m_pInstanceId, pValue);
        break;

    default:
        hr = E_INVALIDARG;
    }
    
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CCLRErrorReportingManager::SetBucketParametersForUnhandledException(BucketParameters const* pBucketParams, DWORD* pCountParams)
{
    STATIC_CONTRACT_ENTRY_POINT;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(g_fEEStarted)
        return HOST_E_INVALIDOPERATION;

    if (pBucketParams == NULL || pCountParams == NULL || pBucketParams->fInited != TRUE)
        return E_INVALIDARG;

    *pCountParams = 0;

    if (!m_pBucketParamsCache)
    {
        m_pBucketParamsCache = new (nothrow) BucketParamsCache(InvalidBucketParamIndex);
        if (!m_pBucketParamsCache)
            return E_OUTOFMEMORY;
    }

    HRESULT hr = S_OK;
    bool hasOverride = false;

    for (DWORD i = 0; i < InvalidBucketParamIndex; ++i)
    {
        if (pBucketParams->pszParams[i][0] != W('\0'))
        {
            hasOverride = true;
            hr = m_pBucketParamsCache->SetAt(static_cast<BucketParameterIndex>(i), pBucketParams->pszParams[i]);
            if (SUCCEEDED(hr))
                *pCountParams += 1;
            else
                break;
        }
    }
    
    if (!hasOverride)
        return E_INVALIDARG;
    
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

WCHAR const* CCLRErrorReportingManager::GetApplicationData(ApplicationDataKey key)
{
    LIMITED_METHOD_CONTRACT;

    WCHAR* pValue = NULL;

    switch (key)
    {
    case ApplicationID:
        pValue = m_pApplicationId;
        break;

    case InstanceID:
        pValue = m_pInstanceId;
        break;

    default:
        _ASSERTE(!"invalid key specified");
    }

    return pValue;
}

WCHAR const* CCLRErrorReportingManager::GetBucketParamOverride(BucketParameterIndex bucketParamId)
{
    LIMITED_METHOD_CONTRACT;

    if (!m_pBucketParamsCache)
        return NULL;

    return m_pBucketParamsCache->GetAt(bucketParamId);
}

#endif // FEATURE_WINDOWSPHONE

CCLRErrorReportingManager::CCLRErrorReportingManager()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_WINDOWSPHONE
    m_pApplicationId = NULL;
    m_pInstanceId = NULL;
    m_pBucketParamsCache = NULL;
#endif
}

CCLRErrorReportingManager::~CCLRErrorReportingManager()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_WINDOWSPHONE
    if (m_pApplicationId)
        delete[] m_pApplicationId;

    if (m_pInstanceId)
        delete[] m_pInstanceId;

    if (m_pBucketParamsCache)
        delete m_pBucketParamsCache;
#endif
}

#endif // defined(FEATURE_WINDOWSPHONE)

void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pMSEX->dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = GlobalMemoryStatusEx(pMSEX);
    _ASSERTE (fRet);
}

// This is the instance that exposes interfaces out to all the other DLLs of the CLR
// so they can use our services for TLS, synchronization, memory allocation, etc.
static BYTE g_CEEInstance[sizeof(CExecutionEngine)];
static Volatile<IExecutionEngine*> g_pCEE = NULL;

PTLS_CALLBACK_FUNCTION CExecutionEngine::Callbacks[MAX_PREDEFINED_TLS_SLOT];

extern "C" IExecutionEngine * __stdcall IEE()
{
    LIMITED_METHOD_CONTRACT;

    // Unfortunately,we can't probe here. The probing system requires the
    // use of TLS, and in order to initialize TLS we need to call IEE.

    //BEGIN_ENTRYPOINT_VOIDRET;


    // The following code does NOT contain a race condition.  The following code is BY DESIGN.
    // The issue is that we can have two separate threads inside this if statement, both of which are
    // initializing the g_CEEInstance variable (and subsequently updating g_pCEE).  This works fine,
    // and will not cause an inconsistent state due to the fact that CExecutionEngine has no
    // local variables.  If multiple threads make it inside this if statement, it will copy the same
    // bytes over g_CEEInstance and there will not be a time when there is an inconsistent state.
    if ( !g_pCEE )
    {
        // Create a local copy on the stack and then copy it over to the static instance.
        // This avoids race conditions caused by multiple initializations of vtable in the constructor
       CExecutionEngine local;
       memcpy(&g_CEEInstance, (void*)&local, sizeof(CExecutionEngine));

       g_pCEE = (IExecutionEngine*)(CExecutionEngine*)&g_CEEInstance;
    }
    //END_ENTRYPOINT_VOIDRET;

    return g_pCEE;
}


HRESULT STDMETHODCALLTYPE CExecutionEngine::QueryInterface(REFIID id, void **pInterface)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    if (!pInterface)
        return E_POINTER;

    *pInterface = NULL;

    //CANNOTTHROWCOMPLUSEXCEPTION();
    if (id == IID_IExecutionEngine)
        *pInterface = (IExecutionEngine *)this;
    else if (id == IID_IEEMemoryManager)
        *pInterface = (IEEMemoryManager *)this;
    else if (id == IID_IUnknown)
        *pInterface = (IUnknown *)(IExecutionEngine *)this;
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
} // HRESULT STDMETHODCALLTYPE CExecutionEngine::QueryInterface()


ULONG STDMETHODCALLTYPE CExecutionEngine::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

ULONG STDMETHODCALLTYPE CExecutionEngine::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

struct ClrTlsInfo
{
    void* data[MAX_PREDEFINED_TLS_SLOT];
};

#define DataToClrTlsInfo(a) ((ClrTlsInfo*)a)

void** CExecutionEngine::GetTlsData()
{
    LIMITED_METHOD_CONTRACT;

   return gCurrentThreadInfo.m_EETlsData;
}

BOOL CExecutionEngine::SetTlsData (void** ppTlsInfo)
{
    LIMITED_METHOD_CONTRACT;

    gCurrentThreadInfo.m_EETlsData = ppTlsInfo;
    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Returns the current logical thread's data block (ClrTlsInfo::data).
//
// Arguments:
//    slot - Index of the slot that is about to be requested
//    force - If the data block does not exist yet, create it as a side-effect
//
// Return Value:
//    NULL, if the data block did not exist yet for the current thread and force was FALSE.
//    A pointer to the data block, otherwise.
//
// Notes:
//    If the underlying OS does not support fiber mode, the data block is stored in TLS.
//    If the underlying OS does support fiber mode, it is primarily stored in FLS,
//    and cached in TLS so that we can use our generated optimized TLS accessors.
//
// TLS support for the other DLLs of the CLR operates quite differently in hosted
// and unhosted scenarios.

void **CExecutionEngine::CheckThreadState(DWORD slot, BOOL force)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    //<TODO> @TODO: Decide on an exception strategy for all the DLLs of the CLR, and then
    // enable all the exceptions out of this method.</TODO>

    // Treat as a runtime assertion, since the invariant spans many DLLs.
    _ASSERTE(slot < MAX_PREDEFINED_TLS_SLOT);
//    if (slot >= MAX_PREDEFINED_TLS_SLOT)
//        COMPlusThrow(kArgumentOutOfRangeException);

    void** pTlsData = CExecutionEngine::GetTlsData();
    BOOL fInTls = (pTlsData != NULL);

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);
    if (pTlsInfo == 0 && force)
    {
#undef HeapAlloc
#undef GetProcessHeap
        // !!! Contract uses our TLS support.  Contract may be used before our host support is set up.
        // !!! To better support contract, we call into OS for memory allocation.
        pTlsInfo = (ClrTlsInfo*) ::HeapAlloc(GetProcessHeap(),0,sizeof(ClrTlsInfo));
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#define HeapAlloc(hHeap, dwFlags, dwBytes) Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)
        if (pTlsInfo == NULL)
        {
            goto LError;
        }
        memset (pTlsInfo, 0, sizeof(ClrTlsInfo));
        // We save the last intolerant marker on stack in this slot.  
        // -1 is the larget unsigned number, and therefore our marker is always smaller than it.
        pTlsInfo->data[TlsIdx_SOIntolerantTransitionHandler] = (void*)(-1);
    }

    if (!fInTls && pTlsInfo)
    {
        if (!CExecutionEngine::SetTlsData(pTlsInfo->data))
        {
            goto LError;
        }
    }

    return pTlsInfo?pTlsInfo->data:NULL;

LError:
    if (pTlsInfo)
    {
#undef HeapFree
#undef GetProcessHeap
        ::HeapFree(GetProcessHeap(), 0, pTlsInfo);
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
    }
    // If this is for the stack probe, and we failed to allocate memory for it, we won't
    // put in a guard page.
    if (slot == TlsIdx_ClrDebugState || slot == TlsIdx_StackProbe)
        return NULL;

    ThrowOutOfMemory();
}


void **CExecutionEngine::CheckThreadStateNoCreate(DWORD slot
#ifdef _DEBUG
                                                  , BOOL fForDestruction
#endif // _DEBUG
                                                  )
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;

    // !!! This function is called during Thread::SwitchIn and SwitchOut
    // !!! It is extremely important that while executing this function, we will not
    // !!! cause fiber switch.  This means we can not allocate memory, lock, etc...


    // Treat as a runtime assertion, since the invariant spans many DLLs.
    _ASSERTE(slot < MAX_PREDEFINED_TLS_SLOT);

    void **pTlsData = CExecutionEngine::GetTlsData();

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);

    return pTlsInfo?pTlsInfo->data:NULL;
}

// Note: Sampling profilers also use this function to initialize TLS for a unmanaged 
// sampling thread so that initialization can be done in advance to avoid deadlocks. 
// See ProfToEEInterfaceImpl::InitializeCurrentThread for more details.
void CExecutionEngine::SetupTLSForThread(Thread *pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_ANY;

#ifdef STRESS_LOG
    if (StressLog::StressLogOn(~0u, 0))
    {
        StressLog::CreateThreadStressLog();
    }
#endif
    void **pTlsData;
    pTlsData = CheckThreadState(0);

    PREFIX_ASSUME(pTlsData != NULL);

#ifdef ENABLE_CONTRACTS
    // Profilers need the side effect of GetClrDebugState() to perform initialization
    // in advance to avoid deadlocks. Refer to ProfToEEInterfaceImpl::InitializeCurrentThread
    ClrDebugState *pDebugState = ::GetClrDebugState();

    if (pThread)
        pThread->m_pClrDebugState = pDebugState; 
#endif
}

static void ThreadDetachingHelper(PTLS_CALLBACK_FUNCTION callback, void* pData)
{
    // Do not use contract.  We are freeing TLS blocks.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    callback(pData);
}

// Called here from a thread detach or from destruction of a Thread object.  In
// the detach case, we get our info from TLS.  In the destruct case, it comes from
// the object we are destructing.
void CExecutionEngine::ThreadDetaching(void ** pTlsData)
{
    // Can not cause memory allocation during thread detach, so no real contracts.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    // This function may be called twice:
    // 1. When a physical thread dies, our DLL_THREAD_DETACH calls this function with pTlsData = NULL
    // 2. When a fiber is destroyed, or OS calls FlsCallback after DLL_THREAD_DETACH process.
    // We will null the FLS and TLS entry if it matches the deleted one.

    if (pTlsData)
    {
        DeleteTLS (pTlsData);
    }
}

void CExecutionEngine::DeleteTLS(void ** pTlsData)
{
    // Can not cause memory allocation during thread detach, so no real contracts.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    if (CExecutionEngine::GetTlsData() == NULL)
    {
        // We have not allocated TlsData yet.
        return;
    }

    PREFIX_ASSUME(pTlsData != NULL);

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);
    BOOL fNeed;
    do
    {
        fNeed = FALSE;
        for (int i=0; i<MAX_PREDEFINED_TLS_SLOT; i++)
        {
            if (i == TlsIdx_ClrDebugState ||
                i == TlsIdx_StressLog)
            {
                // StressLog and DebugState may be needed during callback.
                continue;
            }
            // If we have some data and a callback, issue it.
            if (Callbacks[i] != 0 && pTlsInfo->data[i] != 0)
            {
                void* pData = pTlsInfo->data[i];
                pTlsInfo->data[i] = 0;
                ThreadDetachingHelper(Callbacks[i], pData);
                fNeed = TRUE;
            }
        }
    } while (fNeed);

    if (pTlsInfo->data[TlsIdx_StressLog] != 0)
    {
#ifdef STRESS_LOG
        StressLog::ThreadDetach((ThreadStressLog *)pTlsInfo->data[TlsIdx_StressLog]);
#else
        _ASSERTE (!"should not have StressLog");
#endif
    }

    if (Callbacks[TlsIdx_ClrDebugState] != 0 && pTlsInfo->data[TlsIdx_ClrDebugState] != 0)
    {
        void* pData = pTlsInfo->data[TlsIdx_ClrDebugState];
        pTlsInfo->data[TlsIdx_ClrDebugState] = 0;
        ThreadDetachingHelper(Callbacks[TlsIdx_ClrDebugState], pData);
    }

    // NULL TLS and FLS entry so that we don't double free.
    // We may get two callback here on thread death
    // 1. From EEDllMain
    // 2. From OS callback on FLS destruction
    if (CExecutionEngine::GetTlsData() == pTlsData)
    {
        CExecutionEngine::SetTlsData(0);
    }

#undef HeapFree
#undef GetProcessHeap
    ::HeapFree (GetProcessHeap(),0,pTlsInfo);
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
#define GetProcessHeap() Dont_Use_GetProcessHeap()

}

#ifdef ENABLE_CONTRACTS_IMPL
// Fls callback to deallocate ClrDebugState when our FLS block goes away.
void FreeClrDebugState(LPVOID pTlsData);
#endif

VOID STDMETHODCALLTYPE CExecutionEngine::TLS_AssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    CheckThreadState(slot);

    // They can toggle between a callback and no callback.  But anything else looks like
    // confusion on their part.
    //
    // (TlsIdx_ClrDebugState associates its callback from utilcode.lib - which can be replicated. But
    // all the callbacks are equally good.)
    _ASSERTE(slot == TlsIdx_ClrDebugState || Callbacks[slot] == 0 || Callbacks[slot] == callback || callback == 0);
    if (slot == TlsIdx_ClrDebugState)
    {
#ifdef ENABLE_CONTRACTS_IMPL
        // ClrDebugState is shared among many dlls.  Some dll, like perfcounter.dll, may be unloaded.
        // We force the callback function to be in mscorwks.dll.
        Callbacks[slot] = FreeClrDebugState;
#else
        _ASSERTE (!"should not get here");
#endif
    }
    else
        Callbacks[slot] = callback;
}

LPVOID* STDMETHODCALLTYPE CExecutionEngine::TLS_GetDataBlock()
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    return CExecutionEngine::GetTlsData();
}

LPVOID STDMETHODCALLTYPE CExecutionEngine::TLS_GetValue(DWORD slot)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EETlsGetValue(slot);
}

BOOL STDMETHODCALLTYPE CExecutionEngine::TLS_CheckValue(DWORD slot, LPVOID * pValue)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EETlsCheckValue(slot, pValue);
}

VOID STDMETHODCALLTYPE CExecutionEngine::TLS_SetValue(DWORD slot, LPVOID pData)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    EETlsSetValue(slot,pData);
}


VOID STDMETHODCALLTYPE CExecutionEngine::TLS_ThreadDetaching()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    CExecutionEngine::ThreadDetaching(NULL);
}


CRITSEC_COOKIE STDMETHODCALLTYPE CExecutionEngine::CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    CRITSEC_COOKIE cookie = NULL;
    BEGIN_ENTRYPOINT_VOIDRET;
    cookie = ::EECreateCriticalSection(*(CrstType*)&level, flags);
    END_ENTRYPOINT_VOIDRET;
    return cookie;
}

void STDMETHODCALLTYPE CExecutionEngine::DestroyLock(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    ::EEDeleteCriticalSection(cookie);
}

void STDMETHODCALLTYPE CExecutionEngine::AcquireLock(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    BEGIN_SO_INTOLERANT_CODE(GetThread());
    ::EEEnterCriticalSection(cookie);
    END_SO_INTOLERANT_CODE;
}

void STDMETHODCALLTYPE CExecutionEngine::ReleaseLock(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    BEGIN_SO_INTOLERANT_CODE(GetThread());
    ::EELeaveCriticalSection(cookie);
    END_SO_INTOLERANT_CODE;
}

// Locking routines supplied by the EE to the other DLLs of the CLR.  In a _DEBUG
// build of the EE, we poison the Crst as a poor man's attempt to do some argument
// validation.
#define POISON_BITS 3

static inline EVENT_COOKIE CLREventToCookie(CLREvent * pEvent)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) pEvent) & POISON_BITS) == 0);
#ifdef _DEBUG
    pEvent = (CLREvent *) (((uintptr_t) pEvent) | POISON_BITS);
#endif
    return (EVENT_COOKIE) pEvent;
}

static inline CLREvent *CookieToCLREvent(EVENT_COOKIE cookie)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    if (cookie)
    {
    cookie = (EVENT_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
    }
#endif
    return (CLREvent *) cookie;
}


EVENT_COOKIE STDMETHODCALLTYPE CExecutionEngine::CreateAutoEvent(BOOL bInitialState)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    EVENT_COOKIE event = NULL;
    BEGIN_ENTRYPOINT_THROWS;
    NewHolder<CLREvent> pEvent(new CLREvent());
    pEvent->CreateAutoEvent(bInitialState);
    event = CLREventToCookie(pEvent);
    pEvent.SuppressRelease();
    END_ENTRYPOINT_THROWS;

    return event;
}

EVENT_COOKIE STDMETHODCALLTYPE CExecutionEngine::CreateManualEvent(BOOL bInitialState)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    EVENT_COOKIE event = NULL;
    BEGIN_ENTRYPOINT_THROWS;

    NewHolder<CLREvent> pEvent(new CLREvent());
    pEvent->CreateManualEvent(bInitialState);
    event = CLREventToCookie(pEvent);
    pEvent.SuppressRelease();

    END_ENTRYPOINT_THROWS;

    return event;
}

void STDMETHODCALLTYPE CExecutionEngine::CloseEvent(EVENT_COOKIE event)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        pEvent->CloseEvent();
        delete pEvent;
    }
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrSetEvent(EVENT_COOKIE event)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        return pEvent->Set();
    }
    return FALSE;
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrResetEvent(EVENT_COOKIE event)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        return pEvent->Reset();
    }
    return FALSE;
}

DWORD STDMETHODCALLTYPE CExecutionEngine::WaitForEvent(EVENT_COOKIE event,
                                                       DWORD dwMilliseconds,
                                                       BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        return pEvent->Wait(dwMilliseconds,bAlertable);
    }

    if (GetThread() && bAlertable)
        ThrowHR(E_INVALIDARG);
    return WAIT_FAILED;
}

DWORD STDMETHODCALLTYPE CExecutionEngine::WaitForSingleObject(HANDLE handle,
                                                              DWORD dwMilliseconds)
{
    STATIC_CONTRACT_WRAPPER;
    STATIC_CONTRACT_SO_TOLERANT;
    return ::WaitForSingleObject(handle,dwMilliseconds);
}

static inline SEMAPHORE_COOKIE CLRSemaphoreToCookie(CLRSemaphore * pSemaphore)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    _ASSERTE((((uintptr_t) pSemaphore) & POISON_BITS) == 0);
#ifdef _DEBUG
    pSemaphore = (CLRSemaphore *) (((uintptr_t) pSemaphore) | POISON_BITS);
#endif
    return (SEMAPHORE_COOKIE) pSemaphore;
}

static inline CLRSemaphore *CookieToCLRSemaphore(SEMAPHORE_COOKIE cookie)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    if (cookie)
    {
    cookie = (SEMAPHORE_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
    }
#endif
    return (CLRSemaphore *) cookie;
}


SEMAPHORE_COOKIE STDMETHODCALLTYPE CExecutionEngine::ClrCreateSemaphore(DWORD dwInitial,
                                                                        DWORD dwMax)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    NewHolder<CLRSemaphore> pSemaphore(new CLRSemaphore());
    pSemaphore->Create(dwInitial, dwMax);
    SEMAPHORE_COOKIE ret = CLRSemaphoreToCookie(pSemaphore);;
    pSemaphore.SuppressRelease();
    return ret;
}

void STDMETHODCALLTYPE CExecutionEngine::ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRSemaphore *pSemaphore = CookieToCLRSemaphore(semaphore);
    pSemaphore->Close();
    delete pSemaphore;
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore,
                                                             LONG lReleaseCount,
                                                             LONG *lpPreviousCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRSemaphore *pSemaphore = CookieToCLRSemaphore(semaphore);
    return pSemaphore->Release(lReleaseCount,lpPreviousCount);
}

DWORD STDMETHODCALLTYPE CExecutionEngine::ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore,
                                                              DWORD dwMilliseconds,
                                                              BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    CLRSemaphore *pSemaphore = CookieToCLRSemaphore(semaphore);
    return pSemaphore->Wait(dwMilliseconds,bAlertable);
}

static inline MUTEX_COOKIE CLRMutexToCookie(CLRMutex * pMutex)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) pMutex) & POISON_BITS) == 0);
#ifdef _DEBUG
    pMutex = (CLRMutex *) (((uintptr_t) pMutex) | POISON_BITS);
#endif
    return (MUTEX_COOKIE) pMutex;
}

static inline CLRMutex *CookieToCLRMutex(MUTEX_COOKIE cookie)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    if (cookie)
    {
    cookie = (MUTEX_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
    }
#endif
    return (CLRMutex *) cookie;
}


MUTEX_COOKIE STDMETHODCALLTYPE CExecutionEngine::ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                                BOOL bInitialOwner,
                                                                LPCTSTR lpName)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
        SO_TOLERANT;    // we catch any erros and free the allocated memory
    }
    CONTRACTL_END;


    MUTEX_COOKIE mutex = 0;
    CLRMutex *pMutex = new (nothrow) CLRMutex();
    if (pMutex)
    {
        EX_TRY
        {
            pMutex->Create(lpMutexAttributes, bInitialOwner, lpName);
            mutex = CLRMutexToCookie(pMutex);
        }
        EX_CATCH
        {
            delete pMutex;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    return mutex;
}

void STDMETHODCALLTYPE CExecutionEngine::ClrCloseMutex(MUTEX_COOKIE mutex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRMutex *pMutex = CookieToCLRMutex(mutex);
    pMutex->Close();
    delete pMutex;
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRMutex *pMutex = CookieToCLRMutex(mutex);
    return pMutex->Release();
}

DWORD STDMETHODCALLTYPE CExecutionEngine::ClrWaitForMutex(MUTEX_COOKIE mutex,
                                                          DWORD dwMilliseconds,
                                                          BOOL bAlertable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRMutex *pMutex = CookieToCLRMutex(mutex);
    return pMutex->Wait(dwMilliseconds,bAlertable);
}

#undef ClrSleepEx
DWORD STDMETHODCALLTYPE CExecutionEngine::ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    return EESleepEx(dwMilliseconds,bAlertable);
}
#define ClrSleepEx EESleepEx

#undef ClrAllocationDisallowed
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrAllocationDisallowed()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEAllocationDisallowed();
}
#define ClrAllocationDisallowed EEAllocationDisallowed

#undef ClrVirtualAlloc
LPVOID STDMETHODCALLTYPE CExecutionEngine::ClrVirtualAlloc(LPVOID lpAddress,
                                                           SIZE_T dwSize,
                                                           DWORD flAllocationType,
                                                           DWORD flProtect)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}
#define ClrVirtualAlloc EEVirtualAlloc

#undef ClrVirtualFree
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrVirtualFree(LPVOID lpAddress,
                                                        SIZE_T dwSize,
                                                        DWORD dwFreeType)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEVirtualFree(lpAddress, dwSize, dwFreeType);
}
#define ClrVirtualFree EEVirtualFree

#undef ClrVirtualQuery
SIZE_T STDMETHODCALLTYPE CExecutionEngine::ClrVirtualQuery(LPCVOID lpAddress,
                                                           PMEMORY_BASIC_INFORMATION lpBuffer,
                                                           SIZE_T dwLength)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEVirtualQuery(lpAddress, lpBuffer, dwLength);
}
#define ClrVirtualQuery EEVirtualQuery

#if defined(_DEBUG) && !defined(FEATURE_PAL)
static VolatilePtr<BYTE> s_pStartOfUEFSection = NULL;
static VolatilePtr<BYTE> s_pEndOfUEFSectionBoundary = NULL;
static Volatile<DWORD> s_dwProtection = 0;
#endif // _DEBUG && !FEATURE_PAL

#undef ClrVirtualProtect

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrVirtualProtect(LPVOID lpAddress,
                                                           SIZE_T dwSize,
                                                           DWORD flNewProtect,
                                                           PDWORD lpflOldProtect)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

   // Get the UEF installation details - we will use these to validate
   // that the calls to ClrVirtualProtect are not going to affect the UEF.
   // 
   // The OS UEF invocation mechanism was updated. When a UEF is setup,the OS captures 
   // the following details about it:
   //  1) Protection of the pages in which the UEF lives
   //  2) The size of the region in which the UEF lives
   //  3) The region's Allocation Base
   //
   //  The OS verifies details surrounding the UEF before invocation.  For security reasons 
   //  the page protection cannot change between SetUnhandledExceptionFilter and invocation.
   //
   // Prior to this change, the UEF lived in a common section of code_Seg, along with
   // JIT_PatchedCode. Thus, their pages have the same protection, they live
   //  in the same region (and thus, its size is the same).
   //
   // In EEStartupHelper, when we setup the UEF and then invoke InitJitHelpers1 and InitJitHelpers2, 
   // they perform some optimizations that result in the memory page protection being changed. When
   // the UEF is to be invoked, the OS does the check on the UEF's cached details against the current
   // memory pages. This check used to fail when on 64bit retail builds when JIT_PatchedCode was
   // aligned after the UEF with a different memory page protection (post the optimizations by InitJitHelpers). 
   // Thus, the UEF was never invoked.
   //
   // To circumvent this, we put the UEF in its own section in the code segment so that any modifications
   // to memory pages will not affect the UEF details that the OS cached. This is done in Excep.cpp
   // using the "#pragma code_seg" directives.
   //
   // Below, we double check that:
   // 
   // 1) the address being protected does not lie in the region of of the UEF. 
   // 2) the section after UEF is not having the same memory protection as UEF section.
   //
   // We assert if either of the two conditions above are true.

#if defined(_DEBUG) && !defined(FEATURE_PAL)
   // We do this check in debug/checked builds only

    // Do we have the UEF details?
    if (s_pEndOfUEFSectionBoundary.Load() == NULL)
    {
        // Get reference to MSCORWKS image in memory...
        PEDecoder pe(g_pMSCorEE);

        // Find the UEF section from the image
        IMAGE_SECTION_HEADER* pUEFSection = pe.FindSection(CLR_UEF_SECTION_NAME);
        _ASSERTE(pUEFSection != NULL);
        if (pUEFSection)
        {
            // We got our section - get the start of the section
            BYTE* pStartOfUEFSection = static_cast<BYTE*>(pe.GetBase())+pUEFSection->VirtualAddress;
            s_pStartOfUEFSection = pStartOfUEFSection;

            // Now we need the protection attributes for the memory region in which the
            // UEF section is...
            MEMORY_BASIC_INFORMATION uefInfo;
            if (ClrVirtualQuery(pStartOfUEFSection, &uefInfo, sizeof(uefInfo)) != 0)
            {
                // Calculate how many pages does the UEF section take to get to the start of the
                // next section. We dont calculate this as
                //
                // pStartOfUEFSection + uefInfo.RegionSize
                //
                // because the section following UEF will also be included in the region size
                // if it has the same protection as the UEF section.
                DWORD dwUEFSectionPageCount = ((pUEFSection->Misc.VirtualSize + GetOsPageSize() - 1)/GetOsPageSize());

                BYTE* pAddressOfFollowingSection = pStartOfUEFSection + (GetOsPageSize() * dwUEFSectionPageCount);
                
                // Ensure that the section following us is having different memory protection
                MEMORY_BASIC_INFORMATION nextSectionInfo;
                _ASSERTE(ClrVirtualQuery(pAddressOfFollowingSection, &nextSectionInfo, sizeof(nextSectionInfo)) != 0);
                _ASSERTE(nextSectionInfo.Protect != uefInfo.Protect);
                
                // save the memory protection details
                s_dwProtection = uefInfo.Protect;

                // Get the end of the UEF section
                BYTE* pEndOfUEFSectionBoundary = pAddressOfFollowingSection - 1;
                
                // Set the end of UEF section boundary
                FastInterlockExchangePointer(s_pEndOfUEFSectionBoundary.GetPointer(), pEndOfUEFSectionBoundary);
            }
            else
            {
                _ASSERTE(!"Unable to get UEF Details!");
            }
        }
    }

    if (s_pEndOfUEFSectionBoundary.Load() != NULL)
    {
        // Is the protection being changed?
        if (flNewProtect != s_dwProtection)
        {
            // Is the target address NOT affecting the UEF ? Possible cases:
            // 1) Starts and ends before the UEF start
            // 2) Starts after the UEF start

            void* pEndOfRangeAddr = static_cast<BYTE*>(lpAddress)+dwSize-1;

            _ASSERTE_MSG(((pEndOfRangeAddr < s_pStartOfUEFSection.Load()) || (lpAddress > s_pEndOfUEFSectionBoundary.Load())), 
                "Do not virtual protect the section in which UEF lives!");
        }
    }
#endif // _DEBUG && !FEATURE_PAL

    return EEVirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}
#define ClrVirtualProtect EEVirtualProtect

#undef ClrGetProcessHeap
HANDLE STDMETHODCALLTYPE CExecutionEngine::ClrGetProcessHeap()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEGetProcessHeap();
}
#define ClrGetProcessHeap EEGetProcessHeap

#undef ClrGetProcessExecutableHeap
HANDLE STDMETHODCALLTYPE CExecutionEngine::ClrGetProcessExecutableHeap()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEGetProcessExecutableHeap();
}
#define ClrGetProcessExecutableHeap EEGetProcessExecutableHeap


#undef ClrHeapCreate
HANDLE STDMETHODCALLTYPE CExecutionEngine::ClrHeapCreate(DWORD flOptions,
                                                         SIZE_T dwInitialSize,
                                                         SIZE_T dwMaximumSize)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapCreate(flOptions, dwInitialSize, dwMaximumSize);
}
#define ClrHeapCreate EEHeapCreate

#undef ClrHeapDestroy
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrHeapDestroy(HANDLE hHeap)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapDestroy(hHeap);
}
#define ClrHeapDestroy EEHeapDestroy

#undef ClrHeapAlloc
LPVOID STDMETHODCALLTYPE CExecutionEngine::ClrHeapAlloc(HANDLE hHeap,
                                                        DWORD dwFlags,
                                                        SIZE_T dwBytes)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    // We need to guarentee a very small stack consumption in allocating.  And we can't allow
    // an SO to happen while calling into the host.  This will force a hard SO which is OK because
    // we shouldn't ever get this close inside the EE in SO-intolerant code, so this should
    // only fail if we call directly in from outside the EE, such as the JIT.
    MINIMAL_STACK_PROBE_CHECK_THREAD(GetThread());

    return EEHeapAlloc(hHeap, dwFlags, dwBytes);
}
#define ClrHeapAlloc EEHeapAlloc

#undef ClrHeapFree
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrHeapFree(HANDLE hHeap,
                                                     DWORD dwFlags,
                                                     LPVOID lpMem)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapFree(hHeap, dwFlags, lpMem);
}
#define ClrHeapFree EEHeapFree

#undef ClrHeapValidate
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrHeapValidate(HANDLE hHeap,
                                                         DWORD dwFlags,
                                                         LPCVOID lpMem)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapValidate(hHeap, dwFlags, lpMem);
}
#define ClrHeapValidate EEHeapValidate

//------------------------------------------------------------------------------
// Helper function to get an exception object from outside the exception.  In
//  the CLR, it may be from the Thread object.  Non-CLR users have no thread object,
//  and it will do nothing.

void CExecutionEngine::GetLastThrownObjectExceptionFromThread(void **ppvException)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    // Cast to our real type.
    Exception **ppException = reinterpret_cast<Exception**>(ppvException);

    // Try to get a better message.
    GetLastThrownObjectExceptionFromThread_Internal(ppException);

} // HRESULT CExecutionEngine::GetLastThrownObjectExceptionFromThread()


LocaleID RuntimeGetFileSystemLocale()
{
    return PEImage::GetFileSystemLocale();
};

HRESULT CorHost2::DllGetActivationFactory(DWORD appDomainID, LPCWSTR wszTypeName, IActivationFactory ** factory)
{
#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
    // WinRT activation currently supported in default domain only
    if (appDomainID != DefaultADID)
        return HOST_E_INVALIDOPERATION;

    HRESULT hr = S_OK;

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            return hr;
        }
    }

    if(SystemDomain::GetCurrentDomain()->GetId().m_dwId != DefaultADID)
    {
        return HOST_E_INVALIDOPERATION;
    }

    return DllGetActivationFactoryImpl(NULL, wszTypeName, NULL, factory);
#else
    return E_NOTIMPL;
#endif
}


#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION

HRESULT STDMETHODCALLTYPE DllGetActivationFactoryImpl(LPCWSTR wszAssemblyName, 
                                                      LPCWSTR wszTypeName, 
                                                      LPCWSTR wszCodeBase,
                                                      IActivationFactory ** factory)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    AppDomain* pDomain = SystemDomain::System()->DefaultDomain();
    _ASSERTE(pDomain);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP();

        bool bIsPrimitive;
        TypeHandle typeHandle = WinRTTypeNameConverter::GetManagedTypeFromWinRTTypeName(wszTypeName, &bIsPrimitive);
        if (!bIsPrimitive && !typeHandle.IsNull() && !typeHandle.IsTypeDesc() && typeHandle.AsMethodTable()->IsExportedToWinRT())
        {
            struct _gc {
                OBJECTREF type;
            } gc;
            memset(&gc, 0, sizeof(gc));


            IActivationFactory* activationFactory;
            GCPROTECT_BEGIN(gc);

            gc.type = typeHandle.GetManagedClassObject();

            MethodDescCallSite mdcs(METHOD__WINDOWSRUNTIMEMARSHAL__GET_ACTIVATION_FACTORY_FOR_TYPE);
            ARG_SLOT args[1] = {
                ObjToArgSlot(gc.type)
            };
            activationFactory = (IActivationFactory*)mdcs.Call_RetLPVOID(args);

            *factory = activationFactory;

            GCPROTECT_END();
        }
        else
        {
            hr = COR_E_TYPELOAD;
        }
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // !FEATURE_COMINTEROP_MANAGED_ACTIVATION




#endif // !DACCESS_COMPILE
