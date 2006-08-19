#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

RESULT
test_slist_append ()
{
	GSList *list = g_slist_prepend (NULL, "first");
	if (g_slist_length (list) != 1)
		return "Prepend failed";

	g_slist_append (list, g_slist_prepend (NULL, "second"));

	if (g_slist_length (list) != 2)
		return "Append failed";

	return OK;
}

RESULT
test_slist_concat ()
{
	GSList *foo = g_slist_prepend (NULL, "foo");
	GSList *bar = g_slist_prepend (NULL, "bar");

	GSList *list = g_slist_concat (foo, bar);

	if (g_slist_length (list) != 2)
		return "Concat failed.";

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
		return "Find failed";

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
		return "Remove failed";

	if (strcmp ("two", list->data) != 0)
		return "Remove failed";

	return OK;
}

RESULT
test_slist_remove_link ()
{
	GSList *foo = g_slist_prepend (NULL, "a");
	GSList *bar = g_slist_prepend (NULL, "b");
	GSList *baz = g_slist_prepend (NULL, "c");
	GSList *list = foo;

	g_slist_concat (foo, bar);
	g_slist_concat (foo, baz);	

	list = g_slist_remove_link (list, bar);

	if (g_slist_length (list) != 2)
		return FAILED ("remove_link failed #1");

	if (bar->next != NULL)
		return FAILED ("remove_link failed #2");

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

	return OK;
}

static Test slist_tests [] = {
	{"slist_append", test_slist_append},
	{"slist_concat", test_slist_concat},
	{"slist_find", test_slist_find},
	{"slist_remove", test_slist_remove},
	{"slist_remove_link", test_slist_remove_link},
	{"slist_insert_sorted", test_slist_insert_sorted},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(slist_tests_init, slist_tests)

