/**
 * \file
 * Gray queue management.
 *
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGEN_GRAY_H__
#define __MONO_SGEN_GRAY_H__

#include "mono/sgen/sgen-protocol.h"

/*
 * This gray queue has to be as optimized as possible, because it is in the core of
 * the mark/copy phase of the garbage collector. The memory access has then to be as
 * cache friendly as possible. That's why we use a cursor based implementation.
 * 
 * This simply consist in maintaining a pointer to the current element in the
 * queue. In addition to using this cursor, we use a simple linked list of arrays,
 * called sections, so that we have the cache friendliness of arrays without having
 * the cost of memory reallocation of a dynaic array, not the cost of memory
 * indirection of a linked list.
 * 
 * This implementation also allows the dequeuing of a whole section at a time. This is
 * for example used in the parallel GC because it would be too costly to take one element 
 * at a time. This imply the main constraint that, because we don't carry the cursor
 * with the section, we still have to store the index of the last element. This is done 
 * through the 'size' field on the section, which default value is it's maximum value
 * SGEN_GRAY_QUEUE_SECTION_SIZE. This field is updated in multiple cases :
 *  - section allocation : default value
 *  - object push : default value if we fill the current queue first
 *  - section dequeue : position of the cursor in the dequeued section
 *  - section enqueue : position of the cursor in the previously first section in the queue
 * 
 * The previous implementation was an index based access where we would store the index
 * of the last element in the section. This was less efficient because we would have
 * to make 1 memory access for the index value, 1 for the base address of the objects
 * array and another 1 for the actual value in the array.
 */

/* SGEN_GRAY_QUEUE_HEADER_SIZE is number of machine words */
#ifdef SGEN_CHECK_GRAY_OBJECT_SECTIONS
#define SGEN_GRAY_QUEUE_HEADER_SIZE	5
#else
#define SGEN_GRAY_QUEUE_HEADER_SIZE	3
#endif

#define SGEN_GRAY_QUEUE_SECTION_SIZE	(512 - SGEN_GRAY_QUEUE_HEADER_SIZE)

#ifdef SGEN_CHECK_GRAY_OBJECT_SECTIONS
typedef enum {
	GRAY_QUEUE_SECTION_STATE_FLOATING,
	GRAY_QUEUE_SECTION_STATE_ENQUEUED,
 	GRAY_QUEUE_SECTION_STATE_FREE_LIST,
 	GRAY_QUEUE_SECTION_STATE_FREED
} GrayQueueSectionState;
#endif

typedef struct _GrayQueueEntry GrayQueueEntry;
struct _GrayQueueEntry {
	GCObject *obj;
	SgenDescriptor desc;
};

#define SGEN_GRAY_QUEUE_ENTRY(obj,desc)	{ (obj), (desc) }

#define GRAY_OBJECT_ENQUEUE_SERIAL(queue, obj, desc) (GRAY_OBJECT_ENQUEUE (queue, obj, desc, FALSE))
#define GRAY_OBJECT_ENQUEUE_PARALLEL(queue, obj, desc) (GRAY_OBJECT_ENQUEUE (queue, obj, desc, TRUE))
#define GRAY_OBJECT_DEQUEUE_SERIAL(queue, obj, desc) (GRAY_OBJECT_DEQUEUE (queue, obj, desc, FALSE))
#define GRAY_OBJECT_DEQUEUE_PARALLEL(queue, obj, desc) (GRAY_OBJECT_DEQUEUE (queue, obj, desc, TRUE))

/*
 * This is a stack now instead of a queue, so the most recently added items are removed
 * first, improving cache locality, and keeping the stack size manageable.
 */
typedef struct _GrayQueueSection GrayQueueSection;
struct _GrayQueueSection {
#ifdef SGEN_CHECK_GRAY_OBJECT_SECTIONS
	/*
	 * The dummy is here so that the state doesn't get overwritten
	 * by the internal allocator once the section is freed.
	 */
	int dummy;
	GrayQueueSectionState state;
#endif
	int size;
	GrayQueueSection *next, *prev;
	GrayQueueEntry entries [SGEN_GRAY_QUEUE_SECTION_SIZE];
};

typedef struct _SgenGrayQueue SgenGrayQueue;

typedef void (*GrayQueueAllocPrepareFunc) (SgenGrayQueue*);
typedef void (*GrayQueueEnqueueCheckFunc) (GCObject*);

struct _SgenGrayQueue {
	GrayQueueEntry *cursor;
	GrayQueueSection *first, *last;
	GrayQueueSection *free_list;
	mono_mutex_t steal_mutex;
	gint32 num_sections;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	GrayQueueEnqueueCheckFunc enqueue_check_func;
#endif
};

typedef struct _SgenSectionGrayQueue SgenSectionGrayQueue;

struct _SgenSectionGrayQueue {
	GrayQueueSection *first;
	gboolean locked;
	mono_mutex_t lock;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	GrayQueueEnqueueCheckFunc enqueue_check_func;
#endif
};

#define GRAY_LAST_CURSOR_POSITION(s) ((s)->entries + SGEN_GRAY_QUEUE_SECTION_SIZE - 1)
#define GRAY_FIRST_CURSOR_POSITION(s) ((s)->entries)

#ifdef HEAVY_STATISTICS
extern guint64 stat_gray_queue_section_alloc;
extern guint64 stat_gray_queue_section_free;
extern guint64 stat_gray_queue_enqueue_fast_path;
extern guint64 stat_gray_queue_dequeue_fast_path;
extern guint64 stat_gray_queue_enqueue_slow_path;
extern guint64 stat_gray_queue_dequeue_slow_path;
#endif

void sgen_init_gray_queues (void);

void sgen_gray_object_enqueue (SgenGrayQueue *queue, GCObject *obj, SgenDescriptor desc, gboolean is_parallel);
GrayQueueEntry sgen_gray_object_dequeue (SgenGrayQueue *queue, gboolean is_parallel);
GrayQueueSection* sgen_gray_object_dequeue_section (SgenGrayQueue *queue);
GrayQueueSection* sgen_gray_object_steal_section (SgenGrayQueue *queue);
void sgen_gray_object_spread (SgenGrayQueue *queue, int num_sections);
void sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section, gboolean is_parallel);
void sgen_gray_object_queue_trim_free_list (SgenGrayQueue *queue);
void sgen_gray_object_queue_init (SgenGrayQueue *queue, GrayQueueEnqueueCheckFunc enqueue_check_func, gboolean reuse_free_list);
void sgen_gray_object_queue_dispose (SgenGrayQueue *queue);
void sgen_gray_object_queue_deinit (SgenGrayQueue *queue);
void sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue, gboolean is_parallel);
void sgen_gray_object_free_queue_section (GrayQueueSection *section);

void sgen_section_gray_queue_init (SgenSectionGrayQueue *queue, gboolean locked,
		GrayQueueEnqueueCheckFunc enqueue_check_func);
gboolean sgen_section_gray_queue_is_empty (SgenSectionGrayQueue *queue);
GrayQueueSection* sgen_section_gray_queue_dequeue (SgenSectionGrayQueue *queue);
void sgen_section_gray_queue_enqueue (SgenSectionGrayQueue *queue, GrayQueueSection *section);

gboolean sgen_gray_object_fill_prefetch (SgenGrayQueue *queue);

static inline gboolean
sgen_gray_object_queue_is_empty (SgenGrayQueue *queue)
{
	return queue->first == NULL;
}

static inline MONO_ALWAYS_INLINE void
GRAY_OBJECT_ENQUEUE (SgenGrayQueue *queue, GCObject *obj, SgenDescriptor desc, gboolean is_parallel)
{
#if SGEN_MAX_DEBUG_LEVEL >= 9
	sgen_gray_object_enqueue (queue, obj, desc, is_parallel);
#else
	if (G_UNLIKELY (!queue->first || queue->cursor == GRAY_LAST_CURSOR_POSITION (queue->first))) {
		sgen_gray_object_enqueue (queue, obj, desc, is_parallel);
	} else {
		GrayQueueEntry entry = SGEN_GRAY_QUEUE_ENTRY (obj, desc);

		HEAVY_STAT (stat_gray_queue_enqueue_fast_path ++);

		*++queue->cursor = entry;
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
		sgen_binary_protocol_gray_enqueue (queue, queue->cursor, obj);
#endif
	}
#endif
}

static inline MONO_ALWAYS_INLINE void
GRAY_OBJECT_DEQUEUE (SgenGrayQueue *queue, GCObject** obj, SgenDescriptor *desc, gboolean is_parallel)
{
	GrayQueueEntry entry;
#if SGEN_MAX_DEBUG_LEVEL >= 9
	entry = sgen_gray_object_dequeue (queue, is_parallel);
	*obj = entry.obj;
	*desc = entry.desc;
#else
	if (!queue->first) {
		HEAVY_STAT (stat_gray_queue_dequeue_fast_path ++);

		*obj = NULL;
	} else if (G_UNLIKELY (queue->cursor == GRAY_FIRST_CURSOR_POSITION (queue->first))) {
		entry = sgen_gray_object_dequeue (queue, is_parallel);
		*obj = entry.obj;
		*desc = entry.desc;
	} else {
		HEAVY_STAT (stat_gray_queue_dequeue_fast_path ++);

		entry = *queue->cursor--;
		*obj = entry.obj;
		*desc = entry.desc;
#ifdef SGEN_HEAVY_BINARY_PROTOCOL
		sgen_binary_protocol_gray_dequeue (queue, queue->cursor + 1, *obj);
#endif
	}
#endif
}

#endif
