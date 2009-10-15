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

	if(item_count != NULL) {
		*item_count = i;
	}
	
	return array;
}

static guint guess_size(guint length)
{
	guint size = 1;

	while(size < length) {
		size <<= 1;
	}

	return size;
}

RESULT ptrarray_alloc()
{
	GPtrArrayPriv *array;
	guint i;
	
	array = (GPtrArrayPriv *)ptrarray_alloc_and_fill(&i);
	
	if(array->size != guess_size(array->len)) {
		return FAILED("Size should be %d, but it is %d", 
			guess_size(array->len), array->size);
	}
	
	if(array->len != i) {
		return FAILED("Expected %d node(s) in the array", i);
	}
	
	g_ptr_array_free((GPtrArray *)array, TRUE);

	return OK;
}

RESULT ptrarray_for_iterate()
{
	GPtrArray *array = ptrarray_alloc_and_fill(NULL);
	guint i;

	for(i = 0; i < array->len; i++) {
		char *item = (char *)g_ptr_array_index(array, i);
		if(item != items[i]) {
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

void foreach_callback(gpointer data, gpointer user_data)
{
	char *item = (char *)data;
	const char *item_cmp = items[foreach_iterate_index++];

	if(foreach_iterate_error != NULL) {
		return;
	}

	if(item != item_cmp) {
		foreach_iterate_error = FAILED(
			"Expected item at %d to be %s, but it was %s", 
				foreach_iterate_index - 1, item_cmp, item);
	}
}

RESULT ptrarray_foreach_iterate()
{
	GPtrArray *array = ptrarray_alloc_and_fill(NULL);
	
	foreach_iterate_index = 0;
	foreach_iterate_error = NULL;
	
	g_ptr_array_foreach(array, foreach_callback, array);
	
	g_ptr_array_free(array, TRUE);

	return foreach_iterate_error;
}

RESULT ptrarray_set_size()
{
	GPtrArray *array = g_ptr_array_new();
	guint i, grow_length = 50;
	
	g_ptr_array_add(array, (gpointer)items[0]);
	g_ptr_array_add(array, (gpointer)items[1]);
	g_ptr_array_set_size(array, grow_length);

	if(array->len != grow_length) {
		return FAILED("Array length should be 50, it is %d", array->len);
	} else if(array->pdata[0] != items[0]) {
		return FAILED("Item 0 was overwritten, should be %s", items[0]);
	} else if(array->pdata[1] != items[1]) {
		return FAILED("Item 1 was overwritten, should be %s", items[1]);
	}

	for(i = 2; i < array->len; i++) {
		if(array->pdata[i] != NULL) {
			return FAILED("Item %d is not NULL, it is %p", i, array->pdata[i]);
		}
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

RESULT ptrarray_remove_index()
{
	GPtrArray *array;
	guint i;
	
	array = ptrarray_alloc_and_fill(&i);
	
	g_ptr_array_remove_index(array, 0);
	if(array->pdata[0] != items[1]) {
		return FAILED("First item is not %s, it is %s", items[1],
			array->pdata[0]);
	}

	g_ptr_array_remove_index(array, array->len - 1);
	
	if(array->pdata[array->len - 1] != items[array->len]) {
		return FAILED("Last item is not %s, it is %s", 
			items[array->len - 2], array->pdata[array->len - 1]);
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

RESULT ptrarray_remove_index_fast()
{
	GPtrArray *array;
	guint i;

	array = ptrarray_alloc_and_fill(&i);

	g_ptr_array_remove_index_fast(array, 0);
	if(array->pdata[0] != items[array->len]) {
		return FAILED("First item is not %s, it is %s", items[array->len],
			array->pdata[0]);
	}

	g_ptr_array_remove_index_fast(array, array->len - 1);
	if(array->pdata[array->len - 1] != items[array->len - 1]) {
		return FAILED("Last item is not %s, it is %s",
			items[array->len - 1], array->pdata[array->len - 1]);
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

RESULT ptrarray_remove()
{
	GPtrArray *array;
	guint i;
	
	array = ptrarray_alloc_and_fill(&i);

	g_ptr_array_remove(array, (gpointer)items[7]);

	if(!g_ptr_array_remove(array, (gpointer)items[4])) {
		return FAILED("Item %s not removed", items[4]);
	}

	if(g_ptr_array_remove(array, (gpointer)items[4])) {
		return FAILED("Item %s still in array after removal", items[4]);
	}

	if(array->pdata[array->len - 1] != items[array->len + 1]) {
		return FAILED("Last item in GPtrArray not correct");
	}

	g_ptr_array_free(array, TRUE);

	return OK;
}

static gint ptrarray_sort_compare(gconstpointer a, gconstpointer b)
{
	gchar *stra = *(gchar **) a;
	gchar *strb = *(gchar **) b;
	return strcmp(stra, strb);
}

RESULT ptrarray_sort()
{
	GPtrArray *array = g_ptr_array_new();
	guint i;
	gchar *letters [] = { "A", "B", "C", "D", "E" };
	
	g_ptr_array_add(array, letters[0]);
	g_ptr_array_add(array, letters[1]);
	g_ptr_array_add(array, letters[2]);
	g_ptr_array_add(array, letters[3]);
	g_ptr_array_add(array, letters[4]);
	
	g_ptr_array_sort(array, ptrarray_sort_compare);

	for(i = 0; i < array->len; i++) {
		if(array->pdata[i] != letters[i]) {
			return FAILED("Array out of order, expected %s got %s", 
				(gchar *)array->pdata[i], letters[i]);
		}
	}

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
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ptrarray_tests_init, ptrarray_tests)


