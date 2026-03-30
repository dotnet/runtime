// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       sysdefs.h
/// \brief      Common includes, definitions, system-specific things etc.
///
/// This file is used also by the lzma command line tool, that's why this
/// file is separate from common.h.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_SYSDEFS_H
#define LZMA_SYSDEFS_H

//////////////
// Includes //
//////////////

#ifdef HAVE_CONFIG_H
#	include <config.h>
#endif

// Choose if MinGW-w64's stdio replacement functions should be used.
// The default has varied slightly in the past so it's clearest to always
// set it explicitly.
//
// Modern MinGW-w64 enables the replacement functions even with UCRT
// when _GNU_SOURCE is defined. That's good because UCRT doesn't support
// the POSIX thousand separator flag in printf (like "%'u"). Otherwise
// XZ Utils works with the UCRT stdio functions.
//
// The replacement functions add over 20 KiB to each executable. For
// size-optimized builds (HAVE_SMALL), disable the replacements.
// Then thousand separators aren't shown in xz's messages but this is
// a minor downside compare to the slower speed of the HAVE_SMALL builds.
//
// The legacy MSVCRT is pre-C99 and it's best to always use the stdio
// replacements functions from MinGW-w64.
#if defined(__MINGW32__) && !defined(__USE_MINGW_ANSI_STDIO)
#	define __USE_MINGW_ANSI_STDIO 1
#	include <_mingw.h>
#	if defined(_UCRT) && defined(HAVE_SMALL)
#		undef __USE_MINGW_ANSI_STDIO
#		define __USE_MINGW_ANSI_STDIO 0
#	endif
#endif

// size_t and NULL
#include <stddef.h>

#ifdef HAVE_INTTYPES_H
#	include <inttypes.h>
#endif

// C99 says that inttypes.h always includes stdint.h, but some systems
// don't do that, and require including stdint.h separately.
#ifdef HAVE_STDINT_H
#	include <stdint.h>
#endif

// Some pre-C99 systems have SIZE_MAX in limits.h instead of stdint.h. The
// limits are also used to figure out some macros missing from pre-C99 systems.
#include <limits.h>

// Be more compatible with systems that have non-conforming inttypes.h.
// We assume that int is 32-bit and that long is either 32-bit or 64-bit.
// Full Autoconf test could be more correct, but this should work well enough.
// Note that this duplicates some code from lzma.h, but this is better since
// we can work without inttypes.h thanks to Autoconf tests.
#ifndef UINT32_C
#	if UINT_MAX != 4294967295U
#		error UINT32_C is not defined and unsigned int is not 32-bit.
#	endif
#	define UINT32_C(n) n ## U
#endif
#ifndef UINT32_MAX
#	define UINT32_MAX UINT32_C(4294967295)
#endif
#ifndef PRIu32
#	define PRIu32 "u"
#endif
#ifndef PRIx32
#	define PRIx32 "x"
#endif
#ifndef PRIX32
#	define PRIX32 "X"
#endif

#if ULONG_MAX == 4294967295UL
#	ifndef UINT64_C
#		define UINT64_C(n) n ## ULL
#	endif
#	ifndef PRIu64
#		define PRIu64 "llu"
#	endif
#	ifndef PRIx64
#		define PRIx64 "llx"
#	endif
#	ifndef PRIX64
#		define PRIX64 "llX"
#	endif
#else
#	ifndef UINT64_C
#		define UINT64_C(n) n ## UL
#	endif
#	ifndef PRIu64
#		define PRIu64 "lu"
#	endif
#	ifndef PRIx64
#		define PRIx64 "lx"
#	endif
#	ifndef PRIX64
#		define PRIX64 "lX"
#	endif
#endif
#ifndef UINT64_MAX
#	define UINT64_MAX UINT64_C(18446744073709551615)
#endif

// Incorrect(?) SIZE_MAX:
//   - Interix headers typedef size_t to unsigned long,
//     but a few lines later define SIZE_MAX to INT32_MAX.
//   - SCO OpenServer (x86) headers typedef size_t to unsigned int
//     but define SIZE_MAX to INT32_MAX.
#if defined(__INTERIX) || defined(_SCO_DS)
#	undef SIZE_MAX
#endif

// The code currently assumes that size_t is either 32-bit or 64-bit.
#ifndef SIZE_MAX
#	if SIZEOF_SIZE_T == 4
#		define SIZE_MAX UINT32_MAX
#	elif SIZEOF_SIZE_T == 8
#		define SIZE_MAX UINT64_MAX
#	else
#		error size_t is not 32-bit or 64-bit
#	endif
#endif
#if SIZE_MAX != UINT32_MAX && SIZE_MAX != UINT64_MAX
#	error size_t is not 32-bit or 64-bit
#endif

#include <stdlib.h>
#include <assert.h>

// Pre-C99 systems lack stdbool.h. All the code in XZ Utils must be written
// so that it works with fake bool type, for example:
//
//    bool foo = (flags & 0x100) != 0;
//    bool bar = !!(flags & 0x100);
//
// This works with the real C99 bool but breaks with fake bool:
//
//    bool baz = (flags & 0x100);
//
#ifdef HAVE_STDBOOL_H
#	include <stdbool.h>
#else
#	if ! HAVE__BOOL
typedef unsigned char _Bool;
#	endif
#	define bool _Bool
#	define false 0
#	define true 1
#	define __bool_true_false_are_defined 1
#endif

// We may need alignas from C11/C17/C23.
#if __STDC_VERSION__ >= 202311
	// alignas is a keyword in C23. Do nothing.
#elif __STDC_VERSION__ >= 201112
	// Oracle Developer Studio 12.6 lacks <stdalign.h>.
	// For simplicity, avoid the header with all C11/C17 compilers.
#	define alignas _Alignas
#elif defined(__GNUC__) || defined(__clang__)
#	define alignas(n) __attribute__((__aligned__(n)))
#else
#	define alignas(n)
#endif

#include <string.h>

// MSVC v19.00 (VS 2015 version 14.0) and later should work.
//
// MSVC v19.27 (VS 2019 version 16.7) added support for restrict.
// Older ones support only __restrict.
#ifdef _MSC_VER
#	if _MSC_VER < 1927 && !defined(restrict)
#		define restrict __restrict
#	endif
#endif

////////////
// Macros //
////////////

#undef memzero
#define memzero(s, n) memset(s, 0, n)

// NOTE: Avoid using MIN() and MAX(), because even conditionally defining
// those macros can cause some portability trouble, since on some systems
// the system headers insist defining their own versions.
#define my_min(x, y) ((x) < (y) ? (x) : (y))
#define my_max(x, y) ((x) > (y) ? (x) : (y))

#ifndef ARRAY_SIZE
#	define ARRAY_SIZE(array) (sizeof(array) / sizeof((array)[0]))
#endif

#if defined(__GNUC__) \
		&& ((__GNUC__ == 4 && __GNUC_MINOR__ >= 3) || __GNUC__ > 4)
#	define lzma_attr_alloc_size(x) __attribute__((__alloc_size__(x)))
#else
#	define lzma_attr_alloc_size(x)
#endif

#if __STDC_VERSION__ >= 202311
#	define FALLTHROUGH [[__fallthrough__]]
#elif (defined(__GNUC__) && __GNUC__ >= 7) \
		|| (defined(__clang_major__) && __clang_major__ >= 10)
#	define FALLTHROUGH __attribute__((__fallthrough__))
#else
#	define FALLTHROUGH ((void)0)
#endif

#endif
