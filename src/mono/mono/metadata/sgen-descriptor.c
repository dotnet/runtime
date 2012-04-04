/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
#include <signal.h>
#include <errno.h>
#include <assert.h>
#ifdef __MACH__
#undef _XOPEN_SOURCE
#endif
#ifdef __MACH__
#define _XOPEN_SOURCE
#endif

#include "metadata/sgen-gc.h"

#define MAX_USER_DESCRIPTORS 16

#define MAKE_ROOT_DESC(type,val) ((type) | ((val) << ROOT_DESC_TYPE_SHIFT))
#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))


static gsize* complex_descriptors = NULL;
static int complex_descriptors_size = 0;
static int complex_descriptors_next = 0;
static MonoGCRootMarkFunc user_descriptors [MAX_USER_DESCRIPTORS];
static int user_descriptors_next = 0;
static void *all_ref_root_descrs [32];


static int
alloc_complex_descriptor (gsize *bitmap, int numbits)
{
	int nwords, res, i;

	numbits = ALIGN_TO (numbits, GC_BITS_PER_WORD);
	nwords = numbits / GC_BITS_PER_WORD + 1;

	sgen_gc_lock ();
	res = complex_descriptors_next;
	/* linear search, so we don't have duplicates with domain load/unload
	 * this should not be performance critical or we'd have bigger issues
	 * (the number and size of complex descriptors should be small).
	 */
	for (i = 0; i < complex_descriptors_next; ) {
		if (complex_descriptors [i] == nwords) {
			int j, found = TRUE;
			for (j = 0; j < nwords - 1; ++j) {
				if (complex_descriptors [i + 1 + j] != bitmap [j]) {
					found = FALSE;
					break;
				}
			}
			if (found) {
				sgen_gc_unlock ();
				return i;
			}
		}
		i += complex_descriptors [i];
	}
	if (complex_descriptors_next + nwords > complex_descriptors_size) {
		int new_size = complex_descriptors_size * 2 + nwords;
		complex_descriptors = g_realloc (complex_descriptors, new_size * sizeof (gsize));
		complex_descriptors_size = new_size;
	}
	DEBUG (6, fprintf (gc_debug_file, "Complex descriptor %d, size: %d (total desc memory: %d)\n", res, nwords, complex_descriptors_size));
	complex_descriptors_next += nwords;
	complex_descriptors [res] = nwords;
	for (i = 0; i < nwords - 1; ++i) {
		complex_descriptors [res + 1 + i] = bitmap [i];
		DEBUG (6, fprintf (gc_debug_file, "\tvalue: %p\n", (void*)complex_descriptors [res + 1 + i]));
	}
	sgen_gc_unlock ();
	return res;
}

gsize*
sgen_get_complex_descriptor (mword desc)
{
	return complex_descriptors + (desc >> LOW_TYPE_BITS);
}

/*
 * Descriptor builders.
 */
void*
mono_gc_make_descr_for_string (gsize *bitmap, int numbits)
{
	return (void*) DESC_TYPE_RUN_LENGTH;
}

void*
mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	mword desc = 0;
	size_t stored_size = obj_size;

	stored_size += SGEN_ALLOC_ALIGN - 1;
	stored_size &= ~(SGEN_ALLOC_ALIGN - 1);

	for (i = 0; i < numbits; ++i) {
		if (bitmap [i / GC_BITS_PER_WORD] & ((gsize)1 << (i % GC_BITS_PER_WORD))) {
			if (first_set < 0)
				first_set = i;
			last_set = i;
			num_set++;
		}
	}

	if (first_set < 0) {
		DEBUG (6, fprintf (gc_debug_file, "Ptrfree descriptor %p, size: %zd\n", (void*)desc, stored_size));
		if (stored_size <= MAX_RUNLEN_OBJECT_SIZE)
			return (void*)(DESC_TYPE_RUN_LENGTH | stored_size);
		return (void*)DESC_TYPE_COMPLEX_PTRFREE;
	}

	g_assert (!(stored_size & 0x7));

	if (stored_size <= MAX_RUNLEN_OBJECT_SIZE) {
		/* check run-length encoding first: one byte offset, one byte number of pointers
		 * on 64 bit archs, we can have 3 runs, just one on 32.
		 * It may be better to use nibbles.
		 */
		if (first_set < 256 && num_set < 256 && (first_set + num_set == last_set + 1)) {
			desc = DESC_TYPE_RUN_LENGTH | stored_size | (first_set << 16) | (num_set << 24);
			DEBUG (6, fprintf (gc_debug_file, "Runlen descriptor %p, size: %zd, first set: %d, num set: %d\n", (void*)desc, stored_size, first_set, num_set));
			return (void*) desc;
		}
	}

	/* we know the 2-word header is ptr-free */
	if (last_set < SMALL_BITMAP_SIZE + OBJECT_HEADER_WORDS) {
		desc = DESC_TYPE_SMALL_BITMAP | stored_size | ((*bitmap >> OBJECT_HEADER_WORDS) << SMALL_BITMAP_SHIFT);
		DEBUG (6, fprintf (gc_debug_file, "Smallbitmap descriptor %p, size: %zd, last set: %d\n", (void*)desc, stored_size, last_set));
		return (void*) desc;
	}

	/* we know the 2-word header is ptr-free */
	if (last_set < LARGE_BITMAP_SIZE + OBJECT_HEADER_WORDS) {
		desc = DESC_TYPE_LARGE_BITMAP | ((*bitmap >> OBJECT_HEADER_WORDS) << LOW_TYPE_BITS);
		DEBUG (6, fprintf (gc_debug_file, "Largebitmap descriptor %p, size: %zd, last set: %d\n", (void*)desc, stored_size, last_set));
		return (void*) desc;
	}
	/* it's a complex object ... */
	desc = DESC_TYPE_COMPLEX | (alloc_complex_descriptor (bitmap, last_set + 1) << LOW_TYPE_BITS);
	return (void*) desc;
}

/* If the array holds references, numbits == 1 and the first bit is set in elem_bitmap */
void*
mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
{
	int first_set = -1, num_set = 0, last_set = -1, i;
	mword desc = DESC_TYPE_VECTOR | (vector ? VECTOR_KIND_SZARRAY : VECTOR_KIND_ARRAY);
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
			return (void*)(desc | VECTOR_SUBTYPE_PTRFREE | (elem_size << VECTOR_ELSIZE_SHIFT));
		return (void*)DESC_TYPE_COMPLEX_PTRFREE;
	}

	if (elem_size <= MAX_ELEMENT_SIZE) {
		desc |= elem_size << VECTOR_ELSIZE_SHIFT;
		if (!num_set) {
			return (void*)(desc | VECTOR_SUBTYPE_PTRFREE);
		}
		/* Note: we also handle structs with just ref fields */
		if (num_set * sizeof (gpointer) == elem_size) {
			return (void*)(desc | VECTOR_SUBTYPE_REFS | ((gssize)(-1) << 16));
		}
		/* FIXME: try run-len first */
		/* Note: we can't skip the object header here, because it's not present */
		if (last_set <= SMALL_BITMAP_SIZE) {
			return (void*)(desc | VECTOR_SUBTYPE_BITMAP | (*elem_bitmap << 16));
		}
	}
	/* it's am array of complex structs ... */
	desc = DESC_TYPE_COMPLEX_ARR;
	desc |= alloc_complex_descriptor (elem_bitmap, last_set + 1) << LOW_TYPE_BITS;
	return (void*) desc;
}

/* Return the bitmap encoded by a descriptor */
gsize*
mono_gc_get_bitmap_for_descr (void *descr, int *numbits)
{
	mword d = (mword)descr;
	gsize *bitmap;

	switch (d & 0x7) {
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

	case DESC_TYPE_SMALL_BITMAP:
		bitmap = g_new0 (gsize, 1);

		bitmap [0] = (d >> SMALL_BITMAP_SHIFT) << OBJECT_HEADER_WORDS;

		*numbits = GC_BITS_PER_WORD;
		return bitmap;

	case DESC_TYPE_LARGE_BITMAP: {
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
	default:
		g_assert_not_reached ();
	}
}

void*
mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
{
	if (numbits == 0) {
		return (void*)MAKE_ROOT_DESC (ROOT_DESC_BITMAP, 0);
	} else if (numbits < ((sizeof (*bitmap) * 8) - ROOT_DESC_TYPE_SHIFT)) {
		return (void*)MAKE_ROOT_DESC (ROOT_DESC_BITMAP, bitmap [0]);
	} else {
		mword complex = alloc_complex_descriptor (bitmap, numbits);
		return (void*)MAKE_ROOT_DESC (ROOT_DESC_COMPLEX, complex);
	}
}

void*
mono_gc_make_root_descr_all_refs (int numbits)
{
	gsize *gc_bitmap;
	void *descr;
	int num_bytes = numbits / 8;

	if (numbits < 32 && all_ref_root_descrs [numbits])
		return all_ref_root_descrs [numbits];

	gc_bitmap = g_malloc0 (ALIGN_TO (ALIGN_TO (numbits, 8) + 1, sizeof (gsize)));
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

void*
mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker)
{
	void *descr;

	g_assert (user_descriptors_next < MAX_USER_DESCRIPTORS);
	descr = (void*)MAKE_ROOT_DESC (ROOT_DESC_USER, (mword)user_descriptors_next);
	user_descriptors [user_descriptors_next ++] = marker;

	return descr;
}

void*
sgen_get_complex_descriptor_bitmap (mword desc)
{
	return complex_descriptors + (desc >> ROOT_DESC_TYPE_SHIFT);
}

MonoGCRootMarkFunc
sgen_get_user_descriptor_func (mword desc)
{
	return user_descriptors [desc >> ROOT_DESC_TYPE_SHIFT];
}

#endif
