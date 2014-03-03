/*
 * sgen-qsort.h: Fast inline sorting
 *
 * Copyright (C) 2014 Xamarin Inc
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
#ifndef __MONO_SGENQSORT_H__
#define __MONO_SGENQSORT_H__

#define DEF_QSORT_INLINE(NAME,ARRAY_TYPE,COMPARE_FUN)	\
static size_t partition_##NAME (ARRAY_TYPE base[], size_t nel) {	\
	size_t pivot_idx = nel >> 1;	\
	size_t s, i;	\
	ARRAY_TYPE pivot = base [pivot_idx];	\
	{ ARRAY_TYPE tmp = base [pivot_idx]; base [pivot_idx] = base [nel - 1]; base [nel - 1] = tmp; }	\
	s = 0;	\
	for (i = 0; i < nel - 1; ++i) {	\
		if (COMPARE_FUN (base [i], pivot) <= 0) {	\
			{ ARRAY_TYPE tmp = base [i]; base [i] = base [s]; base [s] = tmp; }	\
			++s;	\
		}	\
	}	\
	{ ARRAY_TYPE tmp = base [s]; base [s] = base [nel - 1]; base [nel - 1] = tmp; }	\
	return s;	\
}	\
static void rec_##NAME (ARRAY_TYPE base[], size_t nel) {	\
	size_t pivot_idx;	\
	if (nel <= 1)	\
		return; \
	pivot_idx = partition_##NAME (base, nel); \
	rec_##NAME (base, pivot_idx);	\
	if (pivot_idx < nel)	\
		rec_##NAME (&base[pivot_idx + 1], nel - pivot_idx - 1);	\
}	\
static void qsort_##NAME (ARRAY_TYPE base[], size_t nel) {	\
	rec_##NAME (base, nel);	\
}	\


#endif