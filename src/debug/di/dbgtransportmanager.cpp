//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#include "stdafx.h"
#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"
#include "coreclrremotedebugginginterfaces.h"

#ifdef FEATURE_DBGIPC_TRANSPORT_DI

//
// Provides access to various process enumeration and control facilities for a remote machine.
//

// The one and only instance of the DbgTransportManager in the process.
DbgTransportManager *g_pDbgTransportManager = NULL;

DbgTransportManager::DbgTransportManager()
{
    memset(this, 0, sizeof(*this));
}

// Startup/shutdown calls. These are ref-counted (cordbg, for instance, is constructed in such a way that
// the command shell will attempt to load mscordbi and initialize an associated DbgTransportManager
// multiple times).
HRESULT DbgTransportManager::Init()
{
    if (InterlockedIncrement(&m_lRefCount) == 1)
    {
        m_sLock.Init("DbgTransportManager Lock", RSLock::cLockFlat, RSLock::LL_DBG_TRANSPORT_MANAGER_LOCK);
        m_pTargets = NULL;
    }

    return S_OK;
}

void DbgTransportManager::Shutdown()
{
    if (InterlockedDecrement(&m_lRefCount) == 0)
    {
        m_sLock.Destroy();

        while (m_pTargets)
        {
            TargetRef *pTargetRef = m_pTargets;
            m_pTargets = pTargetRef->m_pNext;

            pTargetRef->m_pTarget->Shutdown();
            delete pTargetRef->m_pTarget;

            delete pTargetRef;
        }
    }
}

// Attempt to connect to a debugging proxy on the machine at the given address and with the specified port
// number. If the port number is given as zero use the port stored in user debugger configuration. On success
// a pointer to a DbgTransportTarget object will be returned.
HRESULT DbgTransportManager::ConnectToTarget(DWORD dwIPAddress, USHORT usPort, DbgTransportTarget **ppTarget)
{
    RSLockHolder lock(&m_sLock);

    // Look for an existing target with matching IP address and port number.
    TargetRef *pTargetRef = m_pTargets;
    while (pTargetRef)
    {
        // Matches must have identical IP address and port number and must also be in a good connection state
        // (otherwise we're looking at a target that hit a network error and is just waiting until outstanding
        // references to it have been released -- in these circumstances we allow a new target to be allocated
        // in order to re-attempt connection to the proxy).
        if (pTargetRef->m_dwIPAddress == dwIPAddress &&
            pTargetRef->m_usPort == usPort &&
            !pTargetRef->m_pTarget->IsProxyConnectionBad())
        {
            pTargetRef->m_dwRefCount++;
            *ppTarget = pTargetRef->m_pTarget;
            return S_OK;
        }

        pTargetRef = pTargetRef->m_pNext;
    }

    // If we get here there wasn't an appropriate existing entry, so create one.

    // First the reference structure used to track the target.
    pTargetRef = new (nothrow) TargetRef();
    if (pTargetRef == NULL)
        return E_OUTOFMEMORY;

    // Then the target object itself.
    DbgTransportTarget *pTarget = new (nothrow) DbgTransportTarget();
    if (pTargetRef == NULL)
    {
        delete pTargetRef;
        return E_OUTOFMEMORY;
    }

    // Initialize the target (this will attempt a connection to the proxy immediately).
    HRESULT hr = pTarget->Init(dwIPAddress, usPort);
    if (FAILED(hr))
    {
        pTarget->Shutdown();
        delete pTarget;
        delete pTargetRef;
        return hr;
    }

    // Everything's good, go ahead and initialize and link in the target reference.
    pTargetRef->m_dwRefCount = 1;
    pTargetRef->m_pTarget = pTarget;
    pTargetRef->m_dwIPAddress = dwIPAddress;
    pTargetRef->m_usPort = usPort;

    pTargetRef->m_pNext = m_pTargets;
    m_pTargets = pTargetRef;

    *ppTarget = pTarget;
    return S_OK;
}

// Add another reference to a target already acquired by ConnectToTarget (used by clients when they want
// to hand a target out to independent code).
void DbgTransportManager::ReferenceTarget(DbgTransportTarget *pTarget)
{
    RSLockHolder lock(&m_sLock);

    // We need to locate the target reference for this target.
    TargetRef *pTargetRef = m_pTargets;
    while (pTargetRef)
    {
        if (pTargetRef->m_pTarget == pTarget)
        {
            pTargetRef->m_dwRefCount++;
            return;
        }
        pTargetRef = pTargetRef->m_pNext;
    }

    // Shouldn't get here.
    _ASSERTE(FALSE);
}

// Release reference to a DbgTransportTarget. If this is the last active reference then the connection to the
// proxy will be severed and the object deallocated.
void DbgTransportManager::ReleaseTarget(DbgTransportTarget *pTarget)
{
    RSLockHolder lock(&m_sLock);

    // We need to locate the target reference for this target (and the previous reference so we can perform
    // the fixup to remove the entry from the queue if this was the last reference to the target).
    TargetRef *pTargetRef = m_pTargets;
    TargetRef *pLastRef = NULL;
    while (pTargetRef)
    {
        if (pTargetRef->m_pTarget == pTarget)
        {
            pTargetRef->m_dwRefCount--;
            if (pTargetRef->m_dwRefCount == 0)
            {
                // This was the last reference to this particular target. Remove it from the queue and
                // deallocate it.
                if (pLastRef)
                    pLastRef->m_pNext = pTargetRef->m_pNext;
                else
                    m_pTargets = pTargetRef->m_pNext;

                delete pTargetRef;

                pTarget->Shutdown();
                delete pTarget;
            }
            return;
        }

        pLastRef = pTargetRef;
        pTargetRef = pTargetRef->m_pNext;
    }

    // Shouldn't get here.
    _ASSERTE(FALSE);
}

DbgTransportTarget::DbgTransportTarget()
{
    memset(this, 0, sizeof(*this));
}

// Initialization routine called only by the DbgTransportManager.
HRESULT DbgTransportTarget::Init(DWORD dwIPAddress, USHORT usPort)
{
    m_ullLastUpdate = 0;
    m_fShutdown = false;

    // Target platform is initially unknown. This gets set when the proxy replies to our initial GetSystemInfo
    // message.
    m_ePlatform = DTP_Unknown;

    // If a port number hasn't been specified query the debugger configuration for the current user, this will
    // give us the default.
    if (usPort == 0)
    {
        DbgConfiguration sDbgConfig;
        if (!GetDebuggerConfiguration(&sDbgConfig))
        {
            DbgTransportLog(LC_Always, "Failed to locate debugger configuration");
            return CORDBG_E_REMOTE_INVALID_CONFIG;
        }
        _ASSERTE(sDbgConfig.m_fEnabled); // Debugging is always enabled on right side.
        m_usProxyPort = sDbgConfig.m_usProxyPort;
    }
    else
        m_usProxyPort = usPort;

    // Do the same for IP address except the fallback is an environment variable (and after that 127.0.0.1 for
    // local debugging).
    if (dwIPAddress == 0)
    {
        LPWSTR wszProxyIP = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_DbgTransportProxyAddress);
        if (wszProxyIP != NULL)
        {
            int cbReq = WszWideCharToMultiByte(CP_UTF8, 0, wszProxyIP, -1, 0, 0, 0, 0);
            char *szProxyIP = new (nothrow) char[cbReq + 1];
            if (szProxyIP != NULL)
            {
                WszWideCharToMultiByte(CP_UTF8, 0, wszProxyIP, -1, szProxyIP, cbReq + 1, 0,0);
                m_dwProxyIP = DBGIPC_NTOHL(inet_addr(szProxyIP));
            }
            
            REGUTIL::FreeConfigString(wszProxyIP);
        }
        if (m_dwProxyIP == 0)
            m_dwProxyIP = DBGIPC_NTOHL(inet_addr("127.0.0.1"));
    }
    else
        m_dwProxyIP = dwIPAddress;

    // Allocate the connection manager and initialize it.
    m_pConnectionManager = AllocateSecConnMgr();
    if (m_pConnectionManager == NULL)
        return E_OUTOFMEMORY;

    SecConnStatus eStatus = m_pConnectionManager->Initialize();
    if (eStatus != SCS_Success)
    {
        DbgTransportLog(LC_Always, "Failed to initialize connection manager with %u", eStatus);
        switch (eStatus)
        {
        case SCS_OutOfMemory:
            return E_OUTOFMEMORY;
        case SCS_InvalidConfiguration:
            return CORDBG_E_REMOTE_INVALID_CONFIG;
        default:
            return E_FAIL;
        }
    }

    m_sLock.Init("DbgTransportTarget Lock", RSLock::cLockFlat, RSLock::LL_DBG_TRANSPORT_TARGET_LOCK);
    m_fInitLock = true;

    // Outgoing requests are identified with a monotonically increasing ID starting from 1.
    m_dwNextRequestID = 1;

    // We store a singly-linked list of requests to the proxy that haven't been replied yet.
    m_pRequestList = NULL;

    // Attempt to contact the proxy and form a connection to it.
    eStatus = m_pConnectionManager->AllocateConnection(m_dwProxyIP, m_usProxyPort, &m_pConnection);
    if (eStatus == SCS_Success)
        eStatus = m_pConnection->Connect();
    if (eStatus != SCS_Success)
    {
        DbgTransportLog(LC_Always, "Failed to connect to proxy with %u", eStatus);
        switch (eStatus)
        {
        case SCS_OutOfMemory:
            return E_OUTOFMEMORY;
        case SCS_UnknownTarget:
            return CORDBG_E_REMOTE_UNKNOWN_TARGET;
        case SCS_NoListener:
            return CORDBG_E_REMOTE_NO_LISTENER;
        case SCS_NetworkFailure:
            return CORDBG_E_REMOTE_NETWORK_FAILURE;
        case SCS_MismatchedCerts:
            return CORDBG_E_REMOTE_MISMATCHED_CERTS;
        default:
            return E_ABORT;
        }
    }

    // Create a thread used to monitor remote process state.
    m_hProcessEventThread = CreateThread(NULL, 0, ProcessEventWorkerStatic, this, 0, NULL);
    if (m_hProcessEventThread == NULL)
        return E_OUTOFMEMORY;

    // Send the initial message to the proxy which informs it of our protocol version and queries the target
    // platform and protocol version. This must be done after the thread above is started since we rely on
    // this thread to process replies.
    DWORD dwProxyMajorVersion;
    DWORD dwProxyMinorVersion;
    HRESULT hr = MakeProxyRequest(DPMT_GetSystemInfo,
                                  &dwProxyMajorVersion,
                                  &dwProxyMinorVersion,
                                  &m_ePlatform);
    if (FAILED(hr))
    {
        DbgTransportLog(LC_Always, "GetSystemInfo request to proxy failed with %08X", hr);
        return hr;
    }

    // Check that we can deal with the proxy's protocol.
    if (dwProxyMajorVersion != kCurrentMajorVersion)
    {
        DbgTransportLog(LC_Always, "Don't understand proxy protocol v%u.%u",
                        dwProxyMajorVersion, dwProxyMinorVersion);
        return CORDBG_E_REMOTE_MISMATCHED_PROTOCOLS;
    }

    m_fProxyConnectionBad = false;

    return S_OK;
}

// Shutdown routine called only by the DbgTransportManager.
void DbgTransportTarget::Shutdown()
{
    DbgTransportLog(LC_Always, "DbgTransportTarget shutting down");

    m_fShutdown = true;
    m_fProxyConnectionBad = true;

    if (m_hProcessEventThread)
    {
        // Unwedge the process event thread if it's blocked in a Receive().
        m_pConnection->CancelReceive();

        // Wait for the process event thread to see the shutdown status and close itself down.
        WaitForSingleObject(m_hProcessEventThread, INFINITE);
        CloseHandle(m_hProcessEventThread);
    }

    // Cleanup process list.
    DeallocateProcessList(m_pProcessList);

    if (m_pConnection)
        m_pConnection->Destroy();

    if (m_pConnectionManager)
        m_pConnectionManager->Destroy();

    if (m_fInitLock)
        m_sLock.Destroy();
}

// Indicates when the connection to a proxy has failed: this target object will remain until the last
// reference to it is released (DbgTransportManager::ReleaseTransport) but will fail all further requests. The
// manager will then allow a new attempt to create a connection to the proxy to be made (wrapped in a new
// DbgTransportTarget).
bool DbgTransportTarget::IsProxyConnectionBad()
{
    return m_fProxyConnectionBad;
}

// Fill caller allocated table at pdwProcesses with the pids of processes currently alive on the target
// system. Return the number of slots filled in *pcProcesses. The size of the table is given by cSlots. If
// more than this number of processes are alive then *pcProcesses is set to the total number and E_ABORT
// returned.
HRESULT DbgTransportTarget::EnumProcesses(DWORD *pdwProcesses, DWORD cSlots, DWORD *pcProcesses)
{
    if (m_fProxyConnectionBad)
        return E_ABORT;

    *pcProcesses = 0;

    // Get an up-to-date process list from the proxy.
    UpdateProcessList();

    // Must access the process list under the lock.
    {
        RSLockHolder lock(&m_sLock);

        // Populate the output table from the new process list.
        DWORD i = 0;
        DWORD cSlotsLeft = cSlots;

        // Fill the output table with as many process IDs as we have (or until we run out of slots). Carry on
        // to the end of the process regardless so we can report how many processes there actually are.
        for (ProcessEntry *pProcess = m_pProcessList; pProcess; pProcess = pProcess->m_pNext)
        {
            // Entries for dead processes can persist until an associated transport is released. Don't report
            // these.
            if (pProcess->m_fExited)
                continue;

            if (cSlotsLeft)
            {
                pdwProcesses[i] = pProcess->m_dwPID;
                cSlotsLeft--;
            }

            i++;
        }

        // Return total count to caller.
        *pcProcesses = i;

    } // Leave lock

    return *pcProcesses > cSlots ? E_ABORT : S_OK;
}

// Given a PID attempt to find or create a DbgTransportSession instance to manage a connection to a runtime in
// that process. Returns E_UNEXPECTED if the process can't be found. Also returns a handle that can be waited
// on for process termination.
HRESULT DbgTransportTarget::GetTransportForProcess(DWORD                   dwPID,
                                                   DbgTransportSession   **ppTransport,
                                                   HANDLE                 *phProcessHandle)
{
    if (m_fProxyConnectionBad)
        return E_ABORT;

    // Get an up-to-date process list from the proxy.
    UpdateProcessList();

    // Process list can only be examined under the lock.
    {
        RSLockHolder lock(&m_sLock);

        // Scan each process in the list.
        ProcessEntry *pProcess = m_pProcessList;
        while (pProcess)
        {
            if (pProcess->m_dwPID == dwPID)
            {
                // We've found a match.
                if (pProcess->m_fExited)
                {
                    // But it was for a dead process. Don't report this one (though we know the process is dead so
                    // return E_UNEXPECTED).
                    return E_UNEXPECTED;
                }

              RetryTransport:
                // If we already know about runtimes in this process then attempt to attach to the first one.
                // CORECLRTODO: In the next version we'll wire up the additional logic to enable the caller to
                // indicate which runtime they want to target within a single process.
                if (pProcess->m_pRuntimes)
                {
                    RuntimeEntry *pRuntime = pProcess->m_pRuntimes;

                    // If we have a runtime entry already then the LS is already present and we know the port
                    // to connect to. If there's already a transport in place then we can (and must) use that.
                    // Otherwise we can allocate and initialize one based on the port information.
                    DbgTransportSession *pTransport = pRuntime->m_pDbgTransport;
                    if (pTransport == NULL)
                    {
                        // No transport yet, allocate one.
                        pTransport = new (nothrow) DbgTransportSession();
                        if (pTransport == NULL)
                            return E_OUTOFMEMORY;

                        // Initialize it (this immediately starts the remote connection process).
                        HRESULT hr = pTransport->Init(m_dwProxyIP, pRuntime->m_usPort, pProcess->m_hExitedEvent);
                        if (FAILED(hr))
                        {
                            lock.Release();
                            pTransport->Shutdown();
                            delete pTransport;
                            return hr;
                        }

                        pRuntime->m_pDbgTransport = pTransport;
                    }

                    // One more caller knows about this transport instance. (Which in turn is another reason
                    // the process can't be deleted yet).
                    pRuntime->m_cTransportRef++;
                    pProcess->m_cProcessRef++;

                    *ppTransport = pTransport;
                    if (!DuplicateHandle(GetCurrentProcess(), 
                                         pProcess->m_hExitedEvent,
                                         GetCurrentProcess(), 
                                         phProcessHandle,
                                         0,      // ignored since we are going to pass DUPLICATE_SAME_ACCESS
                                         FALSE, 
                                         DUPLICATE_SAME_ACCESS))
                    {
                        lock.Release();
                        return HRESULT_FROM_GetLastError();
                    }
                    return S_OK;
                }

                // If we get here we've found the process record but there's no known runtime yet. The proxy
                // will send us an event if either a runtime starts up or the process dies, so we'll wait on
                // both of these. We can't wait with the lock held so increment the ref count on the process
                // (to keep the entry and the events we're about to wait on valid) and drop the lock first.

                pProcess->m_cProcessRef++;
                lock.Release();

                // We need to send an early attach notification to the proxy so that when the next runtime
                // starts up and registers it will know to suspend itself until we attach (i.e. this is the
                // early attach). Obviously we're racing with runtime startup here but that's by definition.
                bool fProcessExited;
                HRESULT hr = MakeProxyRequest(DPMT_EarlyAttach, pProcess->m_pruidProcess, &fProcessExited);
                if (FAILED(hr))
                {
                    lock.Acquire();
                    pProcess->m_cProcessRef--;
                    return hr;
                }

                // The process might have managed to exit before we even built a process entry for it on this
                // side. In that case a process termination might have been missed. Checking for the exit
                // status again with the EarlyAttach request above closes the hole (we establish a process
                // entry and lock it in place so any termination events from that point on will be caught,
                // then we fire an EarlyAttach and check the current status).
                if (fProcessExited)
                {
                    lock.Acquire();
                    pProcess->m_cProcessRef--;
                    pProcess->m_fExited = true;
                    SetEvent(pProcess->m_hExitedEvent);
                    return E_UNEXPECTED;
                }

                DbgTransportLog(LC_Always, "Waiting on runtime starting or process termination for %08X(%u, %u)",
                                pProcess, pProcess->m_dwPID, pProcess->m_pruidProcess);

                HANDLE rgEvents[] = { pProcess->m_hRuntimeStartedEvent, pProcess->m_hExitedEvent };
                DWORD dwResult = WaitForMultipleObjectsEx(2, rgEvents, FALSE, INFINITE, FALSE);
                _ASSERTE(dwResult == WAIT_OBJECT_0 || dwResult == (WAIT_OBJECT_0 + 1));

                DbgTransportLog(LC_Always, "    %s", dwResult == WAIT_OBJECT_0 ? "Runtime started" : "Process terminated");

                // Take the lock again and determine what our status is.
                lock.Acquire();

                // We have no further need to keep this process record alive (once we drop the lock).
                _ASSERTE(pProcess->m_cProcessRef > 0);
                pProcess->m_cProcessRef--;

                // If the process terminated then exit with E_UNEXPECTED. Note that this might be a zombie
                // entry marked with m_fExited = true in this case, but rather than duplicate entry cleanup
                // code we'll let the next process list update flush this record (now that the ref count has
                // been decremented).
                if (dwResult == (WAIT_OBJECT_0 + 1))
                    return E_UNEXPECTED;

                // We should have at least one runtime entry now; just jump back to the code that knows how to
                // re-use or allocate a transport on it.
                _ASSERTE(pProcess->m_pRuntimes);
                goto RetryTransport;
            }

            pProcess = pProcess->m_pNext;
        }
    } // Leave lock

    // Didn't find a process with a matching PID.
    return E_UNEXPECTED;
}

// Returns true if the given PID identifies a running process which is hosting at least one CoreCLR
// runtime.
bool DbgTransportTarget::IsManagedProcess(DWORD dwPID)
{
    // Maybe we already know the process is managed.
    {
        RSLockHolder lock(&m_sLock);
        ProcessEntry *pProcess = LocateProcessByPID(dwPID);
#ifdef _PREFAST_ 
#pragma warning(push)
#pragma warning(disable:6011) // Prefast doesn't understand the guard to avoid de-referencing a NULL pointer below.
#endif // _PREFAST_
        if (pProcess && pProcess->m_pRuntimes)
            return true;
#ifdef _PREFAST_ 
#pragma warning(pop)
#endif // _PREFAST_
    } // Leave lock

    // Get an up-to-date process list from the proxy in case we've haven't done this for a while and a runtime
    // has started up in the meantime.
    UpdateProcessList();

    // Try once again.
    {
        RSLockHolder lock(&m_sLock);
        ProcessEntry *pProcess = LocateProcessByPID(dwPID);
        return pProcess ? pProcess->m_pRuntimes != NULL : false;
    } // Leave lock
}

// Release another reference to the transport associated with dwPID. Once all references are gone (modulo the
// manager's own weak reference) clean up the transport and deallocate it.
void DbgTransportTarget::ReleaseTransport(DbgTransportSession *pTransport)
{
    DbgTransportSession *pTransportToShutdown = NULL;

    // Process list can only be examined under the lock.
    {    
        RSLockHolder lock(&m_sLock);

        // Scan all processes we know about.
        ProcessEntry *pProcess = m_pProcessList;
        while (pProcess)
        {
            // Scan each runtime we know about in the current process.
            RuntimeEntry *pRuntime = pProcess->m_pRuntimes;
            while (pRuntime)
            {
                if (pRuntime->m_pDbgTransport == pTransport)
                {
                    // Found it.

                    // Decrement the transport ref count. This is also one less reason to hold onto the
                    // process record.
                    _ASSERTE(pRuntime->m_cTransportRef > 0 && pProcess->m_cProcessRef > 0);
                    pRuntime->m_cTransportRef--;
                    pProcess->m_cProcessRef--;

                    // If nobody references this transport any more we can shut it down and delete it. Don't
                    // do this under the lock however.
                    if (pRuntime->m_cTransportRef == 0)
                    {
                        pTransportToShutdown = pRuntime->m_pDbgTransport;
                        pRuntime->m_pDbgTransport = NULL;
                    }

                    lock.Release();

                    // If we made the transport inaccessible above we can shut it down and deallocate it now.
                    if (pTransportToShutdown)
                    {
                        pTransportToShutdown->Shutdown();
                        delete pTransportToShutdown;
                    }

                    return;
                }

                pRuntime = pRuntime->m_pNext;
            }

            pProcess = pProcess->m_pNext;
        }
    } // Leave lock

    _ASSERTE(!"Failed to find ProcessEntry to release transport reference");
}

// Run the command line given on the remote machine to create a process. Return the PID of this process. When
// and if the process starts a runtime and registers with the proxy it will be told to halt and wait for a
// debugger attach.
HRESULT DbgTransportTarget::CreateProcess(LPCWSTR  wszCommand,
                                          LPCWSTR  wszArgs,
                                          LPCWSTR  wszCurrentDirectory,
                                          LPVOID   pvEnvironment,
                                          DWORD   *pdwPID)
{
    if (m_fProxyConnectionBad)
        return E_ABORT;

    DWORD cchCommand = wszCommand ? wcslen(wszCommand) : 0;
    DWORD cchArgs = wszArgs ? wcslen(wszArgs) : 0;

    // Proxy expects the command line as a single string.
    LPWSTR wszCommandLine = (LPWSTR)_alloca((cchCommand + 1 + cchArgs + 1) * sizeof(WCHAR));
    wszCommandLine[0] = W('\0');
    if (wszCommand)
    {
        wcscat(wszCommandLine, wszCommand);
        wcscat(wszCommandLine, W(" "));
    }
    if (wszArgs)
        wcscat(wszCommandLine, wszArgs);

    // Check how big a UTF8 version of the command line would be.
    int cbReqd = WszWideCharToMultiByte(CP_UTF8, 0, wszCommandLine, -1, 0, 0, 0, 0);

    LPSTR szCommandLine = (LPSTR)_alloca(cbReqd);

    // Do the conversion from 16-bit.
    WszWideCharToMultiByte(CP_UTF8, 0, wszCommandLine, -1, szCommandLine, cbReqd, 0, 0);

    // If a default directory is supplied then convert it to UTF8.
    LPSTR szCurrentDirectory = NULL;
    if (wszCurrentDirectory)
    {
        cbReqd = WszWideCharToMultiByte(CP_UTF8, 0, wszCurrentDirectory, -1, 0, 0, 0, 0);
        szCurrentDirectory = (LPSTR)_alloca(cbReqd);
        WszWideCharToMultiByte(CP_UTF8, 0, wszCurrentDirectory, -1, szCurrentDirectory, cbReqd, 0, 0);
    }

    // Prepare to format an attribute block containing all the launch parameters we'll send to the proxy.
    // There are two phases: first we plan how much space will be required in the block then we allocate the
    // block and fill it in.
    DbgAttributeBlockWriter sAttrWriter;

    sAttrWriter.ScheduleStringValue(szCommandLine);

    if (szCurrentDirectory)
        sAttrWriter.ScheduleStringValue(szCurrentDirectory);

    // Determine how large the environment block is (if it's supplied).
    DWORD cbEnvironment = 0;
    if (pvEnvironment)
    {
        char *szEnv = (char *)pvEnvironment;

        // The environment is a series of nul-terminated strings followed by a final nul.
        while (*szEnv)
        {
            DWORD cbString = strlen(szEnv) + 1;
            cbEnvironment += cbString;
            szEnv += cbString;
        }

        // Account for final nul.
        cbEnvironment++;
    }

    if (cbEnvironment)
        sAttrWriter.ScheduleValue(cbEnvironment);

    // By now we know how large an attribute block we need.
    DWORD   cbAttributeBlock = sAttrWriter.GetRequiredBufferSize();
    BYTE   *pbAttributeBlock = new (nothrow) BYTE[cbAttributeBlock];
    if (pbAttributeBlock == NULL)
        return E_OUTOFMEMORY;

    // Initialize the attribute block.
    sAttrWriter.BeginFormatting((char*)pbAttributeBlock);

    sAttrWriter.AddStringValue(DAT_CommandLine, szCommandLine);
    if (szCurrentDirectory)
        sAttrWriter.AddStringValue(DAT_DefaultDirectory, szCurrentDirectory);
    if (cbEnvironment)
        sAttrWriter.AddValue(DAT_Environment, (char*)pvEnvironment, cbEnvironment);

    // Allocate a new process entry up front (but don't link it into the list until we know we've created the
    // remote process).
    ProcessEntry *pProcess = new (nothrow) ProcessEntry();
    if (pProcess == NULL)
    {
        delete [] pbAttributeBlock;
        return E_OUTOFMEMORY;
    }
    memset(pProcess, 0, sizeof(ProcessEntry));

    strncpy(pProcess->m_szCommandLine, szCommandLine, kMaxCommandLine);
    pProcess->m_szCommandLine[kMaxCommandLine - 1] = '\0';

    pProcess->m_hExitedEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
    if (pProcess->m_hExitedEvent == NULL)
    {
        delete [] pbAttributeBlock;
        delete pProcess;
        return E_OUTOFMEMORY;
    }

    pProcess->m_hRuntimeStartedEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
    if (pProcess->m_hRuntimeStartedEvent == NULL)
    {
        delete [] pbAttributeBlock;
        delete pProcess;
        return E_OUTOFMEMORY;
    }

    // Send the launch request to the proxy. It will reply with a PID or an hresult on failure.
    HRESULT hr = MakeProxyRequest(DPMT_LaunchProcess,
                                  pbAttributeBlock,
                                  &pProcess->m_dwPID,
                                  &pProcess->m_pruidProcess);

    delete [] pbAttributeBlock;

    if (SUCCEEDED(hr))
    {
        // The remote process has been created.
        *pdwPID = pProcess->m_dwPID;

        // Take the lock and check whether we already have an entry for this process (this can happen due to
        // an EnumProcesses from another thread).
        {
            RSLockHolder lock(&m_sLock);
            ProcessEntry *pSearchProcess = m_pProcessList;
            while (pSearchProcess)
            {
                if (pSearchProcess->m_pruidProcess == pProcess->m_pruidProcess)
                    break;
                pSearchProcess = pSearchProcess->m_pNext;
            }

            if (pSearchProcess)
            {
                // Someone else has already made the update. Discard our unneeded copy.
                delete pProcess;
            }
            else
            {
                // No current entry for this process, link it in.
                pProcess->m_pNext = m_pProcessList;
                m_pProcessList = pProcess;
            }
        } // Leave lock
    }
    else
    {
        // The process was not created, throw away the process entry we'd prepared.
        delete pProcess;
    }

    return hr;
}

// Kill the process identified by PID.
void DbgTransportTarget::KillProcess(DWORD dwPID)
{
    if (m_fProxyConnectionBad)
        return;

    PRUID pruidTarget = 0;

    // Look up the process by PID so we can find the corresponding PRUID (which is the proxy's version of the
    // PID).
    {
        RSLockHolder lock(&m_sLock);
        ProcessEntry *pProcess = LocateProcessByPID(dwPID);
        if (pProcess)
            pruidTarget = pProcess->m_pruidProcess;
    } // Leave lock

    if (pruidTarget)
    {
        HRESULT hr = MakeProxyRequest(DPMT_TerminateProcess, pruidTarget);
        if (FAILED(hr))
        {
            // Network failure could prevent our terminate from getting through. We don't currently support
            // rebuilding a network connection and retrying, so report the process as dead to prevent a hang
            // in the debugger on this end.
            RSLockHolder lock(&m_sLock);
            ProcessEntry *pProcess = LocateProcessByPID(dwPID);
            if (pProcess)
            {
                pProcess->m_fExited = true;
                SetEvent(pProcess->m_hExitedEvent);
            }
        } // Leave lock
    }
}

// Ask the remote debugger proxy for an updated list of processes and reflect these changes into our local
// process list. Any failure will leave the current process list state unchanged.
void DbgTransportTarget::UpdateProcessList()
{
    // As an optimization, don't update the process list more than once a second.
    if ((CLRGetTickCount64() - m_ullLastUpdate) <= 1000)
        return;
    m_ullLastUpdate = CLRGetTickCount64();

    // Send message to the proxy asking for a list of processes and CoreCLR instances. By the time the
    // MakeProxyRequest request call below completes the process/runtime database will have been updated.
    MakeProxyRequest(DPMT_EnumProcesses);
}

// Locate a process entry by PID. Assumes the lock is already held.
DbgTransportTarget::ProcessEntry *DbgTransportTarget::LocateProcessByPID(DWORD dwPID)
{
    _ASSERTE(m_sLock.HasLock());

    ProcessEntry *pProcess = m_pProcessList;
    while (pProcess)
    {
        if (pProcess->m_dwPID == dwPID)
            return pProcess;
        pProcess = pProcess->m_pNext;
    }
    return NULL;
}

DbgTransportTarget::RuntimeEntry::~RuntimeEntry()
{
    // If there's still a transport attached to the remote runtime shut it down and delete it.
    if (m_pDbgTransport)
    {
        m_pDbgTransport->Shutdown();
        delete m_pDbgTransport;
    }
}

DbgTransportTarget::ProcessEntry::~ProcessEntry()
{
    // Clean up any records for runtimes hosted within this process.
    while (m_pRuntimes)
    {
        RuntimeEntry *pDelRuntime = m_pRuntimes;
        m_pRuntimes = m_pRuntimes->m_pNext;
        delete pDelRuntime;
    }

    if (m_hExitedEvent)
        CloseHandle(m_hExitedEvent);

    if (m_hRuntimeStartedEvent)
        CloseHandle(m_hRuntimeStartedEvent);
}

// Deallocate all resources associated with a process list.
void DbgTransportTarget::DeallocateProcessList(ProcessEntry *pProcessList)
{
    while (pProcessList)
    {
        ProcessEntry *pDelProcess = pProcessList;
        pProcessList = pProcessList->m_pNext;
        delete pDelProcess;
    }
}

// Format and send a request to the proxy then wait on a reply (if appropriate) and return results to the
// caller.
HRESULT DbgTransportTarget::MakeProxyRequest(DbgProxyMessageType eType, ...)
{
    va_list args;

    va_start(args, eType);

    // We allocate space for some request related context on the stack. For the duration of the request (until
    // a reply is received) this context is linked on a global request queue so the process event thread can
    // handle matching replies to requests.
    Request sRequest;
    sRequest.m_pNext = NULL;
    sRequest.m_hrResult = S_OK;

    // DPMT_TerminateProcess requests don't expect a reply.
    if (eType != DPMT_TerminateProcess)
    {
        sRequest.m_hCompletionEvent = WszCreateEvent(NULL, FALSE, FALSE, NULL); // Auto-reset, not signalled
        if (sRequest.m_hCompletionEvent == NULL)
            return E_OUTOFMEMORY;
    }
    else
        sRequest.m_hCompletionEvent = NULL;

    // Space for the message header.
    DbgProxyMessageHeader sMessage;

    // Format common message fields.
    memset(&sMessage, 0, sizeof(sMessage));
    sMessage.m_eType = eType;

    // Based on request type fill in the remainder of the request fields and record the addresses of the
    // caller's output buffer(s) if any.
    BYTE *pbAttributeBlock = NULL;
    DWORD cbAttributeBlock = 0;
    switch (eType)
    {
    case DPMT_GetSystemInfo:
        DbgTransportLog(LC_Always, "Sending 'GetSystemInfo'");
        sMessage.VariantData.GetSystemInfo.m_uiMajorVersion = kCurrentMajorVersion;
        sMessage.VariantData.GetSystemInfo.m_uiMinorVersion = kCurrentMinorVersion;
        sRequest.OutputBuffers.GetSystemInfo.m_pdwMajorVersion = va_arg(args, DWORD*);
        sRequest.OutputBuffers.GetSystemInfo.m_pdwMinorVersion = va_arg(args, DWORD*);
        sRequest.OutputBuffers.GetSystemInfo.m_pePlatform = va_arg(args, DbgTargetPlatform*);
        break;

    case DPMT_EnumProcesses:
        DbgTransportLog(LC_Always, "Sending 'EnumProcesses'");
        break;

    case DPMT_LaunchProcess:
    {
        DbgTransportLog(LC_Always, "Sending 'LaunchProcess'");
        pbAttributeBlock = va_arg(args, BYTE*);
        DbgAttributeBlockReader sAttrReader((char*)pbAttributeBlock);
        cbAttributeBlock = sAttrReader.GetBlockSize();
        sMessage.VariantData.LaunchProcess.m_cbAttributeBlock = cbAttributeBlock;
        sRequest.OutputBuffers.LaunchProcess.m_pdwPID = va_arg(args, DWORD*);
        sRequest.OutputBuffers.LaunchProcess.m_ppruidProcess = va_arg(args, PRUID*);
        break;
    }

    case DPMT_EarlyAttach:
        DbgTransportLog(LC_Always, "Sending 'EarlyAttach'");
        sMessage.VariantData.EarlyAttach.m_pruidProcess = va_arg(args, DWORD);
        sRequest.OutputBuffers.EarlyAttach.m_pfProcessExited = va_arg(args, bool*);
        break;

    case DPMT_TerminateProcess:
        DbgTransportLog(LC_Always, "Sending 'TerminateProcess'");
        sMessage.VariantData.TerminateProcess.m_pruidProcess = va_arg(args, DWORD);
        break;

    default:
        _ASSERTE(!"Illegal message type for MakeProxyRequest");
        va_end(args);
        return E_FAIL;
    }
    va_end(args);

    // We must hold the lock in order to send messages, allocate request IDs or touch the request queue.
    {
        RSLockHolder lock(&m_sLock);

        // While under the lock we can check the connection state without races. We either see the connection
        // state is bad and abort the operation now or we successfully queue the request (in which case it is
        // the process event thread's responsibility to abort the request if an error occurs).
        if (m_fProxyConnectionBad)
        {
            if (sRequest.m_hCompletionEvent)
                CloseHandle(sRequest.m_hCompletionEvent);
            return E_ABORT;
        }

        // Allocate a request ID and add the request to the queue (except for messages that don't expect a
        // reply).
        if (sRequest.m_hCompletionEvent != NULL)
        {
            // Allocate a unique ID for this request. This will allow us to match the reply that comes back.
            sRequest.m_dwID = sMessage.m_uiRequestID = m_dwNextRequestID++;

            // The request queue is not ordered, so just place the new request at the head.
            sRequest.m_pNext = m_pRequestList;
            m_pRequestList = &sRequest;
        }
        else
            sMessage.m_uiRequestID = 0;

        // Now the type and request ID have been filled in we can calculate the value of the magic field used
        // as an extra layer of validation in the message format.
        sMessage.m_uiMagic = DBGPROXY_MAGIC_VALUE(&sMessage);

        // Send the message header.
        if (!m_pConnection->Send((unsigned char*)&sMessage, sizeof(sMessage)))
        {
            DbgTransportLog(LC_Always, "DbgTransportTarget::MakeProxyRequest(): Send() failed");
            if (sRequest.m_hCompletionEvent)
            {
                m_pRequestList = sRequest.m_pNext;
                CloseHandle(sRequest.m_hCompletionEvent);
            }

            return E_ABORT;
        }

        // Launch requests have additional data (an attribute block).
        if (eType == DPMT_LaunchProcess)
        {
            _ASSERTE(pbAttributeBlock && cbAttributeBlock);

            if (!m_pConnection->Send(pbAttributeBlock, cbAttributeBlock))
            {
                DbgTransportLog(LC_Always, "DbgTransportTarget::MakeProxyRequest(): Send() failed");

                m_pRequestList = sRequest.m_pNext;
                CloseHandle(sRequest.m_hCompletionEvent);

                return E_ABORT;
            }
        }

    } // Leave lock

    // We're done if we don't expect a reply.
    if (sRequest.m_hCompletionEvent == NULL)
        return S_OK;

    // Now wait on the completion event (this will be signalled by the process event thread once it has
    // matched a reply to our request successfully).
    WaitForSingleObject(sRequest.m_hCompletionEvent, INFINITE);

    // No more need for the completionm event.
    CloseHandle(sRequest.m_hCompletionEvent);

    // Return the completion result from the transmission record.
    return sRequest.m_hrResult;
}

// Static entry point for the process event thread.
DWORD WINAPI DbgTransportTarget::ProcessEventWorkerStatic(LPVOID lpvContext)
{
    // Just dispatch straight to the version that's an instance method.
    ((DbgTransportTarget*)lpvContext)->ProcessEventWorker();
    return 0;
}

// Instance method version of the worker, called from ProcessEventWorkerStatic().
void DbgTransportTarget::ProcessEventWorker()
{
    // Loop handling requests until we're told to shutdown or hit a network error.
    while (!m_fShutdown)
    {
        // How the worker reacts to the incoming data is driven by shared state set up by other threads in
        // this process calling MakeProxyRequest(). These calls cause a message to be sent directly to the
        // proxy but also set up state so that this thread will know how to dispatch replies from the proxy
        // back to the originating thread.

        // There are three sizes of message that we can receive: most messages will fit in the common message
        // header but DPMT_RuntimeStarted needs something a little larger (an additional DbgProxyRuntimeInfo
        // structure) and DPMT_ProcessList includes an variable sized list of process and runtime records.
        // Allocate storage for a common header on the stack and always receive the header into this. We'll
        // handle any extra data required on a case by case basis (which is easy since there's no requirement
        // to actually assemble the incoming message into a single contiguous buffer at any point).
        DbgProxyMessageHeader sMessage;

        if (!m_pConnection->Receive((unsigned char *)&sMessage, sizeof(sMessage)))
        {
            DbgTransportLog(LC_Always, "DbgTransportTarget: Receive() failed");
            goto NetworkError;
        }

        // Validate the magic number in the header that we use as an additional check of the messages's
        // integrity (this makes it much harder to launch a network attack based on sending random data).
        if (sMessage.m_uiMagic != DBGPROXY_MAGIC_VALUE(&sMessage))
        {
            DbgTransportLog(LC_Always, "DbgTransportTarget: message failed magic number test");
            goto NetworkError;
        }

        // Most of the incoming messages are replies to requests that we have on our request queue. Locate the
        // original request (keyed off the ID returned in the reply).
        Request *pRequest = NULL;
        if (sMessage.m_eType != DPMT_RuntimeStarted &&
            sMessage.m_eType != DPMT_ProcessTerminated)
        {
            pRequest = LocateOriginalRequest(&sMessage);
            if (pRequest == NULL)
            {
                DbgTransportLog(LC_Always, "DbgTransportTarget: can't find request for reply %u",
                                (unsigned)sMessage.m_uiRequestID);
                goto NetworkError;
            }
        }

        // Process the rest of the message based on the type.
        switch (sMessage.m_eType)
        {
        case DPMT_SystemInfo:
            DbgTransportLog(LC_Always, "Received 'SystemInfo'");
            PREFIX_ASSUME(pRequest != NULL);

            *pRequest->OutputBuffers.GetSystemInfo.m_pdwMajorVersion =
                sMessage.VariantData.SystemInfo.m_uiMajorVersion;
            *pRequest->OutputBuffers.GetSystemInfo.m_pdwMinorVersion =
                sMessage.VariantData.SystemInfo.m_uiMinorVersion;
            *pRequest->OutputBuffers.GetSystemInfo.m_pePlatform =
                sMessage.VariantData.SystemInfo.m_ePlatform;
            break;

        case DPMT_ProcessList:
            DbgTransportLog(LC_Always, "Received 'ProcessList(%u, %u)'",
                            (unsigned)sMessage.VariantData.ProcessList.m_uiProcessRecords,
                            (unsigned)sMessage.VariantData.ProcessList.m_uiRuntimeRecords);
            PREFIX_ASSUME(pRequest != NULL);

            ProcessProcessList(&sMessage, pRequest);
            if (FAILED(pRequest->m_hrResult))
                goto NetworkError;
            break;

        case DPMT_ProcessLaunched:
            DbgTransportLog(LC_Always, "Received 'ProcessLaunched'");
            PREFIX_ASSUME(pRequest != NULL);

            // We successfully launched a process remotely (or an error code indicating why we could not).
            // On success copy PID and PRUID of the new process back to the requester.
            switch (sMessage.VariantData.ProcessLaunched.m_eResult)
            {
            case DPLR_Success:
                pRequest->m_hrResult = S_OK;
                *pRequest->OutputBuffers.LaunchProcess.m_pdwPID =
                    sMessage.VariantData.ProcessLaunched.m_uiPID;
                *pRequest->OutputBuffers.LaunchProcess.m_ppruidProcess =
                    sMessage.VariantData.ProcessLaunched.m_pruidProcess;
                break;
            case DPLR_OutOfMemory:
                pRequest->m_hrResult = E_OUTOFMEMORY;
                break;
            case DPLR_Denied:
                pRequest->m_hrResult = E_ACCESSDENIED;
                break;
            case DPLR_NotFound:
                pRequest->m_hrResult = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                break;
            case DPLR_UnspecifiedError:
                pRequest->m_hrResult = E_FAIL;
                break;
            default:
                _ASSERTE(!"Unknown ProcessLaunched result code");
                pRequest->m_hrResult = E_FAIL;
            }
            break;

        case DPMT_RuntimeStarted:
        {
            DbgTransportLog(LC_Always, "Received 'RuntimeStarted'");

            // RuntimeStarted sends a process info block as well as runtime info (in case the process is new
            // as well).

            // Read DbgProxyProcessInfo structure.
            DbgProxyProcessInfo sProcessInfo;
            if (!m_pConnection->Receive((unsigned char *)&sProcessInfo, sizeof(sProcessInfo)))
            {
                DbgTransportLog(LC_Always, "DbgTransportTarget: Receive() failed");
                goto NetworkError;
            }

            // Read DbgProxyRuntimeInfo structure.
            DbgProxyRuntimeInfo sRuntimeInfo;
            if (!m_pConnection->Receive((unsigned char *)&sRuntimeInfo, sizeof(sRuntimeInfo)))
            {
                DbgTransportLog(LC_Always, "DbgTransportTarget: Receive() failed");
                goto NetworkError;
            }

            // A runtime has started up in some process on the target machine.
            // Add a runtime record to our database (if it's not already there).
            if (!ProcessRuntimeStarted(&sProcessInfo, &sRuntimeInfo))
                goto NetworkError;
            break;
        }

        case DPMT_ProcessTerminated:
        {
            DbgTransportLog(LC_Always, "Received 'ProcessTerminated'");

            // A process has terminated on the target machine.
            // See if we were tracking the process on our side and if so either delete the entry (if it's
            // not being used) or fire the process termination event.
            {
                RSLockHolder lock(&m_sLock);

                ProcessEntry *pProcess = m_pProcessList;
                ProcessEntry *pLastProcess = NULL;
                while (pProcess)
                {
                    if (pProcess->m_pruidProcess == sMessage.VariantData.ProcessTerminated.m_pruidProcess)
                    {
                        // Found a matching entry. Is it in use?
                        if (pProcess->m_cProcessRef > 0)
                        {
                            // Can't delete the entry. Signal the process exited event (which will move some
                            // users off).
                            SetEvent(pProcess->m_hExitedEvent);
                            pProcess->m_fExited = true;
                        }
                        else
                        {
                            // Nobody's using this process entry, we can unlink and deallocate it.
                            if (pLastProcess)
                                pLastProcess->m_pNext = pProcess->m_pNext;
                            else
                                m_pProcessList = pProcess->m_pNext;
                            delete pProcess;
                        }
                        break;
                    }
                    pLastProcess = pProcess;
                    pProcess = pProcess->m_pNext;
                }
            } // Leave lock
            break;
        }

        case DPMT_EarlyAttachDone:
            DbgTransportLog(LC_Always, "Received 'EarlyAttachDone(%s)'",
                            sMessage.VariantData.EarlyAttachDone.m_fProcessExited ? "dead" : "alive");
            PREFIX_ASSUME(pRequest != NULL);

            // The only thing we need to do here is pass back the indication of whether the process
            // managed to exit before the attach was registered.
            *pRequest->OutputBuffers.EarlyAttach.m_pfProcessExited =
                sMessage.VariantData.EarlyAttachDone.m_fProcessExited;
            break;

        default:
            _ASSERTE(!"Invalid message typr");
        }

        // If this was a reply then the original request has been updated at this point. We only need to
        // remove it from the global queue and signal the completion event to unblock the requesting thread.
        if (pRequest)
        {
            RSLockHolder lock(&m_sLock);

            // Look for the previous item in the queue.
            Request *pSearchRequest = m_pRequestList;
            while (pSearchRequest)
            {
                if (pSearchRequest->m_pNext == pRequest)
                {
                    pSearchRequest->m_pNext = pRequest->m_pNext;
                    break;
                }
                pSearchRequest = pSearchRequest->m_pNext;
            }

            // No match, maybe the transmission was at the head of the queue.
            if (pSearchRequest == NULL)
            {
                _ASSERTE(m_pRequestList == pRequest);
                m_pRequestList = pRequest->m_pNext;
            }

            // Now complete the request to the caller.
            SetEvent(pRequest->m_hCompletionEvent);
        } // Leave lock

        // Loop round for the next message.
    }

  NetworkError:

    // We get here if we were asked to shutdown or hit a network error.
    m_fProxyConnectionBad = true;

    // Abort any outstanding requests.
    {
        RSLockHolder lock(&m_sLock);

        Request *pRequest = m_pRequestList;
        while (pRequest)
        {
            Request *pAbortRequest = pRequest;
            pRequest = pRequest->m_pNext;

            pAbortRequest->m_hrResult = E_ABORT;
            SetEvent(pAbortRequest->m_hCompletionEvent);
        }
    } // Leave lock

    // If this isn't shutdown (i.e. we got here as the result of a network error) run through the process list
    // and report them all as terminated (better than having the debugger time-out some request and then hang
    // forever as it tries to terminate the process itself).
    if (!m_fShutdown)
    {
        for (ProcessEntry *pProcess = m_pProcessList; pProcess; pProcess = pProcess->m_pNext)
        {
            if (!pProcess->m_fExited)
            {
                pProcess->m_fExited = true;
                SetEvent(pProcess->m_hExitedEvent);
            }
        }
    }
}

// If this message is a reply to a right-side request locate that request. Otherwise return NULL.
DbgTransportTarget::Request *DbgTransportTarget::LocateOriginalRequest(DbgProxyMessageHeader *pMessage)
{
    // Search the request queue for a matching ID.
    Request *pRequest;
    {
        RSLockHolder lock(&m_sLock);

        pRequest = m_pRequestList;
        while (pRequest)
        {
            if (pRequest->m_dwID == pMessage->m_uiRequestID)
                break;
            pRequest = pRequest->m_pNext;
        }
    } // Leave lock

    return pRequest;
}

// Process an incoming ProcessList message.
void DbgTransportTarget::ProcessProcessList(DbgProxyMessageHeader  *pMessage,
                                            Request                *pRequest)
{
    // The message header is followed by a sequence of DbgProxyProcessInfo records then a sequence of
    // DbgProxyRuntimeInfo records. The lengths of both of these sequences are provided in the header.
    DWORD cProcessRecords = pMessage->VariantData.ProcessList.m_uiProcessRecords;
    DWORD cRuntimeRecords = pMessage->VariantData.ProcessList.m_uiRuntimeRecords;

    // Impose some reasonable bounds to catch badly formed replies (and so we don't have to worry about
    // integer overflow in the allocation code that follows).
    if (cProcessRecords > 1024 || cRuntimeRecords > 1024)
    {
        _ASSERTE(!"Badly formed ProcessList message");
        pRequest->m_hrResult = E_UNEXPECTED;
        return;
    }

    // Allocate space for all the process and runtime records in one contiguous block.
    DWORD cbRecordBuffer = (cProcessRecords * sizeof(DbgProxyProcessInfo)) +
                           (cRuntimeRecords * sizeof(DbgProxyRuntimeInfo));
    BYTE *pbRecordBuffer = new (nothrow) BYTE[cbRecordBuffer];
    if (pbRecordBuffer == NULL)
    {
        DbgTransportLog(LC_Always, "Failed to allocate memory for %u process records and %u runtime records",
                        cProcessRecords, cRuntimeRecords);
        pRequest->m_hrResult = E_OUTOFMEMORY;
        return;
    }

    DbgProxyProcessInfo *pProcessRecords = (DbgProxyProcessInfo*)pbRecordBuffer;
    DbgProxyRuntimeInfo *pRuntimeRecords = (DbgProxyRuntimeInfo*)(pProcessRecords + cProcessRecords);

    // If we've gotten to this point we believe the message looks valid and we have all resources
    // allocated necessary to receive the response for this portion of the reply.
    if (!m_pConnection->Receive(pbRecordBuffer, cbRecordBuffer))
    {
        DbgTransportLog(LC_Always, "ProcessProcessList: Receive() failed");
        pRequest->m_hrResult = E_FAIL;
        return;
    }

    // Now parse the records and make any necessary updates to the cached process/runtime state we already
    // have.
    {
        RSLockHolder lock(&m_sLock);

        // First we walk the current list of processes and mark each entry as potentially removeable. This is
        // part of the algorithm to detect processes which have terminated, see the next stage for the rest.
        ProcessEntry *pProcess = m_pProcessList;
        while (pProcess)
        {
            pProcess->m_fRemove = true;
            pProcess = pProcess->m_pNext;
        }

        // Next we traverse the incoming list of process records, determining which we already know about
        // (and marking the corresponding ProcessEntry) and those that are new (for which we create new
        // ProcessEntry structures).
        for (DWORD i = 0; i < cProcessRecords; i++)
        {
            // See if we can locate an existing ProcessEntry (i.e. one with the same PRUID as the current
            // process record).
            pProcess = m_pProcessList;
            while (pProcess)
            {
                if (pProcess->m_pruidProcess == pProcessRecords[i].m_pruidProcess)
                {
                    _ASSERTE(pProcess->m_dwPID == pProcessRecords[i].m_uiPID);

                    // We've found a match so we indicate that this ProcessEntry is still live.
                    pProcess->m_fRemove = false;
                    break;
                }

                pProcess = pProcess->m_pNext;
            }
            
            // If we didn't find a matching ProcessEntry create one now.
            if (pProcess == NULL)
            {
                pProcess = new (nothrow) ProcessEntry();
                if (pProcess == NULL)
                    goto FailedUpdate;

                pProcess->m_pNext = m_pProcessList;
                pProcess->m_dwPID = pProcessRecords[i].m_uiPID;
                pProcess->m_pruidProcess = pProcessRecords[i].m_pruidProcess;
                strcpy_s(pProcess->m_szCommandLine, kMaxCommandLine, pProcessRecords[i].m_szCommandLine);
                pProcess->m_pRuntimes = NULL;
                pProcess->m_hExitedEvent = NULL;
                pProcess->m_hRuntimeStartedEvent = NULL;
                pProcess->m_fExited = false;
                pProcess->m_fRemove = false;
                pProcess->m_cProcessRef = 0;

                // Allocate event used to signal this process' termination.
                pProcess->m_hExitedEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
                if (pProcess->m_hExitedEvent == NULL)
                {
                    delete pProcess;
                    goto FailedUpdate;
                }

                // Allocate event used to signal the first runtime has started within this process.
                pProcess->m_hRuntimeStartedEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
                if (pProcess->m_hRuntimeStartedEvent == NULL)
                {
                    delete pProcess;
                    goto FailedUpdate;
                }

                // Link the new process entry to the database.
                m_pProcessList = pProcess;
            }
        }

        // Now walk the updated ProcessEntry structures. Each that wasn't marked as being present in the
        // incoming process infos indicates a dead process. We either remove such entries or, if they're being
        // used currently (have a connected debugger session etc.), we simply mark them as dead so they won't
        // appear in process enumerations and we'll delete them as soon as their use count falls to zero.
        pProcess = m_pProcessList;
        ProcessEntry *pLastProcess = NULL;
        while (pProcess)
        {
            if (pProcess->m_fRemove)
            {
                // The process has terminated. Can we release the ProcessEntry yet?
                if (pProcess->m_cProcessRef == 0)
                {
                    // Nobody is using the process, we can get rid of it.

                    // Unlink the entry from the list.
                    if (pLastProcess)
                        pLastProcess->m_pNext = pProcess->m_pNext;
                    else
                        m_pProcessList = pProcess->m_pNext;

                    // Since we've removed this entry from the list the last entry remains the same for the
                    // next iteration of the loop. We have to extract the next entry from the current one
                    // before we deallocate it.
                    ProcessEntry *pDeleteEntry = pProcess;
                    pProcess = pProcess->m_pNext;

                    // Finally we can delete the current entry (this automatically takes care of any runtime
                    // entries and other process entry owned resources).
                    delete pDeleteEntry;

                    continue;
                }
                else
                {
                    // Process is in use. Simply mark it as exited for now.
                    pProcess->m_fExited = true;
                    SetEvent(pProcess->m_hExitedEvent);
                }
            }

            pLastProcess = pProcess;
            pProcess = pProcess->m_pNext;
        }

        // Next we walk the incoming runtime records. Here we're just looking for new runtimes to add since we
        // don't currently support shutting down a runtime without terminating the process.
        for (DWORD i = 0; i < cRuntimeRecords; i++)
        {
            // First find the parent ProcessEntry record. This must exist (if the proxy sent a runtime record
            // it must send the parent process record).
            pProcess = m_pProcessList;
            while (pProcess)
            {
                if (pProcess->m_pruidProcess == pRuntimeRecords[i].m_pruidProcess)
                {
                    pProcess->m_fRemove = false;
                    break;
                }

                pProcess = pProcess->m_pNext;
            }
            PREFIX_ASSUME(pProcess != NULL);

            // Walk the list of RuntimeEntry records associated with this ProcessEntry to see if we already
            // have this entry.
            RuntimeEntry *pRuntime = pProcess->m_pRuntimes;
            while (pRuntime)
            {
                if (pRuntime->m_pruidRuntime == pRuntimeRecords[i].m_pruidRuntime)
                {
                    // We already have this entry.
                    _ASSERTE(pRuntime->m_usPort == pRuntimeRecords[i].m_usPort);
                    break;
                }

                pRuntime = pRuntime->m_pNext;
            }

            // If this is a new runtime add a corresponding RuntimeEntry.
            if (pRuntime == NULL)
            {
                pRuntime = new (nothrow) RuntimeEntry();
                if (pRuntime == NULL)
                    goto FailedUpdate;

                pRuntime->m_pruidRuntime = pRuntimeRecords[i].m_pruidRuntime;
                pRuntime->m_usPort = pRuntimeRecords[i].m_usPort;
                pRuntime->m_pDbgTransport = NULL;
                pRuntime->m_cTransportRef = 0;

                // Link the runtime entry onto the process.
                pRuntime->m_pNext = pProcess->m_pRuntimes;
                pProcess->m_pRuntimes = pRuntime;

                // Since there's at least one runtime for this process make sure the runtime started event is set.
                SetEvent(pProcess->m_hRuntimeStartedEvent);
            }
        }
    }

    // We're done, all incoming data has been consumed.
    delete [] pbRecordBuffer;
    return;

  FailedUpdate:
    pRequest->m_hrResult = E_OUTOFMEMORY;
    delete [] pbRecordBuffer;
}

// Process an incoming RuntimeStarted datagram.
bool DbgTransportTarget::ProcessRuntimeStarted(DbgProxyProcessInfo *pProcessInfo,
                                               DbgProxyRuntimeInfo *pRuntimeInfo)
{
    // A runtime has started up in some process on the target machine.
    // Add a runtime record to our database (if it's not already there).
    {
        RSLockHolder lock(&m_sLock);

        // Search for the parent process in our list.
        ProcessEntry *pProcess = m_pProcessList;
        while (pProcess)
        {
            if (pProcess->m_pruidProcess == pRuntimeInfo->m_pruidProcess)
                break;
            pProcess = pProcess->m_pNext;
        }

        DbgTransportLog(LC_Always, "Processing 'RuntimeStarted' for (%u, %u, %u)",
                        (unsigned)pProcessInfo->m_uiPID,
                        (PRUID)pRuntimeInfo->m_pruidProcess,
                        (PRUID)pRuntimeInfo->m_pruidRuntime);

        // If we haven't recorded the process yet create an entry for it.
        if (pProcess == NULL)
        {
            pProcess = new (nothrow) ProcessEntry();
            if (pProcess == NULL)
                return false;

            DbgTransportLog(LC_Always, "    No existing process record, created %08X", pProcess);

            memset(pProcess, 0, sizeof(ProcessEntry));
            pProcess->m_dwPID = pProcessInfo->m_uiPID;
            pProcess->m_pruidProcess = pProcessInfo->m_pruidProcess;
            strcpy(pProcess->m_szCommandLine, pProcessInfo->m_szCommandLine);

            pProcess->m_hExitedEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
            if (pProcess->m_hExitedEvent == NULL)
            {
                delete pProcess;
                return false;
            }

            pProcess->m_hRuntimeStartedEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
            if (pProcess->m_hRuntimeStartedEvent == NULL)
            {
                delete pProcess;
                return false;
            }

            // Link the new process entry into the list.
            pProcess->m_pNext = m_pProcessList;
            m_pProcessList = pProcess;
        }
        else
            DbgTransportLog(LC_Always, "    Found existing process record %08X", pProcess);

        // Now we have a process record we can look for a runtime entry.
        RuntimeEntry *pRuntime = pProcess->m_pRuntimes;
        while (pRuntime)
        {
            if (pRuntime->m_pruidRuntime == pRuntimeInfo->m_pruidRuntime)
                break;
            pRuntime = pRuntime->m_pNext;
        }

        // If we didn't have an entry for this runtime create one now.
        if (pRuntime == NULL)
        {
            pRuntime = new (nothrow) RuntimeEntry();
            if (pRuntime == NULL)
                return false;

            DbgTransportLog(LC_Always, "    No existing runtime record, created %08X", pRuntime);

            pRuntime->m_pruidRuntime = pRuntimeInfo->m_pruidRuntime;
            pRuntime->m_usPort = pRuntimeInfo->m_usPort;
            pRuntime->m_pDbgTransport = NULL;
            pRuntime->m_cTransportRef = 0;

            // Link the runtime entry onto the process.
            pRuntime->m_pNext = pProcess->m_pRuntimes;
            pProcess->m_pRuntimes = pRuntime;

            // Since there's at least one runtime for this process make sure the runtime started event is set.
            SetEvent(pProcess->m_hRuntimeStartedEvent);
        }
        else
            DbgTransportLog(LC_Always, "    Found existing runtime record %08X", pRuntime);
    } // Leave lock

    return true;
}

// A version of EnumProcesses used when we're controlled by the Visual Studio debugger. This API is exposed to
// our port supplier implementation via the ICoreClrDebugTarget interface implemented by the
// CoreClrDebugTarget class implemented in DbgTransportManager.cpp.
HRESULT DbgTransportTarget::EnumProcessesForVS(DWORD *pcProcs, CoreClrDebugProcInfo **ppProcs)
{
    *pcProcs = 0;
    *ppProcs = NULL;

    // Update our process and runtime view from the remote target.
    UpdateProcessList();

    // Acquire the lock to get a consistent view of current state.
    {
        RSLockHolder lock(&m_sLock);

        ProcessEntry   *pProcess;
        DWORD           cProcesses = 0;

        // Count the number of processes we know about.
        for (pProcess = m_pProcessList; pProcess; pProcess = pProcess->m_pNext)
        {
            // Skip reporting of processes that have already exited (and we're waiting to clean up).
            if (pProcess->m_fExited)
                continue;
            cProcesses++;
        }

        // We're done if there aren't any.
        if (cProcesses == 0)
            return S_OK;

        // Otherwise allocate an array large enough to hold information about each process.
        CoreClrDebugProcInfo *pProcInfos = new (nothrow) CoreClrDebugProcInfo[cProcesses];
        if (pProcInfos == NULL)
            return E_OUTOFMEMORY;

        // Iterate over the processes again, this time filling in the data for each one in the output array.
        DWORD idxCurrentProc = 0;
        for (pProcess = m_pProcessList; pProcess; pProcess = pProcess->m_pNext)
        {
            // Skip reporting of processes that have already exited (and we're waiting to clean up).
            if (pProcess->m_fExited)
                continue;

            pProcInfos[idxCurrentProc].m_dwPID = pProcess->m_dwPID;
            pProcInfos[idxCurrentProc].m_dwInternalID = pProcess->m_pruidProcess;

            if (MultiByteToWideChar(CP_UTF8, 0, pProcess->m_szCommandLine, -1, pProcInfos[idxCurrentProc].m_wszName, kMaxCommandLine) == 0)
            {
                delete [] pProcInfos;
                return E_OUTOFMEMORY;
            }

            idxCurrentProc++;
        }

        *pcProcs = cProcesses;
        *ppProcs = pProcInfos;
    }

    return S_OK;
}

// A similar API for VS that enumerates runtimes running within the given process. Returns S_FALSE if
// there are none.
HRESULT DbgTransportTarget::EnumRuntimesForVS(PRUID pruidProcess, DWORD *pcRuntimes, CoreClrDebugRuntimeInfo **ppRuntimes)
{
    *pcRuntimes = 0;
    *ppRuntimes = NULL;

    // Update our process and runtime view from the remote target.
    UpdateProcessList();

    // Acquire the lock to get a consistent view of current state.
    {
        RSLockHolder lock(&m_sLock);

        // Look for the process record with the matching PRUID.
        ProcessEntry   *pProcess;
        for (pProcess = m_pProcessList; pProcess; pProcess = pProcess->m_pNext)
        {
            if (pProcess->m_fExited)
                continue;
            if (pProcess->m_pruidProcess == pruidProcess)
                break;
        }

        // We couldn't find a match -- the process must have terminated so it certainly doesn't have any
        // runtimes.
        if (pProcess == NULL)
            return S_FALSE;

        // Count the runtime instances running in the process.
        DWORD cRuntimes = 0;
        RuntimeEntry *pRuntime;
        for (pRuntime = pProcess->m_pRuntimes; pRuntime; pRuntime = pRuntime->m_pNext)
            cRuntimes++;

        // We're done if there aren't any.
        if (cRuntimes == 0)
            return S_OK;

        // Otherwise allocate an array large enough to hold information about each runtime.
        CoreClrDebugRuntimeInfo *pRuntimeInfos = new (nothrow) CoreClrDebugRuntimeInfo[cRuntimes];
        if (pRuntimeInfos == NULL)
            return E_OUTOFMEMORY;

        // Iterate over the runtimes again, this time filling in data for each one in the output array.
        DWORD idxCurrentRuntime = 0;
        for (pRuntime = pProcess->m_pRuntimes; pRuntime; pRuntime = pRuntime->m_pNext)
        {
            pRuntimeInfos[idxCurrentRuntime].m_dwInternalID = pRuntime->m_pruidRuntime;
            idxCurrentRuntime++;
        }

        *pcRuntimes = cRuntimes;
        *ppRuntimes = pRuntimeInfos;
    }

    return S_OK;
}


// When we're being driven by the Visual Studio debugger CoreCLR supplies an entity known as a port supplier
// to handle interactions between VS and the remote system when setting up debug sessions. The port supplier
// implements the connection to the remote proxy by talking to DbgTransportTarget instances controlled via the
// following class through a psuedo-COM interface, ICoreClrDebugTarget, described in
// debug\inc\CoreClrRemoteDebuggingInterfaces.h.
class CoreClrDebugTarget : public ICoreClrDebugTarget
{
public:
    CoreClrDebugTarget(DWORD dwAddress) :
        m_lRefCount(1),
        m_dwAddress(dwAddress),
        m_pTarget(NULL)
    {
    }

    ~CoreClrDebugTarget()
    {
        if (m_pTarget)
            g_pDbgTransportManager->ReleaseTarget(m_pTarget);
    }

    STDMETHODIMP_(void) AddRef()
    {
        InterlockedIncrement(&m_lRefCount);
    }

    STDMETHODIMP_(void) Release()
    {
        LONG lRef = InterlockedDecrement(&m_lRefCount);
        if (lRef == 0)
            delete this;
    }

    // Enumerate all processes on the target system (whether they have managed code or not). The memory
    // returned is deallocated via FreeMemory().
    STDMETHODIMP EnumProcesses(DWORD *pcProcs, CoreClrDebugProcInfo **ppProcs)
    {
        return m_pTarget->EnumProcessesForVS(pcProcs, ppProcs);
    }

    // Enumerate all runtimes running in the given process on the target system. The memory returned is
    // deallocated via FreeMemory().
    STDMETHODIMP EnumRuntimes(DWORD dwInternalProcessID, DWORD *pcRuntimes, CoreClrDebugRuntimeInfo **ppRuntimes)
    {
        return m_pTarget->EnumRuntimesForVS((PRUID)dwInternalProcessID, pcRuntimes, ppRuntimes);
    }

    // Free memory allocated via EnumProcesses or EnumRuntimes.
    STDMETHODIMP_(void) FreeMemory(void *pMemory)
    {
        delete [] (BYTE*)pMemory;
    }

    // Non-exported method used by CreateCoreClrDebugTarget below to connect an instance of DbgTransportTarget
    // to the remote system's proxy process.
    HRESULT Init()
    {
        HRESULT hr = g_pDbgTransportManager->ConnectToTarget(m_dwAddress, 0, &m_pTarget);
        if (FAILED(hr))
            return hr;

        return S_OK;
    }

private:
    LONG                m_lRefCount;    // COM-style ref count
    DWORD               m_dwAddress;    // IPv4 address of target system
    DbgTransportTarget *m_pTarget;      // Currently connected DbgTransportTarget
};

// Function exported by mscordbi_mac* to allow the port supplier used by Visual Studio to query remote system
// state.
extern "C" HRESULT __stdcall CreateCoreClrDebugTarget(DWORD dwAddress, ICoreClrDebugTarget **ppTarget)
{
    HRESULT hr;

    *ppTarget = NULL;

    // Allocate a new object implementing ICoreClrDebugTarget.
    CoreClrDebugTarget *pTarget = new (nothrow) CoreClrDebugTarget(dwAddress);
    if (pTarget == NULL)
        return E_OUTOFMEMORY;

    // Attempt to connect it to the remote system.
    hr = pTarget->Init();
    if (FAILED(hr))
    {
        delete pTarget;
        return hr;
    }

    *ppTarget = static_cast<ICoreClrDebugTarget*>(pTarget);
    return S_OK;
}

#endif // FEATURE_DBGIPC_TRANSPORT_DI
