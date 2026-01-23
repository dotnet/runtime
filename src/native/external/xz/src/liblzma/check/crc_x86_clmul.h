// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       crc_x86_clmul.h
/// \brief      CRC32 and CRC64 implementations using CLMUL instructions.
///
/// The CRC32 and CRC64 implementations use 32/64-bit x86 SSSE3, SSE4.1, and
/// CLMUL instructions. This is compatible with Elbrus 2000 (E2K) too.
///
/// See the Intel white paper "Fast CRC Computation for Generic Polynomials
/// Using PCLMULQDQ Instruction" from 2009. The original file seems to be
/// gone from Intel's website but a version is available here:
/// https://www.researchgate.net/publication/263424619_Fast_CRC_computation
/// (The link was checked on 2024-06-11.)
///
/// While this file has both CRC32 and CRC64 implementations, only one
/// can be built at a time. The version to build is selected by defining
/// BUILDING_CRC_CLMUL to 32 or 64 before including this file.
///
/// NOTE: The x86 CLMUL CRC implementation was rewritten for XZ Utils 5.8.0.
//
//  Authors:    Lasse Collin
//              Ilya Kurdyukov
//
///////////////////////////////////////////////////////////////////////////////

// This file must not be included more than once.
#ifdef LZMA_CRC_X86_CLMUL_H
#	error crc_x86_clmul.h was included twice.
#endif
#define LZMA_CRC_X86_CLMUL_H

#if BUILDING_CRC_CLMUL != 32 && BUILDING_CRC_CLMUL != 64
#	error BUILDING_CRC_CLMUL is undefined or has an invalid value
#endif

#include <immintrin.h>

#if defined(_MSC_VER)
#	include <intrin.h>
#elif defined(HAVE_CPUID_H)
#	include <cpuid.h>
#endif


// EDG-based compilers (Intel's classic compiler and compiler for E2K) can
// define __GNUC__ but the attribute must not be used with them.
// The new Clang-based ICX needs the attribute.
//
// NOTE: Build systems check for this too, keep them in sync with this.
#if (defined(__GNUC__) || defined(__clang__)) && !defined(__EDG__)
#	define crc_attr_target \
		__attribute__((__target__("ssse3,sse4.1,pclmul")))
#else
#	define crc_attr_target
#endif


// GCC and Clang would produce good code with _mm_set_epi64x
// but MSVC needs _mm_cvtsi64_si128 on x86-64.
#if defined(__i386__) || defined(_M_IX86)
#	define my_set_low64(a) _mm_set_epi64x(0, (a))
#else
#	define my_set_low64(a) _mm_cvtsi64_si128(a)
#endif


// Align it so that the whole array is within the same cache line.
// More than one unaligned load can be done from this during the
// same CRC function call.
//
// The bytes [0] to [31] are used with AND to clear the low bytes. (With ANDN
// those could be used to clear the high bytes too but it's not needed here.)
//
// The bytes [16] to [47] are for left shifts.
// The bytes [32] to [63] are for right shifts.
alignas(64)
static uint8_t vmasks[64] = {
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
	0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
	0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
};


// *Unaligned* 128-bit load
crc_attr_target
static inline __m128i
my_load128(const uint8_t *p)
{
	return _mm_loadu_si128((const __m128i *)p);
}


// Keep the highest "count" bytes as is and clear the remaining low bytes.
crc_attr_target
static inline __m128i
keep_high_bytes(__m128i v, size_t count)
{
	return _mm_and_si128(my_load128((vmasks + count)), v);
}


// Shift the 128-bit value left by "amount" bytes (not bits).
crc_attr_target
static inline __m128i
shift_left(__m128i v, size_t amount)
{
	return _mm_shuffle_epi8(v, my_load128((vmasks + 32 - amount)));
}


// Shift the 128-bit value right by "amount" bytes (not bits).
crc_attr_target
static inline __m128i
shift_right(__m128i v, size_t amount)
{
	return _mm_shuffle_epi8(v, my_load128((vmasks + 32 + amount)));
}


crc_attr_target
static inline __m128i
fold(__m128i v, __m128i k)
{
	__m128i a = _mm_clmulepi64_si128(v, k, 0x00);
	__m128i b = _mm_clmulepi64_si128(v, k, 0x11);
	return _mm_xor_si128(a, b);
}


crc_attr_target
static inline __m128i
fold_xor(__m128i v, __m128i k, const uint8_t *buf)
{
	return _mm_xor_si128(my_load128(buf), fold(v, k));
}


#if BUILDING_CRC_CLMUL == 32
crc_attr_target
static uint32_t
crc32_arch_optimized(const uint8_t *buf, size_t size, uint32_t crc)
#else
crc_attr_target
static uint64_t
crc64_arch_optimized(const uint8_t *buf, size_t size, uint64_t crc)
#endif
{
	// We will assume that there is at least one byte of input.
	if (size == 0)
		return crc;

	// See crc_clmul_consts_gen.c.
#if BUILDING_CRC_CLMUL == 32
	const __m128i fold512 = _mm_set_epi64x(0x1d9513d7, 0x8f352d95);
	const __m128i fold128 = _mm_set_epi64x(0xccaa009e, 0xae689191);
	const __m128i mu_p = _mm_set_epi64x(
		(int64_t)0xb4e5b025f7011641, 0x1db710640);
#else
	const __m128i fold512 = _mm_set_epi64x(
		(int64_t)0x081f6054a7842df4, (int64_t)0x6ae3efbb9dd441f3);

	const __m128i fold128 = _mm_set_epi64x(
		(int64_t)0xdabe95afc7875f40, (int64_t)0xe05dd497ca393ae4);

	const __m128i mu_p = _mm_set_epi64x(
		(int64_t)0x9c3e466c172963d5, (int64_t)0x92d8af2baf0e1e84);
#endif

	__m128i v0, v1, v2, v3;

	crc = ~crc;

	if (size < 8) {
		uint64_t x = crc;
		size_t i = 0;

		// Checking the bit instead of comparing the size means
		// that we don't need to update the size between the steps.
		if (size & 4) {
			x ^= read32le(buf);
			buf += 4;
			i = 32;
		}

		if (size & 2) {
			x ^= (uint64_t)read16le(buf) << i;
			buf += 2;
			i += 16;
		}

		if (size & 1)
			x ^= (uint64_t)*buf << i;

		v0 = my_set_low64((int64_t)x);
		v0 = shift_left(v0, 8 - size);

	} else if (size < 16) {
		v0 = my_set_low64((int64_t)(crc ^ read64le(buf)));

		// NOTE: buf is intentionally left 8 bytes behind so that
		// we can read the last 1-7 bytes with read64le(buf + size).
		size -= 8;

		// Handling 8-byte input specially is a speed optimization
		// as the clmul can be skipped. A branch is also needed to
		// avoid a too high shift amount.
		if (size > 0) {
			const size_t padding = 8 - size;
			uint64_t high = read64le(buf + size) >> (padding * 8);

#if defined(__i386__) || defined(_M_IX86)
			// Simple but likely not the best code for 32-bit x86.
			v0 = _mm_insert_epi32(v0, (int32_t)high, 2);
			v0 = _mm_insert_epi32(v0, (int32_t)(high >> 32), 3);
#else
			v0 = _mm_insert_epi64(v0, (int64_t)high, 1);
#endif

			v0 = shift_left(v0, padding);

			v1 = _mm_srli_si128(v0, 8);
			v0 = _mm_clmulepi64_si128(v0, fold128, 0x10);
			v0 = _mm_xor_si128(v0, v1);
		}
	} else {
		v0 = my_set_low64((int64_t)crc);

		// To align or not to align the buf pointer? If the end of
		// the buffer isn't aligned, aligning the pointer here would
		// make us do an extra folding step with the associated byte
		// shuffling overhead. The cost of that would need to be
		// lower than the benefit of aligned reads. Testing on an old
		// Intel Ivy Bridge processor suggested that aligning isn't
		// worth the cost but it likely depends on the processor and
		// buffer size. Unaligned loads (MOVDQU) should be fast on
		// x86 processors that support PCLMULQDQ, so we don't align
		// the buf pointer here.

		// Read the first (and possibly the only) full 16 bytes.
		v0 = _mm_xor_si128(v0, my_load128(buf));
		buf += 16;
		size -= 16;

		if (size >= 48) {
			v1 = my_load128(buf);
			v2 = my_load128(buf + 16);
			v3 = my_load128(buf + 32);
			buf += 48;
			size -= 48;

			while (size >= 64) {
				v0 = fold_xor(v0, fold512, buf);
				v1 = fold_xor(v1, fold512, buf + 16);
				v2 = fold_xor(v2, fold512, buf + 32);
				v3 = fold_xor(v3, fold512, buf + 48);
				buf += 64;
				size -= 64;
			}

			v0 = _mm_xor_si128(v1, fold(v0, fold128));
			v0 = _mm_xor_si128(v2, fold(v0, fold128));
			v0 = _mm_xor_si128(v3, fold(v0, fold128));
		}

		while (size >= 16) {
			v0 = fold_xor(v0, fold128, buf);
			buf += 16;
			size -= 16;
		}

		if (size > 0) {
			// We want the last "size" number of input bytes to
			// be at the high bits of v1. First do a full 16-byte
			// load and then mask the low bytes to zeros.
			v1 = my_load128(buf + size - 16);
			v1 = keep_high_bytes(v1, size);

			// Shift high bytes from v0 to the low bytes of v1.
			//
			// Alternatively we could replace the combination
			// keep_high_bytes + shift_right + _mm_or_si128 with
			// _mm_shuffle_epi8 + _mm_blendv_epi8 but that would
			// require larger tables for the masks. Now there are
			// three loads (instead of two) from the mask tables
			// but they all are from the same cache line.
			v1 = _mm_or_si128(v1, shift_right(v0, size));

			// Shift high bytes of v0 away, padding the
			// low bytes with zeros.
			v0 = shift_left(v0, 16 - size);

			v0 = _mm_xor_si128(v1, fold(v0, fold128));
		}

		v1 = _mm_srli_si128(v0, 8);
		v0 = _mm_clmulepi64_si128(v0, fold128, 0x10);
		v0 = _mm_xor_si128(v0, v1);
	}

	// Barrett reduction

#if BUILDING_CRC_CLMUL == 32
	v1 = _mm_clmulepi64_si128(v0, mu_p, 0x10); // v0 * mu
	v1 = _mm_clmulepi64_si128(v1, mu_p, 0x00); // v1 * p
	v0 = _mm_xor_si128(v0, v1);
	return ~(uint32_t)_mm_extract_epi32(v0, 2);
#else
	// Because p is 65 bits but one bit doesn't fit into the 64-bit
	// half of __m128i, finish the second clmul by shifting v1 left
	// by 64 bits and xorring it to the final result.
	v1 = _mm_clmulepi64_si128(v0, mu_p, 0x10); // v0 * mu
	v2 = _mm_slli_si128(v1, 8);
	v1 = _mm_clmulepi64_si128(v1, mu_p, 0x00); // v1 * p
	v0 = _mm_xor_si128(v0, v2);
	v0 = _mm_xor_si128(v0, v1);
#if defined(__i386__) || defined(_M_IX86)
	return ~(((uint64_t)(uint32_t)_mm_extract_epi32(v0, 3) << 32) |
			(uint64_t)(uint32_t)_mm_extract_epi32(v0, 2));
#else
	return ~(uint64_t)_mm_extract_epi64(v0, 1);
#endif
#endif
}


// Even though this is an inline function, compile it only when needed.
// This way it won't appear in E2K builds at all.
#if defined(CRC32_GENERIC) || defined(CRC64_GENERIC)
// Inlining this function duplicates the function body in crc32_resolve() and
// crc64_resolve(), but this is acceptable because this is a tiny function.
static inline bool
is_arch_extension_supported(void)
{
	int success = 1;
	uint32_t r[4]; // eax, ebx, ecx, edx

#if defined(_MSC_VER)
	// This needs <intrin.h> with MSVC. ICC has it as a built-in
	// on all platforms.
	__cpuid(r, 1);
#elif defined(HAVE_CPUID_H)
	// Compared to just using __asm__ to run CPUID, this also checks
	// that CPUID is supported and saves and restores ebx as that is
	// needed with GCC < 5 with position-independent code (PIC).
	success = __get_cpuid(1, &r[0], &r[1], &r[2], &r[3]);
#else
	// Just a fallback that shouldn't be needed.
	__asm__("cpuid\n\t"
			: "=a"(r[0]), "=b"(r[1]), "=c"(r[2]), "=d"(r[3])
			: "a"(1), "c"(0));
#endif

	// Returns true if these are supported:
	// CLMUL (bit 1 in ecx)
	// SSSE3 (bit 9 in ecx)
	// SSE4.1 (bit 19 in ecx)
	const uint32_t ecx_mask = (1 << 1) | (1 << 9) | (1 << 19);
	return success && (r[2] & ecx_mask) == ecx_mask;

	// Alternative methods that weren't used:
	//   - ICC's _may_i_use_cpu_feature: the other methods should work too.
	//   - GCC >= 6 / Clang / ICX __builtin_cpu_supports("pclmul")
	//
	// CPUID decoding is needed with MSVC anyway and older GCC. This keeps
	// the feature checks in the build system simpler too. The nice thing
	// about __builtin_cpu_supports would be that it generates very short
	// code as is it only reads a variable set at startup but a few bytes
	// doesn't matter here.
}
#endif
