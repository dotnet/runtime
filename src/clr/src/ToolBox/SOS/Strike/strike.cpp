// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==

// ===========================================================================
// STRIKE.CPP
// ===========================================================================
//
// History:
//   09/07/99  Microsoft  Created
//
//************************************************************************************************
// SOS is the native debugging extension designed to support investigations into CLR (mis-)
// behavior by both users of the runtime as well as the code owners. It allows inspection of 
// internal structures, of user visible entities, as well as execution control.
// 
// This is the main SOS file hosting the implementation of all the exposed commands. A good 
// starting point for understanding the semantics of these commands is the sosdocs.txt file.
// 
// #CrossPlatformSOS
// SOS currently supports cross platform debugging from x86 to ARM. It takes a different approach 
// from the DAC: whereas for the DAC we produce one binary for each supported host-target 
// architecture pair, for SOS we produce only one binary for each host architecture; this one 
// binary contains code for all supported target architectures. In doing this SOS depends on two
// assumptions:
//   . that the debugger will load the appropriate DAC, and 
//   . that the host and target word size is identical.
// The second assumption is identical to the DAC assumption, and there will be considerable effort
// required (in the EE, the DAC, and SOS) if we ever need to remove it.
// 
// In an ideal world SOS would be able to retrieve all platform specific information it needs 
// either from the debugger or from DAC. However, SOS has taken some subtle and not so subtle
// dependencies on the CLR and the target platform.
// To resolve this problem, SOS now abstracts the target behind the IMachine interface, and uses 
// calls on IMachine to take target-specific actions. It implements X86Machine, ARMMachine, and 
// AMD64Machine. An instance of these exists in each appropriate host (e.g. the X86 version of SOS
// contains instances of X86Machine and ARMMachine, the ARM version contains an instance of 
// ARMMachine, and the AMD64 version contains an instance of AMD64Machine). The code included in 
// each version if determined by the SosTarget*** MSBuild symbols, and SOS_TARGET_*** conditional 
// compilation symbols (as specified in sos.targets).
// 
// Most of the target specific code is hosted in disasm.h/.cpp, and disasmX86.cpp, disasmARM.cpp.
// Some code currently under _TARGET_*** ifdefs may need to be reviewed/revisited.
// 
// Issues:
// The one-binary-per-host decision does have some drawbacks: 
//   . Currently including system headers or even CLR headers will only account for the host 
//     target, IOW, when building the X86 version of SOS, CONTEXT will refer to the X86 CONTEXT 
//     structure, so we need to be careful when debugging ARM targets. The CONTEXT issue is 
//     partially resolved by CROSS_PLATFORM_CONTEXT (there is still a need to be very careful 
//     when handling arrays of CONTEXTs - see _EFN_StackTrace for details on this).
//   . For larger includes (e.g. GC info), we will need to include files in specific namespaces, 
//     with specific _TARGET_*** macros defined in order to avoid name clashes and ensure correct
//     system types are used.
// -----------------------------------------------------------------------------------------------

#define DO_NOT_DISABLE_RAND //this is a standalone tool, and can use rand()

#include <windows.h>
#include <winver.h>
#include <winternl.h>
#include <psapi.h>
#ifndef FEATURE_PAL
#include <list>   
#endif // !FEATURE_PAL
#include <wchar.h>

#include "platformspecific.h"

#define NOEXTAPI
#define KDEXT_64BIT
#include <wdbgexts.h>
#undef DECLARE_API
#undef StackTrace

#include <dbghelp.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stddef.h>

#include "strike.h"
#include "sos.h"

#ifndef STRESS_LOG
#define STRESS_LOG
#endif // STRESS_LOG
#define STRESS_LOG_READONLY
#include "stresslog.h"

#include "util.h"

#include "corhdr.h"
#include "cor.h"
#include "cordebug.h"
#include "dacprivate.h"
#include "corexcep.h"

#define  CORHANDLE_MASK 0x1
#define SWITCHED_OUT_FIBER_OSID 0xbaadf00d;

#define DEFINE_EXT_GLOBALS

#include "data.h"
#include "disasm.h"

#include "predeftlsslot.h"

#include "hillclimbing.h"

#include "sos_md.h"

#ifndef FEATURE_PAL

#include "ExpressionNode.h"
#include "WatchCmd.h"

#include <algorithm>

#include "tls.h"

typedef struct _VM_COUNTERS {
    SIZE_T PeakVirtualSize;
    SIZE_T VirtualSize;
    ULONG PageFaultCount;
    SIZE_T PeakWorkingSetSize;
    SIZE_T WorkingSetSize;
    SIZE_T QuotaPeakPagedPoolUsage;
    SIZE_T QuotaPagedPoolUsage;
    SIZE_T QuotaPeakNonPagedPoolUsage;
    SIZE_T QuotaNonPagedPoolUsage;
    SIZE_T PagefileUsage;
    SIZE_T PeakPagefileUsage;
} VM_COUNTERS;
typedef VM_COUNTERS *PVM_COUNTERS;

const PROCESSINFOCLASS ProcessVmCounters = static_cast<PROCESSINFOCLASS>(3);

#endif // !FEATURE_PAL

#include <set>
#include <vector>
#include <map>

BOOL CallStatus;
BOOL ControlC = FALSE;

IMetaDataDispenserEx *pDisp = NULL;
WCHAR g_mdName[mdNameLen];

#ifndef FEATURE_PAL
HMODULE g_hInstance = NULL;
#include <algorithm>
#endif // !FEATURE_PAL

#ifdef _MSC_VER
#pragma warning(disable:4244)   // conversion from 'unsigned int' to 'unsigned short', possible loss of data
#pragma warning(disable:4189)   // local variable is initialized but not referenced
#endif

#ifdef FEATURE_PAL
#define SOSPrefix ""
#define SOSThreads "clrthreads"
#else
#define SOSPrefix "!"
#define SOSThreads "!threads"
#endif

#if defined _X86_ && !defined FEATURE_PAL
// disable FPO for X86 builds
#pragma optimize("y", off)
#endif

#undef assert

#ifdef _MSC_VER
#pragma warning(default:4244)
#pragma warning(default:4189)
#endif

#ifndef FEATURE_PAL
#include "ntinfo.h"
#endif // FEATURE_PAL

#ifndef IfFailRet
#define IfFailRet(EXPR) do { Status = (EXPR); if(FAILED(Status)) { return (Status); } } while (0)
#endif

#ifdef FEATURE_PAL

#define NOTHROW
#define MINIDUMP_NOT_SUPPORTED()

#else // !FEATURE_PAL

#define MINIDUMP_NOT_SUPPORTED()   \
    if (IsMiniDumpFile())      \
    {                          \
        ExtOut("This command is not supported in a minidump without full memory\n"); \
        ExtOut("To try the command anyway, run !MinidumpMode 0\n"); \
        return Status;         \
    }

#define NOTHROW (std::nothrow)

#include "safemath.h"

DECLARE_API (MinidumpMode)
{
    INIT_API ();
    DWORD_PTR Value=0;

    CMDValue arg[] = 
    {   // vptr, type
        {&Value, COHEX}
    };

    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg)) 
    {
        return Status;
    }    
    if (nArg == 0)
    {
        // Print status of current mode
       ExtOut("Current mode: %s - unsafe minidump commands are %s.\n",
               g_InMinidumpSafeMode ? "1" : "0",
               g_InMinidumpSafeMode ? "disabled" : "enabled");
    }
    else
    {
        if (Value != 0 && Value != 1)
        {
            ExtOut("Mode must be 0 or 1\n");
            return Status;
        }

        g_InMinidumpSafeMode = (BOOL) Value;
        ExtOut("Unsafe minidump commands are %s.\n",
                g_InMinidumpSafeMode ? "disabled" : "enabled");
    }

    return Status;
}

#endif // FEATURE_PAL

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to get the MethodDesc for a given eip     *  
*                                                                      *
\**********************************************************************/
DECLARE_API(IP2MD)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    BOOL dml = FALSE;
    TADDR IP = 0;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&IP, COHEX},
    };
    size_t nArg;
    
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    EnableDMLHolder dmlHolder(dml);

    if (IP == 0)
    {
        ExtOut("%s is not IP\n", args);
        return Status;
    }

    CLRDATA_ADDRESS cdaStart = TO_CDADDR(IP);
    CLRDATA_ADDRESS pMD;

    
    if ((Status = g_sos->GetMethodDescPtrFromIP(cdaStart, &pMD)) != S_OK)
    {
        ExtOut("Failed to request MethodData, not in JIT code range\n");
        return Status;
    }

    DMLOut("MethodDesc:   %s\n", DMLMethodDesc(pMD));
    DumpMDInfo(TO_TADDR(pMD), cdaStart, FALSE /* fStackTraceFormat */);

    WCHAR filename[MAX_LONGPATH];
    ULONG linenum;
    // symlines will be non-zero only if SYMOPT_LOAD_LINES was set in the symbol options
    ULONG symlines = 0;
    if (SUCCEEDED(g_ExtSymbols->GetSymbolOptions(&symlines)))
    {
        symlines &= SYMOPT_LOAD_LINES;
    }

    if (symlines != 0 && 
        SUCCEEDED(GetLineByOffset(TO_CDADDR(IP), &linenum, filename, _countof(filename))))
    {
        ExtOut("Source file:  %S @ %d\n", filename, linenum);
    }

    return Status;
}

// (MAX_STACK_FRAMES is also used by x86 to prevent infinite loops in _EFN_StackTrace)
#define MAX_STACK_FRAMES 1000

#if defined(_TARGET_WIN64_)
#define DEBUG_STACK_CONTEXT AMD64_CONTEXT
#elif defined(_TARGET_ARM_) // _TARGET_WIN64_
#define DEBUG_STACK_CONTEXT ARM_CONTEXT
#elif defined(_TARGET_X86_) // _TARGET_ARM_
#define DEBUG_STACK_CONTEXT X86_CONTEXT
#endif // _TARGET_X86_

#ifdef DEBUG_STACK_CONTEXT
// I use a global set of frames for stack walking on win64 because the debugger's
// GetStackTrace function doesn't provide a way to find out the total size of a stackwalk,
// and I'd like to have a reasonably big maximum without overflowing the stack by declaring
// the buffer locally and I also want to get a managed trace in a low memory environment
// (so no dynamic allocation if possible).
DEBUG_STACK_FRAME g_Frames[MAX_STACK_FRAMES];
DEBUG_STACK_CONTEXT g_FrameContexts[MAX_STACK_FRAMES];

static HRESULT
GetContextStackTrace(ULONG osThreadId, PULONG pnumFrames)
{
    PDEBUG_CONTROL4 debugControl4;
    HRESULT hr;

    // Do we have advanced capability?
    if ((hr = g_ExtControl->QueryInterface(__uuidof(IDebugControl4), (void **)&debugControl4)) == S_OK)
    {
        ULONG oldId, id;
        g_ExtSystem->GetCurrentThreadId(&oldId);

        if ((hr = g_ExtSystem->GetThreadIdBySystemId(osThreadId, &id)) != S_OK) {
            return hr;
        }
        g_ExtSystem->SetCurrentThreadId(id);

        // GetContextStackTrace fills g_FrameContexts as an array of
        // contexts packed as target architecture contexts. We cannot 
        // safely cast this as an array of CROSS_PLATFORM_CONTEXT, since 
        // sizeof(CROSS_PLATFORM_CONTEXT) != sizeof(TGT_CONTEXT)
        hr = debugControl4->GetContextStackTrace(
            NULL,
            0,
            g_Frames,
            MAX_STACK_FRAMES,
            g_FrameContexts,
            MAX_STACK_FRAMES*g_targetMachine->GetContextSize(),
            g_targetMachine->GetContextSize(),
            pnumFrames);

        g_ExtSystem->SetCurrentThreadId(oldId);
        debugControl4->Release();
    }
    return hr;
}

#endif // DEBUG_STACK_CONTEXT

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function displays the stack trace.  It looks at each DWORD   *  
*    on stack.  If the DWORD is a return address, the symbol name or
*    managed function name is displayed.                               *
*                                                                      *
\**********************************************************************/
void DumpStackInternal(DumpStackFlag *pDSFlag)
{    
    ReloadSymbolWithLineInfo();
    
    ULONG64 StackOffset;
    g_ExtRegisters->GetStackOffset (&StackOffset);
    if (pDSFlag->top == 0) {
        pDSFlag->top = TO_TADDR(StackOffset);
    }
    size_t value;
    while (g_ExtData->ReadVirtual(TO_CDADDR(pDSFlag->top), &value, sizeof(size_t), NULL) != S_OK) {
        if (IsInterrupt())
            return;
        pDSFlag->top = NextOSPageAddress(pDSFlag->top);
    }

#ifndef FEATURE_PAL     
    if (pDSFlag->end == 0) {
        // Find the current stack range
        NT_TIB teb;
        ULONG64 dwTebAddr=0;

        g_ExtSystem->GetCurrentThreadTeb(&dwTebAddr);
        if (SafeReadMemory(TO_TADDR(dwTebAddr), &teb, sizeof(NT_TIB), NULL))
        {
            if (pDSFlag->top > TO_TADDR(teb.StackLimit)
            && pDSFlag->top <= TO_TADDR(teb.StackBase))
            {
                if (pDSFlag->end == 0 || pDSFlag->end > TO_TADDR(teb.StackBase))
                    pDSFlag->end = TO_TADDR(teb.StackBase);
            }
        }
    }
#endif // FEATURE_PAL
    
    if (pDSFlag->end == 0)
    {
        ExtOut("TEB information is not available so a stack size of 0xFFFF is assumed\n");
        pDSFlag->end = pDSFlag->top + 0xFFFF;
    }
    
    if (pDSFlag->end < pDSFlag->top)
    {
        ExtOut("Wrong option: stack selection wrong\n");
        return;
    }

    DumpStackWorker(*pDSFlag);
}

#if defined(FEATURE_PAL) && defined(_TARGET_AMD64_)
static BOOL UnwindStackFrames(ULONG32 osThreadId);
#endif

DECLARE_API(DumpStack)
{
    INIT_API_NO_RET_ON_FAILURE();

    MINIDUMP_NOT_SUPPORTED();

    DumpStackFlag DSFlag;
    DSFlag.fEEonly = FALSE;
    DSFlag.fSuppressSrcInfo = FALSE;
    DSFlag.top = 0;
    DSFlag.end = 0;

    BOOL unwind = FALSE;
    BOOL dml = FALSE;
    CMDOption option[] = {
        // name, vptr, type, hasValue
        {"-EE", &DSFlag.fEEonly, COBOOL, FALSE},
        {"-n",  &DSFlag.fSuppressSrcInfo, COBOOL, FALSE},
        {"-unwind",  &unwind, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };
    CMDValue arg[] = {
        // vptr, type
        {&DSFlag.top, COHEX},
        {&DSFlag.end, COHEX}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
        return Status;

    // symlines will be non-zero only if SYMOPT_LOAD_LINES was set in the symbol options
    ULONG symlines = 0;
    if (!DSFlag.fSuppressSrcInfo && SUCCEEDED(g_ExtSymbols->GetSymbolOptions(&symlines)))
    {
        symlines &= SYMOPT_LOAD_LINES;
    }
    DSFlag.fSuppressSrcInfo = DSFlag.fSuppressSrcInfo || (symlines == 0);

    EnableDMLHolder enabledml(dml);

    ULONG sysId = 0, id = 0;
    g_ExtSystem->GetCurrentThreadSystemId(&sysId);
    ExtOut("OS Thread Id: 0x%x ", sysId);
    g_ExtSystem->GetCurrentThreadId(&id);
    ExtOut("(%d)\n", id);

#if defined(FEATURE_PAL) && defined(_TARGET_AMD64_)
    if (unwind)
    {
        UnwindStackFrames(sysId);
    }
    else
#endif
    {
        DumpStackInternal(&DSFlag);
    }
    return Status;
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function displays the stack trace for threads that EE knows  *  
*    from ThreadStore.                                                 *
*                                                                      *
\**********************************************************************/
DECLARE_API (EEStack)
{
    INIT_API();    

    MINIDUMP_NOT_SUPPORTED();  

    DumpStackFlag DSFlag;
    DSFlag.fEEonly = FALSE;
    DSFlag.fSuppressSrcInfo = FALSE;
    DSFlag.top = 0;
    DSFlag.end = 0;

    BOOL bShortList = FALSE;
    BOOL dml = FALSE;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-EE", &DSFlag.fEEonly, COBOOL, FALSE},
        {"-short", &bShortList, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };    

    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
    {
        return Status;
    }

    EnableDMLHolder enableDML(dml);

    ULONG Tid;
    g_ExtSystem->GetCurrentThreadId(&Tid);

    DacpThreadStoreData ThreadStore;
    if ((Status = ThreadStore.Request(g_sos)) != S_OK)
    {
        ExtOut("Failed to request ThreadStore\n");
        return Status;
    }    

    CLRDATA_ADDRESS CurThread = ThreadStore.firstThread;
    while (CurThread)
    {
        if (IsInterrupt())
            break;

        DacpThreadData Thread;        
        if ((Status = Thread.Request(g_sos, CurThread)) != S_OK)
        {
            ExtOut("Failed to request Thread at %p\n", CurThread);
            return Status;
        }

        ULONG id=0;
        if (g_ExtSystem->GetThreadIdBySystemId (Thread.osThreadId, &id) != S_OK)
        {
            CurThread = Thread.nextThread;    
            continue;
        }
        
        ExtOut("---------------------------------------------\n");
        ExtOut("Thread %3d\n", id);
        BOOL doIt = FALSE;

        
#define TS_Hijacked 0x00000080

        if (!bShortList) 
        {
            doIt = TRUE;
        }
        else if ((Thread.lockCount > 0) || (Thread.state & TS_Hijacked)) 
        {             
            // TODO: bring back || (int)vThread.m_pFrame != -1  {
            doIt = TRUE;
        }
        else 
        {
            ULONG64 IP;
            g_ExtRegisters->GetInstructionOffset (&IP);
            JITTypes jitType;
            TADDR methodDesc;
            TADDR gcinfoAddr;
            IP2MethodDesc (TO_TADDR(IP), methodDesc, jitType, gcinfoAddr);
            if (methodDesc)
            {
                doIt = TRUE;
            }
        }
        
        if (doIt) 
        {
            g_ExtSystem->SetCurrentThreadId(id);
            DSFlag.top = 0;
            DSFlag.end = 0;
            DumpStackInternal(&DSFlag);
        }

        CurThread = Thread.nextThread;
    }

    g_ExtSystem->SetCurrentThreadId(Tid);
    return Status;
}

HRESULT DumpStackObjectsRaw(size_t nArg, __in_z LPSTR exprBottom, __in_z LPSTR exprTop, BOOL bVerify)
{
    size_t StackTop = 0;
    size_t StackBottom = 0;
    if (nArg==0)
    {
        ULONG64 StackOffset;
        g_ExtRegisters->GetStackOffset(&StackOffset);

        StackTop = TO_TADDR(StackOffset);
    }
    else
    {
        StackTop = GetExpression(exprTop);
        if (StackTop == 0)
        {
            ExtOut("wrong option: %s\n", exprTop);
            return E_FAIL;
        }

        if (nArg==2)
        {
            StackBottom = GetExpression(exprBottom);
            if (StackBottom == 0)
            {
                ExtOut("wrong option: %s\n", exprBottom);
                return E_FAIL;
            }
        }
    }
    
#ifndef FEATURE_PAL
    NT_TIB teb;
    ULONG64 dwTebAddr=0;
    HRESULT hr = g_ExtSystem->GetCurrentThreadTeb(&dwTebAddr);
    if (SUCCEEDED(hr) && SafeReadMemory (TO_TADDR(dwTebAddr), &teb, sizeof (NT_TIB), NULL))
    {
        if (StackTop > TO_TADDR(teb.StackLimit) && StackTop <= TO_TADDR(teb.StackBase))
        {
            if (StackBottom == 0 || StackBottom > TO_TADDR(teb.StackBase))
                StackBottom = TO_TADDR(teb.StackBase);
        }
    }
#endif
    
    if (StackBottom == 0)
        StackBottom = StackTop + 0xFFFF;
    
    if (StackBottom < StackTop)
    {
        ExtOut("Wrong option: stack selection wrong\n");
        return E_FAIL;
    }

    // We can use the gc snapshot to eliminate object addresses that are
    // not on the gc heap. 
    if (!g_snapshot.Build())
    {
        ExtOut("Unable to determine bounds of gc heap\n");
        return E_FAIL;
    }   

    // Print thread ID.
    ULONG id = 0;
    g_ExtSystem->GetCurrentThreadSystemId (&id);
    ExtOut("OS Thread Id: 0x%x ", id);
    g_ExtSystem->GetCurrentThreadId (&id);
    ExtOut("(%d)\n", id);
    
    DumpStackObjectsHelper(StackTop, StackBottom, bVerify);
    return S_OK;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the address and name of all       *
*    Managed Objects on the stack.                                     *  
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpStackObjects)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    StringHolder exprTop, exprBottom;

    BOOL bVerify = FALSE;
    BOOL dml = FALSE;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-verify", &bVerify, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };    
    CMDValue arg[] = 
    {   // vptr, type
        {&exprTop.data, COSTRING},
        {&exprBottom.data, COSTRING}
    };
    size_t nArg;

    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder enableDML(dml);
    
    return DumpStackObjectsRaw(nArg, exprBottom.data, exprTop.data, bVerify);
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a MethodDesc      *
*    for a given address                                               *  
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpMD)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    
    DWORD_PTR dwStartAddr = NULL;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&dwStartAddr, COHEX},
    };
    size_t nArg;

    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    
    DumpMDInfo(dwStartAddr);
    
    return Status;
}

BOOL GatherDynamicInfo(TADDR DynamicMethodObj, DacpObjectData *codeArray, 
                       DacpObjectData *tokenArray, TADDR *ptokenArrayAddr)
{
    BOOL bRet = FALSE;
    int iOffset;
    DacpObjectData objData; // temp object

    if (codeArray == NULL || tokenArray == NULL)
        return bRet;
    
    if (objData.Request(g_sos, TO_CDADDR(DynamicMethodObj)) != S_OK)
        return bRet;
    
    iOffset = GetObjFieldOffset(TO_CDADDR(DynamicMethodObj), objData.MethodTable, W("m_resolver"));
    if (iOffset <= 0)
        return bRet;
    
    TADDR resolverPtr;
    if (FAILED(MOVE(resolverPtr, DynamicMethodObj + iOffset)))
        return bRet;

    if (objData.Request(g_sos, TO_CDADDR(resolverPtr)) != S_OK)
        return bRet;
    
    iOffset = GetObjFieldOffset(TO_CDADDR(resolverPtr), objData.MethodTable, W("m_code"));
    if (iOffset <= 0)
        return bRet;

    TADDR codePtr;
    if (FAILED(MOVE(codePtr, resolverPtr + iOffset)))
        return bRet;

    if (codeArray->Request(g_sos, TO_CDADDR(codePtr)) != S_OK)
        return bRet;
    
    if (codeArray->dwComponentSize != 1)
        return bRet;
        
    // We also need the resolution table
    iOffset = GetObjFieldOffset (TO_CDADDR(resolverPtr), objData.MethodTable, W("m_scope"));
    if (iOffset <= 0)
        return bRet;

    TADDR scopePtr;
    if (FAILED(MOVE(scopePtr, resolverPtr + iOffset)))
        return bRet;

    if (objData.Request(g_sos, TO_CDADDR(scopePtr)) != S_OK)
        return bRet;
    
    iOffset = GetObjFieldOffset (TO_CDADDR(scopePtr), objData.MethodTable, W("m_tokens"));
    if (iOffset <= 0)
        return bRet;

    TADDR tokensPtr;
    if (FAILED(MOVE(tokensPtr, scopePtr + iOffset)))
        return bRet;

    if (objData.Request(g_sos, TO_CDADDR(tokensPtr)) != S_OK)
        return bRet;
    
    iOffset = GetObjFieldOffset(TO_CDADDR(tokensPtr), objData.MethodTable, W("_items"));
    if (iOffset <= 0)
        return bRet;

    TADDR itemsPtr;
    MOVE (itemsPtr, tokensPtr + iOffset);

    *ptokenArrayAddr = itemsPtr;
    
    if (tokenArray->Request(g_sos, TO_CDADDR(itemsPtr)) != S_OK)
        return bRet;

    bRet = TRUE; // whew.
    return bRet;
}

DECLARE_API(DumpIL)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    DWORD_PTR dwStartAddr = NULL;
    DWORD_PTR dwDynamicMethodObj = NULL;
    BOOL dml = FALSE;
    BOOL fILPointerDirectlySpecified = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
        {"/i", &fILPointerDirectlySpecified, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&dwStartAddr, COHEX},
    };
    size_t nArg;

    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);    
    if (dwStartAddr == NULL)
    {
        ExtOut("Must pass a valid expression\n");
        return Status;
    }

    if (fILPointerDirectlySpecified)
    {
        return DecodeILFromAddress(NULL, dwStartAddr);
    }

    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        return Status;
    }

    if (g_snapshot.GetHeap(dwStartAddr) != NULL)
    {
        dwDynamicMethodObj = dwStartAddr;
    }
    
    if (dwDynamicMethodObj == NULL)
    {
        // We have been given a MethodDesc
        DacpMethodDescData MethodDescData;
        if (MethodDescData.Request(g_sos, TO_CDADDR(dwStartAddr)) != S_OK)
        {
            ExtOut("%p is not a MethodDesc\n", SOS_PTR(dwStartAddr));
            return Status;
        }

        if (MethodDescData.bIsDynamic && MethodDescData.managedDynamicMethodObject)
        {
            dwDynamicMethodObj = TO_TADDR(MethodDescData.managedDynamicMethodObject);
            if (dwDynamicMethodObj == NULL)
            {
                ExtOut("Unable to print IL for DynamicMethodDesc %p\n", SOS_PTR(dwDynamicMethodObj));
                return Status;
            }
        }
        else
        {
            // This is not a dynamic method, print the IL for it.
            // Get the module
            DacpModuleData dmd;    
            if (dmd.Request(g_sos, MethodDescData.ModulePtr) != S_OK)
            {
                ExtOut("Unable to get module\n");
                return Status;
            }

            ToRelease<IMetaDataImport> pImport = MDImportForModule(&dmd);
            if (pImport == NULL)
            {
                ExtOut("bad import\n");
                return Status;
            }

            ULONG pRva;
            DWORD dwFlags;
            if (pImport->GetRVA(MethodDescData.MDToken, &pRva, &dwFlags) != S_OK)
            {
                ExtOut("error in import\n");
                return Status;
            }    

            CLRDATA_ADDRESS ilAddrClr;
            if (g_sos->GetILForModule(MethodDescData.ModulePtr, pRva, &ilAddrClr) != S_OK)
            {
                ExtOut("FindIL failed\n");
                return Status;
            }

            TADDR ilAddr = TO_TADDR(ilAddrClr);
            IfFailRet(DecodeILFromAddress(pImport, ilAddr));
        }
    }
    
    if (dwDynamicMethodObj != NULL)
    {
        // We have a DynamicMethod managed object, let us visit the town and paint.        
        DacpObjectData codeArray;
        DacpObjectData tokenArray;
        DWORD_PTR tokenArrayAddr;
        if (!GatherDynamicInfo (dwDynamicMethodObj, &codeArray, &tokenArray, &tokenArrayAddr))
        {
            DMLOut("Error gathering dynamic info from object at %s.\n", DMLObject(dwDynamicMethodObj));
            return Status;
        }
        
        // Read the memory into a local buffer
        BYTE *pArray = new NOTHROW BYTE[(SIZE_T)codeArray.dwNumComponents];
        if (pArray == NULL)
        {
            ExtOut("Not enough memory to read IL\n");
            return Status;
        }
        
        Status = g_ExtData->ReadVirtual(UL64_TO_CDA(codeArray.ArrayDataPtr), pArray, (ULONG)codeArray.dwNumComponents, NULL);
        if (Status != S_OK)
        {
            ExtOut("Failed to read memory\n");
            delete [] pArray;
            return Status;
        }

        // Now we have a local copy of the IL, and a managed array for token resolution.
        // Visit our IL parser with this info.        
        ExtOut("This is dynamic IL. Exception info is not reported at this time.\n");
        ExtOut("If a token is unresolved, run \"!do <addr>\" on the addr given\n");
        ExtOut("in parenthesis. You can also look at the token table yourself, by\n");
        ExtOut("running \"!DumpArray %p\".\n\n", SOS_PTR(tokenArrayAddr));
        DecodeDynamicIL(pArray, (ULONG)codeArray.dwNumComponents, tokenArray);
        
        delete [] pArray;                
    }    
    return Status;
}

void DumpSigWorker (
        DWORD_PTR dwSigAddr,
        DWORD_PTR dwModuleAddr,
        BOOL fMethod)
{
    //
    // Find the length of the signature and copy it into the debugger process.
    //

    ULONG cbSig = 0;
    const ULONG cbSigInc = 256;
    ArrayHolder<COR_SIGNATURE> pSig = new NOTHROW COR_SIGNATURE[cbSigInc];
    if (pSig == NULL)
    {
        ReportOOM();        
        return;
    }
    
    CQuickBytes sigString;
    for (;;)
    {
        if (IsInterrupt())
            return;

        ULONG cbCopied;
        if (!SafeReadMemory(TO_TADDR(dwSigAddr + cbSig), pSig + cbSig, cbSigInc, &cbCopied))
            return;
        cbSig += cbCopied;

        sigString.ReSize(0);
        GetSignatureStringResults result;
        if (fMethod)
            result = GetMethodSignatureString(pSig, cbSig, dwModuleAddr, &sigString);
        else
            result = GetSignatureString(pSig, cbSig, dwModuleAddr, &sigString);

        if (GSS_ERROR == result)
            return;

        if (GSS_SUCCESS == result)
            break;

        // If we didn't get the full amount back, and we failed to parse the
        // signature, it's not valid because of insufficient data
        if (cbCopied < 256)
        {
            ExtOut("Invalid signature\n");
            return;
        }

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6280) // "Suppress PREFast warning about mismatch alloc/free"
#endif

        PCOR_SIGNATURE pSigNew = (PCOR_SIGNATURE)realloc(pSig, cbSig+cbSigInc);

#ifdef _PREFAST_
#pragma warning(pop)
#endif

        if (pSigNew == NULL)
        {
            ExtOut("Out of memory\n");
            return;
        }
        
        pSig = pSigNew;
    }

    ExtOut("%S\n", (PCWSTR)sigString.Ptr());
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump a signature object.               *
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpSig)
{
    INIT_API();

    MINIDUMP_NOT_SUPPORTED();
    
    //
    // Fetch arguments
    //

    StringHolder sigExpr;
    StringHolder moduleExpr;
    CMDValue arg[] = 
    {
        {&sigExpr.data, COSTRING},
        {&moduleExpr.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg))
    {
        return Status;
    }
    if (nArg != 2)
    {
        ExtOut("!DumpSig <sigaddr> <moduleaddr>\n");
        return Status;
    }

    DWORD_PTR dwSigAddr = GetExpression(sigExpr.data);        
    DWORD_PTR dwModuleAddr = GetExpression(moduleExpr.data);

    if (dwSigAddr == 0 || dwModuleAddr == 0)
    {
        ExtOut("Invalid parameters %s %s\n", sigExpr.data, moduleExpr.data);
        return Status;
    }
    
    DumpSigWorker(dwSigAddr, dwModuleAddr, TRUE);
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump a portion of a signature object.  *
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpSigElem)
{
    INIT_API();

    MINIDUMP_NOT_SUPPORTED();
    

    //
    // Fetch arguments
    //

    StringHolder sigExpr;
    StringHolder moduleExpr;
    CMDValue arg[] = 
    {
        {&sigExpr.data, COSTRING},
        {&moduleExpr.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg))
    {
        return Status;
    }

    if (nArg != 2)
    {
        ExtOut("!DumpSigElem <sigaddr> <moduleaddr>\n");
        return Status;
    }

    DWORD_PTR dwSigAddr = GetExpression(sigExpr.data);        
    DWORD_PTR dwModuleAddr = GetExpression(moduleExpr.data);

    if (dwSigAddr == 0 || dwModuleAddr == 0)
    {
        ExtOut("Invalid parameters %s %s\n", sigExpr.data, moduleExpr.data);
        return Status;
    }

    DumpSigWorker(dwSigAddr, dwModuleAddr, FALSE);
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of an EEClass from   *  
*    a given address
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpClass)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    
    DWORD_PTR dwStartAddr = 0;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&dwStartAddr, COHEX}
    };

    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    if (nArg == 0) 
    {
        ExtOut("Missing EEClass address\n");
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);

    CLRDATA_ADDRESS methodTable;
    if ((Status=g_sos->GetMethodTableForEEClass(TO_CDADDR(dwStartAddr), &methodTable)) != S_OK)
    {
        ExtOut("Invalid EEClass address\n");
        return Status;
    }

    DacpMethodTableData mtdata;
    if ((Status=mtdata.Request(g_sos, TO_CDADDR(methodTable)))!=S_OK)
    {
        ExtOut("EEClass has an invalid MethodTable address\n");
        return Status;
    }            

    sos::MethodTable mt = TO_TADDR(methodTable);
    ExtOut("Class Name:      %S\n", mt.GetName());

    WCHAR fileName[MAX_LONGPATH];
    FileNameForModule(TO_TADDR(mtdata.Module), fileName);
    ExtOut("mdToken:         %p\n", mtdata.cl);
    ExtOut("File:            %S\n", fileName);

    CLRDATA_ADDRESS ParentEEClass = NULL;
    if (mtdata.ParentMethodTable)
    {
        DacpMethodTableData mtdataparent;
        if ((Status=mtdataparent.Request(g_sos, TO_CDADDR(mtdata.ParentMethodTable)))!=S_OK)
        {
            ExtOut("EEClass has an invalid MethodTable address\n");
            return Status;
        }                     
        ParentEEClass = mtdataparent.Class;
    }

    DMLOut("Parent Class:    %s\n", DMLClass(ParentEEClass));
    DMLOut("Module:          %s\n", DMLModule(mtdata.Module));
    DMLOut("Method Table:    %s\n", DMLMethodTable(methodTable));
    ExtOut("Vtable Slots:    %x\n", mtdata.wNumVirtuals);
    ExtOut("Total Method Slots:  %x\n", mtdata.wNumVtableSlots);
    ExtOut("Class Attributes:    %x  ", mtdata.dwAttrClass);

    if (IsTdInterface(mtdata.dwAttrClass))
        ExtOut("Interface, ");
    if (IsTdAbstract(mtdata.dwAttrClass))
        ExtOut("Abstract, ");
    if (IsTdImport(mtdata.dwAttrClass))
        ExtOut("ComImport, ");
    
    ExtOut("\n");        

    DacpMethodTableFieldData vMethodTableFields;
    if (SUCCEEDED(vMethodTableFields.Request(g_sos, methodTable)))
    {
        ExtOut("NumInstanceFields:   %x\n", vMethodTableFields.wNumInstanceFields);
        ExtOut("NumStaticFields:     %x\n", vMethodTableFields.wNumStaticFields);

        if (vMethodTableFields.wNumThreadStaticFields != 0)
        {
            ExtOut("NumThreadStaticFields: %x\n", vMethodTableFields.wNumThreadStaticFields);
        }

        if (vMethodTableFields.wNumInstanceFields + vMethodTableFields.wNumStaticFields > 0)
        {
            DisplayFields(methodTable, &mtdata, &vMethodTableFields, NULL, TRUE, FALSE);
        }
    }

    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a MethodTable     *  
*    from a given address                                              *
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpMT)
{
    DWORD_PTR dwStartAddr=0;
    DWORD_PTR dwOriginalAddr;
    
    INIT_API();

    MINIDUMP_NOT_SUPPORTED();
    
    BOOL bDumpMDTable = FALSE;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-MD", &bDumpMDTable, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&dwStartAddr, COHEX}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    TableOutput table(2, 16, AlignLeft, false);

    if (nArg == 0)
    {
        Print("Missing MethodTable address\n");
        return Status;
    }

    dwOriginalAddr = dwStartAddr;
    dwStartAddr = dwStartAddr&~3;
    
    if (!IsMethodTable(dwStartAddr))
    {
        Print(dwOriginalAddr, " is not a MethodTable\n");
        return Status;
    }
 
    DacpMethodTableData vMethTable;
    vMethTable.Request(g_sos, TO_CDADDR(dwStartAddr));    

    if (vMethTable.bIsFree) 
    {
        Print("Free MethodTable\n");
        return Status;
    }

    DacpMethodTableCollectibleData vMethTableCollectible;
    vMethTableCollectible.Request(g_sos, TO_CDADDR(dwStartAddr));

    table.WriteRow("EEClass:", EEClassPtr(vMethTable.Class));

    table.WriteRow("Module:", ModulePtr(vMethTable.Module));

    sos::MethodTable mt = (TADDR)dwStartAddr;
    table.WriteRow("Name:", mt.GetName());

    WCHAR fileName[MAX_LONGPATH];
    FileNameForModule(TO_TADDR(vMethTable.Module), fileName);
    table.WriteRow("mdToken:", Pointer(vMethTable.cl));
    table.WriteRow("File:", fileName[0] ? fileName : W("Unknown Module"));

    if (vMethTableCollectible.LoaderAllocatorObjectHandle != NULL)
    {
        TADDR loaderAllocator;
        if (SUCCEEDED(MOVE(loaderAllocator, vMethTableCollectible.LoaderAllocatorObjectHandle)))
        {
            table.WriteRow("LoaderAllocator:", ObjectPtr(loaderAllocator));
        }
    }

    table.WriteRow("BaseSize:", PrefixHex(vMethTable.BaseSize));
    table.WriteRow("ComponentSize:", PrefixHex(vMethTable.ComponentSize));
    table.WriteRow("Slots in VTable:", Decimal(vMethTable.wNumMethods));
    
    table.SetColWidth(0, 29);
    table.WriteRow("Number of IFaces in IFaceMap:", Decimal(vMethTable.wNumInterfaces));

    if (bDumpMDTable)
    {
        table.ReInit(4, POINTERSIZE_HEX, AlignRight);
        table.SetColAlignment(3, AlignLeft);
        table.SetColWidth(2, 6);

        Print("--------------------------------------\n");
        Print("MethodDesc Table\n");

        table.WriteRow("Entry", "MethodDesc", "JIT", "Name");

        for (DWORD n = 0; n < vMethTable.wNumMethods; n++)
        {
            JITTypes jitType;
            DWORD_PTR methodDesc=0;
            DWORD_PTR gcinfoAddr;

            CLRDATA_ADDRESS entry;
            if (g_sos->GetMethodTableSlot(dwStartAddr, n, &entry) != S_OK)
            {
                PrintLn("<error getting slot ", Decimal(n), ">");
                continue;
            }

            IP2MethodDesc((DWORD_PTR)entry, methodDesc, jitType, gcinfoAddr);
            table.WriteColumn(0, entry);
            table.WriteColumn(1, MethodDescPtr(methodDesc));

            if (jitType == TYPE_UNKNOWN && methodDesc != NULL)
            {
                // We can get a more accurate jitType from NativeCodeAddr of the methoddesc,
                // because the methodtable entry hasn't always been patched.
                DacpMethodDescData tmpMethodDescData;
                if (tmpMethodDescData.Request(g_sos, TO_CDADDR(methodDesc)) == S_OK)
                {
                    DacpCodeHeaderData codeHeaderData;                        
                    if (codeHeaderData.Request(g_sos,tmpMethodDescData.NativeCodeAddr) == S_OK)
                    {        
                        jitType = (JITTypes) codeHeaderData.JITType;
                    }
                }
            }

            const char *pszJitType = "NONE";
            if (jitType == TYPE_JIT)
                pszJitType = "JIT";
            else if (jitType == TYPE_PJIT)
                pszJitType = "PreJIT";
            else
            {
                DacpMethodDescData MethodDescData;
                if (MethodDescData.Request(g_sos, TO_CDADDR(methodDesc)) == S_OK)
                {
                    // Is it an fcall?
                    if ((TO_TADDR(MethodDescData.NativeCodeAddr) >=  TO_TADDR(moduleInfo[MSCORWKS].baseAddr)) &&
                        ((TO_TADDR(MethodDescData.NativeCodeAddr) <  TO_TADDR(moduleInfo[MSCORWKS].baseAddr + moduleInfo[MSCORWKS].size))))
                    {
                        pszJitType = "FCALL";
                    }
                }
            }

            table.WriteColumn(2, pszJitType);
            
            NameForMD_s(methodDesc,g_mdName,mdNameLen);                        
            table.WriteColumn(3, g_mdName);
        }
    }
    return Status;    
}

extern size_t Align (size_t nbytes);

HRESULT PrintVC(TADDR taMT, TADDR taObject, BOOL bPrintFields = TRUE)
{       
    HRESULT Status;
    DacpMethodTableData mtabledata;
    if ((Status = mtabledata.Request(g_sos, TO_CDADDR(taMT)))!=S_OK)
        return Status;
    
    size_t size = mtabledata.BaseSize;
    if ((Status=g_sos->GetMethodTableName(TO_CDADDR(taMT), mdNameLen, g_mdName, NULL))!=S_OK)
        return Status;

    ExtOut("Name:        %S\n", g_mdName);
    DMLOut("MethodTable: %s\n", DMLMethodTable(taMT));
    DMLOut("EEClass:     %s\n", DMLClass(mtabledata.Class));
    ExtOut("Size:        %d(0x%x) bytes\n", size, size);

    FileNameForModule(TO_TADDR(mtabledata.Module), g_mdName);
    ExtOut("File:        %S\n", g_mdName[0] ? g_mdName : W("Unknown Module"));

    if (bPrintFields)
    {
        DacpMethodTableFieldData vMethodTableFields;
        if ((Status = vMethodTableFields.Request(g_sos,TO_CDADDR(taMT)))!=S_OK)
            return Status;

        ExtOut("Fields:\n");

        if (vMethodTableFields.wNumInstanceFields + vMethodTableFields.wNumStaticFields > 0)
            DisplayFields(TO_CDADDR(taMT), &mtabledata, &vMethodTableFields, taObject, TRUE, TRUE);
    }

    return S_OK;
}

void PrintRuntimeTypeInfo(TADDR p_rtObject, const DacpObjectData & rtObjectData)
{
    // Get the method table
    int iOffset = GetObjFieldOffset(TO_CDADDR(p_rtObject), rtObjectData.MethodTable, W("m_handle"));
    if (iOffset > 0)
    {            
        TADDR mtPtr;
        if (SUCCEEDED(GetMTOfObject(p_rtObject + iOffset, &mtPtr)))
        {
            sos::MethodTable mt = mtPtr;
            ExtOut("Type Name:   %S\n", mt.GetName());
            DMLOut("Type MT:     %s\n", DMLMethodTable(mtPtr));
        }                        
    }        
}

HRESULT PrintObj(TADDR taObj, BOOL bPrintFields = TRUE)
{
    if (!sos::IsObject(taObj, true))
    {
        ExtOut("<Note: this object has an invalid CLASS field>\n");
    }

    DacpObjectData objData;
    HRESULT Status;
    if ((Status=objData.Request(g_sos, TO_CDADDR(taObj))) != S_OK)
    {        
        ExtOut("Invalid object\n");
        return Status;
    }

    if (objData.ObjectType==OBJ_FREE)
    {
        ExtOut("Free Object\n");
        DWORD_PTR size = (DWORD_PTR)objData.Size;
        ExtOut("Size:        %" POINTERSIZE_TYPE "d(0x%" POINTERSIZE_TYPE "x) bytes\n", size, size);
        return S_OK;
    }
    
    sos::Object obj = taObj;
    ExtOut("Name:        %S\n", obj.GetTypeName());
    DMLOut("MethodTable: %s\n", DMLMethodTable(objData.MethodTable));

    
    DacpMethodTableData mtabledata;
    if ((Status=mtabledata.Request(g_sos,objData.MethodTable)) == S_OK)
    {
        DMLOut("EEClass:     %s\n", DMLClass(mtabledata.Class));
    }
    else        
    {
        ExtOut("Invalid EEClass address\n");
        return Status;
    }

    if (objData.RCW != NULL)
    {
        DMLOut("RCW:         %s\n", DMLRCWrapper(objData.RCW));
    }
    if (objData.CCW != NULL)
    {
        DMLOut("CCW:         %s\n", DMLCCWrapper(objData.CCW));
    }

    DWORD_PTR size = (DWORD_PTR)objData.Size;
    ExtOut("Size:        %" POINTERSIZE_TYPE "d(0x%" POINTERSIZE_TYPE "x) bytes\n", size, size);

    if (_wcscmp(obj.GetTypeName(), W("System.RuntimeType")) == 0)
    {
        PrintRuntimeTypeInfo(taObj, objData);
    }

    if (_wcscmp(obj.GetTypeName(), W("System.RuntimeType+RuntimeTypeCache")) == 0)
    {
        // Get the method table
        int iOffset = GetObjFieldOffset (TO_CDADDR(taObj), objData.MethodTable, W("m_runtimeType"));
        if (iOffset > 0)
        {            
            TADDR rtPtr;
            if (MOVE(rtPtr, taObj + iOffset) == S_OK)
            {
                DacpObjectData rtObjectData;
                if ((Status=rtObjectData.Request(g_sos, TO_CDADDR(rtPtr))) != S_OK)
                {        
                    ExtOut("Error when reading RuntimeType field\n");
                    return Status;
                }

                PrintRuntimeTypeInfo(rtPtr, rtObjectData);
            }                        
        }        
    }

    if (objData.ObjectType==OBJ_ARRAY)
    {
        ExtOut("Array:       Rank %d, Number of elements %" POINTERSIZE_TYPE "d, Type %s",
                objData.dwRank, (DWORD_PTR)objData.dwNumComponents, ElementTypeName(objData.ElementType));

        IfDMLOut(" (<exec cmd=\"!DumpArray /d %p\">Print Array</exec>)", SOS_PTR(taObj));
        ExtOut("\n");
        
        if (objData.ElementType == ELEMENT_TYPE_I1 ||
            objData.ElementType == ELEMENT_TYPE_U1 ||
            objData.ElementType == ELEMENT_TYPE_CHAR)
        {
            bool wide = objData.ElementType == ELEMENT_TYPE_CHAR;

            // Get the size of the character array, but clamp it to a reasonable length.
            TADDR pos = taObj + (2 * sizeof(DWORD_PTR));
            DWORD_PTR num;
            moveN(num, taObj + sizeof(DWORD_PTR));

            if (IsDMLEnabled())
                DMLOut("<exec cmd=\"%s %x L%x\">Content</exec>:     ", (wide) ? "dw" : "db", pos, num);
            else
                ExtOut("Content:     ");
            CharArrayContent(pos, (ULONG)(num <= 128 ? num : 128), wide);
            ExtOut("\n");
        }
    }
    else
    {
        FileNameForModule(TO_TADDR(mtabledata.Module), g_mdName);
        ExtOut("File:        %S\n", g_mdName[0] ? g_mdName : W("Unknown Module"));
    }

    if (objData.ObjectType == OBJ_STRING)
    {
        ExtOut("String:      ");
        StringObjectContent(taObj);
        ExtOut("\n");
    }
    else if (objData.ObjectType == OBJ_OBJECT)
    {
        ExtOut("Object\n");
    }    

    if (bPrintFields)
    {
        DacpMethodTableFieldData vMethodTableFields;
        if ((Status = vMethodTableFields.Request(g_sos,TO_CDADDR(objData.MethodTable)))!=S_OK)
            return Status;

        ExtOut("Fields:\n");
        if (vMethodTableFields.wNumInstanceFields + vMethodTableFields.wNumStaticFields > 0)
        {
            DisplayFields(objData.MethodTable, &mtabledata, &vMethodTableFields, taObj, TRUE, FALSE);
        }
        else
        {
            ExtOut("None\n");
        }
    }

    sos::ThinLockInfo lockInfo;
    if (obj.GetThinLock(lockInfo))
    {
        ExtOut("ThinLock owner %x (%p), Recursive %x\n", lockInfo.ThreadId, 
            SOS_PTR(lockInfo.ThreadPtr), lockInfo.Recursion);
    }
    
    return S_OK;
}

BOOL IndicesInRange (DWORD * indices, DWORD * lowerBounds, DWORD * bounds, DWORD rank)
{
    int i = 0;
    if (!ClrSafeInt<int>::subtraction((int)rank, 1, i))
    {
        ExtOut("<integer underflow>\n");
        return FALSE;
    }

    for (; i >= 0; i--)
    {
        if (indices[i] >= bounds[i] + lowerBounds[i])
        {
            if (i == 0)
            {
                return FALSE;
            }
            
            indices[i] = lowerBounds[i];
            indices[i - 1]++;
        }
    }

    return TRUE;
}

void ExtOutIndices (DWORD * indices, DWORD rank)
{
    for (DWORD i = 0; i < rank; i++)
    {
        ExtOut("[%d]", indices[i]);
    }
}

size_t OffsetFromIndices (DWORD * indices, DWORD * lowerBounds, DWORD * bounds, DWORD rank)
{
    _ASSERTE(rank >= 0);
    size_t multiplier = 1;
    size_t offset = 0;
    int i = 0;
    if (!ClrSafeInt<int>::subtraction((int)rank, 1, i))
    {
        ExtOut("<integer underflow>\n");
        return 0;
    }

    for (; i >= 0; i--) 
    {
        DWORD curIndex = indices[i] - lowerBounds[i];
        offset += curIndex * multiplier;
        multiplier *= bounds[i];
    }

    return offset;
}
HRESULT PrintArray(DacpObjectData& objData, DumpArrayFlags& flags, BOOL isPermSetPrint);
#ifdef _DEBUG
HRESULT PrintPermissionSet (TADDR p_PermSet)
{
    HRESULT Status = S_OK;

    DacpObjectData PermSetData;
    if ((Status=PermSetData.Request(g_sos, TO_CDADDR(p_PermSet))) != S_OK)
    {        
        ExtOut("Invalid object\n");
        return Status;
    }

    
    sos::MethodTable mt = TO_TADDR(PermSetData.MethodTable);
    if (_wcscmp (W("System.Security.PermissionSet"), mt.GetName()) != 0 && _wcscmp(W("System.Security.NamedPermissionSet"), mt.GetName()) != 0)
    {
        ExtOut("Invalid PermissionSet object\n");
        return S_FALSE;
    }

    ExtOut("PermissionSet object: %p\n", SOS_PTR(p_PermSet));
    
    // Print basic info

    // Walk the fields, printing some fields in a special way.

    int iOffset = GetObjFieldOffset (TO_CDADDR(p_PermSet), PermSetData.MethodTable, W("m_Unrestricted"));
    
    if (iOffset > 0)        
    {
        BYTE unrestricted;
        MOVE(unrestricted, p_PermSet + iOffset);
        if (unrestricted)
            ExtOut("Unrestricted: TRUE\n");
        else
            ExtOut("Unrestricted: FALSE\n");
    }

    iOffset = GetObjFieldOffset (TO_CDADDR(p_PermSet), PermSetData.MethodTable, W("m_permSet"));
    if (iOffset > 0)
    {
        TADDR tbSetPtr;
        MOVE(tbSetPtr, p_PermSet + iOffset);
        if (tbSetPtr != NULL)
        {
            DacpObjectData tbSetData;
            if ((Status=tbSetData.Request(g_sos, TO_CDADDR(tbSetPtr))) != S_OK)
            {        
                ExtOut("Invalid object\n");
                return Status;
            }

            iOffset = GetObjFieldOffset (TO_CDADDR(tbSetPtr), tbSetData.MethodTable, W("m_Set"));
            if (iOffset > 0)
            {
                DWORD_PTR PermsArrayPtr;
                MOVE(PermsArrayPtr, tbSetPtr + iOffset);
                if (PermsArrayPtr != NULL)
                {
                    // Print all the permissions in the array
                    DacpObjectData objData;
                    if ((Status=objData.Request(g_sos, TO_CDADDR(PermsArrayPtr))) != S_OK)
                    {        
                        ExtOut("Invalid object\n");
                        return Status;
                    }
                    DumpArrayFlags flags;
                    flags.bDetail = TRUE;
                    return PrintArray(objData, flags, TRUE);
                }
            }

            iOffset = GetObjFieldOffset (TO_CDADDR(tbSetPtr), tbSetData.MethodTable, W("m_Obj"));
            if (iOffset > 0)
            {
                DWORD_PTR PermObjPtr;
                MOVE(PermObjPtr, tbSetPtr + iOffset);
                if (PermObjPtr != NULL)
                {
                    // Print the permission object
                    return PrintObj(PermObjPtr);
                }
            }
            

        }
    }
    return Status;
}

#endif // _DEBUG

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of an object from a  *  
*    given address
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpArray)
{
    INIT_API();

    DumpArrayFlags flags;
    
    MINIDUMP_NOT_SUPPORTED();

    BOOL dml = FALSE;
    
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-start", &flags.startIndex, COSIZE_T, TRUE},
        {"-length", &flags.Length, COSIZE_T, TRUE},
        {"-details", &flags.bDetail, COBOOL, FALSE},
        {"-nofields", &flags.bNoFieldsForElement, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&flags.strObject, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    DWORD_PTR p_Object = GetExpression (flags.strObject);
    if (p_Object == 0)
    {
        ExtOut("Invalid parameter %s\n", flags.strObject);
        return Status;
    }

    if (!sos::IsObject(p_Object, true))
    {
        ExtOut("<Note: this object has an invalid CLASS field>\n");
    }
    
    DacpObjectData objData;
    if ((Status=objData.Request(g_sos, TO_CDADDR(p_Object))) != S_OK)
    {  
        ExtOut("Invalid object\n");
        return Status;
    }

    if (objData.ObjectType != OBJ_ARRAY)
    {
        ExtOut("Not an array, please use !DumpObj instead\n");
        return S_OK;
    }
    return PrintArray(objData, flags, FALSE);
}


HRESULT PrintArray(DacpObjectData& objData, DumpArrayFlags& flags, BOOL isPermSetPrint)
{
    HRESULT Status = S_OK;

    if (objData.dwRank != 1 && (flags.Length != (DWORD_PTR)-1 ||flags.startIndex != 0))
    {
        ExtOut("For multi-dimension array, length and start index are supported\n");
        return S_OK;
    }

    if (flags.startIndex > objData.dwNumComponents)
    {
        ExtOut("Start index out of range\n");
        return S_OK;
    }

    if (!flags.bDetail && flags.bNoFieldsForElement)
    {
        ExtOut("-nofields has no effect unless -details is specified\n");
    }
    
    DWORD i;
    if (!isPermSetPrint)
    {
        // TODO: don't depend on this being a MethodTable
        NameForMT_s(TO_TADDR(objData.ElementTypeHandle), g_mdName, mdNameLen);

        ExtOut("Name:        %S[", g_mdName);
        for (i = 1; i < objData.dwRank; i++)
            ExtOut(",");
        ExtOut("]\n");
        
        DMLOut("MethodTable: %s\n", DMLMethodTable(objData.MethodTable));

        {
            DacpMethodTableData mtdata;
            if (SUCCEEDED(mtdata.Request(g_sos, objData.MethodTable)))
            {
                DMLOut("EEClass:     %s\n", DMLClass(mtdata.Class));
            }            
        }

        DWORD_PTR size = (DWORD_PTR)objData.Size;
        ExtOut("Size:        %" POINTERSIZE_TYPE "d(0x%" POINTERSIZE_TYPE "x) bytes\n", size, size);

        ExtOut("Array:       Rank %d, Number of elements %" POINTERSIZE_TYPE "d, Type %s\n", 
                objData.dwRank, (DWORD_PTR)objData.dwNumComponents, ElementTypeName(objData.ElementType));
        DMLOut("Element Methodtable: %s\n", DMLMethodTable(objData.ElementTypeHandle));
    }

    BOOL isElementValueType = IsElementValueType(objData.ElementType);

    DWORD dwRankAllocSize;
    if (!ClrSafeInt<DWORD>::multiply(sizeof(DWORD), objData.dwRank, dwRankAllocSize))
    {
        ExtOut("Integer overflow on array rank\n");
        return Status;
    }

    DWORD *lowerBounds = (DWORD *)alloca(dwRankAllocSize);
    if (!SafeReadMemory(objData.ArrayLowerBoundsPtr, lowerBounds, dwRankAllocSize, NULL))
    {
        ExtOut("Failed to read lower bounds info from the array\n");        
        return S_OK;
    }

    DWORD *bounds = (DWORD *)alloca(dwRankAllocSize);
    if (!SafeReadMemory (objData.ArrayBoundsPtr, bounds, dwRankAllocSize, NULL))
    {
        ExtOut("Failed to read bounds info from the array\n");        
        return S_OK;
    }

    //length is only supported for single-dimension array
    if (objData.dwRank == 1 && flags.Length != (DWORD_PTR)-1)
    {
        bounds[0] = _min(bounds[0], (DWORD)(flags.Length + flags.startIndex) - lowerBounds[0]);
    }
    
    DWORD *indices = (DWORD *)alloca(dwRankAllocSize);
    for (i = 0; i < objData.dwRank; i++)
    {
        indices[i] = lowerBounds[i];
    }

    //start index is only supported for single-dimension array
    if (objData.dwRank == 1)
    {
        indices[0] = (DWORD)flags.startIndex;
    }
    
    //Offset should be calculated by OffsetFromIndices. However because of the way 
    //how we grow indices, incrementing offset by one happens to match indices in every iteration    
    for (size_t offset = OffsetFromIndices (indices, lowerBounds, bounds, objData.dwRank);
        IndicesInRange (indices, lowerBounds, bounds, objData.dwRank); 
        indices[objData.dwRank - 1]++, offset++)
    {      
        if (IsInterrupt())
        {
            ExtOut("interrupted by user\n");
            break;
        }

        TADDR elementAddress = TO_TADDR(objData.ArrayDataPtr + offset * objData.dwComponentSize);
        TADDR p_Element = NULL;
        if (isElementValueType)
        {
            p_Element = elementAddress;        
        }
        else if (!SafeReadMemory (elementAddress, &p_Element, sizeof (p_Element), NULL))
        {
            ExtOut("Failed to read element at ");        
            ExtOutIndices(indices, objData.dwRank);
            ExtOut("\n");
            continue;
        }

        if (p_Element)
        {
            ExtOutIndices(indices, objData.dwRank);

            if (isElementValueType)
            {
                DMLOut( " %s\n", DMLValueClass(objData.ElementTypeHandle, p_Element));
            }
            else
            {
                DMLOut(" %s\n", DMLObject(p_Element));
            }
        }
        else if (!isPermSetPrint)
        {
            ExtOutIndices(indices, objData.dwRank);
            ExtOut(" null\n");
        }

        if (flags.bDetail)
        {
            IncrementIndent();
            if (isElementValueType)
            {
                PrintVC(TO_TADDR(objData.ElementTypeHandle), elementAddress, !flags.bNoFieldsForElement);
            }
            else if (p_Element != NULL)
            {
                PrintObj(p_Element, !flags.bNoFieldsForElement);
            }
            DecrementIndent();
        }
    }
    
    return S_OK;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of an object from a  *  
*    given address
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpObj)    
{
    INIT_API();

    MINIDUMP_NOT_SUPPORTED();    

    BOOL dml = FALSE;
    BOOL bNoFields = FALSE;
    BOOL bRefs = FALSE;
    StringHolder str_Object;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-nofields", &bNoFields, COBOOL, FALSE},
        {"-refs", &bRefs, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&str_Object.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    
    DWORD_PTR p_Object = GetExpression(str_Object.data);
    EnableDMLHolder dmlHolder(dml);
    if (p_Object == 0)
    {
        ExtOut("Invalid parameter %s\n", args);
        return Status;
    }

    Status = PrintObj(p_Object, !bNoFields);
    
    if (SUCCEEDED(Status) && bRefs)
    {
        ExtOut("GC Refs:\n");
        TableOutput out(2, POINTERSIZE_HEX, AlignRight, 4);
        out.WriteRow("offset", "object");
        for (sos::RefIterator itr(TO_TADDR(p_Object)); itr; ++itr)
            out.WriteRow(Hex(itr.GetOffset()), ObjectPtr(*itr));
    }
    
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a delegate from a *
*    given address.                                                    *
*                                                                      *
\**********************************************************************/

DECLARE_API(DumpDelegate)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    try
    {
        BOOL dml = FALSE;
        DWORD_PTR dwAddr = 0;

        CMDOption option[] =
        {   // name, vptr, type, hasValue
            {"/d", &dml, COBOOL, FALSE}
        };
        CMDValue arg[] =
        {   // vptr, type
            {&dwAddr, COHEX}
        };
        size_t nArg;
        if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
        {
            return Status;
        }
        if (nArg != 1)
        {
            ExtOut("Usage: !DumpDelegate <delegate object address>\n");
            return Status;
        }

        EnableDMLHolder dmlHolder(dml);
        CLRDATA_ADDRESS delegateAddr = TO_CDADDR(dwAddr);

        if (!sos::IsObject(delegateAddr))
        {
            ExtOut("Invalid object.\n");
        }
        else
        {
            sos::Object delegateObj = TO_TADDR(delegateAddr);
            if (!IsDerivedFrom(TO_CDADDR(delegateObj.GetMT()), W("System.Delegate")))
            {
                ExtOut("Object of type '%S' is not a delegate.", delegateObj.GetTypeName());
            }
            else
            {
                ExtOut("Target           Method           Name\n");

                std::vector<CLRDATA_ADDRESS> delegatesRemaining;
                delegatesRemaining.push_back(delegateAddr);
                while (delegatesRemaining.size() > 0)
                {
                    delegateAddr = delegatesRemaining.back();
                    delegatesRemaining.pop_back();
                    delegateObj = TO_TADDR(delegateAddr);

                    int offset;
                    if ((offset = GetObjFieldOffset(delegateObj.GetAddress(), delegateObj.GetMT(), W("_target"))) != 0)
                    {
                        CLRDATA_ADDRESS target;
                        MOVE(target, delegateObj.GetAddress() + offset);

                        if ((offset = GetObjFieldOffset(delegateObj.GetAddress(), delegateObj.GetMT(), W("_invocationList"))) != 0)
                        {
                            CLRDATA_ADDRESS invocationList;
                            MOVE(invocationList, delegateObj.GetAddress() + offset);

                            if ((offset = GetObjFieldOffset(delegateObj.GetAddress(), delegateObj.GetMT(), W("_invocationCount"))) != 0)
                            {
                                int invocationCount;
                                MOVE(invocationCount, delegateObj.GetAddress() + offset);

                                if (invocationList == NULL)
                                {
                                    CLRDATA_ADDRESS md;
                                    DMLOut("%s ", DMLObject(target));
                                    if (TryGetMethodDescriptorForDelegate(delegateAddr, &md))
                                    {
                                        DMLOut("%s ", DMLMethodDesc(md));
                                        NameForMD_s((DWORD_PTR)md, g_mdName, mdNameLen);
                                        ExtOut("%S\n", g_mdName);
                                    }
                                    else
                                    {
                                        ExtOut("(unknown)\n");
                                    }
                                }
                                else if (sos::IsObject(invocationList, false))
                                {
                                    DacpObjectData objData;
                                    if (objData.Request(g_sos, invocationList) == S_OK &&
                                        objData.ObjectType == OBJ_ARRAY &&
                                        invocationCount <= (int)objData.dwNumComponents)
                                    {
                                        for (int i = 0; i < invocationCount; i++)
                                        {
                                            CLRDATA_ADDRESS elementPtr;
                                            MOVE(elementPtr, TO_CDADDR(objData.ArrayDataPtr + (i * objData.dwComponentSize)));
                                            if (elementPtr != NULL && sos::IsObject(elementPtr, false))
                                            {
                                                delegatesRemaining.push_back(elementPtr);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return S_OK;
    }
    catch (const sos::Exception &e)
    {
        ExtOut("%s\n", e.what());
        return E_FAIL;
    }
}

CLRDATA_ADDRESS isExceptionObj(CLRDATA_ADDRESS mtObj)
{
    // We want to follow back until we get the mt for System.Exception
    DacpMethodTableData dmtd;
    CLRDATA_ADDRESS walkMT = mtObj;
    while(walkMT != NULL)
    {
        if (dmtd.Request(g_sos, walkMT) != S_OK)
        {
            break;            
        }
        if (walkMT == g_special_usefulGlobals.ExceptionMethodTable)
        {
            return walkMT;
        }
        walkMT = dmtd.ParentMethodTable;
    }
    return NULL;
}

CLRDATA_ADDRESS isSecurityExceptionObj(CLRDATA_ADDRESS mtObj)
{
    // We want to follow back until we get the mt for System.Exception
    DacpMethodTableData dmtd;
    CLRDATA_ADDRESS walkMT = mtObj;
    while(walkMT != NULL)
    {
        if (dmtd.Request(g_sos, walkMT) != S_OK)
        {
            break;            
        }
        NameForMT_s(TO_TADDR(walkMT), g_mdName, mdNameLen);                
        if (_wcscmp(W("System.Security.SecurityException"), g_mdName) == 0)
        {
            return walkMT;
        }
        walkMT = dmtd.ParentMethodTable;
    }
    return NULL;
}

// Fill the passed in buffer with a text header for generated exception information.
// Returns the number of characters in the wszBuffer array on exit.
// If NULL is passed for wszBuffer, just returns the number of characters needed.
size_t AddExceptionHeader (__out_ecount_opt(bufferLength) WCHAR *wszBuffer, size_t bufferLength)
{
#ifdef _TARGET_WIN64_
    const WCHAR *wszHeader = W("    SP               IP               Function\n");
#else
    const WCHAR *wszHeader = W("    SP       IP       Function\n");
#endif // _TARGET_WIN64_
    if (wszBuffer)
    {
        swprintf_s(wszBuffer, bufferLength, wszHeader);
    }
    return _wcslen(wszHeader);
}

struct StackTraceElement 
{
    UINT_PTR        ip;
    UINT_PTR        sp;
    DWORD_PTR       pFunc;  // MethodDesc
    // TRUE if this element represents the last frame of the foreign
    // exception stack trace.
    BOOL            fIsLastFrameFromForeignStackTrace;

};

#include "sos_stacktrace.h"

class StringOutput
{
public:
    CQuickString cs;
    StringOutput()
    {
        cs.Alloc(1024);
        cs.String()[0] = L'\0';
    }

    BOOL Append(__in_z LPCWSTR pszStr)
    {
        size_t iInputLen = _wcslen (pszStr);        
        size_t iCurLen = _wcslen (cs.String());
        if ((iCurLen + iInputLen + 1) > cs.Size())
        {
            if (cs.ReSize(iCurLen + iInputLen + 1) != S_OK)
            {
                return FALSE;
            }
        }

        wcsncat_s (cs.String(), cs.Size(), pszStr, _TRUNCATE);
        return TRUE;
    }
    
    size_t Length()
    {
        return _wcslen(cs.String());
    }

    WCHAR *String()
    {
        return cs.String();
    }
};

static HRESULT DumpMDInfoBuffer(DWORD_PTR dwStartAddr, DWORD Flags, ULONG64 Esp, ULONG64 IPAddr, StringOutput& so);

// Using heuristics to determine if an exception object represented an async (hardware) or a 
// managed exception
// We need to use these heuristics when the System.Exception object is not the active exception
// on some thread, but it's something found somewhere on the managed heap.

// uses the MapWin32FaultToCOMPlusException to figure out how we map async exceptions
// to managed exceptions and their HRESULTs
static const HRESULT AsyncHResultValues[] =
{
    COR_E_ARITHMETIC,    // kArithmeticException
    COR_E_OVERFLOW,      // kOverflowException
    COR_E_DIVIDEBYZERO,  // kDivideByZeroException
    COR_E_FORMAT,        // kFormatException
    COR_E_NULLREFERENCE, // kNullReferenceException
    E_POINTER,           // kAccessViolationException
    // the EE is raising the next exceptions more often than the OS will raise an async 
    // exception for these conditions, so in general treat these as Synchronous
      // COR_E_INDEXOUTOFRANGE, // kIndexOutOfRangeException
      // COR_E_OUTOFMEMORY,   // kOutOfMemoryException
      // COR_E_STACKOVERFLOW, // kStackOverflowException
    COR_E_DATAMISALIGNED, // kDataMisalignedException
    
};
BOOL IsAsyncException(CLRDATA_ADDRESS taObj, CLRDATA_ADDRESS mtObj)
{
    // by default we'll treat exceptions as synchronous
    UINT32 xcode = EXCEPTION_COMPLUS;
    int iOffset = GetObjFieldOffset (taObj, mtObj, W("_xcode"));
    if (iOffset > 0)
    {
        HRESULT hr = MOVE(xcode, taObj + iOffset);
        if (hr != S_OK)
        {
            xcode = EXCEPTION_COMPLUS;
            goto Done;
        }
    }

    if (xcode == EXCEPTION_COMPLUS)
    {
        HRESULT ehr = 0;
        iOffset = GetObjFieldOffset (taObj, mtObj, W("_HResult"));
        if (iOffset > 0)
        {
            HRESULT hr = MOVE(ehr, taObj + iOffset);
            if (hr != S_OK)
            {
                xcode = EXCEPTION_COMPLUS;
                goto Done;
            }
            for (size_t idx = 0; idx < _countof(AsyncHResultValues); ++idx)
            {
                if (ehr == AsyncHResultValues[idx])
                {
                    xcode = ehr;
                    break;
                }
            }
        }
    }
Done:
    return xcode != EXCEPTION_COMPLUS;
}

// Overload that mirrors the code above when the ExceptionObjectData was already retrieved from LS
BOOL IsAsyncException(const DacpExceptionObjectData & excData)
{
    if ((DWORD)excData.XCode != EXCEPTION_COMPLUS)
        return TRUE;

    HRESULT ehr = excData.HResult;
    for (size_t idx = 0; idx < _countof(AsyncHResultValues); ++idx)
    {
        if (ehr == AsyncHResultValues[idx])
        {
            return TRUE;
        }
    }

    return FALSE;
}


#define SOS_STACKTRACE_SHOWEXPLICITFRAMES  0x00000002
size_t FormatGeneratedException (DWORD_PTR dataPtr, 
    UINT bytes, 
    __out_ecount_opt(bufferLength) WCHAR *wszBuffer, 
    size_t bufferLength, 
    BOOL bAsync,
    BOOL bNestedCase = FALSE,
    BOOL bLineNumbers = FALSE)
{
    UINT count = bytes / sizeof(StackTraceElement);
    size_t Length = 0;

    if (wszBuffer && bufferLength > 0)
    {
        wszBuffer[0] = L'\0';
    }
    
    // Buffer is calculated for sprintf below ("   %p %p %S\n");
    WCHAR wszLineBuffer[mdNameLen + 8 + sizeof(size_t)*2 + MAX_LONGPATH + 8];

    if (count == 0)
    {
        return 0;
    }
    
    if (bNestedCase)
    {
        // If we are computing the call stack for a nested exception, we
        // don't want to print the last frame, because the outer exception
        // will have that frame.
        count--;
    }
    
    for (UINT i = 0; i < count; i++)
    {
        StackTraceElement ste;
        MOVE (ste, dataPtr + i*sizeof(StackTraceElement));

        // ste.ip must be adjusted because of an ancient workaround in the exception 
        // infrastructure. The workaround is that the exception needs to have
        // an ip address that will map to the line number where the exception was thrown.
        // (It doesn't matter that it's not a valid instruction). (see /vm/excep.cpp)
        //
        // This "counterhack" is not 100% accurate
        // The biggest issue is that !PrintException must work with exception objects 
        // that may not be currently active; as a consequence we cannot rely on the 
        // state of some "current thread" to infer whether the IP values stored in 
        // the exception object have been adjusted or not. If we could, we may examine 
        // the topmost "Frame" and make the decision based on whether it's a 
        // FaultingExceptionFrame or not.
        // 1. On IA64 the IP values are never adjusted by the EE so there's nothing 
        //    to adjust back.
        // 2. On AMD64:
        //    (a) if the exception was an async (hardware) exception add 1 to all 
        //        IP values in the exception object
        //    (b) if the exception was a managed exception (either raised by the 
        //        EE or thrown by managed code) do not adjust any IP values
        // 3. On X86:
        //    (a) if the exception was an async (hardware) exception add 1 to 
        //        all but the topmost IP value in the exception object
        //    (b) if the exception was a managed exception (either raised by 
        //        the EE or thrown by managed code) add 1 to all IP values in 
        //        the exception object
#if defined(_TARGET_AMD64_)
        if (bAsync)
        {
            ste.ip += 1;
        }
#elif defined(_TARGET_X86_)
        if (IsDbgTargetX86() && (!bAsync || i != 0))
        {
            ste.ip += 1;
        }
#endif // defined(_TARGET_AMD64_) || defined(_TARGET__X86_)

        StringOutput so;
        HRESULT Status = DumpMDInfoBuffer(ste.pFunc, SOS_STACKTRACE_SHOWADDRESSES|SOS_STACKTRACE_SHOWEXPLICITFRAMES, ste.sp, ste.ip, so);

        // If DumpMDInfoBuffer failed (due to out of memory or missing metadata), 
        // or did not update so (when ste is an explicit frames), do not update wszBuffer
        if (Status == S_OK)
        {
            WCHAR filename[MAX_LONGPATH] = W("");
            ULONG linenum = 0;
            if (bLineNumbers && 
                SUCCEEDED(GetLineByOffset(TO_CDADDR(ste.ip), &linenum, filename, _countof(filename))))
            {
                swprintf_s(wszLineBuffer, _countof(wszLineBuffer), W("    %s [%s @ %d]\n"), so.String(), filename, linenum);
            }
            else
            {
                swprintf_s(wszLineBuffer, _countof(wszLineBuffer), W("    %s\n"), so.String());
            }

            Length += _wcslen(wszLineBuffer);

            if (wszBuffer)
            {
                wcsncat_s(wszBuffer, bufferLength, wszLineBuffer, _TRUNCATE);
            }
        }
    }

    return Length;
}

// ExtOut has an internal limit for the string size
void SosExtOutLargeString(__inout_z __inout_ecount_opt(len) WCHAR * pwszLargeString, size_t len)
{
    const size_t chunkLen = 2048;

    WCHAR *pwsz = pwszLargeString;  // beginning of a chunk
    size_t count = len/chunkLen;
    // write full chunks
    for (size_t idx = 0; idx < count; ++idx)
    {
        WCHAR *pch = pwsz + chunkLen; // after the chunk
        // zero terminate the chunk
        WCHAR ch = *pch;
        *pch = L'\0';

        ExtOut("%S", pwsz);

        // restore whacked char
        *pch = ch;

        // advance to next chunk
        pwsz += chunkLen;
    }

    // last chunk
    ExtOut("%S", pwsz);
}

HRESULT FormatException(CLRDATA_ADDRESS taObj, BOOL bLineNumbers = FALSE)
{
    HRESULT Status = S_OK;

    DacpObjectData objData;
    if ((Status=objData.Request(g_sos, taObj)) != S_OK)
    {        
        ExtOut("Invalid object\n");
        return Status;
    }

    // Make sure it is an exception object, and get the MT of Exception
    CLRDATA_ADDRESS exceptionMT = isExceptionObj(objData.MethodTable);
    if (exceptionMT == NULL)
    {
        ExtOut("Not a valid exception object\n");
        return Status;
    }

    DMLOut("Exception object: %s\n", DMLObject(taObj));
    
    if (NameForMT_s(TO_TADDR(objData.MethodTable), g_mdName, mdNameLen))
    {
        ExtOut("Exception type:   %S\n", g_mdName);
    }
    else
    {
        ExtOut("Exception type:   <Unknown>\n");
    }

    // Print basic info

    // First try to get exception object data using ISOSDacInterface2
    DacpExceptionObjectData excData;
    BOOL bGotExcData = SUCCEEDED(excData.Request(g_sos, taObj));

    // Walk the fields, printing some fields in a special way.
    // HR, InnerException, Message, StackTrace, StackTraceString

    {
        TADDR taMsg = 0;
        if (bGotExcData)
        {
            taMsg = TO_TADDR(excData.Message);
        }
        else
        {
            int iOffset = GetObjFieldOffset(taObj, objData.MethodTable, W("_message"));
            if (iOffset > 0)
            {
                MOVE (taMsg, taObj + iOffset);
            }
        }

        ExtOut("Message:          ");

        if (taMsg)
            StringObjectContent(taMsg);
        else
            ExtOut("<none>");

        ExtOut("\n");
    }

    {
        TADDR taInnerExc = 0;
        if (bGotExcData)
        {
            taInnerExc = TO_TADDR(excData.InnerException);
        }
        else
        {
            int iOffset = GetObjFieldOffset(taObj, objData.MethodTable, W("_innerException"));
            if (iOffset > 0)
            {
                MOVE (taInnerExc, taObj + iOffset);
            }
        }

        ExtOut("InnerException:   ");
        if (taInnerExc)
        {
            TADDR taMT;
            if (SUCCEEDED(GetMTOfObject(taInnerExc, &taMT)))
            {
                NameForMT_s(taMT, g_mdName, mdNameLen);                
                ExtOut("%S, ", g_mdName);
                if (IsDMLEnabled())
                    DMLOut("Use <exec cmd=\"!PrintException /d %p\">!PrintException %p</exec> to see more.\n", taInnerExc, taInnerExc);
                else
                    ExtOut("Use !PrintException %p to see more.\n", SOS_PTR(taInnerExc));
            }
            else
            {
                ExtOut("<invalid MethodTable of inner exception>");
            }
        }
        else
        {
            ExtOut("<none>\n");
        }
    }

    BOOL bAsync = bGotExcData ? IsAsyncException(excData)
                              : IsAsyncException(taObj, objData.MethodTable);

    {
        TADDR taStackTrace = 0;
        if (bGotExcData)
        {
            taStackTrace = TO_TADDR(excData.StackTrace);
        }
        else
        {
            int iOffset = GetObjFieldOffset (taObj, objData.MethodTable, W("_stackTrace"));
            if (iOffset > 0)        
            {
                MOVE(taStackTrace, taObj + iOffset);
            }
        }

        ExtOut("StackTrace (generated):\n");
        if (taStackTrace)
        {
            DWORD arrayLen;
            HRESULT hr = MOVE(arrayLen, taStackTrace + sizeof(DWORD_PTR));

            if (arrayLen != 0 && hr == S_OK)
            {
#ifdef _TARGET_WIN64_
                DWORD_PTR dataPtr = taStackTrace + sizeof(DWORD_PTR) + sizeof(DWORD) + sizeof(DWORD);
#else
                DWORD_PTR dataPtr = taStackTrace + sizeof(DWORD_PTR) + sizeof(DWORD);
#endif // _TARGET_WIN64_
                size_t stackTraceSize = 0;
                MOVE (stackTraceSize, dataPtr);

                DWORD cbStackSize = static_cast<DWORD>(stackTraceSize * sizeof(StackTraceElement));
                dataPtr += sizeof(size_t) + sizeof(size_t); // skip the array header, then goes the data
            
                if (stackTraceSize == 0)
                {
                    ExtOut("Unable to decipher generated stack trace\n");
                }
                else
                {
                    size_t iHeaderLength = AddExceptionHeader (NULL, 0);
                    size_t iLength = FormatGeneratedException (dataPtr, cbStackSize, NULL, 0, bAsync, FALSE, bLineNumbers);
                    WCHAR *pwszBuffer = new NOTHROW WCHAR[iHeaderLength + iLength + 1];
                    if (pwszBuffer)
                    {
                        AddExceptionHeader(pwszBuffer, iHeaderLength + 1);
                        FormatGeneratedException(dataPtr, cbStackSize, pwszBuffer + iHeaderLength, iLength + 1, bAsync, FALSE, bLineNumbers);
                        SosExtOutLargeString(pwszBuffer, iHeaderLength + iLength + 1);
                        delete[] pwszBuffer;
                    }
                    ExtOut("\n");
                }
            }
            else
            {
                ExtOut("<Not Available>\n");
            }
        }                   
        else
        {
            ExtOut("<none>\n");
        }
    }

    {
        TADDR taStackString;
        if (bGotExcData)
        {
            taStackString = TO_TADDR(excData.StackTraceString);
        }
        else
        {
            int iOffset = GetObjFieldOffset (taObj, objData.MethodTable, W("_stackTraceString"));
            MOVE (taStackString, taObj + iOffset);
        }

        ExtOut("StackTraceString: ");
        if (taStackString)
        {
            StringObjectContent(taStackString);
            ExtOut("\n\n"); // extra newline looks better
        }
        else
        {
            ExtOut("<none>\n");
        }
    }

    {
        DWORD hResult;
        if (bGotExcData)
        {
            hResult = excData.HResult;
        }
        else
        {
            int iOffset = GetObjFieldOffset (taObj, objData.MethodTable, W("_HResult"));
            MOVE (hResult, taObj + iOffset);
        }

        ExtOut("HResult: %lx\n", hResult);
    }

    if (isSecurityExceptionObj(objData.MethodTable) != NULL)
    {
        // We have a SecurityException Object: print out the debugString if present
        int iOffset = GetObjFieldOffset (taObj, objData.MethodTable, W("m_debugString"));
        if (iOffset > 0)        
        {
            TADDR taDebugString;
            MOVE (taDebugString, taObj + iOffset);                
            
            if (taDebugString)
            {
                ExtOut("SecurityException Message: ");
                StringObjectContent(taDebugString);
                ExtOut("\n\n"); // extra newline looks better
            }
        }            
    }

    return Status;
}

DECLARE_API(PrintException)
{
    INIT_API();
    
    BOOL dml = FALSE;
    BOOL bShowNested = FALSE;   
    BOOL bLineNumbers = FALSE;
    BOOL bCCW = FALSE;
    StringHolder strObject;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-nested", &bShowNested, COBOOL, FALSE},
        {"-lines", &bLineNumbers, COBOOL, FALSE},
        {"-l", &bLineNumbers, COBOOL, FALSE},
        {"-ccw", &bCCW, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&strObject, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (bLineNumbers)
    {
        ULONG symlines = 0;
        if (SUCCEEDED(g_ExtSymbols->GetSymbolOptions(&symlines)))
        {
            symlines &= SYMOPT_LOAD_LINES;
        }
        if (symlines == 0)
        {
            ExtOut("In order for the option -lines to enable display of source information\n"
                   "the debugger must be configured to load the line number information from\n"
                   "the symbol files. Use the \".lines; .reload\" command to achieve this.\n");
            // don't even try
            bLineNumbers = FALSE;
        }
    }

    EnableDMLHolder dmlHolder(dml);
    DWORD_PTR p_Object = NULL;
    if (nArg == 0)
    {
        if (bCCW)
        {
            ExtOut("No CCW pointer specified\n");
            return Status;
        }

        // Look at the last exception object on this thread

        CLRDATA_ADDRESS threadAddr = GetCurrentManagedThread();
        DacpThreadData Thread;
        
        if ((threadAddr == NULL) || (Thread.Request(g_sos, threadAddr) != S_OK))
        {
            ExtOut("The current thread is unmanaged\n");
            return Status;
        }

        DWORD_PTR dwAddr = NULL;
        if ((!SafeReadMemory(TO_TADDR(Thread.lastThrownObjectHandle),
                            &dwAddr,
                            sizeof(dwAddr), NULL)) || (dwAddr==NULL))
        {
            ExtOut("There is no current managed exception on this thread\n");            
        }    
        else
        {        
            p_Object = dwAddr;        
        }
    }
    else
    {
        p_Object = GetExpression(strObject.data);
        if (p_Object == 0)
        {
            if (bCCW)
            {
                ExtOut("Invalid CCW pointer %s\n", args);
            }
            else
            {
                ExtOut("Invalid exception object %s\n", args);
            }
            return Status;
        }

        if (bCCW)
        {
            // check if the address is a CCW pointer and then
            // get the exception object from it
            DacpCCWData ccwData;
            if (ccwData.Request(g_sos, p_Object) == S_OK)
            {
                p_Object = TO_TADDR(ccwData.managedObject);
            }
        }
    }

    if (p_Object)
    {
        FormatException(TO_CDADDR(p_Object), bLineNumbers);
    }

    // Are there nested exceptions?
    CLRDATA_ADDRESS threadAddr = GetCurrentManagedThread();
    DacpThreadData Thread;
    
    if ((threadAddr == NULL) || (Thread.Request(g_sos, threadAddr) != S_OK))
    {
        ExtOut("The current thread is unmanaged\n");
        return Status;
    }

    if (Thread.firstNestedException)
    {
        if (!bShowNested)
        {
            ExtOut("There are nested exceptions on this thread. Run with -nested for details\n");
            return Status;
        }
        
        CLRDATA_ADDRESS currentNested = Thread.firstNestedException;
        do
        {
            CLRDATA_ADDRESS obj = 0, next = 0;
            Status = g_sos->GetNestedExceptionData(currentNested, &obj, &next);

            if (Status != S_OK)
            {
                ExtOut("Error retrieving nested exception info %p\n", SOS_PTR(currentNested));
                return Status;
            }

            if (IsInterrupt())
            {
                ExtOut("<aborted>\n");
                return Status;
            }

            ExtOut("\nNested exception -------------------------------------------------------------\n");
            Status = FormatException(obj, bLineNumbers);
            if (Status != S_OK)
            {
                return Status;
            }
            
            currentNested = next;
        }
        while(currentNested != NULL);        
    }
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of an object from a  *  
*    given address
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpVC)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    DWORD_PTR p_MT = NULL;
    DWORD_PTR p_Object = NULL;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&p_MT, COHEX},
        {&p_Object, COHEX},
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (nArg!=2)
    {
        ExtOut("Usage: !DumpVC <Method Table> <Value object start addr>\n");
        return Status;
    }
    
    if (!IsMethodTable(p_MT))
    {
        ExtOut("Not a managed object\n");
        return S_OK;
    } 

    return PrintVC(p_MT, p_Object);
}

#ifndef FEATURE_PAL

#ifdef FEATURE_COMINTEROP

DECLARE_API(DumpRCW)
{
    INIT_API();
    
    BOOL dml = FALSE;
    StringHolder strObject;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE}
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&strObject, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (nArg == 0)
    {
        ExtOut("Missing RCW address\n");
        return Status;
    }
    else
    {
        DWORD_PTR p_RCW = GetExpression(strObject.data);
        if (p_RCW == 0)
        {
            ExtOut("Invalid RCW %s\n", args);
        }
        else
        {
            DacpRCWData rcwData;
            if ((Status = rcwData.Request(g_sos, p_RCW)) != S_OK)
            {
                ExtOut("Error requesting RCW data\n");
                return Status;
            }
            BOOL isDCOMProxy;
            if (FAILED(rcwData.IsDCOMProxy(g_sos, p_RCW, &isDCOMProxy)))
            {
                isDCOMProxy = FALSE;
            }

            DMLOut("Managed object:             %s\n", DMLObject(rcwData.managedObject));
            DMLOut("Creating thread:            %p\n", SOS_PTR(rcwData.creatorThread));
            ExtOut("IUnknown pointer:           %p\n", SOS_PTR(rcwData.unknownPointer));
            ExtOut("COM Context:                %p\n", SOS_PTR(rcwData.ctxCookie));
            ExtOut("Managed ref count:          %d\n", rcwData.refCount);
            ExtOut("IUnknown V-table pointer :  %p (captured at RCW creation time)\n", SOS_PTR(rcwData.vtablePtr));

            ExtOut("Flags:                      %s%s%s%s%s%s%s%s\n", 
                (rcwData.isDisconnected? "IsDisconnected " : ""),
                (rcwData.supportsIInspectable? "SupportsIInspectable " : ""),
                (rcwData.isAggregated? "IsAggregated " : ""),
                (rcwData.isContained? "IsContained " : ""),
                (rcwData.isJupiterObject? "IsJupiterObject " : ""),
                (rcwData.isFreeThreaded? "IsFreeThreaded " : ""),
                (rcwData.identityPointer == TO_CDADDR(p_RCW)? "IsUnique " : ""),
                (isDCOMProxy ? "IsDCOMProxy " : "")
                );

            // Jupiter data hidden by default
            if (rcwData.isJupiterObject)
            {
                ExtOut("IJupiterObject:    %p\n", SOS_PTR(rcwData.jupiterObject));            
            }
            
            ExtOut("COM interface pointers:\n");

            ArrayHolder<DacpCOMInterfacePointerData> pArray = new NOTHROW DacpCOMInterfacePointerData[rcwData.interfaceCount];
            if (pArray == NULL)
            {
                ReportOOM();            
                return Status;
            }

            if ((Status = g_sos->GetRCWInterfaces(p_RCW, rcwData.interfaceCount, pArray, NULL)) != S_OK)
            {
                ExtOut("Error requesting COM interface pointers\n");
                return Status;
            }

            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s Type\n", "IP", "Context", "MT");
            for (int i = 0; i < rcwData.interfaceCount; i++)
            {
                // Ignore any NULL MethodTable interface cache. At this point only IJupiterObject 
                // is saved as NULL MethodTable at first slot, and we've already printed outs its 
                // value earlier.
                if (pArray[i].methodTable == NULL)
                    continue;
                
                NameForMT_s(TO_TADDR(pArray[i].methodTable), g_mdName, mdNameLen);
                
                DMLOut("%p %p %s %S\n", SOS_PTR(pArray[i].interfacePtr), SOS_PTR(pArray[i].comContext), DMLMethodTable(pArray[i].methodTable), g_mdName);
            }
        }
    }

    return Status;
}

DECLARE_API(DumpCCW)
{
    INIT_API();
    
    BOOL dml = FALSE;
    StringHolder strObject;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE}
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&strObject, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (nArg == 0)
    {
        ExtOut("Missing CCW address\n");
        return Status;
    }
    else
    {
        DWORD_PTR p_CCW = GetExpression(strObject.data);
        if (p_CCW == 0)
        {
            ExtOut("Invalid CCW %s\n", args);
        }
        else
        {
            DacpCCWData ccwData;
            if ((Status = ccwData.Request(g_sos, p_CCW)) != S_OK)
            {
                ExtOut("Error requesting CCW data\n");
                return Status;
            }

            if (ccwData.ccwAddress != p_CCW)
                ExtOut("CCW:               %p\n", SOS_PTR(ccwData.ccwAddress));
            
            DMLOut("Managed object:    %s\n", DMLObject(ccwData.managedObject));
            ExtOut("Outer IUnknown:    %p\n", SOS_PTR(ccwData.outerIUnknown));
            ExtOut("Ref count:         %d%s\n", ccwData.refCount, ccwData.isNeutered ? " (NEUTERED)" : "");
            ExtOut("Flags:             %s%s\n",
                (ccwData.isExtendsCOMObject? "IsExtendsCOMObject " : ""),
                (ccwData.isAggregated? "IsAggregated " : "")
                );
                
            // Jupiter information hidden by default
            if (ccwData.jupiterRefCount > 0)
            {
                ExtOut("Jupiter ref count: %d%s%s%s%s\n", 
                    ccwData.jupiterRefCount, 
                    (ccwData.isPegged || ccwData.isGlobalPegged) ? ", Pegged by" : "",
                    ccwData.isPegged ? " Jupiter " : "",
                    (ccwData.isPegged && ccwData.isGlobalPegged) ? "&" : "",
                    ccwData.isGlobalPegged ? " CLR " : ""
                    );
            }
            
            ExtOut("RefCounted Handle: %p%s\n", 
                SOS_PTR(ccwData.handle), 
                (ccwData.hasStrongRef ? " (STRONG)" : " (WEAK)"));

            ExtOut("COM interface pointers:\n");            

            ArrayHolder<DacpCOMInterfacePointerData> pArray = new NOTHROW DacpCOMInterfacePointerData[ccwData.interfaceCount];
            if (pArray == NULL)
            {
                ReportOOM();            
                return Status;
            }

            if ((Status = g_sos->GetCCWInterfaces(p_CCW, ccwData.interfaceCount, pArray, NULL)) != S_OK)
            {
                ExtOut("Error requesting COM interface pointers\n");
                return Status;
            }

            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s Type\n", "IP", "MT", "Type");
            for (int i = 0; i < ccwData.interfaceCount; i++)
            {
                if (pArray[i].methodTable == NULL)
                {
                    wcscpy_s(g_mdName, mdNameLen, W("IDispatch/IUnknown"));
                }
                else
                {
                    NameForMT_s(TO_TADDR(pArray[i].methodTable), g_mdName, mdNameLen);
                }
                
                DMLOut("%p %s %S\n", pArray[i].interfacePtr, DMLMethodTable(pArray[i].methodTable), g_mdName);
            }
        }
    }

    return Status;
}

#endif // FEATURE_COMINTEROP

#ifdef _DEBUG
/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a PermissionSet   *
*    from a given address.                                             * 
*                                                                      *
\**********************************************************************/
/* 
    COMMAND: dumppermissionset.
    !DumpPermissionSet <PermissionSet object address>

    This command allows you to examine a PermissionSet object. Note that you can 
    also use DumpObj such an object in greater detail. DumpPermissionSet attempts 
    to extract all the relevant information from a PermissionSet that you might be 
    interested in when performing Code Access Security (CAS) related debugging.

    Here is a simple PermissionSet object:

    0:000> !DumpPermissionSet 014615f4 
    PermissionSet object: 014615f4
    Unrestricted: TRUE

    Note that this is an unrestricted PermissionSet object that does not contain 
    any individual permissions. 

    Here is another example of a PermissionSet object, one that is not unrestricted 
    and contains a single permission:

    0:003> !DumpPermissionSet 01469fa8 
    PermissionSet object: 01469fa8
    Unrestricted: FALSE
    Name: System.Security.Permissions.ReflectionPermission
    MethodTable: 5b731308
    EEClass: 5b7e0d78
    Size: 12(0xc) bytes
     (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_32\mscorlib\2.0.
    0.0__b77a5c561934e089\mscorlib.dll)

    Fields:
          MT    Field   Offset                 Type VT     Attr    Value Name
    5b73125c  4001d66        4         System.Int32  0 instance        2 m_flags

    Here is another example of an unrestricted PermissionSet, one that contains 
    several permissions. The numbers in parentheses before each Permission object 
    represents the index of that Permission in the PermissionSet.

    0:003> !DumpPermissionSet 01467bd8
    PermissionSet object: 01467bd8
    Unrestricted: FALSE
    [1] 01467e90
        Name: System.Security.Permissions.FileDialogPermission
        MethodTable: 5b73023c
        EEClass: 5b7dfb18
        Size: 12(0xc) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_32\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        5b730190  4001cc2        4         System.Int32  0 instance        1 access
    [4] 014682a8
        Name: System.Security.Permissions.ReflectionPermission
        MethodTable: 5b731308
        EEClass: 5b7e0d78
        Size: 12(0xc) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_32\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        5b73125c  4001d66        4         System.Int32  0 instance        0 m_flags
    [17] 0146c060
        Name: System.Diagnostics.EventLogPermission
        MethodTable: 569841c4
        EEClass: 56a03e5c
        Size: 28(0x1c) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        5b6d65d4  4003078        4      System.Object[]  0 instance 0146c190 tagNames
        5b6c9ed8  4003079        8          System.Type  0 instance 0146c17c permissionAccessType
        5b6cd928  400307a       10       System.Boolean  0 instance        0 isUnrestricted
        5b6c45f8  400307b        c ...ections.Hashtable  0 instance 0146c1a4 rootTable
        5b6c090c  4003077      bfc        System.String  0   static 00000000 computerName
        56984434  40030e7       14 ...onEntryCollection  0 instance 00000000 innerCollection
    [18] 0146ceb4
        Name: System.Net.WebPermission
        MethodTable: 5696dfc4
        EEClass: 569e256c
        Size: 20(0x14) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        5b6cd928  400238e        c       System.Boolean  0 instance        0 m_Unrestricted
        5b6cd928  400238f        d       System.Boolean  0 instance        0 m_UnrestrictedConnect
        5b6cd928  4002390        e       System.Boolean  0 instance        0 m_UnrestrictedAccept
        5b6c639c  4002391        4 ...ections.ArrayList  0 instance 0146cf3c m_connectList
        5b6c639c  4002392        8 ...ections.ArrayList  0 instance 0146cf54 m_acceptList
        569476f8  4002393      8a4 ...Expressions.Regex  0   static 00000000 s_MatchAllRegex
    [19] 0146a5fc
        Name: System.Net.DnsPermission
        MethodTable: 56966408
        EEClass: 569d3c08
        Size: 12(0xc) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        5b6cd928  4001d2c        4       System.Boolean  0 instance        1 m_noRestriction
    [20] 0146d8ec
        Name: System.Web.AspNetHostingPermission
        MethodTable: 569831bc
        EEClass: 56a02ccc
        Size: 12(0xc) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        56983090  4003074        4         System.Int32  0 instance      600 _level
    [21] 0146e394
        Name: System.Net.NetworkInformation.NetworkInformationPermission
        MethodTable: 5697ac70
        EEClass: 569f7104
        Size: 16(0x10) bytes
         (C:\WINDOWS\Microsoft.NET\Framework\v2.0.x86chk\assembly\GAC_MSIL\System\2.0.0.0__b77a5c561934e089\System.dll)
        Fields:
              MT    Field   Offset                 Type VT     Attr    Value Name
        5697ab38  4002c34        4         System.Int32  0 instance        0 access
        5b6cd928  4002c35        8       System.Boolean  0 instance        0 unrestricted


    The abbreviation !dps can be used for brevity.

    \\
*/
DECLARE_API(DumpPermissionSet)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    DWORD_PTR p_Object = NULL;

    CMDValue arg[] = 
    {
        {&p_Object, COHEX}
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (nArg!=1)
    {
        ExtOut("Usage: !DumpPermissionSet <PermissionSet object addr>\n");
        return Status;
    }
    

    return PrintPermissionSet(p_Object);
}

#endif // _DEBUG

void GCPrintGenerationInfo(DacpGcHeapDetails &heap);
void GCPrintSegmentInfo(DacpGcHeapDetails &heap, DWORD_PTR &total_size);

#endif // FEATURE_PAL

void DisplayInvalidStructuresMessage()
{
    ExtOut("The garbage collector data structures are not in a valid state for traversal.\n");
    ExtOut("It is either in the \"plan phase,\" where objects are being moved around, or\n");
    ExtOut("we are at the initialization or shutdown of the gc heap. Commands related to \n");
    ExtOut("displaying, finding or traversing objects as well as gc heap segments may not \n");
    ExtOut("work properly. !dumpheap and !verifyheap may incorrectly complain of heap \n");
    ExtOut("consistency errors.\n");
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function dumps GC heap size.                                 *  
*                                                                      *
\**********************************************************************/
DECLARE_API(EEHeap)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    BOOL dml = FALSE;
    BOOL showgc = FALSE;
    BOOL showloader = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-gc", &showgc, COBOOL, FALSE},
        {"-loader", &showloader, COBOOL, FALSE},
        {"/d", &dml, COBOOL, FALSE},
    };

    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (showloader || !showgc)
    {
        // Loader heap.
        DWORD_PTR allHeapSize = 0;
        DWORD_PTR wasted = 0;    
        DacpAppDomainStoreData adsData;
        if ((Status=adsData.Request(g_sos))!=S_OK)
        {
            ExtOut("Unable to get AppDomain information\n");
            return Status;
        }

        // The first one is the system domain.
        ExtOut("Loader Heap:\n");
        IfFailRet(PrintDomainHeapInfo("System Domain", adsData.systemDomain, &allHeapSize, &wasted));
        if (adsData.sharedDomain != NULL)
        {
            IfFailRet(PrintDomainHeapInfo("Shared Domain", adsData.sharedDomain, &allHeapSize, &wasted));
        }
        
        ArrayHolder<CLRDATA_ADDRESS> pArray = new NOTHROW CLRDATA_ADDRESS[adsData.DomainCount];

        if (pArray==NULL)
        {
            ReportOOM();            
            return Status;
        }

        if ((Status=g_sos->GetAppDomainList(adsData.DomainCount, pArray, NULL))!=S_OK)
        {
            ExtOut("Unable to get the array of all AppDomains.\n");
            return Status;
        }

        for (int n=0;n<adsData.DomainCount;n++)
        {
            if (IsInterrupt())
                break;

            char domain[16];
            sprintf_s(domain, _countof(domain), "Domain %d", n+1);

            IfFailRet(PrintDomainHeapInfo(domain, pArray[n], &allHeapSize, &wasted));

        }

        // Jit code heap
        ExtOut("--------------------------------------\n");
        ExtOut("Jit code heap:\n");

        if (IsMiniDumpFile())
        {
            ExtOut("<no information>\n");
        }
        else
        {
            allHeapSize += JitHeapInfo();
        }

    
        // Module Data
        {
            int numModule;
            ArrayHolder<DWORD_PTR> moduleList = ModuleFromName(NULL, &numModule);   
            if (moduleList == NULL)
            {
                ExtOut("Failed to request module list.\n");
            }
            else
            {
                // Module Thunk Heaps
                ExtOut("--------------------------------------\n");
                ExtOut("Module Thunk heaps:\n");
                allHeapSize += PrintModuleHeapInfo(moduleList, numModule, ModuleHeapType_ThunkHeap, &wasted);

                // Module Lookup Table Heaps
                ExtOut("--------------------------------------\n");
                ExtOut("Module Lookup Table heaps:\n");
                allHeapSize += PrintModuleHeapInfo(moduleList, numModule, ModuleHeapType_LookupTableHeap, &wasted);
            }
        }

        ExtOut("--------------------------------------\n");
        ExtOut("Total LoaderHeap size:   ");
        PrintHeapSize(allHeapSize, wasted);
        ExtOut("=======================================\n");
    }

    if (showgc || !showloader)
    {   
        // GC Heap
        DWORD dwNHeaps = 1;

        if (!GetGcStructuresValid())
        {
            DisplayInvalidStructuresMessage();
        }
        
        DacpGcHeapData gcheap;
        if (gcheap.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting GC Heap data\n");
            return Status;
        }

        if (gcheap.bServerMode)
        {
            dwNHeaps = gcheap.HeapCount;
        }

        ExtOut("Number of GC Heaps: %d\n", dwNHeaps);
        DWORD_PTR totalSize = 0;
        if (!gcheap.bServerMode)
        {
            DacpGcHeapDetails heapDetails;
            if (heapDetails.Request(g_sos) != S_OK)
            {
                ExtOut("Error requesting details\n");
                return Status;
            }

            GCHeapInfo (heapDetails, totalSize);
            ExtOut("Total Size:              ");
            PrintHeapSize(totalSize, 0);
        }
        else
        {
            DWORD dwAllocSize;
            if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
            {
                ExtOut("Failed to get GCHeaps: integer overflow\n");
                return Status;
            }

            CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
            if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
            {
                ExtOut("Failed to get GCHeaps\n");
                return Status;
            }
                        
            DWORD n;
            for (n = 0; n < dwNHeaps; n ++)
            {
                DacpGcHeapDetails heapDetails;
                if (heapDetails.Request(g_sos, heapAddrs[n]) != S_OK)
                {
                    ExtOut("Error requesting details\n");
                    return Status;
                }
                ExtOut("------------------------------\n");
                ExtOut("Heap %d (%p)\n", n, SOS_PTR(heapAddrs[n]));
                DWORD_PTR heapSize = 0;
                GCHeapInfo (heapDetails, heapSize);
                totalSize += heapSize;
                ExtOut("Heap Size:       " WIN86_8SPACES);
                PrintHeapSize(heapSize, 0);
            }
        }
        ExtOut("------------------------------\n");
        ExtOut("GC Heap Size:    " WIN86_8SPACES);
        PrintHeapSize(totalSize, 0);
    }
    return Status;
}

void PrintGCStat(HeapStat *inStat, const char* label=NULL)
{
    if (inStat)
    {
        bool sorted = false;
        try
        {
            inStat->Sort();
            sorted = true;
            inStat->Print(label);
        }
        catch(...)
        {
            ExtOut("Exception occurred while trying to %s the GC stats.\n", sorted ? "print" : "sort");
        }

        inStat->Delete();
    }
}

#ifndef FEATURE_PAL

DECLARE_API(TraverseHeap)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    BOOL bXmlFormat = FALSE;
    BOOL bVerify = FALSE;
    StringHolder Filename;

    CMDOption option[] = 
    {   // name, vptr,        type, hasValue
        {"-xml", &bXmlFormat, COBOOL, FALSE},
        {"-verify", &bVerify, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&Filename.data, COSTRING},
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    if (nArg != 1)
    {
        ExtOut("usage: HeapTraverse [-xml] filename\n");
        return Status;
    }

    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        return Status;
    }

    FILE* file = NULL;
    if (fopen_s(&file, Filename.data, "w") != 0) {
        ExtOut("Unable to open file\n");
        return Status;
    }

    if (!bVerify)
        ExtOut("Assuming a uncorrupted GC heap.  If this is a crash dump consider -verify option\n"); 

    HeapTraverser traverser(bVerify != FALSE);

    ExtOut("Writing %s format to file %s\n", bXmlFormat ? "Xml" : "CLRProfiler", Filename.data);    
    ExtOut("Gathering types...\n");

    // TODO: there may be a canonical list of methodtables in the runtime that we can
    // traverse instead of exploring the gc heap for that list. We could then simplify the
    // tree structure to a sorted list of methodtables, and the index is the ID.

    // TODO: "Traversing object members" code should be generalized and shared between
    // !gcroot and !traverseheap. Also !dumpheap can begin using GCHeapsTraverse.

    if (!traverser.Initialize())
    {
        ExtOut("Error initializing heap traversal\n");
        fclose(file);
        return Status;
    }

    if (!traverser.CreateReport (file, bXmlFormat ? FORMAT_XML : FORMAT_CLRPROFILER))
    {
        ExtOut("Unable to write heap report\n");
        fclose(file);
        return Status;
    }

    fclose(file);                
    ExtOut("\nfile %s saved\n", Filename.data);
    
    return Status;
}

#endif // FEATURE_PAL

struct PrintRuntimeTypeArgs
{
    DWORD_PTR mtOfRuntimeType;
    int handleFieldOffset;
    DacpAppDomainStoreData adstore;
};

void PrintRuntimeTypes(DWORD_PTR objAddr,size_t Size,DWORD_PTR methodTable,LPVOID token)
{
    PrintRuntimeTypeArgs *pArgs = (PrintRuntimeTypeArgs *)token;

    if (pArgs->mtOfRuntimeType == NULL)
    {
        NameForMT_s(methodTable, g_mdName, mdNameLen);

        if (_wcscmp(g_mdName, W("System.RuntimeType")) == 0)
        {
            pArgs->mtOfRuntimeType = methodTable;
            pArgs->handleFieldOffset = GetObjFieldOffset(TO_CDADDR(objAddr), TO_CDADDR(methodTable), W("m_handle"));
            if (pArgs->handleFieldOffset <= 0)
                ExtOut("Error getting System.RuntimeType.m_handle offset\n");

            pArgs->adstore.Request(g_sos);
        }
    }

    if ((methodTable == pArgs->mtOfRuntimeType) && (pArgs->handleFieldOffset > 0))
    {
        // Get the method table and display the information.
        DWORD_PTR mtPtr;
        if (MOVE(mtPtr, objAddr + pArgs->handleFieldOffset) == S_OK)
        {
            DMLOut(DMLObject(objAddr));

            CLRDATA_ADDRESS appDomain = GetAppDomainForMT(mtPtr);
            if (appDomain != NULL)
            {
                if (appDomain == pArgs->adstore.sharedDomain)
                    ExtOut(" %" POINTERSIZE "s", "Shared");

                else if (appDomain == pArgs->adstore.systemDomain)
                    ExtOut(" %" POINTERSIZE "s", "System");
                else
                    DMLOut(" %s", DMLDomain(appDomain));
            }
            else
            {
                ExtOut(" %" POINTERSIZE "s", "?");
            }
        
            NameForMT_s(mtPtr, g_mdName, mdNameLen);
            DMLOut(" %s %S\n", DMLMethodTable(mtPtr), g_mdName);
        }
    }
}


DECLARE_API(DumpRuntimeTypes)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };

    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL))
        return Status;

    EnableDMLHolder dmlHolder(dml);

    ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s Type Name              \n",
           "Address", "Domain", "MT");
    ExtOut("------------------------------------------------------------------------------\n");

    PrintRuntimeTypeArgs pargs;
    ZeroMemory(&pargs, sizeof(PrintRuntimeTypeArgs));

    GCHeapsTraverse(PrintRuntimeTypes, (LPVOID)&pargs);
    return Status;
}

#define MIN_FRAGMENTATIONBLOCK_BYTES (1024*512)
namespace sos
{
    class FragmentationBlock
    {
    public:
        FragmentationBlock(TADDR addr, size_t size, TADDR next, TADDR mt)
            : mAddress(addr), mSize(size), mNext(next), mNextMT(mt)
        {
        }

        inline TADDR GetAddress() const
        {
            return mAddress;
        }
        inline size_t GetSize() const
        {
            return mSize;
        }

        inline TADDR GetNextObject() const
        {
            return mNext;
        }

        inline TADDR GetNextMT() const
        {
            return mNextMT;
        }

    private:
        TADDR mAddress;
        size_t mSize;
        TADDR mNext;
        TADDR mNextMT;
    };
}

class DumpHeapImpl
{
public:
    DumpHeapImpl(PCSTR args)
        : mStart(0), mStop(0), mMT(0),  mMinSize(0), mMaxSize(~0),
          mStat(FALSE), mStrings(FALSE), mVerify(FALSE),
          mThinlock(FALSE), mShort(FALSE), mDML(FALSE),
          mLive(FALSE), mDead(FALSE), mType(NULL)
    {
        ArrayHolder<char> type = NULL;

        TADDR minTemp = 0;
        CMDOption option[] = 
        {   // name, vptr, type, hasValue
            {"-mt", &mMT, COHEX, TRUE},              // dump objects with a given MethodTable
            {"-type", &type, COSTRING, TRUE},        // list objects of specified type
            {"-stat", &mStat, COBOOL, FALSE},        // dump a summary of types and the number of instances of each
            {"-strings", &mStrings, COBOOL, FALSE},  // dump a summary of string objects
            {"-verify", &mVerify, COBOOL, FALSE},    // verify heap objects (!heapverify)
            {"-thinlock", &mThinlock, COBOOL, FALSE},// list only thinlocks
            {"-short", &mShort, COBOOL, FALSE},      // list only addresses
            {"-min", &mMinSize, COHEX, TRUE},        // min size of objects to display
            {"-max", &mMaxSize, COHEX, TRUE},        // max size of objects to display
            {"-live", &mLive, COHEX, FALSE},         // only print live objects
            {"-dead", &mDead, COHEX, FALSE},         // only print dead objects
#ifndef FEATURE_PAL
            {"/d", &mDML, COBOOL, FALSE},            // Debugger Markup Language
#endif
        };

        CMDValue arg[] = 
        {   // vptr, type
            {&mStart, COHEX},
            {&mStop, COHEX}
        };

        size_t nArgs = 0;
        if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArgs))
            sos::Throw<sos::Exception>("Failed to parse command line arguments.");

        if (mStart == 0)
            mStart = minTemp;

        if (mStop == 0)
            mStop = sos::GCHeap::HeapEnd;

        if (type && mMT)
        {
            sos::Throw<sos::Exception>("Cannot specify both -mt and -type");
        }
        
        if (mLive && mDead)
        {
            sos::Throw<sos::Exception>("Cannot specify both -live and -dead.");
        }

        if (mMinSize > mMaxSize)
        {
            sos::Throw<sos::Exception>("wrong argument");
        }
        
        // If the user gave us a type, convert it to unicode and clean up "type".
        if (type && !mStrings)
        {
            size_t iLen = strlen(type) + 1;
            mType = new WCHAR[iLen];
            MultiByteToWideChar(CP_ACP, 0, type, -1, mType, (int)iLen);
        }
    }

    ~DumpHeapImpl()
    {
        if (mType)
            delete [] mType;
    }

    void Run()
    {
        // enable Debugger Markup Language
        EnableDMLHolder dmlholder(mDML); 
        sos::GCHeap gcheap;

        if (!gcheap.AreGCStructuresValid())
            DisplayInvalidStructuresMessage();
        
        if (IsMiniDumpFile())
        {
            ExtOut("In a minidump without full memory, most gc heap structures will not be valid.\n");
            ExtOut("If you need this functionality, get a full memory dump with \".dump /ma mydump.dmp\"\n");
        }

#ifndef FEATURE_PAL
        if (mLive || mDead)
        {
            GCRootImpl gcroot;
            mLiveness = gcroot.GetLiveObjects();
        }
#endif

        // Some of the "specialty" versions of DumpHeap have slightly
        // different implementations than the standard version of DumpHeap.
        // We seperate them out to not clutter the standard DumpHeap function.
        if (mShort)
            DumpHeapShort(gcheap);
        else if (mThinlock)
            DumpHeapThinlock(gcheap);
        else if (mStrings)
            DumpHeapStrings(gcheap);
        else
            DumpHeap(gcheap);

        if (mVerify)
            ValidateSyncTable(gcheap);
    }

    static bool ValidateSyncTable(sos::GCHeap &gcheap)
    {
        bool succeeded = true;
        for (sos::SyncBlkIterator itr; itr; ++itr)
        {
            sos::CheckInterrupt();

            if (!itr->IsFree())
            {
                if (!sos::IsObject(itr->GetObject(), true))
                {
                    ExtOut("SyncBlock %d corrupted, points to invalid object %p\n", 
                            itr->GetIndex(), SOS_PTR(itr->GetObject()));
                        succeeded = false;
                }
                else
                {
                    // Does the object header point to this syncblock index?
                    sos::Object obj = itr->GetObject();
                    ULONG header = 0;

                    if (!obj.TryGetHeader(header))
                    {
                        ExtOut("Failed to get object header for object %p while inspecting syncblock at index %d.\n",
                                SOS_PTR(itr->GetObject()), itr->GetIndex());
                        succeeded = false;
                    }
                    else
                    {
                        bool valid = false;
                        if ((header & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) != 0 && (header & BIT_SBLK_IS_HASHCODE) == 0)
                        {
                            ULONG index = header & MASK_SYNCBLOCKINDEX;
                            valid = (ULONG)itr->GetIndex() == index;
                        }
                        
                        if (!valid)
                        {
                            ExtOut("Object header for %p should have a SyncBlock index of %d.\n",
                                    SOS_PTR(itr->GetObject()), itr->GetIndex());
                            succeeded = false;
                        }
                    }
                }
            }
        }

        return succeeded;
    }
private:
    DumpHeapImpl(const DumpHeapImpl &);

    bool Verify(const sos::ObjectIterator &itr)
    {
        if (mVerify)
        {
            char buffer[1024];
            if (!itr.Verify(buffer, _countof(buffer)))
            {
                ExtOut(buffer);
                return false;
            }
        }
        
        return true;
    }

    bool IsCorrectType(const sos::Object &obj)
    {
        if (mMT != NULL)
            return mMT == obj.GetMT();

        if (mType != NULL)
        {
            WString name = obj.GetTypeName();
            return _wcsstr(name.c_str(), mType) != NULL;
        }

        return true;
    }

    bool IsCorrectSize(const sos::Object &obj)
    {
        size_t size = obj.GetSize();
        return size >= mMinSize && size <= mMaxSize;
    }

    bool IsCorrectLiveness(const sos::Object &obj)
    {
#ifndef FEATURE_PAL
        if (mLive && mLiveness.find(obj.GetAddress()) == mLiveness.end())
            return false;

        if (mDead && (mLiveness.find(obj.GetAddress()) != mLiveness.end() || obj.IsFree()))
            return false;
#endif
        return true;
    }



    inline void PrintHeader()
    {
        ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %8s\n", "Address", "MT", "Size");
    }

    void DumpHeap(sos::GCHeap &gcheap)
    {
        HeapStat stats;

        // For heap fragmentation tracking.
        TADDR lastFreeObj = NULL;
        size_t lastFreeSize = 0;

        if (!mStat)
            PrintHeader();

        for (sos::ObjectIterator itr = gcheap.WalkHeap(mStart, mStop); itr; ++itr)
        {
            if (!Verify(itr))
                return;

            bool onLOH = itr.IsCurrObjectOnLOH();

            // Check for free objects to report fragmentation
            if (lastFreeObj != NULL)
                ReportFreeObject(lastFreeObj, lastFreeSize, itr->GetAddress(), itr->GetMT());

            if (!onLOH && itr->IsFree())
            {
                lastFreeObj = *itr;
                lastFreeSize = itr->GetSize();
            }
            else
            {
                lastFreeObj = NULL;
            }

            if (IsCorrectType(*itr) && IsCorrectSize(*itr) && IsCorrectLiveness(*itr))
            {
                stats.Add((DWORD_PTR)itr->GetMT(), (DWORD)itr->GetSize());
                if (!mStat)
                    DMLOut("%s %s %8d%s\n", DMLObject(itr->GetAddress()), DMLDumpHeapMT(itr->GetMT()), itr->GetSize(), 
                                            itr->IsFree() ? " Free":"     ");
            }
        }

        if (!mStat)
            ExtOut("\n");

        stats.Sort();
        stats.Print();

        PrintFragmentationReport();
    }

    struct StringSetEntry
    {
        StringSetEntry() : count(0), size(0)
        {
            str[0] = 0;
        }
        
        StringSetEntry(__in_ecount(64) WCHAR tmp[64], size_t _size)
            : count(1), size(_size)
        {
            memcpy(str, tmp, sizeof(str));
        }
        
        void Add(size_t _size) const
        {
            count++;
            size += _size;
        }
        
        mutable size_t count;
        mutable size_t size;
        WCHAR str[64];
        
        bool operator<(const StringSetEntry &rhs) const
        {
            return _wcscmp(str, rhs.str) < 0;
        }
    };

    
    static bool StringSetCompare(const StringSetEntry &a1, const StringSetEntry &a2)
    {
        return a1.size < a2.size;
    }

    void DumpHeapStrings(sos::GCHeap &gcheap)
    {
#ifdef FEATURE_PAL
        ExtOut("Not implemented.\n");
#else
        const int offset = sos::Object::GetStringDataOffset();
        typedef std::set<StringSetEntry> Set;
        Set set;            // A set keyed off of the string's text
        
        StringSetEntry tmp;  // Temp string used to keep track of the set
        ULONG fetched = 0;

        TableOutput out(3, POINTERSIZE_HEX, AlignRight);
        for (sos::ObjectIterator itr = gcheap.WalkHeap(mStart, mStop); itr; ++itr)
        {
            if (IsInterrupt())
                break;
                
            if (itr->IsString() && IsCorrectSize(*itr) && IsCorrectLiveness(*itr))
            {
                CLRDATA_ADDRESS addr = itr->GetAddress();
                size_t size = itr->GetSize();
                
                if (!mStat)
                    out.WriteRow(ObjectPtr(addr), Pointer(itr->GetMT()), Decimal(size));

                // Don't bother calculating the size of the string, just read the full 64 characters of the buffer.  The null
                // terminator we read will terminate the string.
                HRESULT hr = g_ExtData->ReadVirtual(TO_CDADDR(addr+offset), tmp.str, sizeof(WCHAR)*(_countof(tmp.str)-1), &fetched);
                if (SUCCEEDED(hr))
                {
                    // Ensure we null terminate the string.  Note that this will not overrun the buffer as we only
                    // wrote a max of 63 characters into the 64 character buffer.
                    tmp.str[fetched/sizeof(WCHAR)] = 0;
                    Set::iterator sitr = set.find(tmp);
                    if (sitr == set.end())
                    {
                        tmp.size = size;
                        tmp.count = 1;
                        set.insert(tmp);
                    }
                    else
                    {
                        sitr->Add(size);
                    }
                }
            }
        }

        ExtOut("\n");

        // Now flatten the set into a vector.  This is much faster than keeping two sets, or using a multimap.
        typedef std::vector<StringSetEntry> Vect;
        Vect v(set.begin(), set.end());
        std::sort(v.begin(), v.end(), &DumpHeapImpl::StringSetCompare);

        // Now print out the data.  The call to Flatten ensures that we don't print newlines to break up the
        // output in strange ways.
        for (Vect::iterator vitr = v.begin(); vitr != v.end(); ++vitr)
        {
            if (IsInterrupt())
                break;
                
            Flatten(vitr->str, (unsigned int)_wcslen(vitr->str));
            out.WriteRow(Decimal(vitr->size), Decimal(vitr->count), vitr->str);
        }
#endif // FEATURE_PAL
    }

    void DumpHeapShort(sos::GCHeap &gcheap)
    {
        for (sos::ObjectIterator itr = gcheap.WalkHeap(mStart, mStop); itr; ++itr)
        {
            if (!Verify(itr))
                return;

            if (IsCorrectType(*itr) && IsCorrectSize(*itr) && IsCorrectLiveness(*itr))
                DMLOut("%s\n", DMLObject(itr->GetAddress()));
        }
    }

    void DumpHeapThinlock(sos::GCHeap &gcheap)
    {
        int count = 0;

        PrintHeader();
        for (sos::ObjectIterator itr = gcheap.WalkHeap(mStart, mStop); itr; ++itr)
        {
            if (!Verify(itr))
                return;

            sos::ThinLockInfo lockInfo; 
            if (IsCorrectType(*itr) && itr->GetThinLock(lockInfo))
            {
                DMLOut("%s %s %8d", DMLObject(itr->GetAddress()), DMLDumpHeapMT(itr->GetMT()), itr->GetSize());
                ExtOut(" ThinLock owner %x (%p) Recursive %x\n", lockInfo.ThreadId,
                                        SOS_PTR(lockInfo.ThreadPtr), lockInfo.Recursion);

                count++;
            }
        }

        ExtOut("Found %d objects.\n", count);
    }

private:
    TADDR mStart,
          mStop,
          mMT,
          mMinSize,
          mMaxSize;

    BOOL mStat,
         mStrings,
         mVerify,
         mThinlock,
         mShort,
         mDML,
         mLive,
         mDead;


    WCHAR *mType;

private:
#if !defined(FEATURE_PAL)
    // Windows only
    std::unordered_set<TADDR> mLiveness;
    typedef std::list<sos::FragmentationBlock> FragmentationList;
    FragmentationList mFrag;

    void InitFragmentationList()
    {
        mFrag.clear();
    }

    void ReportFreeObject(TADDR addr, size_t size, TADDR next, TADDR mt)
    {
        if (size >= MIN_FRAGMENTATIONBLOCK_BYTES)
            mFrag.push_back(sos::FragmentationBlock(addr, size, next, mt));
    }

    void PrintFragmentationReport()
    {
        if (mFrag.size() > 0)
        {
            ExtOut("Fragmented blocks larger than 0.5 MB:\n");
            ExtOut("%" POINTERSIZE "s %8s %16s\n", "Addr", "Size", "Followed by");
 
            for (FragmentationList::const_iterator itr = mFrag.begin(); itr != mFrag.end(); ++itr)
            {
                sos::MethodTable mt = itr->GetNextMT();
                ExtOut("%p %6.1fMB " WIN64_8SPACES "%p %S\n",
                            SOS_PTR(itr->GetAddress()),
                            ((double)itr->GetSize()) / 1024.0 / 1024.0,
                            SOS_PTR(itr->GetNextObject()),
                            mt.GetName());
            }
        }
    }
#else
    void InitFragmentationList() {}
    void ReportFreeObject(TADDR, TADDR, size_t, TADDR) {}
    void PrintFragmentationReport() {}
#endif
};

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function dumps async state machines on GC heap,              *
*    displaying details about each async operation found.              *
*    (May not work if GC is in progress.)                              *
*                                                                      *
\**********************************************************************/

void ResolveContinuation(CLRDATA_ADDRESS* contAddr)
{
    // Ideally this continuation is itself an async method box.
    sos::Object contObj = TO_TADDR(*contAddr);
    if (GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("StateMachine")) == 0)
    {
        // It was something else.

        // If it's a standard task continuation, get its task field.
        int offset;
        if ((offset = GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("m_task"))) != 0)
        {
            MOVE(*contAddr, contObj.GetAddress() + offset);
            if (sos::IsObject(*contAddr, false))
            {
                contObj = TO_TADDR(*contAddr);
            }
        }
        else
        {
            // If it's storing an action wrapper, try to follow to that action's target.
            if ((offset = GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("m_action"))) != 0)
            {
                MOVE(*contAddr, contObj.GetAddress() + offset);
                if (sos::IsObject(*contAddr, false))
                {
                    contObj = TO_TADDR(*contAddr);
                }
            }

            // If we now have an Action, try to follow through to the delegate's target.
            if ((offset = GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("_target"))) != 0)
            {
                MOVE(*contAddr, contObj.GetAddress() + offset);
                if (sos::IsObject(*contAddr, false))
                {
                    contObj = TO_TADDR(*contAddr);

                    // In some cases, the delegate's target might be a ContinuationWrapper, in which case we want to unwrap that as well.
                    if (_wcsncmp(contObj.GetTypeName(), W("System.Runtime.CompilerServices.AsyncMethodBuilderCore+ContinuationWrapper"), 74) == 0 &&
                        (offset = GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("_continuation"))) != 0)
                    {
                        MOVE(*contAddr, contObj.GetAddress() + offset);
                        if (sos::IsObject(*contAddr, false))
                        {
                            contObj = TO_TADDR(*contAddr);
                            if ((offset = GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("_target"))) != 0)
                            {
                                MOVE(*contAddr, contObj.GetAddress() + offset);
                                if (sos::IsObject(*contAddr, false))
                                {
                                    contObj = TO_TADDR(*contAddr);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Use whatever object we ended with.
        *contAddr = contObj.GetAddress();
    }
}

bool TryGetContinuation(CLRDATA_ADDRESS addr, CLRDATA_ADDRESS mt, CLRDATA_ADDRESS* contAddr)
{
    // Get the continuation field from the task.
    int offset = GetObjFieldOffset(addr, mt, W("m_continuationObject"));
    if (offset != 0)
    {
        DWORD_PTR contObjPtr;
        MOVE(contObjPtr, addr + offset);
        if (sos::IsObject(contObjPtr, false))
        {
            *contAddr = TO_CDADDR(contObjPtr);
            ResolveContinuation(contAddr);
            return true;
        }
    }

    return false;
}

struct AsyncRecord
{
    CLRDATA_ADDRESS Address;
    CLRDATA_ADDRESS MT;
    DWORD Size;
    CLRDATA_ADDRESS StateMachineAddr;
    CLRDATA_ADDRESS StateMachineMT;
    BOOL FilteredByOptions;
    BOOL IsStateMachine;
    BOOL IsValueType;
    BOOL IsTopLevel;
    int TaskStateFlags;
    int StateValue;
    std::vector<CLRDATA_ADDRESS> Continuations;
};

bool AsyncRecordIsCompleted(AsyncRecord& ar)
{
    const int TASK_STATE_COMPLETED_MASK = 0x1600000;
    return (ar.TaskStateFlags & TASK_STATE_COMPLETED_MASK) != 0;
}

const char* GetAsyncRecordStatusDescription(AsyncRecord& ar)
{
    const int TASK_STATE_RAN_TO_COMPLETION = 0x1000000;
    const int TASK_STATE_FAULTED = 0x200000;
    const int TASK_STATE_CANCELED = 0x400000;

    if ((ar.TaskStateFlags & TASK_STATE_RAN_TO_COMPLETION) != 0) return "Success";
    if ((ar.TaskStateFlags & TASK_STATE_FAULTED) != 0) return "Failed";
    if ((ar.TaskStateFlags & TASK_STATE_CANCELED) != 0) return "Canceled";
    return "Pending";
}

void ExtOutTaskDelegateMethod(sos::Object& obj)
{
    DacpFieldDescData actionField;
    int offset = GetObjFieldOffset(obj.GetAddress(), obj.GetMT(), W("m_action"), TRUE, &actionField);
    if (offset != 0)
    {
        CLRDATA_ADDRESS actionAddr;
        MOVE(actionAddr, obj.GetAddress() + offset);
        CLRDATA_ADDRESS actionMD;
        if (actionAddr != NULL && TryGetMethodDescriptorForDelegate(actionAddr, &actionMD))
        {
            NameForMD_s((DWORD_PTR)actionMD, g_mdName, mdNameLen);
            ExtOut("(%S) ", g_mdName);
        }
    }
}

void ExtOutTaskStateFlagsDescription(int stateFlags)
{
    if (stateFlags == 0) return;

    ExtOut("State Flags: ");

    // TaskCreationOptions.*
    if ((stateFlags & 0x01) != 0) ExtOut("PreferFairness ");
    if ((stateFlags & 0x02) != 0) ExtOut("LongRunning ");
    if ((stateFlags & 0x04) != 0) ExtOut("AttachedToParent ");
    if ((stateFlags & 0x08) != 0) ExtOut("DenyChildAttach ");
    if ((stateFlags & 0x10) != 0) ExtOut("HideScheduler ");
    if ((stateFlags & 0x40) != 0) ExtOut("RunContinuationsAsynchronously ");

    // InternalTaskOptions.*
    if ((stateFlags & 0x0200) != 0) ExtOut("ContinuationTask ");
    if ((stateFlags & 0x0400) != 0) ExtOut("PromiseTask ");
    if ((stateFlags & 0x1000) != 0) ExtOut("LazyCancellation ");
    if ((stateFlags & 0x2000) != 0) ExtOut("QueuedByRuntime ");
    if ((stateFlags & 0x4000) != 0) ExtOut("DoNotDispose ");

    // TASK_STATE_*
    if ((stateFlags & 0x10000) != 0) ExtOut("STARTED ");
    if ((stateFlags & 0x20000) != 0) ExtOut("DELEGATE_INVOKED ");
    if ((stateFlags & 0x40000) != 0) ExtOut("DISPOSED ");
    if ((stateFlags & 0x80000) != 0) ExtOut("EXCEPTIONOBSERVEDBYPARENT ");
    if ((stateFlags & 0x100000) != 0) ExtOut("CANCELLATIONACKNOWLEDGED ");
    if ((stateFlags & 0x200000) != 0) ExtOut("FAULTED ");
    if ((stateFlags & 0x400000) != 0) ExtOut("CANCELED ");
    if ((stateFlags & 0x800000) != 0) ExtOut("WAITING_ON_CHILDREN ");
    if ((stateFlags & 0x1000000) != 0) ExtOut("RAN_TO_COMPLETION ");
    if ((stateFlags & 0x2000000) != 0) ExtOut("WAITINGFORACTIVATION ");
    if ((stateFlags & 0x4000000) != 0) ExtOut("COMPLETION_RESERVED ");
    if ((stateFlags & 0x8000000) != 0) ExtOut("THREAD_WAS_ABORTED ");
    if ((stateFlags & 0x10000000) != 0) ExtOut("WAIT_COMPLETION_NOTIFICATION ");
    if ((stateFlags & 0x20000000) != 0) ExtOut("EXECUTIONCONTEXT_IS_NULL ");
    if ((stateFlags & 0x40000000) != 0) ExtOut("TASKSCHEDULED_WAS_FIRED ");

    ExtOut("\n");
}

void ExtOutStateMachineFields(AsyncRecord& ar)
{
    DacpMethodTableData mtabledata;
    DacpMethodTableFieldData vMethodTableFields;
    if (mtabledata.Request(g_sos, ar.StateMachineMT) == S_OK &&
        vMethodTableFields.Request(g_sos, ar.StateMachineMT) == S_OK &&
        vMethodTableFields.wNumInstanceFields + vMethodTableFields.wNumStaticFields > 0)
    {
        DisplayFields(ar.StateMachineMT, &mtabledata, &vMethodTableFields, (DWORD_PTR)ar.StateMachineAddr, TRUE, ar.IsValueType);
    }
}

void FindStateMachineTypes(DWORD_PTR* corelibModule, mdTypeDef* stateMachineBox, mdTypeDef* debugStateMachineBox)
{
    int numModule;
    ArrayHolder<DWORD_PTR> moduleList = ModuleFromName(const_cast<LPSTR>("System.Private.CoreLib.dll"), &numModule);
    if (moduleList != NULL && numModule == 1)
    {
        *corelibModule = moduleList[0];
        GetInfoFromName(*corelibModule, "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1", stateMachineBox);
        GetInfoFromName(*corelibModule, "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+DebugFinalizableAsyncStateMachineBox`1", debugStateMachineBox);
    }
    else
    {
        *corelibModule = 0;
        *stateMachineBox = 0;
        *debugStateMachineBox = 0;
    }
}

DECLARE_API(DumpAsync)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        return E_FAIL;
    }

    try
    {
        // Process command-line arguments.
        size_t nArg = 0;
        TADDR mt = NULL, addr = NULL;
        ArrayHolder<char> ansiType = NULL;
        ArrayHolder<WCHAR> type = NULL;
        BOOL dml = FALSE, includeCompleted = FALSE, includeStacks = FALSE, includeRoots = FALSE, includeAllTasks = FALSE, dumpFields = FALSE;
        CMDOption option[] =
        {   // name, vptr, type, hasValue
            { "-addr", &addr, COHEX, TRUE },                // dump only the async object at the specified address
            { "-mt", &mt, COHEX, TRUE },                        // dump only async objects with a given MethodTable
            { "-type", &ansiType, COSTRING, TRUE },             // dump only async objects that contain the specified type substring
            { "-tasks", &includeAllTasks, COBOOL, FALSE },      // include all tasks that can be found on the heap, not just async methods
            { "-completed", &includeCompleted, COBOOL, FALSE }, // include async objects that are in a completed state
            { "-fields", &dumpFields, COBOOL, FALSE },          // show relevant fields of found async objects
            { "-stacks", &includeStacks, COBOOL, FALSE },       // gather and output continuation/stack information
            { "-roots", &includeRoots, COBOOL, FALSE },         // gather and output GC root information
#ifndef FEATURE_PAL
            { "/d", &dml, COBOOL, FALSE },                      // Debugger Markup Language
#endif
        };
        if (!GetCMDOption(args, option, _countof(option), NULL, 0, &nArg) || nArg != 0)
        {
            sos::Throw<sos::Exception>(
                "Usage: DumpAsync [-addr ObjectAddr] [-mt MethodTableAddr] [-type TypeName] [-tasks] [-completed] [-fields] [-stacks] [-roots]\n"
                "[-addr ObjectAddr]     => Only display the async object at the specified address.\n"
                "[-mt MethodTableAddr]  => Only display top-level async objects with the specified method table address.\n"
                "[-type TypeName]       => Only display top-level async objects whose type name includes the specified substring.\n"
                "[-tasks]               => Include Task and Task-derived objects, in addition to any state machine objects found.\n"
                "[-completed]           => Include async objects that represent completed operations but that are still on the heap.\n"
                "[-fields]              => Show the fields of state machines.\n"
                "[-stacks]              => Gather, output, and consolidate based on continuation chains / async stacks for discovered async objects.\n"
                "[-roots]               => Perform a gcroot on each rendered async object.\n"
                );
        }
        if (ansiType != NULL)
        {
            size_t ansiTypeLen = strlen(ansiType) + 1;
            type = new WCHAR[ansiTypeLen];
            MultiByteToWideChar(CP_ACP, 0, ansiType, -1, type, (int)ansiTypeLen);
        }
        
        EnableDMLHolder dmlHolder(dml);
        BOOL hasTypeFilter = mt != NULL || ansiType != NULL || addr != NULL;

        // Display a message if the heap isn't verified.
        sos::GCHeap gcheap;
        if (!gcheap.AreGCStructuresValid())
        {
            DisplayInvalidStructuresMessage();
        }

        // Find the state machine types
        DWORD_PTR corelibModule;
        mdTypeDef stateMachineBoxMd, debugStateMachineBoxMd;
        FindStateMachineTypes(&corelibModule, &stateMachineBoxMd, &debugStateMachineBoxMd);

        // Walk each heap object looking for async state machine objects.  As we're targeting .NET Core 2.1+, all such objects
        // will be Task or Task-derived types.
        std::map<CLRDATA_ADDRESS, AsyncRecord> asyncRecords;
        for (sos::ObjectIterator itr = gcheap.WalkHeap(); !IsInterrupt() && itr != NULL; ++itr)
        {
            // Skip objects too small to be state machines or tasks, avoiding some compiler-generated caching data structures.
            if (itr->GetSize() <= 24) 
            {
                continue;
            }

            // Match only async objects.
            if (includeAllTasks)
            {
                // If the user has selected to include all tasks and not just async state machine boxes, we simply need to validate
                // that this is Task or Task-derived, and if it's not, skip it.
                if (!IsDerivedFrom(itr->GetMT(), W("System.Threading.Tasks.Task")))
                {
                    continue;
                }
            }
            else
            {
                // Otherwise, we only care about AsyncStateMachineBox`1 as well as the DebugFinalizableAsyncStateMachineBox`1
                // that's used when certain ETW events are set.
                DacpMethodTableData mtdata;
                if (mtdata.Request(g_sos, TO_TADDR(itr->GetMT())) != S_OK ||
                    mtdata.Module != corelibModule ||
                    (mtdata.cl != stateMachineBoxMd && mtdata.cl != debugStateMachineBoxMd))
                {
                    continue;
                }
            }

            // Create an AsyncRecord to store the state for this instance.  We're likely going to keep the object at this point,
            // though we may still discard/skip it with a few checks later; to do that, though, we'll need some of the info
            // gathered here, so we construct the record to store the data.
            AsyncRecord ar;
            ar.Address = itr->GetAddress();
            ar.MT = itr->GetMT();
            ar.Size = (DWORD)itr->GetSize();
            ar.StateMachineAddr = itr->GetAddress();
            ar.StateMachineMT = itr->GetMT();
            ar.IsValueType = false;
            ar.IsTopLevel = true;
            ar.IsStateMachine = false;
            ar.TaskStateFlags = 0;
            ar.StateValue = 0;
            ar.FilteredByOptions = // we process all objects to support forming proper chains, but then only display ones that match the user's request
                (mt == NULL || mt == itr->GetMT()) && // Match only MTs the user requested.
                (type == NULL || _wcsstr(itr->GetTypeName(), type) != NULL) && // Match only type name substrings the user requested.
                (addr == NULL || addr == itr->GetAddress()); // Match only the object at the specified address.

            // Get the state flags for the task.  This is used to determine whether async objects are completed (and thus should
            // be culled by default).  It avoids our needing to depend on interpreting the compiler's "<>1__state" field, and also lets
            // us display state information for non-async state machine objects.
            DacpFieldDescData stateFlagsField;
            int offset = GetObjFieldOffset(ar.Address, ar.MT, W("m_stateFlags"), TRUE, &stateFlagsField);
            if (offset != 0)
            {
                MOVE(ar.TaskStateFlags, ar.Address + offset);
            }

            // Get the async state machine object's StateMachine field.
            DacpFieldDescData stateMachineField;
            int stateMachineFieldOffset = GetObjFieldOffset(TO_CDADDR(itr->GetAddress()), itr->GetMT(), W("StateMachine"), TRUE, &stateMachineField);
            if (stateMachineFieldOffset != 0)
            {
                ar.IsStateMachine = true;
                ar.IsValueType = stateMachineField.Type == ELEMENT_TYPE_VALUETYPE;

                // Get the address and method table of the state machine.  While it'll generally be a struct, it is valid for it to be a
                // class (the C# compiler generates a class in debug builds to better support Edit-And-Continue), so we accommodate both.
                DacpFieldDescData stateField;
                int stateFieldOffset = -1;
                if (ar.IsValueType)
                {
                    ar.StateMachineAddr = itr->GetAddress() + stateMachineFieldOffset;
                    ar.StateMachineMT = stateMachineField.MTOfType;
                    stateFieldOffset = GetValueFieldOffset(ar.StateMachineMT, W("<>1__state"), &stateField);
                }
                else
                {
                    MOVE(ar.StateMachineAddr, itr->GetAddress() + stateMachineFieldOffset);
                    DacpObjectData objData;
                    if (objData.Request(g_sos, ar.StateMachineAddr) == S_OK)
                    {
                        ar.StateMachineMT = objData.MethodTable; // update from Canon to actual type
                        stateFieldOffset = GetObjFieldOffset(ar.StateMachineAddr, ar.StateMachineMT, W("<>1__state"), TRUE, &stateField);
                    }
                }

                if (stateFieldOffset >= 0 && (ar.IsValueType || stateFieldOffset != 0))
                {
                    MOVE(ar.StateValue, ar.StateMachineAddr + stateFieldOffset);
                }
            }

            // If we only want to include incomplete async objects, skip this one if it's completed.
            if (!includeCompleted && AsyncRecordIsCompleted(ar))
            {
                continue;
            }

            // If the user has asked to include "async stacks" information, resolve any continuation
            // that might be registered with it.  This could be a single continuation, or it could
            // be a list of continuations in the case of the same task being awaited multiple times.
            CLRDATA_ADDRESS nextAddr;
            if (includeStacks && TryGetContinuation(itr->GetAddress(), itr->GetMT(), &nextAddr))
            {
                sos::Object contObj = TO_TADDR(nextAddr);
                if (_wcsncmp(contObj.GetTypeName(), W("System.Collections.Generic.List`1"), 33) == 0)
                {
                    // The continuation is a List<object>.  Iterate through its internal object[]
                    // looking for non-null objects, and adding each one as a continuation.
                    int itemsOffset = GetObjFieldOffset(contObj.GetAddress(), contObj.GetMT(), W("_items"));
                    if (itemsOffset != 0)
                    {
                        DWORD_PTR listItemsPtr;
                        MOVE(listItemsPtr, contObj.GetAddress() + itemsOffset);
                        if (sos::IsObject(listItemsPtr, false))
                        {
                            DacpObjectData objData;
                            if (objData.Request(g_sos, TO_CDADDR(listItemsPtr)) == S_OK && objData.ObjectType == OBJ_ARRAY)
                            {
                                for (SIZE_T i = 0; i < objData.dwNumComponents; i++)
                                {
                                    CLRDATA_ADDRESS elementPtr;
                                    MOVE(elementPtr, TO_CDADDR(objData.ArrayDataPtr + (i * objData.dwComponentSize)));
                                    if (elementPtr != NULL && sos::IsObject(elementPtr, false))
                                    {
                                        ResolveContinuation(&elementPtr);
                                        ar.Continuations.push_back(elementPtr);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ar.Continuations.push_back(contObj.GetAddress());
                }
            }

            // We've gathered all of the needed information for this heap object.  Add it to our list of async records.
            asyncRecords.insert(std::pair<CLRDATA_ADDRESS, AsyncRecord>(ar.Address, ar));
        }

        // As with DumpHeap, output a summary table about all of the objects we found.  In contrast, though, his is based on the filtered
        // list of async records we gathered rather than everything on the heap.
        if (addr == NULL) // no point in stats if we're only targeting a single object
        {
            HeapStat stats;
            for (std::map<CLRDATA_ADDRESS, AsyncRecord>::iterator arIt = asyncRecords.begin(); arIt != asyncRecords.end(); ++arIt)
            {
                if (!hasTypeFilter || arIt->second.FilteredByOptions)
                {
                    stats.Add((DWORD_PTR)arIt->second.MT, (DWORD)arIt->second.Size);
                }
            }
            stats.Sort();
            stats.Print();
        }

        // If the user has asked for "async stacks" and if there's not MT/type name filter, look through all of our async records
        // to find the "top-level" nodes that start rather than that are a part of a continuation chain.  When we then iterate through
        // async records, we only print ones out that are still classified as top-level.  We don't do this if there's a type filter
        // because in that case we consider those and only those objects to be top-level.
        if (includeStacks && !hasTypeFilter)
        {
            size_t uniqueChains = asyncRecords.size();
            for (std::map<CLRDATA_ADDRESS, AsyncRecord>::iterator arIt = asyncRecords.begin(); arIt != asyncRecords.end(); ++arIt)
            {
                for (std::vector<CLRDATA_ADDRESS>::iterator contIt = arIt->second.Continuations.begin(); contIt != arIt->second.Continuations.end(); ++contIt)
                {
                    std::map<CLRDATA_ADDRESS, AsyncRecord>::iterator found = asyncRecords.find(*contIt);
                    if (found != asyncRecords.end())
                    {
                        if (found->second.IsTopLevel)
                        {
                            found->second.IsTopLevel = false;
                            uniqueChains--;
                        }
                    }
                }
            }

            ExtOut("In %d chains.\n", uniqueChains);
        }

        // Print out header for the main line of each result.
        ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %8s ", "Address", "MT", "Size");
        if (includeCompleted) ExtOut("%8s ", "Status");
        ExtOut("%10s %s\n", "State", "Description");

        // Output each top-level async record.
        int counter = 0;
        for (std::map<CLRDATA_ADDRESS, AsyncRecord>::iterator arIt = asyncRecords.begin(); arIt != asyncRecords.end(); ++arIt)
        {
            if (!arIt->second.IsTopLevel || (hasTypeFilter && !arIt->second.FilteredByOptions))
            {
                continue;
            }

            // Output the state machine's details as a single line.
            sos::Object obj = TO_TADDR(arIt->second.Address);
            if (arIt->second.IsStateMachine)
            {
                // This has a StateMachine.  Output its details.
                sos::MethodTable mt = TO_TADDR(arIt->second.StateMachineMT);
                DMLOut("%s %s %8d ", DMLAsync(obj.GetAddress()), DMLDumpHeapMT(obj.GetMT()), obj.GetSize());
                if (includeCompleted) ExtOut("%8s ", GetAsyncRecordStatusDescription(arIt->second));
                ExtOut("%10d %S\n", arIt->second.StateValue, mt.GetName());
                if (dumpFields) ExtOutStateMachineFields(arIt->second);
            }
            else
            {
                // This does not have a StateMachine.  Output the details of the Task itself.
                DMLOut("%s %s %8d ", DMLAsync(obj.GetAddress()), DMLDumpHeapMT(obj.GetMT()), obj.GetSize());
                if (includeCompleted) ExtOut("%8s ", GetAsyncRecordStatusDescription(arIt->second));
                ExtOut("[%08x] %S ", arIt->second.TaskStateFlags, obj.GetTypeName());
                ExtOutTaskDelegateMethod(obj);
                ExtOut("\n");
                if (dumpFields) ExtOutTaskStateFlagsDescription(arIt->second.TaskStateFlags);
            }

            // If we gathered any continuations for this record, output the chains now.
            if (includeStacks && arIt->second.Continuations.size() > 0)
            {
                ExtOut(includeAllTasks ? "Continuation chains:\n" : "Async \"stack\":\n");
                std::vector<std::pair<int, CLRDATA_ADDRESS>> continuationChainToExplore;
                continuationChainToExplore.push_back(std::pair<int, CLRDATA_ADDRESS>(1, obj.GetAddress()));

                // Do a depth-first traversal of continuations, outputting each continuation found and then
                // looking in our gathered objects list for its continuations.
                std::set<CLRDATA_ADDRESS> seen;
                while (continuationChainToExplore.size() > 0)
                {
                    // Pop the next continuation from the stack.
                    std::pair<int, CLRDATA_ADDRESS> cur = continuationChainToExplore.back();
                    continuationChainToExplore.pop_back();

                    // Get the async record for this continuation.  It should be one we already know about.
                    std::map<CLRDATA_ADDRESS, AsyncRecord>::iterator curAsyncRecord = asyncRecords.find(cur.second);
                    if (curAsyncRecord == asyncRecords.end())
                    {
                        continue;
                    }

                    // Make sure to avoid cycles in the rare case where async records may refer to each other.
                    if (seen.find(cur.second) != seen.end())
                    {
                        continue;
                    }
                    seen.insert(cur.second);

                    // Iterate through all continuations from this object.
                    for (std::vector<CLRDATA_ADDRESS>::iterator contIt = curAsyncRecord->second.Continuations.begin(); contIt != curAsyncRecord->second.Continuations.end(); ++contIt)
                    {
                        sos::Object cont = TO_TADDR(*contIt);

                        // Print out the depth of the continuation with dots, then its address.
                        for (int i = 0; i < cur.first; i++) ExtOut(".");
                        DMLOut("%s ", DMLObject(cont.GetAddress()));

                        // Print out the name of the method for this task's delegate if it has one (state machines won't, but others tasks may).
                        ExtOutTaskDelegateMethod(cont);

                        // Find the async record for this continuation, and output its name.  If it's a state machine,
                        // also output its current state value so that a user can see at a glance its status.
                        std::map<CLRDATA_ADDRESS, AsyncRecord>::iterator contAsyncRecord = asyncRecords.find(cont.GetAddress());
                        if (contAsyncRecord != asyncRecords.end())
                        {
                            sos::MethodTable contMT = TO_TADDR(contAsyncRecord->second.StateMachineMT);
                            if (contAsyncRecord->second.IsStateMachine) ExtOut("(%d) ", contAsyncRecord->second.StateValue);
                            ExtOut("%S\n", contMT.GetName());
                            if (contAsyncRecord->second.IsStateMachine && dumpFields) ExtOutStateMachineFields(contAsyncRecord->second);
                        }
                        else
                        {
                            ExtOut("%S\n", cont.GetTypeName());
                        }

                        // Add this continuation to the stack to explore.
                        continuationChainToExplore.push_back(std::pair<int, CLRDATA_ADDRESS>(cur.first + 1, *contIt));
                    }
                }
            }

            // Finally, output gcroots, as they can serve as alternative/more detailed "async stacks", and also help to highlight
            // state machines that aren't being kept alive.  However, they're more expensive to compute, so they're opt-in.
            if (includeRoots)
            {
                ExtOut("GC roots:\n");
                IncrementIndent();
                GCRootImpl gcroot;
                int numRoots = gcroot.PrintRootsForObject(obj.GetAddress(), FALSE, FALSE);
                DecrementIndent();
                if (numRoots == 0 && !AsyncRecordIsCompleted(arIt->second))
                {
                    ExtOut("Incomplete state machine or task with 0 roots.\n");
                }
            }

            // If we're rendering more than one line per entry, output a separator to help distinguish the entries.
            if (dumpFields || includeStacks || includeRoots)
            {
                ExtOut("--------------------------------------------------------------------------------\n");
            }
        }

        return S_OK;
    }
    catch (const sos::Exception &e)
    {
        ExtOut("%s\n", e.what());
        return E_FAIL;
    }
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function dumps all objects on GC heap. It also displays      *  
*    statistics of objects.  If GC heap is corrupted, it will stop at 
*    the bad place.  (May not work if GC is in progress.)              *
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpHeap)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        return E_FAIL;
    }

    try
    {
        DumpHeapImpl dumpHeap(args);
        dumpHeap.Run();

        return S_OK;
    }
    catch(const sos::Exception &e)
    {
        ExtOut("%s\n", e.what());
        return E_FAIL;
    }
}

DECLARE_API(VerifyHeap)
{    
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    
    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        return E_FAIL;
    }
    
    try
    {
        bool succeeded = true;
        char buffer[1024];
        sos::GCHeap gcheap;
        sos::ObjectIterator itr = gcheap.WalkHeap();

        while (itr)
        {
            if (itr.Verify(buffer, _countof(buffer)))
            {
                ++itr;
            }
            else
            {
                succeeded = false;
                ExtOut(buffer);
                itr.MoveToNextObjectCarefully();
            }
        }

        if (!DumpHeapImpl::ValidateSyncTable(gcheap))
            succeeded = false;

        if (succeeded)
            ExtOut("No heap corruption detected.\n");

        return S_OK;
    }
    catch(const sos::Exception &e)
    {
        ExtOut("%s\n", e.what());
        return E_FAIL;
    }
}

#ifndef FEATURE_PAL

enum failure_get_memory
{
    fgm_no_failure = 0,
    fgm_reserve_segment = 1,
    fgm_commit_segment_beg = 2,
    fgm_commit_eph_segment = 3,
    fgm_grow_table = 4,
    fgm_commit_table = 5
};

enum oom_reason
{
    oom_no_failure = 0,
    oom_budget = 1,
    oom_cant_commit = 2,
    oom_cant_reserve = 3,
    oom_loh = 4,
    oom_low_mem = 5,
    oom_unproductive_full_gc = 6
};

static const char *const str_oom[] = 
{
    "There was no managed OOM due to allocations on the GC heap", // oom_no_failure 
    "This is likely to be a bug in GC", // oom_budget
    "Didn't have enough memory to commit", // oom_cant_commit
    "This is likely to be a bug in GC", // oom_cant_reserve 
    "Didn't have enough memory to allocate an LOH segment", // oom_loh 
    "Low on memory during GC", // oom_low_mem 
    "Could not do a full GC" // oom_unproductive_full_gc
};

static const char *const str_fgm[] = 
{
    "There was no failure to allocate memory", // fgm_no_failure 
    "Failed to reserve memory", // fgm_reserve_segment
    "Didn't have enough memory to commit beginning of the segment", // fgm_commit_segment_beg
    "Didn't have enough memory to commit the new ephemeral segment", // fgm_commit_eph_segment
    "Didn't have enough memory to grow the internal GC data structures", // fgm_grow_table
    "Didn't have enough memory to commit the internal GC data structures", // fgm_commit_table
};

void PrintOOMInfo(DacpOomData* oomData)
{
    ExtOut("Managed OOM occurred after GC #%d (Requested to allocate %d bytes)\n", 
        oomData->gc_index, oomData->alloc_size);

    if ((oomData->reason == oom_budget) ||
        (oomData->reason == oom_cant_reserve))
    {
        // TODO: This message needs to be updated with more precious info.
        ExtOut("%s, please contact PSS\n", str_oom[oomData->reason]);
    }
    else
    {
        ExtOut("Reason: %s\n", str_oom[oomData->reason]);
    }

    // Now print out the more detailed memory info if any.
    if (oomData->fgm != fgm_no_failure)
    {
        ExtOut("Detail: %s: %s (%d bytes)", 
            (oomData->loh_p ? "LOH" : "SOH"), 
            str_fgm[oomData->fgm],
            oomData->size);
    
        if ((oomData->fgm == fgm_commit_segment_beg) ||
            (oomData->fgm == fgm_commit_eph_segment) ||
            (oomData->fgm == fgm_grow_table) ||
            (oomData->fgm == fgm_commit_table))
        {
            // If it's a commit error (fgm_grow_table can indicate a reserve
            // or a commit error since we make one VirtualAlloc call to
            // reserve and commit), we indicate the available commit
            // space if we recorded it.
            if (oomData->available_pagefile_mb)
            {
                ExtOut(" - on GC entry available commit space was %d MB",
                    oomData->available_pagefile_mb);
            }
        }

        ExtOut("\n");
    }
}

DECLARE_API(AnalyzeOOM)
{    
    INIT_API();    
    MINIDUMP_NOT_SUPPORTED();    
    
#ifndef FEATURE_PAL

    if (!InitializeHeapData ())
    {
        ExtOut("GC Heap not initialized yet.\n");
        return S_OK;
    }

    BOOL bHasManagedOOM = FALSE;
    DacpOomData oomData;
    memset (&oomData, 0, sizeof(oomData));
    if (!IsServerBuild())
    {
        if (oomData.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting OOM data\n");
            return E_FAIL;
        }
        if (oomData.reason != oom_no_failure)
        {
            bHasManagedOOM = TRUE;
            PrintOOMInfo(&oomData);
        }
    }
    else
    {   
        DWORD dwNHeaps = GetGcHeapCount();
        DWORD dwAllocSize;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtOut("Failed to get GCHeaps:  integer overflow\n");
            return Status;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtOut("Failed to get GCHeaps\n");
            return Status;
        }
        
        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            if (oomData.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtOut("Heap %d: Error requesting OOM data\n", n);
                return E_FAIL;
            }
            if (oomData.reason != oom_no_failure)
            {
                if (!bHasManagedOOM)
                {
                    bHasManagedOOM = TRUE;
                }
                ExtOut("---------Heap %#-2d---------\n", n);
                PrintOOMInfo(&oomData);
            }
        }
    }

    if (!bHasManagedOOM)
    {
        ExtOut("%s\n", str_oom[oomData.reason]);
    }

    return S_OK;
#else
    _ASSERTE(false);
    return E_FAIL;
#endif // FEATURE_PAL
}

DECLARE_API(VerifyObj)
{
    INIT_API();    
    MINIDUMP_NOT_SUPPORTED();

    TADDR  taddrObj = 0;
    TADDR  taddrMT;
    size_t objSize;

    BOOL bValid = FALSE;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&taddrObj, COHEX}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    BOOL bContainsPointers;

    if (FAILED(GetMTOfObject(taddrObj, &taddrMT)) ||
        !GetSizeEfficient(taddrObj, taddrMT, FALSE, objSize, bContainsPointers))
    {
        ExtOut("object %#p does not have valid method table\n", SOS_PTR(taddrObj));
        goto Exit;
    }

    // we need to build g_snapshot as it is later used in GetGeneration
    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        goto Exit;
    }
    {
        DacpGcHeapDetails *pheapDetails = g_snapshot.GetHeap(taddrObj);
        bValid = VerifyObject(*pheapDetails, taddrObj, taddrMT, objSize, TRUE);
    }

Exit:
    if (bValid)
    {
        ExtOut("object %#p is a valid object\n", SOS_PTR(taddrObj));
    }

    return Status;
}

void LNODisplayOutput(LPCWSTR tag, TADDR pMT, TADDR currentObj, size_t size) 
{
    sos::Object obj(currentObj, pMT);
    DMLOut("%S %s %12d (0x%x)\t%S\n", tag, DMLObject(currentObj), size, size, obj.GetTypeName());
}

DECLARE_API(ListNearObj)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

#if !defined(FEATURE_PAL)

    TADDR taddrArg = 0;
    TADDR taddrObj = 0;
    // we may want to provide a more exact version of searching for the 
    // previous object in the heap, using the brick table, instead of 
    // looking for what may be valid method tables...
    //BOOL bExact;
    //CMDOption option[] = 
    //{
    //    // name, vptr, type, hasValue
    //    {"-exact", &bExact, COBOOL, FALSE}
    //};

    BOOL dml = FALSE;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {
        // vptr, type
        {&taddrArg, COHEX}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg) || nArg != 1)
    {
        ExtOut("Usage: !ListNearObj <obj_address>\n");
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);

    if (!g_snapshot.Build())
    {
        ExtOut("Unable to build snapshot of the garbage collector state\n");
        return Status;    
    }

    taddrObj = Align(taddrArg);

    DacpGcHeapDetails *heap = g_snapshot.GetHeap(taddrArg);
    if (heap == NULL)
    {
        ExtOut("Address %p does not lie in the managed heap\n", SOS_PTR(taddrObj));
        return Status;
    }

    TADDR_SEGINFO trngSeg  = {0, 0, 0};
    TADDR_RANGE   allocCtx = {0, 0};
    BOOL          bLarge;
    int           gen;
    if (!GCObjInHeap(taddrObj, *heap, trngSeg, gen, allocCtx, bLarge))
    {
        ExtOut("Failed to find the segment of the managed heap where the object %p resides\n", 
            SOS_PTR(taddrObj));
        return Status;
    }

    TADDR  objMT = NULL;
    size_t objSize = 0;
    BOOL   bObj    = FALSE;
    TADDR  taddrCur;
    TADDR  curMT   = 0;
    size_t curSize = 0;
    BOOL   bCur    = FALSE;
    TADDR  taddrNxt;
    TADDR  nxtMT   = 0;
    size_t nxtSize = 0;
    BOOL   bNxt    = FALSE;
    BOOL   bContainsPointers;

    std::vector<TADDR> candidate;
    candidate.reserve(10);

    // since we'll be reading back I'll prime the read cache to a buffer before the current address
    MOVE(taddrCur, _max(trngSeg.start, taddrObj-DT_OS_PAGE_SIZE));

    // ===== Look for a good candidate preceeding taddrObj

    for (taddrCur = taddrObj - sizeof(TADDR); taddrCur >= trngSeg.start; taddrCur -= sizeof(TADDR))
    {
        // currently we don't pay attention to allocation contexts.  if this
        // proves to be an issue we need to reconsider the code below
        if (SUCCEEDED(GetMTOfObject(taddrCur, &curMT)) &&
            GetSizeEfficient(taddrCur, curMT, bLarge, curSize, bContainsPointers))
        {
            // remember this as one of the possible "good" objects preceeding taddrObj
            candidate.push_back(taddrCur);

            std::vector<TADDR>::iterator it = 
                std::find(candidate.begin(), candidate.end(), taddrCur+curSize);
            if (it != candidate.end())
            {
                // We found a chain of two objects preceeding taddrObj.  We'll
                // trust this is a good indication that the two objects are valid.
                // What is not valid is possibly the object following the second 
                // one...
                taddrCur = *it;
                GetMTOfObject(taddrCur, &curMT);
                GetSizeEfficient(taddrCur, curMT, bLarge, curSize, bContainsPointers);
                bCur = TRUE;
                break;
            }
        }
    }

    if (!bCur && !candidate.empty())
    {
        // pick the closest object to taddrObj
        taddrCur = *(candidate.begin());
        GetMTOfObject(taddrCur, &curMT);
        GetSizeEfficient(taddrCur, curMT, bLarge, curSize, bContainsPointers);
        // we have a candidate, even if not confirmed
        bCur = TRUE;
    }

    taddrNxt = taddrObj;
    if (taddrArg == taddrObj) 
    {
        taddrNxt += sizeof(TADDR);
    }

    // ===== Now look at taddrObj
    if (taddrObj == taddrArg) 
    {
        // only look at taddrObj if it's the same as what user passed in, meaning it's aligned.  
        if (SUCCEEDED(GetMTOfObject(taddrObj, &objMT)) &&
            GetSizeEfficient(taddrObj, objMT, bLarge, objSize, bContainsPointers))
        {
            bObj = TRUE;
            taddrNxt = taddrObj+objSize;
        }
    }

    if ((taddrCur + curSize > taddrArg) && taddrCur + curSize < trngSeg.end)
    {
        if (SUCCEEDED(GetMTOfObject(taddrCur + curSize, &nxtMT)) &&
            GetSizeEfficient(taddrObj, objMT, bLarge, objSize, bContainsPointers))
        {
            taddrNxt = taddrCur+curSize;
        }
    }

    // ===== And finally move on to elements following taddrObj
    
    for (; taddrNxt < trngSeg.end; taddrNxt += sizeof(TADDR))
    {
        if (SUCCEEDED(GetMTOfObject(taddrNxt, &nxtMT)) &&
            GetSizeEfficient(taddrNxt, nxtMT, bLarge, nxtSize, bContainsPointers))
        {
            bNxt = TRUE;
            break;
        }
    }

    if (bCur)
        LNODisplayOutput(W("Before: "), curMT, taddrCur, curSize);
    else
        ExtOut("Before: couldn't find any object between %#p and %#p\n",
            SOS_PTR(trngSeg.start), SOS_PTR(taddrArg));

    if (bObj)
        LNODisplayOutput(W("Current:"), objMT, taddrObj, objSize);

    if (bNxt)
        LNODisplayOutput(W("After:  "), nxtMT, taddrNxt, nxtSize);
    else
        ExtOut("After:  couldn't find any object between %#p and %#p\n",
            SOS_PTR(taddrArg), SOS_PTR(trngSeg.end));

    if (bCur && bNxt && 
        (((taddrCur+curSize == taddrObj) && (taddrObj+objSize == taddrNxt)) || (taddrCur+curSize == taddrNxt)))
    {
        ExtOut("Heap local consistency confirmed.\n");
    }
    else
    {
        ExtOut("Heap local consistency not confirmed.\n");
    }

    return Status;    

#else

    _ASSERTE(false);
    return E_FAIL;

#endif // FEATURE_PAL
}


DECLARE_API(GCHeapStat)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    

#ifndef FEATURE_PAL

    BOOL bIncUnreachable = FALSE;
    BOOL dml = FALSE;

    CMDOption option[] = {
        // name, vptr, type, hasValue
        {"-inclUnrooted", &bIncUnreachable, COBOOL, FALSE},
        {"-iu",           &bIncUnreachable, COBOOL, FALSE},
        {"/d",            &dml, COBOOL, FALSE}
    };
    
    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    ExtOut("%-8s %12s %12s %12s %12s\n", "Heap", "Gen0", "Gen1", "Gen2", "LOH");

    if (!IsServerBuild())
    {
        float tempf;
        DacpGcHeapDetails heapDetails;
        if (heapDetails.Request(g_sos) != S_OK)
        {
            ExtErr("Error requesting gc heap details\n");
            return Status;
        }

        HeapUsageStat hpUsage;
        if (GCHeapUsageStats(heapDetails, bIncUnreachable, &hpUsage))
        {
            ExtOut("Heap%-4d %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u\n", 0, 
                hpUsage.genUsage[0].allocd, hpUsage.genUsage[1].allocd, 
                hpUsage.genUsage[2].allocd, hpUsage.genUsage[3].allocd);
            ExtOut("\nFree space:                                                  Percentage\n");
            ExtOut("Heap%-4d %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u ", 0, 
                hpUsage.genUsage[0].freed, hpUsage.genUsage[1].freed, 
                hpUsage.genUsage[2].freed, hpUsage.genUsage[3].freed);
            tempf = ((float)(hpUsage.genUsage[0].freed+hpUsage.genUsage[1].freed+hpUsage.genUsage[2].freed)) /
                (hpUsage.genUsage[0].allocd+hpUsage.genUsage[1].allocd+hpUsage.genUsage[2].allocd);
            ExtOut("SOH:%3d%% LOH:%3d%%\n", (int)(100 * tempf), 
                (int)(100*((float)hpUsage.genUsage[3].freed) / (hpUsage.genUsage[3].allocd)));
            if (bIncUnreachable)
            {
            ExtOut("\nUnrooted objects:                                            Percentage\n");
            ExtOut("Heap%-4d %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u ", 0, 
                hpUsage.genUsage[0].unrooted, hpUsage.genUsage[1].unrooted, 
                hpUsage.genUsage[2].unrooted, hpUsage.genUsage[3].unrooted);
            tempf = ((float)(hpUsage.genUsage[0].unrooted+hpUsage.genUsage[1].unrooted+hpUsage.genUsage[2].unrooted)) / 
                (hpUsage.genUsage[0].allocd+hpUsage.genUsage[1].allocd+hpUsage.genUsage[2].allocd);
            ExtOut("SOH:%3d%% LOH:%3d%%\n", (int)(100 * tempf),
                (int)(100*((float)hpUsage.genUsage[3].unrooted) / (hpUsage.genUsage[3].allocd)));
            }
        }
    }
    else
    {
        float tempf;
        DacpGcHeapData gcheap;
        if (gcheap.Request(g_sos) != S_OK)
        {
            ExtErr("Error requesting GC Heap data\n");
            return Status;
        }

        DWORD dwAllocSize;
        DWORD dwNHeaps = gcheap.HeapCount;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtErr("Failed to get GCHeaps:  integer overflow\n");
            return Status;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtErr("Failed to get GCHeaps\n");
            return Status;
        }

        ArrayHolder<HeapUsageStat> hpUsage = new NOTHROW HeapUsageStat[dwNHeaps];
        if (hpUsage == NULL)
        {
            ReportOOM();
            return Status;
        }

        // aggregate stats across heaps / generation
        GenUsageStat genUsageStat[4] = {0, 0, 0, 0};

        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            DacpGcHeapDetails heapDetails;
            if (heapDetails.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtErr("Error requesting gc heap details\n");
                return Status;
            }

            if (GCHeapUsageStats(heapDetails, bIncUnreachable, &hpUsage[n]))
            {
                for (int i = 0; i < 4; ++i)
                {
                    genUsageStat[i].allocd   += hpUsage[n].genUsage[i].allocd;
                    genUsageStat[i].freed    += hpUsage[n].genUsage[i].freed;
                    if (bIncUnreachable)
                    {
                    genUsageStat[i].unrooted += hpUsage[n].genUsage[i].unrooted;
                    }
                }
            }
        }

        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            ExtOut("Heap%-4d %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u\n", n, 
                hpUsage[n].genUsage[0].allocd, hpUsage[n].genUsage[1].allocd, 
                hpUsage[n].genUsage[2].allocd, hpUsage[n].genUsage[3].allocd);
        }
        ExtOut("Total    %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u\n",
            genUsageStat[0].allocd, genUsageStat[1].allocd, 
            genUsageStat[2].allocd, genUsageStat[3].allocd);

        ExtOut("\nFree space:                                                  Percentage\n");
        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            ExtOut("Heap%-4d %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u ", n, 
                hpUsage[n].genUsage[0].freed, hpUsage[n].genUsage[1].freed, 
                hpUsage[n].genUsage[2].freed, hpUsage[n].genUsage[3].freed);

            tempf = ((float)(hpUsage[n].genUsage[0].freed+hpUsage[n].genUsage[1].freed+hpUsage[n].genUsage[2].freed)) /
                (hpUsage[n].genUsage[0].allocd+hpUsage[n].genUsage[1].allocd+hpUsage[n].genUsage[2].allocd);
            ExtOut("SOH:%3d%% LOH:%3d%%\n", (int)(100 * tempf), 
                (int)(100*((float)hpUsage[n].genUsage[3].freed) / (hpUsage[n].genUsage[3].allocd))
            );
        }
        ExtOut("Total    %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u\n",
            genUsageStat[0].freed, genUsageStat[1].freed, 
            genUsageStat[2].freed, genUsageStat[3].freed);

        if (bIncUnreachable)
        {
            ExtOut("\nUnrooted objects:                                            Percentage\n");
            for (DWORD n = 0; n < dwNHeaps; n ++)
            {
                ExtOut("Heap%-4d %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u ", n, 
                    hpUsage[n].genUsage[0].unrooted, hpUsage[n].genUsage[1].unrooted, 
                    hpUsage[n].genUsage[2].unrooted, hpUsage[n].genUsage[3].unrooted);

                tempf = ((float)(hpUsage[n].genUsage[0].unrooted+hpUsage[n].genUsage[1].unrooted+hpUsage[n].genUsage[2].unrooted)) / 
                    (hpUsage[n].genUsage[0].allocd+hpUsage[n].genUsage[1].allocd+hpUsage[n].genUsage[2].allocd);
                ExtOut("SOH:%3d%% LOH:%3d%%\n", (int)(100 * tempf),
                    (int)(100*((float)hpUsage[n].genUsage[3].unrooted) / (hpUsage[n].genUsage[3].allocd)));
            }
            ExtOut("Total    %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u %12" POINTERSIZE_TYPE "u\n",
                genUsageStat[0].unrooted, genUsageStat[1].unrooted, 
                genUsageStat[2].unrooted, genUsageStat[3].unrooted);
        }

    }

    return Status;
    
#else

    _ASSERTE(false);
    return E_FAIL;

#endif // FEATURE_PAL
}

#endif // FEATURE_PAL

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function dumps what is in the syncblock cache.  By default   *  
*    it dumps all active syncblocks.  Using -all to dump all syncblocks
*                                                                      *
\**********************************************************************/
DECLARE_API(SyncBlk)
{
    INIT_API();    
    MINIDUMP_NOT_SUPPORTED();    
    
    BOOL bDumpAll = FALSE;
    size_t nbAsked = 0;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-all", &bDumpAll, COBOOL, FALSE},
        {"/d", &dml, COBOOL, FALSE}
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&nbAsked, COSIZE_T}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    DacpSyncBlockData syncBlockData;
    if (syncBlockData.Request(g_sos,1) != S_OK)
    {
        ExtOut("Error requesting SyncBlk data\n");
        return Status;
    }

    DWORD dwCount = syncBlockData.SyncBlockCount;
    
    ExtOut("Index" WIN64_8SPACES " SyncBlock MonitorHeld Recursion Owning Thread Info" WIN64_8SPACES "  SyncBlock Owner\n");
    ULONG freeCount = 0;
    ULONG CCWCount = 0;
    ULONG RCWCount = 0;
    ULONG CFCount = 0;
    for (DWORD nb = 1; nb <= dwCount; nb++)
    {
        if (IsInterrupt())
            return Status;
        
        if (nbAsked && nb != nbAsked) 
        {
            continue;
        }

        if (syncBlockData.Request(g_sos,nb) != S_OK)
        {
            ExtOut("SyncBlock %d is invalid%s\n", nb,
                (nb != nbAsked) ? ", continuing..." : "");
            continue;
        }

        BOOL bPrint = (bDumpAll || nb == nbAsked || (syncBlockData.MonitorHeld > 0 && !syncBlockData.bFree));

        if (bPrint)
        {
            ExtOut("%5d ", nb);
            if (!syncBlockData.bFree || nb != nbAsked)
            {            
                ExtOut("%p  ", syncBlockData.SyncBlockPointer); 
                ExtOut("%11d ", syncBlockData.MonitorHeld);
                ExtOut("%9d ", syncBlockData.Recursion);
                ExtOut("%p ", syncBlockData.HoldingThread);

                if (syncBlockData.HoldingThread == ~0ul)
                {
                    ExtOut(" orphaned ");
                }
                else if (syncBlockData.HoldingThread != NULL)
                {
                    DacpThreadData Thread;
                    if ((Status = Thread.Request(g_sos, syncBlockData.HoldingThread)) != S_OK)
                    {
                        ExtOut("Failed to request Thread at %p\n", syncBlockData.HoldingThread);
                        return Status;
                    }

                    DMLOut(DMLThreadID(Thread.osThreadId));
                    ULONG id;
                    if (g_ExtSystem->GetThreadIdBySystemId(Thread.osThreadId, &id) == S_OK)
                    {
                        ExtOut("%4d ", id);
                    }
                    else
                    {
                        ExtOut(" XXX ");
                    }
                }
                else
                {
                    ExtOut("    none  ");
                }

                if (syncBlockData.bFree)
                {
                    ExtOut("  %8d", 0);    // TODO: do we need to print the free synctable list?
                }
                else
                {
                    sos::Object obj = TO_TADDR(syncBlockData.Object);
                    DMLOut("  %s %S", DMLObject(syncBlockData.Object), obj.GetTypeName());
                }
            }
        }
                                    
        if (syncBlockData.bFree) 
        {
            freeCount ++;
            if (bPrint) {
                ExtOut(" Free");
            }
        }
        else 
        {
#ifdef FEATURE_COMINTEROP            
            if (syncBlockData.COMFlags) {
                switch (syncBlockData.COMFlags) {
                case SYNCBLOCKDATA_COMFLAGS_CCW:
                    CCWCount ++;
                    break;
                case SYNCBLOCKDATA_COMFLAGS_RCW:
                    RCWCount ++;
                    break;
                case SYNCBLOCKDATA_COMFLAGS_CF:
                    CFCount ++;
                    break;
                }
            }
#endif // FEATURE_COMINTEROP            
        }

        if (syncBlockData.MonitorHeld > 1)            
        {
            // TODO: implement this
            /*
            ExtOut(" ");
            DWORD_PTR pHead = (DWORD_PTR)vSyncBlock.m_Link.m_pNext;
            DWORD_PTR pNext = pHead;
            Thread vThread;
    
            while (1)
            {
                if (IsInterrupt())
                    return Status;
                DWORD_PTR pWaitEventLink = pNext - offsetLinkSB;
                WaitEventLink vWaitEventLink;
                vWaitEventLink.Fill(pWaitEventLink);
                if (!CallStatus) {
                    break;
                }
                DWORD_PTR dwAddr = (DWORD_PTR)vWaitEventLink.m_Thread;
                ExtOut("%x ", dwAddr);
                vThread.Fill (dwAddr);
                if (!CallStatus) {
                    break;
                }
                if (bPrint)
                    DMLOut("%s,", DMLThreadID(vThread.m_OSThreadId));
                pNext = (DWORD_PTR)vWaitEventLink.m_LinkSB.m_pNext;
                if (pNext == 0)
                    break;
            }  
            */
        }
        
        if (bPrint)
            ExtOut("\n");
    }
    
    ExtOut("-----------------------------\n");
    ExtOut("Total           %d\n", dwCount);
#ifdef FEATURE_COMINTEROP
    ExtOut("CCW             %d\n", CCWCount);
    ExtOut("RCW             %d\n", RCWCount);
    ExtOut("ComClassFactory %d\n", CFCount);
#endif
    ExtOut("Free            %d\n", freeCount);
   
    return Status;
}

#ifdef FEATURE_COMINTEROP
struct VisitRcwArgs
{
    BOOL bDetail;
    UINT MTACount;
    UINT STACount;
    ULONG FTMCount;
};

void VisitRcw(CLRDATA_ADDRESS RCW,CLRDATA_ADDRESS Context,CLRDATA_ADDRESS Thread, BOOL bIsFreeThreaded, LPVOID token)
{
    VisitRcwArgs *pArgs = (VisitRcwArgs *) token;

    if (pArgs->bDetail)
    {
        if (pArgs->MTACount == 0 && pArgs->STACount == 0 && pArgs->FTMCount == 0)
        {
            // First time, print a header
            ExtOut("RuntimeCallableWrappers (RCW) to be cleaned:\n");
            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s Apartment\n",
                "RCW", "CONTEXT", "THREAD");
        }        
        LPCSTR szThreadApartment;
        if (bIsFreeThreaded)
        {
            szThreadApartment = "(FreeThreaded)";
            pArgs->FTMCount++;
        }
        else if (Thread == NULL)
        {
            szThreadApartment = "(MTA)";
            pArgs->MTACount++;
        }
        else
        {
            szThreadApartment = "(STA)";
            pArgs->STACount++;
        }        
        
        ExtOut("%" POINTERSIZE "p %" POINTERSIZE "p %" POINTERSIZE "p %9s\n",
            SOS_PTR(RCW), 
            SOS_PTR(Context), 
            SOS_PTR(Thread),
            szThreadApartment);
    }
}

DECLARE_API(RCWCleanupList)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    DWORD_PTR p_CleanupList = GetExpression(args);

    VisitRcwArgs travArgs;
    ZeroMemory(&travArgs,sizeof(VisitRcwArgs));  
    travArgs.bDetail = TRUE;

    // We need to detect when !RCWCleanupList is called with an expression which evaluates to 0
    // (to print out an Invalid parameter message), but at the same time we need to allow an
    // empty argument list which would result in p_CleanupList equaling 0.
    if (p_CleanupList || strlen(args) == 0)
    {
        HRESULT hr = g_sos->TraverseRCWCleanupList(p_CleanupList, (VISITRCWFORCLEANUP)VisitRcw, &travArgs);
    
        if (SUCCEEDED(hr))
        {
            ExtOut("Free-Threaded Interfaces to be released: %d\n", travArgs.FTMCount);
            ExtOut("MTA Interfaces to be released: %d\n", travArgs.MTACount);
            ExtOut("STA Interfaces to be released: %d\n", travArgs.STACount);
        }
        else
        {
            ExtOut("An error occurred while traversing the cleanup list.\n");
        }
    }
    else
    {
        ExtOut("Invalid parameter %s\n", args);
    }
    
    return Status;
}
#endif // FEATURE_COMINTEROP

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of the finalizer     *
*    queue.                                                            *  
*                                                                      *
\**********************************************************************/
DECLARE_API(FinalizeQueue)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    BOOL bDetail = FALSE;
    BOOL bAllReady = FALSE;
    BOOL bShort    = FALSE;
    BOOL dml = FALSE;
    TADDR taddrMT  = 0;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-detail",   &bDetail,   COBOOL, FALSE},
        {"-allReady", &bAllReady, COBOOL, FALSE},
        {"-short",    &bShort,    COBOOL, FALSE},
        {"/d",        &dml,       COBOOL, FALSE},
        {"-mt",       &taddrMT,   COHEX,  TRUE},
    };

    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (!bShort)
    {
        DacpSyncBlockCleanupData dsbcd;
        CLRDATA_ADDRESS sbCurrent = NULL;
        ULONG cleanCount = 0;
        while ((dsbcd.Request(g_sos,sbCurrent) == S_OK) && dsbcd.SyncBlockPointer)
        {
            if (bDetail)
            {
                if (cleanCount == 0) // print first time only
                {
                    ExtOut("SyncBlocks to be cleaned by the finalizer thread:\n");
                    ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                        "SyncBlock", "RCW", "CCW", "ComClassFactory");                
                }
                
                ExtOut("%" POINTERSIZE "p %" POINTERSIZE "p %" POINTERSIZE "p %" POINTERSIZE "p\n", 
                    (ULONG64) dsbcd.SyncBlockPointer,
                    (ULONG64) dsbcd.blockRCW,
                    (ULONG64) dsbcd.blockCCW,
                    (ULONG64) dsbcd.blockClassFactory);
            }

            cleanCount++;
            sbCurrent = dsbcd.nextSyncBlock;
            if (sbCurrent == NULL)
            {
                break;
            }
        }

        ExtOut("SyncBlocks to be cleaned up: %d\n", cleanCount);

#ifdef FEATURE_COMINTEROP
        VisitRcwArgs travArgs;
        ZeroMemory(&travArgs,sizeof(VisitRcwArgs));  
        travArgs.bDetail = bDetail;
        g_sos->TraverseRCWCleanupList(0, (VISITRCWFORCLEANUP) VisitRcw, &travArgs);
        ExtOut("Free-Threaded Interfaces to be released: %d\n", travArgs.FTMCount);
        ExtOut("MTA Interfaces to be released: %d\n", travArgs.MTACount);
        ExtOut("STA Interfaces to be released: %d\n", travArgs.STACount);    
#endif // FEATURE_COMINTEROP    

// noRCW:
        ExtOut("----------------------------------\n");
    }

    // GC Heap
    DWORD dwNHeaps = GetGcHeapCount();

    HeapStat hpStat;

    if (!IsServerBuild())
    {
        DacpGcHeapDetails heapDetails;
        if (heapDetails.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting details\n");
            return Status;
        }

        GatherOneHeapFinalization(heapDetails, &hpStat, bAllReady, bShort);
    }
    else
    {   
        DWORD dwAllocSize;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtOut("Failed to get GCHeaps:  integer overflow\n");
            return Status;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtOut("Failed to get GCHeaps\n");
            return Status;
        }
        
        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            DacpGcHeapDetails heapDetails;
            if (heapDetails.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtOut("Error requesting details\n");
                return Status;
            }

            ExtOut("------------------------------\n");
            ExtOut("Heap %d\n", n);
            GatherOneHeapFinalization(heapDetails, &hpStat, bAllReady, bShort);
        }        
    }
    
    if (!bShort)
    {
        if (bAllReady)
        {
            PrintGCStat(&hpStat, "Statistics for all finalizable objects that are no longer rooted:\n");
        }
        else
        {
            PrintGCStat(&hpStat, "Statistics for all finalizable objects (including all objects ready for finalization):\n");
        }
    }

    return Status;
}

enum {
    // These are the values set in m_dwTransientFlags.
    // Note that none of these flags survive a prejit save/restore.

    M_CRST_NOTINITIALIZED       = 0x00000001,   // Used to prevent destruction of garbage m_crst
    M_LOOKUPCRST_NOTINITIALIZED = 0x00000002,

    SUPPORTS_UPDATEABLE_METHODS = 0x00000020,
    CLASSES_FREED               = 0x00000040,
    HAS_PHONY_IL_RVAS           = 0x00000080,
    IS_EDIT_AND_CONTINUE        = 0x00000200,
};

void ModuleMapTraverse(UINT index, CLRDATA_ADDRESS methodTable, LPVOID token)
{
    ULONG32 rid = (ULONG32)(size_t)token;
    NameForMT_s(TO_TADDR(methodTable), g_mdName, mdNameLen);

    DMLOut("%s 0x%08x %S\n", DMLMethodTable(methodTable), (ULONG32)TokenFromRid(rid, index), g_mdName);
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a Module          *
*    for a given address                                               *  
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpModule)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    
    DWORD_PTR p_ModuleAddr = NULL;
    BOOL bMethodTables = FALSE;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-mt", &bMethodTables, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE}
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&p_ModuleAddr, COHEX}
    };

    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (nArg != 1)
    {
        ExtOut("Usage: DumpModule [-mt] <Module Address>\n");
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    DacpModuleData module;
    if ((Status=module.Request(g_sos, TO_CDADDR(p_ModuleAddr)))!=S_OK)
    {
        ExtOut("Fail to fill Module %p\n", SOS_PTR(p_ModuleAddr));
        return Status;
    }
    
    WCHAR FileName[MAX_LONGPATH];
    FileNameForModule (&module, FileName);
    ExtOut("Name:       %S\n", FileName[0] ? FileName : W("Unknown Module"));

    ExtOut("Attributes: ");
    if (module.bIsPEFile)
        ExtOut("PEFile ");
    if (module.bIsReflection)
        ExtOut("Reflection ");
    if (module.dwTransientFlags & SUPPORTS_UPDATEABLE_METHODS)
        ExtOut("SupportsUpdateableMethods");
    ExtOut("\n");
    
    DMLOut("Assembly:   %s\n", DMLAssembly(module.Assembly));

    ExtOut("LoaderHeap:              %p\n", SOS_PTR(module.pLookupTableHeap));
    ExtOut("TypeDefToMethodTableMap: %p\n", SOS_PTR(module.TypeDefToMethodTableMap));
    ExtOut("TypeRefToMethodTableMap: %p\n", SOS_PTR(module.TypeRefToMethodTableMap));
    ExtOut("MethodDefToDescMap:      %p\n", SOS_PTR(module.MethodDefToDescMap));
    ExtOut("FieldDefToDescMap:       %p\n", SOS_PTR(module.FieldDefToDescMap));
    ExtOut("MemberRefToDescMap:      %p\n", SOS_PTR(module.MemberRefToDescMap));
    ExtOut("FileReferencesMap:       %p\n", SOS_PTR(module.FileReferencesMap));
    ExtOut("AssemblyReferencesMap:   %p\n", SOS_PTR(module.ManifestModuleReferencesMap));

    if (module.ilBase && module.metadataStart)
        ExtOut("MetaData start address:  %p (%d bytes)\n", SOS_PTR(module.metadataStart), module.metadataSize);

    if (bMethodTables)
    {
        ExtOut("\nTypes defined in this module\n\n");
        ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %s\n", "MT", "TypeDef", "Name");
                
        ExtOut("------------------------------------------------------------------------------\n");
        g_sos->TraverseModuleMap(TYPEDEFTOMETHODTABLE, TO_CDADDR(p_ModuleAddr), ModuleMapTraverse, (LPVOID)mdTypeDefNil);        

        ExtOut("\nTypes referenced in this module\n\n");
        ExtOut("%" POINTERSIZE "s   %" POINTERSIZE "s %s\n", "MT", "TypeRef", "Name");
        
        ExtOut("------------------------------------------------------------------------------\n");
        g_sos->TraverseModuleMap(TYPEREFTOMETHODTABLE, TO_CDADDR(p_ModuleAddr), ModuleMapTraverse, (LPVOID)mdTypeDefNil);     
    }
    
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a Domain          *
*    for a given address                                               *  
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpDomain)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    
    DWORD_PTR p_DomainAddr = 0;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&p_DomainAddr, COHEX},
    };
    size_t nArg;

    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);

    DacpAppDomainStoreData adsData;
    if ((Status=adsData.Request(g_sos))!=S_OK)
    {
        ExtOut("Unable to get AppDomain information\n");
        return Status;
    }
    
    if (p_DomainAddr)
    {
        DacpAppDomainData appDomain1;
        if ((Status=appDomain1.Request(g_sos, TO_CDADDR(p_DomainAddr)))!=S_OK)
        {
            ExtOut("Fail to fill AppDomain\n");
            return Status;
        }

        ExtOut("--------------------------------------\n");

        if (p_DomainAddr == adsData.sharedDomain)
        {
            DMLOut("Shared Domain:      %s\n", DMLDomain(adsData.sharedDomain));
        }
        else if (p_DomainAddr == adsData.systemDomain)
        {
            DMLOut("System Domain:      %s\n", DMLDomain(adsData.systemDomain));
        }
        else
        {
            DMLOut("Domain %d:%s          %s\n", appDomain1.dwId, (appDomain1.dwId >= 10) ? "" : " ", DMLDomain(p_DomainAddr));
        }

        DomainInfo(&appDomain1);
        return Status;
    }
        
    ExtOut("--------------------------------------\n");
    DMLOut("System Domain:      %s\n", DMLDomain(adsData.systemDomain));
    DacpAppDomainData appDomain;
    if ((Status=appDomain.Request(g_sos,adsData.systemDomain))!=S_OK)
    {
        ExtOut("Unable to get system domain info.\n");
        return Status;
    }
    DomainInfo(&appDomain);
    
    if (adsData.sharedDomain != NULL)
    {
        ExtOut("--------------------------------------\n");
        DMLOut("Shared Domain:      %s\n", DMLDomain(adsData.sharedDomain));
        if ((Status=appDomain.Request(g_sos, adsData.sharedDomain))!=S_OK)
        {
            ExtOut("Unable to get shared domain info\n");
            return Status;
        }
        DomainInfo(&appDomain);
    }

    ArrayHolder<CLRDATA_ADDRESS> pArray = new NOTHROW CLRDATA_ADDRESS[adsData.DomainCount];
    if (pArray==NULL)
    {
        ReportOOM();
        return Status;
    }

    if ((Status=g_sos->GetAppDomainList(adsData.DomainCount, pArray, NULL))!=S_OK)
    {
        ExtOut("Unable to get array of AppDomains\n");
        return Status;
    }

    for (int n=0;n<adsData.DomainCount;n++)
    {
        if (IsInterrupt())
            break;

        if ((Status=appDomain.Request(g_sos, pArray[n])) != S_OK)
        {
            ExtOut("Failed to get appdomain %p, error %lx\n", SOS_PTR(pArray[n]), Status);
            return Status;
        }

        ExtOut("--------------------------------------\n");
        DMLOut("Domain %d:%s          %s\n", appDomain.dwId, (appDomain.dwId >= 10) ? "" : " ", DMLDomain(pArray[n]));
        DomainInfo(&appDomain);
    }

    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the contents of a Assembly        *
*    for a given address                                               *  
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpAssembly)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    DWORD_PTR p_AssemblyAddr = 0;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&p_AssemblyAddr, COHEX},
    };
    size_t nArg;

    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);

    if (p_AssemblyAddr == 0)
    {
        ExtOut("Invalid Assembly %s\n", args);
        return Status;
    }
    
    DacpAssemblyData Assembly;
    if ((Status=Assembly.Request(g_sos, TO_CDADDR(p_AssemblyAddr)))!=S_OK)
    {
        ExtOut("Fail to fill Assembly\n");
        return Status;
    }
    DMLOut("Parent Domain:      %s\n", DMLDomain(Assembly.ParentDomain));
    if (g_sos->GetAssemblyName(TO_CDADDR(p_AssemblyAddr), mdNameLen, g_mdName, NULL)==S_OK)
        ExtOut("Name:               %S\n", g_mdName);
    else
        ExtOut("Name:               Unknown\n");

    AssemblyInfo(&Assembly);
    return Status;
}


String GetHostingCapabilities(DWORD hostConfig)
{
    String result;

    bool bAnythingPrinted = false;

#define CHK_AND_PRINT(hType,hStr)                                \
    if (hostConfig & (hType)) {                                  \
        if (bAnythingPrinted) result += ", ";                    \
        result += hStr;                                          \
        bAnythingPrinted = true;                                 \
    }

    CHK_AND_PRINT(CLRMEMORYHOSTED, "Memory");
    CHK_AND_PRINT(CLRTASKHOSTED, "Task");
    CHK_AND_PRINT(CLRSYNCHOSTED, "Sync");
    CHK_AND_PRINT(CLRTHREADPOOLHOSTED, "Threadpool");
    CHK_AND_PRINT(CLRIOCOMPLETIONHOSTED, "IOCompletion");
    CHK_AND_PRINT(CLRASSEMBLYHOSTED, "Assembly");
    CHK_AND_PRINT(CLRGCHOSTED, "GC");
    CHK_AND_PRINT(CLRSECURITYHOSTED, "Security");

#undef CHK_AND_PRINT

    return result;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the managed threads               *
*                                                                      *
\**********************************************************************/
HRESULT PrintThreadsFromThreadStore(BOOL bMiniDump, BOOL bPrintLiveThreadsOnly)
{
    HRESULT Status;
    
    DacpThreadStoreData ThreadStore;
    if ((Status = ThreadStore.Request(g_sos)) != S_OK)
    {
        Print("Failed to request ThreadStore\n");
        return Status;
    }

    TableOutput table(2, 17);

    table.WriteRow("ThreadCount:", Decimal(ThreadStore.threadCount));
    table.WriteRow("UnstartedThread:", Decimal(ThreadStore.unstartedThreadCount));
    table.WriteRow("BackgroundThread:", Decimal(ThreadStore.backgroundThreadCount));
    table.WriteRow("PendingThread:", Decimal(ThreadStore.pendingThreadCount));
    table.WriteRow("DeadThread:", Decimal(ThreadStore.deadThreadCount));

    if (ThreadStore.fHostConfig & ~CLRHOSTED)
    {
        String hosting = "yes";

        hosting += " (";
        hosting += GetHostingCapabilities(ThreadStore.fHostConfig);
        hosting += ")";

        table.WriteRow("Hosted Runtime:", hosting);
    }
    else
    {
        table.WriteRow("Hosted Runtime:", "no");
    }

    const bool hosted = (ThreadStore.fHostConfig & CLRTASKHOSTED) != 0;
    table.ReInit(hosted ? 12 : 11, POINTERSIZE_HEX);
    table.SetWidths(10, 4, 4, 4, _max(9, POINTERSIZE_HEX), 
                      8, 11, 1+POINTERSIZE_HEX*2, POINTERSIZE_HEX,
                      5, 3, POINTERSIZE_HEX);

    table.SetColAlignment(0, AlignRight);
    table.SetColAlignment(1, AlignRight);
    table.SetColAlignment(2, AlignRight);
    table.SetColAlignment(4, AlignRight);

    table.WriteColumn(8, "Lock");
    table.WriteRow("", "ID", "OSID", "ThreadOBJ", "State", "GC Mode", "GC Alloc Context", "Domain", "Count", "Apt");
    
    if (hosted)
        table.WriteColumn("Fiber");

    table.WriteColumn("Exception");
    
    DacpThreadData Thread;
    CLRDATA_ADDRESS CurThread = ThreadStore.firstThread;
    while (CurThread)
    {
        if (IsInterrupt())
            break;

        if ((Status = Thread.Request(g_sos, CurThread)) != S_OK)
        {
            PrintLn("Failed to request Thread at ", Pointer(CurThread));
            return Status;
        }

        BOOL bSwitchedOutFiber = Thread.osThreadId == SWITCHED_OUT_FIBER_OSID;
        if (!IsKernelDebugger())
        {
            ULONG id = 0;          
            
            if (bSwitchedOutFiber)
            {
                table.WriteColumn(0, "<<<< ");
            }
            else if (g_ExtSystem->GetThreadIdBySystemId(Thread.osThreadId, &id) == S_OK)
            {
                table.WriteColumn(0, Decimal(id));
            }
            else if (bPrintLiveThreadsOnly)
            {
                CurThread = Thread.nextThread;
                continue;
            }
            else
            {
                table.WriteColumn(0, "XXXX ");
            }
        }

        table.WriteColumn(1, Decimal(Thread.corThreadId));
        table.WriteColumn(2, ThreadID(bSwitchedOutFiber ? 0 : Thread.osThreadId));
        table.WriteColumn(3, Pointer(CurThread));
        table.WriteColumn(4, ThreadState(Thread.state));
        table.WriteColumn(5,  Thread.preemptiveGCDisabled == 1 ? "Cooperative" : "Preemptive");
        table.WriteColumnFormat(6, "%p:%p", Thread.allocContextPtr, Thread.allocContextLimit);

        if (Thread.domain)
        {
            table.WriteColumn(7, AppDomainPtr(Thread.domain));
        }
        else
        {
            CLRDATA_ADDRESS domain = 0;
            if (FAILED(g_sos->GetDomainFromContext(Thread.context, &domain)))
                table.WriteColumn(7, "<error>");
            else
                table.WriteColumn(7, AppDomainPtr(domain));
        }
        
        table.WriteColumn(8, Decimal(Thread.lockCount));

        // Apartment state
#ifndef FEATURE_PAL           
        DWORD_PTR OleTlsDataAddr;
        if (!bSwitchedOutFiber 
                && SafeReadMemory(Thread.teb + offsetof(TEB, ReservedForOle),
                            &OleTlsDataAddr,
                            sizeof(OleTlsDataAddr), NULL) && OleTlsDataAddr != 0)
        {
            DWORD AptState;
            if (SafeReadMemory(OleTlsDataAddr+offsetof(SOleTlsData,dwFlags),
                               &AptState,
                               sizeof(AptState), NULL))
            {
                if (AptState & OLETLS_APARTMENTTHREADED)
                    table.WriteColumn(9, "STA");
                else if (AptState & OLETLS_MULTITHREADED)
                    table.WriteColumn(9, "MTA");
                else if (AptState & OLETLS_INNEUTRALAPT)
                    table.WriteColumn(9, "NTA");
                else
                    table.WriteColumn(9, "Ukn");
            }
            else
            {
                table.WriteColumn(9, "Ukn");
            }
        }
        else
#endif // FEATURE_PAL
            table.WriteColumn(9, "Ukn");

        if (hosted)
            table.WriteColumn(10, Thread.fiberData);
        
        WString lastCol;
        if (CurThread == ThreadStore.finalizerThread)
            lastCol += W("(Finalizer) ");
        if (CurThread == ThreadStore.gcThread)
            lastCol += W("(GC) ");

        const int TS_TPWorkerThread         = 0x01000000;    // is this a threadpool worker thread?
        const int TS_CompletionPortThread   = 0x08000000;    // is this is a completion port thread?
        
        if (Thread.state & TS_TPWorkerThread)
            lastCol += W("(Threadpool Worker) ");
        else if (Thread.state & TS_CompletionPortThread)
            lastCol += W("(Threadpool Completion Port) ");
        
        
        TADDR taLTOH;
        if (Thread.lastThrownObjectHandle && SafeReadMemory(TO_TADDR(Thread.lastThrownObjectHandle),
                                                            &taLTOH, sizeof(taLTOH), NULL) && taLTOH)
        {
            TADDR taMT;
            if (SafeReadMemory(taLTOH, &taMT, sizeof(taMT), NULL))
            {
                if (NameForMT_s(taMT, g_mdName, mdNameLen))
                    lastCol += WString(g_mdName) + W(" ") + ExceptionPtr(taLTOH);
                else
                    lastCol += WString(W("<Invalid Object> (")) + Pointer(taLTOH) + W(")");

                // Print something if there are nested exceptions on the thread
                if (Thread.firstNestedException)
                    lastCol += W(" (nested exceptions)");
            }
        }

        table.WriteColumn(lastCol);
        CurThread = Thread.nextThread;
    }

    return Status;
}

#ifndef FEATURE_PAL
HRESULT PrintSpecialThreads()
{
    Print("\n");

    DWORD dwCLRTLSDataIndex = 0;
    HRESULT Status = g_sos->GetTLSIndex(&dwCLRTLSDataIndex);
    
    if (!SUCCEEDED (Status))
    {
        Print("Failed to retrieve Tls Data index\n");
        return Status;
    }


    ULONG ulOriginalThreadID = 0;
    Status = g_ExtSystem->GetCurrentThreadId (&ulOriginalThreadID);
    if (!SUCCEEDED (Status))
    {
        Print("Failed to require current Thread ID\n");
        return Status;
    }

    ULONG ulTotalThreads = 0;
    Status = g_ExtSystem->GetNumberThreads (&ulTotalThreads);
    if (!SUCCEEDED (Status))
    {
        Print("Failed to require total thread number\n");
        return Status;
    }

    TableOutput table(3, 4, AlignRight, 5);
    table.WriteRow("", "OSID", "Special thread type");

    for (ULONG ulThread = 0; ulThread < ulTotalThreads; ulThread++)
    {
        ULONG Id = 0;
        ULONG SysId = 0;        
        HRESULT threadStatus = g_ExtSystem->GetThreadIdsByIndex(ulThread, 1, &Id, &SysId);
        if (!SUCCEEDED (threadStatus))
        {
            PrintLn("Failed to get thread ID for thread ", Decimal(ulThread));        
            continue;
        }    

        threadStatus = g_ExtSystem->SetCurrentThreadId(Id);
        if (!SUCCEEDED (threadStatus))
        {
            PrintLn("Failed to switch to thread ", ThreadID(SysId));        
            continue;
        }    

        CLRDATA_ADDRESS cdaTeb = 0;        
        threadStatus = g_ExtSystem->GetCurrentThreadTeb(&cdaTeb);
        if (!SUCCEEDED (threadStatus))
        {
            PrintLn("Failed to get Teb for Thread ", ThreadID(SysId));        
            continue;
        } 

        TADDR CLRTLSDataAddr = 0;

        TADDR tlsArrayAddr = NULL;
        if (!SafeReadMemory (TO_TADDR(cdaTeb) + WINNT_OFFSETOF__TEB__ThreadLocalStoragePointer , &tlsArrayAddr, sizeof (void**), NULL))
        {
            PrintLn("Failed to get Tls expansion slots for thread ", ThreadID(SysId));        
            continue;
        }

        if (tlsArrayAddr == NULL)
        {
            continue;
        }

        TADDR moduleTlsDataAddr = 0;
        if (!SafeReadMemory (tlsArrayAddr + sizeof (void*) * (dwCLRTLSDataIndex & 0xFFFF), &moduleTlsDataAddr, sizeof (void**), NULL))
        {
            PrintLn("Failed to get Tls expansion slots for thread ", ThreadID(SysId));        
            continue;
        }

        CLRTLSDataAddr = moduleTlsDataAddr + ((dwCLRTLSDataIndex & 0x7FFF0000) >> 16) + OFFSETOF__TLS__tls_EETlsData;

        TADDR CLRTLSData = NULL;
        if (!SafeReadMemory (CLRTLSDataAddr, &CLRTLSData, sizeof (TADDR), NULL))
        {
            PrintLn("Failed to get CLR Tls data for thread ", ThreadID(SysId));        
            continue;
        }

        if (CLRTLSData == NULL)
        {
            continue;
        }

        size_t ThreadType = 0;
        if (!SafeReadMemory (CLRTLSData + sizeof (TADDR) * TlsIdx_ThreadType, &ThreadType, sizeof (size_t), NULL))
        {
            PrintLn("Failed to get thread type info not found for thread ", ThreadID(SysId));        
            continue;
        }
        
        if (ThreadType == 0)
        {
            continue;
        }

        table.WriteColumn(0, Decimal(Id));
        table.WriteColumn(1, ThreadID(SysId));

        String type;
        if (ThreadType & ThreadType_GC)
        {
            type += "GC ";
        }
        if (ThreadType & ThreadType_Timer)
        {
            type += "Timer ";
        }
        if (ThreadType & ThreadType_Gate)
        {
            type += "Gate ";
        }
        if (ThreadType & ThreadType_DbgHelper)
        {
            type += "DbgHelper ";
        }
        if (ThreadType & ThreadType_Shutdown)
        {
            type += "Shutdown ";
        }
        if (ThreadType & ThreadType_DynamicSuspendEE)
        {
            type += "SuspendEE ";
        }
        if (ThreadType & ThreadType_Finalizer)
        {
            type += "Finalizer ";
        }
        if (ThreadType & ThreadType_ShutdownHelper)
        {
            type += "ShutdownHelper ";
        }
        if (ThreadType & ThreadType_Threadpool_IOCompletion)
        {
            type += "IOCompletion ";
        }
        if (ThreadType & ThreadType_Threadpool_Worker)
        {
            type += "ThreadpoolWorker ";
        }
        if (ThreadType & ThreadType_Wait)
        {
            type += "Wait ";
        }
        if (ThreadType & ThreadType_ProfAPI_Attach)
        {
            type += "ProfilingAPIAttach ";
        }
        if (ThreadType & ThreadType_ProfAPI_Detach)
        {
            type += "ProfilingAPIDetach ";
        }

        table.WriteColumn(2, type);
    }

    Status = g_ExtSystem->SetCurrentThreadId (ulOriginalThreadID);
    if (!SUCCEEDED (Status))
    {
        ExtOut("Failed to switch to original thread\n");        
        return Status;
    }    

    return Status;
}
#endif //FEATURE_PAL

HRESULT SwitchToExceptionThread()
{
    HRESULT Status;
    
    DacpThreadStoreData ThreadStore;
    if ((Status = ThreadStore.Request(g_sos)) != S_OK)
    {
        Print("Failed to request ThreadStore\n");
        return Status;
    }

    DacpThreadData Thread;
    CLRDATA_ADDRESS CurThread = ThreadStore.firstThread;
    while (CurThread)
    {
        if (IsInterrupt())
            break;

        if ((Status = Thread.Request(g_sos, CurThread)) != S_OK)
        {
            PrintLn("Failed to request Thread at ", Pointer(CurThread));
            return Status;
        }
        
        TADDR taLTOH;
        if (Thread.lastThrownObjectHandle != NULL)
        {
            if (SafeReadMemory(TO_TADDR(Thread.lastThrownObjectHandle), &taLTOH, sizeof(taLTOH), NULL))
            {
                if (taLTOH != NULL)
                {
                    ULONG id;
                    if (g_ExtSystem->GetThreadIdBySystemId(Thread.osThreadId, &id) == S_OK)
                    {
                        if (g_ExtSystem->SetCurrentThreadId(id) == S_OK)
                        {
                            PrintLn("Found managed exception on thread ", ThreadID(Thread.osThreadId));
                            break;
                        }
                    }
                }
            }
        }

        CurThread = Thread.nextThread;
    }

    return Status;
}

struct ThreadStateTable
{
    unsigned int State;
    const char * Name;
};
static const struct ThreadStateTable ThreadStates[] =
{
    {0x1, "Thread Abort Requested"},
    {0x2, "GC Suspend Pending"},
    {0x4, "User Suspend Pending"},
    {0x8, "Debug Suspend Pending"},
    {0x10, "GC On Transitions"},
    {0x20, "Legal to Join"},
    {0x40, "Yield Requested"},
    {0x80, "Hijacked by the GC"},
    {0x100, "Blocking GC for Stack Overflow"},
    {0x200, "Background"},
    {0x400, "Unstarted"},
    {0x800, "Dead"},
    {0x1000, "CLR Owns"},
    {0x2000, "CoInitialized"},
    {0x4000, "In Single Threaded Apartment"},
    {0x8000, "In Multi Threaded Apartment"},
    {0x10000, "Reported Dead"},
    {0x20000, "Fully initialized"},
    {0x40000, "Task Reset"},
    {0x80000, "Sync Suspended"},
    {0x100000, "Debug Will Sync"},
    {0x200000, "Stack Crawl Needed"},
    {0x400000, "Suspend Unstarted"},
    {0x800000, "Aborted"},
    {0x1000000, "Thread Pool Worker Thread"},
    {0x2000000, "Interruptible"},
    {0x4000000, "Interrupted"},
    {0x8000000, "Completion Port Thread"},
    {0x10000000, "Abort Initiated"},
    {0x20000000, "Finalized"},
    {0x40000000, "Failed to Start"},
    {0x80000000, "Detached"},
};

DECLARE_API(ThreadState)
{
    INIT_API_NODAC();

    size_t state = GetExpression(args);
    int count = 0;

    if (state)
    {

        for (unsigned int i = 0; i < _countof(ThreadStates); ++i)
            if (state & ThreadStates[i].State)
            {
                ExtOut("    %s\n", ThreadStates[i].Name);
                count++;
            }
    }
    
    // If we did not find any thread states, print out a message to let the user
    // know that the function is working correctly.
    if (count == 0)
        ExtOut("    No thread states for '%s'\n", args);

    return Status;
}

DECLARE_API(Threads)
{
    INIT_API();

    BOOL bPrintSpecialThreads = FALSE;
    BOOL bPrintLiveThreadsOnly = FALSE;
    BOOL bSwitchToManagedExceptionThread = FALSE;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-special", &bPrintSpecialThreads, COBOOL, FALSE},
        {"-live", &bPrintLiveThreadsOnly, COBOOL, FALSE},
        {"-managedexception", &bSwitchToManagedExceptionThread, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
    {
        return Status;
    }

    if (bSwitchToManagedExceptionThread)
    {
        return SwitchToExceptionThread();
    }
    
    // We need to support minidumps for this command.
    BOOL bMiniDump = IsMiniDumpFile();

    if (bMiniDump && bPrintSpecialThreads)
    {
        Print("Special thread information is not available in mini dumps.\n");
    }

    EnableDMLHolder dmlHolder(dml);

    try
    {
        Status = PrintThreadsFromThreadStore(bMiniDump, bPrintLiveThreadsOnly);
        if (!bMiniDump && bPrintSpecialThreads)
        {
#ifdef FEATURE_PAL
            Print("\n-special not supported.\n");
#else //FEATURE_PAL    
            HRESULT Status2 = PrintSpecialThreads(); 
            if (!SUCCEEDED(Status2))
                Status = Status2;
#endif //FEATURE_PAL            
        }
    }
    catch (sos::Exception &e)
    {
        ExtOut("%s\n", e.what());
    }
    
    return Status;
}

#ifndef FEATURE_PAL
/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the Watson Buckets.               *
*                                                                      *
\**********************************************************************/
DECLARE_API(WatsonBuckets)
{
    INIT_API();

    // We don't need to support minidumps for this command.
    if (IsMiniDumpFile())
    {
        ExtOut("Not supported on mini dumps.\n");
    }
    
    // Get the current managed thread.
    CLRDATA_ADDRESS threadAddr = GetCurrentManagedThread();
    DacpThreadData Thread;

    if ((threadAddr == NULL) || ((Status = Thread.Request(g_sos, threadAddr)) != S_OK))
    {
        ExtOut("The current thread is unmanaged\n");
        return Status;
    }
    
    // Get the definition of GenericModeBlock.
#include <msodw.h>
    GenericModeBlock gmb;

    if ((Status = g_sos->GetClrWatsonBuckets(threadAddr, &gmb)) != S_OK)
    {
        ExtOut("Can't get Watson Buckets\n");
        return Status;
    }

    ExtOut("Watson Bucket parameters:\n");
    ExtOut("b1: %S\n", gmb.wzP1);
    ExtOut("b2: %S\n", gmb.wzP2);
    ExtOut("b3: %S\n", gmb.wzP3);
    ExtOut("b4: %S\n", gmb.wzP4);
    ExtOut("b5: %S\n", gmb.wzP5);
    ExtOut("b6: %S\n", gmb.wzP6);
    ExtOut("b7: %S\n", gmb.wzP7);
    ExtOut("b8: %S\n", gmb.wzP8);
    ExtOut("b9: %S\n", gmb.wzP9);
        
    return Status;
} // WatsonBuckets()
#endif // FEATURE_PAL

struct PendingBreakpoint
{
    WCHAR szModuleName[MAX_LONGPATH];
    WCHAR szFunctionName[mdNameLen];
    WCHAR szFilename[MAX_LONGPATH];
    DWORD lineNumber;
    TADDR pModule; 
    DWORD ilOffset;
    mdMethodDef methodToken;
    void SetModule(TADDR module)
    {
        pModule = module;
    }

    bool ModuleMatches(TADDR compare)
    {
        return (compare == pModule);
    }

    PendingBreakpoint *pNext;
    PendingBreakpoint() : lineNumber(0), ilOffset(0), methodToken(0), pNext(NULL) 
    {
        szModuleName[0] = L'\0';
        szFunctionName[0] = L'\0';
        szFilename[0] = L'\0';
    }
};

void IssueDebuggerBPCommand ( CLRDATA_ADDRESS addr )
{
    const int MaxBPsCached = 1024;
    static CLRDATA_ADDRESS alreadyPlacedBPs[MaxBPsCached];
    static int curLimit = 0;

    // on ARM the debugger requires breakpoint addresses to be sanitized
    if (IsDbgTargetArm())
#ifndef FEATURE_PAL
      addr &= ~THUMB_CODE;
#else
      addr |= THUMB_CODE; // lldb expects thumb code bit set
#endif      

    // if we overflowed our cache consider all new BPs unique...
    BOOL bUnique = curLimit >= MaxBPsCached;
    if (!bUnique)
    {
        bUnique = TRUE;
        for (int i = 0; i < curLimit; ++i)
        {
            if (alreadyPlacedBPs[i] == addr)
            {
                bUnique = FALSE;
                break;
            }
        }
    }
    if (bUnique)
    {
        char buffer[64]; // sufficient for "bp <pointersize>"
        static WCHAR wszNameBuffer[1024]; // should be large enough

        // get the MethodDesc name
        CLRDATA_ADDRESS pMD;
        if (g_sos->GetMethodDescPtrFromIP(addr, &pMD) != S_OK
            || g_sos->GetMethodDescName(pMD, 1024, wszNameBuffer, NULL) != S_OK)
        {
            wcscpy_s(wszNameBuffer, _countof(wszNameBuffer), W("UNKNOWN"));        
        }

#ifndef FEATURE_PAL
        sprintf_s(buffer, _countof(buffer), "bp %p", (void*) (size_t) addr);
#else
        sprintf_s(buffer, _countof(buffer), "breakpoint set --address 0x%p", (void*) (size_t) addr);
#endif
        ExtOut("Setting breakpoint: %s [%S]\n", buffer, wszNameBuffer);
        g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);

        if (curLimit < MaxBPsCached)
        {
            alreadyPlacedBPs[curLimit++] = addr;
        }
    }
}

class Breakpoints
{
    PendingBreakpoint* m_breakpoints;
public:
    Breakpoints()
    {
        m_breakpoints = NULL;
    }
    ~Breakpoints()
    {
        PendingBreakpoint *pCur = m_breakpoints;
        while(pCur)
        {
            PendingBreakpoint *pNext = pCur->pNext;
            delete pCur;
            pCur = pNext;
        }
        m_breakpoints = NULL;
    }

    void Add(__in_z LPWSTR szModule, __in_z LPWSTR szName, TADDR mod, DWORD ilOffset)
    {
        if (!IsIn(szModule, szName, mod))
        {
            PendingBreakpoint *pNew = new PendingBreakpoint();
            wcscpy_s(pNew->szModuleName, MAX_LONGPATH, szModule);
            wcscpy_s(pNew->szFunctionName, mdNameLen, szName);
            pNew->SetModule(mod);
            pNew->ilOffset = ilOffset;
            pNew->pNext = m_breakpoints;
            m_breakpoints = pNew;
        }
    }

    void Add(__in_z LPWSTR szModule, __in_z LPWSTR szName, mdMethodDef methodToken, TADDR mod, DWORD ilOffset)
    {
        if (!IsIn(methodToken, mod, ilOffset))
        {
            PendingBreakpoint *pNew = new PendingBreakpoint();
            wcscpy_s(pNew->szModuleName, MAX_LONGPATH, szModule);
            wcscpy_s(pNew->szFunctionName, mdNameLen, szName);
            pNew->methodToken = methodToken;
            pNew->SetModule(mod);
            pNew->ilOffset = ilOffset;
            pNew->pNext = m_breakpoints;
            m_breakpoints = pNew;
        }
    }

    void Add(__in_z LPWSTR szFilename, DWORD lineNumber, TADDR mod)
    {
        if (!IsIn(szFilename, lineNumber, mod))
        {
            PendingBreakpoint *pNew = new PendingBreakpoint();
            wcscpy_s(pNew->szFilename, MAX_LONGPATH, szFilename);
            pNew->lineNumber = lineNumber;
            pNew->SetModule(mod);
            pNew->pNext = m_breakpoints;
            m_breakpoints = pNew;
        }
    }

    void Add(__in_z LPWSTR szFilename, DWORD lineNumber, mdMethodDef methodToken, TADDR mod, DWORD ilOffset)
    {
        if (!IsIn(methodToken, mod, ilOffset))
        {
            PendingBreakpoint *pNew = new PendingBreakpoint();
            wcscpy_s(pNew->szFilename, MAX_LONGPATH, szFilename);
            pNew->lineNumber = lineNumber;
            pNew->methodToken = methodToken;
            pNew->SetModule(mod);
            pNew->ilOffset = ilOffset;
            pNew->pNext = m_breakpoints;
            m_breakpoints = pNew;
        }
    }

    //returns true if updates are still needed for this module, FALSE if all BPs are now bound
    BOOL Update(TADDR mod, BOOL isNewModule)
    {
        BOOL bNeedUpdates = FALSE;
        PendingBreakpoint *pCur = NULL;

        if(isNewModule)
        {
            SymbolReader symbolReader;
            SymbolReader* pSymReader = &symbolReader;
            if(LoadSymbolsForModule(mod, &symbolReader) != S_OK)
                pSymReader = NULL;

            // Get tokens for any modules that match. If there was a change,
            // update notifications.                
            pCur = m_breakpoints;
            while(pCur)
            {
                PendingBreakpoint *pNext = pCur->pNext;
                ResolvePendingNonModuleBoundBreakpoint(mod, pCur, pSymReader);
                pCur = pNext;
            }
        }

        pCur = m_breakpoints;
        while(pCur)
        {
            PendingBreakpoint *pNext = pCur->pNext;
            if (ResolvePendingBreakpoint(mod, pCur))
            {
                bNeedUpdates = TRUE;
            }
            pCur = pNext;
        }
        return bNeedUpdates;
    }

    BOOL UpdateKnownCodeAddress(TADDR mod, CLRDATA_ADDRESS bpLocation)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        BOOL bpSet = FALSE;

        while(pCur)
        {
            PendingBreakpoint *pNext = pCur->pNext;
            if (pCur->ModuleMatches(mod))
            {
                IssueDebuggerBPCommand(bpLocation);
                bpSet = TRUE;
                break;
            }

            pCur = pNext;
        }

        return bpSet;
    }

    void RemovePendingForModule(TADDR mod)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        while(pCur)
        {
            PendingBreakpoint *pNext = pCur->pNext;
            if (pCur->ModuleMatches(mod))
            {
                // Delete the current node, and keep going
                Delete(pCur);
            }

            pCur = pNext;
        }                
    }
    
    void ListBreakpoints()
    {
        PendingBreakpoint *pCur = m_breakpoints;
        size_t iBreakpointIndex = 1;
        ExtOut(SOSPrefix "bpmd pending breakpoint list\n Breakpoint index - Location, ModuleID, Method Token\n");
        while(pCur)
        {
            //windbg likes to format %p as always being 64 bits
            ULONG64 modulePtr = (ULONG64)pCur->pModule;

            if(pCur->szModuleName[0] != L'\0')
                ExtOut("%d - %ws!%ws+%d, 0x%p, 0x%08x\n", iBreakpointIndex, pCur->szModuleName, pCur->szFunctionName, pCur->ilOffset, modulePtr, pCur->methodToken);
            else
                ExtOut("%d - %ws:%d, 0x%p, 0x%08x\n",  iBreakpointIndex, pCur->szFilename, pCur->lineNumber, modulePtr, pCur->methodToken);
            iBreakpointIndex++;
            pCur = pCur->pNext;
        }
    }

#ifndef FEATURE_PAL
    void SaveBreakpoints(FILE* pFile)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        while(pCur)
        {
            if(pCur->szModuleName[0] != L'\0')
                fprintf_s(pFile, "!bpmd %ws %ws %d\n", pCur->szModuleName, pCur->szFunctionName, pCur->ilOffset);
            else
                fprintf_s(pFile, "!bpmd %ws:%d\n",  pCur->szFilename, pCur->lineNumber);
            pCur = pCur->pNext;
        }
    }
#endif

    void CleanupNotifications()
    {
#ifdef FEATURE_PAL
        if (m_breakpoints == NULL)
        {
            g_ExtServices->ClearExceptionCallback();
        }
#endif
    }

    void ClearBreakpoint(size_t breakPointToClear)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        size_t iBreakpointIndex = 1;
        while(pCur)
        {
            if (breakPointToClear == iBreakpointIndex)
            {
                ExtOut("%d - %ws, %ws, %p\n", iBreakpointIndex, pCur->szModuleName, pCur->szFunctionName, pCur->pModule);
                ExtOut("Cleared\n");
                Delete(pCur);
                break;
            }
            iBreakpointIndex++;
            pCur = pCur->pNext;
        }

        if (pCur == NULL)
        {
            ExtOut("Invalid pending breakpoint index.\n");
        }
        CleanupNotifications();
    }

    void ClearAllBreakpoints()
    {
        size_t iBreakpointIndex = 1;
        for (PendingBreakpoint *pCur = m_breakpoints; pCur != NULL; )
        {
            PendingBreakpoint* pNext = pCur->pNext;
            Delete(pCur);
            iBreakpointIndex++;
            pCur = pNext;
        }
        CleanupNotifications();

        ExtOut("All pending breakpoints cleared.\n");
    }

    HRESULT LoadSymbolsForModule(TADDR mod, SymbolReader* pSymbolReader)
    {
        HRESULT Status = S_OK;
        ToRelease<IXCLRDataModule> pModule;
        IfFailRet(g_sos->GetModule(mod, &pModule));

        ToRelease<IMetaDataImport> pMDImport = NULL;
        IfFailRet(pModule->QueryInterface(IID_IMetaDataImport, (LPVOID *) &pMDImport));

        IfFailRet(pSymbolReader->LoadSymbols(pMDImport, pModule));

        return S_OK;
    }

    HRESULT ResolvePendingNonModuleBoundBreakpoint(__in_z WCHAR* pFilename, DWORD lineNumber, TADDR mod, SymbolReader* pSymbolReader)
    {
        HRESULT Status = S_OK;
        if(pSymbolReader == NULL)
            return S_FALSE; // no symbols, can't bind here

        mdMethodDef methodDef;
        ULONG32 ilOffset;
        if(FAILED(Status = pSymbolReader->ResolveSequencePoint(pFilename, lineNumber, mod, &methodDef, &ilOffset)))
        {
            return S_FALSE; // not binding in a module is typical
        }

        Add(pFilename, lineNumber, methodDef, mod, ilOffset);
        return Status;
    }

    HRESULT ResolvePendingNonModuleBoundBreakpoint(__in_z WCHAR* pModuleName, __in_z WCHAR* pMethodName, TADDR mod, DWORD ilOffset)
    {
        HRESULT Status = S_OK;
        char szName[mdNameLen];
        int numModule;
        
        ToRelease<IXCLRDataModule> module;
        IfFailRet(g_sos->GetModule(mod, &module));

        WideCharToMultiByte(CP_ACP, 0, pModuleName, (int)(_wcslen(pModuleName) + 1), szName, mdNameLen, NULL, NULL);

        ArrayHolder<DWORD_PTR> moduleList = ModuleFromName(szName, &numModule);
        if (moduleList == NULL)
        {
            ExtOut("Failed to request module list.\n");
            return E_FAIL;
        }

        for (int i = 0; i < numModule; i++)
        {
            // If any one entry in moduleList matches, then the current PendingBreakpoint
            // is the right one.
            if(moduleList[i] != TO_TADDR(mod))
                continue;

            CLRDATA_ENUM h;
            if (module->StartEnumMethodDefinitionsByName(pMethodName, 0, &h) == S_OK)
            {
                IXCLRDataMethodDefinition *pMeth = NULL;
                while (module->EnumMethodDefinitionByName(&h, &pMeth) == S_OK)
                {
                    mdMethodDef methodToken;
                    ToRelease<IXCLRDataModule> pUnusedModule;
                    IfFailRet(pMeth->GetTokenAndScope(&methodToken, &pUnusedModule));

                    Add(pModuleName, pMethodName, methodToken, mod, ilOffset);
                    pMeth->Release();
                }
                module->EndEnumMethodDefinitionsByName(h);
            }
        }
        return S_OK;
    }

    // Return TRUE if there might be more instances that will be JITTED later
    static BOOL ResolveMethodInstances(IXCLRDataMethodDefinition *pMeth, DWORD ilOffset)
    {
        BOOL bFoundCode = FALSE;
        BOOL bNeedDefer = FALSE;
        CLRDATA_ENUM h1;
        
        if (pMeth->StartEnumInstances (NULL, &h1) == S_OK)
        {
            IXCLRDataMethodInstance *inst = NULL;
            while (pMeth->EnumInstance (&h1, &inst) == S_OK)
            {
                BOOL foundByIlOffset = FALSE;
                ULONG32 rangesNeeded = 0;
                if(inst->GetAddressRangesByILOffset(ilOffset, 0, &rangesNeeded, NULL) == S_OK)
                {
                    ArrayHolder<CLRDATA_ADDRESS_RANGE> ranges = new NOTHROW CLRDATA_ADDRESS_RANGE[rangesNeeded];
                    if (ranges != NULL)
                    {
                        if (inst->GetAddressRangesByILOffset(ilOffset, rangesNeeded, NULL, ranges) == S_OK)
                        {
                            for (DWORD i = 0; i < rangesNeeded; i++)
                            {
                                IssueDebuggerBPCommand(ranges[i].startAddress);
                                bFoundCode = TRUE;
                                foundByIlOffset = TRUE;
                            }
                        }
                    }
                }
                
                if (!foundByIlOffset && ilOffset == 0)
                {
                    CLRDATA_ADDRESS addr = 0;
                    if (inst->GetRepresentativeEntryAddress(&addr) == S_OK)
                    {
                        IssueDebuggerBPCommand(addr);
                        bFoundCode = TRUE;
                    }
                }
            }
            pMeth->EndEnumInstances (h1);
        }

        // if this is a generic method we need to add a deferred bp
        BOOL bGeneric = FALSE;
        pMeth->HasClassOrMethodInstantiation(&bGeneric);

        bNeedDefer = !bFoundCode || bGeneric;
        // This is down here because we only need to call SetCodeNofiication once.
        if (bNeedDefer)
        {
            if (pMeth->SetCodeNotification (CLRDATA_METHNOTIFY_GENERATED) != S_OK)
            {
                bNeedDefer = FALSE;
                ExtOut("Failed to set code notification\n");
            }
        }
        return bNeedDefer;
    }

private:    
    BOOL IsIn(__in_z LPWSTR szModule, __in_z LPWSTR szName, TADDR mod)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        while(pCur)
        {
            if (pCur->ModuleMatches(mod) && 
                _wcsicmp(pCur->szModuleName, szModule) == 0 &&
                _wcscmp(pCur->szFunctionName, szName) == 0)
            {
                return TRUE;
            }
            pCur = pCur->pNext;
        }
        return FALSE;
    }

    BOOL IsIn(__in_z LPWSTR szFilename, DWORD lineNumber, TADDR mod)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        while(pCur)
        {
            if (pCur->ModuleMatches(mod) && 
                _wcsicmp(pCur->szFilename, szFilename) == 0 &&
                pCur->lineNumber == lineNumber)
            {
                return TRUE;
            }
            pCur = pCur->pNext;
        }
        return FALSE;
    }

    BOOL IsIn(mdMethodDef token, TADDR mod, DWORD ilOffset)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        while(pCur)
        {
            if (pCur->ModuleMatches(mod) && 
                pCur->methodToken == token &&
                pCur->ilOffset == ilOffset)
            {
                return TRUE;
            }
            pCur = pCur->pNext;
        }
        return FALSE;
    }

    void Delete(PendingBreakpoint *pDelete)
    {
        PendingBreakpoint *pCur = m_breakpoints;
        PendingBreakpoint *pPrev = NULL;
        while(pCur)
        {
            if (pCur == pDelete)
            {
                if (pPrev == NULL)
                {
                    m_breakpoints = pCur->pNext;
                }
                else
                {
                    pPrev->pNext = pCur->pNext;
                }
                delete pCur;
                return;
            }
            pPrev = pCur;
            pCur = pCur->pNext;
        }
    }



    HRESULT ResolvePendingNonModuleBoundBreakpoint(TADDR mod, PendingBreakpoint *pCur, SymbolReader* pSymbolReader)
    {
        // This function only works with pending breakpoints that are not module bound.
        if (pCur->pModule == NULL)
        {
            if(pCur->szModuleName[0] != L'\0')
            {
                return ResolvePendingNonModuleBoundBreakpoint(pCur->szModuleName, pCur->szFunctionName, mod, pCur->ilOffset);
            }
            else
            {
                return ResolvePendingNonModuleBoundBreakpoint(pCur->szFilename, pCur->lineNumber, mod, pSymbolReader);
            }
        }
        else
        {
            return S_OK;
        }
    }

    // Returns TRUE if further instances may be jitted, FALSE if all instances are now resolved
    BOOL ResolvePendingBreakpoint(TADDR addr, PendingBreakpoint *pCur)
    {
        // Only go forward if the module matches the current PendingBreakpoint
        if (!pCur->ModuleMatches(addr))
        {
            return FALSE;
        }

        ToRelease<IXCLRDataModule> mod;
        if (FAILED(g_sos->GetModule(addr, &mod)))
        {
            return FALSE;
        }

        if(pCur->methodToken == 0)
        {
            return FALSE;
        }

        ToRelease<IXCLRDataMethodDefinition> pMeth = NULL;
        mod->GetMethodDefinitionByToken(pCur->methodToken, &pMeth);

        // We may not need the code notification. Maybe it was ngen'd and we
        // already have the method?
        // We can delete the current entry if ResolveMethodInstances() set all BPs
        return ResolveMethodInstances(pMeth, pCur->ilOffset);
    }
};

Breakpoints g_bpoints;

// Controls whether optimizations are disabled on module load and whether NGEN can be used
BOOL g_fAllowJitOptimization = TRUE;

// Controls whether a one-shot breakpoint should be inserted the next time
// execution is about to enter a catch clause
BOOL g_stopOnNextCatch = FALSE;

// According to the latest debuggers these callbacks will not get called
// unless the user (or an extension, like SOS :-)) had previously enabled
// clrn with "sxe clrn".
class CNotification : public IXCLRDataExceptionNotification5
{
    static int s_condemnedGen;

    int m_count;
    int m_dbgStatus;
public:
    CNotification() 
        : m_count(0)
        , m_dbgStatus(DEBUG_STATUS_NO_CHANGE)
    {}

    int GetDebugStatus()
    {
        return m_dbgStatus;
    }

    STDMETHODIMP QueryInterface (REFIID iid, void **ppvObject)
    {
        if (ppvObject == NULL)
            return E_INVALIDARG;

        if (IsEqualIID(iid, IID_IUnknown)
            || IsEqualIID(iid, IID_IXCLRDataExceptionNotification)
            || IsEqualIID(iid, IID_IXCLRDataExceptionNotification2)
            || IsEqualIID(iid, IID_IXCLRDataExceptionNotification3)
            || IsEqualIID(iid, IID_IXCLRDataExceptionNotification4)
            || IsEqualIID(iid, IID_IXCLRDataExceptionNotification5))
        {
            *ppvObject = static_cast<IXCLRDataExceptionNotification5*>(this);
            AddRef();
            return S_OK;
        }
        else
            return E_NOINTERFACE;

    }

    STDMETHODIMP_(ULONG) AddRef(void) { return ++m_count; }
    STDMETHODIMP_(ULONG) Release(void)
    {
        m_count--;
        if (m_count < 0)
        {
            m_count = 0;
        }
        return m_count;
    }

            
    /*
     * New code was generated or discarded for a method.:
     */
    STDMETHODIMP OnCodeGenerated(IXCLRDataMethodInstance* method)
    {
        m_dbgStatus = DEBUG_STATUS_GO_HANDLED;
        return S_OK;
    }

    STDMETHODIMP OnCodeGenerated2(IXCLRDataMethodInstance* method, CLRDATA_ADDRESS nativeCodeLocation)
    {
        // Some method has been generated, make a breakpoint.
        ULONG32 len = mdNameLen;
        LPWSTR szModuleName = (LPWSTR)alloca(mdNameLen * sizeof(WCHAR));
        if (method->GetName(0, mdNameLen, &len, g_mdName) == S_OK)
        {            
            ToRelease<IXCLRDataModule> pMod;
            HRESULT hr = method->GetTokenAndScope(NULL, &pMod);
            if (SUCCEEDED(hr))
            {
                len = mdNameLen;
                if (pMod->GetName(mdNameLen, &len, szModuleName) == S_OK)
                {
                    ExtOut("JITTED %S!%S\n", szModuleName, g_mdName);
                    
                    DacpGetModuleAddress dgma;
                    if (SUCCEEDED(dgma.Request(pMod)))
                    {
                        g_bpoints.UpdateKnownCodeAddress(TO_TADDR(dgma.ModulePtr), nativeCodeLocation);
                    }
                    else
                    {
                        ExtOut("Failed to request module address.\n");
                    }
                }
            }
        }

        m_dbgStatus = DEBUG_STATUS_GO_HANDLED;
        return S_OK;
    }

    STDMETHODIMP OnCodeDiscarded(IXCLRDataMethodInstance* method)
    {
        return E_NOTIMPL;
    }

    /*
     * The process or task reached the desired execution state.
     */
    STDMETHODIMP OnProcessExecution(ULONG32 state) { return E_NOTIMPL; }
    STDMETHODIMP OnTaskExecution(IXCLRDataTask* task,
                            ULONG32 state) { return E_NOTIMPL; }

    /*
     * The given module was loaded or unloaded.
     */
    STDMETHODIMP OnModuleLoaded(IXCLRDataModule* mod)
    {
        DacpGetModuleAddress dgma;
        if (SUCCEEDED(dgma.Request(mod)))
        {
            g_bpoints.Update(TO_TADDR(dgma.ModulePtr), TRUE);
        }

        if(!g_fAllowJitOptimization)
        {
            HRESULT hr;
            ToRelease<IXCLRDataModule2> mod2;
            if(FAILED(mod->QueryInterface(__uuidof(IXCLRDataModule2), (void**) &mod2)))
            {
                ExtOut("SOS: warning, optimizations for this module could not be suppressed because this CLR version doesn't support the functionality\n");
            }
            else if(FAILED(hr = mod2->SetJITCompilerFlags(CORDEBUG_JIT_DISABLE_OPTIMIZATION)))
            {
                if(hr == CORDBG_E_CANT_CHANGE_JIT_SETTING_FOR_ZAP_MODULE)
                    ExtOut("SOS: warning, optimizations for this module could not be surpressed because an optimized prejitted image was loaded\n");
                else
                    ExtOut("SOS: warning, optimizations for this module could not be surpressed hr=0x%x\n", hr);
            }
        }
        
        m_dbgStatus = DEBUG_STATUS_GO_HANDLED;
        return S_OK;
    }

    STDMETHODIMP OnModuleUnloaded(IXCLRDataModule* mod)
    {
        DacpGetModuleAddress dgma;
        if (SUCCEEDED(dgma.Request(mod)))
        {
            g_bpoints.RemovePendingForModule(TO_TADDR(dgma.ModulePtr));
        }

        m_dbgStatus = DEBUG_STATUS_GO_HANDLED;
        return S_OK;
    }

    /*
     * The given type was loaded or unloaded.
     */
    STDMETHODIMP OnTypeLoaded(IXCLRDataTypeInstance* typeInst) 
    { return E_NOTIMPL; }
    STDMETHODIMP OnTypeUnloaded(IXCLRDataTypeInstance* typeInst) 
    { return E_NOTIMPL; }

    STDMETHODIMP OnAppDomainLoaded(IXCLRDataAppDomain* domain)
    { return E_NOTIMPL; }
    STDMETHODIMP OnAppDomainUnloaded(IXCLRDataAppDomain* domain)
    { return E_NOTIMPL; }
    STDMETHODIMP OnException(IXCLRDataExceptionState* exception)
    { return E_NOTIMPL; }

    STDMETHODIMP OnGcEvent(GcEvtArgs gcEvtArgs)
{
        // by default don't stop on these notifications...
        m_dbgStatus = DEBUG_STATUS_GO_HANDLED;

        IXCLRDataProcess2* idp2 = NULL;
        if (SUCCEEDED(g_clrData->QueryInterface(IID_IXCLRDataProcess2, (void**) &idp2)))
        {
            if (gcEvtArgs.typ == GC_MARK_END)
            {
                // erase notification request
                GcEvtArgs gea = { GC_MARK_END, { 0 } };
                idp2->SetGcNotification(gea);

                s_condemnedGen = bitidx(gcEvtArgs.condemnedGeneration);

                ExtOut("CLR notification: GC - Performing a gen %d collection. Determined surviving objects...\n", s_condemnedGen);

                // GC_MARK_END notification means: give the user a chance to examine the debuggee
                m_dbgStatus = DEBUG_STATUS_BREAK;
            }
        }

        return S_OK;
    }

     /*
     * Catch is about to be entered
     */
    STDMETHODIMP ExceptionCatcherEnter(IXCLRDataMethodInstance* method, DWORD catcherNativeOffset)
    {
        if(g_stopOnNextCatch)
        {
            CLRDATA_ADDRESS startAddr;
            if(method->GetRepresentativeEntryAddress(&startAddr) == S_OK)
            {
                CHAR buffer[100];
#ifndef FEATURE_PAL
                sprintf_s(buffer, _countof(buffer), "bp /1 %p", (void*) (size_t) (startAddr+catcherNativeOffset));
#else
                sprintf_s(buffer, _countof(buffer), "breakpoint set --one-shot --address 0x%p", (void*) (size_t) (startAddr+catcherNativeOffset));
#endif
                g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);
            }
            g_stopOnNextCatch = FALSE;
        }

        m_dbgStatus = DEBUG_STATUS_GO_HANDLED;
        return S_OK;
    }

    static int GetCondemnedGen()
    {
        return s_condemnedGen;
    }

};

int CNotification::s_condemnedGen = -1;

BOOL CheckCLRNotificationEvent(DEBUG_LAST_EVENT_INFO_EXCEPTION* pdle)
{
    ISOSDacInterface4 *psos4 = NULL;
    CLRDATA_ADDRESS arguments[3];
    HRESULT Status;

    if (SUCCEEDED(Status = g_sos->QueryInterface(__uuidof(ISOSDacInterface4), (void**) &psos4)))
    {
        int count = _countof(arguments);
        int countNeeded = 0;

        Status = psos4->GetClrNotification(arguments, count, &countNeeded);
        psos4->Release();

        if (SUCCEEDED(Status))
        {
            memset(&pdle->ExceptionRecord, 0, sizeof(pdle->ExceptionRecord));
            pdle->FirstChance = TRUE;
            pdle->ExceptionRecord.ExceptionCode = CLRDATA_NOTIFY_EXCEPTION;

            _ASSERTE(count <= EXCEPTION_MAXIMUM_PARAMETERS);
            for (int i = 0; i < count; i++)
            {
                pdle->ExceptionRecord.ExceptionInformation[i] = arguments[i];
            }
            // The rest of the ExceptionRecord isn't used by TranslateExceptionRecordToNotification
            return TRUE;
        }
        // No pending exception notification
        return FALSE;
    }

    // The new DAC based interface doesn't exists so ask the debugger for the last exception 
    // information. NOTE: this function doesn't work on xplat version when the coreclr symbols
    // have been stripped.

    ULONG Type, ProcessId, ThreadId;
    ULONG ExtraInformationUsed;
    Status = g_ExtControl->GetLastEventInformation(
        &Type,
        &ProcessId,
        &ThreadId,
        pdle,
        sizeof(DEBUG_LAST_EVENT_INFO_EXCEPTION),
        &ExtraInformationUsed,
        NULL,
        0,
        NULL);

    if (Status != S_OK || Type != DEBUG_EVENT_EXCEPTION)
    {
        return FALSE;
    }

    if (!pdle->FirstChance || pdle->ExceptionRecord.ExceptionCode != CLRDATA_NOTIFY_EXCEPTION)
    {
        return FALSE;
    }

    return TRUE;
}

HRESULT HandleCLRNotificationEvent()
{
    /*
     * Did we get module load notification? If so, check if any in our pending list
     * need to be registered for jit notification.
     *
     * Did we get a jit notification? If so, check if any can be removed and
     * real breakpoints be set.
     */
    DEBUG_LAST_EVENT_INFO_EXCEPTION dle;
    CNotification Notification;

    if (!CheckCLRNotificationEvent(&dle))
    {
#ifndef FEATURE_PAL
        ExtOut("Expecting first chance CLRN exception\n");
        return E_FAIL;
#else
        g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, "process continue", 0);
        return S_OK;
#endif
    }

    // Notification only needs to live for the lifetime of the call below, so it's a non-static
    // local.
    HRESULT Status = g_clrData->TranslateExceptionRecordToNotification(&dle.ExceptionRecord, &Notification);
    if (Status != S_OK)
    {
        ExtErr("Error processing exception notification\n");
        return Status;
    }
    else
    {
        switch (Notification.GetDebugStatus())
        {
            case DEBUG_STATUS_GO:
            case DEBUG_STATUS_GO_HANDLED:
            case DEBUG_STATUS_GO_NOT_HANDLED:
#ifndef FEATURE_PAL
                g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, "g", 0);
#else
                g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, "process continue", 0);
#endif
                break;
            default:
                break;
        }
    }

    return S_OK;
}

#ifndef FEATURE_PAL

DECLARE_API(HandleCLRN)
{
    INIT_API();    
    MINIDUMP_NOT_SUPPORTED();    

    return HandleCLRNotificationEvent();
}

#else // FEATURE_PAL

HRESULT HandleExceptionNotification(ILLDBServices *client)
{
    INIT_API();
    return HandleCLRNotificationEvent();
}

#endif // FEATURE_PAL

DECLARE_API(bpmd)
{
    INIT_API_NOEE();
    MINIDUMP_NOT_SUPPORTED();
    char buffer[1024];
    
    if (IsDumpFile())
    {
        ExtOut(SOSPrefix "bpmd is not supported on a dump file.\n");
        return Status;
    }


    // We keep a list of managed breakpoints the user wants to set, and display pending bps
    // bpmd. If you call bpmd <module name> <method> we will set or update an existing bp.
    // bpmd acts as a feeder of breakpoints to bp when the time is right.
    //

    StringHolder DllName,TypeName;
    int lineNumber = 0;
    size_t Offset = 0;

    DWORD_PTR pMD = NULL;
    BOOL fNoFutureModule = FALSE;
    BOOL fList = FALSE;
    size_t clearItem = 0; 
    BOOL fClearAll = FALSE;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-md", &pMD, COHEX, TRUE},
        {"-nofuturemodule", &fNoFutureModule, COBOOL, FALSE},
        {"-list", &fList, COBOOL, FALSE},
        {"-clear", &clearItem, COSIZE_T, TRUE},
        {"-clearall", &fClearAll, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&DllName.data, COSTRING},
        {&TypeName.data, COSTRING},
        {&Offset, COSIZE_T},
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    bool fBadParam = false;
    bool fIsFilename = false;
    int commandsParsed = 0;

    if (pMD != NULL)
    {
        if (nArg != 0)
        {
            fBadParam = true;
        }
        commandsParsed++;
    }
    if (fList)
    {
        commandsParsed++;
        if (nArg != 0)
        {
            fBadParam = true;
        }
    }
    if (fClearAll)
    {
        commandsParsed++;
        if (nArg != 0)
        {
            fBadParam = true;
        }
    }
    if (clearItem != 0)
    {
        commandsParsed++;
        if (nArg != 0)
        {
            fBadParam = true;
        }
    }
    if (1 <= nArg && nArg <= 3)
    {
        commandsParsed++;
        // did we get dll and type name or file:line#? Search for a colon in the first arg
        // to see if it is in fact a file:line#
        CHAR* pColon = strchr(DllName.data, ':');
#ifndef FEATURE_PAL 
        if (FAILED(g_ExtSymbols->GetModuleByModuleName(MAIN_CLR_MODULE_NAME_A, 0, NULL, NULL))) {
#else
        if (FAILED(g_ExtSymbols->GetModuleByModuleName(MAIN_CLR_DLL_NAME_A, 0, NULL, NULL))) {
#endif
           ExtOut("%s not loaded yet\n", MAIN_CLR_DLL_NAME_A);
           return Status;
        }

        if(NULL != pColon)
        {
            fIsFilename = true;
            *pColon = '\0';
            pColon++;
            if(1 != sscanf_s(pColon, "%d", &lineNumber))
            {
                ExtOut("Unable to parse line number\n");
                fBadParam = true;
            }
            else if(lineNumber < 0)
            {
                ExtOut("Line number must be positive\n");
                fBadParam = true;
            }
            if(nArg != 1) fBadParam = 1;
        }
    }

    if (fBadParam || (commandsParsed != 1))
    {
        ExtOut("Usage: " SOSPrefix "bpmd -md <MethodDesc pointer>\n");
        ExtOut("Usage: " SOSPrefix "bpmd [-nofuturemodule] <module name> <managed function name> [<il offset>]\n");
        ExtOut("Usage: " SOSPrefix "bpmd <filename>:<line number>\n");
        ExtOut("Usage: " SOSPrefix "bpmd -list\n");
        ExtOut("Usage: " SOSPrefix "bpmd -clear <pending breakpoint number>\n");
        ExtOut("Usage: " SOSPrefix "bpmd -clearall\n");
#ifdef FEATURE_PAL
        ExtOut("See \"soshelp bpmd\" for more details.\n");
#else
        ExtOut("See \"!help bpmd\" for more details.\n");
#endif
        return Status;
    }

    if (fList)
    {
        g_bpoints.ListBreakpoints();
        return Status;
    }
    if (clearItem != 0)
    {
        g_bpoints.ClearBreakpoint(clearItem);
        return Status;
    }
    if (fClearAll)
    {
        g_bpoints.ClearAllBreakpoints();
        return Status;
    }
    // Add a breakpoint
    // Do we already have this breakpoint?
    // Or, before setting it, is the module perhaps already loaded and code
    // is available? If so, don't add to our pending list, just go ahead and
    // set the real breakpoint.    
    
    LPWSTR ModuleName = (LPWSTR)alloca(mdNameLen * sizeof(WCHAR));
    LPWSTR FunctionName = (LPWSTR)alloca(mdNameLen * sizeof(WCHAR));
    LPWSTR Filename = (LPWSTR)alloca(MAX_LONGPATH * sizeof(WCHAR));

    BOOL bNeedNotificationExceptions = FALSE;

    if (pMD == NULL)
    {
        int numModule = 0;
        int numMethods = 0;

        ArrayHolder<DWORD_PTR> moduleList = NULL;

        if(!fIsFilename)
        {
            MultiByteToWideChar(CP_ACP, 0, DllName.data, -1, ModuleName, mdNameLen);
            MultiByteToWideChar(CP_ACP, 0, TypeName.data, -1, FunctionName, mdNameLen);
        }
        else
        {
            MultiByteToWideChar(CP_ACP, 0, DllName.data, -1, Filename, MAX_LONGPATH);
        }

        // Get modules that may need a breakpoint bound
        if ((Status = CheckEEDll()) == S_OK)
        {
            if ((Status = LoadClrDebugDll()) != S_OK)
            {
                // if the EE is loaded but DAC isn't we should stop.
                DACMessage(Status);
                return Status;
            }
            g_bDacBroken = FALSE;                                       \

            // Get the module list
            moduleList = ModuleFromName(fIsFilename ? NULL : DllName.data, &numModule);

            // Its OK if moduleList is NULL
            // There is a very normal case when checking for modules after clr is loaded
            // but before any AppDomains or assemblies are created
            // for example:
            // >sxe ld:clr
            // >g
            // ...
            // ModLoad: clr.dll
            // >!bpmd Foo.dll Foo.Bar
        }
        // If LoadClrDebugDll() succeeded make sure we release g_clrData
        ToRelease<IXCLRDataProcess> spIDP(g_clrData);
        ToRelease<ISOSDacInterface> spISD(g_sos);
        ResetGlobals();
        
        // we can get here with EE not loaded => 0 modules
        //                      EE is loaded => 0 or more modules
        ArrayHolder<DWORD_PTR> pMDs = NULL;
        for (int iModule = 0; iModule < numModule; iModule++)
        {
            ToRelease<IXCLRDataModule> ModDef;
            if (g_sos->GetModule(moduleList[iModule], &ModDef) != S_OK)
            {
                continue;
            }

            HRESULT symbolsLoaded = S_FALSE;
            if(!fIsFilename)
            {
                g_bpoints.ResolvePendingNonModuleBoundBreakpoint(ModuleName, FunctionName, moduleList[iModule], (DWORD)Offset);
            }
            else
            {
                SymbolReader symbolReader;
                symbolsLoaded = g_bpoints.LoadSymbolsForModule(moduleList[iModule], &symbolReader);
                if(symbolsLoaded == S_OK &&
                   g_bpoints.ResolvePendingNonModuleBoundBreakpoint(Filename, lineNumber, moduleList[iModule], &symbolReader) == S_OK)
                {
                    // if we have symbols then get the function name so we can lookup the MethodDescs
                    mdMethodDef methodDefToken;
                    ULONG32 ilOffset;
                    if(SUCCEEDED(symbolReader.ResolveSequencePoint(Filename, lineNumber, moduleList[iModule], &methodDefToken, &ilOffset)))
                    {
                        ToRelease<IXCLRDataMethodDefinition> pMethodDef = NULL;
                        if (SUCCEEDED(ModDef->GetMethodDefinitionByToken(methodDefToken, &pMethodDef)))
                        {
                            ULONG32 nameLen = 0;
                            pMethodDef->GetName(0, mdNameLen, &nameLen, FunctionName);
                            
                            // get the size of the required buffer
                            int buffSize = WideCharToMultiByte(CP_ACP, 0, FunctionName, -1, TypeName.data, 0, NULL, NULL);
                            
                            TypeName.data = new NOTHROW char[buffSize];
                            if (TypeName.data != NULL)
                            {
                                int bytesWritten = WideCharToMultiByte(CP_ACP, 0, FunctionName, -1, TypeName.data, buffSize, NULL, NULL);
                                _ASSERTE(bytesWritten == buffSize);
                            }
                        }
                    }
                }
            }

            HRESULT gotMethodDescs = GetMethodDescsFromName(moduleList[iModule], ModDef, TypeName.data, &pMDs, &numMethods);
            if (FAILED(gotMethodDescs) && (!fIsFilename))
            {
                // BPs via file name will enumerate through modules so there will be legitimate failures.
                // for module/type name we already found a match so this shouldn't fail (this is the original behavior).
                ExtOut("Error getting MethodDescs for module %p\n", moduleList[iModule]);
                return Status;
            }

            // for filename+line number only print extra info if symbols for this module are loaded (it can get quite noisy otherwise).
            if ((!fIsFilename) || (fIsFilename && symbolsLoaded == S_OK))
            {
                for (int i = 0; i < numMethods; i++)
                {
                    if (pMDs[i] == MD_NOT_YET_LOADED)
                    {
                        continue;
                    }
                    ExtOut("MethodDesc = %p\n", SOS_PTR(pMDs[i]));
                }
            }

            if (g_bpoints.Update(moduleList[iModule], FALSE))
            {
                bNeedNotificationExceptions = TRUE;
            }
        }

        if (!fNoFutureModule)
        {
            // add a pending breakpoint that will find future loaded modules, and
            // wait for the module load notification.
            if (!fIsFilename)
            {
                g_bpoints.Add(ModuleName, FunctionName, NULL, (DWORD)Offset);
            }
            else
            {
                g_bpoints.Add(Filename, lineNumber, NULL);
            }
            bNeedNotificationExceptions = TRUE;

            ULONG32 flags = 0;
            g_clrData->GetOtherNotificationFlags(&flags);
            flags |= (CLRDATA_NOTIFY_ON_MODULE_LOAD | CLRDATA_NOTIFY_ON_MODULE_UNLOAD);
            g_clrData->SetOtherNotificationFlags(flags);
        }
    }
    else /* We were given a MethodDesc already */
    {
        // if we've got an explicit MD, then we better have CLR and mscordacwks loaded
        INIT_API_EE()
        INIT_API_DAC();

        DacpMethodDescData MethodDescData;
        ExtOut("MethodDesc = %p\n", SOS_PTR(pMD));
        if (MethodDescData.Request(g_sos, TO_CDADDR(pMD)) != S_OK)
        {
            ExtOut("%p is not a valid MethodDesc\n", SOS_PTR(pMD));
            return Status;
        }
        
        if (MethodDescData.bHasNativeCode)
        {
            IssueDebuggerBPCommand((size_t) MethodDescData.NativeCodeAddr);
        }
        else if (MethodDescData.bIsDynamic)
        {
#ifndef FEATURE_PAL
            // Dynamic methods don't have JIT notifications. This is something we must
            // fix in the next release. Until then, you have a cumbersome user experience.
            ExtOut("This DynamicMethodDesc is not yet JITTED. Placing memory breakpoint at %p\n",
                MethodDescData.AddressOfNativeCodeSlot);
            
            sprintf_s(buffer, _countof(buffer),
#ifdef _TARGET_WIN64_
                "ba w8"
#else
                "ba w4" 
#endif // _TARGET_WIN64_

                " /1 %p \"bp poi(%p); g\"",
                (void*) (size_t) MethodDescData.AddressOfNativeCodeSlot,
                (void*) (size_t) MethodDescData.AddressOfNativeCodeSlot);

            Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);
            if (FAILED(Status))
            {
                ExtOut("Unable to set breakpoint with IDebugControl::Execute: %x\n",Status);
                ExtOut("Attempted to run: %s\n", buffer);                
            }            
#else
            ExtErr("This DynamicMethodDesc is not yet JITTED %p\n", MethodDescData.AddressOfNativeCodeSlot);
#endif // FEATURE_PAL
        }
        else
        {
            // Must issue a pending breakpoint.
            if (g_sos->GetMethodDescName(pMD, mdNameLen, FunctionName, NULL) != S_OK)
            {
                ExtOut("Unable to get method name for MethodDesc %p\n", SOS_PTR(pMD));
                return Status;
            }

            FileNameForModule ((DWORD_PTR) MethodDescData.ModulePtr, ModuleName);

            // We didn't find code, add a breakpoint.
            g_bpoints.ResolvePendingNonModuleBoundBreakpoint(ModuleName, FunctionName, TO_TADDR(MethodDescData.ModulePtr), 0);
            g_bpoints.Update(TO_TADDR(MethodDescData.ModulePtr), FALSE);
            bNeedNotificationExceptions = TRUE;            
        }
    }

    if (bNeedNotificationExceptions)
    {
        ExtOut("Adding pending breakpoints...\n");
#ifndef FEATURE_PAL
        sprintf_s(buffer, _countof(buffer), "sxe -c \"!HandleCLRN\" clrn");
        Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);        
#else
        Status = g_ExtServices->SetExceptionCallback(HandleExceptionNotification);
#endif // FEATURE_PAL
    }

    return Status;
}

#ifndef FEATURE_PAL

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the managed threadpool            *
*                                                                      *
\**********************************************************************/
DECLARE_API(ThreadPool)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    DacpThreadpoolData threadpool;

    if ((Status = threadpool.Request(g_sos)) == S_OK)
    {
        BOOL doHCDump = FALSE, doWorkItemDump = FALSE, dml = FALSE;

        CMDOption option[] = 
        {   // name, vptr, type, hasValue
            {"-ti", &doHCDump, COBOOL, FALSE},
            {"-wi", &doWorkItemDump, COBOOL, FALSE},
#ifndef FEATURE_PAL
            {"/d", &dml, COBOOL, FALSE},
#endif
        };    

        if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
        {
            return Status;
        }

        EnableDMLHolder dmlHolder(dml);

        ExtOut ("CPU utilization: %d%%\n", threadpool.cpuUtilization);            
        ExtOut ("Worker Thread:");
        ExtOut (" Total: %d", threadpool.NumWorkingWorkerThreads + threadpool.NumIdleWorkerThreads + threadpool.NumRetiredWorkerThreads);
        ExtOut (" Running: %d", threadpool.NumWorkingWorkerThreads);
        ExtOut (" Idle: %d", threadpool.NumIdleWorkerThreads);
        ExtOut (" MaxLimit: %d", threadpool.MaxLimitTotalWorkerThreads);        
        ExtOut (" MinLimit: %d", threadpool.MinLimitTotalWorkerThreads);        
        ExtOut ("\n");        

        int numWorkRequests = 0;
        CLRDATA_ADDRESS workRequestPtr = threadpool.FirstUnmanagedWorkRequest;
        DacpWorkRequestData workRequestData;
        while (workRequestPtr)
        {
            if ((Status = workRequestData.Request(g_sos,workRequestPtr))!=S_OK)
            {
                ExtOut("    Failed to examine a WorkRequest\n");
                return Status;
            }
            numWorkRequests++;
            workRequestPtr = workRequestData.NextWorkRequest;
        }

        ExtOut ("Work Request in Queue: %d\n", numWorkRequests);    
        workRequestPtr = threadpool.FirstUnmanagedWorkRequest;
        while (workRequestPtr)
        {
            if ((Status = workRequestData.Request(g_sos,workRequestPtr))!=S_OK)
            {
                ExtOut("    Failed to examine a WorkRequest\n");
                return Status;
            }

            if (workRequestData.Function == threadpool.AsyncTimerCallbackCompletionFPtr)
                ExtOut ("    AsyncTimerCallbackCompletion TimerInfo@%p\n", SOS_PTR(workRequestData.Context));
            else
                ExtOut ("    Unknown Function: %p  Context: %p\n", SOS_PTR(workRequestData.Function),
                    SOS_PTR(workRequestData.Context));

            workRequestPtr = workRequestData.NextWorkRequest;
        }

        if (doWorkItemDump && g_snapshot.Build())
        {
            // Display a message if the heap isn't verified.
            sos::GCHeap gcheap;
            if (!gcheap.AreGCStructuresValid())
            {
                DisplayInvalidStructuresMessage();
            }

            // Walk every heap item looking for the global queue and local queues.
            ExtOut("\nQueued work items:\n%" POINTERSIZE "s %" POINTERSIZE "s %s\n", "Queue", "Address", "Work Item");
            HeapStat stats;
            for (sos::ObjectIterator itr = gcheap.WalkHeap(); !IsInterrupt() && itr != NULL; ++itr)
            {
                if (_wcscmp(itr->GetTypeName(), W("System.Threading.ThreadPoolWorkQueue")) == 0)
                {
                    // We found a global queue (there should be only one, given one AppDomain).
                    // Get its workItems ConcurrentQueue<IThreadPoolWorkItem>.
                    int offset = GetObjFieldOffset(itr->GetAddress(), itr->GetMT(), W("workItems"));
                    if (offset > 0)
                    {
                        DWORD_PTR workItemsConcurrentQueuePtr;
                        MOVE(workItemsConcurrentQueuePtr, itr->GetAddress() + offset);
                        if (sos::IsObject(workItemsConcurrentQueuePtr, false))
                        {
                            // We got the ConcurrentQueue.  Get its head segment.
                            sos::Object workItemsConcurrentQueue = TO_TADDR(workItemsConcurrentQueuePtr);
                            offset = GetObjFieldOffset(workItemsConcurrentQueue.GetAddress(), workItemsConcurrentQueue.GetMT(), W("_head"));
                            if (offset > 0)
                            {
                                // Now, walk from segment to segment, each of which contains an array of work items.
                                DWORD_PTR segmentPtr;
                                MOVE(segmentPtr, workItemsConcurrentQueue.GetAddress() + offset);
                                while (sos::IsObject(segmentPtr, false))
                                {
                                    sos::Object segment = TO_TADDR(segmentPtr);

                                    // Get the work items array.  It's an array of Slot structs, which starts with the T.
                                    offset = GetObjFieldOffset(segment.GetAddress(), segment.GetMT(), W("_slots"));
                                    if (offset <= 0)
                                    {
                                        break;
                                    }

                                    DWORD_PTR slotsPtr;
                                    MOVE(slotsPtr, segment.GetAddress() + offset);
                                    if (!sos::IsObject(slotsPtr, false))
                                    {
                                        break;
                                    }

                                    // Walk every element in the array, outputting details on non-null work items.
                                    DacpObjectData slotsArray;
                                    if (slotsArray.Request(g_sos, TO_CDADDR(slotsPtr)) == S_OK && slotsArray.ObjectType == OBJ_ARRAY)
                                    {
                                        for (int i = 0; i < slotsArray.dwNumComponents; i++)
                                        {
                                            CLRDATA_ADDRESS workItemPtr;
                                            MOVE(workItemPtr, TO_CDADDR(slotsArray.ArrayDataPtr + (i * slotsArray.dwComponentSize))); // the item object reference is at the beginning of the Slot
                                            if (workItemPtr != NULL && sos::IsObject(workItemPtr, false))
                                            {
                                                sos::Object workItem = TO_TADDR(workItemPtr);
                                                stats.Add((DWORD_PTR)workItem.GetMT(), (DWORD)workItem.GetSize());
                                                DMLOut("%" POINTERSIZE "s %s %S", "[Global]", DMLObject(workItem.GetAddress()), workItem.GetTypeName());
                                                if ((offset = GetObjFieldOffset(workItem.GetAddress(), workItem.GetMT(), W("_callback"))) > 0 ||
                                                    (offset = GetObjFieldOffset(workItem.GetAddress(), workItem.GetMT(), W("m_action"))) > 0)
                                                {
                                                    CLRDATA_ADDRESS delegatePtr;
                                                    MOVE(delegatePtr, workItem.GetAddress() + offset);
                                                    CLRDATA_ADDRESS md;
                                                    if (TryGetMethodDescriptorForDelegate(delegatePtr, &md))
                                                    {
                                                        NameForMD_s((DWORD_PTR)md, g_mdName, mdNameLen);
                                                        ExtOut(" => %S", g_mdName);
                                                    }
                                                }
                                                ExtOut("\n");
                                            }
                                        }
                                    }

                                    // Move to the next segment.
                                    DacpFieldDescData segmentField;
                                    offset = GetObjFieldOffset(segment.GetAddress(), segment.GetMT(), W("_nextSegment"), TRUE, &segmentField);
                                    if (offset <= 0)
                                    {
                                        break;
                                    }

                                    MOVE(segmentPtr, segment.GetAddress() + offset);
                                    if (segmentPtr == NULL)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (_wcscmp(itr->GetTypeName(), W("System.Threading.ThreadPoolWorkQueue+WorkStealingQueue")) == 0)
                {
                    // We found a local queue.  Get its work items array.
                    int offset = GetObjFieldOffset(itr->GetAddress(), itr->GetMT(), W("m_array"));
                    if (offset > 0)
                    {
                        // Walk every element in the array, outputting details on non-null work items.
                        DWORD_PTR workItemArrayPtr;
                        MOVE(workItemArrayPtr, itr->GetAddress() + offset);
                        DacpObjectData workItemArray;
                        if (workItemArray.Request(g_sos, TO_CDADDR(workItemArrayPtr)) == S_OK && workItemArray.ObjectType == OBJ_ARRAY)
                        {
                            for (int i = 0; i < workItemArray.dwNumComponents; i++)
                            {
                                CLRDATA_ADDRESS workItemPtr;
                                MOVE(workItemPtr, TO_CDADDR(workItemArray.ArrayDataPtr + (i * workItemArray.dwComponentSize)));
                                if (workItemPtr != NULL && sos::IsObject(workItemPtr, false))
                                {
                                    sos::Object workItem = TO_TADDR(workItemPtr);
                                    stats.Add((DWORD_PTR)workItem.GetMT(), (DWORD)workItem.GetSize());
                                    DMLOut("%s %s %S", DMLObject(itr->GetAddress()), DMLObject(workItem.GetAddress()), workItem.GetTypeName());
                                    if ((offset = GetObjFieldOffset(workItem.GetAddress(), workItem.GetMT(), W("_callback"))) > 0 ||
                                        (offset = GetObjFieldOffset(workItem.GetAddress(), workItem.GetMT(), W("m_action"))) > 0)
                                    {
                                        CLRDATA_ADDRESS delegatePtr;
                                        MOVE(delegatePtr, workItem.GetAddress() + offset);
                                        CLRDATA_ADDRESS md;
                                        if (TryGetMethodDescriptorForDelegate(delegatePtr, &md))
                                        {
                                            NameForMD_s((DWORD_PTR)md, g_mdName, mdNameLen);
                                            ExtOut(" => %S", g_mdName);
                                        }
                                    }
                                    ExtOut("\n");
                                }
                            }
                        }
                    }
                }
            }

            // Output a summary.
            stats.Sort();
            stats.Print();
            ExtOut("\n");
        }

        if (doHCDump)
        {
            ExtOut ("--------------------------------------\n");
            ExtOut ("\nThread Injection History\n");
            if (threadpool.HillClimbingLogSize > 0)
            {
                static char const * const TransitionNames[] = 
                {
                    "Warmup", 
                    "Initializing",
                    "RandomMove",
                    "ClimbingMove",
                    "ChangePoint",
                    "Stabilizing",
                    "Starvation",
                    "ThreadTimedOut",
                    "Undefined"
                };

                ExtOut("\n    Time Transition     New #Threads      #Samples   Throughput\n");
                DacpHillClimbingLogEntry entry;

                // get the most recent entry first, so we can calculate time offsets
                
                int index = (threadpool.HillClimbingLogFirstIndex + threadpool.HillClimbingLogSize-1) % HillClimbingLogCapacity;
                CLRDATA_ADDRESS entryPtr = threadpool.HillClimbingLog + (index * sizeof(HillClimbingLogEntry));
                if ((Status = entry.Request(g_sos,entryPtr))!=S_OK)
                {
                    ExtOut("    Failed to examine a HillClimbing log entry\n");
                    return Status;
                }
                DWORD endTime = entry.TickCount;

                for (int i = 0; i < threadpool.HillClimbingLogSize; i++)
                {
                    index = (i + threadpool.HillClimbingLogFirstIndex) % HillClimbingLogCapacity;
                    entryPtr = threadpool.HillClimbingLog + (index * sizeof(HillClimbingLogEntry));

                    if ((Status = entry.Request(g_sos,entryPtr))!=S_OK)
                    {
                        ExtOut("    Failed to examine a HillClimbing log entry\n");
                        return Status;
                    }

                    ExtOut("%8.2lf %-14s %12d  %12d  %11.2lf\n",
                        (double)(int)(entry.TickCount - endTime) / 1000.0,
                        TransitionNames[entry.Transition],
                        entry.NewControlSetting,
                        entry.LastHistoryCount,
                        entry.LastHistoryMean);
                }
            }
        }
            
        ExtOut ("--------------------------------------\n");
        ExtOut ("Number of Timers: %d\n", threadpool.NumTimers);
        ExtOut ("--------------------------------------\n");
        
        ExtOut ("Completion Port Thread:");
        ExtOut ("Total: %d", threadpool.NumCPThreads);    
        ExtOut (" Free: %d", threadpool.NumFreeCPThreads);    
        ExtOut (" MaxFree: %d", threadpool.MaxFreeCPThreads);    
        ExtOut (" CurrentLimit: %d", threadpool.CurrentLimitTotalCPThreads);
        ExtOut (" MaxLimit: %d", threadpool.MaxLimitTotalCPThreads);
        ExtOut (" MinLimit: %d", threadpool.MinLimitTotalCPThreads);        
        ExtOut ("\n");
    }
    else
    {
        ExtOut("Failed to request ThreadpoolMgr information\n");
    }
    return Status;
}

#endif // FEATURE_PAL

DECLARE_API(FindAppDomain)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    DWORD_PTR p_Object = NULL;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&p_Object, COHEX},
    };
    size_t nArg;

    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    
    if ((p_Object == 0) || !sos::IsObject(p_Object))
    {
        ExtOut("%p is not a valid object\n", SOS_PTR(p_Object));
        return Status;
    }
    
    DacpAppDomainStoreData adstore;
    if (adstore.Request(g_sos) != S_OK)
    {
        ExtOut("Error getting AppDomain information\n");
        return Status;
    }    

    CLRDATA_ADDRESS appDomain = GetAppDomain (TO_CDADDR(p_Object));

    if (appDomain != NULL)
    {
        DMLOut("AppDomain: %s\n", DMLDomain(appDomain));
        if (appDomain == adstore.sharedDomain)
        {
            ExtOut("Name:      Shared Domain\n");
            ExtOut("ID:        (shared domain)\n");            
        }
        else if (appDomain == adstore.systemDomain)
        {
            ExtOut("Name:      System Domain\n");
            ExtOut("ID:        (system domain)\n");
        }
        else
        {
            DacpAppDomainData domain;
            if ((domain.Request(g_sos, appDomain) != S_OK) ||
                (g_sos->GetAppDomainName(appDomain,mdNameLen,g_mdName, NULL)!=S_OK))
            {
                ExtOut("Error getting AppDomain %p.\n", SOS_PTR(appDomain));
                return Status;
            }

            ExtOut("Name:      %S\n", (g_mdName[0]!=L'\0') ? g_mdName : W("None"));
            ExtOut("ID:        %d\n", domain.dwId);
        }
    }
    else
    {
        ExtOut("The type is declared in the shared domain and other\n");
        ExtOut("methods of finding the AppDomain failed. Try running\n");
        if (IsDMLEnabled())
            DMLOut("<exec cmd=\"!gcroot /d %p\">!gcroot %p</exec>, and if you find a root on a\n", p_Object, p_Object);
        else
            ExtOut(SOSPrefix "gcroot %p, and if you find a root on a\n", p_Object);
        ExtOut("stack, check the AppDomain of that stack with " SOSThreads ".\n");
        ExtOut("Note that the Thread could have transitioned between\n");
        ExtOut("multiple AppDomains.\n");
    }
    
    return Status;
}

#ifndef FEATURE_PAL

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to get the COM state (e.g. APT,contexe    *
*    activity.                                                         *  
*                                                                      *
\**********************************************************************/
#ifdef FEATURE_COMINTEROP
DECLARE_API(COMState)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    

    ULONG numThread;
    ULONG maxId;
    g_ExtSystem->GetTotalNumberThreads(&numThread,&maxId);

    ULONG curId;
    g_ExtSystem->GetCurrentThreadId(&curId);

    SIZE_T AllocSize;
    if (!ClrSafeInt<SIZE_T>::multiply(sizeof(ULONG), numThread, AllocSize))
    {
        ExtOut("  Error!  integer overflow on numThread 0x%08x\n", numThread);
        return Status;
    }
    ULONG *ids = (ULONG*)alloca(AllocSize);
    ULONG *sysIds = (ULONG*)alloca(AllocSize);
    g_ExtSystem->GetThreadIdsByIndex(0,numThread,ids,sysIds);
#if defined(_TARGET_WIN64_)
    ExtOut("      ID             TEB  APT    APTId CallerTID          Context\n");
#else
    ExtOut("     ID     TEB   APT    APTId CallerTID Context\n");
#endif
    for (ULONG i = 0; i < numThread; i ++) {
        g_ExtSystem->SetCurrentThreadId(ids[i]);
        CLRDATA_ADDRESS cdaTeb;
        g_ExtSystem->GetCurrentThreadTeb(&cdaTeb);
        ExtOut("%3d %4x %p", ids[i], sysIds[i], SOS_PTR(CDA_TO_UL64(cdaTeb)));
        // Apartment state
        TADDR OleTlsDataAddr;
        if (SafeReadMemory(TO_TADDR(cdaTeb) + offsetof(TEB,ReservedForOle),
                            &OleTlsDataAddr,
                            sizeof(OleTlsDataAddr), NULL) && OleTlsDataAddr != 0) {
            DWORD AptState;
            if (SafeReadMemory(OleTlsDataAddr+offsetof(SOleTlsData,dwFlags),
                               &AptState,
                               sizeof(AptState), NULL)) {
                if (AptState & OLETLS_APARTMENTTHREADED) {
                    ExtOut(" STA");
                }
                else if (AptState & OLETLS_MULTITHREADED) {
                    ExtOut(" MTA");
                }
                else if (AptState & OLETLS_INNEUTRALAPT) {
                    ExtOut(" NTA");
                }
                else {
                    ExtOut(" Ukn");
                }

                // Read these fields only if we were able to read anything of the SOleTlsData structure
                DWORD dwApartmentID;
                if (SafeReadMemory(OleTlsDataAddr+offsetof(SOleTlsData,dwApartmentID),
                                   &dwApartmentID,
                                   sizeof(dwApartmentID), NULL)) {
                    ExtOut(" %8x", dwApartmentID);
                }
                else
                    ExtOut(" %8x", 0);
                
                DWORD dwTIDCaller;
                if (SafeReadMemory(OleTlsDataAddr+offsetof(SOleTlsData,dwTIDCaller),
                                   &dwTIDCaller,
                                   sizeof(dwTIDCaller), NULL)) {
                    ExtOut("  %8x", dwTIDCaller);
                }
                else
                    ExtOut("  %8x", 0);
                
                size_t Context;
                if (SafeReadMemory(OleTlsDataAddr+offsetof(SOleTlsData,pCurrentCtx),
                                   &Context,
                                   sizeof(Context), NULL)) {
                    ExtOut(" %p", SOS_PTR(Context));
                }
                else
                    ExtOut(" %p", SOS_PTR(0));

            }
            else
                ExtOut(" Ukn");            
        }
        else
            ExtOut(" Ukn");
        ExtOut("\n");
    }

    g_ExtSystem->SetCurrentThreadId(curId);
    return Status;
}
#endif // FEATURE_COMINTEROP

#endif // FEATURE_PAL

BOOL traverseEh(UINT clauseIndex,UINT totalClauses,DACEHInfo *pEHInfo,LPVOID token)
{
    size_t methodStart = (size_t) token;
    
    if (IsInterrupt())
    {
        return FALSE;
    }

    ExtOut("EHHandler %d: %s ", clauseIndex, EHTypeName(pEHInfo->clauseType));

    LPCWSTR typeName = EHTypedClauseTypeName(pEHInfo);
    if (typeName != NULL)
    {
        ExtOut("catch(%S) ", typeName);
    }

    if (IsClonedFinally(pEHInfo))
        ExtOut("(cloned finally)");
    else if (pEHInfo->isDuplicateClause)
        ExtOut("(duplicate)");

    ExtOut("\n");
    ExtOut("Clause:  ");

    ULONG64 addrStart = pEHInfo->tryStartOffset + methodStart;
    ULONG64 addrEnd   = pEHInfo->tryEndOffset   + methodStart;

#ifdef _WIN64
    ExtOut("[%08x`%08x, %08x`%08x]",
            (ULONG)(addrStart >> 32), (ULONG)addrStart,
            (ULONG)(addrEnd   >> 32), (ULONG)addrEnd);
#else
    ExtOut("[%08x, %08x]", (ULONG)addrStart, (ULONG)addrEnd);
#endif

    ExtOut(" [%x, %x]\n", 
        (UINT32) pEHInfo->tryStartOffset,
        (UINT32) pEHInfo->tryEndOffset);

    ExtOut("Handler: ");

    addrStart = pEHInfo->handlerStartOffset + methodStart;
    addrEnd   = pEHInfo->handlerEndOffset   + methodStart;

#ifdef _WIN64
    ExtOut("[%08x`%08x, %08x`%08x]",
            (ULONG)(addrStart >> 32), (ULONG)addrStart,
            (ULONG)(addrEnd   >> 32), (ULONG)addrEnd);
#else
    ExtOut("[%08x, %08x]", (ULONG)addrStart, (ULONG)addrEnd);
#endif

    ExtOut(" [%x, %x]\n", 
        (UINT32) pEHInfo->handlerStartOffset,
        (UINT32) pEHInfo->handlerEndOffset);
    
    if (pEHInfo->clauseType == EHFilter)
    {
        ExtOut("Filter: ");

        addrStart = pEHInfo->filterOffset + methodStart;

#ifdef _WIN64
        ExtOut("[%08x`%08x]", (ULONG)(addrStart >> 32), (ULONG)addrStart);
#else
        ExtOut("[%08x]", (ULONG)addrStart);
#endif

        ExtOut(" [%x]\n",
            (UINT32) pEHInfo->filterOffset);
    }
    
    ExtOut("\n");        
    return TRUE;
}

DECLARE_API(EHInfo)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    DWORD_PTR dwStartAddr = NULL;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };

    CMDValue arg[] = 
    {   // vptr, type
        {&dwStartAddr, COHEX},
    };

    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg) || (0 == nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    DWORD_PTR tmpAddr = dwStartAddr;

    if (!IsMethodDesc(dwStartAddr)) 
    {
        JITTypes jitType;
        DWORD_PTR methodDesc;
        DWORD_PTR gcinfoAddr;
        IP2MethodDesc (dwStartAddr, methodDesc, jitType, gcinfoAddr);
        tmpAddr = methodDesc;
    }

    DacpMethodDescData MD;
    if ((tmpAddr == 0) || (MD.Request(g_sos, TO_CDADDR(tmpAddr)) != S_OK))
    {
        ExtOut("%p is not a MethodDesc\n", SOS_PTR(tmpAddr));
        return Status;
    }

    if (1 == nArg && !MD.bHasNativeCode)
    {
        ExtOut("No EH info available\n");
        return Status;
    }

    DacpCodeHeaderData codeHeaderData;
    if (codeHeaderData.Request(g_sos, TO_CDADDR(MD.NativeCodeAddr)) != S_OK)
    {
        ExtOut("Unable to get codeHeader information\n");
        return Status;
    }        
    
    DMLOut("MethodDesc:   %s\n", DMLMethodDesc(MD.MethodDescPtr));
    DumpMDInfo(TO_TADDR(MD.MethodDescPtr));

    ExtOut("\n");
    Status = g_sos->TraverseEHInfo(TO_CDADDR(MD.NativeCodeAddr), traverseEh, (LPVOID)MD.NativeCodeAddr);

    if (Status == E_ABORT)
    {
        ExtOut("<user aborted>\n");
    }
    else if (Status != S_OK)
    {
        ExtOut("Failed to perform EHInfo traverse\n");
    }
    
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the GC encoding of a managed      *
*    function.                                                         *  
*                                                                      *
\**********************************************************************/
DECLARE_API(GCInfo)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();
    

    TADDR taStartAddr = NULL;
    TADDR taGCInfoAddr;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&taStartAddr, COHEX},
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg) || (0 == nArg))
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    TADDR tmpAddr = taStartAddr;

    if (!IsMethodDesc(taStartAddr)) 
    {
        JITTypes jitType;
        TADDR methodDesc;
        TADDR gcinfoAddr;
        IP2MethodDesc(taStartAddr, methodDesc, jitType, gcinfoAddr);
        tmpAddr = methodDesc;
    }

    DacpMethodDescData MD;
    if ((tmpAddr == 0) || (MD.Request(g_sos, TO_CDADDR(tmpAddr)) != S_OK))
    {
        ExtOut("%p is not a valid MethodDesc\n", SOS_PTR(taStartAddr));
        return Status;
    }

    if (1 == nArg && !MD.bHasNativeCode)
    {
        ExtOut("No GC info available\n");
        return Status;
    }

    DacpCodeHeaderData codeHeaderData;

    if (
        // Try to get code header data from taStartAddr.  This will get the code
        // header corresponding to the IP address, even if the function was rejitted
        (codeHeaderData.Request(g_sos, TO_CDADDR(taStartAddr)) != S_OK) &&

        // If that didn't work, just try to use the code address that the MD
        // points to.  If the function was rejitted, this will only give you the
        // original JITted code, but that's better than nothing
        (codeHeaderData.Request(g_sos, TO_CDADDR(MD.NativeCodeAddr)) != S_OK)
        )
    {
        // We always used to emit this (before rejit support), even if we couldn't get
        // the code header, so keep on doing so.
        ExtOut("entry point %p\n", SOS_PTR(MD.NativeCodeAddr));

        // And now the error....
        ExtOut("Unable to get codeHeader information\n");
        return Status;
    }

    // We have the code header, so use it to determine the method start

    ExtOut("entry point %p\n", SOS_PTR(codeHeaderData.MethodStart));

    if (codeHeaderData.JITType == TYPE_UNKNOWN)
    {
        ExtOut("unknown Jit\n");
        return Status;
    }
    else if (codeHeaderData.JITType == TYPE_JIT)
    {
        ExtOut("Normal JIT generated code\n");
    }
    else if (codeHeaderData.JITType == TYPE_PJIT)
    {
        ExtOut("preJIT generated code\n");
    }

    taGCInfoAddr = TO_TADDR(codeHeaderData.GCInfo);

    ExtOut("GC info %p\n", SOS_PTR(taGCInfoAddr));

    // assume that GC encoding table is never more than
    // 40 + methodSize * 2
    int tableSize = 0;
    if (!ClrSafeInt<int>::multiply(codeHeaderData.MethodSize, 2, tableSize) ||
        !ClrSafeInt<int>::addition(tableSize, 40, tableSize))
    {
        ExtOut("<integer overflow>\n");
        return E_FAIL;
    }
    ArrayHolder<BYTE> table = new NOTHROW BYTE[tableSize];
    if (table == NULL)
    {
        ExtOut("Could not allocate memory to read the gc info.\n");
        return E_OUTOFMEMORY;
    }
    
    memset(table, 0, tableSize);
    // We avoid using move here, because we do not want to return
    if (!SafeReadMemory(taGCInfoAddr, table, tableSize, NULL))
    {
        ExtOut("Could not read memory %p\n", SOS_PTR(taGCInfoAddr));
        return Status;
    }

    // Mutable table pointer since we need to pass the appropriate
    // offset into the table to DumpGCTable.
    GCInfoToken gcInfoToken = { table, GCINFO_VERSION };
    unsigned int methodSize = (unsigned int)codeHeaderData.MethodSize;

    g_targetMachine->DumpGCInfo(gcInfoToken, methodSize, ExtOut, true /*encBytes*/, true /*bPrintHeader*/);

    return Status;
}

#if !defined(FEATURE_PAL)

void DecodeGCTableEntry (const char *fmt, ...)
{
    GCEncodingInfo *pInfo = (GCEncodingInfo*)GetFiberData();
    va_list va;

    //
    // Append the new data to the buffer
    //

    va_start(va, fmt);

    int cch = _vsnprintf_s(&pInfo->buf[pInfo->cch], _countof(pInfo->buf) - pInfo->cch, _countof(pInfo->buf) - pInfo->cch - 1, fmt, va);
    if (cch >= 0)
        pInfo->cch += cch;

    va_end(va);

    pInfo->buf[pInfo->cch] = '\0';

    //
    // If there are complete lines in the buffer, decode them.
    //

    for (;;)
    {
        char *pNewLine = strchr(pInfo->buf, '\n');

        if (!pNewLine)
            break;

        //
        // The line should start with a 16-bit (x86) or 32-bit (non-x86) hex
        // offset.  strtoul returns ULONG_MAX or 0 on failure.  0 is a valid
        // offset for the first encoding, or while the last offset was 0.
        //

        if (isxdigit(pInfo->buf[0]))
        {
            char *pEnd;
            ULONG ofs = strtoul(pInfo->buf, &pEnd, 16);

            if (   isspace(*pEnd)
                && -1 != ofs 
                && (   -1 == pInfo->ofs
                    || 0 == pInfo->ofs
                    || ofs > 0))
            {
                pInfo->ofs = ofs;
                *pNewLine = '\0';

                SwitchToFiber(pInfo->pvMainFiber);
            }
        }
        else if (0 == strncmp(pInfo->buf, "Untracked:", 10))
        {
            pInfo->ofs = 0;
            *pNewLine = '\0';

            SwitchToFiber(pInfo->pvMainFiber);
        }

        //
        // Shift the remaining data to the start of the buffer
        //

        strcpy_s(pInfo->buf, _countof(pInfo->buf), pNewLine+1);
        pInfo->cch = (int)strlen(pInfo->buf);
    }
}


VOID CALLBACK DumpGCTableFiberEntry (LPVOID pvGCEncodingInfo)
{
    GCEncodingInfo *pInfo = (GCEncodingInfo*)pvGCEncodingInfo;
    GCInfoToken gcInfoToken = { pInfo->table, GCINFO_VERSION };
    g_targetMachine->DumpGCInfo(gcInfoToken, pInfo->methodSize, DecodeGCTableEntry, false /*encBytes*/, false /*bPrintHeader*/);

    pInfo->fDoneDecoding = true;
    SwitchToFiber(pInfo->pvMainFiber);
}
#endif // !FEATURE_PAL

BOOL gatherEh(UINT clauseIndex,UINT totalClauses,DACEHInfo *pEHInfo,LPVOID token)
{
    SOSEHInfo *pInfo = (SOSEHInfo *) token;

    if (pInfo == NULL)
    {
        return FALSE;
    }
    
    if (pInfo->m_pInfos == NULL)
    {
        // First time, initialize structure
        pInfo->EHCount = totalClauses;
        pInfo->m_pInfos = new NOTHROW DACEHInfo[totalClauses];
        if (pInfo->m_pInfos == NULL)
        {
            ReportOOM();            
            return FALSE;
        }
    }

    pInfo->m_pInfos[clauseIndex] = *((DACEHInfo*)pEHInfo);
    return TRUE;
}


/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to unassembly a managed function.         *
*    It tries to print symbolic info for function call, contants...    *  
*                                                                      *
\**********************************************************************/
DECLARE_API(u)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    
    DWORD_PTR dwStartAddr = NULL;
    BOOL fWithGCInfo = FALSE;
    BOOL fWithEHInfo = FALSE;
    BOOL bSuppressLines = FALSE;
    BOOL bDisplayOffsets = FALSE;
    BOOL dml = FALSE;
    size_t nArg;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"-gcinfo", &fWithGCInfo, COBOOL, FALSE},
#endif
        {"-ehinfo", &fWithEHInfo, COBOOL, FALSE},
        {"-n", &bSuppressLines, COBOOL, FALSE},
        {"-o", &bDisplayOffsets, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&dwStartAddr, COHEX},
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg) || (nArg < 1))
    {
        return Status;
    }
    // symlines will be non-zero only if SYMOPT_LOAD_LINES was set in the symbol options
    ULONG symlines = 0;
    if (!bSuppressLines && SUCCEEDED(g_ExtSymbols->GetSymbolOptions(&symlines)))
    {
        symlines &= SYMOPT_LOAD_LINES;
    }
    bSuppressLines = bSuppressLines || (symlines == 0);

    EnableDMLHolder dmlHolder(dml);
    // dwStartAddr is either some IP address or a MethodDesc.  Start off assuming it's a
    // MethodDesc.
    DWORD_PTR methodDesc = dwStartAddr;
    if (!IsMethodDesc(methodDesc))
    {
        // Not a methodDesc, so gotta find it ourselves
        DWORD_PTR tmpAddr = dwStartAddr;
        JITTypes jt;
        DWORD_PTR gcinfoAddr;
        IP2MethodDesc (tmpAddr, methodDesc, jt,
                       gcinfoAddr);
        if (!methodDesc || jt == TYPE_UNKNOWN)
        {
            // It is not managed code.
            ExtOut("Unmanaged code\n");
            UnassemblyUnmanaged(dwStartAddr, bSuppressLines);
            return Status;
        }
    }

    DacpMethodDescData MethodDescData;
    if ((Status=MethodDescData.Request(g_sos, TO_CDADDR(methodDesc))) != S_OK)
    {
        ExtOut("Failed to get method desc for %p.\n", SOS_PTR(dwStartAddr));
        return Status;
    }    

    if (!MethodDescData.bHasNativeCode)
    {
        ExtOut("Not jitted yet\n");
        return Status;
    }

    // Get the appropriate code header. If we were passed an MD, then use
    // MethodDescData.NativeCodeAddr to find the code header; if we were passed an IP, use
    // that IP to find the code header. This ensures that, for rejitted functions, we
    // disassemble the rejit version that the user explicitly specified with their IP.
    DacpCodeHeaderData codeHeaderData;
    if (codeHeaderData.Request(
        g_sos, 
        TO_CDADDR(
            (dwStartAddr == methodDesc) ? MethodDescData.NativeCodeAddr : dwStartAddr)
            ) != S_OK)

    {
        ExtOut("Unable to get codeHeader information\n");
        return Status;
    }                
    
    if (codeHeaderData.MethodStart == 0)
    {
        ExtOut("not a valid MethodDesc\n");
        return Status;
    }
    
    if (codeHeaderData.JITType == TYPE_UNKNOWN)
    {
        ExtOut("unknown Jit\n");
        return Status;
    }
    else if (codeHeaderData.JITType == TYPE_JIT)
    {
        ExtOut("Normal JIT generated code\n");
    }
    else if (codeHeaderData.JITType == TYPE_PJIT)
    {
        ExtOut("preJIT generated code\n");
    }

    NameForMD_s(methodDesc, g_mdName, mdNameLen);
    ExtOut("%S\n", g_mdName);   
    if (codeHeaderData.ColdRegionStart != NULL)
    {
        ExtOut("Begin %p, size %x. Cold region begin %p, size %x\n",
            SOS_PTR(codeHeaderData.MethodStart), codeHeaderData.HotRegionSize,
            SOS_PTR(codeHeaderData.ColdRegionStart), codeHeaderData.ColdRegionSize);
    }
    else
    {
        ExtOut("Begin %p, size %x\n", SOS_PTR(codeHeaderData.MethodStart), codeHeaderData.MethodSize);
    }

#if !defined(FEATURE_PAL)
    //
    // Set up to mix gc info with the code if requested
    //

    GCEncodingInfo gcEncodingInfo = {0};

    // The actual GC Encoding Table, this is updated during the course of the function.
    gcEncodingInfo.table = NULL;

    // The holder to make sure we clean up the memory for the table
    ArrayHolder<BYTE> table = NULL;

    if (fWithGCInfo)
    {
        // assume that GC encoding table is never more than 40 + methodSize * 2
        int tableSize = 0;
        if (!ClrSafeInt<int>::multiply(codeHeaderData.MethodSize, 2, tableSize) ||
            !ClrSafeInt<int>::addition(tableSize, 40, tableSize))
        {
            ExtOut("<integer overflow>\n");
            return E_FAIL;
        }


        // Assign the new array to the mutable gcEncodingInfo table and to the
        // table ArrayHolder to clean this up when the function exits.
        table = gcEncodingInfo.table = new NOTHROW BYTE[tableSize];
        
        if (gcEncodingInfo.table == NULL)
        {
            ExtOut("Could not allocate memory to read the gc info.\n");
            return E_OUTOFMEMORY;
        }
        
        memset (gcEncodingInfo.table, 0, tableSize);
        // We avoid using move here, because we do not want to return
        if (!SafeReadMemory(TO_TADDR(codeHeaderData.GCInfo), gcEncodingInfo.table, tableSize, NULL))
        {
            ExtOut("Could not read memory %p\n", SOS_PTR(codeHeaderData.GCInfo));
            return Status;
        }

        //
        // Skip the info header
        //
        gcEncodingInfo.methodSize = (unsigned int)codeHeaderData.MethodSize;

        //
        // DumpGCTable will call gcPrintf for each encoding.  We'd like a "give
        // me the next encoding" interface, but we're stuck with the callback.
        // To reconcile this without messing up too much code, we'll create a
        // fiber to dump the gc table.  When we need the next gc encoding,
        // we'll switch to this fiber.  The callback will note the next offset,
        // and switch back to the main fiber.
        //

        gcEncodingInfo.ofs = -1;
        gcEncodingInfo.hotSizeToAdd = 0;
        
        gcEncodingInfo.pvMainFiber = ConvertThreadToFiber(NULL);
        if (!gcEncodingInfo.pvMainFiber && ERROR_ALREADY_FIBER == GetLastError())
            gcEncodingInfo.pvMainFiber = GetCurrentFiber();
        
        if (!gcEncodingInfo.pvMainFiber)
            return Status;

        gcEncodingInfo.pvGCTableFiber = CreateFiber(0, DumpGCTableFiberEntry, &gcEncodingInfo);
        if (!gcEncodingInfo.pvGCTableFiber)
            return Status;

        SwitchToFiber(gcEncodingInfo.pvGCTableFiber);
    }    
#endif

    SOSEHInfo *pInfo = NULL;
    if (fWithEHInfo)
    {
        pInfo = new NOTHROW SOSEHInfo;
        if (pInfo == NULL)
        {
            ReportOOM();                
        }
        else if (g_sos->TraverseEHInfo(MethodDescData.NativeCodeAddr, gatherEh, (LPVOID)pInfo) != S_OK)
        {
            ExtOut("Failed to gather EHInfo data\n");
            delete pInfo;
            pInfo = NULL;            
        }
    }
    
    if (codeHeaderData.ColdRegionStart == NULL)
    {
        g_targetMachine->Unassembly (
                (DWORD_PTR) codeHeaderData.MethodStart,
                ((DWORD_PTR)codeHeaderData.MethodStart) + codeHeaderData.MethodSize,
                dwStartAddr,
                (DWORD_PTR) MethodDescData.GCStressCodeCopy,
#if !defined(FEATURE_PAL)
                fWithGCInfo ? &gcEncodingInfo : 
#endif            
                NULL,
                pInfo,
                bSuppressLines,
                bDisplayOffsets
                );
    }
    else
    {
        ExtOut("Hot region:\n");
        g_targetMachine->Unassembly (
                (DWORD_PTR) codeHeaderData.MethodStart,
                ((DWORD_PTR)codeHeaderData.MethodStart) + codeHeaderData.HotRegionSize,
                dwStartAddr,
                (DWORD_PTR) MethodDescData.GCStressCodeCopy,
#if !defined(FEATURE_PAL)
                fWithGCInfo ? &gcEncodingInfo : 
#endif            
                NULL,
                pInfo,
                bSuppressLines,
                bDisplayOffsets
                );

        ExtOut("Cold region:\n");
        
#if !defined(FEATURE_PAL)
        // Displaying gcinfo for a cold region requires knowing the size of
        // the hot region preceeding.
        gcEncodingInfo.hotSizeToAdd = codeHeaderData.HotRegionSize;
#endif            
        g_targetMachine->Unassembly (
                (DWORD_PTR) codeHeaderData.ColdRegionStart,
                ((DWORD_PTR)codeHeaderData.ColdRegionStart) + codeHeaderData.ColdRegionSize,
                dwStartAddr,
                ((DWORD_PTR) MethodDescData.GCStressCodeCopy) + codeHeaderData.HotRegionSize,                
#if !defined(FEATURE_PAL)
                fWithGCInfo ? &gcEncodingInfo : 
#endif            
                NULL,
                pInfo,
                bSuppressLines,
                bDisplayOffsets
                );

    }

    if (pInfo)
    {
        delete pInfo;
        pInfo = NULL;
    }
    
#if !defined(FEATURE_PAL)
    if (fWithGCInfo)
        DeleteFiber(gcEncodingInfo.pvGCTableFiber);
#endif

    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the in-memory stress log          *
*    !DumpLog [filename]                                               *
*             will dump the stress log corresponding to the clr.dll    *
*             loaded in the debuggee's VAS                             *
*    !DumpLog -addr <addr_of_StressLog::theLog> [filename]             *
*             will dump the stress log associated with any DLL linked  *
*             against utilcode.lib, most commonly mscordbi.dll         *
*             (e.g. !DumpLog -addr mscordbi!StressLog::theLog)         *
*                                                                      *
\**********************************************************************/
DECLARE_API(DumpLog)
{        
    INIT_API_NO_RET_ON_FAILURE();

    MINIDUMP_NOT_SUPPORTED();    

    const char* fileName = "StressLog.txt";

    CLRDATA_ADDRESS StressLogAddress = NULL;
    
    StringHolder sFileName, sLogAddr;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-addr", &sLogAddr.data, COSTRING, TRUE}
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&sFileName.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (nArg > 0 && sFileName.data != NULL)
    {
        fileName = sFileName.data;
    }

    // allow users to specify -addr mscordbdi!StressLog::theLog, for example.
    if (sLogAddr.data != NULL)
    {
        StressLogAddress = GetExpression(sLogAddr.data);
    }

    if (StressLogAddress == NULL)
    {
        if (g_bDacBroken)
        {
#ifdef FEATURE_PAL
            ExtOut("No stress log address. DAC is broken; can't get it\n");
            return E_FAIL;
#else
            // Try to find stress log symbols
            DWORD_PTR dwAddr = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!StressLog::theLog");
            StressLogAddress = dwAddr;        
#endif
        }
        else if (g_sos->GetStressLogAddress(&StressLogAddress) != S_OK)
        {
            ExtOut("Unable to find stress log via DAC\n");
            return E_FAIL;
        }
    }

    if (StressLogAddress == NULL)
    {
        ExtOut("Please provide the -addr argument for the address of the stress log, since no recognized runtime is loaded.\n");
        return E_FAIL;
    }

    ExtOut("Attempting to dump Stress log to file '%s'\n", fileName);

    
    
    Status = StressLog::Dump(StressLogAddress, fileName, g_ExtData);

    if (Status == S_OK)
        ExtOut("SUCCESS: Stress log dumped\n");
    else if (Status == S_FALSE)
        ExtOut("No Stress log in the image, no file written\n");
    else
        ExtOut("FAILURE: Stress log not dumped\n");

    return Status;
}

#ifndef FEATURE_PAL
DECLARE_API (DumpGCLog)
{
    INIT_API_NODAC();
    MINIDUMP_NOT_SUPPORTED();    
    
    if (GetEEFlavor() == UNKNOWNEE) 
    {
        ExtOut("CLR not loaded\n");
        return Status;
    }

    const char* fileName = "GCLog.txt";
    int iLogSize = 1024*1024;
    BYTE* bGCLog = NULL;
    int iRealLogSize = iLogSize - 1;
    DWORD dwWritten = 0;

    while (isspace (*args))
        args ++;

    if (*args != 0)
        fileName = args;
    
    DWORD_PTR dwAddr = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!SVR::gc_log_buffer");
    moveN (dwAddr, dwAddr);

    if (dwAddr == 0)
    {
        dwAddr = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!WKS::gc_log_buffer");
        moveN (dwAddr, dwAddr);
        if (dwAddr == 0)
        {
            ExtOut("Can't get either WKS or SVR GC's log file");
            return E_FAIL;
        }
    }
    
    ExtOut("Dumping GC log at %08x\n", dwAddr);

    g_bDacBroken = FALSE;
    
    ExtOut("Attempting to dump GC log to file '%s'\n", fileName);
    
    Status = E_FAIL;

    HANDLE hGCLog = CreateFileA(
        fileName,
        GENERIC_WRITE,
        FILE_SHARE_READ,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if (hGCLog == INVALID_HANDLE_VALUE)
    {
        ExtOut("failed to create file: %d\n", GetLastError());
        goto exit;
    }

    bGCLog = new NOTHROW BYTE[iLogSize];
    if (bGCLog == NULL)
    {
        ReportOOM();
        goto exit;
    }

    memset (bGCLog, 0, iLogSize);
    if (!SafeReadMemory(dwAddr, bGCLog, iLogSize, NULL))
    {
        ExtOut("failed to read memory from %08x\n", dwAddr);
    }

    while (iRealLogSize >= 0)
    {
        if (bGCLog[iRealLogSize] != '*')
        {
            break;
        }

        iRealLogSize--;
    }

    WriteFile (hGCLog, bGCLog, iRealLogSize + 1, &dwWritten, NULL);

    Status = S_OK;

exit:

    if (bGCLog != NULL)
    {
        delete [] bGCLog;
    }

    if (hGCLog != INVALID_HANDLE_VALUE)
    {
        CloseHandle (hGCLog);
    }

    if (Status == S_OK)
        ExtOut("SUCCESS: Stress log dumped\n");
    else if (Status == S_FALSE)
        ExtOut("No Stress log in the image, no file written\n");
    else
        ExtOut("FAILURE: Stress log not dumped\n");

    return Status;
}

DECLARE_API (DumpGCConfigLog)
{
    INIT_API();
#ifdef GC_CONFIG_DRIVEN    
    MINIDUMP_NOT_SUPPORTED();    

    if (GetEEFlavor() == UNKNOWNEE) 
    {
        ExtOut("CLR not loaded\n");
        return Status;
    }

    const char* fileName = "GCConfigLog.txt";

    while (isspace (*args))
        args ++;

    if (*args != 0)
        fileName = args;
    
    if (!InitializeHeapData ())
    {
        ExtOut("GC Heap not initialized yet.\n");
        return S_OK;
    }

    BOOL fIsServerGC = IsServerBuild();

    DWORD_PTR dwAddr = 0; 
    DWORD_PTR dwAddrOffset = 0;
    
    if (fIsServerGC) 
    {
        dwAddr = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!SVR::gc_config_log_buffer");
        dwAddrOffset = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!SVR::gc_config_log_buffer_offset");
    }
    else
    {
        dwAddr = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!WKS::gc_config_log_buffer");
        dwAddrOffset = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!WKS::gc_config_log_buffer_offset");
    }

    moveN (dwAddr, dwAddr);
    moveN (dwAddrOffset, dwAddrOffset);

    if (dwAddr == 0)
    {
        ExtOut("Can't get either WKS or SVR GC's config log buffer");
        return E_FAIL;
    }
    
    ExtOut("Dumping GC log at %08x\n", dwAddr);

    g_bDacBroken = FALSE;
    
    ExtOut("Attempting to dump GC log to file '%s'\n", fileName);
    
    Status = E_FAIL;
    
    HANDLE hGCLog = CreateFileA(
        fileName,
        GENERIC_WRITE,
        FILE_SHARE_READ,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if (hGCLog == INVALID_HANDLE_VALUE)
    {
        ExtOut("failed to create file: %d\n", GetLastError());
        goto exit;
    }

    {
        int iLogSize = (int)dwAddrOffset;

        ArrayHolder<BYTE> bGCLog = new NOTHROW BYTE[iLogSize];
        if (bGCLog == NULL)
        {
            ReportOOM();
            goto exit;
        }

        memset (bGCLog, 0, iLogSize);
        if (!SafeReadMemory(dwAddr, bGCLog, iLogSize, NULL))
        {
            ExtOut("failed to read memory from %08x\n", dwAddr);
        }

        SetFilePointer (hGCLog, 0, 0, FILE_END);
        DWORD dwWritten;
        WriteFile (hGCLog, bGCLog, iLogSize, &dwWritten, NULL);
    }

    Status = S_OK;

exit:

    if (hGCLog != INVALID_HANDLE_VALUE)
    {
        CloseHandle (hGCLog);
    }

    if (Status == S_OK)
        ExtOut("SUCCESS: Stress log dumped\n");
    else if (Status == S_FALSE)
        ExtOut("No Stress log in the image, no file written\n");
    else
        ExtOut("FAILURE: Stress log not dumped\n");

    return Status;
#else
    ExtOut("Not implemented\n");
    return S_OK;
#endif //GC_CONFIG_DRIVEN
}
#endif // FEATURE_PAL

#ifdef GC_CONFIG_DRIVEN
static const char * const str_interesting_data_points[] =
{
    "pre short", // 0
    "post short", // 1
    "merged pins", // 2
    "converted pins", // 3
    "pre pin", // 4
    "post pin", // 5
    "pre and post pin", // 6
    "pre short padded", // 7
    "post short padded", // 7
};

static const char * const str_heap_compact_reasons[] = 
{
    "low on ephemeral space",
    "high fragmentation",
    "couldn't allocate gaps",
    "user specfied compact LOH",
    "last GC before OOM",
    "induced compacting GC",
    "fragmented gen0 (ephemeral GC)", 
    "high memory load (ephemeral GC)",
    "high memory load and frag",
    "very high memory load and frag",
    "no gc mode"
};

static BOOL gc_heap_compact_reason_mandatory_p[] =
{
    TRUE, //compact_low_ephemeral = 0,
    FALSE, //compact_high_frag = 1,
    TRUE, //compact_no_gaps = 2,
    TRUE, //compact_loh_forced = 3,
    TRUE, //compact_last_gc = 4
    TRUE, //compact_induced_compacting = 5,
    FALSE, //compact_fragmented_gen0 = 6, 
    FALSE, //compact_high_mem_load = 7, 
    TRUE, //compact_high_mem_frag = 8, 
    TRUE, //compact_vhigh_mem_frag = 9,
    TRUE //compact_no_gc_mode = 10
};

static const char * const str_heap_expand_mechanisms[] = 
{
    "reused seg with normal fit",
    "reused seg with best fit",
    "expand promoting eph",
    "expand with a new seg",
    "no memory for a new seg",
    "expand in next full GC"
};

static const char * const str_bit_mechanisms[] = 
{
    "using mark list",
    "demotion"
};

static const char * const str_gc_global_mechanisms[] =
{
    "concurrent GCs", 
    "compacting GCs",
    "promoting GCs",
    "GCs that did demotion",
    "card bundles",
    "elevation logic"
};

void PrintInterestingGCInfo(DacpGCInterestingInfoData* dataPerHeap)
{
    ExtOut("Interesting data points\n");
    size_t* data = dataPerHeap->interestingDataPoints;
    for (int i = 0; i < DAC_NUM_GC_DATA_POINTS; i++)
    {
        ExtOut("%20s: %d\n", str_interesting_data_points[i], data[i]);
    }

    ExtOut("\nCompacting reasons\n");
    data = dataPerHeap->compactReasons;
    for (int i = 0; i < DAC_MAX_COMPACT_REASONS_COUNT; i++)
    {
        ExtOut("[%s]%35s: %d\n", (gc_heap_compact_reason_mandatory_p[i] ? "M" : "W"), str_heap_compact_reasons[i], data[i]);
    }

    ExtOut("\nExpansion mechanisms\n");
    data = dataPerHeap->expandMechanisms;
    for (int i = 0; i < DAC_MAX_EXPAND_MECHANISMS_COUNT; i++)
    {
        ExtOut("%30s: %d\n", str_heap_expand_mechanisms[i], data[i]);
    }

    ExtOut("\nOther mechanisms enabled\n");
    data = dataPerHeap->bitMechanisms;
    for (int i = 0; i < DAC_MAX_GC_MECHANISM_BITS_COUNT; i++)
    {
        ExtOut("%20s: %d\n", str_bit_mechanisms[i], data[i]);
    }
}
#endif //GC_CONFIG_DRIVEN

DECLARE_API(DumpGCData)
{
    INIT_API();

#ifdef GC_CONFIG_DRIVEN
    MINIDUMP_NOT_SUPPORTED();    

    if (!InitializeHeapData ())
    {
        ExtOut("GC Heap not initialized yet.\n");
        return S_OK;
    }

    DacpGCInterestingInfoData interestingInfo;
    interestingInfo.RequestGlobal(g_sos);
    for (int i = 0; i < DAC_MAX_GLOBAL_GC_MECHANISMS_COUNT; i++)
    {
        ExtOut("%-30s: %d\n", str_gc_global_mechanisms[i], interestingInfo.globalMechanisms[i]);
    }

    ExtOut("\n[info per heap]\n");

    if (!IsServerBuild())
    {
        if (interestingInfo.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting interesting GC info\n");
            return E_FAIL;
        }
            
        PrintInterestingGCInfo(&interestingInfo);
    }
    else
    {   
        DWORD dwNHeaps = GetGcHeapCount();
        DWORD dwAllocSize;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtOut("Failed to get GCHeaps:  integer overflow\n");
            return Status;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtOut("Failed to get GCHeaps\n");
            return Status;
        }
        
        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            if (interestingInfo.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtOut("Heap %d: Error requesting interesting GC info\n", n);
                return E_FAIL;
            }

            ExtOut("--------info for heap %d--------\n", n);
            PrintInterestingGCInfo(&interestingInfo);
            ExtOut("\n");
        }
    }

    return S_OK;
#else
    ExtOut("Not implemented\n");
    return S_OK;
#endif //GC_CONFIG_DRIVEN
}

#ifndef FEATURE_PAL
/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to dump the build number and type of the  *  
*    mscoree.dll                                                       *
*                                                                      *
\**********************************************************************/
DECLARE_API (EEVersion)
{
    INIT_API();

    EEFLAVOR eef = GetEEFlavor();
    if (eef == UNKNOWNEE) {
        ExtOut("CLR not loaded\n");
        return Status;
    }
    
    if (g_ExtSymbols2) {
        VS_FIXEDFILEINFO version;
        
        BOOL ret = GetEEVersion(&version);
            
        if (ret) 
        {
            if (version.dwFileVersionMS != (DWORD)-1)
            {
                ExtOut("%u.%u.%u.%u",
                       HIWORD(version.dwFileVersionMS),
                       LOWORD(version.dwFileVersionMS),
                       HIWORD(version.dwFileVersionLS),
                       LOWORD(version.dwFileVersionLS));
                if (version.dwFileFlags & VS_FF_DEBUG) 
                {                    
                    ExtOut(" Checked or debug build");
                }
                else
                { 
                    BOOL fRet = IsRetailBuild ((size_t)moduleInfo[eef].baseAddr);

                    if (fRet)
                        ExtOut(" retail");
                    else
                        ExtOut(" free");
                }

                ExtOut("\n");
            }
        }
    }    
    
    if (!InitializeHeapData ())
        ExtOut("GC Heap not initialized, so GC mode is not determined yet.\n");
    else if (IsServerBuild()) 
        ExtOut("Server mode with %d gc heaps\n", GetGcHeapCount()); 
    else
        ExtOut("Workstation mode\n");

    if (!GetGcStructuresValid())
    {
        ExtOut("In plan phase of garbage collection\n");
    }

    // Print SOS version
    VS_FIXEDFILEINFO sosVersion;
    if (GetSOSVersion(&sosVersion))
    {
        if (sosVersion.dwFileVersionMS != (DWORD)-1)
        {
            ExtOut("SOS Version: %u.%u.%u.%u",
                   HIWORD(sosVersion.dwFileVersionMS),
                   LOWORD(sosVersion.dwFileVersionMS),
                   HIWORD(sosVersion.dwFileVersionLS),
                   LOWORD(sosVersion.dwFileVersionLS));
            if (sosVersion.dwFileFlags & VS_FF_DEBUG) 
            {                    
                ExtOut(" Checked or debug build");                    
            }
            else
            { 
                ExtOut(" retail build");                    
            }

            ExtOut("\n");
        }
    }
    return Status;
}
#endif // FEATURE_PAL

#ifndef FEATURE_PAL
/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to print the environment setting for      *  
*    the current process.                                              *
*                                                                      *
\**********************************************************************/
DECLARE_API (ProcInfo)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();        

    if (IsDumpFile())
    {
        ExtOut("!ProcInfo is not supported on a dump file.\n");
        return Status;
    }

#define INFO_ENV        0x00000001
#define INFO_TIME       0x00000002    
#define INFO_MEM        0x00000004
#define INFO_ALL        0xFFFFFFFF

    DWORD fProcInfo = INFO_ALL;

    if (_stricmp (args, "-env") == 0) {
        fProcInfo = INFO_ENV;
    }

    if (_stricmp (args, "-time") == 0) {
        fProcInfo = INFO_TIME;
    }

    if (_stricmp (args, "-mem") == 0) {
        fProcInfo = INFO_MEM;
    }

    if (fProcInfo & INFO_ENV) {
        ExtOut("---------------------------------------\n");
        ExtOut("Environment\n");
        ULONG64 pPeb;
        g_ExtSystem->GetCurrentProcessPeb(&pPeb);

        static ULONG Offset_ProcessParam = -1;
        static ULONG Offset_Environment = -1;
        if (Offset_ProcessParam == -1)
        {
            ULONG TypeId;
            ULONG64 NtDllBase;
            if (SUCCEEDED(g_ExtSymbols->GetModuleByModuleName ("ntdll",0,NULL,
                                                               &NtDllBase)))
            {
                if (SUCCEEDED(g_ExtSymbols->GetTypeId (NtDllBase, "PEB", &TypeId)))
                {
                    if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                         "ProcessParameters", &Offset_ProcessParam)))
                        Offset_ProcessParam = -1;
                }
                if (SUCCEEDED(g_ExtSymbols->GetTypeId (NtDllBase, "_RTL_USER_PROCESS_PARAMETERS", &TypeId)))
                {
                    if (FAILED (g_ExtSymbols->GetFieldOffset(NtDllBase, TypeId,
                                                         "Environment", &Offset_Environment)))
                        Offset_Environment = -1;
                }
            }
        }
        // We can not get it from PDB.  Use the fixed one.
        if (Offset_ProcessParam == -1)
            Offset_ProcessParam = offsetof (DT_PEB, ProcessParameters);

        if (Offset_Environment == -1)
            Offset_Environment = offsetof (DT_RTL_USER_PROCESS_PARAMETERS, Environment);


        ULONG64 addr = pPeb + Offset_ProcessParam;
        DWORD_PTR value;
        g_ExtData->ReadVirtual(UL64_TO_CDA(addr), &value, sizeof(PVOID), NULL);
        addr = value + Offset_Environment;
        g_ExtData->ReadVirtual(UL64_TO_CDA(addr), &value, sizeof(PVOID), NULL);

        static WCHAR buffer[DT_OS_PAGE_SIZE/2];        
        ULONG readBytes = DT_OS_PAGE_SIZE;
        ULONG64 Page;
        if ((g_ExtData->ReadDebuggerData( DEBUG_DATA_MmPageSize, &Page, sizeof(Page), NULL)) == S_OK
            && Page > 0)
        {
            ULONG uPageSize = (ULONG)(ULONG_PTR)Page;
            if (readBytes > uPageSize) {
                readBytes = uPageSize;
            }
        }        
        addr = value;
        while (1) {
            if (IsInterrupt())
                return Status;
            if (FAILED(g_ExtData->ReadVirtual(UL64_TO_CDA(addr), &buffer, readBytes, NULL)))
                break;
            addr += readBytes;
            WCHAR *pt = buffer;
            WCHAR *end = pt;
            while (pt < &buffer[DT_OS_PAGE_SIZE/2]) {
                end = _wcschr (pt, L'\0');
                if (end == NULL) {
                    char format[20];
                    sprintf_s (format,_countof (format), "%dS", &buffer[DT_OS_PAGE_SIZE/2] - pt);
                    ExtOut(format, pt);
                    break;
                }
                else if (end == pt) {
                    break;
                }
                ExtOut("%S\n", pt);
                pt = end + 1;
            }
            if (end == pt) {
                break;
            }
        }
    }
    
    HANDLE hProcess = INVALID_HANDLE_VALUE;
    if (fProcInfo & (INFO_TIME | INFO_MEM)) {
        ULONG64 handle;
        g_ExtSystem->GetCurrentProcessHandle(&handle);
        hProcess = (HANDLE)handle;
    }
    
    if (!IsDumpFile() && fProcInfo & INFO_TIME) {
        FILETIME CreationTime;
        FILETIME ExitTime;
        FILETIME KernelTime;
        FILETIME UserTime;

        typedef BOOL (WINAPI *FntGetProcessTimes)(HANDLE, LPFILETIME, LPFILETIME, LPFILETIME, LPFILETIME);
        static FntGetProcessTimes pFntGetProcessTimes = (FntGetProcessTimes)-1;
        if (pFntGetProcessTimes == (FntGetProcessTimes)-1) {
            HINSTANCE hstat = LoadLibrary ("Kernel32.dll");
            if (hstat != 0)
            {
                pFntGetProcessTimes = (FntGetProcessTimes)GetProcAddress (hstat, "GetProcessTimes");
                FreeLibrary (hstat);
            }
            else
                pFntGetProcessTimes = NULL;
        }

        if (pFntGetProcessTimes && pFntGetProcessTimes (hProcess,&CreationTime,&ExitTime,&KernelTime,&UserTime)) {
            ExtOut("---------------------------------------\n");
            ExtOut("Process Times\n");
            static const char *Month[] = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep",
                        "Oct", "Nov", "Dec"};
            SYSTEMTIME SystemTime;
            FILETIME LocalFileTime;
            if (FileTimeToLocalFileTime (&CreationTime,&LocalFileTime)
                && FileTimeToSystemTime (&LocalFileTime,&SystemTime)) {
                ExtOut("Process Started at: %4d %s %2d %d:%d:%d.%02d\n",
                        SystemTime.wYear, Month[SystemTime.wMonth-1], SystemTime.wDay,
                        SystemTime.wHour, SystemTime.wMinute,
                        SystemTime.wSecond, SystemTime.wMilliseconds/10);
            }
        
            DWORD nDay = 0;
            DWORD nHour = 0;
            DWORD nMin = 0;
            DWORD nSec = 0;
            DWORD nHundred = 0;
            
            ULONG64 totalTime;
             
            totalTime = KernelTime.dwLowDateTime + (((ULONG64)KernelTime.dwHighDateTime) << 32);
            nDay = (DWORD)(totalTime/(24*3600*10000000ui64));
            totalTime %= 24*3600*10000000ui64;
            nHour = (DWORD)(totalTime/(3600*10000000ui64));
            totalTime %= 3600*10000000ui64;
            nMin = (DWORD)(totalTime/(60*10000000));
            totalTime %= 60*10000000;
            nSec = (DWORD)(totalTime/10000000);
            totalTime %= 10000000;
            nHundred = (DWORD)(totalTime/100000);
            ExtOut("Kernel CPU time   : %d days %02d:%02d:%02d.%02d\n",
                    nDay, nHour, nMin, nSec, nHundred);
            
            DWORD sDay = nDay;
            DWORD sHour = nHour;
            DWORD sMin = nMin;
            DWORD sSec = nSec;
            DWORD sHundred = nHundred;
            
            totalTime = UserTime.dwLowDateTime + (((ULONG64)UserTime.dwHighDateTime) << 32);
            nDay = (DWORD)(totalTime/(24*3600*10000000ui64));
            totalTime %= 24*3600*10000000ui64;
            nHour = (DWORD)(totalTime/(3600*10000000ui64));
            totalTime %= 3600*10000000ui64;
            nMin = (DWORD)(totalTime/(60*10000000));
            totalTime %= 60*10000000;
            nSec = (DWORD)(totalTime/10000000);
            totalTime %= 10000000;
            nHundred = (DWORD)(totalTime/100000);
            ExtOut("User   CPU time   : %d days %02d:%02d:%02d.%02d\n",
                    nDay, nHour, nMin, nSec, nHundred);
        
            sDay += nDay;
            sHour += nHour;
            sMin += nMin;
            sSec += nSec;
            sHundred += nHundred;
            if (sHundred >= 100) {
                sSec += sHundred/100;
                sHundred %= 100;
            }
            if (sSec >= 60) {
                sMin += sSec/60;
                sSec %= 60;
            }
            if (sMin >= 60) {
                sHour += sMin/60;
                sMin %= 60;
            }
            if (sHour >= 24) {
                sDay += sHour/24;
                sHour %= 24;
            }
            ExtOut("Total  CPU time   : %d days %02d:%02d:%02d.%02d\n",
                    sDay, sHour, sMin, sSec, sHundred);
        }
    }

    if (!IsDumpFile() && fProcInfo & INFO_MEM) {
        typedef
        NTSTATUS
        (NTAPI
         *FntNtQueryInformationProcess)(HANDLE, PROCESSINFOCLASS, PVOID, ULONG, PULONG);

        static FntNtQueryInformationProcess pFntNtQueryInformationProcess = (FntNtQueryInformationProcess)-1;
        if (pFntNtQueryInformationProcess == (FntNtQueryInformationProcess)-1) {
            HINSTANCE hstat = LoadLibrary ("ntdll.dll");
            if (hstat != 0)
            {
                pFntNtQueryInformationProcess = (FntNtQueryInformationProcess)GetProcAddress (hstat, "NtQueryInformationProcess");
                FreeLibrary (hstat);
            }
            else
                pFntNtQueryInformationProcess = NULL;
        }
        VM_COUNTERS memory;
        if (pFntNtQueryInformationProcess &&
            NT_SUCCESS (pFntNtQueryInformationProcess (hProcess,ProcessVmCounters,&memory,sizeof(memory),NULL))) {
            ExtOut("---------------------------------------\n");
            ExtOut("Process Memory\n");
            ExtOut("WorkingSetSize: %8d KB       PeakWorkingSetSize: %8d KB\n",
                    memory.WorkingSetSize/1024, memory.PeakWorkingSetSize/1024);
            ExtOut("VirtualSize:    %8d KB       PeakVirtualSize:    %8d KB\n", 
                    memory.VirtualSize/1024, memory.PeakVirtualSize/1024);
            ExtOut("PagefileUsage:  %8d KB       PeakPagefileUsage:  %8d KB\n", 
                    memory.PagefileUsage/1024, memory.PeakPagefileUsage/1024);
        }

        MEMORYSTATUS memstat;
        GlobalMemoryStatus (&memstat);
        ExtOut("---------------------------------------\n");
        ExtOut("%ld percent of memory is in use.\n\n",
                memstat.dwMemoryLoad);
        ExtOut("Memory Availability (Numbers in MB)\n\n");
        ExtOut("                  %8s     %8s\n", "Total", "Avail");
        ExtOut("Physical Memory   %8d     %8d\n", memstat.dwTotalPhys/1024/1024, memstat.dwAvailPhys/1024/1024);
        ExtOut("Page File         %8d     %8d\n", memstat.dwTotalPageFile/1024/1024, memstat.dwAvailPageFile/1024/1024);
        ExtOut("Virtual Memory    %8d     %8d\n", memstat.dwTotalVirtual/1024/1024, memstat.dwAvailVirtual/1024/1024);
    }

    return Status;
}
#endif // FEATURE_PAL

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to find the address of EE data for a      *  
*    metadata token.                                                   *
*                                                                      *
\**********************************************************************/
DECLARE_API(Token2EE)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    StringHolder DllName;
    ULONG64 token = 0;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };

    CMDValue arg[] = 
    {   // vptr, type
        {&DllName.data, COSTRING},
        {&token, COHEX}
    };

    size_t nArg;
    if (!GetCMDOption(args,option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (nArg!=2)
    {
        ExtOut("Usage: !Token2EE module_name mdToken\n");
        ExtOut("       You can pass * for module_name to search all modules.\n");
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    int numModule;
    ArrayHolder<DWORD_PTR> moduleList = NULL;

    if (strcmp(DllName.data, "*") == 0)
    {
        moduleList = ModuleFromName(NULL, &numModule);
    }
    else
    {
        moduleList = ModuleFromName(DllName.data, &numModule);
    }
    
    if (moduleList == NULL)
    {
        ExtOut("Failed to request module list.\n");
    }
    else
    {
        for (int i = 0; i < numModule; i ++)
        {
            if (IsInterrupt())
                break;

            if (i > 0)
            {
                ExtOut("--------------------------------------\n");
            }        

            DWORD_PTR dwAddr = moduleList[i];
            WCHAR FileName[MAX_LONGPATH];
            FileNameForModule(dwAddr, FileName);

            // We'd like a short form for this output
            LPWSTR pszFilename = _wcsrchr (FileName, DIRECTORY_SEPARATOR_CHAR_W);
            if (pszFilename == NULL)
            {
                pszFilename = FileName;
            }
            else
            {
                pszFilename++; // skip past the last "\" character
            }
            
            DMLOut("Module:      %s\n", DMLModule(dwAddr));
            ExtOut("Assembly:    %S\n", pszFilename);
            
            GetInfoFromModule(dwAddr, (ULONG)token);
        }
    }
    
    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function is called to find the address of EE data for a      *  
*    metadata token.                                                   *
*                                                                      *
\**********************************************************************/
DECLARE_API(Name2EE)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    StringHolder DllName, TypeName; 
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    
    CMDValue arg[] = 
    {   // vptr, type
        {&DllName.data, COSTRING},
        {&TypeName.data, COSTRING}
    };
    size_t nArg;
    
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);

    if (nArg == 1)
    {
        // The input may be in the form <modulename>!<type>
        // If so, do some surgery on the input params.

        // There should be only 1 ! character
        LPSTR pszSeperator = strchr (DllName.data, '!');
        if (pszSeperator != NULL)
        {
            if (strchr (pszSeperator + 1, '!') == NULL)
            {
                size_t capacity_TypeName_data = strlen(pszSeperator + 1) + 1;
                TypeName.data = new NOTHROW char[capacity_TypeName_data];
                if (TypeName.data)
                {
                    // get the type name,
                    strcpy_s (TypeName.data, capacity_TypeName_data, pszSeperator + 1);
                    // and truncate DllName
                    *pszSeperator = '\0';

                    // Do some extra validation
                    if (strlen (DllName.data) >= 1 &&
                        strlen (TypeName.data) > 1)
                    {
                        nArg = 2;
                    }
                }
            }
        }
    }
    
    if (nArg != 2)
    {
        ExtOut("Usage: " SOSPrefix "name2ee module_name item_name\n");
        ExtOut("  or   " SOSPrefix "name2ee module_name!item_name\n");        
        ExtOut("       use * for module_name to search all loaded modules\n");
        ExtOut("Examples: " SOSPrefix "name2ee  mscorlib.dll System.String.ToString\n");
        ExtOut("          " SOSPrefix "name2ee *!System.String\n");
        return Status;
    }
    
    int numModule;
    ArrayHolder<DWORD_PTR> moduleList = NULL;
    if (strcmp(DllName.data, "*") == 0)
    {
        moduleList = ModuleFromName(NULL, &numModule);
    }
    else
    {
        moduleList = ModuleFromName(DllName.data, &numModule);
    }
            

    if (moduleList == NULL)
    {
        ExtOut("Failed to request module list.\n", DllName.data);
    }
    else
    {
        for (int i = 0; i < numModule; i ++)
        {
            if (IsInterrupt())
                break;

            if (i > 0)
            {
                ExtOut("--------------------------------------\n");
            }
            
            DWORD_PTR dwAddr = moduleList[i];
            WCHAR FileName[MAX_LONGPATH];
            FileNameForModule (dwAddr, FileName);

            // We'd like a short form for this output
            LPWSTR pszFilename = _wcsrchr (FileName, DIRECTORY_SEPARATOR_CHAR_W);
            if (pszFilename == NULL)
            {
                pszFilename = FileName;
            }
            else
            {
                pszFilename++; // skip past the last "\" character
            }
            
            DMLOut("Module:      %s\n", DMLModule(dwAddr));
            ExtOut("Assembly:    %S\n", pszFilename);
            GetInfoFromName(dwAddr, TypeName.data);
        }
    }
 
    return Status;
}


#ifndef FEATURE_PAL
DECLARE_API(PathTo)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    DWORD_PTR root = NULL;
    DWORD_PTR target = NULL;
    BOOL dml = FALSE;
    size_t nArg;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&root, COHEX},
        {&target, COHEX},
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }    
    
    if (root == 0 || target == 0)
    {
        ExtOut("Invalid argument %s\n", args);
        return Status;
    }
    
    GCRootImpl gcroot;
    bool result = gcroot.PrintPathToObject(root, target);
    
    if (!result)
        ExtOut("Did not find a path from %p to %p.\n", SOS_PTR(root), SOS_PTR(target));
    
    return Status;
}
#endif



/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function finds all roots (on stack or in handles) for a      *  
*    given object.                                                     *
*                                                                      *
\**********************************************************************/
DECLARE_API(GCRoot)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    BOOL bNoStacks = FALSE;
    DWORD_PTR obj = NULL;
    BOOL dml = FALSE;
    BOOL all = FALSE;
    size_t nArg;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-nostacks", &bNoStacks, COBOOL, FALSE},
        {"-all", &all, COBOOL, FALSE},
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 

    {   // vptr, type
        {&obj, COHEX}
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }    
    if (obj == 0)
    {
        ExtOut("Invalid argument %s\n", args);
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);      
    GCRootImpl gcroot;
    int i = gcroot.PrintRootsForObject(obj, all == TRUE, bNoStacks == TRUE);
    
    if (IsInterrupt())
        ExtOut("Interrupted, data may be incomplete.\n");
    
    if (all)
        ExtOut("Found %d roots.\n", i);
    else
        ExtOut("Found %d unique roots (run '" SOSPrefix "gcroot -all' to see all roots).\n", i);

    return Status;
}

DECLARE_API(GCWhere)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    BOOL dml = FALSE;
    BOOL bGetBrick;
    BOOL bGetCard;
    TADDR taddrObj = 0;
    size_t nArg;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-brick", &bGetBrick, COBOOL, FALSE},
        {"-card", &bGetCard, COBOOL, FALSE},
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&taddrObj, COHEX}
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    // Obtain allocation context for each managed thread.    
    AllocInfo allocInfo;
    allocInfo.Init();

    TADDR_SEGINFO trngSeg  = { 0, 0, 0 };
    TADDR_RANGE   allocCtx = { 0, 0 };
    int   gen = -1;
    BOOL  bLarge = FALSE;
    BOOL  bFound = FALSE;

    size_t size = 0;
    if (sos::IsObject(taddrObj))
    {
        TADDR taddrMT; 
        BOOL  bContainsPointers;
        if(FAILED(GetMTOfObject(taddrObj, &taddrMT)) ||
           !GetSizeEfficient(taddrObj, taddrMT, FALSE, size, bContainsPointers))
        {
            ExtWarn("Couldn't get size for object %#p: possible heap corruption.\n",
                SOS_PTR(taddrObj));
        }
    }

    if (!IsServerBuild())
    {
        DacpGcHeapDetails heapDetails;
        if (heapDetails.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting gc heap details\n");
            return Status;
        }

        if (GCObjInHeap(taddrObj, heapDetails, trngSeg, gen, allocCtx, bLarge))
        {
            ExtOut("Address   " WIN64_8SPACES " Gen   Heap   segment   " WIN64_8SPACES " begin     " WIN64_8SPACES " allocated  " WIN64_8SPACES " size\n");
            ExtOut("%p   %d     %2d     %p   %p   %p    0x%x(%d)\n",
                SOS_PTR(taddrObj), gen, 0, SOS_PTR(trngSeg.segAddr), SOS_PTR(trngSeg.start), SOS_PTR(trngSeg.end), size, size);
            bFound = TRUE;
        }
    }
    else
    {
        DacpGcHeapData gcheap;
        if (gcheap.Request(g_sos) != S_OK)
        {
            ExtOut("Error requesting GC Heap data\n");
            return Status;
        }

        DWORD dwAllocSize;
        DWORD dwNHeaps = gcheap.HeapCount;
        if (!ClrSafeInt<DWORD>::multiply(sizeof(CLRDATA_ADDRESS), dwNHeaps, dwAllocSize))
        {
            ExtOut("Failed to get GCHeaps:  integer overflow\n");
            return Status;
        }

        CLRDATA_ADDRESS *heapAddrs = (CLRDATA_ADDRESS*)alloca(dwAllocSize);
        if (g_sos->GetGCHeapList(dwNHeaps, heapAddrs, NULL) != S_OK)
        {
            ExtOut("Failed to get GCHeaps\n");
            return Status;
        }
 
        for (DWORD n = 0; n < dwNHeaps; n ++)
        {
            DacpGcHeapDetails heapDetails;
            if (heapDetails.Request(g_sos, heapAddrs[n]) != S_OK)
            {
                ExtOut("Error requesting details\n");
                return Status;
            }

            if (GCObjInHeap(taddrObj, heapDetails, trngSeg, gen, allocCtx, bLarge))
            {
                ExtOut("Address " WIN64_8SPACES " Gen Heap segment " WIN64_8SPACES " begin   " WIN64_8SPACES " allocated" WIN64_8SPACES " size\n");
                ExtOut("%p   %d     %2d     %p   %p   %p    0x%x(%d)\n",
                    SOS_PTR(taddrObj), gen, n, SOS_PTR(trngSeg.segAddr), SOS_PTR(trngSeg.start), SOS_PTR(trngSeg.end), size, size);
                bFound = TRUE;
                break;
            }
        }
    }

    if (!bFound)
    {
        ExtOut("Address %#p not found in the managed heap.\n", SOS_PTR(taddrObj));
    }

    return Status;
}

#ifndef FEATURE_PAL

DECLARE_API(FindRoots)
{
#ifndef FEATURE_PAL
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();

    if (IsDumpFile())
    {
        ExtOut("!FindRoots is not supported on a dump file.\n");
        return Status;
    }
    
    LONG_PTR gen = -100; // initialized outside the legal range: [-1, 2]
    StringHolder sgen;
    TADDR taObj = NULL;
    BOOL dml = FALSE;
    size_t nArg;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-gen", &sgen.data, COSTRING, TRUE},
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&taObj, COHEX}
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (sgen.data != NULL)
    {
        if (_stricmp(sgen.data, "any") == 0)
        {
            gen = -1;
        }
        else
        {
            gen = GetExpression(sgen.data);
        }
    }
    if ((gen < -1 || gen > 2) && (taObj == 0))
    {
        ExtOut("Incorrect options.  Usage:\n\t!FindRoots -gen <N>\n\t\twhere N is 0, 1, 2, or \"any\". OR\n\t!FindRoots <obj>\n");
        return Status;
    }

    if (gen >= -1 && gen <= 2)
    {
        IXCLRDataProcess2* idp2 = NULL;
        if (FAILED(g_clrData->QueryInterface(IID_IXCLRDataProcess2, (void**) &idp2)))
        {
            ExtOut("Your version of the runtime/DAC do not support this command.\n");
            return Status;
        }

        // Request GC_MARK_END notifications from debuggee
        GcEvtArgs gea = { GC_MARK_END, { ((gen == -1) ? 7 : (1 << gen)) } };
        idp2->SetGcNotification(gea);
        // ... and register the notification handler
        g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, "sxe -c \"!HandleCLRN\" clrn", 0);
        // the above notification is removed in CNotification::OnGcEvent()
    }
    else
    {
        // verify that the last event in the debugger was indeed a CLRN exception
        DEBUG_LAST_EVENT_INFO_EXCEPTION dle;
        CNotification Notification;

        if (!CheckCLRNotificationEvent(&dle))
        {
            ExtOut("The command !FindRoots can only be used after the debugger stopped on a CLRN GC notification.\n");
            ExtOut("At this time !GCRoot should be used instead.\n");
            return Status;
        }
        // validate argument
        if (!g_snapshot.Build())
        {
            ExtOut("Unable to build snapshot of the garbage collector state\n");
            return Status;
        }

        if (g_snapshot.GetHeap(taObj) == NULL)
        {
            ExtOut("Address %#p is not in the managed heap.\n", SOS_PTR(taObj));
            return Status;
        }

        int ogen = g_snapshot.GetGeneration(taObj);
        if (ogen > CNotification::GetCondemnedGen())
        {
            DMLOut("Object %s will survive this collection:\n\tgen(%#p) = %d > %d = condemned generation.\n",
                DMLObject(taObj), SOS_PTR(taObj), ogen, CNotification::GetCondemnedGen());
            return Status;
        }

        GCRootImpl gcroot;
        int roots = gcroot.FindRoots(CNotification::GetCondemnedGen(), taObj);
        
        ExtOut("Found %d roots.\n", roots);
    }

    return Status;
#else
    return E_NOTIMPL;
#endif
}

class GCHandleStatsForDomains
{
public:
    GCHandleStatsForDomains() 
        : m_singleDomainMode(FALSE), m_numDomains(0), m_pStatistics(NULL), m_pDomainPointers(NULL), m_sharedDomainIndex(-1), m_systemDomainIndex(-1)
    {
    }

    ~GCHandleStatsForDomains()
    {
        if (m_pStatistics)
        {
            if (m_singleDomainMode)
                delete m_pStatistics;
            else
                delete [] m_pStatistics;
        }
        
        if (m_pDomainPointers)
            delete [] m_pDomainPointers;
    }
    
    BOOL Init(BOOL singleDomainMode)
    {
        m_singleDomainMode = singleDomainMode; 
        if (m_singleDomainMode)
        {
            m_numDomains = 1;
            m_pStatistics = new NOTHROW GCHandleStatistics();
            if (m_pStatistics == NULL)
                return FALSE;
        }
        else
        {
            DacpAppDomainStoreData adsData;
            if (adsData.Request(g_sos) != S_OK)
                return FALSE;

            LONG numSpecialDomains = (adsData.sharedDomain != NULL) ? 2 : 1;
            m_numDomains = adsData.DomainCount + numSpecialDomains;
            ArrayHolder<CLRDATA_ADDRESS> pArray = new NOTHROW CLRDATA_ADDRESS[m_numDomains];
            if (pArray == NULL)
                return FALSE;

            int i = 0;
            if (adsData.sharedDomain != NULL)
            {
                pArray[i++] = adsData.sharedDomain;
            }

            pArray[i] = adsData.systemDomain;

            m_sharedDomainIndex = i - 1; // The m_sharedDomainIndex is set to -1 if there is no shared domain
            m_systemDomainIndex = i;
            
            if (g_sos->GetAppDomainList(adsData.DomainCount, pArray+numSpecialDomains, NULL) != S_OK)
                return FALSE;
            
            m_pDomainPointers = pArray.Detach();
            m_pStatistics = new NOTHROW GCHandleStatistics[m_numDomains];
            if (m_pStatistics == NULL)
                return FALSE;
        }
        
        return TRUE;
    }

    GCHandleStatistics *LookupStatistics(CLRDATA_ADDRESS appDomainPtr) const
    {
        if (m_singleDomainMode)
        {
            // You can pass NULL appDomainPtr if you are in singleDomainMode
            return m_pStatistics;
        }
        else
        {
            for (int i=0; i < m_numDomains; i++)
                if (m_pDomainPointers[i] == appDomainPtr)
                    return m_pStatistics + i;
        }
        
        return NULL;
    }
    
    
    GCHandleStatistics *GetStatistics(int appDomainIndex) const
    {
        SOS_Assert(appDomainIndex >= 0);
        SOS_Assert(appDomainIndex < m_numDomains);
        
        return m_singleDomainMode ? m_pStatistics : m_pStatistics + appDomainIndex;
    }
    
    int GetNumDomains() const
    {
        return m_numDomains;
    }
    
    CLRDATA_ADDRESS GetDomain(int index) const
    {
        SOS_Assert(index >= 0);
        SOS_Assert(index < m_numDomains);
        return m_pDomainPointers[index];
    }

    int GetSharedDomainIndex()
    {
        return m_sharedDomainIndex;
    }

    int GetSystemDomainIndex()
    {
        return m_systemDomainIndex;
    }

private:
    BOOL m_singleDomainMode;
    int m_numDomains;
    GCHandleStatistics *m_pStatistics;
    CLRDATA_ADDRESS *m_pDomainPointers;
    int m_sharedDomainIndex;
    int m_systemDomainIndex;
};

class GCHandlesImpl
{
public:
    GCHandlesImpl(PCSTR args)
        : mPerDomain(FALSE), mStat(FALSE), mDML(FALSE), mType((int)~0)
    {
        ArrayHolder<char> type = NULL;
        CMDOption option[] = 
        {
            {"-perdomain", &mPerDomain, COBOOL, FALSE},
            {"-stat", &mStat, COBOOL, FALSE},
            {"-type", &type, COSTRING, TRUE},
            {"/d", &mDML, COBOOL, FALSE},
        };
        
        if (!GetCMDOption(args,option,_countof(option),NULL,0,NULL))
            sos::Throw<sos::Exception>("Failed to parse command line arguments.");
        
        if (type != NULL)
            if (_stricmp(type, "Pinned") == 0)
                mType = HNDTYPE_PINNED;
            else if (_stricmp(type, "RefCounted") == 0)
                mType = HNDTYPE_REFCOUNTED;
            else if (_stricmp(type, "WeakShort") == 0)
                mType = HNDTYPE_WEAK_SHORT;
            else if (_stricmp(type, "WeakLong") == 0)
                mType = HNDTYPE_WEAK_LONG;
            else if (_stricmp(type, "Strong") == 0)
                mType = HNDTYPE_STRONG;
            else if (_stricmp(type, "Variable") == 0)
                mType = HNDTYPE_VARIABLE;
            else if (_stricmp(type, "AsyncPinned") == 0)
                mType = HNDTYPE_ASYNCPINNED;
            else if (_stricmp(type, "SizedRef") == 0)
                mType = HNDTYPE_SIZEDREF;
            else if (_stricmp(type, "Dependent") == 0)
                mType = HNDTYPE_DEPENDENT;
            else if (_stricmp(type, "WeakWinRT") == 0)
                mType = HNDTYPE_WEAK_WINRT;
            else
                sos::Throw<sos::Exception>("Unknown handle type '%s'.", type.GetPtr());
    }
    
    void Run()
    {
        EnableDMLHolder dmlHolder(mDML);
        
        mOut.ReInit(6, POINTERSIZE_HEX, AlignRight);
        mOut.SetWidths(5, POINTERSIZE_HEX, 11, POINTERSIZE_HEX, 8, POINTERSIZE_HEX);
        mOut.SetColAlignment(1, AlignLeft);
        
        if (mHandleStat.Init(!mPerDomain) == FALSE)
            sos::Throw<sos::Exception>("Error getting per-appdomain handle information");
        
        if (!mStat)
            mOut.WriteRow("Handle", "Type", "Object", "Size", "Data", "Type");
            
        WalkHandles();
        
        for (int i=0; (i < mHandleStat.GetNumDomains()) && !IsInterrupt(); i++)
        {
            GCHandleStatistics *pStats = mHandleStat.GetStatistics(i);

            if (mPerDomain)
            {
                Print( "------------------------------------------------------------------------------\n");           
                Print("GC Handle Statistics for AppDomain ", AppDomainPtr(mHandleStat.GetDomain(i)));
            
                if (i == mHandleStat.GetSharedDomainIndex())
                    Print(" (Shared Domain)\n");
                else if (i == mHandleStat.GetSystemDomainIndex())
                    Print(" (System Domain)\n");
                else
                    Print("\n");
            }

            if (!mStat)
                Print("\n");
            PrintGCStat(&pStats->hs);
            
            // Don't print handle stats if the user has filtered by type.  All handles will be the same
            // type, and the total count will be displayed by PrintGCStat.
            if (mType == (unsigned int)~0)
            {
                Print("\n");
                PrintGCHandleStats(pStats);
            }
        }
    }

private:
    void WalkHandles()
    {
        ToRelease<ISOSHandleEnum> handles;
        if (FAILED(g_sos->GetHandleEnum(&handles)))
        {
            if (IsMiniDumpFile())
                sos::Throw<sos::Exception>("Unable to display GC handles.\nA minidump without full memory may not have this information.");
            else
                sos::Throw<sos::Exception>("Failed to walk the handle table.");
        }
      
        // GCC can't handle stacks which are too large.
#ifndef FEATURE_PAL
        SOSHandleData data[256];
#else
        SOSHandleData data[4];
#endif
        
        unsigned int fetched = 0;
        HRESULT hr = S_OK;
        do
        {
            if (FAILED(hr = handles->Next(_countof(data), data, &fetched)))
            {
                ExtOut("Error %x while walking the handle table.\n", hr);
                break;
            }
            
            WalkHandles(data, fetched);
        } while (_countof(data) == fetched);
    }
    
    void WalkHandles(SOSHandleData data[], unsigned int count)
    {
        for (unsigned int i = 0; i < count; ++i)
        {
            sos::CheckInterrupt();
        
            if (mType != (unsigned int)~0 && mType != data[i].Type)
                continue;
        
            GCHandleStatistics *pStats = mHandleStat.LookupStatistics(data[i].AppDomain);
            TADDR objAddr = 0;
            TADDR mtAddr = 0;
            size_t size = 0;
            const WCHAR *mtName = 0;
            const char *type = 0;
            
            if (FAILED(MOVE(objAddr, data[i].Handle)))
            {
                objAddr = 0;
                mtName = W("<error>");
            }
            else
            {
                sos::Object obj(TO_TADDR(objAddr));
                mtAddr = obj.GetMT();
                if (sos::MethodTable::IsFreeMT(mtAddr))
                {
                    mtName = W("<free>");
                }
                else if (!sos::MethodTable::IsValid(mtAddr))
                {
                    mtName = W("<error>");
                }
                else
                {
                    size = obj.GetSize();
                    if (mType == (unsigned int)~0 || mType == data[i].Type)
                        pStats->hs.Add(obj.GetMT(), (DWORD)size);
                }
            }
        
            switch(data[i].Type)
            {
                case HNDTYPE_PINNED:
                    type = "Pinned";
                    if (pStats) pStats->pinnedHandleCount++;
                    break;
                case HNDTYPE_REFCOUNTED:
                    type = "RefCounted";
                    if (pStats) pStats->refCntHandleCount++;
                    break;    
                case HNDTYPE_STRONG:
                    type = "Strong";
                    if (pStats) pStats->strongHandleCount++;
                    break;
                case HNDTYPE_WEAK_SHORT:
                    type = "WeakShort";
                    if (pStats) pStats->weakShortHandleCount++;
                    break;
                case HNDTYPE_WEAK_LONG:
                    type = "WeakLong";
                    if (pStats) pStats->weakLongHandleCount++;
                    break;
                case HNDTYPE_ASYNCPINNED:
                    type = "AsyncPinned";
                    if (pStats) pStats->asyncPinnedHandleCount++;
                    break;
                case HNDTYPE_VARIABLE:
                    type = "Variable";
                    if (pStats) pStats->variableCount++;
                    break;
                case HNDTYPE_SIZEDREF:
                    type = "SizedRef";
                    if (pStats) pStats->sizedRefCount++;
                    break;
                case HNDTYPE_DEPENDENT:
                    type = "Dependent";
                    if (pStats) pStats->dependentCount++;
                    break;
                case HNDTYPE_WEAK_WINRT:
                    type = "WeakWinRT";
                    if (pStats) pStats->weakWinRTHandleCount++;
                    break;
                default:
                    DebugBreak();
                    type = "Unknown";
                    pStats->unknownHandleCount++;
                    break;
            }
            
            if (type && !mStat)
            {
                sos::MethodTable mt = mtAddr;
                if (mtName == 0)
                    mtName = mt.GetName();
                
                if (data[i].Type == HNDTYPE_REFCOUNTED)
                    mOut.WriteRow(data[i].Handle, type, ObjectPtr(objAddr), Decimal(size), Decimal(data[i].RefCount), mtName);
                else if (data[i].Type == HNDTYPE_DEPENDENT)
                    mOut.WriteRow(data[i].Handle, type, ObjectPtr(objAddr), Decimal(size), ObjectPtr(data[i].Secondary), mtName);
                else if (data[i].Type == HNDTYPE_WEAK_WINRT)
                    mOut.WriteRow(data[i].Handle, type, ObjectPtr(objAddr), Decimal(size), Pointer(data[i].Secondary), mtName);
                else
                    mOut.WriteRow(data[i].Handle, type, ObjectPtr(objAddr), Decimal(size), "", mtName);
            }
        }
    }
    
    inline void PrintHandleRow(const char *text, int count)
    {
        if (count)
            mOut.WriteRow(text, Decimal(count));
    }
    
    void PrintGCHandleStats(GCHandleStatistics *pStats)
    {
        Print("Handles:\n");
        mOut.ReInit(2, 21, AlignLeft, 4);
        
        PrintHandleRow("Strong Handles:", pStats->strongHandleCount);
        PrintHandleRow("Pinned Handles:", pStats->pinnedHandleCount);
        PrintHandleRow("Async Pinned Handles:", pStats->asyncPinnedHandleCount);
        PrintHandleRow("Ref Count Handles:", pStats->refCntHandleCount);
        PrintHandleRow("Weak Long Handles:", pStats->weakLongHandleCount);
        PrintHandleRow("Weak Short Handles:", pStats->weakShortHandleCount);
        PrintHandleRow("Weak WinRT Handles:", pStats->weakWinRTHandleCount);
        PrintHandleRow("Variable Handles:", pStats->variableCount);
        PrintHandleRow("SizedRef Handles:", pStats->sizedRefCount);
        PrintHandleRow("Dependent Handles:", pStats->dependentCount);
        PrintHandleRow("Other Handles:", pStats->unknownHandleCount);
    }
    
private:
    BOOL mPerDomain, mStat, mDML;
    unsigned int mType;
    TableOutput mOut;
    GCHandleStatsForDomains mHandleStat;
};

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function dumps GC Handle statistics        *
*                                                                      *
\**********************************************************************/
DECLARE_API(GCHandles)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    try
    {
        GCHandlesImpl gchandles(args);
        gchandles.Run();
    }
    catch(const sos::Exception &e)
    {
        Print(e.what());
    }

    return Status;
}

// This is an experimental and undocumented SOS API that attempts to step through code
// stopping once jitted code is reached. It currently has some issues - it can take arbitrarily long
// to reach jitted code and canceling it is terrible. At best it doesn't cancel, at worst it
// kills the debugger. IsInterrupt() doesn't work nearly as nicely as one would hope :/
#ifndef FEATURE_PAL
DECLARE_API(TraceToCode)
{
    INIT_API_NOEE();

    static ULONG64 g_clrBaseAddr = 0;


    while(true)
    {
        if (IsInterrupt())
        {
            ExtOut("Interrupted\n");
            return S_FALSE;
        }

        ULONG64 Offset;
        g_ExtRegisters->GetInstructionOffset(&Offset);

        DWORD codeType = 0;
        ULONG64 base = 0;
        CLRDATA_ADDRESS cdaStart = TO_CDADDR(Offset);
        DacpMethodDescData MethodDescData;
        if(g_ExtSymbols->GetModuleByOffset(Offset, 0, NULL, &base) == S_OK)
        {
            if(g_clrBaseAddr == 0)
            {
                g_ExtSymbols->GetModuleByModuleName (MAIN_CLR_MODULE_NAME_A,0,NULL,
                    &g_clrBaseAddr);
            }
            if(g_clrBaseAddr == base)
            {
                ExtOut("Compiled code in CLR\n");
                codeType = 4;
            }
            else
            {
                ExtOut("Compiled code in module @ 0x%I64x\n", base);
                codeType = 8;
            }
        }
        else if (g_sos != NULL || LoadClrDebugDll()==S_OK)
        {
            CLRDATA_ADDRESS addr;
            if(g_sos->GetMethodDescPtrFromIP(cdaStart, &addr) == S_OK)
            {
                WCHAR wszNameBuffer[1024]; // should be large enough

                // get the MethodDesc name
                if ((g_sos->GetMethodDescName(addr, 1024, wszNameBuffer, NULL) == S_OK) &&
                    _wcsncmp(W("DomainBoundILStubClass"), wszNameBuffer, 22)==0)
                {
                    ExtOut("ILStub\n");
                    codeType = 2;
                }
                else
                {
                    ExtOut("Jitted code\n");
                    codeType = 1;
                }
            }
            else
            {
                ExtOut("Not compiled or jitted, assuming stub\n");
                codeType = 16;
            }
        }
        else
        {
            // not compiled but CLR isn't loaded... some other code generator?
            return E_FAIL;
        }

        if(codeType == 1)
        {
            return S_OK;
        }
        else
        {
            Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, "thr; .echo wait" ,0);
            if (FAILED(Status))
            {
                ExtOut("Error tracing instruction\n");
                return Status;
            }
        }
    }

    return Status;

}
#endif // FEATURE_PAL

// This is an experimental and undocumented API that sets a debugger pseudo-register based
// on the type of code at the given IP. It can be used in scripts to keep stepping until certain
// kinds of code have been reached. Presumbably its slower than !TraceToCode but at least it
// cancels much better
#ifndef FEATURE_PAL
DECLARE_API(GetCodeTypeFlags)
{
    INIT_API();   
    

    char buffer[100+mdNameLen];
    size_t ip;
    StringHolder PReg;
    
    CMDValue arg[] = {
        // vptr, type
        {&ip, COSIZE_T},
        {&PReg.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    size_t preg = 1; // by default
    if (nArg == 2)
    {
        preg = GetExpression(PReg.data);
        if (preg > 19)
        {
            ExtOut("Pseudo-register number must be between 0 and 19\n");
            return Status;
        }
    }        

    sprintf_s(buffer,_countof (buffer),
        "r$t%d=0",
        preg);
    Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer ,0);
    if (FAILED(Status))
    {
        ExtOut("Error initialized register $t%d to zero\n", preg);
        return Status;
    }    

    ULONG64 base = 0;
    CLRDATA_ADDRESS cdaStart = TO_CDADDR(ip);
    DWORD codeType = 0;
    CLRDATA_ADDRESS addr;
    if(g_sos->GetMethodDescPtrFromIP(cdaStart, &addr) == S_OK)
    {
        WCHAR wszNameBuffer[1024]; // should be large enough

        // get the MethodDesc name
        if (g_sos->GetMethodDescName(addr, 1024, wszNameBuffer, NULL) == S_OK &&
            _wcsncmp(W("DomainBoundILStubClass"), wszNameBuffer, 22)==0)
        {
            ExtOut("ILStub\n");
            codeType = 2;
        }
        else
        {
            ExtOut("Jitted code");
            codeType = 1;
        }
    }
    else if(g_ExtSymbols->GetModuleByOffset (ip, 0, NULL, &base) == S_OK)
    {
        ULONG64 clrBaseAddr = 0;
        if(SUCCEEDED(g_ExtSymbols->GetModuleByModuleName (MAIN_CLR_MODULE_NAME_A,0,NULL, &clrBaseAddr)) && base==clrBaseAddr)
        {
            ExtOut("Compiled code in CLR");
            codeType = 4;
        }
        else
        {
            ExtOut("Compiled code in module @ 0x%I64x\n", base);
            codeType = 8;
        }
    }
    else
    {
        ExtOut("Not compiled or jitted, assuming stub\n");
        codeType = 16;
    }

    sprintf_s(buffer,_countof (buffer),
        "r$t%d=%x",
        preg, codeType);
    Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);
    if (FAILED(Status))
    {
        ExtOut("Error setting register $t%d\n", preg);
        return Status;
    }  
    return Status;

}
#endif // FEATURE_PAL

DECLARE_API(StopOnException)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    

    char buffer[100+mdNameLen];

    BOOL fDerived = FALSE;
    BOOL fCreate1 = FALSE;    
    BOOL fCreate2 = FALSE;    

    CMDOption option[] = {
        // name, vptr, type, hasValue
        {"-derived", &fDerived, COBOOL, FALSE}, // catch derived exceptions
        {"-create", &fCreate1, COBOOL, FALSE}, // create 1st chance handler
        {"-create2", &fCreate2, COBOOL, FALSE}, // create 2nd chance handler
    };

    StringHolder TypeName,PReg;
    
    CMDValue arg[] = {
        // vptr, type
        {&TypeName.data, COSTRING},
        {&PReg.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (IsDumpFile())
    {
        ExtOut("Live debugging session required\n");
        return Status;
    }
    if (nArg < 1 || nArg > 2)
    {
        ExtOut("usage: StopOnException [-derived] [-create | -create2] <type name>\n");
        ExtOut("                       [<pseudo-register number for result>]\n");            
        ExtOut("ex:    StopOnException -create System.OutOfMemoryException 1\n");
        return Status;
    }

    size_t preg = 1; // by default
    if (nArg == 2)
    {
        preg = GetExpression(PReg.data);
        if (preg > 19)
        {
            ExtOut("Pseudo-register number must be between 0 and 19\n");
            return Status;
        }
    }        

    sprintf_s(buffer,_countof (buffer),
        "r$t%d=0",
        preg);
    Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);
    if (FAILED(Status))
    {
        ExtOut("Error initialized register $t%d to zero\n", preg);
        return Status;
    }    
    
    if (fCreate1 || fCreate2)
    {            
        sprintf_s(buffer,_countof (buffer),
            "sxe %s \"!soe %s %s %d;.if(@$t%d==0) {g} .else {.echo '%s hit'}\" %x",
            fCreate1 ? "-c" : "-c2",
            fDerived ? "-derived" : "",
            TypeName.data,
            preg,
            preg,
            TypeName.data,
            EXCEPTION_COMPLUS
            );
            
        Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);        
        if (FAILED(Status))
        {
            ExtOut("Error setting breakpoint: %s\n", buffer);
            return Status;
        }        

        ExtOut("Breakpoint set\n");
        return Status;
    }    

    // Find the last thrown exception on this thread.
    // Does it match? If so, set the register.
    CLRDATA_ADDRESS threadAddr = GetCurrentManagedThread();
    DacpThreadData Thread;
    
    if ((threadAddr == NULL) || (Thread.Request(g_sos, threadAddr) != S_OK))
    {
        ExtOut("The current thread is unmanaged\n");
        return Status;
    }

    TADDR taLTOH;
    if (!SafeReadMemory(Thread.lastThrownObjectHandle,
                        &taLTOH,
                        sizeof(taLTOH), NULL))
    {
        ExtOut("There is no current managed exception on this thread\n");
        return Status;
    }
    
    if (taLTOH)
    {
        LPWSTR typeNameWide = (LPWSTR)alloca(mdNameLen * sizeof(WCHAR));
        MultiByteToWideChar(CP_ACP,0,TypeName.data,-1,typeNameWide,mdNameLen);
        
        TADDR taMT;
        if (SafeReadMemory(taLTOH, &taMT, sizeof(taMT), NULL))
        {            
            NameForMT_s (taMT, g_mdName, mdNameLen);
            if ((_wcscmp(g_mdName,typeNameWide) == 0) ||
                (fDerived && IsDerivedFrom(taMT, typeNameWide)))
            {
                sprintf_s(buffer,_countof (buffer),
                    "r$t%d=1",
                    preg);
                Status = g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, buffer, 0);
                if (FAILED(Status))
                {
                    ExtOut("Failed to execute the following command: %s\n", buffer);
                }
            }
        }
    }

    return Status;
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function finds the size of an object or all roots.           *  
*                                                                      *
\**********************************************************************/
DECLARE_API(ObjSize)
{
#ifndef FEATURE_PAL
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    
    
    BOOL dml = FALSE;
    StringHolder str_Object;    


    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&str_Object.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    TADDR obj = GetExpression(str_Object.data);

    GCRootImpl gcroot;
    if (obj == 0)
    {
        gcroot.ObjSize();
    }
    else
    {
        if(!sos::IsObject(obj))
        {
            ExtOut("%p is not a valid object.\n", SOS_PTR(obj));
            return Status;
        }

        size_t size = gcroot.ObjSize(obj);
        TADDR mt = 0;
        MOVE(mt, obj);
        sos::MethodTable methodTable = mt;
        ExtOut("sizeof(%p) = %d (0x%x) bytes (%S)\n", SOS_PTR(obj), size, size, methodTable.GetName());
    }
    return Status;
#else
    return E_NOTIMPL;
#endif

}

#ifndef FEATURE_PAL
// For FEATURE_PAL, MEMORY_BASIC_INFORMATION64 doesn't exist yet. TODO?
DECLARE_API(GCHandleLeaks)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    ExtOut("-------------------------------------------------------------------------------\n");
    ExtOut("GCHandleLeaks will report any GCHandles that couldn't be found in memory.      \n");
    ExtOut("Strong and Pinned GCHandles are reported at this time. You can safely abort the\n");
    ExtOut("memory scan with Control-C or Control-Break.                                   \n");
    ExtOut("-------------------------------------------------------------------------------\n");
    
    static DWORD_PTR array[2000];
    UINT i;
    BOOL dml = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"/d", &dml, COBOOL, FALSE},
    };

    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL)) 
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    
    UINT iFinal = FindAllPinnedAndStrong(array,sizeof(array)/sizeof(DWORD_PTR));
    ExtOut("Found %d handles:\n",iFinal);
    for (i=1;i<=iFinal;i++)
    {
        ExtOut("%p\t", SOS_PTR(array[i-1]));
        if ((i % 4) == 0)
            ExtOut("\n");
    }

    ExtOut("\nSearching memory\n");
    // Now search memory for this:
    DWORD_PTR buffer[1024];
    ULONG64 memCur = 0x0;
    BOOL bAbort = FALSE;

    //find out memory used by stress log
    StressLogMem stressLog;
    CLRDATA_ADDRESS StressLogAddress = NULL;
    if (LoadClrDebugDll() != S_OK)
    {
        // Try to find stress log symbols
        DWORD_PTR dwAddr = GetValueFromExpression(MAIN_CLR_MODULE_NAME_A "!StressLog::theLog");
        StressLogAddress = dwAddr;        
        g_bDacBroken = TRUE;
    }
    else
    {
        if (g_sos->GetStressLogAddress(&StressLogAddress) != S_OK)
        {
            ExtOut("Unable to find stress log via DAC\n");
        }
        g_bDacBroken = FALSE;
    }

    if (stressLog.Init (StressLogAddress, g_ExtData))
    {
        ExtOut("Reference found in stress log will be ignored\n");
    }
    else
    {
        ExtOut("Failed to read whole or part of stress log, some references may come from stress log\n");
    }
    
    
    while (!bAbort)
    {
        NTSTATUS status;
        MEMORY_BASIC_INFORMATION64 memInfo;

        status = g_ExtData2->QueryVirtual(UL64_TO_CDA(memCur), &memInfo);
                
        if( !NT_SUCCESS(status) ) 
        {            
            break;
        }

        if (memInfo.State == MEM_COMMIT)
        {            
            for (ULONG64 memIter = memCur; memIter < (memCur + memInfo.RegionSize); memIter+=sizeof(buffer))
            {
                if (IsInterrupt())
                {
                    ExtOut("Quitting at %p due to user abort\n", SOS_PTR(memIter));
                    bAbort = TRUE;
                    break;
                }

                if ((memIter % 0x10000000)==0x0)
                {
                    ExtOut("Searching %p...\n", SOS_PTR(memIter));
                }
                
                ULONG size = 0;
                HRESULT ret;
                ret = g_ExtData->ReadVirtual(UL64_TO_CDA(memIter), buffer, sizeof(buffer), &size);
                if (ret == S_OK)
                {
                    for (UINT x=0;x<1024;x++)
                    {            
                        DWORD_PTR value = buffer[x];
                        // We don't care about the low bit. Also, the GCHandle class turns on the
                        // low bit for pinned handles, so without the statement below, we wouldn't
                        // notice pinned handles.
                        value = value & ~1; 
                        for (i=0;i<iFinal;i++)
                        {
                            ULONG64 addrInDebugee = (ULONG64)memIter+(x*sizeof(DWORD_PTR));
                            if ((array[i] & ~1) == value)
                            {
                                if (stressLog.IsInStressLog (addrInDebugee))
                                {
                                    ExtOut("Found %p in stress log at location %p, reference not counted\n", SOS_PTR(value), addrInDebugee);
                                }
                                else
                                {
                                    ExtOut("Found %p at location %p\n", SOS_PTR(value), addrInDebugee);
                                    array[i] |= 0x1;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (size > 0)
                    {
                        ExtOut("only read %x bytes at %p\n", size, SOS_PTR(memIter));
                    }
                }
            }
        }
        
        memCur += memInfo.RegionSize;
    }

    int numNotFound = 0;
    for (i=0;i<iFinal;i++)
    {
        if ((array[i] & 0x1) == 0)
        {
            numNotFound++;
            // ExtOut("WARNING: %p not found\n", SOS_PTR(array[i]));
        }
    }

    if (numNotFound > 0)
    {
        ExtOut("------------------------------------------------------------------------------\n");    
        ExtOut("Some handles were not found. If the number of not-found handles grows over the\n");
        ExtOut("lifetime of your application, you may have a GCHandle leak. This will cause   \n");
        ExtOut("the GC Heap to grow larger as objects are being kept alive, referenced only   \n");
        ExtOut("by the orphaned handle. If the number doesn't grow over time, note that there \n");
        ExtOut("may be some noise in this output, as an unmanaged application may be storing  \n");
        ExtOut("the handle in a non-standard way, perhaps with some bits flipped. The memory  \n");
        ExtOut("scan wouldn't be able to find those.                                          \n");
        ExtOut("------------------------------------------------------------------------------\n");    

        ExtOut("Didn't find %d handles:\n", numNotFound);
        int numPrinted=0;
        for (i=0;i<iFinal;i++)
        {
            if ((array[i] & 0x1) == 0)
            {
                numPrinted++;
                ExtOut("%p\t", SOS_PTR(array[i]));
                if ((numPrinted % 4) == 0)
                    ExtOut("\n");
            }
        }   
        ExtOut("\n");
    }
    else
    {       
        ExtOut("------------------------------------------------------------------------------\n");    
        ExtOut("All handles found");
        if (bAbort)
            ExtOut(" even though you aborted.\n");
        else
            ExtOut(".\n");        
        ExtOut("A leak may still exist because in a general scan of process memory SOS can't  \n");
        ExtOut("differentiate between garbage and valid structures, so you may have false     \n");
        ExtOut("positives. If you still suspect a leak, use this function over time to        \n");
        ExtOut("identify a possible trend.                                                    \n");
        ExtOut("------------------------------------------------------------------------------\n");    
    }
    
    return Status;
}
#endif // FEATURE_PAL

#endif // FEATURE_PAL

class ClrStackImplWithICorDebug
{
private:
    static HRESULT DereferenceAndUnboxValue(ICorDebugValue * pValue, ICorDebugValue** ppOutputValue, BOOL * pIsNull = NULL)
    {
        HRESULT Status = S_OK;
        *ppOutputValue = NULL;
        if(pIsNull != NULL) *pIsNull = FALSE;

        ToRelease<ICorDebugReferenceValue> pReferenceValue;
        Status = pValue->QueryInterface(IID_ICorDebugReferenceValue, (LPVOID*) &pReferenceValue);
        if (SUCCEEDED(Status))
        {
            BOOL isNull = FALSE;
            IfFailRet(pReferenceValue->IsNull(&isNull));
            if(!isNull)
            {
                ToRelease<ICorDebugValue> pDereferencedValue;
                IfFailRet(pReferenceValue->Dereference(&pDereferencedValue));
                return DereferenceAndUnboxValue(pDereferencedValue, ppOutputValue);
            }
            else
            {
                if(pIsNull != NULL) *pIsNull = TRUE;
                *ppOutputValue = pValue;
                (*ppOutputValue)->AddRef();
                return S_OK;
            }
        }

        ToRelease<ICorDebugBoxValue> pBoxedValue;
        Status = pValue->QueryInterface(IID_ICorDebugBoxValue, (LPVOID*) &pBoxedValue);
        if (SUCCEEDED(Status))
        {
            ToRelease<ICorDebugObjectValue> pUnboxedValue;
            IfFailRet(pBoxedValue->GetObject(&pUnboxedValue));
            return DereferenceAndUnboxValue(pUnboxedValue, ppOutputValue);
        }
        *ppOutputValue = pValue;
        (*ppOutputValue)->AddRef();
        return S_OK;
    }

    static BOOL ShouldExpandVariable(__in_z WCHAR* varToExpand, __in_z WCHAR* currentExpansion)
    {
        if(currentExpansion == NULL || varToExpand == NULL) return FALSE;

        size_t varToExpandLen = _wcslen(varToExpand);
        size_t currentExpansionLen = _wcslen(currentExpansion);
        if(currentExpansionLen > varToExpandLen) return FALSE;
        if(currentExpansionLen < varToExpandLen && varToExpand[currentExpansionLen] != L'.') return FALSE;
        if(_wcsncmp(currentExpansion, varToExpand, currentExpansionLen) != 0) return FALSE;

        return TRUE;
    }

    static BOOL IsEnum(ICorDebugValue * pInputValue)
    {
        ToRelease<ICorDebugValue> pValue;
        if(FAILED(DereferenceAndUnboxValue(pInputValue, &pValue, NULL))) return FALSE;

        WCHAR baseTypeName[mdNameLen];
        ToRelease<ICorDebugValue2> pValue2;
        ToRelease<ICorDebugType> pType;
        ToRelease<ICorDebugType> pBaseType;

        if(FAILED(pValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2))) return FALSE;
        if(FAILED(pValue2->GetExactType(&pType))) return FALSE;
        if(FAILED(pType->GetBase(&pBaseType)) || pBaseType == NULL) return FALSE;
        if(FAILED(GetTypeOfValue(pBaseType, baseTypeName, mdNameLen))) return  FALSE;

        return (_wcsncmp(baseTypeName, W("System.Enum"), 11) == 0);
    }

    static HRESULT AddGenericArgs(ICorDebugType * pType, __inout_ecount(typeNameLen) WCHAR* typeName, ULONG typeNameLen)
    {
        bool isFirst = true;
        ToRelease<ICorDebugTypeEnum> pTypeEnum;
        if(SUCCEEDED(pType->EnumerateTypeParameters(&pTypeEnum)))
        {
            ULONG numTypes = 0;
            ToRelease<ICorDebugType> pCurrentTypeParam;
            
            while(SUCCEEDED(pTypeEnum->Next(1, &pCurrentTypeParam, &numTypes)))
            {
                if(numTypes == 0) break;

                if(isFirst)
                {
                    isFirst = false;
                    wcsncat_s(typeName, typeNameLen, W("&lt;"), typeNameLen);
                }
                else wcsncat_s(typeName, typeNameLen, W(","), typeNameLen);

                WCHAR typeParamName[mdNameLen];
                typeParamName[0] = L'\0';
                GetTypeOfValue(pCurrentTypeParam, typeParamName, mdNameLen);
                wcsncat_s(typeName, typeNameLen, typeParamName, typeNameLen);
            }
            if(!isFirst)
                wcsncat_s(typeName, typeNameLen, W("&gt;"), typeNameLen);
        }

        return S_OK;
    }

    static HRESULT GetTypeOfValue(ICorDebugType * pType, __inout_ecount(typeNameLen) WCHAR* typeName, ULONG typeNameLen)
    {
        HRESULT Status = S_OK;

        CorElementType corElemType;
        IfFailRet(pType->GetType(&corElemType));

        switch (corElemType)
        {
        //List of unsupported CorElementTypes:
        //ELEMENT_TYPE_END            = 0x0,
        //ELEMENT_TYPE_VAR            = 0x13,     // a class type variable VAR <U1>
        //ELEMENT_TYPE_GENERICINST    = 0x15,     // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
        //ELEMENT_TYPE_TYPEDBYREF     = 0x16,     // TYPEDREF  (it takes no args) a typed referece to some other type
        //ELEMENT_TYPE_MVAR           = 0x1e,     // a method type variable MVAR <U1>
        //ELEMENT_TYPE_CMOD_REQD      = 0x1F,     // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef>
        //ELEMENT_TYPE_CMOD_OPT       = 0x20,     // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>
        //ELEMENT_TYPE_INTERNAL       = 0x21,     // INTERNAL <typehandle>
        //ELEMENT_TYPE_MAX            = 0x22,     // first invalid element type
        //ELEMENT_TYPE_MODIFIER       = 0x40,
        //ELEMENT_TYPE_SENTINEL       = 0x01 | ELEMENT_TYPE_MODIFIER, // sentinel for varargs
        //ELEMENT_TYPE_PINNED         = 0x05 | ELEMENT_TYPE_MODIFIER,
        //ELEMENT_TYPE_R4_HFA         = 0x06 | ELEMENT_TYPE_MODIFIER, // used only internally for R4 HFA types
        //ELEMENT_TYPE_R8_HFA         = 0x07 | ELEMENT_TYPE_MODIFIER, // used only internally for R8 HFA types
        default:
            swprintf_s(typeName, typeNameLen, W("(Unhandled CorElementType: 0x%x)\0"), corElemType);
            break;

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
            {
                //Defaults in case we fail...
                if(corElemType == ELEMENT_TYPE_VALUETYPE) swprintf_s(typeName, typeNameLen, W("struct\0"));
                else swprintf_s(typeName, typeNameLen, W("class\0"));

                mdTypeDef typeDef;
                ToRelease<ICorDebugClass> pClass;
                if(SUCCEEDED(pType->GetClass(&pClass)) && SUCCEEDED(pClass->GetToken(&typeDef)))
                {
                    ToRelease<ICorDebugModule> pModule;
                    IfFailRet(pClass->GetModule(&pModule));

                    ToRelease<IUnknown> pMDUnknown;
                    ToRelease<IMetaDataImport> pMD;
                    IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
                    IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));

                    if(SUCCEEDED(NameForToken_s(TokenFromRid(typeDef, mdtTypeDef), pMD, g_mdName, mdNameLen, false)))
                        swprintf_s(typeName, typeNameLen, W("%s\0"), g_mdName);
                }
                AddGenericArgs(pType, typeName, typeNameLen);
            }
            break;
        case ELEMENT_TYPE_VOID:
            swprintf_s(typeName, typeNameLen, W("void\0"));
            break;
        case ELEMENT_TYPE_BOOLEAN:
            swprintf_s(typeName, typeNameLen, W("bool\0"));
            break;
        case ELEMENT_TYPE_CHAR:
            swprintf_s(typeName, typeNameLen, W("char\0"));
            break;
        case ELEMENT_TYPE_I1:
            swprintf_s(typeName, typeNameLen, W("signed byte\0"));
            break;
        case ELEMENT_TYPE_U1:
            swprintf_s(typeName, typeNameLen, W("byte\0"));
            break;
        case ELEMENT_TYPE_I2:
            swprintf_s(typeName, typeNameLen, W("short\0"));
            break;
        case ELEMENT_TYPE_U2:
            swprintf_s(typeName, typeNameLen, W("unsigned short\0"));
            break;    
        case ELEMENT_TYPE_I4:
            swprintf_s(typeName, typeNameLen, W("int\0"));
            break;
        case ELEMENT_TYPE_U4:
            swprintf_s(typeName, typeNameLen, W("unsigned int\0"));
            break;
        case ELEMENT_TYPE_I8:
            swprintf_s(typeName, typeNameLen, W("long\0"));
            break;
        case ELEMENT_TYPE_U8:
            swprintf_s(typeName, typeNameLen, W("unsigned long\0"));
            break;
        case ELEMENT_TYPE_R4:
            swprintf_s(typeName, typeNameLen, W("float\0"));
            break;
        case ELEMENT_TYPE_R8:
            swprintf_s(typeName, typeNameLen, W("double\0"));
            break;
        case ELEMENT_TYPE_OBJECT:
            swprintf_s(typeName, typeNameLen, W("object\0"));
            break;
        case ELEMENT_TYPE_STRING:
            swprintf_s(typeName, typeNameLen, W("string\0"));
            break;
        case ELEMENT_TYPE_I:
            swprintf_s(typeName, typeNameLen, W("IntPtr\0"));
            break;
        case ELEMENT_TYPE_U:
            swprintf_s(typeName, typeNameLen, W("UIntPtr\0"));
            break;
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_PTR:
            {
                ToRelease<ICorDebugType> pFirstParameter;
                if(SUCCEEDED(pType->GetFirstTypeParameter(&pFirstParameter)))
                    GetTypeOfValue(pFirstParameter, typeName, typeNameLen);
                else
                    swprintf_s(typeName, typeNameLen, W("<unknown>\0"));

                switch(corElemType)
                {
                case ELEMENT_TYPE_SZARRAY: 
                    wcsncat_s(typeName, typeNameLen, W("[]\0"), typeNameLen);
                    return S_OK;
                case ELEMENT_TYPE_ARRAY:
                    {
                        ULONG32 rank = 0;
                        pType->GetRank(&rank);
                        wcsncat_s(typeName, typeNameLen, W("["), typeNameLen);
                        for(ULONG32 i = 0; i < rank - 1; i++)
                        {
                            // 
                            wcsncat_s(typeName, typeNameLen, W(","), typeNameLen);
                        }
                        wcsncat_s(typeName, typeNameLen, W("]\0"), typeNameLen);
                    }
                    return S_OK;
                case ELEMENT_TYPE_BYREF:   
                    wcsncat_s(typeName, typeNameLen, W("&\0"), typeNameLen);
                    return S_OK;
                case ELEMENT_TYPE_PTR:     
                    wcsncat_s(typeName, typeNameLen, W("*\0"), typeNameLen);
                    return S_OK;
                default:
                    // note we can never reach here as this is a nested switch
                    // and corElemType can only be one of the values above
                    break;
                }
            }
            break;
        case ELEMENT_TYPE_FNPTR:
            swprintf_s(typeName, typeNameLen, W("*(...)\0"));
            break;
        case ELEMENT_TYPE_TYPEDBYREF:
            swprintf_s(typeName, typeNameLen, W("typedbyref\0"));
            break;
        }
        return S_OK;
    }

    static HRESULT GetTypeOfValue(ICorDebugValue * pValue, __inout_ecount(typeNameLen) WCHAR* typeName, ULONG typeNameLen)
    {
        HRESULT Status = S_OK;

        CorElementType corElemType;
        IfFailRet(pValue->GetType(&corElemType));

        ToRelease<ICorDebugType> pType;
        ToRelease<ICorDebugValue2> pValue2;
        if(SUCCEEDED(pValue->QueryInterface(IID_ICorDebugValue2, (void**) &pValue2)) && SUCCEEDED(pValue2->GetExactType(&pType)))
            return GetTypeOfValue(pType, typeName, typeNameLen);
        else
            swprintf_s(typeName, typeNameLen, W("<unknown>\0"));

        return S_OK;
    }

    static HRESULT PrintEnumValue(ICorDebugValue* pInputValue, BYTE* enumValue)
    {
        HRESULT Status = S_OK;

        ToRelease<ICorDebugValue> pValue;
        IfFailRet(DereferenceAndUnboxValue(pInputValue, &pValue, NULL));

        mdTypeDef currentTypeDef;
        ToRelease<ICorDebugClass> pClass;
        ToRelease<ICorDebugValue2> pValue2;
        ToRelease<ICorDebugType> pType;
        ToRelease<ICorDebugModule> pModule;
        IfFailRet(pValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2));
        IfFailRet(pValue2->GetExactType(&pType));
        IfFailRet(pType->GetClass(&pClass));
        IfFailRet(pClass->GetModule(&pModule));
        IfFailRet(pClass->GetToken(&currentTypeDef));

        ToRelease<IUnknown> pMDUnknown;
        ToRelease<IMetaDataImport> pMD;
        IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
        IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));


        //First, we need to figure out the underlying enum type so that we can correctly type cast the raw values of each enum constant
        //We get that from the non-static field of the enum variable (I think the field is called __value or something similar)
        ULONG numFields = 0;
        HCORENUM fEnum = NULL;
        mdFieldDef fieldDef;
        CorElementType enumUnderlyingType = ELEMENT_TYPE_END;
        while(SUCCEEDED(pMD->EnumFields(&fEnum, currentTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
        {
            DWORD             fieldAttr = 0;
            PCCOR_SIGNATURE   pSignatureBlob = NULL;
            ULONG             sigBlobLength = 0;
            if(SUCCEEDED(pMD->GetFieldProps(fieldDef, NULL, NULL, 0, NULL, &fieldAttr, &pSignatureBlob, &sigBlobLength, NULL, NULL, NULL)))
            {
                if((fieldAttr & fdStatic) == 0)
                {
                    CorSigUncompressCallingConv(pSignatureBlob);
                    enumUnderlyingType = CorSigUncompressElementType(pSignatureBlob);
                    break;
                }
            }
        }
        pMD->CloseEnum(fEnum);


        //Now that we know the underlying enum type, let's decode the enum variable into OR-ed, human readable enum contants
        fEnum = NULL;
        bool isFirst = true;
        ULONG64 remainingValue = *((ULONG64*)enumValue);
        while(SUCCEEDED(pMD->EnumFields(&fEnum, currentTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
        {
            ULONG             nameLen = 0;
            DWORD             fieldAttr = 0;
            WCHAR             mdName[mdNameLen];
            UVCP_CONSTANT     pRawValue = NULL;
            ULONG             rawValueLength = 0;
            if(SUCCEEDED(pMD->GetFieldProps(fieldDef, NULL, mdName, mdNameLen, &nameLen, &fieldAttr, NULL, NULL, NULL, &pRawValue, &rawValueLength)))
            {
                DWORD enumValueRequiredAttributes = fdPublic | fdStatic | fdLiteral | fdHasDefault;
                if((fieldAttr & enumValueRequiredAttributes) != enumValueRequiredAttributes)
                    continue;

                ULONG64 currentConstValue = 0;
                switch (enumUnderlyingType)
                {
                    case ELEMENT_TYPE_CHAR:
                    case ELEMENT_TYPE_I1:
                        currentConstValue = (ULONG64)(*((CHAR*)pRawValue));
                        break;
                    case ELEMENT_TYPE_U1:
                        currentConstValue = (ULONG64)(*((BYTE*)pRawValue));
                        break;
                    case ELEMENT_TYPE_I2:
                        currentConstValue = (ULONG64)(*((SHORT*)pRawValue));
                        break;
                    case ELEMENT_TYPE_U2:
                        currentConstValue = (ULONG64)(*((USHORT*)pRawValue));
                        break;
                    case ELEMENT_TYPE_I4:
                        currentConstValue = (ULONG64)(*((INT32*)pRawValue));
                        break;
                    case ELEMENT_TYPE_U4:
                        currentConstValue = (ULONG64)(*((UINT32*)pRawValue));
                        break;
                    case ELEMENT_TYPE_I8:
                        currentConstValue = (ULONG64)(*((LONG*)pRawValue));
                        break;
                    case ELEMENT_TYPE_U8:
                        currentConstValue = (ULONG64)(*((ULONG*)pRawValue));
                        break;
                    case ELEMENT_TYPE_I:
                        currentConstValue = (ULONG64)(*((int*)pRawValue));
                        break;
                    case ELEMENT_TYPE_U:
                    case ELEMENT_TYPE_R4:
                    case ELEMENT_TYPE_R8:
                    // Technically U and the floating-point ones are options in the CLI, but not in the CLS or C#, so these are NYI
                    default:
                        currentConstValue = 0;
                }

                if((currentConstValue == remainingValue) || ((currentConstValue != 0) && ((currentConstValue & remainingValue) == currentConstValue)))
                {
                    remainingValue &= ~currentConstValue;
                    if(isFirst)
                    {
                        ExtOut(" = %S", mdName);
                        isFirst = false;
                    }
                    else ExtOut(" | %S", mdName);
                }
            }
        }
        pMD->CloseEnum(fEnum);

        return S_OK;
    }

    static HRESULT PrintStringValue(ICorDebugValue * pValue)
    {
        HRESULT Status;

        ToRelease<ICorDebugStringValue> pStringValue;
        IfFailRet(pValue->QueryInterface(IID_ICorDebugStringValue, (LPVOID*) &pStringValue));

        ULONG32 cchValue;
        IfFailRet(pStringValue->GetLength(&cchValue));
        cchValue++;         // Allocate one more for null terminator

        CQuickString quickString;
        quickString.Alloc(cchValue);

        ULONG32 cchValueReturned;
        IfFailRet(pStringValue->GetString(
            cchValue,
            &cchValueReturned,
            quickString.String()));

        ExtOut(" = \"%S\"\n", quickString.String());
        
        return S_OK;
    }

    static HRESULT PrintSzArrayValue(ICorDebugValue * pValue, ICorDebugILFrame * pILFrame, IMetaDataImport * pMD, int indent, __in_z WCHAR* varToExpand, __inout_ecount(currentExpansionSize) WCHAR* currentExpansion, DWORD currentExpansionSize, int currentFrame)
    {
        HRESULT Status = S_OK;

        ToRelease<ICorDebugArrayValue> pArrayValue;
        IfFailRet(pValue->QueryInterface(IID_ICorDebugArrayValue, (LPVOID*) &pArrayValue));

        ULONG32 nRank;
        IfFailRet(pArrayValue->GetRank(&nRank));
        if (nRank != 1)
        {
            return E_UNEXPECTED;
        }

        ULONG32 cElements;
        IfFailRet(pArrayValue->GetCount(&cElements));

        if (cElements == 0) ExtOut("   (empty)\n");
        else if (cElements == 1) ExtOut("   (1 element)\n");
        else ExtOut("   (%d elements)\n", cElements);

        if(!ShouldExpandVariable(varToExpand, currentExpansion)) return S_OK;
        size_t currentExpansionLen = _wcslen(currentExpansion);

        for (ULONG32 i=0; i < cElements; i++)
        {
            for(int j = 0; j <= indent; j++) ExtOut("    ");
            currentExpansion[currentExpansionLen] = L'\0';
            swprintf_s(currentExpansion, mdNameLen, W("%s.[%d]\0"), currentExpansion, i);

            bool printed = false;
            CorElementType corElemType;
            ToRelease<ICorDebugType> pFirstParameter;
            ToRelease<ICorDebugValue2> pValue2;
            ToRelease<ICorDebugType> pType;
            if(SUCCEEDED(pArrayValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2)) && SUCCEEDED(pValue2->GetExactType(&pType)))
            {
                if(SUCCEEDED(pType->GetFirstTypeParameter(&pFirstParameter)) && SUCCEEDED(pFirstParameter->GetType(&corElemType)))
                {
                    switch(corElemType)
                    {
                    //If the array element is something that we can expand with !clrstack, show information about the type of this element
                    case ELEMENT_TYPE_VALUETYPE:
                    case ELEMENT_TYPE_CLASS:
                    case ELEMENT_TYPE_SZARRAY:
                        {
                            WCHAR typeOfElement[mdNameLen];
                            GetTypeOfValue(pFirstParameter, typeOfElement, mdNameLen);
                            DMLOut(" |- %s = %S", DMLManagedVar(currentExpansion, currentFrame, i), typeOfElement);
                            printed = true;
                        }
                        break;
                    default:
                        break;
                    }
                }
            }
            if(!printed) DMLOut(" |- %s", DMLManagedVar(currentExpansion, currentFrame, i));

            ToRelease<ICorDebugValue> pElementValue;
            IfFailRet(pArrayValue->GetElementAtPosition(i, &pElementValue));
            IfFailRet(PrintValue(pElementValue, pILFrame, pMD, indent + 1, varToExpand, currentExpansion, currentExpansionSize, currentFrame));
        }

        return S_OK;
    }

    static HRESULT PrintValue(ICorDebugValue * pInputValue, ICorDebugILFrame * pILFrame, IMetaDataImport * pMD, int indent, __in_z WCHAR* varToExpand, __inout_ecount(currentExpansionSize) WCHAR* currentExpansion, DWORD currentExpansionSize, int currentFrame)
    {
        HRESULT Status = S_OK;

        BOOL isNull = TRUE;
        ToRelease<ICorDebugValue> pValue;
        IfFailRet(DereferenceAndUnboxValue(pInputValue, &pValue, &isNull));

        if(isNull)
        {
            ExtOut(" = null\n");
            return S_OK;
        }

        ULONG32 cbSize;
        IfFailRet(pValue->GetSize(&cbSize));
        ArrayHolder<BYTE> rgbValue = new NOTHROW BYTE[cbSize];
        if (rgbValue == NULL)
        {
            ReportOOM();
            return E_OUTOFMEMORY;
        }

        memset(rgbValue.GetPtr(), 0, cbSize * sizeof(BYTE));

        CorElementType corElemType;
        IfFailRet(pValue->GetType(&corElemType));
        if (corElemType == ELEMENT_TYPE_STRING)
        {
            return PrintStringValue(pValue);
        }

        if (corElemType == ELEMENT_TYPE_SZARRAY)
        {
            return PrintSzArrayValue(pValue, pILFrame, pMD, indent, varToExpand, currentExpansion, currentExpansionSize, currentFrame);
        }

        ToRelease<ICorDebugGenericValue> pGenericValue;
        IfFailRet(pValue->QueryInterface(IID_ICorDebugGenericValue, (LPVOID*) &pGenericValue));
        IfFailRet(pGenericValue->GetValue((LPVOID) &(rgbValue[0])));

        if(IsEnum(pValue))
        {
            Status = PrintEnumValue(pValue, rgbValue);
            ExtOut("\n");
            return Status;
        }

        switch (corElemType)
        {
        default:
            ExtOut("  (Unhandled CorElementType: 0x%x)\n", corElemType);
            break;

        case ELEMENT_TYPE_PTR:
            ExtOut("  = <pointer>\n");
            break;

        case ELEMENT_TYPE_FNPTR:
            {
                CORDB_ADDRESS addr = 0;
                ToRelease<ICorDebugReferenceValue> pReferenceValue = NULL;
                if(SUCCEEDED(pValue->QueryInterface(IID_ICorDebugReferenceValue, (LPVOID*) &pReferenceValue)))
                    pReferenceValue->GetValue(&addr);
                ExtOut("  = <function pointer 0x%x>\n", addr);
            }
            break;

        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
            CORDB_ADDRESS addr;
            if(SUCCEEDED(pValue->GetAddress(&addr)))
            {
                ExtOut(" @ 0x%I64x\n", addr);
            }
            else
            {
                ExtOut("\n");
            }
            ProcessFields(pValue, NULL, pILFrame, indent + 1, varToExpand, currentExpansion, currentExpansionSize, currentFrame);
            break;

        case ELEMENT_TYPE_BOOLEAN:
            ExtOut("  = %s\n", rgbValue[0] == 0 ? "false" : "true");
            break;

        case ELEMENT_TYPE_CHAR:
            ExtOut("  = '%C'\n", *(WCHAR *) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_I1:
            ExtOut("  = %d\n", *(char*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_U1:
            ExtOut("  = %d\n", *(unsigned char*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_I2:
            ExtOut("  = %hd\n", *(short*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_U2:
            ExtOut("  = %hu\n", *(unsigned short*) &(rgbValue[0]));
            break;
        
        case ELEMENT_TYPE_I:
            ExtOut("  = %d\n", *(int*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_U:
            ExtOut("  = %u\n", *(unsigned int*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_I4:
            ExtOut("  = %d\n", *(int*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_U4:
            ExtOut("  = %u\n", *(unsigned int*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_I8:
            ExtOut("  = %I64d\n", *(__int64*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_U8:
            ExtOut("  = %I64u\n", *(unsigned __int64*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_R4:
            ExtOut("  = %f\n", (double) *(float*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_R8:
            ExtOut("  = %f\n", *(double*) &(rgbValue[0]));
            break;

        case ELEMENT_TYPE_OBJECT:
            ExtOut("  = object\n");
            break;

            // TODO: The following corElementTypes are not yet implemented here.  Array
            // might be interesting to add, though the others may be of rather limited use:
            // ELEMENT_TYPE_ARRAY          = 0x14,     // MDARRAY <type> <rank> <bcount> <bound1> ... <lbcount> <lb1> ...
            // 
            // ELEMENT_TYPE_GENERICINST    = 0x15,     // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
        }

        return S_OK;
    }

    static HRESULT PrintParameters(BOOL bParams, BOOL bLocals, IMetaDataImport * pMD, mdTypeDef typeDef, mdMethodDef methodDef, ICorDebugILFrame * pILFrame, ICorDebugModule * pModule, __in_z WCHAR* varToExpand, int currentFrame)
    {
        HRESULT Status = S_OK;

        ULONG cParams = 0;
        ToRelease<ICorDebugValueEnum> pParamEnum;
        IfFailRet(pILFrame->EnumerateArguments(&pParamEnum));
        IfFailRet(pParamEnum->GetCount(&cParams));
        if (cParams > 0 && bParams)
        {
            DWORD methAttr = 0;
            IfFailRet(pMD->GetMethodProps(methodDef, NULL, NULL, 0, NULL, &methAttr, NULL, NULL, NULL, NULL));

            ExtOut("\nPARAMETERS:\n");
            for (ULONG i=0; i < cParams; i++)
            {
                ULONG paramNameLen = 0;
                mdParamDef paramDef;
                WCHAR paramName[mdNameLen] = W("\0");

                if(i == 0 && (methAttr & mdStatic) == 0)
                    swprintf_s(paramName, mdNameLen, W("this\0"));
                else 
                {
                    int idx = ((methAttr & mdStatic) == 0)? i : (i + 1);
                    if(SUCCEEDED(pMD->GetParamForMethodIndex(methodDef, idx, &paramDef)))
                        pMD->GetParamProps(paramDef, NULL, NULL, paramName, mdNameLen, &paramNameLen, NULL, NULL, NULL, NULL);
                }
                if(_wcslen(paramName) == 0)
                    swprintf_s(paramName, mdNameLen, W("param_%d\0"), i);

                ToRelease<ICorDebugValue> pValue;
                ULONG cArgsFetched;
                Status = pParamEnum->Next(1, &pValue, &cArgsFetched);

                if (FAILED(Status))
                {
                    ExtOut("  + (Error 0x%x retrieving parameter '%S')\n", Status, paramName);
                    continue;
                }

                if (Status == S_FALSE)
                {
                    break;
                }

                WCHAR typeName[mdNameLen] = W("\0");
                GetTypeOfValue(pValue, typeName, mdNameLen);
                DMLOut("  + %S %s", typeName, DMLManagedVar(paramName, currentFrame, paramName));

                ToRelease<ICorDebugReferenceValue> pRefValue;
                if(SUCCEEDED(pValue->QueryInterface(IID_ICorDebugReferenceValue, (void**)&pRefValue)) && pRefValue != NULL)
                {
                    BOOL bIsNull = TRUE;
                    pRefValue->IsNull(&bIsNull);
                    if(bIsNull)
                    {
                        ExtOut(" = null\n");
                        continue;
                    }
                }

                WCHAR currentExpansion[mdNameLen];
                swprintf_s(currentExpansion, mdNameLen, W("%s\0"), paramName);
                if((Status=PrintValue(pValue, pILFrame, pMD, 0, varToExpand, currentExpansion, mdNameLen, currentFrame)) != S_OK)
                    ExtOut("  + (Error 0x%x printing parameter %d)\n", Status, i);
            }
        }
        else if (cParams == 0 && bParams)
            ExtOut("\nPARAMETERS: (none)\n");

        ULONG cLocals = 0;
        ToRelease<ICorDebugValueEnum> pLocalsEnum;
        IfFailRet(pILFrame->EnumerateLocalVariables(&pLocalsEnum));
        IfFailRet(pLocalsEnum->GetCount(&cLocals));
        if (cLocals > 0 && bLocals)
        {
            bool symbolsAvailable = false;
            SymbolReader symReader;
            if(SUCCEEDED(symReader.LoadSymbols(pMD, pModule)))
                symbolsAvailable = true;
            ExtOut("\nLOCALS:\n");
            for (ULONG i=0; i < cLocals; i++)
            {
                ULONG paramNameLen = 0;
                WCHAR paramName[mdNameLen] = W("\0");

                ToRelease<ICorDebugValue> pValue;
                if(symbolsAvailable)
                {
                    Status = symReader.GetNamedLocalVariable(pILFrame, i, paramName, mdNameLen, &pValue);
                }
                else
                {
                    ULONG cArgsFetched;
                    Status = pLocalsEnum->Next(1, &pValue, &cArgsFetched);
                }
                if(_wcslen(paramName) == 0)
                    swprintf_s(paramName, mdNameLen, W("local_%d\0"), i);

                if (FAILED(Status))
                {
                    ExtOut("  + (Error 0x%x retrieving local variable '%S')\n", Status, paramName);
                    continue;
                }

                if (Status == S_FALSE)
                {
                    break;
                }

                WCHAR typeName[mdNameLen] = W("\0");
                GetTypeOfValue(pValue, typeName, mdNameLen);
                DMLOut("  + %S %s", typeName, DMLManagedVar(paramName, currentFrame, paramName));

                ToRelease<ICorDebugReferenceValue> pRefValue = NULL;
                if(SUCCEEDED(pValue->QueryInterface(IID_ICorDebugReferenceValue, (void**)&pRefValue)) && pRefValue != NULL)
                {
                    BOOL bIsNull = TRUE;
                    pRefValue->IsNull(&bIsNull);
                    if(bIsNull)
                    {
                        ExtOut(" = null\n");
                        continue;
                    }
                }

                WCHAR currentExpansion[mdNameLen];
                swprintf_s(currentExpansion, mdNameLen, W("%s\0"), paramName);
                if((Status=PrintValue(pValue, pILFrame, pMD, 0, varToExpand, currentExpansion, mdNameLen, currentFrame)) != S_OK)
                    ExtOut("  + (Error 0x%x printing local variable %d)\n", Status, i);
            }
        }
        else if (cLocals == 0 && bLocals)
            ExtOut("\nLOCALS: (none)\n");

        if(bParams || bLocals)
            ExtOut("\n");

        return S_OK;
    }

    static HRESULT ProcessFields(ICorDebugValue* pInputValue, ICorDebugType* pTypeCast, ICorDebugILFrame * pILFrame, int indent, __in_z WCHAR* varToExpand, __inout_ecount(currentExpansionSize) WCHAR* currentExpansion, DWORD currentExpansionSize, int currentFrame)
    {
        if(!ShouldExpandVariable(varToExpand, currentExpansion)) return S_OK;
        size_t currentExpansionLen = _wcslen(currentExpansion);

        HRESULT Status = S_OK;

        BOOL isNull = FALSE;
        ToRelease<ICorDebugValue> pValue;
        IfFailRet(DereferenceAndUnboxValue(pInputValue, &pValue, &isNull));

        if(isNull) return S_OK;

        mdTypeDef currentTypeDef;
        ToRelease<ICorDebugClass> pClass;
        ToRelease<ICorDebugValue2> pValue2;
        ToRelease<ICorDebugType> pType;
        ToRelease<ICorDebugModule> pModule;
        IfFailRet(pValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2));
        if(pTypeCast == NULL)
            IfFailRet(pValue2->GetExactType(&pType));
        else
        {
            pType = pTypeCast;
            pType->AddRef();
        }
        IfFailRet(pType->GetClass(&pClass));
        IfFailRet(pClass->GetModule(&pModule));
        IfFailRet(pClass->GetToken(&currentTypeDef));

        ToRelease<IUnknown> pMDUnknown;
        ToRelease<IMetaDataImport> pMD;
        IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
        IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));

        WCHAR baseTypeName[mdNameLen] = W("\0");
        ToRelease<ICorDebugType> pBaseType;
        if(SUCCEEDED(pType->GetBase(&pBaseType)) && pBaseType != NULL && SUCCEEDED(GetTypeOfValue(pBaseType, baseTypeName, mdNameLen)))
        {
            if(_wcsncmp(baseTypeName, W("System.Enum"), 11) == 0)
                return S_OK;
            else if(_wcsncmp(baseTypeName, W("System.Object"), 13) != 0 && _wcsncmp(baseTypeName, W("System.ValueType"), 16) != 0)
            {
                currentExpansion[currentExpansionLen] = W('\0');
                wcscat_s(currentExpansion, currentExpansionSize, W(".\0"));
                wcscat_s(currentExpansion, currentExpansionSize, W("[basetype]"));
                for(int i = 0; i < indent; i++) ExtOut("    ");
                DMLOut(" |- %S %s\n", baseTypeName, DMLManagedVar(currentExpansion, currentFrame, W("[basetype]")));

                if(ShouldExpandVariable(varToExpand, currentExpansion))
                    ProcessFields(pInputValue, pBaseType, pILFrame, indent + 1, varToExpand, currentExpansion, currentExpansionSize, currentFrame);
            }
        }


        ULONG numFields = 0;
        HCORENUM fEnum = NULL;
        mdFieldDef fieldDef;
        while(SUCCEEDED(pMD->EnumFields(&fEnum, currentTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
        {
            ULONG             nameLen = 0;
            DWORD             fieldAttr = 0;
            WCHAR             mdName[mdNameLen];
            WCHAR             typeName[mdNameLen];
            if(SUCCEEDED(pMD->GetFieldProps(fieldDef, NULL, mdName, mdNameLen, &nameLen, &fieldAttr, NULL, NULL, NULL, NULL, NULL)))
            {
                currentExpansion[currentExpansionLen] = W('\0');
                wcscat_s(currentExpansion, currentExpansionSize, W(".\0"));
                wcscat_s(currentExpansion, currentExpansionSize, mdName);

                ToRelease<ICorDebugValue> pFieldVal;
                if(fieldAttr & fdLiteral)
                {
                    //TODO: Is it worth it??
                    //ExtOut(" |- const %S", mdName);
                }
                else
                {
                    for(int i = 0; i < indent; i++) ExtOut("    ");

                    if (fieldAttr & fdStatic)
                        pType->GetStaticFieldValue(fieldDef, pILFrame, &pFieldVal);
                    else
                    {
                        ToRelease<ICorDebugObjectValue> pObjValue;
                        if (SUCCEEDED(pValue->QueryInterface(IID_ICorDebugObjectValue, (LPVOID*) &pObjValue)))
                            pObjValue->GetFieldValue(pClass, fieldDef, &pFieldVal);
                    }

                    if(pFieldVal != NULL)
                    {
                        typeName[0] = L'\0';
                        GetTypeOfValue(pFieldVal, typeName, mdNameLen);
                        DMLOut(" |- %S %s", typeName, DMLManagedVar(currentExpansion, currentFrame, mdName));
                        PrintValue(pFieldVal, pILFrame, pMD, indent, varToExpand, currentExpansion, currentExpansionSize, currentFrame);
                    }
                    else if(!(fieldAttr & fdLiteral)) 
                        ExtOut(" |- < unknown type > %S\n", mdName);
                }
            }
        }
        pMD->CloseEnum(fEnum);
        return S_OK;
    }

public:

    // This is the main worker function used if !clrstack is called with "-i" to indicate
    // that the public ICorDebug* should be used instead of the private DAC interface. NOTE:
    // Currently only bParams is supported. NOTE: This is a work in progress and the
    // following would be good to do:
    //     * More thorough testing with interesting stacks, especially with transitions into
    //         and out of managed code.
    //     * Consider interleaving this code back into the main body of !clrstack if it turns
    //         out that there's a lot of duplication of code between these two functions.
    //         (Still unclear how things will look once locals is implemented.)
    static HRESULT ClrStackFromPublicInterface(BOOL bParams, BOOL bLocals, BOOL bSuppressLines, __in_z WCHAR* varToExpand = NULL, int onlyShowFrame = -1)
    {
        HRESULT Status;

        IfFailRet(InitCorDebugInterface());

        ExtOut("\n\n\nDumping managed stack and managed variables using ICorDebug.\n");
        ExtOut("=============================================================================\n");

        ToRelease<ICorDebugThread> pThread;
        ToRelease<ICorDebugThread3> pThread3;
        ToRelease<ICorDebugStackWalk> pStackWalk;
        ULONG ulThreadID = 0;
        g_ExtSystem->GetCurrentThreadSystemId(&ulThreadID);

        IfFailRet(g_pCorDebugProcess->GetThread(ulThreadID, &pThread));
        IfFailRet(pThread->QueryInterface(IID_ICorDebugThread3, (LPVOID *) &pThread3));
        IfFailRet(pThread3->CreateStackWalk(&pStackWalk));

        InternalFrameManager internalFrameManager;
        IfFailRet(internalFrameManager.Init(pThread3));
        
    #if defined(_AMD64_) || defined(_ARM64_)
        ExtOut("%-16s %-16s %s\n", "Child SP", "IP", "Call Site");
    #elif defined(_X86_) || defined(_ARM_)
        ExtOut("%-8s %-8s %s\n", "Child SP", "IP", "Call Site");
    #endif

        int currentFrame = -1;

        for (Status = S_OK; ; Status = pStackWalk->Next())
        {
            currentFrame++;

            if (Status == CORDBG_S_AT_END_OF_STACK)
            {
                ExtOut("Stack walk complete.\n");
                break;
            }
            IfFailRet(Status);

            if (IsInterrupt())
            {
                ExtOut("<interrupted>\n");
                break;
            }
            
            CROSS_PLATFORM_CONTEXT context;
            ULONG32 cbContextActual;
            if ((Status=pStackWalk->GetContext(
                DT_CONTEXT_FULL, 
                sizeof(context),
                &cbContextActual,
                (BYTE *)&context))!=S_OK)
            {
                ExtOut("GetFrameContext failed: %lx\n",Status);
                break;
            }

            // First find the info for the Frame object, if the current frame has an associated clr!Frame.
            CLRDATA_ADDRESS sp = GetSP(context);
            CLRDATA_ADDRESS ip = GetIP(context);

            ToRelease<ICorDebugFrame> pFrame;
            IfFailRet(pStackWalk->GetFrame(&pFrame));
            if (Status == S_FALSE)
            {
                DMLOut("%p %s [NativeStackFrame]\n", SOS_PTR(sp), DMLIP(ip));
                continue;
            }

            // TODO: What about internal frames preceding the above native stack frame? 
            // Should I just exclude the above native stack frame from the output?
            // TODO: Compare caller frame (instead of current frame) against internal frame,
            // to deal with issues of current frame's current SP being closer to leaf than
            // EE Frames it pushes.  By "caller" I mean not just managed caller, but the
            // very next non-internal frame dbi would return (native or managed). OR...
            // perhaps I should use GetStackRange() instead, to see if the internal frame
            // appears leafier than the base-part of the range of the currently iterated
            // stack frame?  I think I like that better.
            _ASSERTE(pFrame != NULL);
            IfFailRet(internalFrameManager.PrintPrecedingInternalFrames(pFrame));

            // Print the stack and instruction pointers.
            DMLOut("%p %s ", SOS_PTR(sp), DMLIP(ip));

            ToRelease<ICorDebugRuntimeUnwindableFrame> pRuntimeUnwindableFrame;
            Status = pFrame->QueryInterface(IID_ICorDebugRuntimeUnwindableFrame, (LPVOID *) &pRuntimeUnwindableFrame);
            if (SUCCEEDED(Status))
            {
                ExtOut("[RuntimeUnwindableFrame]\n");
                continue;
            }

            // Print the method/Frame info

            // TODO: IS THE FOLLOWING NECESSARY, OR AM I GUARANTEED THAT ALL INTERNAL FRAMES
            // CAN BE FOUND VIA GetActiveInternalFrames?
            ToRelease<ICorDebugInternalFrame> pInternalFrame;
            Status = pFrame->QueryInterface(IID_ICorDebugInternalFrame, (LPVOID *) &pInternalFrame);
            if (SUCCEEDED(Status))
            {
                // This is a clr!Frame.
                LPCWSTR pwszFrameName = W("TODO: Implement GetFrameName");
                ExtOut("[%S: p] ", pwszFrameName);
            }

            // Print the frame's associated function info, if it has any.
            ToRelease<ICorDebugILFrame> pILFrame;
            HRESULT hrILFrame = pFrame->QueryInterface(IID_ICorDebugILFrame, (LPVOID*) &pILFrame);

            if (SUCCEEDED(hrILFrame))
            {
                ToRelease<ICorDebugFunction> pFunction;
                Status = pFrame->GetFunction(&pFunction);
                if (FAILED(Status))
                {
                    // We're on a JITted frame, but there's no Function for it.  So it must
                    // be... 
                    ExtOut("[IL Stub or LCG]\n");
                    continue;
                }

                ToRelease<ICorDebugClass> pClass;
                ToRelease<ICorDebugModule> pModule;
                mdMethodDef methodDef;
                IfFailRet(pFunction->GetClass(&pClass));
                IfFailRet(pFunction->GetModule(&pModule));
                IfFailRet(pFunction->GetToken(&methodDef));

                WCHAR wszModuleName[100];
                ULONG32 cchModuleNameActual;
                IfFailRet(pModule->GetName(_countof(wszModuleName), &cchModuleNameActual, wszModuleName));

                ToRelease<IUnknown> pMDUnknown;
                ToRelease<IMetaDataImport> pMD;
                ToRelease<IMDInternalImport> pMDInternal;
                IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
                IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));
                IfFailRet(GetMDInternalFromImport(pMD, &pMDInternal));

                mdTypeDef typeDef;
                IfFailRet(pClass->GetToken(&typeDef));

                // Note that we don't need to pretty print the class, as class name is
                // already printed from GetMethodName below

                CQuickBytes functionName;
                // TODO: WARNING: GetMethodName() appears to include lots of unexercised
                // code, as evidenced by some fundamental bugs I found.  It should either be
                // thoroughly reviewed, or some other more exercised code path to grab the
                // name should be used.
                // TODO: If we do stay with GetMethodName, it should be updated to print
                // generics properly.  Today, it does not show generic type parameters, and
                // if any arguments have a generic type, those arguments are just shown as
                // "__Canon", even when they're value types.
                GetMethodName(methodDef, pMD, &functionName);

                DMLOut(DMLManagedVar(W("-a"), currentFrame, (LPWSTR)functionName.Ptr()));
                ExtOut(" (%S)\n", wszModuleName);

                if (SUCCEEDED(hrILFrame) && (bParams || bLocals))
                {
                    if(onlyShowFrame == -1 || (onlyShowFrame >= 0 && currentFrame == onlyShowFrame))
                        IfFailRet(PrintParameters(bParams, bLocals, pMD, typeDef, methodDef, pILFrame, pModule, varToExpand, currentFrame));
                }
            }
        }
        ExtOut("=============================================================================\n");

#ifdef FEATURE_PAL
        // Temporary until we get a process exit notification plumbed from lldb
        UninitCorDebugInterface();
#endif
        return S_OK;
    }
};

WString BuildRegisterOutput(const SOSStackRefData &ref, bool printObj)
{
    WString res;
    
    if (ref.HasRegisterInformation)
    {
        WCHAR reg[32];
        HRESULT hr = g_sos->GetRegisterName(ref.Register, _countof(reg), reg, NULL);
        if (SUCCEEDED(hr))
            res = reg;
        else
            res = W("<unknown register>");
            
        if (ref.Offset)
        {
            int offset = ref.Offset;
            if (offset > 0)
            {
                res += W("+");
            }
            else
            {
                res += W("-");
                offset = -offset;
            }
            
            res += Hex(offset);
        }
        
        res += W(": ");
    }
    
    if (ref.Address)
        res += WString(Pointer(ref.Address));
        
    if (printObj)
    {
        if (ref.Address)
            res += W(" -> ");

        res += WString(ObjectPtr(ref.Object));
    }

    if (ref.Flags & SOSRefPinned)
    {
        res += W(" (pinned)");
    }
    
    if (ref.Flags & SOSRefInterior)
    {
        res += W(" (interior)");
    }
    
    return res;
}

void PrintRef(const SOSStackRefData &ref, TableOutput &out)
{
    WString res = BuildRegisterOutput(ref);
    
    if (ref.Object && (ref.Flags & SOSRefInterior) == 0)
    {
        WCHAR type[128];
        sos::BuildTypeWithExtraInfo(TO_TADDR(ref.Object), _countof(type), type);
        
        res += WString(W(" - ")) + type;
    }
    
    out.WriteColumn(2, res);
}


class ClrStackImpl
{
public:
    static void PrintThread(ULONG osID, BOOL bParams, BOOL bLocals, BOOL bSuppressLines, BOOL bGC, BOOL bFull, BOOL bDisplayRegVals)
    {
        // Symbols variables
        ULONG symlines = 0; // symlines will be non-zero only if SYMOPT_LOAD_LINES was set in the symbol options
        if (!bSuppressLines && SUCCEEDED(g_ExtSymbols->GetSymbolOptions(&symlines)))
        {
            symlines &= SYMOPT_LOAD_LINES;
        }
        
        if (symlines == 0)
            bSuppressLines = TRUE;
        
        ToRelease<IXCLRDataStackWalk> pStackWalk;
        
        HRESULT hr = CreateStackWalk(osID, &pStackWalk);
        if (FAILED(hr) || pStackWalk == NULL)
        {
            ExtOut("Failed to start stack walk: %lx\n", hr);
            return;
        }

#ifdef DEBUG_STACK_CONTEXT
        PDEBUG_STACK_FRAME currentNativeFrame = NULL;
        ULONG numNativeFrames = 0;
        if (bFull)
        {
            hr = GetContextStackTrace(osID, &numNativeFrames);
            if (FAILED(hr))
            {
                ExtOut("Failed to get native stack frames: %lx\n", hr);
                return;
            }
            currentNativeFrame = &g_Frames[0];
        }
#endif // DEBUG_STACK_CONTEXT
        
        unsigned int refCount = 0, errCount = 0;
        ArrayHolder<SOSStackRefData> pRefs = NULL;
        ArrayHolder<SOSStackRefError> pErrs = NULL;
        if (bGC && FAILED(GetGCRefs(osID, &pRefs, &refCount, &pErrs, &errCount)))
            refCount = 0;
            
        TableOutput out(3, POINTERSIZE_HEX, AlignRight);
        out.WriteRow("Child SP", "IP", "Call Site");
                
        do
        {
            if (IsInterrupt())
            {
                ExtOut("<interrupted>\n");
                break;
            }
            CLRDATA_ADDRESS ip = 0, sp = 0;
            hr = GetFrameLocation(pStackWalk, &ip, &sp);

            DacpFrameData FrameData;
            HRESULT frameDataResult = FrameData.Request(pStackWalk);
            if (SUCCEEDED(frameDataResult) && FrameData.frameAddr)
                sp = FrameData.frameAddr;

#ifdef DEBUG_STACK_CONTEXT
            while ((numNativeFrames > 0) && (currentNativeFrame->StackOffset <= sp))
            {
                if (currentNativeFrame->StackOffset != sp)
                {
                    PrintNativeStackFrame(out, currentNativeFrame, bSuppressLines);
                }
                currentNativeFrame++;
                numNativeFrames--;
            }
#endif // DEBUG_STACK_CONTEXT

            // Print the stack pointer.
            out.WriteColumn(0, sp);

            // Print the method/Frame info
            if (SUCCEEDED(frameDataResult) && FrameData.frameAddr)
            {
                // Skip the instruction pointer because it doesn't really mean anything for method frames
                out.WriteColumn(1, bFull ? String("") : NativePtr(ip));
                
                // This is a clr!Frame.
                out.WriteColumn(2, GetFrameFromAddress(TO_TADDR(FrameData.frameAddr), pStackWalk, bFull));
            
                // Print out gc references for the Frame.  
                for (unsigned int i = 0; i < refCount; ++i)
                    if (pRefs[i].Source == sp)
                        PrintRef(pRefs[i], out);
                        
                // Print out an error message if we got one.
                for (unsigned int i = 0; i < errCount; ++i)
                    if (pErrs[i].Source == sp)
                        out.WriteColumn(2, "Failed to enumerate GC references.");
            }
            else
            {
                out.WriteColumn(1, InstructionPtr(ip));
                out.WriteColumn(2, MethodNameFromIP(ip, bSuppressLines, bFull, bFull));
                    
                // Print out gc references.  refCount will be zero if bGC is false (or if we
                // failed to fetch gc reference information).
                for (unsigned int i = 0; i < refCount; ++i)
                    if (pRefs[i].Source == ip && pRefs[i].StackPointer == sp)
                        PrintRef(pRefs[i], out);

                // Print out an error message if we got one.
                for (unsigned int i = 0; i < errCount; ++i)
                    if (pErrs[i].Source == sp)
                        out.WriteColumn(2, "Failed to enumerate GC references.");

                if (bParams || bLocals)
                    PrintArgsAndLocals(pStackWalk, bParams, bLocals);
            }

            if (bDisplayRegVals)
                PrintManagedFrameContext(pStackWalk);

        } while (pStackWalk->Next() == S_OK);

#ifdef DEBUG_STACK_CONTEXT
        while (numNativeFrames > 0)
        {
            PrintNativeStackFrame(out, currentNativeFrame, bSuppressLines);
            currentNativeFrame++;
            numNativeFrames--;
        }
#endif // DEBUG_STACK_CONTEXT
    }
    
    static HRESULT PrintManagedFrameContext(IXCLRDataStackWalk *pStackWalk)
    {
        CROSS_PLATFORM_CONTEXT context;
        HRESULT hr = pStackWalk->GetContext(DT_CONTEXT_FULL, g_targetMachine->GetContextSize(), NULL, (BYTE *)&context);
        if (FAILED(hr) || hr == S_FALSE)
        {
            // GetFrameContext returns S_FALSE if the frame iterator is invalid.  That's basically an error for us.
            ExtOut("GetFrameContext failed: %lx\n", hr);
            return E_FAIL;
        }
                     
#if defined(SOS_TARGET_AMD64)
        String outputFormat3 = "    %3s=%016x %3s=%016x %3s=%016x\n";
        String outputFormat2 = "    %3s=%016x %3s=%016x\n";
        ExtOut(outputFormat3, "rsp", context.Amd64Context.Rsp, "rbp", context.Amd64Context.Rbp, "rip", context.Amd64Context.Rip);
        ExtOut(outputFormat3, "rax", context.Amd64Context.Rax, "rbx", context.Amd64Context.Rbx, "rcx", context.Amd64Context.Rcx);
        ExtOut(outputFormat3, "rdx", context.Amd64Context.Rdx, "rsi", context.Amd64Context.Rsi, "rdi", context.Amd64Context.Rdi);
        ExtOut(outputFormat3, "r8", context.Amd64Context.R8, "r9", context.Amd64Context.R9, "r10", context.Amd64Context.R10);
        ExtOut(outputFormat3, "r11", context.Amd64Context.R11, "r12", context.Amd64Context.R12, "r13", context.Amd64Context.R13);
        ExtOut(outputFormat2, "r14", context.Amd64Context.R14, "r15", context.Amd64Context.R15);
#elif defined(SOS_TARGET_X86)
        String outputFormat3 = "    %3s=%08x %3s=%08x %3s=%08x\n";
        String outputFormat2 = "    %3s=%08x %3s=%08x\n";
        ExtOut(outputFormat3, "esp", context.X86Context.Esp, "ebp", context.X86Context.Ebp, "eip", context.X86Context.Eip);
        ExtOut(outputFormat3, "eax", context.X86Context.Eax, "ebx", context.X86Context.Ebx, "ecx", context.X86Context.Ecx);      
        ExtOut(outputFormat3, "edx", context.X86Context.Edx, "esi", context.X86Context.Esi, "edi", context.X86Context.Edi);
#elif defined(SOS_TARGET_ARM)
        String outputFormat3 = "    %3s=%08x %3s=%08x %3s=%08x\n";
        String outputFormat2 = "    %s=%08x %s=%08x\n";
        String outputFormat1 = "    %s=%08x\n";
        ExtOut(outputFormat3, "r0", context.ArmContext.R0, "r1", context.ArmContext.R1, "r2", context.ArmContext.R2);
        ExtOut(outputFormat3, "r3", context.ArmContext.R3, "r4", context.ArmContext.R4, "r5", context.ArmContext.R5);
        ExtOut(outputFormat3, "r6", context.ArmContext.R6, "r7", context.ArmContext.R7, "r8", context.ArmContext.R8);
        ExtOut(outputFormat3, "r9", context.ArmContext.R9, "r10", context.ArmContext.R10, "r11", context.ArmContext.R11);
        ExtOut(outputFormat1, "r12", context.ArmContext.R12);
        ExtOut(outputFormat3, "sp", context.ArmContext.Sp, "lr", context.ArmContext.Lr, "pc", context.ArmContext.Pc);
        ExtOut(outputFormat2, "cpsr", context.ArmContext.Cpsr, "fpsr", context.ArmContext.Fpscr);
#elif defined(SOS_TARGET_ARM64)
        String outputXRegFormat3 = "    x%d=%016x x%d=%016x x%d=%016x\n";
        String outputXRegFormat1 = "    x%d=%016x\n";
        String outputFormat3     = "    %s=%016x %s=%016x %s=%016x\n";
        String outputFormat2     = "    %s=%08x %s=%08x\n";
        DWORD64 *X = context.Arm64Context.X;
        for (int i = 0; i < 9; i++)
        {
            ExtOut(outputXRegFormat3, i + 0, X[i + 0], i + 1, X[i + 1], i + 2, X[i + 2]);
        }
        ExtOut(outputXRegFormat1, 28, X[28]);
        ExtOut(outputFormat3, "sp", context.ArmContext.Sp, "lr", context.ArmContext.Lr, "pc", context.ArmContext.Pc);
        ExtOut(outputFormat2, "cpsr", context.ArmContext.Cpsr, "fpsr", context.ArmContext.Fpscr);
#else
        ExtOut("Can't display register values for this platform\n");
#endif
        return S_OK;

    }

    static HRESULT GetFrameLocation(IXCLRDataStackWalk *pStackWalk, CLRDATA_ADDRESS *ip, CLRDATA_ADDRESS *sp)
    {
        CROSS_PLATFORM_CONTEXT context;
        HRESULT hr = pStackWalk->GetContext(DT_CONTEXT_FULL, g_targetMachine->GetContextSize(), NULL, (BYTE *)&context);
        if (FAILED(hr) || hr == S_FALSE)
        {
            // GetFrameContext returns S_FALSE if the frame iterator is invalid.  That's basically an error for us.
            ExtOut("GetFrameContext failed: %lx\n", hr);
            return E_FAIL;
        }

        // First find the info for the Frame object, if the current frame has an associated clr!Frame.
        *ip = GetIP(context);
        *sp = GetSP(context);
        
        if (IsDbgTargetArm())
            *ip = *ip & ~THUMB_CODE;
        
        return S_OK;
    }
    
    static void PrintNativeStackFrame(TableOutput out, PDEBUG_STACK_FRAME frame, BOOL bSuppressLines)
    {
        char filename[MAX_LONGPATH + 1];
        char symbol[1024];
        ULONG64 displacement;

        ULONG64 ip = frame->InstructionOffset;

        out.WriteColumn(0, frame->StackOffset);
        out.WriteColumn(1, NativePtr(ip));

        HRESULT hr = g_ExtSymbols->GetNameByOffset(TO_CDADDR(ip), symbol, _countof(symbol), NULL, &displacement);
        if (SUCCEEDED(hr) && symbol[0] != '\0')
        {
            String frameOutput;
            frameOutput += symbol;

            if (displacement)
            {
                frameOutput += " + ";
                frameOutput += Decimal(displacement);
            }

            if (!bSuppressLines)
            {
                ULONG line;
                hr = g_ExtSymbols->GetLineByOffset(TO_CDADDR(ip), &line, filename, _countof(filename), NULL, NULL);
                if (SUCCEEDED(hr))
                {
                    frameOutput += " at ";
                    frameOutput += filename;
                    frameOutput += ":";
                    frameOutput += Decimal(line);
                }
            }

            out.WriteColumn(2, frameOutput);
        }
        else
        {
            out.WriteColumn(2, "");
        }
    }

    static void PrintCurrentThread(BOOL bParams, BOOL bLocals, BOOL bSuppressLines, BOOL bGC, BOOL bNative, BOOL bDisplayRegVals)
    {
        ULONG id = 0;
        ULONG osid = 0;
        
        g_ExtSystem->GetCurrentThreadSystemId(&osid);
        ExtOut("OS Thread Id: 0x%x ", osid);
        g_ExtSystem->GetCurrentThreadId(&id);
        ExtOut("(%d)\n", id);
        
        PrintThread(osid, bParams, bLocals, bSuppressLines, bGC, bNative, bDisplayRegVals);
    }

    static void PrintAllThreads(BOOL bParams, BOOL bLocals, BOOL bSuppressLines, BOOL bGC, BOOL bNative, BOOL bDisplayRegVals)
    {
        HRESULT Status;

        DacpThreadStoreData ThreadStore;
        if ((Status = ThreadStore.Request(g_sos)) != S_OK)
        {
            ExtErr("Failed to request ThreadStore\n");
            return;
        }

        DacpThreadData Thread;
        CLRDATA_ADDRESS CurThread = ThreadStore.firstThread;
        while (CurThread != 0)
        {
            if (IsInterrupt())
                break;

            if ((Status = Thread.Request(g_sos, CurThread)) != S_OK)
            {
                ExtErr("Failed to request thread at %p\n", CurThread);
                return;
            }
            ExtOut("OS Thread Id: 0x%x\n", Thread.osThreadId);
            PrintThread(Thread.osThreadId, bParams, bLocals, bSuppressLines, bGC, bNative, bDisplayRegVals);
            CurThread = Thread.nextThread;
        }
    }

private: 
    static HRESULT CreateStackWalk(ULONG osID, IXCLRDataStackWalk **ppStackwalk)
    {
        HRESULT hr = S_OK;
        ToRelease<IXCLRDataTask> pTask;

        if ((hr = g_clrData->GetTaskByOSThreadID(osID, &pTask)) != S_OK)
        {
            ExtOut("Unable to walk the managed stack. The current thread is likely not a \n");
            ExtOut("managed thread. You can run " SOSThreads " to get a list of managed threads in\n");
            ExtOut("the process\n");
            return hr;
        }

        return pTask->CreateStackWalk(CLRDATA_SIMPFRAME_UNRECOGNIZED |
                                      CLRDATA_SIMPFRAME_MANAGED_METHOD |
                                      CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                                      CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                                      ppStackwalk);
    }

    /* Prints the args and locals of for a thread's stack.
     * Params:
     *      pStackWalk - the stack we are printing
     *      bArgs - whether to print args
     *      bLocals - whether to print locals
     */
    static void PrintArgsAndLocals(IXCLRDataStackWalk *pStackWalk, BOOL bArgs, BOOL bLocals)
    {
        ToRelease<IXCLRDataFrame> pFrame;
        ToRelease<IXCLRDataValue> pVal;
        ULONG32 argCount = 0;
        ULONG32 localCount = 0;
        HRESULT hr = S_OK;
        
        hr = pStackWalk->GetFrame(&pFrame);
        
        // Print arguments
        if (SUCCEEDED(hr) && bArgs)
            hr = pFrame->GetNumArguments(&argCount);
                
        if (SUCCEEDED(hr) && bArgs)
            hr = ShowArgs(argCount, pFrame, pVal);
        
        // Print locals
        if (SUCCEEDED(hr) && bLocals)
            hr = pFrame->GetNumLocalVariables(&localCount);
        
        if (SUCCEEDED(hr) && bLocals)
            ShowLocals(localCount, pFrame, pVal);
            
        ExtOut("\n");
    }
    
    

    /* Displays the arguments to a function
     * Params:
     *      argy - the number of arguments the function has
     *      pFramey - the frame we are inspecting
     *      pVal - a pointer to the CLRDataValue we use to query for info about the args
     */
    static HRESULT ShowArgs(ULONG32 argy, IXCLRDataFrame *pFramey, IXCLRDataValue *pVal)
    {
        CLRDATA_ADDRESS addr = 0;
        BOOL fPrintedLocation = FALSE;
        ULONG64 outVar = 0;
        ULONG32 tmp;
        HRESULT hr = S_OK;
        
        ArrayHolder<WCHAR> argName = new NOTHROW WCHAR[mdNameLen];
        if (!argName)
        {
            ReportOOM();
            return E_FAIL;
        }
        
        for (ULONG32 i=0; i < argy; i++)
        {   
            if (i == 0)
            {      
                ExtOut("    PARAMETERS:\n");
            }
            
            hr = pFramey->GetArgumentByIndex(i,
                                   &pVal,
                                   mdNameLen,
                                   &tmp,
                                   argName);
            
            if (FAILED(hr))
                return hr;

            ExtOut("        ");
            
            if (argName[0] != L'\0')
            {
                ExtOut("%S ", argName.GetPtr());
            }
            
            // At times we cannot print the value of a parameter (most
            // common case being a non-primitive value type).  In these 
            // cases we need to print the location of the parameter, 
            // so that we can later examine it (e.g. using !dumpvc)
            {
                bool result = SUCCEEDED(pVal->GetNumLocations(&tmp)) && tmp == 1;
                if (result)
                    result = SUCCEEDED(pVal->GetLocationByIndex(0, &tmp, &addr));
                
                if (result)
                {
                    if (tmp == CLRDATA_VLOC_REGISTER)
                    {
                        ExtOut("(<CLR reg>) ");
                    }
                    else
                    {
                        ExtOut("(0x%p) ", SOS_PTR(CDA_TO_UL64(addr)));
                    }
                    fPrintedLocation = TRUE;
                }
            }

            if (argName[0] != L'\0' || fPrintedLocation)
            {
                ExtOut("= ");                
            }
            
            if (HRESULT_CODE(pVal->GetBytes(0,&tmp,NULL)) == ERROR_BUFFER_OVERFLOW)
            {
                ArrayHolder<BYTE> pByte = new NOTHROW BYTE[tmp + 1];
                if (pByte == NULL)
                {
                    ReportOOM();
                    return E_FAIL;
                }
                
                hr = pVal->GetBytes(tmp, &tmp, pByte);
                
                if (FAILED(hr))
                {
                    ExtOut("<unable to retrieve data>\n");
                }
                else
                {
                    switch(tmp)
                    {
                        case 1: outVar = *((BYTE *)pByte.GetPtr()); break;
                        case 2: outVar = *((short *)pByte.GetPtr()); break;
                        case 4: outVar = *((DWORD *)pByte.GetPtr()); break;
                        case 8: outVar = *((ULONG64 *)pByte.GetPtr()); break;
                        default: outVar = 0;
                    }

                    if (outVar)
                        DMLOut("0x%s\n", DMLObject(outVar));
                    else
                        ExtOut("0x%p\n", SOS_PTR(outVar));
                }
                
            }
            else
            {
                ExtOut("<no data>\n");
            }
            
            pVal->Release();
        }
        
        return S_OK;
    }


    /* Prints the locals of a frame.
     * Params:
     *      localy - the number of locals in the frame
     *      pFramey - the frame we are inspecting
     *      pVal - a pointer to the CLRDataValue we use to query for info about the args
     */
    static HRESULT ShowLocals(ULONG32 localy, IXCLRDataFrame *pFramey, IXCLRDataValue *pVal)
    {
        for (ULONG32 i=0; i < localy; i++)
        {   
            if (i == 0)
                ExtOut("    LOCALS:\n");
            
            HRESULT hr;
            ExtOut("        ");
            
            // local names don't work in Whidbey.
            hr = pFramey->GetLocalVariableByIndex(i, &pVal, mdNameLen, NULL, g_mdName);
            if (FAILED(hr))
            {
                return hr;
            }

            ULONG32 numLocations;
            if (SUCCEEDED(pVal->GetNumLocations(&numLocations)) &&
                numLocations == 1)
            {
                ULONG32 flags;
                CLRDATA_ADDRESS addr;
                if (SUCCEEDED(pVal->GetLocationByIndex(0, &flags, &addr)))
                {
                    if (flags == CLRDATA_VLOC_REGISTER)
                    {
                        ExtOut("<CLR reg> ");
                    }
                    else
                    {
                        ExtOut("0x%p ", SOS_PTR(CDA_TO_UL64(addr)));
                    }
                }

                // Can I get a name for the item?

                ExtOut("= ");                
            }
            ULONG32 dwSize = 0;
            hr = pVal->GetBytes(0, &dwSize, NULL);
            
            if (HRESULT_CODE(hr) == ERROR_BUFFER_OVERFLOW)
            {
                ArrayHolder<BYTE> pByte = new NOTHROW BYTE[dwSize + 1];
                if (pByte == NULL)
                {
                    ReportOOM();
                    return E_FAIL;
                }

                hr = pVal->GetBytes(dwSize,&dwSize,pByte);

                if (FAILED(hr))
                {
                    ExtOut("<unable to retrieve data>\n");
                }
                else
                {
                    ULONG64 outVar = 0;
                    switch(dwSize)
                    {
                        case 1: outVar = *((BYTE *) pByte.GetPtr()); break;
                        case 2: outVar = *((short *) pByte.GetPtr()); break;
                        case 4: outVar = *((DWORD *) pByte.GetPtr()); break;
                        case 8: outVar = *((ULONG64 *) pByte.GetPtr()); break;
                        default: outVar = 0;
                    }

                    if (outVar)
                        DMLOut("0x%s\n", DMLObject(outVar));
                    else
                        ExtOut("0x%p\n", SOS_PTR(outVar));
                }
            }
            else
            {
                ExtOut("<no data>\n");
            }
            
            pVal->Release();
        }
        
        return S_OK;
    }

};

#ifndef FEATURE_PAL

WatchCmd g_watchCmd;

// The grand new !Watch command, private to Apollo for now
DECLARE_API(Watch)
{
    INIT_API_NOEE();
    BOOL bExpression = FALSE;
    StringHolder addExpression;
    StringHolder aExpression;
    StringHolder saveName;
    StringHolder sName;
    StringHolder expression;
    StringHolder filterName;
    StringHolder renameOldName;
    size_t expandIndex = -1;
    size_t removeIndex = -1;
    BOOL clear = FALSE;

    size_t nArg = 0;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-add", &addExpression.data, COSTRING, TRUE},
        {"-a", &aExpression.data, COSTRING, TRUE},
        {"-save", &saveName.data, COSTRING, TRUE},
        {"-s", &sName.data, COSTRING, TRUE},
        {"-clear", &clear, COBOOL, FALSE},
        {"-c", &clear, COBOOL, FALSE},
        {"-expand", &expandIndex, COSIZE_T, TRUE},
        {"-filter", &filterName.data, COSTRING, TRUE},
        {"-r", &removeIndex, COSIZE_T, TRUE},
        {"-remove", &removeIndex, COSIZE_T, TRUE},
        {"-rename", &renameOldName.data, COSTRING, TRUE},
    };

    CMDValue arg[] = 
    {   // vptr, type
        {&expression.data, COSTRING}
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }

    if(addExpression.data != NULL || aExpression.data != NULL)
    {
        WCHAR pAddExpression[MAX_EXPRESSION];
        swprintf_s(pAddExpression, MAX_EXPRESSION, W("%S"), addExpression.data != NULL ? addExpression.data : aExpression.data);
        Status = g_watchCmd.Add(pAddExpression);
    }
    else if(removeIndex != -1)
    {
        if(removeIndex <= 0)
        {
            ExtOut("Index must be a postive decimal number\n");
        }
        else
        {
            Status = g_watchCmd.Remove((int)removeIndex);
            if(Status == S_OK)
                ExtOut("Watch expression #%d has been removed\n", removeIndex);
            else if(Status == S_FALSE)
                ExtOut("There is no watch expression with index %d\n", removeIndex);
            else
                ExtOut("Unknown failure 0x%x removing watch expression\n", Status);
        }
    }
    else if(saveName.data != NULL || sName.data != NULL)
    {
        WCHAR pSaveName[MAX_EXPRESSION];
        swprintf_s(pSaveName, MAX_EXPRESSION, W("%S"), saveName.data != NULL ? saveName.data : sName.data);
        Status = g_watchCmd.SaveList(pSaveName);
    }
    else if(clear)
    {
        g_watchCmd.Clear();
    }
    else if(renameOldName.data != NULL)
    {
        if(nArg != 1)
        {
             ExtOut("Must provide an old and new name. Usage: !watch -rename <old_name> <new_name>.\n");
             return S_FALSE;
        }
        WCHAR pOldName[MAX_EXPRESSION];
        swprintf_s(pOldName, MAX_EXPRESSION, W("%S"), renameOldName.data);
        WCHAR pNewName[MAX_EXPRESSION];
        swprintf_s(pNewName, MAX_EXPRESSION, W("%S"), expression.data);
        g_watchCmd.RenameList(pOldName, pNewName);
    }
    // print the tree, possibly with filtering and/or expansion
    else if(expandIndex != -1 || expression.data == NULL)
    {
        WCHAR pExpression[MAX_EXPRESSION];
        pExpression[0] = '\0';

        if(expandIndex != -1)
        {
            if(expression.data != NULL)
            {
                swprintf_s(pExpression, MAX_EXPRESSION, W("%S"), expression.data);
            }
            else
            {
                ExtOut("No expression was provided. Usage !watch -expand <index> <expression>\n");
                return S_FALSE;
            }
        }
        WCHAR pFilterName[MAX_EXPRESSION];
        pFilterName[0] = '\0';

        if(filterName.data != NULL)
        {
            swprintf_s(pFilterName, MAX_EXPRESSION, W("%S"), filterName.data);
        }

        g_watchCmd.Print((int)expandIndex, pExpression, pFilterName);
    }
    else
    {
        ExtOut("Unrecognized argument: %s\n", expression.data);
    }

    return Status;
}

#endif // FEATURE_PAL

DECLARE_API(ClrStack)
{
    INIT_API();

    BOOL bAll = FALSE;    
    BOOL bParams = FALSE;
    BOOL bLocals = FALSE;
    BOOL bSuppressLines = FALSE;
    BOOL bICorDebug = FALSE;
    BOOL bGC = FALSE;
    BOOL dml = FALSE;
    BOOL bFull = FALSE;
    BOOL bDisplayRegVals = FALSE;
    BOOL bAllThreads = FALSE;    
    DWORD frameToDumpVariablesFor = -1;
    StringHolder cvariableName;
    ArrayHolder<WCHAR> wvariableName = new NOTHROW WCHAR[mdNameLen];
    if (wvariableName == NULL)
    {
        ReportOOM();
        return E_OUTOFMEMORY;
    }

    memset(wvariableName, 0, sizeof(wvariableName));

    size_t nArg = 0;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-a", &bAll, COBOOL, FALSE},
        {"-all", &bAllThreads, COBOOL, FALSE},
        {"-p", &bParams, COBOOL, FALSE},
        {"-l", &bLocals, COBOOL, FALSE},
        {"-n", &bSuppressLines, COBOOL, FALSE},
        {"-i", &bICorDebug, COBOOL, FALSE},
        {"-gc", &bGC, COBOOL, FALSE},
        {"-f", &bFull, COBOOL, FALSE},
        {"-r", &bDisplayRegVals, COBOOL, FALSE },
#ifndef FEATURE_PAL
        {"/d", &dml, COBOOL, FALSE},
#endif
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&cvariableName.data, COSTRING},
        {&frameToDumpVariablesFor, COSIZE_T},
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return Status;
    }

    EnableDMLHolder dmlHolder(dml);
    if (bAll || bParams || bLocals)
    {
        // No parameter or local supports for minidump case!
        MINIDUMP_NOT_SUPPORTED();        
    }

    if (bAll)
    {
        bParams = bLocals = TRUE;
    }

    if (bICorDebug)
    {
        if(nArg > 0)
        {
            bool firstParamIsNumber = true;
            for(DWORD i = 0; i < strlen(cvariableName.data); i++)
                firstParamIsNumber = firstParamIsNumber && isdigit(cvariableName.data[i]);

            if(firstParamIsNumber && nArg == 1)
            {
                frameToDumpVariablesFor = (DWORD)GetExpression(cvariableName.data);
                cvariableName.data[0] = '\0';
            }
        }
        if(cvariableName.data != NULL && strlen(cvariableName.data) > 0)
            swprintf_s(wvariableName, mdNameLen, W("%S\0"), cvariableName.data);
        
        if(_wcslen(wvariableName) > 0)
            bParams = bLocals = TRUE;

        EnableDMLHolder dmlHolder(TRUE);
        return ClrStackImplWithICorDebug::ClrStackFromPublicInterface(bParams, bLocals, FALSE, wvariableName, frameToDumpVariablesFor);
    }
    
    if (bAllThreads) {
        ClrStackImpl::PrintAllThreads(bParams, bLocals, bSuppressLines, bGC, bFull, bDisplayRegVals);
    }
    else {
        ClrStackImpl::PrintCurrentThread(bParams, bLocals, bSuppressLines, bGC, bFull, bDisplayRegVals);
    }
    
    return S_OK;
}

#ifndef FEATURE_PAL

BOOL IsMemoryInfoAvailable()
{
    ULONG Class;
    ULONG Qualifier;
    g_ExtControl->GetDebuggeeType(&Class,&Qualifier);
    if (Qualifier == DEBUG_DUMP_SMALL) 
    {
        g_ExtControl->GetDumpFormatFlags(&Qualifier);
        if ((Qualifier & DEBUG_FORMAT_USER_SMALL_FULL_MEMORY) == 0)            
        {
            if ((Qualifier & DEBUG_FORMAT_USER_SMALL_FULL_MEMORY_INFO) == 0)
            {
                return FALSE;
            }            
        }
    }        
    return TRUE;
}

DECLARE_API( VMMap )
{
    INIT_API();

    if (IsMiniDumpFile() || !IsMemoryInfoAvailable())
    {
        ExtOut("!VMMap requires a full memory dump (.dump /ma) or a live process.\n");
    }
    else
    {
        vmmap();
    }

    return Status;
}   // DECLARE_API( vmmap )

DECLARE_API( SOSFlush )
{
    INIT_API();

    g_clrData->Flush();
    
    return Status;
}   // DECLARE_API( SOSFlush )

DECLARE_API( VMStat )
{
    INIT_API();

    if (IsMiniDumpFile() || !IsMemoryInfoAvailable())
    {
        ExtOut("!VMStat requires a full memory dump (.dump /ma) or a live process.\n");
    }
    else
    {
        vmstat();
    }

    return Status;
}   // DECLARE_API( vmmap )

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function saves a dll to a file.                              *  
*                                                                      *
\**********************************************************************/
DECLARE_API(SaveModule)
{
    INIT_API();
    MINIDUMP_NOT_SUPPORTED();    

    StringHolder Location;
    DWORD_PTR moduleAddr = NULL;
    BOOL bIsImage;

    CMDValue arg[] = 
    {   // vptr, type
        {&moduleAddr, COHEX},
        {&Location.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg)) 
    {
        return Status;
    }
    if (nArg != 2)
    {
        ExtOut("Usage: SaveModule <address> <file to save>\n");
        return Status;
    }
    if (moduleAddr == 0) {
        ExtOut ("Invalid arg\n");
        return Status;
    }

    char* ptr = Location.data;
    
    DWORD_PTR dllBase = 0;
    ULONG64 base;
    if (g_ExtSymbols->GetModuleByOffset(TO_CDADDR(moduleAddr),0,NULL,&base) == S_OK)
    {
        dllBase = TO_TADDR(base);
    }
    else if (IsModule(moduleAddr))
    {        
        DacpModuleData module;
        module.Request(g_sos, TO_CDADDR(moduleAddr));
        dllBase = TO_TADDR(module.ilBase);
        if (dllBase == 0)
        {
            ExtOut ("Module does not have base address\n");
            return Status;
        }
    }
    else
    {
        ExtOut ("%p is not a Module or base address\n", SOS_PTR(moduleAddr));
        return Status;
    }

    MEMORY_BASIC_INFORMATION64 mbi;
    if (FAILED(g_ExtData2->QueryVirtual(TO_CDADDR(dllBase), &mbi)))
    {
        ExtOut("Failed to retrieve information about segment %p", SOS_PTR(dllBase));
        return Status;
    }

    // module loaded as an image or mapped as a flat file?
    bIsImage = (mbi.Type == MEM_IMAGE);

    IMAGE_DOS_HEADER DosHeader;
    if (g_ExtData->ReadVirtual(TO_CDADDR(dllBase), &DosHeader, sizeof(DosHeader), NULL) != S_OK)
        return S_FALSE;

    IMAGE_NT_HEADERS Header;
    if (g_ExtData->ReadVirtual(TO_CDADDR(dllBase + DosHeader.e_lfanew), &Header, sizeof(Header), NULL) != S_OK)
        return S_FALSE;

    DWORD_PTR sectionAddr = dllBase + DosHeader.e_lfanew + offsetof(IMAGE_NT_HEADERS,OptionalHeader)
            + Header.FileHeader.SizeOfOptionalHeader;    

    IMAGE_SECTION_HEADER section;
    struct MemLocation
    {
        DWORD_PTR VAAddr;
        DWORD_PTR VASize;
        DWORD_PTR FileAddr;
        DWORD_PTR FileSize;
    };

    int nSection = Header.FileHeader.NumberOfSections;
    ExtOut("%u sections in file\n",nSection);
    MemLocation *memLoc = (MemLocation*)_alloca(nSection*sizeof(MemLocation));
    int indxSec = -1;
    int slot;
    for (int n = 0; n < nSection; n++)
    {
        if (g_ExtData->ReadVirtual(TO_CDADDR(sectionAddr), &section, sizeof(section), NULL) == S_OK)
        {
            for (slot = 0; slot <= indxSec; slot ++)
                if (section.PointerToRawData < memLoc[slot].FileAddr)
                    break;

            for (int k = indxSec; k >= slot; k --)
                memcpy(&memLoc[k+1], &memLoc[k], sizeof(MemLocation));

            memLoc[slot].VAAddr = section.VirtualAddress;
            memLoc[slot].VASize = section.Misc.VirtualSize;
            memLoc[slot].FileAddr = section.PointerToRawData;
            memLoc[slot].FileSize = section.SizeOfRawData;
            ExtOut("section %d - VA=%x, VASize=%x, FileAddr=%x, FileSize=%x\n",
                n, memLoc[slot].VAAddr,memLoc[slot]. VASize,memLoc[slot].FileAddr,
                memLoc[slot].FileSize);
            indxSec ++;
        }
        else
        {
            ExtOut("Fail to read PE section info\n");
            return Status;
        }
        sectionAddr += sizeof(section);
    }

    if (ptr[0] == '\0')
    {
        ExtOut ("File not specified\n");
        return Status;
    }

    PCSTR file = ptr;
    ptr += strlen(ptr)-1;
    while (isspace(*ptr))
    {
        *ptr = '\0';
        ptr --;
    }

    HANDLE hFile = CreateFileA(file,GENERIC_WRITE,0,NULL,CREATE_ALWAYS,0,NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        ExtOut ("Fail to create file %s\n", file);
        return Status;
    }

    ULONG pageSize = OSPageSize();
    char *buffer = (char *)_alloca(pageSize);
    DWORD nRead;
    DWORD nWrite;
    
    // NT PE Headers
    TADDR dwAddr = dllBase;
    TADDR dwEnd = dllBase + Header.OptionalHeader.SizeOfHeaders;
    while (dwAddr < dwEnd)
    {
        nRead = pageSize;
        if (dwEnd - dwAddr < nRead)
            nRead = (ULONG)(dwEnd - dwAddr);

        if (g_ExtData->ReadVirtual(TO_CDADDR(dwAddr), buffer, nRead, &nRead) == S_OK)
        {
            WriteFile(hFile,buffer,nRead,&nWrite,NULL);
        }
        else
        {
            ExtOut ("Fail to read memory\n");
            goto end;
        }
        dwAddr += nRead;
    }

    for (slot = 0; slot <= indxSec; slot ++)
    {
        dwAddr = dllBase + (bIsImage ? memLoc[slot].VAAddr : memLoc[slot].FileAddr);
        dwEnd = memLoc[slot].FileSize + dwAddr - 1;

        while (dwAddr <= dwEnd)
        {
            nRead = pageSize;
            if (dwEnd - dwAddr + 1 < pageSize)
                nRead = (ULONG)(dwEnd - dwAddr + 1);
            
            if (g_ExtData->ReadVirtual(TO_CDADDR(dwAddr), buffer, nRead, &nRead) == S_OK)
            {
                WriteFile(hFile,buffer,nRead,&nWrite,NULL);
            }
            else
            {
                ExtOut ("Fail to read memory\n");
                goto end;
            }
            dwAddr += pageSize;
        }
    }
end:
    CloseHandle (hFile);
    return Status;
}

#ifdef _DEBUG
DECLARE_API(dbgout)
{
    INIT_API();

    BOOL bOff = FALSE;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-off", &bOff, COBOOL, FALSE},
    };

    if (!GetCMDOption(args, option, _countof(option), NULL, 0, NULL))
    {
        return Status;
    }    

    Output::SetDebugOutputEnabled(!bOff);
    return Status;
}
DECLARE_API(filthint)
{
    INIT_API();

    BOOL bOff = FALSE;
    DWORD_PTR filter = 0;

    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-off", &bOff, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&filter, COHEX}
    };
    size_t nArg;
    if (!GetCMDOption(args, option, _countof(option),
                      arg, _countof(arg), &nArg)) 
    {
        return Status;
    }    
    if (bOff)
    {
        g_filterHint = 0;
        return Status;
    }

    g_filterHint = filter;
    return Status;
}
#endif // _DEBUG

#endif // FEATURE_PAL

static HRESULT DumpMDInfoBuffer(DWORD_PTR dwStartAddr, DWORD Flags, ULONG64 Esp, 
        ULONG64 IPAddr, StringOutput& so)
{
#define DOAPPEND(str)         \
    do { \
    if (!so.Append((str))) {  \
    return E_OUTOFMEMORY; \
    }} while (0)

    // Should we skip explicit frames?  They are characterized by Esp = 0, && Eip = 0 or 1.
    // See comment in FormatGeneratedException() for explanation why on non_IA64 Eip is 1, and not 0
    if (!(Flags & SOS_STACKTRACE_SHOWEXPLICITFRAMES) && (Esp == 0) && (IPAddr == 1))
    {
        return S_FALSE;
    }

    DacpMethodDescData MethodDescData;
    if (MethodDescData.Request(g_sos, TO_CDADDR(dwStartAddr)) != S_OK)
    {
        return E_FAIL;
    }

    ArrayHolder<WCHAR> wszNameBuffer = new WCHAR[MAX_LONGPATH+1];

    if (Flags & SOS_STACKTRACE_SHOWADDRESSES)
    {
        _snwprintf_s(wszNameBuffer, MAX_LONGPATH, MAX_LONGPATH, W("%p %p "), (void*)(size_t) Esp, (void*)(size_t) IPAddr); // _TRUNCATE
        DOAPPEND(wszNameBuffer);
    }

    DacpModuleData dmd;
    BOOL bModuleNameWorked = FALSE;
    ULONG64 addrInModule = IPAddr;
    if (dmd.Request(g_sos, MethodDescData.ModulePtr) == S_OK)
    {
        CLRDATA_ADDRESS base = 0;
        if (g_sos->GetPEFileBase(dmd.File, &base) == S_OK)
        {
            if (base)
            {
                addrInModule = base;
            }
        }
    }
    ULONG Index;
    ULONG64 base;
    if (g_ExtSymbols->GetModuleByOffset(UL64_TO_CDA(addrInModule), 0, &Index, &base) == S_OK)
    {                                    
        ArrayHolder<char> szModuleName = new char[MAX_LONGPATH+1];
        if (g_ExtSymbols->GetModuleNames(Index, base, NULL, 0, NULL, szModuleName, MAX_LONGPATH, NULL, NULL, 0, NULL) == S_OK)
        {
            MultiByteToWideChar (CP_ACP, 0, szModuleName, MAX_LONGPATH, wszNameBuffer, MAX_LONGPATH);
            DOAPPEND (wszNameBuffer);
            bModuleNameWorked = TRUE;
        }
    }
#ifdef FEATURE_PAL
    else
    {
        if (g_sos->GetPEFileName(dmd.File, MAX_LONGPATH, wszNameBuffer, NULL) == S_OK)
        {
            if (wszNameBuffer[0] != W('\0'))
            {
                WCHAR *pJustName = _wcsrchr(wszNameBuffer, DIRECTORY_SEPARATOR_CHAR_W);
                if (pJustName == NULL)
                    pJustName = wszNameBuffer - 1;

                DOAPPEND(pJustName + 1);
                bModuleNameWorked = TRUE;
            }
        }
    }
#endif // FEATURE_PAL

    // Under certain circumstances DacpMethodDescData::GetMethodDescName() 
    //   returns a module qualified method name
    HRESULT hr = g_sos->GetMethodDescName(dwStartAddr, MAX_LONGPATH, wszNameBuffer, NULL);

    WCHAR* pwszMethNameBegin = (hr != S_OK ? NULL : _wcschr(wszNameBuffer, L'!'));
    if (!bModuleNameWorked && hr == S_OK && pwszMethNameBegin != NULL)
    {
        // if we weren't able to get the module name, but GetMethodDescName returned
        // the module as part of the returned method name, use this data
        DOAPPEND(wszNameBuffer);
    }
    else
    {
        if (!bModuleNameWorked)
        {
            DOAPPEND (W("UNKNOWN"));
        }
        DOAPPEND(W("!"));
        if (hr == S_OK)
        {
            // the module name we retrieved above from debugger will take 
            // precedence over the name possibly returned by GetMethodDescName()
            DOAPPEND(pwszMethNameBegin != NULL ? (pwszMethNameBegin+1) : (WCHAR *)wszNameBuffer);
        }
        else
        {
            DOAPPEND(W("UNKNOWN"));
        }
    }

    ULONG64 Displacement = (IPAddr - MethodDescData.NativeCodeAddr);
    if (Displacement)
    {
        _snwprintf_s(wszNameBuffer, MAX_LONGPATH, MAX_LONGPATH, W("+%#x"), Displacement); // _TRUNCATE
        DOAPPEND (wszNameBuffer);
    }

    return S_OK;
#undef DOAPPEND
}

BOOL AppendContext(LPVOID pTransitionContexts, size_t maxCount, size_t *pcurCount, size_t uiSizeOfContext,
    CROSS_PLATFORM_CONTEXT *context)
{
    if (pTransitionContexts == NULL || *pcurCount >= maxCount)
    {
        ++(*pcurCount);
        return FALSE;
    }
    if (uiSizeOfContext == sizeof(StackTrace_SimpleContext))
    {
        StackTrace_SimpleContext *pSimple = (StackTrace_SimpleContext *) pTransitionContexts;
        g_targetMachine->FillSimpleContext(&pSimple[*pcurCount], context);
    }
    else if (uiSizeOfContext == g_targetMachine->GetContextSize())
    {
        // FillTargetContext ensures we only write uiSizeOfContext bytes in pTransitionContexts
        // and not sizeof(CROSS_PLATFORM_CONTEXT) bytes (which would overrun).
        g_targetMachine->FillTargetContext(pTransitionContexts, context, (int)(*pcurCount));
    }
    else
    {
        return FALSE;
    }
    ++(*pcurCount);
    return TRUE;
}

HRESULT CALLBACK ImplementEFNStackTrace(
    PDEBUG_CLIENT client,
    __out_ecount_opt(*puiTextLength) WCHAR wszTextOut[],
    size_t *puiTextLength,
    LPVOID pTransitionContexts,
    size_t *puiTransitionContextCount,
    size_t uiSizeOfContext,
    DWORD Flags) 
{

#define DOAPPEND(str) if (!so.Append((str))) { \
    Status = E_OUTOFMEMORY;                    \
    goto Exit;                                 \
}

    HRESULT Status = E_FAIL;    
    StringOutput so;
    size_t transitionContextCount = 0;

    if (puiTextLength == NULL)
    {
        return E_INVALIDARG;
    }

    if (pTransitionContexts)
    {
        if (puiTransitionContextCount == NULL)
        {
            return E_INVALIDARG;
        }

        // Do error checking on context size
        if ((uiSizeOfContext != g_targetMachine->GetContextSize()) &&
            (uiSizeOfContext != sizeof(StackTrace_SimpleContext)))
        {
            return E_INVALIDARG;
        }
    }

    IXCLRDataStackWalk *pStackWalk = NULL;
    IXCLRDataTask* Task;
    ULONG ThreadId;

    if ((Status = g_ExtSystem->GetCurrentThreadSystemId(&ThreadId)) != S_OK ||
        (Status = g_clrData->GetTaskByOSThreadID(ThreadId, &Task)) != S_OK)
    {
        // Not a managed thread.
        return SOS_E_NOMANAGEDCODE;
    }

    Status = Task->CreateStackWalk(CLRDATA_SIMPFRAME_UNRECOGNIZED |
                                   CLRDATA_SIMPFRAME_MANAGED_METHOD |
                                   CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                                   CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                                   &pStackWalk);

    Task->Release();

    if (Status != S_OK)
    {
        if (Status == E_FAIL)
        {
            return SOS_E_NOMANAGEDCODE;
        }
        return Status;
    }

#ifdef _TARGET_WIN64_
    ULONG numFrames = 0;
    BOOL bInNative = TRUE;

    Status = GetContextStackTrace(ThreadId, &numFrames);
    if (FAILED(Status))
    {
        goto Exit;
    }

    for (ULONG i = 0; i < numFrames; i++)
    {
        PDEBUG_STACK_FRAME pCur = g_Frames + i;                

        CLRDATA_ADDRESS pMD;
        if (g_sos->GetMethodDescPtrFromIP(pCur->InstructionOffset, &pMD) == S_OK)
        {
            if (bInNative || transitionContextCount==0)
            {
                // We only want to list one transition frame if there are multiple frames.
                bInNative = FALSE;

                DOAPPEND (W("(TransitionMU)\n"));
                // For each transition, we need to store the context information
                if (puiTransitionContextCount)
                {
                    // below we cast the i-th AMD64_CONTEXT to CROSS_PLATFORM_CONTEXT
                    AppendContext (pTransitionContexts, *puiTransitionContextCount, 
                        &transitionContextCount, uiSizeOfContext, (CROSS_PLATFORM_CONTEXT*)(&(g_FrameContexts[i])));
                }
                else
                {
                    transitionContextCount++;
                }
            }

            Status = DumpMDInfoBuffer((DWORD_PTR) pMD, Flags,
                    pCur->StackOffset, pCur->InstructionOffset, so);
            if (FAILED(Status))
            {
                goto Exit;
            }
            else if (Status == S_OK)
            {
                DOAPPEND (W("\n"));
            }
            // for S_FALSE do not append anything

        }        
        else
        {
            if (!bInNative)
            {
                // We only want to list one transition frame if there are multiple frames.
                bInNative = TRUE;

                DOAPPEND (W("(TransitionUM)\n"));
                // For each transition, we need to store the context information
                if (puiTransitionContextCount)
                {
                    AppendContext (pTransitionContexts, *puiTransitionContextCount, 
                        &transitionContextCount, uiSizeOfContext, (CROSS_PLATFORM_CONTEXT*)(&(g_FrameContexts[i])));
                }
                else
                {
                    transitionContextCount++;
                }
            }
        }
    }

Exit:
#else // _TARGET_WIN64_

#ifdef _DEBUG
    size_t prevLength = 0;
    static WCHAR wszNameBuffer[1024]; // should be large enough
    wcscpy_s(wszNameBuffer, 1024, W("Frame")); // default value
#endif

    BOOL bInNative = TRUE;

    UINT frameCount = 0;
    do
    {
        DacpFrameData FrameData;
        if ((Status = FrameData.Request(pStackWalk)) != S_OK)
        {
            goto Exit;
        }

        CROSS_PLATFORM_CONTEXT context;
        if ((Status=pStackWalk->GetContext(DT_CONTEXT_FULL, g_targetMachine->GetContextSize(),
                                           NULL, (BYTE *)&context))!=S_OK)
        {
            goto Exit;
        }

        ExtDbgOut ( " * Ctx[BSI]:  %08x  %08x  %08x    ", GetBP(context), GetSP(context), GetIP(context) );

        CLRDATA_ADDRESS pMD;
        if (!FrameData.frameAddr)
        {
            if (bInNative || transitionContextCount==0)
            {
                // We only want to list one transition frame if there are multiple frames.
                bInNative = FALSE;

                DOAPPEND (W("(TransitionMU)\n"));
                // For each transition, we need to store the context information
                if (puiTransitionContextCount)
                {
                    AppendContext (pTransitionContexts, *puiTransitionContextCount, 
                            &transitionContextCount, uiSizeOfContext, &context);
                }
                else
                {
                    transitionContextCount++;
                }                    
            }

            // we may have a method, try to get the methoddesc
            if (g_sos->GetMethodDescPtrFromIP(GetIP(context), &pMD)==S_OK)
            {
                Status = DumpMDInfoBuffer((DWORD_PTR) pMD, Flags, 
                                          GetSP(context), GetIP(context), so);
                if (FAILED(Status))
                {
                    goto Exit;
                }
                else if (Status == S_OK)
                {
                    DOAPPEND (W("\n"));
                }
                // for S_FALSE do not append anything
            }
        }
        else
        {
#ifdef _DEBUG
            if (Output::IsDebugOutputEnabled())
            {
                DWORD_PTR vtAddr;
                MOVE(vtAddr, TO_TADDR(FrameData.frameAddr));
                if (g_sos->GetFrameName(TO_CDADDR(vtAddr), 1024, wszNameBuffer, NULL) == S_OK)
                    ExtDbgOut("[%ls: %08x] ", wszNameBuffer, FrameData.frameAddr);  
                else
                    ExtDbgOut("[Frame: %08x] ", FrameData.frameAddr);
            }
#endif
            if (!bInNative)
            {
                // We only want to list one transition frame if there are multiple frames.
                bInNative = TRUE;

                DOAPPEND (W("(TransitionUM)\n"));
                // For each transition, we need to store the context information
                if (puiTransitionContextCount)
                {
                    AppendContext (pTransitionContexts, *puiTransitionContextCount, 
                            &transitionContextCount, uiSizeOfContext, &context);
                }
                else
                {
                    transitionContextCount++;
                }                    
            }
        }

#ifdef _DEBUG
        if (so.Length() > prevLength)
        {
            ExtDbgOut ( "%ls", so.String()+prevLength );
            prevLength = so.Length();
        }
        else
            ExtDbgOut ( "\n" );
#endif

    } 
    while ((frameCount++) < MAX_STACK_FRAMES && pStackWalk->Next()==S_OK);
    
    Status = S_OK;

Exit:
#endif // _TARGET_WIN64_

    if (pStackWalk)
    {
        pStackWalk->Release();
        pStackWalk = NULL;
    }

    // We have finished. Does the user want to copy this data to a buffer?
    if (Status == S_OK)
    {
        if(wszTextOut)
        {
            // They want at least partial output
            wcsncpy_s (wszTextOut, *puiTextLength, so.String(),  *puiTextLength-1); // _TRUNCATE
        }
        else
        {
            *puiTextLength = _wcslen (so.String()) + 1;
        }

        if (puiTransitionContextCount)
        {
            *puiTransitionContextCount = transitionContextCount;
        }
    }

    return Status;
}

#ifdef FEATURE_PAL
#define PAL_TRY_NAKED PAL_CPP_TRY
#define PAL_EXCEPT_NAKED(disp) PAL_CPP_CATCH_ALL
#define PAL_ENDTRY_NAKED PAL_CPP_ENDTRY
#endif

// TODO: Convert PAL_TRY_NAKED to something that works on the Mac.
HRESULT CALLBACK ImplementEFNStackTraceTry(
    PDEBUG_CLIENT client,
    __out_ecount_opt(*puiTextLength) WCHAR wszTextOut[],
    size_t *puiTextLength,
    LPVOID pTransitionContexts,
    size_t *puiTransitionContextCount,
    size_t uiSizeOfContext,
    DWORD Flags) 
{
    HRESULT Status = E_FAIL;

    PAL_TRY_NAKED
    {
        Status = ImplementEFNStackTrace(client, wszTextOut, puiTextLength, 
            pTransitionContexts, puiTransitionContextCount,
            uiSizeOfContext, Flags);
    }
    PAL_EXCEPT_NAKED (EXCEPTION_EXECUTE_HANDLER)
    {
    }        
    PAL_ENDTRY_NAKED

    return Status;
}

// See sos_stacktrace.h for the contract with the callers regarding the LPVOID arguments.
HRESULT CALLBACK _EFN_StackTrace(
    PDEBUG_CLIENT client,
    __out_ecount_opt(*puiTextLength) WCHAR wszTextOut[],
    size_t *puiTextLength,
    __out_bcount_opt(uiSizeOfContext*(*puiTransitionContextCount)) LPVOID pTransitionContexts,
    size_t *puiTransitionContextCount,
    size_t uiSizeOfContext,
    DWORD Flags) 
{
    INIT_API();    

    Status = ImplementEFNStackTraceTry(client, wszTextOut, puiTextLength, 
        pTransitionContexts, puiTransitionContextCount,
        uiSizeOfContext, Flags);

    return Status;
}


BOOL FormatFromRemoteString(DWORD_PTR strObjPointer, __out_ecount(cchString) PWSTR wszBuffer, ULONG cchString)
{
    BOOL bRet = FALSE;

    wszBuffer[0] = L'\0';
    
    DacpObjectData objData;
    if (objData.Request(g_sos, TO_CDADDR(strObjPointer))!=S_OK)
    {
        return bRet;
    }

    strobjInfo stInfo;

    if (MOVE(stInfo, strObjPointer) != S_OK)
    {
        return bRet;
    }
    
    DWORD dwBufLength = 0;
    if (!ClrSafeInt<DWORD>::addition(stInfo.m_StringLength, 1, dwBufLength))
    {
        ExtOut("<integer overflow>\n");
        return bRet;
    }

    LPWSTR pwszBuf = new NOTHROW WCHAR[dwBufLength];
    if (pwszBuf == NULL)
    {
        return bRet;
    }
    
    if (g_sos->GetObjectStringData(TO_CDADDR(strObjPointer), stInfo.m_StringLength+1, pwszBuf, NULL)!=S_OK)
    {
        delete [] pwszBuf;
        return bRet;
    }

    // String is in format
    // <SP><SP><SP>at <function name>(args,...)\n
    // ...
    // Parse and copy just <function name>(args,...)

    LPWSTR pwszPointer = pwszBuf;

    WCHAR PSZSEP[] = W("   at ");

    UINT Length = 0;
    while(1)
    {
        if (_wcsncmp(pwszPointer, PSZSEP, _countof(PSZSEP)-1) != 0)
        {
            delete [] pwszBuf;
            return bRet;
        }

        pwszPointer += _wcslen(PSZSEP);
        LPWSTR nextPos = _wcsstr(pwszPointer, PSZSEP);
        if (nextPos == NULL)
        {
            // Done! Note that we are leaving the function before we add the last
            // line of stack trace to the output string. This is on purpose because
            // this string needs to be merged with a real trace, and the last line
            // of the trace will be common to the real trace.
            break;
        }
        WCHAR c = *nextPos;
        *nextPos = L'\0';

        // Buffer is calculated for sprintf below ("   %p %p %S\n");
        WCHAR wszLineBuffer[mdNameLen + 8 + sizeof(size_t)*2];

        // Note that we don't add a newline because we have this embedded in wszLineBuffer
        swprintf_s(wszLineBuffer, _countof(wszLineBuffer), W("    %p %p %s"), (void*)(size_t)-1, (void*)(size_t)-1, pwszPointer);
        Length += (UINT)_wcslen(wszLineBuffer);
        
        if (wszBuffer)
        {            
            wcsncat_s(wszBuffer, cchString, wszLineBuffer, _TRUNCATE);
        }

        *nextPos = c;
        // Move to the next line.
        pwszPointer = nextPos;
    }
    
    delete [] pwszBuf; 

    // Return TRUE only if the stack string had any information that was successfully parsed.
    // (Length > 0) is a good indicator of that.
    bRet = (Length > 0);
    return bRet;
}

HRESULT AppendExceptionInfo(CLRDATA_ADDRESS cdaObj, 
    __out_ecount(cchString) PWSTR wszStackString,
    ULONG cchString,
    BOOL bNestedCase) // If bNestedCase is TRUE, the last frame of the computed stack is left off
{    
    DacpObjectData objData;
    if (objData.Request(g_sos, cdaObj) != S_OK)
    {        
        return E_FAIL;
    }

    // Make sure it is an exception object, and get the MT of Exception
    CLRDATA_ADDRESS exceptionMT = isExceptionObj(objData.MethodTable);
    if (exceptionMT == NULL)
    {
        return E_INVALIDARG;
    }

    // First try to get exception object data using ISOSDacInterface2
    DacpExceptionObjectData excData;
    BOOL bGotExcData = SUCCEEDED(excData.Request(g_sos, cdaObj));

    int iOffset;    
    // Is there a _remoteStackTraceString? We'll want to prepend that data.
    // We only have string data, so IP/SP info has to be set to -1.
    DWORD_PTR strPointer;
    if (bGotExcData)
    {
        strPointer = TO_TADDR(excData.RemoteStackTraceString);
    }
    else
    {
        iOffset = GetObjFieldOffset (cdaObj, objData.MethodTable, W("_remoteStackTraceString"));
        MOVE (strPointer, TO_TADDR(cdaObj) + iOffset);        
    }
    if (strPointer)
    {
        WCHAR *pwszBuffer = new NOTHROW WCHAR[cchString];
        if (pwszBuffer == NULL)
        {
            return E_OUTOFMEMORY;
        }
        
        if (FormatFromRemoteString(strPointer, pwszBuffer, cchString))
        {
            // Prepend this stuff to the string for the user
            wcsncat_s(wszStackString, cchString, pwszBuffer, _TRUNCATE);
        }
        delete[] pwszBuffer;
    }
    
    BOOL bAsync = bGotExcData ? IsAsyncException(excData)
                              : IsAsyncException(cdaObj, objData.MethodTable);

    DWORD_PTR arrayPtr;
    if (bGotExcData)
    {
        arrayPtr = TO_TADDR(excData.StackTrace);
    }
    else
    {
        iOffset = GetObjFieldOffset (cdaObj, objData.MethodTable, W("_stackTrace"));
        MOVE (arrayPtr, TO_TADDR(cdaObj) + iOffset);
    }

    if (arrayPtr)
    {
        DWORD arrayLen;
        MOVE (arrayLen, arrayPtr + sizeof(DWORD_PTR));

        if (arrayLen)
        {
#ifdef _TARGET_WIN64_
            DWORD_PTR dataPtr = arrayPtr + sizeof(DWORD_PTR) + sizeof(DWORD) + sizeof(DWORD);
#else
            DWORD_PTR dataPtr = arrayPtr + sizeof(DWORD_PTR) + sizeof(DWORD);
#endif // _TARGET_WIN64_
            size_t stackTraceSize = 0;
            MOVE (stackTraceSize, dataPtr); // data length is stored at the beginning of the array in this case

            DWORD cbStackSize = static_cast<DWORD>(stackTraceSize * sizeof(StackTraceElement));
            dataPtr += sizeof(size_t) + sizeof(size_t); // skip the array header, then goes the data
            
            if (stackTraceSize != 0)
            {                
                size_t iLength = FormatGeneratedException (dataPtr, cbStackSize, NULL, 0, bAsync, bNestedCase);
                WCHAR *pwszBuffer = new NOTHROW WCHAR[iLength + 1];
                if (pwszBuffer)
                {
                    FormatGeneratedException(dataPtr, cbStackSize, pwszBuffer, iLength + 1, bAsync, bNestedCase);
                    wcsncat_s(wszStackString, cchString, pwszBuffer, _TRUNCATE);
                    delete[] pwszBuffer;
                }
                else
                {
                    return E_OUTOFMEMORY;
                }
            }
        }
    }                   
    return S_OK;
}

HRESULT ImplementEFNGetManagedExcepStack(
    CLRDATA_ADDRESS cdaStackObj, 
    __out_ecount(cchString) PWSTR wszStackString,
    ULONG cchString)
{
    HRESULT Status = E_FAIL;

    if (wszStackString == NULL || cchString == 0)
    {
        return E_INVALIDARG;
    }

    CLRDATA_ADDRESS threadAddr = GetCurrentManagedThread();
    DacpThreadData Thread;
    BOOL bCanUseThreadContext = TRUE;

    ZeroMemory(&Thread, sizeof(DacpThreadData));
    
    if ((threadAddr == NULL) || (Thread.Request(g_sos, threadAddr) != S_OK))
    {
        // The current thread is unmanaged
        bCanUseThreadContext = FALSE;
    }

    if (cdaStackObj == NULL)    
    {
        if (!bCanUseThreadContext)
        {
            return E_INVALIDARG;
        }
        
        TADDR taLTOH = NULL;
        if ((!SafeReadMemory(TO_TADDR(Thread.lastThrownObjectHandle),
                            &taLTOH,
                            sizeof(taLTOH), NULL)) || (taLTOH==NULL))
        {
            return Status;
        }    
        else
        {        
            cdaStackObj = TO_CDADDR(taLTOH);
        }
    }

    // Put the stack trace header on
    AddExceptionHeader(wszStackString, cchString);
    
    // First is there a nested exception?
    if (bCanUseThreadContext && Thread.firstNestedException)    
    {
        CLRDATA_ADDRESS obj = 0, next = 0;
        CLRDATA_ADDRESS currentNested = Thread.firstNestedException;
        do
        {
            Status = g_sos->GetNestedExceptionData(currentNested, &obj, &next);

            // deal with the inability to read a nested exception gracefully
            if (Status != S_OK)
            {
                break;
            }
                        
            Status = AppendExceptionInfo(obj, wszStackString, cchString, TRUE);
            currentNested = next;
        }
        while(currentNested != NULL);                        
    }
    
    Status = AppendExceptionInfo(cdaStackObj, wszStackString, cchString, FALSE);

    return Status;
}

// TODO: Enable this when ImplementEFNStackTraceTry is fixed.
// This function, like VerifyDAC, exists for the purpose of testing
// hard-to-get-to SOS APIs.
DECLARE_API(VerifyStackTrace)
{
    INIT_API();

    BOOL bVerifyManagedExcepStack = FALSE;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-ManagedExcepStack", &bVerifyManagedExcepStack, COBOOL, FALSE},
    };
    
    if (!GetCMDOption(args, option, _countof(option), NULL,0,NULL)) 
    {
        return Status;
    }

    if (bVerifyManagedExcepStack)
    {
        CLRDATA_ADDRESS threadAddr = GetCurrentManagedThread();
        DacpThreadData Thread;

        TADDR taExc = NULL;
        if ((threadAddr == NULL) || (Thread.Request(g_sos, threadAddr) != S_OK))
        {
            ExtOut("The current thread is unmanaged\n");
            return Status;
        }

        TADDR taLTOH = NULL;
        if ((!SafeReadMemory(TO_TADDR(Thread.lastThrownObjectHandle),
                            &taLTOH,
                            sizeof(taLTOH), NULL)) || (taLTOH == NULL))
        {
            ExtOut("There is no current managed exception on this thread\n");            
            return Status;
        }    
        else
        {        
            taExc = taLTOH;
        }

        const SIZE_T cchStr = 4096;
        WCHAR *wszStr = (WCHAR *)alloca(cchStr * sizeof(WCHAR));
        if (ImplementEFNGetManagedExcepStack(TO_CDADDR(taExc), wszStr, cchStr) != S_OK)
        {
            ExtOut("Error!\n");
            return Status;
        }

        ExtOut("_EFN_GetManagedExcepStack(%P, wszStr, sizeof(wszStr)) returned:\n", SOS_PTR(taExc));
        ExtOut("%S\n", wszStr);

        if (ImplementEFNGetManagedExcepStack((ULONG64)NULL, wszStr, cchStr) != S_OK)
        {
            ExtOut("Error!\n");
            return Status;
        }

        ExtOut("_EFN_GetManagedExcepStack(NULL, wszStr, sizeof(wszStr)) returned:\n");
        ExtOut("%S\n", wszStr);
    }
    else
    {
        size_t textLength = 0;
        size_t contextLength = 0;
        Status = ImplementEFNStackTraceTry(client,
                                 NULL,
                                 &textLength,
                                 NULL,
                                 &contextLength,
                                 0,
                                 0);

        if (Status != S_OK)
        {
            ExtOut("Error: %lx\n", Status);
            return Status;
        }

        ExtOut("Number of characters requested: %d\n", textLength);
        WCHAR *wszBuffer = new NOTHROW WCHAR[textLength + 1];
        if (wszBuffer == NULL)
        {
            ReportOOM();
            return Status;
        }

        // For the transition contexts buffer the callers are expected to allocate 
        // contextLength * sizeof(TARGET_CONTEXT), and not
        // contextLength * sizeof(CROSS_PLATFORM_CONTEXT). See sos_stacktrace.h for
        // details.
        LPBYTE pContexts = new NOTHROW BYTE[contextLength * g_targetMachine->GetContextSize()];

        if (pContexts == NULL)
        {
            ReportOOM();
            delete[] wszBuffer;
            return Status;
        }

        Status = ImplementEFNStackTrace(client,
                                 wszBuffer,
                                 &textLength,
                                 pContexts,
                                 &contextLength,
                                 g_targetMachine->GetContextSize(),
                                 0);

        if (Status != S_OK)
        {
            ExtOut("Error: %lx\n", Status);
            delete[] wszBuffer;
            delete [] pContexts;
            return Status;
        }

        ExtOut("%S\n", wszBuffer);

        ExtOut("Context information:\n");
        if (IsDbgTargetX86())
        {
            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                   "Ebp", "Esp", "Eip"); 
        }
        else if (IsDbgTargetAmd64())
        {
            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                   "Rbp", "Rsp", "Rip"); 
        }
        else if (IsDbgTargetArm())
        {
            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                   "FP", "SP", "PC"); 
        }
        else
        {
            ExtOut("Unsupported platform");
            delete [] pContexts;
            delete[] wszBuffer;
            return S_FALSE;
        }

        for (size_t j=0; j < contextLength; j++)
        {
            CROSS_PLATFORM_CONTEXT *pCtx = (CROSS_PLATFORM_CONTEXT*)(pContexts + j*g_targetMachine->GetContextSize());
            ExtOut("%p %p %p\n", GetBP(*pCtx), GetSP(*pCtx), GetIP(*pCtx));
        }

        delete [] pContexts;

        StackTrace_SimpleContext *pSimple = new NOTHROW StackTrace_SimpleContext[contextLength];
        if (pSimple == NULL)
        {
            ReportOOM();
            delete[] wszBuffer;
            return Status;
        }

        Status = ImplementEFNStackTrace(client,
                                 wszBuffer,
                                 &textLength,
                                 pSimple,
                                 &contextLength,
                                 sizeof(StackTrace_SimpleContext),
                                 0);

        if (Status != S_OK)
        {
            ExtOut("Error: %lx\n", Status);
            delete[] wszBuffer;
            delete [] pSimple;
            return Status;
        }

        ExtOut("Simple Context information:\n");
        if (IsDbgTargetX86())
            ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                       "Ebp", "Esp", "Eip"); 
        else if (IsDbgTargetAmd64())
                ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                       "Rbp", "Rsp", "Rip"); 
        else if (IsDbgTargetArm())
                ExtOut("%" POINTERSIZE "s %" POINTERSIZE "s %" POINTERSIZE "s\n",
                       "FP", "SP", "PC"); 
        else 
        {
            ExtOut("Unsupported platform");
            delete[] wszBuffer;
            delete [] pSimple;
            return S_FALSE;
        }
        for (size_t j=0; j < contextLength; j++)
        {
            ExtOut("%p %p %p\n", SOS_PTR(pSimple[j].FrameOffset),
                    SOS_PTR(pSimple[j].StackOffset),
                    SOS_PTR(pSimple[j].InstructionOffset));
        }
        delete [] pSimple;
        delete[] wszBuffer;
    }

    return Status;
}

#ifndef FEATURE_PAL

// This is an internal-only Apollo extension to de-optimize the code
DECLARE_API(SuppressJitOptimization)
{
    INIT_API_NOEE();    
    MINIDUMP_NOT_SUPPORTED();    

    StringHolder onOff;
    CMDValue arg[] = 
    {   // vptr, type
        {&onOff.data, COSTRING},
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg)) 
    {
        return E_FAIL;
    }

    if(nArg == 1 && (_stricmp(onOff.data, "On") == 0))
    {
        // if CLR is already loaded, try to change the flags now
        if(CheckEEDll() == S_OK)
        {
            SetNGENCompilerFlags(CORDEBUG_JIT_DISABLE_OPTIMIZATION);
        }

        if(!g_fAllowJitOptimization)
            ExtOut("JIT optimization is already suppressed\n");
        else
        {
            g_fAllowJitOptimization = FALSE;
            g_ExtControl->Execute(DEBUG_EXECUTE_NOT_LOGGED, "sxe -c \"!HandleCLRN\" clrn", 0);
            ExtOut("JIT optimization will be suppressed\n");
        }


    }
    else if(nArg == 1 && (_stricmp(onOff.data, "Off") == 0))
    {
        // if CLR is already loaded, try to change the flags now
        if(CheckEEDll() == S_OK)
        {
            SetNGENCompilerFlags(CORDEBUG_JIT_DEFAULT);
        }

        if(g_fAllowJitOptimization)
            ExtOut("JIT optimization is already permitted\n");
        else
        {
            g_fAllowJitOptimization = TRUE;
            ExtOut("JIT optimization will be permitted\n");
        }
    }
    else
    {
        ExtOut("Usage: !SuppressJitOptimization <on|off>\n");
    }

    return S_OK;
}

// Uses ICorDebug to set the state of desired NGEN compiler flags. This can suppress pre-jitted optimized
// code
HRESULT SetNGENCompilerFlags(DWORD flags)
{
    HRESULT hr;

    ToRelease<ICorDebugProcess2> proc2;
    if(FAILED(hr = InitCorDebugInterface()))
    {
        ExtOut("SOS: warning, prejitted code optimizations could not be changed. Failed to load ICorDebug HR = 0x%x\n", hr);
    }
    else if(FAILED(g_pCorDebugProcess->QueryInterface(__uuidof(ICorDebugProcess2), (void**) &proc2)))
    {
        if(flags != CORDEBUG_JIT_DEFAULT)
        {
            ExtOut("SOS: warning, prejitted code optimizations could not be changed. This CLR version doesn't support the functionality\n");
        }
        else
        {
            hr = S_OK;
        }
    }
    else if(FAILED(hr = proc2->SetDesiredNGENCompilerFlags(flags)))
    {
        // Versions of CLR that don't have SetDesiredNGENCompilerFlags DAC-ized will return E_FAIL.
        // This was first supported in the clr_triton branch around 4/1/12, Apollo release
        // It will likely be supported in desktop CLR during Dev12
        if(hr == E_FAIL)
        {
            if(flags != CORDEBUG_JIT_DEFAULT)
            {
                ExtOut("SOS: warning, prejitted code optimizations could not be changed. This CLR version doesn't support the functionality\n");
            }
            else
            {
                hr = S_OK;
            }
        }
        else if(hr == CORDBG_E_NGEN_NOT_SUPPORTED)
        {
            if(flags != CORDEBUG_JIT_DEFAULT)
            {
                ExtOut("SOS: warning, prejitted code optimizations could not be changed. This CLR version doesn't support NGEN\n");
            }
            else
            {
                hr = S_OK;
            }
        }
        else if(hr == CORDBG_E_MUST_BE_IN_CREATE_PROCESS)
        {
            DWORD currentFlags = 0;
            if(FAILED(hr = proc2->GetDesiredNGENCompilerFlags(&currentFlags)))
            {
                ExtOut("SOS: warning, prejitted code optimizations could not be changed. GetDesiredNGENCompilerFlags failed hr=0x%x\n", hr);
            }
            else if(currentFlags != flags)
            {
                ExtOut("SOS: warning, prejitted code optimizations could not be changed at this time. This setting is fixed once CLR starts\n");
            }
            else
            {
                hr = S_OK;
            }
        }
        else
        {
            ExtOut("SOS: warning, prejitted code optimizations could not be changed at this time. SetDesiredNGENCompilerFlags hr = 0x%x\n", hr);
        }
    }

    return hr;
}


// This is an internal-only Apollo extension to save breakpoint/watch state
DECLARE_API(SaveState)
{
    INIT_API_NOEE();    
    MINIDUMP_NOT_SUPPORTED();    

    StringHolder filePath;
    CMDValue arg[] = 
    {   // vptr, type
        {&filePath.data, COSTRING},
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg)) 
    {
        return E_FAIL;
    }

    if(nArg == 0)
    {
        ExtOut("Usage: !SaveState <file_path>\n");
    }

    FILE* pFile;
    errno_t error = fopen_s(&pFile, filePath.data, "w");
    if(error != 0)
    {
        ExtOut("Failed to open file %s, error=0x%x\n", filePath.data, error);
        return E_FAIL;
    }

    g_bpoints.SaveBreakpoints(pFile);
    g_watchCmd.SaveListToFile(pFile);

    fclose(pFile);
    ExtOut("Session breakpoints and watch expressions saved to %s\n", filePath.data);
    return S_OK;
}

#endif // FEATURE_PAL

DECLARE_API(StopOnCatch)
{
    INIT_API();    
    MINIDUMP_NOT_SUPPORTED();    

    g_stopOnNextCatch = TRUE;
    ULONG32 flags = 0;
    g_clrData->GetOtherNotificationFlags(&flags);
    flags |= CLRDATA_NOTIFY_ON_EXCEPTION_CATCH_ENTER;
    g_clrData->SetOtherNotificationFlags(flags);
    ExtOut("Debuggee will break the next time a managed exception is caught during execution\n");
    return S_OK;
}

// This is an undocumented SOS extension command intended to help test SOS
// It causes the Dml output to be printed to the console uninterpretted so
// that a test script can read the commands which are hidden in the markup
DECLARE_API(ExposeDML)
{
    Output::SetDMLExposed(true);
    return S_OK;
}

// According to kksharma the Windows debuggers always sign-extend
// arguments when calling externally, therefore StackObjAddr 
// conforms to CLRDATA_ADDRESS contract.
HRESULT CALLBACK
_EFN_GetManagedExcepStack(
    PDEBUG_CLIENT client,
    ULONG64 StackObjAddr,
   __out_ecount (cbString) PSTR szStackString,
    ULONG cbString
    )
{
    INIT_API();

    ArrayHolder<WCHAR> tmpStr = new NOTHROW WCHAR[cbString];
    if (tmpStr == NULL)
    {
        ReportOOM();
        return E_OUTOFMEMORY;
    }

    if (FAILED(Status = ImplementEFNGetManagedExcepStack(StackObjAddr, tmpStr, cbString)))
    {
        return Status;
    }

    if (WideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, tmpStr, -1, szStackString, cbString, NULL, NULL) == 0)
    {
        return E_FAIL;
    }

    return S_OK;
}

// same as _EFN_GetManagedExcepStack, but returns the stack as a wide string.
HRESULT CALLBACK
_EFN_GetManagedExcepStackW(
    PDEBUG_CLIENT client,
    ULONG64 StackObjAddr,
    __out_ecount(cchString) PWSTR wszStackString,
    ULONG cchString
    )
{
    INIT_API();

    return ImplementEFNGetManagedExcepStack(StackObjAddr, wszStackString, cchString);
}
    
// According to kksharma the Windows debuggers always sign-extend
// arguments when calling externally, therefore objAddr 
// conforms to CLRDATA_ADDRESS contract.
HRESULT CALLBACK
_EFN_GetManagedObjectName(
    PDEBUG_CLIENT client,
    ULONG64 objAddr,
    __out_ecount (cbName) PSTR szName,
    ULONG cbName
    )
{
    INIT_API ();

    if (!sos::IsObject(objAddr, false))
    {
        return E_INVALIDARG;
    }

    sos::Object obj = TO_TADDR(objAddr);

    if (WideCharToMultiByte(CP_ACP, 0, obj.GetTypeName(), (int) (_wcslen(obj.GetTypeName()) + 1),
                            szName, cbName, NULL, NULL) == 0)
    {
        return E_FAIL;
    }
    return S_OK;
}

// According to kksharma the Windows debuggers always sign-extend
// arguments when calling externally, therefore objAddr 
// conforms to CLRDATA_ADDRESS contract.
HRESULT CALLBACK
_EFN_GetManagedObjectFieldInfo(
    PDEBUG_CLIENT client,
    ULONG64 objAddr,
    __out_ecount (mdNameLen) PSTR szFieldName,
    PULONG64 pValue,
    PULONG pOffset
    )
{
    INIT_API();
    DacpObjectData objData;
    LPWSTR fieldName = (LPWSTR)alloca(mdNameLen * sizeof(WCHAR));
    
    if (szFieldName == NULL || *szFieldName == '\0' ||
        objAddr == NULL)
    {
        return E_FAIL;
    }

    if (pOffset == NULL && pValue == NULL)
    {
        // One of these needs to be valid
        return E_FAIL;
    }
        
    if (FAILED(objData.Request(g_sos, objAddr)))
    {        
        return E_FAIL;
    }
    
    MultiByteToWideChar(CP_ACP,0,szFieldName,-1,fieldName,mdNameLen);

    int iOffset = GetObjFieldOffset (objAddr, objData.MethodTable, fieldName);
    if (iOffset <= 0)
    {
        return E_FAIL;
    }

    if (pOffset)
    {
        *pOffset = (ULONG) iOffset;
    }

    if (pValue)
    {
        if (FAILED(g_ExtData->ReadVirtual(UL64_TO_CDA(objAddr + iOffset), pValue, sizeof(ULONG64), NULL)))
        {
            return E_FAIL;
        }
    }

    return S_OK;
}

#ifdef FEATURE_PAL

#ifdef CREATE_DUMP_SUPPORTED
#include <dumpcommon.h>
#include "datatarget.h"
extern bool CreateDumpForSOS(const char* programPath, const char* dumpPathTemplate, pid_t pid, MINIDUMP_TYPE minidumpType, ICLRDataTarget* dataTarget);
extern bool g_diagnostics;
#endif // CREATE_DUMP_SUPPORTED

DECLARE_API(CreateDump)
{
    INIT_API();
#ifdef CREATE_DUMP_SUPPORTED
    StringHolder sFileName;
    BOOL normal = FALSE;
    BOOL withHeap = FALSE;
    BOOL triage = FALSE;
    BOOL full = FALSE;
    BOOL diag = FALSE;

    size_t nArg = 0;
    CMDOption option[] = 
    {   // name, vptr, type, hasValue
        {"-n", &normal, COBOOL, FALSE},
        {"-h", &withHeap, COBOOL, FALSE},
        {"-t", &triage, COBOOL, FALSE},
        {"-f", &full, COBOOL, FALSE},
        {"-d", &diag, COBOOL, FALSE},
    };
    CMDValue arg[] = 
    {   // vptr, type
        {&sFileName.data, COSTRING}
    };
    if (!GetCMDOption(args, option, _countof(option), arg, _countof(arg), &nArg))
    {
        return E_FAIL;
    }
    MINIDUMP_TYPE minidumpType = MiniDumpWithPrivateReadWriteMemory;
    ULONG pid = 0; 
    g_ExtSystem->GetCurrentProcessId(&pid);

    if (full)
    {
        minidumpType = MiniDumpWithFullMemory;
    }
    else if (withHeap)
    {
        minidumpType = MiniDumpWithPrivateReadWriteMemory;
    }
    else if (triage)
    {
        minidumpType = MiniDumpFilterTriage;
    }
    else if (normal)
    {
        minidumpType = MiniDumpNormal;
    }
    g_diagnostics = diag;

    const char* programPath = g_ExtServices->GetCoreClrDirectory();
    const char* dumpPathTemplate = "/tmp/coredump.%d";
    ToRelease<ICLRDataTarget> dataTarget = new DataTarget();
    dataTarget->AddRef();

    if (sFileName.data != nullptr)
    {
        dumpPathTemplate = sFileName.data;
    }
    if (!CreateDumpForSOS(programPath, dumpPathTemplate, pid, minidumpType, dataTarget))
    {
        Status = E_FAIL;
    } 
#else // CREATE_DUMP_SUPPORTED
    ExtErr("CreateDump not supported on this platform\n");
#endif // CREATE_DUMP_SUPPORTED
    return Status;
}

#endif // FEATURE_PAL

void PrintHelp (__in_z LPCSTR pszCmdName)
{
    static LPSTR pText = NULL;

    if (pText == NULL) {
#ifndef FEATURE_PAL
        HGLOBAL hResource = NULL;
        HRSRC hResInfo = FindResource (g_hInstance, TEXT ("DOCUMENTATION"), TEXT ("TEXT"));
        if (hResInfo) hResource = LoadResource (g_hInstance, hResInfo);
        if (hResource) pText = (LPSTR) LockResource (hResource); 
        if (pText == NULL)
        {
            ExtOut("Error loading documentation resource\n");
            return;
        }
#else
        int err = PAL_InitializeDLL();
        if(err != 0)
        {
            ExtOut("Error initializing PAL\n");
            return;
        }
        char lpFilename[MAX_LONGPATH + 12]; // + 12 to make enough room for strcat function.
        strcpy_s(lpFilename, _countof(lpFilename), g_ExtServices->GetCoreClrDirectory());
        strcat_s(lpFilename, _countof(lpFilename), "sosdocsunix.txt");
        
        HANDLE hSosDocFile = CreateFileA(lpFilename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
        if (hSosDocFile == INVALID_HANDLE_VALUE) {
            ExtOut("Error finding documentation file\n");
            return;
        }

        HANDLE hMappedSosDocFile = CreateFileMappingA(hSosDocFile, NULL, PAGE_READONLY, 0, 0, NULL);
        CloseHandle(hSosDocFile);
        if (hMappedSosDocFile == NULL) { 
            ExtOut("Error mapping documentation file\n");
            return;
        }

        pText = (LPSTR)MapViewOfFile(hMappedSosDocFile, FILE_MAP_READ, 0, 0, 0);
        CloseHandle(hMappedSosDocFile);
        if (pText == NULL)
        {
            ExtOut("Error loading documentation file\n");
            return;
        }
#endif
    }

    // Find our line in the text file
    char searchString[MAX_LONGPATH];
    sprintf_s(searchString, _countof(searchString), "COMMAND: %s.", pszCmdName);
    
    LPSTR pStart = strstr(pText, searchString);
    LPSTR pEnd = NULL;
    if (!pStart)
    {
        ExtOut("Documentation for %s not found.\n", pszCmdName);
        return;
    }

    // Go to the end of this line:
    pStart = strchr(pStart, '\n');
    if (!pStart)
    {
        ExtOut("Expected newline in documentation resource.\n");
        return;
    }

    // Bypass the newline that pStart points to and setup pEnd for the loop below. We set
    // pEnd to be the old pStart since we add one to it when we call strstr.
    pEnd = pStart++;

    // Find the first occurrence of \\ followed by an \r or an \n on a line by itself.
    do
    {
        pEnd = strstr(pEnd+1, "\\\\");
    } while (pEnd && ((pEnd[-1] != '\r' && pEnd[-1] != '\n') || (pEnd[3] != '\r' && pEnd[3] != '\n')));

    if (pEnd)
    {
        // We have found a \\ followed by a \r or \n.  Do not print out the character pEnd points
        // to, as this will be the first \ (this is why we don't add one to the second parameter).
        ExtOut("%.*s", pEnd - pStart, pStart);
    }
    else
    {
        // If pEnd is false then we have run to the end of the document.  However, we did find
        // the command to print, so we should simply print to the end of the file.  We'll add
        // an extra newline here in case the file does not contain one.
        ExtOut("%s\n", pStart);
    }
}

/**********************************************************************\
* Routine Description:                                                 *
*                                                                      *
*    This function displays the commands available in strike and the   *  
*    arguments passed into each.
*                                                                      *
\**********************************************************************/
DECLARE_API(Help)
{
    // Call extension initialization functions directly, because we don't need the DAC dll to be initialized to get help.
    HRESULT Status;
    __ExtensionCleanUp __extensionCleanUp;
    if ((Status = ExtQuery(client)) != S_OK) return Status;
    ControlC = FALSE;

    StringHolder commandName;
    CMDValue arg[] = 
    {
        {&commandName.data, COSTRING}
    };
    size_t nArg;
    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg))
    {
        return Status;
    }

    ExtOut("-------------------------------------------------------------------------------\n");

    if (nArg == 1)
    {        
        // Convert commandName to lower-case
        LPSTR curChar = commandName.data;
        while (*curChar != '\0')
        {
            if ( ((unsigned) *curChar <= 0x7F) && isupper(*curChar))
            {
                *curChar = (CHAR) tolower(*curChar);
            }
            curChar++;
        }

        // Strip off leading "!" if the user put that.
        curChar = commandName.data;
        if (*curChar == '!')
            curChar++;
        
        PrintHelp (curChar);
    }
    else
    {
        PrintHelp ("contents");
    }
    
    return S_OK;
}

#if defined(FEATURE_PAL) && defined(_TARGET_AMD64_)

static BOOL 
ReadMemoryAdapter(PVOID address, PVOID buffer, SIZE_T size)
{
    ULONG fetched;
    HRESULT hr = g_ExtData->ReadVirtual(TO_CDADDR(address), buffer, size, &fetched);
    return SUCCEEDED(hr);
}

static BOOL
GetStackFrame(CONTEXT* context, ULONG numNativeFrames)
{
    KNONVOLATILE_CONTEXT_POINTERS contextPointers;
    memset(&contextPointers, 0, sizeof(contextPointers));

    ULONG64 baseAddress;
    HRESULT hr = g_ExtSymbols->GetModuleByOffset(context->Rip, 0, NULL, &baseAddress);
    if (FAILED(hr))
    {
        PDEBUG_STACK_FRAME frame = &g_Frames[0];
        for (unsigned int i = 0; i < numNativeFrames; i++, frame++) {
            if (frame->InstructionOffset == context->Rip)
            {
                if ((i + 1) >= numNativeFrames) {
                    return FALSE;
                }
                memcpy(context, &(g_FrameContexts[i + 1]), sizeof(*context));
                return TRUE;
            }
        }
        return FALSE;
    }
    if (!PAL_VirtualUnwindOutOfProc(context, &contextPointers, baseAddress, ReadMemoryAdapter))
    {
        return FALSE;
    }
    return TRUE;
}

static BOOL
UnwindStackFrames(ULONG32 osThreadId)
{
    ULONG numNativeFrames = 0;
    HRESULT hr = GetContextStackTrace(osThreadId, &numNativeFrames);
    if (FAILED(hr))
    {
        return FALSE;
    }
    CONTEXT context;
    memset(&context, 0, sizeof(context));
    context.ContextFlags = CONTEXT_FULL;

    hr = g_ExtSystem->GetThreadContextById(osThreadId, CONTEXT_FULL, sizeof(context), (PBYTE)&context);
    if (FAILED(hr))
    {
        return FALSE;
    }
    TableOutput out(3, POINTERSIZE_HEX, AlignRight);
    out.WriteRow("RSP", "RIP", "Call Site");

    DEBUG_STACK_FRAME nativeFrame;
    memset(&nativeFrame, 0, sizeof(nativeFrame));

    do 
    {
        if (context.Rip == 0)
        {
            break;
        }
        nativeFrame.InstructionOffset = context.Rip;
        nativeFrame.ReturnOffset = context.Rip;
        nativeFrame.FrameOffset = context.Rbp;
        nativeFrame.StackOffset = context.Rsp;
        ClrStackImpl::PrintNativeStackFrame(out, &nativeFrame, FALSE);

    } while (GetStackFrame(&context, numNativeFrames));

    return TRUE;
}

#endif // FEATURE_PAL && _TARGET_AMD64_
