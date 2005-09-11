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

/* FIXME.  Very incomplete.  No support for 64 bits.	*/

#include "../all_atomic_load_store.h"

#include "../test_and_set_t_is_ao_t.h" /* Probably suboptimal */

AO_INLINE AO_TS_VAL_t
AO_test_and_set_full(volatile AO_TS_t *addr) {
  int oldval;
  int temp = 1; /* locked value */

         __asm__ __volatile__ (
          "     l     %0,0(%2)\n"
          "0:   cs    %0,%1,0(%2)\n"
          "     jl    0b"
          : "=&d" (ret)
          : "d" (1), "a" (addr)
          : "cc", "memory");
  return oldval;
}

#define AO_HAVE_test_and_set_full



