/*
 * sgen-gray.h: Gray queue management.
 *
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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
#ifndef __MONO_SGEN_GRAY_H__
#define __MONO_SGEN_GRAY_H__

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

#define SGEN_GRAY_QUEUE_SECTION_SIZE	(128 - 3)

#ifdef SGEN_CHECK_GRAY_OBJECT_SECTIONS
typedef enum {
	GRAY_QUEUE_SECTION_STATE_FLOATING,
	GRAY_QUEUE_SECTION_STATE_ENQUEUED,
 	GRAY_QUEUE_SECTION_STATE_FREE_LIST,
 	GRAY_QUEUE_SECTION_STATE_FREED
} GrayQueueSectionState;
#endif

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
	GrayQueueSection *next;
	char *objects [SGEN_GRAY_QUEUE_SECTION_SIZE];
};

typedef struct _SgenGrayQueue SgenGrayQueue;

typedef void (*GrayQueueAllocPrepareFunc) (SgenGrayQueue*);
typedef void (*GrayQueueEnqueueCheckFunc) (char*);

struct _SgenGrayQueue {
	char **cursor;
	GrayQueueSection *first;
	GrayQueueSection *free_list;
	GrayQueueAllocPrepareFunc alloc_prepare_func;
#ifdef SGEN_CHECK_GRAY_OBJECT_ENQUEUE
	GrayQueueEnqueueCheckFunc enqueue_check_func;
#endif
	void *alloc_prepare_data;
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

#define GRAY_LAST_CURSOR_POSITION(s) ((char**)(s)->objects + SGEN_GRAY_QUEUE_SECTION_SIZE - 1)
#define GRAY_FIRST_CURSOR_POSITION(s) ((char**)(s)->objects)

void sgen_gray_object_enqueue (SgenGrayQueue *queue, char *obj) MONO_INTERNAL;
char* sgen_gray_object_dequeue (SgenGrayQueue *queue) MONO_INTERNAL;
GrayQueueSection* sgen_gray_object_dequeue_section (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_enqueue_section (SgenGrayQueue *queue, GrayQueueSection *section) MONO_INTERNAL;
void sgen_gray_object_queue_init (SgenGrayQueue *queue, GrayQueueEnqueueCheckFunc enqueue_check_func) MONO_INTERNAL;
void sgen_gray_object_queue_init_invalid (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_queue_init_with_alloc_prepare (SgenGrayQueue *queue, GrayQueueEnqueueCheckFunc enqueue_check_func,
		GrayQueueAllocPrepareFunc func, void *data) MONO_INTERNAL;
void sgen_gray_object_queue_deinit (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_queue_disable_alloc_prepare (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_alloc_queue_section (SgenGrayQueue *queue) MONO_INTERNAL;
void sgen_gray_object_free_queue_section (GrayQueueSection *section) MONO_INTERNAL;

void sgen_section_gray_queue_init (SgenSectionGrayQueue *queue, gboolean locked,
		GrayQueueEnqueueCheckFunc enqueue_check_func) MONO_INTERNAL;
gboolean sgen_section_gray_queue_is_empty (SgenSectionGrayQueue *queue) MONO_INTERNAL;
GrayQueueSection* sgen_section_gray_queue_dequeue (SgenSectionGrayQueue *queue) MONO_INTERNAL;
void sgen_section_gray_queue_enqueue (SgenSectionGrayQueue *queue, GrayQueueSection *section) MONO_INTERNAL;

static inline gboolean
sgen_gray_object_queue_is_empty (SgenGrayQueue *queue)
{
	return queue->first == NULL;
}

#endif
