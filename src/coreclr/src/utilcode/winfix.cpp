// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// WinWrap.cpp
//

//
// This file contains wrapper functions for Win32 API's that take strings.
//
// COM+ internally uses UNICODE as the internal state and string format.  This
// file will undef the mapping macros so that one cannot mistakingly call a
// method that isn't going to work.  Instead, you have to call the correct
// wrapper API.
//
//*****************************************************************************

#include "stdafx.h"                     // Precompiled header key.
#include "winwrap.h"                    // Header for macros and functions.
#include "utilcode.h"
#include "holder.h"
#include "ndpversion.h"
#include "pedecoder.h"


// ====== READ BEFORE ADDING CONTRACTS ==================================================
// The functions in this file propagate SetLastError codes to their callers.
// Contracts are not guaranteed to preserve these codes (and no, we're not taking
// the overhead hit to make them do so. Don't bother asking.)
//
// Most of the wrappers have a contract of the form:
//
//     NOTHROW;
//     INJECT_FAULT(xxx);
//
// For such functions, use the special purpose construct:
//
//     WINWRAPPER_NO_CONTRACT(xxx);
//
// For everything else, use STATIC_CONTRACT.
//     
#undef CONTRACT
#define CONTRACT $$$$$$$$READ_COMMENT_IN_WINFIX_CPP$$$$$$$$$$

#undef CONTRACTL
#define CONTRACTL $$$$$$$$READ_COMMENT_IN_WINFIX_CPP$$$$$$$$$$

#ifdef ENABLE_CONTRACTS_IMPL
static BOOL gWinWrapperContractRecursionBreak = FALSE;


class WinWrapperContract
{
    public:
        WinWrapperContract(const char *szFunction, const char *szFile, int lineNum)
        {
            CANNOT_HAVE_CONTRACT;

            m_pClrDebugState = NULL;
            
            if (gWinWrapperContractRecursionBreak)
            {
                return;
            }

            m_pClrDebugState = GetClrDebugState();

            // Save old debug state
            m_IncomingClrDebugState = *m_pClrDebugState;


            m_pClrDebugState->ViolationMaskReset( ThrowsViolation );

            if (m_pClrDebugState->IsFaultForbid() && !(m_pClrDebugState->ViolationMask() & (FaultViolation|FaultNotFatal|BadDebugState)))
            {
                gWinWrapperContractRecursionBreak = TRUE;

                CONTRACT_ASSERT("INJECT_FAULT called in a FAULTFORBID region.",
                                Contract::FAULT_Forbid,
                                Contract::FAULT_Mask,
                                szFunction,
                                szFile,
                                lineNum
                                );
            }

            
        };

        ~WinWrapperContract()
        {
            CANNOT_HAVE_CONTRACT;

            //!!!!!! THIS DESTRUCTOR MUST NOT CHANGE THE GETLASTERROR VALUE !!!!!!

            // Backout all changes to debug state.
            if (m_pClrDebugState != NULL)
            {
                *m_pClrDebugState = m_IncomingClrDebugState;
            }
        }
    private:
        ClrDebugState *m_pClrDebugState;
        ClrDebugState  m_IncomingClrDebugState;

};



#endif

#ifdef ENABLE_CONTRACTS_IMPL
#define WINWRAPPER_NO_CONTRACT(stmt) \
    STATIC_CONTRACT_NOTHROW;      \
    STATIC_CONTRACT_FAULT;        \
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;  \
    WinWrapperContract __wcontract(__FUNCTION__, __FILE__, __LINE__); \
    if (0) {stmt}  \
        
#define STATIC_WINWRAPPER_NO_CONTRACT(stmt) \
    STATIC_CONTRACT_NOTHROW;      \
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;  \
    STATIC_CONTRACT_FAULT;        \
    if (0) {stmt}  \
        
        
#else
#define WINWRAPPER_NO_CONTRACT(stmt)
#define STATIC_WINWRAPPER_NO_CONTRACT(stmt) 
#endif

ULONG g_dwMaxDBCSCharByteSize = 0;

// The only purpose of this function is to make a local copy of lpCommandLine.
// Because windows implementation of CreateProcessW can actually change lpCommandLine,
// but we'd like to keep it const.
BOOL
WszCreateProcess(
    LPCWSTR lpApplicationName,
    LPCWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
    )
{
    WINWRAPPER_NO_CONTRACT(SetLastError(ERROR_OUTOFMEMORY); return 0;);

    BOOL fResult;
    DWORD err;
    {
        size_t commandLineLength = wcslen(lpCommandLine) + 1;
        NewArrayHolder<WCHAR> nonConstCommandLine(new (nothrow) WCHAR[commandLineLength]);
        if (nonConstCommandLine == NULL)
        {
            SetLastError(ERROR_OUTOFMEMORY);
            return 0;
        }
            
        memcpy(nonConstCommandLine, lpCommandLine, commandLineLength * sizeof(WCHAR));
            
        fResult = CreateProcessW(lpApplicationName,
                                   nonConstCommandLine,
                                   lpProcessAttributes,
                                   lpThreadAttributes,
                                   bInheritHandles,
                                   dwCreationFlags,
                                   lpEnvironment,
                                   (LPWSTR)lpCurrentDirectory,
                                   lpStartupInfo,
                                   lpProcessInformation);

        // At the end of the current scope, the last error code will be overwritten by the destructor of
        // NewArrayHolder. So we save the error code here, and restore it after the end of the current scope.
        err = GetLastError();
    }

    SetLastError(err);
    return fResult;
}

#ifndef FEATURE_PAL


#include "psapi.h"
#include "tlhelp32.h"
#include "winnls.h"

//********** Globals. *********************************************************
bool            g_fEnsureCharSetInfoInitialized = FALSE; // true if we've detected the platform's character set characteristics

// Detect Unicode support of the operating system, and initialize globals
void EnsureCharSetInfoInitialized()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    if (!g_fEnsureCharSetInfoInitialized)
    {
        // NOTE: Do not use any of the Wsz* wrapper functions right now. They will have
        // problems.

        // Per Shupak, you're supposed to get the maximum size of a DBCS char
        // dynamically to work properly on all locales (bug 2757).
        CPINFO cpInfo;
        if (GetCPInfo(CP_ACP, &cpInfo))
            g_dwMaxDBCSCharByteSize = cpInfo.MaxCharSize;
        else
            g_dwMaxDBCSCharByteSize = 2;

        VolatileStore(&g_fEnsureCharSetInfoInitialized, true);
    }

    return;
}


// Running with an interactive workstation.
BOOL RunningInteractive()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    static int fInteractive = -1;
    if (fInteractive != -1)
        return fInteractive != 0;

#if !defined(FEATURE_CORESYSTEM)
        HWINSTA hwinsta = NULL;

        if ((hwinsta = GetProcessWindowStation() ) != NULL)
        {
            DWORD lengthNeeded;
            USEROBJECTFLAGS flags;

            if (GetUserObjectInformationW (hwinsta, UOI_FLAGS, &flags, sizeof(flags), &lengthNeeded))
           {
                    if ((flags.dwFlags & WSF_VISIBLE) == 0)
                        fInteractive = 0;
            }
        }
#endif // !FEATURE_CORESYSTEM

    if (fInteractive != 0)
        fInteractive = 1;

    return fInteractive != 0;
}


// Wrapper function around CheckTokenMembership to determine if the token enables the SID "S-1-5-<rid>".
// If hToken is NULL, this function uses the thread's impersonation token. If the thread is not impersonating, the 
// process token is used.
//
// If the function succeeds, it returns ERROR_SUCCESS, else it returns the error code returned by GetLastError()
static DWORD TokenEnablesSID(IN HANDLE hToken OPTIONAL, IN DWORD rid, OUT BOOL& fResult)
{
    DWORD dwError;
    SID_IDENTIFIER_AUTHORITY SIDAuthNT = SECURITY_NT_AUTHORITY;
    PSID pSid = NULL;
    HMODULE hAdvApi32 = NULL;
    typedef BOOL (WINAPI *CheckTokenMembership_t)(HANDLE TokenHandle, PSID SidToCheck, PBOOL IsMember);
    CheckTokenMembership_t pfnCheckTokenMembership = NULL;

    hAdvApi32 = WszGetModuleHandle(W("advapi32.dll"));
    if (hAdvApi32 == NULL)
    {
        dwError = ERROR_MOD_NOT_FOUND;
        goto lExit;
    }
    pfnCheckTokenMembership = (CheckTokenMembership_t) GetProcAddress(hAdvApi32, "CheckTokenMembership");
    if (pfnCheckTokenMembership == NULL)
    {
        dwError = GetLastError();
        goto lExit;
    }

    fResult = FALSE;
    if (!AllocateAndInitializeSid(&SIDAuthNT, 1, rid, 0, 0, 0, 0, 0, 0, 0, &pSid))
    {
        dwError = GetLastError();
        goto lExit;
    }
    if (!pfnCheckTokenMembership(hToken, pSid, &fResult))
    {
        dwError = GetLastError();
        goto lExit;
    }
    dwError = ERROR_SUCCESS;

lExit:
    if (pSid) FreeSid(pSid);
    return dwError;
    
}

// Determines if the process is running as Local System or as a service. Note that
// the function attempts to determine the process' identity and not the thread's 
// (if the thread is impersonating).
//
// Parameters:
//    fIsLocalSystemOrService - TRUE if the function succeeds and the process is
//                              running as SYSTEM or as a service
//
// Return value:
//
// If the function succeeds, it returns ERROR_SUCCESS, else it returns the error
// code returned by GetLastError()
//
// Notes:
// 
// This function will generally fail if the calling thread is impersonating at the 
// ANONYMOUS level; see the comments in the function.
//
DWORD RunningAsLocalSystemOrService(OUT BOOL& fIsLocalSystemOrService)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    static int fLocalSystemOrService = -1;
    if (fLocalSystemOrService != -1)
    {
        fIsLocalSystemOrService = fLocalSystemOrService != 0;
        return ERROR_SUCCESS;
    }

    DWORD dwError;
    HANDLE hThreadToken = NULL;
    HANDLE hProcessToken = NULL;
    HANDLE hDuplicatedProcessToken = NULL;
    BOOL fLocalSystem = FALSE;
    BOOL fService = FALSE;
    BOOL bReverted = FALSE;

    if (OpenThreadToken(GetCurrentThread(), TOKEN_IMPERSONATE, TRUE, &hThreadToken))
    {
        if (RevertToSelf())
        {
            bReverted = TRUE;
        }
#ifdef _DEBUG
        else
        {
            // For debugging only, continue as the impersonated user; see comment below
            dwError = GetLastError();
        }
#endif // #ifdef _DEBUG
    }
#ifdef _DEBUG
    else
    {
        dwError = GetLastError();
        if (dwError == ERROR_NO_IMPERSONATION_TOKEN || dwError == ERROR_NO_TOKEN)
        {
            // The thread is not impersonating; it's safe to continue
        }
        else
        {
            // The thread could be impersonating, but we won't be able to restore the impersonation
            // token if we RevertToSelf(). Continue as the impersonated user. OpenProcessToken will
            // fail (unless the impersonated user is SYSTEM or the same as the process' user).
            //
            // Note that this case will occur if the impersonation level is ANONYMOUS, the error
            // code will be ERROR_CANT_OPEN_ANONYMOUS. 
        }
    }
#endif // #ifdef _DEBUG

    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_DUPLICATE, &hProcessToken))
    {
        dwError = GetLastError();
        goto lExit;
    }
        
    if (!DuplicateToken(hProcessToken, SecurityImpersonation, &hDuplicatedProcessToken))
    {
        dwError = GetLastError();
        goto lExit;
    }

    dwError = TokenEnablesSID(hDuplicatedProcessToken, SECURITY_LOCAL_SYSTEM_RID, fLocalSystem);
    if (dwError != ERROR_SUCCESS)
    {
        goto lExit;
    }
    if (fLocalSystem)
    {
        goto lExit;
    }

    dwError = TokenEnablesSID(hDuplicatedProcessToken, SECURITY_SERVICE_RID, fService);
    
lExit:
    if (bReverted)
    {
        if (!SetThreadToken(NULL, hThreadToken))
        {
            DWORD dwLastError = GetLastError();
            _ASSERT("SetThreadToken failed");

            TerminateProcess(GetCurrentProcess(), dwLastError);
        }
    }
    
    if (hThreadToken) CloseHandle(hThreadToken);
    if (hProcessToken) CloseHandle(hProcessToken);
    if (hDuplicatedProcessToken) CloseHandle(hDuplicatedProcessToken);
    
    if (dwError != ERROR_SUCCESS)
    {
        fIsLocalSystemOrService = FALSE; // We don't really know
    }
    else
    {
        fLocalSystemOrService = (fLocalSystem || fService)? 1 : 0;
        fIsLocalSystemOrService = fLocalSystemOrService != 0;
    }
        
    return dwError;
    
}

typedef HRESULT(WINAPI *pfnSetThreadDescription)(HANDLE hThread, PCWSTR lpThreadDescription);
extern pfnSetThreadDescription g_pfnSetThreadDescription;

// Dummy method if windows version does not support it
HRESULT SetThreadDescriptionDummy(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return NOERROR;
}

HRESULT WINAPI InitializeSetThreadDescription(HANDLE hThread, PCWSTR lpThreadDescription)
{
    HMODULE hKernel32 = WszLoadLibrary(W("kernel32.dll"));

    pfnSetThreadDescription pLocal = NULL; 
    if (hKernel32 != NULL)
    {
        // store to thread local variable to prevent data race
        pLocal = (pfnSetThreadDescription)GetProcAddress(hKernel32, "SetThreadDescription");
    }

    if (pLocal == NULL) // method is only available with Windows 10 Creators Update or later
    {
        g_pfnSetThreadDescription = SetThreadDescriptionDummy;
    }
    else
    {
        g_pfnSetThreadDescription = pLocal;
    }

    return g_pfnSetThreadDescription(hThread, lpThreadDescription);
}

pfnSetThreadDescription g_pfnSetThreadDescription = &InitializeSetThreadDescription;

// Set unmanaged thread name which will show up in ETW and Debuggers which know how to read this data.
HRESULT SetThreadName(HANDLE hThread, PCWSTR lpThreadDescription)
{
    return g_pfnSetThreadDescription(hThread, lpThreadDescription);
}

#endif //!FEATURE_PAL
