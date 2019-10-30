// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



	Module Name:

	    interlock.s

Abstract:

	    Implementation of Interlocked functions for the SPARC
	    platform. These functions are processor dependent.
	    See the i386 implementations for more information.

	--*/

	// A handy macro for declaring a public function
	// The first argument is the function name.
	#define ASMFUNC(n,typename); \
	    .align 4		;                \
	    .global n		;               \
	    .type n,typename	;        \
n:				;

	.text
	ASMFUNC(InterlockedIncrement,#function)
	        ld [%o0], %o1
loopI:
	        mov  %o1, %o2
	        add  %o1, 1, %o1
	        cas [%o0], %o2, %o1
	        cmp %o2, %o1
	        bne loopI
	        nop
	        retl
	        add %o1, 1, %o0


	ASMFUNC(InterlockedDecrement,#function)
	        ld [%o0], %o1
loopD:
	        mov  %o1, %o2
	        sub  %o1, 1, %o1
	        cas [%o0], %o2, %o1
	        cmp %o2, %o1
	        bne loopD
	        nop
	        retl
	        sub  %o1, 1, %o0


	ASMFUNC(InterlockedExchange,#function)
	        swap [%o0], %o1
	        retl
	        mov %o1, %o0

	ASMFUNC(InterlockedCompareExchange,#function)
	        cas [%o0], %o2, %o1
	        retl
	        mov %o1, %o0

	ASMFUNC(MemoryBarrier,#function)
	        // ROTORTODO:	 SPARC
	        retl
	        nop

	ASMFUNC(YieldProcessor,#function)
	        retl
	        nop
