// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

#ifndef __unwinder_amd64_h__
#define __unwinder_amd64_h__

#include "unwinder.h"


//---------------------------------------------------------------------------------------
//
// See the comment for the base class code:OOPStackUnwinder.
//

class OOPStackUnwinderAMD64 : public OOPStackUnwinder
{
public:
    // Unwind the given CONTEXT to the caller CONTEXT.  The CONTEXT will be overwritten.  
    static BOOL Unwind(CONTEXT * pContext);

    //
    // Everything below comes from dbghelp.dll.
    //

    static HRESULT VirtualUnwind(__in DWORD HandlerType,
        __in DWORD64 ImageBase,
        __in DWORD64 ControlPc,
        __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
        __inout PCONTEXT ContextRecord,
        __out PVOID *HandlerData,
        __out PDWORD64 EstablisherFrame,
        __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
        __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine);

protected:

    static ULONG UnwindOpSlots(__in UNWIND_CODE UnwindCode);

    static HRESULT UnwindEpilogue(__in ULONG64 ImageBase,
                                  __in ULONG64 ControlPc,
                                  __in ULONG EpilogueOffset,
                                  __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
                                  __inout PCONTEXT ContextRecord,
                                  __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);

    static HRESULT UnwindPrologue(__in DWORD64 ImageBase,
                                  __in DWORD64 ControlPc,
                                  __in DWORD64 FrameBase,
                                  __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
                                  __inout PCONTEXT ContextRecord,
                                  __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
                                  __deref_out _PIMAGE_RUNTIME_FUNCTION_ENTRY *FinalFunctionEntry);

    static _PIMAGE_RUNTIME_FUNCTION_ENTRY LookupPrimaryFunctionEntry
        (__in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
         __in DWORD64 ImageBase);

    static _PIMAGE_RUNTIME_FUNCTION_ENTRY SameFunction
        (__in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
         __in DWORD64 ImageBase,
         __in DWORD64 ControlPc);

    static UNWIND_INFO * GetUnwindInfo(TADDR taUnwindInfo);
};

#endif // __unwinder_amd64_h__

