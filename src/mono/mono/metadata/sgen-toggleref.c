/*
 *
 * sgen-toggleref.c: toggleref support for sgen
 *
 * Copyright 2011 Xamarin, Inc.
 *
 * Author:
 *  Rodrigo Kumpera (kumpera@gmail.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */


#include "config.h"

#ifdef HAVE_SGEN_GC

#include "sgen-gc.h"
#include "sgen-toggleref.h"


/*only one of the two can be non null at a given time*/
typedef struct {
	void *strong_ref;
	void *weak_ref;
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

	DEBUG (4, fprintf (gc_debug_file, "Proccessing ToggleRefs %d\n", toggleref_array_size));

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

	DEBUG (4, fprintf (gc_debug_file, "Done Proccessing ToggleRefs dropped %d strong %d weak %d final size %d\n",
		toggle_ref_counts [MONO_TOGGLE_REF_DROP],
		toggle_ref_counts [MONO_TOGGLE_REF_STRONG],
		toggle_ref_counts [MONO_TOGGLE_REF_WEAK],
		w));
}

void
sgen_scan_togglerefs (CopyOrMarkObjectFunc copy_func, char *start, char *end, SgenGrayQueue *queue)
{
	int i;

	DEBUG (4, fprintf (gc_debug_file, "Scanning ToggleRefs %d\n", toggleref_array_size));

	for (i = 0; i < toggleref_array_size; ++i) {
		if (toggleref_array [i].strong_ref) {
			char *object = toggleref_array [i].strong_ref;
			if (object >= start && object < end) {
				DEBUG (6, fprintf (gc_debug_file, "\tcopying strong slot %d\n", i));
				copy_func (&toggleref_array [i].strong_ref, queue);
			}
		} else if (toggleref_array [i].weak_ref) {
			char *object = toggleref_array [i].weak_ref;

			if (object >= start && object < end) {
				if (sgen_gc_is_object_ready_for_finalization (object)) {
					DEBUG (6, fprintf (gc_debug_file, "\tcleaning weak slot %d\n", i));
					toggleref_array [i].weak_ref = NULL; /* We defer compaction to only happen on the callback step. */
				} else {
					DEBUG (6, fprintf (gc_debug_file, "\tkeeping weak slot %d\n", i));
					copy_func (&toggleref_array [i].weak_ref, queue);
				}
			}
		}
	}
}

static void
ensure_toggleref_capacity (int capacity)
{
	if (!toggleref_array) {
		toggleref_array_capacity = 32;
		toggleref_array = sgen_alloc_internal_dynamic (
			toggleref_array_capacity * sizeof (MonoGCToggleRef),
			INTERNAL_MEM_TOGGLEREF_DATA);	
	}
	if (toggleref_array_size + capacity >= toggleref_array_capacity) {
		MonoGCToggleRef *tmp;
		int old_capacity = toggleref_array_capacity;
		while (toggleref_array_capacity < toggleref_array_size + capacity)
			toggleref_array_size *= 2;

		tmp = sgen_alloc_internal_dynamic (
			toggleref_array_capacity * sizeof (MonoGCToggleRef),
			INTERNAL_MEM_TOGGLEREF_DATA);

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

	DEBUG (4, fprintf (gc_debug_file, "Adding toggleref %p %d\n", object, strong_ref));

	sgen_gc_lock ();

	ensure_toggleref_capacity (1);
	toggleref_array [toggleref_array_size].strong_ref = strong_ref ? object : NULL;
	toggleref_array [toggleref_array_size].weak_ref = strong_ref ? NULL : object;
	++toggleref_array_size;

	sgen_gc_unlock ();
}

/**
 * mono_gc_toggleref_register_callback:
 * @callback callback used to determine the new state of the given object.
 *
 * The callback must decide the status of a given object. It must return one of the values in the MONO_TOGGLE_REF_ enum.
 * This function is called with the world running but with the GC locked. This means that you can do everything that doesn't
 * require GC interaction. This includes, but not limited to, allocating objects, (de)registering for finalization, manipulating
 *gchandles, storing to reference fields or interacting with other threads that might perform such operations.
 */
void
mono_gc_toggleref_register_callback (MonoToggleRefStatus (*proccess_toggleref) (MonoObject *obj))
{
	toggleref_callback = proccess_toggleref;
}

#endif
