/**
 * \file
 * GC descriptors describe object layout.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif
#ifdef HAVE_SEMAPHORE_H
#include <semaphore.h>
#endif
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <assert.h>
#ifdef __MACH__
#undef _XOPEN_SOURCE
#endif
#ifdef __MACH__
#define _XOPEN_SOURCE
#endif

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/gc-internal-agnostic.h"
#include "mono/sgen/sgen-array-list.h"

#define MAX_USER_DESCRIPTORS 16

#define MAKE_ROOT_DESC(type,val) ((type) | ((val) << ROOT_DESC_TYPE_SHIFT))

static SgenArrayList complex_descriptors = SGEN_ARRAY_LIST_INIT (NULL, NULL, NULL, INTERNAL_MEM_COMPLEX_DESCRIPTORS);
static SgenUserRootMarkFunc user_descriptors [MAX_USER_DESCRIPTORS];
static int user_descriptors_next = 0;
static SgenDescriptor all_ref_root_descrs [32];

#ifdef HEAVY_STATISTICS
static guint64 stat_scanned_count_per_descriptor [DESC_TYPE_MAX];
static guint64 stat_copied_count_per_descriptor [DESC_TYPE_MAX];
#endif

static int
alloc_complex_descriptor (gsize *bitmap, int numbits)
{
	int nwords, res, i;
	volatile gpointer *slot;
	gsize *descriptor;

	SGEN_ASSERT (0, sizeof (gsize) == sizeof (mword), "We expect gsize and mword to have same size");

	numbits = ALIGN_TO (numbits, GC_BITS_PER_WORD);
	nwords = numbits / GC_BITS_PER_WORD + 1;

	sgen_gc_lock ();
	/* linear search, so we don't have duplicates with domain load/unload
	 * this should not be performance critical or we'd have bigger issues
	 * (the number and size of complex descriptors should be small).
	 */
	SGEN_ARRAY_LIST_FOREACH_SLOT (&complex_descriptors, slot) {
		gsize first_word = *(gsize*)slot;
		if (first_word == 0) {
			/* Unused slots should be 0 so we simply skip them */
			continue;
		} else if (first_word == nwords) {
			int j, found = TRUE;
			for (j = 0; j < nwords - 1; ++j) {
				if (((gsize*)slot) [j + 1] != bitmap [j]) {
					found = FALSE;
					break;
				}
			}
			if (found) {
				sgen_gc_unlock ();
				return __index;
			}
		}
		/* Skip the bitmap words */
		__index += (guint32)(first_word - 1);
		__offset += (guint32)(first_word - 1);
	} SGEN_ARRAY_LIST_END_FOREACH_SLOT;

	res = sgen_array_list_alloc_block (&complex_descriptors, nwords);

	SGEN_LOG (6, "Complex descriptor %d, size: %d (total desc memory: %d)", res, nwords, complex_descriptors.capacity);
	descriptor = (gsize*)sgen_array_list_get_slot (&complex_descriptors, res);
	descriptor [0] = nwords;
	for (i = 0; i < nwords - 1; ++i) {
		descriptor [1 + i] = bitmap [i];
		SGEN_LOG (6, "\tvalue: %p", (void*)descriptor [1 + i]);
	}
	sgen_gc_unlock ();
	return res;
}

gsize*
sgen_get_complex_descriptor (SgenDescriptor desc)
{
	return (gsize*) sgen_array_list_get_slot (&complex_descriptors, desc >> LOW_TYPE_BITS);
}

/*
 * Descriptor builders.
 */
SgenDescriptor
mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	SgenDescriptor desc = 0;
	size_t stored_size = SGEN_ALIGN_UP (obj_size);

	for (i = 0; i < numbits; ++i) {
		if (bitmap [i / GC_BITS_PER_WORD] & ((gsize)1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}

	if (first_set < 0) {
		SGEN_LOG (6, "Ptrfree descriptor %" PRIu64 ", size: %zu", (uint64_t)desc, stored_size);
		if (stored_size <= MAX_RUNLEN_OBJECT_SIZE && stored_size <= SGEN_MAX_SMALL_OBJ_SIZE)
			return (SgenDescriptor)(DESC_TYPE_SMALL_PTRFREE | stored_size);
		return DESC_TYPE_COMPLEX_PTRFREE;
	}

	g_assert (!(stored_size & 0x7));

	SGEN_ASSERT (5, stored_size == SGEN_ALIGN_UP (stored_size), "Size is not aligned");

	/* we know the 2-word header is ptr-free */
	if (last_set < BITMAP_NUM_BITS + OBJECT_HEADER_WORDS && stored_size <= SGEN_MAX_SMALL_OBJ_SIZE) {
		desc = (SgenDescriptor)(DESC_TYPE_BITMAP | ((*bitmap >> OBJECT_HEADER_WORDS) << LOW_TYPE_BITS));
		SGEN_LOG (6, "Largebitmap descriptor %" PRIu64 ", size: %zu, last set: %d", (uint64_t)desc, stored_size, last_set);
		return desc;
	}

	if (stored_size <= MAX_RUNLEN_OBJECT_SIZE && stored_size <= SGEN_MAX_SMALL_OBJ_SIZE) {
		/* check run-length encoding first: one byte offset, one byte number of pointers
		 * on 64 bit archs, we can have 3 runs, just one on 32.
		 * It may be better to use nibbles.
		 */
		if (first_set < 256 && num_set < 256 && (first_set + num_set == last_set + 1)) {
			desc = (SgenDescriptor)(DESC_TYPE_RUN_LENGTH | stored_size | (first_set << 16) | (num_set << 24));
			SGEN_LOG (6, "Runlen descriptor %" PRIu64 ", size: %zu, first set: %d, num set: %d", (uint64_t)desc, stored_size, first_set, num_set);
			return desc;
		}
	}

	/* it's a complex object ... */
	desc = DESC_TYPE_COMPLEX | (alloc_complex_descriptor (bitmap, last_set + 1) << LOW_TYPE_BITS);
	return desc;
}

/* If the array holds references, numbits == 1 and the first bit is set in elem_bitmap */
SgenDescriptor
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	SgenDescriptor desc = DESC_TYPE_VECTOR | (vector ? VECTOR_KIND_SZARRAY : VECTOR_KIND_ARRAY);
	for (i = 0; i < numbits; ++i) {
		if (elem_bitmap [i / GC_BITS_PER_WORD] & ((gsize)1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}

	if (first_set < 0) {
		if (elem_size <= MAX_ELEMENT_SIZE)
			return (SgenDescriptor)(desc | VECTOR_SUBTYPE_PTRFREE | (elem_size << VECTOR_ELSIZE_SHIFT));
		return DESC_TYPE_COMPLEX_PTRFREE;
	}

	if (elem_size <= MAX_ELEMENT_SIZE) {
		desc |= elem_size << VECTOR_ELSIZE_SHIFT;
		if (!num_set) {
			return desc | VECTOR_SUBTYPE_PTRFREE;
		}
		/* Note: we also handle structs with just ref fields */
		if (num_set * sizeof (gpointer) == elem_size) {
			return desc | VECTOR_SUBTYPE_REFS | ((gsize)(-1) << 16);
		}
		/* FIXME: try run-len first */
		/* Note: we can't skip the object header here, because it's not present */
		if (last_set < VECTOR_BITMAP_SIZE) {
			return (SgenDescriptor)(desc | VECTOR_SUBTYPE_BITMAP | (*elem_bitmap << 16));
		}
	}
	/* it's am array of complex structs ... */
	desc = DESC_TYPE_COMPLEX_ARR;
	desc |= alloc_complex_descriptor (elem_bitmap, last_set + 1) << LOW_TYPE_BITS;
	return desc;
}

/* Return the bitmap encoded by a descriptor */
gsize*
mono_gc_get_bitmap_for_descr (SgenDescriptor descr, int *numbits)
{
	SgenDescriptor d = (SgenDescriptor)descr;
	gsize *bitmap;

	switch (d & DESC_TYPE_MASK) {
	case DESC_TYPE_RUN_LENGTH: {
		int first_set = (d >> 16) & 0xff;
		int num_set = (d >> 24) & 0xff;
		int i;

		bitmap = g_new0 (gsize, (first_set + num_set + 7) / 8);

		for (i = first_set; i < first_set + num_set; ++i)
			bitmap [i / GC_BITS_PER_WORD] |= ((gsize)1 << (i % GC_BITS_PER_WORD));

		*numbits = first_set + num_set;

		return bitmap;
	}

	case DESC_TYPE_BITMAP: {
		gsize bmap = (d >> LOW_TYPE_BITS) << OBJECT_HEADER_WORDS;

		bitmap = g_new0 (gsize, 1);
		bitmap [0] = bmap;
		*numbits = 0;
		while (bmap) {
			(*numbits) ++;
			bmap >>= 1;
		}
		return bitmap;
	}

	case DESC_TYPE_COMPLEX: {
		gsize *bitmap_data = sgen_get_complex_descriptor (d);
		int bwords = (int)(*bitmap_data) - 1;//Max scalar object size is 1Mb, which means up to 32k descriptor words
		int i;

		bitmap = g_new0 (gsize, bwords);
		*numbits = bwords * GC_BITS_PER_WORD;

		for (i = 0; i < bwords; ++i) {
			bitmap [i] = bitmap_data [i + 1];
		}

		return bitmap;
	}

	default:
		g_assert_not_reached ();
	}
}

/**
 * mono_gc_make_descr_from_bitmap:
 */
SgenDescriptor
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	if (numbits == 0) {
		return MAKE_ROOT_DESC (ROOT_DESC_BITMAP, 0);
	} else if (numbits < ((sizeof (*bitmap) * 8) - ROOT_DESC_TYPE_SHIFT)) {
		return (SgenDescriptor)(MAKE_ROOT_DESC (ROOT_DESC_BITMAP, bitmap [0]));
	} else {
		SgenDescriptor complex = alloc_complex_descriptor (bitmap, numbits);
		return MAKE_ROOT_DESC (ROOT_DESC_COMPLEX, complex);
	}
}

SgenDescriptor
mono_gc_make_vector_descr (void)
{
	return MAKE_ROOT_DESC (ROOT_DESC_VECTOR, 0);
}

SgenDescriptor
mono_gc_make_root_descr_all_refs (int numbits)
{
	gsize *gc_bitmap;
	SgenDescriptor descr;
	int num_bytes = numbits / 8;

	if (numbits < 32 && all_ref_root_descrs [numbits])
		return all_ref_root_descrs [numbits];

	gc_bitmap = (gsize *)g_malloc0 (ALIGN_TO (ALIGN_TO (numbits, 8) + 1, sizeof (gsize)));
	memset (gc_bitmap, 0xff, num_bytes);
	if (numbits < ((sizeof (*gc_bitmap) * 8) - ROOT_DESC_TYPE_SHIFT))
		gc_bitmap[0] = GUINT64_TO_LE(gc_bitmap[0]);
	else if (numbits && num_bytes % (sizeof (*gc_bitmap)))
		gc_bitmap[num_bytes / 8] = GUINT64_TO_LE(gc_bitmap [num_bytes / 8]);
	if (numbits % 8)
		gc_bitmap [numbits / 8] = (1 << (numbits % 8)) - 1;
	descr = mono_gc_make_descr_from_bitmap (gc_bitmap, numbits);
	g_free (gc_bitmap);

	if (numbits < 32)
		all_ref_root_descrs [numbits] = descr;

	return descr;
}

SgenDescriptor
sgen_make_user_root_descriptor (SgenUserRootMarkFunc marker)
{
	SgenDescriptor descr;

	g_assert (user_descriptors_next < MAX_USER_DESCRIPTORS);
	descr = MAKE_ROOT_DESC (ROOT_DESC_USER, (SgenDescriptor)user_descriptors_next);
	user_descriptors [user_descriptors_next ++] = marker;

	return descr;
}

void*
sgen_get_complex_descriptor_bitmap (SgenDescriptor desc)
{
	return (void*) sgen_array_list_get_slot (&complex_descriptors, desc >> ROOT_DESC_TYPE_SHIFT);
}

SgenUserRootMarkFunc
sgen_get_user_descriptor_func (SgenDescriptor desc)
{
	return user_descriptors [desc >> ROOT_DESC_TYPE_SHIFT];
}

#ifdef HEAVY_STATISTICS
void
sgen_descriptor_count_scanned_object (SgenDescriptor desc)
{
	int type = desc & DESC_TYPE_MASK;
	SGEN_ASSERT (0, type, "Descriptor type can't be zero");
	++stat_scanned_count_per_descriptor [type - 1];
}

void
sgen_descriptor_count_copied_object (SgenDescriptor desc)
{
	int type = desc & DESC_TYPE_MASK;
	SGEN_ASSERT (0, type, "Descriptor type can't be zero");
	++stat_copied_count_per_descriptor [type - 1];
}
#endif

void
sgen_init_descriptors (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("# scanned RUN_LENGTH", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_RUN_LENGTH - 1]);
	mono_counters_register ("# scanned SMALL_PTRFREE", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_SMALL_PTRFREE - 1]);
	mono_counters_register ("# scanned COMPLEX", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_COMPLEX - 1]);
	mono_counters_register ("# scanned VECTOR", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_VECTOR - 1]);
	mono_counters_register ("# scanned BITMAP", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_BITMAP - 1]);
	mono_counters_register ("# scanned COMPLEX_ARR", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_COMPLEX_ARR - 1]);
	mono_counters_register ("# scanned COMPLEX_PTRFREE", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_scanned_count_per_descriptor [DESC_TYPE_COMPLEX_PTRFREE - 1]);

	mono_counters_register ("# copied RUN_LENGTH", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_RUN_LENGTH - 1]);
	mono_counters_register ("# copied SMALL_PTRFREE", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_SMALL_PTRFREE - 1]);
	mono_counters_register ("# copied COMPLEX", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_COMPLEX - 1]);
	mono_counters_register ("# copied VECTOR", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_VECTOR - 1]);
	mono_counters_register ("# copied BITMAP", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_BITMAP - 1]);
	mono_counters_register ("# copied COMPLEX_ARR", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_COMPLEX_ARR - 1]);
	mono_counters_register ("# copied COMPLEX_PTRFREE", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_copied_count_per_descriptor [DESC_TYPE_COMPLEX_PTRFREE - 1]);
#endif
}

#endif
