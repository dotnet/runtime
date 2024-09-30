// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <dn-config.h>
#include "dn-simdhash.h"

#include "dn-simdhash-utils.h"

typedef struct dn_ptrpair_t {
	void *first;
	void *second;
} dn_ptrpair_t;

static inline uint32_t
dn_ptrpair_t_hash (dn_ptrpair_t key)
{
	return (MurmurHash3_32_ptr(key.first, 0) ^ MurmurHash3_32_ptr(key.second, 1));
}

static inline uint8_t
dn_ptrpair_t_equals (dn_ptrpair_t lhs, dn_ptrpair_t rhs)
{
	return (lhs.first == rhs.first) && (lhs.second == rhs.second);
}

#define DN_SIMDHASH_T dn_simdhash_ptrpair_ptr
#define DN_SIMDHASH_KEY_T dn_ptrpair_t
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER(hash, key) dn_ptrpair_t_hash(key)
#define DN_SIMDHASH_KEY_EQUALS(hash, lhs, rhs) dn_ptrpair_t_equals(lhs, rhs)
#if SIZEOF_VOID_P == 8
// 192 bytes holds 12 16-byte blocks, so 11 keys and one suffix table
#define DN_SIMDHASH_BUCKET_CAPACITY 11
#else
// 128 bytes holds 16 8-byte blocks, so 14 keys and one suffix table
#define DN_SIMDHASH_BUCKET_CAPACITY 14
#endif

#include "dn-simdhash-specialization.h"
