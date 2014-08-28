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

extern long long stat_scan_object_called_major;

#ifdef FIXED_HEAP
#define PREFETCH_DYNAMIC_HEAP(addr)
#else
#define PREFETCH_DYNAMIC_HEAP(addr)	PREFETCH ((addr))
#endif

#ifdef SCAN_FOR_CONCURRENT_MARK
#define FOLLOW_OBJECT(addr)	(!sgen_ptr_in_nursery ((addr)))
#define ALWAYS_ADD_TO_GLOBAL_REMSET	1
#define CONCURRENT_NAME(x)	x ## _concurrent
#else
#define FOLLOW_OBJECT(addr)	1
#define ALWAYS_ADD_TO_GLOBAL_REMSET	0
#define CONCURRENT_NAME(x)	x
#endif

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		void *__old = *(ptr);					\
		void *__copy;						\
		SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP ((obj), (ptr)); \
		if (__old && FOLLOW_OBJECT (__old)) {			\
			PREFETCH_DYNAMIC_HEAP (__old);			\
			CONCURRENT_NAME (major_copy_or_mark_object) ((ptr), __old, queue); \
			__copy = *(ptr);				\
			SGEN_COND_LOG (9, __old != __copy, "Overwrote field at %p with %p (was: %p)", (ptr), *(ptr), __old); \
			if (G_UNLIKELY (sgen_ptr_in_nursery (__copy) && !sgen_ptr_in_nursery ((ptr)))) \
				sgen_add_to_global_remset ((ptr), __copy);	\
		} else {						\
			if (ALWAYS_ADD_TO_GLOBAL_REMSET && G_UNLIKELY (sgen_ptr_in_nursery (__old) && !sgen_ptr_in_nursery ((ptr)))) \
				sgen_add_to_global_remset ((ptr), __old); \
		}							\
	} while (0)

static void
CONCURRENT_NAME (major_scan_object) (char *start, SgenGrayQueue *queue)
{
	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"

	SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP;
	HEAVY_STAT (++stat_scan_object_called_major);
}

#ifdef SCAN_FOR_CONCURRENT_MARK
#ifdef SGEN_PARALLEL_MARK
#error concurrent and parallel mark not supported yet
#else
static void
CONCURRENT_NAME (major_scan_vtype) (char *start, mword desc, SgenGrayQueue *queue BINARY_PROTOCOL_ARG (size_t size))
{
	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

#define SCAN_OBJECT_NOVTABLE
#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"

	SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP;
}
#endif
#endif

#undef PREFETCH_DYNAMIC_HEAP
#undef FOLLOW_OBJECT
#undef ALWAYS_ADD_TO_GLOBAL_REMSET
#undef CONCURRENT_NAME
