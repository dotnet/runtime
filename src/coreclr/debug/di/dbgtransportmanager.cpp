// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"

#ifdef FEATURE_DBGIPC_TRANSPORT_DI

#ifdef HOST_UNIX
#include <errno.h>
#include <signal.h>
#include <sys/wait.h>
#include <unistd.h>
#endif // HOST_UNIX

DbgTransportTarget g_DbgTransportTarget{};

#ifdef HOST_UNIX
// Polling interval for the per-process exit poller thread.
static const useconds_t s_processExitPollIntervalUsec = 250 * 1000;

// Polls the target PID for exit. waitpid is identity-stable for child processes; kill is a
// best-effort fallback for non-children and can race PID reuse.
/* static */
void *DbgTransportTarget::ProcessExitPollerThread(void *arg)
{
    ProcessEntry *entry = static_cast<ProcessEntry *>(arg);

    while (!entry->m_fStopPoller)
    {
        bool exited = false;

        int status;
        pid_t r;
        do
        {
            r = waitpid(entry->m_dwPID, &status, WNOHANG);
        } while (r == -1 && errno == EINTR);

        if (r == (pid_t)entry->m_dwPID)
        {
            exited = WIFEXITED(status) || WIFSIGNALED(status);
        }
        else if (r == -1 && errno == ECHILD)
        {
            int killResult;
            do
            {
                killResult = kill(entry->m_dwPID, 0);
            } while (killResult == -1 && errno == EINTR);

            exited = killResult == -1 && errno == ESRCH;
        }

        if (exited)
        {
            entry->m_hProcessExited->Set();
            break;
        }

        usleep(s_processExitPollIntervalUsec);
    }

    return nullptr;
}
#endif // HOST_UNIX

DbgTransportTarget::DbgTransportTarget()
    : m_pProcessList{}
    , m_sLock{}
{
}

// Initialization routine called only by the DbgTransportManager.
HRESULT DbgTransportTarget::Init()
{
    // The Unix loader does not invoke DbgDllMain DLL_PROCESS_DETACH for mscordbi at process exit,
    // so Shutdown() may never run. Mark the lock as allowing leak to skip the destructor assert.
    m_sLock.Init("DbgTransportTarget Lock", RSLock::cLockFlat | RSLock::cLockAllowLeak, RSLock::LL_DBG_TRANSPORT_TARGET_LOCK);

    return S_OK;
}

// Shutdown routine called only by the DbgTransportManager.
void DbgTransportTarget::Shutdown()
{
    DbgTransportLog(LC_Always, "DbgTransportTarget shutting down");

    {
        RSLockHolder lock(&m_sLock);
        while (m_pProcessList)
        {
            ProcessEntry *pDelProcess = m_pProcessList;
            m_pProcessList = m_pProcessList->m_pNext;
            delete pDelProcess;
        }
    }
    m_sLock.Destroy();
}


// Given a PID attempt to find or create a DbgTransportSession instance to manage a connection to a runtime in
// that process. Returns E_UNEXPECTED if the process can't be found. Also returns a handle that can be waited
// on for process termination.
HRESULT DbgTransportTarget::GetTransportForProcess(const ProcessDescriptor  *pProcessDescriptor,
                                                   DbgTransportSession     **ppTransport,
                                                   WaitHandle             **ppProcessHandle)
{
    RSLockHolder lock(&m_sLock);
    HRESULT hr = S_OK;
    DWORD dwPID = pProcessDescriptor->m_Pid;

    ProcessEntry *entry = LocateProcessByPID(dwPID);

    if (entry == NULL)
    {

       NewHolder<ProcessEntry> newEntry = new(nothrow) ProcessEntry();
       if (newEntry == NULL)
           return E_OUTOFMEMORY;

       NewHolder<DbgTransportSession> transport = new(nothrow) DbgTransportSession();
       if (transport == NULL)
       {
           return E_OUTOFMEMORY;
       }


       // Probe the process to make sure it exists, then create a waitable that becomes signaled
       // on process exit. Windows uses the process handle itself; Unix uses a latch set by
       // a poller thread.
#ifdef HOST_UNIX
       if (kill(dwPID, 0) != 0)
       {
           transport->Shutdown();
           return (errno == ESRCH) ? E_INVALIDARG : E_FAIL;
       }

       WaitLatch *pProcessExited = new (nothrow) WaitLatch();
       if ((pProcessExited == NULL) || !pProcessExited->IsValid())
       {
           delete pProcessExited;
           transport->Shutdown();
           return E_FAIL;
       }
#else // HOST_UNIX
       HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
       if (hProcess == NULL)
       {
           transport->Shutdown();
           return HRESULT_FROM_GetLastError();
       }

       NativeHandle *pProcessExited = new (nothrow) NativeHandle(hProcess);
       bool allocationFailed = pProcessExited == nullptr;
       DWORD error = GetLastError();
       CloseHandle(hProcess);
       if (allocationFailed || !pProcessExited->IsValid())
       {
           delete pProcessExited;
           transport->Shutdown();
           return allocationFailed ? E_OUTOFMEMORY : HRESULT_FROM_WIN32(error);
       }
#endif // HOST_UNIX

       newEntry->m_dwPID = dwPID;
       newEntry->m_hProcessExited = pProcessExited;
#ifdef HOST_UNIX
       newEntry->m_fStopPoller = false;
       newEntry->m_fPollerStarted = false;

       if (pthread_create(&newEntry->m_pollerThread, nullptr, &ProcessExitPollerThread, newEntry.GetValue()) != 0)
       {
           transport->Shutdown();
           return E_FAIL;
       }
       newEntry->m_fPollerStarted = true;
#endif // HOST_UNIX

       // Initialize it (this immediately starts the remote connection process).
       hr = transport->Init(*pProcessDescriptor, *pProcessExited);
       if (FAILED(hr))
       {
           transport->Shutdown();
           // ProcessEntry destructor stops the poller and releases the waitable.
           return hr;
       }

       entry = newEntry;
       newEntry.SuppressRelease();
       entry->m_transport = transport;
       transport.SuppressRelease();
       entry->m_cProcessRef = 0;

       // Adding new entry to the list.
       entry->m_pNext = m_pProcessList;
       m_pProcessList = entry;
    }

    entry->m_cProcessRef++;
    _ASSERTE(entry->m_cProcessRef > 0);
    _ASSERTE(entry->m_transport != NULL);
    _ASSERTE(entry->m_hProcessExited->IsValid());

    *ppTransport = entry->m_transport;
    *ppProcessHandle = new (nothrow) WaitHandle(*entry->m_hProcessExited);
    if ((*ppProcessHandle == nullptr) || !(*ppProcessHandle)->IsValid())
    {
        delete *ppProcessHandle;
        *ppProcessHandle = nullptr;
        return E_FAIL;
    }

    return hr;
}


// Release another reference to the transport associated with dwPID. Once all references are gone (modulo the
// manager's own weak reference) clean up the transport and deallocate it.
void DbgTransportTarget::ReleaseTransport(DbgTransportSession *pTransport)
{
    RSLockHolder lock(&m_sLock);

    ProcessEntry *entry = m_pProcessList;

    // Pointer to the pointer that points to *entry.
    // It either points to m_pProcessList or m_pNext of some entry.
    // It is used to fix the linked list after deletion of an entry.
    ProcessEntry **prevPtr = &m_pProcessList;

    // Looking for ProcessEntry with a given transport
    while (entry)
    {

        _ASSERTE(entry->m_cProcessRef > 0);
        _ASSERTE(entry->m_transport != NULL);
        _ASSERTE(entry->m_hProcessExited->IsValid());

        if (entry->m_transport == pTransport)
        {
            // Mark that it has one less holder now
            entry->m_cProcessRef--;

            // If no more holders remove the entry from the list and free resources
            if (entry->m_cProcessRef == 0)
            {
                *prevPtr = entry->m_pNext;
                delete entry;
            }
            return;
        }
        prevPtr = &entry->m_pNext;
        entry = entry->m_pNext;
    }

    _ASSERTE(!"Trying to release transport that doesn't belong to this DbgTransportTarget");
    pTransport->Shutdown();
}

// Kill the process identified by PID.
void DbgTransportTarget::KillProcess(DWORD dwPID)
{
#ifdef HOST_UNIX
    kill(dwPID, SIGKILL);
#else
    HANDLE hProcess = OpenProcess(PROCESS_TERMINATE, FALSE, dwPID);
    if (hProcess != NULL)
    {
        TerminateProcess(hProcess, 0);
        CloseHandle(hProcess);
    }
#endif
}

DbgTransportTarget::ProcessEntry::~ProcessEntry()
{
#ifdef HOST_UNIX
    if (m_fPollerStarted)
    {
        m_fStopPoller = true;
        pthread_join(m_pollerThread, nullptr);
        m_fPollerStarted = false;
    }
#endif // HOST_UNIX

    if (m_hProcessExited != NULL)
    {
        delete m_hProcessExited;
        m_hProcessExited = NULL;
    }

    if (m_transport != NULL)
    {
        m_transport->Shutdown();
        m_transport = NULL;
    }
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

#endif // FEATURE_DBGIPC_TRANSPORT_DI
