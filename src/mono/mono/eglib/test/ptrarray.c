#include <stdio.h>
#include <glib.h>
#include "test.h"

/* Redefine the private structure only to verify proper allocations */
typedef struct _GPtrArrayPriv {
	gpointer *pdata;
	guint len;
	guint size;
} GPtrArrayPriv;

/* Don't add more than 32 items to this please */
static const char *items [] = {
	"Apples", "Oranges", "Plumbs", "Goats", "Snorps", "Grapes", 
	"Tickle", "Place", "Coffee", "Cookies", "Cake", "Cheese",
	"Tseng", "Holiday", "Avenue", "Smashing", "Water", "Toilet",
	NULL
};

static GPtrArray *ptrarray_alloc_and_fill(guint *item_count)
{
	GPtrArray *array = g_ptr_array_new();
	gint i;
	
	for(i = 0; items[i] != NULL; i++) {
		g_ptr_array_add(array, (gpointer)items[i]);
	}

	if (item_count != NULL) {
		*item_count = i;
	}
	
	return array;
}

static guint guess_size(guint length)
{
	guint size = 1;

	while (size < length) {
		size <<= 1;
	}

	return size;
}

static RESULT
ptrarray_alloc (void)
{
	GPtrArrayPriv *array;
	guint i;
	
	array = (GPtrArrayPriv *)ptrarray_alloc_and_fill(&i);
	
	if (array->size != guess_size(array->len)) {
		return FAILED("Size should be %d, but it is %d", 
			guess_size(array->len), array->size);
	}
	
	if (array->len != i) {
		return FAILED("Expected %d node(s) in the array", i);
	}
	
	g_ptr_array_free((GPtrArray *)array, TRUE);

	return OK;
}

static
RESULT ptrarray_for_iterate (void)
{
	GPtrArray *array = ptrarray_alloc_and_fill(NULL);
	guint i;

	for (i = 0; i < array->len; i++) {
		char *item = (char *)g_ptr_array_index(array, i);
		if (item != items[i]) {
			return FAILED(
				"Expected item at %d to be %s, but it was %s", 
				i, items[i], item);
		}
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static gint foreach_iterate_index = 0;
static char *foreach_iterate_error = NULL;

static void
foreach_callback (gpointer data, gpointer user_data)
{
	char *item = (char *)data;
	const char *item_cmp = items[foreach_iterate_index++];

	if (foreach_iterate_error != NULL) {
		return;
	}

	if (item != item_cmp) {
		foreach_iterate_error = FAILED(
			"Expected item at %d to be %s, but it was %s", 
				foreach_iterate_index - 1, item_cmp, item);
	}
}

static RESULT
ptrarray_foreach_iterate (void)
{
	GPtrArray *array = ptrarray_alloc_and_fill(NULL);
	
	foreach_iterate_index = 0;
	foreach_iterate_error = NULL;
	
	g_ptr_array_foreach(array, foreach_callback, array);
	
	g_ptr_array_free(array, TRUE);

	return foreach_iterate_error;
}

static RESULT
ptrarray_set_size (void)
{
	GPtrArray *array = g_ptr_array_new();
	guint i, grow_length = 50;
	
	g_ptr_array_add(array, (gpointer)items[0]);
	g_ptr_array_add(array, (gpointer)items[1]);
	g_ptr_array_set_size(array, grow_length);

	if (array->len != grow_length) {
		return FAILED("Array length should be 50, it is %d", array->len);
	} else if (array->pdata[0] != items[0]) {
		return FAILED("Item 0 was overwritten, should be %s", items[0]);
	} else if (array->pdata[1] != items[1]) {
		return FAILED("Item 1 was overwritten, should be %s", items[1]);
	}

	for (i = 2; i < array->len; i++) {
		if (array->pdata[i] != NULL) {
			return FAILED("Item %d is not NULL, it is %p", i, array->pdata[i]);
		}
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static RESULT
ptrarray_remove_index (void)
{
	GPtrArray *array;
	guint i;
	
	array = ptrarray_alloc_and_fill(&i);
	
	g_ptr_array_remove_index(array, 0);
	if (array->pdata[0] != items[1]) {
		return FAILED("First item is not %s, it is %s", items[1],
			array->pdata[0]);
	}

	g_ptr_array_remove_index(array, array->len - 1);
	
	if (array->pdata[array->len - 1] != items[array->len]) {
		return FAILED("Last item is not %s, it is %s", 
			items[array->len - 2], array->pdata[array->len - 1]);
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static RESULT
ptrarray_remove_index_fast (void)
{
	GPtrArray *array;
	guint i;

	array = ptrarray_alloc_and_fill(&i);

	g_ptr_array_remove_index_fast(array, 0);
	if (array->pdata[0] != items[array->len]) {
		return FAILED("First item is not %s, it is %s", items[array->len],
			array->pdata[0]);
	}

	g_ptr_array_remove_index_fast(array, array->len - 1);
	if (array->pdata[array->len - 1] != items[array->len - 1]) {
		return FAILED("Last item is not %s, it is %s",
			items[array->len - 1], array->pdata[array->len - 1]);
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static RESULT
ptrarray_remove (void)
{
	GPtrArray *array;
	guint i;
	
	array = ptrarray_alloc_and_fill(&i);

	g_ptr_array_remove(array, (gpointer)items[7]);

	if (!g_ptr_array_remove(array, (gpointer)items[4])) {
		return FAILED("Item %s not removed", items[4]);
	}

	if (g_ptr_array_remove(array, (gpointer)items[4])) {
		return FAILED("Item %s still in array after removal", items[4]);
	}

	if (array->pdata[array->len - 1] != items[array->len + 1]) {
		return FAILED("Last item in GPtrArray not correct");
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static gint
ptrarray_sort_compare (gconstpointer a, gconstpointer b)
{
	gchar *stra = *(gchar **) a;
	gchar *strb = *(gchar **) b;
	return strcmp(stra, strb);
}

static RESULT
ptrarray_sort (void)
{
	GPtrArray *array = g_ptr_array_new();
	guint i;
	static gchar * const letters [] = { (char*)"A", (char*)"B", (char*)"C", (char*)"D", (char*)"E" };
	
	g_ptr_array_add(array, letters[0]);
	g_ptr_array_add(array, letters[1]);
	g_ptr_array_add(array, letters[2]);
	g_ptr_array_add(array, letters[3]);
	g_ptr_array_add(array, letters[4]);
	
	g_ptr_array_sort(array, ptrarray_sort_compare);

	for (i = 0; i < array->len; i++) {
		if (array->pdata[i] != letters[i]) {
			return FAILED("Array out of order, expected %s got %s at position %d",
				letters [i], (gchar *) array->pdata [i], i);
		}
	}

	g_ptr_array_free(array, TRUE);
	
	return OK;
}

static gint
ptrarray_sort_compare_with_data (gconstpointer a, gconstpointer b, gpointer user_data)
{
	gchar *stra = *(gchar **) a;
	gchar *strb = *(gchar **) b;

	if (strcmp (user_data, "this is the data for qsort") != 0)
		fprintf (stderr, "oops at compare with_data\n");

	return strcmp(stra, strb);
}

static RESULT
ptrarray_sort_with_data (void)
{
	GPtrArray *array = g_ptr_array_new();
	guint i;
	static gchar * const letters [] = { (char*)"A", (char*)"B", (char*)"C", (char*)"D", (char*)"E" };

	g_ptr_array_add(array, letters[4]);
	g_ptr_array_add(array, letters[1]);
	g_ptr_array_add(array, letters[2]);
	g_ptr_array_add(array, letters[0]);
	g_ptr_array_add(array, letters[3]);

	g_ptr_array_sort_with_data(array, ptrarray_sort_compare_with_data, (char*)"this is the data for qsort");

	for (i = 0; i < array->len; i++) {
		if (array->pdata[i] != letters[i]) {
			return FAILED("Array out of order, expected %s got %s at position %d",
				letters [i], (gchar *) array->pdata [i], i);
		}
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static RESULT
ptrarray_remove_fast (void)
{
	GPtrArray *array = g_ptr_array_new();
	static gchar * const letters [] = { (char*)"A", (char*)"B", (char*)"C", (char*)"D", (char*)"E" };
	
	if (g_ptr_array_remove_fast (array, NULL))
		return FAILED ("Removing NULL succeeded");

	g_ptr_array_add(array, letters[0]);
	if (!g_ptr_array_remove_fast (array, letters[0]) || array->len != 0)
		return FAILED ("Removing last element failed");

	g_ptr_array_add(array, letters[0]);
	g_ptr_array_add(array, letters[1]);
	g_ptr_array_add(array, letters[2]);
	g_ptr_array_add(array, letters[3]);
	g_ptr_array_add(array, letters[4]);

	if (!g_ptr_array_remove_fast (array, letters[0]) || array->len != 4)
		return FAILED ("Removing first element failed");

	if (array->pdata [0] != letters [4])
		return FAILED ("First element wasn't replaced with last upon removal");

	if (g_ptr_array_remove_fast (array, letters[0]))
		return FAILED ("Succedeed removing a non-existing element");

	if (!g_ptr_array_remove_fast (array, letters[3]) || array->len != 3)
		return FAILED ("Failed removing \"D\"");

	if (!g_ptr_array_remove_fast (array, letters[1]) || array->len != 2)
		return FAILED ("Failed removing \"B\"");

	if (array->pdata [0] != letters [4] || array->pdata [1] != letters [2])
		return FAILED ("Last two elements are wrong");
	g_ptr_array_free(array, TRUE);
	
	return OK;
}

static Test ptrarray_tests [] = {
	{"alloc", ptrarray_alloc},
	{"for_iterate", ptrarray_for_iterate},
	{"foreach_iterate", ptrarray_foreach_iterate},
	{"set_size", ptrarray_set_size},
	{"remove_index", ptrarray_remove_index},
	{"remove_index_fast", ptrarray_remove_index_fast},
	{"remove", ptrarray_remove},
	{"sort", ptrarray_sort},
	{"remove_fast", ptrarray_remove_fast},
	{"sort_with_data", ptrarray_sort_with_data},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ptrarray_tests_init, ptrarray_tests)
