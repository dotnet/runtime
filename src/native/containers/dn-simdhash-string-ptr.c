// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

typedef struct dn_simdhash_str_key {
	uint32_t length;
	const char *text;
} dn_simdhash_str_key;

// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.
//
// Implementation was copied from https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
// with changes around strict-aliasing/unaligned reads

inline static uint32_t
ROTL32 (uint32_t x, int8_t r)
{
	return (x << r) | (x >> (32 - r));
}

inline static uint32_t
getblock32 (const uint32_t* ptr, int i)
{
	uint32_t val = 0;
	memcpy(&val, ptr + i, sizeof(uint32_t));
	return val;
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

inline static uint32_t
MurmurHash3_x86_32 (const void * key, int len, uint32_t seed)
{
	const uint8_t * data = (const uint8_t*)key;
	const int nblocks = len / 4;

	uint32_t h1 = seed;

	const uint32_t c1 = 0xcc9e2d51;
	const uint32_t c2 = 0x1b873593;

	//----------
	// body

	const uint32_t * blocks = (const uint32_t *)(data + nblocks*4);

	for(int i = -nblocks; i; i++)
	{
		uint32_t k1 = getblock32(blocks,i);

		k1 *= c1;
		k1 = ROTL32(k1,15);
		k1 *= c2;

		h1 ^= k1;
		h1 = ROTL32(h1,13);
		h1 = h1*5+0xe6546b64;
	}

	//----------
	// tail

	const uint8_t * tail = (const uint8_t*)(data + nblocks*4);

	uint32_t k1 = 0;

	switch(len & 3)
	{
		case 3: k1 ^= tail[2] << 16;
		case 2: k1 ^= tail[1] << 8;
		case 1: k1 ^= tail[0];
				k1 *= c1; k1 = ROTL32(k1,15); k1 *= c2; h1 ^= k1;
	};

	//----------
	// finalization

	h1 ^= len;

	h1 = fmix32(h1);

	return h1;
}

// end of murmurhash

static int32_t
dn_simdhash_str_equal (dn_simdhash_str_key v1, dn_simdhash_str_key v2)
{
	if (v1.text == v2.text)
		return 1;
	if (v1.length != v2.length)
		return 0;
	return memcmp (v1.text, v2.text, v1.length) == 0;
}

static uint32_t
dn_simdhash_str_hash (dn_simdhash_str_key v1)
{
	if (!v1.length)
		return 0;

	// FIXME: Select a good seed.
	return MurmurHash3_x86_32(v1.text, v1.length, 0);
}

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T dn_simdhash_str_key
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER dn_simdhash_str_hash
#define DN_SIMDHASH_KEY_EQUALS dn_simdhash_str_equal
#define DN_SIMDHASH_KEY_IS_POINTER 0
#define DN_SIMDHASH_VALUE_IS_POINTER 1
#define DN_SIMDHASH_ACCESSOR_SUFFIX _raw
#define DN_SIMDHASH_BUCKET_CAPACITY 9

#include "dn-simdhash-specialization.h"
#include "dn-simdhash-string-ptr.h"

static dn_simdhash_str_key
make_key (const char *text)
{
	dn_simdhash_str_key result = { 0, };
	if (text) {
		result.length = strlen(text);
		result.text = text;
	}
	return result;
}

uint8_t
dn_simdhash_string_ptr_try_add (dn_simdhash_string_ptr_t *hash, const char *key, void *value)
{
	return dn_simdhash_string_ptr_try_add_raw(hash, make_key(key), value);
}

uint8_t
dn_simdhash_string_ptr_try_get_value (dn_simdhash_string_ptr_t *hash, const char *key, void **result)
{
	return dn_simdhash_string_ptr_try_get_value_raw(hash, make_key(key), result);
}

uint8_t
dn_simdhash_string_ptr_try_remove (dn_simdhash_string_ptr_t *hash, const char *key)
{
	return dn_simdhash_string_ptr_try_remove_raw(hash, make_key(key));
}
