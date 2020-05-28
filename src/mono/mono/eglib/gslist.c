/*
 * gslist.c: Singly-linked list implementation
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
#include "config.h"
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

/*
 * Insert the given data in a new node after the current node. 
 * Return new node.
 */
static GSList *
insert_after (GSList *list, gpointer data)
{
	list->next = g_slist_prepend (list->next, data);
	return list->next;
}

/*
 * Return the node prior to the node containing 'data'.
 * If the list is empty, or the first node contains 'data', return NULL.
 * If no node contains 'data', return the last node.
 */
static GSList*
find_prev (GSList *list, gconstpointer data)
{
	GSList *prev = NULL;
	while (list) {
		if (list->data == data)
			break;
		prev = list;
		list = list->next;
	}
	return prev;
}

/* like 'find_prev', but searches for node 'link' */
static GSList*
find_prev_link (GSList *list, GSList *link)
{
	GSList *prev = NULL;
	while (list) {
		if (list == link)
			break;
		prev = list;
		list = list->next;
	}
	return prev;
}

GSList*
g_slist_insert_before (GSList *list, GSList *sibling, gpointer data)
{
	GSList *prev = find_prev_link (list, sibling);

	if (!prev)
		return g_slist_prepend (list, data);

	insert_after (prev, data);
	return list;
}

void
g_slist_free (GSList *list)
{
	while (list) {
		GSList *next = list->next;
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

	for (list = list->next; list; list = list->next)
		tmp = insert_after (tmp, list->data);

	return copy;
}

GSList*
g_slist_concat (GSList *list1, GSList *list2)
{
	if (!list1)
		return list2;

	g_slist_last (list1)->next = list2;
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
	for (; list; list = list->next)
		if (list->data == data)
			break;
	return list;
}

GSList *
g_slist_find_custom (GSList *list, gconstpointer data, GCompareFunc func)
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
	GSList *prev = find_prev (list, data);
	GSList *current = prev ? prev->next : list;

	if (current) {
		if (prev)
			prev->next = current->next;
		else
			list = current->next;
		g_slist_free_1 (current);
	}

	return list;
}

GSList*
g_slist_remove_all (GSList *list, gconstpointer data)
{
	GSList *next = list;
	GSList *prev = NULL;
	GSList *current;

	while (next) {
		GSList *tmp_prev = find_prev (next, data);
		if (tmp_prev)
			prev = tmp_prev;
		current = prev ? prev->next : list;

		if (!current)
			break;

		next = current->next;

		if (prev)
			prev->next = next;
		else
			list = next;
		g_slist_free_1 (current);
	}

	return list;
}

GSList*
g_slist_remove_link (GSList *list, GSList *link)
{
	GSList *prev = find_prev_link (list, link);
	GSList *current = prev ? prev->next : list;

	if (current) {
		if (prev)
			prev->next = current->next;
		else
			list = current->next;
		current->next = NULL;
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
	GSList *prev = NULL;
	
	if (!func)
		return list;

	if (!list || func (list->data, data) > 0)
		return g_slist_prepend (list, data);

	/* Invariant: func (prev->data, data) <= 0) */
	for (prev = list; prev->next; prev = prev->next)
		if (func (prev->next->data, data) > 0)
			break;

	/* ... && (prev->next == 0 || func (prev->next->data, data) > 0)) */
	insert_after (prev, data);
	return list;
}

gint
g_slist_index (GSList *list, gconstpointer data)
{
	gint index = 0;
	
	while (list) {
		if (list->data == data)
			return index;
		
		index++;
		list = list->next;
	}
	
	return -1;
}

GSList*
g_slist_nth (GSList *list, guint n)
{
	for (; list; list = list->next) {
		if (n == 0)
			break;
		n--;
	}
	return list;
}

gpointer
g_slist_nth_data (GSList *list, guint n)
{
	GSList *node = g_slist_nth (list, n);
	return node ? node->data : NULL;
}

typedef GSList list_node;
#include "sort.frag.h"

GSList*
g_slist_sort (GSList *list, GCompareFunc func)
{
	if (!list || !list->next)
		return list;
	return do_sort (list, func);
}
