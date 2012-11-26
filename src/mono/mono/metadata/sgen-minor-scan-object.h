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

extern long long stat_scan_object_called_nursery;

#if defined(SGEN_SIMPLE_NURSERY)
#define SERIAL_SCAN_OBJECT simple_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE simple_nursery_serial_scan_vtype
#define PARALLEL_SCAN_OBJECT simple_nursery_parallel_scan_object
#define PARALLEL_SCAN_VTYPE simple_nursery_parallel_scan_vtype

#elif defined (SGEN_SPLIT_NURSERY)
#define SERIAL_SCAN_OBJECT split_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE split_nursery_serial_scan_vtype
#define PARALLEL_SCAN_OBJECT split_nursery_parallel_scan_object
#define PARALLEL_SCAN_VTYPE split_nursery_parallel_scan_vtype

#else
#error "Please define GC_CONF_NAME"
#endif

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		void *__copy;		\
		if (__old) {	\
			PARALLEL_COPY_OBJECT ((ptr), queue);	\
			__copy = *(ptr);	\
			SGEN_COND_LOG (9, __old != __copy, "Overwrote field at %p with %p (was: %p)", (ptr), *(ptr), __old);	\
			if (G_UNLIKELY (sgen_ptr_in_nursery (__copy) && !sgen_ptr_in_nursery ((ptr)))) \
				sgen_add_to_global_remset ((ptr));	\
		}	\
	} while (0)

/*
 * Scan the object pointed to by @start for references to
 * other objects between @from_start and @from_end and copy
 * them to the gray_objects area.
 */
static void
PARALLEL_SCAN_OBJECT (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_nursery);
}

/*
 * scan_vtype:
 *
 * Scan the valuetype pointed to by START, described by DESC for references to
 * other objects between @from_start and @from_end and copy them to the gray_objects area.
 * Returns a pointer to the end of the object.
 */
static void
PARALLEL_SCAN_VTYPE (char *start, mword desc, SgenGrayQueue *queue)
{
	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

#define SCAN_OBJECT_NOVTABLE
#include "sgen-scan-object.h"
}

#undef HANDLE_PTR
/* Global remsets are handled in SERIAL_COPY_OBJECT_FROM_OBJ */
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		if (__old) {	\
			SERIAL_COPY_OBJECT_FROM_OBJ ((ptr), queue);	\
			SGEN_COND_LOG (9, __old != *(ptr), "Overwrote field at %p with %p (was: %p)", (ptr), *(ptr), __old); \
		}	\
	} while (0)

static void
SERIAL_SCAN_OBJECT (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_nursery);
}

static void
SERIAL_SCAN_VTYPE (char *start, mword desc, SgenGrayQueue *queue)
{
	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

#define SCAN_OBJECT_NOVTABLE
#include "sgen-scan-object.h"
}

#define FILL_MINOR_COLLECTOR_SCAN_OBJECT(collector)	do {			\
		(collector)->parallel_ops.scan_object = PARALLEL_SCAN_OBJECT;	\
		(collector)->parallel_ops.scan_vtype = PARALLEL_SCAN_VTYPE;	\
		(collector)->serial_ops.scan_object = SERIAL_SCAN_OBJECT;	\
		(collector)->serial_ops.scan_vtype = SERIAL_SCAN_VTYPE; \
	} while (0)
