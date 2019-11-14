// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: SecurityWrapper.cpp
//

//
// Wrapper around Win32 Security functions
//
//*****************************************************************************

#include "stdafx.h"

#include "securitywrapper.h"
#include "ex.h"
#include "holder.h"


// For GetSidFromProcess*
#include <tlhelp32.h>
#include "wtsapi32.h"


//-----------------------------------------------------------------------------
// Constructor for Sid wrapper class.
// pSid - OS sid to wrap
//-----------------------------------------------------------------------------
Sid::Sid(PSID pSid)
{
    _ASSERTE(pSid != NULL);
    m_pSid = pSid;
}

//-----------------------------------------------------------------------------
// Aesthetic wrapper for Sid equality
//-----------------------------------------------------------------------------
bool Sid::Equals(PSID a, PSID b)
{
    return EqualSid(a, b) != 0;
}

//-----------------------------------------------------------------------------
// Ctor for SidBuffer class
//-----------------------------------------------------------------------------
SidBuffer::SidBuffer()
{
    m_pBuffer = NULL;
}

//-----------------------------------------------------------------------------
// Dtor for SidBuffer class.
//-----------------------------------------------------------------------------
SidBuffer::~SidBuffer()
{
    delete [] m_pBuffer;
}

//-----------------------------------------------------------------------------
// Get the underlying sid
// Caller assumes SidBuffer has been initialized.
//-----------------------------------------------------------------------------
Sid SidBuffer::GetSid()
{
    _ASSERTE(m_pBuffer != NULL);
    Sid s((PSID) m_pBuffer);
    return s;
}

// ----------------------------------------------------------------------------
// Used by GetSidFromProcessWorker to determine which SID from the
// process token to use when initializing the SID
enum SidType
{
    // Use TokenOwner: the default owner SID used for newly created objects
    kOwnerSid,

    // Use TokenUser: the user account from the token
    kUserSid,
};

// ----------------------------------------------------------------------------
// GetSidFromProcessWorker
//
// Description:
//    Internal helper.  Gets the SID for the given process and given sid type
//
// Arguments:
//    * dwProcessId - [in] Process to get SID from
//    * sidType - [in] Type of sid to get (owner or user)
//    * ppSid - [out] SID found.  Caller responsible for deleting this memory.
//
// Return Value:
//    HRESULT indicating success / failure.
//
// Notes:
//    * Caller owns deleting (*ppSid) when done with the SID
//

HRESULT GetSidFromProcessWorker(DWORD dwProcessId, SidType sidType, PSID *ppSid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT                     hr = S_OK;
    TOKEN_USER                  *pTokUser = NULL;
    HANDLE                      hProc = INVALID_HANDLE_VALUE;
    HANDLE                      hToken = INVALID_HANDLE_VALUE;
    DWORD                       dwRetLength = 0;
    LPVOID                      pvTokenInfo = NULL;
    TOKEN_INFORMATION_CLASS     tokenInfoClass;
    PSID                        pSidFromTokenInfo = NULL;
    DWORD                       cbSid;
    PSID                        pSid = NULL;

    LOG((LF_CORDB, LL_INFO10000,
         "SecurityUtil::GetSidFromProcess: 0x%08x\n",
         dwProcessId));

    _ASSERTE(ppSid);
    *ppSid = NULL;

    _ASSERTE((sidType == kOwnerSid) || (sidType == kUserSid));
    tokenInfoClass = (sidType == kOwnerSid) ? TokenOwner : TokenUser;

    hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, dwProcessId);

    if (hProc == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }
    if (!OpenProcessToken(hProc, TOKEN_QUERY, &hToken))
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }

    // figure out the length
    GetTokenInformation(hToken, tokenInfoClass, NULL, 0, &dwRetLength);
    _ASSERTE(dwRetLength);

    pvTokenInfo = new (nothrow) BYTE[dwRetLength];
    if (pvTokenInfo == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }

    if (!GetTokenInformation(hToken, tokenInfoClass, pvTokenInfo, dwRetLength, &dwRetLength))
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }

    // Copy over the SID
    pSidFromTokenInfo =
        (sidType == kOwnerSid) ?
            ((TOKEN_OWNER *) pvTokenInfo)->Owner :
            ((TOKEN_USER *) pvTokenInfo)->User.Sid;
    cbSid = GetLengthSid(pSidFromTokenInfo);
    pSid = new (nothrow) BYTE[cbSid];
    if (pSid == NULL)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        if (!CopySid(cbSid, pSid, pSidFromTokenInfo))
        {
            hr = HRESULT_FROM_GetLastError();
            goto exit;
        }
    }

    *ppSid = pSid;
    pSid = NULL;

exit:
    if (hToken != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hToken);
    }
    if (hProc != INVALID_HANDLE_VALUE)
    {
        // clean up
        CloseHandle(hProc);
    }
    if (pvTokenInfo)
    {
        delete [] (reinterpret_cast<BYTE*>(pvTokenInfo));
    }

    if (pSid)
    {
        delete [] (reinterpret_cast<BYTE*>(pSid));
    }

    LOG((LF_CORDB, LL_INFO10000,
         "SecurityUtil::GetSidFromProcess return hr : 0x%08x\n",
         hr));

    return hr;
}

#ifndef FEATURE_CORESYSTEM
//-----------------------------------------------------------------------------
// get the sid of a given process id using WTSEnumerateProcesses
// @todo: Make this function fail when WTSEnumerateProcesses is not available
// Or is it always available on all of our platform?
//
// Caller remember to call delete on *ppSid
//-----------------------------------------------------------------------------
HRESULT GetSidFromProcessEXWorker(DWORD dwProcessId, PSID *ppSid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(ppSid));
    }
    CONTRACTL_END;

    HRESULT            hr = S_OK;
    PWTS_PROCESS_INFOW rgProcessInfo = NULL;
    DWORD              dwNumProcesses;
    DWORD              iProc;
    DWORD              cbSid;
    PSID               pSid = NULL;

    LOG((LF_CORDB, LL_INFO10000,
         "SecurityUtil::GetSidFromProcessEx: 0x%08x\n",
         dwProcessId));


    *ppSid = NULL;
    if (!WTSEnumerateProcessesW(WTS_CURRENT_SERVER_HANDLE,   // use local server
                                0,              // Reserved must be zero
                                1,              // version must be 1
                                &rgProcessInfo, // Receives pointer to process list
                                &dwNumProcesses))
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }

    for (iProc = 0; iProc < dwNumProcesses; iProc++)
    {

        if (rgProcessInfo[iProc].ProcessId == dwProcessId)
        {
            if (rgProcessInfo[iProc].pUserSid == NULL)
            {
                LOG((LF_CORDB, LL_INFO10000,
                     "SecurityUtil::GetSidFromProcessEx is not able to retrieve SID\n"));

                // if there is no Sid for the user, don't call GetLengthSid.
                // It will crash! It is ok to return E_FAIL as caller will ignore it.
                hr = E_FAIL;
                goto exit;
            }
            cbSid = GetLengthSid(rgProcessInfo[iProc].pUserSid);
            pSid = new (nothrow) BYTE[cbSid];
            if (pSid == NULL)
            {
                hr = E_OUTOFMEMORY;
            }
            else
            {
                if (!CopySid(cbSid, pSid, rgProcessInfo[iProc].pUserSid))
                {
                    hr = HRESULT_FROM_GetLastError();
                }
                else
                {
                    // We are done. Go to exit
                    hr = S_OK;
                }
            }

            // we already find a match. Even if we fail from memory allocation of CopySid, still
            // goto exit.
            goto exit;
        }
    }

    // Walk the whole list and cannot find the matching PID
    // Find a better error code.
    hr = E_FAIL;

exit:

    if (rgProcessInfo)
    {
        WTSFreeMemory(rgProcessInfo);
    }

    if (FAILED(hr) && pSid)
    {
        delete [] (reinterpret_cast<BYTE*>(pSid));
    }

    if (SUCCEEDED(hr))
    {
        _ASSERTE(pSid);
        *ppSid = pSid;
    }
    LOG((LF_CORDB, LL_INFO10000,
         "SecurityUtil::GetSidFromProcessEx return hr : 0x%08x\n",
         hr));


    return hr;
}
#endif // !FEATURE_CORESYSTEM

//-----------------------------------------------------------------------------
// The functions below initialize this SidBuffer instance with a Sid from
// the token of the specified process.  The first pair use the OWNER sid from
// the process token if possible; else use the term serv API to find the
// USER sid from the process token.  This seems a little inconsistent, but
// remains this way for backward compatibility.  The second pair consistently
// use the USER sid (never the OWNER).
//
// While the USER and OWNER sid are often the same, they are not always the
// same.  For example, running a process on win2k3 server as a member of the
// local admin group causes the USER sid to be the logged-on user, and the
// OWNER sid to be the local admins group.  At least, that's how it was on
// Monday.  Expect this to change randomly at unexpected times, as most
// security-related behavior does.
//-----------------------------------------------------------------------------


// ----------------------------------------------------------------------------
// SidBuffer::InitFromProcessNoThrow
//
// Description:
//    Initialize this SidBuffer instance with a Sid from the token of the specified
//    process. Use the OWNER sid from the process token if possible; else use the term
//    serv API to find the USER sid from the process token. This seems a little
//    inconsistent, but remains this way for backward compatibility.
//
// Arguments:
//    * pid - Process ID from which to grab the SID
//
// Return Value:
//    HRESULT indicating success / failure
//

HRESULT SidBuffer::InitFromProcessNoThrow(DWORD pid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_pBuffer == NULL);
    HRESULT hr = GetSidFromProcessWorker(pid, kOwnerSid, (PSID *) &m_pBuffer);
#ifndef FEATURE_CORESYSTEM
    if (FAILED(hr))
    {
        hr = GetSidFromProcessEXWorker(pid, (PSID *) &m_pBuffer);
    }
#endif // !FEATURE_CORESYSTEM
    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(m_pBuffer != NULL);
    return S_OK;
}

// See code:SidBuffer::InitFromProcessNoThrow.  Throws if there's an error.
void SidBuffer::InitFromProcess(DWORD pid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = InitFromProcessNoThrow(pid);
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }
}

// ----------------------------------------------------------------------------
// SidBuffer::InitFromProcessAppContainerSidNoThrow
//
// Description:
//    Initialize this SidBuffer instance with the TokenAppContainerSid from
//    the process token
//
// Arguments:
//    * pid - Process ID from which to grab the SID
//
// Return Value:
//    HRESULT indicating success / failure
//    S_FALSE indicates the process isn't in an AppContainer
//
HRESULT SidBuffer::InitFromProcessAppContainerSidNoThrow(DWORD pid)
{
    HRESULT hr = S_OK;
    HANDLE hToken = NULL;
    BOOL fIsLowBox = FALSE;

    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, pid);
    if (hProcess == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }
    if (!OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }

    // Define new TOKEN_INFORMATION_CLASS/ TOKEN_APPCONTAINER_INFORMATION members for Win8 since they are not in the DevDiv copy of WinSDK yet
    typedef enum _TOKEN_INFORMATION_CLASS_WIN8 {
        TokenIsAppContainer = TokenLogonSid + 1,
        TokenCapabilities,
        TokenAppContainerSid
    } TOKEN_INFORMATION_CLASS_WIN8;

    typedef struct _TOKEN_APPCONTAINER_INFORMATION
    {
        PSID TokenPackage;
    } TOKEN_APPCONTAINER_INFORMATION, *PTOKEN_APPCONTAINER_INFORMATION;

    DWORD size;
    if (!GetTokenInformation(hToken, (TOKEN_INFORMATION_CLASS)TokenIsAppContainer, &fIsLowBox, sizeof(fIsLowBox), &size))
    {
        DWORD gle = GetLastError();
        if (gle == ERROR_INVALID_PARAMETER || gle == ERROR_INVALID_FUNCTION)
        {
            hr = S_FALSE; // We are on an OS which doesn't understand LowBox
        }
        else
        {
            hr = HRESULT_FROM_WIN32(gle);
        }
        goto exit;
    }

    if (!fIsLowBox)
    {
        hr = S_FALSE;
        goto exit;
    }

    UCHAR PackSid[SECURITY_MAX_SID_SIZE + sizeof(TOKEN_APPCONTAINER_INFORMATION)];
    if (!GetTokenInformation(hToken, (TOKEN_INFORMATION_CLASS)TokenAppContainerSid, &PackSid, sizeof(PackSid), &size))
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }

    {
        PTOKEN_APPCONTAINER_INFORMATION pTokPack = (PTOKEN_APPCONTAINER_INFORMATION)&PackSid;
        PSID pLowBoxPackage = pTokPack->TokenPackage;
        DWORD dwSidLen = GetLengthSid(pLowBoxPackage);
        m_pBuffer = new (nothrow) BYTE[dwSidLen];
        if (m_pBuffer == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto exit;
        }
        else
        {
            if (!CopySid(dwSidLen, m_pBuffer, pLowBoxPackage))
            {
                hr = HRESULT_FROM_GetLastError();
                delete m_pBuffer;
                m_pBuffer = NULL;
                goto exit;
            }
        }
    }

exit:
    if (hProcess != NULL)
    {
        CloseHandle(hProcess);
    }
    if (hToken != NULL)
    {
        CloseHandle(hToken);
    }

    return hr;
}

// ----------------------------------------------------------------------------
// SidBuffer::InitFromProcessUserNoThrow
//
// Description:
//    Initialize this SidBuffer instance with a Sid from the token of the specified
//    process. Use the USER sid from the process token if possible; else use the term
//    serv API to find the USER sid from the process token.
//
// Arguments:
//    * pid - Process ID from which to grab the SID
//
// Return Value:
//    HRESULT indicating success / failure
//

HRESULT SidBuffer::InitFromProcessUserNoThrow(DWORD pid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_pBuffer == NULL);
    HRESULT hr = GetSidFromProcessWorker(pid, kUserSid, (PSID *) &m_pBuffer);
#ifndef FEATURE_CORESYSTEM
    if (FAILED(hr))
    {
        hr = GetSidFromProcessEXWorker(pid, (PSID *) &m_pBuffer);
    }
#endif // !FEATURE_CORESYSTEM
    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(m_pBuffer != NULL);
    return S_OK;
}

// See code:SidBuffer::InitFromProcessUserNoThrow.  Throws if there's an error.
void SidBuffer::InitFromProcessUser(DWORD pid)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = InitFromProcessUserNoThrow(pid);
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }
}

//-----------------------------------------------------------------------------
// Ctor for Dacl class. Wraps a win32 dacl.
//-----------------------------------------------------------------------------
Dacl::Dacl(PACL pAcl)
{
    m_acl = pAcl;
}

//-----------------------------------------------------------------------------
// Get number of ACE (Access Control Entries) in this DACL.
//-----------------------------------------------------------------------------
SIZE_T Dacl::GetAceCount()
{
    return (SIZE_T) m_acl->AceCount;
}

//-----------------------------------------------------------------------------
// Get Raw a ACE at the given index.
// Caller assumes index is valid (0 <= dwAceIndex < GetAceCount())
// Throws on error (which should only be if the index is out of bounds).
//-----------------------------------------------------------------------------
ACE_HEADER * Dacl::GetAce(SIZE_T dwAceIndex)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    ACE_HEADER * pAce = NULL;
    BOOL fOk = ::GetAce(m_acl, (DWORD) dwAceIndex, (LPVOID*) &pAce);
    _ASSERTE(fOk == (pAce != NULL));
    if (!fOk)
    {
        ThrowLastError();
    }
    return pAce;
}



//-----------------------------------------------------------------------------
// Ctor for SecurityDescriptor
//-----------------------------------------------------------------------------
Win32SecurityDescriptor::Win32SecurityDescriptor()
{
    m_pDesc = NULL;
}

//-----------------------------------------------------------------------------
// Dtor for security Descriptor.
//-----------------------------------------------------------------------------
Win32SecurityDescriptor::~Win32SecurityDescriptor()
{
    delete [] ((BYTE*) m_pDesc);
}



//-----------------------------------------------------------------------------
// Get the dacl for this security descriptor.
//-----------------------------------------------------------------------------
Dacl Win32SecurityDescriptor::GetDacl()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_pDesc != NULL);

    BOOL bPresent;
    BOOL bDaclDefaulted;
    PACL acl;

    if (GetSecurityDescriptorDacl(m_pDesc, &bPresent, &acl, &bDaclDefaulted) == 0)
    {
        ThrowLastError();
    }
    if (!bPresent)
    {
        // No dacl. We consider this an error because all of the objects we expect
        // to see should be dacled. If it's not dacled, then it's a malicious user spoofing it.
        ThrowHR(E_INVALIDARG);
    }

    Dacl d(acl);
    return d;
}

//-----------------------------------------------------------------------------
// Get the owner from the security descriptor.
//-----------------------------------------------------------------------------
HRESULT Win32SecurityDescriptor::GetOwnerNoThrow( PSID* ppSid)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_pDesc != NULL);
    BOOL bOwnerDefaulted;

    if( ppSid == NULL )
    {
        return E_INVALIDARG;
    }

    if (GetSecurityDescriptorOwner(m_pDesc, ppSid, &bOwnerDefaulted) == 0)
    {
        DWORD err = GetLastError();
        return HRESULT_FROM_WIN32(err);
    }

    return S_OK;
}
Sid Win32SecurityDescriptor::GetOwner()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    PSID pSid;
    HRESULT hr = GetOwnerNoThrow( &pSid );
    if( FAILED(hr) )
    {
        ThrowHR( hr );
    }

    Sid s(pSid);
    return s;
}

//-----------------------------------------------------------------------------
// Initialize this instance of a SecurityDescriptor with the SD for the handle.
// The handle must have READ_CONTROL permissions to do this.
// Throws on error.
//-----------------------------------------------------------------------------
HRESULT Win32SecurityDescriptor::InitFromHandleNoThrow(HANDLE h)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_pDesc == NULL); //  only init once.

    DWORD       cbNeeded = 0;

    DWORD flags = OWNER_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION;

    // Now get the creator's SID. First get the size of the array needed.
    BOOL fOk = GetKernelObjectSecurity(h, flags, NULL, 0, &cbNeeded);
    DWORD err = GetLastError();

    // Caller should give us a handle for which this succeeds. First call will
    // fail w/ InsufficientBuffer.
    CONSISTENCY_CHECK_MSGF(fOk || (err == ERROR_INSUFFICIENT_BUFFER), ("Failed to get KernelSecurity for object handle=%p.Err=%d\n", h, err));

    PSECURITY_DESCRIPTOR pSD = (PSECURITY_DESCRIPTOR) new(nothrow) BYTE[cbNeeded];
    if( pSD == NULL )
    {
        return E_OUTOFMEMORY;
    }

    if (GetKernelObjectSecurity(h, flags, pSD, cbNeeded, &cbNeeded) == 0)
    {
        // get last error and fail out.
        err = GetLastError();
        delete [] ((BYTE*) pSD);
        return HRESULT_FROM_WIN32(err);
    }

    m_pDesc = pSD;
    return S_OK;
}
void Win32SecurityDescriptor::InitFromHandle(HANDLE h)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    HRESULT hr = InitFromHandleNoThrow(h);
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }
}
