/*
 * mono-cq.c: concurrent queue
 *
 * Authors:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *
 * Copyright (c) 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc
 */

#include <mono/metadata/object.h>
#include <mono/metadata/mono-cq.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/atomic.h>

#define CQ_DEBUG(...)
//#define CQ_DEBUG(...) g_message(__VA_ARGS__)

struct _MonoCQ {
	MonoMList *head;
	MonoMList *tail;
	volatile gint32 count;
};

/* matches the System.MonoListItem object */
struct _MonoMList {
	MonoObject object;
	MonoMList *next;
	MonoObject *data;
};

/* matches the System.MonoCQItem object */
struct _MonoCQItem {
	MonoObject object;
	MonoArray *array; // MonoObjects
	MonoArray *array_state; // byte array
	volatile gint32 first;
	volatile gint32 last;
};

typedef struct _MonoCQItem MonoCQItem;
#define CQ_ARRAY_SIZE	64

static MonoVTable *monocq_item_vtable = NULL;

static MonoCQItem *
mono_cqitem_alloc (void)
{
	MonoCQItem *queue;
	MonoDomain *domain = mono_get_root_domain ();

	if (!monocq_item_vtable) {
		MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System", "MonoCQItem");
		monocq_item_vtable = mono_class_vtable (domain, klass);
		g_assert (monocq_item_vtable);
	}
	queue = (MonoCQItem *) mono_object_new_fast (monocq_item_vtable);
	MONO_OBJECT_SETREF (queue, array, mono_array_new (domain, mono_defaults.object_class, CQ_ARRAY_SIZE));
	MONO_OBJECT_SETREF (queue, array_state, mono_array_new (domain, mono_defaults.byte_class, CQ_ARRAY_SIZE));
	return queue;
}

MonoCQ *
mono_cq_create ()
{
	MonoCQ *cq;

	cq = g_new0 (MonoCQ, 1);
	MONO_GC_REGISTER_ROOT_SINGLE (cq->head);
	MONO_GC_REGISTER_ROOT_SINGLE (cq->tail);
	cq->head = mono_mlist_alloc ((MonoObject *) mono_cqitem_alloc ());
	cq->tail = cq->head;
	CQ_DEBUG ("Created %p", cq);
	return cq;
}

void
mono_cq_destroy (MonoCQ *cq)
{
	CQ_DEBUG ("Destroy %p", cq);
	if (!cq)
		return;

	mono_gc_bzero (cq, sizeof (MonoCQ));
	MONO_GC_UNREGISTER_ROOT (cq->tail);
	MONO_GC_UNREGISTER_ROOT (cq->head);
	g_free (cq);
}

gint32
mono_cq_count (MonoCQ *cq)
{
	if (!cq)
		return 0;

	CQ_DEBUG ("Count %d", cq->count);
	return cq->count;
}

static void
mono_cq_add_node (MonoCQ *cq)
{
	MonoMList *n;
	MonoMList *prev_tail;

	CQ_DEBUG ("Adding node");
	n = mono_mlist_alloc ((MonoObject *) mono_cqitem_alloc ());
	prev_tail = cq->tail;
	MONO_OBJECT_SETREF (prev_tail, next, n);

	/* prev_tail->next must be visible before the new tail is */
	STORE_STORE_FENCE;

	cq->tail = n;
}

static gboolean
mono_cqitem_try_enqueue (MonoCQ *cq, MonoObject *obj)
{
	MonoCQItem *queue;
	MonoMList *tail;
	gint32 pos;

	tail = cq->tail;
	queue = (MonoCQItem *) tail->data;
	do {
		pos = queue->last;
		if (pos >= CQ_ARRAY_SIZE) {
			CQ_DEBUG ("enqueue(): pos >= CQ_ARRAY_SIZE, %d >= %d", pos, CQ_ARRAY_SIZE);
			return FALSE;
		}

		if (InterlockedCompareExchange (&queue->last, pos + 1, pos) == pos) {
			mono_array_setref (queue->array, pos, obj);
			STORE_STORE_FENCE;
			mono_array_set (queue->array_state, char, pos, TRUE);
			if ((pos + 1) == CQ_ARRAY_SIZE) {
				CQ_DEBUG ("enqueue(): pos + 1 == CQ_ARRAY_SIZE, %d. Adding node.", CQ_ARRAY_SIZE);
				mono_cq_add_node (cq);
			}
			return TRUE;
		}
	} while (TRUE);
	g_assert_not_reached ();
}

void
mono_cq_enqueue (MonoCQ *cq, MonoObject *obj)
{
	if (cq == NULL || obj == NULL)
		return;

	do {
		if (mono_cqitem_try_enqueue (cq, obj)) {
			CQ_DEBUG ("Queued one");
			InterlockedIncrement (&cq->count);
			break;
		}
		SleepEx (0, FALSE);
	} while (TRUE);
}

static void
mono_cq_remove_node (MonoCQ *cq)
{
	MonoMList *old_head;

	CQ_DEBUG ("Removing node");
	old_head = cq->head;
	/* Not needed now that array_state is GC memory
	MonoCQItem *queue;
	int i;
	gboolean retry;
	queue = (MonoCQItem *) old_head->data;
	do {
		retry = FALSE;
		for (i = 0; i < CQ_ARRAY_SIZE; i++) {
			if (mono_array_get (queue->array_state, char, i) == TRUE) {
				retry = TRUE;
				break;
			}
		}
		if (retry)
			SleepEx (0, FALSE);
	} while (retry);
	 */
	while (old_head->next == NULL)
		SleepEx (0, FALSE);
	cq->head = old_head->next;
	old_head = NULL;
}

static gboolean
mono_cqitem_try_dequeue (MonoCQ *cq, MonoObject **obj)
{
	MonoCQItem *queue;
	MonoMList *head;
	gint32 pos;

	head = cq->head;
	queue = (MonoCQItem *) head->data;
	do {
		pos = queue->first;
		if (pos >= queue->last || pos >= CQ_ARRAY_SIZE)
			return FALSE;

		if (InterlockedCompareExchange (&queue->first, pos + 1, pos) == pos) {
			while (mono_array_get (queue->array_state, char, pos) == FALSE) {
				SleepEx (0, FALSE);
			}
			LOAD_LOAD_FENCE;
			*obj = mono_array_get (queue->array, MonoObject *, pos);

			/*
			Here don't need to fence since the only spot that reads it is the one above.
			Additionally, the first store is superfluous, so it can happen OOO with the second.
			*/
			mono_array_set (queue->array, MonoObject *, pos, NULL);
			mono_array_set (queue->array_state, char, pos, FALSE);
			
			/*
			We should do a STORE_LOAD fence here to make sure subsequent loads see new state instead
			of the above stores. We can safely ignore this as the only issue of seeing a stale value
			is the thread yielding. Given how unfrequent this will be in practice, we better avoid the
			very expensive STORE_LOAD fence.
			*/
			
			if ((pos + 1) == CQ_ARRAY_SIZE) {
				mono_cq_remove_node (cq);
			}
			return TRUE;
		}
	} while (TRUE);
	g_assert_not_reached ();
}

gboolean
mono_cq_dequeue (MonoCQ *cq, MonoObject **result)
{
	while (cq->count > 0) {
		if (mono_cqitem_try_dequeue (cq, result)) {
			CQ_DEBUG ("Dequeued one");
			InterlockedDecrement (&cq->count);
			return TRUE;
		}
		SleepEx (0, FALSE);
	}
	return FALSE;
}

