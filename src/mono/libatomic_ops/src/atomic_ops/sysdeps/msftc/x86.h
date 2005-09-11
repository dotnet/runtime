/*
 * Copyright (c) 2003 Hewlett-Packard Development Company, L.P.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. 
 */

/* The following really assume we have a 486 or better. */
/* If ASSUME_WINDOWS98 is defined, we assume Windows 98 or newer.	*/

#include "../aligned_atomic_load_store.h"

/* Real X86 implementations, except for some old WinChips, appear	*/
/* to enforce ordering between memory operations, EXCEPT that a later	*/
/* read can pass earlier writes, presumably due to the visible		*/
/* presence of store buffers.						*/
/* We ignore both the WinChips, and the fact that the official specs	*/
/* seem to be much weaker (and arguably too weak to be usable).		*/

#include "../ordered_except_wr.h"

#include "../test_and_set_t_is_ao_t.h"
	/* We should use byte-sized test-and-set locations, as with	*/
	/* gcc.  But I couldn't find the appropriate compiler		*/
	/* intrinsic.	- HB						*/

#include <windows.h>

/* As far as we can tell, the lfence and sfence instructions are not	*/
/* currently needed or useful for cached memory accesses.		*/

AO_INLINE AO_t
AO_fetch_and_add1_full (volatile AO_t *p)
{
  return InterlockedIncrement((LONG volatile *)p) - 1;
}

#define AO_HAVE_fetch_and_add1_full

AO_INLINE AO_t
AO_fetch_and_sub1_full (volatile AO_t *p)
{
  return InterlockedDecrement((LONG volatile *)p) + 1;
}

#define AO_HAVE_fetch_and_sub1_full

AO_INLINE AO_TS_VAL_t
AO_test_and_set_full(volatile AO_TS_t *addr)
{
  return (AO_TS_VAL_t) InterlockedExchange((LONG volatile *)addr,
		   			   (LONG)AO_TS_SET);
}

#define AO_HAVE_test_and_set_full

#ifdef AO_ASSUME_WINDOWS98
/* Returns nonzero if the comparison succeeded. */
AO_INLINE int
AO_compare_and_swap_full(volatile AO_t *addr,
		  	 AO_t old, AO_t new_val) 
{
# if 0
    /* Use the pointer variant, since that should always have the right size. */
    /* This seems to fail with VC++ 6 on Win2000 because the routine isn't    */
    /* actually there.							      */
    return InterlockedCompareExchangePointer((PVOID volatile *)addr,
		    			     (PVOID)new_val, (PVOID) old)
	   == (PVOID)old;
# endif
    /* FIXME - This is nearly useless on win64.			*/
    /* Use InterlockedCompareExchange64 for win64?		*/
    return InterlockedCompareExchange((DWORD volatile *)addr,
		    		      (DWORD)new_val, (DWORD) old)
	   == (DWORD)old;
}

#define AO_HAVE_compare_and_swap_full
#endif /* ASSUME_WINDOWS98 */
