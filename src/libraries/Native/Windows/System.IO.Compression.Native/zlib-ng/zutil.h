#ifndef ZUTIL_H_
#define ZUTIL_H_
/* zutil.h -- internal interface and configuration of the compression library
 * Copyright (C) 1995-2016 Jean-loup Gailly, Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/* WARNING: this file should *not* be used by applications. It is
   part of the implementation of the compression library and is
   subject to change. Applications should only use zlib.h.
 */

#if defined(HAVE_VISIBILITY_INTERNAL)
#  define Z_INTERNAL __attribute__((visibility ("internal")))
#elif defined(HAVE_VISIBILITY_HIDDEN)
#  define Z_INTERNAL __attribute__((visibility ("hidden")))
#else
#  define Z_INTERNAL
#endif

#ifndef __cplusplus
#  define Z_REGISTER register
#else
#  define Z_REGISTER
#endif

#ifndef Z_TLS
#  define Z_TLS
#endif

#include <stddef.h>
#include <string.h>
#include <stdlib.h>
#include <stdint.h>
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif
#include "zbuild.h"

typedef unsigned char uch; /* Included for compatibility with external code only */
typedef uint16_t ush;      /* Included for compatibility with external code only */
typedef unsigned long ulg;

extern z_const char * const PREFIX(z_errmsg)[10]; /* indexed by 2-zlib_error */
/* (size given to avoid silly warnings with Visual C++) */

#define ERR_MSG(err) PREFIX(z_errmsg)[Z_NEED_DICT-(err)]

#define ERR_RETURN(strm, err) return (strm->msg = ERR_MSG(err), (err))
/* To be used only when the state is known to be valid */

        /* common constants */

#ifndef DEF_WBITS
#  define DEF_WBITS MAX_WBITS
#endif
/* default windowBits for decompression. MAX_WBITS is for compression only */

#if MAX_MEM_LEVEL >= 8
#  define DEF_MEM_LEVEL 8
#else
#  define DEF_MEM_LEVEL  MAX_MEM_LEVEL
#endif
/* default memLevel */

#define STORED_BLOCK 0
#define STATIC_TREES 1
#define DYN_TREES    2
/* The three kinds of block type */

#define MIN_MATCH  3
#define MAX_MATCH  258
/* The minimum and maximum match lengths */

#define PRESET_DICT 0x20 /* preset dictionary flag in zlib header */

#define ADLER32_INITIAL_VALUE 1 /* initial adler-32 hash value */

        /* target dependencies */

#ifdef AMIGA
#  define OS_CODE  1
#endif

#ifdef __370__
#  if __TARGET_LIB__ < 0x20000000
#    define OS_CODE 4
#  elif __TARGET_LIB__ < 0x40000000
#    define OS_CODE 11
#  else
#    define OS_CODE 8
#  endif
#endif

#if defined(ATARI) || defined(atarist)
#  define OS_CODE  5
#endif

#ifdef OS2
#  define OS_CODE  6
#endif

#if defined(MACOS) || defined(TARGET_OS_MAC)
#  define OS_CODE  7
#endif

#ifdef __acorn
#  define OS_CODE 13
#endif

#if defined(_WIN32) && !defined(__CYGWIN__)
#  define OS_CODE  10
#endif

#ifdef __APPLE__
#  define OS_CODE 19
#endif

#if (defined(_MSC_VER) && (_MSC_VER > 600))
#  define fdopen(fd, type)  _fdopen(fd, type)
#endif

/* MS Visual Studio does not allow inline in C, only C++.
   But it provides __inline instead, so use that. */
#if defined(_MSC_VER) && !defined(inline) && !defined(__cplusplus)
#  define inline __inline
#endif

        /* common defaults */

#ifndef OS_CODE
#  define OS_CODE  3  /* assume Unix */
#endif

         /* functions */

/* Diagnostic functions */
#ifdef ZLIB_DEBUG
#  include <stdio.h>
   extern int Z_INTERNAL z_verbose;
   extern void Z_INTERNAL z_error(char *m);
#  define Assert(cond, msg) {if (!(cond)) z_error(msg);}
#  define Trace(x) {if (z_verbose >= 0) fprintf x;}
#  define Tracev(x) {if (z_verbose > 0) fprintf x;}
#  define Tracevv(x) {if (z_verbose > 1) fprintf x;}
#  define Tracec(c, x) {if (z_verbose > 0 && (c)) fprintf x;}
#  define Tracecv(c, x) {if (z_verbose > 1 && (c)) fprintf x;}
#else
#  define Assert(cond, msg)
#  define Trace(x)
#  define Tracev(x)
#  define Tracevv(x)
#  define Tracec(c, x)
#  define Tracecv(c, x)
#endif

void Z_INTERNAL *zng_calloc(void *opaque, unsigned items, unsigned size);
void Z_INTERNAL   zng_cfree(void *opaque, void *ptr);

#define ZALLOC(strm, items, size) (*((strm)->zalloc))((strm)->opaque, (items), (size))
#define ZFREE(strm, addr)         (*((strm)->zfree))((strm)->opaque, (void *)(addr))
#define TRY_FREE(s, p)            {if (p) ZFREE(s, p);}

/* Reverse the bytes in a value. Use compiler intrinsics when
   possible to take advantage of hardware implementations. */
#if defined(_MSC_VER) && (_MSC_VER >= 1300)
#  pragma intrinsic(_byteswap_ulong)
#  define ZSWAP16(q) _byteswap_ushort(q)
#  define ZSWAP32(q) _byteswap_ulong(q)
#  define ZSWAP64(q) _byteswap_uint64(q)

#elif defined(__Clang__) || (defined(__GNUC__) && \
        (__GNUC__ > 4 || (__GNUC__ == 4 && __GNUC_MINOR__ >= 8)))
#  define ZSWAP16(q) __builtin_bswap16(q)
#  define ZSWAP32(q) __builtin_bswap32(q)
#  define ZSWAP64(q) __builtin_bswap64(q)

#elif defined(__GNUC__) && (__GNUC__ >= 2) && defined(__linux__)
#  include <byteswap.h>
#  define ZSWAP16(q) bswap_16(q)
#  define ZSWAP32(q) bswap_32(q)
#  define ZSWAP64(q) bswap_64(q)

#elif defined(__FreeBSD__) || defined(__NetBSD__) || defined(__DragonFly__)
#  include <sys/endian.h>
#  define ZSWAP16(q) bswap16(q)
#  define ZSWAP32(q) bswap32(q)
#  define ZSWAP64(q) bswap64(q)
#elif defined(__OpenBSD__)
#  include <sys/endian.h>
#  define ZSWAP16(q) swap16(q)
#  define ZSWAP32(q) swap32(q)
#  define ZSWAP64(q) swap64(q)
#elif defined(__INTEL_COMPILER)
/* ICC does not provide a two byte swap. */
#  define ZSWAP16(q) ((((q) & 0xff) << 8) | (((q) & 0xff00) >> 8))
#  define ZSWAP32(q) _bswap(q)
#  define ZSWAP64(q) _bswap64(q)

#else
#  define ZSWAP16(q) ((((q) & 0xff) << 8) | (((q) & 0xff00) >> 8))
#  define ZSWAP32(q) ((((q) >> 24) & 0xff) + (((q) >> 8) & 0xff00) + \
                     (((q) & 0xff00) << 8) + (((q) & 0xff) << 24))
#  define ZSWAP64(q)                           \
          ((q & 0xFF00000000000000u) >> 56u) | \
          ((q & 0x00FF000000000000u) >> 40u) | \
          ((q & 0x0000FF0000000000u) >> 24u) | \
          ((q & 0x000000FF00000000u) >> 8u)  | \
          ((q & 0x00000000FF000000u) << 8u)  | \
          ((q & 0x0000000000FF0000u) << 24u) | \
          ((q & 0x000000000000FF00u) << 40u) | \
          ((q & 0x00000000000000FFu) << 56u)
#endif

/* Only enable likely/unlikely if the compiler is known to support it */
#if (defined(__GNUC__) && (__GNUC__ >= 3)) || defined(__INTEL_COMPILER) || defined(__Clang__)
#  define LIKELY_NULL(x)        __builtin_expect((x) != 0, 0)
#  define LIKELY(x)             __builtin_expect(!!(x), 1)
#  define UNLIKELY(x)           __builtin_expect(!!(x), 0)
#  define PREFETCH_L1(addr)     __builtin_prefetch(addr, 0, 3)
#  define PREFETCH_L2(addr)     __builtin_prefetch(addr, 0, 2)
#  define PREFETCH_RW(addr)     __builtin_prefetch(addr, 1, 2)
#elif defined(__WIN__)
#  include <xmmintrin.h>
#  define LIKELY_NULL(x)        x
#  define LIKELY(x)             x
#  define UNLIKELY(x)           x
#  define PREFETCH_L1(addr)     _mm_prefetch((char *) addr, _MM_HINT_T0)
#  define PREFETCH_L2(addr)     _mm_prefetch((char *) addr, _MM_HINT_T1)
#  define PREFETCH_RW(addr)     _mm_prefetch((char *) addr, _MM_HINT_T1)
#else
#  define LIKELY_NULL(x)        x
#  define LIKELY(x)             x
#  define UNLIKELY(x)           x
#  define PREFETCH_L1(addr)     addr
#  define PREFETCH_L2(addr)     addr
#  define PREFETCH_RW(addr)     addr
#endif /* (un)likely */

#if defined(_MSC_VER)
#  define ALIGNED_(x) __declspec(align(x))
#else
#  if defined(__GNUC__)
#    define ALIGNED_(x) __attribute__ ((aligned(x)))
#  endif
#endif

#if defined(X86_FEATURES)
#  include "arch/x86/x86.h"
#elif defined(ARM_FEATURES)
#  include "arch/arm/arm.h"
#elif defined(POWER_FEATURES)
#  include "arch/power/power.h"
#endif

#endif /* ZUTIL_H_ */
