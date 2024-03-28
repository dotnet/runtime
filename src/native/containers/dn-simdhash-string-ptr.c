// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"

// FIXME: for some reason g_str_hash and g_str_equal don't link properly if we use them here...
static int32_t
dn_simdhash_str_equal (const char * v1, const char * v2)
{
	return v1 == v2 || strcmp (v1, v2) == 0;
}

// FIXME: Use a hash function that's fast and avalanches
static uint32_t
dn_simdhash_str_hash (const char * v1)
{
	uint32_t hash = 0;
	unsigned char *p = (unsigned char *) v1;

	while (*p++)
		hash = (hash << 5) - (hash + *p);

	return hash;
}

#define DN_SIMDHASH_T dn_simdhash_string_ptr
#define DN_SIMDHASH_KEY_T const char *
#define DN_SIMDHASH_VALUE_T void *
#define DN_SIMDHASH_KEY_HASHER dn_simdhash_str_hash
#define DN_SIMDHASH_KEY_COMPARER dn_simdhash_str_equal
#define DN_SIMDHASH_KEY_IS_POINTER 1
#define DN_SIMDHASH_VALUE_IS_POINTER 1

#include "dn-simdhash-specialization.h"
