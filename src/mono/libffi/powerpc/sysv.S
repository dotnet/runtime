/* -----------------------------------------------------------------------
   sysv.h - Copyright (c) 1998 Geoffrey Keating
   
   PowerPC Assembly glue.

   $Id: sysv.S,v 1.1 2001/08/02 19:31:55 orph Exp $

   Permission is hereby granted, free of charge, to any person obtaining
   a copy of this software and associated documentation files (the
   ``Software''), to deal in the Software without restriction, including
   without limitation the rights to use, copy, modify, merge, publish,
   distribute, sublicense, and/or sell copies of the Software, and to
   permit persons to whom the Software is furnished to do so, subject to
   the following conditions:

   The above copyright notice and this permission notice shall be included
   in all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
   IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */

#define LIBFFI_ASM	
#include <ffi.h>
#include <powerpc/asm.h>

	.globl ffi_prep_args
ENTRY(ffi_call_SYSV)
	/* Save the old stack pointer as AP.  */
	mr	%r8,%r1

	/* Allocate the stack space we need.  */
	stwux	%r1,%r1,%r4
	/* Save registers we use.  */
	mflr	%r9
	stw	%r28,-16(%r8)
	stw	%r29,-12(%r8)
	stw	%r30, -8(%r8)
	stw	%r31, -4(%r8)
	stw	%r9,   4(%r8)

	/* Save arguments over call...  */
	mr	%r31,%r5	/* flags, */
	mr	%r30,%r6	/* rvalue, */
	mr	%r29,%r7	/* function address, */
	mr	%r28,%r8	/* our AP. */

	/* Call ffi_prep_args.  */
	mr	%r4,%r1
	bl	JUMPTARGET(ffi_prep_args)

	/* Now do the call.  */
	/* Set up cr1 with bits 4-7 of the flags.  */
	mtcrf	0x40,%r31
	/* Get the address to call into CTR.  */
	mtctr	%r29
	/* Load all those argument registers.  */
	lwz	%r3,-16-(8*4)(%r28)
	lwz	%r4,-16-(7*4)(%r28)
	lwz	%r5,-16-(6*4)(%r28)
	lwz	%r6,-16-(5*4)(%r28)
	bf-	5,1f
	nop
	lwz	%r7,-16-(4*4)(%r28)
	lwz	%r8,-16-(3*4)(%r28)
	lwz	%r9,-16-(2*4)(%r28)
	lwz	%r10,-16-(1*4)(%r28)
	nop
1:

	/* Load all the FP registers.  */
	bf-	6,2f
	lfd	%f1,-16-(8*4)-(8*8)(%r28)
	lfd	%f2,-16-(8*4)-(7*8)(%r28)
	lfd	%f3,-16-(8*4)-(6*8)(%r28)
	lfd	%f4,-16-(8*4)-(5*8)(%r28)
	nop
	lfd	%f5,-16-(8*4)-(4*8)(%r28)
	lfd	%f6,-16-(8*4)-(3*8)(%r28)
	lfd	%f7,-16-(8*4)-(2*8)(%r28)
	lfd	%f8,-16-(8*4)-(1*8)(%r28)
2:

	/* Make the call.  */
	bctrl

	/* Now, deal with the return value.  */
	mtcrf	0x01,%r31
	bt-	30,L(done_return_value)
	bt-	29,L(fp_return_value)
	stw	%r3,0(%r30)
	bf+	28,L(done_return_value)
	stw	%r4,4(%r30)
	/* Fall through...  */

L(done_return_value):
	/* Restore the registers we used and return.  */
	lwz	%r9,   4(%r28)
	lwz	%r31, -4(%r28)
	mtlr	%r9
	lwz	%r30, -8(%r28)
	lwz	%r29,-12(%r28)
	lwz	%r28,-16(%r28)
	lwz	%r1,0(%r1)
	blr

L(fp_return_value):
	bf	28,L(float_return_value)
	stfd	%f1,0(%r30)
	b	L(done_return_value)
L(float_return_value):
	stfs	%f1,0(%r30)
	b	L(done_return_value)
END(ffi_call_SYSV)
