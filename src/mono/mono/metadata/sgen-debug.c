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
#include "metadata/threadpool-internals.h"

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
		vtable = (MonoVTable*)LOAD_VTABLE (ptr);
	} else {
		if (sgen_ptr_is_in_los (ptr, &start)) {
			if (ptr == start)
				printf ("Pointer is the start of object %p in LOS space.\n", start);
			else
				printf ("Pointer is at offset 0x%x of object %p in LOS space.\n", (int)(ptr - start), start);
			ptr = start;
			mono_sgen_los_describe_pointer (ptr);
			vtable = (MonoVTable*)LOAD_VTABLE (ptr);
		} else if (major_collector.ptr_is_in_non_pinned_space (ptr, &start)) {
			if (ptr == start)
				printf ("Pointer is the start of object %p in oldspace.\n", start);
			else if (start)
				printf ("Pointer is at offset 0x%x of object %p in oldspace.\n", (int)(ptr - start), start);
			else
				printf ("Pointer inside oldspace.\n");
			if (start)
				ptr = start;
			vtable = major_collector.describe_pointer (ptr);
		} else if (major_collector.obj_is_from_pinned_alloc (ptr)) {
			// FIXME: Handle pointers to the inside of objects
			printf ("Pointer is inside a pinned chunk.\n");
			vtable = (MonoVTable*)LOAD_VTABLE (ptr);
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

	printf ("VTable: %p\n", vtable);
	if (vtable == NULL) {
		printf ("VTable is invalid (empty).\n");
		goto bridge;
	}
	if (sgen_ptr_in_nursery (vtable)) {
		printf ("VTable is invalid (points inside nursery).\n");
		goto bridge;
	}
	printf ("Class: %s\n", vtable->klass->name);

	desc = ((GCVTable*)vtable)->desc;
	printf ("Descriptor: %lx\n", (long)desc);

	type = desc & 0x7;
	printf ("Descriptor type: %d (%s)\n", type, descriptor_types [type]);

	size = sgen_safe_object_get_size ((MonoObject*)ptr);
	printf ("Size: %d\n", (int)size);

 bridge:
	sgen_bridge_describe_pointer ((MonoObject*)ptr);
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
		if (!sgen_get_remset ()->find_address ((char*)(ptr)) && !sgen_cement_lookup (*(ptr))) { \
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
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, (IterateObjectCallbackFunc)check_consistency_callback, NULL);

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
			missing_remsets = TRUE;				\
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

	major_collector.iterate_objects (ITERATE_OBJECTS_ALL, (IterateObjectCallbackFunc)check_mod_union_callback, (void*)FALSE);

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
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, (IterateObjectCallbackFunc)check_major_refs_callback, NULL);
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
	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, verify_object_pointers_callback, (void*) (size_t) allow_missing_pinned);
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

	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, check_marked_callback, (void*)FALSE);
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

static gboolean scan_object_for_specific_ref_precise = TRUE;

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj) do {		\
	if ((MonoObject*)*(ptr) == key) {	\
	g_print ("found ref to %p in object %p (%s) at offset %td\n",	\
			key, (obj), sgen_safe_name ((obj)), ((char*)(ptr) - (char*)(obj))); \
	}								\
	} while (0)

static void
scan_object_for_specific_ref (char *start, MonoObject *key)
{
	char *forwarded;

	if ((forwarded = SGEN_OBJECT_IS_FORWARDED (start)))
		start = forwarded;

	if (scan_object_for_specific_ref_precise) {
		#include "sgen-scan-object.h"
	} else {
		mword *words = (mword*)start;
		size_t size = safe_object_get_size ((MonoObject*)start);
		int i;
		for (i = 0; i < size / sizeof (mword); ++i) {
			if (words [i] == (mword)key) {
				g_print ("found possible ref to %p in object %p (%s) at offset %td\n",
						key, start, sgen_safe_name (start), i * sizeof (mword));
			}
		}
	}
}

static void
scan_object_for_specific_ref_callback (char *obj, size_t size, MonoObject *key)
{
	scan_object_for_specific_ref (obj, key);
}

static void
check_root_obj_specific_ref (RootRecord *root, MonoObject *key, MonoObject *obj)
{
	if (key != obj)
		return;
	g_print ("found ref to %p in root record %p\n", key, root);
}

static MonoObject *check_key = NULL;
static RootRecord *check_root = NULL;

static void
check_root_obj_specific_ref_from_marker (void **obj)
{
	check_root_obj_specific_ref (check_root, check_key, *obj);
}

static void
scan_roots_for_specific_ref (MonoObject *key, int root_type)
{
	void **start_root;
	RootRecord *root;
	check_key = key;

	SGEN_HASH_TABLE_FOREACH (&roots_hash [root_type], start_root, root) {
		mword desc = root->root_desc;

		check_root = root;

		switch (desc & ROOT_DESC_TYPE_MASK) {
		case ROOT_DESC_BITMAP:
			desc >>= ROOT_DESC_TYPE_SHIFT;
			while (desc) {
				if (desc & 1)
					check_root_obj_specific_ref (root, key, *start_root);
				desc >>= 1;
				start_root++;
			}
			return;
		case ROOT_DESC_COMPLEX: {
			gsize *bitmap_data = sgen_get_complex_descriptor_bitmap (desc);
			int bwords = (*bitmap_data) - 1;
			void **start_run = start_root;
			bitmap_data++;
			while (bwords-- > 0) {
				gsize bmap = *bitmap_data++;
				void **objptr = start_run;
				while (bmap) {
					if (bmap & 1)
						check_root_obj_specific_ref (root, key, *objptr);
					bmap >>= 1;
					++objptr;
				}
				start_run += GC_BITS_PER_WORD;
			}
			break;
		}
		case ROOT_DESC_USER: {
			MonoGCRootMarkFunc marker = sgen_get_user_descriptor_func (desc);
			marker (start_root, check_root_obj_specific_ref_from_marker);
			break;
		}
		case ROOT_DESC_RUN_LEN:
			g_assert_not_reached ();
		default:
			g_assert_not_reached ();
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	check_key = NULL;
	check_root = NULL;
}

void
mono_gc_scan_for_specific_ref (MonoObject *key, gboolean precise)
{
	void **ptr;
	RootRecord *root;

	scan_object_for_specific_ref_precise = precise;

	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)scan_object_for_specific_ref_callback, key, TRUE);

	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, (IterateObjectCallbackFunc)scan_object_for_specific_ref_callback, key);

	sgen_los_iterate_objects ((IterateObjectCallbackFunc)scan_object_for_specific_ref_callback, key);

	scan_roots_for_specific_ref (key, ROOT_TYPE_NORMAL);
	scan_roots_for_specific_ref (key, ROOT_TYPE_WBARRIER);

	SGEN_HASH_TABLE_FOREACH (&roots_hash [ROOT_TYPE_PINNED], ptr, root) {
		while (ptr < (void**)root->end_root) {
			check_root_obj_specific_ref (root, *ptr, key);
			++ptr;
		}
	} SGEN_HASH_TABLE_FOREACH_END;
}

static MonoDomain *check_domain = NULL;

static void
check_obj_not_in_domain (void **o)
{
	g_assert (((MonoObject*)(*o))->vtable->domain != check_domain);
}

void
sgen_scan_for_registered_roots_in_domain (MonoDomain *domain, int root_type)
{
	void **start_root;
	RootRecord *root;
	check_domain = domain;
	SGEN_HASH_TABLE_FOREACH (&roots_hash [root_type], start_root, root) {
		mword desc = root->root_desc;

		/* The MonoDomain struct is allowed to hold
		   references to objects in its own domain. */
		if (start_root == (void**)domain)
			continue;

		switch (desc & ROOT_DESC_TYPE_MASK) {
		case ROOT_DESC_BITMAP:
			desc >>= ROOT_DESC_TYPE_SHIFT;
			while (desc) {
				if ((desc & 1) && *start_root)
					check_obj_not_in_domain (*start_root);
				desc >>= 1;
				start_root++;
			}
			break;
		case ROOT_DESC_COMPLEX: {
			gsize *bitmap_data = sgen_get_complex_descriptor_bitmap (desc);
			int bwords = (*bitmap_data) - 1;
			void **start_run = start_root;
			bitmap_data++;
			while (bwords-- > 0) {
				gsize bmap = *bitmap_data++;
				void **objptr = start_run;
				while (bmap) {
					if ((bmap & 1) && *objptr)
						check_obj_not_in_domain (*objptr);
					bmap >>= 1;
					++objptr;
				}
				start_run += GC_BITS_PER_WORD;
			}
			break;
		}
		case ROOT_DESC_USER: {
			MonoGCRootMarkFunc marker = sgen_get_user_descriptor_func (desc);
			marker (start_root, check_obj_not_in_domain);
			break;
		}
		case ROOT_DESC_RUN_LEN:
			g_assert_not_reached ();
		default:
			g_assert_not_reached ();
		}
	} SGEN_HASH_TABLE_FOREACH_END;

	check_domain = NULL;
}

static gboolean
is_xdomain_ref_allowed (gpointer *ptr, char *obj, MonoDomain *domain)
{
	MonoObject *o = (MonoObject*)(obj);
	MonoObject *ref = (MonoObject*)*(ptr);
	int offset = (char*)(ptr) - (char*)o;

	if (o->vtable->klass == mono_defaults.thread_class && offset == G_STRUCT_OFFSET (MonoThread, internal_thread))
		return TRUE;
	if (o->vtable->klass == mono_defaults.internal_thread_class && offset == G_STRUCT_OFFSET (MonoInternalThread, current_appcontext))
		return TRUE;

#ifndef DISABLE_REMOTING
	if (mono_defaults.real_proxy_class->supertypes && mono_class_has_parent_fast (o->vtable->klass, mono_defaults.real_proxy_class) &&
			offset == G_STRUCT_OFFSET (MonoRealProxy, unwrapped_server))
		return TRUE;
#endif
	/* Thread.cached_culture_info */
	if (!strcmp (ref->vtable->klass->name_space, "System.Globalization") &&
			!strcmp (ref->vtable->klass->name, "CultureInfo") &&
			!strcmp(o->vtable->klass->name_space, "System") &&
			!strcmp(o->vtable->klass->name, "Object[]"))
		return TRUE;
	/*
	 *  at System.IO.MemoryStream.InternalConstructor (byte[],int,int,bool,bool) [0x0004d] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.IO/MemoryStream.cs:121
	 * at System.IO.MemoryStream..ctor (byte[]) [0x00017] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.IO/MemoryStream.cs:81
	 * at (wrapper remoting-invoke-with-check) System.IO.MemoryStream..ctor (byte[]) <IL 0x00020, 0xffffffff>
	 * at System.Runtime.Remoting.Messaging.CADMethodCallMessage.GetArguments () [0x0000d] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.Runtime.Remoting.Messaging/CADMessages.cs:327
	 * at System.Runtime.Remoting.Messaging.MethodCall..ctor (System.Runtime.Remoting.Messaging.CADMethodCallMessage) [0x00017] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.Runtime.Remoting.Messaging/MethodCall.cs:87
	 * at System.AppDomain.ProcessMessageInDomain (byte[],System.Runtime.Remoting.Messaging.CADMethodCallMessage,byte[]&,System.Runtime.Remoting.Messaging.CADMethodReturnMessage&) [0x00018] in /home/schani/Work/novell/trunk/mcs/class/corlib/System/AppDomain.cs:1213
	 * at (wrapper remoting-invoke-with-check) System.AppDomain.ProcessMessageInDomain (byte[],System.Runtime.Remoting.Messaging.CADMethodCallMessage,byte[]&,System.Runtime.Remoting.Messaging.CADMethodReturnMessage&) <IL 0x0003d, 0xffffffff>
	 * at System.Runtime.Remoting.Channels.CrossAppDomainSink.ProcessMessageInDomain (byte[],System.Runtime.Remoting.Messaging.CADMethodCallMessage) [0x00008] in /home/schani/Work/novell/trunk/mcs/class/corlib/System.Runtime.Remoting.Channels/CrossAppDomainChannel.cs:198
	 * at (wrapper runtime-invoke) object.runtime_invoke_CrossAppDomainSink/ProcessMessageRes_object_object (object,intptr,intptr,intptr) <IL 0x0004c, 0xffffffff>
	 */
	if (!strcmp (ref->vtable->klass->name_space, "System") &&
			!strcmp (ref->vtable->klass->name, "Byte[]") &&
			!strcmp (o->vtable->klass->name_space, "System.IO") &&
			!strcmp (o->vtable->klass->name, "MemoryStream"))
		return TRUE;
	/* append_job() in threadpool.c */
	if (!strcmp (ref->vtable->klass->name_space, "System.Runtime.Remoting.Messaging") &&
			!strcmp (ref->vtable->klass->name, "AsyncResult") &&
			!strcmp (o->vtable->klass->name_space, "System") &&
			!strcmp (o->vtable->klass->name, "Object[]") &&
			mono_thread_pool_is_queue_array ((MonoArray*) o))
		return TRUE;
	return FALSE;
}

static void
check_reference_for_xdomain (gpointer *ptr, char *obj, MonoDomain *domain)
{
	MonoObject *o = (MonoObject*)(obj);
	MonoObject *ref = (MonoObject*)*(ptr);
	int offset = (char*)(ptr) - (char*)o;
	MonoClass *class;
	MonoClassField *field;
	char *str;

	if (!ref || ref->vtable->domain == domain)
		return;
	if (is_xdomain_ref_allowed (ptr, obj, domain))
		return;

	field = NULL;
	for (class = o->vtable->klass; class; class = class->parent) {
		int i;

		for (i = 0; i < class->field.count; ++i) {
			if (class->fields[i].offset == offset) {
				field = &class->fields[i];
				break;
			}
		}
		if (field)
			break;
	}

	if (ref->vtable->klass == mono_defaults.string_class)
		str = mono_string_to_utf8 ((MonoString*)ref);
	else
		str = NULL;
	g_print ("xdomain reference in %p (%s.%s) at offset %d (%s) to %p (%s.%s) (%s)  -  pointed to by:\n",
			o, o->vtable->klass->name_space, o->vtable->klass->name,
			offset, field ? field->name : "",
			ref, ref->vtable->klass->name_space, ref->vtable->klass->name, str ? str : "");
	mono_gc_scan_for_specific_ref (o, TRUE);
	if (str)
		g_free (str);
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	check_reference_for_xdomain ((ptr), (obj), domain)

static void
scan_object_for_xdomain_refs (char *start, mword size, void *data)
{
	MonoDomain *domain = ((MonoObject*)start)->vtable->domain;

	#include "sgen-scan-object.h"
}

void
sgen_check_for_xdomain_refs (void)
{
	LOSObject *bigobj;

	sgen_scan_area_with_callback (nursery_section->data, nursery_section->end_data,
			(IterateObjectCallbackFunc)scan_object_for_xdomain_refs, NULL, FALSE);

	major_collector.iterate_objects (ITERATE_OBJECTS_SWEEP_ALL, (IterateObjectCallbackFunc)scan_object_for_xdomain_refs, NULL);

	for (bigobj = los_object_list; bigobj; bigobj = bigobj->next)
		scan_object_for_xdomain_refs (bigobj->data, sgen_los_object_size (bigobj), NULL);
}

static int
compare_xrefs (const void *a_ptr, const void *b_ptr)
{
	const MonoGCBridgeXRef *a = a_ptr;
	const MonoGCBridgeXRef *b = b_ptr;

	if (a->src_scc_index < b->src_scc_index)
		return -1;
	if (a->src_scc_index > b->src_scc_index)
		return 1;

	if (a->dst_scc_index < b->dst_scc_index)
		return -1;
	if (a->dst_scc_index > b->dst_scc_index)
		return 1;

	return 0;
}

static void
dump_processor_state (SgenBridgeProcessor *p)
{
	int i;

	printf ("------\n");
	printf ("SCCS %d\n", p->num_sccs);
	for (i = 0; i < p->num_sccs; ++i) {
		int j;
		MonoGCBridgeSCC *scc = p->api_sccs [i];
		printf ("\tSCC %d:", i);
		for (j = 0; j < scc->num_objs; ++j) {
			MonoObject *obj = scc->objs [j];
			printf (" %p", obj);
		}
		printf ("\n");
	}

	printf ("XREFS %d\n", p->num_xrefs);
	for (i = 0; i < p->num_xrefs; ++i)
		printf ("\t%d -> %d\n", p->api_xrefs [i].src_scc_index, p->api_xrefs [i].dst_scc_index);

	printf ("-------\n");
}

gboolean
sgen_compare_bridge_processor_results (SgenBridgeProcessor *a, SgenBridgeProcessor *b)
{
	int i;
	SgenHashTable obj_to_a_scc = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_DEBUG, INTERNAL_MEM_BRIDGE_DEBUG, sizeof (int), mono_aligned_addr_hash, NULL);
	SgenHashTable b_scc_to_a_scc = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_DEBUG, INTERNAL_MEM_BRIDGE_DEBUG, sizeof (int), g_direct_hash, NULL);
	MonoGCBridgeXRef *a_xrefs, *b_xrefs;
	size_t xrefs_alloc_size;

	// dump_processor_state (a);
	// dump_processor_state (b);

	if (a->num_sccs != b->num_sccs)
		g_error ("SCCS count expected %d but got %d", a->num_sccs, b->num_sccs);
	if (a->num_xrefs != b->num_xrefs)
		g_error ("SCCS count expected %d but got %d", a->num_xrefs, b->num_xrefs);

	/*
	 * First we build a hash of each object in `a` to its respective SCC index within
	 * `a`.  Along the way we also assert that no object is more than one SCC.
	 */
	for (i = 0; i < a->num_sccs; ++i) {
		int j;
		MonoGCBridgeSCC *scc = a->api_sccs [i];

		g_assert (scc->num_objs > 0);

		for (j = 0; j < scc->num_objs; ++j) {
			MonoObject *obj = scc->objs [j];
			gboolean new_entry = sgen_hash_table_replace (&obj_to_a_scc, obj, &i, NULL);
			g_assert (new_entry);
		}
	}

	/*
	 * Now we check whether each of the objects in `b` are in `a`, and whether the SCCs
	 * of `b` contain the same sets of objects as those of `a`.
	 *
	 * While we're doing this, build a hash table to map from `b` SCC indexes to `a` SCC
	 * indexes.
	 */
	for (i = 0; i < b->num_sccs; ++i) {
		MonoGCBridgeSCC *scc = b->api_sccs [i];
		MonoGCBridgeSCC *a_scc;
		int *a_scc_index_ptr;
		int a_scc_index;
		int j;
		gboolean new_entry;

		g_assert (scc->num_objs > 0);
		a_scc_index_ptr = sgen_hash_table_lookup (&obj_to_a_scc, scc->objs [0]);
		g_assert (a_scc_index_ptr);
		a_scc_index = *a_scc_index_ptr;

		//g_print ("A SCC %d -> B SCC %d\n", a_scc_index, i);

		a_scc = a->api_sccs [a_scc_index];
		g_assert (a_scc->num_objs == scc->num_objs);

		for (j = 1; j < scc->num_objs; ++j) {
			a_scc_index_ptr = sgen_hash_table_lookup (&obj_to_a_scc, scc->objs [j]);
			g_assert (a_scc_index_ptr);
			g_assert (*a_scc_index_ptr == a_scc_index);
		}

		new_entry = sgen_hash_table_replace (&b_scc_to_a_scc, GINT_TO_POINTER (i), &a_scc_index, NULL);
		g_assert (new_entry);
	}

	/*
	 * Finally, check that we have the same xrefs.  We do this by making copies of both
	 * xref arrays, and replacing the SCC indexes in the copy for `b` with the
	 * corresponding indexes in `a`.  Then we sort both arrays and assert that they're
	 * the same.
	 *
	 * At the same time, check that no xref is self-referential and that there are no
	 * duplicate ones.
	 */

	xrefs_alloc_size = a->num_xrefs * sizeof (MonoGCBridgeXRef);
	a_xrefs = sgen_alloc_internal_dynamic (xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG, TRUE);
	b_xrefs = sgen_alloc_internal_dynamic (xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG, TRUE);

	memcpy (a_xrefs, a->api_xrefs, xrefs_alloc_size);
	for (i = 0; i < b->num_xrefs; ++i) {
		MonoGCBridgeXRef *xref = &b->api_xrefs [i];
		int *scc_index_ptr;

		g_assert (xref->src_scc_index != xref->dst_scc_index);

		scc_index_ptr = sgen_hash_table_lookup (&b_scc_to_a_scc, GINT_TO_POINTER (xref->src_scc_index));
		g_assert (scc_index_ptr);
		b_xrefs [i].src_scc_index = *scc_index_ptr;

		scc_index_ptr = sgen_hash_table_lookup (&b_scc_to_a_scc, GINT_TO_POINTER (xref->dst_scc_index));
		g_assert (scc_index_ptr);
		b_xrefs [i].dst_scc_index = *scc_index_ptr;
	}

	qsort (a_xrefs, a->num_xrefs, sizeof (MonoGCBridgeXRef), compare_xrefs);
	qsort (b_xrefs, a->num_xrefs, sizeof (MonoGCBridgeXRef), compare_xrefs);

	for (i = 0; i < a->num_xrefs; ++i) {
		g_assert (a_xrefs [i].src_scc_index == b_xrefs [i].src_scc_index);
		g_assert (a_xrefs [i].dst_scc_index == b_xrefs [i].dst_scc_index);
	}

	sgen_hash_table_clean (&obj_to_a_scc);
	sgen_hash_table_clean (&b_scc_to_a_scc);
	sgen_free_internal_dynamic (a_xrefs, xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG);
	sgen_free_internal_dynamic (b_xrefs, xrefs_alloc_size, INTERNAL_MEM_BRIDGE_DEBUG);

	return TRUE;
}

#endif /*HAVE_SGEN_GC*/
