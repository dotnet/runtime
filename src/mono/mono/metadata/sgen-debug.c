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
#include "metadata/sgen-protocol.h"
#include "metadata/sgen-memory-governor.h"
#include "metadata/sgen-pinning.h"

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
	"INVALID",
	"run_length",
	"small_bitmap",
	"complex",
	"vector",
	"large_bitmap",
	"complex_arr",
	"complex_ptrfree"
};

static char* describe_nursery_ptr (char *ptr, gboolean need_setup);

static void
describe_pointer (char *ptr, gboolean need_setup)
{
	MonoVTable *vtable;
	mword desc;
	int type;
	char *start;
	char *forwarded;
	mword size;

 restart:
	if (sgen_ptr_in_nursery (ptr)) {
		start = describe_nursery_ptr (ptr, need_setup);
		if (!start)
			return;
		ptr = start;
	} else {
		if (sgen_ptr_is_in_los (ptr, &start)) {
			if (ptr == start)
				printf ("Pointer is the start of object %p in LOS space.\n", start);
			else
				printf ("Pointer is at offset 0x%x of object %p in LOS space.\n", (int)(ptr - start), start);
			ptr = start;
			mono_sgen_los_describe_pointer (ptr);
		} else if (major_collector.ptr_is_in_non_pinned_space (ptr, &start)) {
			if (ptr == start)
				printf ("Pointer is the start of object %p in oldspace.\n", start);
			else if (start)
				printf ("Pointer is at offset 0x%x of object %p in oldspace.\n", (int)(ptr - start), start);
			else
				printf ("Pointer inside oldspace.\n");
			if (start)
				ptr = start;
			major_collector.describe_pointer (ptr);
		} else if (major_collector.obj_is_from_pinned_alloc (ptr)) {
			printf ("Pointer is inside a pinned chunk.\n");
		} else {
			printf ("Pointer unknown.\n");
			return;
		}
	}

	if (object_is_pinned (ptr))
		printf ("Object is pinned.\n");

	if ((forwarded = object_is_forwarded (ptr))) {
		printf ("Object is forwarded to %p:\n", forwarded);
		ptr = forwarded;
		goto restart;
	}

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

	size = sgen_safe_object_get_size ((MonoObject*)ptr);
	printf ("Size: %td\n", size);
}

void
describe_ptr (char *ptr)
{
	describe_pointer (ptr, TRUE);
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
			SGEN_LOG (0, "Oldspace->newspace reference %p at offset %td in object %p (%s.%s) not found in remsets.", *(ptr), (char*)(ptr) - (char*)(obj), (obj), ((MonoObject*)(obj))->vtable->klass->name_space, ((MonoObject*)(obj))->vtable->klass->name); \
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

static gboolean
is_major_or_los_object_marked (char *obj)
{
	if (sgen_safe_object_get_size ((MonoObject*)obj) > SGEN_MAX_SMALL_OBJ_SIZE) {
		return sgen_los_object_is_pinned (obj);
	} else {
		return sgen_get_major_collector ()->is_object_live (obj);
	}
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
	if (*(ptr) && !sgen_ptr_in_nursery ((char*)*(ptr)) && !is_major_or_los_object_marked ((char*)*(ptr))) { \
		if (!sgen_get_remset ()->find_address_with_cards (start, cards, (char*)(ptr))) { \
			SGEN_LOG (0, "major->major reference %p at offset %td in object %p (%s.%s) not found in remsets.", *(ptr), (char*)(ptr) - (char*)(obj), (obj), ((MonoObject*)(obj))->vtable->klass->name_space, ((MonoObject*)(obj))->vtable->klass->name); \
			binary_protocol_missing_remset ((obj), (gpointer)LOAD_VTABLE ((obj)), (char*)(ptr) - (char*)(obj), *(ptr), (gpointer)LOAD_VTABLE(*(ptr)), object_is_pinned (*(ptr))); \
		}																\
	}																	\
	} while (0)

static void
check_mod_union_callback (char *start, size_t size, void *dummy)
{
	gboolean in_los = (gboolean) (size_t) dummy;
	GCVTable *vt = (GCVTable*)LOAD_VTABLE (start);
	guint8 *cards;
	SGEN_LOG (8, "Scanning object %p, vtable: %p (%s)", start, vt, vt->klass->name);

	if (!is_major_or_los_object_marked (start))
		return;

	if (in_los)
		cards = sgen_los_header_for_object (start)->cardtable_mod_union;
	else
		cards = sgen_get_major_collector ()->get_cardtable_mod_union_for_object (start);

	SGEN_ASSERT (0, cards, "we must have mod union for marked major objects");

#include "sgen-scan-object.h"
}

void
sgen_check_mod_union_consistency (void)
{
	missing_remsets = FALSE;

	major_collector.iterate_objects (TRUE, TRUE, (IterateObjectCallbackFunc)check_mod_union_callback, (void*)FALSE);

	sgen_los_iterate_objects ((IterateObjectCallbackFunc)check_mod_union_callback, (void*)TRUE);

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

static void
setup_valid_nursery_objects (void)
{
	if (!valid_nursery_objects)
		valid_nursery_objects = sgen_alloc_os_memory (DEFAULT_NURSERY_SIZE, SGEN_ALLOC_INTERNAL | SGEN_ALLOC_ACTIVATE, "debugging data");
	valid_nursery_object_count = 0;
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data, setup_mono_sgen_scan_area_with_callback, NULL, FALSE);
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

static char*
describe_nursery_ptr (char *ptr, gboolean need_setup)
{
	int i;

	if (need_setup)
		setup_valid_nursery_objects ();

	for (i = 0; i < valid_nursery_object_count; ++i) {
		if (valid_nursery_objects [i] >= ptr)
			break;
	}

	if (i >= valid_nursery_object_count || valid_nursery_objects [i] + safe_object_get_size ((MonoObject *)valid_nursery_objects [i]) < ptr) {
		SGEN_LOG (0, "nursery-ptr (unalloc'd-memory)\n");
		return NULL;
	} else {
		char *obj = valid_nursery_objects [i];
		if (obj == ptr)
			SGEN_LOG (0, "nursery-ptr\n");
		else
			SGEN_LOG (0, "nursery-ptr (interior-ptr offset %td)\n", ptr - obj);
		return obj;
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
bad_pointer_spew (char *obj, char **slot)
{
	char *ptr = *slot;
	MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);

	SGEN_LOG (0, "Invalid object pointer %p at offset %td in object %p (%s.%s):", ptr,
		(char*)slot - obj,
		obj, vtable->klass->name_space, vtable->klass->name);
	describe_pointer (ptr, FALSE);
	broken_heap = TRUE;
}

static void
missing_remset_spew (char *obj, char **slot)
{
	char *ptr = *slot;
	MonoVTable *vtable = (MonoVTable*)LOAD_VTABLE (obj);

	SGEN_LOG (0, "Oldspace->newspace reference %p at offset %td in object %p (%s.%s) not found in remsets.",
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
				if (!sgen_get_remset ()->find_address ((char*)(ptr)) && !sgen_cement_lookup ((char*)*(ptr)) && (!allow_missing_pinned || !SGEN_OBJECT_IS_PINNED ((char*)*(ptr)))) \
			        missing_remset_spew ((char*)obj, (char**)ptr);	\
			}	\
        } \
	} while (0)

static void
verify_object_pointers_callback (char *start, size_t size, void *data)
{
	gboolean allow_missing_pinned = (gboolean) (size_t) data;

#include "sgen-scan-object.h"
}

/*
FIXME:
-This heap checker is racy regarding inlined write barriers and other JIT tricks that
depend on OP_DUMMY_USE.
*/
void
sgen_check_whole_heap (gboolean allow_missing_pinned)
{
	setup_valid_nursery_objects ();

	broken_heap = FALSE;
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data, verify_object_pointers_callback, (void*) (size_t) allow_missing_pinned, FALSE);
	major_collector.iterate_objects (TRUE, TRUE, verify_object_pointers_callback, (void*) (size_t) allow_missing_pinned);
	sgen_los_iterate_objects (verify_object_pointers_callback, (void*) (size_t) allow_missing_pinned);

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
				SGEN_LOG (0, "Object %p referenced in thread %p (id %p) at %p, stack: %p-%p", obj, info, (gpointer)mono_thread_info_get_tid (info), start, info->stack_start, info->stack_end);
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
				SGEN_LOG (0, "Object %p referenced in saved reg %d of thread %p (id %p)", obj, j, info, (gpointer)mono_thread_info_get_tid (info));
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
					SGEN_LOG (0, "Object %p referenced in pinned roots %p-%p\n", obj, start, root->end_root);
				}
				start++;
			}
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	find_pinning_ref_from_thread (obj, size);
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		char* __target = *(char**)ptr;				\
		if (__target) {						\
			g_assert (is_valid_object_pointer (__target));	\
			if (sgen_ptr_in_nursery (__target)) {		\
				g_assert (SGEN_OBJECT_IS_PINNED (__target)); \
			} else if (sgen_los_is_valid_object (__target)) { \
				g_assert (sgen_los_object_is_pinned (__target)); \
			} else if (major_collector.is_valid_object (__target)) { \
				g_assert (major_collector.is_object_live (__target)); \
			} else {					\
				g_assert_not_reached ();		\
			}						\
		}							\
	} while (0)

static void
check_marked_callback (char *start, size_t size, void *dummy)
{
	gboolean is_los = (gboolean) (size_t) dummy;

	if (is_los) {
		if (!sgen_los_object_is_pinned (start))
			return;
	} else {
		if (!major_collector.is_object_live (start))
			return;
	}

#include "sgen-scan-object.h"
}

void
sgen_check_major_heap_marked (void)
{
	setup_valid_nursery_objects ();

	major_collector.iterate_objects (TRUE, TRUE, check_marked_callback, (void*)FALSE);
	sgen_los_iterate_objects (check_marked_callback, (void*)TRUE);
}

static void
check_nursery_objects_pinned_callback (char *obj, size_t size, void *data /* ScanCopyContext *ctx */)
{
	gboolean pinned = (gboolean) (size_t) data;

	g_assert (!SGEN_OBJECT_IS_FORWARDED (obj));
	if (pinned)
		g_assert (SGEN_OBJECT_IS_PINNED (obj));
	else
		g_assert (!SGEN_OBJECT_IS_PINNED (obj));
}

void
sgen_check_nursery_objects_pinned (gboolean pinned)
{
	sgen_clear_nursery_fragments ();
	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)check_nursery_objects_pinned_callback, (void*) (size_t) pinned /* (void*)&ctx */, FALSE);
}

#endif /*HAVE_SGEN_GC*/
