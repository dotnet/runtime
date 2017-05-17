// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

#ifndef __unwinder_i386_h__
#define __unwinder_i386_h__

#include "unwinder.h"

#ifdef WIN64EXCEPTIONS
//---------------------------------------------------------------------------------------
//
// See the comment for the base class code:OOPStackUnwinder.
//

class OOPStackUnwinderX86 : public OOPStackUnwinder
{
public:
    static BOOL Unwind(T_CONTEXT* pContextRecord, T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers);

    static HRESULT VirtualUnwind(__in DWORD HandlerType,
        __in DWORD ImageBase,
        __in DWORD ControlPc,
        __in _PIMAGE_RUNTIME_FUNCTION_ENTRY FunctionEntry,
        __inout PCONTEXT ContextRecord,
        __out PVOID *HandlerData,
        __out PDWORD EstablisherFrame,
        __inout_opt PKNONVOLATILE_CONTEXT_POINTERS ContextPointers,
        __deref_opt_out_opt PEXCEPTION_ROUTINE *HandlerRoutine);
};
#endif // WIN64EXCEPTIONS

#endif // __unwinder_i386_h__
