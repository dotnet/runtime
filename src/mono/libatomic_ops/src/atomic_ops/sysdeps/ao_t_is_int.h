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
 * Inclusion of this file signifies that AO_t is in fact int.  Hence
 * any AO_... operations can also server as AO_int_... operations.
 * We currently define only the more important ones here, and allow for
 * the normal generalization process to define the others.
 * We should probably add others in the future.
 */

#if defined(AO_HAVE_compare_and_swap_full) && \
    !defined(AO_HAVE_int_compare_and_swap_full)
#  define AO_int_compare_and_swap_full(addr, old, new_val) \
		AO_compare_and_swap_full((volatile AO_t *)addr, \
				         (AO_t) old, (AO_t)new_val)
#  define AO_HAVE_int_compare_and_swap_full
# endif

#if defined(AO_HAVE_compare_and_swap_acquire) && \
    !defined(AO_HAVE_int_compare_and_swap_acquire)
#  define AO_int_compare_and_swap_acquire(addr, old, new_val) \
		AO_compare_and_swap_acquire((volatile AO_t *)addr, \
				            (AO_t) old, (AO_t)new_val)
#  define AO_HAVE_int_compare_and_swap_acquire
# endif

#if defined(AO_HAVE_compare_and_swap_release) && \
    !defined(AO_HAVE_int_compare_and_swap_release)
#  define AO_int_compare_and_swap_release(addr, old, new_val) \
		AO_compare_and_swap_release((volatile AO_t *)addr, \
				         (AO_t) old, (AO_t)new_val)
#  define AO_HAVE_int_compare_and_swap_release
# endif

#if defined(AO_HAVE_compare_and_swap_write) && \
    !defined(AO_HAVE_int_compare_and_swap_write)
#  define AO_int_compare_and_swap_write(addr, old, new_val) \
		AO_compare_and_swap_write((volatile AO_t *)addr, \
				          (AO_t) old, (AO_t)new_val)
#  define AO_HAVE_int_compare_and_swap_write
# endif

#if defined(AO_HAVE_compare_and_swap_read) && \
    !defined(AO_HAVE_int_compare_and_swap_read)
#  define AO_int_compare_and_swap_read(addr, old, new_val) \
		AO_compare_and_swap_read((volatile AO_t *)addr, \
				         (AO_t) old, (AO_t)new_val)
#  define AO_HAVE_int_compare_and_swap_read
# endif

#if defined(AO_HAVE_compare_and_swap) && \
    !defined(AO_HAVE_int_compare_and_swap)
#  define AO_int_compare_and_swap(addr, old, new_val) \
		AO_compare_and_swap((volatile AO_t *)addr, \
				    (AO_t) old, (AO_t)new_val)
#  define AO_HAVE_int_compare_and_swap
# endif

#if defined(AO_HAVE_load_acquire) && \
    !defined(AO_HAVE_int_load_acquire)
#  define AO_int_load_acquire(addr) (int)AO_load_acquire((volatile AO_t *)addr)
#  define AO_HAVE_int_load_acquire
# endif

#if defined(AO_HAVE_store_release) && \
    !defined(AO_HAVE_int_store_release)
#  define AO_int_store_release(addr, val) \
	AO_store_release((volatile AO_t *)addr, (AO_t)val)
#  define AO_HAVE_int_store_release
# endif

#if defined(AO_HAVE_fetch_and_add_full) && \
    !defined(AO_HAVE_int_fetch_and_add_full)
#  define AO_int_fetch_and_add_full(addr, incr) \
	(int)AO_fetch_and_add_full((volatile AO_t *)addr, (AO_t)incr)
#  define AO_HAVE_int_fetch_and_add_full
# endif

#if defined(AO_HAVE_fetch_and_add1_acquire) && \
    !defined(AO_HAVE_int_fetch_and_add1_acquire)
#  define AO_int_fetch_and_add1_acquire(addr) \
	(int)AO_fetch_and_add1_acquire((volatile AO_t *)addr)
#  define AO_HAVE_int_fetch_and_add1_acquire
# endif

#if defined(AO_HAVE_fetch_and_add1_release) && \
    !defined(AO_HAVE_int_fetch_and_add1_release)
#  define AO_int_fetch_and_add1_release(addr) \
	(int)AO_fetch_and_add1_release((volatile AO_t *)addr)
#  define AO_HAVE_int_fetch_and_add1_release
# endif

#if defined(AO_HAVE_fetch_and_sub1_acquire) && \
    !defined(AO_HAVE_int_fetch_and_sub1_acquire)
#  define AO_int_fetch_and_sub1_acquire(addr) \
	(int)AO_fetch_and_sub1_acquire((volatile AO_t *)addr)
#  define AO_HAVE_int_fetch_and_sub1_acquire
# endif

#if defined(AO_HAVE_fetch_and_sub1_release) && \
    !defined(AO_HAVE_int_fetch_and_sub1_release)
#  define AO_int_fetch_and_sub1_release(addr) \
	(int)AO_fetch_and_sub1_release((volatile AO_t *)addr)
#  define AO_HAVE_int_fetch_and_sub1_release
# endif

