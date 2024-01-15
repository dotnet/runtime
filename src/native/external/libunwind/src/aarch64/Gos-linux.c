/* libunwind - a platform-independent unwind library
   Copyright (C) 2008 CodeSourcery
   Copyright (C) 2011-2013 Linaro Limited
   Copyright (C) 2012 Tommi Rantala <tt.rantala@gmail.com>

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

#include "unwind_i.h"

#ifndef UNW_REMOTE_ONLY

HIDDEN int
aarch64_local_resume (unw_addr_space_t as, unw_cursor_t *cursor, void *arg)
{
  struct cursor *c = (struct cursor *) cursor;
  unw_context_t *uc = c->uc;

  if (c->sigcontext_format == AARCH64_SCF_NONE)
    {
      /* Since there are no signals involved here we restore EH and non scratch
         registers only.  */
      __asm__ __volatile__ (
        "ldr x0,  %[x0]\n\t"
        "ldr x1,  %[x1]\n\t"
        "ldr x2,  %[x2]\n\t"
        "ldr x3,  %[x3]\n\t"
        "ldr x19, %[x19]\n\t"
        "ldr x20, %[x20]\n\t"
        "ldr x21, %[x21]\n\t"
        "ldr x22, %[x22]\n\t"
        "ldr x23, %[x23]\n\t"
        "ldr x24, %[x24]\n\t"
        "ldr x25, %[x25]\n\t"
        "ldr x26, %[x26]\n\t"
        "ldr x27, %[x27]\n\t"
        "ldr x28, %[x28]\n\t"
        "ldr x29, %[x29]\n\t"
        "ldr x30, %[x30]\n\t"
        "ldr d8,  %[d8]\n\t"
        "ldr d9,  %[d9]\n\t"
        "ldr d10, %[d10]\n\t"
        "ldr d11, %[d11]\n\t"
        "ldr d12, %[d12]\n\t"
        "ldr d13, %[d13]\n\t"
        "ldr d14, %[d14]\n\t"
        "ldr d15, %[d15]\n\t"
        "ldr x5,  %[sp]\n\t"
        "mov sp, x5\n\t"
        "ret\n"
        :
        : [x0]  "m"(uc->uc_mcontext.regs[0]),
		  [x1]  "m"(uc->uc_mcontext.regs[1]),
		  [x2]  "m"(uc->uc_mcontext.regs[2]),
		  [x3]  "m"(uc->uc_mcontext.regs[3]),
		  [x19] "m"(uc->uc_mcontext.regs[19]),
		  [x20] "m"(uc->uc_mcontext.regs[20]),
		  [x21] "m"(uc->uc_mcontext.regs[21]),
		  [x22] "m"(uc->uc_mcontext.regs[22]),
		  [x23] "m"(uc->uc_mcontext.regs[23]),
		  [x24] "m"(uc->uc_mcontext.regs[24]),
		  [x25] "m"(uc->uc_mcontext.regs[25]),
		  [x26] "m"(uc->uc_mcontext.regs[26]),
		  [x27] "m"(uc->uc_mcontext.regs[27]),
		  [x28] "m"(uc->uc_mcontext.regs[28]),
		  [x29] "m"(uc->uc_mcontext.regs[29]), /* FP */
		  [x30] "m"(uc->uc_mcontext.regs[30]), /* LR */
		  [d8]  "m"(GET_FPCTX(uc)->vregs[8]),
		  [d9]  "m"(GET_FPCTX(uc)->vregs[9]),
		  [d10] "m"(GET_FPCTX(uc)->vregs[10]),
		  [d11] "m"(GET_FPCTX(uc)->vregs[11]),
		  [d12] "m"(GET_FPCTX(uc)->vregs[12]),
		  [d13] "m"(GET_FPCTX(uc)->vregs[13]),
		  [d14] "m"(GET_FPCTX(uc)->vregs[14]),
		  [d15] "m"(GET_FPCTX(uc)->vregs[15]),
          [sp]  "m"(uc->uc_mcontext.sp)
		: "x0",   "x1",  "x2",  "x3", "x19", "x20", "x21", "x22", "x23", "x24",
		  "x25", "x26", "x27", "x28", "x29", "x30"
	  );
    }
  else
    {
      struct sigcontext *sc = (struct sigcontext *) c->sigcontext_addr;

      if (c->dwarf.eh_valid_mask & 0x1) sc->regs[0] = c->dwarf.eh_args[0];
      if (c->dwarf.eh_valid_mask & 0x2) sc->regs[1] = c->dwarf.eh_args[1];
      if (c->dwarf.eh_valid_mask & 0x4) sc->regs[2] = c->dwarf.eh_args[2];
      if (c->dwarf.eh_valid_mask & 0x8) sc->regs[3] = c->dwarf.eh_args[3];

      sc->regs[4] = uc->uc_mcontext.regs[4];
      sc->regs[5] = uc->uc_mcontext.regs[5];
      sc->regs[6] = uc->uc_mcontext.regs[6];
      sc->regs[7] = uc->uc_mcontext.regs[7];
      sc->regs[8] = uc->uc_mcontext.regs[8];
      sc->regs[9] = uc->uc_mcontext.regs[9];
      sc->regs[10] = uc->uc_mcontext.regs[10];
      sc->regs[11] = uc->uc_mcontext.regs[11];
      sc->regs[12] = uc->uc_mcontext.regs[12];
      sc->regs[13] = uc->uc_mcontext.regs[13];
      sc->regs[14] = uc->uc_mcontext.regs[14];
      sc->regs[15] = uc->uc_mcontext.regs[15];
      sc->regs[16] = uc->uc_mcontext.regs[16];
      sc->regs[17] = uc->uc_mcontext.regs[17];
      sc->regs[18] = uc->uc_mcontext.regs[18];
      sc->regs[19] = uc->uc_mcontext.regs[19];
      sc->regs[20] = uc->uc_mcontext.regs[20];
      sc->regs[21] = uc->uc_mcontext.regs[21];
      sc->regs[22] = uc->uc_mcontext.regs[22];
      sc->regs[23] = uc->uc_mcontext.regs[23];
      sc->regs[24] = uc->uc_mcontext.regs[24];
      sc->regs[25] = uc->uc_mcontext.regs[25];
      sc->regs[26] = uc->uc_mcontext.regs[26];
      sc->regs[27] = uc->uc_mcontext.regs[27];
      sc->regs[28] = uc->uc_mcontext.regs[28];
      sc->regs[29] = uc->uc_mcontext.regs[29];
      sc->regs[30] = uc->uc_mcontext.regs[30];
      sc->sp = uc->uc_mcontext.sp;
      sc->pc = uc->uc_mcontext.pc;
      sc->pstate = uc->uc_mcontext.pstate;

      __asm__ __volatile__ (
        "mov sp, %0\n"
        "ret %1\n"
        : : "r" (c->sigcontext_sp), "r" (c->sigcontext_pc)
      );
   }
  unreachable();
  return -UNW_EINVAL;
}

#endif /* !UNW_REMOTE_ONLY */
