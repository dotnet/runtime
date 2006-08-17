#include <stdio.h>
#include <glib.h>
#include "test.h"

char*
compare (GSList *list1, GSList *list2)
{
	while (list1 && list2) {
		if (list1->data != list2->data)
			return "the lists are not equal";

		list1 = list1->next;
		list2 = list2->next;
	}

	return NULL;
}

char*
test_slist_append ()
{
	GSList *list = g_slist_prepend (NULL, "first");
	if (g_slist_length (list) != 1)
		return "Prepend failed";

	g_slist_append (list, g_slist_prepend (NULL, "second"));

	if (g_slist_length (list) != 2)
		return "Append failed";

	return NULL;
}

char *
test_slist_concat ()
{
	GSList *foo = g_slist_prepend (NULL, "foo");
	GSList *bar = g_slist_prepend (NULL, "bar");

	GSList *list = g_slist_concat (foo, bar);

	if (g_slist_length (list) != 2)
		return "Concat failed.";

	return NULL;
}

char*
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

	return NULL;
}

char*
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

	return NULL;
}

char*
test_slist_remove_link ()
{
	GSList *foo = g_slist_prepend (NULL, "a");
	GSList *bar = g_slist_prepend (NULL, "b");
	GSList *baz = g_slist_prepend (NULL, "c");
	GSList *list = foo;

	g_slist_append (foo, bar);
	g_slist_append (foo, baz);	

	list = g_slist_remove_link (list, bar);

	if (g_slist_length (list) != 2)
		return "remove_link failed #1";

	if (bar->next != NULL)
		return "remove_link failed #2";

	return NULL;
}
