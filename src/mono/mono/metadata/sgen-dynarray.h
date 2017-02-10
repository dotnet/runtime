/**
 * \file
 * Copyright 2016 Xamarin, Inc.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

 // Growable array implementation used by sgen-new-bridge and sgen-tarjan-bridge.

typedef struct {
	int size;
	int capacity;		/* if negative, data points to another DynArray's data */
	char *data;
} DynArray;

/*Specializations*/

// IntArray supports an optimization (in sgen-new-bridge.c): If capacity is less than 0 it is a "copy" and does not own its buffer.
typedef struct {
	DynArray array;
} DynIntArray;

// PtrArray supports an optimization: If size is equal to 1 it is a "singleton" and data points to the single held item, not to a buffer.
typedef struct {
	DynArray array;
} DynPtrArray;

typedef struct {
	DynArray array;
} DynSCCArray;

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
	if (da->capacity < 0) {
		dyn_array_init (da);
		return;
	}

	if (da->capacity == 0)
		return;

	sgen_free_internal_dynamic (da->data, elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = NULL;
}

static void
dyn_array_empty (DynArray *da)
{
	if (da->capacity < 0)
		dyn_array_init (da);
	else
		da->size = 0;
}

static char *
dyn_array_ensure_capacity_internal (DynArray *da, int capacity, int elem_size)
{
	if (da->capacity <= 0)
		da->capacity = 2;
	while (capacity > da->capacity)
		da->capacity *= 2;

	return (char *)sgen_alloc_internal_dynamic (elem_size * da->capacity, INTERNAL_MEM_BRIDGE_DATA, TRUE);
}

static void
dyn_array_ensure_capacity (DynArray *da, int capacity, int elem_size)
{
	int old_capacity = da->capacity;
	char *new_data;

	g_assert (capacity > 0);

	if (capacity <= old_capacity)
		return;

	new_data = dyn_array_ensure_capacity_internal (da, capacity, elem_size);
	memcpy (new_data, da->data, elem_size * da->size);
	if (old_capacity > 0)
		sgen_free_internal_dynamic (da->data, elem_size * old_capacity, INTERNAL_MEM_BRIDGE_DATA);
	da->data = new_data;
}

static gboolean
dyn_array_is_copy (DynArray *da)
{
	return da->capacity < 0;
}

static void
dyn_array_ensure_independent (DynArray *da, int elem_size)
{
	if (!dyn_array_is_copy (da))
		return;
	dyn_array_ensure_capacity (da, da->size, elem_size);
	g_assert (da->capacity > 0);
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

static void
dyn_array_copy (DynArray *dst, DynArray *src, int elem_size)
{
	dyn_array_uninit (dst, elem_size);

	if (src->size == 0)
		return;

	dst->size = src->size;
	dst->capacity = -1;
	dst->data = src->data;
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

#ifdef NEW_XREFS
static void
dyn_array_int_empty (DynIntArray *da)
{
	dyn_array_empty (&da->array);
}
#endif

static void
dyn_array_int_add (DynIntArray *da, int x)
{
	int *p = (int *)dyn_array_add (&da->array, sizeof (int));
	*p = x;
}

static int
dyn_array_int_get (DynIntArray *da, int x)
{
	return ((int*)da->array.data)[x];
}

#ifdef NEW_XREFS
static void
dyn_array_int_set (DynIntArray *da, int idx, int val)
{
	((int*)da->array.data)[idx] = val;
}
#endif

static void
dyn_array_int_ensure_independent (DynIntArray *da)
{
	dyn_array_ensure_independent (&da->array, sizeof (int));
}

static void
dyn_array_int_copy (DynIntArray *dst, DynIntArray *src)
{
	dyn_array_copy (&dst->array, &src->array, sizeof (int));
}

static gboolean
dyn_array_int_is_copy (DynIntArray *da)
{
	return dyn_array_is_copy (&da->array);
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
#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (da->array.capacity == 1)
		dyn_array_ptr_init (da);
	else
#endif
		dyn_array_uninit (&da->array, sizeof (void*));
}

static int
dyn_array_ptr_size (DynPtrArray *da)
{
	return da->array.size;
}

static void
dyn_array_ptr_empty (DynPtrArray *da)
{
#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (da->array.capacity == 1)
		dyn_array_ptr_init (da);
	else
#endif
		dyn_array_empty (&da->array);
}

static void*
dyn_array_ptr_get (DynPtrArray *da, int x)
{
#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (da->array.capacity == 1) {
		g_assert (x == 0);
		return da->array.data;
	}
#endif
	return ((void**)da->array.data)[x];
}

static void
dyn_array_ptr_set (DynPtrArray *da, int x, void *ptr)
{
#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (da->array.capacity == 1) {
		g_assert (x == 0);
		da->array.data = ptr;
	} else
#endif
	{
		((void**)da->array.data)[x] = ptr;
	}
}

static void
dyn_array_ptr_add (DynPtrArray *da, void *ptr)
{
	void **p;

#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (da->array.capacity == 0) {
		da->array.capacity = 1;
		da->array.size = 1;
		p = (void**)&da->array.data;
	} else if (da->array.capacity == 1) {
		void *ptr0 = da->array.data;
		void **p0;
		dyn_array_init (&da->array);
		p0 = (void **)dyn_array_add (&da->array, sizeof (void*));
		*p0 = ptr0;
		p = (void **)dyn_array_add (&da->array, sizeof (void*));
	} else
#endif
	{
		p = (void **)dyn_array_add (&da->array, sizeof (void*));
	}
	*p = ptr;
}

#define dyn_array_ptr_push dyn_array_ptr_add

static void*
dyn_array_ptr_pop (DynPtrArray *da)
{
	int size = da->array.size;
	void *p;
	g_assert (size > 0);
#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (da->array.capacity == 1) {
		p = dyn_array_ptr_get (da, 0);
		dyn_array_init (&da->array);
	} else
#endif
	{
		g_assert (da->array.capacity > 1);
		dyn_array_ensure_independent (&da->array, sizeof (void*));
		p = dyn_array_ptr_get (da, size - 1);
		--da->array.size;
	}
	return p;
}

static void
dyn_array_ptr_ensure_capacity (DynPtrArray *da, int capacity)
{
#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
	if (capacity == 1 && da->array.capacity < 1) {
		da->array.capacity = 1;
	} else if (da->array.capacity == 1) // TODO size==1
	{
		if (capacity > 1)
		{
			void *ptr = dyn_array_ptr_get (da, 0);
			da->array.data = dyn_array_ensure_capacity_internal(&da->array, capacity, sizeof (void*));
			dyn_array_ptr_set (da, 0, ptr);
		}
	}
#endif
	{
		dyn_array_ensure_capacity (&da->array, capacity, sizeof (void*));
	}
}

static void
dyn_array_ptr_set_all (DynPtrArray *dst, DynPtrArray *src)
{
	const int copysize = src->array.size;
	if (copysize > 0) {
		dyn_array_ptr_ensure_capacity (dst, copysize);

#ifdef OPTIMIZATION_SINGLETON_DYN_ARRAY
		if (copysize == 1) {
			dyn_array_ptr_set (dst, 0, dyn_array_ptr_get (src, 0));
		} else
#endif
		{
			memcpy (dst->array.data, src->array.data, copysize * sizeof (void*));
		}
	}
	dst->array.size = src->array.size;
}
