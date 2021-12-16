/* libunwind - a platform-independent unwind library
   Copyright (C) 2008 CodeSourcery

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

#ifndef LIBUNWIND_H
#define LIBUNWIND_H

#if defined(__cplusplus) || defined(c_plusplus)
extern "C" {
#endif

#include <inttypes.h>
#include <ucontext.h>

#ifdef loongarch
# undef loongarch
#endif

#define UNW_TARGET      loongarch
#define UNW_TARGET_LOONGARCH 1

#define _U_TDEP_QP_TRUE 0       /* see libunwind-dynamic.h  */

/* This needs to be big enough to accommodate "struct cursor", while
   leaving some slack for future expansion.  Changing this value will
   require recompiling all users of this library.  Stack allocation is
   relatively cheap and unwind-state copying is relatively rare, so we
   want to err on making it rather too big than too small.  */

/* FIXME for LOONGARCH. Too big?  What do other things use for similar tasks?  */
#define UNW_TDEP_CURSOR_LEN     4096

/* The size of a "word" varies on LOONGARCH.  This type is used for memory
   addresses and register values.  To allow a single library to support
   multiple ABIs, and to support N32 at all, we must use a 64-bit type
   even when addresses are only 32 bits.  */
typedef uint64_t unw_word_t;
typedef int32_t unw_sword_t;

/* FIXME: LOONGARCH ABIs.  */
typedef long double unw_tdep_fpreg_t;

typedef enum
  {
    UNW_LOONGARCH_R0,
    UNW_LOONGARCH_R1,
    UNW_LOONGARCH_R2,
    UNW_LOONGARCH_R3,
    UNW_LOONGARCH_R4,
    UNW_LOONGARCH_R5,
    UNW_LOONGARCH_R6,
    UNW_LOONGARCH_R7,
    UNW_LOONGARCH_R8,
    UNW_LOONGARCH_R9,
    UNW_LOONGARCH_R10,
    UNW_LOONGARCH_R11,
    UNW_LOONGARCH_R12,
    UNW_LOONGARCH_R13,
    UNW_LOONGARCH_R14,
    UNW_LOONGARCH_R15,
    UNW_LOONGARCH_R16,
    UNW_LOONGARCH_R17,
    UNW_LOONGARCH_R18,
    UNW_LOONGARCH_R19,
    UNW_LOONGARCH_R20,
    UNW_LOONGARCH_R21,
    UNW_LOONGARCH_R22,
    UNW_LOONGARCH_R23,
    UNW_LOONGARCH_R24,
    UNW_LOONGARCH_R25,
    UNW_LOONGARCH_R26,
    UNW_LOONGARCH_R27,
    UNW_LOONGARCH_R28,
    UNW_LOONGARCH_R29,
    UNW_LOONGARCH_R30,
    UNW_LOONGARCH_R31,

    UNW_LOONGARCH_PC = 34,

    /* FIXME: Other registers!  */

    /* For LOONGARCH, the CFA is the value of SP (r3) at the call site in the
       previous frame.  */
    UNW_LOONGARCH_CFA,

    UNW_TDEP_LAST_REG = UNW_LOONGARCH_R31,

    UNW_TDEP_IP = UNW_LOONGARCH_R1,
    UNW_TDEP_SP = UNW_LOONGARCH_R3,
    UNW_TDEP_EH = UNW_LOONGARCH_R0   /* FIXME.  */
  }
loongarch_regnum_t;

typedef enum
  {
    UNW_LOONGARCH_ABI_LP32,
    UNW_LOONGARCH_ABI_LP64
  }
loongarch_abi_t;

#define UNW_TDEP_NUM_EH_REGS    2       /* FIXME for LOONGARCH.  */

typedef struct unw_tdep_save_loc
  {
    /* Additional target-dependent info on a save location.  */
  }
unw_tdep_save_loc_t;

/* On x86, we can directly use ucontext_t as the unwind context.  FIXME for
   LOONGARCH.  */
typedef ucontext_t unw_tdep_context_t;

#include "libunwind-dynamic.h"

typedef struct
  {
    /* no loongarch-specific auxiliary proc-info */
  }
unw_tdep_proc_info_t;

#include "libunwind-common.h"

/* There is no getcontext() on LOONGARCH.  Use a stub version which only saves GP
   registers.  FIXME: Not ideal, may not be sufficient for all libunwind
   use cases.  */
#define unw_tdep_getcontext UNW_ARCH_OBJ(getcontext)
extern int unw_tdep_getcontext (ucontext_t *uc);

#define unw_tdep_is_fpreg               UNW_ARCH_OBJ(is_fpreg)
extern int unw_tdep_is_fpreg (int);

#if defined(__cplusplus) || defined(c_plusplus)
}
#endif

#endif /* LIBUNWIND_H */
