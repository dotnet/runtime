// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __DBG_TRANSPORT_MANAGER_INCLUDED
#define __DBG_TRANSPORT_MANAGER_INCLUDED

#ifdef FEATURE_DBGIPC_TRANSPORT_DI

#ifdef HOST_UNIX
#include <pthread.h>
#endif

// TODO: Ideally we'd like to remove this class and don't do any process related book keeping in DBI.

// This is a registry of all the processes a debugger knows about, different components call it in order to
// obtain right instance of DbgTransportSession for a given PID. It keeps list of processes and transports for them.
// It also handles things like creating and killing a process.

// Usual lifecycle looks like this:
// Debug an existing process:
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

    // Kill the process identified by PID.
    void KillProcess(DWORD dwPID);

    HRESULT Init();
    void Shutdown();

private:
    struct ProcessEntry
    {
        ProcessEntry           *m_pNext;            // Next entry in the list
        DWORD                   m_dwPID;            // Process ID for this entry
        HANDLE                  m_hProcessExited;   // Waitable handle that becomes signaled when the
                                                    // process exits. On HOST_WINDOWS this is the process
                                                    // handle itself; on HOST_UNIX it is a manual-reset
                                                    // event signaled by the poller thread below.
        DbgTransportSession    *m_transport;        // Debugger's connection to the process
        DWORD                   m_cProcessRef;      // Ref count
#ifdef HOST_UNIX
        pthread_t               m_pollerThread;     // Thread that polls m_dwPID for exit
        bool                    m_fPollerStarted;   // True once m_pollerThread has been created
        Volatile<bool>          m_fStopPoller;      // Set to true to ask the poller thread to exit
#endif // HOST_UNIX

        ~ProcessEntry();
    };

#ifdef HOST_UNIX
    static void *ProcessExitPollerThread(void *arg);
#endif

    ProcessEntry           *m_pProcessList;         // Head of list of currently alive processes (unsorted)
    RSLock                  m_sLock;                // Lock protecting read and write access to the target list

    // Locate a process entry by PID. Assumes the lock is already held.
    ProcessEntry *LocateProcessByPID(DWORD dwPID);
};

extern DbgTransportTarget g_DbgTransportTarget;

#endif // FEATURE_DBGIPC_TRANSPORT_DI

#endif // __DBG_TRANSPORT_MANAGER_INCLUDED
