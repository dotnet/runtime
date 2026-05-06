/**
 * \file
 * Fast inline sorting
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGENQSORT_H__
#define __MONO_SGENQSORT_H__

/* Copied from non-inline implementation in sgen-qsort.c */
#define DEF_QSORT_INLINE(name, type, compare) \
static inline void \
qsort_swap_##name (type array[], const ssize_t i, const ssize_t j, type *const swap_tmp) \
{ \
	*swap_tmp = array [i]; \
	array [i] = array [j]; \
	array [j] = *swap_tmp; \
} \
\
static void \
qsort_rec_##name ( \
	type array[], \
	ssize_t begin, \
	ssize_t end, \
	type *const pivot_tmp, \
	type *const swap_tmp) \
{ \
	ssize_t left, right, middle, pivot; \
	while (begin < end) { \
		left = begin; \
		right = end; \
		middle = begin + (end - begin) / 2; \
		if (compare (array [middle], array [left]) < 0) \
			qsort_swap_##name (array, middle, left, swap_tmp); \
		if (compare (array [right], array [left]) < 0) \
			qsort_swap_##name (array, right, left, swap_tmp); \
		if (compare (array [right], array [middle]) < 0) \
			qsort_swap_##name (array, right, middle, swap_tmp); \
		pivot = middle; \
		*pivot_tmp = array [pivot]; \
		for (;;) { \
			while (left <= right && compare (array [left], *pivot_tmp) <= 0) \
				++left; \
			while (left <= right && compare (array [right], *pivot_tmp) > 0) \
				--right; \
			if (left > right) \
				break; \
			qsort_swap_##name (array, left, right, swap_tmp); \
			if (pivot == right) \
				pivot = left; \
			++left; \
			--right; \
		} \
		array [pivot] = array [right]; \
		array [right] = *pivot_tmp; \
		--right; \
		if (right - begin < end - left) { \
			qsort_rec_##name (array, begin, right, pivot_tmp, swap_tmp); \
			begin = left; \
		} else { \
			qsort_rec_##name (array, left, end, pivot_tmp, swap_tmp); \
			end = right; \
		} \
	} \
}	\
\
static inline void \
qsort_##name (type array[], size_t count) \
{ \
	type pivot_tmp; \
	type swap_tmp; \
	qsort_rec_##name (array, 0, (ssize_t)count - 1, &pivot_tmp, &swap_tmp); \
}

#endif
