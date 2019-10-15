// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "stdafx.h"

#include "securityutil.h"
#include "ex.h"

#include "securitywrapper.h"

// These are the right that we will give to the global section and global events used
// in communicating between debugger and debugee
//
// SECTION_ALL_ACCESS is needed for the IPC block. Unfortunately, we DACL our events and
// IPC block identically. Or this particular right does not need to bleed into here. 
//
#ifndef CLR_IPC_GENERIC_RIGHT
#define CLR_IPC_GENERIC_RIGHT (GENERIC_READ | GENERIC_WRITE | GENERIC_EXECUTE | STANDARD_RIGHTS_ALL | SECTION_ALL_ACCESS)
#endif


//*****************************************************************
// static helper function
//
// helper to form ACL that contains AllowedACE of users of current 
// process and target process
//
// [IN] pid - target process id
// [OUT] ppACL - ACL for the process
//
// Clean up - 
// Caller remember to call FreeACL() on *ppACL
//*****************************************************************
HRESULT SecurityUtil::GetACLOfPid(DWORD pid, PACL *ppACL)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT         hr = S_OK;
    _ASSERTE(ppACL);
    *ppACL = NULL;

    PSID    pCurrentProcessSid = NULL;
    PSID    pTargetProcessSid = NULL;
    PSID    pTargetProcessAppContainerSid = NULL;
    DWORD   cSid = 0;
    DWORD   dwAclSize = 0;

    LOG((LF_CORDB, LL_INFO10000,
         "SecurityUtil::GetACLOfPid on pid : 0x%08x\n",
         pid));


    SidBuffer sidCurrentProcess;
    SidBuffer sidTargetProcess;
    SidBuffer sidTargetProcessAppContainer;

    // Get sid for current process.            
    EX_TRY
    {
        sidCurrentProcess.InitFromProcess(GetCurrentProcessId()); // throw on error.
        pCurrentProcessSid = sidCurrentProcess.GetSid().RawSid();
        cSid++;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    // Get sid for target process.
    EX_TRY
    {
        sidTargetProcess.InitFromProcess(pid); // throws on error.
        pTargetProcessSid = sidTargetProcess.GetSid().RawSid();
        cSid++;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    
    //FISHY: what is the scenario where only one of the above calls succeeds?
    if (cSid == 0)
    {
        // failed to get any useful sid. Just return.
        // need a better error.
        //
        hr = E_FAIL;
        goto exit;
    }

    hr = sidTargetProcessAppContainer.InitFromProcessAppContainerSidNoThrow(pid);
    if (FAILED(hr))
    {
        goto exit;
    }
    else if (hr == S_OK)
    {
        pTargetProcessAppContainerSid = sidTargetProcessAppContainer.GetSid().RawSid();
        cSid++;
    }
    else if(hr == S_FALSE) //not an app container, no sid to add
    {
        hr = S_OK; // don't leak S_FALSE
    }

    LOG((LF_CORDB, LL_INFO10000,
         "SecurityUtil::GetACLOfPid number of sid : 0x%08x\n",
         cSid));

    // Now allocate space for ACL. First calculate the space is need to hold ACL
    dwAclSize = sizeof(ACL) + (sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD)) * cSid;
    if (pCurrentProcessSid)
    {
        dwAclSize += GetLengthSid(pCurrentProcessSid);
    }
    if (pTargetProcessSid)
    {
        dwAclSize += GetLengthSid(pTargetProcessSid);
    }
    if (pTargetProcessAppContainerSid)
    {
        dwAclSize += GetLengthSid(pTargetProcessAppContainerSid);
    }

    *ppACL = (PACL) new (nothrow) char[dwAclSize];
    if (*ppACL == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }

    // Initialize ACL
    // add each sid to the allowed ace list
    if (!InitializeAcl(*ppACL, dwAclSize, ACL_REVISION))
    {
        hr = HRESULT_FROM_GetLastError();
        goto exit;
    }

    if (pCurrentProcessSid)
    {
        // add the current process's sid into ACL if we have it
        if (!AddAccessAllowedAce(*ppACL, ACL_REVISION, CLR_IPC_GENERIC_RIGHT, pCurrentProcessSid))
        {
            hr = HRESULT_FROM_GetLastError();
            goto exit;
        }
    }

    if (pTargetProcessSid)
    {
        // add the target process's sid into ACL if we have it
        if (!AddAccessAllowedAce(*ppACL, ACL_REVISION, CLR_IPC_GENERIC_RIGHT, pTargetProcessSid))
        {
            hr = HRESULT_FROM_GetLastError();
            goto exit;
        }
    }

    if (pTargetProcessAppContainerSid)
    {
        // add the target process's AppContainer's sid into ACL if we have it
        if (!AddAccessAllowedAce(*ppACL, ACL_REVISION, CLR_IPC_GENERIC_RIGHT, pTargetProcessAppContainerSid))
        {
            hr = HRESULT_FROM_GetLastError();
            goto exit;
        }
    }

    // we better to form a valid ACL to return
    _ASSERTE(IsValidAcl(*ppACL));
exit:
    if (FAILED(hr) && *ppACL)
    {
        delete [] (reinterpret_cast<char*>(ppACL));
    }
    return hr;
}   // SecurityUtil::GetACLOfPid


//*****************************************************************
// static helper function
//
// free the ACL allocated by SecurityUtil::GetACLOfPid
//
// [IN] pACL - ACL to be freed
//
//*****************************************************************
void SecurityUtil::FreeACL(PACL pACL)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    if (pACL)
    {
        delete [] (reinterpret_cast<char*>(pACL));
    }
}   // SecurityUtil::FreeACL


//*****************************************************************
// constructor
//
// [IN] pACL - ACL that this instance of SecurityUtil will held on to
//
//*****************************************************************
SecurityUtil::SecurityUtil(PACL pACL)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    m_pACL = pACL;
    m_pSacl = NULL;
    m_fInitialized = false;
}

//*****************************************************************
// destructor
//
// free the ACL that this instance of SecurityUtil helds on to
//
//*****************************************************************
SecurityUtil::~SecurityUtil()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    FreeACL(m_pACL);
    FreeACL(m_pSacl);
}

//*****************************************************************
// Initialization function
//
// form the SecurityDescriptor that will represent the m_pACL
//
//*****************************************************************
HRESULT SecurityUtil::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT    hr = S_OK;

    if (m_pACL)
    {
        if (!InitializeSecurityDescriptor(&m_SD, SECURITY_DESCRIPTOR_REVISION))
        {
            hr = HRESULT_FROM_GetLastError();
            return hr;
        }
        if (!SetSecurityDescriptorDacl(&m_SD, TRUE, m_pACL, FALSE))
        {
            hr = HRESULT_FROM_GetLastError();
            return hr;
        }

        m_SA.nLength = sizeof(SECURITY_ATTRIBUTES);
        m_SA.lpSecurityDescriptor = &m_SD;
        m_SA.bInheritHandle = FALSE;
        m_fInitialized = true;
    }
    return S_OK;
}

// ***************************************************************************
// Initialization functions which will call the normal Init and add a 
// mandatory label entry to the sacl
//
// Expects hProcess to be a valid handle to the process which has the desired
// mandatory label
// ***************************************************************************
HRESULT SecurityUtil::Init(HANDLE hProcess)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = Init();
    if (FAILED(hr))
    {
        return hr;
    }
    
    NewArrayHolder<BYTE> pLabel;

    hr = GetMandatoryLabelFromProcess(hProcess, &pLabel);
    if (FAILED(hr))
    { 
        return hr;
    }

    TOKEN_MANDATORY_LABEL * ptml = (TOKEN_MANDATORY_LABEL *) pLabel.GetValue();

    hr = SetSecurityDescriptorMandatoryLabel(ptml->Label.Sid);

    return hr;
}


// ***************************************************************************
// Given a process, this will put the mandatory label into a buffer and point
// ppbLabel at the buffer.
// 
// Caller must free ppbLabel via the array "delete []" operator
// ***************************************************************************
HRESULT SecurityUtil::GetMandatoryLabelFromProcess(HANDLE hProcess, LPBYTE * ppbLabel)
{
    *ppbLabel = NULL;
    
    DWORD dwSize = 0;
    HandleHolder hToken;
    DWORD err = 0;

    if(!OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        return HRESULT_FROM_GetLastError();
    }

    if(!GetTokenInformation(hToken, (TOKEN_INFORMATION_CLASS)TokenIntegrityLevel, NULL, 0, &dwSize))
    {
        err = GetLastError();
    }
    
    // We need to make sure that GetTokenInformation failed in a predictable manner so we know that
    // dwSize has the correct buffer size in it.
    if (err != ERROR_INSUFFICIENT_BUFFER || dwSize == 0)
    {
        return HRESULT_FROM_WIN32(err);
    }

    NewArrayHolder<BYTE> pLabel = new (nothrow) BYTE[dwSize];
    if (pLabel == NULL)
    {
        return E_OUTOFMEMORY;
    }

    if(!GetTokenInformation(hToken, (TOKEN_INFORMATION_CLASS)TokenIntegrityLevel, pLabel, dwSize, &dwSize))
    {
        return HRESULT_FROM_GetLastError();
    }

    // Our caller will be freeing the memory so use Extract
    *ppbLabel = pLabel.Extract();

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Returns pointer inside the specified mandatory SID to the DWORD representing the
// integrity level of the process.  This DWORD will be one of the
// SECURITY_MANDATORY_*_RID constants.
//
// Arguments:
//      psidIntegrityLevelLabel - [in] PSID in which to find the integrity level.
//
// Return Value:
//      Pointer to the RID stored in the specified SID.  This RID represents the
//      integrity level of the process
//

// static
DWORD * SecurityUtil::GetIntegrityLevelFromMandatorySID(PSID psidIntegrityLevelLabel)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return GetSidSubAuthority(psidIntegrityLevelLabel, (*GetSidSubAuthorityCount(psidIntegrityLevelLabel) - 1));
}

// Creates a mandatory label ace and sets it to be the entry in the 
// security descriptor's sacl. This assumes there are no other entries 
// in the sacl
HRESULT SecurityUtil::SetSecurityDescriptorMandatoryLabel(PSID psidIntegrityLevelLabel)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD cbSid = GetLengthSid(psidIntegrityLevelLabel);
    DWORD cbAceStart = offsetof(SYSTEM_MANDATORY_LABEL_ACE, SidStart);
    // We are about allocate memory for a ACL and an ACE so we need space for:
    // 1) the ACL: sizeof(ACL)
    // 2) the entry: the sid is of variable size, so the SYSTEM_MANDATORY_LABEL_ACE
    //    structure has only the first DWORD of the sid in its definition, to get the
    //    appropriate size we get size without SidStart and add on the actual size of the sid
    DWORD cbSacl = sizeof(ACL) + cbAceStart + cbSid;

    NewArrayHolder<BYTE> sacl = new (nothrow) BYTE[cbSacl];
    
    m_pSacl = NULL;

    if (sacl == NULL)
    {
        return E_OUTOFMEMORY;
    }
    ZeroMemory(sacl.GetValue(), cbSacl);
    PACL pSacl = reinterpret_cast<ACL *>(sacl.GetValue());
    SYSTEM_MANDATORY_LABEL_ACE * pLabelAce = reinterpret_cast<SYSTEM_MANDATORY_LABEL_ACE *>(sacl.GetValue() + sizeof(ACL));
    PSID psid = reinterpret_cast<SID *>(&pLabelAce->SidStart);

    // Our buffer looks like this now: (not drawn to scale)
    // sacl  pSacl pLabelAce psid
    //  -      -
    //  |      |
    //  |      -       -
    //  |              |       
    //  |              |       -
    //  |              -       |
    //  -                      -

    DWORD dwIntegrityLevel = *(GetIntegrityLevelFromMandatorySID(psidIntegrityLevelLabel));

    if (dwIntegrityLevel >= SECURITY_MANDATORY_MEDIUM_RID)
    {
        // No need to set the integrity level unless it's lower than medium
        return S_OK;
    }

    if(!InitializeAcl(pSacl, cbSacl, ACL_REVISION))
    { 
        return HRESULT_FROM_GetLastError();
    }

    pSacl->AceCount = 1;

    pLabelAce->Header.AceType = SYSTEM_MANDATORY_LABEL_ACE_TYPE;
    pLabelAce->Header.AceSize = WORD(cbAceStart + cbSid);
    pLabelAce->Mask = SYSTEM_MANDATORY_LABEL_NO_WRITE_UP;

    memcpy(psid, psidIntegrityLevelLabel, cbSid);

    if(!SetSecurityDescriptorSacl(m_SA.lpSecurityDescriptor, TRUE, pSacl, FALSE))
    {
        return HRESULT_FROM_GetLastError();
    }

    // No need to delete the sacl buffer, it will be deleted in the
    // destructor of this class
    m_pSacl = (PACL)sacl.Extract();
    return S_OK;
}

//*****************************************************************
// Return SECURITY_ATTRIBUTES that we form in the Init function
//
// No clean up is needed after calling this function. The destructor of the 
// instance will do the right thing. Note that this is designed such that
// we minimize memory allocation, ie the SECURITY_DESCRIPTOR and
// SECURITY_ATTRIBUTES are embedded in the SecurityUtil instance. 
//
// Caller should not modify the returned SECURITY_ATTRIBUTES!!!
//*****************************************************************
HRESULT SecurityUtil::GetSA(SECURITY_ATTRIBUTES **ppSA)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(ppSA);

    if (m_fInitialized == false)
    {
        _ASSERTE(!"Bad code path!");
        *ppSA = NULL;
        return E_FAIL;
    }

    *ppSA = &m_SA;
    return S_OK;
}
