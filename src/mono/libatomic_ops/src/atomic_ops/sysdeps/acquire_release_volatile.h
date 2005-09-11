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
 * This file adds definitions appropriate for environments in which an AO_t
 * volatile load has acquire semantics, and an AO_t volatile store has release
 * semantics.  This is arguably supposed to be true with the standard Itanium
 * software conventions.
 */

/*
 * Empirically gcc/ia64 does some reordering of ordinary operations around volatiles
 * even when we think it shouldn't.  Gcc 3.3 and earlier could reorder a volatile store
 * with another store.  As of March 2005, gcc pre-4 reused previously computed
 * common subexpressions across a volatile load.
 * Hence we now add compiler barriers for gcc.
 */
#if !defined(AO_GCC_BARRIER)
#  if defined(__GNUC__)
#    define AO_GCC_BARRIER() AO_compiler_barrier()
#  else
#    define AO_GCC_BARRIER()
#  endif
#endif

AO_INLINE AO_t
AO_load_acquire(volatile AO_t *p)
{
  AO_t result = *p;
  /* A normal volatile load generates an ld.acq		*/
  AO_GCC_BARRIER();
  return result;
}
#define AO_HAVE_load_acquire

AO_INLINE void
AO_store_release(volatile AO_t *p, AO_t val)
{
  AO_GCC_BARRIER();
  /* A normal volatile store generates an st.rel	*/
  *p = val;
}
#define AO_HAVE_store_release


