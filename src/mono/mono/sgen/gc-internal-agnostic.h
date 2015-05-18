/*
 * gc-internal-agnostic.h: Mono-agnostic GC interface.
 *
 * Copyright (C) 2015 Xamarin Inc
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

#ifndef __MONO_METADATA_GCINTERNALAGNOSTIC_H__
#define __MONO_METADATA_GCINTERNALAGNOSTIC_H__

#include <config.h>
#include <glib.h>
#include <stdio.h>

#include "mono/utils/mono-compiler.h"
#include "mono/utils/parse.h"
#include "mono/utils/memfuncs.h"
#ifdef HAVE_SGEN_GC
#include "mono/sgen/sgen-conf.h"
#endif

typedef struct {
	guint minor_gc_count;
	guint major_gc_count;
	guint64 minor_gc_time;
	guint64 major_gc_time;
	guint64 major_gc_time_concurrent;
} GCStats;

extern GCStats gc_stats;

#ifdef HAVE_SGEN_GC
typedef SgenDescriptor MonoGCDescriptor;
#define MONO_GC_DESCRIPTOR_NULL	SGEN_DESCRIPTOR_NULL
#else
typedef void* MonoGCDescriptor;
#define MONO_GC_DESCRIPTOR_NULL NULL
#endif

/*
 * Try to register a foreign thread with the GC, if we fail or the backend
 * can't cope with this concept - we return FALSE.
 */
extern gboolean mono_gc_register_thread (void *baseptr);

gboolean mono_gc_parse_environment_string_extract_number (const char *str, size_t *out);

MonoGCDescriptor mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size);
MonoGCDescriptor mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size);

/* simple interface for data structures needed in the runtime */
MonoGCDescriptor mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits);

/* Return a root descriptor for a root with all refs */
MonoGCDescriptor mono_gc_make_root_descr_all_refs (int numbits);

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

#endif
