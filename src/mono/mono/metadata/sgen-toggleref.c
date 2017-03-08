/**
 * \file
 * toggleref support for sgen
 *
 * Author:
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2011 Xamarin, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"
#include "sgen-toggleref.h"
#include "sgen/sgen-client.h"


/*only one of the two can be non null at a given time*/
typedef struct {
	GCObject *strong_ref;
	GCObject *weak_ref;
} MonoGCToggleRef;

static MonoToggleRefStatus (*toggleref_callback) (MonoObject *obj);
static MonoGCToggleRef *toggleref_array;
static int toggleref_array_size;
static int toggleref_array_capacity;

void
sgen_process_togglerefs (void)
{
	int i, w;
	int toggle_ref_counts [3] = { 0, 0, 0 };

	SGEN_LOG (4, "Proccessing ToggleRefs %d", toggleref_array_size);

	for (i = w = 0; i < toggleref_array_size; ++i) {
		int res;
		MonoGCToggleRef r = toggleref_array [i];

		MonoObject *obj;

		if (r.strong_ref)
			obj = r.strong_ref;
		else if (r.weak_ref)
			obj = r.weak_ref;
		else
			continue;

		res = toggleref_callback (obj);
		++toggle_ref_counts [res];
		switch (res) {
		case MONO_TOGGLE_REF_DROP:
			break;
		case MONO_TOGGLE_REF_STRONG:
			toggleref_array [w].strong_ref = obj;
			toggleref_array [w].weak_ref = NULL;
			++w;
			break;
		case MONO_TOGGLE_REF_WEAK:
			toggleref_array [w].strong_ref = NULL;
			toggleref_array [w].weak_ref = obj;
			++w;
			break;
		default:
			g_assert_not_reached ();
		}
	}

	toggleref_array_size = w;

	SGEN_LOG (4, "Done Proccessing ToggleRefs dropped %d strong %d weak %d final size %d",
		toggle_ref_counts [MONO_TOGGLE_REF_DROP],
		toggle_ref_counts [MONO_TOGGLE_REF_STRONG],
		toggle_ref_counts [MONO_TOGGLE_REF_WEAK],
		w);
}

void sgen_client_mark_togglerefs (char *start, char *end, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	SgenGrayQueue *queue = ctx.queue;
	int i;

	SGEN_LOG (4, "Marking ToggleRefs %d", toggleref_array_size);

	for (i = 0; i < toggleref_array_size; ++i) {
		if (toggleref_array [i].strong_ref) {
			GCObject *object = toggleref_array [i].strong_ref;
			if ((char*)object >= start && (char*)object < end) {
				SGEN_LOG (6, "\tcopying strong slot %d", i);
				copy_func (&toggleref_array [i].strong_ref, queue);
			}
		}
	}
	sgen_drain_gray_stack (ctx);
}

void sgen_client_clear_togglerefs (char *start, char *end, ScanCopyContext ctx)
{
	CopyOrMarkObjectFunc copy_func = ctx.ops->copy_or_mark_object;
	SgenGrayQueue *queue = ctx.queue;
	int i;

	SGEN_LOG (4, "Clearing ToggleRefs %d", toggleref_array_size);

	for (i = 0; i < toggleref_array_size; ++i) {
		if (toggleref_array [i].weak_ref) {
			GCObject *object = toggleref_array [i].weak_ref;

			if ((char*)object >= start && (char*)object < end) {
				if (sgen_gc_is_object_ready_for_finalization (object)) {
					SGEN_LOG (6, "\tcleaning weak slot %d", i);
					toggleref_array [i].weak_ref = NULL; /* We defer compaction to only happen on the callback step. */
				} else {
					SGEN_LOG (6, "\tkeeping weak slot %d", i);
					copy_func (&toggleref_array [i].weak_ref, queue);
				}
			}
		}
	}
	sgen_drain_gray_stack (ctx);
}

static void
ensure_toggleref_capacity (int capacity)
{
	if (!toggleref_array) {
		toggleref_array_capacity = 32;
		toggleref_array = (MonoGCToggleRef *)sgen_alloc_internal_dynamic (
			toggleref_array_capacity * sizeof (MonoGCToggleRef),
			INTERNAL_MEM_TOGGLEREF_DATA,
			TRUE);
	}
	if (toggleref_array_size + capacity >= toggleref_array_capacity) {
		MonoGCToggleRef *tmp;
		int old_capacity = toggleref_array_capacity;
		while (toggleref_array_capacity < toggleref_array_size + capacity)
			toggleref_array_capacity *= 2;

		tmp = (MonoGCToggleRef *)sgen_alloc_internal_dynamic (
			toggleref_array_capacity * sizeof (MonoGCToggleRef),
			INTERNAL_MEM_TOGGLEREF_DATA,
			TRUE);

		memcpy (tmp, toggleref_array, toggleref_array_size * sizeof (MonoGCToggleRef));

		sgen_free_internal_dynamic (toggleref_array, old_capacity * sizeof (MonoGCToggleRef), INTERNAL_MEM_TOGGLEREF_DATA);
		toggleref_array = tmp;
	}
}

/**
 * mono_gc_toggleref_add:
 * @object object to register for toggleref processing
 * @strong_ref if true the object is registered with a strong ref, a weak one otherwise
 *
 * Register a given object for toggleref processing. It will be stored internally and the toggleref callback will be called
 * on it until it returns MONO_TOGGLE_REF_DROP or is collected.
*/
void
mono_gc_toggleref_add (MonoObject *object, mono_bool strong_ref)
{
	if (!toggleref_callback)
		return;

	SGEN_LOG (4, "Adding toggleref %p %d", object, strong_ref);

	sgen_gc_lock ();

	ensure_toggleref_capacity (1);
	toggleref_array [toggleref_array_size].strong_ref = strong_ref ? object : NULL;
	toggleref_array [toggleref_array_size].weak_ref = strong_ref ? NULL : object;
	++toggleref_array_size;

	sgen_gc_unlock ();
}

/**
 * mono_gc_toggleref_register_callback:
 * \param callback callback used to determine the new state of the given object.
 *
 * The callback must decide the status of a given object. It must return one of the values in the \c MONO_TOGGLE_REF_ enum.
 * This function is called with the world running but with the GC locked. This means that you can do everything that doesn't
 * require GC interaction. This includes, but not limited to, allocating objects, (de)registering for finalization, manipulating
 * gchandles, storing to reference fields or interacting with other threads that might perform such operations.
 */
void
mono_gc_toggleref_register_callback (MonoToggleRefStatus (*proccess_toggleref) (MonoObject *obj))
{
	toggleref_callback = proccess_toggleref;
}

static MonoToggleRefStatus
test_toggleref_callback (MonoObject *obj)
{
	static MonoClassField *mono_toggleref_test_field;
	MonoToggleRefStatus status = MONO_TOGGLE_REF_DROP;

	if (!mono_toggleref_test_field) {
		mono_toggleref_test_field = mono_class_get_field_from_name (mono_object_get_class (obj), "__test");
		g_assert (mono_toggleref_test_field);
	}

	mono_field_get_value (obj, mono_toggleref_test_field, &status);
	printf ("toggleref-cb obj %d\n", status);
	return status;
}

void
sgen_register_test_toggleref_callback (void)
{
	toggleref_callback = test_toggleref_callback;
}

#endif
