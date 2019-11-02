// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"
#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"
#include "coreclrremotedebugginginterfaces.h"



#ifdef FEATURE_DBGIPC_TRANSPORT_DI

DbgTransportTarget *g_pDbgTransportTarget = NULL;

DbgTransportTarget::DbgTransportTarget()
{
    memset(this, 0, sizeof(*this));
}

// Initialization routine called only by the DbgTransportManager.
HRESULT DbgTransportTarget::Init()
{
    m_sLock.Init("DbgTransportTarget Lock", RSLock::cLockFlat, RSLock::LL_DBG_TRANSPORT_TARGET_LOCK);

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
                                                   HANDLE                   *phProcessHandle)
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


       HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
       if (hProcess == NULL)
       {
           transport->Shutdown();
           return HRESULT_FROM_GetLastError();
       }

       // Initialize it (this immediately starts the remote connection process).
       hr = transport->Init(*pProcessDescriptor, hProcess);
       if (FAILED(hr))
       {
           transport->Shutdown();
           CloseHandle(hProcess);
           return hr;
       }

       entry = newEntry;
       newEntry.SuppressRelease();
       entry->m_dwPID = dwPID;
       entry->m_hProcess = hProcess;
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
    _ASSERTE((intptr_t)entry->m_hProcess > 0);

    *ppTransport = entry->m_transport;
    if (!DuplicateHandle(GetCurrentProcess(),
                         entry->m_hProcess,
                         GetCurrentProcess(),
                         phProcessHandle,
                         0,      // ignored since we are going to pass DUPLICATE_SAME_ACCESS
                         FALSE,
                         DUPLICATE_SAME_ACCESS))
    {
        return HRESULT_FROM_GetLastError();
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
        _ASSERTE((intptr_t)entry->m_hProcess > 0);

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

HRESULT DbgTransportTarget::CreateProcess(LPCWSTR lpApplicationName,
                          LPCWSTR lpCommandLine,
                          LPSECURITY_ATTRIBUTES lpProcessAttributes,
                          LPSECURITY_ATTRIBUTES lpThreadAttributes,
                          BOOL bInheritHandles,
                          DWORD dwCreationFlags,
                          LPVOID lpEnvironment,
                          LPCWSTR lpCurrentDirectory,
                          LPSTARTUPINFOW lpStartupInfo,
                          LPPROCESS_INFORMATION lpProcessInformation)
{

    BOOL result = WszCreateProcess(lpApplicationName,
                                   lpCommandLine,
                                   lpProcessAttributes,
                                   lpThreadAttributes,
                                   bInheritHandles,
                                   dwCreationFlags,
                                   lpEnvironment,
                                   lpCurrentDirectory,
                                   lpStartupInfo,
                                   lpProcessInformation);

    if (!result)
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}

// Kill the process identified by PID.
void DbgTransportTarget::KillProcess(DWORD dwPID)
{
    HANDLE hProcess = OpenProcess(PROCESS_TERMINATE, FALSE, dwPID);
    if (hProcess != NULL)
    {
        TerminateProcess(hProcess, 0);
        CloseHandle(hProcess);
    }
}

DbgTransportTarget::ProcessEntry::~ProcessEntry()
{
    CloseHandle(m_hProcess);
    m_hProcess = NULL;

    m_transport->Shutdown();
    m_transport = NULL;
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
