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


	UNREFERENCED_PARAMETER(szDemandClass);
	UNREFERENCED_PARAMETER(dwFlags);

	// Allocate the security exception object
	*pThrowable = AllocateObject(pMT);
	CallDefaultConstructor(*pThrowable);

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

}


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



BOOL QCALLTYPE SecurityPolicy::IsSameType(LPCWSTR pLeft, LPCWSTR pRight)
{
    QCALL_CONTRACT;

    BOOL bEqual = FALSE;

    BEGIN_QCALL;

// @telesto: Is this #ifdef-#else-#endif required anymore? Used to be needed when security was bypassing
// loader and accessing Fusion interfaces. Seems like that's been fixed to use GetFusionNameFrom...
    bEqual=TRUE;

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
