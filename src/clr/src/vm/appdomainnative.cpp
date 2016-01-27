// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "common.h"
#include "appdomain.hpp"
#include "appdomainnative.hpp"
#ifdef FEATURE_REMOTING
#include "remoting.h"
#include "appdomainhelper.h"
#endif
#include "security.h"
#include "vars.hpp"
#include "eeconfig.h"
#include "appdomain.inl"
#include "eventtrace.h"
#ifndef FEATURE_CORECLR
#include "comutilnative.h"
#endif // !FEATURE_CORECLR
#if defined(FEATURE_APPX)
#include "appxutil.h"
#endif // FEATURE_APPX
#if defined(FEATURE_APPX_BINDER) && defined(FEATURE_HOSTED_BINDER)
#include "clrprivbinderappx.h"
#include "clrprivtypecachewinrt.h"
#endif // FEATURE_APPX_BINDER && FEATURE_HOSTED_BINDER
#ifdef FEATURE_VERSIONING
#include "../binder/inc/clrprivbindercoreclr.h"
#endif

#include "clr/fs/path.h"
using namespace clr::fs;

//************************************************************************
inline AppDomain *AppDomainNative::ValidateArg(APPDOMAINREF pThis)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
        THROWS;
    }
    CONTRACTL_END;

    if (pThis == NULL)
    {
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));
    }

    // Should not get here with a Transparent proxy for the this pointer -
    // should have always called through onto the real object
#ifdef FEATURE_REMOTING
    _ASSERTE(! CRemotingServices::IsTransparentProxy(OBJECTREFToObject(pThis)));
#endif

    AppDomain* pDomain = (AppDomain*)pThis->GetDomain();

    if(!pDomain)
    {
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));
    }

    // can only be accessed from within current domain
    _ASSERTE(GetAppDomain() == pDomain);

    // should not get here with an invalid appdomain. Once unload it, we won't let anyone else
    // in and any threads that are already in will be unwound.
    _ASSERTE(SystemDomain::GetAppDomainAtIndex(pDomain->GetIndex()) != NULL);
    return pDomain;
}


#ifdef FEATURE_REMOTING
//************************************************************************
FCIMPL5(Object*, AppDomainNative::CreateDomain, StringObject* strFriendlyNameUNSAFE, Object* appdomainSetupUNSAFE, Object* providedEvidenceUNSAFE, Object* creatorsEvidenceUNSAFE, void* parentSecurityDescriptor)
{
    FCALL_CONTRACT;

    struct _gc
    {
        OBJECTREF       retVal;
        STRINGREF       strFriendlyName;
        OBJECTREF       appdomainSetup;
        OBJECTREF       providedEvidence;
        OBJECTREF       creatorsEvidence;
        OBJECTREF       entryPointProxy;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    gc.strFriendlyName=(STRINGREF)strFriendlyNameUNSAFE;
    gc.appdomainSetup=(OBJECTREF)appdomainSetupUNSAFE;
    gc.providedEvidence=(OBJECTREF)providedEvidenceUNSAFE;
    gc.creatorsEvidence=(OBJECTREF)creatorsEvidenceUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    CreateDomainHelper(&gc.strFriendlyName, &gc.appdomainSetup, &gc.providedEvidence, &gc.creatorsEvidence, parentSecurityDescriptor, &gc.entryPointProxy, &gc.retVal);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.retVal);
}
FCIMPLEND

FCIMPL5(Object*, AppDomainNative::CreateInstance, StringObject* strFriendlyNameUNSAFE, Object* appdomainSetupUNSAFE, Object* providedEvidenceUNSAFE, Object* creatorsEvidenceUNSAFE, void* parentSecurityDescriptor)
{
    FCALL_CONTRACT;

    struct _gc
    {
        OBJECTREF       retVal;
        STRINGREF       strFriendlyName;
        OBJECTREF       appdomainSetup;
        OBJECTREF       providedEvidence;
        OBJECTREF       creatorsEvidence;
        OBJECTREF       entryPointProxy;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    gc.strFriendlyName=(STRINGREF)strFriendlyNameUNSAFE;
    gc.appdomainSetup=(OBJECTREF)appdomainSetupUNSAFE;
    gc.providedEvidence=(OBJECTREF)providedEvidenceUNSAFE;
    gc.creatorsEvidence=(OBJECTREF)creatorsEvidenceUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    CreateDomainHelper(&gc.strFriendlyName, &gc.appdomainSetup, &gc.providedEvidence, &gc.creatorsEvidence, parentSecurityDescriptor, &gc.entryPointProxy, &gc.retVal);

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.entryPointProxy);
}
FCIMPLEND

void AppDomainNative::CreateDomainHelper (STRINGREF* ppFriendlyName, OBJECTREF* ppAppdomainSetup, OBJECTREF* ppProvidedEvidence, OBJECTREF* ppCreatorsEvidence, void* parentSecurityDescriptor, OBJECTREF* pEntryPointProxy, OBJECTREF* pRetVal)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(IsProtectedByGCFrame(ppFriendlyName));
        PRECONDITION(IsProtectedByGCFrame(ppAppdomainSetup));
        PRECONDITION(IsProtectedByGCFrame(ppProvidedEvidence));
        PRECONDITION(IsProtectedByGCFrame(ppCreatorsEvidence));
        PRECONDITION(IsProtectedByGCFrame(pEntryPointProxy));
        PRECONDITION(IsProtectedByGCFrame(pRetVal));
    }
    CONTRACTL_END;


    AppDomainCreationHolder<AppDomain> pDomain;

    // This helper will send the AppDomain creation notifications for profiler / debugger.
    // If it throws, its backout code will also send a notification.
    // If it succeeds, then we still need to send a AppDomainCreateFinished notification.
    AppDomain::CreateUnmanagedObject(pDomain);

#ifdef PROFILING_SUPPORTED
    EX_TRY
#endif    
    {
        OBJECTREF setupInfo=NULL;
        GCPROTECT_BEGIN(setupInfo);

        MethodDescCallSite prepareDataForSetup(METHOD__APP_DOMAIN__PREPARE_DATA_FOR_SETUP);

        ARG_SLOT args[8];
        args[0]=ObjToArgSlot(*ppFriendlyName);
        args[1]=ObjToArgSlot(*ppAppdomainSetup);
        args[2]=ObjToArgSlot(*ppProvidedEvidence);
        args[3]=ObjToArgSlot(*ppCreatorsEvidence);
        args[4]=PtrToArgSlot(parentSecurityDescriptor);
        args[5]=PtrToArgSlot(NULL);
        args[6]=PtrToArgSlot(NULL);
        args[7]=PtrToArgSlot(NULL);

        setupInfo = prepareDataForSetup.Call_RetOBJECTREF(args);

#ifndef FEATURE_CORECLR
        // We need to setup domain sorting before any other managed code runs in the domain, since that code 
        // could end up caching data based on the sorting mode of the domain.
        pDomain->InitializeSorting(ppAppdomainSetup);
        pDomain->InitializeHashing(ppAppdomainSetup);
#endif

        // We need to ensure that the AppDomainProxy is generated before we call into DoSetup, since
        // GetAppDomainProxy will ensure that remoting is correctly configured in the domain.  DoSetup can
        // end up loading user assemblies into the domain, and those assemblies may require that remoting be
        // setup already.  For instance, C++/CLI applications may trigger the CRT to try to marshal a
        // reference to the default domain into the current domain, which won't work correctly without this
        // setup being done.
        *pRetVal = pDomain->GetAppDomainProxy();

        *pEntryPointProxy=pDomain->DoSetup(&setupInfo);


        GCPROTECT_END();

        pDomain->CacheStringsForDAC();
    }

#ifdef PROFILING_SUPPORTED
    EX_HOOK
    {
        // Need the first assembly loaded in to get any data on an app domain.
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
            GCX_PREEMP();
            g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID)(AppDomain *) pDomain, GET_EXCEPTION()->GetHR());
            END_PIN_PROFILER();
        }
    }
    EX_END_HOOK;

    // Need the first assembly loaded in to get any data on an app domain.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID)(AppDomain*) pDomain, S_OK);
        END_PIN_PROFILER();
    }        
#endif // PROFILING_SUPPORTED

    ETW::LoaderLog::DomainLoad(pDomain, (LPWSTR)(*ppFriendlyName)->GetBuffer());

    // DoneCreating releases ownership of AppDomain.  After this call, there should be no access to pDomain.
    pDomain.DoneCreating();
}
#endif // FEATURE_REMOTING

void QCALLTYPE AppDomainNative::SetupDomainSecurity(QCall::AppDomainHandle pDomain,
                                                    QCall::ObjectHandleOnStack ohEvidence,
                                                    IApplicationSecurityDescriptor *pParentSecurityDescriptor,
                                                    BOOL fPublishAppDomain)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    struct
    {
        OBJECTREF orEvidence;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCX_COOP();
    GCPROTECT_BEGIN(gc)
    if (ohEvidence.m_ppObject != NULL)
    {
        gc.orEvidence = ObjectToOBJECTREF(*ohEvidence.m_ppObject);
    }


    // Set up the default AppDomain property.
    IApplicationSecurityDescriptor *pSecDesc = pDomain->GetSecurityDescriptor();

    if (!pSecDesc->IsHomogeneous() && pDomain->IsDefaultDomain())
    {
        Security::SetDefaultAppDomainProperty(pSecDesc);
    }
    // Set up the evidence property in the VM side.
    else
    {
        // If there is no provided evidence then this new appdomain gets the same evidence as the creator.
        //
        // If there is no provided evidence and this AppDomain is not homogeneous, then it automatically 
        // is also a default appdomain (for security grant set purposes)
        //
        //
        // If evidence is provided, the new appdomain is not a default appdomain and
        // we simply use the provided evidence.

        if (gc.orEvidence == NULL)  
        {
            _ASSERTE(pParentSecurityDescriptor == NULL ||  pParentSecurityDescriptor->IsDefaultAppDomainEvidence());

            if (pSecDesc->IsHomogeneous())
            {
                // New domain gets default AD evidence
                Security::SetDefaultAppDomainEvidenceProperty(pSecDesc);
            }
            else
            {
                // New domain gets to be a default AD
                Security::SetDefaultAppDomainProperty(pSecDesc);
            }
        }
    }

#ifdef FEATURE_CAS_POLICY
    if (gc.orEvidence != NULL)
    {
        pSecDesc->SetEvidence(gc.orEvidence);
    }
#endif // FEATURE_CAS_POLICY

    // We need to downgrade sharing level if the AppDomain is homogeneous and not fully trusted, or the
    // AppDomain is in legacy mode.  Effectively, we need to be sure that all assemblies loaded into the
    // domain must be fully trusted in order to allow non-GAC sharing.
#ifdef FEATURE_FUSION
    if (pDomain->GetSharePolicy() == AppDomain::SHARE_POLICY_ALWAYS)
    {
        bool fSandboxedHomogenousDomain = false;
        if (pSecDesc->IsHomogeneous())
        {
            pSecDesc->Resolve();
            fSandboxedHomogenousDomain = !pSecDesc->IsFullyTrusted();
        }

        if (fSandboxedHomogenousDomain || pSecDesc->IsLegacyCasPolicyEnabled())
        {
            // We may not be able to reduce sharing policy at this point, if we have already loaded
            // some non-GAC assemblies as domain neutral.  For this case we must regrettably fail
            // the whole operation.
            if (!pDomain->ReduceSharePolicyFromAlways())
            {
                ThrowHR(COR_E_CANNOT_SET_POLICY);
            }
        }
    }
#endif

    // Now finish the initialization.
    pSecDesc->FinishInitialization();

    // once domain is loaded it is publically available so if you have anything 
    // that a list interrogator might need access to if it gets a hold of the
    // appdomain, then do it above the LoadDomain.
    if (fPublishAppDomain)
        SystemDomain::LoadDomain(pDomain);

#ifdef _DEBUG
    LOG((LF_APPDOMAIN, LL_INFO100, "AppDomainNative::CreateDomain domain [%d] %p %S\n", pDomain->GetIndex().m_dwIndex, (AppDomain*)pDomain, pDomain->GetFriendlyName()));
#endif

    GCPROTECT_END();

    END_QCALL;
}

FCIMPL2(void, AppDomainNative::SetupFriendlyName, AppDomainBaseObject* refThisUNSAFE, StringObject* strFriendlyNameUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        APPDOMAINREF    refThis;
        STRINGREF       strFriendlyName;
    } gc;

    gc.refThis          = (APPDOMAINREF) refThisUNSAFE;
    gc.strFriendlyName  = (STRINGREF)    strFriendlyNameUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc)

    AppDomainRefHolder pDomain(ValidateArg(gc.refThis));
    pDomain->AddRef();

    // If the user created this domain, need to know this so the debugger doesn't
    // go and reset the friendly name that was provided.
    pDomain->SetIsUserCreatedDomain();

    WCHAR* pFriendlyName = NULL;
    Thread *pThread = GetThread();

    CheckPointHolder cph(pThread->m_MarshalAlloc.GetCheckpoint()); //hold checkpoint for autorelease
    if (gc.strFriendlyName != NULL) {
        WCHAR* pString = NULL;
        int    iString;
        gc.strFriendlyName->RefInterpretGetStringValuesDangerousForGC(&pString, &iString);
        if (ClrSafeInt<int>::addition(iString, 1, iString))
        {
            pFriendlyName = new (&pThread->m_MarshalAlloc) WCHAR[(iString)];

            // Check for a valid string allocation
            if (pFriendlyName == (WCHAR*)-1)
                pFriendlyName = NULL;
            else
                memcpy(pFriendlyName, pString, iString*sizeof(WCHAR));
        }
    }

    pDomain->SetFriendlyName(pFriendlyName);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#if FEATURE_COMINTEROP

FCIMPL1(void, AppDomainNative::SetDisableInterfaceCache, AppDomainBaseObject* refThisUNSAFE)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
        SO_TOLERANT;
        THROWS;
    }
    CONTRACTL_END;

    struct _gc
    {
        APPDOMAINREF    refThis;
    } gc;

    gc.refThis          = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc)

    AppDomainRefHolder pDomain(ValidateArg(gc.refThis));
    pDomain->AddRef();

    pDomain->SetDisableInterfaceCache();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_FUSION
FCIMPL1(LPVOID, AppDomainNative::GetFusionContext, AppDomainBaseObject* refThis)
{
    FCALL_CONTRACT;

    LPVOID rv = NULL;
    
    HELPER_METHOD_FRAME_BEGIN_RET_1(rv);

    AppDomain* pApp = ValidateArg((APPDOMAINREF)refThis);

    rv = pApp->CreateFusionContext();

    HELPER_METHOD_FRAME_END();

    return rv;
}
FCIMPLEND
#endif

FCIMPL1(void*, AppDomainNative::GetSecurityDescriptor, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    void*        pvRetVal = NULL;    
    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    
    pvRetVal = ValidateArg(refThis)->GetSecurityDescriptor();

    HELPER_METHOD_FRAME_END();
    return pvRetVal;
}
FCIMPLEND

#ifdef FEATURE_LOADER_OPTIMIZATION
FCIMPL2(void, AppDomainNative::UpdateLoaderOptimization, AppDomainBaseObject* refThisUNSAFE, DWORD optimization)
{
    FCALL_CONTRACT;

    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(refThis);

    ValidateArg(refThis)->SetSharePolicy((AppDomain::SharePolicy) (optimization & AppDomain::SHARE_POLICY_MASK));

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#endif // FEATURE_LOADER_OPTIMIZATION

#ifdef FEATURE_FUSION
FCIMPL3(void, AppDomainNative::UpdateContextProperty, LPVOID fusionContext, StringObject* keyUNSAFE, Object* valueUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        STRINGREF key;
        OBJECTREF value;
    } gc;

    gc.key   = ObjectToSTRINGREF(keyUNSAFE);
    gc.value = ObjectToOBJECTREF(valueUNSAFE);
    _ASSERTE(gc.key != NULL);

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    IApplicationContext* pContext = (IApplicationContext*) fusionContext;

    BOOL fFXOnly;
    DWORD size = sizeof(fFXOnly);
    HRESULT hr = pContext->Get(ACTAG_FX_ONLY, &fFXOnly, &size, 0);
    if (hr == HRESULT_FROM_WIN32(ERROR_NOT_FOUND))
    {
        fFXOnly = FALSE;
        hr = S_FALSE;
    }
    IfFailThrow(hr);

    if (!fFXOnly)
    {
        DWORD lgth = gc.key->GetStringLength();
        CQuickBytes qb;
        LPWSTR key = (LPWSTR) qb.AllocThrows((lgth+1)*sizeof(WCHAR));
        memcpy(key, gc.key->GetBuffer(), lgth*sizeof(WCHAR));
        key[lgth] = W('\0');
            
        AppDomain::SetContextProperty(pContext, key, &gc.value);
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#endif  // FEATURE_FUSION

/* static */
INT32 AppDomainNative::ExecuteAssemblyHelper(Assembly* pAssembly,
                                             BOOL bCreatedConsole,
                                             PTRARRAYREF *pStringArgs)
{
    STATIC_CONTRACT_THROWS;

    struct Param
    {
        Assembly* pAssembly;
        PTRARRAYREF *pStringArgs;
        INT32 iRetVal;
    } param;
    param.pAssembly = pAssembly;
    param.pStringArgs = pStringArgs;
    param.iRetVal = 0;

    EE_TRY_FOR_FINALLY(Param *, pParam, &param)
    {
        pParam->iRetVal = pParam->pAssembly->ExecuteMainMethod(pParam->pStringArgs, FALSE /* waitForOtherThreads */);
    }
    EE_FINALLY 
    {
#ifndef FEATURE_PAL
        if(bCreatedConsole)
            FreeConsole();
#endif // !FEATURE_PAL
    } 
    EE_END_FINALLY

    return param.iRetVal;
}

static void UpgradeLinkTimeCheckToLateBoundDemand(MethodDesc* pMeth)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BOOL isEveryoneFullyTrusted = FALSE;

    struct _gc 
    {
        OBJECTREF refClassNonCasDemands;
        OBJECTREF refClassCasDemands;
        OBJECTREF refMethodNonCasDemands;
        OBJECTREF refMethodCasDemands;
        OBJECTREF refThrowable;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    isEveryoneFullyTrusted = Security::AllDomainsOnStackFullyTrusted();

    // If all assemblies in the domain are fully trusted then we are not
    // going to do any security checks anyway..
    if (isEveryoneFullyTrusted) 
    {
        goto Exit1;
    }


    if (pMeth->RequiresLinktimeCheck()) 
    {
        // Fetch link demand sets from all the places in metadata where we might
        // find them (class and method). These might be split into CAS and non-CAS
        // sets as well.
        Security::RetrieveLinktimeDemands(pMeth,
                                          &gc.refClassCasDemands,
                                          &gc.refClassNonCasDemands,
                                          &gc.refMethodCasDemands,
                                          &gc.refMethodNonCasDemands);

        if (gc.refClassCasDemands == NULL && gc.refClassNonCasDemands == NULL &&
            gc.refMethodCasDemands == NULL && gc.refMethodNonCasDemands == NULL &&
            isEveryoneFullyTrusted) 
        {
            // All code access security demands will pass anyway.
            goto Exit1;
        }
   
        // The following logic turns link demands on the target method into full
        // stack walks in order to close security holes in poorly written
        // reflection users.

#ifdef FEATURE_APTCA
        if (Security::IsUntrustedCallerCheckNeeded(pMeth) )
        {
            // Check for untrusted caller
            // It is possible that wrappers like VBHelper libraries that are
            // fully trusted, make calls to public methods that do not have
            // safe for Untrusted caller custom attribute set.
            // Like all other link demand that gets transformed to a full stack 
            // walk for reflection, calls to public methods also gets 
            // converted to full stack walk

            // NOTE: this will always do the APTCA check, regardless of method caller
            Security::DoUntrustedCallerChecks(NULL, pMeth, TRUE);
        }
#endif

        // CAS Link Demands
        if (gc.refClassCasDemands != NULL)
            Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, gc.refClassCasDemands);

        if (gc.refMethodCasDemands != NULL)
            Security::DemandSet(SSWT_LATEBOUND_LINKDEMAND, gc.refMethodCasDemands);

        // Non-CAS demands are not applied against a grant
        // set, they're standalone.
        if (gc.refClassNonCasDemands != NULL)
            Security::CheckNonCasDemand(&gc.refClassNonCasDemands);

        if (gc.refMethodNonCasDemands != NULL)
            Security::CheckNonCasDemand(&gc.refMethodNonCasDemands);
    }

Exit1:;
    GCPROTECT_END();
}

FCIMPL3(INT32, AppDomainNative::ExecuteAssembly, AppDomainBaseObject* refThisUNSAFE,
    AssemblyBaseObject* assemblyNameUNSAFE, PTRArray* stringArgsUNSAFE)
{
    FCALL_CONTRACT;

    INT32 iRetVal = 0;

    struct _gc
    {
        APPDOMAINREF    refThis;
        ASSEMBLYREF     assemblyName;
        PTRARRAYREF     stringArgs;
    } gc;

    gc.refThis      = (APPDOMAINREF) refThisUNSAFE;
    gc.assemblyName = (ASSEMBLYREF)  assemblyNameUNSAFE;
    gc.stringArgs   = (PTRARRAYREF)  stringArgsUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    AppDomain* pDomain = ValidateArg(gc.refThis);

    if (gc.assemblyName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_Generic"));

    if((BaseDomain*) pDomain == SystemDomain::System()) 
        COMPlusThrow(kUnauthorizedAccessException, W("UnauthorizedAccess_SystemDomain"));

    Assembly* pAssembly = (Assembly*) gc.assemblyName->GetAssembly();

    if (!pDomain->m_pRootAssembly)
        pDomain->m_pRootAssembly = pAssembly;

    MethodDesc *pEntryPointMethod;
    {
        pEntryPointMethod = pAssembly->GetEntryPoint();
        if (pEntryPointMethod)
        {
            UpgradeLinkTimeCheckToLateBoundDemand(pEntryPointMethod);        
        }
    }

    BOOL bCreatedConsole = FALSE;

#ifndef FEATURE_PAL
    if (pAssembly->GetManifestFile()->GetSubsystem() == IMAGE_SUBSYSTEM_WINDOWS_CUI)
    {
        {
            GCX_COOP();
            Security::CheckBeforeAllocConsole(pDomain, pAssembly);
        }
        bCreatedConsole = AllocConsole();
        StackSString codebase;
        pAssembly->GetManifestFile()->GetCodeBase(codebase);
        SetConsoleTitle(codebase);
    }
#endif // !FEATURE_PAL

    // This helper will call FreeConsole()
    iRetVal = ExecuteAssemblyHelper(pAssembly, bCreatedConsole, &gc.stringArgs);

    HELPER_METHOD_FRAME_END();

    return iRetVal;
}
FCIMPLEND

#ifdef FEATURE_VERSIONING
FCIMPL1(void,
        AppDomainNative::CreateContext,
        AppDomainBaseObject *refThisUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        APPDOMAINREF refThis;
    } gc;

    gc.refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    AppDomain* pDomain = ValidateArg(gc.refThis);

    if((BaseDomain*) pDomain == SystemDomain::System())
    {
        COMPlusThrow(kUnauthorizedAccessException, W("UnauthorizedAccess_SystemDomain"));
    }

    pDomain->CreateFusionContext();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

void QCALLTYPE AppDomainNative::SetupBindingPaths(__in_z LPCWSTR wszTrustedPlatformAssemblies, __in_z LPCWSTR wszPlatformResourceRoots, __in_z LPCWSTR wszAppPaths, __in_z LPCWSTR wszAppNiPaths, __in_z LPCWSTR appLocalWinMD)
{
    QCALL_CONTRACT;
    
    BEGIN_QCALL;
    
    AppDomain* pDomain = GetAppDomain();

    SString sTrustedPlatformAssemblies(wszTrustedPlatformAssemblies);
    SString sPlatformResourceRoots(wszPlatformResourceRoots);
    SString sAppPaths(wszAppPaths);
    SString sAppNiPaths(wszAppNiPaths);
    SString sappLocalWinMD(appLocalWinMD);
        
    CLRPrivBinderCoreCLR *pBinder = pDomain->GetTPABinderContext();
    _ASSERTE(pBinder != NULL);
    IfFailThrow(pBinder->SetupBindingPaths(sTrustedPlatformAssemblies,
                                            sPlatformResourceRoots,
                                            sAppPaths,
                                            sAppNiPaths));

#ifdef FEATURE_COMINTEROP
        pDomain->SetWinrtApplicationContext(sappLocalWinMD);
#endif

    END_QCALL;
}

#endif // FEATURE_VERSIONING

FCIMPL12(Object*, AppDomainNative::CreateDynamicAssembly, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* assemblyNameUNSAFE, Object* identityUNSAFE, StackCrawlMark* stackMark, Object* requiredPsetUNSAFE, Object* optionalPsetUNSAFE, Object* refusedPsetUNSAFE, U1Array *securityRulesBlobUNSAFE, U1Array *aptcaBlobUNSAFE, INT32 access, INT32 dwFlags, SecurityContextSource securityContextSource)
{
    FCALL_CONTRACT;

    ASSEMBLYREF refRetVal = NULL;

    //<TODO>
    // @TODO: there MUST be a better way to do this...
    //</TODO>
    CreateDynamicAssemblyArgs   args;

    args.refThis                = (APPDOMAINREF)    refThisUNSAFE;
    args.assemblyName           = (ASSEMBLYNAMEREF) assemblyNameUNSAFE;
    args.identity               = (OBJECTREF)       identityUNSAFE;
    args.requiredPset           = (OBJECTREF)       requiredPsetUNSAFE;
    args.optionalPset           = (OBJECTREF)       optionalPsetUNSAFE;
    args.refusedPset            = (OBJECTREF)       refusedPsetUNSAFE;
    args.securityRulesBlob      = (U1ARRAYREF)      securityRulesBlobUNSAFE;
    args.aptcaBlob              = (U1ARRAYREF)      aptcaBlobUNSAFE;
    args.loaderAllocator        = NULL;

    args.access                 = access;
    args.flags                  = static_cast<DynamicAssemblyFlags>(dwFlags);
    args.stackMark              = stackMark;
    args.securityContextSource  = securityContextSource;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT((CreateDynamicAssemblyArgsGC&)args);

    AppDomain* pAppDomain = ValidateArg(args.refThis);

    Assembly *pAssembly = Assembly::CreateDynamic(pAppDomain, &args);

    refRetVal = (ASSEMBLYREF) pAssembly->GetExposedObject();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

//---------------------------------------------------------------------------------------
//
// Returns true if the DisableFusionUpdatesFromADManager config switch is turned on.
//
// Arguments:
//    adhTarget   - AppDomain to get domain manager information about
//

// static
BOOL QCALLTYPE AppDomainNative::DisableFusionUpdatesFromADManager(QCall::AppDomainHandle adhTarget)
{
    QCALL_CONTRACT;

    BOOL bUpdatesDisabled = FALSE;

    BEGIN_QCALL;

    bUpdatesDisabled = !!(g_pConfig->DisableFusionUpdatesFromADManager());

    END_QCALL;

    return bUpdatesDisabled;
}

#ifdef FEATURE_APPX

//
// Keep in sync with bcl\system\appdomain.cs
//
enum
{
    APPX_FLAGS_INITIALIZED =        0x01,

    APPX_FLAGS_APPX_MODEL =         0x02,
    APPX_FLAGS_APPX_DESIGN_MODE =   0x04,
    APPX_FLAGS_APPX_NGEN =          0x08,
    APPX_FLAGS_APPX_MASK =          APPX_FLAGS_APPX_MODEL |
                                    APPX_FLAGS_APPX_DESIGN_MODE |
                                    APPX_FLAGS_APPX_NGEN,

    APPX_FLAGS_API_CHECK =          0x10,
};

// static
INT32 QCALLTYPE AppDomainNative::GetAppXFlags()
{
    QCALL_CONTRACT;

    UINT32 flags = APPX_FLAGS_INITIALIZED;

    BEGIN_QCALL;

    if (AppX::IsAppXProcess())
    {
        flags |= APPX_FLAGS_APPX_MODEL;

        if (AppX::IsAppXDesignMode())
            flags |= APPX_FLAGS_APPX_DESIGN_MODE;
        else
            flags |= APPX_FLAGS_API_CHECK;

        if (AppX::IsAppXNGen())
            flags |= APPX_FLAGS_APPX_NGEN;
    }

    //
    // 0: normal (only check in non-dev-mode APPX)
    // 1: always check
    // 2: never check
    //
    switch (g_pConfig->GetWindows8ProfileAPICheckFlag())
    {
        case 1:
            flags |= APPX_FLAGS_API_CHECK;
            break;
        case 2:
            flags &= ~APPX_FLAGS_API_CHECK;
            break;
        default:
            break;
    }

    END_QCALL;

    return flags;
}

#endif // FEATURE_APPX

//---------------------------------------------------------------------------------------
//
// Get the assembly and type containing the AppDomainManager used for the current domain
//
// Arguments:
//    adhTarget   - AppDomain to get domain manager information about
//    retAssembly - [out] assembly which contains the AppDomainManager
//    retType     - [out] AppDomainManger for the domain
//    
// Notes:
//    If the AppDomain does not have an AppDomainManager, retAssembly and retType will be null on return.
//

// static
void QCALLTYPE AppDomainNative::GetAppDomainManagerType(QCall::AppDomainHandle adhTarget,
                                                        QCall::StringHandleOnStack shRetAssembly,
                                                        QCall::StringHandleOnStack shRetType)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (adhTarget->HasAppDomainManagerInfo())
    {
        shRetAssembly.Set(adhTarget->GetAppDomainManagerAsm());
        shRetType.Set(adhTarget->GetAppDomainManagerType());
    }
    else
    {
        shRetAssembly.Set(static_cast<LPCWSTR>(NULL));
        shRetType.Set(static_cast<LPCWSTR>(NULL));
    }

    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Set the assembly and type containing the AppDomainManager to be used for the current domain
//
// Arguments:
//    adhTarget   - AppDomain to set domain manager information for
//    wszAssembly - assembly which contains the AppDomainManager
//    wszType     - AppDomainManger for the domain
//

// static
void QCALLTYPE AppDomainNative::SetAppDomainManagerType(QCall::AppDomainHandle adhTarget,
                                                        __in_z LPCWSTR wszAssembly,
                                                        __in_z LPCWSTR wszType)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(wszAssembly));
        PRECONDITION(CheckPointer(wszType));
        PRECONDITION(!GetAppDomain()->HasAppDomainManagerInfo());
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    // If the AppDomainManager type is the same as the domain manager setup by the CLR host, then we can
    // propagate the host's initialization flags to the new domain as well;
    EInitializeNewDomainFlags initializationFlags = eInitializeNewDomainFlags_None;
    if (CorHost2::HasAppDomainManagerInfo())
    {
        if (wcscmp(CorHost2::GetAppDomainManagerAsm(), wszAssembly) == 0 &&
            wcscmp(CorHost2::GetAppDomainManagerType(), wszType) == 0)
        {
            initializationFlags = CorHost2::GetAppDomainManagerInitializeNewDomainFlags();
        }
    }

    adhTarget->SetAppDomainManagerInfo(wszAssembly, wszType, initializationFlags);

    // If the initialization flags promise that the domain manager isn't going to modify security, then do a
    // pre-resolution of the domain now so that we can do some basic verification of the state later.  We
    // don't care about the actual result now, just that the resolution took place to compare against later.
    if (initializationFlags & eInitializeNewDomainFlags_NoSecurityChanges)
    {
        BOOL fIsFullyTrusted;
        BOOL fIsHomogeneous;
        adhTarget->GetSecurityDescriptor()->PreResolve(&fIsFullyTrusted, &fIsHomogeneous);
    }

    END_QCALL;
}

#ifdef FEATURE_APPDOMAINMANAGER_INITOPTIONS

FCIMPL0(FC_BOOL_RET, AppDomainNative::HasHost)
{
    FCALL_CONTRACT;
    FC_RETURN_BOOL(CorHost2::GetHostControl() != NULL);
}
FCIMPLEND

//
// Callback to the CLR host to register an AppDomainManager->AppDomain ID pair with it.
//
// Arguments:
//    punkAppDomainManager - COM reference to the AppDomainManager being registered with the host
//

// static
void QCALLTYPE AppDomainNative::RegisterWithHost(IUnknown *punkAppDomainManager)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(punkAppDomainManager));
        PRECONDITION(CheckPointer(CorHost2::GetHostControl()));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    EnsureComStarted();

    IHostControl *pHostControl = CorHost2::GetHostControl();
    ADID dwDomainId = SystemDomain::GetCurrentDomain()->GetId();
    HRESULT hr = S_OK;

    BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
    hr = pHostControl->SetAppDomainManager(dwDomainId.m_dwId, punkAppDomainManager);
    END_SO_TOLERANT_CODE_CALLING_HOST;

    if (FAILED(hr))
    {
        ThrowHR(hr);
    }

    END_QCALL;
}
#endif // FEATURE_APPDOMAINMANAGER_INITOPTIONS

FCIMPL1(void, AppDomainNative::SetHostSecurityManagerFlags, DWORD dwFlags);
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    GetThread()->GetDomain()->GetSecurityDescriptor()->SetHostSecurityManagerFlags(dwFlags);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// static
void QCALLTYPE AppDomainNative::SetSecurityHomogeneousFlag(QCall::AppDomainHandle adhTarget,
                                                           BOOL fRuntimeSuppliedHomogenousGrantSet)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    IApplicationSecurityDescriptor *pAppSecDesc = adhTarget->GetSecurityDescriptor();
    pAppSecDesc->SetHomogeneousFlag(fRuntimeSuppliedHomogenousGrantSet);

    END_QCALL;
}

#ifdef FEATURE_CAS_POLICY

// static
void QCALLTYPE AppDomainNative::SetLegacyCasPolicyEnabled(QCall::AppDomainHandle adhTarget)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    IApplicationSecurityDescriptor *pAppSecDesc = adhTarget->GetSecurityDescriptor();
    pAppSecDesc->SetLegacyCasPolicyEnabled();

    END_QCALL;
}

// static
BOOL QCALLTYPE AppDomainNative::IsLegacyCasPolicyEnabled(QCall::AppDomainHandle adhTarget)
{
    QCALL_CONTRACT;

    BOOL fLegacyCasPolicy = FALSE;

    BEGIN_QCALL;

    IApplicationSecurityDescriptor *pAppSecDesc = adhTarget->GetSecurityDescriptor();
    fLegacyCasPolicy = !!pAppSecDesc->IsLegacyCasPolicyEnabled();

    END_QCALL;

    return fLegacyCasPolicy;
}

#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_APTCA

// static
void QCALLTYPE AppDomainNative::SetCanonicalConditionalAptcaList(QCall::AppDomainHandle adhTarget,
                                                                 LPCWSTR wszCanonicalConditionalAptcaList)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    IApplicationSecurityDescriptor *pAppSecDesc = adhTarget->GetSecurityDescriptor();

    GCX_COOP();
    pAppSecDesc->SetCanonicalConditionalAptcaList(wszCanonicalConditionalAptcaList);

    END_QCALL;
}

#endif // FEATURE_APTCA

FCIMPL1(Object*, AppDomainNative::GetFriendlyName, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF    str     = NULL;
    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    AppDomain* pApp = ValidateArg(refThis);

    LPCWSTR wstr = pApp->GetFriendlyName();
    if (wstr)
        str = StringObject::NewString(wstr);   

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(str);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, AppDomainNative::IsDefaultAppDomainForEvidence, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    BOOL retVal = FALSE;
    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    AppDomain* pApp = ValidateArg((APPDOMAINREF) refThisUNSAFE);
    retVal = pApp->GetSecurityDescriptor()->IsDefaultAppDomainEvidence();

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

FCIMPL2(Object*, AppDomainNative::GetAssemblies, AppDomainBaseObject* refThisUNSAFE, CLR_BOOL forIntrospection);
{
    FCALL_CONTRACT;

    struct _gc
    {
        PTRARRAYREF     AsmArray;
        APPDOMAINREF    refThis;
    } gc;

    gc.AsmArray = NULL;
    gc.refThis  = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    MethodTable * pAssemblyClass = MscorlibBinder::GetClass(CLASS__ASSEMBLY);

    AppDomain * pApp = ValidateArg(gc.refThis);

    // Allocate an array with as many elements as there are assemblies in this
    //  appdomain.  This will usually be correct, but there may be assemblies
    //  that are still loading, and those won't be included in the array of
    //  loaded assemblies.  When that happens, the array will have some trailing
    //  NULL entries; those entries will need to be trimmed.
    size_t nArrayElems = pApp->m_Assemblies.GetCount(pApp);
    gc.AsmArray = (PTRARRAYREF) AllocateObjectArray(
        (DWORD)nArrayElems, 
        pAssemblyClass);

    size_t numAssemblies = 0;
    {
        // Iterate over the loaded assemblies in the appdomain, and add each one to
        //  to the array.  Quit when the array is full, in case assemblies have been
        //  loaded into this appdomain, on another thread.
        AppDomain::AssemblyIterator i = pApp->IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeLoaded | 
            (forIntrospection ? kIncludeIntrospection : kIncludeExecution)));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        
        while (i.Next(pDomainAssembly.This()) && (numAssemblies < nArrayElems))
        {
            // Do not change this code.  This is done this way to
            //  prevent a GC hole in the SetObjectReference() call.  The compiler
            //  is free to pick the order of evaluation.
            OBJECTREF o = (OBJECTREF)pDomainAssembly->GetExposedAssemblyObject();
            if (o == NULL)
            {   // The assembly was collected and is not reachable from managed code anymore
                continue;
            }
            gc.AsmArray->SetAt(numAssemblies++, o);
            // If it is a collectible assembly, it is now referenced from the managed world, so we can 
            // release the native reference in the holder
        }
    }

    // If we didn't fill the array, allocate a new array that is exactly the
    //  right size, and copy the data to it.
    if (numAssemblies < nArrayElems)
    {
        PTRARRAYREF AsmArray2;
        AsmArray2 = (PTRARRAYREF) AllocateObjectArray(
            (DWORD)numAssemblies, 
            pAssemblyClass);

        for (size_t ix = 0; ix < numAssemblies; ++ix)
        {
            AsmArray2->SetAt(ix, gc.AsmArray->GetAt(ix));
        }

        gc.AsmArray = AsmArray2;
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.AsmArray);
} // AppDomainNative::GetAssemblies
FCIMPLEND


FCIMPL1(void, AppDomainNative::Unload, INT32 dwId)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    IfFailThrow(AppDomain::UnloadById(ADID(dwId),TRUE));

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, AppDomainNative::IsDomainIdValid, INT32 dwId)
{
    FCALL_CONTRACT;

    BOOL retVal = FALSE;
    HELPER_METHOD_FRAME_BEGIN_RET_0()

    AppDomainFromIDHolder ad((ADID)dwId, TRUE);
    retVal=!ad.IsUnloaded();
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

#ifdef FEATURE_REMOTING
FCIMPL0(Object*, AppDomainNative::GetDefaultDomain)
{
    FCALL_CONTRACT;

    APPDOMAINREF rv = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(rv);

    if (GetThread()->GetDomain()->IsDefaultDomain())
        rv = (APPDOMAINREF) SystemDomain::System()->DefaultDomain()->GetExposedObject();
    else
        rv = (APPDOMAINREF) SystemDomain::System()->DefaultDomain()->GetAppDomainProxy();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(rv);
}
FCIMPLEND
#endif    

FCIMPL1(INT32, AppDomainNative::GetId, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    INT32        iRetVal = 0;
    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    AppDomain* pApp = ValidateArg(refThis);
    // can only be accessed from within current domain
    _ASSERTE(GetThread()->GetDomain() == pApp);

    iRetVal = pApp->GetId().m_dwId;

    HELPER_METHOD_FRAME_END();
    return iRetVal;
}
FCIMPLEND

FCIMPL1(void, AppDomainNative::ChangeSecurityPolicy, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(refThis);
    AppDomain* pApp = ValidateArg(refThis);

#ifdef FEATURE_FUSION

    // We do not support sharing behavior of ALWAYS when using app-domain local security config
    if (pApp->GetSharePolicy() == AppDomain::SHARE_POLICY_ALWAYS)
    {
        // We may not be able to reduce sharing policy at this point, if we have already loaded
        // some non-GAC assemblies as domain neutral.  For this case we must regrettably fail
        // the whole operation.
        if (!pApp->ReduceSharePolicyFromAlways())
            ThrowHR(COR_E_CANNOT_SET_POLICY);
    }
#endif
    pApp->GetSecurityDescriptor()->SetPolicyLevelFlag();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


FCIMPL2(Object*, AppDomainNative::IsStringInterned, AppDomainBaseObject* refThisUNSAFE, StringObject* pStringUNSAFE)
{
    FCALL_CONTRACT;

    APPDOMAINREF    refThis     = (APPDOMAINREF)ObjectToOBJECTREF(refThisUNSAFE);
    STRINGREF       refString   = ObjectToSTRINGREF(pStringUNSAFE);
    STRINGREF*      prefRetVal  = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_2(refThis, refString);

    ValidateArg(refThis);
    
    if (refString == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    prefRetVal = refThis->GetDomain()->IsStringInterned(&refString);

    HELPER_METHOD_FRAME_END();

    if (prefRetVal == NULL)
        return NULL;

    return OBJECTREFToObject(*prefRetVal);
}
FCIMPLEND

FCIMPL2(Object*, AppDomainNative::GetOrInternString, AppDomainBaseObject* refThisUNSAFE, StringObject* pStringUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF    refRetVal  = NULL;
    APPDOMAINREF refThis    = (APPDOMAINREF) refThisUNSAFE;
    STRINGREF    pString    = (STRINGREF)    pStringUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(refThis, pString);

    ValidateArg(refThis);

    if (pString == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    STRINGREF* stringVal = refThis->GetDomain()->GetOrInternString(&pString);
    if (stringVal != NULL)
    {
        refRetVal = *stringVal;
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND


FCIMPL1(Object*, AppDomainNative::GetDynamicDir, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF    str        = NULL;
#ifdef FEATURE_FUSION    
    APPDOMAINREF refThis    = (APPDOMAINREF) refThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);
    
    AppDomain *pDomain = ValidateArg(refThis);
    str = StringObject::NewString(pDomain->GetDynamicDir());
    HELPER_METHOD_FRAME_END();
#endif    
    return OBJECTREFToObject(str);
}
FCIMPLEND

// static
void QCALLTYPE AppDomainNative::GetGrantSet(QCall::AppDomainHandle adhTarget,
                                            QCall::ObjectHandleOnStack retGrantSet)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    IApplicationSecurityDescriptor *pSecDesc = adhTarget->GetSecurityDescriptor();

    GCX_COOP();
    pSecDesc->Resolve();
    retGrantSet.Set(pSecDesc->GetGrantedPermissionSet());

    END_QCALL;
}


FCIMPL1(FC_BOOL_RET, AppDomainNative::IsUnloadingForcedFinalize, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    BOOL retVal = FALSE;
    APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    AppDomain* pApp = ValidateArg((APPDOMAINREF)refThis);
    retVal = pApp->IsFinalized();

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, AppDomainNative::IsFinalizingForUnload, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    BOOL            retVal = FALSE;
    APPDOMAINREF    refThis = (APPDOMAINREF) refThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    AppDomain* pApp = ValidateArg(refThis);
    retVal = pApp->IsFinalizing();

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

FCIMPL2(StringObject*, AppDomainNative::nApplyPolicy, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* refAssemblyNameUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        APPDOMAINREF    refThis;
        ASSEMBLYNAMEREF assemblyName;
        STRINGREF       rv;
    } gc;

    gc.refThis      = (APPDOMAINREF)refThisUNSAFE;
    gc.assemblyName = (ASSEMBLYNAMEREF) refAssemblyNameUNSAFE;
    gc.rv           = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    AppDomain* pDomain;
    pDomain = ValidateArg(gc.refThis);

    if (gc.assemblyName == NULL)
    {
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_AssemblyName"));
    }
    if( (gc.assemblyName->GetSimpleName() == NULL) )
    {
        COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));
    }
    Thread *pThread = GetThread();
    CheckPointHolder cph(pThread->m_MarshalAlloc.GetCheckpoint()); //hold checkpoint for autorelease

    // Initialize spec
    AssemblySpec spec;
    spec.InitializeSpec(&(pThread->m_MarshalAlloc), 
                        &gc.assemblyName,
                        FALSE, /*fIsStringized*/ 
                        FALSE /*fForIntrospection*/
                       );

    StackSString sDisplayName;

#ifdef FEATURE_FUSION
    {
        GCX_PREEMP();

        SafeComHolderPreemp<IAssemblyName> pAssemblyName(NULL);
        SafeComHolderPreemp<IAssemblyName> pBoundName(NULL);
        IfFailThrow(spec.CreateFusionName(&pAssemblyName));
        HRESULT hr = PreBindAssembly(pDomain->GetFusionContext(),
                                    pAssemblyName,
                                    NULL, // pAsmParent (only needed to see if parent is loadfrom - in this case, we always want it to load in the normal ctx)
                                    &pBoundName,
                                    NULL  // pvReserved
                                    );
        if (FAILED(hr) && hr != FUSION_E_REF_DEF_MISMATCH)
        {
            ThrowHR(hr);
        }

        FusionBind::GetAssemblyNameDisplayName(pBoundName, /*modifies*/sDisplayName, 0 /*flags*/);
    }
#else
    spec.GetFileOrDisplayName(0,sDisplayName);
#endif

    gc.rv = StringObject::NewString(sDisplayName);

    HELPER_METHOD_FRAME_END();
    return (StringObject*)OBJECTREFToObject(gc.rv);
}
FCIMPLEND

FCIMPL1(UINT32, AppDomainNative::GetAppDomainId, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    FCUnique(0x91);

    UINT32 retVal = 0;
    APPDOMAINREF domainRef = (APPDOMAINREF) refThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(domainRef);

    AppDomain* pDomain = ValidateArg(domainRef);
    retVal = pDomain->GetId().m_dwId;

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND

FCIMPL1(void , AppDomainNative::PublishAnonymouslyHostedDynamicMethodsAssembly, AssemblyBaseObject * pAssemblyUNSAFE);
{
    CONTRACTL
    {
        FCALL_CHECK;
    }
    CONTRACTL_END;

    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);
    if (refAssembly == NULL)
        FCThrowResVoid(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly* pDomainAssembly = refAssembly->GetDomainAssembly();

    pDomainAssembly->GetAppDomain()->SetAnonymouslyHostedDynamicMethodsAssembly(pDomainAssembly);
}
FCIMPLEND

#ifdef FEATURE_CORECLR    

void QCALLTYPE AppDomainNative::SetNativeDllSearchDirectories(__in_z LPCWSTR wszNativeDllSearchDirectories)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(wszNativeDllSearchDirectories));
    }
    CONTRACTL_END;

    BEGIN_QCALL;
    AppDomain *pDomain = GetAppDomain();

    SString sDirectories(wszNativeDllSearchDirectories);

    if(sDirectories.GetCount() > 0)
    {
        SString::CIterator start = sDirectories.Begin();
        SString::CIterator itr = sDirectories.Begin();
        SString::CIterator end = sDirectories.End();
        SString qualifiedPath;

        while (itr != end)
        {
            start = itr;
            BOOL found = sDirectories.Find(itr, PATH_SEPARATOR_CHAR_W);
            if (!found)
            {
                itr = end;
            }

            SString qualifiedPath(sDirectories,start,itr);

            if (found)
            {
                itr++;
            }

            unsigned len = qualifiedPath.GetCount();

            if (len > 0)
            {
                if (qualifiedPath[len - 1] != DIRECTORY_SEPARATOR_CHAR_W)
                {
                    qualifiedPath.Append(DIRECTORY_SEPARATOR_CHAR_W);
                }

                NewHolder<SString> stringHolder (new SString(qualifiedPath));
                IfFailThrow(pDomain->m_NativeDllSearchDirectories.Append(stringHolder.GetValue()));
                stringHolder.SuppressRelease();
            }
        }
    }
    END_QCALL;
}

#endif // FEATURE_CORECLR    

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
FCIMPL0(void, AppDomainNative::EnableMonitoring)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    EnableARM();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, AppDomainNative::MonitoringIsEnabled)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();

    FC_RETURN_BOOL(g_fEnableARM);
}
FCIMPLEND

FCIMPL1(INT64, AppDomainNative::GetTotalProcessorTime, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    INT64 i64RetVal = -1;

    if (g_fEnableARM)
    {
        APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;
        HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

        AppDomain* pDomain = ValidateArg(refThis);
        // can only be accessed from within current domain
        _ASSERTE(GetThread()->GetDomain() == pDomain);

        i64RetVal = (INT64)pDomain->QueryProcessorUsage();

        HELPER_METHOD_FRAME_END();
    }

    return i64RetVal;
}
FCIMPLEND

FCIMPL1(INT64, AppDomainNative::GetTotalAllocatedMemorySize, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    INT64 i64RetVal = -1;

    if (g_fEnableARM)
    {
        APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;
        HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

        AppDomain* pDomain = ValidateArg(refThis);
        // can only be accessed from within current domain
        _ASSERTE(GetThread()->GetDomain() == pDomain);

        i64RetVal = (INT64)pDomain->GetAllocBytes();

        HELPER_METHOD_FRAME_END();
    }

    return i64RetVal;
}
FCIMPLEND

FCIMPL1(INT64, AppDomainNative::GetLastSurvivedMemorySize, AppDomainBaseObject* refThisUNSAFE)
{
    FCALL_CONTRACT;

    INT64 i64RetVal = -1;

    if (g_fEnableARM)
    {
        APPDOMAINREF refThis = (APPDOMAINREF) refThisUNSAFE;
        HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

        AppDomain* pDomain = ValidateArg(refThis);
        // can only be accessed from within current domain
        _ASSERTE(GetThread()->GetDomain() == pDomain);

        i64RetVal = (INT64)pDomain->GetSurvivedBytes();

        HELPER_METHOD_FRAME_END();
    }

    return i64RetVal;
}
FCIMPLEND

FCIMPL0(INT64, AppDomainNative::GetLastSurvivedProcessMemorySize)
{
    FCALL_CONTRACT;

    INT64 i64RetVal = -1;

    if (g_fEnableARM)
    {
        i64RetVal = SystemDomain::GetTotalSurvivedBytes();
    }

    return i64RetVal;


}
FCIMPLEND
#endif // FEATURE_APPDOMAIN_RESOURCE_MONITORING

#if defined(FEATURE_HOSTED_BINDER) && defined(FEATURE_APPX_BINDER)
ICLRPrivBinder * QCALLTYPE AppDomainNative::CreateDesignerContext(LPCWSTR *rgPaths, 
                                                            UINT cPaths,
                                                            BOOL fShared)
{
    QCALL_CONTRACT;

    ICLRPrivBinder *pRetVal = nullptr;

    BEGIN_QCALL;
    ReleaseHolder<ICLRPrivBinder> pBinder;

     // The runtime check is done on the managed side to enable the debugger to use
     // FuncEval to create designer contexts outside of DesignMode.
    _ASSERTE(AppX::IsAppXDesignMode() || (AppX::IsAppXProcess() && CORDebuggerAttached()));

    AppDomain *pAppDomain = GetAppDomain();

    pBinder = CLRPrivBinderAppX::CreateParentedBinder(fShared ? pAppDomain->GetLoadContextHostBinder() : pAppDomain->GetSharedContextHostBinder(), CLRPrivTypeCacheWinRT::GetOrCreateTypeCache(), rgPaths, cPaths, fShared /* fCanUseNativeImages */);

    {
        BaseDomain::LockHolder lh(pAppDomain);
        pAppDomain->AppDomainInterfaceReleaseList.Append(pRetVal);
    }
    pBinder.SuppressRelease();
    pRetVal = pBinder;
    
    END_QCALL;

    return pRetVal;
}

void QCALLTYPE AppDomainNative::SetCurrentDesignerContext(BOOL fDesignerContext, ICLRPrivBinder *newContext)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (fDesignerContext)
    {
        GetAppDomain()->SetCurrentContextHostBinder(newContext);
    }
    else
    {
        // Managed code is responsible for ensuring this isn't called more than once per AppDomain.
        GetAppDomain()->SetSharedContextHostBinder(newContext);
    }

    END_QCALL;
}
#endif // defined(FEATURE_HOSTED_BINDER) && defined(FEATURE_APPX_BINDER)

