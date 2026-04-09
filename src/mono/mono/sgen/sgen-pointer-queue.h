/**
 * \file
 * A pointer queue that can be sorted.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SGEN_POINTER_QUEUE_H__
#define __MONO_SGEN_POINTER_QUEUE_H__

#include <glib.h>

typedef struct {
	void **data;
	size_t size;
	size_t next_slot;
	int mem_type;
} SgenPointerQueue;

#define SGEN_POINTER_QUEUE_INIT(mem_type)	{ NULL, 0, 0, (mem_type) }

void sgen_pointer_queue_add (SgenPointerQueue *queue, void *ptr);
void sgen_pointer_queue_clear (SgenPointerQueue *queue);
void sgen_pointer_queue_remove_nulls (SgenPointerQueue *queue);
void sgen_pointer_queue_sort_uniq (SgenPointerQueue *queue);
size_t sgen_pointer_queue_search (SgenPointerQueue *queue, void *addr);
size_t sgen_pointer_queue_find (SgenPointerQueue *queue, void *ptr);
void sgen_pointer_queue_init (SgenPointerQueue *queue, int mem_type);
void* sgen_pointer_queue_pop (SgenPointerQueue *queue);
gboolean sgen_pointer_queue_is_empty (SgenPointerQueue *queue);
void sgen_pointer_queue_free (SgenPointerQueue *queue);
gboolean sgen_pointer_queue_will_grow (SgenPointerQueue *queue);

#endif
