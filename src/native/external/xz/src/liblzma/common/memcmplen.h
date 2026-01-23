// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       memcmplen.h
/// \brief      Optimized comparison of two buffers
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_MEMCMPLEN_H
#define LZMA_MEMCMPLEN_H

#include "common.h"

#ifdef HAVE_IMMINTRIN_H
#	include <immintrin.h>
#endif

// Only include <intrin.h> if it is needed. The header is only needed
// on Windows when using an MSVC compatible compiler. The Intel compiler
// can use the intrinsics without the header file.
#if defined(TUKLIB_FAST_UNALIGNED_ACCESS) \
		&& defined(_MSC_VER) \
		&& (defined(_M_X64) \
			|| defined(_M_ARM64) || defined(_M_ARM64EC)) \
		&& !defined(__INTEL_COMPILER)
#	include <intrin.h>
#endif


/// Find out how many equal bytes the two buffers have.
///
/// \param      buf1    First buffer
/// \param      buf2    Second buffer
/// \param      len     How many bytes have already been compared and will
///                     be assumed to match
/// \param      limit   How many bytes to compare at most, including the
///                     already-compared bytes. This must be significantly
///                     smaller than UINT32_MAX to avoid integer overflows.
///                     Up to LZMA_MEMCMPLEN_EXTRA bytes may be read past
///                     the specified limit from both buf1 and buf2.
///
/// \return     Number of equal bytes in the buffers is returned.
///             This is always at least len and at most limit.
///
/// \note       LZMA_MEMCMPLEN_EXTRA defines how many extra bytes may be read.
///             It's rounded up to 2^n. This extra amount needs to be
///             allocated in the buffers being used. It needs to be
///             initialized too to keep Valgrind quiet.
static lzma_always_inline uint32_t
lzma_memcmplen(const uint8_t *buf1, const uint8_t *buf2,
		uint32_t len, uint32_t limit)
{
	assert(len <= limit);
	assert(limit <= UINT32_MAX / 2);

#if defined(TUKLIB_FAST_UNALIGNED_ACCESS) \
		&& (((TUKLIB_GNUC_REQ(3, 4) || defined(__clang__)) \
				&& SIZE_MAX == UINT64_MAX) \
			|| (defined(__INTEL_COMPILER) && defined(__x86_64__)) \
			|| (defined(__INTEL_COMPILER) && defined(_M_X64)) \
			|| (defined(_MSC_VER) && (defined(_M_X64) \
				|| defined(_M_ARM64) || defined(_M_ARM64EC))))
	// This is only for x86-64 and ARM64 for now. This might be fine on
	// other 64-bit processors too.
	//
	// Reasons to use subtraction instead of xor:
	//
	//   - On some x86-64 processors (Intel Sandy Bridge to Tiger Lake),
	//     sub+jz and sub+jnz can be fused but xor+jz or xor+jnz cannot.
	//     Thus using subtraction has potential to be a tiny amount faster
	//     since the code checks if the quotient is non-zero.
	//
	//   - Some processors (Intel Pentium 4) used to have more ALU
	//     resources for add/sub instructions than and/or/xor.
	//
	// The processor info is based on Agner Fog's microarchitecture.pdf
	// version 2023-05-26. https://www.agner.org/optimize/
#define LZMA_MEMCMPLEN_EXTRA 8
	while (len < limit) {
#	ifdef WORDS_BIGENDIAN
		const uint64_t x = read64ne(buf1 + len) ^ read64ne(buf2 + len);
#	else
		const uint64_t x = read64ne(buf1 + len) - read64ne(buf2 + len);
#	endif
		if (x != 0) {
	// MSVC or Intel C compiler on Windows
#	if defined(_MSC_VER) || defined(__INTEL_COMPILER)
			unsigned long tmp;
			_BitScanForward64(&tmp, x);
			len += (uint32_t)tmp >> 3;
	// GCC, Clang, or Intel C compiler
#	elif defined(WORDS_BIGENDIAN)
			len += (uint32_t)__builtin_clzll(x) >> 3;
#	else
			len += (uint32_t)__builtin_ctzll(x) >> 3;
#	endif
			return my_min(len, limit);
		}

		len += 8;
	}

	return limit;

#elif defined(TUKLIB_FAST_UNALIGNED_ACCESS) \
		&& defined(HAVE__MM_MOVEMASK_EPI8) \
		&& (defined(__SSE2__) \
			|| (defined(_MSC_VER) && defined(_M_IX86_FP) \
				&& _M_IX86_FP >= 2))
	// NOTE: This will use 128-bit unaligned access which
	// TUKLIB_FAST_UNALIGNED_ACCESS wasn't meant to permit,
	// but it's convenient here since this is x86-only.
	//
	// SSE2 version for 32-bit and 64-bit x86. On x86-64 the above
	// version is sometimes significantly faster and sometimes
	// slightly slower than this SSE2 version, so this SSE2
	// version isn't used on x86-64.
#	define LZMA_MEMCMPLEN_EXTRA 16
	while (len < limit) {
		const uint32_t x = 0xFFFF ^ (uint32_t)_mm_movemask_epi8(
			_mm_cmpeq_epi8(
			_mm_loadu_si128((const __m128i *)(buf1 + len)),
			_mm_loadu_si128((const __m128i *)(buf2 + len))));

		if (x != 0) {
			len += ctz32(x);
			return my_min(len, limit);
		}

		len += 16;
	}

	return limit;

#elif defined(TUKLIB_FAST_UNALIGNED_ACCESS) && !defined(WORDS_BIGENDIAN)
	// Generic 32-bit little endian method
#	define LZMA_MEMCMPLEN_EXTRA 4
	while (len < limit) {
		uint32_t x = read32ne(buf1 + len) - read32ne(buf2 + len);
		if (x != 0) {
			if ((x & 0xFFFF) == 0) {
				len += 2;
				x >>= 16;
			}

			if ((x & 0xFF) == 0)
				++len;

			return my_min(len, limit);
		}

		len += 4;
	}

	return limit;

#elif defined(TUKLIB_FAST_UNALIGNED_ACCESS) && defined(WORDS_BIGENDIAN)
	// Generic 32-bit big endian method
#	define LZMA_MEMCMPLEN_EXTRA 4
	while (len < limit) {
		uint32_t x = read32ne(buf1 + len) ^ read32ne(buf2 + len);
		if (x != 0) {
			if ((x & 0xFFFF0000) == 0) {
				len += 2;
				x <<= 16;
			}

			if ((x & 0xFF000000) == 0)
				++len;

			return my_min(len, limit);
		}

		len += 4;
	}

	return limit;

#else
	// Simple portable version that doesn't use unaligned access.
#	define LZMA_MEMCMPLEN_EXTRA 0
	while (len < limit && buf1[len] == buf2[len])
		++len;

	return len;
#endif
}

#endif
