// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "common.h"
#include "appdomain.hpp"
#include "appdomainnative.hpp"
#include "vars.hpp"
#include "eeconfig.h"
#include "appdomain.inl"
#include "eventtrace.h"
#if defined(FEATURE_APPX)
#include "appxutil.h"
#endif // FEATURE_APPX
#include "../binder/inc/clrprivbindercoreclr.h"

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
        if (WinRTSupported())
        {
            pDomain->SetWinrtApplicationContext(sappLocalWinMD);
        }
#endif

    END_QCALL;
}


FCIMPL4(Object*, AppDomainNative::CreateDynamicAssembly, AppDomainBaseObject* refThisUNSAFE, AssemblyNameBaseObject* assemblyNameUNSAFE, StackCrawlMark* stackMark, INT32 access)
{
    FCALL_CONTRACT;

    ASSEMBLYREF refRetVal = NULL;

    //<TODO>
    // @TODO: there MUST be a better way to do this...
    //</TODO>
    CreateDynamicAssemblyArgs   args;

    args.refThis                = (APPDOMAINREF)    refThisUNSAFE;
    args.assemblyName           = (ASSEMBLYNAMEREF) assemblyNameUNSAFE;
    args.loaderAllocator        = NULL;

    args.access                 = access;
    args.stackMark              = stackMark;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT((CreateDynamicAssemblyArgsGC&)args);

    AppDomain* pAppDomain = ValidateArg(args.refThis);

    Assembly *pAssembly = Assembly::CreateDynamic(pAppDomain, &args);

    refRetVal = (ASSEMBLYREF) pAssembly->GetExposedObject();

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

#ifdef FEATURE_APPX

//
// Keep in sync with bcl\system\appdomain.cs
//
enum
{
    APPX_FLAGS_INITIALIZED =        0x01,

    APPX_FLAGS_APPX_MODEL =         0x02,
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
    }

    END_QCALL;

    return flags;
}

#endif // FEATURE_APPX

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
            kIncludeLoaded | kIncludeExecution));
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
    return OBJECTREFToObject(str);
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
                        FALSE /*fIsStringized*/
                       );

    StackSString sDisplayName;

    spec.GetFileOrDisplayName(0,sDisplayName);

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


