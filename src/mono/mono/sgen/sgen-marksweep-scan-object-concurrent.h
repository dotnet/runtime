/*
 * sgen-major-scan-object.h: Object scanning in the major collectors.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
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

extern guint64 stat_scan_object_called_major;

/*
 * FIXME: We use the same scanning function in the concurrent collector whether we scan
 * during the starting/finishing collection pause (with the world stopped) or from the
 * concurrent worker thread.
 *
 * As long as the world is stopped, we should just follow pointers into the nursery and
 * evict if possible.  In that case we also don't need the ALWAYS_ADD_TO_GLOBAL_REMSET case,
 * which only seems to make sense for when the world is stopped, in which case we only need
 * it because we don't follow into the nursery.
 */

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		GCObject *__old = *(ptr);					\
		SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP ((obj), (ptr)); \
		binary_protocol_scan_process_reference ((obj), (ptr), __old); \
		if (__old && !sgen_ptr_in_nursery (__old)) {		\
			PREFETCH_READ (__old);			\
			major_copy_or_mark_object_concurrent ((ptr), __old, queue); \
		} else {						\
			if (G_UNLIKELY (sgen_ptr_in_nursery (__old) && !sgen_ptr_in_nursery ((ptr)))) \
				ADD_TO_GLOBAL_REMSET ((full_object), (ptr), __old); \
		}							\
	} while (0)

/* FIXME: Unify this with optimized code in sgen-marksweep.c. */

#undef ADD_TO_GLOBAL_REMSET
#define ADD_TO_GLOBAL_REMSET(object,ptr,target)	mark_mod_union_card ((object), (void**)(ptr))

static void
major_scan_object_no_mark_concurrent_anywhere (GCObject *full_object, SgenDescriptor desc, SgenGrayQueue *queue)
{
	char *start = (char*)full_object;

	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

#ifdef HEAVY_STATISTICS
	sgen_descriptor_count_scanned_object (desc);
#endif
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	add_scanned_object (start);
#endif

#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"

	SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP;
	HEAVY_STAT (++stat_scan_object_called_major);
}

static void
major_scan_object_no_mark_concurrent_start (GCObject *start, SgenDescriptor desc, SgenGrayQueue *queue)
{
	major_scan_object_no_mark_concurrent_anywhere (start, desc, queue);
}

static void
major_scan_object_no_mark_concurrent (GCObject *start, SgenDescriptor desc, SgenGrayQueue *queue)
{
	SGEN_ASSERT (0, !sgen_ptr_in_nursery (start), "Why are we scanning nursery objects in the concurrent collector?");
	major_scan_object_no_mark_concurrent_anywhere (start, desc, queue);
}

#undef ADD_TO_GLOBAL_REMSET
#define ADD_TO_GLOBAL_REMSET(object,ptr,target)	sgen_add_to_global_remset ((ptr), (target))

static void
major_scan_vtype_concurrent_finish (GCObject *full_object, char *start, SgenDescriptor desc, SgenGrayQueue *queue BINARY_PROTOCOL_ARG (size_t size))
{
	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

#ifdef HEAVY_STATISTICS
	/* FIXME: We're half scanning this object.  How do we account for that? */
	//add_scanned_object (start);
#endif

	/* The descriptors include info about the object header as well */
	start -= SGEN_CLIENT_OBJECT_HEADER_SIZE;

#define SCAN_OBJECT_NOVTABLE
#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"

	SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP;
}
