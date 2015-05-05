/*
 * test-sgen-qsort.c: Unit test for quicksort.
 *
 * Copyright (C) 2013 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
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

	assert (!memcmp (b1, b2, len));

	free (b1);
	free (b2);
}

static void
compare_sorts2 (void *base, size_t nel)
{
	size_t len = nel * sizeof (teststruct_t*);
	void *b1 = malloc (len);
	void *b2 = malloc (len);

	memcpy (b1, base, len);
	memcpy (b2, base, len);

	qsort (b1, nel, sizeof (teststruct_t*), compare_teststructs2);
	qsort_test_struct (b2, nel);

	assert (!memcmp (b1, b2, len));

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
