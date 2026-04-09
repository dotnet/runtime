/**
 * \file
 * A lock-free somewhat-queue that doesn't
 * require hazard pointers.
 *
 * (C) Copyright 2011 Xamarin Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_LOCK_FREE_ARRAY_QUEUE_H__
#define __MONO_LOCK_FREE_ARRAY_QUEUE_H__

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-mmap.h>

typedef struct _MonoLockFreeArrayChunk MonoLockFreeArrayChunk;

typedef struct {
	size_t entry_size;
	MonoLockFreeArrayChunk *chunk_list;
	MonoMemAccountType account_type;
} MonoLockFreeArray;

typedef struct {
	MonoLockFreeArray array;
	gint32 num_used_entries;
} MonoLockFreeArrayQueue;

#define MONO_LOCK_FREE_ARRAY_INIT(entry_size, account_type)		{ (entry_size), NULL, (account_type) }
#define MONO_LOCK_FREE_ARRAY_QUEUE_INIT(entry_size, account_type)	{ MONO_LOCK_FREE_ARRAY_INIT ((entry_size) + sizeof (gpointer), (account_type)), 0 }

gpointer mono_lock_free_array_nth (MonoLockFreeArray *arr, int index);

typedef gpointer (*MonoLockFreeArrayIterateFunc) (int index, gpointer entry_ptr, gpointer user_data);
gpointer mono_lock_free_array_iterate (MonoLockFreeArray *arr, MonoLockFreeArrayIterateFunc func, gpointer user_data);

void mono_lock_free_array_cleanup (MonoLockFreeArray *arr);

void mono_lock_free_array_queue_push (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr);
gboolean mono_lock_free_array_queue_pop (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr);

void mono_lock_free_array_queue_cleanup (MonoLockFreeArrayQueue *q);

#endif
