/**
 * \file
 * Lock free queue.
 *
 * (C) Copyright 2011 Novell, Inc
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

/*
 * This is an implementation of a lock-free queue, as described in
 *
 * Simple, Fast, and Practical Non-Blocking and Blocking
 *   Concurrent Queue Algorithms
 * Maged M. Michael, Michael L. Scott
 * 1995
 *
 * A few slight modifications have been made:
 *
 * We use hazard pointers to rule out the ABA problem, instead of the
 * counter as in the paper.
 *
 * Memory management of the queue entries is done by the caller, not
 * by the queue implementation.  This implies that the dequeue
 * function must return the queue entry, not just the data.
 *
 * Therefore, the dummy entry must never be returned.  We do this by
 * re-enqueuing a new dummy entry after we dequeue one and then
 * retrying the dequeue.  We need more than one dummy because they
 * must be hazardly freed.
 */

#include <stdlib.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <mono/utils/mono-membar.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/atomic.h>

#include <mono/utils/lock-free-queue.h>

#define INVALID_NEXT	((MonoLockFreeQueueNode*)-1)
#define END_MARKER	((MonoLockFreeQueueNode*)-2)
#define FREE_NEXT	((MonoLockFreeQueueNode*)-3)

/*
 * Initialize a lock-free queue in-place at @q.
 */
void
mono_lock_free_queue_init (MonoLockFreeQueue *q)
{
	int i;
	for (i = 0; i < MONO_LOCK_FREE_QUEUE_NUM_DUMMIES; ++i) {
		q->dummies [i].node.next = (i == 0) ? END_MARKER : FREE_NEXT;
		q->dummies [i].in_use = i == 0 ? 1 : 0;
#ifdef QUEUE_DEBUG
		q->dummies [i].node.in_queue = i == 0 ? TRUE : FALSE;
#endif
	}

	q->head = q->tail = &q->dummies [0].node;
	q->has_dummy = 1;
}

/*
 * Initialize @node's state. If @poison is TRUE, @node may not be enqueued to a
 * queue - @mono_lock_free_queue_node_unpoison must be called first; otherwise,
 * the node can be enqueued right away.
 *
 * The poisoning feature is mainly intended for ensuring correctness in complex
 * lock-free code that uses the queue. For example, in some code that reuses
 * nodes, nodes can be poisoned when they're dequeued, and then unpoisoned and
 * enqueued in their hazard free callback.
 */
void
mono_lock_free_queue_node_init (MonoLockFreeQueueNode *node, gboolean poison)
{
	node->next = poison ? INVALID_NEXT : FREE_NEXT;
#ifdef QUEUE_DEBUG
	node->in_queue = FALSE;
#endif
}

/*
 * Unpoisons @node so that it may be enqueued.
 */
void
mono_lock_free_queue_node_unpoison (MonoLockFreeQueueNode *node)
{
	g_assert (node->next == INVALID_NEXT);
#ifdef QUEUE_DEBUG
	g_assert (!node->in_queue);
#endif
	node->next = FREE_NEXT;
}

/*
 * Enqueue @node to @q. @node must have been initialized by a prior call to
 * @mono_lock_free_queue_node_init, and must not be in a poisoned state.
 */
void
mono_lock_free_queue_enqueue (MonoLockFreeQueue *q, MonoLockFreeQueueNode *node)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoLockFreeQueueNode *tail;

#ifdef QUEUE_DEBUG
	g_assert (!node->in_queue);
	node->in_queue = TRUE;
	mono_memory_write_barrier ();
#endif

	g_assert (node->next == FREE_NEXT);
	node->next = END_MARKER;
	for (;;) {
		MonoLockFreeQueueNode *next;

		tail = (MonoLockFreeQueueNode *) mono_get_hazardous_pointer ((gpointer volatile*)&q->tail, hp, 0);
		mono_memory_read_barrier ();
		/*
		 * We never dereference next so we don't need a
		 * hazardous load.
		 */
		next = tail->next;
		mono_memory_read_barrier ();

		/* Are tail and next consistent? */
		if (tail == q->tail) {
			g_assert (next != INVALID_NEXT && next != FREE_NEXT);
			g_assert (next != tail);

			if (next == END_MARKER) {
				/*
				 * Here we require that nodes that
				 * have been dequeued don't have
				 * next==END_MARKER.  If they did, we
				 * might append to a node that isn't
				 * in the queue anymore here.
				 */
				if (mono_atomic_cas_ptr ((gpointer volatile*)&tail->next, node, END_MARKER) == END_MARKER)
					break;
			} else {
				/* Try to advance tail */
				mono_atomic_cas_ptr ((gpointer volatile*)&q->tail, next, tail);
			}
		}

		mono_memory_write_barrier ();
		mono_hazard_pointer_clear (hp, 0);
	}

	/* Try to advance tail */
	mono_atomic_cas_ptr ((gpointer volatile*)&q->tail, node, tail);

	mono_memory_write_barrier ();
	mono_hazard_pointer_clear (hp, 0);
}

static void
free_dummy (gpointer _dummy)
{
	MonoLockFreeQueueDummy *dummy = (MonoLockFreeQueueDummy *) _dummy;
	mono_lock_free_queue_node_unpoison (&dummy->node);
	g_assert (dummy->in_use);
	mono_memory_write_barrier ();
	dummy->in_use = 0;
}

static MonoLockFreeQueueDummy*
get_dummy (MonoLockFreeQueue *q)
{
	int i;
	for (i = 0; i < MONO_LOCK_FREE_QUEUE_NUM_DUMMIES; ++i) {
		MonoLockFreeQueueDummy *dummy = &q->dummies [i];

		if (dummy->in_use)
			continue;

		if (mono_atomic_cas_i32 (&dummy->in_use, 1, 0) == 0)
			return dummy;
	}
	return NULL;
}

static gboolean
is_dummy (MonoLockFreeQueue *q, MonoLockFreeQueueNode *n)
{
	return n >= &q->dummies [0].node && n <= &q->dummies [MONO_LOCK_FREE_QUEUE_NUM_DUMMIES-1].node;
}

static gboolean
try_reenqueue_dummy (MonoLockFreeQueue *q)
{
	MonoLockFreeQueueDummy *dummy;

	if (q->has_dummy)
		return FALSE;

	dummy = get_dummy (q);
	if (!dummy)
		return FALSE;

	if (mono_atomic_cas_i32 (&q->has_dummy, 1, 0) != 0) {
		dummy->in_use = 0;
		return FALSE;
	}

	mono_lock_free_queue_enqueue (q, &dummy->node);

	return TRUE;
}

/*
 * Dequeues a node from @q. Returns NULL if no nodes are available. The returned
 * node is hazardous and must be freed with @mono_thread_hazardous_try_free or
 * @mono_thread_hazardous_queue_free - it must not be freed directly.
 */
MonoLockFreeQueueNode*
mono_lock_free_queue_dequeue (MonoLockFreeQueue *q)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoLockFreeQueueNode *head;

 retry:
	for (;;) {
		MonoLockFreeQueueNode *tail, *next;

		head = (MonoLockFreeQueueNode *) mono_get_hazardous_pointer ((gpointer volatile*)&q->head, hp, 0);
		tail = (MonoLockFreeQueueNode*)q->tail;
		mono_memory_read_barrier ();
		next = head->next;
		mono_memory_read_barrier ();

		/* Are head, tail and next consistent? */
		if (head == q->head) {
			g_assert (next != INVALID_NEXT && next != FREE_NEXT);
			g_assert (next != head);

			/* Is queue empty or tail behind? */
			if (head == tail) {
				if (next == END_MARKER) {
					/* Queue is empty */
					mono_hazard_pointer_clear (hp, 0);

					/*
					 * We only continue if we
					 * reenqueue the dummy
					 * ourselves, so as not to
					 * wait for threads that might
					 * not actually run.
					 */
					if (!is_dummy (q, head) && try_reenqueue_dummy (q))
						continue;

					return NULL;
				}

				/* Try to advance tail */
				mono_atomic_cas_ptr ((gpointer volatile*)&q->tail, next, tail);
			} else {
				g_assert (next != END_MARKER);
				/* Try to dequeue head */
				if (mono_atomic_cas_ptr ((gpointer volatile*)&q->head, next, head) == head)
					break;
			}
		}

		mono_memory_write_barrier ();
		mono_hazard_pointer_clear (hp, 0);
	}

	/*
	 * The head is dequeued now, so we know it's this thread's
	 * responsibility to free it - no other thread can.
	 */
	mono_memory_write_barrier ();
	mono_hazard_pointer_clear (hp, 0);

	g_assert (head->next);
	/*
	 * Setting next here isn't necessary for correctness, but we
	 * do it to make sure that we catch dereferencing next in a
	 * node that's not in the queue anymore.
	 */
	head->next = INVALID_NEXT;
#if QUEUE_DEBUG
	g_assert (head->in_queue);
	head->in_queue = FALSE;
	mono_memory_write_barrier ();
#endif

	if (is_dummy (q, head)) {
		g_assert (q->has_dummy);
		q->has_dummy = 0;
		mono_memory_write_barrier ();
		mono_thread_hazardous_try_free (head, free_dummy);
		if (try_reenqueue_dummy (q))
			goto retry;
		return NULL;
	}

	/* The caller must hazardously free the node. */
	return head;
}
