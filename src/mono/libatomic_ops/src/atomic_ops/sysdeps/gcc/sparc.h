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

/* FIXME.  Very incomplete.  No support for sparc64.	*/
/* Non-ancient SPARCs provide compare-and-swap (casa).	*/
/* We should make that available.			*/

#include "../all_atomic_load_store.h"

/* Real SPARC code uses TSO:				*/
#include "../ordered_except_wr.h"

/* Test_and_set location is just a byte.                */
#include "../test_and_set_t_is_char.h"

AO_INLINE AO_TS_VAL_t
AO_test_and_set_full(volatile AO_TS_t *addr) {
  int oldval;

   __asm__ __volatile__("ldstub %1,%0"
	                : "=r"(oldval), "=m"(*addr)
	                : "m"(*addr) : "memory");
   return oldval;
}

#define AO_HAVE_test_and_set_full

/* FIXME: This needs to be extended for SPARC v8 and v9.	*/
/* SPARC V8 also has swap.  V9 has CAS.				*/
/* There are barriers like membar #LoadStore.			*/
/* CASA (32-bit) and CASXA(64-bit) instructions were		*/
/* added in V9.							*/
