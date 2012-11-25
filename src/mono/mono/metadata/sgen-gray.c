/*
 * sgen-gray.c: Gray queue management.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "utils/mono-counters.h"

#define GRAY_QUEUE_LENGTH_LIMIT	64

void
sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue)
{
	GrayQueueSection *section;

	if (queue->alloc_prepare_func)
		queue->alloc_prepare_func (queue);

	if (queue->free_list) {
		/* Use the previously allocated queue sections if possible */
		section = queue->free_list;
		queue->free_list = section->next;
	} else {
		/* Allocate a new section */
		section = sgen_alloc_internal (INTERNAL_MEM_GRAY_QUEUE);
	}

	section->end = 0;

	/* Link it with the others */
	section->next = queue->first;
	queue->first = section;
}

void
sgen_gray_object_free_queue_section (GrayQueueSection *section)
{
	sgen_free_internal (section, INTERNAL_MEM_GRAY_QUEUE);
}

/*
 * The following two functions are called in the inner loops of the
 * collector, so they need to be as fast as possible.  We have macros
 * for them in sgen-gc.h.
 */

void
sgen_gray_object_enqueue (SgenGrayQueue *queue, char *obj)
{
	SGEN_ASSERT (9, obj, "enqueueing a null object");
	//sgen_check_objref (obj);
	if (G_UNLIKELY (!queue->first || queue->first->end == SGEN_GRAY_QUEUE_SECTION_SIZE))
		sgen_gray_object_alloc_queue_section (queue);
	SGEN_ASSERT (9, queue->first->end < SGEN_GRAY_QUEUE_SECTION_SIZE, "gray queue %p overflow, first %p, end %d", queue, queue->first, queue->first->end);
	queue->first->objects [queue->first->end++] = obj;

	SGEN_LOG_DO (9, ++queue->balance);
}

char*
sgen_gray_object_dequeue (SgenGrayQueue *queue)
{
	char *obj;

	if (sgen_gray_object_queue_is_empty (queue))
		return NULL;

	SGEN_ASSERT (9, queue->first->end, "gray queue %p underflow, first %p, end %d", queue, queue->first, queue->first->end);

	obj = queue->first->objects [--queue->first->end];

	if (G_UNLIKELY (queue->first->end == 0)) {
		GrayQueueSection *section = queue->first;
		queue->first = section->next;
		section->next = queue->free_list;
		queue->free_list = section;
	}

	SGEN_LOG_DO (9, --queue->balance);

	return obj;
}

GrayQueueSection*
sgen_gray_object_dequeue_section (SgenGrayQueue *queue)
{
	GrayQueueSection *section;

	if (!queue->first)
		return NULL;

	section = queue->first;
	queue->first = section->next;

	section->next = NULL;

	return section;
}

void
sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section)
{
	section->next = queue->first;
	queue->first = section;
}

void
sgen_gray_object_queue_init (SgenGrayQueue *queue)
{
	GrayQueueSection *section, *next;
	int i;

	g_assert (sgen_gray_object_queue_is_empty (queue));
	SGEN_ASSERT (9, queue->balance == 0, "unbalanced queue on init %d", queue->balance);

	/* Free the extra sections allocated during the last collection */
	i = 0;
	for (section = queue->free_list; section && i < GRAY_QUEUE_LENGTH_LIMIT - 1; section = section->next)
		i ++;
	if (!section)
		return;
	while (section->next) {
		next = section->next;
		section->next = next->next;
		sgen_gray_object_free_queue_section (next);
	}
}

void
sgen_gray_object_queue_init_with_alloc_prepare (SgenGrayQueue *queue, GrayQueueAllocPrepareFunc func, void *data)
{
	sgen_gray_object_queue_init (queue);
	queue->alloc_prepare_func = func;
	queue->alloc_prepare_data = data;
}

#endif
