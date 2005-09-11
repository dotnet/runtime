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

/*
 * This file specifies Itanimum primitives for use with the HP compiler
 * unde HP/UX.  We use intrinsics instead of the inline assembly code in the
 * gcc file.
 */

#include "../atomic_load_store.h"

#include "../acquire_release_volatile.h"

#include "../test_and_set_t_is_char.h"

#include <machine/sys/inline.h>

#ifdef __LP64__
# define AO_T_FASIZE _FASZ_D
# define AO_T_SIZE _SZ_D
#else
# define AO_T_FASIZE _FASZ_W
# define AO_T_SIZE _SZ_W
#endif

AO_INLINE void
AO_nop_full()
{
  _Asm_mf();
}
#define AO_HAVE_nop_full

AO_INLINE AO_t
AO_fetch_and_add1_acquire (volatile AO_t *p)
{
  return _Asm_fetchadd(AO_T_FASIZE, _SEM_ACQ, p, 1,
		       _LDHINT_NONE, _DOWN_MEM_FENCE);
}
#define AO_HAVE_fetch_and_add1_acquire

AO_INLINE AO_t
AO_fetch_and_add1_release (volatile AO_t *p)
{
  return _Asm_fetchadd(AO_T_FASIZE, _SEM_REL, p, 1,
		       _LDHINT_NONE, _UP_MEM_FENCE);
}

#define AO_HAVE_fetch_and_add1_release

AO_INLINE AO_t
AO_fetch_and_sub1_acquire (volatile AO_t *p)
{
  return _Asm_fetchadd(AO_T_FASIZE, _SEM_ACQ, p, -1,
		       _LDHINT_NONE, _DOWN_MEM_FENCE);
}

#define AO_HAVE_fetch_and_sub1_acquire

AO_INLINE AO_t
AO_fetch_and_sub1_release (volatile AO_t *p)
{
  return _Asm_fetchadd(AO_T_FASIZE, _SEM_REL, p, -1,
		       _LDHINT_NONE, _UP_MEM_FENCE);
}

#define AO_HAVE_fetch_and_sub1_release

AO_INLINE int
AO_compare_and_swap_acquire(volatile AO_t *addr,
		             AO_t old, AO_t new_val) 
{
  AO_t oldval;

  _Asm_mov_to_ar(_AREG_CCV, old, _DOWN_MEM_FENCE);
  oldval = _Asm_cmpxchg(AO_T_SIZE, _SEM_ACQ, addr,
		  	new_val, _LDHINT_NONE, _DOWN_MEM_FENCE);
  return (oldval == old);
}

#define AO_HAVE_compare_and_swap_acquire

AO_INLINE int
AO_compare_and_swap_release(volatile AO_t *addr,
		             AO_t old, AO_t new_val) 
{
  AO_t oldval;
  _Asm_mov_to_ar(_AREG_CCV, old, _UP_MEM_FENCE);
  oldval = _Asm_cmpxchg(AO_T_SIZE, _SEM_REL, addr,
		  	new_val, _LDHINT_NONE, _UP_MEM_FENCE);
  /* Hopefully the compiler knows not to reorder the above two? */
  return (oldval == old);
}

#define AO_HAVE_compare_and_swap_release

AO_INLINE int
AO_char_compare_and_swap_acquire(volatile unsigned char *addr,
		                 unsigned char old, unsigned char new_val) 
{
  unsigned char oldval;

  _Asm_mov_to_ar(_AREG_CCV, old, _DOWN_MEM_FENCE);
  oldval = _Asm_cmpxchg(_SZ_B, _SEM_ACQ, addr,
		  	new_val, _LDHINT_NONE, _DOWN_MEM_FENCE);
  return (oldval == old);
}

#define AO_HAVE_char_compare_and_swap_acquire

AO_INLINE int
AO_char_compare_and_swap_release(volatile unsigned char *addr,
		                 unsigned char old, unsigned char new_val) 
{
  insigned char oldval;
  _Asm_mov_to_ar(_AREG_CCV, old, _UP_MEM_FENCE);
  oldval = _Asm_cmpxchg(_SZ_B, _SEM_REL, addr,
		  	new_val, _LDHINT_NONE, _UP_MEM_FENCE);
  /* Hopefully the compiler knows not to reorder the above two? */
  return (oldval == old);
}

#define AO_HAVE_char_compare_and_swap_release

AO_INLINE int
AO_short_compare_and_swap_acquire(volatile unsigned short *addr,
		                 unsigned short old, unsigned short new_val) 
{
  unsigned short oldval;

  _Asm_mov_to_ar(_AREG_CCV, old, _DOWN_MEM_FENCE);
  oldval = _Asm_cmpxchg(_SZ_B, _SEM_ACQ, addr,
		  	new_val, _LDHINT_NONE, _DOWN_MEM_FENCE);
  return (oldval == old);
}

#define AO_HAVE_short_compare_and_swap_acquire

AO_INLINE int
AO_short_compare_and_swap_release(volatile unsigned short *addr,
		                 unsigned short old, unsigned short new_val) 
{
  insigned short oldval;
  _Asm_mov_to_ar(_AREG_CCV, old, _UP_MEM_FENCE);
  oldval = _Asm_cmpxchg(_SZ_B, _SEM_REL, addr,
		  	new_val, _LDHINT_NONE, _UP_MEM_FENCE);
  /* Hopefully the compiler knows not to reorder the above two? */
  return (oldval == old);
}

#define AO_HAVE_short_compare_and_swap_release

#ifndef __LP64__
# include "ao_t_is_int.h"
#endif

