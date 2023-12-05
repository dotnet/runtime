// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// This file contains icalls used in jitted interpreter traces and wrappers,
//  along with infrastructure to support code generration

// This file implements most of interpreter automatic PGO.
// Loading/saving the actual table is your responsibility via mono_interp_pgo_(load|save)_table

#ifndef __USE_ISOC99
#define __USE_ISOC99
#endif
#include "config.h"

// We start with a fixed-size table and then grow it by a given ratio when we run out of space
// Generally speaking size doubling is suboptimal so we use a 1.5x ratio
#define TABLE_MINIMUM_SIZE 4096
#define TABLE_GROWTH_FACTOR 150
#define INTERP_PGO_LOG_INTERVAL_MS 10

#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/bsearch.h>

#include "interp.h"
#include "interp-internals.h"
#include "transform.h"
#include "interp-intrins.h"
#include "tiering.h"

#include "interp-pgo.h"

#include <string.h>
#include <stdlib.h>
#include <math.h>

#include <mono/utils/options.h>
#include <mono/utils/atomic.h>


// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.
//
// Implementation was copied from https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
// with changes around strict-aliasing/unaligned reads

#define MM3_HASH_BYTE_SIZE 16   // MurMurHash3 is 128-bit, so we need 16 bytes to store it
#define MM3_HASH_BUFFER_SIZE 33 // MurMurHash3 is 128-bit, so we need 32 chars + 1 char to store null-terminator

inline static uint64_t ROTL64(uint64_t x, int8_t r)
{
    return (x << r) | (x >> (64 - r));
}

inline static uint64_t getblock64(const uint8_t* ptr)
{
    uint64_t val = 0;
    memcpy(&val, ptr, sizeof(uint64_t));
    return val;
}

inline static void setblock64(uint8_t* ptr, uint64_t val)
{
    memcpy(ptr, &val, sizeof(uint64_t));
}

// Finalization mix - force all bits of a hash block to avalanche
inline static uint64_t fmix64(uint64_t k)
{
    k ^= k >> 33;
    k *= 0xff51afd7ed558ccdLLU;
    k ^= k >> 33;
    k *= 0xc4ceb9fe1a85ec53LLU;
    k ^= k >> 33;
    return k;
}

static void MurmurHash3_128(const void* key, const size_t len, const uint32_t seed, uint8_t out[MM3_HASH_BYTE_SIZE])
{
    const uint8_t* data = (const uint8_t*)key;
    const size_t nblocks = len / MM3_HASH_BYTE_SIZE;
    uint64_t h1 = seed;
    uint64_t h2 = seed;
    const uint64_t c1 = 0x87c37b91114253d5LLU;
    const uint64_t c2 = 0x4cf5ad432745937fLLU;

    // body
    for (size_t i = 0; i < nblocks; i++)
    {
        uint64_t k1 = getblock64(data + (i * 2 + 0) * sizeof(uint64_t));
        uint64_t k2 = getblock64(data + (i * 2 + 1) * sizeof(uint64_t));

        k1 *= c1; k1 = ROTL64(k1, 31); k1 *= c2; h1 ^= k1;
        h1 = ROTL64(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729;
        k2 *= c2; k2 = ROTL64(k2, 33); k2 *= c1; h2 ^= k2;
        h2 = ROTL64(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5;
    }

    // tail
    const uint8_t* tail = data + nblocks * MM3_HASH_BYTE_SIZE;
    uint64_t k1 = 0;
    uint64_t k2 = 0;

    switch (len & 15)
    {
        case 15: k2 ^= (uint64_t)(tail[14]) << 48;
        case 14: k2 ^= (uint64_t)(tail[13]) << 40;
        case 13: k2 ^= (uint64_t)(tail[12]) << 32;
        case 12: k2 ^= (uint64_t)(tail[11]) << 24;
        case 11: k2 ^= (uint64_t)(tail[10]) << 16;
        case 10: k2 ^= (uint64_t)(tail[9]) << 8;
        case 9:  k2 ^= (uint64_t)(tail[8]) << 0;
            k2 *= c2; k2 = ROTL64(k2, 33); k2 *= c1; h2 ^= k2;

        case 8: k1 ^= (uint64_t)(tail[7]) << 56;
        case 7: k1 ^= (uint64_t)(tail[6]) << 48;
        case 6: k1 ^= (uint64_t)(tail[5]) << 40;
        case 5: k1 ^= (uint64_t)(tail[4]) << 32;
        case 4: k1 ^= (uint64_t)(tail[3]) << 24;
        case 3: k1 ^= (uint64_t)(tail[2]) << 16;
        case 2: k1 ^= (uint64_t)(tail[1]) << 8;
        case 1: k1 ^= (uint64_t)(tail[0]) << 0;
            k1 *= c1; k1 = ROTL64(k1, 31); k1 *= c2; h1 ^= k1;
            break;
    }

    // finalization
    h1 ^= len;
    h2 ^= len;
    h1 += h2;
    h2 += h1;
    h1 = fmix64(h1);
    h2 = fmix64(h2);
    h1 += h2;
    h2 += h1;

    setblock64((uint8_t*)(out), h1);
    setblock64((uint8_t*)(out) + sizeof(uint64_t), h2);
}

// end of murmurhash


static gint64 generate_started, generate_total_time;
static gint32 generate_depth;

static gint32
ms_from_100ns_ticks (gint64 ticks) {
	return (int)((ticks + 500) / 1000);
}

void
mono_interp_pgo_generate_start (void) {
	if (!mono_opt_interp_codegen_timing)
		return;

	if (mono_atomic_inc_i32 (&generate_depth) == 1)
		generate_started = mono_100ns_ticks ();
}

void
mono_interp_pgo_generate_end (void) {
	if (!mono_opt_interp_codegen_timing)
		return;
	if (mono_atomic_dec_i32 (&generate_depth) != 0)
		return;

	gint64 elapsed = mono_100ns_ticks () - generate_started,
		new_total = mono_atomic_add_i64 (&generate_total_time, elapsed);
	gint32 total_ms = ms_from_100ns_ticks (new_total),
		prior_total_ms = ms_from_100ns_ticks (new_total - elapsed);

	if ((total_ms / INTERP_PGO_LOG_INTERVAL_MS) != (prior_total_ms / INTERP_PGO_LOG_INTERVAL_MS))
		g_printf ("generate_code elapsed time: %dms\n", total_ms);
}


typedef struct {
	uint8_t *data;
	uint32_t size, capacity;
} interp_pgo_table;

// loaded_table is the table we loaded from persistent storage at startup (if any),
//  while building_table is the table we built during the current run. we store these
//  separately so that we don't have to maintain a sorted table (for bsearch) on an
//  ongoing basis.
static interp_pgo_table *loaded_table, *building_table;
// Loaded_table is immutable once loaded, so it has no mutex. Any access to building_table
//  needs to be performed while holding this mutex.
static mono_mutex_t building_table_lock;

static int
hash_comparer (const void *needle, const void *haystack)
{
	return memcmp (needle, haystack, MM3_HASH_BYTE_SIZE);
}

static gboolean
table_lookup (interp_pgo_table *table, uint8_t hash[MM3_HASH_BYTE_SIZE]) {
	// Early out if no table is loaded or the table is empty.
	if (!table || !table->size)
		return FALSE;

	g_assert (table->size <= table->capacity);

	void * result = mono_binary_search (hash, table->data, table->size / MM3_HASH_BYTE_SIZE, MM3_HASH_BYTE_SIZE, hash_comparer);
	return (result != NULL);
}

static void
table_add_locked (interp_pgo_table **table_variable, uint8_t hash[MM3_HASH_BYTE_SIZE]) {
	interp_pgo_table *table = *table_variable;
	// If we don't have a table yet, allocate one
	if (!table)
		*table_variable = table = g_malloc0 (sizeof (interp_pgo_table));

	const uint32_t required_size = table->size + MM3_HASH_BYTE_SIZE,
		required_capacity = MAX (required_size, TABLE_MINIMUM_SIZE);

	// If we're out of space or haven't yet allocated a buffer for this table, calculate
	//  an appropriate larger size and grow/allocate the buffer. We start at a fixed size,
	//  then after that grow the current size by a set ratio per step.
	while (required_capacity >= table->capacity) {
		uint32_t new_capacity = MAX (required_capacity, (table->capacity * TABLE_GROWTH_FACTOR / 100));
		if (table->data)
			table->data = g_realloc (table->data, new_capacity);
		else
			table->data = g_malloc0 (new_capacity);
		table->capacity = new_capacity;
	}

	// Copy the whole hash into the table at the end and update the size of the data
	memcpy (table->data + table->size, hash, MM3_HASH_BYTE_SIZE);
	table->size = required_size;
}

static void
table_sort_locked (interp_pgo_table *table) {
	mono_qsort (table->data, table->size / MM3_HASH_BYTE_SIZE, MM3_HASH_BYTE_SIZE, hash_comparer);
}

static void
compute_method_hash (MonoMethod *method, uint8_t outbuf[MM3_HASH_BYTE_SIZE]) {
	// method token + image guid
	size_t size = sizeof(uint32_t) + 16;
	uint32_t *inbuf = alloca (size);
	// method tokens are globally unique within a given assembly
	inbuf[0] = mono_method_get_token (method);
	// use the assembly guid as a unique id for the assembly
	MonoImage *image = m_class_get_image (mono_method_get_class (method));
	memcpy (inbuf + 1, mono_image_get_guid (image), 16);

	MurmurHash3_128 (inbuf, size, 0x43219876, (uint8_t *)outbuf);
}

gboolean
mono_interp_pgo_should_tier_method (MonoMethod *method) {
	// If we didn't load a table, don't bother hashing the method.
	if (!loaded_table)
		return FALSE;

	uint8_t hash[MM3_HASH_BYTE_SIZE];
	compute_method_hash (method, hash);

	if (table_lookup (loaded_table, hash)) {
		if (mono_opt_interp_pgo_logging) {
			char * name = mono_method_full_name (method, TRUE);
			g_print ("Tiering %s because it was in the interp_pgo table\n", name);
			g_free (name);
		}

		return TRUE;
	}

	return FALSE;
}

void
mono_interp_pgo_method_was_tiered (MonoMethod *method) {
	if (!mono_opt_interp_pgo_recording)
		return;

	// Wrappers are already tiered automatically, so we don't put them in the table
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return;

	uint8_t hash[MM3_HASH_BYTE_SIZE] = {0};
	compute_method_hash (method, hash);

	mono_os_mutex_lock (&building_table_lock);
	table_add_locked (&building_table, hash);
	mono_os_mutex_unlock (&building_table_lock);

	if (mono_opt_interp_pgo_logging) {
		char * name = mono_method_full_name (method, TRUE);
		g_print ("added %s to table\n", name);
		g_free (name);
	}
}

#if HOST_BROWSER

#include <emscripten.h>

// We disable this diagnostic because EMSCRIPTEN_KEEPALIVE makes it a false alarm, the keepalive
//  functions are being used externally. Having a bunch of prototypes is pointless since these
//  functions are not consumed by C anywhere else
#pragma clang diagnostic ignored "-Wmissing-prototypes"

EMSCRIPTEN_KEEPALIVE int
mono_interp_pgo_load_table (uint8_t * data, int data_size) {
	// Early-out if a table is already loaded.
	if (loaded_table)
		return 1;
	// If the data we were passed is too small then early out
	if (data_size < sizeof(uint32_t))
		return 3;

	interp_pgo_table *result = g_malloc0 (sizeof (interp_pgo_table));
	// The table storage format is [uint32 size] [data...]
	uint32_t size = *(uint32_t *)data;

	if (mono_opt_interp_pgo_logging)
		g_print ("Loading %d bytes of interp_pgo data (table size == %zu)\n", data_size, size);

	result->data = g_malloc0 (data_size);
	g_assert ((int64_t)size < (int64_t)data_size);
	result->size = size;
	result->capacity = data_size;
	memcpy (result->data, data + sizeof (uint32_t), result->size);

	// Atomically swap the new table in
	interp_pgo_table *old_table = mono_atomic_cas_ptr ((volatile gpointer*)&loaded_table, result, NULL);

	if (old_table) {
		// We lost a race with another thread that also loaded a table, so destroy ours and leave
		//  theirs in place.
		if (result->data)
			g_free (result->data);
		g_free (result);

		return 2;
	}

	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_interp_pgo_save_table (uint8_t * data, int data_size) {
	if (!building_table)
		return 0;

	mono_os_mutex_lock (&building_table_lock);
	interp_pgo_table *table = building_table;
	int expected_size = table->size + sizeof (uint32_t);
	if (data_size != expected_size) {
		mono_os_mutex_unlock (&building_table_lock);
		return expected_size;
	}
	table_sort_locked (table);
	// The table storage format is [uint32 size] [data...]
	memcpy (data, &table->size, sizeof (uint32_t));
	memcpy (data + sizeof (uint32_t), table->data, table->size);
	mono_os_mutex_unlock (&building_table_lock);
	return 0;
}

#endif // HOST_BROWSER
