// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

typedef struct dn_simdhash_str_key {
	const char *text;
	// We keep a precomputed hash to speed up rehashing and scans.
	uint32_t hash;
#if SIZEOF_VOID_P == 8
	// HACK: Perfect cache alignment isn't possible for a 12-byte struct, so pad it to 16 bytes
	uint32_t padding;
#endif
} dn_simdhash_str_key;


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

static inline int32_t
dn_simdhash_str_equal (dn_simdhash_str_key v1, dn_simdhash_str_key v2)
{
	if (v1.text == v2.text)
		return 1;
	return strcmp(v1.text, v2.text) == 0;
}

static inline uint32_t
dn_simdhash_str_hash (dn_simdhash_str_key v1)
{
	return v1.hash;
}

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T dn_simdhash_str_key
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER dn_simdhash_str_hash
#define DN_SIMDHASH_KEY_EQUALS dn_simdhash_str_equal
#define DN_SIMDHASH_ACCESSOR_SUFFIX _raw

// perfect cache alignment. 32-bit ptrs: 8-byte keys. 64-bit: 16-byte keys.
#if SIZEOF_VOID_P == 8
#define DN_SIMDHASH_BUCKET_CAPACITY 11
#else
#define DN_SIMDHASH_BUCKET_CAPACITY 12
#endif

#include "dn-simdhash-specialization.h"
#include "dn-simdhash-string-ptr.h"

static dn_simdhash_str_key
dn_simdhash_make_str_key (const char *text)
{
	dn_simdhash_str_key result = { 0, };
	if (text) {
		// FIXME: Select a good seed.
		result.hash = MurmurHash3_32_streaming((uint8_t *)text, 0);
		result.text = text;
	}
	return result;
}

uint8_t
dn_simdhash_string_ptr_try_add (dn_simdhash_string_ptr_t *hash, const char *key, void *value)
{
	return dn_simdhash_string_ptr_try_add_raw(hash, dn_simdhash_make_str_key(key), value);
}

uint8_t
dn_simdhash_string_ptr_try_get_value (dn_simdhash_string_ptr_t *hash, const char *key, void **result)
{
	return dn_simdhash_string_ptr_try_get_value_raw(hash, dn_simdhash_make_str_key(key), result);
}

uint8_t
dn_simdhash_string_ptr_try_remove (dn_simdhash_string_ptr_t *hash, const char *key)
{
	return dn_simdhash_string_ptr_try_remove_raw(hash, dn_simdhash_make_str_key(key));
}

// FIXME: Find a way to make this easier to define
void
dn_simdhash_string_ptr_foreach (dn_simdhash_string_ptr_t *hash, dn_simdhash_string_ptr_foreach_func func, void *user_data)
{
	assert(hash);
	assert(func);

	dn_simdhash_buffers_t buffers = hash->buffers;
	bucket_t *bucket_address = address_of_bucket(buffers, 0);
	for (
		uint32_t i = 0, bc = buffers.buckets_length, value_slot_base = 0;
		i < bc; i++, bucket_address++, value_slot_base += DN_SIMDHASH_BUCKET_CAPACITY
	) {
		uint32_t c = dn_simdhash_bucket_count(bucket_address->suffixes);
		for (uint32_t j = 0; j < c; j++) {
			DN_SIMDHASH_KEY_T *key = &bucket_address->keys[j];
			DN_SIMDHASH_VALUE_T value = *address_of_value(buffers, value_slot_base + j);
			func(key->text, value, user_data);
		}
	}
}
