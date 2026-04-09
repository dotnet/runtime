#if defined(_MSC_VER) && defined(_DEBUG)
#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>
#endif

#include <eglib/test/test.h>
#include <containers/dn-umap.h>
#include <containers/dn-umap-t.h>


#ifdef _CRTDBG_MAP_ALLOC
static _CrtMemState dn_umap_memory_start_snapshot;
static _CrtMemState dn_umap_memory_end_snapshot;
static _CrtMemState dn_umap_memory_diff_snapshot;
#endif

#define POINTER_TO_INT32(v) ((int32_t)(ptrdiff_t)(v))
#define INT32_TO_POINTER(v) ((void *)(ptrdiff_t)(v))

static
void
DN_CALLBACK_CALLTYPE
test_umap_key_dispose_func (void *data)
{
	(*(uint32_t *)data)++;
}

static
void
DN_CALLBACK_CALLTYPE
test_umap_value_dispose_func (void *data)
{
	(*(uint32_t *)data)++;
}

static
void
DN_CALLBACK_CALLTYPE
test_umap_str_key_dispose_func (void *data)
{
	free (data);
}

static
bool
DN_CALLBACK_CALLTYPE
test_umap_find_func (
	const void *a,
	const void *b)
{
	if (!a || !b)
		return false;

	return !strcmp ((const char *)a, (const char *)b);
}

static
void
DN_CALLBACK_CALLTYPE
test_umap_for_each_func (
	void *key,
	void *value,
	void *user_data)
{
	(*(uint32_t *)user_data)++;
}

static
RESULT
test_umap_setup (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_umap_memory_start_snapshot);
#endif
	return OK;
}

static
RESULT
test_umap_alloc (void)
{
	dn_umap_t *map = dn_umap_alloc ();
	if (!map)
		return FAILED ("failed to alloc map");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_init (void)
{
	dn_umap_t map;
	if (!dn_umap_init (&map))
		return FAILED ("failed to init map");

	dn_umap_dispose (&map);

	return OK;
}

static
RESULT
test_umap_free (void)
{
	uint32_t dispose_count = 0;

	dn_umap_custom_alloc_params_t params = {0, };
	params.value_dispose_func = test_umap_value_dispose_func;

	dn_umap_t *map = dn_umap_custom_alloc (&params);
	if (!map)
		return FAILED ("failed to custom alloc map");

	dn_umap_insert (map, INT32_TO_POINTER (1), &dispose_count);
	dn_umap_insert (map, INT32_TO_POINTER (2), &dispose_count);

	dn_umap_free (map);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on free");

	return OK;
}

static
RESULT
test_umap_dispose (void)
{
	uint32_t dispose_count = 0;
	dn_umap_t map;
	dn_umap_custom_init_params_t params = {0, };

	params.value_dispose_func = test_umap_value_dispose_func;
	if (!dn_umap_custom_init (&map, &params))
		return FAILED ("failed to custom init map");

	dn_umap_insert (&map, INT32_TO_POINTER (1), &dispose_count);
	dn_umap_insert (&map, INT32_TO_POINTER (2), &dispose_count);

	dn_umap_dispose (&map);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on free");

	return OK;
}

static
RESULT
test_umap_empty (void)
{
	const char * items[] = { "first", "second" };

	dn_umap_t *map = dn_umap_alloc ();
	if (!map)
		return FAILED ("failed to alloc map");

	if (!dn_umap_empty (map))
		return FAILED ("failed empty #1");

	dn_umap_insert (map, (char *)items [0], (char *)items [0]);

	if (dn_umap_empty (map))
		return FAILED ("failed empty #2");

	dn_umap_insert (map, (char *)items [1], (char *)items [1]);

	if (dn_umap_empty (map))
		return FAILED ("failed empty #3");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_size (void)
{
	dn_umap_t *map = dn_umap_alloc ();

	if (dn_umap_size (map) != 0)
		return FAILED ("map size didn't match");

	for (int32_t i = 0; i < 10; ++i)
		dn_umap_insert (map, INT32_TO_POINTER (i), NULL);

	if (dn_umap_size (map) != 10)
		return FAILED ("size failed #1");

	dn_umap_clear (map);

	if (dn_umap_size (map) != 0)
		return FAILED ("size failed #2");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_max_size (void)
{
	dn_umap_t *map = dn_umap_alloc ();
	if (!map)
		return FAILED ("failed to alloc map");

	if (dn_umap_max_size (map) != UINT32_MAX)
		return FAILED ("max_size failed");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_clear (void)
{
	uint32_t dispose_count = 0;

	const char * items[] = { "first", "second" };

	dn_umap_t *map = dn_umap_alloc ();
	if (!map)
		return FAILED ("failed to alloc map");

	if (!dn_umap_empty (map))
		return FAILED ("failed empty #1");

	dn_umap_insert (map, (char *)items [0], (char *)items [0]);

	if (dn_umap_empty (map))
		return FAILED ("failed empty #2");

	dn_umap_insert (map, (char *)items [1], (char *)items [1]);

	dn_umap_clear (map);

	if (!dn_umap_empty (map))
		return FAILED ("failed empty #3");

	dn_umap_free (map);

	dn_umap_custom_alloc_params_t params = {0, };
	params.value_dispose_func = test_umap_value_dispose_func;

	map = dn_umap_custom_alloc (&params);

	dn_umap_insert (map, INT32_TO_POINTER (1), &dispose_count);
	dn_umap_insert (map, INT32_TO_POINTER (2), &dispose_count);

	dn_umap_clear (map);

	if (dispose_count != 2)
		return FAILED ("invalid dispose count on clear");

	dispose_count = 0;
	dn_umap_free (map);

	if (dispose_count != 0)
		return FAILED ("invalid dispose count on clear/free");

	return OK;
}

static
RESULT
test_umap_insert (void)
{
	dn_umap_result_t result;
	const char *items[] = { "first", "second" };

	dn_umap_t *map = dn_umap_alloc ();
	result = dn_umap_insert (map, (char *)items [0], (char *)items [0]);
	if (!result.result || dn_umap_it_key (result.it) != items [0] || dn_umap_it_value (result.it) != items [0])
		return FAILED ("insert failed #1");

	result = dn_umap_insert (map, (char *)items [1], (char *)items [1]);
	if (!result.result || dn_umap_it_key (result.it) != items [1] || dn_umap_it_value (result.it) != items [1])
		return FAILED ("insert failed #2");

	result = dn_umap_insert (map, (char *)items [1], NULL);
	if (result.result || dn_umap_it_key (result.it) != items [1] || dn_umap_it_value (result.it) != items [1])
		return FAILED ("insert failed #3");

	dn_umap_free (map);

	dn_umap_custom_alloc_params_t params = {0, };

	params.hash_func = dn_str_hash;
	params.equal_func = dn_str_equal;
	params.key_dispose_func = test_umap_str_key_dispose_func;

	map = dn_umap_custom_alloc (&params);
	dn_umap_insert (map, strdup ("first"), (char *)items [0]);

	char *exists = strdup ("first");
	result = dn_umap_insert (map, exists, (char *)items [0]);
	if (result.result)
		return FAILED ("insert failed #4");
	free (exists);

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_insert_or_assign (void)
{
	dn_umap_result_t result;
	const char *items[] = { "first", "second" };

	dn_umap_t *map = dn_umap_alloc ();
	result = dn_umap_insert_or_assign (map, (char *)items [0], (char *)items [0]);
	if (!result.result || dn_umap_it_key (result.it) != items [0] || dn_umap_it_value (result.it) != items [0])
		return FAILED ("insert_or_assign failed #1");

	result = dn_umap_insert_or_assign (map, (char *)items [1], (char *)items [1]);
	if (!result.result || dn_umap_it_key (result.it) != items [1] || dn_umap_it_value (result.it) != items [1])
		return FAILED ("insert_or_assign failed #2");

	result = dn_umap_insert_or_assign (map, (char *)items [1], NULL);
	if (!result.result || dn_umap_it_key (result.it) != items [1] || dn_umap_it_value (result.it) != NULL)
		return FAILED ("insert_or_assign failed #3");

	dn_umap_free (map);

	dn_umap_custom_alloc_params_t params = {0, };

	params.hash_func = dn_str_hash;
	params.equal_func = dn_str_equal;
	params.key_dispose_func = test_umap_str_key_dispose_func;

	map = dn_umap_custom_alloc (&params);
	dn_umap_insert_or_assign (map, strdup ("first"), (char *)items [0]);

	result = dn_umap_insert_or_assign (map, (char *)"first", (char *)items [1]);
	if (!result.result || strcmp (dn_umap_it_key (result.it), items [0]) || dn_umap_it_value (result.it) != items [1])
		return FAILED ("insert_or_assign failed #4");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_erase (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_insert (map, (char *)items [0], (char *)items [0]);
	dn_umap_insert (map, (char *)items [1], (char *)items [1]);

	dn_umap_it_t it = dn_umap_begin (map);
	char *key = dn_umap_it_key (it);
	char *value = dn_umap_it_value (it);

	dn_umap_it_t result = dn_umap_erase (it);
	if (dn_umap_size (map) != 1 || dn_umap_it_key (result) == key || dn_umap_it_value (result) == value)
		return FAILED ("erase failed #1");

	if (dn_umap_erase_key (map, NULL) != 0)
		return FAILED ("erase failed #2");

	dn_umap_insert (map, (char *)items [2], (char *)items [2]);
	dn_umap_insert (map, (char *)items [3], (char *)items [3]);

	if (dn_umap_erase_key (map, items [2]) == 0)
		return FAILED ("erase failed #3");

	result = dn_umap_erase (dn_umap_begin (map));
	result = dn_umap_erase (dn_umap_begin (map));

	if (!dn_umap_it_end (result))
		return FAILED ("erase failed #4");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_extract (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_insert (map, (char *)items [0], (char *)items [1]);
	dn_umap_insert (map, (char *)items [2], (char *)items [3]);

	char *key;
	char *value;

	if (!dn_umap_extract_key (map, items [0], (void **)&key, (void **)&value) || key != items [0] || value != items [1])
		return FAILED ("extract failed #1");

	if (dn_umap_size (map) != 1)
		return FAILED ("extract failed #2");

	dn_umap_free (map);

	uint32_t key_dispose_count = 0;
	uint32_t value_dispose_count = 0;

	dn_umap_custom_alloc_params_t params = {0, };

	params.key_dispose_func = test_umap_str_key_dispose_func;
	params.value_dispose_func = test_umap_value_dispose_func;

	map = dn_umap_custom_alloc (&params);

	dn_umap_insert (map, &key_dispose_count, &value_dispose_count);
	if (!dn_umap_extract_key (map, &key_dispose_count, NULL, NULL))
		return FAILED ("extract failed #3");

	if (key_dispose_count != 0 || value_dispose_count != 0 || dn_umap_size (map) != 0)
		return FAILED ("extract failed #4");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_find (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_insert (map, (char *)items [2], (char *)items [2]);
	dn_umap_insert (map, (char *)items [1], (char *)items [1]);
	dn_umap_insert (map, (char *)items [0], (char *)items [0]);

	const char *data = items [3];
	dn_umap_insert (map, (char *)data, (char *)data);

	dn_umap_it_t found1 = dn_umap_find (map, data);
	dn_umap_it_t found2 = dn_umap_custom_find (map, data, test_umap_find_func);

	if (dn_umap_it_key_t (found1, char *) != data || dn_umap_it_key_t (found2, char *) != data)
		return FAILED ("find failed #1");

	found1 = dn_umap_find (map, NULL);
	found2 = dn_umap_custom_find (map, NULL, test_umap_find_func);
	if (!dn_umap_it_end (found1) || !dn_umap_it_end (found2))
		return FAILED ("find failed #2");

	dn_umap_free (map);

	dn_umap_custom_alloc_params_t params = {0, };

	params.hash_func = dn_str_hash;
	params.equal_func = dn_str_equal;

	map = dn_umap_custom_alloc (&params);

	dn_umap_insert (map, (char *)items [2], (char *)items [2]);
	dn_umap_insert (map, (char *)items [1], (char *)items [1]);
	dn_umap_insert (map, (char *)items [0], (char *)items [0]);

	found1 = dn_umap_find (map, "second");
	found2 = dn_umap_custom_find (map, "second", test_umap_find_func);

	if (dn_umap_it_key (found1) != dn_umap_it_key (found2))
		return FAILED ("find failed #3");

	dn_umap_free (map);

	return OK;
}

static RESULT
test_umap_find_2 (void)
{
	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_insert (map, NULL, INT32_TO_POINTER (1));
	dn_umap_insert (map, INT32_TO_POINTER (1), INT32_TO_POINTER (2));

	dn_umap_it_t found = dn_umap_find (map, NULL);
	if (dn_umap_it_end (found))
		return FAILED ("Did not find the NULL");

	if (dn_umap_it_key (found) != NULL)
		return FAILED ("Incorrect key found");

	if (dn_umap_it_value (found) != INT32_TO_POINTER (1))
		return FAILED ("Got wrong value %p\n", dn_umap_it_value (found));

	found = dn_umap_find (map, INT32_TO_POINTER (1));
	if (dn_umap_it_end (found))
		return FAILED ("Did not find the 1");

	if (dn_umap_it_key (found) != INT32_TO_POINTER(1))
		return FAILED ("Incorrect key found");

	if (dn_umap_it_value (found) != INT32_TO_POINTER (2))
		return FAILED ("Got wrong value %p\n", dn_umap_it_value (found));

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_contains (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_insert (map, (char *)items [2], (char *)items [2]);
	dn_umap_insert (map, (char *)items [1], (char *)items [1]);
	dn_umap_insert (map, (char *)items [0], (char *)items [0]);

	if (!dn_umap_contains (map, items [0]))
		return FAILED ("contains failed #1");

	if (dn_umap_contains (map, "unkown"))
		return FAILED ("contains failed #2");

	dn_umap_erase_key (map, items [1]);

	if (dn_umap_contains (map, items [1]))
		return FAILED ("contains failed #3");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_rehash (void)
{
	dn_umap_t *map = dn_umap_alloc ();

	for (uint32_t i = 0; i < 1000; i++)
		dn_umap_insert (map, INT32_TO_POINTER (i), INT32_TO_POINTER (i));

	dn_umap_rehash (map, dn_umap_size (map) * 2);

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_reserve (void)
{
	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_reserve (map, 1000);

	for (uint32_t i = 0; i < 1000; i++)
		dn_umap_insert (map, INT32_TO_POINTER (i), INT32_TO_POINTER (i));

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_for_each (void)
{
	uint32_t count = 0;
	dn_umap_t *map = dn_umap_alloc ();

	for (uint32_t i = 0; i < 100; ++i)
		dn_umap_insert (map, INT32_TO_POINTER (i), INT32_TO_POINTER (i));

	dn_umap_for_each (map, test_umap_for_each_func, &count);
	if (count != 100)
		return FAILED ("for_each failed");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_iterator (void)
{
	uint32_t count = 0;
	dn_umap_t *map = dn_umap_alloc ();

	for (uint32_t i = 0; i < 100; ++i)
		dn_umap_insert (map, INT32_TO_POINTER (i), INT32_TO_POINTER (i));

	DN_UMAP_FOREACH_BEGIN (uint32_t, key, uint32_t, value, map) {
		if (key == value)
			count++;
	} DN_UMAP_FOREACH_END;

	if (count != 100)
		return FAILED ("foreach iterator failed #2");

	count = 0;
	DN_UMAP_FOREACH_KEY_BEGIN (uint32_t, key, map) {
		count += key;
	} DN_UMAP_FOREACH_END;

	if (count != 4950)
		return FAILED ("foreach iterator failed #4");

	dn_umap_free (map);

	return OK;
}

static RESULT
test_umap_iterator_2 (void)
{
	dn_umap_custom_alloc_params_t params = {0, };

	params.hash_func = dn_direct_hash;
	params.equal_func = dn_direct_equal;

	dn_umap_t *map = dn_umap_custom_alloc (&params);

	uint32_t sum = 0;
	for (uint32_t i = 0; i < 1000; i++) {
		sum += i;
		dn_umap_insert (map, INT32_TO_POINTER (i), INT32_TO_POINTER (i));
	}

	uint32_t keys_sum = 0;
	uint32_t values_sum = 0;

	DN_UMAP_FOREACH_BEGIN (uint32_t, key, uint32_t, value, map) {
		if (key != value)
			return FAILED ("key != value");
		keys_sum += key;
		values_sum += value;
	} DN_UMAP_FOREACH_END;

	if (keys_sum != sum || values_sum != sum)
		return FAILED ("Did not find all key-value pairs");

	dn_umap_free (map);

	return OK;
}

uint32_t foreach_count = 0;
uint32_t foreach_fail = 0;

static void
umap_for_each_str_str_func (void *key, void *value, void *user_data)
{
	foreach_count++;
	if (POINTER_TO_INT32 (user_data) != 'a')
		foreach_fail = 1;
}

static RESULT
test_umap_str_str_map (void)
{
	dn_umap_custom_alloc_params_t params = {0, };
	params.hash_func = dn_str_hash;
	params.equal_func = dn_str_equal;

	dn_umap_t *map = dn_umap_custom_alloc (&params);

	foreach_count = 0;
	foreach_fail = 0;

	dn_umap_insert (map, (char *)"hello", (char *)"world");
	dn_umap_insert (map, (char*)"my", (char*)"god");

	dn_umap_for_each (map, umap_for_each_str_str_func, INT32_TO_POINTER ('a'));

	if (foreach_count != 2)
		return FAILED ("did not find all keys, got %d expected 2", foreach_count);

	if (foreach_fail)
		return FAILED("failed to pass the user-data to foreach");

	if (dn_umap_erase_key (map, "my") == 0)
		return FAILED ("did not find known key");

	if (dn_umap_size (map) != 1)
		return FAILED ("unexpected size");

	dn_umap_insert_or_assign (map, (char *)"hello", (char *)"moon");
	dn_umap_it_t found = dn_umap_find (map, "hello");
	if (dn_umap_it_end (found) || strcmp (dn_umap_it_value_t (found, char *), "moon") != 0)
		return FAILED ("did not replace world with moon");

	if (dn_umap_erase_key (map, "hello") == 0)
		return FAILED ("did not find known key");

	if (dn_umap_size (map) != 0)
		return FAILED ("unexpected size");

	dn_umap_free (map);

	return OK;
}

static RESULT
test_umap_grow (void)
{
	dn_umap_custom_alloc_params_t params = {0, };
	params.hash_func = dn_str_hash;
	params.equal_func = dn_str_equal;
	params.key_dispose_func = free;
	params.value_dispose_func = free;

	dn_umap_t *map = dn_umap_custom_alloc (&params);

	char buffer1 [30];
	char buffer2 [30];
	uint32_t count = 0;

	for (uint32_t i = 0; i < 1000; i++) {
		sprintf (buffer1, "%d", i);
		sprintf (buffer2, "x-%d", i);
		dn_umap_insert (map, strdup (buffer1), strdup (buffer2));
	}

	for (uint32_t i = 0; i < 1000; i++){
		sprintf (buffer1, "%d", i);
		dn_umap_it_t found = dn_umap_find (map, buffer1);
		sprintf (buffer1, "x-%d", i);
		if (strcmp (dn_umap_it_value_t (found, char *), buffer1) != 0)
			return FAILED ("Failed to lookup the key %d, the value was %s\n", i, dn_umap_it_value_t (found, char *));
	}

	if (dn_umap_size (map) != 1000)
		return FAILED ("Did not find 1000 elements on the hash, found %d\n", dn_umap_size (map));

	dn_umap_for_each (map, test_umap_for_each_func, &count);
	if (count != 1000){
		return FAILED ("for each count is not 1000");
	}

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_find_erase (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_insert (map, (char *)items [2], (char *)items [2]);
	dn_umap_insert (map, (char *)items [1], (char *)items [1]);
	dn_umap_insert (map, (char *)items [0], (char *)items [0]);

	const char *data = items [3];
	dn_umap_insert (map, (char *)data, (char *)data);

	dn_umap_it_t found = dn_umap_find (map, data);

	if (dn_umap_it_key_t (found, char *) != data)
		return FAILED ("find failed #1");

	dn_umap_erase (found);

	found = dn_umap_find (map, data);

	if (!dn_umap_it_end (found))
		return FAILED ("find failed #2");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_ptr_uint32_insert_or_assign (void)
{
	dn_umap_result_t result;
	const char *items[] = { "first", "second" };

	dn_umap_t *map = dn_umap_alloc ();

	result = dn_umap_ptr_uint32_insert (map, (void *) items [0], 1);
	if (!result.result || dn_umap_it_key_ptr (result.it) != (void *)items [0] || dn_umap_it_value_uint32_t (result.it) != 1)
		return FAILED ("insert failed #1");

	result = dn_umap_ptr_uint32_insert (map, (void *) items [1], 2);
	if (!result.result || dn_umap_it_key_ptr (result.it) != (void *)items [1] || dn_umap_it_value_uint32_t (result.it) != 2)
		return FAILED ("insert failed #2");

	result = dn_umap_insert_or_assign (map, (void *)items [1], 0);
	if (!result.result || dn_umap_it_key_ptr (result.it) != (void *)items [1] || dn_umap_it_value_uint32_t (result.it) != 0)
		return FAILED ("insert_or_assign failed");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_ptr_uint32_find_erase (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_ptr_uint32_insert (map, (void *)items [2], 3);
	dn_umap_ptr_uint32_insert (map, (void *)items [1], 2);
	dn_umap_ptr_uint32_insert (map, (void *)items [0], 1);

	const void *data = items [3];
	dn_umap_ptr_uint32_insert (map, (void *)data, 4);

	dn_umap_it_t found = dn_umap_ptr_uint32_find (map, data);

	if (dn_umap_it_key_ptr (found) != (void *)data)
		return FAILED ("find failed #1");

	dn_umap_erase (found);

	found = dn_umap_find (map, data);

	if (!dn_umap_it_end (found))
		return FAILED ("find failed #2");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_ptr_uint32_find_erase_insert (void)
{
	const char *items[] = { "first", "second", "third", "fourth"};

	dn_umap_t *map = dn_umap_alloc ();

	dn_umap_ptr_uint32_insert (map, (char *)items [3], 4);
	dn_umap_ptr_uint32_insert (map, (char *)items [2], 3);
	dn_umap_ptr_uint32_insert (map, (char *)items [1], 2);
	dn_umap_ptr_uint32_insert (map, (char *)items [0], 1);

	dn_umap_it_t found = dn_umap_ptr_uint32_find (map, items [1]);

	if (dn_umap_it_end (found))
		return FAILED ("find failed #1");

	if (dn_umap_it_key_ptr (found) != (void *)items [1])
		return FAILED ("find failed #2");

	if (dn_umap_it_value_uint32_t (found) != 2)
		return FAILED ("find failed #3");

	dn_umap_erase (found);

	found = dn_umap_find (map, items [1]);
	if (!dn_umap_it_end (found))
		return FAILED ("find failed #4");

	dn_umap_result_t result = dn_umap_ptr_uint32_insert (map, (char *)items [1], 2);
	if (!result.result || dn_umap_it_end (result.it))
		return FAILED ("insert failed");

	found = dn_umap_find (map, items [1]);
	if (dn_umap_it_end (found))
		return FAILED ("find failed #5");

	dn_umap_free (map);

	return OK;
}

static
RESULT
test_umap_teardown (void)
{
#ifdef _CRTDBG_MAP_ALLOC
	_CrtMemCheckpoint (&dn_umap_memory_end_snapshot);
	if ( _CrtMemDifference(&dn_umap_memory_diff_snapshot, &dn_umap_memory_start_snapshot, &dn_umap_memory_end_snapshot) ) {
		_CrtMemDumpStatistics( &dn_umap_memory_diff_snapshot );
		return FAILED ("memory leak detected!");
	}
#endif
	return OK;
}

static Test dn_umap_tests [] = {
	{"test_umap_setup", test_umap_setup},
	{"test_umap_alloc", test_umap_alloc},
	{"test_umap_init", test_umap_init},
	{"test_umap_free", test_umap_free},
	{"test_umap_dispose", test_umap_dispose},
	{"test_umap_empty", test_umap_empty},
	{"test_umap_size", test_umap_size},
	{"test_umap_max_size", test_umap_max_size},
	{"test_umap_clear", test_umap_clear},
	{"test_umap_insert", test_umap_insert},
	{"test_umap_insert_or_assign", test_umap_insert_or_assign},
	{"test_umap_erase", test_umap_erase},
	{"test_umap_extract", test_umap_extract},
	{"test_umap_find", test_umap_find},
	{"test_umap_find_2", test_umap_find_2},
	{"test_umap_contains", test_umap_contains},
	{"test_umap_rehash", test_umap_rehash},
	{"test_umap_reserve", test_umap_reserve},
	{"test_umap_for_each", test_umap_for_each},
	{"test_umap_iterator", test_umap_iterator},
	{"test_umap_iterator_2", test_umap_iterator_2},
	{"test_umap_str_str_map", test_umap_str_str_map},
	{"test_umap_grow", test_umap_grow},
	{"test_umap_find_erase", test_umap_find_erase},
	{"test_umap_ptr_uint32_insert_or_assign", test_umap_ptr_uint32_insert_or_assign},
	{"test_umap_ptr_uint32_find_erase", test_umap_ptr_uint32_find_erase},
	{"test_umap_ptr_uint32_find_erase_insert", test_umap_ptr_uint32_find_erase_insert},
	{"test_umap_teardown", test_umap_teardown},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(dn_umap_tests_init, dn_umap_tests)
