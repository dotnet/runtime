// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************\
*                                                                             *
* BBSweep.h -    Classes for sweeping profile data to disk                    *
*                                                                             *
*               Version 1.0                                                   *
*******************************************************************************
*                                                                             *
*  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY      *
*  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE        *
*  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR      *
*  PURPOSE.                                                                   *
*                                                                             *
\*****************************************************************************/

#ifndef _BBSWEEP_H_
#define _BBSWEEP_H_

#ifndef TARGET_UNIX
#include <aclapi.h>
#endif // !TARGET_UNIX

#ifndef ARRAYSIZE
#define ARRAYSIZE(x)    (sizeof(x)/sizeof(x[0]))
#endif // !ARRAYSIZE

// The CLR headers don't allow us to use methods like SetEvent directly (instead
// we need to use the host APIs).  However, this file is included both in the CLR
// and in the BBSweep tool, and the host API is not available in the tool.  Moreover,
// BBSweep is not designed to work in an environment where the host controls
// synchronization.  For this reason, we work around the problem by undefining
// these APIs (the CLR redefines them so that they will not be used).
#pragma push_macro("SetEvent")
#pragma push_macro("ResetEvent")
#pragma push_macro("ReleaseSemaphore")
#pragma push_macro("LocalFree")
#undef SetEvent
#undef ResetEvent
#undef ReleaseSemaphore
#undef LocalFree

// MAX_COUNT is the maximal number of runtime processes that can run at a given time
#define MAX_COUNT 20

#define INVALID_PID  -1

/* CLRBBSweepCallback is implemented by the CLR which passes it as an argument to WatchForSweepEvents.
 * It is used by BBSweep to tell the CLR to write the profile data to disk at the right time.
 */

class ICLRBBSweepCallback
{
public:
    virtual HRESULT WriteProfileData() = NULL;  // tells the runtime to write the profile data to disk
};

/* BBSweep is used by both the CLR and the BBSweep utility.
 * BBSweep: calls the PerformSweep method which returns after all the CLR processes
 *          have written their profile data to disk.
 * CLR:     starts up a sweeper thread which calls WatchForSweepEvents and waits until the
 *          sweeper program is invoked.  At that point, all the CLR processes will synchronize
 *          and write their profile data to disk one at a time.  The sweeper threads will then
 *          wait for the next sweep event.  The CLR also calls ShutdownBBSweepThread at
 *          shutdown which returns when the BBSweep thread has terminated.
 */

class BBSweep
{
public:
    BBSweep()
    {
        // The BBSweep constructor could be called even the the object is not used, so
        // don't do any work here.
        bInitialized = false;
        bTerminate   = false;
        hSweepMutex          = NULL;
        hProfDataWriterMutex = NULL;
        hSweepEvent          = NULL;
        hTerminationEvent    = NULL;
        hProfWriterSemaphore = NULL;
        hBBSweepThread       = NULL;
    }

    ~BBSweep()
    {
        // When the destructor is called, everything should be cleaned up already.
    }

    // Called by the sweeper utility to tell all the CLR threads to write their profile
    // data to disk.
    // THIS FUNCTIONALITY IS ALSO DUPLICATED IN TOOLBOX\MPGO\BBSWEEP.CS
    // IF YOU CHANGE THIS CODE, YOU MUST ALSO CHANGE THAT TO MATCH!
    bool PerformSweep(DWORD processID = INVALID_PID)
    {
        bool success = true;

        if (!Initialize(processID, FALSE)) return false;

        ::WaitForSingleObject(hSweepMutex, INFINITE);
        {
            success = success && ::SetEvent(hSweepEvent);
            {
                for (int i=0; i<MAX_COUNT; i++)
                {
                    ::WaitForSingleObject(hProfWriterSemaphore, INFINITE);
                }

                ::ReleaseSemaphore(hProfWriterSemaphore, MAX_COUNT, NULL);

            }
            success = success && ::ResetEvent(hSweepEvent);
        }
        ::ReleaseMutex(hSweepMutex);

        return success;
    }

    // Called by the CLR sweeper thread to wait until a sweep event, at which point
    // it calls back into the CLR via the clrCallback interface to write the profile
    // data to disk.
    bool WatchForSweepEvents(ICLRBBSweepCallback *clrCallback)
    {
        if (!Initialize()) return false;

        bool success = true;

        while (!bTerminate)
        {
            ::WaitForSingleObject(hSweepMutex, INFINITE);
            {
                ::WaitForSingleObject(hProfWriterSemaphore, INFINITE);
            }
            ::ReleaseMutex(hSweepMutex);

            HANDLE hEvents[2];
            hEvents[0] = hSweepEvent;
            hEvents[1] = hTerminationEvent;
            ::WaitForMultipleObjectsEx(2, hEvents, false, INFINITE, FALSE);

            ::WaitForSingleObject(hProfDataWriterMutex, INFINITE);
            {
                if (!bTerminate && FAILED(clrCallback->WriteProfileData()))
                    success = false;
            }
            ::ReleaseMutex(hProfDataWriterMutex);

            ::ReleaseSemaphore(hProfWriterSemaphore, 1, NULL);
        }

        return success;
    }

    void SetBBSweepThreadHandle(HANDLE threadHandle)
    {
        hBBSweepThread = threadHandle;
    }

    void ShutdownBBSweepThread()
    {
        // Set the termination event and wait for the BBSweep thread to terminate on its own.
        // Note that this is called by the shutdown thread (and never called by the BBSweep thread).
        if (hBBSweepThread && bInitialized)
        {
            bTerminate = true;
            ::SetEvent(hTerminationEvent);
            ::WaitForSingleObject(hBBSweepThread, INFINITE);
            Cleanup();
        }
    }

    void Cleanup()
    {
        if (hSweepMutex)          { ::CloseHandle(hSweepMutex);           hSweepMutex =          NULL;}
        if (hProfDataWriterMutex) { ::CloseHandle(hProfDataWriterMutex);  hProfDataWriterMutex = NULL;}
        if (hSweepEvent)          { ::CloseHandle(hSweepEvent);           hSweepEvent =          NULL;}
        if (hTerminationEvent)    { ::CloseHandle(hTerminationEvent);     hTerminationEvent =    NULL;}
        if (hProfWriterSemaphore)  { ::CloseHandle(hProfWriterSemaphore); hProfWriterSemaphore = NULL;}
    }

private:

    // THIS FUNCTIONALITY IS ALSO DUPLICATED IN TOOLBOX\MPGO\BBSWEEP.CS
    // IF YOU CHANGE THIS CODE, YOU MUST ALSO CHANGE THAT TO MATCH!
    bool Initialize(DWORD processID = INVALID_PID, BOOL fromRuntime = TRUE)
    {
        if (!bInitialized)
        {
            SECURITY_ATTRIBUTES * pSecurityAttributes = NULL;

#ifndef FEATURE_CORESYSTEM // @CORESYSTEMTODO
            PSECURITY_DESCRIPTOR pSD = NULL;
            PSID pAdminSid = NULL;
            HANDLE hToken = NULL;
            PACL pACL = NULL;
            LPVOID buffer = NULL;

            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken))
                goto cleanup;

            // don't set pSecurityAttributes for AppContainer processes
            if(!IsAppContainerProcess(hToken))
            {
                SECURITY_ATTRIBUTES securityAttributes;
                PSID pUserSid = NULL;
                SID_IDENTIFIER_AUTHORITY SIDAuthNT = SECURITY_NT_AUTHORITY;
                DWORD retLength;

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6211) // PREfast warning: Leaking memory 'pSD' due to an exception.
#endif /*_PREFAST_ */
                pSD = (PSECURITY_DESCRIPTOR) new char[SECURITY_DESCRIPTOR_MIN_LENGTH];
                if (!pSD)
                    goto cleanup;

                if (GetTokenInformation(hToken, TokenOwner, NULL, 0, &retLength))
                    goto cleanup;

                buffer = (LPVOID) new char[retLength];
                if (!buffer)
                    goto cleanup;
#ifdef _PREFAST_
#pragma warning(pop)
#endif /*_PREFAST_*/

                // Get the SID for the current user
                if (!GetTokenInformation(hToken, TokenOwner, (LPVOID) buffer, retLength, &retLength))
                    goto cleanup;

                pUserSid = ((TOKEN_OWNER *) buffer)->Owner;

                // Get the SID for the admin group
                // Create a SID for the BUILTIN\Administrators group.
                if(! AllocateAndInitializeSid(&SIDAuthNT, 2,
                    SECURITY_BUILTIN_DOMAIN_RID,
                    DOMAIN_ALIAS_RID_ADMINS,
                    0, 0, 0, 0, 0, 0,
                    &pAdminSid))
                    goto cleanup;

                EXPLICIT_ACCESS ea[2];
                ZeroMemory(ea, 2 * sizeof(EXPLICIT_ACCESS));

                // Initialize an EXPLICIT_ACCESS structure for an ACE.
                // The ACE will allow the current user full access
                ea[0].grfAccessPermissions = STANDARD_RIGHTS_ALL | SPECIFIC_RIGHTS_ALL; // KEY_ALL_ACCESS;
                ea[0].grfAccessMode = SET_ACCESS;
                ea[0].grfInheritance= NO_INHERITANCE;
                ea[0].Trustee.TrusteeForm = TRUSTEE_IS_SID;
                ea[0].Trustee.TrusteeType = TRUSTEE_IS_USER;
                ea[0].Trustee.ptstrName  = (LPTSTR) pUserSid;

                // Initialize an EXPLICIT_ACCESS structure for an ACE.
                // The ACE will allow admins full access
                ea[1].grfAccessPermissions = STANDARD_RIGHTS_ALL | SPECIFIC_RIGHTS_ALL; //KEY_ALL_ACCESS;
                ea[1].grfAccessMode = SET_ACCESS;
                ea[1].grfInheritance= NO_INHERITANCE;
                ea[1].Trustee.TrusteeForm = TRUSTEE_IS_SID;
                ea[1].Trustee.TrusteeType = TRUSTEE_IS_GROUP;
                ea[1].Trustee.ptstrName  = (LPTSTR) pAdminSid;

                if (SetEntriesInAcl(2, ea, NULL, &pACL) != ERROR_SUCCESS)
                    goto cleanup;

                if (!InitializeSecurityDescriptor(pSD, SECURITY_DESCRIPTOR_REVISION))
                    goto cleanup;

                if (!SetSecurityDescriptorDacl(pSD, TRUE, pACL, FALSE))
                    goto cleanup;

                memset((void *) &securityAttributes, 0, sizeof(SECURITY_ATTRIBUTES));
                securityAttributes.nLength              = sizeof(SECURITY_ATTRIBUTES);
                securityAttributes.lpSecurityDescriptor = pSD;
                securityAttributes.bInheritHandle       = FALSE;

                pSecurityAttributes = &securityAttributes;
            }
#endif // !FEATURE_CORESYSTEM

            WCHAR objectName[MAX_LONGPATH] = {0};
            WCHAR objectNamePrefix[MAX_LONGPATH] = {0};
            GetObjectNamePrefix(processID, fromRuntime, objectNamePrefix);
            // if there is a non-empty name prefix, append a '\'
            if (objectNamePrefix[0] != '\0')
                wcscat_s(objectNamePrefix, ARRAYSIZE(objectNamePrefix), W("\\"));
            swprintf_s(objectName, MAX_LONGPATH, W("%sBBSweep_hSweepMutex"), objectNamePrefix);
            hSweepMutex          = ::WszCreateMutex(pSecurityAttributes, false,       objectName);
            swprintf_s(objectName, MAX_LONGPATH, W("%sBBSweep_hProfDataWriterMutex"), objectNamePrefix);
            hProfDataWriterMutex = ::WszCreateMutex(pSecurityAttributes, false,       objectName);
            swprintf_s(objectName, MAX_LONGPATH, W("%sBBSweep_hSweepEvent"), objectNamePrefix);
            hSweepEvent          = ::WszCreateEvent(pSecurityAttributes, true, false, objectName);

            // Note that hTerminateEvent is not a named event.  That is because it is not
            // shared amongst the CLR processes (each process terminates at a different time)
            hTerminationEvent    = ::WszCreateEvent(pSecurityAttributes, true, false, NULL);
            swprintf_s(objectName, MAX_LONGPATH, W("%sBBSweep_hProfWriterSemaphore"), objectNamePrefix);
            hProfWriterSemaphore = ::WszCreateSemaphore(pSecurityAttributes, MAX_COUNT, MAX_COUNT, objectName);

#ifndef FEATURE_CORESYSTEM // @CORESYSTEMTODO
cleanup:
            if (pSD) delete [] ((char *) pSD);
            if (pAdminSid) FreeSid(pAdminSid);
            if (hToken) CloseHandle(hToken);
            if (pACL) LocalFree(pACL);
            if (buffer) delete [] ((char *) buffer);
#endif
        }

        bInitialized = hSweepMutex          &&
            hProfDataWriterMutex &&
            hSweepEvent          &&
            hTerminationEvent    &&
            hProfWriterSemaphore;

        if (!bInitialized) Cleanup();
        return bInitialized;
    }

#ifndef TARGET_UNIX
    BOOL IsAppContainerProcess(HANDLE hToken)
    {
#ifndef TokenIsAppContainer
#define TokenIsAppContainer ((TOKEN_INFORMATION_CLASS) 29)
#endif
        BOOL fIsAppContainerProcess;
        DWORD dwReturnLength;
        if (!GetTokenInformation(hToken, TokenIsAppContainer, &fIsAppContainerProcess, sizeof(BOOL), &dwReturnLength) ||
            dwReturnLength != sizeof(BOOL))
        {
            fIsAppContainerProcess = FALSE;
        }
        return fIsAppContainerProcess;
    }
#endif // !TARGET_UNIX

    // helper to get the correct object name prefix
    void GetObjectNamePrefix(DWORD processID, BOOL fromRuntime, __inout_z WCHAR* objectNamePrefix)
    {
        // default prefix
        swprintf_s(objectNamePrefix, MAX_LONGPATH, W("Global"));
#ifndef TARGET_UNIX
        //
        // This method can be called:
        // 1. From process init code
        // 2. From bbsweepclr.exe
        //
        // When called from process init code, processID is always INVALID_PID.
        // In case it is a AppContainer process, we need to add the AppContainerNamedObjectPath to prefix.
        // And if it is a non-AppContainer process, we will continue to use the default prefix (Global).
        // We use IsAppContainerProcess(CurrentProcessId) to make this decision.
        //
        //
        // When called from bbsweepclr, processID is valid when sweeping a AppContainer process.
        // We use this valid processID to determine if the process being swept is AppContainer indeed and then
        // add AppContainerNamedObjectPath to prefix. This is done by IsAppContainerProcess(processID).
        //
        // In case INVALID_PID is passed(non-AppContainer process), we have to use default prefix. To handle this
        // case we use IsAppContainerProcess(CurrentProcessId) and since bbsweepclr is a non-AppContainer process,
        // this check always returns false and we end up using the intended(default) prefix.
        //
        if(processID == INVALID_PID) {
            // we reach here when:
            // * called from process init code:
            // * called from bbsweepclr.exe and no processID has been passed as argument, that is, when sweeping a non-AppContainer process
            processID = GetCurrentProcessId();
        }

        HandleHolder hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processID);
        if (hProcess  != INVALID_HANDLE_VALUE)
        {
            HandleHolder hToken = NULL;
            // if in the process init code of a AppContainer app or if bbsweepclr is used to sweep a AppContainer app,
            // construct the object name prefix using AppContainerNamedObjectPath
            if (OpenProcessToken(hProcess, TOKEN_QUERY, &hToken) && IsAppContainerProcess(hToken))
            {
                WCHAR appxNamedObjPath[MAX_LONGPATH] = { 0 };
                ULONG appxNamedObjPathBufLen = 0;

                if (fromRuntime)
                {
                    // for AppContainer apps, create the object in the "default" object path, i.e. do not provide any prefix
                    objectNamePrefix[0] = W('\0');
                }
                else
                {
#if defined (FEATURE_CORESYSTEM) && !defined(DACCESS_COMPILE)
#define MODULE_NAME W("api-ms-win-security-appcontainer-l1-1-0.dll")
#else
#define MODULE_NAME W("kernel32.dll")
#endif
                    typedef BOOL(WINAPI *PFN_GetAppContainerNamedObjectPath)
                        (HANDLE Token, PSID AppContainerSid, ULONG ObjectPathLength, WCHAR * ObjectPath, PULONG ReturnLength);

                    PFN_GetAppContainerNamedObjectPath pfnGetAppContainerNamedObjectPath = (PFN_GetAppContainerNamedObjectPath)
                        GetProcAddress(WszGetModuleHandle(MODULE_NAME), "GetAppContainerNamedObjectPath");
                    if (pfnGetAppContainerNamedObjectPath)
                    {
                        // for bbsweepclr sweeping a AppContainer app, create the object specifying the AppContainer's path
                        DWORD sessionId = 0;
                        ProcessIdToSessionId(processID, &sessionId);
                        pfnGetAppContainerNamedObjectPath(hToken, NULL, sizeof (appxNamedObjPath) / sizeof (WCHAR), appxNamedObjPath, &appxNamedObjPathBufLen);
                        swprintf_s(objectNamePrefix, MAX_LONGPATH, W("Global\\Session\\%d\\%s"), sessionId, appxNamedObjPath);
                    }
                }
            }
        }
#endif // TARGET_UNIX
    }
private:

    bool bInitialized;            // true when the BBSweep object has initialized successfully
    bool bTerminate;              // set to true when the CLR wants us to terminate
    HANDLE hSweepMutex;           // prevents processing from incrementing the semaphore after the sweep has began
    HANDLE hProfDataWriterMutex;  // guarantees that profile data will be written by one process at a time
    HANDLE hSweepEvent;           // tells the CLR processes to sweep their profile data
    HANDLE hTerminationEvent;     // set when the CLR process is ready to terminate
    HANDLE hProfWriterSemaphore;  // helps determine when all the writers are finished
    HANDLE hBBSweepThread;        // a handle to the CLR sweeper thread (that calls watch for sweep events)
};

#pragma pop_macro("LocalFree")
#pragma pop_macro("ReleaseSemaphore")
#pragma pop_macro("ResetEvent")
#pragma pop_macro("SetEvent")

#endif //_BBSWEEP_H
