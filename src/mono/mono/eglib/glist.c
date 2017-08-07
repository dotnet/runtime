/*
 * glist.c: Doubly-linked list implementation
 *
 * Authors:
 *   Duncan Mak (duncan@novell.com)
 *   Raja R Harinath (rharinath@novell.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * (C) 2006 Novell, Inc.
 */
#include <stdio.h>
#include <glib.h>

GList*
g_list_alloc ()
{
	return g_new0 (GList, 1);
}

static inline GList*
new_node (GList *prev, gpointer data, GList *next)
{
	GList *node = g_list_alloc ();
	node->data = data;
	node->prev = prev;
	node->next = next;
	if (prev)
		prev->next = node;
	if (next)
		next->prev = node;
	return node;
}

static inline GList*
disconnect_node (GList *node)
{
	if (node->next)
		node->next->prev = node->prev;
	if (node->prev)
		node->prev->next = node->next;
	return node;
}

GList *
g_list_prepend (GList *list, gpointer data)
{
	return new_node (list ? list->prev : NULL, data, list);
}

void
g_list_free_1 (GList *list)
{
	g_free (list);
}

void
g_list_free (GList *list)
{
	while (list){
		GList *next = list->next;
		g_list_free_1 (list);
		list = next;
	}
}

GList*
g_list_append (GList *list, gpointer data)
{
	GList *node = new_node (g_list_last (list), data, NULL);
	return list ? list : node;
}

GList *
g_list_concat (GList *list1, GList *list2)
{
	if (list1 && list2) {
		list2->prev = g_list_last (list1);
		list2->prev->next = list2;
	}
	return list1 ? list1 : list2;
}

guint
g_list_length (GList *list)
{
	guint length = 0;

	while (list) {
		length ++;
		list = list->next;
	}

	return length;
}

GList*
g_list_remove (GList *list, gconstpointer data)
{
	GList *current = g_list_find (list, data);
	if (!current)
		return list;

	if (current == list)
		list = list->next;
	g_list_free_1 (disconnect_node (current));

	return list;
}

GList*
g_list_remove_all (GList *list, gconstpointer data)
{
	GList *current = g_list_find (list, data);

	if (!current)
		return list;

	while (current) {
		if (current == list)
			list = list->next;
		g_list_free_1 (disconnect_node (current));

		current = g_list_find (list, data);
	}

	return list;
}

GList*
g_list_remove_link (GList *list, GList *link)
{
	if (list == link)
		list = list->next;

	disconnect_node (link);
	link->next = NULL;
	link->prev = NULL;

	return list;
}

GList*
g_list_delete_link (GList *list, GList *link)
{
	list = g_list_remove_link (list, link);
	g_list_free_1 (link);

	return list;
}

GList*
g_list_find (GList *list, gconstpointer data)
{
	while (list){
		if (list->data == data)
			return list;

		list = list->next;
	}

	return NULL;
}

GList*
g_list_find_custom (GList *list, gconstpointer data, GCompareFunc func)
{
	if (!func)
		return NULL;
	
	while (list) {
		if (func (list->data, data) == 0)
			return list;
		
		list = list->next;
	}
	
	return NULL;
}

GList*
g_list_reverse (GList *list)
{
	GList *reverse = NULL;

	while (list) {
		reverse = list;
		list = reverse->next;

		reverse->next = reverse->prev;
		reverse->prev = list;
	}

	return reverse;
}

GList*
g_list_first (GList *list)
{
	if (!list)
		return NULL;

	while (list->prev)
		list = list->prev;

	return list;
}

GList*
g_list_last (GList *list)
{
	if (!list)
		return NULL;

	while (list->next)
		list = list->next;

	return list;
}

GList*
g_list_insert_sorted (GList *list, gpointer data, GCompareFunc func)
{
	GList *prev = NULL;
	GList *current;
	GList *node;

	if (!func)
		return list;

	/* Invariant: !prev || func (prev->data, data) <= 0) */
	for (current = list; current; current = current->next) {
		if (func (current->data, data) > 0)
			break;
		prev = current;
	}

	node = new_node (prev, data, current);
	return list == current ? node : list;
}

GList*
g_list_insert_before (GList *list, GList *sibling, gpointer data)
{
	if (sibling) {
		GList *node = new_node (sibling->prev, data, sibling);
		return list == sibling ? node : list;
	}
	return g_list_append (list, data);
}

void
g_list_foreach (GList *list, GFunc func, gpointer user_data)
{
	while (list){
		(*func) (list->data, user_data);
		list = list->next;
	}
}

gint
g_list_index (GList *list, gconstpointer data)
{
	gint index = 0;

	while (list){
		if (list->data == data)
			return index;

		index ++;
		list = list->next;
	}

	return -1;
}

GList*
g_list_nth (GList *list, guint n)
{
	for (; list; list = list->next) {
		if (n == 0)
			break;
		n--;
	}
	return list;
}

gpointer
g_list_nth_data (GList *list, guint n)
{
	GList *node = g_list_nth (list, n);
	return node ? node->data : NULL;
}

GList*
g_list_copy (GList *list)
{
	GList *copy = NULL;

	if (list) {
		GList *tmp = new_node (NULL, list->data, NULL);
		copy = tmp;

		for (list = list->next; list; list = list->next)
			tmp = new_node (tmp, list->data, NULL);
	}

	return copy;
}

typedef GList list_node;
#include "sort.frag.h"

GList*
g_list_sort (GList *list, GCompareFunc func)
{
	GList *current;
	if (!list || !list->next)
		return list;
	list = do_sort (list, func);

	/* Fixup: do_sort doesn't update 'prev' pointers */
	list->prev = NULL;
	for (current = list; current->next; current = current->next)
		current->next->prev = current;

	return list;
}
