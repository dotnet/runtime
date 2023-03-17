// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* Copyright (c) 2006-2009 Novell, Inc.
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

#include "dn-queue.h"

dn_queue_t *
dn_queue_custom_alloc (dn_allocator_t *allocator)
{
	dn_queue_t *queue = (dn_queue_t *)dn_allocator_alloc (allocator, sizeof (dn_queue_t));
	if (!dn_queue_custom_init (queue, allocator)) {
		dn_allocator_free (allocator, queue);
		return NULL;
	}

	return queue;
}

bool
dn_queue_custom_init (
	dn_queue_t *queue,
	dn_allocator_t *allocator)
{
	if (DN_UNLIKELY (!queue))
		return false;

	memset (queue, 0, sizeof(dn_queue_t));
	dn_list_custom_init (&queue->_internal.list, allocator);

	return true;
}

void
dn_queue_custom_free (
	dn_queue_t *queue,
	dn_queue_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY (!queue))
		return;

	dn_allocator_t *allocator = queue->_internal.list._internal._allocator;
	dn_list_custom_dispose (&queue->_internal.list, dispose_func);
	dn_allocator_free (allocator, queue);
}

void
dn_queue_custom_dispose (
	dn_queue_t *queue,
	dn_queue_dispose_func_t dispose_func)
{
	if (DN_UNLIKELY(!queue))
		return;

	dn_list_custom_dispose (&queue->_internal.list, dispose_func);
}
