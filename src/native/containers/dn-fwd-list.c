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

#include "dn-fwd-list.h"

dn_fwd_list_node_t _fwd_list_before_begin_it_node = { 0 };

static dn_fwd_list_node_t *
fwd_list_new_node (
	dn_allocator_t *allocator,
	dn_fwd_list_node_t *node,
	void *data)
{
	dn_fwd_list_node_t *new_node = (dn_fwd_list_node_t *)dn_allocator_alloc (allocator, sizeof (dn_fwd_list_node_t));
	if (DN_UNLIKELY (!new_node))
		return NULL;

	new_node->data = data;
	new_node->next = node;

	return new_node;
}

static dn_fwd_list_node_t *
fwd_list_insert_node_before (
	dn_allocator_t *allocator,
	dn_fwd_list_node_t *node,
	void *data)
{
	return fwd_list_new_node (allocator, node, data);
}

static dn_fwd_list_node_t *
fwd_list_insert_node_after (
	dn_allocator_t *allocator,
	dn_fwd_list_node_t *node,
	void *data)
{
	node->next = fwd_list_insert_node_before (allocator, node->next, data);
	return node->next;
}

static void
fwd_list_free_node (
	dn_allocator_t *allocator,
	dn_fwd_list_node_t *node)
{
	dn_allocator_free (allocator, node);
}

static void
fwd_list_dispose_node (
	dn_allocator_t *allocator,
	dn_fwd_list_dispose_func_t dispose_func,
	dn_fwd_list_node_t *node)
{
	if (node && dispose_func)
		dispose_func (node->data);
	dn_allocator_free (allocator, node);
}

static void
fwd_list_remove_node (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_equal_func_t equal_func,
	dn_fwd_list_dispose_func_t dispose_func)
{
	dn_fwd_list_node_t *current = list->head;
	dn_fwd_list_node_t *prev = current;
	dn_fwd_list_node_t *next;

	while (current) {
		next = current->next;
		if ((equal_func && equal_func (current->data, data)) || (!equal_func && current->data == data)) {
			if (current == list->head) {
				list->head = next;
			} else if (current == list->tail) {
				prev->next = NULL;
				list->tail = prev;
			} else {
				prev->next = next;
			}
			fwd_list_dispose_node (list->_internal._allocator, dispose_func, current);
		} else {
			prev = current;
		}

		current = next;
	}
}

dn_fwd_list_t *
dn_fwd_list_custom_alloc (dn_allocator_t *allocator)
{
	dn_fwd_list_t *list = (dn_fwd_list_t *)dn_allocator_alloc (allocator, sizeof (dn_fwd_list_t));
	if (!dn_fwd_list_custom_init (list, allocator)) {
		dn_allocator_free (allocator, list);
		return NULL;
	}

	return list;
}

bool
dn_fwd_list_custom_init (
	dn_fwd_list_t *list,
	dn_allocator_t *allocator)
{
	if (DN_UNLIKELY (!list))
		return false;

	memset (list, 0, sizeof(dn_fwd_list_t));
	list->_internal._allocator = allocator;

	return true;
}

void
dn_fwd_list_custom_free (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY(!list))
		return;

	dn_fwd_list_custom_dispose (list, dispose_func);
	dn_allocator_free (list->_internal._allocator, list);
}

void
dn_fwd_list_custom_dispose (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY(!list))
		return;

	dn_fwd_list_node_t *current = list->head;
	while (current) {
		dn_fwd_list_node_t *next = current->next;
		if (dispose_func)
			dispose_func (current->data);
		dn_allocator_free (list->_internal._allocator, current);
		current = next;
	}
}

void
dn_fwd_list_custom_clear (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list);

	dn_fwd_list_custom_dispose (list, dispose_func);

	list->head = NULL;
	list->tail = NULL;
}

dn_fwd_list_result_t
dn_fwd_list_insert_after (
	dn_fwd_list_it_t position,
	void *data)
{
	dn_fwd_list_t *list = position._internal._list;

	DN_ASSERT (list);

	if (position.it == &_fwd_list_before_begin_it_node || !list->head) {
		position.it = fwd_list_insert_node_before (list->_internal._allocator, list->head, data);
		list->head = position.it;
	} else if (!position.it) {
		position.it = fwd_list_insert_node_after (list->_internal._allocator, list->tail, data);
	} else {
		position.it = fwd_list_insert_node_after (list->_internal._allocator, position.it, data);
	}

	if (position.it && !position.it->next)
		list->tail = position.it;

	dn_fwd_list_result_t result = { { position.it, {position._internal._list } }, position.it != NULL };
	return result;
}

dn_fwd_list_result_t
dn_fwd_list_insert_range_after (
	dn_fwd_list_it_t position,
	dn_fwd_list_it_t first,
	dn_fwd_list_it_t last)
{
	dn_fwd_list_result_t result = { { position.it, {position._internal._list } }, true };

	if (first.it == last.it)
		return result;

	for (; first.it && first.it != last.it; first.it = first.it->next)
		result = dn_fwd_list_insert_after (position, first.it->data);

	if (last.it)
		result = dn_fwd_list_insert_after (position, last.it->data);

	return result;
}

dn_fwd_list_it_t
dn_fwd_list_custom_erase_after (
	dn_fwd_list_it_t position,
	dn_fwd_list_dispose_func_t dispose_func)
{
	dn_fwd_list_t *list = position._internal._list;

	DN_ASSERT (list && !dn_fwd_list_it_end (position));

	if (position.it == &_fwd_list_before_begin_it_node) {
		if (dispose_func)
			dispose_func (*dn_fwd_list_front (list));
		dn_fwd_list_pop_front (list);
		position.it = list->head;
	} else if (position.it->next) {
		dn_fwd_list_node_t *to_erase = position.it->next;
		position.it->next = position.it->next->next;
		fwd_list_dispose_node (position._internal._list->_internal._allocator, dispose_func, to_erase);
	}

	if (!position.it->next) {
		list->tail = position.it;
		position.it = NULL;
	}

	return position;
}

void
dn_fwd_list_custom_pop_front (
	dn_fwd_list_t *list,
	dn_fwd_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list && list->head);

	dn_fwd_list_node_t *next = list->head->next;
	fwd_list_dispose_node (list->_internal._allocator, dispose_func, list->head);

	list->head = next;
	if (!list->head)
		list->tail = NULL;
}

bool
dn_fwd_list_custom_resize (
	dn_fwd_list_t *list,
	uint32_t count,
	dn_fwd_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list);

	if (count == 0) {
		dn_fwd_list_clear (list);
		return false;
	}

	dn_fwd_list_node_t *current = list->head;
	uint32_t i = 0;
	while (current) {
		i++;
		if (i == count) {
			dn_fwd_list_node_t *to_dispose = current->next;

			while (to_dispose) {
				dn_fwd_list_node_t *next = to_dispose->next;
				fwd_list_dispose_node (list->_internal._allocator, dispose_func, to_dispose);
				to_dispose = next;
			}

			current->next = NULL;
			list->tail = current;
			break;
		}
		current = current->next;
	}

	while (i++ < count)
		dn_fwd_list_insert_after (dn_fwd_list_end (list), NULL);

	return true;
}

void
dn_fwd_list_custom_remove (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list);
	fwd_list_remove_node (list, data, NULL, dispose_func);
}

void
dn_fwd_list_custom_remove_if (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_equal_func_t equal_func,
	dn_fwd_list_dispose_func_t dispose_func)
{
	DN_ASSERT (list && equal_func);
	fwd_list_remove_node (list, data, equal_func, dispose_func);
}

void
dn_fwd_list_reverse (dn_fwd_list_t *list)
{
	DN_ASSERT (list);

	dn_fwd_list_node_t *node = list->head;
	dn_fwd_list_node_t *next;
	dn_fwd_list_node_t *prev = NULL;

	list->tail = list->head;

	while (node) {
		next = node->next;
		node->next = prev;

		prev = node;
		node = next;
	}

	list->head = prev;
}

void
dn_fwd_list_for_each (
	dn_fwd_list_t *list,
	dn_fwd_list_for_each_func_t for_each_func,
	void *user_data)
{
	DN_ASSERT (list && for_each_func);

	for (dn_fwd_list_node_t *it = list->head; it; it = it->next)
		for_each_func (it->data, user_data);
}

typedef dn_fwd_list_node_t list_node;
typedef dn_fwd_list_compare_func_t compare_func_t;
#include "dn-sort-frag.inc"

void
dn_fwd_list_sort (
	dn_fwd_list_t *list,
	dn_fwd_list_compare_func_t compare_func)
{
	DN_ASSERT (list && compare_func);

	if (DN_UNLIKELY (!list->head || !list->head->next))
		return;
	
	list->head = do_sort (list->head, compare_func);

	dn_fwd_list_node_t *current = list->head;
	while (current->next)
		current = current->next;

	list->tail = current;
}

dn_fwd_list_it_t
dn_fwd_list_custom_find (
	dn_fwd_list_t *list,
	const void *data,
	dn_fwd_list_equal_func_t equal_func)
{
	DN_ASSERT (list);

	dn_fwd_list_it_t found = { NULL, { list } };
	for (dn_fwd_list_node_t *it = list->head; it; it = it->next) {
		if ((equal_func && equal_func (it->data, data)) || (!equal_func && it->data == data)) {
			found.it = it;
			break;
		}
	}

	return found;
}
