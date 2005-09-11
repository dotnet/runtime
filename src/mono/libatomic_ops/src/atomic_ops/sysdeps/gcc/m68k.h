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

/* FIXME.  Very incomplete.  */
#include "../all_aligned_atomic_load_store.h"

/* Are there any m68k multiprocessors still around?  	*/
/* AFAIK, Alliants were sequentially consistent.	*/
#include "../ordered.h"

#include "../test_and_set_t_is_ao_t.h"

/* Contributed by Tony Mantler or new.  Should be changed to MIT license? */
AO_INLINE AO_TS_VAL_t
AO_test_and_set_full(volatile AO_TS_t *addr) {
  int oldval;

  /* The return value is semi-phony. */
  /* 'tas' sets bit 7 while the return */
  /* value pretends bit 0 was set */
  __asm__ __volatile__(
                 "tas %1@; sne %0; negb %0"
                 : "=d" (oldval)
                 : "a" (addr) : "memory");
   return oldval;
}

#define AO_HAVE_test_and_set_full



