// Licensed to the .NET Foundation under one or more agreements.
//The .NET Foundation licenses this file to you under the MIT license.
//See the LICENSE file in the project root for more information.


#include "common.h"

#include "security.h"
#include "perfcounters.h"
#include "eventtrace.h"
#include "appdomainstack.inl"

#ifndef FEATURE_PAL
#include <shlobj.h>
#include <Accctrl.h>
#include <Aclapi.h>
#include "urlmon.h"
#endif // !FEATURE_PAL

#ifndef CROSSGEN_COMPILE
void *SecurityProperties::operator new(size_t size, LoaderHeap *pHeap)
{
    WRAPPER_NO_CONTRACT;
    return pHeap->AllocMem(S_SIZE_T(size));
}

void SecurityProperties::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // No action required
}

#ifdef FEATURE_CAS_POLICY

// static
CrstStatic SecurityPolicy::s_crstPolicyInit;

// static
bool SecurityPolicy::s_fPolicyInitialized = false;

void SecurityPolicy::InitPolicyConfig()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    GCX_PREEMP();

    CrstHolder initializePolicy(&s_crstPolicyInit);

    if (!s_fPolicyInitialized)
    {
        // Note: These buffers should be at least as big as the longest possible
        // string that will be placed into them by the code below.
        const size_t cchcache = MAX_LONGPATH + sizeof( W("defaultusersecurity.config.cch") ) / sizeof( WCHAR ) + 1;
        const size_t cchconfig = MAX_LONGPATH + sizeof( W("defaultusersecurity.config.cch") ) / sizeof( WCHAR ) + 1;
        NewArrayHolder<WCHAR> cache(new WCHAR[cchcache]);
        NewArrayHolder<WCHAR> config(new WCHAR[cchconfig]);

        HRESULT hr = SecurityConfig::GetMachineDirectory(config, MAX_LONGPATH);
        if (FAILED(hr))
            ThrowHR(hr);

        wcscat_s( config, cchconfig, W("security.config") );
        wcscpy_s( cache, cchcache, config );
        wcscat_s( cache, cchcache, W(".cch") );
        SecurityConfig::InitData( SecurityConfig::MachinePolicyLevel, config, cache );

        hr = SecurityConfig::GetMachineDirectory(config, MAX_LONGPATH);
        if (FAILED(hr))
            ThrowHR(hr);

        wcscat_s( config, cchconfig, W("enterprisesec.config") );
        wcscpy_s( cache, cchcache, config );
        wcscat_s( cache, cchcache, W(".cch") );
        SecurityConfig::InitData( SecurityConfig::EnterprisePolicyLevel, config, cache );

        BOOL result = SecurityConfig::GetUserDirectory(config, MAX_LONGPATH);
        if (result) {
            wcscat_s( config, cchconfig, W("security.config") );
            wcscpy_s( cache, cchcache, config );
            wcscat_s( cache, cchcache, W(".cch") );
            SecurityConfig::InitData( SecurityConfig::UserPolicyLevel, config, cache );
        }

        s_fPolicyInitialized = true;
    }
}
#endif // FEATURE_CAS_POLICY

void SecurityPolicy::Start()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifndef FEATURE_PAL
    // Making sure we are in sync with URLMon
    _ASSERTE(URLZONE_LOCAL_MACHINE == LocalMachine);
    _ASSERTE(URLZONE_INTRANET == Intranet);
    _ASSERTE(URLZONE_TRUSTED == Trusted);
    _ASSERTE(URLZONE_INTERNET == Internet);
    _ASSERTE(URLZONE_UNTRUSTED == Untrusted);
#endif // !FEATURE_PAL

#ifdef FEATURE_CAS_POLICY
    s_crstPolicyInit.Init(CrstSecurityPolicyInit);

    SecurityConfig::Init();

    if (Security::IsProcessWideLegacyCasPolicyEnabled())
    {
        SecurityPolicy::InitPolicyConfig();
    }

    g_pCertificateCache = new CertificateCache();
#endif // FEATURE_CAS_POLICY
}

void SecurityPolicy::Stop()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

}

#ifdef FEATURE_CAS_POLICY
void SecurityPolicy::SaveCache()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        BOOL fRet = FALSE;
        EX_TRY
        {
            // If CLR is hosted, a host can deny a thread during SetupThread call.
            if (IsShutdownSpecialThread())
            {
                SetupInternalThread();
            }
            else
            {
                SetupThread();
            }
        }
        EX_CATCH
        {
            fRet = TRUE;
        }
        EX_END_CATCH(SwallowAllExceptions);
        if (fRet)
        {
            return;
        }
    }

    SecurityConfig::SaveCacheData( SecurityConfig::MachinePolicyLevel );
    SecurityConfig::SaveCacheData( SecurityConfig::UserPolicyLevel );
    SecurityConfig::SaveCacheData( SecurityConfig::EnterprisePolicyLevel );

    SecurityConfig::Cleanup();
}
#endif

void QCALLTYPE SecurityPolicy::GetGrantedPermissions(QCall::ObjectHandleOnStack retGranted, QCall::ObjectHandleOnStack retDenied, QCall::StackCrawlMarkHandle stackmark)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    AppDomain* pDomain = NULL;

    Assembly* callerAssembly = SystemDomain::GetCallersAssembly( stackmark, &pDomain );
    _ASSERTE( callerAssembly != NULL);

    IAssemblySecurityDescriptor* pSecDesc = callerAssembly->GetSecurityDescriptor(pDomain);
    _ASSERTE( pSecDesc != NULL );

    {
        GCX_COOP();

        OBJECTREF orDenied;
        OBJECTREF orGranted = pSecDesc->GetGrantedPermissionSet(&orDenied);

        retGranted.Set(orGranted);
        retDenied.Set(orDenied);
    }

    END_QCALL;
}

#ifdef FEATURE_IMPERSONATION
FCIMPL0(DWORD, SecurityPolicy::GetImpersonationFlowMode)
{
    FCALL_CONTRACT;
    return (g_pConfig->ImpersonationMode());
}
FCIMPLEND
#endif

void SecurityPolicy::CreateSecurityException(__in_z const char *szDemandClass, DWORD dwFlags, OBJECTREF *pThrowable)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    MAKE_WIDEPTR_FROMUTF8(wszDemandClass, szDemandClass);

    MethodTable * pMT = MscorlibBinder::GetClass(CLASS__SECURITY_EXCEPTION);

#ifdef FEATURE_CAS_POLICY
    MethodTable * pMTSecPerm = MscorlibBinder::GetClass(CLASS__SECURITY_PERMISSION);

    struct _gc {
        STRINGREF strDemandClass;
        OBJECTREF secPerm;
        STRINGREF strPermState;
        OBJECTREF secPermType;
        OBJECTREF secElement;
    } gc;
    memset(&gc, 0, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    gc.strDemandClass = StringObject::NewString(wszDemandClass);
    if (gc.strDemandClass == NULL) COMPlusThrowOM();
    // Get the type seen by reflection
    gc.secPermType = pMTSecPerm->GetManagedClassObject();
    // Allocate the security exception object
    *pThrowable = AllocateObject(pMT);
    // Allocate the security permission object
    gc.secPerm = AllocateObject(pMTSecPerm);

    // Call the construtor with the correct flag
    MethodDescCallSite ctor(METHOD__SECURITY_PERMISSION__CTOR);
    ARG_SLOT arg3[2] = {
        ObjToArgSlot(gc.secPerm),
        (ARG_SLOT)dwFlags
    };
    ctor.Call(arg3);

    // Now, get the ToXml method
    MethodDescCallSite toXML(METHOD__SECURITY_PERMISSION__TOXML, &gc.secPerm);
    ARG_SLOT arg4 = ObjToArgSlot(gc.secPerm);
    gc.secElement = toXML.Call_RetOBJECTREF(&arg4);

    MethodDescCallSite toString(METHOD__SECURITY_ELEMENT__TO_STRING, &gc.secElement);
    ARG_SLOT arg5 = ObjToArgSlot(gc.secElement);
    gc.strPermState = toString.Call_RetSTRINGREF(&arg5);

    MethodDescCallSite exceptionCtor(METHOD__SECURITY_EXCEPTION__CTOR);

    ARG_SLOT arg6[4] = {
        ObjToArgSlot(*pThrowable),
        ObjToArgSlot(gc.strDemandClass),
        ObjToArgSlot(gc.secPermType),
        ObjToArgSlot(gc.strPermState),
    };
    exceptionCtor.Call(arg6);

    GCPROTECT_END();
#else // FEATURE_CAS_POLICY

	UNREFERENCED_PARAMETER(szDemandClass);
	UNREFERENCED_PARAMETER(dwFlags);

	// Allocate the security exception object
	*pThrowable = AllocateObject(pMT);
	CallDefaultConstructor(*pThrowable);

#endif // FEATURE_CAS_POLICY
}

DECLSPEC_NORETURN void SecurityPolicy::ThrowSecurityException(__in_z const char *szDemandClass, DWORD dwFlags)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    GCX_COOP();

    struct _gc {
        OBJECTREF throwable;
    } gc;
    memset(&gc, 0, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    CreateSecurityException(szDemandClass, dwFlags, &gc.throwable);
    COMPlusThrow(gc.throwable);

    GCPROTECT_END();
}

#ifdef FEATURE_CAS_POLICY
//-----------------------------------------------------------------------------------------
//
// Fire an ETW event to indicate that an evidence object has been generated for an assembly
//
// Arguments:
//    type    - Type of evidence that was generated
//    pPEFile - PEFile for the assembly the evidence was for
//

// static
void SecurityPolicy::TraceEvidenceGeneration(EvidenceType type, PEFile *pPEFile)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pPEFile));
        PRECONDITION(type >= kAssemblySupplied && type <= kZone);
    }
    CONTRACTL_END;

    const SString& strPath = pPEFile->GetILimage()->GetPath();
    FireEtwEvidenceGenerated(type,
                             GetThread()->GetDomain()->GetId().m_dwId,
                             strPath.IsEmpty() ? W("") : strPath.GetUnicode(),
                             GetClrInstanceId());
}

// Called if CAS policy is not enabled, but we either have a host or a simple sandbox domain which will
// determine the grant set of some evidence.
OBJECTREF SecurityPolicy::ResolveGrantSet(OBJECTREF evidence, DWORD *pdwSpecialFlags, BOOL fCheckExecutionPermission)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(!GetAppDomain()->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());
        PRECONDITION(CheckPointer(pdwSpecialFlags));
    }
    CONTRACTL_END;

    struct
    {
        OBJECTREF evidence;
        OBJECTREF grantSet;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    gc.evidence = evidence;

    GCPROTECT_BEGIN(gc);

    MethodDescCallSite resolve(METHOD__SECURITY_ENGINE__RESOLVE_GRANT_SET);
    
    ARG_SLOT args[3];
    args[0] = ObjToArgSlot(gc.evidence);
    args[1] = PtrToArgSlot(pdwSpecialFlags);
    args[2] = BoolToArgSlot(fCheckExecutionPermission);

    gc.grantSet = resolve.Call_RetOBJECTREF(args);

    GCPROTECT_END();

    return gc.grantSet;
}

// Resolve legacy CAS policy
OBJECTREF SecurityPolicy::ResolveCasPolicy(OBJECTREF evidence,
                                           OBJECTREF reqdPset,
                                           OBJECTREF optPset,
                                           OBJECTREF denyPset,
                                           OBJECTREF* grantdenied,
                                           DWORD* dwSpecialFlags,
                                           BOOL checkExecutionPermission)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(GetAppDomain()->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());
        PRECONDITION(SecurityPolicy::s_fPolicyInitialized);
        PRECONDITION(CheckPointer(dwSpecialFlags));
    } CONTRACTL_END;


    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    // If we got here, then we are going to do at least one security
    // check. Make sure security is initialized.

    struct _gc {
        OBJECTREF reqdPset;         // Required Requested Permissions
        OBJECTREF optPset;          // Optional Requested Permissions
        OBJECTREF denyPset;         // Denied Permissions
        OBJECTREF evidence;         // Object containing evidence
        OBJECTREF refRetVal;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.evidence = evidence;
    gc.reqdPset = reqdPset;
    gc.denyPset = denyPset;
    gc.optPset = optPset;

    GCPROTECT_BEGIN(gc);

    MethodDescCallSite resolvePolicy(METHOD__SECURITY_MANAGER__RESOLVE_CAS_POLICY);

    ARG_SLOT args[7];
    args[0] = ObjToArgSlot(gc.evidence);
    args[1] = ObjToArgSlot(gc.reqdPset);
    args[2] = ObjToArgSlot(gc.optPset);
    args[3] = ObjToArgSlot(gc.denyPset);
    args[4] = PtrToArgSlot(grantdenied);
    args[5] = PtrToArgSlot(dwSpecialFlags);
    args[6] = BoolToArgSlot(checkExecutionPermission);

    {
        // Elevate thread's allowed loading level.  This can cause load failures if assemblies loaded from this point on require
        // any assemblies currently being loaded.
       OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
        // call policy resolution routine in managed code
        gc.refRetVal = resolvePolicy.Call_RetOBJECTREF(args);
    }

    GCPROTECT_END();
    return gc.refRetVal;
}
#endif // FEATURE_CAS_POLICY

#endif // CROSSGEN_COMPILE

BOOL SecurityPolicy::CanSkipVerification(DomainAssembly * pAssembly)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pAssembly));
    } CONTRACTL_END;

    BOOL canSkipVerification = TRUE;
    if (!pAssembly->IsSystem())
    {
        AssemblySecurityDescriptor *pSec;
        {
            GCX_COOP();
            pSec = static_cast<AssemblySecurityDescriptor*>(pAssembly->GetSecurityDescriptor());
        }
        _ASSERTE(pSec);
        if (pSec)
        {
            canSkipVerification = pSec->CanSkipVerification();
        }
        else
        {
            canSkipVerification = FALSE;
        }
    }

    return canSkipVerification;
}

BOOL SecurityPolicy::CanCallUnmanagedCode(Module *pModule)
{
    CONTRACTL {
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    SharedSecurityDescriptor *pSharedSecDesc = static_cast<SharedSecurityDescriptor*>(pModule->GetAssembly()->GetSharedSecurityDescriptor());
    if (pSharedSecDesc)
        return pSharedSecDesc->CanCallUnmanagedCode();

    AssemblySecurityDescriptor *pSec = static_cast<AssemblySecurityDescriptor*>(pModule->GetSecurityDescriptor());
    _ASSERTE(pSec);
    return pSec->CanCallUnmanagedCode();
}

#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_CAS_POLICY
SecZone QCALLTYPE SecurityPolicy::CreateFromUrl(LPCWSTR wszUrl)
{
    QCALL_CONTRACT;

    SecZone dwZone = NoZone;

    BEGIN_QCALL;

    if (wszUrl != NULL)
    {
        dwZone = SecurityPolicy::MapUrlToZone(wszUrl);
    }

    END_QCALL;

    return dwZone;
}

HRESULT
GetSecurityPolicyRegKey(
    __out WCHAR **ppszSecurityPolicy)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END;
    
    DWORD dwLen = 0;

    HRESULT hr = g_pCLRRuntime->GetVersionString(NULL, &dwLen);
    if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        return hr;
    
    size_t bufSize = _countof(FRAMEWORK_REGISTRY_KEY_W) + 1 + dwLen + _countof(KEY_COM_SECURITY_POLICY);
    NewArrayHolder<WCHAR> key(new(nothrow) WCHAR[bufSize]);
    if (key == NULL)
        return E_OUTOFMEMORY;
    wcscpy_s(key, bufSize, FRAMEWORK_REGISTRY_KEY_W W("\\"));

    hr = g_pCLRRuntime->GetVersionString(key + NumItems(FRAMEWORK_REGISTRY_KEY_W), &dwLen);
    if (FAILED(hr))
        return hr;
    
    size_t offset = _countof(FRAMEWORK_REGISTRY_KEY_W)+dwLen-1;
    wcscpy_s(key + offset, bufSize - offset, KEY_COM_SECURITY_POLICY);
    key.SuppressRelease();
    *ppszSecurityPolicy = key;
    return S_OK;
} // GetSecurityPolicyRegKey

HRESULT SecurityPolicy::ApplyCustomZoneOverride(SecZone *pdwZone)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(*pdwZone >= NumZones); 
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END;
    
    NewArrayHolder<WCHAR> key(NULL);
    HRESULT hr = GetSecurityPolicyRegKey(&key);
    if (FAILED(hr))
        return hr;
    if (REGUTIL::GetLong(KEY_COM_SECURITY_ZONEOVERRIDE, 0, key, HKEY_POLICY_ROOT) == 1)
        *pdwZone=Internet;
    return S_OK;
} // ApplyCustomZoneOverride

//---------------------------------------------------------------------------------------
//
// Determine which security zone a URL belongs to
//
// Arguments:
//    wszUrl - URL to get zone information about
//
// Return Value:
//    Security zone the URL belongs to
//
// Notes:
//    If the runtime cannot map the URL, we'll return NoZone. A mapping to a zone that the VM doesn't
//    know about will cause us to check the TreatCustomZonesAsInternetZone registry key and potentially
//    map it back to the Internet zone.
//

// static
SecZone SecurityPolicy::MapUrlToZone(__in_z LPCWSTR wszUrl)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(wszUrl != NULL);
    }
    CONTRACTL_END;

    SecZone dwZone = NoZone;

    ReleaseHolder<IInternetSecurityManager> securityManager = NULL;
    HRESULT hr = CoInternetCreateSecurityManager(NULL, &securityManager, 0);

    if (SUCCEEDED(hr))
    {
        _ASSERTE(sizeof(SecZone) == sizeof(DWORD));
        hr = securityManager->MapUrlToZone(wszUrl, reinterpret_cast<DWORD *>(&dwZone), 0);

        if (SUCCEEDED(hr))
        {
            // if this is a custom zone, see if the user wants us to map it back to the Internet zone
            if (dwZone >= NumZones)
            {
                SecZone dwMappedZone = dwZone;
                hr = ApplyCustomZoneOverride(&dwMappedZone);
                if (SUCCEEDED(hr))
                {
                    dwZone = dwMappedZone;
                }
            }
        }
        else
        {
            dwZone = NoZone;
        }
    }

    return dwZone;
}
#endif //FEATURE_CAS_POLICY

BOOL QCALLTYPE SecurityPolicy::IsLocalDrive(LPCWSTR wszPath)
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

#ifndef FEATURE_PAL
    BEGIN_QCALL;

    WCHAR rootPath[4];
    ZeroMemory( rootPath, sizeof( rootPath ) );

    rootPath[0] = wszPath[0];
    wcscat_s( rootPath, COUNTOF(rootPath), W(":\\") );

    UINT driveType = WszGetDriveType( rootPath );
    retVal =
       (driveType == DRIVE_REMOVABLE ||
        driveType == DRIVE_FIXED ||
        driveType == DRIVE_CDROM ||
        driveType == DRIVE_RAMDISK);

    END_QCALL;

#else // !FEATURE_PAL
    retVal = TRUE;
#endif // !FEATURE_PAL

    return retVal;
}

void QCALLTYPE SecurityPolicy::_GetLongPathName(LPCWSTR wszPath, QCall::StringHandleOnStack retLongPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

#if !defined(PLATFORM_UNIX)
    PathString wszBuffer;
                
    if (SecurityPolicy::GetLongPathNameHelper( wszPath, wszBuffer ) != 0)
    {
        retLongPath.Set( wszBuffer.GetUnicode() );
    }
#endif // !PLATFORM_UNIX

    END_QCALL;
}

#if !defined(PLATFORM_UNIX)
size_t GetLongPathNameHelperthatThrows(const WCHAR* wszShortPath, SString& wszBuffer)
{
    CONTRACTL{
        THROWS;
    GC_NOTRIGGER;
    MODE_ANY;
    } CONTRACTL_END;

    DWORD size = WszGetLongPathName(wszShortPath, wszBuffer);

    if (size == 0)
    {
        // We have to deal with files that do not exist so just
        // because GetLongPathName doesn't give us anything doesn't
        // mean that we can give up.  We iterate through the input
        // trying GetLongPathName on every subdirectory until
        // it succeeds or we run out of string.

        size_t len = wcslen(wszShortPath);
        NewArrayHolder<WCHAR> wszIntermediateBuffer = new (nothrow) WCHAR[len + 1];

        if (wszIntermediateBuffer == NULL)
        {
            return 0;
        }

        wcscpy_s(wszIntermediateBuffer, len + 1, wszShortPath);

        size_t index = len;

        do
        {
            while (index > 0 && (wszIntermediateBuffer[index - 1] != W('\\') && wszIntermediateBuffer[index - 1] != W('/')))
                --index;

            if (index == 0)
                break;

#ifdef _PREFAST_
#pragma prefast(push)
#pragma prefast(disable:26001, "suppress prefast warning about underflow by doing index-1 which is checked above.")
#endif // _PREFAST_

            wszIntermediateBuffer[index - 1] = W('\0');

#ifdef _PREFAST_
#pragma prefast(pop)
#endif

            size = WszGetLongPathName(wszIntermediateBuffer, wszBuffer);

            if (size != 0)
            {

                int sizeBuffer = wszBuffer.GetCount();

                if (wszBuffer[sizeBuffer - 1] != W('\\') && wszBuffer[sizeBuffer - 1] != W('/'))
                    wszBuffer.Append(W("\\"));

                wszBuffer.Append(&wszIntermediateBuffer[index]);


                return (DWORD)wszBuffer.GetCount();

            }
        } while (true);

        return 0;
    }
    else
    {
        return (DWORD)wszBuffer.GetCount();
    }
}
size_t SecurityPolicy::GetLongPathNameHelper(const WCHAR* wszShortPath, SString& wszBuffer)
{
    CONTRACTL{
        NOTHROW;
    GC_NOTRIGGER;
    MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    size_t retval = 0;

    EX_TRY
    {
        retval = GetLongPathNameHelperthatThrows(wszShortPath,wszBuffer);
    }
    EX_CATCH_HRESULT(hr);

    if (hr != S_OK)
    {
        retval = 0;
    }

    return retval;
}

#endif // !PLATFORM_UNIX

void QCALLTYPE SecurityPolicy::GetDeviceName(LPCWSTR wszDriveLetter, QCall::StringHandleOnStack retDeviceName)
{
    QCALL_CONTRACT;

#if !defined(FEATURE_CORECLR)
    BEGIN_QCALL;

    WCHAR networkName[MAX_LONGPATH];
    DWORD networkNameSize = MAX_LONGPATH;
    ZeroMemory( networkName, sizeof( networkName ) );

    UINT driveType = WszGetDriveType( wszDriveLetter );
    if (driveType == DRIVE_REMOVABLE ||
        driveType == DRIVE_FIXED ||
        driveType == DRIVE_CDROM ||
        driveType == DRIVE_RAMDISK)
    {
        retDeviceName.Set( wszDriveLetter );
        goto lExit;
    }

    if (WszWNetGetConnection(wszDriveLetter, networkName, &networkNameSize) != NO_ERROR)
    {
        goto lExit;
    }

    retDeviceName.Set( networkName );

lExit: ;

    END_QCALL;
#endif // !FEATURE_CORECLR
}

#ifdef FEATURE_CAS_POLICY

//
// Fire the ETW event that signals that a specific type of evidence has been created
// 
// Arguments:
//    pPEFile - PEFile the evidence was generated for
//    type    - type of evidence generated
//

// static
void QCALLTYPE SecurityPolicy::FireEvidenceGeneratedEvent(PEFile *pPEFile,
                                                          EvidenceType type)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pPEFile));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    TraceEvidenceGeneration(type, pPEFile);

    END_QCALL;
}

// static

void QCALLTYPE SecurityPolicy::GetEvidence(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retEvidence)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    IAssemblySecurityDescriptor *pSecDesc = pAssembly->GetSecurityDescriptor();

    _ASSERTE(pSecDesc->GetDomain() == GetAppDomain());

    GCX_COOP();
    if (pSecDesc->IsEvidenceComputed())
        retEvidence.Set(pSecDesc->GetAdditionalEvidence());
        else
        retEvidence.Set(pSecDesc->GetEvidence());
    
    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Determine if an evidence collection has a delay generated strong name evidence object
// which was used during the process of demand evaluation.
//
// Arguments:
//    orEvidence - evidence collection to examine
//
// Return Value:
//    true if orEvidence contains unverified strong name evidence which has been used to generate a grant,
//    false if orEvidence does not contain strong name evidence or that evidence was verified / not used
//

// static
BOOL SecurityPolicy::WasStrongNameEvidenceUsed(OBJECTREF orEvidence)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // If we don't have any evidence, then there isn't any strong name evidence, and therefore it couldn't
    // have been used.
    if (orEvidence == NULL)
    {
        return FALSE;
    }

    BOOL fStrongNameEvidenceWasUsed = FALSE;

    GCPROTECT_BEGIN(orEvidence);

    MethodDescCallSite wasSnEvidenceUsed(METHOD__EVIDENCE__WAS_STRONGNAME_EVIDENCE_USED);

    ARG_SLOT args[] = { ObjToArgSlot(orEvidence) };
    fStrongNameEvidenceWasUsed = !!wasSnEvidenceUsed.Call_RetBool(args);
    
    GCPROTECT_END();

    return fStrongNameEvidenceWasUsed;
}
#endif // FEATURE_CAS_POLICY

FCIMPL0(void, SecurityPolicy::IncrementOverridesCount)
{
    FCALL_CONTRACT;

    Thread *pThread = GetThread();
    pThread->IncrementOverridesCount();
}
FCIMPLEND

FCIMPL0(void, SecurityPolicy::DecrementOverridesCount)
{
    FCALL_CONTRACT;

    Thread *pThread = GetThread();
    pThread->DecrementOverridesCount();
}
FCIMPLEND

FCIMPL0(void, SecurityPolicy::IncrementAssertCount)
{
    FCALL_CONTRACT;

    Thread *pThread = GetThread();
    pThread->IncrementAssertCount();
}
FCIMPLEND

FCIMPL0(void, SecurityPolicy::DecrementAssertCount)
{
    FCALL_CONTRACT;

    Thread *pThread = GetThread();
    pThread->DecrementAssertCount();
}
FCIMPLEND

#ifdef FEATURE_CAS_POLICY
//
// Evidence QCalls
// 

//---------------------------------------------------------------------------------------
//
// Get the assembly level permission requests
//
// Arguments:
//    pAssembly              - Assembly to get the declarative security of
//    retMinimumPermissions  - [out] RequestMinimum set of the assembly
//    retOptionalPermissions - [out] RequestOptional set of the assembly
//    retRefusedPermissions  - [out] RequestRefuse set of the assembly
//

// static
void QCALLTYPE SecurityPolicy::GetAssemblyPermissionRequests(QCall::AssemblyHandle pAssembly,
                                                             QCall::ObjectHandleOnStack retMinimumPermissions,
                                                             QCall::ObjectHandleOnStack retOptionalPermissions,
                                                             QCall::ObjectHandleOnStack retRefusedPermissions)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;


    TraceEvidenceGeneration(kPermissionRequest, pAssembly->GetFile());
    AssemblySecurityDescriptor *pSecurityDescriptor = static_cast<AssemblySecurityDescriptor*>(pAssembly->GetSecurityDescriptor());
    
    _ASSERTE(pSecurityDescriptor->GetDomain()->GetSecurityDescriptor()->IsLegacyCasPolicyEnabled());

    struct
    {
        OBJECTREF objMinimumPermissions;
        OBJECTREF objOptionalPermissions;
        OBJECTREF objRefusedPermissions;
    }
    gc;
    ZeroMemory(&gc, sizeof(gc));

    GCX_COOP();
    GCPROTECT_BEGIN(gc);

    gc.objMinimumPermissions = pSecurityDescriptor->GetRequestedPermissionSet(&gc.objOptionalPermissions,
                                                                              &gc.objRefusedPermissions);

    retMinimumPermissions.Set(gc.objMinimumPermissions);
    retOptionalPermissions.Set(gc.objOptionalPermissions);
    retRefusedPermissions.Set(gc.objRefusedPermissions);

    GCPROTECT_END();

    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Get the serialized evidence stream from an assembly
//
// Arguments:
//    pPEFile               - PEFile to load the evidence stream from
//    retSerializedEvidence - [out] contents of the serialized evidence
//

// static
void QCALLTYPE SecurityPolicy::GetAssemblySuppliedEvidence(PEFile *pPEFile,
                                                           QCall::ObjectHandleOnStack retSerializedEvidence)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pPEFile));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    DWORD cbResource;
    BYTE *pbResource;

    // Load the resource from the PE file. We do not need to free this memory, since we're getting a direct
    // pointer into the PE contents rather than a buffer.
    TraceEvidenceGeneration(kAssemblySupplied, pPEFile);
    BOOL fFoundSerializedEvidence = pPEFile->GetResource("Security.Evidence",
                                                         &cbResource,
                                                         &pbResource,
                                                         NULL,
                                                         NULL,
                                                         NULL,
                                                         NULL,
                                                         FALSE,
                                                         TRUE,
                                                         NULL,
                                                         NULL);

    if (fFoundSerializedEvidence)
    {
        retSerializedEvidence.SetByteArray(pbResource, cbResource);
    }

    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Get the zone and URL that the PEFile was loaded from
//
// Arguments:
//    pPEFile  - PEFile to load the evidence stream from
//    pZone    - [out] SecurityZone the file was loaded from
//    retUrl   - [out] URL the file was loaded from

// static
void QCALLTYPE SecurityPolicy::GetLocationEvidence(PEFile *pPEFile,
                                                   SecZone *pZone,
                                                   QCall::StringHandleOnStack retUrl)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pPEFile));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    StackSString ssCodeBase;
    BYTE pbUniqueID[MAX_SIZE_SECURITY_ID];
    DWORD cbUniqueID = COUNTOF(pbUniqueID);

    // The location information is used to create Site, Url, and Zone evidence so fire all three events
    TraceEvidenceGeneration(kSite, pPEFile);
    TraceEvidenceGeneration(kUrl, pPEFile);
    TraceEvidenceGeneration(kZone, pPEFile);

    pPEFile->GetSecurityIdentity(ssCodeBase, pZone, 0, pbUniqueID, &cbUniqueID);
    
    retUrl.Set(ssCodeBase);
    
    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Get the X.509 certificate that the PE file's Authenticode signature was created with
//
// Arguments:
//    pPEFile        - PEFile to load the evidence stream from
//    retCertificate - [out] certificate that signed the file

// static
void QCALLTYPE SecurityPolicy::GetPublisherCertificate(PEFile *pPEFile,
                                                       QCall::ObjectHandleOnStack retCertificate)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pPEFile));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    TraceEvidenceGeneration(kPublisher, pPEFile);
    COR_TRUST *pAuthenticodeSignature = pPEFile->GetAuthenticodeSignature();
    if (pAuthenticodeSignature != NULL && pAuthenticodeSignature->pbSigner != NULL)
    {
        retCertificate.SetByteArray(pAuthenticodeSignature->pbSigner, pAuthenticodeSignature->cbSigner);
    }

    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Get the components of an assembly's strong name to generate strong name evidence with
//
// Arguments:
//    pAssembly        - assembly to get the strong name of
//    retPublicKeyBlob - [out] public component of the key the assembly is signed with
//    retSimpleName    - [out] simple name of the file
//    piMajorVersion   - [out] major version
//    piMinorVersion   - [out] minor version
//    piBuild          - [out] build
//    piRevision       - [out] revision
//
// Notes:
//    retPublicKeyBlob will be null for a simply named assembly
//

// static
void QCALLTYPE SecurityPolicy::GetStrongNameInformation(QCall::AssemblyHandle pAssembly,
                                                        QCall::ObjectHandleOnStack retPublicKeyBlob,
                                                        QCall::StringHandleOnStack retSimpleName,
                                                        USHORT *piMajorVersion,
                                                        USHORT *piMinorVersion,
                                                        USHORT *piBuild,
                                                        USHORT *piRevision)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(piMajorVersion));
        PRECONDITION(CheckPointer(piMinorVersion));
        PRECONDITION(CheckPointer(piBuild));
        PRECONDITION(CheckPointer(piRevision));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    PEAssembly *pPEAssembly = pAssembly->GetFile();
    TraceEvidenceGeneration(kStrongName, pPEAssembly);

    DWORD cbPublicKey;
    const BYTE *pbPublicKey = reinterpret_cast<const BYTE*>(pPEAssembly->GetPublicKey(&cbPublicKey));

    if (pbPublicKey != NULL && cbPublicKey > 0)
    {
        pPEAssembly->GetVersion(piMajorVersion, piMinorVersion, piBuild, piRevision);
        retPublicKeyBlob.SetByteArray(pbPublicKey, cbPublicKey);
        retSimpleName.Set(pPEAssembly->GetSimpleName());
    }
    else
    {
        GCX_COOP();
        retPublicKeyBlob.Set(NULL);
    }

    END_QCALL;
}

#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_FUSION 
static void GetFusionNameFromAssemblyQualifiedTypeName(LPCWSTR pAssemblyQualifedTypeName, IAssemblyName ** ppFusionName)
{
    STANDARD_VM_CONTRACT;

    StackSString ssAssemblyQualifedTypeName(pAssemblyQualifedTypeName);
    StackSString ssAssemblyName;

    SString::Iterator iter = ssAssemblyQualifedTypeName.Begin();

    if (ssAssemblyQualifedTypeName.Find( iter, ',' ))
    {
    iter++;
    while (*iter == ' ' )
        iter++;

    ssAssemblyName.Set( ssAssemblyQualifedTypeName,
                        iter,
                        ssAssemblyQualifedTypeName.End() );
}

    StackScratchBuffer sBuffer;
    AssemblySpec spec;
    spec.Init(ssAssemblyName.GetANSI(sBuffer));

    IfFailThrow(spec.CreateFusionName(ppFusionName));
}
#endif // FEATURE_FUSION 

BOOL QCALLTYPE SecurityPolicy::IsSameType(LPCWSTR pLeft, LPCWSTR pRight)
{
    QCALL_CONTRACT;

    BOOL bEqual = FALSE;

    BEGIN_QCALL;

// @telesto: Is this #ifdef-#else-#endif required anymore? Used to be needed when security was bypassing
// loader and accessing Fusion interfaces. Seems like that's been fixed to use GetFusionNameFrom...
#ifdef FEATURE_FUSION 

    AppDomain* pDomain = GetAppDomain();
    IApplicationContext* pAppCtx = pDomain->GetFusionContext();

    _ASSERTE( pAppCtx != NULL && "Fusion context not setup yet" );

    SafeComHolderPreemp<IAssemblyName> pAssemblyNameLeft;
    SafeComHolderPreemp<IAssemblyName> pAssemblyNameRight;
    
    GetFusionNameFromAssemblyQualifiedTypeName(pLeft, &pAssemblyNameLeft);
    GetFusionNameFromAssemblyQualifiedTypeName(pRight, &pAssemblyNameRight);

    SafeComHolderPreemp<IAssemblyName> pAssemblyNamePostPolicyLeft;
    SafeComHolderPreemp<IAssemblyName> pAssemblyNamePostPolicyRight;

    if (FAILED(PreBindAssembly(pAppCtx, pAssemblyNameLeft,  NULL, &pAssemblyNamePostPolicyLeft,  NULL)) ||
        FAILED(PreBindAssembly(pAppCtx, pAssemblyNameRight, NULL, &pAssemblyNamePostPolicyRight, NULL)))
    {
        // version-agnostic comparison.
        bEqual = pAssemblyNameLeft->IsEqual(pAssemblyNameRight, ASM_CMPF_NAME | ASM_CMPF_PUBLIC_KEY_TOKEN | ASM_CMPF_CULTURE) == S_OK;
    }
    else
    {
        // version-agnostic comparison.
        bEqual = pAssemblyNamePostPolicyLeft->IsEqual(pAssemblyNamePostPolicyRight, ASM_CMPF_NAME | ASM_CMPF_PUBLIC_KEY_TOKEN | ASM_CMPF_CULTURE) == S_OK;
    }
#else // FEATURE_FUSION
    bEqual=TRUE;
#endif // FEATURE_FUSION

    END_QCALL;

    return bEqual;
}

FCIMPL1(FC_BOOL_RET, SecurityPolicy::SetThreadSecurity, CLR_BOOL fThreadSecurity)
{
    FCALL_CONTRACT;

    Thread* pThread = GetThread();
    BOOL inProgress = pThread->IsSecurityStackwalkInProgess();
    pThread->SetSecurityStackwalkInProgress(fThreadSecurity);
    FC_RETURN_BOOL(inProgress);
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, SecurityPolicy::IsDefaultThreadSecurityInfo)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(SecurityStackWalk::HasFlagsOrFullyTrusted(0));
}
FCIMPLEND

#endif // CROSSGEN_COMPILE
