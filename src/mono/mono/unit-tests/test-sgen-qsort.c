/*
 * test-sgen-qsort.c: Unit test for quicksort.
 *
 * Copyright (C) 2013 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#include <sgen/sgen-gc.h>
#include <sgen/sgen-qsort.h>

#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <assert.h>

static int
compare_ints (const void *pa, const void *pb)
{
	int a = *(const int*)pa;
	int b = *(const int*)pb;
	if (a < b)
		return -1;
	if (a == b)
		return 0;
	return 1;
}

typedef struct {
	int key;
	int val;
} teststruct_t;

static int
compare_teststructs (const void *pa, const void *pb)
{
	int a = ((const teststruct_t*)pa)->key;
	int b = ((const teststruct_t*)pb)->key;
	if (a < b)
		return -1;
	if (a == b)
		return 0;
	return 1;
}

static int
compare_teststructs2 (const void *pa, const void *pb)
{
	int a = (*((const teststruct_t**)pa))->key;
	int b = (*((const teststruct_t**)pb))->key;
	if (a < b)
		return -1;
	if (a == b)
		return 0;
	return 1;
}

DEF_QSORT_INLINE(test_struct, teststruct_t*, compare_teststructs)

static void
compare_sorts (void *base, size_t nel, size_t width, int (*compar) (const void*, const void*))
{
	size_t len = nel * width;
	void *b1 = malloc (len);
	void *b2 = malloc (len);

	memcpy (b1, base, len);
	memcpy (b2, base, len);

	qsort (b1, nel, width, compar);
	sgen_qsort (b2, nel, width, compar);

	/* We can't assert that qsort and sgen_qsort produce the same results
	 * because qsort is not guaranteed to be stable, so they will tend to differ
	 * in adjacent equal elements. Instead, we assert that the array is sorted
	 * according to the comparator.
	 */
	for (size_t i = 0; i < nel - 1; ++i)
		assert (compar ((char *)b2 + i * width, (char *)b2 + (i + 1) * width) <= 0);

	free (b1);
	free (b2);
}

static void
compare_sorts2 (void *base, size_t nel)
{
	size_t width = sizeof (teststruct_t*);
	size_t len = nel * width;
	void *b1 = malloc (len);
	void *b2 = malloc (len);

	memcpy (b1, base, len);
	memcpy (b2, base, len);

	qsort (b1, nel, sizeof (teststruct_t*), compare_teststructs2);
	qsort_test_struct ((teststruct_t **)b2, nel);

	for (size_t i = 0; i < nel - 1; ++i)
		assert (compare_teststructs2 ((char *)b2 + i * width, (char *)b2 + (i + 1) * width) <= 0);

	free (b1);
	free (b2);
}
int
main (void)
{
	int i;
	for (i = 1; i < 4000; ++i) {
		int a [i];
		int j;

		for (j = 0; j < i; ++j)
			a [j] = i - j - 1;
		compare_sorts (a, i, sizeof (int), compare_ints);
	}

	srandom (time (NULL));
	for (i = 0; i < 2000; ++i) {
		teststruct_t a [200];
		int j;
		for (j = 0; j < 200; ++j) {
			a [j].key = random ();
			a [j].val = random ();
		}

		compare_sorts (a, 200, sizeof (teststruct_t), compare_teststructs);
	}

	srandom (time (NULL));
	for (i = 0; i < 2000; ++i) {
		teststruct_t a [200];
		teststruct_t *b [200];
		int j;
		for (j = 0; j < 200; ++j) {
			a [j].key = random ();
			a [j].val = random ();
			b [j] = &a[j];
		}

		compare_sorts2 (b, 200);
	}
	return 0;
}
