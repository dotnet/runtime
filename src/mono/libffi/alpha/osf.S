/* -----------------------------------------------------------------------
   osf.S - Copyright (c) 1998 Cygnus Solutions
   
   Alpha/OSF Foreign Function Interface 

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
	
	.arch ev6
	.text

/* ffi_call_osf (void *args, unsigned long bytes, unsigned flags,
		 void *raddr, void (*fnaddr)());

   Bit o trickiness here -- ARGS+BYTES is the base of the stack frame
   for this function.  This has been allocated by ffi_call.  We also
   deallocate some of the stack that has been alloca'd.  */

	.align	3
	.globl	ffi_call_osf
	.ent	ffi_call_osf
ffi_call_osf:
	.frame	$15, 32, $26, 0
	.mask   0x4008000, -32
	addq	$16,$17,$1
	mov	$16, $30
	stq	$26, 0($1)
	stq	$15, 8($1)
	stq	$18, 16($1)
	mov	$1, $15
	.prologue 0

	stq	$19, 24($1)
	mov	$20, $27

	# Load up all of the (potential) argument registers.
	ldq	$16, 0($30)
	ldt	$f16, 0($30)
	ldt	$f17, 8($30)
	ldq	$17, 8($30)
	ldt	$f18, 16($30)
	ldq	$18, 16($30)
	ldt	$f19, 24($30)
	ldq	$19, 24($30)
	ldt	$f20, 32($30)
	ldq	$20, 32($30)
	ldt	$f21, 40($30)
	ldq	$21, 40($30)

	# Deallocate the register argument area.
	lda	$30, 48($30)

	jsr	$26, ($27), 0
	ldgp	$29, 0($26)

	# If the return value pointer is NULL, assume no return value.
	ldq	$19, 24($15)
	ldq	$18, 16($15)
	ldq	$26, 0($15)
	beq	$19, $noretval

	# Store the return value out in the proper type.
	cmpeq	$18, FFI_TYPE_INT, $1
	bne	$1, $retint
	cmpeq	$18, FFI_TYPE_FLOAT, $2
	bne	$2, $retfloat
	cmpeq	$18, FFI_TYPE_DOUBLE, $3
	bne	$3, $retdouble

$noretval:
	ldq	$15, 8($15)
	ret

$retint:
	stq	$0, 0($19)
	nop
	ldq	$15, 8($15)
	ret

$retfloat:
	sts	$f0, 0($19)
	nop
	ldq	$15, 8($15)
	ret

$retdouble:
	stt	$f0, 0($19)
	nop
	ldq	$15, 8($15)
	ret

	.end	ffi_call_osf

/* ffi_closure_osf(...)

   Receives the closure argument in $1.   */

	.align	3
	.globl	ffi_closure_osf
	.ent	ffi_closure_osf
ffi_closure_osf:
	.frame	$30, 16*8, $26, 0
	.mask	0x4000000, -16*8
	ldgp	$29, 0($27)
	subq	$30, 16*8, $30
	stq	$26, 0($30)
	.prologue 1

	# Store all of the potential argument registers in va_list format.
	stt	$f16, 4*8($30)
	stt	$f17, 5*8($30)
	stt	$f18, 6*8($30)
	stt	$f19, 7*8($30)
	stt	$f20, 8*8($30)
	stt	$f21, 9*8($30)
	stq	$16, 10*8($30)
	stq	$17, 11*8($30)
	stq	$18, 12*8($30)
	stq	$19, 13*8($30)
	stq	$20, 14*8($30)
	stq	$21, 15*8($30)

	# Call ffi_closure_osf_inner to do the bulk of the work.
	mov	$1, $16
	lda	$17, 2*8($30)
	lda	$18, 10*8($30)
	jsr	$26, ffi_closure_osf_inner
	ldgp	$29, 0($26)
	ldq	$26, 0($30)

	# Load up the return value in the proper type.
	lda	$1, $load_table
	s4addq	$0, $1, $1
	ldl	$1, 0($1)
	addq	$1, $29, $1
	jmp	$31, ($1), $load_32

	.align 4
$load_none:
	addq	$30, 16*8, $30
	ret

	.align 4
$load_float:
	lds	$f0, 16($30)
	nop
	addq	$30, 16*8, $30
	ret

	.align 4
$load_double:
	ldt	$f0, 16($30)
	nop
	addq	$30, 16*8, $30
	ret

	.align 4
$load_u8:
#ifdef __alpha_bwx__
	ldbu	$0, 16($30)
	nop
#else
	ldq	$0, 16($30)
	and	$0, 255, $0
#endif
	addq	$30, 16*8, $30
	ret

	.align 4
$load_s8:
#ifdef __alpha_bwx__
	ldbu	$0, 16($30)
	sextb	$0, $0
#else
	ldq	$0, 16($30)
	sll	$0, 56, $0
	sra	$0, 56, $0
#endif
	addq	$30, 16*8, $30
	ret

	.align 4
$load_u16:
#ifdef __alpha_bwx__
	ldwu	$0, 16($30)
	nop
#else
	ldq	$0, 16($30)
	zapnot	$0, 3, $0
#endif
	addq	$30, 16*8, $30
	ret

	.align 4
$load_s16:
#ifdef __alpha_bwx__
	ldwu	$0, 16($30)
	sextw	$0, $0
#else
	ldq	$0, 16($30)
	sll	$0, 48, $0
	sra	$0, 48, $0
#endif
	addq	$30, 16*8, $30
	ret

	.align 4
$load_32:
	ldl	$0, 16($30)
	nop
	addq	$30, 16*8, $30
	ret

	.align 4
$load_64:
	ldq	$0, 16($30)
	nop
	addq	$30, 16*8, $30
	ret

	.end	ffi_closure_osf

.section .rodata
$load_table:
	.gprel32 $load_none	# FFI_TYPE_VOID
	.gprel32 $load_32	# FFI_TYPE_INT
	.gprel32 $load_float	# FFI_TYPE_FLOAT
	.gprel32 $load_double	# FFI_TYPE_DOUBLE
	.gprel32 $load_double	# FFI_TYPE_LONGDOUBLE
	.gprel32 $load_u8	# FFI_TYPE_UINT8
	.gprel32 $load_s8	# FFI_TYPE_SINT8
	.gprel32 $load_u16	# FFI_TYPE_UINT16
	.gprel32 $load_s16	# FFI_TYPE_SINT16
	.gprel32 $load_32	# FFI_TYPE_UINT32
	.gprel32 $load_32	# FFI_TYPE_SINT32
	.gprel32 $load_64	# FFI_TYPE_UINT64
	.gprel32 $load_64	# FFI_TYPE_SINT64
	.gprel32 $load_none	# FFI_TYPE_STRUCT
	.gprel32 $load_64	# FFI_TYPE_POINTER

/* Assert that the table above is in sync with ffi.h.  */

#if	   FFI_TYPE_FLOAT != 2		\
	|| FFI_TYPE_DOUBLE != 3		\
	|| FFI_TYPE_UINT8 != 5		\
	|| FFI_TYPE_SINT8 != 6		\
	|| FFI_TYPE_UINT16 != 7		\
	|| FFI_TYPE_SINT16 != 8		\
	|| FFI_TYPE_UINT32 != 9		\
	|| FFI_TYPE_SINT32 != 10	\
	|| FFI_TYPE_UINT64 != 11	\
	|| FFI_TYPE_SINT64 != 12	\
	|| FFI_TYPE_STRUCT != 13	\
	|| FFI_TYPE_POINTER != 14	\
	|| FFI_TYPE_LAST != 14
#error "osf.S out of sync with ffi.h"
#endif
