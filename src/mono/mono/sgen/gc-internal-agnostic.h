/**
 * \file
 * Mono-agnostic GC interface.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_METADATA_GCINTERNALAGNOSTIC_H__
#define __MONO_METADATA_GCINTERNALAGNOSTIC_H__

#include <config.h>
#include <glib.h>
#include <stdio.h>

#include "mono/utils/ward.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/parse.h"
#include "mono/utils/memfuncs.h"
#ifdef HAVE_SGEN_GC
#include "mono/sgen/sgen-conf.h"
#endif

/* h indicates whether to hide or just tag.
 * (-!!h ^ p) is used instead of (h ? ~p : p) to avoid multiple mentions of p.
 */
#define MONO_GC_HIDE_POINTER(p,t,h) ((gpointer)(((-(size_t)!!(h) ^ (size_t)(p)) & ~(size_t)3) | ((t) & (size_t)3)))
#define MONO_GC_REVEAL_POINTER(p,h) ((gpointer)((-(size_t)!!(h) ^ (size_t)(p)) & ~(size_t)3))

#define MONO_GC_POINTER_TAG(p) ((size_t)(p) & (size_t)3)

#define MONO_GC_HANDLE_OCCUPIED_MASK (1)
#define MONO_GC_HANDLE_VALID_MASK (2)
#define MONO_GC_HANDLE_TAG_MASK (MONO_GC_HANDLE_OCCUPIED_MASK | MONO_GC_HANDLE_VALID_MASK)

#define MONO_GC_HANDLE_METADATA_POINTER(p,h) (MONO_GC_HIDE_POINTER ((p), MONO_GC_HANDLE_OCCUPIED_MASK, (h)))
#define MONO_GC_HANDLE_OBJECT_POINTER(p,h) (MONO_GC_HIDE_POINTER ((p), MONO_GC_HANDLE_OCCUPIED_MASK | MONO_GC_HANDLE_VALID_MASK, (h)))

#define MONO_GC_HANDLE_OCCUPIED(slot) ((size_t)(slot) & MONO_GC_HANDLE_OCCUPIED_MASK)
#define MONO_GC_HANDLE_VALID(slot) ((size_t)(slot) & MONO_GC_HANDLE_VALID_MASK)

#define MONO_GC_HANDLE_TAG(slot) ((size_t)(slot) & MONO_GC_HANDLE_TAG_MASK)

#define MONO_GC_HANDLE_IS_OBJECT_POINTER(slot) (MONO_GC_HANDLE_TAG (slot) == (MONO_GC_HANDLE_OCCUPIED_MASK | MONO_GC_HANDLE_VALID_MASK))
#define MONO_GC_HANDLE_IS_METADATA_POINTER(slot) (MONO_GC_HANDLE_TAG (slot) == MONO_GC_HANDLE_OCCUPIED_MASK)

/* These should match System.Runtime.InteropServices.GCHandleType */
typedef enum {
	HANDLE_TYPE_MIN = 0,
	HANDLE_WEAK = HANDLE_TYPE_MIN,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED,
	HANDLE_WEAK_FIELDS,
	HANDLE_TYPE_MAX
} GCHandleType;

#define GC_HANDLE_TYPE_IS_WEAK(x) ((x) <= HANDLE_WEAK_TRACK)

#define MONO_GC_HANDLE_TYPE_SHIFT (3)
#define MONO_GC_HANDLE_TYPE_MASK ((1 << MONO_GC_HANDLE_TYPE_SHIFT) - 1)
#define MONO_GC_HANDLE_TYPE(x) ((GCHandleType)(((x) & MONO_GC_HANDLE_TYPE_MASK) - 1))
#define MONO_GC_HANDLE_SLOT(x) ((x) >> MONO_GC_HANDLE_TYPE_SHIFT)
#define MONO_GC_HANDLE_TYPE_IS_WEAK(x) ((x) <= HANDLE_WEAK_TRACK)
#define MONO_GC_HANDLE(slot, type) (((slot) << MONO_GC_HANDLE_TYPE_SHIFT) | (((type) & MONO_GC_HANDLE_TYPE_MASK) + 1))

typedef struct {
	gint32 minor_gc_count;
	gint32 major_gc_count;
	gint64 minor_gc_time;
	gint64 major_gc_time;
	gint64 major_gc_time_concurrent;
} GCStats;

extern GCStats gc_stats;

#ifdef HAVE_SGEN_GC
typedef SgenDescriptor MonoGCDescriptor;
#define MONO_GC_DESCRIPTOR_NULL	SGEN_DESCRIPTOR_NULL
#else
typedef void* MonoGCDescriptor;
#define MONO_GC_DESCRIPTOR_NULL NULL
#endif

gboolean mono_gc_parse_environment_string_extract_number (const char *str, size_t *out);

MonoGCDescriptor mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size)
    MONO_PERMIT (need (sgen_lock_gc));
MonoGCDescriptor mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size)
    MONO_PERMIT (need (sgen_lock_gc));

/* simple interface for data structures needed in the runtime */
MonoGCDescriptor mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits)
    MONO_PERMIT (need (sgen_lock_gc));

/* Return a root descriptor for a vector with repeating refs bitmap */
MonoGCDescriptor mono_gc_make_vector_descr (void);

/* Return a root descriptor for a root with all refs */
MonoGCDescriptor mono_gc_make_root_descr_all_refs (int numbits)
    MONO_PERMIT (need (sgen_lock_gc));

/* Return the bitmap encoded by a descriptor */
gsize* mono_gc_get_bitmap_for_descr (MonoGCDescriptor descr, int *numbits);

/*
These functions must be used when it's possible that either destination is not
word aligned or size is not a multiple of word size.
*/
void mono_gc_bzero_atomic (void *dest, size_t size);
void mono_gc_bzero_aligned (void *dest, size_t size);
void mono_gc_memmove_atomic (void *dest, const void *src, size_t size);
void mono_gc_memmove_aligned (void *dest, const void *src, size_t size);

FILE *mono_gc_get_logfile (void);

/* equivalent to options set via MONO_GC_PARAMS */
void mono_gc_params_set (const char* options);
/* equivalent to options set via MONO_GC_DEBUG */
void mono_gc_debug_set (const char* options);

#endif
