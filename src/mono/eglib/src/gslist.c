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

void
g_slist_free_1 (GSList *list)
{
	g_free (list);
}

GSList*
g_slist_append (GSList *list, gpointer data)
{
	return g_slist_concat (list, g_slist_prepend (NULL, data));
}

/* This is also a list node constructor. */
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
	GSList *next;
	while (list) {
		next = list->next;
		g_slist_free_1 (list);
		list = next;
	}
}

GSList*
g_slist_copy (GSList *list)
{
	GSList *copy, *tmp;

	if (!list)
		return NULL;

	copy = g_slist_prepend (NULL, list->data);
	tmp = copy;

	while (list->next) {
		tmp->next = g_slist_prepend (NULL, list->next->data);

		tmp = tmp->next;
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
	while (list) {
		(*func) (list->data, user_data);
		list = list->next;
	}
}

GSList*
g_slist_last (GSList *list)
{
	if (!list)
		return NULL;

	while (list->next)
		list = list->next;

	return list;
}

GSList*
g_slist_find (GSList *list, gconstpointer data)
{
	while (list){
		if (list->data == data)
			return list;

		list = list->next;
	}

	return NULL;
}

guint
g_slist_length (GSList *list)
{
	guint length = 0;

	while (list) {
		length ++;
		list = list->next;
	}

	return length;
}

GSList*
g_slist_remove (GSList *list, gconstpointer data)
{
	GSList *prev = NULL;
	GSList *current = NULL;
	
	if (!list)
		return NULL;

	if (list->data == data)
		return list->next;

	prev = list;
	current = list->next;

	while (current) {
		if (current->data == data){
			prev->next = current->next;
			g_slist_free_1 (current);

			break;
		}
		prev = current;
		current = current->next;
	}

	return list;
}


GSList*
g_slist_remove_link (GSList *list, GSList *link)
{
	GSList *prev = NULL;
	GSList *current = NULL;

	if (!list)
		return NULL;

	if (list == link) {
		GSList *next = list->next;
		list->next = NULL;
		return next;
	}
	
	prev = list;
	current = list->next;

	while (current){
		if (current == link){
			prev->next = current->next;
			current->next = NULL;
			break;
		}

		prev = current;
		current = current->next;
	}

	return list;
}

GSList*
g_slist_delete_link (GSList *list, GSList *link)
{
	list = g_slist_remove_link (list, link);
	g_slist_free_1 (link);

	return list;
}

GSList*
g_slist_reverse (GSList *list)
{
	GSList *prev = NULL;
	while (list){
		GSList *next = list->next;
		list->next = prev;
		prev = list;
		list = next;
	}

	return prev;
}

GSList*
g_slist_insert_sorted (GSList *list, gpointer data, GCompareFunc func)
{
	GSList *current = NULL;
	GSList *prev = NULL;
	
	if (!func)
		return list;

	if (!list)
		return g_slist_prepend (NULL, data);

	if (func (list->data, data) > 0)
		return g_slist_prepend (list, data);
	
	prev = list;
	current = list->next;

	while (current){
		if (func (current->data, data) > 0){
			prev->next = g_slist_prepend (current, data);
			break;
		}
		current = current->next;
		prev = current;
	}

	return list;
}
