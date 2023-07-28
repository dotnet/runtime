// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef __unwinder_i386_h__
#define __unwinder_i386_h__

#include "baseunwinder.h"

#ifdef FEATURE_EH_FUNCLETS
//---------------------------------------------------------------------------------------
//
// See the comment for the base class code:OOPStackUnwinder.
//

class OOPStackUnwinderX86 : public OOPStackUnwinder
{
public:
    static BOOL Unwind(T_CONTEXT* pContextRecord, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers);

    static HRESULT VirtualUnwind(_In_ DWORD HandlerType,
        _In_ DWORD ImageBase,
        _In_ DWORD ControlPc,
        _In_ _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
        __inout PCONTEXT ContextRecord,
        _Out_ PVOID *HandlerData,
        _Out_ PDWORD EstablisherFrame,
        __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
        _Outptr_opt_result_maybenull_ PEXCEPTION_ROUTINE *HandlerRoutine);
};
#endif // FEATURE_EH_FUNCLETS

#endif // __unwinder_i386_h__
