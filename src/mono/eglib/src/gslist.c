/*
 * gslist.c: Singly-linked list implementation
 *
 * Author:
 *   Duncan Mak (duncan@a-chinaman.com)
 *
 * (C) 2006 Novell, Inc.
 */

#include <stdio.h>
#include <glib.h>

GSList*
g_slist_alloc (void)
{
	return g_new0 (GSList, 1);
}

GSList*
g_slist_append (GSList *list, gpointer data)
{
	GSList *value = g_slist_alloc ();
	value->data = data;
	value->next = NULL;

	if (list) {
		GSList *last = g_slist_last (list);
		last->next = value;
		value = list;
	}

	return value;
}

GSList*
g_slist_prepend (GSList *list, gpointer data)
{
	GSList *head = g_slist_alloc ();
	head->data = data;
	head->next = list;

	return head;
}

void
g_slist_free (GSList *list)
{
	while (list) {
		g_free (list->data);
		list = list->next;
	}
}

GSList*
g_slist_copy (GSList *list)
{
	if (!list)
		return NULL;

	GSList *copy = g_slist_alloc ();
	GSList *tmp = copy;	
	copy->data = list->data;

	while (list->next) {
		GSList *value = g_slist_alloc ();
		value->data = list->next->data;
		value->next = NULL;
		
		tmp->next = value;
		tmp = value;
		list = list->next;
	}

	return copy;
}

GSList*
g_slist_concat (GSList *list1, GSList *list2)
{
	if (list1)
		g_slist_last (list1)->next = list2;
	else
		list1 = list2;

	return list1;
}

void
g_slist_foreach (GSList *list, GFunc func, gpointer user_data)
{
	if (!list)
		return;
	
	while (list) {
		(*func) (list->data, user_data);
		list = list->next;
	}
}

GSList*
g_slist_last (GSList *list)
{
	GSList *last = list;

	if (!list)
		return NULL;
	
	while (last)
		last = last->next;
	
	return last;
}
