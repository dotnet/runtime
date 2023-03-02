// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* (C) 2006 Novell, Inc.
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
 */

#include "dn-list.h"

static dn_list_node_t *
list_new_node (
	dn_allocator_t *allocator,
	dn_list_node_t *prev,
	dn_list_node_t *next,
	void *data)
{
	dn_list_node_t *node = (dn_list_node_t *)dn_allocator_alloc (allocator, sizeof (dn_list_node_t));
	if (DN_UNLIKELY (!node))
		return NULL;

	node->data = data;
	node->prev = prev;
	node->next = next;

	if (prev)
		prev->next = node;
	if (next)
		next->prev = node;

	return node;
}

static dn_list_node_t*
list_unlink_node (dn_list_node_t *node)
{
	if (node->next)
		node->next->prev = node->prev;
	if (node->prev)
		node->prev->next = node->next;

	return node;
}


static dn_list_node_t *
list_insert_node_before (
	dn_allocator_t *allocator,
	dn_list_node_t *node,
	void *data)
{
	return list_new_node (allocator, node ? node->prev : NULL, node, data);
}

static dn_list_node_t *
list_insert_node_after (
	dn_allocator_t *allocator,
	dn_list_node_t *node,
	void *data)
{
	return list_new_node (allocator, node, node ? node->next : NULL, data);
}

static void
list_free_node (
	dn_allocator_t *allocator,
	dn_list_node_t *node)
{
	dn_allocator_free (allocator, node);
}

static void
list_dispose_node (
	dn_allocator_t *allocator,
	dn_list_dispose_func_t dispose_func,
	dn_list_node_t *node)
{
	if (node && dispose_func)
		dispose_func (node->data);
	list_free_node (allocator, node);
}

static void
list_remove_node (
	dn_list_t *list,
	const void * data,
	dn_list_equal_func_t equal_func,
	dn_list_dispose_func_t dispose_func)
{
	dn_list_node_t *current = list->head;
	dn_list_node_t *next;

	while (current) {
		next = current->next;
		if ((equal_func && equal_func (current->data, data)) || (!equal_func && current->data == data)) {
			if (current == list->head)
				list->head = next;
			if (current == list->tail)
				list->tail = current->prev;
			list_dispose_node (list->_internal._allocator, dispose_func, list_unlink_node (current));
		}
		current = next;
	}
}

dn_list_t *
dn_list_custom_alloc (dn_allocator_t *allocator)
{
	dn_list_t *list = (dn_list_t *)dn_allocator_alloc (allocator, sizeof (dn_list_t));
	if (!dn_list_custom_init (list, allocator)) {
		dn_allocator_free (allocator, list);
		return NULL;
	}

	return list;
}

bool
dn_list_custom_init (
	dn_list_t *list,
	dn_allocator_t *allocator)
{
	if (DN_UNLIKELY (!list))
		return false;

	memset (list, 0, sizeof(dn_list_t));
	list->_internal._allocator = allocator;

	return true;
}

void
dn_list_custom_free (
	dn_list_t *list,
	dn_list_dispose_func_t dispose_func)
{
	if (list) {
		dn_list_custom_dispose (list, dispose_func);
		dn_allocator_free (list->_internal._allocator, list);
	}
}

void
dn_list_custom_dispose (
	dn_list_t *list,
	dn_list_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY(!list))
		return;

	dn_list_node_t *current = list->head;
	while (current) {
		dn_list_node_t *next = current->next;
		list_dispose_node (list->_internal._allocator, dispose_func, current);
		current = next;
	}
}

uint32_t
dn_list_size (const dn_list_t *list)
{
	DN_ASSERT (list);

	uint32_t size = 0;
	dn_list_node_t *nodes = list->head;

	while (nodes) {
		size ++;
		nodes = nodes->next;
	}

	return size;
}

void
dn_list_custom_clear (
	dn_list_t *list,
	dn_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list);

	dn_list_custom_dispose (list, dispose_func);

	list->head = NULL;
	list->tail = NULL;
}

dn_list_result_t
dn_list_insert (
	dn_list_it_t position,
	void *data)
{
	dn_list_t *list = position._internal._list;

	DN_ASSERT (list);

	if (!list->head)
		position.it = list_insert_node_before (list->_internal._allocator, list->head, data);
	else if (!position.it)
		position.it = list_insert_node_after (list->_internal._allocator, list->tail, data);
	else
		position.it = list_insert_node_before (list->_internal._allocator, position.it, data);

	if (position.it) {
		if (!position.it->prev)
			list->head = position.it;
		if (!position.it->next)
			list->tail = position.it;
	}

	dn_list_result_t result = { { position.it, {position._internal._list } }, position.it != NULL };
	return result;
}

dn_list_result_t
dn_list_insert_range (
	dn_list_it_t position,
	dn_list_it_t first,
	dn_list_it_t last)
{
	dn_list_result_t first_inserted = { { position.it, {position._internal._list } }, true };

	if (first.it == last.it)
		return first_inserted;

	DN_ASSERT (first.it);

	first_inserted = dn_list_insert (position, first.it->data);

	for (first.it = first.it->next; first.it && first.it != last.it; first.it = first.it->next)
		dn_list_insert (position, first.it->data);

	if (last.it)
		dn_list_insert (position, last.it->data);

	return first_inserted;
}

dn_list_it_t
dn_list_custom_erase (
	dn_list_it_t position,
	dn_list_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY(!position.it))
		return position;

	dn_list_t *list = position._internal._list;

	DN_ASSERT (list && !dn_list_it_end (position));

	if (position.it == list->head) {
		if (dispose_func)
			dispose_func (*dn_list_front (list));
		dn_list_pop_front (list);
		position.it = list->head;
	} else if (position.it == list->tail) {
		if (dispose_func)
			dispose_func (*dn_list_back (list));
		dn_list_pop_back (list);
		position.it = NULL;
	} else if (position.it) {
		dn_list_node_t *to_remove = position.it;
		position.it = position.it->next;
		list_dispose_node (list->_internal._allocator, dispose_func, list_unlink_node (to_remove));
	}

	return position;
}

void
dn_list_custom_pop_back (
	dn_list_t *list,
	dn_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list && list->tail);

	dn_list_node_t *prev = list->tail->prev;
	list_dispose_node (list->_internal._allocator, dispose_func, list_unlink_node (list->tail));

	list->tail = prev;
	if (!list->tail)
		list->head = NULL;
}

void
dn_list_custom_pop_front (
	dn_list_t *list,
	dn_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list && list->head);

	dn_list_node_t *next = list->head->next;
	list_dispose_node (list->_internal._allocator, dispose_func, list_unlink_node (list->head));

	list->head = next;
	if (!list->head)
		list->tail = NULL;
}

bool
dn_list_custom_resize (
	dn_list_t *list,
	uint32_t count,
	dn_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list);

	if (count == 0) {
		dn_list_custom_clear (list, dispose_func);
		return true;
	}

	dn_list_node_t *current = list->head;
	uint32_t i = 0;
	while (current) {
		i++;
		if (i == count) {
			dn_list_node_t *to_dispose = current->next;
			while (to_dispose) {
				dn_list_node_t *next = to_dispose->next;
				list_dispose_node (list->_internal._allocator, dispose_func, list_unlink_node (to_dispose));
				to_dispose = next;
			}
			list->tail = current;
			break;
		}
		current = current->next;
	}

	while (i++ < count)
		dn_list_insert (dn_list_end (list), NULL);

	return true;
}

void
dn_list_custom_remove (
	dn_list_t *list,
	const void *data,
	dn_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list);
	list_remove_node (list, data, NULL, dispose_func);
}

void
dn_list_custom_remove_if (
	dn_list_t *list,
	const void * data,
	dn_list_equal_func_t equal_func,
	dn_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list && equal_func);
	list_remove_node (list, data, equal_func, dispose_func);
}

void
dn_list_reverse (dn_list_t *list)
{
	DN_ASSERT (list);

	dn_list_node_t *node = list->head;
	dn_list_node_t *reverse;

	list->head = list->tail;
	list->tail = node;

	while (node) {
		reverse = node;
		node = reverse->next;

		reverse->next = reverse->prev;
		reverse->prev = node;
	}
}

void
dn_list_for_each (
	dn_list_t *list,
	dn_list_for_each_func_t for_each_func,
	void *user_data)
{
	DN_ASSERT (list && for_each_func);

	for (dn_list_node_t *it = list->head; it; it = it->next)
		for_each_func (it->data, user_data);
}

typedef dn_list_node_t list_node;
typedef dn_list_compare_func_t compare_func_t;
#include "dn-sort-frag.inc"

void
dn_list_sort (
	dn_list_t *list,
	dn_list_compare_func_t compare_func)
{
	DN_ASSERT (list && compare_func);

	if (DN_UNLIKELY (!list->head || !list->head->next))
		return;

	list->head = do_sort (list->head, compare_func);
	list->head->prev = NULL;

	dn_list_node_t *current;
	for (current = list->head; current->next; current = current->next)
		current->next->prev = current;

	list->tail = current;
}

dn_list_it_t
dn_list_custom_find (
	dn_list_t *list,
	const void *data,
	dn_list_equal_func_t equal_func)
{
	DN_ASSERT (list);

	dn_list_it_t found = { NULL, { list } };
	for (dn_list_node_t *it = list->head; it; it = it->next) {
		if ((equal_func && equal_func (it->data, data)) || (!equal_func && it->data == data)) {
			found.it = it;
			break;
		}
	}

	return found;
}
