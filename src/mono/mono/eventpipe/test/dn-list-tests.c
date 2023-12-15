#if defined(_MSC_VER) && defined(_DEBUG)
#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#endif

#include <eglib/test/test.h>
#include <containers/dn-list.h>


#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState dn_list_memory_start_snapshot;
static _CrtMemState dn_list_memory_end_snapshot;
static _CrtMemState dn_list_memory_diff_snapshot;
#endif

#define N_ELEMS 101
#define POINTER_TO_INT32(v) ((int32_t)(ptrdiff_t)(v))
#define INT32_TO_POINTER(v) ((void *)(ptrdiff_t)(v))

static
void
DN_CALLBACK_CALLTYPE
test_list_dispose_func (void *data)
{
	(*(uint32_t *)data)++;
}

static int32_t test_list_dispose_count = 0;

static
void
DN_CALLBACK_CALLTYPE
test_list_dispose_count_func (void *data)
{
	test_list_dispose_count++;
}

static
bool
DN_CALLBACK_CALLTYPE
test_list_remove_if_func (const void *data, const void *user_data)
{
	return !strcmp ((const char *)data, (const char *)user_data);
}

static
void
DN_CALLBACK_CALLTYPE
test_list_foreach_func (
	void *data,
	void *user_data)
{
	(*(uint32_t *)user_data)++;
}

static
int32_t
DN_CALLBACK_CALLTYPE
test_list_sort_compare (
	const void *p1,
	const void *p2)
{
	return POINTER_TO_INT32 (p1) - POINTER_TO_INT32 (p2);
}

static
bool
DN_CALLBACK_CALLTYPE
test_list_find_func (
	const void *a,
	const void *b)
{
	if (!a || !b)
		return false;

	return !strcmp ((const char *)a, (const char *)b);
}

static
RESULT
test_list_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_list_memory_start_snapshot);
#endif
	return OK;
}

static
RESULT
test_list_alloc (void)
{
	dn_list_t *list = dn_list_alloc ();
	if (!list)
		return FAILED ("failed to alloc list");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_init (void)
{
	dn_list_t list;
	if (!dn_list_init (&list))
		return FAILED ("failed to init list");

	dn_list_dispose (&list);

	return OK;
}

static
RESULT
test_list_free (void)
{
	uint32_t dispose_count = 0;
	dn_list_t *list = dn_list_custom_alloc (DN_DEFAULT_ALLOCATOR);
	if (!list)
		return FAILED ("failed to custom alloc list");

	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);

	dn_list_custom_free (list, test_list_dispose_func);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on free");

	return OK;
}

static
RESULT
test_list_dispose (void)
{
	uint32_t dispose_count = 0;
	dn_list_t list;
	if (!dn_list_custom_init (&list, DN_DEFAULT_ALLOCATOR))
		return FAILED ("failed to custom init list");

	dn_list_insert (dn_list_end (&list), &dispose_count);
	dn_list_insert (dn_list_end (&list), &dispose_count);

	dn_list_custom_dispose (&list, test_list_dispose_func);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on free");

	return OK;
}

static
RESULT
test_list_front (void)
{
	const char * items[] = { "first", "second" };

	dn_list_t *list = dn_list_alloc ();
	if (!list)
		return FAILED ("failed to alloc list");

	dn_list_insert (dn_list_end (list), (char *)items [0]);

	if (*dn_list_front_t (list, char *) != items [0])
		return FAILED ("failed list front #1");

	dn_list_insert (dn_list_begin (list), (char *)items [1]);

	if (*dn_list_front_t (list, char *) != items [1])
		return FAILED ("failed list front #2");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_empty (void)
{
	const char * items[] = { "first", "second" };

	dn_list_t *list = dn_list_alloc ();
	if (!list)
		return FAILED ("failed to alloc list");

	if (!dn_list_empty (list))
		return FAILED ("failed empty #1");

	dn_list_insert (dn_list_end (list), (char *)items [0]);

	if (dn_list_empty (list))
		return FAILED ("failed empty #2");

	dn_list_insert (dn_list_end (list), (char *)items [1]);

	if (dn_list_empty (list))
		return FAILED ("failed empty #3");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_max_size (void)
{
	dn_list_t *list = dn_list_alloc ();
	if (!list)
		return FAILED ("failed to alloc list");

	if (dn_list_max_size (list) != UINT32_MAX)
		return FAILED ("max_size failed");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_clear (void)
{
	uint32_t dispose_count = 0;
	const char * items[] = { "first", "second" };

	dn_list_t *list = dn_list_alloc ();
	if (!list)
		return FAILED ("failed to alloc list");

	if (!dn_list_empty (list))
		return FAILED ("failed empty #1");

	dn_list_insert (dn_list_end (list), (char *)items [0]);

	if (dn_list_empty (list))
		return FAILED ("failed empty #2");

	dn_list_insert (dn_list_end (list), (char *)items [1]);

	dn_list_clear (list);

	if (!dn_list_empty (list))
		return FAILED ("failed empty #3");

	dn_list_free (list);

	list = dn_list_custom_alloc (DN_DEFAULT_ALLOCATOR);

	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);

	dn_list_custom_clear (list, test_list_dispose_func);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on clear");

	dispose_count = 0;
	dn_list_custom_free (list, test_list_dispose_func);

	if (dispose_count != 0)
		return FAILED ("invalid dispose count on clear/free");

	return OK;
}

static
RESULT
test_list_insert (void)
{
	const char *items[] = { "first", "second" };

	dn_list_t *list = dn_list_alloc ();
	if (!dn_list_insert (dn_list_end (list), (char *)items [0]).result)
		return FAILED ("insert failed #1");

	if (!dn_list_insert (dn_list_end (list), (char *)items [1]).result)
		return FAILED ("insert failed #2");

	size_t i = 0;
	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (strcmp (item, items [i]))
			return FAILED ("insert, found %s, expected %s #1", item, items [i]);
		i++;
	} DN_LIST_FOREACH_END;

	dn_list_free (list);

	list = dn_list_alloc ();
	if (!dn_list_insert (dn_list_end (list), (char *)items [1]).result)
		return FAILED ("insert failed #3");

	if (!dn_list_insert (dn_list_begin (list), (char *)items [0]).result)
		return FAILED ("insert failed #4");

	i = 0;
	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (strcmp (item, items [i]))
			return FAILED ("insert, found %s, expected %s #1", item, items [i]);
		i++;
	} DN_LIST_FOREACH_END;

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_insert_range (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_list_t *list1 = dn_list_alloc ();
	dn_list_t *list2 = dn_list_alloc ();

	dn_list_insert (dn_list_end (list1), (char *)items [0]);
	dn_list_insert (dn_list_end (list2), (char *)items [1]);

	if (!dn_list_insert_range (dn_list_end (list1), dn_list_begin (list2), dn_list_end (list2)).result)
		return FAILED ("insert_range failed #1");

	size_t i = 0;
	DN_LIST_FOREACH_BEGIN (char *, item, list1) {
		if (strcmp (item, items [i]))
			return FAILED ("insert_range, found %s, expected %s #1", item, items [i]);
		i++;
	} DN_LIST_FOREACH_END;

	dn_list_free (list1);
	dn_list_free (list2);

	list1 = dn_list_alloc ();
	list2 = dn_list_alloc ();

	dn_list_insert (dn_list_end (list1), (char *)items [1]);
	dn_list_insert (dn_list_end (list2), (char *)items [0]);

	if (!dn_list_insert_range (dn_list_begin (list1), dn_list_begin (list2), dn_list_end (list2)).result)
		return FAILED ("insert_range failed #2");

	i = 0;
	DN_LIST_FOREACH_BEGIN (char *, item, list1) {
		if (strcmp (item, items [i]))
			return FAILED ("insert_range, found %s, expected %s #2", item, items [i]);
		i++;
	} DN_LIST_FOREACH_END;

	dn_list_free (list1);
	dn_list_free (list2);

	list1 = dn_list_alloc ();
	list2 = dn_list_alloc ();

	dn_list_insert (dn_list_end (list1), (char *)items [0]);
	dn_list_insert (dn_list_end (list1), (char *)items [1]);
	dn_list_insert (dn_list_end (list1), (char *)items [3]);
	dn_list_insert (dn_list_end (list2), (char *)items [2]);

	dn_list_it_t it = dn_list_begin (list1);
	it = dn_list_it_next_n (it, 2);

	if (!dn_list_insert_range (it, dn_list_begin (list2), dn_list_end (list2)).result)
		return FAILED ("insert_range failed #2");

	i = 0;
	DN_LIST_FOREACH_BEGIN (char *, item, list1) {
		if (strcmp (item, items [i]))
			return FAILED ("insert_range, found %s, expected %s #2", item, items [i]);
		i++;
	} DN_LIST_FOREACH_END;

	dn_list_free (list1);
	dn_list_free (list2);

	return OK;
}

static
RESULT
test_list_erase (void)
{
	uint32_t dispose_count = 0;
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_list_t *list = dn_list_alloc ();

	dn_list_insert (dn_list_end (list), (char *)items [0]);
	dn_list_insert (dn_list_end (list), (char *)items [1]);

	// Remove first.
	dn_list_erase (dn_list_begin (list));

	if (!list->head || !list->head->data || strcmp (list->head->data, "second") || list->head->next)
		return FAILED ("erase failed #1");

	dn_list_insert (dn_list_end (list), (char *)items [2]);
	dn_list_insert (dn_list_end (list), (char *)items [3]);

	dn_list_it_t it = dn_list_begin (list);
	dn_list_it_advance (&it,1);

	// Remove third.
	dn_list_erase (it);

	it = dn_list_begin (list);
	dn_list_it_advance (&it,1);

	// Remove forth.
	dn_list_erase (it);

	if (!list->head || !list->head->data || strcmp (list->head->data, "second") || list->head->next)
		return FAILED ("erase failed #2");

	dn_list_free (list);

	list = dn_list_alloc ();

	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);

	dn_list_custom_erase (dn_list_begin (list), test_list_dispose_func);
	dn_list_custom_erase (dn_list_begin (list), test_list_dispose_func);

	if (dispose_count != 2)
		return FAILED ("erase failed #3");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_push_front (void)
{
	const char *items[] = { "a", "b", "c"};

	dn_list_t list;

	dn_list_init (&list);

	dn_list_push_front (&list, (char *)items [0]);
	if (*dn_list_front_t (&list, char *) != items [0])
		return FAILED ("push_front failed #1");

	dn_list_push_front (&list, (char *)items [1]);
	if (*dn_list_front_t (&list, char *) != items [1])
		return FAILED ("push_front failed #2");

	dn_list_push_front (&list, (char *)items [2]);

	uint32_t i = 2;
	DN_LIST_FOREACH_BEGIN (char *, item, &list) {
		if (strcmp (item, items [i]))
			return FAILED ("push_front failed, found %s, expected %s #2", item, items [i]);
		i--;
	} DN_LIST_FOREACH_END;

	dn_list_dispose (&list);

	return OK;
}

static
RESULT
test_list_pop_front (void)
{
	uint32_t dispose_count = 0;
	const char *items[] = { "a", "b", "c"};

	dn_list_t list;

	dn_list_custom_init (&list, DN_DEFAULT_ALLOCATOR);

	dn_list_push_front (&list, (char *)items [2]);
	dn_list_push_front (&list, (char *)items [1]);
	dn_list_push_front (&list, (char *)items [0]);

	if (*dn_list_front_t (&list, char *) != items [0])
		return FAILED ("push_front failed");

	dn_list_pop_front (&list);

	if (*dn_list_front_t (&list, char *) != items [1])
		return FAILED ("pop_front failed #1");

	dn_list_pop_front (&list);

	if (*dn_list_front_t (&list, char *) != items [2])
		return FAILED ("pop_front failed #2");

	dn_list_pop_front (&list);

	dn_list_dispose (&list);

	dn_list_custom_init (&list, DN_DEFAULT_ALLOCATOR);

	dn_list_push_front (&list, &dispose_count);
	dn_list_push_front (&list, &dispose_count);
	dn_list_push_front (&list, &dispose_count);

	dn_list_custom_pop_front (&list, test_list_dispose_func);

	if (dispose_count == 0)
		return FAILED ("pop_front dispose count failed #1");

	dn_list_custom_dispose (&list, test_list_dispose_func);

	if (dispose_count != 3)
		return FAILED ("pop_front dispose count failed #2");

	return OK;
}

static
RESULT
test_list_push_back (void)
{
	const char *items[] = { "a", "b", "c"};

	dn_list_t list;

	dn_list_init (&list);

	dn_list_push_back (&list, (char *)items [0]);
	if (*dn_list_back_t (&list, char *) != items [0])
		return FAILED ("push_back failed #1");

	dn_list_push_back (&list, (char *)items [1]);
	if (*dn_list_back_t (&list, char *) != items [1])
		return FAILED ("push_back failed #2");

	dn_list_push_back (&list, (char *)items [2]);

	uint32_t i = 0;
	DN_LIST_FOREACH_BEGIN (char *, item, &list) {
		if (strcmp (item, items [i]))
			return FAILED ("push_back failed, found %s, expected %s #2", item, items [i]);
		i++;
	} DN_LIST_FOREACH_END;

	dn_list_dispose (&list);

	return OK;
}

static
RESULT
test_list_pop_back (void)
{
	uint32_t dispose_count = 0;
	const char *items[] = { "a", "b", "c"};

	dn_list_t list;

	dn_list_custom_init (&list, DN_DEFAULT_ALLOCATOR);

	dn_list_push_back (&list, (char *)items [2]);
	dn_list_push_back (&list, (char *)items [1]);
	dn_list_push_back (&list, (char *)items [0]);

	if (*dn_list_back_t (&list, char *) != items [0])
		return FAILED ("push_back failed");

	dn_list_pop_back (&list);

	if (*dn_list_back_t (&list, char *) != items [1])
		return FAILED ("pop_back failed #1");

	dn_list_pop_back (&list);

	if (*dn_list_front_t (&list, char *) != items [2])
		return FAILED ("pop_back failed #2");

	dn_list_pop_back (&list);

	dn_list_dispose (&list);

	dn_list_custom_init (&list, DN_DEFAULT_ALLOCATOR);

	dn_list_push_back (&list, &dispose_count);
	dn_list_push_back (&list, &dispose_count);
	dn_list_push_back (&list, &dispose_count);

	dn_list_custom_pop_back (&list, test_list_dispose_func);

	if (dispose_count == 0)
		return FAILED ("pop_back dispose count failed #1");

	dn_list_custom_dispose (&list, test_list_dispose_func);

	if (dispose_count != 3)
		return FAILED ("pop_back dispose count failed #2");

	return OK;
}

static
RESULT
test_list_resize (void)
{
	uint32_t dispose_count = 0;
	dn_list_t *list = dn_list_custom_alloc (DN_DEFAULT_ALLOCATOR);

	for (uint32_t i = 0; i < 10; i++)
		dn_list_push_front (list, &dispose_count);

	dn_list_custom_resize (list, 0, test_list_dispose_func);

	if (dispose_count != 10)
		return FAILED ("failed resize #1");

	for (uint32_t i = 0; i < 100; i++)
		dn_list_push_front (list, &dispose_count);
	
	dispose_count = 0;
	dn_list_custom_resize (list, 90, test_list_dispose_func);

	if (dispose_count != 10)
		return FAILED ("failed resize #2");

	dispose_count = 0;
	dn_list_custom_resize (list, 10, test_list_dispose_func);

	if (dispose_count != 80)
		return FAILED ("failed resize #3");

	dispose_count = 0;

	dn_list_custom_free (list, test_list_dispose_func);

	if (dispose_count != 10)
		return FAILED ("failed free");

	return OK;
}

static
RESULT
test_list_remove (void)
{
	uint32_t dispose_count = 0;
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_list_t *list = dn_list_alloc ();

	dn_list_insert (dn_list_end (list), (char *)items [0]);
	dn_list_insert (dn_list_end (list), (char *)items [1]);
	dn_list_insert (dn_list_end (list), (char *)items [2]);
	dn_list_insert (dn_list_end (list), (char *)items [3]);

	dn_list_insert (dn_list_end (list), (char *)items [0]);
	dn_list_insert (dn_list_end (list), (char *)items [1]);
	dn_list_insert (dn_list_end (list), (char *)items [2]);
	dn_list_insert (dn_list_end (list), (char *)items [3]);

	// Remove all "second"
	dn_list_remove (list, items [1]);

	if (dn_list_size (list) != 6)
		return FAILED ("remove failed, incorrect size #1");

	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (!strcmp (item, items [1]))
			return FAILED ("remove failed, found removed item %s #1", item);
	} DN_LIST_FOREACH_END;

	// Remove all "first"
	dn_list_remove (list, items [0]);

	if (dn_list_size (list) != 4)
		return FAILED ("remove failed, incorrect size #2");

	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (!strcmp (item, items [0]))
			return FAILED ("remove failed, found removed item %s #2", item);
	} DN_LIST_FOREACH_END;

	// Remove all "fourth"
	dn_list_remove (list, items [3]);

	if (dn_list_size (list) != 2)
		return FAILED ("remove failed, incorrect size #3");

	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (!strcmp (item, items [3]))
			return FAILED ("remove failed, found removed item %s #3", item);
	} DN_LIST_FOREACH_END;

	// "fourth" already removed.
	dn_list_remove (list, items [3]);

	// Validate that only "third" is left.
	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (strcmp (item, items [2]))
			return FAILED ("remove failed, unexpected item %s #4", item);
	} DN_LIST_FOREACH_END;

	dn_list_free (list);

	list = dn_list_alloc ();

	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);
	dn_list_insert (dn_list_end (list), &dispose_count);

	dn_list_custom_remove (list, &dispose_count, test_list_dispose_func);
	if (dispose_count != 4)
		return FAILED ("custom remove failed, incorrect dispose count");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_remove_if (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_list_t *list = dn_list_alloc ();

	dn_list_insert (dn_list_end (list), (char *)items [0]);
	dn_list_insert (dn_list_end (list), (char *)items [1]);
	dn_list_insert (dn_list_end (list), (char *)items [2]);
	dn_list_insert (dn_list_end (list), (char *)items [3]);

	dn_list_insert (dn_list_end (list), (char *)items [0]);
	dn_list_insert (dn_list_end (list), (char *)items [1]);
	dn_list_insert (dn_list_end (list), (char *)items [2]);
	dn_list_insert (dn_list_end (list), (char *)items [3]);

	// Remove all "second"
	dn_list_remove_if (list, items [1], test_list_remove_if_func);

	if (dn_list_size (list) != 6)
		return FAILED ("remove failed, incorrect size #1");

	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (!strcmp (item, items [1]))
			return FAILED ("remove failed, found removed item %s #1", item);
	} DN_LIST_FOREACH_END;

	// Remove all "first"
	dn_list_remove_if (list, items [0], test_list_remove_if_func);

	if (dn_list_size (list) != 4)
		return FAILED ("remove failed, incorrect size #2");

	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (!strcmp (item, items [0]))
			return FAILED ("remove failed, found removed item %s #2", item);
	} DN_LIST_FOREACH_END;

	// Remove all "fourth"
	dn_list_remove_if (list, items [3], test_list_remove_if_func);

	if (dn_list_size (list) != 2)
		return FAILED ("remove failed, incorrect size #3");

	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (!strcmp (item, items [3]))
			return FAILED ("remove failed, found removed item %s #3", item);
	} DN_LIST_FOREACH_END;

	// "fourth" already removed.
	dn_list_remove_if (list, items [3], test_list_remove_if_func);

	// Validate that only "third" is left.
	DN_LIST_FOREACH_BEGIN (char *, item, list) {
		if (strcmp (item, items [2]))
			return FAILED ("remove failed, unexpected item %s #4", item);
	} DN_LIST_FOREACH_END;

	dn_list_free (list);

	list = dn_list_alloc ();

	dn_list_insert (dn_list_end (list), (char *)items [0]);
	dn_list_insert (dn_list_end (list), (char *)items [1]);
	dn_list_insert (dn_list_end (list), (char *)items [2]);
	dn_list_insert (dn_list_end (list), (char *)items [3]);

	test_list_dispose_count = 0;
	dn_list_custom_remove_if (list, items [2], test_list_remove_if_func, test_list_dispose_count_func);
	if (test_list_dispose_count != 1)
		return FAILED ("custom remove if failed, incorrect dispose count");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_reverse (void)
{
	uint32_t count = N_ELEMS;
	dn_list_t *list = dn_list_alloc ();

	for (uint32_t i = 0; i < N_ELEMS; ++i)
		dn_list_insert (dn_list_end (list), INT32_TO_POINTER (i));

	dn_list_reverse (list);

	DN_LIST_FOREACH_BEGIN (void *, data, list) {
		if (POINTER_TO_INT32 (data) != count - 1)
			return FAILED ("reverse failed #1");
		count--;
	} DN_LIST_FOREACH_END;

	if (count != 0)
		return FAILED ("reverse failed #2");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_for_each (void)
{
	uint32_t count = 0;
	dn_list_t *list = dn_list_alloc ();

	for (uint32_t i = 0; i < N_ELEMS; ++i)
		dn_list_insert (dn_list_begin (list), INT32_TO_POINTER (i));

	dn_list_for_each (list, test_list_foreach_func, &count);
	if (count != N_ELEMS)
		return FAILED ("for_each failed");

	dn_list_free (list);

	return OK;
}

static
bool
list_verify_sort (
	dn_list_t *list,
	int32_t len)
{
	int32_t prev = POINTER_TO_INT32 (*dn_list_front (list));
	dn_list_pop_front (list);
	len--;

	DN_LIST_FOREACH_BEGIN (void *, item, list) {
		int32_t curr = POINTER_TO_INT32 (item);
		if (prev > curr)
			return false;
		prev = curr;

		if (len == 0)
			return false;
		len--;
	} DN_LIST_FOREACH_END;

	return len == 0;
}

static
RESULT
test_list_sort (void)
{
	int32_t i, j, mul;
	dn_list_t *list = dn_list_alloc ();

	for (i = 0; i < N_ELEMS; ++i)
		dn_list_push_front (list, INT32_TO_POINTER (i));

	dn_list_sort (list, test_list_sort_compare);
	if (!list_verify_sort (list, N_ELEMS))
		return FAILED ("decreasing list");

	dn_list_free (list);

	list = dn_list_alloc ();
	for (i = 0; i < N_ELEMS; ++i)
		dn_list_push_front (list, INT32_TO_POINTER (-i));
	dn_list_sort (list, test_list_sort_compare);
	if (!list_verify_sort (list, N_ELEMS))
		return FAILED ("increasing list");

	dn_list_free (list);

	list = dn_list_alloc ();
	dn_list_push_front (list, INT32_TO_POINTER (0));
	for (i = 1; i < N_ELEMS; ++i) {
		dn_list_push_front (list, INT32_TO_POINTER (-i));
		dn_list_push_front (list, INT32_TO_POINTER (i));
	}

	dn_list_sort (list, test_list_sort_compare);
	if (!list_verify_sort (list, 2*N_ELEMS-1))
		return FAILED ("alternating list");

	dn_list_free (list);

	list = dn_list_alloc ();
	mul = 1;
	for (i = 1; i < N_ELEMS; ++i) {
		mul = -mul;
		for (j = 0; j < i; ++j)
			dn_list_push_front (list, INT32_TO_POINTER (mul * j));
	}
	dn_list_sort (list, test_list_sort_compare);
	if (!list_verify_sort (list, (N_ELEMS*N_ELEMS - N_ELEMS)/2))
		return FAILED ("wavering list");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_find (void)
{
	dn_list_t *list = dn_list_alloc ();

	dn_list_push_front (list, (char*)"three");
	dn_list_push_front (list, (char*)"two");
	dn_list_push_front (list, (char*)"one");

	char *data = (char*)"four";
	dn_list_insert (dn_list_end (list), data);

	dn_list_it_t found1 = dn_list_find (list, data);
	dn_list_it_t found2 = dn_list_custom_find (list, data, test_list_find_func);

	if (*dn_list_it_data_t (found1, char *) != data || *dn_list_it_data_t (found2, char *) != data)
		return FAILED ("find failed #1");

	found1 = dn_list_find (list, NULL);
	found2 = dn_list_custom_find (list, NULL, test_list_find_func);
	if (found1.it != dn_list_end (list).it || found2.it != dn_list_end (list).it)
		return FAILED ("find failed #2");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_find_erase (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_list_t *list = dn_list_alloc ();

	dn_list_push_front (list, items [2]);
	dn_list_push_front (list, items [1]);
	dn_list_push_front (list, items [0]);

	dn_list_insert (dn_list_end (list), items [3]);

	dn_list_it_t found = dn_list_find (list, items [1]);
	if (dn_list_it_end (found))
		return FAILED ("find failed #1");

	if (*dn_list_it_data_t (found, char *) != items [1])
		return FAILED ("find failed #2");

	dn_list_it_t erased = dn_list_erase (found);
	if (dn_list_it_end (erased))
		return FAILED ("erase failed");

	found = dn_list_find (list, items [1]);
	if (!dn_list_it_end (found))
		return FAILED ("find failed #3");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_iterator (void)
{
	uint32_t count = 0;
	dn_list_t *list = dn_list_alloc ();

	for (uint32_t i = 0; i < N_ELEMS; ++i)
		dn_list_insert (dn_list_end (list), INT32_TO_POINTER (i));

	DN_LIST_FOREACH_BEGIN (void *, data, list) {
		if (POINTER_TO_INT32 (data) != count)
			return FAILED ("foreach iterator failed #1");
		count++;
	} DN_LIST_FOREACH_END;

	if (count != N_ELEMS)
		return FAILED ("foreach iterator failed #2");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_rev_iterator (void)
{
	uint32_t count = N_ELEMS;
	dn_list_t *list = dn_list_alloc ();

	for (uint32_t i = 0; i < N_ELEMS; ++i)
		dn_list_insert (dn_list_end (list), INT32_TO_POINTER (i));

	DN_LIST_FOREACH_RBEGIN (void *, data, list) {
		if (POINTER_TO_INT32 (data) != count - 1)
			return FAILED ("foreach rev iterator failed #1");
		count--;
	} DN_LIST_FOREACH_END;

	if (count != 0)
		return FAILED ("foreach rev iterator failed #2");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_iterate_remove (void)
{
	dn_list_t *list = dn_list_alloc ();

	for (uint32_t i = 0; i < N_ELEMS; ++i)
		dn_list_insert (dn_list_end (list), INT32_TO_POINTER (i));

	for (dn_list_it_t it = dn_list_begin (list); !dn_list_it_end (it); ) {
		void *data = *dn_list_it_data (it);
		it = dn_list_it_next (it);
		dn_list_remove (list, data);
	}

	if (dn_list_size (list) != 0)
		return FAILED ("iterate remove failed");

	dn_list_free (list);

	return OK;
}

static
RESULT
test_list_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_list_memory_end_snapshot);
	if ( _CrtMemDifference(&dn_list_memory_diff_snapshot, &dn_list_memory_start_snapshot, &dn_list_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &dn_list_memory_diff_snapshot );
		return FAILED ("memory leak detected!");
	}
#endif
	return OK;
}

static Test dn_list_tests [] = {
	{"test_list_setup", test_list_setup},
	{"test_list_alloc", test_list_alloc},
	{"test_list_init", test_list_init},
	{"test_list_free", test_list_free},
	{"test_list_dispose", test_list_dispose},
	{"test_list_front", test_list_front},
	{"test_list_empty", test_list_empty},
	{"test_list_max_size", test_list_max_size},
	{"test_list_clear", test_list_clear},
	{"test_list_insert", test_list_insert},
	{"test_list_insert_range", test_list_insert_range},
	{"test_list_erase", test_list_erase},
	{"test_list_push_front", test_list_push_front},
	{"test_list_pop_front", test_list_pop_front},
	{"test_list_push_back", test_list_push_back},
	{"test_list_pop_back", test_list_pop_back},
	{"test_list_resize", test_list_resize},
	{"test_list_remove", test_list_remove},
	{"test_list_remove_if", test_list_remove_if},
	{"test_list_reverse", test_list_reverse},
	{"test_list_for_each", test_list_for_each},
	{"test_list_sort", test_list_sort},
	{"test_list_find", test_list_find},
	{"test_list_find_erase", test_list_find_erase},
	{"test_list_iterator", test_list_iterator},
	{"test_list_rev_iterator", test_list_rev_iterator},
	{"test_list_iterate_remove", test_list_iterate_remove},
	{"test_list_teardown", test_list_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dn_list_tests_init, dn_list_tests)
