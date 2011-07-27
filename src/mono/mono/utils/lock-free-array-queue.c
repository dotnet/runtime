/*
 * lock-free-array-queue.c: A lock-free somewhat-queue that doesn't
 * require hazard pointers.
 *
 * (C) Copyright 2011 Xamarin Inc.
 */

/*
 * The queue is a linked list of arrays (chunks).  Chunks are never
 * removed from the list, only added to the end, in a lock-free manner.
 *
 * Adding or removing an entry in the queue is only possible at the
 * end.  To do so, the thread first has to increment or decrement
 * q->num_used_entries.  The entry thus added or removed now "belongs"
 * to that thread.  It first CASes the state to BUSY, writes/reads the
 * entry data, and then sets the state to USED or FREE.
 */

#include <mono/io-layer/io-layer.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-mmap.h>

#include <mono/utils/lock-free-array-queue.h>

enum {
	STATE_FREE,
	STATE_USED,
	STATE_BUSY
};

typedef struct {
	gint32 state;
	gpointer data [MONO_ZERO_LEN_ARRAY];
} Entry;

struct _MonoLockFreeArrayQueueChunk {
	MonoLockFreeArrayQueueChunk *next;
	gint32 num_entries;
	char entries [MONO_ZERO_LEN_ARRAY];
};

typedef MonoLockFreeArrayQueueChunk Chunk;
typedef MonoLockFreeArrayQueue Queue;

#define ENTRY_SIZE(q)	(sizeof (Entry) - sizeof (gpointer) * MONO_ZERO_LEN_ARRAY + (q)->entry_size)

static Chunk*
alloc_chunk (Queue *q)
{
	int size = mono_pagesize ();
	int num_entries = (size - (sizeof (Chunk) - ENTRY_SIZE (q) * MONO_ZERO_LEN_ARRAY)) / ENTRY_SIZE (q);
	Chunk *chunk = mono_valloc (0, size, MONO_MMAP_READ | MONO_MMAP_WRITE);
	chunk->num_entries = num_entries;
	return chunk;
}

static void
free_chunk (Chunk *chunk)
{
	mono_vfree (chunk, mono_pagesize ());
}

static Entry*
get_entry (Queue *q, int index)
{
	Chunk *chunk;

	g_assert (index >= 0);

	if (!q->chunk_list) {
		chunk = alloc_chunk (q);
		mono_memory_write_barrier ();
		if (InterlockedCompareExchangePointer ((volatile gpointer *)&q->chunk_list, chunk, NULL) != NULL)
			free_chunk (chunk);
	}

	chunk = q->chunk_list;
	g_assert (chunk);

	while (index >= chunk->num_entries) {
		Chunk *next = chunk->next;
		if (!next) {
			next = alloc_chunk (q);
			mono_memory_write_barrier ();
			if (InterlockedCompareExchangePointer ((volatile gpointer *) &chunk->next, next, NULL) != NULL) {
				free_chunk (next);
				next = chunk->next;
				g_assert (next);
			}
		}
		index -= chunk->num_entries;
		chunk = next;
	}

	return (Entry*) (chunk->entries + index * ENTRY_SIZE (q));
}

void
mono_lock_free_array_queue_push (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr)
{
	int index, num_used;
	Entry *entry;

	do {
		index = InterlockedIncrement (&q->num_used_entries) - 1;
		entry = get_entry (q, index);
	} while (InterlockedCompareExchange (&entry->state, STATE_BUSY, STATE_FREE) != STATE_FREE);

	mono_memory_write_barrier ();

	memcpy (entry->data, entry_data_ptr, q->entry_size);

	mono_memory_write_barrier ();

	entry->state = STATE_USED;

	mono_memory_barrier ();

	do {
		num_used = q->num_used_entries;
		if (num_used > index)
			break;
	} while (InterlockedCompareExchange (&q->num_used_entries, index + 1, num_used) != num_used);

	mono_memory_write_barrier ();
}

gboolean
mono_lock_free_array_queue_pop (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr)
{
	int index;
	Entry *entry;

	do {
		do {
			index = q->num_used_entries;
			if (index == 0)
				return FALSE;
		} while (InterlockedCompareExchange (&q->num_used_entries, index - 1, index) != index);

		entry = get_entry (q, index - 1);
	} while (InterlockedCompareExchange (&entry->state, STATE_BUSY, STATE_USED) != STATE_USED);

	/* Reading the item must happen before CASing the state. */
	mono_memory_barrier ();

	memcpy (entry_data_ptr, entry->data, q->entry_size);

	mono_memory_barrier ();

	entry->state = STATE_FREE;

	mono_memory_write_barrier ();

	return TRUE;
}

void
mono_lock_free_array_queue_cleanup (MonoLockFreeArrayQueue *q)
{
	Chunk *chunk;

	chunk = q->chunk_list;
	q->chunk_list = NULL;
	while (chunk) {
		Chunk *next = chunk->next;
		free_chunk (chunk);
		chunk = next;
	}
	q->num_used_entries = 0;
}
