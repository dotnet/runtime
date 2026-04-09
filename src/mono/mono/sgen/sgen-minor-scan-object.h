/**
 * \file
 * Object scanning in the nursery collectors.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

extern guint64 stat_scan_object_called_nursery;

#undef SERIAL_SCAN_OBJECT
#undef SERIAL_SCAN_VTYPE
#undef SERIAL_SCAN_PTR_FIELD
#undef SERIAL_DRAIN_GRAY_STACK

#if defined(SGEN_SIMPLE_NURSERY)

#ifdef SGEN_SIMPLE_PAR_NURSERY
#ifdef SGEN_CONCURRENT_MAJOR
#define SERIAL_SCAN_OBJECT simple_par_nursery_serial_with_concurrent_major_scan_object
#define SERIAL_SCAN_VTYPE simple_par_nursery_serial_with_concurrent_major_scan_vtype
#define SERIAL_SCAN_PTR_FIELD simple_par_nursery_serial_with_concurrent_major_scan_ptr_field
#define SERIAL_DRAIN_GRAY_STACK simple_par_nursery_serial_with_concurrent_major_drain_gray_stack
#else
#define SERIAL_SCAN_OBJECT simple_par_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE simple_par_nursery_serial_scan_vtype
#define SERIAL_SCAN_PTR_FIELD simple_par_nursery_serial_scan_ptr_field
#define SERIAL_DRAIN_GRAY_STACK simple_par_nursery_serial_drain_gray_stack
#endif
#else
#ifdef SGEN_CONCURRENT_MAJOR
#define SERIAL_SCAN_OBJECT simple_nursery_serial_with_concurrent_major_scan_object
#define SERIAL_SCAN_VTYPE simple_nursery_serial_with_concurrent_major_scan_vtype
#define SERIAL_SCAN_PTR_FIELD simple_nursery_serial_with_concurrent_major_scan_ptr_field
#define SERIAL_DRAIN_GRAY_STACK simple_nursery_serial_with_concurrent_major_drain_gray_stack
#else
#define SERIAL_SCAN_OBJECT simple_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE simple_nursery_serial_scan_vtype
#define SERIAL_SCAN_PTR_FIELD simple_nursery_serial_scan_ptr_field
#define SERIAL_DRAIN_GRAY_STACK simple_nursery_serial_drain_gray_stack
#endif
#endif

#elif defined (SGEN_SPLIT_NURSERY)

#ifdef SGEN_CONCURRENT_MAJOR
#define SERIAL_SCAN_OBJECT split_nursery_serial_with_concurrent_major_scan_object
#define SERIAL_SCAN_VTYPE split_nursery_serial_with_concurrent_major_scan_vtype
#define SERIAL_SCAN_PTR_FIELD split_nursery_serial_with_concurrent_major_scan_ptr_field
#define SERIAL_DRAIN_GRAY_STACK split_nursery_serial_with_concurrent_major_drain_gray_stack
#else
#define SERIAL_SCAN_OBJECT split_nursery_serial_scan_object
#define SERIAL_SCAN_VTYPE split_nursery_serial_scan_vtype
#define SERIAL_SCAN_PTR_FIELD split_nursery_serial_scan_ptr_field
#define SERIAL_DRAIN_GRAY_STACK split_nursery_serial_drain_gray_stack
#endif

#else
#error "No nursery configuration specified"
#endif

#undef HANDLE_PTR
/* Global remsets are handled in SERIAL_COPY_OBJECT_FROM_OBJ */
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP ((obj), (ptr)); \
		sgen_binary_protocol_scan_process_reference ((full_object), (ptr), __old); \
		if (__old) {	\
			SERIAL_COPY_OBJECT_FROM_OBJ ((ptr), queue);	\
			SGEN_COND_LOG (9, __old != *(ptr), "Overwrote field at %p with %p (was: %p)", (ptr), *(ptr), __old); \
		}	\
	} while (0)

static void
SERIAL_SCAN_OBJECT (GCObject *full_object, SgenDescriptor desc, SgenGrayQueue *queue)
{
	char *start = (char*)full_object;

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

static void
SERIAL_SCAN_PTR_FIELD (GCObject *full_object, GCObject **ptr, SgenGrayQueue *queue)
{
	HANDLE_PTR (ptr, NULL);
}

static gboolean
SERIAL_DRAIN_GRAY_STACK (SgenGrayQueue *queue)
{
#ifdef SGEN_SIMPLE_PAR_NURSERY
	int i;
	/*
	 * We do bounded iteration so we can switch to optimized context
	 * when we are the last worker remaining.
	 */
	for (i = 0; i < 32; i++) {
#else
	for (;;) {
#endif
		GCObject *obj;
		SgenDescriptor desc;

#ifdef SGEN_SIMPLE_PAR_NURSERY
		GRAY_OBJECT_DEQUEUE_PARALLEL (queue, &obj, &desc);
#else
		GRAY_OBJECT_DEQUEUE_SERIAL (queue, &obj, &desc);
#endif
		if (!obj)
			return TRUE;

		SERIAL_SCAN_OBJECT (obj, desc, queue);
	}

	return FALSE;
}

#define FILL_MINOR_COLLECTOR_SCAN_OBJECT(ops)	do {			\
		(ops)->scan_object = SERIAL_SCAN_OBJECT;			\
		(ops)->scan_vtype = SERIAL_SCAN_VTYPE;			\
		(ops)->scan_ptr_field = SERIAL_SCAN_PTR_FIELD;		\
		(ops)->drain_gray_stack = SERIAL_DRAIN_GRAY_STACK;	\
	} while (0)
