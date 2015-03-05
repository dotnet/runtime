/*
 * sgen-pointer-queue.h: A pointer queue that can be sorted.
 *
 * Copyright (C) 2014 Xamarin Inc
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

#ifndef __MONO_SGEN_POINTER_QUEUE_H__
#define __MONO_SGEN_POINTER_QUEUE_H__

#include <glib.h>

typedef struct {
	void **data;
	size_t size;
	size_t next_slot;
	int mem_type;
} SgenPointerQueue;

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

#endif
