/*
 * sgen-debug.c: Collector debugging
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2005-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2011 Xamarin, Inc.
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

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-cardtable.h"
#include "metadata/sgen-ssb.h"
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-memory-governor.h"

#define LOAD_VTABLE	SGEN_LOAD_VTABLE

#define object_is_forwarded	SGEN_OBJECT_IS_FORWARDED
#define object_is_pinned	SGEN_OBJECT_IS_PINNED
#define safe_object_get_size	sgen_safe_object_get_size

void describe_ptr (char *ptr);
void check_object (char *start);

/*
 * ######################################################################
 * ########  Collector debugging
 * ######################################################################
 */

const char*descriptor_types [] = {
	"run_length",
	"small_bitmap",
	"string",
	"complex",
	"vector",
	"array",
	"large_bitmap",
	"complex_arr"
};

void
describe_ptr (char *ptr)
{
	MonoVTable *vtable;
	mword desc;
	int type;
	char *start;

	if (sgen_ptr_in_nursery (ptr)) {
		printf ("Pointer inside nursery.\n");
	} else {
		if (sgen_ptr_is_in_los (ptr, &start)) {
			if (ptr == start)
				printf ("Pointer is the start of object %p in LOS space.\n", start);
			else
				printf ("Pointer is at offset 0x%x of object %p in LOS space.\n", (int)(ptr - start), start);
			ptr = start;
		} else if (major_collector.ptr_is_in_non_pinned_space (ptr, &start)) {
			if (ptr == start)
				printf ("Pointer is the start of object %p in oldspace.\n", start);
			else if (start)
				printf ("Pointer is at offset 0x%x of object %p in oldspace.\n", (int)(ptr - start), start);
			else
				printf ("Pointer inside oldspace.\n");
			if (start)
				ptr = start;
		} else if (major_collector.obj_is_from_pinned_alloc (ptr)) {
			printf ("Pointer is inside a pinned chunk.\n");
		} else {
			printf ("Pointer unknown.\n");
			return;
		}
	}

	if (object_is_pinned (ptr))
		printf ("Object is pinned.\n");

	if (object_is_forwarded (ptr))
		printf ("Object is forwared.\n");

	// FIXME: Handle pointers to the inside of objects
	vtable = (MonoVTable*)LOAD_VTABLE (ptr);

	printf ("VTable: %p\n", vtable);
	if (vtable == NULL) {
		printf ("VTable is invalid (empty).\n");
		return;
	}
	if (sgen_ptr_in_nursery (vtable)) {
		printf ("VTable is invalid (points inside nursery).\n");
		return;
	}
	printf ("Class: %s\n", vtable->klass->name);

	desc = ((GCVTable*)vtable)->desc;
	printf ("Descriptor: %lx\n", (long)desc);

	type = desc & 0x7;
	printf ("Descriptor type: %d (%s)\n", type, descriptor_types [type]);
}

static gboolean missing_remsets;

/*
 * We let a missing remset slide if the target object is pinned,
 * because the store might have happened but the remset not yet added,
 * but in that case the target must be pinned.  We might theoretically
 * miss some missing remsets this way, but it's very unlikely.
 */
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
	if (*(ptr) && sgen_ptr_in_nursery ((char*)*(ptr))) { \
		if (!sgen_get_remset ()->find_address ((char*)(ptr))) { \
			SGEN_LOG (1, "Oldspace->newspace reference %p at offset %td in object %p (%s.%s) not found in remsets.", *(ptr), (char*)(ptr) - (char*)(obj), (obj), ((MonoObject*)(obj))->vtable->klass->name_space, ((MonoObject*)(obj))->vtable->klass->name); \
			binary_protocol_missing_remset ((obj), (gpointer)LOAD_VTABLE ((obj)), (char*)(ptr) - (char*)(obj), *(ptr), (gpointer)LOAD_VTABLE(*(ptr)), object_is_pinned (*(ptr))); \
			if (!object_is_pinned (*(ptr)))								\
				missing_remsets = TRUE;									\
		}																\
	}																	\
	} while (0)

/*
 * Check that each object reference which points into the nursery can
 * be found in the remembered sets.
 */
static void
check_consistency_callback (char *start, size_t size, void *dummy)
{
	GCVTable *vt = (GCVTable*)LOAD_VTABLE (start);
	SGEN_LOG (8, "Scanning object %p, vtable: %p (%s)", start, vt, vt->klass->name);

#define SCAN_OBJECT_ACTION
#include "sgen-scan-object.h"
}

/*
 * Perform consistency check of the heap.
 *
 * Assumes the world is stopped.
 */
void
sgen_check_consistency (void)
{
	// Need to add more checks

	missing_remsets = FALSE;

	SGEN_LOG (1, "Begin heap consistency check...");

	// Check that oldspace->newspace pointers are registered with the collector
	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)check_consistency_callback, NULL);

	sgen_los_iterate_objects ((IterateObjectCallbackFunc)check_consistency_callback, NULL);

	SGEN_LOG (1, "Heap consistency check done.");

	if (!binary_protocol_is_enabled ())
		g_assert (!missing_remsets);
}


#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		if (*(ptr) && !LOAD_VTABLE (*(ptr)))						\
			g_error ("Could not load vtable for obj %p slot %d (size %d)", obj, (char*)ptr - (char*)obj, safe_object_get_size ((MonoObject*)obj));		\
	} while (0)

static void
check_major_refs_callback (char *start, size_t size, void *dummy)
{
#define SCAN_OBJECT_ACTION
#include "sgen-scan-object.h"
}

void
sgen_check_major_refs (void)
{
	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)check_major_refs_callback, NULL);
	sgen_los_iterate_objects ((IterateObjectCallbackFunc)check_major_refs_callback, NULL);
}

/* Check that the reference is valid */
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(ptr)) {	\
			g_assert (sgen_safe_name (*(ptr)) != NULL);	\
		}	\
	} while (0)

/*
 * check_object:
 *
 *   Perform consistency check on an object. Currently we only check that the
 * reference fields are valid.
 */
void
check_object (char *start)
{
	if (!start)
		return;

#include "sgen-scan-object.h"
}


static char **valid_nursery_objects;
static int valid_nursery_object_count;
static gboolean broken_heap;

static void 
setup_mono_sgen_scan_area_with_callback (char *object, size_t size, void *data)
{
	valid_nursery_objects [valid_nursery_object_count++] = object;
}

static gboolean
find_object_in_nursery_dump (char *object)
{
	int first = 0, last = valid_nursery_object_count;
	while (first < last) {
		int middle = first + ((last - first) >> 1);
		if (object == valid_nursery_objects [middle])
			return TRUE;

		if (object < valid_nursery_objects [middle])
			last = middle;
		else
			first = middle + 1;
	}
	g_assert (first == last);
	return FALSE;
}

static void
describe_nursery_ptr (char *ptr)
{
	int i;

	for (i = 0; i < valid_nursery_object_count; ++i) {
		if (valid_nursery_objects [i] >= ptr)
			break;
	}

	if (i >= valid_nursery_object_count || valid_nursery_objects [i] + safe_object_get_size ((MonoObject *)valid_nursery_objects [i]) < ptr) {
		SGEN_LOG (1, "nursery-ptr (unalloc'd-memory)");
	} else {
		char *obj = valid_nursery_objects [i];
		MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);
		int size = safe_object_get_size ((MonoObject *)obj);

		if (obj == ptr)
			SGEN_LOG (1, "nursery-ptr (object %s.%s size %d)", 
				vtable->klass->name_space, vtable->klass->name, size);
		else
			SGEN_LOG (1, "nursery-ptr (interior-ptr offset %td of %p (%s.%s) size %d)",
				ptr - obj, obj,
				vtable->klass->name_space, vtable->klass->name, size);
	}
}

static gboolean
is_valid_object_pointer (char *object)
{
	if (sgen_ptr_in_nursery (object))
		return find_object_in_nursery_dump (object);
	
	if (sgen_los_is_valid_object (object))
		return TRUE;

	if (major_collector.is_valid_object (object))
		return TRUE;
	return FALSE;
}


static void
describe_pointer (char *ptr)
{
	if (sgen_ptr_in_nursery (ptr)) {
		describe_nursery_ptr (ptr);
	} else if (major_collector.describe_pointer (ptr)) {
		//Nothing really
	} else if (!mono_sgen_los_describe_pointer (ptr)) {
		SGEN_LOG (1, "\tnon-heap-ptr");
	}
}

static void
bad_pointer_spew (char *obj, char **slot)
{
	char *ptr = *slot;
	MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);

	SGEN_LOG (1, "Invalid object pointer %p at offset %td in object %p (%s.%s):", ptr,
		(char*)slot - obj,
		obj, vtable->klass->name_space, vtable->klass->name);
	describe_pointer (ptr);
	broken_heap = TRUE;
}

static void
missing_remset_spew (char *obj, char **slot)
{
	char *ptr = *slot;
	MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);

    SGEN_LOG (1, "Oldspace->newspace reference %p at offset %td in object %p (%s.%s) not found in remsets.",
 		ptr, (char*)slot - obj, obj, 
		vtable->klass->name_space, vtable->klass->name);

	broken_heap = TRUE;
}

/*
FIXME Flag missing remsets due to pinning as non fatal
*/
#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		if (*(char**)ptr) {	\
			if (!is_valid_object_pointer (*(char**)ptr)) {	\
				bad_pointer_spew ((char*)obj, (char**)ptr);	\
			} else if (!sgen_ptr_in_nursery (obj) && sgen_ptr_in_nursery ((char*)*ptr)) {	\
				if (!sgen_get_remset ()->find_address ((char*)(ptr))) \
			        missing_remset_spew ((char*)obj, (char**)ptr);	\
			}	\
        } \
	} while (0)

static void
verify_object_pointers_callback (char *start, size_t size, void *dummy)
{
#define SCAN_OBJECT_ACTION
#include "sgen-scan-object.h"
}

/*
FIXME:
-This heap checker is racy regarding inlined write barriers and other JIT tricks that
depend on OP_DUMMY_USE.
*/
void
sgen_check_whole_heap (void)
{
	/*setup valid_nursery_objects*/
	if (!valid_nursery_objects)
		valid_nursery_objects = sgen_alloc_os_memory (DEFAULT_NURSERY_SIZE, SGEN_ALLOC_INTERNAL | SGEN_ALLOC_ACTIVATE, "debugging data");
	valid_nursery_object_count = 0;
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data, setup_mono_sgen_scan_area_with_callback, NULL, FALSE);

	broken_heap = FALSE;
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data, verify_object_pointers_callback, NULL, FALSE);
	major_collector.iterate_objects (TRUE, TRUE, verify_object_pointers_callback, NULL);
	sgen_los_iterate_objects (verify_object_pointers_callback, NULL);	

	g_assert (!broken_heap);
}

static gboolean
ptr_in_heap (char *object)
{
	if (sgen_ptr_in_nursery (object))
		return TRUE;
	
	if (sgen_los_is_valid_object (object))
		return TRUE;

	if (major_collector.is_valid_object (object))
		return TRUE;
	return FALSE;
}

/*
 * sgen_check_objref:
 *   Do consistency checks on the object reference OBJ. Assert on failure.
 */
void
sgen_check_objref (char *obj)
{
	g_assert (ptr_in_heap (obj));
}

static void
find_pinning_ref_from_thread (char *obj, size_t size)
{
	int j;
	SgenThreadInfo *info;
	char *endobj = obj + size;

	FOREACH_THREAD (info) {
		char **start = (char**)info->stack_start;
		if (info->skip)
			continue;
		while (start < (char**)info->stack_end) {
			if (*start >= obj && *start < endobj) {
				SGEN_LOG (1, "Object %p referenced in thread %p (id %p) at %p, stack: %p-%p", obj, info, (gpointer)mono_thread_info_get_tid (info), start, info->stack_start, info->stack_end);
			}
			start++;
		}

		for (j = 0; j < ARCH_NUM_REGS; ++j) {
#ifdef USE_MONO_CTX
			mword w = ((mword*)&info->ctx) [j];
#else
			mword w = (mword)&info->regs [j];
#endif

			if (w >= (mword)obj && w < (mword)obj + size)
				SGEN_LOG (1, "Object %p referenced in saved reg %d of thread %p (id %p)", obj, j, info, (gpointer)mono_thread_info_get_tid (info));
		} END_FOREACH_THREAD
	}
}

/*
 * Debugging function: find in the conservative roots where @obj is being pinned.
 */
static G_GNUC_UNUSED void
find_pinning_reference (char *obj, size_t size)
{
	char **start;
	RootRecord *root;
	char *endobj = obj + size;

	SGEN_HASH_TABLE_FOREACH (&roots_hash [ROOT_TYPE_NORMAL], start, root) {
		/* if desc is non-null it has precise info */
		if (!root->root_desc) {
			while (start < (char**)root->end_root) {
				if (*start >= obj && *start < endobj) {
					SGEN_LOG (1, "Object %p referenced in pinned roots %p-%p\n", obj, start, root->end_root);
				}
				start++;
			}
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	find_pinning_ref_from_thread (obj, size);
}

#endif /*HAVE_SGEN_GC*/
