// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_ARCH_H__
#define __DN_SIMDHASH_ARCH_H__

// HACK: for better language server parsing
#include "dn-simdhash.h"

static DN_FORCEINLINE(int)
find_first_matching_suffix_scalar (uint8_t needle[DN_SIMDHASH_VECTOR_WIDTH], uint8_t haystack[DN_SIMDHASH_VECTOR_WIDTH], uint32_t count)
{
	// TODO: It might be profitable to hand-unroll this loop, but right now doing so
	//  hits a bug in clang and generates really bad WASM.
	for (uint32_t i = 0; i < count; i++)
		if (needle[i] == haystack[i])
			return i;

	return 32;
}

static DN_FORCEINLINE(int)
find_first_matching_suffix_scalar_1 (uint8_t needle, uint8_t haystack[DN_SIMDHASH_VECTOR_WIDTH], uint32_t count)
{
	// TODO: It might be profitable to hand-unroll this loop, but right now doing so
	//  hits a bug in clang and generates really bad WASM.
	for (uint32_t i = 0; i < count; i++)
		if (needle == haystack[i])
			return i;

	return 32;
}

#if defined(__clang__) || defined (__GNUC__) // use vector intrinsics

#if defined(__wasm_simd128__)
#include <wasm_simd128.h>
#elif defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
#include <emmintrin.h>
#elif defined(__ARM_NEON)
#include <arm_neon.h>
#elif defined(__wasm)
#ifdef DN_SIMDHASH_WARNINGS
#pragma message("WARNING: Building dn_simdhash for WASM without -msimd128! Performance will be terrible!")
#endif
#else
#ifdef DN_SIMDHASH_WARNINGS
#pragma message("WARNING: Unsupported architecture for dn_simdhash! Performance will be terrible!")
#endif
#endif

// extract/replace lane opcodes require constant indices on some target architectures,
//  and in some cases it is profitable to do a single-byte memory load/store instead of
//  a full vector load/store, so we expose both layouts as a union

typedef uint8_t dn_u8x16 __attribute__ ((vector_size (DN_SIMDHASH_VECTOR_WIDTH), aligned(DN_SIMDHASH_VECTOR_WIDTH)));
typedef union {
	dn_u8x16 vec;
#if defined(_M_AMD64) || defined(_M_X64) || (_M_IX86_FP == 2) || defined(__SSE2__)
	__m128i m128;
#endif
	uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;


static DN_FORCEINLINE(uint32_t)
ctz (uint32_t value)
{
	// __builtin_ctz is undefined for 0
	if (value == 0)
		return 32;
	return (uint32_t)__builtin_ctz(value);
}

static DN_FORCEINLINE(dn_simdhash_suffixes)
build_search_vector (uint8_t needle)
{
	// this produces a splat and then .const, .and in wasm, and the other architectures are fine too
	dn_u8x16 needles = {
		needle, needle, needle, needle, needle, needle, needle, needle,
		needle, needle, needle, needle, needle, needle, needle, needle
	};
	// TODO: Evaluate whether the & mask is actually worth it. In the C# prototype, it wasn't.
	// Not doing it means there is a ~1% chance of a false positive in the data bytes near the
	//  end of the bucket, but we check the index against the bucket count anyway, so...
	dn_u8x16 mask = {
		0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu,
		0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0x00u, 0x00u
	};
	dn_simdhash_suffixes result;
	result.vec = needles & mask;
	return result;
}

// returns an index in range 0-14 on match, 32 if no match
static DN_FORCEINLINE(uint32_t)
find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack, uint32_t count)
{
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
	#define DN_SIMDHASH_USE_SCALAR_FALLBACK 1
	return find_first_matching_suffix_scalar(needle.values, haystack.values, count);
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

typedef struct {
	__m128i m128;
	uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

static DN_FORCEINLINE(dn_simdhash_suffixes)
build_search_vector (uint8_t needle)
{
	dn_simdhash_suffixes result;
	// FIXME: Completely untested.
	result.m128 = _mm_set1_epi8(needle);
	__m128i mask = _mm_set_epi8( // FIXME: setr not set?
		0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu,
		0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0x00u, 0x00u
	);
	result.m128 = _mm_and_si128(result.m128, mask);
	return result;
}

// returns an index in range 0-14 on match, 32 if no match
static DN_FORCEINLINE(uint32_t)
find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack, uint32_t count)
{
	// FIXME: Completely untested.
	__m128i match_vector = _mm_cmpeq_epi8(needle.m128, haystack.m128);
	return ctz(_mm_movemask_epi8(match_vector));
}

#else // unknown compiler and/or unknown non-simd arch

#ifdef DN_SIMDHASH_WARNINGS
#pragma message("WARNING: Unsupported architecture/compiler for dn_simdhash! Performance will be terrible!")
#endif

typedef struct {
	uint8_t values[DN_SIMDHASH_VECTOR_WIDTH];
} dn_simdhash_suffixes;

static DN_FORCEINLINE(dn_simdhash_suffixes)
build_search_vector (uint8_t needle)
{
	dn_simdhash_suffixes result;
	for (int i = 0; i < DN_SIMDHASH_VECTOR_WIDTH; i++)
		result.values[i] = (i >= DN_SIMDHASH_MAX_BUCKET_CAPACITY) ? 0 : needle;
	return result;
}

// returns an index in range 0-14 on match, 32 if no match
static DN_FORCEINLINE(uint32_t)
find_first_matching_suffix (dn_simdhash_suffixes needle, dn_simdhash_suffixes haystack, uint32_t count)
{
	#define DN_SIMDHASH_USE_SCALAR_FALLBACK 1
	return find_first_matching_suffix_scalar(needle.values, haystack.values, count);
}

#endif // end of clang/gcc or msvc or fallback

#endif // __DN_SIMDHASH_ARCH_H__
