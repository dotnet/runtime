#include <stdio.h>
#include <string.h>
#include <glib.h>
#include "test.h"

char*
test_list_length ()
{
	GList *list = g_list_prepend (NULL, "foo");

	if (g_list_length (list) != 1)
		return "length failed. #1";

	list = g_list_prepend (list, "bar");
	if (g_list_length (list) != 2)
		return "length failed. #2";

	list = g_list_append (list, "bar");
	if (g_list_length (list) != 3)
		return "length failed. #3";
	
	return NULL;
}

char*
test_list_nth ()
{
	char *foo = "foo";
	char *bar = "bar";
	char *baz = "baz";
	GList *nth, *list;
	list = g_list_prepend (NULL, baz);
	list = g_list_prepend (list, bar);
	list = g_list_prepend (list, foo);

	nth = g_list_nth (list, 0);
	if (nth->data != foo)
		return "nth failed. #1";

	nth = g_list_nth (list, 1);
	if (nth->data != bar)
		return "nth failed. #2";
	
	nth = g_list_nth (list, 2);
	if (nth->data != baz)
		return "nth failed. #3";

	return NULL;
}

char*
test_list_index ()
{
	int i;
	char *foo = "foo";
	char *bar = "bar";
	char *baz = "baz";
	GList *list;
	list = g_list_prepend (NULL, baz);
	list = g_list_prepend (list, bar);
	list = g_list_prepend (list, foo);

	i = g_list_index (list, foo);
	if (i != 0)
		return "index failed. #1";

	i = g_list_index (list, bar);
	if (i != 1)
		return "index failed. #2";
	
	i = g_list_index (list, baz);
	if (i != 2)
		return "index failed. #3";

	return NULL;
}

char*
test_list_append ()
{
	GList *list = g_list_prepend (NULL, "first");
	if (g_list_length (list) != 1)
		return "Prepend failed";

	g_list_append (list, g_list_prepend (NULL, "second"));

	if (g_list_length (list) != 2)
		return "Append failed";

	return NULL;
}
char *
test_list_last ()
{
	GList *foo = g_list_prepend (NULL, "foo");
	GList *bar = g_list_prepend (NULL, "bar");
	GList *last;
	
	foo = g_list_concat (foo, bar);
	last = g_list_last (foo);

	if (last != bar)
		return "last failed. #1";

	foo = g_list_concat (foo, g_list_prepend (NULL, "baz"));
	foo = g_list_concat (foo, g_list_prepend (NULL, "quux"));

	last = g_list_last (foo);	
	if (strcmp ("quux", last->data))
		return "last failed. #2";

	return NULL;
}

char *
test_list_concat ()
{
	GList *foo = g_list_prepend (NULL, "foo");
	GList *bar = g_list_prepend (NULL, "bar");
	GList *list = g_list_concat (foo, bar);

	if (g_list_length (list) != 2)
		return "Concat failed. #1";

	if (strcmp (list->data, "foo"))
		return "Concat failed. #2";

	if (strcmp (list->next->data, "bar"))
		return "Concat failed. #3";

	if (g_list_first (list) != foo)
		return "Concat failed. #4";
	
	if (g_list_last (list) != bar)
		return "Concat failed. #5";

	return NULL;
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

char*
test_list_insert_sorted ()
{
	GList *list = g_list_prepend (NULL, "a");
	list = g_list_append (list, "aaa");

	/* insert at the middle */
	list = g_list_insert_sorted (list, "aa", compare);
	if (strcmp ("aa", list->next->data))
		return result ("insert_sorted failed. #1");

	/* insert at the beginning */
	list = g_list_insert_sorted (list, "", compare);
	
	if (strcmp ("", list->data))
		return result ("insert_sorted failed. #2");		

	/* insert at the end */
	list = g_list_insert_sorted (list, "aaaa", compare);
	if (strcmp ("aaaa", g_list_last (list)->data))
		return result ("insert_sorted failed. #3");

	return NULL;
}

char *
test_list_copy ()
{
	int i, length;
	GList *list, *copy;
	list = g_list_prepend (NULL, "a");
	list = g_list_append  (list, "aa");
	list = g_list_append  (list, "aaa");
	list = g_list_append  (list, "aaaa");

	length = g_list_length (list);
	copy = g_list_copy (list);

	for (i = 0; i < length; i++)
		if (strcmp (g_list_nth (list, i)->data,
			    g_list_nth (copy, i)->data))
			return "copy failed.";
	return NULL;
}

char *
test_list_reverse ()
{
	guint i, length;
	GList *list, *reverse;
	list = g_list_prepend (NULL, "a");
	list = g_list_append  (list, "aa");
	list = g_list_append  (list, "aaa");
	list = g_list_append  (list, "aaaa");

	length  = g_list_length (list);
	reverse = g_list_reverse (g_list_copy (list));

	if (g_list_length (reverse) != length)
		return "reverse failed #1";

	for (i = 0; i < length; i++){
		guint j = length - i - 1;
		if (strcmp (g_list_nth (list, i)->data,
			    g_list_nth (reverse, j)->data))
			return "reverse failed. #2";
	}
	return NULL;
}

static Test list_tests [] = {
	{"list_length", test_list_length},
	{"list_nth", test_list_nth},
	{"list_index", test_list_index},	
	{"list_last", test_list_last},	
	{"list_append", test_list_append},
	{"list_concat", test_list_concat},
	{"list_insert_sorted", test_list_insert_sorted},
	{"list_copy", test_list_copy},
	{"list_reverse", test_list_reverse},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(list_tests_init, list_tests)
