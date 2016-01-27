// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//-----------------------------------------------------------------------------
// PerfCounterDefs.h
//
// Internal Interface for CLR to use Performance counters
//-----------------------------------------------------------------------------


#ifndef _PerfCounterDefs_h_
#define _PerfCounterDefs_h_

#include "contract.h"

//-----------------------------------------------------------------------------
// PerfCounters are only enabled if ENABLE_PERF_COUNTERS is defined.
// If we know we want them (such as in the impl or perfmon, then define this
// in before we include this header, else define this from the sources file.
//
// Note that some platforms don't use perfcounters, so to avoid a build
// break, you must wrap PerfCounter code (such as instrumenting in either 
// #ifdef or use the COUNTER_ONLY(x) macro (defined below)
// 

//-----------------------------------------------------------------------------
// Name of global IPC block
#define SHARED_PERF_IPC_NAME W("SharedPerfIPCBlock")


//-----------------------------------------------------------------------------
// Attributes for the IPC block
//-----------------------------------------------------------------------------
const int PERF_ATTR_ON      = 0x0001;   // Are we even updating any counters?
const int PERF_ATTR_GLOBAL  = 0x0002;   // Is this a global or private block?





//.............................................................................
// Tri Counter. Support for the common trio of counters (Total, Current, and 
// Instantaneous). This compiles to the same thing if we had them all separate,
// but it's a lot cleaner this way.
//.............................................................................
struct TRICOUNT
{
    DWORD Cur;                              // Current, has +, -
    DWORD Total;                            // Total, has  only +
    inline void operator++(int) {
        LIMITED_METHOD_CONTRACT;
        Cur ++; Total ++;
    }
    inline void operator--(int) {
        LIMITED_METHOD_CONTRACT;
        Cur --;
    }
    inline void operator+=(int delta) {
        LIMITED_METHOD_CONTRACT;
        Cur += delta; Total += delta;
    }
    inline void operator-=(int delta) {
        LIMITED_METHOD_CONTRACT;
        Cur -= delta;
    }
    inline void operator=(int delta) {
        LIMITED_METHOD_CONTRACT;
        Cur = delta;
        Total = delta;
    }
    inline void operator+=(TRICOUNT delta) {
        LIMITED_METHOD_CONTRACT;
        Cur += delta.Cur; Total += delta.Total;
    }

};

//.............................................................................
// Interlocked Tri Counter. Support for the common trio of counters (Total, Current,
// and Instantaneous). This compiles to the same thing if we had them all separate,
// but it's a lot cleaner this way.
//.............................................................................
struct TRICOUNT_IL
{
    DWORD Cur;                              // Current, has +, -
    DWORD Total;                            // Total, has  only +
    inline void operator++(int) {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement((LPLONG)&Cur); InterlockedIncrement((LPLONG)&Total);
    }
    inline void operator--(int) {
        LIMITED_METHOD_CONTRACT;
        InterlockedDecrement((LPLONG)&Cur);
    }
    inline void operator+=(int delta) {
        LIMITED_METHOD_CONTRACT;
        while (TRUE)
        {
            LONG old_Cur = Cur;
            if (InterlockedCompareExchange((LPLONG)&Cur, Cur+delta, old_Cur) == old_Cur)
                break;
        }
        while (TRUE)
        {
            LONG old_Total = Total;
            if (InterlockedCompareExchange((LPLONG)&Total, Total+delta, old_Total) == old_Total)
                break;
        }
    }
    inline void operator-=(int delta) {
        LIMITED_METHOD_CONTRACT;
        while (TRUE)
        {
            LONG old_Cur = Cur;
            if (InterlockedCompareExchange((LPLONG)&Cur, Cur-delta, old_Cur) == old_Cur)
                break;
        }
    }
    inline void operator=(int delta) {
        LIMITED_METHOD_CONTRACT;
        while (TRUE)
        {
            LONG old_Cur = Cur;
            if (InterlockedCompareExchange((LPLONG)&Cur, delta, old_Cur) == old_Cur)
                break;
        }

        while (TRUE)
        {
            LONG old_Total = Total;
            if (InterlockedCompareExchange((LPLONG)&Total, delta, old_Total) == old_Total)
                break;
        }
    }
    inline void operator+=(TRICOUNT_IL delta) {
        LIMITED_METHOD_CONTRACT;
        while (TRUE)
        {
            LONG old_Cur = Cur;
            if (InterlockedCompareExchange((LPLONG)&Cur, Cur+delta.Cur, old_Cur) == old_Cur)
                break;
        }
        while (TRUE)
        {
            LONG old_Total = Total;
            if (InterlockedCompareExchange((LPLONG)&Total, Total+delta.Total, old_Total) == old_Total)
                break;
        }
    }

};


//.............................................................................
// Dual Counter. Support for the (Total and Instantaneous (rate)). Helpful in cases
// where the current value is always same as the total value. ie. the counter is never
// decremented.
// This compiles to the same thing if we had them separate, but it's a lot cleaner 
// this way.
//.............................................................................
struct DUALCOUNT
{
    DWORD Total;                            
    inline void operator++(int) {
        LIMITED_METHOD_CONTRACT;
        Total ++;
    }

    inline void operator+=(int delta) {
        LIMITED_METHOD_CONTRACT;
        Total += delta;
    }

    inline void operator+=(DUALCOUNT delta) {
        LIMITED_METHOD_CONTRACT;
        Total += delta.Total;
    }


};

//-----------------------------------------------------------------------------
// Format for the Perf Counter IPC Block
// IPC block is broken up into sections. This marks it easier to marshall
// into different perfmon objects
//
//.............................................................................
// Naming convention (by prefix):
// c - Raw count of something.
// cb- count of bytes
// time - time value.
// depth - stack depth
//-----------------------------------------------------------------------------

const int MAX_TRACKED_GENS = 3; // number of generations we track

//
// Perf_GC_Wow64 mimics in a 64 bit process, the layout of Perf_GC in a 32 bit process
// It does this by replacing all size_t by DWORD
//
// *** Keep contents of Perf_GC_Wow64 and Perf_GC in sync ***

struct Perf_GC_Wow64
{
public:
    DWORD cGenCollections[MAX_TRACKED_GENS];// count of collects per gen
    DWORD cbPromotedMem[MAX_TRACKED_GENS-1]; // count of promoted memory
    DWORD cbPromotedFinalizationMem;       // count of memory promoted due to finalization
    DWORD cProcessID;                      // process ID
    DWORD cGenHeapSize[MAX_TRACKED_GENS];  // size of heaps per gen
    DWORD cTotalCommittedBytes;            // total number of committed bytes.
    DWORD cTotalReservedBytes;             // bytes reserved via VirtualAlloc
    DWORD cLrgObjSize;                     // size of Large Object Heap
    DWORD cSurviveFinalize;                // count of instances surviving from finalizing
    DWORD cHandles;                        // count of GC handles
    DWORD cbAlloc;                         // bytes allocated
    DWORD cbLargeAlloc;                    // bytes allocated for Large Objects
    DWORD cInducedGCs;                     // number of explicit GCs
    DWORD timeInGC;                        // Time in GC
    DWORD timeInGCBase;                    // must follow time in GC counter
    
    DWORD cPinnedObj;                      // # of Pinned Objects
    DWORD cSinkBlocks;                     // # of sink blocks
};

// *** Keep contents of Perf_GC_Wow64 and Perf_GC in sync ***
#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_GC
{
public:
    size_t cGenCollections[MAX_TRACKED_GENS];// count of collects per gen
    size_t cbPromotedMem[MAX_TRACKED_GENS-1]; // count of promoted memory
    size_t cbPromotedFinalizationMem;       // count of memory promoted due to finalization
    size_t cProcessID;                      // process ID
    size_t cGenHeapSize[MAX_TRACKED_GENS];  // size of heaps per gen
    size_t cTotalCommittedBytes;            // total number of committed bytes.
    size_t cTotalReservedBytes;             // bytes reserved via VirtualAlloc
    size_t cLrgObjSize;                     // size of Large Object Heap
    size_t cSurviveFinalize;                // count of instances surviving from finalizing
    size_t cHandles;                        // count of GC handles
    size_t cbAlloc;                         // bytes allocated
    size_t cbLargeAlloc;                    // bytes allocated for Large Objects
    size_t cInducedGCs;                     // number of explicit GCs
    DWORD  timeInGC;                        // Time in GC
    DWORD  timeInGCBase;                    // must follow time in GC counter
    
    size_t cPinnedObj;                      // # of Pinned Objects
    size_t cSinkBlocks;                     // # of sink blocks

    Perf_GC();
    Perf_GC(Perf_GC_Wow64& copyFrom);
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

//
// Perf_Loading_Wow64 mimics in a 64 bit process, the layout of Perf_Loading 
// in a 32 bit process. It does this by replacing all size_t by DWORD
//
// *** Keep contents of Perf_Loading_Wow64 and Perf_Loading in sync ***
struct Perf_Loading_Wow64
{
// Loading
public:
    TRICOUNT cClassesLoaded;
    TRICOUNT_IL cAppDomains;                   // Current # of AppDomains
    TRICOUNT cAssemblies;                   // Current # of Assemblies.
    UNALIGNED LONGLONG timeLoading;         // % time loading
    DWORD cAsmSearchLen;                    // Avg search length for assemblies
    DUALCOUNT cLoadFailures;                // Classes Failed to load
    DWORD cbLoaderHeapSize;                 // Total size of heap used by the loader
    DUALCOUNT cAppDomainsUnloaded;          // Rate at which app domains are unloaded
};

// *** Keep contents of Perf_Loading_Wow64 and Perf_Loading in sync ***
#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_Loading
{
// Loading
public:
    TRICOUNT cClassesLoaded;
    TRICOUNT_IL cAppDomains;                   // Current # of AppDomains
    TRICOUNT cAssemblies;                   // Current # of Assemblies.
    UNALIGNED LONGLONG timeLoading;                   // % time loading
    DWORD cAsmSearchLen;                    // Avg search length for assemblies
    DUALCOUNT cLoadFailures;                // Classes Failed to load
    size_t cbLoaderHeapSize;                 // Total size of heap used by the loader
    DUALCOUNT cAppDomainsUnloaded;          // Rate at which app domains are unloaded

    Perf_Loading();
    Perf_Loading(Perf_Loading_Wow64& copyFrom);
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_Jit
{
// Jitting
    DWORD cMethodsJitted;                   // number of methods jitted
    TRICOUNT cbILJitted;                    // IL jitted stats
//    DUALCOUNT cbPitched;                    // Total bytes pitched
    DWORD cJitFailures;                     // # of standard Jit failures
    DWORD timeInJit;                        // Time in JIT since last sample
    DWORD timeInJitBase;                    // Time in JIT base counter
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_Excep
{
// Exceptions
    DUALCOUNT cThrown;                          // Number of Exceptions thrown
    DWORD cFiltersExecuted;                 // Number of Filters executed
    DWORD cFinallysExecuted;                // Number of Finallys executed
    DWORD cThrowToCatchStackDepth;          // Delta from throw to catch site on stack
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_Interop
{
// Interop
    DWORD cCCW;                             // Number of CCWs
    DWORD cStubs;                           // Number of stubs
    DWORD cMarshalling;                      // # of time marshalling args and return values.
    DWORD cTLBImports;                      // Number of tlbs we import
    DWORD cTLBExports;                      // Number of tlbs we export
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_LocksAndThreads
{
// Locks
    DUALCOUNT cContention;                      // # of times in AwareLock::EnterEpilogue()
    TRICOUNT cQueueLength;                      // Lenght of queue
// Threads
    DWORD cCurrentThreadsLogical;           // Number (created - destroyed) of logical threads 
    DWORD cCurrentThreadsPhysical;          // Number (created - destroyed) of OS threads 
    TRICOUNT cRecognizedThreads;            // # of Threads execute in runtime's control
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64


// IMPORTANT!!!!!!!: The first two fields in the struct have to be together
// and be the first two fields in the struct. The managed code in ChannelServices.cs
// depends on this.
#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_Contexts
{
// Contexts & Remoting
    DUALCOUNT cRemoteCalls;                 // # of remote calls    
    DWORD cChannels;                        // Number of current channels
    DWORD cProxies;                         // Number of context proxies. 
    DWORD cClasses;                         // # of Context-bound classes
    DWORD cObjAlloc;                        // # of context bound objects allocated
    DWORD cContexts;                        // The current number of contexts.
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

//
// Perf_Security_Wow64 mimics in a 64 bit process, the layout of Perf_Security 
// in a 32 bit process. It does this by packing all members on 4 byte boundary
// ("timeAuthorize" field which is 8 bytes in size, will get 8 byte aligned 
// on 64 bit by default)
//
// *** Keep contents of Perf_Security_Wow64 and Perf_Security in sync ***
#include <pshpack4.h>
struct Perf_Security_Wow64
{
// Security
public:
    DWORD cTotalRTChecks;                   // Total runtime checks
    UNALIGNED LONGLONG timeAuthorize;       // % time authenticating
    DWORD cLinkChecks;                      // link time checks
    DWORD timeRTchecks;                     // % time in Runtime checks
    DWORD timeRTchecksBase;                 // % time in Runtime checks base counter
    DWORD stackWalkDepth;                   // depth of stack for security checks
};
#include <poppack.h>

#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct Perf_Security
{
// Security
public:
    DWORD cTotalRTChecks;                   // Total runtime checks
    UNALIGNED LONGLONG timeAuthorize;       // % time authenticating
    DWORD cLinkChecks;                      // link time checks
    DWORD timeRTchecks;                     // % time in Runtime checks
    DWORD timeRTchecksBase;                 // % time in Runtime checks base counter
    DWORD stackWalkDepth;                   // depth of stack for security checks

    Perf_Security();
    Perf_Security(Perf_Security_Wow64& copyFrom);
};
#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64


//
// PerfCounterWow64IPCControlBlock mimics in a 64 bit process, the layout of 
// PerfCounterIPCControlBlock in a 32 bit process.
//
// *** Keep contents of PerfCounterWow64IPCControlBlock and PerfCounterIPCControlBlock in sync ***
#include <pshpack4.h>
struct PerfCounterWow64IPCControlBlock
{   
public:
// Versioning info
    WORD m_cBytes;      // size of this entire block
    WORD m_wAttrs;      // attributes for this block

// Counter Sections
    Perf_GC_Wow64       m_GC;
    Perf_Contexts       m_Context;
    Perf_Interop        m_Interop;
    Perf_Loading_Wow64  m_Loading;
    Perf_Excep          m_Excep;
    Perf_LocksAndThreads      m_LocksAndThreads;
    Perf_Jit            m_Jit;
    Perf_Security_Wow64 m_Security;
};
#include <poppack.h>

// Note: PerfMonDll marshalls data out of here by copying a continous block of memory.
// We can still add new members to the subsections above, but if we change their
// placement in the structure below, we may break PerfMon's marshalling

// *** Keep contents of PerfCounterWow64IPCControlBlock and PerfCounterIPCControlBlock in sync ***
#ifndef _WIN64
#include <pshpack4.h>
#endif //#ifndef _WIN64
struct PerfCounterIPCControlBlock
{   
public:
// Versioning info
    WORD m_cBytes;      // size of this entire block
    WORD m_wAttrs;      // attributes for this block

// Counter Sections
    Perf_GC         m_GC;
    Perf_Contexts   m_Context;
    Perf_Interop    m_Interop;
    Perf_Loading    m_Loading;
    Perf_Excep      m_Excep;
    Perf_LocksAndThreads      m_LocksAndThreads;
    Perf_Jit        m_Jit;
    Perf_Security   m_Security;

    PerfCounterIPCControlBlock();
    PerfCounterIPCControlBlock(PerfCounterWow64IPCControlBlock& copyFrom);
};

#ifndef _WIN64
#include <poppack.h>
#endif //#ifndef _WIN64

//
// Inline definitions
//

#include "perfcounterdefs.inl"

#endif // _PerfCounterDefs_h_
