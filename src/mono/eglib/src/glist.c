/*
 * glist.c: Doubly-linked list implementation
 *
 * Author:
 *   Duncan Mak (duncan@novell.com)
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
_prepend (GList *list, gpointer data)
{
	GList *head = g_list_alloc ();
	head->data = data;
	head->next = list;

	if (list){
		head->prev = list->prev;
		if (list->prev)
			list->prev->next = head;
		
		list->prev = head;
	} else
		head->prev = NULL;
	
	return head;
}

GList *
g_list_prepend (GList *list, gpointer data)
{
	return _prepend (list, data);
}

void
g_list_free_1 (GList *list)
{
	g_free (list);
}

void
g_list_free (GList *list)
{
	GList *next;
	while (list){
		next = list->next;
		g_list_free_1 (list);
		list = next;
	}
}

GList*
g_list_append (GList *list, gpointer data)
{
	return g_list_concat (list, g_list_prepend (NULL, data));
}

static inline GList*
_concat (GList *list1, GList *list2)
{
	GList *last;
	if (!list1 && !list2)
		return NULL;
	
	if (!list1)
		return list2;

	if (!list2)
		return list1;

	last = g_list_last (list1);
	last->next = list2;
	list2->prev = last;

	return list1;
}

GList *
g_list_concat (GList *list1, GList *list2)
{
	return _concat (list1, list2);
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
	return NULL;
}

GList*
g_list_remove_link (GList *list, GList *link)
{
	return NULL;
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
	GList *current = NULL;
	
	if (!func)
		return list;

	if (!list)
		return g_list_prepend (NULL, data);

	if (func (list->data, data) > 0)
		return g_list_prepend (list, data);
	
	current = list->next;

	while (current){
		if (func (current->data, data) > 0){
			current->prev->next = g_list_prepend (current, data);
			break;
		}

		if (current->next == NULL){
			g_list_append (list, data);
			break;
		}
		
		current = current->next;
	}

	return list;
}

GList*
g_list_insert_before (GList *list, GList *sibling, gpointer data)
{
	return NULL;
}

void
g_list_foreach (GList *list, GFunc func, gpointer user_data)
{
	while (list){
		(*func) (list->data, user_data);
		list = list->next;
	}
}

GList*
g_list_sort (GList *list, GCompareFunc func)
{
	return NULL;
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
	guint i = n;
	GList *value = list;
	
	while (i > 0){
		if (value){
			value = value->next;
			i --;
		} else {
			value = NULL;
			break;
		}
	}

	return value;
}

GList*
g_list_copy (GList *list)
{
	GList *copy, *tmp;

	if (!list)
		return NULL;

	copy = g_list_prepend (NULL, list->data);

	if (list->next == NULL)
		return copy;
	
	tmp = copy;

	while (list->next) {
		tmp = g_list_concat (tmp, g_list_prepend (NULL, list->next->data));
		tmp = tmp->next;
		list = list->next;
	}

	return copy;
}

