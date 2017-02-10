/**
 * \file
 * Quicksort.
 *
 * Copyright (C) 2013 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"

#define ELEM(i) \
	(((unsigned char*)array) + ((i) * element_size))
#define SET(i,j) \
	do memmove ((i), (j), element_size); while (0)
#define SWAP(i,j) \
	do { \
		size_t __i = (i), __j = (j); \
		if (__i != __j) { \
			SET (swap_tmp, ELEM (__i)); \
			SET (ELEM (__i), ELEM (__j)); \
			SET (ELEM (__j), swap_tmp); \
		} \
	} while (0)

static void
sgen_qsort_rec (
	void *const array,
	const size_t element_size,
	int (*compare) (const void *, const void *),
	ssize_t begin,
	ssize_t end,
	unsigned char *const pivot_tmp,
	unsigned char *const swap_tmp)
{
	ssize_t left, right, mid, pivot;
	while (begin < end) {
		left = begin;
		right = end;
		mid = begin + (end - begin) / 2;

		/* Choose median of 3 as pivot and pre-sort to avoid O(n^2) case.
		 *
		 * L --o--o----->
		 *     |  |
		 * M --o--|--o-->
		 *        |  |
		 * R -----o--o-->
		 */
		if (compare (ELEM (mid), ELEM (left)) < 0)
			SWAP (mid, left);
		if (compare (ELEM (right), ELEM (left)) < 0)
			SWAP (right, left);
		if (compare (ELEM (right), ELEM (mid)) < 0)
			SWAP (right, mid);
		pivot = mid;
		SET (pivot_tmp, ELEM (pivot));

		/* Partition. */
		for (;;) {
			while (left <= right && compare (ELEM (left), pivot_tmp) <= 0)
				++left;
			while (left <= right && compare (ELEM (right), pivot_tmp) > 0)
				--right;
			if (left > right)
				break;
			SWAP (left, right);
			if (pivot == right)
				pivot = left;
			++left;
			--right;
		}
		SET (ELEM (pivot), ELEM (right));
		SET (ELEM (right), pivot_tmp);
		--right;

		/* Recursively sort shorter partition, loop on longer partition. */
		if (right - begin < end - left) {
			sgen_qsort_rec (
				array,
				element_size,
				compare,
				begin,
				right,
				pivot_tmp,
				swap_tmp);
			begin = left;
		} else {
			sgen_qsort_rec (
				array,
				element_size,
				compare,
				left,
				end,
				pivot_tmp,
				swap_tmp);
			end = right;
		}
	}
}

void sgen_qsort (
	void *const array,
	const size_t count,
	const size_t element_size,
	int (*compare) (const void *, const void *))
{
#ifndef _MSC_VER
	unsigned char pivot_tmp [element_size];
	unsigned char swap_tmp [element_size];
#else
	unsigned char *pivot_tmp = (unsigned char *)alloca (element_size);
	unsigned char *swap_tmp = (unsigned char *)alloca (element_size);
#endif
	sgen_qsort_rec (
		array,
		element_size,
		compare,
		0,
		(ssize_t)count - 1,
		pivot_tmp,
		swap_tmp);
}

#endif
