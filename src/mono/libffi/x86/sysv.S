/* -----------------------------------------------------------------------
   sysv.S - Copyright (c) 1996, 1998  Cygnus Solutions
   
   X86 Foreign Function Interface 

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

.text

.globl ffi_prep_args

	# This assumes we are using gas.
        .balign 16
.globl ffi_call_SYSV
        .type    ffi_call_SYSV,@function

ffi_call_SYSV:
.LFB1:
        pushl %ebp
.LCFI0:
        movl  %esp,%ebp
.LCFI1:
	# Make room for all of the new args.
	movl  16(%ebp),%ecx
	subl  %ecx,%esp

	movl  %esp,%eax

	# Place all of the ffi_prep_args in position
	pushl 12(%ebp)
	pushl %eax
	call  *8(%ebp)

	# Return stack to previous state and call the function
	addl  $8,%esp	

	call  *28(%ebp)

	# Remove the space we pushed for the args
	movl  16(%ebp),%ecx
	addl  %ecx,%esp

	# Load %ecx with the return type code 
	movl  20(%ebp),%ecx	

	# If the return value pointer is NULL, assume no return value.
	cmpl  $0,24(%ebp)
	jne   retint

	# Even if there is no space for the return value, we are 
	# obliged to handle floating-point values.
	cmpl  $FFI_TYPE_FLOAT,%ecx
	jne   noretval
	fstp  %st(0)

        jmp   epilogue

retint:
	cmpl  $FFI_TYPE_INT,%ecx
	jne   retfloat
	# Load %ecx with the pointer to storage for the return value
	movl  24(%ebp),%ecx	
	movl  %eax,0(%ecx)
	jmp   epilogue

retfloat:
	cmpl  $FFI_TYPE_FLOAT,%ecx
	jne   retdouble
	# Load %ecx with the pointer to storage for the return value
	movl  24(%ebp),%ecx	
	fstps (%ecx)
	jmp   epilogue

retdouble:
	cmpl  $FFI_TYPE_DOUBLE,%ecx
	jne   retlongdouble
	# Load %ecx with the pointer to storage for the return value
	movl  24(%ebp),%ecx	
	fstpl (%ecx)
	jmp   epilogue

retlongdouble:
	cmpl  $FFI_TYPE_LONGDOUBLE,%ecx
	jne   retint64
	# Load %ecx with the pointer to storage for the return value
	movl  24(%ebp),%ecx	
	fstpt (%ecx)
	jmp   epilogue
	
retint64:	
	cmpl  $FFI_TYPE_SINT64,%ecx
        jne   retstruct
	# Load %ecx with the pointer to storage for the return value
	movl  24(%ebp),%ecx	
	movl  %eax,0(%ecx)
	movl  %edx,4(%ecx)
	
retstruct:
	# Nothing to do!

noretval:
epilogue:
        movl %ebp,%esp
        popl %ebp
        ret
.LFE1:
.ffi_call_SYSV_end:
        .size    ffi_call_SYSV,.ffi_call_SYSV_end-ffi_call_SYSV

.section	.eh_frame,"aw",@progbits
__FRAME_BEGIN__:
	.4byte	.LLCIE1
.LSCIE1:
	.4byte	0x0
	.byte	0x1
	.byte	0x0
	.byte	0x1
	.byte	0x7c
	.byte	0x8
	.byte	0xc
	.byte	0x4
	.byte	0x4
	.byte	0x88
	.byte	0x1
	.align 4
.LECIE1:
	.set	.LLCIE1,.LECIE1-.LSCIE1
	.4byte	.LLFDE1
.LSFDE1:
	.4byte	.LSFDE1-__FRAME_BEGIN__
	.4byte	.LFB1
	.4byte	.LFE1-.LFB1
	.byte	0x4
	.4byte	.LCFI0-.LFB1
	.byte	0xe
	.byte	0x8
	.byte	0x85
	.byte	0x2
	.byte	0x4
	.4byte	.LCFI1-.LCFI0
	.byte	0xd
	.byte	0x5
	.align 4
.LEFDE1:
	.set	.LLFDE1,.LEFDE1-.LSFDE1
