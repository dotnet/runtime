/* -----------------------------------------------------------------*-C-*-
   libffi 2.0.0 - Copyright (C) 1996, 1997, 1998, 1999, 2000, 
                                2001  Red Hat, Inc.

   Permission is hereby granted, free of charge, to any person obtaining
   a copy of this software and associated documentation files (the
   ``Software''), to deal in the Software without restriction, including
   without limitation the rights to use, copy, modify, merge, publish,
   distribute, sublicense, and/or sell copies of the Software, and to
   permit persons to whom the Software is furnished to do so, subject to
   the following conditions:

   The above copyright notice and this permission notice shall be included
   in all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
   IN NO EVENT SHALL RED HAT BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.

   ----------------------------------------------------------------------- */

#include <fficonfig.h>

#define ALIGN(v, a)  (((((size_t) (v))-1) | ((a)-1))+1)

/* ---- Generic type definitions ----------------------------------------- */

#define FLOAT32 float
#define FLOAT64 double
#define FLOAT80 long double

#define UINT8   unsigned char
#define SINT8   signed char

#if SIZEOF_INT == 2

#define UINT16	unsigned int
#define SINT16  int

#else 
#if SIZEOF_SHORT == 2

#define UINT16  unsigned short
#define SINT16  short

#endif
#endif

#if SIZEOF_INT == 4

#define UINT32	unsigned int
#define SINT32  int

#else 
#if SIZEOF_SHORT == 4

#define UINT32  unsigned short
#define SINT32  short

#else
#if SIZEOF_LONG == 4

#define UINT32  unsigned long
#define SINT32  long

#endif
#endif
#endif

#if SIZEOF_INT == 8

#define UINT64  unsigned int
#define SINT64  int

#else
#if SIZEOF_LONG == 8

#define UINT64  unsigned long
#define SINT64  long

#else
#if SIZEOF_LONG_LONG == 8

#define UINT64  unsigned long long
#define SINT64  long long

#endif
#endif
#endif

#define FFI_TYPE_VOID       0    
#define FFI_TYPE_INT        1
#define FFI_TYPE_FLOAT      2    
#define FFI_TYPE_DOUBLE     3
#if SIZEOF_LONG_DOUBLE == SIZEOF_DOUBLE
#define FFI_TYPE_LONGDOUBLE FFI_TYPE_DOUBLE
#else
#define FFI_TYPE_LONGDOUBLE 4
#endif
#define FFI_TYPE_UINT8      5   /* If this changes, update ffi_mips.h. */
#define FFI_TYPE_SINT8      6   /* If this changes, update ffi_mips.h. */
#define FFI_TYPE_UINT16     7 
#define FFI_TYPE_SINT16     8
#define FFI_TYPE_UINT32     9
#define FFI_TYPE_SINT32     10
#define FFI_TYPE_UINT64     11
#define FFI_TYPE_SINT64     12
#define FFI_TYPE_STRUCT     13  /* If this changes, update ffi_mips.h. */
#define FFI_TYPE_POINTER    14
#define FFI_TYPE_LAST       14

#if _MIPS_SIM==_ABIN32 && defined(_ABIN32)
#define SIZEOF_ARG 8
#else
#define SIZEOF_ARG SIZEOF_VOID_P
#endif

#ifndef __ASSEMBLER__
/* This part of the private header file is only for C code.  */

/* Check for the existence of memcpy. */
#if STDC_HEADERS
# include <string.h>
#else
# ifndef HAVE_MEMCPY
#  define memcpy(d, s, n) bcopy ((s), (d), (n))
# endif
#endif

#ifndef FALSE
#define FALSE 0
#endif

#ifndef TRUE
#define TRUE (!FALSE)
#endif

#ifndef __cplusplus
/* bool is a keyword in C++ */
typedef int bool;
#endif

#ifdef FFI_DEBUG
/* Debugging functions */
void ffi_stop_here(void);
bool ffi_type_test(ffi_type *a);
#define FFI_ASSERT(x) ((x) ? 0 : ffi_assert(__FILE__,__LINE__))
#else
#define FFI_ASSERT(x) 
#endif

/* Perform machine dependent cif processing */
ffi_status ffi_prep_cif_machdep(ffi_cif *cif);

/* Extended cif, used in callback from assembly routine */
typedef struct
{
  ffi_cif *cif;
  void *rvalue;
  void **avalue;
} extended_cif;

#endif /* __ASSEMBLER__ */

