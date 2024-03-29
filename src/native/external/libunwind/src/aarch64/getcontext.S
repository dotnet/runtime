/* libunwind - a platform-independent unwind library
   Copyright (C) 2008 Google, Inc
	Contributed by Paul Pluzhnikov <ppluzhnikov@google.com>
   Copyright (C) 2010 Konstantin Belousov <kib@freebsd.org>

This file is part of libunwind.

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
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.  */

#include "ucontext_i.h"

/*  int _Uaarch64_getcontext_trace (unw_tdep_context_t *ucp)

  Saves limited machine context in UCP necessary for fast trace. If fast trace
  fails, caller will have to get the full context.
*/

	.global _Uaarch64_getcontext_trace
	.hidden _Uaarch64_getcontext_trace
	.type _Uaarch64_getcontext_trace, @function
_Uaarch64_getcontext_trace:
	.cfi_startproc

	/* Save only FP, SP, PC - exclude this call. */
	str x29, [x0, #(UC_MCONTEXT_OFF + SC_X29_OFF)]
	mov x9, sp
	str x9, [x0, #(UC_MCONTEXT_OFF + SC_SP_OFF)]
	str x30, [x0, #(UC_MCONTEXT_OFF + SC_PC_OFF)]

	ret
	.cfi_endproc
	.size _Uaarch64_getcontext_trace, . - _Uaarch64_getcontext_trace

      /* We do not need executable stack.  */
      .section        .note.GNU-stack,"",@progbits
