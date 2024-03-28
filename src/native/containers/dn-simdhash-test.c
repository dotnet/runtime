// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <string.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>

#include "dn-vector.h"
#include "dn-simdhash.h"

#define DN_SIMDHASH_T dn_simdhash_size_t_size_t
#define DN_SIMDHASH_KEY_T size_t
#define DN_SIMDHASH_VALUE_T size_t
#define DN_SIMDHASH_KEY_HASHER(key) (uint32_t)(key & 0xFFFFFFFFu)
#define DN_SIMDHASH_KEY_COMPARER(lhs, rhs) (lhs != rhs)
#define DN_SIMDHASH_KEY_IS_POINTER 1
#define DN_SIMDHASH_VALUE_IS_POINTER 1

#include "dn-simdhash-specialization.h"

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
	printf("%s: expected %zd, got %zd\n", msg, actual, expected);
	return 0;
}

void foreach_callback (void * key, void * value, void * user_data) {
	printf("[%zd, %zd]\n", (size_t)key, (size_t)value);
}

int main () {
	const int c = 4096;
	dn_simdhash_t *test = dn_simdhash_size_t_size_t_new(0, NULL);
	dn_vector_t *keys = dn_vector_alloc(sizeof(DN_SIMDHASH_KEY_T)),
		*values = dn_vector_alloc(sizeof(DN_SIMDHASH_VALUE_T));

	for (int i = 0; i < c; i++) {
		DN_SIMDHASH_KEY_T key = rand();
		dn_vector_push_back(keys, key);
		DN_SIMDHASH_VALUE_T value = (i * 2) + 1;
		dn_vector_push_back(values, value);

		uint8_t ok = dn_simdhash_size_t_size_t_try_add(test, key, value);
		tassert(ok, "Insert failed");
	}

	if (!tasserteq(dn_simdhash_count(test), c, "count did not match"))
		return 1;

	dn_simdhash_foreach(test, foreach_callback, NULL);

	for (int i = 0; i < c; i++) {
		DN_SIMDHASH_KEY_T key = *dn_vector_index_t(keys, DN_SIMDHASH_KEY_T, i);
		DN_SIMDHASH_VALUE_T value, expected_value = *dn_vector_index_t(values, DN_SIMDHASH_VALUE_T, i);

		uint8_t ok = dn_simdhash_size_t_size_t_try_get_value(test, key, &value);
		if (tassert1(ok, key, "did not find key"))
			tasserteq(value, expected_value, "value did not match");
	}

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
