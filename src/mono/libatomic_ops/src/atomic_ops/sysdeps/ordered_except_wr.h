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

/*
 * These are common definitions for architectures that provide processor
 * ordered memory operations except that a later read may pass an
 * earlier write.  Real x86 implementations seem to be in this category,
 * except apparently for some IDT WinChips, which we ignore.
 */

AO_INLINE void
AO_nop_write()
{
  AO_compiler_barrier();
  /* sfence according to Intel docs.  Pentium 3 and up.	*/
  /* Unnecessary for cached accesses?			*/
}

#define AO_HAVE_NOP_WRITE

AO_INLINE void
AO_nop_read()
{
  AO_compiler_barrier();
}

#define AO_HAVE_NOP_READ

#ifdef AO_HAVE_load

AO_INLINE AO_t
AO_load_read(volatile AO_t *addr)
{
  AO_t result = AO_load(addr);
  AO_compiler_barrier();
  return result;
}
#define AO_HAVE_load_read

#define AO_load_acquire(addr) AO_load_read(addr)
#define AO_HAVE_load_acquire

#endif /* AO_HAVE_load */

#ifdef AO_HAVE_char_load

AO_INLINE AO_t
AO_char_load_read(volatile unsigned char *addr)
{
  AO_t result = AO_char_load(addr);
  AO_compiler_barrier();
  return result;
}
#define AO_HAVE_char_load_read

#define AO_char_load_acquire(addr) AO_char_load_read(addr)
#define AO_HAVE_char_load_acquire

#endif /* AO_HAVE_char_load */

#ifdef AO_HAVE_short_load

AO_INLINE AO_t
AO_short_load_read(volatile unsigned short *addr)
{
  AO_t result = AO_short_load(addr);
  AO_compiler_barrier();
  return result;
}
#define AO_HAVE_short_load_read

#define AO_short_load_acquire(addr) AO_short_load_read(addr)
#define AO_HAVE_short_load_acquire

#endif /* AO_HAVE_short_load */

#ifdef AO_HAVE_int_load

AO_INLINE AO_t
AO_int_load_read(volatile unsigned int *addr)
{
  AO_t result = AO_int_load(addr);
  AO_compiler_barrier();
  return result;
}
#define AO_HAVE_int_load_read

#define AO_int_load_acquire(addr) AO_int_load_read(addr)
#define AO_HAVE_int_load_acquire

#endif /* AO_HAVE_int_load */

#if defined(AO_HAVE_store)

AO_INLINE void
AO_store_write(volatile AO_t *addr, AO_t val)
{
  AO_compiler_barrier();
  AO_store(addr, val);
}
# define AO_HAVE_store_write

# define AO_store_release(addr, val) AO_store_write(addr, val)
# define AO_HAVE_store_release

#endif /* AO_HAVE_store */

#if defined(AO_HAVE_char_store)

AO_INLINE void
AO_char_store_write(volatile unsigned char *addr, unsigned char val)
{
  AO_compiler_barrier();
  AO_char_store(addr, val);
}
# define AO_HAVE_char_store_write

# define AO_char_store_release(addr, val) AO_char_store_write(addr, val)
# define AO_HAVE_char_store_release

#endif /* AO_HAVE_char_store */

#if defined(AO_HAVE_short_store)

AO_INLINE void
AO_short_store_write(volatile unsigned short *addr, unsigned short val)
{
  AO_compiler_barrier();
  AO_short_store(addr, val);
}
# define AO_HAVE_short_store_write

# define AO_short_store_release(addr, val) AO_short_store_write(addr, val)
# define AO_HAVE_short_store_release

#endif /* AO_HAVE_short_store */

#if defined(AO_HAVE_int_store)

AO_INLINE void
AO_int_store_write(volatile unsigned int *addr, unsigned int val)
{
  AO_compiler_barrier();
  AO_int_store(addr, val);
}
# define AO_HAVE_int_store_write

# define AO_int_store_release(addr, val) AO_int_store_write(addr, val)
# define AO_HAVE_int_store_release

#endif /* AO_HAVE_int_store */

