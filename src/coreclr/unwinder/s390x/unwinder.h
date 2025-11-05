// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef __unwinder_s390x_h__
#define __unwinder_s390x_h__

#include "baseunwinder.h"


//---------------------------------------------------------------------------------------
//
// See the comment for the base class code:OOPStackUnwinder.
//

class OOPStackUnwinderS390X : public OOPStackUnwinder
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
};

#endif // __unwinder_s390x_h__
