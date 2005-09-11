/* 
 * Copyright (c) 1991-1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996-1999 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1999-2003 by Hewlett-Packard Company. All rights reserved.
 *
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
 */

/* FIXME: untested.						*/
/* The relevant documentation appears to be at			*/
/* http://publibz.boulder.ibm.com/epubs/pdf/dz9zr003.pdf	*/
/* around page 5-96.  Apparently:				*/
/* - Memory references in general are atomic only for a single 	*/
/*   byte.  But it appears that the most common load/store	*/
/*   instructions also guarantee atomicity for aligned 		*/
/*   operands of standard types.  WE FOOLISHLY ASSUME that	*/
/*   compilers only generate those.  If that turns out to be	*/
/*   wrong, we need inline assembly code for AO_load and	*/
/*   AO_store.							*/
/* - A store followed by a load is unordered since the store	*/
/*   may be delayed.  Otherwise everything is ordered.		*/
/* - There is a hardware compare-and-swap (CS) instruction.	*/

#include "ordered_except_wr.h"
#include "all_aligned_atomic_load_store.h"

#include "../test_and_set_t_is_ao_t.h"
/* FIXME: Is there a way to do byte-sized test-and-set?	*/

/* FIXME: AO_nop_full should probably be implemented directly.	*/
/* It appears that certain BCR instructions have that effect.	*/
/* Presumably they're cheaper than CS?				*/

AO_INLINE AO_t AO_compare_and_swap_full(volatile AO_t *addr,
                                               AO_t old, AO_t new_val)
{
  int retval;
  __asm__ __volatile__ (
# ifndef __s390x__
    "     cs  %1,%2,0(%3)\n"
# else
    "     csg %1,%2,0(%3)\n"
# endif
  "     ipm %0\n"
  "     srl %0,28\n"
  : "=&d" (retval), "+d" (old)
  : "d" (new_val), "a" (addr)
  : "cc", "memory");
  return retval == 0;
}

#define AO_HAVE_compare_and_swap_full

/* FIXME: Add double-wide compare-and-swap for 32-bit executables.	*/
