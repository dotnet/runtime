/* -----------------------------------------------------------------------
   o32.S - Copyright (c) 1996, 1998, 2001  Red Hat, Inc.
   
   MIPS Foreign Function Interface 

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
#include <ffi_private.h>
#include <mips/mips.h>

/* Only build this code if we are compiling for o32 */	

#if defined(FFI_MIPS_O32)
	
#define callback a0
#define bytes	 a2
#define flags	 a3
		
#define SIZEOF_FRAME	( 4 * SIZEOF_ARG + 2 * SIZEOF_ARG )

	.text
	.align	2
	.globl	ffi_call_O32
	.ent	ffi_call_O32
ffi_call_O32:	

	# Prologue
	SUBU	$sp, SIZEOF_FRAME			# Frame size
	REG_S	$fp, SIZEOF_FRAME - 2*SIZEOF_ARG($sp)	# Save frame pointer
	REG_S	ra, SIZEOF_FRAME - 1*SIZEOF_ARG($sp)	# Save return address
	move	$fp, $sp

	move	t9, callback	# callback function pointer
	REG_S	flags, SIZEOF_FRAME + 3*SIZEOF_ARG($fp) # flags

	# Allocate at least 4 words in the argstack
	move	v0, bytes
	bge	bytes, 4 * SIZEOF_ARG, bigger	
	LI	v0, 4 * SIZEOF_ARG
	b	sixteen

bigger:	
	ADDU	t0, v0, 2 * SIZEOF_ARG -1	# make sure it is aligned 
	and	v0, t0, -2 * SIZEOF_ARG		# to an 8 byte boundry

sixteen:
	SUBU	$sp, $sp, v0	# move the stack pointer to reflect the
				# arg space

	ADDU	a0, $sp, 4 * SIZEOF_ARG
	ADDU	a3, $fp, SIZEOF_FRAME + 3*SIZEOF_ARG

	jal	t9
	
	REG_L	t0, SIZEOF_FRAME + 3*SIZEOF_ARG($fp)  # load the flags word
	add	t2, t0, 0			   # and copy it into t2

	and     t0, ((1<<4)-1)          # mask out the return type
	SRL	t2, 4			# shift our arg info
		
	ADDU	$sp, $sp, 4 * SIZEOF_ARG	# adjust $sp to new args

	bnez	t0, pass_d			# make it quick for int
	REG_L	a0, 0*SIZEOF_ARG($sp)		# just go ahead and load the
	REG_L	a1, 1*SIZEOF_ARG($sp)		# four regs.
	REG_L	a2, 2*SIZEOF_ARG($sp)
	REG_L	a3, 3*SIZEOF_ARG($sp)
	b	call_it

pass_d:
	bne	t0, FFI_ARGS_D, pass_f
	l.d	$f12, 0*SIZEOF_ARG($sp)	# load $fp regs from args
	REG_L	a2,   2*SIZEOF_ARG($sp)	# passing a double
	REG_L	a3,   3*SIZEOF_ARG($sp)
	b	call_it

pass_f:	
	bne	t0, FFI_ARGS_F, pass_d_d
	l.s	$f12, 0*SIZEOF_ARG($sp)	# load $fp regs from args
	REG_L	a1,   1*SIZEOF_ARG($sp)	# passing a float
	REG_L	a2,   2*SIZEOF_ARG($sp)
	REG_L	a3,   3*SIZEOF_ARG($sp)
	b	call_it		

pass_d_d:		
	bne	t0, FFI_ARGS_DD, pass_f_f
	l.d	$f12, 0*SIZEOF_ARG($sp)	# load $fp regs from args
	l.d	$f14, 2*SIZEOF_ARG($sp)	# passing two doubles
	b	call_it

pass_f_f:	
	bne	t0, FFI_ARGS_FF, pass_d_f
	l.s	$f12, 0*SIZEOF_ARG($sp)	# load $fp regs from args
	l.s	$f14, 1*SIZEOF_ARG($sp)	# passing two floats
	REG_L	a2,   2*SIZEOF_ARG($sp)
	REG_L	a3,   3*SIZEOF_ARG($sp)
	b	call_it

pass_d_f:		
	bne	t0, FFI_ARGS_DF, pass_f_d
	l.d	$f12, 0*SIZEOF_ARG($sp)	# load $fp regs from args
	l.s	$f14, 2*SIZEOF_ARG($sp)	# passing double and float
	REG_L	a3,   3*SIZEOF_ARG($sp)
	b	call_it

pass_f_d:		
 # assume that the only other combination must be float then double
 #	bne	t0, FFI_ARGS_F_D, call_it
	l.s	$f12, 0*SIZEOF_ARG($sp)	# load $fp regs from args
	l.d	$f14, 2*SIZEOF_ARG($sp)	# passing double and float

call_it:	
	# Load the function pointer
	REG_L	t9, SIZEOF_FRAME + 5*SIZEOF_ARG($fp)

	# If the return value pointer is NULL, assume no return value.
	REG_L	t1, SIZEOF_FRAME + 4*SIZEOF_ARG($fp)
	beqz	t1, noretval

	bne     t2, FFI_TYPE_INT, retfloat
	jal	t9
	REG_L	t0, SIZEOF_FRAME + 4*SIZEOF_ARG($fp)
	REG_S	v0, 0(t0)
	b	epilogue

retfloat:
	bne     t2, FFI_TYPE_FLOAT, retdouble
	jal	t9
	REG_L	t0, SIZEOF_FRAME + 4*SIZEOF_ARG($fp)
	s.s	$f0, 0(t0)
	b	epilogue

retdouble:	
	bne	t2, FFI_TYPE_DOUBLE, noretval
	jal	t9
	REG_L	t0, SIZEOF_FRAME + 4*SIZEOF_ARG($fp)
	s.d	$f0, 0(t0)
	b	epilogue
	
noretval:	
	jal	t9
	
	# Epilogue
epilogue:	
	move	$sp, $fp	
	REG_L	$fp, SIZEOF_FRAME - 2*SIZEOF_ARG($sp) # Restore frame pointer
	REG_L	ra, SIZEOF_FRAME - 1*SIZEOF_ARG($sp)  # Restore return address
	ADDU	$sp, SIZEOF_FRAME		      # Fix stack pointer
	j	ra

	.end	ffi_call_O32
	
#endif
