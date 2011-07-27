/*
 * lock-free-array-queue.h: A lock-free somewhat-queue that doesn't
 * require hazard pointers.
 *
 * (C) Copyright 2011 Xamarin Inc.
 */
#ifndef __MONO_LOCK_FREE_ARRAY_QUEUE_H__
#define __MONO_LOCK_FREE_ARRAY_QUEUE_H__

#include <glib.h>
#include <mono/utils/mono-compiler.h>

typedef struct _MonoLockFreeArrayQueueChunk MonoLockFreeArrayQueueChunk;

typedef struct {
	size_t entry_size;
	gint32 num_used_entries;
	MonoLockFreeArrayQueueChunk *chunk_list;
} MonoLockFreeArrayQueue;

#define MONO_LOCK_FREE_ARRAY_QUEUE_INIT(entry_size)	{ (entry_size), 0, NULL }

void mono_lock_free_array_queue_push (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr) MONO_INTERNAL;
gboolean mono_lock_free_array_queue_pop (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr) MONO_INTERNAL;

void mono_lock_free_array_queue_cleanup (MonoLockFreeArrayQueue *q) MONO_INTERNAL;

#endif
