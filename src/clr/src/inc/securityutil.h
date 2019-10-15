// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef SECURITYUTIL_H
#define SECURITYUTIL_H

#include "winnt.h"

// Security utility class. This is currently used by the debugger right-side and dbgshim to figure out the 
// SECURITY_ATTRIBUTES to use on various IPC objects (named events, etc.).
// This is pretty debugger specific, and so perhaps doesn't actually belong in utilcode (that's just the most
// convenient way to share it between mscordbi and dbgshim.dll).  This is also a pretty big mess.  All of 
// this ACL craziness is already gone in Arrowhead, so it's not a high priority to clean this up.
class SecurityUtil
{
public:

    //
    // This will generate ACL containing the current process and 
    // an allowed ACE on the target process of the given pid.
    //
    // Host should free returned *ppACL by calling FreeACL
    //
    static HRESULT GetACLOfPid(DWORD pid, PACL *ppACL);
    
    static void FreeACL(PACL pACL);

    static HRESULT GetMandatoryLabelFromProcess(HANDLE hProcess, LPBYTE * ppbLabel);
    static DWORD * GetIntegrityLevelFromMandatorySID(PSID psidIntegrityLevelLabel);

    // instance functions. SecurityUtil is used to minimized memory allocation when converting
    // pACL to SECURITY_ATTRIBUTES
    // The needed memory to hold SECURITY_ATTRIBUTES and SECURITY_DESCRIPTOR are embedded
    // in the SecurityUtil instance. 
    // 
    SecurityUtil(PACL pACL);
    ~SecurityUtil();
    HRESULT Init();
    HRESULT Init(HANDLE pid);
    HRESULT GetSA(SECURITY_ATTRIBUTES **PPSA);
private:
    HRESULT SetSecurityDescriptorMandatoryLabel(PSID psidIntegrityLevelLabel);
    SECURITY_ATTRIBUTES m_SA;
    SECURITY_DESCRIPTOR m_SD;
    PACL                m_pACL;
    // Saved by SetSecurityDescriptorMandatoryLabel so that the memory can be deleted properly
    PACL                m_pSacl;
    bool                m_fInitialized;
};

#endif // !SECURITYUTIL_H
