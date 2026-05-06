// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/debug.h

Abstract:

    Debug API utility functions



--*/

#ifndef _PAL_DEBUG_H_
#define _PAL_DEBUG_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++
Function :
    DBG_DebugBreak

    Processor-dependent implementation of DebugBreak

(no parameters, no return value)
--*/
extern "C" VOID
DBG_DebugBreak();

/*++
Function:
  IsInDebugBreak(addr)

  Returns true if the address is in DBG_DebugBreak.

--*/
BOOL
IsInDebugBreak(void *addr);

/*++
Function :
    DBG_FlushInstructionCache

    Processor-dependent implementation of FlushInstructionCache

Parameters :
    LPCVOID lpBaseAddress: start of region to flush
    SIZE_T dwSize : length of region to flush

Return value :
    TRUE on success, FALSE on failure

--*/
BOOL
DBG_FlushInstructionCache(
                      IN LPCVOID lpBaseAddress,
                      IN SIZE_T dwSize);

#if defined(__APPLE__)
/*++
Function:
    DBG_CheckStackAlignment

    The Apple ABI requires 16-byte alignment on the stack pointer.
    This function traps/interrupts otherwise.
--*/
VOID
DBG_CheckStackAlignment();
#endif


#ifdef __cplusplus
}
#endif // __cplusplus

#endif //PAL_DEBUG_H_
