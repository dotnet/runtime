/*
 * sgen-bridge.c: Simple generational GC.
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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

#include <stdlib.h>

#include "sgen-gc.h"
#include "sgen-bridge.h"
#include "sgen-hash-table.h"
#include "sgen-qsort.h"
#include "tabledefs.h"
#include "utils/mono-logger-internal.h"
#include "utils/mono-time.h"
#include "utils/mono-compiler.h"


typedef struct {
	int size;
	int capacity;
	char *data;
} DynArray;

/*Specializations*/

typedef struct {
	DynArray array;
} DynIntArray;

typedef struct {
	DynArray array;
} DynPtrArray;


/* private */

static void
dyn_array_init (DynArray *da)
{
	da->size = 0;
	da->capacity = 0;
	da->data = NULL;
}

static void
dyn_array_uninit (DynArray *da, int elem_size)
{
	if (da->capacity <= 0)
		return;

	sgen_free_internal_dynamic (da->data, elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = NULL;
}

static void
dyn_array_ensure_capacity (DynArray *da, int capacity, int elem_size)
{
	int old_capacity = da->capacity;
	char *new_data;

	if (capacity <= old_capacity)
		return;

	if (da->capacity == 0)
		da->capacity = 2;
	while (capacity > da->capacity)
		da->capacity *= 2;

	new_data = sgen_alloc_internal_dynamic (elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA, TRUE);
	memcpy (new_data, da->data, elem_size * da->size);
	sgen_free_internal_dynamic (da->data, elem_size * old_capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = new_data;
}

static void*
dyn_array_add (DynArray *da, int elem_size)
{
	void *p;

	dyn_array_ensure_capacity (da, da->size + 1, elem_size);

	p = da->data + da->size * elem_size;
	++da->size;
	return p;
}

/* int */
static void
dyn_array_int_init (DynIntArray *da)
{
	dyn_array_init (&da->array);
}

static void
dyn_array_int_uninit (DynIntArray *da)
{
	dyn_array_uninit (&da->array, sizeof (int));
}

static int
dyn_array_int_size (DynIntArray *da)
{
	return da->array.size;
}

static void
dyn_array_int_set_size (DynIntArray *da, int size)
{
	da->array.size = size;
}

static void
dyn_array_int_add (DynIntArray *da, int x)
{
	int *p = dyn_array_add (&da->array, sizeof (int));
	*p = x;
}

static int
dyn_array_int_get (DynIntArray *da, int x)
{
	return ((int*)da->array.data)[x];
}

static void
dyn_array_int_set (DynIntArray *da, int idx, int val)
{
	((int*)da->array.data)[idx] = val;
}

static void
dyn_array_int_ensure_capacity (DynIntArray *da, int capacity)
{
	dyn_array_ensure_capacity (&da->array, capacity, sizeof (int));
}

static void
dyn_array_int_set_all (DynIntArray *dst, DynIntArray *src)
{
	dyn_array_int_ensure_capacity (dst, src->array.size);
	memcpy (dst->array.data, src->array.data, src->array.size * sizeof (int));
	dst->array.size = src->array.size;
}

/* ptr */

static void
dyn_array_ptr_init (DynPtrArray *da)
{
	dyn_array_init (&da->array);
}

static void
dyn_array_ptr_uninit (DynPtrArray *da)
{
	dyn_array_uninit (&da->array, sizeof (void*));
}

static int
dyn_array_ptr_size (DynPtrArray *da)
{
	return da->array.size;
}

static void
dyn_array_ptr_set_size (DynPtrArray *da, int size)
{
	da->array.size = size;
}

static void*
dyn_array_ptr_get (DynPtrArray *da, int x)
{
	return ((void**)da->array.data)[x];
}

static void
dyn_array_ptr_add (DynPtrArray *da, void *ptr)
{
	void **p = dyn_array_add (&da->array, sizeof (void*));
	*p = ptr;
}

#define dyn_array_ptr_push dyn_array_ptr_add

static void*
dyn_array_ptr_pop (DynPtrArray *da)
{
	void *p;
	int size = da->array.size;
	g_assert (size > 0);
	p = dyn_array_ptr_get (da, size - 1);
	--da->array.size;
	return p;
}

/* Merge code*/

static DynIntArray merge_array;

static gboolean
dyn_array_int_contains (DynIntArray *da, int x)
{
	int i;
	for (i = 0; i < dyn_array_int_size (da); ++i)
		if (dyn_array_int_get (da, i) == x)
			return TRUE;
	return FALSE;
}


static void
dyn_array_int_merge (DynIntArray *dst, DynIntArray *src)
{
	int i, j;

	dyn_array_int_ensure_capacity (&merge_array, dyn_array_int_size (dst) + dyn_array_int_size (src));
	dyn_array_int_set_size (&merge_array, 0);

	for (i = j = 0; i < dyn_array_int_size (dst) || j < dyn_array_int_size (src); ) {
		if (i < dyn_array_int_size (dst) && j < dyn_array_int_size (src)) {
			int a = dyn_array_int_get (dst, i); 
			int b = dyn_array_int_get (src, j); 
			if (a < b) {
				dyn_array_int_add (&merge_array, a);
				++i;
			} else if (a == b) {
				dyn_array_int_add (&merge_array, a);
				++i;
				++j;	
			} else {
				dyn_array_int_add (&merge_array, b);
				++j;
			}
		} else if (i < dyn_array_int_size (dst)) {
			dyn_array_int_add (&merge_array, dyn_array_int_get (dst, i));
			++i;
		} else {
			dyn_array_int_add (&merge_array, dyn_array_int_get (src, j));
			++j;
		}
	}

	if (dyn_array_int_size (&merge_array) > dyn_array_int_size (dst)) {
		dyn_array_int_set_all (dst, &merge_array);
	}
}

static void
dyn_array_int_merge_one (DynIntArray *array, int value)
{
	int i;
	int tmp;
	int size = dyn_array_int_size (array);

	for (i = 0; i < size; ++i) {
		if (dyn_array_int_get (array, i) == value)
			return;
		else if (dyn_array_int_get (array, i) > value)
			break;
	}

	dyn_array_int_ensure_capacity (array, size + 1);

	if (i < size) {
		tmp = dyn_array_int_get (array, i);
		for (; i < size; ++i) {
			dyn_array_int_set (array, i, value);
			value = tmp;
			tmp = dyn_array_int_get (array, i + 1);
		}
		dyn_array_int_set (array, size, value);
	} else {
		dyn_array_int_set (array, size, value);
	}

	dyn_array_int_set_size (array, size + 1);
}


static void
enable_accounting (void)
{
	// bridge_accounting_enabled = TRUE;
	// hash_table = (SgenHashTable)SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_HASH_TABLE, INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY, sizeof (HashEntryWithAccounting), mono_aligned_addr_hash, NULL);
}

static MonoGCBridgeObjectKind
class_kind (MonoClass *class)
{
	MonoGCBridgeObjectKind res = bridge_callbacks.bridge_class_kind (class);

	/* If it's a bridge, nothing we can do about it. */
	if (res == GC_BRIDGE_TRANSPARENT_BRIDGE_CLASS || res == GC_BRIDGE_OPAQUE_BRIDGE_CLASS)
		return res;

	/* Non bridge classes with no pointers will never point to a bridge, so we can savely ignore them. */
	if (!class->has_references) {
		SGEN_LOG (6, "class %s is opaque\n", class->name);
		return GC_BRIDGE_OPAQUE_CLASS;
	}

	/* Some arrays can be ignored */
	if (class->rank == 1) {
		MonoClass *elem_class = class->element_class;

		/* FIXME the bridge check can be quite expensive, cache it at the class level. */
		/* An array of a sealed type that is not a bridge will never get to a bridge */
		if ((elem_class->flags & TYPE_ATTRIBUTE_SEALED) && !elem_class->has_references && !bridge_callbacks.bridge_class_kind (elem_class)) {
			SGEN_LOG (6, "class %s is opaque\n", class->name);
			return GC_BRIDGE_OPAQUE_CLASS;
		}
	}

	return GC_BRIDGE_TRANSPARENT_CLASS;
}


typedef struct {
	MonoObject *obj;

	int index;
	int low_index;
	int color;

	gboolean on_loop_stack : 1;
	gboolean is_marked : 1;
	gboolean is_bridge : 1;
} ScanData;

typedef struct {
	DynIntArray other_colors;
} ColorData;

static SgenHashTable hash_table = SGEN_HASH_TABLE_INIT (INTERNAL_MEM_BRIDGE_HASH_TABLE, INTERNAL_MEM_BRIDGE_HASH_TABLE_ENTRY, sizeof (ScanData), mono_aligned_addr_hash, NULL);

static DynPtrArray scan_stack, loop_stack, registered_bridges, color_table;
static DynIntArray low_color;
gboolean found_bridge;

static int ignored_objects;
static int object_index;


static int
new_color (gboolean force_new)
{
	ColorData *cd;
	/* XXX Try to find an equal one and return it */
	if (!force_new) {
		
	}

	cd = sgen_alloc_internal_dynamic (sizeof (ColorData), INTERNAL_MEM_BRIDGE_DATA, TRUE);
	dyn_array_int_set_all (&cd->other_colors, &low_color);

	dyn_array_ptr_add (&color_table, cd);

	return dyn_array_ptr_size (&color_table);
}


static ScanData*
get_scan_data (MonoObject *obj)
{
	ScanData *entry = sgen_hash_table_lookup (&hash_table, obj);
	ScanData new_entry;

	if (entry)
		return entry;

	memset (&new_entry, 0, sizeof (ScanData));

	new_entry.obj = obj;
	new_entry.index = new_entry.low_index = new_entry.color = -1;

	sgen_hash_table_replace (&hash_table, obj, &new_entry, NULL);

	return sgen_hash_table_lookup (&hash_table, obj);
}


static void
register_bridge_object (MonoObject *obj)
{
	get_scan_data (obj)->is_bridge = TRUE;
}

static gboolean
is_opaque_object (MonoObject *obj)
{
	if ((obj->vtable->gc_bits & SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT) == SGEN_GC_BIT_BRIDGE_OPAQUE_OBJECT) {
		SGEN_LOG (6, "ignoring %s\n", obj->vtable->klass->name);
		++ignored_objects;
		return TRUE;
	}
	return FALSE;
}

static void
push_object (MonoObject *obj)
{
	ScanData *data;
	MonoObject *fwd = SGEN_OBJECT_IS_FORWARDED (obj);
	if (fwd)
		obj = fwd;

	printf ("\t= pushing %p %s -> ", obj, sgen_safe_name (obj));
	/* Object types we can ignore */
	if (is_opaque_object (obj)) {
		printf ("opaque\n");
		return;
	}

	data = sgen_hash_table_lookup (&hash_table, obj);

	/* Already marked - XXX must be done this way as the bridge themselves are alive. */
	if (data && data->is_marked) {
		printf ("already marked\n");
		return;
	}

	/* We only care about dead objects */
	if (!data && sgen_object_is_live (obj)) {
		printf ("alive\n");
		return;
	}

	data = get_scan_data (obj);
	data->is_marked = TRUE;
	dyn_array_ptr_push (&scan_stack, data);
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		MonoObject *dst = (MonoObject*)*(ptr);			\
		if (dst) push_object (dst); 			\
	} while (0)

static void
push_all (ScanData *data)
{
	MonoObject *obj = data->obj;
	char *start = (char*)obj;

	printf ("**scanning %p %s\n", obj, sgen_safe_name (obj));
	#include "sgen-scan-object.h"
}


static void
compute_low_index (ScanData *data, MonoObject *obj)
{
	ScanData *other;
	MonoObject *fwd = SGEN_OBJECT_IS_FORWARDED (obj);
	if (fwd)
		obj = fwd;

	other = sgen_hash_table_lookup (&hash_table, obj);

	printf ("\tcompute low %p ->%p (%s) %p\n", data->obj, obj, sgen_safe_name (obj), other);
	if (!other)
		return;

	g_assert (other->is_marked);
	g_assert (other->low_index != -1);

	if (other->on_loop_stack && data->low_index > other->low_index)
		data->low_index = other->low_index;

	/* Compute the low color */
	if (other->color != -1)
		dyn_array_int_merge_one (&low_color, other->color);
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		MonoObject *dst = (MonoObject*)*(ptr);			\
		if (dst) compute_low_index (data, dst); 			\
	} while (0)

static void
compute_low (ScanData *data)
{
	MonoObject *obj = data->obj;
	char *start = (char*)obj;

	#include "sgen-scan-object.h"
}

static int
reduce_color (void)
{
	int color;
	int size = dyn_array_int_size (&low_color);

	if (size == 0)
		color = -1;
	else if (size == 1)
		color = dyn_array_int_get (&low_color, 0);
	else
		color = new_color (FALSE);

	return color;
}

static void
dfs (void)
{
	g_assert (dyn_array_ptr_size (&scan_stack) == 1);
	g_assert (dyn_array_ptr_size (&loop_stack) == 0);

	dyn_array_int_set_size (&low_color, 0);

	while (dyn_array_ptr_size (&scan_stack) > 0) {
		ScanData *data = dyn_array_ptr_pop (&scan_stack);
		g_assert (data->is_marked);
		/*
		 * If the object is not on the loop stack, flag it and push its children .
		 * Otherwise finish it computing loop info, scc and colors.
		 */
		if (!data->on_loop_stack) {
			g_assert (data->index == -1);
			g_assert (data->low_index == -1);

			data->on_loop_stack = TRUE;
			dyn_array_ptr_push (&scan_stack, data);
			dyn_array_ptr_push (&loop_stack, data);
			data->low_index = data->index = object_index++;			
			printf ("+scanning %s (%p) index %d color %d\n", sgen_safe_name (data->obj), data->obj, data->index, data->color);

			/*push all refs */
			push_all (data);
		} else {

			/* Compute low index */
			compute_low (data);
			printf ("-finished %s (%p) index %d low-index %d color %d\n", sgen_safe_name (data->obj), data->obj, data->index, data->low_index, data->color);
			found_bridge |= data->is_bridge;

			//SCC root
			if (data->index == data->low_index) {
				int i, color;
				gboolean found = FALSE;

				printf ("|SCC rooted in %s (%p) has bridge %d\n", sgen_safe_name (data->obj), data->obj, found_bridge);
				printf ("\tpoints-to-colors: ");
				for (i = 0; i < dyn_array_int_size (&low_color); ++i)
					printf ("%d ", dyn_array_int_get (&low_color, i));
				printf ("\n");

				if (found_bridge)
					color = new_color (TRUE);
				else
					color = reduce_color ();

				while (dyn_array_ptr_size (&loop_stack) > 0) {
					ScanData *other = dyn_array_ptr_pop (&loop_stack);
					other->color = color;
					other->on_loop_stack = FALSE;

					printf ("\tmember %s (%p) index %d low-index %d color %d\n", sgen_safe_name (other->obj), other->obj, other->index, other->low_index, other->color);

					if (other == data) {
						found = TRUE;
						break;
					}
				}
				g_assert (found);

				dyn_array_int_set_size (&low_color, 0);
				found_bridge = FALSE;
			}
		}
	}
}

static void
register_finalized_object (MonoObject *obj)
{
	g_assert (sgen_need_bridge_processing ());
	dyn_array_ptr_push (&registered_bridges, obj);
}

static void
reset_data (void)
{
	dyn_array_ptr_set_size (&registered_bridges, 0);
}

static void
processing_stw_step (void)
{
	int i;
	int bridge_count;

	if (!dyn_array_ptr_size (&registered_bridges))
		return;
	printf ("-----------------\n");
	/*
	 * bridge_processing_in_progress must be set with the world
	 * stopped.  If not there would be race conditions.
	 */
	bridge_processing_in_progress = TRUE;
	dyn_array_ptr_set_size (&scan_stack, 0);
	dyn_array_ptr_set_size (&loop_stack, 0);
	sgen_hash_table_clean (&hash_table);
	object_index = 0;

	//XXX HACK free the real thing
	dyn_array_ptr_set_size (&color_table, 0);
	

	bridge_count = dyn_array_ptr_size (&registered_bridges);
	for (i = 0; i < bridge_count ; ++i)
		register_bridge_object (dyn_array_ptr_get (&registered_bridges, i));

	for (i = 0; i < bridge_count; ++i) {
		ScanData *sd = get_scan_data (dyn_array_ptr_get (&registered_bridges, i));
		if (!sd->is_marked) {
			sd->is_marked = TRUE;
			dyn_array_ptr_push (&scan_stack, sd);
			dfs ();
		}
	}
	printf ("----summary----\n");
	printf ("bridges:\n");
	for (i = 0; i < bridge_count; ++i) {
		ScanData *sd = get_scan_data (dyn_array_ptr_get (&registered_bridges, i));
		printf ("\t%s (%p) index %d color %d\n", sgen_safe_name (sd->obj), sd->obj, sd->index, sd->color);
	}

	printf ("colors:\n");
	for (i = 0; i <  dyn_array_ptr_size (&color_table); ++i) {
		int j;
		ColorData *cd = dyn_array_ptr_get (&color_table, i);
		printf ("\t%d: ", i + 1);
		for (j = 0; j < dyn_array_int_size (&cd->other_colors); ++j) {
			printf ("%d ", dyn_array_int_get (&cd->other_colors, j));
		}
		printf ("\n");
	}
}

static void
processing_finish (int generation)
{

	if (!dyn_array_ptr_size (&registered_bridges))
		return;

	bridge_processing_in_progress = FALSE;
}

static void
describe_pointer (MonoObject *obj)
{
	// HashEntry *entry;
	int i;

	for (i = 0; i < dyn_array_ptr_size (&registered_bridges); ++i) {
		if (obj == dyn_array_ptr_get (&registered_bridges, i)) {
			printf ("Pointer is a registered bridge object.\n");
			break;
		}
	}

	// entry = sgen_hash_table_lookup (&hash_table, obj);
	// if (!entry)
	// 	return;
	// 
	// printf ("Bridge hash table entry %p:\n", entry);
	// printf ("  is bridge: %d\n", (int)entry->is_bridge);
	// printf ("  is visited: %d\n", (int)entry->is_visited);
}

void
sgen_new_bridge_init (SgenBridgeProcessor *collector)
{
	collector->reset_data = reset_data;
	collector->processing_stw_step = processing_stw_step;
	collector->processing_finish = processing_finish;
	collector->class_kind = class_kind;
	collector->register_finalized_object = register_finalized_object;
	collector->describe_pointer = describe_pointer;
	collector->enable_accounting = enable_accounting;
}

#endif
