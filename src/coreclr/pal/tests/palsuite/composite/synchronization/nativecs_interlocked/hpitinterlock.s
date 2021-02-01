// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    interlock.s

Abstract:

    Implementation of Interlocked functions (32 and 64 bits) for the HPUX/Itanium
    platform. These functions are processor dependent.
    See the i386 implementations for more information.

--*/
    .file   "interlock.s"
    .section    .text,  "ax",   "progbits"
    .align 16
    .global InterlockedExchangeAdd#
    .proc InterlockedExchangeAdd#
InterlockedExchangeAdd:
    .body
    ld4.nt1 r8 = [r32]
    ;;
Iea10:    
    mov ar.ccv = r8    
    add r15 = r33, r8
    mov r14 = r8
    ;;
    cmpxchg4.acq r8 = [r32], r15, ar.ccv
    ;;
    cmp.ne p6,p7 = r8, r14     // check if the target changes?
(p6)br.cond.spnt.few Iea10     // if yes, go back to do it again       
(p7)br.ret.sptk.clr b0
    ;;
    .endp InterlockedExchangeAdd#
        
    .align 16
    .global InterlockedIncrement#
    .proc InterlockedIncrement#
InterlockedIncrement:
    .body
    fetchadd4.acq r8 = [r32], 1
    ;;        
    adds r8 = 1, r8
    br.ret.sptk b0
    ;;
    .endp InterlockedIncrement#
    
    .align 16
    .global InterlockedIncrement64#
    .proc InterlockedIncrement64#
InterlockedIncrement64:
    .body
    fetchadd8.acq r8 = [r32], 1
    ;;        
    adds r8 = 1, r8
    br.ret.sptk b0
    ;;
    .endp InterlockedIncrement64#    
    
    .align 16
    .global InterlockedDecrement#
    .proc InterlockedDecrement#
InterlockedDecrement:
    .body
    fetchadd4.acq r8 = [r32], -1
    ;;
    adds r8 = -1, r8
    br.ret.sptk b0
    ;;
    .endp InterlockedDecrement#
    
    .align 16
    .global InterlockedDecrement64#
    .proc InterlockedDecrement64#
InterlockedDecrement64:
    .body
    fetchadd8.acq r8 = [r32], -1
    ;;
    adds r8 = -1, r8
    br.ret.sptk b0
    ;;
    .endp InterlockedDecrement64#    
    
    .align 16
    .global InterlockedExchange#
    .proc InterlockedExchange#
InterlockedExchange:
    .body
    mf
    zxt4 r33 = r33        // sanitize the upper 32 bits    
    ;;    
    xchg4 r8 = [r32], r33
    br.ret.sptk b0
    ;;
    .endp InterlockedExchange#
    
    .align 16
    .global InterlockedExchange64#
    .proc InterlockedExchange64#
InterlockedExchange64:
    .body
    mf
    xchg8 r8 = [r32], r33
    br.ret.sptk b0
    ;;
    .endp InterlockedExchange64#    
    
    .align 16
    .global InterlockedCompareExchange#
    .proc InterlockedCompareExchange#
InterlockedCompareExchange:
    .body
    mf
    zxt4 r33 = r33        // sanitize the upper 32 bits    
    zxt4 r34 = r34        // sanitize the upper 32 bits
    ;;    
    mov ar.ccv = r34
    ;;
    cmpxchg4.acq r8 = [r32], r33, ar.ccv
    br.ret.sptk.clr b0
    ;;
    .endp InterlockedCompareExchange#
    
    .align 16
    .global InterlockedCompareExchange64#
    .proc InterlockedCompareExchange64#
InterlockedCompareExchange64:
    .body
    mf
    mov ar.ccv = r34
    ;;
    cmpxchg8.acq r8 = [r32], r33, ar.ccv
    br.ret.sptk.clr b0
    ;;
    .endp InterlockedCompareExchange64#

/*++
    DBG_DebugBreak is extracted from DbgBreakPoint function
    in debugstb.s from win64.
--*/    
    BREAKPOINT_STOP = 0x80016
    .align 16
    .global DBG_DebugBreak#
    .proc DBG_DebugBreak#
DBG_DebugBreak:
    .body
    flushrs
    ;;
    break.i BREAKPOINT_STOP
    br.ret.sptk.clr b0
    ;;
    .endp DBG_DebugBreak#    
    
    .align 16
    .global MemoryBarrier#
    .proc MemoryBarrier#
MemoryBarrier:
    .body
    mf
    br.ret.sptk.clr b0
    ;;
    .endp MemoryBarrier#                        
