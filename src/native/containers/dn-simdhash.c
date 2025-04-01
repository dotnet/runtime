// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dn-simdhash.h"
#include "dn-simdhash-utils.h"

static uint32_t
compute_adjusted_capacity (uint32_t requested_capacity)
{
	uint64_t _capacity = requested_capacity;
	_capacity *= DN_SIMDHASH_SIZING_PERCENTAGE;
	_capacity /= 100;
	dn_simdhash_assert(_capacity <= UINT32_MAX);
	return (uint32_t)_capacity;
}

dn_simdhash_t *
dn_simdhash_new_internal (dn_simdhash_meta_t *meta, dn_simdhash_vtable_t vtable, uint32_t capacity, dn_allocator_t *allocator)
{
	const size_t size = sizeof(dn_simdhash_t) + meta->data_size;
	dn_simdhash_t *result = (dn_simdhash_t *)dn_allocator_alloc(allocator, size);
	memset(result, 0, size);

	dn_simdhash_assert(meta);
	dn_simdhash_assert((meta->bucket_capacity > 1) && (meta->bucket_capacity <= DN_SIMDHASH_MAX_BUCKET_CAPACITY));
	dn_simdhash_assert(meta->key_size > 0);
	dn_simdhash_assert(meta->bucket_size_bytes >= (DN_SIMDHASH_VECTOR_WIDTH + (meta->bucket_capacity * meta->key_size)));
	result->meta = meta;
	result->vtable = vtable;
	result->buffers.allocator = allocator;

	dn_simdhash_ensure_capacity_internal(result, compute_adjusted_capacity(capacity));

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
dn_simdhash_ensure_capacity_internal (dn_simdhash_t *hash, uint32_t capacity)
{
	dn_simdhash_assert(hash);
	size_t bucket_count = (capacity + hash->meta->bucket_capacity - 1) / hash->meta->bucket_capacity;
	// FIXME: Only apply this when capacity == 0?
	if (bucket_count < DN_SIMDHASH_MIN_BUCKET_COUNT)
		bucket_count = DN_SIMDHASH_MIN_BUCKET_COUNT;
	dn_simdhash_assert(bucket_count < UINT32_MAX);
	// Bucket count must be a power of two (this enables more efficient hashcode -> bucket mapping)
	bucket_count = next_power_of_two((uint32_t)bucket_count);
	size_t value_count = bucket_count * hash->meta->bucket_capacity;
	dn_simdhash_assert(value_count <= UINT32_MAX);

	dn_simdhash_buffers_t result = { 0, };
	if (bucket_count <= hash->buffers.buckets_length) {
		dn_simdhash_assert(value_count <= hash->buffers.values_length);
		return result;
	}

	/*
	printf (
		"growing from %d bucket(s) to %d bucket(s) for requested capacity %d (actual capacity %d)\n",
		hash->buffers.buckets_length, bucket_count,
		capacity, value_count
	);
	*/
	// Store old buffers so caller can rehash and then free them
	result = hash->buffers;

	size_t grow_at_count = value_count;
	grow_at_count *= 100;
	grow_at_count /= DN_SIMDHASH_SIZING_PERCENTAGE;
	hash->grow_at_count = (uint32_t)grow_at_count;
	hash->buffers.buckets_length = (uint32_t)bucket_count;
	hash->buffers.values_length = (uint32_t)value_count;

	// pad buckets allocation by the width of one vector so we can align it
	size_t buckets_size_bytes = (bucket_count * hash->meta->bucket_size_bytes) + DN_SIMDHASH_VECTOR_WIDTH,
		values_size_bytes = value_count * hash->meta->value_size;

	hash->buffers.buckets = dn_allocator_alloc(hash->buffers.allocator, buckets_size_bytes);
	memset(hash->buffers.buckets, 0, buckets_size_bytes);

	// Calculate necessary bias for alignment
	hash->buffers.buckets_bias = (uint32_t)(DN_SIMDHASH_VECTOR_WIDTH - (((size_t)hash->buffers.buckets) % DN_SIMDHASH_VECTOR_WIDTH));
	// Apply bias
	hash->buffers.buckets = (void *)(((uint8_t *)hash->buffers.buckets) + hash->buffers.buckets_bias);

	// No need to go out of our way to align values
	hash->buffers.values = dn_allocator_alloc(hash->buffers.allocator, values_size_bytes);
	// Skip this for performance; memset is especially slow in wasm
	// memset(hash->buffers.values, 0, values_size_bytes);

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
	assert(hash);
	uint32_t result = 0;
	for (uint32_t bucket_index = 0; bucket_index < hash->buffers.buckets_length; bucket_index++) {
		uint8_t *suffixes = ((uint8_t *)hash->buffers.buckets) + (bucket_index * hash->meta->bucket_size_bytes);
		uint8_t cascade_count = suffixes[DN_SIMDHASH_CASCADED_SLOT];
		result += cascade_count;
	}
	return result;
}

void
dn_simdhash_ensure_capacity (dn_simdhash_t *hash, uint32_t capacity)
{
	dn_simdhash_assert(hash);
	capacity = compute_adjusted_capacity(capacity);
	dn_simdhash_buffers_t old_buffers = dn_simdhash_ensure_capacity_internal(hash, capacity);
	if (old_buffers.buckets) {
		hash->vtable.rehash(hash, old_buffers);
		dn_simdhash_free_buffers(old_buffers);
	}
}
