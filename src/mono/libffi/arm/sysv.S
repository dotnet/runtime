/* -----------------------------------------------------------------------
   sysv.S - Copyright (c) 1998 Cygnus Solutions
   
   ARM Foreign Function Interface 

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
#else
/* XXX these lose for some platforms, I'm sure. */
#define CNAME(x) x
#define ENTRY(x) .globl CNAME(x); .type CNAME(x),%function; CNAME(x):
#endif
	
.text

	# a1:   ffi_prep_args
	# a2:   &ecif
	# a3:   cif->bytes
	# a4:   fig->flags
	# sp+0: ecif.rvalue
	# sp+4: fn

	# This assumes we are using gas.
ENTRY(ffi_call_SYSV)
	# Save registers
        stmfd sp!, {a1-a4, fp, lr}
	mov   fp, sp

	# Make room for all of the new args.
	sub   sp, fp, a3

	# Place all of the ffi_prep_args in position
	mov   ip, a1
	mov   a1, sp
	#     a2 already set

	# And call
	mov   lr, pc
	mov   pc, ip

	# move first 4 parameters in registers
	ldr   a1, [sp, #0]
	ldr   a2, [sp, #4]
	ldr   a3, [sp, #8]
        ldr   a4, [sp, #12]

	# and adjust stack
	ldr   ip, [fp, #8]
        cmp   ip, #16
	movge ip, #16
        add   sp, sp, ip

	# call function
	mov   lr, pc
	ldr   pc, [fp, #28]

	# Remove the space we pushed for the args
	mov   sp, fp

	# Load a3 with the pointer to storage for the return value
	ldr   a3, [sp, #24]

	# Load a4 with the return type code 
	ldr   a4, [sp, #12]

	# If the return value pointer is NULL, assume no return value.
	cmp   a3, #0
	beq   epilogue

# return INT
	cmp   a4, #FFI_TYPE_INT
	streq a1, [a3]
	beq   epilogue

# return FLOAT
	cmp     a4, #FFI_TYPE_FLOAT
	stfeqs  f0, [a3]
	beq     epilogue

# return DOUBLE or LONGDOUBLE
	cmp     a4, #FFI_TYPE_DOUBLE
	stfeqd  f0, [a3]

epilogue:
        ldmfd sp!, {a1-a4, fp, pc}

.ffi_call_SYSV_end:
        .size    CNAME(ffi_call_SYSV),.ffi_call_SYSV_end-CNAME(ffi_call_SYSV)

