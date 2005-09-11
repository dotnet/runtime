/*
 * Copyright (c) 2003 by Hewlett-Packard Company.  All rights reserved.
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

/* The following is useful primarily for debugging and documentation.	*/
/* We define various atomic operations by acquiring a global pthread 	*/
/* lock.  The resulting implementation will perform poorly, but should	*/
/* be correct unless it is used from signal handlers.			*/
/* We assume that all pthread operations act like full memory barriers.	*/
/* (We believe that is the intent of the specification.)		*/

#include <pthread.h>

#include "test_and_set_t_is_ao_t.h"
	/* This is not necessarily compatible with the native		*/
	/* implementation.  But those can't be safely mixed anyway.	*/

/* We define only the full barrier variants, and count on the 		*/
/* generalization section below to fill in the rest.			*/
extern pthread_mutex_t AO_pt_lock;

AO_INLINE void
AO_nop_full()
{
  pthread_mutex_lock(&AO_pt_lock);
  pthread_mutex_unlock(&AO_pt_lock);
}

#define AO_HAVE_nop_full

AO_INLINE AO_t
AO_load_full(volatile AO_t *addr)
{
  AO_t result;
  pthread_mutex_lock(&AO_pt_lock);
  result = *addr;
  pthread_mutex_unlock(&AO_pt_lock);
  return result;
}

#define AO_HAVE_load_full

AO_INLINE void
AO_store_full(volatile AO_t *addr, AO_t val)
{
  pthread_mutex_lock(&AO_pt_lock);
  *addr = val;
  pthread_mutex_unlock(&AO_pt_lock);
}

#define AO_HAVE_store_full

AO_INLINE unsigned char
AO_char_load_full(volatile unsigned char *addr)
{
  unsigned char result;
  pthread_mutex_lock(&AO_pt_lock);
  result = *addr;
  pthread_mutex_unlock(&AO_pt_lock);
  return result;
}

#define AO_HAVE_char_load_full

AO_INLINE void
AO_char_store_full(volatile unsigned char *addr, unsigned char val)
{
  pthread_mutex_lock(&AO_pt_lock);
  *addr = val;
  pthread_mutex_unlock(&AO_pt_lock);
}

#define AO_HAVE_char_store_full

AO_INLINE unsigned short
AO_short_load_full(volatile unsigned short *addr)
{
  unsigned short result;
  pthread_mutex_lock(&AO_pt_lock);
  result = *addr;
  pthread_mutex_unlock(&AO_pt_lock);
  return result;
}

#define AO_HAVE_short_load_full

AO_INLINE void
AO_short_store_full(volatile unsigned short *addr, unsigned short val)
{
  pthread_mutex_lock(&AO_pt_lock);
  *addr = val;
  pthread_mutex_unlock(&AO_pt_lock);
}

#define AO_HAVE_short_store_full

AO_INLINE unsigned int
AO_int_load_full(volatile unsigned int *addr)
{
  unsigned int result;
  pthread_mutex_lock(&AO_pt_lock);
  result = *addr;
  pthread_mutex_unlock(&AO_pt_lock);
  return result;
}

#define AO_HAVE_int_load_full

AO_INLINE void
AO_int_store_full(volatile unsigned int *addr, unsigned int val)
{
  pthread_mutex_lock(&AO_pt_lock);
  *addr = val;
  pthread_mutex_unlock(&AO_pt_lock);
}

#define AO_HAVE_int_store_full

AO_INLINE AO_TS_VAL_t
AO_test_and_set_full(volatile AO_TS_t *addr)
{
  AO_TS_VAL_t result;
  pthread_mutex_lock(&AO_pt_lock);
  result = (AO_TS_VAL_t)(*addr);
  *addr = AO_TS_SET;
  pthread_mutex_unlock(&AO_pt_lock);
  assert(result == AO_TS_SET || result == AO_TS_CLEAR);
  return result;
}

#define AO_HAVE_test_and_set_full

AO_INLINE AO_t
AO_fetch_and_add_full(volatile AO_t *p, AO_t incr)
{
  AO_t tmp;

  pthread_mutex_lock(&AO_pt_lock);
  tmp = *p;
  *p = tmp + incr;
  pthread_mutex_unlock(&AO_pt_lock);
  return tmp;
}

#define AO_HAVE_fetch_and_add_full

AO_INLINE unsigned char
AO_char_fetch_and_add_full(volatile unsigned char *p, unsigned char incr)
{
  unsigned char tmp;

  pthread_mutex_lock(&AO_pt_lock);
  tmp = *p;
  *p = tmp + incr;
  pthread_mutex_unlock(&AO_pt_lock);
  return tmp;
}

#define AO_HAVE_char_fetch_and_add_full

AO_INLINE unsigned short
AO_short_fetch_and_add_full(volatile unsigned short *p, unsigned short incr)
{
  unsigned short tmp;

  pthread_mutex_lock(&AO_pt_lock);
  tmp = *p;
  *p = tmp + incr;
  pthread_mutex_unlock(&AO_pt_lock);
  return tmp;
}

#define AO_HAVE_short_fetch_and_add_full

AO_INLINE unsigned int
AO_int_fetch_and_add_full(volatile unsigned int *p, unsigned int incr)
{
  unsigned int tmp;

  pthread_mutex_lock(&AO_pt_lock);
  tmp = *p;
  *p = tmp + incr;
  pthread_mutex_unlock(&AO_pt_lock);
  return tmp;
}

#define AO_HAVE_int_fetch_and_add_full

AO_INLINE void
AO_or_full(volatile AO_t *p, AO_t incr)
{
  AO_t tmp;

  pthread_mutex_lock(&AO_pt_lock);
  tmp = *p;
  *p = (tmp | incr);
  pthread_mutex_unlock(&AO_pt_lock);
}

#define AO_HAVE_or_full

AO_INLINE int
AO_compare_and_swap_full(volatile AO_t *addr,
		             AO_t old, AO_t new_val) 
{
  pthread_mutex_lock(&AO_pt_lock);
  if (*addr == old)
    {
      *addr = new_val;
      pthread_mutex_unlock(&AO_pt_lock);
      return 1;
    }
  else
    pthread_mutex_unlock(&AO_pt_lock);
    return 0;
}

#define AO_HAVE_compare_and_swap_full

/* Unlike real architectures, we define both double-width CAS variants. */

typedef struct {
	AO_t AO_val1;
	AO_t AO_val2;
} AO_double_t;

#define AO_HAVE_double_t

AO_INLINE int
AO_compare_double_and_swap_double_full(volatile AO_double_t *addr,
		             	       AO_t old1, AO_t old2,
				       AO_t new1, AO_t new2) 
{
  pthread_mutex_lock(&AO_pt_lock);
  if (addr -> AO_val1 == old1 && addr -> AO_val2 == old2)
    {
      addr -> AO_val1 = new1;
      addr -> AO_val2 = new2;
      pthread_mutex_unlock(&AO_pt_lock);
      return 1;
    }
  else
    pthread_mutex_unlock(&AO_pt_lock);
    return 0;
}

#define AO_HAVE_compare_double_and_swap_double_full

AO_INLINE int
AO_compare_and_swap_double_full(volatile AO_double_t *addr,
		             	AO_t old1,
				AO_t new1, AO_t new2) 
{
  pthread_mutex_lock(&AO_pt_lock);
  if (addr -> AO_val1 == old1)
    {
      addr -> AO_val1 = new1;
      addr -> AO_val2 = new2;
      pthread_mutex_unlock(&AO_pt_lock);
      return 1;
    }
  else
    pthread_mutex_unlock(&AO_pt_lock);
    return 0;
}

#define AO_HAVE_compare_and_swap_double_full

/* We can't use hardware loads and stores, since they don't	*/
/* interact correctly with atomic updates.			*/

