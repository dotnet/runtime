#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"


RESULT
test_slist_nth ()
{
	char *foo = "foo";
	char *bar = "bar";
	char *baz = "baz";
	GSList *nth, *list;
	list = g_slist_prepend (NULL, baz);
	list = g_slist_prepend (list, bar);
	list = g_slist_prepend (list, foo);

	nth = g_slist_nth (list, 0);
	if (nth->data != foo)
		return FAILED ("nth failed. #0");

	nth = g_slist_nth (list, 1);
	if (nth->data != bar)
		return FAILED ("nth failed. #1");
	
	nth = g_slist_nth (list, 2);
	if (nth->data != baz)
		return FAILED ("nth failed. #2");

	nth = g_slist_nth (list, 3);
	if (nth)
		return FAILED ("nth failed. #3: %s", nth->data);

	g_slist_free (list);
	return OK;
}

RESULT
test_slist_index ()
{
	int i;
	char *foo = "foo";
	char *bar = "bar";
	char *baz = "baz";
	GSList *list;
	list = g_slist_prepend (NULL, baz);
	list = g_slist_prepend (list, bar);
	list = g_slist_prepend (list, foo);

	i = g_slist_index (list, foo);
	if (i != 0)
		return FAILED ("index failed. #0: %d", i);

	i = g_slist_index (list, bar);
	if (i != 1)
		return FAILED ("index failed. #1: %d", i);
	
	i = g_slist_index (list, baz);
	if (i != 2)
		return FAILED ("index failed. #2: %d", i);

	g_slist_free (list);
	return OK;
}

RESULT
test_slist_append ()
{
	GSList *foo;
	GSList *list = g_slist_append (NULL, "first");
	if (g_slist_length (list) != 1)
		return FAILED ("append(null,...) failed");

	foo = g_slist_append (list, "second");
	if (foo != list)
		return FAILED ("changed list head on non-empty");

	if (g_slist_length (list) != 2)
		return FAILED ("Append failed");

	g_slist_free (list);
	return OK;
}

RESULT
test_slist_concat ()
{
	GSList *foo = g_slist_prepend (NULL, "foo");
	GSList *bar = g_slist_prepend (NULL, "bar");

	GSList *list = g_slist_concat (foo, bar);

	if (g_slist_length (list) != 2)
		return FAILED ("Concat failed.");

	g_slist_free (list);
	return OK;
}

RESULT
test_slist_find ()
{
	GSList *list = g_slist_prepend (NULL, "three");
	GSList *found;
	char *data;
		
	list = g_slist_prepend (list, "two");
	list = g_slist_prepend (list, "one");

	data = "four";
	list = g_slist_append (list, data);

	found = g_slist_find (list, data);

	if (found->data != data)
		return FAILED ("Find failed");

	g_slist_free (list);
	return OK;
}

static gint
find_custom (gconstpointer a, gconstpointer b)
{
	return(strcmp (a, b));
}

RESULT
test_slist_find_custom ()
{
	GSList *list = NULL, *found;
	char *foo = "foo";
	char *bar = "bar";
	char *baz = "baz";
	
	list = g_slist_prepend (list, baz);
	list = g_slist_prepend (list, bar);
	list = g_slist_prepend (list, foo);
	
	found = g_slist_find_custom (list, baz, find_custom);
	
	if (found == NULL)
		return FAILED ("Find failed");
	
	g_slist_free (list);
	
	return OK;
}

RESULT
test_slist_remove ()
{
	GSList *list = g_slist_prepend (NULL, "three");
	char *one = "one";
	list = g_slist_prepend (list, "two");
	list = g_slist_prepend (list, one);

	list = g_slist_remove (list, one);

	if (g_slist_length (list) != 2)
		return FAILED ("Remove failed");

	if (strcmp ("two", list->data) != 0)
		return FAILED ("Remove failed");

	g_slist_free (list);
	return OK;
}

RESULT
test_slist_remove_link ()
{
	GSList *foo = g_slist_prepend (NULL, "a");
	GSList *bar = g_slist_prepend (NULL, "b");
	GSList *baz = g_slist_prepend (NULL, "c");
	GSList *list = foo;

	foo = g_slist_concat (foo, bar);
	foo = g_slist_concat (foo, baz);	

	list = g_slist_remove_link (list, bar);

	if (g_slist_length (list) != 2)
		return FAILED ("remove_link failed #1");

	if (bar->next != NULL)
		return FAILED ("remove_link failed #2");

	g_slist_free (list);	
	g_slist_free (bar);

	return OK;
}

static gint
compare (gconstpointer a, gconstpointer b)
{
	char *foo = (char *) a;
	char *bar = (char *) b;

	if (strlen (foo) < strlen (bar))
		return -1;

	return 1;
}

RESULT
test_slist_insert_sorted ()
{
	GSList *list = g_slist_prepend (NULL, "a");
	list = g_slist_append (list, "aaa");

	/* insert at the middle */
	list = g_slist_insert_sorted (list, "aa", compare);
	if (strcmp ("aa", list->next->data))
		return FAILED("insert_sorted failed #1");

	/* insert at the beginning */
	list = g_slist_insert_sorted (list, "", compare);
	if (strcmp ("", list->data))
		return FAILED ("insert_sorted failed #2");

	/* insert at the end */
	list = g_slist_insert_sorted (list, "aaaa", compare);
	if (strcmp ("aaaa", g_slist_last (list)->data))
		return FAILED ("insert_sorted failed #3");

	g_slist_free (list);	
	return OK;
}

RESULT
test_slist_insert_before ()
{
	GSList *foo, *bar, *baz;

	foo = g_slist_prepend (NULL, "foo");
	foo = g_slist_insert_before (foo, NULL, "bar");
	bar = g_slist_last (foo);

	if (strcmp (bar->data, "bar"))
		return FAILED ("1");

	baz = g_slist_insert_before (foo, bar, "baz");
	if (foo != baz)
		return FAILED ("2");

	if (strcmp (foo->next->data, "baz"))
		return FAILED ("3: %s", foo->next->data);

	g_slist_free (foo);
	return OK;
}

#define N_ELEMS 100

static int intcompare (gconstpointer p1, gconstpointer p2)
{
	return GPOINTER_TO_INT (p1) - GPOINTER_TO_INT (p2);
}

static gboolean verify_sort (GSList *list, int len)
{
	int prev = GPOINTER_TO_INT (list->data);
	len--;
	for (list = list->next; list; list = list->next) {
		int curr = GPOINTER_TO_INT (list->data);
		if (prev > curr)
			return FALSE;
		prev = curr;

		if (len == 0)
			return FALSE;
		len--;
	}
	return len == 0;
}

RESULT
test_slist_sort ()
{
	int i, j, mul;
	GSList *list = NULL;

	for (i = 0; i < N_ELEMS; ++i)
		list = g_slist_prepend (list, GINT_TO_POINTER (i));
	list = g_slist_sort (list, intcompare);
	if (!verify_sort (list, N_ELEMS))
		return FAILED ("decreasing list");

	g_slist_free (list);

	list = NULL;
	for (i = 0; i < N_ELEMS; ++i)
		list = g_slist_prepend (list, GINT_TO_POINTER (-i));
	list = g_slist_sort (list, intcompare);
	if (!verify_sort (list, N_ELEMS))
		return FAILED ("increasing list");

	g_slist_free (list);

	list = g_slist_prepend (NULL, GINT_TO_POINTER (0));
	for (i = 1; i < N_ELEMS; ++i) {
		list = g_slist_prepend (list, GINT_TO_POINTER (-i));
		list = g_slist_prepend (list, GINT_TO_POINTER (i));
	}
	list = g_slist_sort (list, intcompare);
	if (!verify_sort (list, 2*N_ELEMS-1))
		return FAILED ("alternating list");

	g_slist_free (list);

	list = NULL;
	mul = 1;
	for (i = 1; i < N_ELEMS; ++i) {
		mul = -mul;
		for (j = 0; j < i; ++j)
			list = g_slist_prepend (list, GINT_TO_POINTER (mul * j));
	}
	list = g_slist_sort (list, intcompare);
	if (!verify_sort (list, (N_ELEMS*N_ELEMS - N_ELEMS)/2))
		return FAILED ("wavering list");

	g_slist_free (list);

	return OK;
}

static Test slist_tests [] = {
	{"nth", test_slist_nth},
	{"index", test_slist_index},
	{"append", test_slist_append},
	{"concat", test_slist_concat},
	{"find", test_slist_find},
	{"find_custom", test_slist_find_custom},
	{"remove", test_slist_remove},
	{"remove_link", test_slist_remove_link},
	{"insert_sorted", test_slist_insert_sorted},
	{"insert_before", test_slist_insert_before},
	{"sort", test_slist_sort},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(slist_tests_init, slist_tests)

