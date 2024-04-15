// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_ARCH_H__
#define __DN_SIMDHASH_ARCH_H__

// #define DN_SIMDHASH_WARNINGS 1

// HACK: for better language server parsing
#include "dn-simdhash.h"

#if defined(__clang__) || defined (__GNUC__) // use vector intrinsics

#if defined(__wasm_simd128__)
#include <wasm_simd128.h>
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
#include <emmintrin.h>
#elif defined(__ARM_NEON)
#include <arm_neon.h>
#elif defined(__wasm)
#define DN_SIMDHASH_USE_SCALAR_FALLBACK 1
#ifdef DN_SIMDHASH_WARNINGS
#pragma message("WARNING: Building dn_simdhash for WASM without -msimd128! Performance will be terrible!")
#endif
#else
#define DN_SIMDHASH_USE_SCALAR_FALLBACK 1
#ifdef DN_SIMDHASH_WARNINGS
#pragma message("WARNING: Unsupported architecture for dn_simdhash! Performance will be terrible!")
#endif
#endif

// extract/replace lane opcodes require constant indices on some target architectures,
//  and in some cases it is profitable to do a single-byte memory load/store instead of
//  a full vector load/store, so we expose both layouts as a union

typedef uint8_t dn_u8x16 __attribute__ ((vector_size (DN_SIMDHASH_VECTOR_WIDTH), aligned(DN_SIMDHASH_VECTOR_WIDTH)));
typedef union {
	_Alignas(DN_SIMDHASH_VECTOR_WIDTH) dn_u8x16 vec;
#if defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
	_Alignas(DN_SIMDHASH_VECTOR_WIDTH) __m128i m128;
#endif
	_Alignas(DN_SIMDHASH_VECTOR_WIDTH) uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

#ifdef DN_SIMDHASH_USE_SCALAR_FALLBACK
typedef uint8_t dn_simdhash_search_vector;
#else
typedef dn_simdhash_suffixes dn_simdhash_search_vector;
#endif

// Extracting lanes from a vector register on x86/x64 has horrible latency,
//  so it's better to do regular byte loads from the stack
#if defined(__wasm_simd128__)
// For wasm with -msimd128, clang generates truly bizarre load/store code
//  where it does two byte memory loads, then a vector load, then two
//  lane insertions to write the byte loads into the loaded vector
//  before finally passing it to find_first_matching_suffix. So we have to vec[].
// See https://github.com/llvm/llvm-project/issues/87398#issuecomment-2050696298
// Also see https://github.com/llvm/llvm-project/issues/88460
#define dn_simdhash_extract_lane(suffixes, lane) \
	suffixes.vec[lane]
#else
#define dn_simdhash_extract_lane(suffixes, lane) \
	suffixes.values[lane]
#endif

static DN_FORCEINLINE(uint32_t)
ctz (uint32_t value)
{
	// __builtin_ctz is undefined for 0
	if (value == 0)
		return 32;
	return (uint32_t)__builtin_ctz(value);
}

static DN_FORCEINLINE(dn_simdhash_search_vector)
build_search_vector (uint8_t needle)
{
#ifdef DN_SIMDHASH_USE_SCALAR_FALLBACK
	return needle;
#else
	dn_simdhash_suffixes result;
	// this produces a splat in wasm, and the other architectures are fine too
	dn_u8x16 needles = {
		needle, needle, needle, needle, needle, needle, needle, needle,
		needle, needle, needle, needle, needle, needle, needle, needle
	};
	result.vec = needles;
	return result;
#endif
}

// returns an index in range 0-13 on match, 14-32 if no match
static DN_FORCEINLINE(uint32_t)
find_first_matching_suffix (
	dn_simdhash_search_vector needle,
	// Only used by the vectorized implementations; discarded by scalar.
	dn_simdhash_suffixes haystack,
	// HACK: Pass the address of haystack.values directly, for scalar fallback.
	// Without this, clang makes a full unaligned copy of haystack before calling us.
	// Discarded by the vectorized implementations.
	uint8_t haystack_values[DN_SIMDHASH_VECTOR_WIDTH],
	uint32_t count
) {
#if defined(__wasm_simd128__)
	return ctz(wasm_i8x16_bitmask(wasm_i8x16_eq(needle.vec, haystack.vec)));
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
	return ctz(_mm_movemask_epi8(_mm_cmpeq_epi8(needle.m128, haystack.m128)));
#elif defined(__ARM_NEON)
	dn_simdhash_suffixes match_vector;
	// Completely untested.
	static const dn_simdhash_suffixes byte_mask = {
		1, 2, 4, 8, 16, 32, 64, 128, 1, 2, 4, 8, 16, 32, 64, 128
	};
	union {
		uint8_t b[4];
		uint32_t u;
	} msb;
	match_vector.vec = vceqq_u8(needle.vec, haystack.vec);
	dn_simdhash_suffixes masked;
	masked.vec = vandq_u8(match_vector.vec, byte_mask.vec);
	msb.b[0] = vaddv_u8(vget_low_u8(masked.vec));
	msb.b[1] = vaddv_u8(vget_high_u8(masked.vec));
	return ctz(msb.u);
#else
	// HACK: We can't put this in a common helper function without introducing a temporary
	//  unaligned copy-from-table-to-stack in wasm-without-simd
#define ITER(offset) \
	if (needle == haystack_values[offset]) \
		return offset;

	// It is safe to unroll this without bounds checks
	// One would expect this to blow out the branch predictor, but in my testing
	//  it's significantly faster when there is no match, and slightly faster
	//  for cases where there is a match.
	// Looping from 0-count is slower than this in my testing, even though it's
	//  going to check fewer suffixes most of the time - probably due to the
	//  comparison against count for each suffix.
	// FIXME: If we move this into the specialization header, we can limit the
	//  number of unrolled iterations to the number of keys in the bucket.
	ITER(0);
	ITER(1);
	ITER(2);
	ITER(3);
	ITER(4);
	ITER(5);
	ITER(6);
	ITER(7);
	ITER(8);
	ITER(9);
	ITER(10);
	ITER(11);
	ITER(12);
	ITER(13);
#undef ITER
	return 32;
#endif
}

#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
// neither clang or gcc, but we have SSE2 available, so assume this is MSVC on x86 or x86-64
// msvc neon intrinsics don't seem to expose a 128-bit wide vector so there's no neon in here
#include <intrin.h> // for _BitScanForward

static DN_FORCEINLINE(uint32_t)
ctz (uint32_t value)
{
	unsigned long result = 0;
	if (_BitScanForward(&result, value))
		return (uint32_t)result;
	else
		return 32;
}

#include <emmintrin.h>

typedef union {
	_Alignas(DN_SIMDHASH_VECTOR_WIDTH) __m128i m128;
	_Alignas(DN_SIMDHASH_VECTOR_WIDTH) uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

typedef dn_simdhash_suffixes dn_simdhash_search_vector;

#define dn_simdhash_extract_lane(suffixes, lane) \
	suffixes.values[lane]

static DN_FORCEINLINE(dn_simdhash_search_vector)
build_search_vector (uint8_t needle)
{
	dn_simdhash_suffixes result;
	result.m128 = _mm_set1_epi8(needle);
	return result;
}

// returns an index in range 0-13 on match, 14-32 if no match
static DN_FORCEINLINE(uint32_t)
find_first_matching_suffix_internal (
	__m128i needle, __m128i haystack,
	uint32_t count
) {
	return ctz(_mm_movemask_epi8(_mm_cmpeq_epi8(needle, haystack)));
}

// use a macro to discard haystack_values, otherwise MSVC's codegen is worse
#define find_first_matching_suffix(needle, haystack, haystack_values, count) \
	find_first_matching_suffix_internal(needle.m128, haystack.m128, count)

#else // unknown compiler and/or unknown non-simd arch

#define DN_SIMDHASH_USE_SCALAR_FALLBACK 1

#ifdef DN_SIMDHASH_WARNINGS
#pragma message("WARNING: Unsupported architecture/compiler for dn_simdhash! Performance will be terrible!")
#endif

typedef struct {
	_Alignas(DN_SIMDHASH_VECTOR_WIDTH) uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

typedef uint8_t dn_simdhash_search_vector;

#define dn_simdhash_extract_lane(suffixes, lane) \
	suffixes.values[lane]

static DN_FORCEINLINE(dn_simdhash_search_vector)
build_search_vector (uint8_t needle)
{
	return needle;
}

// returns an index in range 0-14 on match, 32 if no match
static DN_FORCEINLINE(uint32_t)
find_first_matching_suffix (
	dn_simdhash_search_vector needle, dn_simdhash_suffixes haystack,
	uint8_t haystack_values[DN_SIMDHASH_VECTOR_WIDTH], uint32_t count
) {
	// TODO: It might be profitable to hand-unroll this loop, but right now doing so
	//  hits a bug in clang and generates really bad WASM.
	// HACK: We can't put this in a common helper function without introducing a temporary
	//  unaligned copy-from-table-to-stack in wasm-without-simd
	for (uint32_t i = 0; i < count; i++)
		if (needle == haystack_values[i])
			return i;
	return 32;
}

#endif // end of clang/gcc or msvc or fallback

#endif // __DN_SIMDHASH_ARCH_H__
