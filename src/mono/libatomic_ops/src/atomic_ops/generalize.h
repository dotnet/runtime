/*
 * Copyright (c) 2003-2004 Hewlett-Packard Development Company, L.P.
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
 * Generalize atomic operations for atomic_ops.h.
 * Should not be included directly.
 *
 * We make no attempt to define useless operations, such as
 * AO_nop_acquire
 * AO_nop_release
 *
 * We have also so far neglected to define some others, which
 * do not appear likely to be useful, e.g. stores with acquire
 * or read barriers.
 *
 * This file is sometimes included twice by atomic_ops.h.
 * All definitions include explicit checks that we are not replacing
 * an earlier definition.  In general, more desirable expansions
 * appear earlier so that we are more likely to use them.
 *
 * We only make safe generalizations, except that by default we define
 * the ...dd_acquire_read operations to be equivalent to those without
 * a barrier.  On platforms for which this is unsafe, the platform-specific
 * file must define AO_NO_DD_ORDERING.
 */

#ifndef ATOMIC_OPS_H
# error Atomic_ops_generalize.h should not be included directly.
#endif

#if AO_CHAR_TS_T
# define AO_TS_COMPARE_AND_SWAP_FULL(a,o,n) \
	 AO_char_compare_and_swap_full(a,o,n)
# define AO_TS_COMPARE_AND_SWAP_ACQUIRE(a,o,n) \
	 AO_char_compare_and_swap_acquire(a,o,n)
# define AO_TS_COMPARE_AND_SWAP_RELEASE(a,o,n) \
	 AO_char_compare_and_swap_release(a,o,n)
# define AO_TS_COMPARE_AND_SWAP(a,o,n) \
	 AO_char_compare_and_swap(a,o,n)
#endif

#if AO_AO_TS_T
# define AO_TS_COMPARE_AND_SWAP_FULL(a,o,n) \
	 AO_compare_and_swap_full(a,o,n)
# define AO_TS_COMPARE_AND_SWAP_ACQUIRE(a,o,n) \
	 AO_compare_and_swap_acquire(a,o,n)
# define AO_TS_COMPARE_AND_SWAP_RELEASE(a,o,n) \
	 AO_compare_and_swap_release(a,o,n)
# define AO_TS_COMPARE_AND_SWAP(a,o,n) \
	 AO_compare_and_swap(a,o,n)
#endif

/* Generate test_and_set_full, if necessary and possible.	*/
#if !defined(AO_HAVE_test_and_set) && \
    !defined(AO_HAVE_test_and_set_release) && \
    !defined(AO_HAVE_test_and_set_acquire) && \
    !defined(AO_HAVE_test_and_set_read) && \
    !defined(AO_HAVE_test_and_set_full)
#  if AO_AO_TS_T && defined(AO_HAVE_compare_and_swap_full) || \
      AO_CHAR_TS_T && defined(AO_HAVE_char_compare_and_swap_full)
     AO_INLINE AO_TS_VAL_t
     AO_test_and_set_full(volatile AO_TS_t *addr)
     {
       if (AO_TS_COMPARE_AND_SWAP_FULL(addr, AO_TS_CLEAR, AO_TS_SET))
         return AO_TS_CLEAR;
       else
         return AO_TS_SET;
     }
#    define AO_HAVE_test_and_set_full
#  endif /* AO_HAVE_compare_and_swap_full */

#  if AO_AO_TS_T && defined(AO_HAVE_compare_and_swap_acquire) || \
      AO_CHAR_TS_T && defined(AO_HAVE_char_compare_and_swap_acquire)
     AO_INLINE AO_TS_VAL_t
     AO_test_and_set_acquire(volatile AO_TS_t *addr)
     {
       if (AO_TS_COMPARE_AND_SWAP_ACQUIRE(addr, AO_TS_CLEAR, AO_TS_SET))
         return AO_TS_CLEAR;
       else
         return AO_TS_SET;
     }
#    define AO_HAVE_test_and_set_acquire
#  endif /* AO_HAVE_compare_and_swap_acquire */

#  if AO_AO_TS_T && defined(AO_HAVE_compare_and_swap_release) || \
      AO_CHAR_TS_T && defined(AO_HAVE_char_compare_and_swap_release)
     AO_INLINE AO_TS_VAL_t
     AO_test_and_set_release(volatile AO_TS_t *addr)
     {
       if (AO_TS_COMPARE_AND_SWAP_RELEASE(addr, AO_TS_CLEAR, AO_TS_SET))
         return AO_TS_CLEAR;
       else
         return AO_TS_SET;
     }
#    define AO_HAVE_test_and_set_release
#  endif /* AO_HAVE_compare_and_swap_release */

#  if AO_AO_TS_T && defined(AO_HAVE_compare_and_swap) || \
      AO_CHAR_TS_T && defined(AO_HAVE_char_compare_and_swap)
     AO_INLINE AO_TS_VAL_t
     AO_test_and_set(volatile AO_TS_t *addr)
     {
       if (AO_TS_COMPARE_AND_SWAP(addr, AO_TS_CLEAR, AO_TS_SET))
         return AO_TS_CLEAR;
       else
         return AO_TS_SET;
     }
#    define AO_HAVE_test_and_set
#  endif /* AO_HAVE_compare_and_swap */

#  if defined(AO_HAVE_test_and_set) && defined(AO_HAVE_nop_full) \
      && !defined(AO_HAVE_test_and_set_acquire)
     AO_INLINE AO_TS_VAL_t
     AO_test_and_set_acquire(volatile AO_TS_t *addr)
     {
       AO_TS_VAL_t result = AO_test_and_set(addr);
       AO_nop_full();
       return result;
     }
#    define AO_HAVE_test_and_set_acquire
#  endif

#endif /* No prior test and set */

/* Nop */
#if !defined(AO_HAVE_nop)
   AO_INLINE void AO_nop(void) {}
#  define AO_HAVE_nop
#endif

#if defined(AO_HAVE_test_and_set_full) && !defined(AO_HAVE_nop_full)
   AO_INLINE void
   AO_nop_full()
   {
     AO_TS_t dummy = AO_TS_INITIALIZER;
     AO_test_and_set_full(&dummy);
   }
#  define AO_HAVE_nop_full
#endif

#if defined(AO_HAVE_nop_acquire)
#  error AO_nop_acquire is useless: dont define.
#endif
#if defined(AO_HAVE_nop_release)
#  error AO_nop_release is useless: dont define.
#endif

#if defined(AO_HAVE_nop_full) && !defined(AO_HAVE_nop_read)
#  define AO_nop_read() AO_nop_full()
#  define AO_HAVE_nop_read
#endif

#if defined(AO_HAVE_nop_full) && !defined(AO_HAVE_nop_write)
#  define AO_nop_write() AO_nop_full()
#  define AO_HAVE_nop_write
#endif

/* Load */
#if defined(AO_HAVE_load_full) && !defined(AO_HAVE_load_acquire)
#  define AO_load_acquire(addr) AO_load_full(addr)
#  define AO_HAVE_load_acquire
#endif

#if defined(AO_HAVE_load_acquire) && !defined(AO_HAVE_load)
#  define AO_load(addr) AO_load_acquire(addr)
#  define AO_HAVE_load
#endif

#if defined(AO_HAVE_load_full) && !defined(AO_HAVE_load_read)
#  define AO_load_read(addr) AO_load_full(addr)
#  define AO_HAVE_load_read
#endif

#if !defined(AO_HAVE_load_acquire_read) && defined(AO_HAVE_load_acquire)
#  define AO_load_acquire_read(addr) AO_load_acquire(addr)
#  define AO_HAVE_load_acquire_read
#endif

#if defined(AO_HAVE_load) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_load_acquire)
   AO_INLINE AO_t
   AO_load_acquire(volatile AO_t *addr)
   {
     AO_t result = AO_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_load_acquire
#endif

#if defined(AO_HAVE_load) && defined(AO_HAVE_nop_read) && \
    !defined(AO_HAVE_load_read)
   AO_INLINE AO_t
   AO_load_read(volatile AO_t *addr)
   {
     AO_t result = AO_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_read();
     return result;
   }
#  define AO_HAVE_load_read
#endif

#if defined(AO_HAVE_load_acquire) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_load_full)
#  define AO_load_full(addr) (AO_nop_full(), AO_load_acquire(addr))
#  define AO_HAVE_load_full
#endif
 
#if !defined(AO_HAVE_load_acquire_read) && defined(AO_HAVE_load_read)
#  define AO_load_acquire_read(addr) AO_load_read(addr)
#  define AO_HAVE_load_acquire_read
#endif

#if defined(AO_HAVE_load_acquire_read) && !defined(AO_HAVE_load)
#  define AO_load(addr) AO_load_acquire_read(addr)
#  define AO_HAVE_load
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_load_acquire_read)
#    define AO_load_dd_acquire_read(addr) AO_load_acquire_read(addr)
#    define AO_HAVE_load_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_load)
#    define AO_load_dd_acquire_read(addr) AO_load(addr)
#    define AO_HAVE_load_dd_acquire_read
#  endif
#endif


/* Store */

#if defined(AO_HAVE_store_full) && !defined(AO_HAVE_store_release)
#  define AO_store_release(addr,val) AO_store_full(addr,val)
#  define AO_HAVE_store_release
#endif

#if defined(AO_HAVE_store_release) && !defined(AO_HAVE_store)
#  define AO_store(addr, val) AO_store_release(addr,val)
#  define AO_HAVE_store
#endif

#if defined(AO_HAVE_store_full) && !defined(AO_HAVE_store_write)
#  define AO_store_write(addr,val) AO_store_full(addr,val)
#  define AO_HAVE_store_write
#endif

#if defined(AO_HAVE_store_release) && !defined(AO_HAVE_store_release_write)
#  define AO_store_release_write(addr, val) AO_store_release(addr,val)
#  define AO_HAVE_store_release_write
#endif

#if defined(AO_HAVE_store_write) && !defined(AO_HAVE_store)
#  define AO_store(addr, val) AO_store_write(addr,val)
#  define AO_HAVE_store
#endif

#if defined(AO_HAVE_store) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_store_release)
#  define AO_store_release(addr,val) (AO_nop_full(), AO_store(addr,val))
#  define AO_HAVE_store_release
#endif

#if defined(AO_HAVE_nop_write) && defined(AO_HAVE_store) && \
     !defined(AO_HAVE_store_write)
#  define AO_store_write(addr, val) (AO_nop_write(), AO_store(addr,val))
#  define AO_HAVE_store_write
#endif

#if defined(AO_HAVE_store_write) && !defined(AO_HAVE_store_release_write)
#  define AO_store_release_write(addr, val) AO_store_write(addr,val)
#  define AO_HAVE_store_release_write
#endif

#if defined(AO_HAVE_store_release) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_store_full)
#  define AO_store_full(addr, val) (AO_store_release(addr, val), AO_nop_full())
#  define AO_HAVE_store_full
#endif

  
/* Fetch_and_add */
/* We first try to implement fetch_and_add variants in terms	*/
/* of the corresponding compare_and_swap variants to minimize	*/
/* adding barriers.						*/
#if defined(AO_HAVE_compare_and_swap_full) && \
    !defined(AO_HAVE_fetch_and_add_full)
   AO_INLINE AO_t
   AO_fetch_and_add_full(volatile AO_t *addr, AO_t incr)
   {
     AO_t old;
     do
       {
         old = *addr;
       }
     while (!AO_compare_and_swap_full(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_fetch_and_add_full
#endif

#if defined(AO_HAVE_compare_and_swap_acquire) && \
    !defined(AO_HAVE_fetch_and_add_acquire)
   AO_INLINE AO_t
   AO_fetch_and_add_acquire(volatile AO_t *addr, AO_t incr)
   {
     AO_t old;
     do
       {
         old = *addr;
       }
     while (!AO_compare_and_swap_acquire(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_fetch_and_add_acquire
#endif

#if defined(AO_HAVE_compare_and_swap_release) && \
    !defined(AO_HAVE_fetch_and_add_release)
   AO_INLINE AO_t
   AO_fetch_and_add_release(volatile AO_t *addr, AO_t incr)
   {
     AO_t old;
     do
       {
         old = *addr;
       }
     while (!AO_compare_and_swap_release(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_fetch_and_add_release
#endif

#if defined(AO_HAVE_compare_and_swap) && \
    !defined(AO_HAVE_fetch_and_add)
   AO_INLINE AO_t
   AO_fetch_and_add(volatile AO_t *addr, AO_t incr)
   {
     AO_t old;
     do
       {
         old = *addr;
       }
     while (!AO_compare_and_swap(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_fetch_and_add
#endif

#if defined(AO_HAVE_fetch_and_add_full)
#  if !defined(AO_HAVE_fetch_and_add_release)
#    define AO_fetch_and_add_release(addr, val) \
  	 AO_fetch_and_add_full(addr, val)
#    define AO_HAVE_fetch_and_add_release
#  endif
#  if !defined(AO_HAVE_fetch_and_add_acquire)
#    define AO_fetch_and_add_acquire(addr, val) \
  	 AO_fetch_and_add_full(addr, val)
#    define AO_HAVE_fetch_and_add_acquire
#  endif
#  if !defined(AO_HAVE_fetch_and_add_write)
#    define AO_fetch_and_add_write(addr, val) \
  	 AO_fetch_and_add_full(addr, val)
#    define AO_HAVE_fetch_and_add_write
#  endif
#  if !defined(AO_HAVE_fetch_and_add_read)
#    define AO_fetch_and_add_read(addr, val) \
  	 AO_fetch_and_add_full(addr, val)
#    define AO_HAVE_fetch_and_add_read
#  endif
#endif /* AO_HAVE_fetch_and_add_full */

#if !defined(AO_HAVE_fetch_and_add) && \
    defined(AO_HAVE_fetch_and_add_release)
#  define AO_fetch_and_add(addr, val) \
  	AO_fetch_and_add_release(addr, val)
#  define AO_HAVE_fetch_and_add
#endif
#if !defined(AO_HAVE_fetch_and_add) && \
    defined(AO_HAVE_fetch_and_add_acquire)
#  define AO_fetch_and_add(addr, val) \
  	AO_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_fetch_and_add
#endif
#if !defined(AO_HAVE_fetch_and_add) && \
    defined(AO_HAVE_fetch_and_add_write)
#  define AO_fetch_and_add(addr, val) \
  	AO_fetch_and_add_write(addr, val)
#  define AO_HAVE_fetch_and_add
#endif
#if !defined(AO_HAVE_fetch_and_add) && \
    defined(AO_HAVE_fetch_and_add_read)
#  define AO_fetch_and_add(addr, val) \
  	AO_fetch_and_add_read(addr, val)
#  define AO_HAVE_fetch_and_add
#endif

#if defined(AO_HAVE_fetch_and_add_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_fetch_and_add_full)
#  define AO_fetch_and_add_full(addr, val) \
  	(AO_nop_full(), AO_fetch_and_add_acquire(addr, val))
#  define AO_HAVE_fetch_and_add_full
#endif

#if !defined(AO_HAVE_fetch_and_add_release_write) && \
    defined(AO_HAVE_fetch_and_add_write)
#  define AO_fetch_and_add_release_write(addr, val) \
  	AO_fetch_and_add_write(addr, val)
#  define AO_HAVE_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_fetch_and_add_release_write) && \
    defined(AO_HAVE_fetch_and_add_release)
#  define AO_fetch_and_add_release_write(addr, val) \
  	AO_fetch_and_add_release(addr, val)
#  define AO_HAVE_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_fetch_and_add_read)
#  define AO_fetch_and_add_acquire_read(addr, val) \
  	AO_fetch_and_add_read(addr, val)
#  define AO_HAVE_fetch_and_add_acquire_read
#endif
#if !defined(AO_HAVE_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_fetch_and_add_acquire)
#  define AO_fetch_and_add_acquire_read(addr, val) \
  	AO_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_fetch_and_add_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_fetch_and_add_acquire_read)
#    define AO_fetch_and_add_dd_acquire_read(addr, val) \
	AO_fetch_and_add_acquire_read(addr, val)
#    define AO_HAVE_fetch_and_add_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_fetch_and_add)
#    define AO_fetch_and_add_dd_acquire_read(addr, val) \
	AO_fetch_and_add(addr, val)
#    define AO_HAVE_fetch_and_add_dd_acquire_read
#  endif
#endif
  
/* Fetch_and_add1 */

#if defined(AO_HAVE_fetch_and_add_full) &&\
    !defined(AO_HAVE_fetch_and_add1_full)
#  define AO_fetch_and_add1_full(addr) AO_fetch_and_add_full(addr,1)
#  define AO_HAVE_fetch_and_add1_full
#endif
#if defined(AO_HAVE_fetch_and_add_release) &&\
    !defined(AO_HAVE_fetch_and_add1_release)
#  define AO_fetch_and_add1_release(addr) AO_fetch_and_add_release(addr,1)
#  define AO_HAVE_fetch_and_add1_release
#endif
#if defined(AO_HAVE_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_fetch_and_add1_acquire)
#  define AO_fetch_and_add1_acquire(addr) AO_fetch_and_add_acquire(addr,1)
#  define AO_HAVE_fetch_and_add1_acquire
#endif
#if defined(AO_HAVE_fetch_and_add_write) &&\
    !defined(AO_HAVE_fetch_and_add1_write)
#  define AO_fetch_and_add1_write(addr) AO_fetch_and_add_write(addr,1)
#  define AO_HAVE_fetch_and_add1_write
#endif
#if defined(AO_HAVE_fetch_and_add_read) &&\
    !defined(AO_HAVE_fetch_and_add1_read)
#  define AO_fetch_and_add1_read(addr) AO_fetch_and_add_read(addr,1)
#  define AO_HAVE_fetch_and_add1_read
#endif
#if defined(AO_HAVE_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_fetch_and_add1_release_write)
#  define AO_fetch_and_add1_release_write(addr) \
	AO_fetch_and_add_release_write(addr,1)
#  define AO_HAVE_fetch_and_add1_release_write
#endif
#if defined(AO_HAVE_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_fetch_and_add1_acquire_read)
#  define AO_fetch_and_add1_acquire_read(addr) \
	AO_fetch_and_add_acquire_read(addr,1)
#  define AO_HAVE_fetch_and_add1_acquire_read
#endif
#if defined(AO_HAVE_fetch_and_add) &&\
    !defined(AO_HAVE_fetch_and_add1)
#  define AO_fetch_and_add1(addr) \
	AO_fetch_and_add(addr,1)
#  define AO_HAVE_fetch_and_add1
#endif

#if defined(AO_HAVE_fetch_and_add1_full)
#  if !defined(AO_HAVE_fetch_and_add1_release)
#    define AO_fetch_and_add1_release(addr) \
  	 AO_fetch_and_add1_full(addr)
#    define AO_HAVE_fetch_and_add1_release
#  endif
#  if !defined(AO_HAVE_fetch_and_add1_acquire)
#    define AO_fetch_and_add1_acquire(addr) \
  	 AO_fetch_and_add1_full(addr)
#    define AO_HAVE_fetch_and_add1_acquire
#  endif
#  if !defined(AO_HAVE_fetch_and_add1_write)
#    define AO_fetch_and_add1_write(addr) \
  	 AO_fetch_and_add1_full(addr)
#    define AO_HAVE_fetch_and_add1_write
#  endif
#  if !defined(AO_HAVE_fetch_and_add1_read)
#    define AO_fetch_and_add1_read(addr) \
  	 AO_fetch_and_add1_full(addr)
#    define AO_HAVE_fetch_and_add1_read
#  endif
#endif /* AO_HAVE_fetch_and_add1_full */

#if !defined(AO_HAVE_fetch_and_add1) && \
    defined(AO_HAVE_fetch_and_add1_release)
#  define AO_fetch_and_add1(addr) \
  	AO_fetch_and_add1_release(addr)
#  define AO_HAVE_fetch_and_add1
#endif
#if !defined(AO_HAVE_fetch_and_add1) && \
    defined(AO_HAVE_fetch_and_add1_acquire)
#  define AO_fetch_and_add1(addr) \
  	AO_fetch_and_add1_acquire(addr)
#  define AO_HAVE_fetch_and_add1
#endif
#if !defined(AO_HAVE_fetch_and_add1) && \
    defined(AO_HAVE_fetch_and_add1_write)
#  define AO_fetch_and_add1(addr) \
  	AO_fetch_and_add1_write(addr)
#  define AO_HAVE_fetch_and_add1
#endif
#if !defined(AO_HAVE_fetch_and_add1) && \
    defined(AO_HAVE_fetch_and_add1_read)
#  define AO_fetch_and_add1(addr) \
  	AO_fetch_and_add1_read(addr)
#  define AO_HAVE_fetch_and_add1
#endif

#if defined(AO_HAVE_fetch_and_add1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_fetch_and_add1_full)
#  define AO_fetch_and_add1_full(addr) \
  	(AO_nop_full(), AO_fetch_and_add1_acquire(addr))
#  define AO_HAVE_fetch_and_add1_full
#endif

#if !defined(AO_HAVE_fetch_and_add1_release_write) && \
    defined(AO_HAVE_fetch_and_add1_write)
#  define AO_fetch_and_add1_release_write(addr) \
  	AO_fetch_and_add1_write(addr)
#  define AO_HAVE_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_fetch_and_add1_release_write) && \
    defined(AO_HAVE_fetch_and_add1_release)
#  define AO_fetch_and_add1_release_write(addr) \
  	AO_fetch_and_add1_release(addr)
#  define AO_HAVE_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_fetch_and_add1_read)
#  define AO_fetch_and_add1_acquire_read(addr) \
  	AO_fetch_and_add1_read(addr)
#  define AO_HAVE_fetch_and_add1_acquire_read
#endif
#if !defined(AO_HAVE_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_fetch_and_add1_acquire)
#  define AO_fetch_and_add1_acquire_read(addr) \
  	AO_fetch_and_add1_acquire(addr)
#  define AO_HAVE_fetch_and_add1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_fetch_and_add1_acquire_read)
#    define AO_fetch_and_add1_dd_acquire_read(addr) \
	AO_fetch_and_add1_acquire_read(addr)
#    define AO_HAVE_fetch_and_add1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_fetch_and_add1)
#    define AO_fetch_and_add1_dd_acquire_read(addr) AO_fetch_and_add1(addr)
#    define AO_HAVE_fetch_and_add1_dd_acquire_read
#  endif
#endif

/* Fetch_and_sub1 */

#if defined(AO_HAVE_fetch_and_add_full) &&\
    !defined(AO_HAVE_fetch_and_sub1_full)
#  define AO_fetch_and_sub1_full(addr) AO_fetch_and_add_full(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_full
#endif
#if defined(AO_HAVE_fetch_and_add_release) &&\
    !defined(AO_HAVE_fetch_and_sub1_release)
#  define AO_fetch_and_sub1_release(addr) \
	AO_fetch_and_add_release(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_release
#endif
#if defined(AO_HAVE_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_fetch_and_sub1_acquire)
#  define AO_fetch_and_sub1_acquire(addr) \
	AO_fetch_and_add_acquire(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_acquire
#endif
#if defined(AO_HAVE_fetch_and_add_write) &&\
    !defined(AO_HAVE_fetch_and_sub1_write)
#  define AO_fetch_and_sub1_write(addr) \
	AO_fetch_and_add_write(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_write
#endif
#if defined(AO_HAVE_fetch_and_add_read) &&\
    !defined(AO_HAVE_fetch_and_sub1_read)
#  define AO_fetch_and_sub1_read(addr) \
	AO_fetch_and_add_read(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_read
#endif
#if defined(AO_HAVE_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_fetch_and_sub1_release_write)
#  define AO_fetch_and_sub1_release_write(addr) \
	AO_fetch_and_add_release_write(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_release_write
#endif
#if defined(AO_HAVE_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_fetch_and_sub1_acquire_read)
#  define AO_fetch_and_sub1_acquire_read(addr) \
	AO_fetch_and_add_acquire_read(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1_acquire_read
#endif
#if defined(AO_HAVE_fetch_and_add) &&\
    !defined(AO_HAVE_fetch_and_sub1)
#  define AO_fetch_and_sub1(addr) \
	AO_fetch_and_add(addr,(AO_t)(-1))
#  define AO_HAVE_fetch_and_sub1
#endif

#if defined(AO_HAVE_fetch_and_sub1_full)
#  if !defined(AO_HAVE_fetch_and_sub1_release)
#    define AO_fetch_and_sub1_release(addr) \
  	 AO_fetch_and_sub1_full(addr)
#    define AO_HAVE_fetch_and_sub1_release
#  endif
#  if !defined(AO_HAVE_fetch_and_sub1_acquire)
#    define AO_fetch_and_sub1_acquire(addr) \
  	 AO_fetch_and_sub1_full(addr)
#    define AO_HAVE_fetch_and_sub1_acquire
#  endif
#  if !defined(AO_HAVE_fetch_and_sub1_write)
#    define AO_fetch_and_sub1_write(addr) \
  	 AO_fetch_and_sub1_full(addr)
#    define AO_HAVE_fetch_and_sub1_write
#  endif
#  if !defined(AO_HAVE_fetch_and_sub1_read)
#    define AO_fetch_and_sub1_read(addr) \
  	 AO_fetch_and_sub1_full(addr)
#    define AO_HAVE_fetch_and_sub1_read
#  endif
#endif /* AO_HAVE_fetch_and_sub1_full */

#if !defined(AO_HAVE_fetch_and_sub1) && \
    defined(AO_HAVE_fetch_and_sub1_release)
#  define AO_fetch_and_sub1(addr) \
  	AO_fetch_and_sub1_release(addr)
#  define AO_HAVE_fetch_and_sub1
#endif
#if !defined(AO_HAVE_fetch_and_sub1) && \
    defined(AO_HAVE_fetch_and_sub1_acquire)
#  define AO_fetch_and_sub1(addr) \
  	AO_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_fetch_and_sub1
#endif
#if !defined(AO_HAVE_fetch_and_sub1) && \
    defined(AO_HAVE_fetch_and_sub1_write)
#  define AO_fetch_and_sub1(addr) \
  	AO_fetch_and_sub1_write(addr)
#  define AO_HAVE_fetch_and_sub1
#endif
#if !defined(AO_HAVE_fetch_and_sub1) && \
    defined(AO_HAVE_fetch_and_sub1_read)
#  define AO_fetch_and_sub1(addr) \
  	AO_fetch_and_sub1_read(addr)
#  define AO_HAVE_fetch_and_sub1
#endif

#if defined(AO_HAVE_fetch_and_sub1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_fetch_and_sub1_full)
#  define AO_fetch_and_sub1_full(addr) \
  	(AO_nop_full(), AO_fetch_and_sub1_acquire(addr))
#  define AO_HAVE_fetch_and_sub1_full
#endif

#if !defined(AO_HAVE_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_fetch_and_sub1_write)
#  define AO_fetch_and_sub1_release_write(addr) \
  	AO_fetch_and_sub1_write(addr)
#  define AO_HAVE_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_fetch_and_sub1_release)
#  define AO_fetch_and_sub1_release_write(addr) \
  	AO_fetch_and_sub1_release(addr)
#  define AO_HAVE_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_fetch_and_sub1_read)
#  define AO_fetch_and_sub1_acquire_read(addr) \
  	AO_fetch_and_sub1_read(addr)
#  define AO_HAVE_fetch_and_sub1_acquire_read
#endif
#if !defined(AO_HAVE_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_fetch_and_sub1_acquire)
#  define AO_fetch_and_sub1_acquire_read(addr) \
  	AO_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_fetch_and_sub1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_fetch_and_sub1_acquire_read)
#    define AO_fetch_and_sub1_dd_acquire_read(addr) \
	AO_fetch_and_sub1_acquire_read(addr)
#    define AO_HAVE_fetch_and_sub1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_fetch_and_sub1)
#    define AO_fetch_and_sub1_dd_acquire_read(addr) AO_fetch_and_sub1(addr)
#    define AO_HAVE_fetch_and_sub1_dd_acquire_read
#  endif
#endif

/* Atomic or */
#if defined(AO_HAVE_compare_and_swap_full) && \
    !defined(AO_HAVE_or_full)
   AO_INLINE void
   AO_or_full(volatile AO_t *addr, AO_t incr)
   {
     AO_t old;
     do
       {
         old = *addr;
       }
     while (!AO_compare_and_swap_full(addr, old, (old | incr)));
   }
#  define AO_HAVE_or_full
#endif

#if defined(AO_HAVE_or_full)
#  if !defined(AO_HAVE_or_release)
#    define AO_or_release(addr, val) \
  	 AO_or_full(addr, val)
#    define AO_HAVE_or_release
#  endif
#  if !defined(AO_HAVE_or_acquire)
#    define AO_or_acquire(addr, val) \
  	 AO_or_full(addr, val)
#    define AO_HAVE_or_acquire
#  endif
#  if !defined(AO_HAVE_or_write)
#    define AO_or_write(addr, val) \
  	 AO_or_full(addr, val)
#    define AO_HAVE_or_write
#  endif
#  if !defined(AO_HAVE_or_read)
#    define AO_or_read(addr, val) \
  	 AO_or_full(addr, val)
#    define AO_HAVE_or_read
#  endif
#endif /* AO_HAVE_or_full */

#if !defined(AO_HAVE_or) && \
    defined(AO_HAVE_or_release)
#  define AO_or(addr, val) \
  	AO_or_release(addr, val)
#  define AO_HAVE_or
#endif
#if !defined(AO_HAVE_or) && \
    defined(AO_HAVE_or_acquire)
#  define AO_or(addr, val) \
  	AO_or_acquire(addr, val)
#  define AO_HAVE_or
#endif
#if !defined(AO_HAVE_or) && \
    defined(AO_HAVE_or_write)
#  define AO_or(addr, val) \
  	AO_or_write(addr, val)
#  define AO_HAVE_or
#endif
#if !defined(AO_HAVE_or) && \
    defined(AO_HAVE_or_read)
#  define AO_or(addr, val) \
  	AO_or_read(addr, val)
#  define AO_HAVE_or
#endif

#if defined(AO_HAVE_or_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_or_full)
#  define AO_or_full(addr, val) \
  	(AO_nop_full(), AO_or_acquire(addr, val))
#endif

#if !defined(AO_HAVE_or_release_write) && \
    defined(AO_HAVE_or_write)
#  define AO_or_release_write(addr, val) \
  	AO_or_write(addr, val)
#  define AO_HAVE_or_release_write
#endif
#if !defined(AO_HAVE_or_release_write) && \
    defined(AO_HAVE_or_release)
#  define AO_or_release_write(addr, val) \
  	AO_or_release(addr, val)
#  define AO_HAVE_or_release_write
#endif
#if !defined(AO_HAVE_or_acquire_read) && \
    defined(AO_HAVE_or_read)
#  define AO_or_acquire_read(addr, val) \
  	AO_or_read(addr, val)
#  define AO_HAVE_or_acquire_read
#endif
#if !defined(AO_HAVE_or_acquire_read) && \
    defined(AO_HAVE_or_acquire)
#  define AO_or_acquire_read(addr, val) \
  	AO_or_acquire(addr, val)
#  define AO_HAVE_or_acquire_read
#endif

/* dd_aquire_read is meaningless.	*/
  
/* Test_and_set */
  
#if defined(AO_HAVE_test_and_set_full)
#  if !defined(AO_HAVE_test_and_set_release)
#    define AO_test_and_set_release(addr) \
  	 AO_test_and_set_full(addr)
#    define AO_HAVE_test_and_set_release
#  endif
#  if !defined(AO_HAVE_test_and_set_acquire)
#    define AO_test_and_set_acquire(addr) \
  	 AO_test_and_set_full(addr)
#    define AO_HAVE_test_and_set_acquire
#  endif
#  if !defined(AO_HAVE_test_and_set_write)
#    define AO_test_and_set_write(addr) \
  	 AO_test_and_set_full(addr)
#    define AO_HAVE_test_and_set_write
#  endif
#  if !defined(AO_HAVE_test_and_set_read)
#    define AO_test_and_set_read(addr) \
  	 AO_test_and_set_full(addr)
#    define AO_HAVE_test_and_set_read
#  endif
#endif /* AO_HAVE_test_and_set_full */

#if !defined(AO_HAVE_test_and_set) && \
    defined(AO_HAVE_test_and_set_release)
#  define AO_test_and_set(addr) \
  	AO_test_and_set_release(addr)
#  define AO_HAVE_test_and_set
#endif
#if !defined(AO_HAVE_test_and_set) && \
    defined(AO_HAVE_test_and_set_acquire)
#  define AO_test_and_set(addr) \
  	AO_test_and_set_acquire(addr)
#  define AO_HAVE_test_and_set
#endif
#if !defined(AO_HAVE_test_and_set) && \
    defined(AO_HAVE_test_and_set_write)
#  define AO_test_and_set(addr) \
  	AO_test_and_set_write(addr)
#  define AO_HAVE_test_and_set
#endif
#if !defined(AO_HAVE_test_and_set) && \
    defined(AO_HAVE_test_and_set_read)
#  define AO_test_and_set(addr) \
  	AO_test_and_set_read(addr)
#  define AO_HAVE_test_and_set
#endif

#if defined(AO_HAVE_test_and_set_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_test_and_set_full)
#  define AO_test_and_set_full(addr) \
  	(AO_nop_full(), AO_test_and_set_acquire(addr))
#  define AO_HAVE_test_and_set_full
#endif

#if !defined(AO_HAVE_test_and_set_release_write) && \
    defined(AO_HAVE_test_and_set_write)
#  define AO_test_and_set_release_write(addr) \
  	AO_test_and_set_write(addr)
#  define AO_HAVE_test_and_set_release_write
#endif
#if !defined(AO_HAVE_test_and_set_release_write) && \
    defined(AO_HAVE_test_and_set_release)
#  define AO_test_and_set_release_write(addr) \
  	AO_test_and_set_release(addr)
#  define AO_HAVE_test_and_set_release_write
#endif
#if !defined(AO_HAVE_test_and_set_acquire_read) && \
    defined(AO_HAVE_test_and_set_read)
#  define AO_test_and_set_acquire_read(addr) \
  	AO_test_and_set_read(addr)
#  define AO_HAVE_test_and_set_acquire_read
#endif
#if !defined(AO_HAVE_test_and_set_acquire_read) && \
    defined(AO_HAVE_test_and_set_acquire)
#  define AO_test_and_set_acquire_read(addr) \
  	AO_test_and_set_acquire(addr)
#  define AO_HAVE_test_and_set_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_test_and_set_acquire_read)
#    define AO_test_and_set_dd_acquire_read(addr) \
	AO_test_and_set_acquire_read(addr)
#    define AO_HAVE_test_and_set_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_test_and_set)
#    define AO_test_and_set_dd_acquire_read(addr) AO_test_and_set(addr)
#    define AO_HAVE_test_and_set_dd_acquire_read
#  endif
#endif

/* Compare_and_swap */
#if defined(AO_HAVE_compare_and_swap) && defined(AO_HAVE_nop_full)\
    && !defined(AO_HAVE_compare_and_swap_acquire)
   AO_INLINE int
   AO_compare_and_swap_acquire(volatile AO_t *addr, AO_t old, AO_t new_val)
   {
     int result = AO_compare_and_swap(addr, old, new_val);
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_compare_and_swap_acquire
#endif
#if defined(AO_HAVE_compare_and_swap) && defined(AO_HAVE_nop_full)\
    && !defined(AO_HAVE_compare_and_swap_release)
#  define AO_compare_and_swap_release(addr, old, new_val) \
  	(AO_nop_full(), AO_compare_and_swap(addr, old, new_val))
#  define AO_HAVE_compare_and_swap_release
#endif
#if defined(AO_HAVE_compare_and_swap_full)
#  if !defined(AO_HAVE_compare_and_swap_release)
#    define AO_compare_and_swap_release(addr, old, new_val) \
  	 AO_compare_and_swap_full(addr, old, new_val)
#    define AO_HAVE_compare_and_swap_release
#  endif
#  if !defined(AO_HAVE_compare_and_swap_acquire)
#    define AO_compare_and_swap_acquire(addr, old, new_val) \
  	 AO_compare_and_swap_full(addr, old, new_val)
#    define AO_HAVE_compare_and_swap_acquire
#  endif
#  if !defined(AO_HAVE_compare_and_swap_write)
#    define AO_compare_and_swap_write(addr, old, new_val) \
  	 AO_compare_and_swap_full(addr, old, new_val)
#    define AO_HAVE_compare_and_swap_write
#  endif
#  if !defined(AO_HAVE_compare_and_swap_read)
#    define AO_compare_and_swap_read(addr, old, new_val) \
  	 AO_compare_and_swap_full(addr, old, new_val)
#    define AO_HAVE_compare_and_swap_read
#  endif
#endif /* AO_HAVE_compare_and_swap_full */

#if !defined(AO_HAVE_compare_and_swap) && \
    defined(AO_HAVE_compare_and_swap_release)
#  define AO_compare_and_swap(addr, old, new_val) \
  	AO_compare_and_swap_release(addr, old, new_val)
#  define AO_HAVE_compare_and_swap
#endif
#if !defined(AO_HAVE_compare_and_swap) && \
    defined(AO_HAVE_compare_and_swap_acquire)
#  define AO_compare_and_swap(addr, old, new_val) \
  	AO_compare_and_swap_acquire(addr, old, new_val)
#  define AO_HAVE_compare_and_swap
#endif
#if !defined(AO_HAVE_compare_and_swap) && \
    defined(AO_HAVE_compare_and_swap_write)
#  define AO_compare_and_swap(addr, old, new_val) \
  	AO_compare_and_swap_write(addr, old, new_val)
#  define AO_HAVE_compare_and_swap
#endif
#if !defined(AO_HAVE_compare_and_swap) && \
    defined(AO_HAVE_compare_and_swap_read)
#  define AO_compare_and_swap(addr, old, new_val) \
  	AO_compare_and_swap_read(addr, old, new_val)
#  define AO_HAVE_compare_and_swap
#endif

#if defined(AO_HAVE_compare_and_swap_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_compare_and_swap_full)
#  define AO_compare_and_swap_full(addr, old, new_val) \
  	(AO_nop_full(), AO_compare_and_swap_acquire(addr, old, new_val))
#  define AO_HAVE_compare_and_swap_full
#endif

#if !defined(AO_HAVE_compare_and_swap_release_write) && \
    defined(AO_HAVE_compare_and_swap_write)
#  define AO_compare_and_swap_release_write(addr, old, new_val) \
  	AO_compare_and_swap_write(addr, old, new_val)
#  define AO_HAVE_compare_and_swap_release_write
#endif
#if !defined(AO_HAVE_compare_and_swap_release_write) && \
    defined(AO_HAVE_compare_and_swap_release)
#  define AO_compare_and_swap_release_write(addr, old, new_val) \
  	AO_compare_and_swap_release(addr, old, new_val)
#  define AO_HAVE_compare_and_swap_release_write
#endif
#if !defined(AO_HAVE_compare_and_swap_acquire_read) && \
    defined(AO_HAVE_compare_and_swap_read)
#  define AO_compare_and_swap_acquire_read(addr, old, new_val) \
  	AO_compare_and_swap_read(addr, old, new_val)
#  define AO_HAVE_compare_and_swap_acquire_read
#endif
#if !defined(AO_HAVE_compare_and_swap_acquire_read) && \
    defined(AO_HAVE_compare_and_swap_acquire)
#  define AO_compare_and_swap_acquire_read(addr, old, new_val) \
  	AO_compare_and_swap_acquire(addr, old, new_val)
#  define AO_HAVE_compare_and_swap_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_compare_and_swap_acquire_read)
#    define AO_compare_and_swap_dd_acquire_read(addr, old, new_val) \
	AO_compare_and_swap_acquire_read(addr, old, new_val)
#    define AO_HAVE_compare_and_swap_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_compare_and_swap)
#    define AO_compare_and_swap_dd_acquire_read(addr, old, new_val) \
	AO_compare_and_swap(addr, old, new_val)
#    define AO_HAVE_compare_and_swap_dd_acquire_read
#  endif
#endif

#include "generalize-small.h"

/* Compare_double_and_swap_double */
#if defined(AO_HAVE_compare_double_and_swap_double) && defined(AO_HAVE_nop_full)\
    && !defined(AO_HAVE_compare_double_and_swap_double_acquire)
   AO_INLINE int
   AO_compare_double_and_swap_double_acquire(volatile AO_double_t *addr,
		   		      	     AO_t o1, AO_t o2,
				             AO_t n1, AO_t n2)
   {
     int result = AO_compare_double_and_swap_double(addr, o1, o2, n1, n2);
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_compare_double_and_swap_double_acquire
#endif
#if defined(AO_HAVE_compare_double_and_swap_double) \
    && defined(AO_HAVE_nop_full)\
    && !defined(AO_HAVE_compare_double_and_swap_double_release)
#  define AO_compare_double_and_swap_double_release(addr, o1, o2, n1, n2) \
  	(AO_nop_full(), AO_compare_double_and_swap_double(addr, o1, o2, n1, n2))
#  define AO_HAVE_compare_double_and_swap_double_release
#endif
#if defined(AO_HAVE_compare_double_and_swap_double_full)
#  if !defined(AO_HAVE_compare_double_and_swap_double_release)
#    define AO_compare_double_and_swap_double_release(addr, o1, o2, n1, n2) \
  	 AO_compare_double_and_swap_double_full(addr, o1, o2, n1, n2)
#    define AO_HAVE_compare_double_and_swap_double_release
#  endif
#  if !defined(AO_HAVE_compare_double_and_swap_double_acquire)
#    define AO_compare_double_and_swap_double_acquire(addr, o1, o2, n1, n2) \
  	 AO_compare_double_and_swap_double_full(addr, o1, o2, n1, n2)
#    define AO_HAVE_compare_double_and_swap_double_acquire
#  endif
#  if !defined(AO_HAVE_compare_double_and_swap_double_write)
#    define AO_compare_double_and_swap_double_write(addr, o1, o2, n1, n2) \
  	 AO_compare_double_and_swap_double_full(addr, o1, o2, n1, n2)
#    define AO_HAVE_compare_double_and_swap_double_write
#  endif
#  if !defined(AO_HAVE_compare_double_and_swap_double_read)
#    define AO_compare_double_and_swap_double_read(addr, o1, o2, n1, n2) \
  	 AO_compare_double_and_swap_double_full(addr, o1, o2, n1, n2)
#    define AO_HAVE_compare_double_and_swap_double_read
#  endif
#endif /* AO_HAVE_compare_double_and_swap_double_full */

#if !defined(AO_HAVE_compare_double_and_swap_double) && \
    defined(AO_HAVE_compare_double_and_swap_double_release)
#  define AO_compare_double_and_swap_double(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_release(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double
#endif
#if !defined(AO_HAVE_compare_double_and_swap_double) && \
    defined(AO_HAVE_compare_double_and_swap_double_acquire)
#  define AO_compare_double_and_swap_double(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_acquire(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double
#endif
#if !defined(AO_HAVE_compare_double_and_swap_double) && \
    defined(AO_HAVE_compare_double_and_swap_double_write)
#  define AO_compare_double_and_swap_double(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_write(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double
#endif
#if !defined(AO_HAVE_compare_double_and_swap_double) && \
    defined(AO_HAVE_compare_double_and_swap_double_read)
#  define AO_compare_double_and_swap_double(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_read(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double
#endif

#if defined(AO_HAVE_compare_double_and_swap_double_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_compare_double_and_swap_double_full)
#  define AO_compare_double_and_swap_double_full(addr, o1, o2, n1, n2) \
  	(AO_nop_full(), AO_compare_double_and_swap_double_acquire(addr, o1, o2, n1, n2))
#  define AO_HAVE_compare_double_and_swap_double_full
#endif

#if !defined(AO_HAVE_compare_double_and_swap_double_release_write) && \
    defined(AO_HAVE_compare_double_and_swap_double_write)
#  define AO_compare_double_and_swap_double_release_write(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_write(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double_release_write
#endif
#if !defined(AO_HAVE_compare_double_and_swap_double_release_write) && \
    defined(AO_HAVE_compare_double_and_swap_double_release)
#  define AO_compare_double_and_swap_double_release_write(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_release(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double_release_write
#endif
#if !defined(AO_HAVE_compare_double_and_swap_double_acquire_read) && \
    defined(AO_HAVE_compare_double_and_swap_double_read)
#  define AO_compare_double_and_swap_double_acquire_read(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_read(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double_acquire_read
#endif
#if !defined(AO_HAVE_compare_double_and_swap_double_acquire_read) && \
    defined(AO_HAVE_compare_double_and_swap_double_acquire)
#  define AO_compare_double_and_swap_double_acquire_read(addr, o1, o2, n1, n2) \
  	AO_compare_double_and_swap_double_acquire(addr, o1, o2, n1, n2)
#  define AO_HAVE_compare_double_and_swap_double_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_compare_double_and_swap_double_acquire_read)
#    define AO_compare_double_and_swap_double_dd_acquire_read(addr, o1, o2, n1, n2) \
	AO_compare_double_and_swap_double_acquire_read(addr, o1, o2, n1, n2)
#    define AO_HAVE_compare_double_and_swap_double_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_compare_double_and_swap_double)
#    define AO_compare_double_and_swap_double_dd_acquire_read(addr, o1, o2, n1, n2) \
	AO_compare_double_and_swap_double(addr, o1, o2, n1, n2)
#    define AO_HAVE_compare_double_and_swap_double_dd_acquire_read
#  endif
#endif

/* Compare_and_swap_double */
#if defined(AO_HAVE_compare_and_swap_double) && defined(AO_HAVE_nop_full)\
    && !defined(AO_HAVE_compare_and_swap_double_acquire)
   AO_INLINE int
   AO_compare_and_swap_double_acquire(volatile AO_double_t *addr,
		   		      	     AO_t o1, AO_t o2,
				             AO_t n1, AO_t n2)
   {
     int result = AO_compare_and_swap_double(addr, o1, n1, n2);
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_compare_and_swap_double_acquire
#endif
#if defined(AO_HAVE_compare_and_swap_double) \
    && defined(AO_HAVE_nop_full)\
    && !defined(AO_HAVE_compare_and_swap_double_release)
#  define AO_compare_and_swap_double_release(addr, o1, n1, n2) \
  	(AO_nop_full(), AO_compare_and_swap_double(addr, o1, n1, n2))
#  define AO_HAVE_compare_and_swap_double_release
#endif
#if defined(AO_HAVE_compare_and_swap_double_full)
#  if !defined(AO_HAVE_compare_and_swap_double_release)
#    define AO_compare_and_swap_double_release(addr, o1, n1, n2) \
  	 AO_compare_and_swap_double_full(addr, o1, n1, n2)
#    define AO_HAVE_compare_and_swap_double_release
#  endif
#  if !defined(AO_HAVE_compare_and_swap_double_acquire)
#    define AO_compare_and_swap_double_acquire(addr, o1, n1, n2) \
  	 AO_compare_and_swap_double_full(addr, o1, n1, n2)
#    define AO_HAVE_compare_and_swap_double_acquire
#  endif
#  if !defined(AO_HAVE_compare_and_swap_double_write)
#    define AO_compare_and_swap_double_write(addr, o1, n1, n2) \
  	 AO_compare_and_swap_double_full(addr, o1, n1, n2)
#    define AO_HAVE_compare_and_swap_double_write
#  endif
#  if !defined(AO_HAVE_compare_and_swap_double_read)
#    define AO_compare_and_swap_double_read(addr, o1, n1, n2) \
  	 AO_compare_and_swap_double_full(addr, o1, n1, n2)
#    define AO_HAVE_compare_and_swap_double_read
#  endif
#endif /* AO_HAVE_compare_and_swap_double_full */

#if !defined(AO_HAVE_compare_and_swap_double) && \
    defined(AO_HAVE_compare_and_swap_double_release)
#  define AO_compare_and_swap_double(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_release(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double
#endif
#if !defined(AO_HAVE_compare_and_swap_double) && \
    defined(AO_HAVE_compare_and_swap_double_acquire)
#  define AO_compare_and_swap_double(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_acquire(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double
#endif
#if !defined(AO_HAVE_compare_and_swap_double) && \
    defined(AO_HAVE_compare_and_swap_double_write)
#  define AO_compare_and_swap_double(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_write(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double
#endif
#if !defined(AO_HAVE_compare_and_swap_double) && \
    defined(AO_HAVE_compare_and_swap_double_read)
#  define AO_compare_and_swap_double(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_read(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double
#endif

#if defined(AO_HAVE_compare_and_swap_double_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_compare_and_swap_double_full)
#  define AO_compare_and_swap_double_full(addr, o1, n1, n2) \
  	(AO_nop_full(), AO_compare_and_swap_double_acquire(addr, o1, n1, n2))
#  define AO_HAVE_compare_and_swap_double_full
#endif

#if !defined(AO_HAVE_compare_and_swap_double_release_write) && \
    defined(AO_HAVE_compare_and_swap_double_write)
#  define AO_compare_and_swap_double_release_write(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_write(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double_release_write
#endif
#if !defined(AO_HAVE_compare_and_swap_double_release_write) && \
    defined(AO_HAVE_compare_and_swap_double_release)
#  define AO_compare_and_swap_double_release_write(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_release(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double_release_write
#endif
#if !defined(AO_HAVE_compare_and_swap_double_acquire_read) && \
    defined(AO_HAVE_compare_and_swap_double_read)
#  define AO_compare_and_swap_double_acquire_read(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_read(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double_acquire_read
#endif
#if !defined(AO_HAVE_compare_and_swap_double_acquire_read) && \
    defined(AO_HAVE_compare_and_swap_double_acquire)
#  define AO_compare_and_swap_double_acquire_read(addr, o1, n1, n2) \
  	AO_compare_and_swap_double_acquire(addr, o1, n1, n2)
#  define AO_HAVE_compare_and_swap_double_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_compare_and_swap_double_acquire_read)
#    define AO_compare_and_swap_double_dd_acquire_read(addr, o1, n1, n2) \
	AO_compare_and_swap_double_acquire_read(addr, o1, n1, n2)
#    define AO_HAVE_compare_and_swap_double_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_compare_and_swap_double)
#    define AO_compare_and_swap_double_dd_acquire_read(addr, o1, n1, n2) \
	AO_compare_and_swap_double(addr, o1, n1, n2)
#    define AO_HAVE_compare_and_swap_double_dd_acquire_read
#  endif
#endif
