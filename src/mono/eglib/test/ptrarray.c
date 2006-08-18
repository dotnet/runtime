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

static GPtrArray *ptrarray_alloc_and_fill(gint *item_count)
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

static gint guess_size(gint length)
{
	gint size = 1;

	while(size < length) {
		size <<= 1;
	}

	return size;
}

char *ptrarray_alloc()
{
	GPtrArrayPriv *array;
	gint i;
	
	array = (GPtrArrayPriv *)ptrarray_alloc_and_fill(&i);
	
	if(array->size != guess_size(array->len)) {
		return result("Size should be %d, but it is %d", 
			guess_size(array->len), array->size);
	}
	
	if(array->len != i) {
		return result("Expected %d node(s) in the array", i);
	}
	
	g_ptr_array_free((GPtrArray *)array, TRUE);

	return NULL;
}

char *ptrarray_for_iterate()
{
	GPtrArray *array = ptrarray_alloc_and_fill(NULL);
	gint i;

	for(i = 0; i < array->len; i++) {
		char *item = (char *)g_ptr_array_index(array, i);
		if(item != items[i]) {
			return result(
				"Expected item at %d to be %s, but it was %s", 
				i, items[i], item);
		}
	}

	g_ptr_array_free(array, TRUE);

	return NULL;
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
		foreach_iterate_error = result(
			"Expected item at %d to be %s, but it was %s", 
				foreach_iterate_index - 1, item_cmp, item);
	}
}

char *ptrarray_foreach_iterate()
{
	GPtrArray *array = ptrarray_alloc_and_fill(NULL);
	
	foreach_iterate_index = 0;
	foreach_iterate_error = NULL;
	
	g_ptr_array_foreach(array, foreach_callback, array);
	
	g_ptr_array_free(array, TRUE);

	return foreach_iterate_error;
}

char *ptrarray_set_size()
{
	GPtrArray *array = g_ptr_array_new();
	gint i, grow_length = 50;
	
	g_ptr_array_add(array, (gpointer)items[0]);
	g_ptr_array_add(array, (gpointer)items[1]);
	g_ptr_array_set_size(array, grow_length);

	if(array->len != grow_length) {
		return result("Array length should be 50, it is %d", array->len);
	} else if(array->pdata[0] != items[0]) {
		return result("Item 0 was overwritten, should be %s", items[0]);
	} else if(array->pdata[1] != items[1]) {
		return result("Item 1 was overwritten, should be %s", items[1]);
	}

	for(i = 2; i < array->len; i++) {
		if(array->pdata[i] != NULL) {
			return result("Item %d is not NULL, it is %p", i, array->pdata[i]);
		}
	}

	g_ptr_array_free(array, TRUE);

	return NULL;
}

char *ptrarray_remove_index()
{
	GPtrArray *array;
	gint i;
	
	array = ptrarray_alloc_and_fill(&i);
	
	g_ptr_array_remove_index(array, 0);
	if(array->pdata[0] != items[1]) {
		return result("First item is not %s, it is %s", items[1],
			array->pdata[0]);
	}

	g_ptr_array_remove_index(array, array->len - 1);
	
	if(array->pdata[array->len - 1] != items[array->len]) {
		return result("Last item is not %s, it is %s", 
			items[array->len - 2], array->pdata[array->len - 1]);
	}

	return NULL;
}

char *ptrarray_remove()
{
	GPtrArray *array;
	gint i;
	
	array = ptrarray_alloc_and_fill(&i);

	g_ptr_array_remove(array, (gpointer)items[7]);

	if(!g_ptr_array_remove(array, (gpointer)items[4])) {
		return result("Item %s not removed", items[4]);
	}

	if(g_ptr_array_remove(array, (gpointer)items[4])) {
		return result("Item %s still in array after removal", items[4]);
	}

	if(array->pdata[array->len - 1] != items[array->len + 1]) {
		return result("Last item in GPtrArray not correct");
	}

	return NULL;
}

static gint ptrarray_sort_compare(gconstpointer a, gconstpointer b)
{
	return strcmp(a, b);
}

char *ptrarray_sort()
{
	GPtrArray *array = g_ptr_array_new();
	gint i;
	static const gchar *letters [] = { "A", "B", "C", "D", "E" };
	
	g_ptr_array_add(array, "E");
	g_ptr_array_add(array, "C");
	g_ptr_array_add(array, "A");
	g_ptr_array_add(array, "D");
	g_ptr_array_add(array, "B");

	g_ptr_array_sort(array, ptrarray_sort_compare);

	for(i = 0; i < array->len; i++) {
		if(strcmp((gchar *)array->pdata[i], letters[i]) != 0) {
			return result("Array out of order, expected %s got %s", 
				(gchar *)array->pdata[i], letters[i]);
		}
	}

	g_ptr_array_free(array, TRUE);
	
	return NULL;
}

static Test ptrarray_tests [] = {
	{"ptrarray_alloc", ptrarray_alloc},
	{"ptrarray_for_iterate", ptrarray_for_iterate},
	{"ptrarray_foreach_iterate", ptrarray_foreach_iterate},
	{"ptrarray_set_size", ptrarray_set_size},
	{"ptrarray_remove_index", ptrarray_remove_index},
	{"ptrarray_remove", ptrarray_remove},
	{"ptrarray_sort", ptrarray_sort},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ptrarray_tests_init, ptrarray_tests)

