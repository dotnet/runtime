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

/* -------------------------------------------------------------------
   The basic API is described in the README file.

   The raw API is designed to bypass some of the argument packing
   and unpacking on architectures for which it can be avoided.

   The closure API allows interpreted functions to be packaged up
   inside a C function pointer, so that they can be called as C functions,
   with no understanding on the client side that they are interpreted.
   It can also be used in other cases in which it is necessary to package
   up a user specified parameter and a function pointer as a single
   function pointer.

   The closure API must be implemented in order to get its functionality,
   e.g. for use by gij.  Routines are provided to emulate the raw API
   if the underlying platform doesn't allow faster implementation.

   More details on the raw and cloure API can be found in:

   http://gcc.gnu.org/ml/java/1999-q3/msg00138.html

   and

   http://gcc.gnu.org/ml/java/1999-q3/msg00174.html
   -------------------------------------------------------------------- */

#ifndef FFI_H
#define FFI_H

#if !defined(__ASSEMBLER__) && !defined(__GNUC__)
#error --- ffi.h requires GNU C ---
#endif

#ifdef __cplusplus
extern "C" {
#endif

#if !defined(LIBFFI_ASM)
#include <stddef.h>
#if defined(FFI_DEBUG) 
#include <stdio.h>
#endif
#endif

#ifndef LIBFFI_ASM

/* ---- Generic type definitions ----------------------------------------- */

typedef enum ffi_abi {

  /* Leave this for debugging purposes */
  FFI_FIRST_ABI = 0,

  /* ---- Sparc -------------------- */
#ifdef __sparc__
  FFI_V8,
  FFI_V8PLUS,
  FFI_V9,
#if defined(__arch64__) || defined(__sparcv9)
  FFI_DEFAULT_ABI = FFI_V9,
#else
  FFI_DEFAULT_ABI = FFI_V8,
#endif
#endif

  /* ---- Intel x86 ---------------- */
#ifdef __i386__
  FFI_SYSV,
  FFI_DEFAULT_ABI = FFI_SYSV,
#endif

  /* ---- Intel ia64 ---------------- */
#ifdef __ia64__
  FFI_UNIX,   	/* Linux and all Unix variants use the same conventions	*/
  FFI_DEFAULT_ABI = FFI_UNIX,
#endif

  /* ---- Mips --------------------- */
#ifdef __mips__
  FFI_O32,
  FFI_N32,
  FFI_N64,

# if defined(__mips_eabi)
#   define FFI_MIPS_EABI
#   define FFI_MIPS_O32
# else
#   if !defined(_MIPS_SIM)
#error -- something is very wrong --
#   else
#     if _MIPS_SIM==_ABIN32 && defined(_ABIN32)
#       define FFI_MIPS_N32
#     else
#       define FFI_MIPS_O32
#     endif
#   endif
# endif
# if defined(FFI_MIPS_O32)
  FFI_DEFAULT_ABI = FFI_O32,
# else
  FFI_DEFAULT_ABI = FFI_N32,
# endif

#endif

  /* ---- Alpha -------------------- */
#ifdef __alpha__
  FFI_OSF,
  FFI_DEFAULT_ABI = FFI_OSF,
#endif

  /* ---- Motorola m68k ------------ */
#ifdef __m68k__
  FFI_SYSV,
  FFI_DEFAULT_ABI = FFI_SYSV,
#endif

  /* ---- PowerPC ------------------ */
#ifdef __powerpc__
  FFI_SYSV,
  FFI_GCC_SYSV,
  FFI_DEFAULT_ABI = FFI_GCC_SYSV,
#endif

  /* ---- ARM  --------------------- */
#ifdef __arm__
  FFI_SYSV,
  FFI_DEFAULT_ABI = FFI_SYSV,
#endif

  /* ---- S390 --------------------- */
#ifdef __S390__
  FFI_SYSV,
  FFI_DEFAULT_ABI = FFI_SYSV,
#endif

  /* Leave this for debugging purposes */
  FFI_LAST_ABI

} ffi_abi;

typedef struct _ffi_type
{
  size_t size;
  unsigned short alignment;
  unsigned short type;
  struct _ffi_type **elements;
} ffi_type;

/* These are defined in ffi.c */
extern ffi_type ffi_type_void;
extern ffi_type ffi_type_uint8;
extern ffi_type ffi_type_sint8;
extern ffi_type ffi_type_uint16;
extern ffi_type ffi_type_sint16;
extern ffi_type ffi_type_uint32;
extern ffi_type ffi_type_sint32;
extern ffi_type ffi_type_uint64;
extern ffi_type ffi_type_sint64;
extern ffi_type ffi_type_float;
extern ffi_type ffi_type_double;
extern ffi_type ffi_type_longdouble;
extern ffi_type ffi_type_pointer;

extern ffi_type ffi_type_ushort;
extern ffi_type ffi_type_sint; 
extern ffi_type ffi_type_uint; 
extern ffi_type ffi_type_slong; 
extern ffi_type ffi_type_ulong;

typedef enum {
  FFI_OK = 0,
  FFI_BAD_TYPEDEF,
  FFI_BAD_ABI 
} ffi_status;

typedef unsigned FFI_TYPE;

typedef struct {
  ffi_abi abi;
  unsigned nargs;
  ffi_type **arg_types;
  ffi_type *rtype;
  unsigned bytes;
  unsigned flags;

#ifdef __mips__
#if _MIPS_SIM == _ABIN32
  unsigned rstruct_flag;
#endif
#endif

} ffi_cif;

/* ---- Definitions for the raw API -------------------------------------- */

#if !FFI_NO_RAW_API

typedef union {
#if _MIPS_SIM==_ABIN32 && defined(_ABIN32)
  long          sint;
  unsigned long uint;
#else
  int      sint;
  unsigned uint;
#endif
  float	   flt;
  char     data[sizeof(void*)];
  void*    ptr;
} ffi_raw;

void ffi_raw_call (/*@dependent@*/ ffi_cif *cif, 
		   void (*fn)(), 
		   /*@out@*/ void *rvalue, 
		   /*@dependent@*/ ffi_raw *avalue);

void ffi_ptrarray_to_raw (ffi_cif *cif, void **args, ffi_raw *raw);
void ffi_raw_to_ptrarray (ffi_cif *cif, ffi_raw *raw, void **args);
size_t ffi_raw_size (ffi_cif *cif);

#if !NO_JAVA_RAW_API

/* This is analogous to the raw API, except it uses Java parameter	*/
/* packing, even on 64-bit machines.  I.e. on 64-bit machines		*/
/* longs and doubles are followed by an empty 64-bit word.		*/

void ffi_java_raw_call (/*@dependent@*/ ffi_cif *cif, 
		        void (*fn)(), 
		        /*@out@*/ void *rvalue, 
		        /*@dependent@*/ ffi_raw *avalue);

void ffi_java_ptrarray_to_raw (ffi_cif *cif, void **args, ffi_raw *raw);
void ffi_java_raw_to_ptrarray (ffi_cif *cif, ffi_raw *raw, void **args);
size_t ffi_java_raw_size (ffi_cif *cif);

#endif /* !NO_JAVA_RAW_API */

#endif /* !FFI_NO_RAW_API */

/* ---- Definitions for closures ----------------------------------------- */

#ifdef __i386__

#define FFI_CLOSURES 1		/* x86 supports closures */
#define FFI_TRAMPOLINE_SIZE 10
#define FFI_NATIVE_RAW_API 1	/* and has native raw api support */

#elif defined(X86_WIN32)

#define FFI_CLOSURES 1		/* x86 supports closures */
#define FFI_TRAMPOLINE_SIZE 10
#define FFI_NATIVE_RAW_API 1	/* and has native raw api support */

#elif defined(IA64)

#define FFI_CLOSURES 1
#define FFI_TRAMPOLINE_SIZE 24  /* Really the following struct, which 	*/
				/* can be interpreted as a C function	*/
				/* decriptor:				*/

struct ffi_ia64_trampoline_struct {
    void * code_pointer;	/* Pointer to ffi_closure_UNIX	*/
    void * fake_gp;		/* Pointer to closure, installed as gp	*/
    void * real_gp;		/* Real gp value, reinstalled by 	*/
				/* ffi_closure_UNIX.			*/
};
#define FFI_NATIVE_RAW_API 0

#elif defined(__alpha__)

#define FFI_CLOSURES 1
#define FFI_TRAMPOLINE_SIZE 24
#define FFI_NATIVE_RAW_API 0

#elif defined(POWERPC)

#define FFI_CLOSURES 1
#define FFI_TRAMPOLINE_SIZE 40
#define FFI_NATIVE_RAW_API 0

#else 

#define FFI_CLOSURES 0
#define FFI_NATIVE_RAW_API 0

#endif



#if FFI_CLOSURES

typedef struct {
  char tramp[FFI_TRAMPOLINE_SIZE];
  ffi_cif   *cif;
  void     (*fun)(ffi_cif*,void*,void**,void*);
  void      *user_data;
} ffi_closure;

ffi_status
ffi_prep_closure (ffi_closure*,
		  ffi_cif *,
		  void (*fun)(ffi_cif*,void*,void**,void*),
		  void *user_data);

#if !FFI_NO_RAW_API

typedef struct {
  char tramp[FFI_TRAMPOLINE_SIZE];

  ffi_cif   *cif;

#if !FFI_NATIVE_RAW_API

  /* if this is enabled, then a raw closure has the same layout 
     as a regular closure.  We use this to install an intermediate 
     handler to do the transaltion, void** -> ffi_raw*. */

  void     (*translate_args)(ffi_cif*,void*,void**,void*);
  void      *this_closure;

#endif

  void     (*fun)(ffi_cif*,void*,ffi_raw*,void*);
  void      *user_data;

} ffi_raw_closure;

ffi_status
ffi_prep_raw_closure (ffi_raw_closure*,
		      ffi_cif *cif,
		      void (*fun)(ffi_cif*,void*,ffi_raw*,void*),
		      void *user_data);

#ifndef NO_JAVA_RAW_API
ffi_status
ffi_prep_java_raw_closure (ffi_raw_closure*,
		           ffi_cif *cif,
		           void (*fun)(ffi_cif*,void*,ffi_raw*,void*),
		           void *user_data);
#endif

#endif /* !FFI_NO_RAW_API */
#endif /* FFI_CLOSURES */

/* ---- Public interface definition -------------------------------------- */

ffi_status ffi_prep_cif (ffi_cif *cif, 
			 ffi_abi abi,
			 unsigned int nargs, 
			 ffi_type *rtype, 
			 ffi_type **atypes);

void ffi_call (ffi_cif *cif, 
	       void (*fn)(), 
	       void *rvalue, 
	       void **avalue);

/* Useful for eliminating compiler warnings */
#define FFI_FN(f) ((void (*)())f)

#endif

#ifdef __cplusplus
}
#endif

#endif

