// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--== 
/* ---------------------------------------------------------------------------

   SOS_Stacktrace.h

   API exported from SOS.DLL for retrieving managed stack traces.
   This extension function is called through the Windows Debugger extension
   interfaces (dbgeng.h).

   Additional functions exported from SOS are documented here as well.

Notes:

HRESULT CALLBACK _EFN_StackTrace(
    PDEBUG_CLIENT client,
    WCHAR wszTextOut[],
    UINT *puiTextLength,
    LPVOID pTransitionContexts,
    UINT *puiTransitionContextCount,
    UINT uiSizeOfContext);

uiSizeOfContext must be either sizeof(SimpleContext) or sizeof(CONTEXT) for the 
architecture (x86, IA64, x64).

if wszTextOut is NULL and *puiTextLength is non-NULL, the function will return 
the necessary string length in *puiTextLength.

If wszTextOut is non-NULL, the function will fill wszTextOut up to the point 
given by *puiTextLength, returning success if there was enough room in the 
buffer or E_OUTOFMEMORY if the buffer wasn't long enough.

The transition portion of the function will be completely ignored if 
pTransitionContexts and puiTransitionContextCount are both NULL. Some callers
would just like text output of the function names.

If pTransitionContexts is NULL and puiTransitionContextCount is non NULL, the 
function will return the necessary number of context entries in 
*puiTransitionContextCount.

If pTransitionContexts is non NULL, the function will treat it as an array of 
structures of length *puiTransitionContextCount. The structure size is given 
by uiSizeOfContext, and must be the size of SimpleContext or CONTEXT for the 
architecture.

wszTextOut will be written in the following format:

"<ModuleName>!<Function Name>[+<offset in hex>]
...
(TRANSITION)
..."

if the offset in hex is 0, no offset will be written (this matches KB output).

If there is no managed code on the thread currently in context, 
SOS_E_NOMANAGEDCODE will be returned. 
   ------------------------------------------------------------------------ */
#ifndef __STACKTRACE_H
#define __STACKTRACE_H
#include <windows.h>
#include <winerror.h>

#ifndef FACILITY_SOS
#define FACILITY_SOS            0xa0
#endif

#ifndef EMAKEHR
#define EMAKEHR(val)            MAKE_HRESULT(SEVERITY_ERROR, FACILITY_SOS, val)
#endif

// Custom Error returns
#define SOS_E_NOMANAGEDCODE                EMAKEHR(0x1000)     // No managed code on the stack

// Flags
//
// Turn on SOS_STACKTRACE_SHOWADDRESSES to see EBP ESP in front of each 
// module!functionname line. By default this is off.
#define SOS_STACKTRACE_SHOWADDRESSES        0x00000001

struct StackTrace_SimpleContext
{
    ULONG64 StackOffset; // esp on x86
    ULONG64 FrameOffset; // ebp
    ULONG64 InstructionOffset; // eip
};

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

HRESULT CALLBACK _EFN_StackTrace(
    PDEBUG_CLIENT client,
    __out_ecount(*puiTextLength) WCHAR wszTextOut[],
    size_t *puiTextLength,
    LPVOID pTransitionContexts,
    size_t *puiTransitionContextCount,
    size_t uiSizeOfContext,
    DWORD Flags);

/* ---------------------------------------------------------------------------

    Additional functions are exported from SOS, and are useful
    for debugging tasks with managed object pointers.
    
   ------------------------------------------------------------------------ */
   
// _EFN_GetManagedExcepStack - given a managed exception object address, returns a string
//                             version of the stack trace contained inside.
// 
// StackObjAddr - a managed object pointer, must be derived from System.Exception
// szStackString - the string returned (out parameter)
// cbString - number of characters available in the string buffer. 
// 
// The output will be truncated of cbString is not long enough for the full stack trace.
HRESULT _EFN_GetManagedExcepStack(
    PDEBUG_CLIENT client,
    ULONG64 StackObjAddr,
    __out_ecount(cbString) PSTR szStackString,
    ULONG cbString
    );

// _EFN_GetManagedExcepStackW - same as _EFN_GetManagedExcepStack, but returns 
//                              the stack as a wide string.
HRESULT _EFN_GetManagedExcepStackW(
    PDEBUG_CLIENT client,
    ULONG64 StackObjAddr,
    __out_ecount(cchString) PWSTR wszStackString,
    ULONG cchString
    );

// _EFN_GetManagedObjectName - given a managed object pointer, return the type name
//
// objAddr - a managed object pointer
// szName - a buffer to be filled with the full type name
// cbName - the number of characters available in the buffer
//
HRESULT _EFN_GetManagedObjectName(
    PDEBUG_CLIENT client,
    ULONG64 objAddr,
    __out_ecount(cbName) PSTR szName,
    ULONG cbName
    );

// _EFN_GetManagedObjectFieldInfo - given an object pointer and a field name, returns
//                                  the offset to the field from the start of the object,
//                                  and the field's value.
//
// objAddr - a managed object pointer
// szFieldName - the field name you are interested in
// pValue - the field value is written here. This parameter can be NULL.
// pOffset - the offset from objAddr to the field. This parameter can be NULL.
//
// At least one of pValue and pOffset must be non-NULL.
HRESULT _EFN_GetManagedObjectFieldInfo(
    PDEBUG_CLIENT client,
    ULONG64 objAddr,
    __out_ecount (mdNameLen) PSTR szFieldName,
    PULONG64 pValue,
    PULONG pOffset
    );

#ifdef __cplusplus
}
#endif // __cplusplus : extern "C"

#endif // __STACKTRACE_H

