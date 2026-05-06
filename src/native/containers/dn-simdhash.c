// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"
#include "dn-simdhash-utils.h"

static uint32_t spaced_primes[] = {
	1,
	3,
	7,
	11,
	17,
	23,
	29,
	37,
	47,
	59,
	71,
	89,
	107,
	131,
	163,
	197,
	239,
	293,
	353,
	431,
	521,
	631,
	761,
	919,
	1103,
	1327,
	1597,
	1931,
	2333,
	2801,
	3371,
	4049,
	4861,
	5839,
	7013,
	8419,
	10103,
	12143,
	14591,
	17519,
	21023,
	25229,
	30293,
	36353,
	43627,
	52361,
	62851,
	75431,
	90523,
	108631,
	130363,
	156437,
	187751,
	225307,
	270371,
	324449,
	389357,
	467237,
	560689,
	672827,
	807403,
	968897,
	1162687,
	1395263,
	1674319,
	2009191,
	2411033,
	2893249,
	3471899,
	4166287,
	4999559,
	5999471,
	7199369,
	7919311,
	8711267,
	9582409,
	10540661,
	11594729,
	12754219,
	14029643,
	15432619,
	16975891,
	18673483,
	20540831,
	22594919,
	24854419,
	27339863,
	30073853,
	33081239,
	36389369,
	40028333,
	44031179,
	48434303,
	53277769,
	58605563,
	64466147,
	70912783,
	78004061,
	85804471,
	94384919,
	103823417,
	114205771,
	125626351,
	138189017,
	152007971,
	167208817,
	183929719,
	202322693,
	222554977,
	244810487,
	269291537,
	296220709,
	325842779,
	358427071,
	394269781,
	433696759,
	477066449,
	524773133,
	577250461,
	634975519,
	698473099,
	768320467,
	845152513,
	929667799,
	1022634581,
	1124898043,
	1237387859,
	1361126671,
	1497239377,
	1646963321,
	1811659669,
	1992825643,
};

static uint32_t
next_prime_number (uint32_t x)
{
	int i, c = (sizeof(spaced_primes)/sizeof(spaced_primes[0]));
	for (i = 0; i < c; i++) {
		if (x <= spaced_primes [i])
			return spaced_primes [i];
	}
	return next_power_of_two(i);
}

static uint32_t
compute_adjusted_capacity (uint32_t requested_capacity)
{
	uint64_t _capacity = requested_capacity;
	_capacity *= DN_SIMDHASH_SIZING_PERCENTAGE;
	_capacity /= 100;
	if (_capacity < requested_capacity)
		_capacity = requested_capacity;
	dn_simdhash_assert(_capacity <= UINT32_MAX);
	return (uint32_t)_capacity;
}

dn_simdhash_t *
dn_simdhash_new_internal (dn_simdhash_meta_t *meta, dn_simdhash_vtable_t vtable, uint32_t capacity, dn_allocator_t *allocator)
{
	const size_t size = sizeof(dn_simdhash_t) + meta->data_size;
	dn_simdhash_t *result = (dn_simdhash_t *)dn_allocator_alloc(allocator, size);
	if (!result)
		return NULL;

	memset(result, 0, size);

	dn_simdhash_assert(meta);
	dn_simdhash_assert((meta->bucket_capacity > 1) && (meta->bucket_capacity <= DN_SIMDHASH_MAX_BUCKET_CAPACITY));
	dn_simdhash_assert(meta->key_size > 0);
	dn_simdhash_assert(meta->bucket_size_bytes >= (DN_SIMDHASH_VECTOR_WIDTH + (meta->bucket_capacity * meta->key_size)));
	result->meta = meta;
	result->vtable = vtable;
	result->buffers.allocator = allocator;

	uint8_t alloc_ok;
	dn_simdhash_ensure_capacity_internal(result, compute_adjusted_capacity(capacity), &alloc_ok);
	if (!alloc_ok) {
		dn_allocator_free(allocator, result);
		return NULL;
	}

	return result;
}

void
dn_simdhash_free (dn_simdhash_t *hash)
{
	dn_simdhash_assert(hash);
	if (hash->vtable.destroy_all)
		hash->vtable.destroy_all(hash);
	dn_simdhash_buffers_t buffers = hash->buffers;
	memset(hash, 0, sizeof(dn_simdhash_t));
	dn_simdhash_free_buffers(buffers);
	dn_allocator_free(buffers.allocator, (void *)hash);
}

void
dn_simdhash_free_buffers (dn_simdhash_buffers_t buffers)
{
	if (buffers.buckets)
		dn_allocator_free(buffers.allocator, (void *)(((uint8_t *)buffers.buckets) - buffers.buckets_bias));
	if (buffers.values)
		dn_allocator_free(buffers.allocator, buffers.values);
}

dn_simdhash_buffers_t
dn_simdhash_ensure_capacity_internal (dn_simdhash_t *hash, uint32_t capacity, uint8_t *ok)
{
	dn_simdhash_assert(hash);
	dn_simdhash_assert(ok);
	*ok = 0;

	size_t bucket_count = (capacity + hash->meta->bucket_capacity - 1) / hash->meta->bucket_capacity;
	// FIXME: Only apply this when capacity == 0?
	if (bucket_count < DN_SIMDHASH_MIN_BUCKET_COUNT)
		bucket_count = DN_SIMDHASH_MIN_BUCKET_COUNT;
	dn_simdhash_assert(bucket_count < UINT32_MAX);
#if DN_SIMDHASH_POWER_OF_TWO_BUCKETS
	// Bucket count must be a power of two (this enables more efficient hashcode -> bucket mapping)
	bucket_count = next_power_of_two((uint32_t)bucket_count);
#else
	bucket_count = next_prime_number((uint32_t)bucket_count);
#endif
	size_t value_count = bucket_count * hash->meta->bucket_capacity;
	dn_simdhash_assert(value_count <= UINT32_MAX);

	dn_simdhash_buffers_t result = { 0, };
	if (bucket_count <= hash->buffers.buckets_length) {
		dn_simdhash_assert(value_count <= hash->buffers.values_length);
		// We didn't grow but we also didn't fail, so we set ok to 1.
		*ok = 1;
		return result;
	}

	/*
	printf (
		"growing from %d bucket(s) to %d bucket(s) for requested capacity %d (actual capacity %d)\n",
		hash->buffers.buckets_length, bucket_count,
		capacity, value_count
	);
	*/

	// pad buckets allocation by the width of one vector so we can align it
	size_t buckets_size_bytes = (bucket_count * hash->meta->bucket_size_bytes) + DN_SIMDHASH_VECTOR_WIDTH,
		values_size_bytes = value_count * hash->meta->value_size;

	// If either of these allocations fail all we can do is return a default-initialized buffers_t, which will
	//  result in the caller not freeing anything and seeing an ok of 0, so they can tell the grow failed.
	// This should leave the hash in a well-formed state and if they try to grow later it might work.
	void *new_buckets = dn_allocator_alloc(hash->buffers.allocator, buckets_size_bytes);
	if (!new_buckets)
		return result;

	void *new_values = dn_allocator_alloc(hash->buffers.allocator, values_size_bytes);
	if (!new_values) {
		dn_allocator_free(hash->buffers.allocator, new_buckets);
		return result;
	}

	// Store old buffers so caller can rehash and then free them
	result = hash->buffers;
	size_t grow_at_count = value_count;
	grow_at_count *= 100;
	grow_at_count /= DN_SIMDHASH_SIZING_PERCENTAGE;
	hash->grow_at_count = (uint32_t)grow_at_count;
	hash->buffers.buckets_length = (uint32_t)bucket_count;
	hash->buffers.values_length = (uint32_t)value_count;

	dn_simdhash_assert(new_buckets);
	dn_simdhash_assert(new_values);

	hash->buffers.buckets = new_buckets;
	memset(hash->buffers.buckets, 0, buckets_size_bytes);

	// Calculate necessary bias for alignment
	hash->buffers.buckets_bias = (uint32_t)(DN_SIMDHASH_VECTOR_WIDTH - (((size_t)hash->buffers.buckets) % DN_SIMDHASH_VECTOR_WIDTH));
	// Apply bias
	hash->buffers.buckets = (void *)(((uint8_t *)hash->buffers.buckets) + hash->buffers.buckets_bias);

	// No need to go out of our way to align values
	hash->buffers.values = new_values;
	// Skip this for performance; memset is especially slow in wasm
	// memset(hash->buffers.values, 0, values_size_bytes);

	*ok = 1;

	return result;
}

void
dn_simdhash_clear (dn_simdhash_t *hash)
{
	dn_simdhash_assert(hash);
	if (hash->vtable.destroy_all)
		hash->vtable.destroy_all(hash);
	hash->count = 0;
	// TODO: Implement a fast clear algorithm that scans buckets and only clears ones w/nonzero count
	memset(hash->buffers.buckets, 0, hash->buffers.buckets_length * hash->meta->bucket_size_bytes);
	// Skip this for performance; memset is especially slow in wasm
	// memset(hash->buffers.values, 0, hash->buffers.values_length * hash->meta->value_size);
}

uint32_t
dn_simdhash_capacity (dn_simdhash_t *hash)
{
	dn_simdhash_assert(hash);
	return hash->buffers.buckets_length * hash->meta->bucket_capacity;
}

uint32_t
dn_simdhash_count (dn_simdhash_t *hash)
{
	dn_simdhash_assert(hash);
	return hash->count;
}

uint32_t
dn_simdhash_overflow_count (dn_simdhash_t *hash)
{
	dn_simdhash_assert(hash);
	uint32_t result = 0;
	for (uint32_t bucket_index = 0; bucket_index < hash->buffers.buckets_length; bucket_index++) {
		uint8_t *suffixes = ((uint8_t *)hash->buffers.buckets) + (bucket_index * hash->meta->bucket_size_bytes);
		uint8_t cascade_count = suffixes[DN_SIMDHASH_CASCADED_SLOT];
		result += cascade_count;
	}
	return result;
}

uint8_t
dn_simdhash_ensure_capacity (dn_simdhash_t *hash, uint32_t capacity)
{
	dn_simdhash_assert(hash);
	capacity = compute_adjusted_capacity(capacity);
	uint8_t result;
	dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(hash, capacity, &result);
	if (old_buffers.buckets) {
		hash->vtable.rehash(hash, old_buffers);
		dn_simdhash_free_buffers(old_buffers);
	}
	return result;
}
