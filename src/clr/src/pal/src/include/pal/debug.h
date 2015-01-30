//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
VOID DBG_DebugBreak();

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
