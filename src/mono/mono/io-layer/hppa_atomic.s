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
#if SIZEOF_VOID_P != 8
#error "Currently only supports 64 bit pointers"
#endif

        .SPACE  $TEXT$,SORT=8
        .SUBSPA $CODE$,QUAD=0,ALIGN=8,ACCESS=0x2c,CODE_ONLY,SORT=24
InterlockedIncrement
        .EXPORT InterlockedIncrement,ENTRY,PRIV_LEV=3,NO_RELOCATION,LONG_RETURN
        .PROC
        .CALLINFO FRAME=0,ARGS_SAVED,ORDERING_AWARE
        .ENTRY
        ADDIL   L'locks-$global$,%r27,%r1
        LDO     R'locks-$global$(%r1),%r31
        EXTRD,U %r26,60,4,%r28
        DEPD,Z  %r28,59,60,%r29
        ADD,L   %r29,%r31,%r31
atomictest$1
        LDCW    0(%r31),%r29
        CMPB,<>,N        %r0,%r29,gotlock$1
        NOP
spin$1
        LDW     0(%r31),%r29
        CMPB,=,N       %r29,%r0,spin$1
        NOP
        B,N     atomictest$1
gotlock$1
        LDW     0(%r26),%r28
        LDO     1(%r28),%r28
        STW     %r28,0(%r26)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        BVE,N   (%r2)
        .PROCEND

InterlockedDecrement
        .EXPORT InterlockedDecrement,ENTRY,PRIV_LEV=3,NO_RELOCATION,LONG_RETURN
        .PROC
        .CALLINFO FRAME=0,ARGS_SAVED,ORDERING_AWARE
        .ENTRY
        ADDIL   L'locks-$global$,%r27,%r1
        LDO     R'locks-$global$(%r1),%r31
        EXTRD,U %r26,60,4,%r28
        DEPD,Z  %r28,59,60,%r29
        ADD,L   %r29,%r31,%r31
atomictest$2
        LDCW    0(%r31),%r29
        CMPB,<>,N        %r0,%r29,gotlock$2
        NOP
spin$2
        LDW     0(%r31),%r29
        CMPB,=,N       %r29,%r0,spin$2
        NOP
        B,N     atomictest$2
gotlock$2
        LDW     0(%r26),%r28
        LDO     -1(%r28),%r28
        STW     %r28,0(%r26)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        BVE,N   (%r2)
        .PROCEND

InterlockedExchange
        .EXPORT InterlockedExchange,ENTRY,PRIV_LEV=3,NO_RELOCATION,LONG_RETURN
        .PROC
        .CALLINFO FRAME=0,ARGS_SAVED,ORDERING_AWARE
        .ENTRY
        ADDIL   L'locks-$global$,%r27,%r1
        LDO     R'locks-$global$(%r1),%r31
        EXTRD,U %r26,60,4,%r28
        DEPD,Z  %r28,59,60,%r29
        ADD,L   %r29,%r31,%r31
atomictest$3
        LDCW    0(%r31),%r29
        CMPB,<>,N        %r0,%r29,gotlock$3
        NOP
spin$3
        LDW     0(%r31),%r29
        CMPB,=,N       %r29,%r0,spin$3
        NOP
        B,N     atomictest$3
gotlock$3
        LDW     0(%r26),%r28
        STW     %r25,0(%r26)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        BVE,N   (%r2)
        .PROCEND

InterlockedExchangePointer
        .EXPORT InterlockedExchangePointer,ENTRY,PRIV_LEV=3,NO_RELOCATION,LONG_RETURN
        .PROC
        .CALLINFO FRAME=0,ARGS_SAVED,ORDERING_AWARE
        .ENTRY
        ADDIL   L'locks-$global$,%r27,%r1
        LDO     R'locks-$global$(%r1),%r31
        EXTRD,U %r26,60,4,%r28
        DEPD,Z  %r28,59,60,%r29
        ADD,L   %r29,%r31,%r31
atomictest$4
        LDCW    0(%r31),%r29
        CMPB,<>,N        %r0,%r29,gotlock$4
        NOP
spin$4
        LDW     0(%r31),%r29
        CMPB,=,N       %r29,%r0,spin$4
        NOP
        B,N     atomictest$4
gotlock$4
        LDD     0(%r26),%r28
        STD     %r25,0(%r26)
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        BVE,N   (%r2)
        .PROCEND

InterlockedCompareExchange
        .EXPORT InterlockedCompareExchange,ENTRY,PRIV_LEV=3,NO_RELOCATION,LONG_RETURN
        .PROC
        .CALLINFO FRAME=0,ARGS_SAVED,ORDERING_AWARE
        .ENTRY
        ADDIL   L'locks-$global$,%r27,%r1
        LDO     R'locks-$global$(%r1),%r31
        EXTRD,U %r26,60,4,%r28
        DEPD,Z  %r28,59,60,%r29
        ADD,L   %r29,%r31,%r31
atomictest$5
        LDCW    0(%r31),%r29
        CMPB,<>,N        %r0,%r29,gotlock$5
        NOP
spin$5
        LDW     0(%r31),%r29
        CMPB,=,N       %r29,%r0,spin$5
        NOP
        B,N     atomictest$5
gotlock$5
        LDW     0(%r26),%r28
        CMPB,<> %r28,%r24,noexchange$5
        NOP
        STW     %r25,0(%r26)
noexchange$5
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        BVE,N   (%r2)
        .PROCEND

InterlockedCompareExchangePointer
        .EXPORT InterlockedCompareExchangePointer,ENTRY,PRIV_LEV=3,NO_RELOCATION,LONG_RETURN
        .PROC
        .CALLINFO FRAME=0,ARGS_SAVED,ORDERING_AWARE
        .ENTRY
        ADDIL   L'locks-$global$,%r27,%r1
        LDO     R'locks-$global$(%r1),%r31
        EXTRD,U %r26,60,4,%r28
        DEPD,Z  %r28,59,60,%r29
        ADD,L   %r29,%r31,%r31
atomictest$6
        LDCW    0(%r31),%r29
        CMPB,<>,N        %r0,%r29,gotlock$6
        NOP
spin$6
        LDW     0(%r31),%r29
        CMPB,=,N       %r29,%r0,spin$6
        NOP
        B,N     atomictest$6
gotlock$6
        LDD     0(%r26),%r28
        CMPB,*<> %r28,%r24,noexchange$6
        NOP
        STD     %r25,0(%r26)
noexchange$6
        LDI     1,%r29
        STW     %r29,0(%r31)
        .EXIT
        BVE,N   (%r2)
        .PROCEND


        .SPACE  $PRIVATE$,SORT=16
        .SUBSPA $SHORTDATA$,QUAD=1,ALIGN=8,ACCESS=0x1f,SORT=24
locks
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
