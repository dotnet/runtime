// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <string.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>

#ifdef _MSC_VER
#include <windows.h>
#define MTICKS_PER_SEC (10 * 1000 * 1000)
#else
#include <sys/time.h>
#endif

#include "dn-vector.h"
#include "dn-simdhash.h"
#include "dn-simdhash-utils.h"

typedef struct {
	int i;
	float f;
} instance_data_t;

void
dn_simdhash_assert_fail (const char *file, int line, const char *condition) {
	printf("simdhash assertion failed at %s:%i:\n%s\n", file, line, condition);
	fflush(stdout);
}

static DN_FORCEINLINE(uint8_t)
key_comparer (instance_data_t data, size_t lhs, size_t rhs) {
	return ((data.f == 4.20f) || (lhs == rhs));
}

#define DN_SIMDHASH_T dn_simdhash_size_t_size_t
#define DN_SIMDHASH_KEY_T size_t
#define DN_SIMDHASH_VALUE_T size_t
#define DN_SIMDHASH_KEY_HASHER(data, key) (uint32_t)(key & 0xFFFFFFFFu)
#define DN_SIMDHASH_KEY_EQUALS key_comparer
#define DN_SIMDHASH_INSTANCE_DATA_T instance_data_t
#define DN_SIMDHASH_ON_REMOVE(data, key, value)  // printf("remove [%zd, %zd], f==%f\n", key, value, data.f)
#define DN_SIMDHASH_ON_REPLACE(data, old_key, new_key, old_value, new_value)  // printf("replace [%zd, %zd] with [%zd, %zd] i==%i\n", key, old_value, key, new_value, data.i)

#include "dn-simdhash-specialization.h"

uint32_t count_cascaded_buckets (dn_simdhash_size_t_size_t_t *hash) {
	uint32_t result = 0;
	dn_simdhash_buffers_t buffers = hash->buffers;
	BEGIN_SCAN_BUCKETS(0, bucket_index, bucket_address)
		result += dn_simdhash_bucket_cascaded_count(bucket_address->suffixes);
	END_SCAN_BUCKETS(0, bucket_index, bucket_address)
	return result;
}

uint8_t tassert (int b, const char *msg) {
	if (b)
		return b;
	printf("%s\n", msg);
	return 0;
}

uint8_t tassert1 (int b, size_t v, const char *msg) {
	if (b)
		return b;
	printf("%s (%zd)\n", msg, v);
	return 0;
}

uint8_t tasserteq (size_t actual, size_t expected, const char *msg) {
	if (actual == expected)
		return 1;
	printf("%s: expected %zd, got %zd\n", msg, expected, actual);
	return 0;
}

void foreach_callback (size_t key, size_t value, void * user_data) {
	// printf("[%zd, %zd]\n", key, value);
	(*(uint32_t *)user_data)++;
}

int64_t get_100ns_ticks () {
#ifdef _MSC_VER
	static LARGE_INTEGER freq;
	static UINT64 start_time;
	UINT64 cur_time;
	LARGE_INTEGER value;

	if (!freq.QuadPart) {
		QueryPerformanceFrequency(&freq);
		QueryPerformanceCounter(&value);
		start_time = value.QuadPart;
	}
	QueryPerformanceCounter(&value);
	cur_time = value.QuadPart;
	return (int64_t)((cur_time - start_time) * (double)MTICKS_PER_SEC / freq.QuadPart);
#else
	struct timeval tv;
	gettimeofday(&tv, NULL);
	return ((int64_t)tv.tv_sec * 1000000 + tv.tv_usec) * 10;
#endif
}

int main () {
	// NOTE: High values of C will cause this test to never complete if libc
	//  rand() is not high quality enough, i.e. MSVC 2022 on x64
	const int c = 32000;
	dn_simdhash_size_t_size_t_t *test = dn_simdhash_size_t_size_t_new(0, NULL);
	dn_simdhash_instance_data(instance_data_t, test).f = 3.14f;
	dn_simdhash_instance_data(instance_data_t, test).i = 42;

	printf("hash(test)=%u\n", MurmurHash3_32_ptr(test, 0));

	dn_vector_t *keys = dn_vector_alloc(sizeof(DN_SIMDHASH_KEY_T)),
		*values = dn_vector_alloc(sizeof(DN_SIMDHASH_VALUE_T));
	// Ensure consistency between runs
	srand(1);

	for (int i = 0; i < c; i++) {
		DN_SIMDHASH_VALUE_T value = (i * 2) + 1;
		DN_SIMDHASH_KEY_T key;

retry: {
		key = rand();
		uint8_t ok = dn_simdhash_size_t_size_t_try_add(test, key, value);
		if (!ok)
			goto retry;
}

		dn_vector_push_back(keys, key);
		dn_vector_push_back(values, value);
	}

	int64_t started = get_100ns_ticks();
	for (int iter = 0; iter < 5; iter++) {
		if (!tasserteq(dn_simdhash_count(test), c, "count did not match"))
			return 1;

		printf("Calling foreach:\n");
		uint32_t foreach_count = 0;
		dn_simdhash_size_t_size_t_foreach(test, foreach_callback, &foreach_count);
		printf("Foreach iterated %u time(s)\n", foreach_count);
		printf("Count: %u, Capacity: %u, Cascaded item count: %u\n", dn_simdhash_count(test), dn_simdhash_capacity(test), count_cascaded_buckets(test));

		for (int i = 0; i < c; i++) {
			DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
			DN_SIMDHASH_VALUE_T value, expected_value = *dn_vector_index_t(values, DN_SIMDHASH_VALUE_T, i);

			uint8_t ok = dn_simdhash_size_t_size_t_try_get_value(test, key, &value);
			if (tassert1(ok, key, "did not find key"))
				tasserteq(value, expected_value, "value did not match");
		}

		// NOTE: Adding duplicates could grow the table if we're unlucky, since the add operation
		//  eagerly grows before doing a table scan if we're at the grow threshold.
		for (int i = 0; i < c; i++) {
			DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
			DN_SIMDHASH_VALUE_T value = *dn_vector_index_t(values, DN_SIMDHASH_VALUE_T, i);

			uint8_t ok = dn_simdhash_size_t_size_t_try_add(test, key, value);
			tassert1(!ok, key, "added duplicate key successfully");
		}

		printf("After adding dupes: Count: %u, Capacity: %u, Cascaded item count: %u\n", dn_simdhash_count(test), dn_simdhash_capacity(test), count_cascaded_buckets(test));
		uint32_t final_capacity = dn_simdhash_capacity(test);

		for (int i = 0; i < c; i++) {
			DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
			uint8_t ok = dn_simdhash_size_t_size_t_try_remove(test, key);
			tassert1(ok, key, "could not remove key");

			DN_SIMDHASH_VALUE_T value;
			ok = dn_simdhash_size_t_size_t_try_get_value(test, key, &value);
			tassert1(!ok, key, "found key after removal");
		}

		if (!tasserteq(dn_simdhash_count(test), 0, "was not empty"))
			return 1;
		if (!tasserteq(dn_simdhash_capacity(test), final_capacity, "capacity changed by emptying"))
			return 1;

		printf ("Calling foreach after emptying:\n");
		foreach_count = 0;
		dn_simdhash_size_t_size_t_foreach(test, foreach_callback, &foreach_count);
		printf("Foreach iterated %u time(s)\n", foreach_count);
		printf("Count: %u, Capacity: %u, Cascaded item count: %u\n", dn_simdhash_count(test), dn_simdhash_capacity(test), count_cascaded_buckets(test));

		for (int i = 0; i < c; i++) {
			DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
			DN_SIMDHASH_VALUE_T value;
			uint8_t ok = dn_simdhash_size_t_size_t_try_get_value(test, key, &value);
			tassert1(!ok, key, "found key after removal");
		}

		for (int i = 0; i < c; i++) {
			DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
			DN_SIMDHASH_VALUE_T value = *dn_vector_index_t(values, DN_SIMDHASH_VALUE_T, i);

			uint8_t ok = dn_simdhash_size_t_size_t_try_add(test, key, value);
			tassert1(ok, key, "could not re-insert key after emptying");
		}

		if (!tasserteq(dn_simdhash_capacity(test), final_capacity, "expected capacity not to change after refilling"))
			return 1;

		for (int i = 0; i < c; i++) {
			DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
			DN_SIMDHASH_VALUE_T value, expected_value = *dn_vector_index_t(values, DN_SIMDHASH_VALUE_T, i);

			uint8_t ok = dn_simdhash_size_t_size_t_try_get_value(test, key, &value);
			if (tassert1(ok, key, "did not find key after refilling"))
				tasserteq(value, expected_value, "value did not match after refilling");
		}

		printf("Calling foreach after refilling:\n");
		foreach_count = 0;
		dn_simdhash_size_t_size_t_foreach(test, foreach_callback, &foreach_count);
		printf("Foreach iterated %u time(s)\n", foreach_count);
		printf("Count: %u, Capacity: %u, Cascaded item count: %u\n", dn_simdhash_count(test), dn_simdhash_capacity(test), count_cascaded_buckets(test));
	}

	int64_t ended = get_100ns_ticks();

	printf("done. elapsed ticks: %lld\n", (ended - started));

	return 0;
	/*
	var test = new SimdDictionary<long, long>();
	var rng = new Random(1234);
	int c = 4096, d = 4096 * 5;
	var keys = new List<long>();
	for (int i = 0; i < c; i++)
		keys.Add(rng.NextInt64());
	for (int i = 0; i < c; i++)
		test.Add(keys[i], i * 2 + 1);

	for (int j = 0; j < d; j++)
		for (int i = 0; i < c; i++)
			if (!test.TryGetValue(keys[i], out _))
				throw new Exception();

	var keyList = test.Keys.ToArray();
	var valueList = test.Values.ToArray();

	var copy = new SimdDictionary<long, long>(test);
	for (int i = 0; i < c; i++)
		if (!copy.TryGetValue(keys[i], out _))
			throw new Exception();

	for (int i = 0; i < c; i++)
		if (!test.Remove(keys[i]))
			throw new Exception();

	for (int i = 0; i < c; i++)
		if (test.TryGetValue(keys[i], out _))
			throw new Exception();

	if (test.Count != 0)
		throw new Exception();
	*/
}
