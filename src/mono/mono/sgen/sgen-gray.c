/*
 * sgen-gray.c: Gray queue management.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-protocol.h"

#ifdef HEAVY_STATISTICS
guint64 stat_gray_queue_section_alloc;
guint64 stat_gray_queue_section_free;
guint64 stat_gray_queue_enqueue_fast_path;
guint64 stat_gray_queue_dequeue_fast_path;
guint64 stat_gray_queue_enqueue_slow_path;
guint64 stat_gray_queue_dequeue_slow_path;
#endif

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

/*
 * Whenever we dispose a gray queue, we save its free list.  Then, in the next collection,
 * we reuse that free list for the new gray queue.
 */
static GrayQueueSection *last_gray_queue_free_list;

void
sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue)
{
	GrayQueueSection *section;

	if (queue->free_list) {
		/* Use the previously allocated queue sections if possible */
		section = queue->free_list;
		queue->free_list = section->next;
		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FREE_LIST, GRAY_QUEUE_SECTION_STATE_FLOATING);
	} else {
		HEAVY_STAT (stat_gray_queue_section_alloc ++);

		/* Allocate a new section */
		section = (GrayQueueSection *)sgen_alloc_internal (INTERNAL_MEM_GRAY_QUEUE);
		STATE_SET (section, GRAY_QUEUE_SECTION_STATE_FLOATING);
	}

	section->size = SGEN_GRAY_QUEUE_SECTION_SIZE;

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	/* Link it with the others */
	section->next = queue->first;
	queue->first = section;
	queue->cursor = section->entries - 1;
}

void
sgen_gray_object_free_queue_section (GrayQueueSection *section)
{
	HEAVY_STAT (stat_gray_queue_section_free ++);

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_FREED);
	sgen_free_internal (section, INTERNAL_MEM_GRAY_QUEUE);
}

/*
 * The following two functions are called in the inner loops of the
 * collector, so they need to be as fast as possible.  We have macros
 * for them in sgen-gc.h.
 */

void
sgen_gray_object_enqueue (SgenGrayQueue *queue, GCObject *obj, SgenDescriptor desc)
{
	GrayQueueEntry entry = SGEN_GRAY_QUEUE_ENTRY (obj, desc);

	HEAVY_STAT (stat_gray_queue_enqueue_slow_path ++);

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
	*++queue->cursor = entry;

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	binary_protocol_gray_enqueue (queue, queue->cursor, obj);
#endif
}

GrayQueueEntry
sgen_gray_object_dequeue (SgenGrayQueue *queue)
{
	GrayQueueEntry entry;

	HEAVY_STAT (stat_gray_queue_dequeue_slow_path ++);

	if (sgen_gray_object_queue_is_empty (queue)) {
		entry.obj = NULL;
		return entry;
	}

	STATE_ASSERT (queue->first, GRAY_QUEUE_SECTION_STATE_ENQUEUED);
	SGEN_ASSERT (9, queue->cursor >= GRAY_FIRST_CURSOR_POSITION (queue->first), "gray queue %p underflow", queue);

	entry = *queue->cursor--;

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	binary_protocol_gray_dequeue (queue, queue->cursor + 1, entry.obj);
#endif

	if (G_UNLIKELY (queue->cursor < GRAY_FIRST_CURSOR_POSITION (queue->first))) {
		GrayQueueSection *section = queue->first;
		queue->first = section->next;
		section->next = queue->free_list;

		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FREE_LIST);

		queue->free_list = section;
		queue->cursor = queue->first ? queue->first->entries + queue->first->size - 1 : NULL;
	}

	return entry;
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
	section->size = queue->cursor - section->entries + 1;

	queue->cursor = queue->first ? queue->first->entries + queue->first->size - 1 : NULL;

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FLOATING);

	return section;
}

void
sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section)
{
	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	if (queue->first)
		queue->first->size = queue->cursor - queue->first->entries + 1;

	section->next = queue->first;
	queue->first = section;
	queue->cursor = queue->first->entries + queue->first->size - 1;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	if (queue->enqueue_check_func) {
		int i;
		for (i = 0; i < section->size; ++i)
			queue->enqueue_check_func (section->entries [i].obj);
	}
#endif
}

void
sgen_gray_object_queue_trim_free_list (SgenGrayQueue *queue)
{
	GrayQueueSection *section, *next;
	int i = 0;
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

void
sgen_gray_object_queue_init (SgenGrayQueue *queue, GrayQueueEnqueueCheckFunc enqueue_check_func, gboolean reuse_free_list)
{
	memset (queue, 0, sizeof (SgenGrayQueue));

#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	queue->enqueue_check_func = enqueue_check_func;
#endif

	if (reuse_free_list) {
		queue->free_list = last_gray_queue_free_list;
		last_gray_queue_free_list = NULL;
	}
}

void
sgen_gray_object_queue_dispose (SgenGrayQueue *queue)
{
	SGEN_ASSERT (0, sgen_gray_object_queue_is_empty (queue), "Why are we disposing a gray queue that's not empty?");

	/* Free the extra sections allocated during the last collection */
	sgen_gray_object_queue_trim_free_list (queue);

	SGEN_ASSERT (0, !last_gray_queue_free_list, "Are we disposing two gray queues after another?");
	last_gray_queue_free_list = queue->free_list;

	/* just to make sure */
	memset (queue, 0, sizeof (SgenGrayQueue));
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

static void
lock_section_queue (SgenSectionGrayQueue *queue)
{
	if (!queue->locked)
		return;

	mono_os_mutex_lock (&queue->lock);
}

static void
unlock_section_queue (SgenSectionGrayQueue *queue)
{
	if (!queue->locked)
		return;

	mono_os_mutex_unlock (&queue->lock);
}

void
sgen_section_gray_queue_init (SgenSectionGrayQueue *queue, gboolean locked, GrayQueueEnqueueCheckFunc enqueue_check_func)
{
	g_assert (sgen_section_gray_queue_is_empty (queue));

	queue->locked = locked;
	if (locked) {
		mono_os_mutex_init_recursive (&queue->lock);
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
			queue->enqueue_check_func (section->entries [i].obj);
	}
#endif

	unlock_section_queue (queue);
}

void
sgen_init_gray_queues (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("Gray Queue alloc section", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_gray_queue_section_alloc);
	mono_counters_register ("Gray Queue free section", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_gray_queue_section_free);
	mono_counters_register ("Gray Queue enqueue fast path", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_gray_queue_enqueue_fast_path);
	mono_counters_register ("Gray Queue dequeue fast path", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_gray_queue_dequeue_fast_path);
	mono_counters_register ("Gray Queue enqueue slow path", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_gray_queue_enqueue_slow_path);
	mono_counters_register ("Gray Queue dequeue slow path", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_gray_queue_dequeue_slow_path);
#endif
}
#endif
