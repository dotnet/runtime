/*
 * lock-free-queue.h: Lock free queue.
 *
 * (C) Copyright 2011 Novell, Inc
 */

#ifndef __MONO_LOCKFREEQUEUE_H__
#define __MONO_LOCKFREEQUEUE_H__

#include <glib.h>

//#define QUEUE_DEBUG	1

typedef struct _MonoLockFreeQueueNode MonoLockFreeQueueNode;

struct _MonoLockFreeQueueNode {
	MonoLockFreeQueueNode *next;
#ifdef QUEUE_DEBUG
	gint32 in_queue;
#endif
};

typedef struct {
	MonoLockFreeQueueNode node;
	gint32 in_use;
} MonoLockFreeQueueDummy;

#define MONO_LOCK_FREE_QUEUE_NUM_DUMMIES	2

typedef struct {
	volatile MonoLockFreeQueueNode *head;
	volatile MonoLockFreeQueueNode *tail;
	MonoLockFreeQueueDummy dummies [MONO_LOCK_FREE_QUEUE_NUM_DUMMIES];
	gint32 has_dummy;
} MonoLockFreeQueue;

void mono_lock_free_queue_init (MonoLockFreeQueue *q) MONO_INTERNAL;

void mono_lock_free_queue_node_init (MonoLockFreeQueueNode *node, gboolean to_be_freed) MONO_INTERNAL;
void mono_lock_free_queue_node_free (MonoLockFreeQueueNode *node) MONO_INTERNAL;

void mono_lock_free_queue_enqueue (MonoLockFreeQueue *q, MonoLockFreeQueueNode *node) MONO_INTERNAL;

MonoLockFreeQueueNode* mono_lock_free_queue_dequeue (MonoLockFreeQueue *q) MONO_INTERNAL;

#endif
