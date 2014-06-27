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
#include "sgen-protocol.h"

#define GRAY_QUEUE_LENGTH_LIMIT	64

#ifdef SGEN_CHECK_GRAY_OBJECT_SECTIONS
#define STATE_TRANSITION(s,o,n)	do {					\
		int __old = (o);					\
		if (InterlockedCompareExchange ((volatile int*)&(s)->state, (n), __old) != __old) \
			g_assert_not_reached ();			\
	} while (0)
#define STATE_SET(s,v)		(s)->state = (v)
#define STATE_ASSERT(s,v)	g_assert ((s)->state == (v))
#else
#define STATE_TRANSITION(s,o,n)
#define STATE_SET(s,v)
#define STATE_ASSERT(s,v)
#endif

void
sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue)
{
	GrayQueueSection *section;

	HEAVY_STAT (gc_stats.gray_queue_section_alloc ++);

	if (queue->alloc_prepare_func)
		queue->alloc_prepare_func (queue);

	if (queue->free_list) {
		/* Use the previously allocated queue sections if possible */
		section = queue->free_list;
		queue->free_list = section->next;
		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FREE_LIST, GRAY_QUEUE_SECTION_STATE_FLOATING);
	} else {
		/* Allocate a new section */
		section = sgen_alloc_internal (INTERNAL_MEM_GRAY_QUEUE);
		STATE_SET (section, GRAY_QUEUE_SECTION_STATE_FLOATING);
	}

	section->size = SGEN_GRAY_QUEUE_SECTION_SIZE;

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	/* Link it with the others */
	section->next = queue->first;
	queue->first = section;
	queue->cursor = (char**)section->objects - 1;
}

void
sgen_gray_object_free_queue_section (GrayQueueSection *section)
{
	HEAVY_STAT (gc_stats.gray_queue_section_free ++);

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_FREED);
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
	HEAVY_STAT (gc_stats.gray_queue_enqueue_slow_path ++);

	SGEN_ASSERT (9, obj, "enqueueing a null object");
	//sgen_check_objref (obj);

#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	if (queue->enqueue_check_func)
		queue->enqueue_check_func (obj);
#endif

	if (G_UNLIKELY (!queue->first || queue->cursor == GRAY_LAST_CURSOR_POSITION (queue->first))) {
		if (queue->first) {
			/* Set the current section size back to default, might have been changed by sgen_gray_object_dequeue_section */
			queue->first->size = SGEN_GRAY_QUEUE_SECTION_SIZE;
		}

		sgen_gray_object_alloc_queue_section (queue);
	}
	STATE_ASSERT (queue->first, GRAY_QUEUE_SECTION_STATE_ENQUEUED);
	SGEN_ASSERT (9, queue->cursor <= GRAY_LAST_CURSOR_POSITION (queue->first), "gray queue %p overflow, first %p, cursor %p", queue, queue->first, queue->cursor);
	*++queue->cursor = obj;

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	binary_protocol_gray_enqueue (queue, queue->cursor, obj);
#endif
}

char*
sgen_gray_object_dequeue (SgenGrayQueue *queue)
{
	char *obj;

	HEAVY_STAT (gc_stats.gray_queue_dequeue_slow_path ++);

	if (sgen_gray_object_queue_is_empty (queue))
		return NULL;

	STATE_ASSERT (queue->first, GRAY_QUEUE_SECTION_STATE_ENQUEUED);
	SGEN_ASSERT (9, queue->cursor >= (char**)queue->first->objects, "gray queue %p underflow, first %p, cursor %d", queue, queue->first, queue->cursor);

	obj = *queue->cursor--;

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	binary_protocol_gray_dequeue (queue, queue->cursor + 1, obj);
#endif

	if (G_UNLIKELY (queue->cursor == (char**)queue->first->objects - 1)) {
		GrayQueueSection *section = queue->first;
		queue->first = section->next;
		section->next = queue->free_list;

		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FREE_LIST);

		queue->free_list = section;
		queue->cursor = queue->first ? (char**)queue->first->objects + queue->first->size - 1 : NULL;
	}

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
	section->size = queue->cursor - (char**)section->objects + 1;

	queue->cursor = queue->first ? (char**)queue->first->objects + queue->first->size - 1 : NULL;

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FLOATING);

	return section;
}

void
sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section)
{
	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	if (queue->first)
		queue->first->size = queue->cursor - (char**)queue->first->objects + 1;

	section->next = queue->first;
	queue->first = section;
	queue->cursor = (char**)queue->first->objects + queue->first->size - 1;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	if (queue->enqueue_check_func) {
		int i;
		for (i = 0; i < section->size; ++i)
			queue->enqueue_check_func (section->objects [i]);
	}
#endif
}

void
sgen_gray_object_queue_init (SgenGrayQueue *queue, GrayQueueEnqueueCheckFunc enqueue_check_func)
{
	GrayQueueSection *section, *next;
	int i;

	g_assert (sgen_gray_object_queue_is_empty (queue));

	queue->alloc_prepare_func = NULL;
	queue->alloc_prepare_data = NULL;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	queue->enqueue_check_func = enqueue_check_func;
#endif

	/* Free the extra sections allocated during the last collection */
	i = 0;
	for (section = queue->free_list; section && i < GRAY_QUEUE_LENGTH_LIMIT - 1; section = section->next) {
		STATE_ASSERT (section, GRAY_QUEUE_SECTION_STATE_FREE_LIST);
		i ++;
	}
	if (!section)
		return;
	while (section->next) {
		next = section->next;
		section->next = next->next;
		STATE_TRANSITION (next, GRAY_QUEUE_SECTION_STATE_FREE_LIST, GRAY_QUEUE_SECTION_STATE_FLOATING);
		sgen_gray_object_free_queue_section (next);
	}
}

static void
invalid_prepare_func (SgenGrayQueue *queue)
{
	g_assert_not_reached ();
}

void
sgen_gray_object_queue_init_invalid (SgenGrayQueue *queue)
{
	sgen_gray_object_queue_init (queue, FALSE);
	queue->alloc_prepare_func = invalid_prepare_func;
	queue->alloc_prepare_data = NULL;
}

void
sgen_gray_object_queue_init_with_alloc_prepare (SgenGrayQueue *queue, GrayQueueEnqueueCheckFunc enqueue_check_func,
		GrayQueueAllocPrepareFunc alloc_prepare_func, void *data)
{
	sgen_gray_object_queue_init (queue, enqueue_check_func);
	queue->alloc_prepare_func = alloc_prepare_func;
	queue->alloc_prepare_data = data;
}

void
sgen_gray_object_queue_deinit (SgenGrayQueue *queue)
{
	g_assert (!queue->first);
	while (queue->free_list) {
		GrayQueueSection *next = queue->free_list->next;
		STATE_TRANSITION (queue->free_list, GRAY_QUEUE_SECTION_STATE_FREE_LIST, GRAY_QUEUE_SECTION_STATE_FLOATING);
		sgen_gray_object_free_queue_section (queue->free_list);
		queue->free_list = next;
	}
}

void
sgen_gray_object_queue_disable_alloc_prepare (SgenGrayQueue *queue)
{
	queue->alloc_prepare_func = NULL;
	queue->alloc_prepare_data = NULL;
}

static void
lock_section_queue (SgenSectionGrayQueue *queue)
{
	if (!queue->locked)
		return;

	mono_mutex_lock (&queue->lock);
}

static void
unlock_section_queue (SgenSectionGrayQueue *queue)
{
	if (!queue->locked)
		return;

	mono_mutex_unlock (&queue->lock);
}

void
sgen_section_gray_queue_init (SgenSectionGrayQueue *queue, gboolean locked, GrayQueueEnqueueCheckFunc enqueue_check_func)
{
	g_assert (sgen_section_gray_queue_is_empty (queue));

	queue->locked = locked;
	if (locked) {
		mono_mutex_init_recursive (&queue->lock);
	}

#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	queue->enqueue_check_func = enqueue_check_func;
#endif
}

gboolean
sgen_section_gray_queue_is_empty (SgenSectionGrayQueue *queue)
{
	return !queue->first;
}

GrayQueueSection*
sgen_section_gray_queue_dequeue (SgenSectionGrayQueue *queue)
{
	GrayQueueSection *section;

	lock_section_queue (queue);

	if (queue->first) {
		section = queue->first;
		queue->first = section->next;

		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FLOATING);

		section->next = NULL;
	} else {
		section = NULL;
	}

	unlock_section_queue (queue);

	return section;
}

void
sgen_section_gray_queue_enqueue (SgenSectionGrayQueue *queue, GrayQueueSection *section)
{
	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	lock_section_queue (queue);

	section->next = queue->first;
	queue->first = section;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	if (queue->enqueue_check_func) {
		int i;
		for (i = 0; i < section->size; ++i)
			queue->enqueue_check_func (section->objects [i]);
	}
#endif

	unlock_section_queue (queue);
}

#endif
