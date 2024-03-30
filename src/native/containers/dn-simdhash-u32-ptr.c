// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.

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

#define DN_SIMDHASH_T dn_simdhash_u32_ptr
#define DN_SIMDHASH_KEY_T uint32_t
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER fmix32
#define DN_SIMDHASH_KEY_EQUALS(lhs, rhs) (lhs == rhs)

#include "dn-simdhash-specialization.h"
