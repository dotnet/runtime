/*
 * sgen-minor-scan-object.h: Object scanning in the nursery collectors.
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

extern guint64 stat_scan_object_called_nursery;

#if defined(SGEN_SIMPLE_NURSERY)
#define SERIAL_SCAN_OBJECT simple_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE simple_nursery_serial_scan_vtype

#elif defined (SGEN_SPLIT_NURSERY)
#define SERIAL_SCAN_OBJECT split_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE split_nursery_serial_scan_vtype

#else
#error "Please define GC_CONF_NAME"
#endif

#undef HANDLE_PTR
/* Global remsets are handled in SERIAL_COPY_OBJECT_FROM_OBJ */
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP ((obj), (ptr)); \
		binary_protocol_scan_process_reference ((obj), (ptr), __old); \
		if (__old) {	\
			SERIAL_COPY_OBJECT_FROM_OBJ ((ptr), queue);	\
			SGEN_COND_LOG (9, __old != *(ptr), "Overwrote field at %p with %p (was: %p)", (ptr), *(ptr), __old); \
		}	\
	} while (0)

static void
SERIAL_SCAN_OBJECT (GCObject *object, SgenDescriptor desc, SgenGrayQueue *queue)
{
	char *start = (char*)object;

	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

#ifdef HEAVY_STATISTICS
	sgen_descriptor_count_scanned_object (desc);
#endif

	SGEN_ASSERT (9, sgen_get_current_collection_generation () == GENERATION_NURSERY, "Must not use minor scan during major collection.");

#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"

	SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP;
	HEAVY_STAT (++stat_scan_object_called_nursery);
}

static void
SERIAL_SCAN_VTYPE (GCObject *full_object, char *start, SgenDescriptor desc, SgenGrayQueue *queue BINARY_PROTOCOL_ARG (size_t size))
{
	SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP;

	SGEN_ASSERT (9, sgen_get_current_collection_generation () == GENERATION_NURSERY, "Must not use minor scan during major collection.");

	/* The descriptors include info about the MonoObject header as well */
	start -= SGEN_CLIENT_OBJECT_HEADER_SIZE;

#define SCAN_OBJECT_NOVTABLE
#define SCAN_OBJECT_PROTOCOL
#include "sgen-scan-object.h"
}

#define FILL_MINOR_COLLECTOR_SCAN_OBJECT(collector)	do {			\
		(collector)->serial_ops.scan_object = SERIAL_SCAN_OBJECT;	\
		(collector)->serial_ops.scan_vtype = SERIAL_SCAN_VTYPE; \
	} while (0)
