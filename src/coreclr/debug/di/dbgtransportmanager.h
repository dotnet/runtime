// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __DBG_TRANSPORT_MANAGER_INCLUDED
#define __DBG_TRANSPORT_MANAGER_INCLUDED

#ifdef FEATURE_DBGIPC_TRANSPORT_DI

#include "coreclrremotedebugginginterfaces.h"


// TODO: Ideally we'd like to remove this class and don't do any process related book keeping in DBI.

// This is a registry of all the processes a debugger knows about, different components call it in order to
// obtain right instance of DbgTransportSession for a given PID. It keeps list of processes and transports for them.
// It also handles things like creating and killing a process.

// Usual lifecycle looks like this:
// Debug a new process:
// * CreateProcess(&pid)
// * On Mac, Optionally obtain an application group ID from a user
// * Create a ProcessDescriptor pd
// * GetTransportForProcess(&pd, &transport)
// * ReleaseTransport(transport)
// * KillProcess(pid)

// Attach to an existing process:
// * Obtain pid (and optionally application group ID on Mac) from a user
// * Create a ProcessDescriptor pd
// * GetTransportForProcess(&pd, &transport)
// * ReleaseTransport(transport)

class DbgTransportTarget
{
public:
    DbgTransportTarget();

    // Given a PID attempt to find or create a DbgTransportSession instance to manage a connection to a
    // runtime in that process. Returns E_UNEXPECTED if the process can't be found. Also returns a handle that
    // can be waited on for process termination.
    HRESULT GetTransportForProcess(const ProcessDescriptor *pProcessDescriptor, DbgTransportSession **ppTransport, HANDLE *phProcessHandle);

    // Give back a previously acquired transport (if nobody else is using the transport it will close down the
    // connection at this point).
    void ReleaseTransport(DbgTransportSession *pTransport);

    // When and if the process starts the runtime will be told to halt and wait for a debugger attach.
    HRESULT CreateProcess(LPCWSTR lpApplicationName,
                          LPCWSTR lpCommandLine,
                          LPSECURITY_ATTRIBUTES lpProcessAttributes,
                          LPSECURITY_ATTRIBUTES lpThreadAttributes,
                          BOOL bInheritHandles,
                          DWORD dwCreationFlags,
                          LPVOID lpEnvironment,
                          LPCWSTR lpCurrentDirectory,
                          LPSTARTUPINFOW lpStartupInfo,
                          LPPROCESS_INFORMATION lpProcessInformation);

    // Kill the process identified by PID.
    void KillProcess(DWORD dwPID);

    HRESULT Init();
    void Shutdown();

private:
    struct ProcessEntry
    {
        ProcessEntry           *m_pNext;            // Next entry in the list
        DWORD                   m_dwPID;            // Process ID for this entry
        HANDLE                  m_hProcess;         // Process handle
        DbgTransportSession    *m_transport;        // Debugger's connection to the process
        DWORD                   m_cProcessRef;      // Ref count

        ~ProcessEntry();
    };

    ProcessEntry           *m_pProcessList;         // Head of list of currently alive processes (unsorted)
    RSLock                  m_sLock;                // Lock protecting read and write access to the target list

    // Locate a process entry by PID. Assumes the lock is already held.
    ProcessEntry *LocateProcessByPID(DWORD dwPID);
};

extern DbgTransportTarget *g_pDbgTransportTarget;

#endif // FEATURE_DBGIPC_TRANSPORT_DI

#endif // __DBG_TRANSPORT_MANAGER_INCLUDED
