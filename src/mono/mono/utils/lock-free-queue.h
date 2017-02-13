/**
 * \file
 * Lock free queue.
 *
 * (C) Copyright 2011 Novell, Inc
 *
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */


#ifndef __MONO_LOCKFREEQUEUE_H__
#define __MONO_LOCKFREEQUEUE_H__

#include <glib.h>
#include <mono/utils/mono-publib.h>

//#define QUEUE_DEBUG	1

typedef struct _MonoLockFreeQueueNode MonoLockFreeQueueNode;

struct _MonoLockFreeQueueNode {
	MonoLockFreeQueueNode * volatile next;
#ifdef QUEUE_DEBUG
	gint32 in_queue;
#endif
};

typedef struct {
	MonoLockFreeQueueNode node;
	volatile gint32 in_use;
} MonoLockFreeQueueDummy;

#define MONO_LOCK_FREE_QUEUE_NUM_DUMMIES	2

typedef struct {
	MonoLockFreeQueueNode * volatile head;
	MonoLockFreeQueueNode * volatile tail;
	MonoLockFreeQueueDummy dummies [MONO_LOCK_FREE_QUEUE_NUM_DUMMIES];
	volatile gint32 has_dummy;
} MonoLockFreeQueue;

MONO_API void mono_lock_free_queue_init (MonoLockFreeQueue *q);

MONO_API void mono_lock_free_queue_node_init (MonoLockFreeQueueNode *node, gboolean poison);
MONO_API void mono_lock_free_queue_node_unpoison (MonoLockFreeQueueNode *node);

MONO_API void mono_lock_free_queue_enqueue (MonoLockFreeQueue *q, MonoLockFreeQueueNode *node);

MONO_API MonoLockFreeQueueNode* mono_lock_free_queue_dequeue (MonoLockFreeQueue *q);

#endif
