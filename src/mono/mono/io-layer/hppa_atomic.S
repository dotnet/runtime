/*
    Copyright (c) 2003 Bernie Solomon <bernard@ugsolutions.com>
    
    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:
    
    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.



    Implementation of "atomic" operations for HPPA. Currently (Oct 9th 2003)
    only implemented for 64 bit compiles. There is only one atomic
    instruction LDCW which is used to implement spinlocks. There are
    16 locks which are selected by taking 4 bits out of the address of
    the relevant variable to try to avoid too much contention
    for a single lock.
*/
#include "config.h"

#ifdef __LP64__
#if SIZEOF_VOID_P != 8
#error "__LP64__ state and SIZEOF_VOID_P don't match!!"
#endif
#define EXPORT_ARGS ,NO_RELOCATION,LONG_RETURN
#define CALLINFO_ARGS ,ARGS_SAVED,ORDERING_AWARE
#define RETURN	bve,n (%rp)
#define LDPTR	ldd
#define STPTR	std
#else
#if SIZEOF_VOID_P != 4
#error "__LP64__ state and SIZEOF_VOID_P don't match!!"
#endif
#define CALLINFO_ARGS
#define EXPORT_ARGS
#define RETURN	bv,n (%rp)
#define LDPTR	ldw
#define STPTR	stw
#endif
        .code

 .label	InterlockedIncrement
        .EXPORT InterlockedIncrement,ENTRY,PRIV_LEV=3 EXPORT_ARGS
        .PROC
        .CALLINFO FRAME=0 CALLINFO_ARGS
        .ENTRY
#ifdef PIC
	ADDIL	LT%locks,%r19
	LDPTR	RT%locks(%r1),%r31
#else
        ADDIL   L%locks-$global$,%dp
        LDO     R%locks-$global$(%r1),%r31
#endif
        EXTRU	%arg0,28,4,%r28
        ZDEP	%r28,27,28,%r29
        ADD,L   %r29,%r31,%r31
 .label	atomictest$1
        LDCW    0(%r31),%r29
        CMPB,<> %r0,%r29,gotlock$1
        NOP
 .label	spin$1
        LDW     0(%r31),%r29
        CMPB,=  %r29,%r0,spin$1
        NOP
        B,N     atomictest$1
 .label	gotlock$1
        LDW     0(%arg0),%ret0
        LDI     1,%r29
        LDO     1(%ret0),%ret0
        STW     %ret0,0(%arg0)
        STW     %r29,0(%r31)
        .EXIT
        RETURN
        .PROCEND

 .label	InterlockedDecrement
        .EXPORT InterlockedDecrement,ENTRY,PRIV_LEV=3 EXPORT_ARGS
        .PROC
        .CALLINFO FRAME=0 CALLINFO_ARGS
        .ENTRY
#ifdef PIC
	ADDIL	LT%locks,%r19
	LDPTR	RT%locks(%r1),%r31
#else
        ADDIL   L%locks-$global$,%dp
        LDO     R%locks-$global$(%r1),%r31
#endif
        EXTRU	%arg0,28,4,%r28
        ZDEP	%r28,27,28,%r29
        ADD,L   %r29,%r31,%r31
 .label	atomictest$2
        LDCW    0(%r31),%r29
        CMPB,<>	%r0,%r29,gotlock$2
        NOP
 .label	spin$2
        LDW     0(%r31),%r29
        CMPB,=	%r29,%r0,spin$2
        NOP
        B,N     atomictest$2
 .label	gotlock$2
        LDW     0(%arg0),%ret0
        LDI     1,%r29
        LDO     -1(%ret0),%ret0
        STW     %ret0,0(%arg0)
        STW     %r29,0(%r31)
        .EXIT
        RETURN
        .PROCEND

 .label	InterlockedExchange
        .EXPORT InterlockedExchange,ENTRY,PRIV_LEV=3 EXPORT_ARGS
        .PROC
        .CALLINFO FRAME=0 CALLINFO_ARGS
        .ENTRY
#ifdef PIC
	ADDIL	LT%locks,%r19
	LDPTR	RT%locks(%r1),%r31
#else
        ADDIL   L%locks-$global$,%dp
        LDO     R%locks-$global$(%r1),%r31
#endif
        EXTRU	%arg0,28,4,%r28
        ZDEP	%r28,27,28,%r29
        ADD,L   %r29,%r31,%r31
 .label	atomictest$3
        LDCW    0(%r31),%r29
        CMPB,<> %r0,%r29,gotlock$3
        NOP
 .label	spin$3
        LDW     0(%r31),%r29
        CMPB,=  %r29,%r0,spin$3
        NOP
        B,N     atomictest$3
 .label	gotlock$3
        LDW     0(%arg0),%ret0
        STW     %arg1,0(%arg0)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        RETURN
        .PROCEND

 .label	InterlockedExchangePointer
        .EXPORT InterlockedExchangePointer,ENTRY,PRIV_LEV=3 EXPORT_ARGS
        .PROC
        .CALLINFO FRAME=0 CALLINFO_ARGS
        .ENTRY
#ifdef PIC
	ADDIL	LT%locks,%r19
	LDPTR	RT%locks(%r1),%r31
#else
        ADDIL   L%locks-$global$,%dp
        LDO     R%locks-$global$(%r1),%r31
#endif
        EXTRU	%arg0,28,4,%r28
        ZDEP	%r28,27,28,%r29
        ADD,L   %r29,%r31,%r31
 .label	atomictest$4
        LDCW    0(%r31),%r29
        CMPB,<> %r0,%r29,gotlock$4
        NOP
 .label	spin$4
        LDW     0(%r31),%r29
        CMPB,=  %r29,%r0,spin$4
        NOP
        B,N     atomictest$4
 .label	gotlock$4
        LDPTR   0(%arg0),%ret0
        STPTR   %arg1,0(%arg0)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        RETURN
        .PROCEND

 .label	InterlockedCompareExchange
        .EXPORT InterlockedCompareExchange,ENTRY,PRIV_LEV=3 EXPORT_ARGS
        .PROC
        .CALLINFO FRAME=0 CALLINFO_ARGS
        .ENTRY
#ifdef PIC
	ADDIL	LT%locks,%r19
	LDPTR	RT%locks(%r1),%r31
#else
        ADDIL   L%locks-$global$,%dp
        LDO     R%locks-$global$(%r1),%r31
#endif
        EXTRU	%arg0,28,4,%r28
        ZDEP	%r28,27,28,%r29
        ADD,L   %r29,%r31,%r31
 .label	atomictest$5
        LDCW    0(%r31),%r29
        CMPB,<> %r0,%r29,gotlock$5
        NOP
 .label	spin$5
        LDW     0(%r31),%r29
        CMPB,=  %r29,%r0,spin$5
        NOP
        B,N     atomictest$5
 .label	gotlock$5
        LDW     0(%arg0),%ret0
        sub,<>	%ret0,%r24,%r0
        STW     %arg1,0(%arg0)		/* not done if %ret0 <> %r24 */
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        RETURN
        .PROCEND

 .label	InterlockedCompareExchangePointer
        .EXPORT InterlockedCompareExchangePointer,ENTRY,PRIV_LEV=3 EXPORT_ARGS
        .PROC
        .CALLINFO FRAME=0 CALLINFO_ARGS
        .ENTRY
#ifdef PIC
	ADDIL	LT%locks,%r19
	LDPTR	RT%locks(%r1),%r31
#else
        ADDIL   L%locks-$global$,%dp
        LDO     R%locks-$global$(%r1),%r31
#endif
        EXTRU	%arg0,28,4,%r28
        ZDEP	%r28,27,28,%r29
        ADD,L   %r29,%r31,%r31
 .label	atomictest$6
        LDCW    0(%r31),%r29
        CMPB,<>	%r0,%r29,gotlock$6
        NOP
 .label	spin$6
        LDW     0(%r31),%r29
        CMPB,=  %r29,%r0,spin$6
        NOP
        B,N     atomictest$6
 .label	gotlock$6
        LDPTR   0(%arg0),%ret0
        sub,<>	%ret0,%r24,%r0
        STPTR   %arg1,0(%arg0)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        RETURN
        .PROCEND


        .data
 .label	locks
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .ALIGN  16
        .STRING "\x00\x00\x00\x01"
        .IMPORT $global$,DATA
        .END
