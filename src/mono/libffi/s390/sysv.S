/* -----------------------------------------------------------------------
   sysv.S - Copyright (c) 2000 Software AG
 
   S390 Foreign Function Interface
 
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
#ifdef HAVE_MACHINE_ASM_H
#include <machine/asm.h>
#endif
	
.text
 
	# r2:	ffi_prep_args
	# r3:	&ecif
	# r4:	cif->bytes
	# r5:	fig->flags
	# r6:	ecif.rvalue
	# sp+0: fn
 
	# This assumes we are using gas.
	.globl	ffi_call_SYSV
	.type	ffi_call_SYSV,%function
ffi_call_SYSV:
	# Save registers
	stm	%r7,%r15,28(%r15)
	l	%r7,96(%r15)	       # Get A(fn)
	lr	%r0,%r15
	ahi	%r15,-128	       # Make room for my args
	st	%r0,0(%r15)	       # Set backchain
	lr	%r11,%r15	       # Establish my stack register
	sr	%r15,%r4	       # Make room for fn args
	ahi	%r15,-96	       # Make room for new frame
	lr	%r10,%r15	       # Establish stack build area
	ahi	%r15,-96	       # Stack for next call
	lr	%r1,%r7
	stm	%r2,%r7,96(%r11)       # Save args on my stack
 
#------------------------------------------------------------------
#	move first 3 parameters in registers
#------------------------------------------------------------------
	lr	%r9,%r2		       # r9:	 &ffi_prep_args
	lr	%r2,%r10	       # Parm 1: &stack Parm 2: &ecif
	basr	%r14,%r9	       # call ffi_prep_args
 
#------------------------------------------------------------------
#	load  first 5 parameter registers
#------------------------------------------------------------------
	lm	%r2,%r6,24(%r10)
 
#------------------------------------------------------------------
#	load  fp parameter registers
#------------------------------------------------------------------
	ld	%f0,48(%r10)
	ld	%f2,56(%r10)
 
#------------------------------------------------------------------
#	call  function
#------------------------------------------------------------------
	lr	%r15,%r10	       # Set new stack
	l	%r9,116(%r11)	       # Get &fn
	basr	%r14,%r9	       # Call function
 
#------------------------------------------------------------------
#	On return:
#	   r2: Return value (r3: Return value + 4 for long long)
#------------------------------------------------------------------
 
#------------------------------------------------------------------
#	If the return value pointer is NULL, assume no return value.
#------------------------------------------------------------------
	icm	%r6,15,112(%r11)
	jz	.Lepilogue
 
	l	%r5,108(%r11)	       # Get return type
#------------------------------------------------------------------
#	return INT
#------------------------------------------------------------------
	chi	%r5,FFI_TYPE_INT
	jne	.Lchk64
 
	st	%r2,0(%r6)
	j	.Lepilogue
 
.Lchk64:
#------------------------------------------------------------------
#	return LONG LONG (signed/unsigned)
#------------------------------------------------------------------
	chi	%r5,FFI_TYPE_UINT64
	je	.LdoLongLong
 
	chi	%r5,FFI_TYPE_SINT64
	jne	.LchkFloat
 
.LdoLongLong:
	stm	%r2,%r3,0(%r6)
	j	.Lepilogue
 
.LchkFloat:
#------------------------------------------------------------------
#	return FLOAT
#------------------------------------------------------------------
	chi	%r5,FFI_TYPE_FLOAT
	jne	.LchkDouble
 
	std	%f0,0(%r6)
	j	.Lepilogue
 
.LchkDouble:
#------------------------------------------------------------------
#	return DOUBLE or LONGDOUBLE
#------------------------------------------------------------------
	chi	%r5,FFI_TYPE_DOUBLE
	jne	.LchkStruct
 
	std	%f0,0(%r6)
	std	%f2,8(%r6)
	j	.Lepilogue
 
.LchkStruct:
#------------------------------------------------------------------
# Structure - rvalue already set as sent as 1st parm to routine
#------------------------------------------------------------------
	chi	%r5,FFI_TYPE_STRUCT
	je	.Lepilogue
 
.Ldefault:
#------------------------------------------------------------------
#	return a pointer
#------------------------------------------------------------------
	st	%r2,0(%r6)
	j	.Lepilogue
 
.Lepilogue:
	l	%r15,0(%r11)
	l	%r4,56(%r15)
	lm	%r7,%r15,28(%r15)
	br	%r4
 
.ffi_call_SYSV_end:
	.size	 ffi_call_SYSV,.ffi_call_SYSV_end-ffi_call_SYSV
