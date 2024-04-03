// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.

inline static uint32_t
ROTL32 (uint32_t x, int8_t r)
{
	return (x << r) | (x >> (32 - r));
}

// Finalization mix - force all bits of a hash block to avalanche
inline static uint32_t
fmix32 (uint32_t h)
{
	h ^= h >> 16;
	h *= 0x85ebca6b;
	h ^= h >> 13;
	h *= 0xc2b2ae35;
	h ^= h >> 16;

	return h;
}

#define BLOCK_COUNT ((sizeof (void *)) / 4)

// Hash a void * (number of 4-byte blocks determined by sizeof (void *))
static uint32_t
MurmurHash3_32_ptr (const void *ptr, uint32_t seed)
{
	uint32_t h1 = seed;
	const uint32_t c1 = 0xcc9e2d51, c2 = 0x1b873593;

	union {
		uint32_t u32[BLOCK_COUNT];
		const void *ptr;
	} u;
	u.ptr = ptr;

	for (uint32_t i = 0; i < BLOCK_COUNT; i++) {
		uint32_t k1 = u.u32[i];
		k1 *= c1;
		k1 = ROTL32(k1, 15);
		k1 *= c2;
		h1 ^= k1;
		h1 = ROTL32(h1, 13);
		h1 = h1 * 5 + 0xe6546b64;
	}

	// finalize
	h1 ^= BLOCK_COUNT;
	h1 = fmix32(h1);
	return h1;
}

// end of murmurhash

#if defined(__clang__) || defined (__GNUC__)
#define unlikely(expr) __builtin_expect(!!(expr), 0)
#define likely(expr)   __builtin_expect(!!(expr), 1)
#else
#define unlikely(expr) (expr)
#define likely(expr) (expr)
#endif

// FNV has bad properties for simdhash even though it's a fairly fast/good hash,
//  but the overhead of having to do strlen() first before passing a string key to
//  MurmurHash3 is significant and annoying. This is an attempt to reformulate the
//  32-bit version of MurmurHash3 into a 1-pass version for null terminated strings.
// The output of this will probably be different from regular MurmurHash3. I don't
//  see that as a problem, since you shouldn't rely on the exact bit patterns of
//  a non-cryptographic hash anyway.
typedef struct scan_result_t {
	union {
		uint32_t u32;
		uint8_t bytes[4];
	} result;
	const uint8_t *next;
} scan_result_t;

static inline scan_result_t
scan_forward (const uint8_t *ptr)
{
	// TODO: On wasm we could do a single u32 load then scan the bytes,
	//  as long as we're sure ptr isn't up against the end of memory
	scan_result_t result = { 0, };

	// I tried to get a loop to auto-unroll, but GCC only unrolls at O3 and MSVC never does.
#define SCAN_1(i) \
	result.result.bytes[i] = ptr[i]; \
	if (unlikely(!result.result.bytes[i])) \
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
	const uint32_t c1 = 0xcc9e2d51, c2 = 0x1b873593;

	// Scan forward through the buffer collecting up to 4 bytes at a time, then hash
	scan_result_t block = scan_forward(key);
	// As long as the scan found at least one nonzero byte, u32 will be != 0
	while (block.result.u32) {
		block_count += 1;

		uint32_t k1 = block.result.u32;
		k1 *= c1;
		k1 = ROTL32(k1, 15);
		k1 *= c2;
		h1 ^= k1;
		h1 = ROTL32(h1, 13);
		h1 = h1 * 5 + 0xe6546b64;

		// If the scan found a null byte next will be 0, so we stop scanning
		if (!block.next)
			break;
		block = scan_forward(block.next);
	}

	// finalize. we don't have an exact byte length but we have a block count
	// it would be ideal to figure out a cheap way to produce an exact byte count,
	//  since then we can compute the length and hash in one go and use memcmp later,
	// since emscripten/musl strcmp isn't optimized at all
	h1 ^= block_count;
	h1 = fmix32(h1);
	return h1;
}

// end of reformulated murmur3-32
