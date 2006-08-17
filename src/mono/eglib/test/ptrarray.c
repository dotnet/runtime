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
		return g_strdup_printf("Size should be %d, but it is %d", 
			guess_size(array->len), array->size);
	}
	
	if(array->len != i) {
		return g_strdup_printf("Expected %d node(s) in the array", i);
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
			return g_strdup_printf(
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
		foreach_iterate_error = g_strdup_printf(
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

static Test ptrarray_tests [] = {
	{"ptrarray_alloc", ptrarray_alloc},
	{"ptrarray_for_iterate", ptrarray_for_iterate},
	{"ptrarray_foreach_iterate", ptrarray_foreach_iterate},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(ptrarray_tests_init, ptrarray_tests)

