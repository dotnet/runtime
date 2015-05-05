/*
 * sgen-qsort.c: Quicksort.
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

#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"

#define ELEM(i)		(((unsigned char*)base) + ((i) * width))
#define SWAP(i,j)	do {					\
		size_t __i = (i), __j = (j);			\
		if (__i != __j) {				\
			memcpy (swap_tmp, ELEM (__i), width);	\
			memcpy (ELEM (__i), ELEM (__j), width);	\
			memcpy (ELEM (__j), swap_tmp, width);	\
		}						\
	} while (0)

static size_t
partition (void *base, size_t nel, size_t width, int (*compar) (const void*, const void*), unsigned char *pivot_tmp, unsigned char *swap_tmp)
{
	size_t pivot_idx = nel >> 1;
	size_t s, i;

	memcpy (pivot_tmp, ELEM (pivot_idx), width);
	SWAP (pivot_idx, nel - 1);
	s = 0;
	for (i = 0; i < nel - 1; ++i) {
		if (compar (ELEM (i), pivot_tmp) <= 0) {
			SWAP (i, s);
			++s;
		}
	}
	SWAP (s, nel - 1);
	return s;
}

static void
qsort_rec (void *base, size_t nel, size_t width, int (*compar) (const void*, const void*), unsigned char *pivot_tmp, unsigned char *swap_tmp)
{
	size_t pivot_idx;

	if (nel <= 1)
		return;

	pivot_idx = partition (base, nel, width, compar, pivot_tmp, swap_tmp);
	qsort_rec (base, pivot_idx, width, compar, pivot_tmp, swap_tmp);
	if (pivot_idx < nel)
		qsort_rec (ELEM (pivot_idx + 1), nel - pivot_idx - 1, width, compar, pivot_tmp, swap_tmp);
}

void
sgen_qsort (void *base, size_t nel, size_t width, int (*compar) (const void*, const void*))
{
#ifndef _MSC_VER
	unsigned char pivot_tmp [width];
	unsigned char swap_tmp [width];
#else
	unsigned char* pivot_tmp = (unsigned char*) alloca(width);
	unsigned char* swap_tmp = (unsigned char*) alloca(width);
#endif

	qsort_rec (base, nel, width, compar, pivot_tmp, swap_tmp);
}

#endif
