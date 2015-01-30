//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef __DBG_TRANSPORT_MANAGER_INCLUDED
#define __DBG_TRANSPORT_MANAGER_INCLUDED

#ifdef FEATURE_DBGIPC_TRANSPORT_DI

#include "dbgproxy.h"
#include "coreclrremotedebugginginterfaces.h"

//
// Provides access to various process enumeration and control facilities for a remote machine.
//
// The top-level entity is a DbgTransportManager. Usually one of these will be allocated per debugger process.
// The manager maintains a pool of DbgTransportTarget objects. Each of these track a connection to the proxy
// on a single target machine. In addition to managing the proxy connection a collection of
// DbgTransportSession objects is maintained, each corresponding to a debug session with a specific CoreCLR
// runtime instance.
//

// Target machine specific state.
class DbgTransportTarget
{
public:
    DbgTransportTarget();

    // Fill caller allocated table at pdwProcesses with the pids of processes currently alive on the target
    // system. Return the number of slots filled in *pcProcesses. The size of the table is given by cSlots. If
    // more than this number of processes are alive then *pcProcesses is set to the total number and
    // E_OUTOFMEMORY returned.
    HRESULT EnumProcesses(DWORD *pdwProcesses, DWORD cSlots, DWORD *pcProcesses);

    // Returns true if the given PID identifies a running process which is hosting at least one CoreCLR
    // runtime.
    bool IsManagedProcess(DWORD dwPID);

    // Given a PID attempt to find or create a DbgTransportSession instance to manage a connection to a
    // runtime in that process. Returns E_UNEXPECTED if the process can't be found. Also returns a handle that
    // can be waited on for process termination.
    HRESULT GetTransportForProcess(DWORD dwPID, DbgTransportSession **ppTransport, HANDLE *phProcessHandle);

    // Give back a previously acquired transport (if nobody else is using the transport it will close down the
    // connection at this point).
    void ReleaseTransport(DbgTransportSession *pTransport);

    // Run the command line given on the remote machine to create a process. Return the PID of this process.
    // When and if the process starts a runtime and registers with the proxy it will be told to halt and wait
    // for a debugger attach.
    HRESULT CreateProcess(LPCWSTR  wszCommand,
                          LPCWSTR  wszArgs,
                          LPCWSTR  wszCurrentDirectory,
                          LPVOID   pvEnvironment,
                          DWORD   *pdwPID);

    // Kill the process identified by PID.
    void KillProcess(DWORD dwPID);

    // Indicates when the connection to a proxy has failed: this target object will remain until the last
    // reference to it is released (DbgTransportManager::ReleaseTransport) but will fail all further requests.
    // The manager will then allow a new attempt to create a connection to the proxy to be made (wrapped in a
    // new DbgTransportTarget).
    bool IsProxyConnectionBad();

    // A version of EnumProcesses used when we're controlled by the Visual Studio debugger. This API is
    // exposed to our port supplier implementation via the ICoreClrDebugTarget interface implemented by the
    // CoreClrDebugTarget class implemented in DbgTransportManager.cpp.
    HRESULT EnumProcessesForVS(DWORD *pcProcs, CoreClrDebugProcInfo **ppProcs);

    // A similar API for VS that enumerates runtimes running within the given process. Returns S_FALSE if
    // there are none.
    HRESULT EnumRuntimesForVS(PRUID pruidProcess, DWORD *pcRuntimes, CoreClrDebugRuntimeInfo **ppRuntimes);

private:
    friend class DbgTransportManager;

    // Initialization and shutdown routines called only by the DbgTransportManager.
    HRESULT Init(DWORD dwIPAddress, USHORT usPort);
    void Shutdown();

    // Data saved for each runtime instance the manager currently knows about. Saved in a singly linked, NULL
    // terminated list on each ProcessEntry (m_pRuntimes).
    struct RuntimeEntry
    {
        RuntimeEntry           *m_pNext;            // Next entry in the list
        PRUID                   m_pruidRuntime;     // Proxy ID for this specific runtime instance
        USHORT                  m_usPort;           // Port number to connect session to.
        DbgTransportSession    *m_pDbgTransport;    // Transport to this runtime or NULL
        DWORD                   m_cTransportRef;    // Number of references to the transport still outstanding

        ~RuntimeEntry();
    };

    // Data saved for each process the manager currently knows about. Saved in a singly linked, NULL
    // terminated list (m_pProcessList).
    struct ProcessEntry
    {
        ProcessEntry           *m_pNext;            // Next entry in the list
        DWORD                   m_dwPID;            // Process ID for this entry
        PRUID                   m_pruidProcess;     // Proxy's ID for this process
        char                    m_szCommandLine[kMaxCommandLine];  // Command line process is running
        RuntimeEntry           *m_pRuntimes;        // Singly linked list of runtimes in process
        HANDLE                  m_hExitedEvent;     // Event signalled once process terminates
        HANDLE                  m_hRuntimeStartedEvent; //Event signalled when first runtime is created in this process
        bool                    m_fExited;          // Marks processes that have exited but still have a transport
        bool                    m_fRemove;          // Used only during process list updates, see ProcessProcessList
        DWORD                   m_cProcessRef;      // Reasons the process can't be deleted yet

        ~ProcessEntry();
    };

    // Record of outgoing request that requires a reply.
    struct Request
    {
        Request            *m_pNext;                // Next in queue of outstanding requests
        DWORD               m_dwID;                 // ID assigned to request (to match reply)
        HANDLE              m_hCompletionEvent;     // Event to signal once the request is completed
        HRESULT             m_hrResult;             // Completion result of request
        union                                       // Request specific output buffers
        {
            struct
            {
                DWORD                  *m_pdwMajorVersion;   // Protocol version employed by the proxy
                DWORD                  *m_pdwMinorVersion;   // Protocol version employed by the proxy
                DbgTargetPlatform      *m_pePlatform;        // Platform type of target system
            } GetSystemInfo;

            struct
            {
                DWORD      *m_pdwPID;               // PID of launched process
                PRUID      *m_ppruidProcess;        // PRUID of launched process
            } LaunchProcess;

            struct
            {
                bool       *m_pfProcessExited;      // Process exited before we could record the attach
            } EarlyAttach;

        } OutputBuffers;
    };

    ULONGLONG               m_ullLastUpdate;        // tick count of the last time the process list is updated
    bool                    m_fInitLock;            // Used to track whether we initialized m_sLock in Init()
    DWORD                   m_dwProxyIP;            // Proxy IP address in host byte order
    USHORT                  m_usProxyPort;          // Proxy port number in host byte order
    DbgTargetPlatform       m_ePlatform;            // Platform type of the target (e.g. MacX86)
    ProcessEntry           *m_pProcessList;         // Head of list of currently alive processes (unsorted)
    RSLock                  m_sLock;                // Lock protecting read and write access to the process list
    bool                    m_fShutdown;            // Flag set once Shutdown() has been called
    HANDLE                  m_hProcessEventThread;  // Handle for the process event thread
    SecConnMgr             *m_pConnectionManager;   // Factory for network connections
    SecConn                *m_pConnection;          // Connection to the proxy
    SOCKET                  m_hSocket;              // UDP socket used to communicate with proxy
    DWORD                   m_dwNextRequestID;      // Next ID to be assigned to an outgoing request
    Request                *m_pRequestList;         // List of requests to proxy that haven't had a reply
    bool                    m_fProxyConnectionBad;  // Initially false, any network failure will transition it
                                                    // to true for good and all further requests on this
                                                    // target will fail.

    // Ask the remote debugger proxy for an updated list of processes and reflect these changes into our local
    // process list. Any failure will leave the current process list state unchanged.
    void UpdateProcessList();

    // Locate a process entry by PID. Assumes the lock is already held.
    ProcessEntry *LocateProcessByPID(DWORD dwPID);

    // Deallocate all resources associated with a process list.
    void DeallocateProcessList(ProcessEntry *pProcessList);

    // Format and send a request to the proxy then wait on a reply (if appropriate) and return results to the
    // caller.
    HRESULT MakeProxyRequest(DbgProxyMessageType eType, ...);

    // Static entry point for the process event thread.
    static DWORD WINAPI ProcessEventWorkerStatic(LPVOID lpvContext);

    // Instance method version of the worker, called from ProcessEventWorkerStatic().
    void ProcessEventWorker();

    // If this message is a reply to a right-side request locate that request. Otherwise return NULL.
    Request *LocateOriginalRequest(DbgProxyMessageHeader *pMessage);

    // Process an incoming ProcessList message.
    void ProcessProcessList(DbgProxyMessageHeader  *pMessage,
                            Request                *pRequest);

    // Process an incoming RuntimeStarted message.
    bool ProcessRuntimeStarted(DbgProxyProcessInfo *pProcessInfo,
                               DbgProxyRuntimeInfo *pRuntimeInfo);
};

// Process level state.
class DbgTransportManager
{
public:
    DbgTransportManager();

    // Startup/shutdown calls. These are ref-counted (cordbg, for instance, is constructed in such a way that
    // the command shell will attempt to load mscordbi and initialize an associated DbgTransportManager
    // multiple times).
    HRESULT Init();
    void Shutdown();

    // Attempt to connect to a debugging proxy on the machine at the given address and with the specified port
    // number. If the port number is given as zero use the port stored in user debugger configuration. On
    // success a pointer to a DbgTransportTarget object will be returned.
    HRESULT ConnectToTarget(DWORD dwIPAddress, USHORT usPort, DbgTransportTarget **ppTarget);

    // Add another reference to a target already acquired by ConnectToTarget (used by clients when they want
    // to hand a target out to independent code).
    void ReferenceTarget(DbgTransportTarget *pTarget);

    // Release reference to a DbgTransportTarget. If this is the last active reference then the connection to
    // the proxy will be severed and the object deallocated.
    void ReleaseTarget(DbgTransportTarget *pTarget);

private:
    // Private structure used to track references to DbgTransportTargets we've allocated.
    struct TargetRef
    {
        TargetRef          *m_pNext;        // Next target in singly linked list (or NULL for end of list)
        DbgTransportTarget *m_pTarget;      // The actual target object
        DWORD               m_dwIPAddress;  // IP address of the target machine
        USHORT              m_usPort;       // TCP port of the proxy on the machine
        DWORD               m_dwRefCount;   // Number of clients with a reference to this target
    };

    LONG                    m_lRefCount;    // Number of references to this manager outstanding
    TargetRef              *m_pTargets;     // Singly linked list of targets allocated so far
    RSLock                  m_sLock;        // Lock protecting read and write access to the target list
};

// The one and only instance of the DbgTransportManager in the process.
extern DbgTransportManager *g_pDbgTransportManager;

#endif // FEATURE_DBGIPC_TRANSPORT_DI

#endif // __DBG_TRANSPORT_MANAGER_INCLUDED
