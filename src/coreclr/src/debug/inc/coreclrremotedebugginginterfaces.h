//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// Defines interfaces shared between mscordbi and the Visual Studio debugger port supplier plugin that we
// provide for debugging remote CoreCLR instances on the Mac.
//

#ifndef __PORT_SUPPLIER_INTERFACES_INCLUDED
#define __PORT_SUPPLIER_INTERFACES_INCLUDED


#include <dbgproxy.h>


class ICoreClrDebugTarget;


// Mscordbi exports a number of C functions to aid in starting up and shutting down the transport manager
// (which owns communication with remote machines) and getting a connection to a particular machine's proxy
// (known as a target).
extern "C" HRESULT __stdcall InitDbgTransportManager();
extern "C" void __stdcall ShutdownDbgTransportManager();
extern "C" HRESULT __stdcall CreateCoreClrDebugTarget(DWORD dwAddress, ICoreClrDebugTarget **ppTarget);


// Definition of the data that ICoreClrDebugTarget will return about a remote process.
struct CoreClrDebugProcInfo
{
    DWORD   m_dwPID;                                    // OS assigned process ID
    DWORD   m_dwInternalID;                             // Proxy assigned process ID (recycles less often)
    WCHAR   m_wszName[kMaxCommandLine];                 // Command and args process is running (possibly truncated)
};


// Definition of the data that ICoreClrDebugTarget will return about a remote runtime instance.
struct CoreClrDebugRuntimeInfo
{
    DWORD   m_dwInternalID;                             // Proxy assigned runtime instance ID
};


// This pseudo-COM interface is provided by mscordbi and called by the port supplier to query details of a
// remote target.
class ICoreClrDebugTarget
{
public:
    STDMETHOD_(void, AddRef)() PURE;
    STDMETHOD_(void, Release)() PURE;

    // Enumerate all user's processes on the target machine (whether they are running managed code or not).
    STDMETHOD(EnumProcesses)(DWORD *pcProcs, CoreClrDebugProcInfo **ppProcs) PURE;

    // Enumerate all runtimes running within the process indicated via the internal process ID.
    STDMETHOD(EnumRuntimes)(DWORD dwInternalProcessID, DWORD *pcRuntimes, CoreClrDebugRuntimeInfo **ppRuntimes) PURE;

    // Free memory returned by Enum* methods.
    STDMETHOD_(void, FreeMemory)(void *pMemory) PURE;
};

#endif // __PORT_SUPPLIER_INTERFACES_INCLUDED
