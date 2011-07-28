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

struct _MonoLockFreeArrayChunk {
	MonoLockFreeArrayChunk *next;
	gint32 num_entries;
	char entries [MONO_ZERO_LEN_ARRAY];
};

typedef MonoLockFreeArrayChunk Chunk;

#define CHUNK_NTH(arr,chunk,index)	((chunk)->entries + (index) * (arr)->entry_size)

static Chunk*
alloc_chunk (MonoLockFreeArray *arr)
{
	int size = mono_pagesize ();
	int num_entries = (size - (sizeof (Chunk) - arr->entry_size * MONO_ZERO_LEN_ARRAY)) / arr->entry_size;
	Chunk *chunk = mono_valloc (0, size, MONO_MMAP_READ | MONO_MMAP_WRITE);
	g_assert (chunk);
	chunk->num_entries = num_entries;
	return chunk;
}

static void
free_chunk (Chunk *chunk)
{
	mono_vfree (chunk, mono_pagesize ());
}

gpointer
mono_lock_free_array_nth (MonoLockFreeArray *arr, int index)
{
	Chunk *chunk;

	g_assert (index >= 0);

	if (!arr->chunk_list) {
		chunk = alloc_chunk (arr);
		mono_memory_write_barrier ();
		if (InterlockedCompareExchangePointer ((volatile gpointer *)&arr->chunk_list, chunk, NULL) != NULL)
			free_chunk (chunk);
	}

	chunk = arr->chunk_list;
	g_assert (chunk);

	while (index >= chunk->num_entries) {
		Chunk *next = chunk->next;
		if (!next) {
			next = alloc_chunk (arr);
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

	return CHUNK_NTH (arr, chunk, index);
}

gpointer
mono_lock_free_array_iterate (MonoLockFreeArray *arr, MonoLockFreeArrayIterateFunc func, gpointer user_data)
{
	Chunk *chunk;
	for (chunk = arr->chunk_list; chunk; chunk = chunk->next) {
		int i;
		for (i = 0; i < chunk->num_entries; ++i) {
			gpointer result = func (i, CHUNK_NTH (arr, chunk, i), user_data);
			if (result)
				return result;
		}
	}
	return NULL;
}

void
mono_lock_free_array_cleanup (MonoLockFreeArray *arr)
{
	Chunk *chunk;

	chunk = arr->chunk_list;
	arr->chunk_list = NULL;
	while (chunk) {
		Chunk *next = chunk->next;
		free_chunk (chunk);
		chunk = next;
	}
}

enum {
	STATE_FREE,
	STATE_USED,
	STATE_BUSY
};

typedef struct {
	gint32 state;
	gpointer data [MONO_ZERO_LEN_ARRAY];
} Entry;

typedef MonoLockFreeArrayQueue Queue;

/* The queue's entry size, calculated from the array's. */
#define ENTRY_SIZE(q)	((q)->array.entry_size - sizeof (gpointer))

void
mono_lock_free_array_queue_push (MonoLockFreeArrayQueue *q, gpointer entry_data_ptr)
{
	int index, num_used;
	Entry *entry;

	do {
		index = InterlockedIncrement (&q->num_used_entries) - 1;
		entry = mono_lock_free_array_nth (&q->array, index);
	} while (InterlockedCompareExchange (&entry->state, STATE_BUSY, STATE_FREE) != STATE_FREE);

	mono_memory_write_barrier ();

	memcpy (entry->data, entry_data_ptr, ENTRY_SIZE (q));

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

		entry = mono_lock_free_array_nth (&q->array, index - 1);
	} while (InterlockedCompareExchange (&entry->state, STATE_BUSY, STATE_USED) != STATE_USED);

	/* Reading the item must happen before CASing the state. */
	mono_memory_barrier ();

	memcpy (entry_data_ptr, entry->data, ENTRY_SIZE (q));

	mono_memory_barrier ();

	entry->state = STATE_FREE;

	mono_memory_write_barrier ();

	return TRUE;
}

void
mono_lock_free_array_queue_cleanup (MonoLockFreeArrayQueue *q)
{
	mono_lock_free_array_cleanup (&q->array);
	q->num_used_entries = 0;
}
