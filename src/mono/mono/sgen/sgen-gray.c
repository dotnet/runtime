/**
 * \file
 * Gray queue management.
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
		if (mono_atomic_cas_i32 ((volatile int*)&(s)->state, (n), __old) != __old) \
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
sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue, gboolean is_parallel)
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

	/* Section is empty */
	section->size = 0;

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	/* Link it with the others */
	section->next = queue->first;
	section->prev = NULL;
	if (queue->first)
		queue->first->prev = section;
	else
		queue->last = section;
	queue->first = section;
	queue->cursor = section->entries - 1;

	if (is_parallel) {
		mono_memory_write_barrier ();
		/*
		 * FIXME
		 * we could probably optimize the code to only rely on the write barrier
		 * for synchronization with the stealer thread. Additionally we could also
		 * do a write barrier once every other gray queue change, and request
		 * to have a minimum of sections before stealing, to keep consistency.
		 */
		mono_atomic_inc_i32 (&queue->num_sections);
	} else {
		queue->num_sections++;
	}
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
sgen_gray_object_enqueue (SgenGrayQueue *queue, GCObject *obj, SgenDescriptor desc, gboolean is_parallel)
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
			/*
			 * We don't actively update the section size with each push/pop. For the first
			 * section we determine the size from the cursor position. For the reset of the
			 * sections we need to have the size set.
			 */
			queue->first->size = SGEN_GRAY_QUEUE_SECTION_SIZE;
		}

		sgen_gray_object_alloc_queue_section (queue, is_parallel);
	}
	STATE_ASSERT (queue->first, GRAY_QUEUE_SECTION_STATE_ENQUEUED);
	SGEN_ASSERT (9, queue->cursor <= GRAY_LAST_CURSOR_POSITION (queue->first), "gray queue %p overflow, first %p, cursor %p", queue, queue->first, queue->cursor);
	*++queue->cursor = entry;

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
	sgen_binary_protocol_gray_enqueue (queue, queue->cursor, obj);
#endif
}

/*
 * We attempt to spread the objects in the gray queue across a number
 * of sections. If the queue has more sections, then it's already spread,
 * if it doesn't have enough sections, then we allocate as many as we
 * can.
 */
void
sgen_gray_object_spread (SgenGrayQueue *queue, int num_sections)
{
	GrayQueueSection *section_start, *section_end;
	int total_entries = 0, num_entries_per_section;
	int num_sections_final;

	if (queue->num_sections >= num_sections)
		return;

	if (!queue->first)
		return;

	/* Compute number of elements in the gray queue */
	queue->first->size = queue->cursor - queue->first->entries + 1;
	total_entries = queue->first->size;
	for (section_start = queue->first->next; section_start != NULL; section_start = section_start->next) {
		SGEN_ASSERT (0, section_start->size == SGEN_GRAY_QUEUE_SECTION_SIZE, "We expect all section aside from the first one to be full");
		total_entries += section_start->size;
	}

	/* Compute how many sections we should have and elements per section */
	num_sections_final = (total_entries > num_sections) ? num_sections : total_entries;
	num_entries_per_section = total_entries / num_sections_final;

	/* Allocate all needed sections */
	while (queue->num_sections < num_sections_final)
		sgen_gray_object_alloc_queue_section (queue, TRUE);

	/* Spread out the elements in the sections. By design, sections at the end are fuller. */
	section_start = queue->first;
	section_end = queue->last;
	while (section_start != section_end) {
		/* We move entries from end to start, until they meet */
		while (section_start->size < num_entries_per_section) {
			GrayQueueEntry entry;
			if (section_end->size <= num_entries_per_section) {
				section_end = section_end->prev;
				if (section_end == section_start)
					break;
			}
			if (section_end->size <= num_entries_per_section)
				break;

			section_end->size--;
			entry = section_end->entries [section_end->size];
			section_start->entries [section_start->size] = entry;
			section_start->size++;
		}
		section_start = section_start->next;
	}

	queue->cursor = queue->first->entries + queue->first->size - 1;
	queue->num_sections = num_sections_final;
}

GrayQueueEntry
sgen_gray_object_dequeue (SgenGrayQueue *queue, gboolean is_parallel)
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
	sgen_binary_protocol_gray_dequeue (queue, queue->cursor + 1, entry.obj);
#endif

	if (G_UNLIKELY (queue->cursor < GRAY_FIRST_CURSOR_POSITION (queue->first))) {
		GrayQueueSection *section;
		gint32 old_num_sections = 0;

		if (is_parallel)
			old_num_sections = mono_atomic_dec_i32 (&queue->num_sections);
		else
			queue->num_sections--;

		if (is_parallel && old_num_sections <= 0) {
			mono_os_mutex_lock (&queue->steal_mutex);
		}

		section = queue->first;
		queue->first = section->next;
		if (queue->first) {
			queue->first->prev = NULL;
		} else {
			queue->last = NULL;
			SGEN_ASSERT (0, !old_num_sections, "Why do we have an inconsistent number of sections ?");
		}
		section->next = queue->free_list;

		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FREE_LIST);

		queue->free_list = section;
		queue->cursor = queue->first ? queue->first->entries + queue->first->size - 1 : NULL;

		if (is_parallel && old_num_sections <= 0) {
			mono_os_mutex_unlock (&queue->steal_mutex);
		}
	}

	return entry;
}

GrayQueueSection*
sgen_gray_object_dequeue_section (SgenGrayQueue *queue)
{
	GrayQueueSection *section;

	if (!queue->first)
		return NULL;

	/* We never steal from this queue */
	queue->num_sections--;

	section = queue->first;
	queue->first = section->next;
	if (queue->first)
		queue->first->prev = NULL;
	else
		queue->last = NULL;

	section->next = NULL;
	section->size = queue->cursor - section->entries + 1;

	queue->cursor = queue->first ? queue->first->entries + queue->first->size - 1 : NULL;

	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FLOATING);

	return section;
}

GrayQueueSection*
sgen_gray_object_steal_section (SgenGrayQueue *queue)
{
	gint32 sections_remaining;
	GrayQueueSection *section = NULL;

	/*
	 * With each push/pop into the queue we increment the number of sections.
	 * There is only one thread accessing the top (the owner) and potentially
	 * multiple workers trying to steal sections from the bottom, so we need
	 * to lock. A num sections decrement from the owner means that the first
	 * section is reserved, while a decrement by the stealer means that the
	 * last section is reserved. If after we decrement the num sections, we
	 * have at least one more section present, it means we can't race with
	 * the other thread. If this is not the case the steal end abandons the
	 * pop, setting back the num_sections, while the owner end will take a
	 * lock to make sure we are not racing with the stealer (since the stealer
	 * might have popped an entry and be in the process of updating the entry
	 * that the owner is trying to pop.
	 */

	if (queue->num_sections <= 1)
		return NULL;

	/* Give up if there is contention on the last section */
	if (mono_os_mutex_trylock (&queue->steal_mutex) != 0)
		return NULL;

	sections_remaining = mono_atomic_dec_i32 (&queue->num_sections);
	if (sections_remaining <= 0) {
		/* The section that we tried to steal might be the head of the queue. */
		mono_atomic_inc_i32 (&queue->num_sections);
	} else {
		/* We have reserved for us the tail section of the queue */
		section = queue->last;
		SGEN_ASSERT (0, section, "Why we don't have any sections to steal?");
		SGEN_ASSERT (0, !section->next, "Why aren't we stealing the tail?");
		queue->last = section->prev;
		section->prev = NULL;
		SGEN_ASSERT (0, queue->last, "Why are we stealing the last section?");
		queue->last->next = NULL;

		STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_ENQUEUED, GRAY_QUEUE_SECTION_STATE_FLOATING);
	}

	mono_os_mutex_unlock (&queue->steal_mutex);
	return section;
}

void
sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section, gboolean is_parallel)
{
	STATE_TRANSITION (section, GRAY_QUEUE_SECTION_STATE_FLOATING, GRAY_QUEUE_SECTION_STATE_ENQUEUED);

	if (queue->first)
		queue->first->size = queue->cursor - queue->first->entries + 1;

	section->next = queue->first;
	section->prev = NULL;
	if (queue->first)
		queue->first->prev = section;
	else
		queue->last = section;
	queue->first = section;
	queue->cursor = queue->first->entries + queue->first->size - 1;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	if (queue->enqueue_check_func) {
		int i;
		for (i = 0; i < section->size; ++i)
			queue->enqueue_check_func (section->entries [i].obj);
	}
#endif
	if (is_parallel) {
		mono_memory_write_barrier ();
		mono_atomic_inc_i32 (&queue->num_sections);
	} else {
		queue->num_sections++;
	}
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

	mono_os_mutex_init (&queue->steal_mutex);

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

	mono_os_mutex_destroy (&queue->steal_mutex);

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
