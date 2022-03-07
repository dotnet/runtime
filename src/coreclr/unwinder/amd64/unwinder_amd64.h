// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    static HRESULT VirtualUnwind(_In_ DWORD HandlerType,
        _In_ DWORD64 ImageBase,
        _In_ DWORD64 ControlPc,
        _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
        __inout PCONTEXT ContextRecord,
        _Out_ PVOID *HandlerData,
        _Out_ PDWORD64 EstablisherFrame,
        __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
        _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine);

protected:

    static ULONG UnwindOpSlots(_In_ UNWIND_CODE UnwindCode);

    static HRESULT UnwindEpilogue(_In_ ULONG64 ImageBase,
                                  _In_ ULONG64 ControlPc,
                                  _In_ ULONG EpilogueOffset,
                                  _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
                                  __inout PCONTEXT ContextRecord,
                                  __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);

    static HRESULT UnwindPrologue(_In_ DWORD64 ImageBase,
                                  _In_ DWORD64 ControlPc,
                                  _In_ DWORD64 FrameBase,
                                  _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
                                  __inout PCONTEXT ContextRecord,
                                  __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
                                  _Outptr_ _PIMAGE_RUNTIME_FUNCTION_ENTRY *FinalFunctionEntry);

    static _PIMAGE_RUNTIME_FUNCTION_ENTRY LookupPrimaryFunctionEntry
        (_In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
         _In_ DWORD64 ImageBase);

    static _PIMAGE_RUNTIME_FUNCTION_ENTRY SameFunction
        (_In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
         _In_ DWORD64 ImageBase,
         _In_ DWORD64 ControlPc);

    static UNWIND_INFO * GetUnwindInfo(TADDR taUnwindInfo);
};

#endif // __unwinder_amd64_h__

