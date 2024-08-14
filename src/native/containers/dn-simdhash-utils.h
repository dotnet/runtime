// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DN_SIMDHASH_UTILS_H__
#define __DN_SIMDHASH_UTILS_H__

#include <stdint.h>

#if defined(__clang__) || defined (__GNUC__)
static DN_FORCEINLINE(uint32_t)
next_power_of_two (uint32_t value) {
	if (value < 2)
		return 1;
	return 1u << (32 - __builtin_clz (value - 1));
}
#else // __clang__ || __GNUC__
static DN_FORCEINLINE(uint32_t)
next_power_of_two (uint32_t value) {
	if (value < 2)
		return 1;
	value--;
	value |= value >> 1;
	value |= value >> 2;
	value |= value >> 4;
	value |= value >> 8;
	value |= value >> 16;
	value++;
	return value;
}
#endif // __clang__ || __GNUC__

// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.

static const uint32_t murmur3_c1 = 0xcc9e2d51, murmur3_c2 = 0x1b873593;

inline static uint32_t
murmur3_rotl32 (uint32_t x, int8_t r)
{
	return (x << r) | (x >> (32 - r));
}

// Finalization mix - force all bits of a hash block to avalanche
inline static uint32_t
murmur3_fmix32 (uint32_t h)
{
	h ^= h >> 16;
	h *= 0x85ebca6b;
	h ^= h >> 13;
	h *= 0xc2b2ae35;
	h ^= h >> 16;

	return h;
}

inline static uint64_t
murmur3_fmix64(uint64_t k)
{
	k ^= k >> 33;
	k *= 0xff51afd7ed558ccdLLU;
	k ^= k >> 33;
	k *= 0xc4ceb9fe1a85ec53LLU;
	k ^= k >> 33;
	return k;
}

// Convenience macro so you can define your own fixed-size MurmurHashes
#define MURMUR3_HASH_BLOCK(block) \
	{ \
		uint32_t k1 = block; \
		k1 *= murmur3_c1; \
		k1 = murmur3_rotl32(k1, 15); \
		k1 *= murmur3_c2; \
		h1 ^= k1; \
		h1 = murmur3_rotl32(h1, 13); \
		h1 = h1 * 5 + 0xe6546b64; \
	}

// Hash a void * (either 4 or 8 bytes)
static inline uint32_t
MurmurHash3_32_ptr (const void *ptr, uint32_t seed)
{
	// mono_aligned_addr_hash shifts all incoming pointers by 3 bits to account
	//  for a presumed 8-byte alignment of addresses (the dlmalloc default).
	const uint32_t alignment_shift = 3;
	// Compute this outside of the if to suppress msvc build warning
	const uint8_t is_64_bit = sizeof(void*) == sizeof(uint64_t);
	union {
		uint32_t u32;
		uint64_t u64;
		const void *ptr;
	} u;
	u.ptr = ptr;

	// Apply murmurhash3's finalization bit mixer to a pointer to compute a 32-bit hash.
	if (is_64_bit) {
		// The high bits of a 64-bit pointer are usually low entropy, as are the
		//  2-3 lowest bits. We want to capture most of the entropy and mix it into
		//  a 32-bit hash to reduce the odds of hash collisions for arbitrary 64-bit
		//  pointers. From my testing, this is a good way to do it.
		return murmur3_fmix32((uint32_t)((u.u64 >> alignment_shift) & 0xFFFFFFFFu));
		// return (uint32_t)(murmur3_fmix64(u.u64 >> alignment_shift) & 0xFFFFFFFFu);
	} else {
		// No need for an alignment shift here, we're mixing the bits and then
		//  simdhash uses 7 of the top bits and a handful of the low bits.
		return murmur3_fmix32(u.u32);
	}
}

// end of murmurhash

// FNV has bad properties for simdhash even though it's a fairly fast/good hash,
//  but the overhead of having to do strlen() first before passing a string key to
//  MurmurHash3 is significant and annoying. This is an attempt to reformulate the
//  32-bit version of MurmurHash3 into a 1-pass version for null terminated strings.
// The output of this will probably be different from regular MurmurHash3. I don't
//  see that as a problem, since you shouldn't rely on the exact bit patterns of
//  a non-cryptographic hash anyway.
typedef struct murmur3_scan_result_t {
	union {
		uint32_t u32;
		uint8_t bytes[4];
	} result;
	const uint8_t *next;
} murmur3_scan_result_t;

static inline murmur3_scan_result_t
murmur3_scan_forward (const uint8_t *ptr)
{
	// TODO: On wasm we could do a single u32 load then scan the bytes,
	//  as long as we're sure ptr isn't up against the end of memory
	murmur3_scan_result_t result = { 0, };

	// I tried to get a loop to auto-unroll, but GCC only unrolls at O3 and MSVC never does.
#define SCAN_1(i) \
	result.result.bytes[i] = ptr[i]; \
	if (DN_UNLIKELY(!result.result.bytes[i])) \
		return result;

	SCAN_1(0);
	SCAN_1(1);
	SCAN_1(2);
	SCAN_1(3);
#undef SCAN_1

	// doing ptr[i] 4 times then computing here produces better code than ptr++ especially on wasm
	result.next = ptr + 4;
	return result;
}

static inline uint32_t
MurmurHash3_32_streaming (const uint8_t *key, uint32_t seed)
{
	uint32_t h1 = seed, block_count = 0;

	// Scan forward through the buffer collecting up to 4 bytes at a time, then hash
	murmur3_scan_result_t block = murmur3_scan_forward(key);
	// As long as the scan found at least one nonzero byte, u32 will be != 0
	while (block.result.u32) {
		block_count += 1;

		MURMUR3_HASH_BLOCK(block.result.u32);

		// If the scan found a null byte next will be 0, so we stop scanning
		if (DN_UNLIKELY(!block.next))
			break;
		block = murmur3_scan_forward(block.next);
	}

	// finalize. we don't have an exact byte length but we have a block count
	// it would be ideal to figure out a cheap way to produce an exact byte count,
	//  since then we can compute the length and hash in one go and use memcmp later,
	// since emscripten/musl strcmp isn't optimized at all
	h1 ^= block_count;
	h1 = murmur3_fmix32(h1);
	return h1;
}

// end of reformulated murmur3-32

void
#ifdef _MSC_VER
__cdecl
#endif
dn_simdhash_assert_fail (const char *file, int line, const char *condition);

#define dn_simdhash_assert(expr) \
	if (DN_UNLIKELY(!(expr))) { \
		dn_simdhash_assert_fail(__FILE__, __LINE__, #expr); \
	}

#endif // __DN_SIMDHASH_UTILS_H__
