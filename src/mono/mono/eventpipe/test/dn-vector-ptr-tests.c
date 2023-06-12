// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(_MSC_VER) && defined(_DEBUG)
#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#endif

#include <eglib/test/test.h>
#include <containers/dn-vector-ptr.h>

#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState dn_vector_ptr_memory_start_snapshot;
static _CrtMemState dn_vector_ptr_memory_end_snapshot;
static _CrtMemState dn_vector_ptr_memory_diff_snapshot;
#endif

#define POINTER_TO_INT32(v) ((int32_t)(ptrdiff_t)(v))
#define INT32_TO_POINTER(v) ((void *)(ptrdiff_t)(v))

/* Don't add more than 32 items to this please */
static const char *test_vector_ptr_items [] = {
	"Apples", "Oranges", "Plumbs", "Goats", "Snorps", "Grapes",
	"Tickle", "Place", "Coffee", "Cookies", "Cake", "Cheese",
	"Tseng", "Holiday", "Avenue", "Smashing", "Water", "Toilet",
	NULL
};

static void *test_vector_ptr_last_disposed_value = 0;
static int32_t test_vector_ptr_dispose_call_count = 0;

static
void
DN_CALLBACK_CALLTYPE
test_vector_ptr_dispose_call_func(void *data)
{
	test_vector_ptr_last_disposed_value = *((void **)data);
	test_vector_ptr_dispose_call_count++;
}

static int32_t test_vector_ptr_foreach_iterate_index = 0;
static char *test_vector_ptr_foreach_iterate_error = NULL;

static
void
DN_CALLBACK_CALLTYPE
test_vector_ptr_foreach_callback (
	void *data,
	void *user_data)
{
	char *item = *((char **)data);
	const char *item_cmp = test_vector_ptr_items [test_vector_ptr_foreach_iterate_index++];

	if (test_vector_ptr_foreach_iterate_error != NULL) {
		return;
	}

	if (item != item_cmp) {
		test_vector_ptr_foreach_iterate_error = FAILED (
			"expected item at %d to be %s, but it was %s",
				test_vector_ptr_foreach_iterate_index - 1, item_cmp, item);
	}
}

static
void
DN_CALLBACK_CALLTYPE
test_vector_ptr_free_func(void *data)
{
	free (*(char **)data);
}

static
void
DN_CALLBACK_CALLTYPE
test_vector_ptr_clear_func(void *data)
{
	(**((uint32_t **)data))++;
}

static
int32_t
DN_CALLBACK_CALLTYPE
test_vector_ptr_sort_compare (
	const void *a,
	const void *b)
{
	char *stra = *(char **) a;
	char *strb = *(char **) b;
	return strcmp(stra, strb);
}

static
RESULT
test_vector_ptr_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_vector_ptr_memory_start_snapshot);
#endif
	return OK;
}

static
dn_vector_ptr_t *
vector_ptr_alloc_and_fill (dn_vector_ptr_t *vector, uint32_t *item_count)
{
	int32_t i;
	if (!vector)
		vector = dn_vector_ptr_alloc ();

	for(i = 0; test_vector_ptr_items [i] != NULL; i++) {
		dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i]);
	}

	if (item_count != NULL) {
		*item_count = i;
	}

	return vector;
}

static
uint32_t
vector_ptr_guess_capacity (uint32_t capacity)
{
	return ((capacity + (capacity >> 1) + 63) & ~63);
}

static
RESULT
test_vector_ptr_alloc (void)
{
	dn_vector_ptr_t *vector;
	uint32_t i;

	vector = vector_ptr_alloc_and_fill (NULL, &i);

	if (dn_vector_ptr_capacity (vector) != vector_ptr_guess_capacity (vector->size)) {
		return FAILED ("capacity should be %d, but it is %d",
			vector_ptr_guess_capacity (vector->size), dn_vector_ptr_capacity (vector));
	}

	if (vector->size != i) {
		return FAILED ("expected %d node(s) in the vector", i);
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_free (void)
{
	int32_t v = 27;
	dn_vector_ptr_t *vector = NULL;

	vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match #1");

	dn_vector_ptr_free (vector);

	vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match #2");

	dn_vector_ptr_push_back (vector, &v);

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_alloc_capacity (void)
{
	dn_vector_ptr_t *vector = NULL;
	dn_vector_ptr_custom_alloc_params_t params = {0, };

	params.capacity = ARRAY_SIZE (test_vector_ptr_items);
	vector = dn_vector_ptr_custom_alloc (&params);
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	if (dn_vector_ptr_capacity (vector) < ARRAY_SIZE (test_vector_ptr_items))
		return FAILED ("capacity didn't match");

	void **data = dn_vector_ptr_data (vector);
	for (int32_t i = 0; i < ARRAY_SIZE (test_vector_ptr_items); ++i) {
		dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i]);
		if (dn_vector_ptr_data (vector) != data)
			return FAILED ("vector pre-alloc failed");
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_for_iterate (void)
{
	dn_vector_ptr_t *vector = vector_ptr_alloc_and_fill (NULL, NULL);
	uint32_t i = 0;

	DN_VECTOR_PTR_FOREACH_BEGIN (char *, item, vector) {
		if (item != test_vector_ptr_items [i]) {
			return FAILED (
				"expected item at %d to be %s, but it was %s",
				i, test_vector_ptr_items [i], item);
		}
		i++;
	} DN_VECTOR_PTR_FOREACH_END;

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_foreach_iterate (void)
{
	dn_vector_ptr_t *vector = vector_ptr_alloc_and_fill (NULL, NULL);

	test_vector_ptr_foreach_iterate_index = 0;
	test_vector_ptr_foreach_iterate_error = NULL;

	dn_vector_ptr_for_each (vector, test_vector_ptr_foreach_callback, vector);

	dn_vector_ptr_free (vector);

	return test_vector_ptr_foreach_iterate_error;
}

static
RESULT
test_vector_ptr_resize (void)
{
	dn_vector_ptr_t *vector= dn_vector_ptr_alloc ();
	uint32_t grow_length = 50;

	dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]);
	dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [1]);
	dn_vector_ptr_resize (vector, grow_length);

	if (vector->size != grow_length) {
		return FAILED ("vector size should be 50, it is %d", vector->size);
	} else if (*dn_vector_ptr_index (vector, 0) != test_vector_ptr_items [0]) {
		return FAILED ("item 0 was overwritten, should be %s", test_vector_ptr_items [0]);
	} else if (*dn_vector_ptr_index (vector, 1) != test_vector_ptr_items [1]) {
		return FAILED ("item 1 was overwritten, should be %s", test_vector_ptr_items [1]);
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_resize_2 (void)
{
	dn_vector_ptr_custom_alloc_params_t params = {0,};
	params.attributes = DN_VECTOR_ATTRIBUTE_MEMORY_INIT;

	dn_vector_ptr_t *vector= dn_vector_ptr_custom_alloc (&params);
	uint32_t i, grow_length = 50;

	dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]);
	dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [1]);
	dn_vector_ptr_resize (vector, grow_length);

	if (vector->size != grow_length) {
		return FAILED ("vector size should be 50, it is %d", vector->size);
	} else if (*dn_vector_ptr_index (vector, 0) != test_vector_ptr_items [0]) {
		return FAILED ("item 0 was overwritten, should be %s", test_vector_ptr_items [0]);
	} else if (*dn_vector_ptr_index (vector, 1) != test_vector_ptr_items [1]) {
		return FAILED ("item 1 was overwritten, should be %s", test_vector_ptr_items [1]);
	}

	for (i = 2; i < vector->size; i++) {
		if (*dn_vector_ptr_index (vector, i) != NULL) {
			return FAILED ("item %d is not NULL, it is %p", i, vector->data[i]);
		}
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_push_back (void)
{
	int32_t v;
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	v = 27;

	dn_vector_ptr_push_back (vector, INT32_TO_POINTER (v));

	if (1 != vector->size)
		return FAILED ("vector push_back failed");

	if (v != POINTER_TO_INT32 (*dn_vector_ptr_index (vector, 0)))
		return FAILED ("dn_vector_index failed");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_push_back_2 (void)
{
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	for (int32_t i = 0; i < 10; ++i)
		dn_vector_ptr_push_back (vector, INT32_TO_POINTER (i));

	for (uint32_t i = 0; i < vector->size; ++i) {
		if (i != POINTER_TO_INT32 (*dn_vector_ptr_index (vector, i)))
			return FAILED ("vector push_back failed");
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_pop_back (void)
{
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	for (int32_t i = 0; i < 10; ++i)
		dn_vector_ptr_push_back (vector, INT32_TO_POINTER (i));

	if (POINTER_TO_INT32 (*dn_vector_ptr_back (vector)) != 9)
		return FAILED ("vector back failed");

	dn_vector_ptr_pop_back (vector);

	if (POINTER_TO_INT32 (*dn_vector_ptr_back (vector)) != 8)
		return FAILED ("vector pop_back failed");

	dn_vector_ptr_pop_back (vector);

	if (POINTER_TO_INT32 (*dn_vector_ptr_back (vector)) != 7)
		return FAILED ("vector pop_back failed");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_pop_back_2 (void)
{
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	for (int32_t i = 0; i < 10; ++i)
		dn_vector_ptr_push_back (vector, INT32_TO_POINTER (i));

	if (POINTER_TO_INT32 (*dn_vector_ptr_back (vector)) != 9)
		return FAILED ("vector back failed");

	test_vector_ptr_last_disposed_value = 0;
	dn_vector_ptr_custom_pop_back (vector, test_vector_ptr_dispose_call_func);
	if (POINTER_TO_INT32 (test_vector_ptr_last_disposed_value) != 9)
		return FAILED ("vector custom_pop_back failed, wrong disposed value #1");

	if (POINTER_TO_INT32 (*dn_vector_ptr_back (vector)) != 8)
		return FAILED ("vector pop_back failed");

	test_vector_ptr_last_disposed_value = 0;
	dn_vector_ptr_custom_pop_back (vector, test_vector_ptr_dispose_call_func);
	if (POINTER_TO_INT32 (test_vector_ptr_last_disposed_value) != 8)
		return FAILED ("vector custom_pop_back failed, wrong disposed value #2");

	if (POINTER_TO_INT32 (*dn_vector_ptr_back (vector)) != 7)
		return FAILED ("vector pop_back failed");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_erase (void)
{
	dn_vector_ptr_t * vector = vector_ptr_alloc_and_fill (NULL, NULL);

	dn_vector_ptr_erase (dn_vector_ptr_begin (vector));
	if (*dn_vector_ptr_index (vector, 0) != test_vector_ptr_items [1]) {
		return FAILED ("first item is not %s, it is %s", test_vector_ptr_items [1],
			*dn_vector_ptr_index (vector, 0));
	}

	dn_vector_ptr_erase (dn_vector_ptr_it_prev (dn_vector_ptr_end (vector)));

	if (*dn_vector_ptr_index (vector, vector->size - 1) != test_vector_ptr_items [vector->size]) {
		return FAILED ("last item is not %s, it is %s",
			test_vector_ptr_items [vector->size - 2], *dn_vector_ptr_index (vector, vector->size - 1));
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_erase_fast (void)
{
	dn_vector_ptr_t *vector = vector_ptr_alloc_and_fill (NULL, NULL);

	dn_vector_ptr_erase_fast (dn_vector_ptr_begin (vector));
	if (*dn_vector_ptr_index (vector, 0) != test_vector_ptr_items [vector->size]) {
		return FAILED ("first item is not %s, it is %s", test_vector_ptr_items [vector->size],
			*dn_vector_ptr_index (vector, 0));
	}

	dn_vector_ptr_erase_fast (dn_vector_ptr_it_prev (dn_vector_ptr_end (vector)));
	if (*dn_vector_ptr_index (vector, vector->size - 1) != test_vector_ptr_items [vector->size - 1]) {
		return FAILED ("last item is not %s, it is %s",
			test_vector_ptr_items [vector->size - 1], *dn_vector_ptr_index (vector, vector->size - 1));
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_erase_fast_2 (void)
{
	dn_vector_ptr_t *vector = vector_ptr_alloc_and_fill (NULL, NULL);

	test_vector_ptr_last_disposed_value = NULL;
	dn_vector_ptr_custom_erase_fast (dn_vector_ptr_it_next_n (dn_vector_ptr_begin (vector), 3), test_vector_ptr_dispose_call_func);
	if (test_vector_ptr_last_disposed_value != test_vector_ptr_items [3])
		return FAILED ("custom erase failed to dispose correct value");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_erase_fast_3 (void)
{
	dn_vector_ptr_t vector;
	dn_vector_ptr_init (&vector);
	vector_ptr_alloc_and_fill (&vector, NULL);

	dn_vector_ptr_erase_fast (dn_vector_ptr_begin (&vector));

	if (vector.data [dn_vector_ptr_size (&vector)] == NULL)
		return FAILED ("erase initialized memory, but shouldn't.");

	dn_vector_ptr_dispose (&vector);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.attributes = DN_VECTOR_ATTRIBUTE_MEMORY_INIT;

	dn_vector_ptr_custom_init (&vector, &params);
	vector_ptr_alloc_and_fill (&vector, NULL);

	dn_vector_ptr_erase_fast (dn_vector_ptr_begin (&vector));

	if (vector.data [dn_vector_ptr_size (&vector)] != NULL)
		return FAILED ("erase didn't initialize memory, but should");

	dn_vector_ptr_dispose (&vector);

	return OK;
}

static
RESULT
test_vector_ptr_capacity (void)
{
	uint32_t size;
	dn_vector_ptr_t *vector = vector_ptr_alloc_and_fill (NULL, &size);

	if (dn_vector_ptr_capacity (vector) < size)
		return FAILED ("invalid vector capacity #1");

	if (dn_vector_ptr_capacity (vector) < vector->size)
		return FAILED ("invalid arvectorray capacity #2");

	uint32_t capacity = dn_vector_ptr_capacity (vector);

	dn_vector_ptr_it_t it = dn_vector_ptr_begin (vector);

	void *value0 = *dn_vector_ptr_index (vector, 0);
	dn_vector_ptr_erase (it);

	void *value1 = *dn_vector_ptr_index (vector, 1);

	it = dn_vector_ptr_it_next (it);
	dn_vector_ptr_erase (it);

	void *value2 = *dn_vector_ptr_index (vector, 2);

	it = dn_vector_ptr_it_next (it);
	dn_vector_ptr_erase (it);

	if (dn_vector_ptr_capacity (vector) != capacity)
		return FAILED ("invalid vector capacity #3");

	dn_vector_ptr_push_back (vector, value0);
	dn_vector_ptr_push_back (vector, value1);
	dn_vector_ptr_push_back (vector, value2);

	if (dn_vector_ptr_capacity (vector) != capacity)
		return FAILED ("invalid vector capacity #4");

	dn_vector_ptr_free (vector);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.capacity = ARRAY_SIZE (test_vector_ptr_items);

	vector = dn_vector_ptr_custom_alloc (&params);
	if (vector->size != 0)
		return FAILED ("vector len didn't match");

	if (dn_vector_ptr_capacity (vector) < ARRAY_SIZE (test_vector_ptr_items))
		return FAILED ("invalid vector capacity #5");

	capacity = dn_vector_ptr_capacity (vector);
	for (int32_t i = 0; i < ARRAY_SIZE (test_vector_ptr_items); ++i)
		dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i]);

	if (dn_vector_ptr_capacity (vector) != capacity)
		return FAILED ("invalid vector capacity #6");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_custom_free (void)
{
	int32_t count = 0;
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);

	dn_vector_ptr_custom_free (vector, test_vector_ptr_clear_func);

	if (count != 2)
		return FAILED ("callback called incorrect number of times");

	vector = dn_vector_ptr_alloc ();
	
	dn_vector_ptr_push_back (vector, malloc (10));
	dn_vector_ptr_push_back (vector, malloc (100));
	
	dn_vector_ptr_custom_free (vector, test_vector_ptr_free_func);

	return OK;
}

static
RESULT
test_vector_ptr_clear (void)
{
	uint32_t count = 0;
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match #1");

	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);

	dn_vector_ptr_clear (vector);
	if (vector->size != 0)
		return FAILED ("vector size didn't match #2");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_custom_clear (void)
{
	uint32_t count = 0;
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	if (vector->size != 0)
		return FAILED ("vector size didn't match #1");

	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);
	dn_vector_ptr_push_back (vector, &count);

	uint32_t capacity = dn_vector_ptr_capacity (vector);

	dn_vector_ptr_custom_clear (vector, test_vector_ptr_clear_func);

	if (vector->size != 0)
		return FAILED ("vector size didn't match #2");

	if (dn_vector_ptr_capacity (vector) != capacity)
		return FAILED ("incorrect vector capacity");

	if (count != 5)
		return FAILED ("allback called incorrect number of times");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_foreach_it (void)
{
	uint32_t count = 0;
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	for (uint32_t i = 0; i < 100; ++i)
		dn_vector_ptr_push_back (vector, INT32_TO_POINTER (i));

	DN_VECTOR_PTR_FOREACH_BEGIN (uint32_t *, value, vector) {
		if (POINTER_TO_INT32 (value) != count)
			return FAILED ("foreach iterator failed #1");
		count++;
	} DN_VECTOR_PTR_FOREACH_END;

	if (count != dn_vector_ptr_size (vector))
		return FAILED ("foreach iterator failed #2");

	dn_vector_ptr_free (vector);
	return OK;
}

static
RESULT
test_vector_ptr_foreach_rev_it (void)
{
	uint32_t count = 100;
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	
	if (vector->size != 0)
		return FAILED ("vector size didn't match");

	for (uint32_t i = 0; i < 100; ++i)
		dn_vector_ptr_push_back (vector, INT32_TO_POINTER (i));

	DN_VECTOR_PTR_FOREACH_RBEGIN (uint32_t *, value, vector) {
		if (POINTER_TO_INT32 (value) != count - 1)
			return FAILED ("foreach reverse iterator failed #1");
		count--;
	} DN_VECTOR_PTR_FOREACH_END;

	if (count != 0)
		return FAILED ("foreach reverse iterator failed #2");

	dn_vector_ptr_free (vector);
	return OK;
}

static
RESULT
test_vector_ptr_sort (void)
{
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();
	uint32_t i;

	static char * const letters [] = { (char*)"A", (char*)"B", (char*)"C", (char*)"D", (char*)"E" };

	dn_vector_ptr_push_back (vector, letters [0]);
	dn_vector_ptr_push_back (vector, letters [1]);
	dn_vector_ptr_push_back (vector, letters [2]);
	dn_vector_ptr_push_back (vector, letters [3]);
	dn_vector_ptr_push_back (vector, letters [4]);

	dn_vector_ptr_sort (vector, test_vector_ptr_sort_compare);

	for (i = 0; i < vector->size; i++) {
		if (vector->data [i] != letters [i]) {
			return FAILED ("vector out of order, expected %s got %s at position %d",
				letters [i], (char *) vector->data [i], i);
		}
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_find (void)
{
	dn_vector_ptr_t *vector = dn_vector_ptr_alloc ();

	static char * const letters [] = { (char*)"A", (char*)"B", (char*)"C", (char*)"D", (char*)"E" };

	dn_vector_ptr_push_back (vector, letters [0]);
	dn_vector_ptr_push_back (vector, letters [1]);
	dn_vector_ptr_push_back (vector, letters [2]);
	dn_vector_ptr_push_back (vector, letters [3]);
	dn_vector_ptr_push_back (vector, letters [4]);

	if (dn_vector_ptr_find (vector, letters [0]).it == dn_vector_ptr_size (vector))
		return FAILED ("failed to find value #1");

	if (dn_vector_ptr_find (vector, letters [1]).it == dn_vector_ptr_size (vector))
		return FAILED ("failed to find value #2");

	if (dn_vector_ptr_find (vector, letters [2]).it == dn_vector_ptr_size (vector))
		return FAILED ("failed to find value #3");

	if (dn_vector_ptr_find (vector, letters [3]).it == dn_vector_ptr_size (vector))
		return FAILED ("failed to find value #4");

	if (dn_vector_ptr_find (vector, letters [4]).it == dn_vector_ptr_size (vector))
		return FAILED ("failed to find value #5");

	if (dn_vector_ptr_find (vector, NULL).it != dn_vector_ptr_size (vector))
		return FAILED ("find failed #6");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_default_local_alloc (void)
{
	DN_DEFAULT_LOCAL_ALLOCATOR (allocator, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;

	uint32_t init_capacity = dn_vector_ptr_default_local_allocator_capacity_size;
	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc");

	for (uint32_t i = 0; i < init_capacity; ++i) {
		for (uint32_t j = 0; j < ARRAY_SIZE (test_vector_ptr_items); ++j) {
			if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [j]))
				return FAILED ("failed vector push_back using custom alloc");
		}
	}

	for (uint32_t i = 0; i < init_capacity; ++i) {
		for (uint32_t j = 0; j < ARRAY_SIZE (test_vector_ptr_items); ++j) {
			if (*dn_vector_ptr_index (vector, ARRAY_SIZE (test_vector_ptr_items) * i + j) != test_vector_ptr_items [j])
				return FAILED ("vector realloc failure using default local alloc");
		}
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
bool
vector_ptr_owned_by_fixed_allocator (
	dn_allocator_fixed_t *allocator,
	dn_vector_ptr_t *vector)
{
	return	allocator->_data._begin <= (void *)dn_vector_ptr_data (vector) &&
		allocator->_data._end > (void *)dn_vector_ptr_data (vector);
}

static
RESULT
test_vector_ptr_local_alloc (void)
{
	uint8_t buffer [1024];
	dn_allocator_fixed_or_malloc_t allocator;

	dn_allocator_fixed_or_malloc_init (&allocator, buffer, ARRAY_SIZE (buffer));
	memset (buffer, 0, ARRAY_SIZE (buffer));

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;

	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc");

	// All should fit in fixed allocator.
	for (uint32_t i = 0; i < ARRAY_SIZE (test_vector_ptr_items); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i]))
			return FAILED ("failed vector push_back using custom alloc #1");
	}

	if (!vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("custom alloc using fixed allocator failed");

	// Make sure we run out of fixed allocator memory, should switch to dynamic allocator.
	for (uint32_t i = 0; i < ARRAY_SIZE (buffer); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc #2");
	}

	if (vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("custom alloc using dynamic allocator failed");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_local_alloc_capacity (void)
{
	uint8_t buffer [dn_vector_ptr_default_local_allocator_byte_size];
	dn_allocator_fixed_or_malloc_t allocator;

	dn_allocator_fixed_or_malloc_init (&allocator, buffer, dn_vector_ptr_default_local_allocator_byte_size);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc");

	if (dn_vector_ptr_capacity (vector) != dn_vector_ptr_default_local_allocator_capacity_size)
		return FAILED ("default local vector should have %d in capacity #1", dn_vector_ptr_default_local_allocator_capacity_size);

	// Make sure pre-allocted fixed allocator is used.
	if (!vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("custom alloc using fixed allocator failed #1");

	// Add pre-allocated amount of test_vector_ptr_items, should fit into fixed buffer.
	for (uint32_t i = 0; i < dn_vector_ptr_capacity (vector); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc");
	}

	if (dn_vector_ptr_capacity (vector) != dn_vector_ptr_default_local_allocator_capacity_size)
		return FAILED ("default local vector should have %d in capacity #2", dn_vector_ptr_default_local_allocator_capacity_size);

	// Make sure pre-allocted fixed allocator is used without reallocs (would cause OOM and switch to dynamic).
	if (!vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("custom alloc using fixed allocator failed #2");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_fixed_alloc_capacity (void)
{
	uint8_t buffer [dn_vector_ptr_default_local_allocator_byte_size];
	dn_allocator_fixed_t allocator;

	dn_allocator_fixed_init (&allocator, buffer, dn_vector_ptr_default_local_allocator_byte_size);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc");

	// Add pre-allocated amount of test_vector_ptr_items, should fit into fixed buffer.
	for (uint32_t i = 0; i < dn_vector_ptr_capacity (vector); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc");
	}

	// Adding one more should hit OOM.
	if (dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]))
		return FAILED ("vector push_back failed to triggered OOM");

	// Make room for on more item.
	dn_vector_ptr_pop_back (vector);

	// Adding one more should not hit OOM.
	if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]))
		return FAILED ("vector push_back triggered OOM");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_fixed_or_malloc_alloc_capacity (void)
{
	uint8_t buffer [dn_vector_ptr_default_local_allocator_byte_size];
	dn_allocator_fixed_or_malloc_t allocator;

	dn_allocator_fixed_or_malloc_init (&allocator, buffer, dn_vector_ptr_default_local_allocator_byte_size);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc");

	// Add pre-allocated amount of test_vector_ptr_items, should fit into fixed allocator.
	for (uint32_t i = 0; i < dn_vector_ptr_capacity (vector); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc #1");
	}

	// Make sure pre-allocted fixed allocator is used.
	if (!vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("custom alloc using fixed allocator failed");

	// Adding one more should not hit OOM.
	if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]))
		return FAILED ("failed vector push_back using custom alloc #2");

	if (dn_vector_ptr_capacity (vector) <= dn_vector_ptr_default_local_allocator_capacity_size)
		return FAILED ("unexpected vector capacity #1");

	uint32_t init_capacity = dn_vector_ptr_capacity (vector);

	// Make room for on more item.
	dn_vector_ptr_pop_back (vector);

	if (dn_vector_ptr_capacity (vector) < init_capacity)
		return FAILED ("unexpected vector capacity #2");

	// Validate continious use of dynamic allocator.
	if (vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("unexpected switch to fixed allocator");

	// Adding one more should not hit OOM.
	if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]))
		return FAILED ("failed vector push_back using custom alloc #3");

	if (dn_vector_ptr_capacity (vector) < init_capacity)
		return FAILED ("unexpected vector capacity #3");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_fixed_reset_alloc_capacity (void)
{
	uint8_t buffer [dn_vector_ptr_default_local_allocator_byte_size];
	dn_allocator_fixed_t allocator;

	dn_allocator_fixed_init (&allocator, buffer, dn_vector_ptr_default_local_allocator_byte_size);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc #1");

	// Add pre-allocated amount of test_vector_ptr_items, should fit into fixed allocator.
	for (uint32_t i = 0; i < dn_vector_ptr_capacity (vector); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc");
	}

	// Adding one more should hit OOM.
	if (dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]))
		return FAILED ("vector push_back failed to triggered OOM");

	dn_vector_ptr_free (vector);

	// Reset fixed allocator.
	dn_allocator_fixed_reset (&allocator);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc #2");

	// Add pre-allocated amount of test_vector_ptr_items, should fit into fixed buffer.
	for (uint32_t i = 0; i < dn_vector_ptr_capacity (vector); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc");
	}

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_fixed_or_malloc_reset_alloc_capacity (void)
{
	uint8_t buffer [dn_vector_ptr_default_local_allocator_byte_size];
	dn_allocator_fixed_or_malloc_t allocator;

	dn_allocator_fixed_or_malloc_init (&allocator, buffer, dn_vector_ptr_default_local_allocator_byte_size);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	dn_vector_ptr_custom_alloc_params_t params = {0, };
	params.allocator = (dn_allocator_t *)&allocator;
	params.capacity = dn_vector_ptr_default_local_allocator_capacity_size;

	dn_vector_ptr_t *vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc #1");

	// Add pre-allocated amount of test_vector_ptr_items, should fit into fixed allocator.
	for (uint32_t i = 0; i < dn_vector_ptr_capacity (vector); ++i) {
		if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [i % ARRAY_SIZE (test_vector_ptr_items)]))
			return FAILED ("failed vector push_back using custom alloc #1");
	}

	// Adding one more should not hit OOM but switch to dynamic allocator.
	if (!dn_vector_ptr_push_back (vector, (char *)test_vector_ptr_items [0]))
		return FAILED ("failed vector push_back using custom alloc #2");

	// Validate use of dynamic allocator.
	if (vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("unexpected switch to fixed allocator #1");

	dn_vector_ptr_free (vector);

	vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc #2");

	// Validate use of dynamic allocator.
	if (vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("unexpected switch to fixed allocator #2");

	dn_vector_ptr_free (vector);

	// Reset fixed part of allocator.
	dn_allocator_fixed_or_malloc_reset (&allocator);
	memset (buffer, 0, dn_vector_ptr_default_local_allocator_byte_size);

	vector = dn_vector_ptr_custom_alloc (&params);
	if (!vector)
		return FAILED ("failed vector custom alloc #2");

	// Validate use of fixed allocator.
	if (!vector_ptr_owned_by_fixed_allocator ((dn_allocator_fixed_t *)&allocator, vector))
		return FAILED ("custom alloc using fixed allocator failed");

	dn_vector_ptr_free (vector);

	return OK;
}

static
RESULT
test_vector_ptr_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_vector_ptr_memory_end_snapshot);
	if ( _CrtMemDifference(&dn_vector_ptr_memory_diff_snapshot, &dn_vector_ptr_memory_start_snapshot, &dn_vector_ptr_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &dn_vector_ptr_memory_diff_snapshot );
		return FAILED ("memory leak detected!");
	}
#endif
	return OK;
}

static Test dn_vector_ptr_tests [] = {
	{"test_vector_ptr_setup", test_vector_ptr_setup},
	{"test_vector_ptr_alloc", test_vector_ptr_alloc},
	{"test_vector_ptr_free", test_vector_ptr_free},
	{"test_vector_ptr_alloc_capacity", test_vector_ptr_alloc_capacity},
	{"test_vector_ptr_for_iterate", test_vector_ptr_for_iterate},
	{"test_vector_ptr_foreach_iterate", test_vector_ptr_foreach_iterate},
	{"test_vector_ptr_resize", test_vector_ptr_resize},
	{"test_vector_ptr_resize_2", test_vector_ptr_resize_2},
	{"test_vector_ptr_push_back", test_vector_ptr_push_back},
	{"test_vector_ptr_push_back_2", test_vector_ptr_push_back_2},
	{"test_vector_ptr_pop_back", test_vector_ptr_pop_back},
	{"test_vector_ptr_pop_back_2", test_vector_ptr_pop_back_2},
	{"test_vector_ptr_erase", test_vector_ptr_erase},
	{"test_vector_ptr_erase_fast", test_vector_ptr_erase_fast},
	{"test_vector_ptr_erase_fast_2", test_vector_ptr_erase_fast_2},
	{"test_vector_ptr_erase_fast_3", test_vector_ptr_erase_fast_3},
	{"test_vector_ptr_capacity", test_vector_ptr_capacity},
	{"test_vector_ptr_custom_free", test_vector_ptr_custom_free},
	{"test_vector_ptr_clear", test_vector_ptr_clear},
	{"test_vector_ptr_custom_clear", test_vector_ptr_custom_clear},
	{"test_vector_ptr_foreach_it", test_vector_ptr_foreach_it},
	{"test_vector_ptr_foreach_rev_it", test_vector_ptr_foreach_rev_it},
	{"test_vector_ptr_sort", test_vector_ptr_sort},
	{"test_vector_ptr_find", test_vector_ptr_find},
	{"test_vector_ptr_default_local_alloc", test_vector_ptr_default_local_alloc},
	{"test_vector_ptr_local_alloc", test_vector_ptr_local_alloc},
	{"test_vector_ptr_local_alloc_capacity", test_vector_ptr_local_alloc_capacity},
	{"test_vector_ptr_fixed_alloc_capacity", test_vector_ptr_fixed_alloc_capacity},
	{"test_vector_ptr_fixed_or_malloc_alloc_capacity", test_vector_ptr_fixed_or_malloc_alloc_capacity},
	{"test_vector_ptr_fixed_reset_alloc_capacity", test_vector_ptr_fixed_reset_alloc_capacity},
	{"test_vector_ptr_fixed_or_malloc_reset_alloc_capacity", test_vector_ptr_fixed_or_malloc_reset_alloc_capacity},
	{"test_vector_ptr_teardown", test_vector_ptr_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dn_vector_ptr_tests_init, dn_vector_ptr_tests)
