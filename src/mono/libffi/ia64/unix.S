/* -----------------------------------------------------------------------
   unix.S - Copyright (c) 1998 Cygnus Solutions
            Copyright (c) 2000 Hewlett Packard Company
   
   IA64/unix Foreign Function Interface 

   Primary author: Hans Boehm, HP Labs

   Loosely modeled on Cygnus code for other platforms.

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
   IN NO EVENT SHALL CYGNUS SOLUTIONS BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */

#define LIBFFI_ASM	
#include <ffi.h>
#include "ia64_flags.h"

/* parameters:	*/
#define callback	in0
#define ecifp		in1
#define bytes		in2
#define flags		in3
#define raddr		in4
#define fn		in5

#define FLOAT_SZ	8 /* in-memory size of fp operands	*/

.text
        .align 16
        .global ffi_call_unix#
        .proc ffi_call_unix#
ffi_call_unix:
	alloc   loc0=ar.pfs,6,5,8,0
	mov 	loc1=b0;
	sub	sp=sp,bytes
	mov	loc4=r1		/* Save gp 	*/
	ld8	r8=[callback],8	/* code address of callback	*/
	;;
	mov 	out0=sp
	mov	out1=ecifp
	mov	out2=bytes
	ld8	r1=[callback]	/* Set up gp for callback.  Unnecessary? */
	mov	b6=r8
	;;
	br.call.sptk.many b0 = b6	/* call ffi_prep_args		*/
	cmp.eq	p6,p0=0,r8		/* r8 nonzero ==> need fp regs	*/
 	;;
(p6)	add	loc2=32+8*FLOAT_SZ,sp
(p6)	br.cond.dptk.many	fp_done
	;;	/* Quiets warning; needed?	*/
	add	loc2=32,sp
	add	loc3=32+FLOAT_SZ,sp
	;;
	ldfd	f8=[loc2],2*FLOAT_SZ
	ldfd	f9=[loc3],2*FLOAT_SZ
	;;
	ldfd	f10=[loc2],2*FLOAT_SZ
	ldfd	f11=[loc3],2*FLOAT_SZ
	;;
	ldfd	f12=[loc2],2*FLOAT_SZ
	ldfd	f13=[loc3],2*FLOAT_SZ
	;;
	ldfd	f14=[loc2],2*FLOAT_SZ
	ldfd	f15=[loc3]
fp_done:
	add	r9=16,sp	/* Pointer to r8_contents	*/
	/* loc2 points at first integer register value.  */
	add	loc3=8,loc2
	;;
	ld8	r8=[r9]		/* Just in case we return large struct */
	ld8	out0=[loc2],16
	ld8	out1=[loc3],16
	;;
	ld8	out2=[loc2],16
	ld8	out3=[loc3],16
	;;
	ld8	out4=[loc2],16
	ld8	out5=[loc3],16
	;;
	ld8	out6=[loc2],16
	ld8	out7=[loc3]
	/* loc2 points at first stack parameter.  Set sp to 16 bytes	*/
	/* below that.							*/
	add	sp=-16,loc2
	
	ld8 	r8=[fn],8
	;;
	ld8	r1=[fn]		/* Set up gp */
	mov	b6=r8;;
	br.call.sptk.many b0 = b6	/* call ffi_prep_args	*/
	
	/* Handle return value. */
	cmp.eq	p6,p0=0,raddr
	cmp.eq	p7,p0=FFI_TYPE_INT,flags
	cmp.eq	p10,p0=FFI_IS_SMALL_STRUCT2,flags
	cmp.eq	p11,p0=FFI_IS_SMALL_STRUCT3,flags
	cmp.eq	p12,p0=FFI_IS_SMALL_STRUCT4,flags
	;;
(p6) 	br.cond.dpnt.few done		/* Dont copy ret values if raddr = 0 */
(p7)	br.cond.dptk.few copy1
(p10)	br.cond.dpnt.few copy2
(p11)	br.cond.dpnt.few copy3
(p12)	br.cond.dpnt.few copy4
	cmp.eq	p8,p0=FFI_TYPE_FLOAT,flags
	cmp.eq	p9,p0=FFI_TYPE_DOUBLE,flags
	tbit.nz	p6,p0=flags,FLOAT_FP_AGGREGATE_BIT
	tbit.nz	p7,p0=flags,DOUBLE_FP_AGGREGATE_BIT
	;;
(p8)	stfs	[raddr]=f8
(p9)	stfd	[raddr]=f8
	;;
(p6)	br.cond.dpnt.few handle_float_hfa
(p7)	br.cond.dpnt.few handle_double_hfa
	br done

copy4:
	add	loc3=24,raddr
	;;
	st8	[loc3]=r11
copy3:
	add	loc3=16,raddr
	;;
	st8	[loc3]=r10
copy2:
	add	loc3=8,raddr
	;;
	st8	[loc3]=r9
copy1:
	st8	[raddr]=r8
	/* In the big struct case, raddr was passed as an argument.	*/
	/* In the void case there was nothing to do.			*/

done:
	mov	r1=loc4		/* Restore gp	*/
	mov	ar.pfs = loc0
	mov	b0 = loc1
	br.ret.sptk.many b0

handle_double_hfa:
	/* Homogeneous floating point array of doubles is returned in	*/
	/* registers f8-f15.  Save one at a time to return area.	*/
	and	flags=0xf,flags	/* Retrieve size	*/
	;;
	cmp.eq	p6,p0=2,flags
	cmp.eq	p7,p0=3,flags
	cmp.eq	p8,p0=4,flags
	cmp.eq	p9,p0=5,flags
	cmp.eq	p10,p0=6,flags
	cmp.eq	p11,p0=7,flags
	cmp.eq	p12,p0=8,flags
	;;
(p6)	br.cond.dptk.few	dhfa2
(p7)	br.cond.dptk.few	dhfa3
(p8)	br.cond.dptk.few	dhfa4
(p9)	br.cond.dptk.few	dhfa5
(p10)	br.cond.dptk.few	dhfa6
(p11)	br.cond.dptk.few	dhfa7
dhfa8:	add 	loc3=7*8,raddr
	;;
	stfd	[loc3]=f15
dhfa7:	add 	loc3=6*8,raddr
	;;
	stfd	[loc3]=f14
dhfa6:	add 	loc3=5*8,raddr
	;;
	stfd	[loc3]=f13
dhfa5:	add 	loc3=4*8,raddr
	;;
	stfd	[loc3]=f12
dhfa4:	add 	loc3=3*8,raddr
	;;
	stfd	[loc3]=f11
dhfa3:	add 	loc3=2*8,raddr
	;;
	stfd	[loc3]=f10
dhfa2:	add 	loc3=1*8,raddr
	;;
	stfd	[loc3]=f9
	stfd	[raddr]=f8
	br	done

handle_float_hfa:
	/* Homogeneous floating point array of floats is returned in	*/
	/* registers f8-f15.  Save one at a time to return area.	*/
	and	flags=0xf,flags	/* Retrieve size	*/
	;;
	cmp.eq	p6,p0=2,flags
	cmp.eq	p7,p0=3,flags
	cmp.eq	p8,p0=4,flags
	cmp.eq	p9,p0=5,flags
	cmp.eq	p10,p0=6,flags
	cmp.eq	p11,p0=7,flags
	cmp.eq	p12,p0=8,flags
	;;
(p6)	br.cond.dptk.few	shfa2
(p7)	br.cond.dptk.few	shfa3
(p8)	br.cond.dptk.few	shfa4
(p9)	br.cond.dptk.few	shfa5
(p10)	br.cond.dptk.few	shfa6
(p11)	br.cond.dptk.few	shfa7
shfa8:	add 	loc3=7*4,raddr
	;;
	stfd	[loc3]=f15
shfa7:	add 	loc3=6*4,raddr
	;;
	stfd	[loc3]=f14
shfa6:	add 	loc3=5*4,raddr
	;;
	stfd	[loc3]=f13
shfa5:	add 	loc3=4*4,raddr
	;;
	stfd	[loc3]=f12
shfa4:	add 	loc3=3*4,raddr
	;;
	stfd	[loc3]=f11
shfa3:	add 	loc3=2*4,raddr
	;;
	stfd	[loc3]=f10
shfa2:	add 	loc3=1*4,raddr
	;;
	stfd	[loc3]=f9
	stfd	[raddr]=f8
	br	done

        .endp ffi_call_unix


.text
        .align 16
        .global ffi_closure_UNIX
        .proc ffi_closure_UNIX
ffi_closure_UNIX:
	alloc   loc0=ar.pfs,8,2,2,0
	mov	loc1=b0
	/* Retrieve closure pointer and real gp.	*/
	mov	out0=gp
	add	gp=16,gp
	;;
	ld8	gp=[gp]
	/* Reserve a structia64_args on the stack such that arguments	*/
	/* past the first 8 are automatically placed in the right	*/
	/* slot.  Note that when we start the sp points at 2 8-byte	*/
	/* scratch words, followed by the extra arguments.		*/
#	define BASIC_ARGS_SZ (8*FLOAT_SZ+8*8+2*8)
#	define FIRST_FP_OFFSET (4*8)
	add	r14=-(BASIC_ARGS_SZ-FIRST_FP_OFFSET),sp
	add	r15=-(BASIC_ARGS_SZ-FIRST_FP_OFFSET-FLOAT_SZ),sp
	add	sp=-BASIC_ARGS_SZ,sp
	/* r14 points to fp_regs[0], r15 points to fp_regs[1]	*/
	;;
	stfd	[r14]=f8,2*FLOAT_SZ
	stfd	[r15]=f9,2*FLOAT_SZ
	;;
	stfd	[r14]=f10,2*FLOAT_SZ
	stfd	[r15]=f11,2*FLOAT_SZ
	;;
	stfd	[r14]=f12,2*FLOAT_SZ
	stfd	[r15]=f13,2*FLOAT_SZ
	;;
	stfd	[r14]=f14,FLOAT_SZ+8
	stfd	[r15]=f15,2*8
	;;
	/* r14 points to first parameter register area, r15 to second. */
	st8	[r14]=in0,2*8
	st8	[r15]=in1,2*8
	;;
	st8	[r14]=in2,2*8
	st8	[r15]=in3,2*8
	;;
	st8	[r14]=in4,2*8
	st8	[r15]=in5,2*8
	;;
	st8	[r14]=in6,2*8
	st8	[r15]=in7,2*8
	/* Call ffi_closure_UNIX_inner */
	mov	out1=sp
	br.call.sptk.many b0=ffi_closure_UNIX_inner
	;;
	mov	b0=loc1
	mov 	ar.pfs=loc0
	br.ret.sptk.many b0
	.endp ffi_closure_UNIX
	

