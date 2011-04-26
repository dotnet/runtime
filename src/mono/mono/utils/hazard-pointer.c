/*
 * hazard-pointer.c: Hazard pointer related code.
 *
 * (C) Copyright 2011 Novell, Inc
 */

#include <config.h>

#include <mono/metadata/class-internals.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-threads.h>
#include <mono/io-layer/io-layer.h>

typedef struct {
	gpointer p;
	MonoHazardousFreeFunc free_func;
} DelayedFreeItem;

enum {
	DFE_STATE_FREE,
	DFE_STATE_USED,
	DFE_STATE_BUSY
};

typedef struct {
	gint32 state;
	DelayedFreeItem item;
} DelayedFreeEntry;

typedef struct _DelayedFreeChunk DelayedFreeChunk;
struct _DelayedFreeChunk {
	DelayedFreeChunk *next;
	gint32 num_entries;
	DelayedFreeEntry entries [MONO_ZERO_LEN_ARRAY];
};

/* The hazard table */
#if MONO_SMALL_CONFIG
#define HAZARD_TABLE_MAX_SIZE	256
#else
#define HAZARD_TABLE_MAX_SIZE	16384 /* There cannot be more threads than this number. */
#endif

static volatile int hazard_table_size = 0;
static MonoThreadHazardPointers * volatile hazard_table = NULL;

/* The table where we keep pointers to blocks to be freed but that
   have to wait because they're guarded by a hazard pointer. */
static volatile gint32 num_used_delayed_free_entries;
static DelayedFreeChunk *delayed_free_chunk_list;

/* The table for small ID assignment */
static CRITICAL_SECTION small_id_mutex;
static int small_id_next;
static int highest_small_id = -1;
static MonoBitSet *small_id_table;

/*
 * Delayed free table
 *
 * The table is a linked list of arrays (chunks).  Chunks are never
 * removed from the list, only added to the end, in a lock-free manner.
 *
 * Adding or removing an entry in the table is only possible at the end.
 * To do so, the thread first has to increment or decrement
 * num_used_delayed_free_entries.  The entry thus added or removed now
 * "belongs" to that thread.  It first CASes the state to BUSY,
 * writes/reads the entry, and then sets the state to USED or FREE.
 *
 * Note that it's possible that there is contention.  Some thread will
 * always make progress, though.
 *
 * The simplest case of contention is one thread pushing and another
 * thread popping the same entry.  The state is FREE at first, so the
 * pushing thread succeeds in setting it to BUSY.  The popping thread
 * will only succeed with its CAS once the state is USED, which is the
 * case after the pushing thread has finished pushing.
 */

static DelayedFreeChunk*
alloc_delayed_free_chunk (void)
{
	int size = mono_pagesize ();
	int num_entries = (size - (sizeof (DelayedFreeChunk) - sizeof (DelayedFreeEntry) * MONO_ZERO_LEN_ARRAY)) / sizeof (DelayedFreeEntry);
	DelayedFreeChunk *chunk = mono_valloc (0, size, MONO_MMAP_READ | MONO_MMAP_WRITE);
	chunk->num_entries = num_entries;
	return chunk;
}

static void
free_delayed_free_chunk (DelayedFreeChunk *chunk)
{
	mono_vfree (chunk, mono_pagesize ());
}

static DelayedFreeEntry*
get_delayed_free_entry (int index)
{
	DelayedFreeChunk *chunk;

	g_assert (index >= 0);

	if (!delayed_free_chunk_list) {
		chunk = alloc_delayed_free_chunk ();
		mono_memory_write_barrier ();
		if (InterlockedCompareExchangePointer ((volatile gpointer *)&delayed_free_chunk_list, chunk, NULL) != NULL)
			free_delayed_free_chunk (chunk);
	}

	chunk = delayed_free_chunk_list;
	g_assert (chunk);

	while (index >= chunk->num_entries) {
		DelayedFreeChunk *next = chunk->next;
		if (!next) {
			next = alloc_delayed_free_chunk ();
			mono_memory_write_barrier ();
			if (InterlockedCompareExchangePointer ((volatile gpointer *) &chunk->next, next, NULL) != NULL) {
				free_delayed_free_chunk (next);
				next = chunk->next;
				g_assert (next);
			}
		}
		index -= chunk->num_entries;
		chunk = next;
	}

	return &chunk->entries [index];
}

static void
delayed_free_push (DelayedFreeItem item)
{
	int index = InterlockedIncrement (&num_used_delayed_free_entries) - 1;
	DelayedFreeEntry *entry = get_delayed_free_entry (index);

	while (InterlockedCompareExchange (&entry->state, DFE_STATE_BUSY, DFE_STATE_FREE) != DFE_STATE_FREE)
		;

	mono_memory_write_barrier ();

	entry->item = item;

	mono_memory_write_barrier ();

	entry->state = DFE_STATE_USED;
}

static gboolean
delayed_free_pop (DelayedFreeItem *item)
{
	int index;
	DelayedFreeEntry *entry;

	do {
		index = num_used_delayed_free_entries;
		if (index == 0)
			return FALSE;
	} while (InterlockedCompareExchange (&num_used_delayed_free_entries, index - 1, index) != index);

	--index;

	entry = get_delayed_free_entry (index);

	while (InterlockedCompareExchange (&entry->state, DFE_STATE_BUSY, DFE_STATE_USED) != DFE_STATE_USED)
		;

	mono_memory_write_barrier ();

	*item = entry->item;

	mono_memory_write_barrier ();

	entry->state = DFE_STATE_FREE;

	return TRUE;
}

/*
 * Allocate a small thread id.
 *
 * FIXME: The biggest part of this function is very similar to
 * domain_id_alloc() in domain.c and should be merged.
 */
int
mono_thread_small_id_alloc (void)
{
	int i, id = -1;

	EnterCriticalSection (&small_id_mutex);

	if (!small_id_table)
		small_id_table = mono_bitset_new (1, 0);

	id = mono_bitset_find_first_unset (small_id_table, small_id_next);
	if (id == -1)
		id = mono_bitset_find_first_unset (small_id_table, -1);

	if (id == -1) {
		MonoBitSet *new_table;
		if (small_id_table->size * 2 >= (1 << 16))
			g_assert_not_reached ();
		new_table = mono_bitset_clone (small_id_table, small_id_table->size * 2);
		id = mono_bitset_find_first_unset (new_table, small_id_table->size - 1);

		mono_bitset_free (small_id_table);
		small_id_table = new_table;
	}

	g_assert (!mono_bitset_test_fast (small_id_table, id));
	mono_bitset_set_fast (small_id_table, id);

	small_id_next++;
	if (small_id_next >= small_id_table->size)
		small_id_next = 0;

	g_assert (id < HAZARD_TABLE_MAX_SIZE);
	if (id >= hazard_table_size) {
#if MONO_SMALL_CONFIG
		hazard_table = g_malloc0 (sizeof (MonoThreadHazardPointers) * HAZARD_TABLE_MAX_SIZE);
		hazard_table_size = HAZARD_TABLE_MAX_SIZE;
#else
		gpointer page_addr;
		int pagesize = mono_pagesize ();
		int num_pages = (hazard_table_size * sizeof (MonoThreadHazardPointers) + pagesize - 1) / pagesize;

		if (hazard_table == NULL) {
			hazard_table = mono_valloc (NULL,
				sizeof (MonoThreadHazardPointers) * HAZARD_TABLE_MAX_SIZE,
				MONO_MMAP_NONE);
		}

		g_assert (hazard_table != NULL);
		page_addr = (guint8*)hazard_table + num_pages * pagesize;

		mono_mprotect (page_addr, pagesize, MONO_MMAP_READ | MONO_MMAP_WRITE);

		++num_pages;
		hazard_table_size = num_pages * pagesize / sizeof (MonoThreadHazardPointers);

#endif
		g_assert (id < hazard_table_size);
		for (i = 0; i < HAZARD_POINTER_COUNT; ++i)
			hazard_table [id].hazard_pointers [i] = NULL;
	}

	if (id > highest_small_id) {
		highest_small_id = id;
		mono_memory_write_barrier ();
	}

	LeaveCriticalSection (&small_id_mutex);

	return id;
}

void
mono_thread_small_id_free (int id)
{
	/* MonoBitSet operations are not atomic. */
	EnterCriticalSection (&small_id_mutex);

	g_assert (id >= 0 && id < small_id_table->size);
	g_assert (mono_bitset_test_fast (small_id_table, id));
	mono_bitset_clear_fast (small_id_table, id);

	LeaveCriticalSection (&small_id_mutex);
}

static gboolean
is_pointer_hazardous (gpointer p)
{
	int i;
	int highest = highest_small_id;

	g_assert (highest < hazard_table_size);

	for (i = 0; i <= highest; ++i) {
		if (hazard_table [i].hazard_pointers [0] == p
				|| hazard_table [i].hazard_pointers [1] == p)
			return TRUE;
	}

	return FALSE;
}

MonoThreadHazardPointers*
mono_hazard_pointer_get (void)
{
	MonoThreadInfo *current_thread = mono_thread_info_current ();

	if (!(current_thread && current_thread->small_id >= 0)) {
		static MonoThreadHazardPointers emerg_hazard_table;
		g_warning ("Thread %p may have been prematurely finalized", current_thread);
		return &emerg_hazard_table;
	}

	return &hazard_table [current_thread->small_id];
}

MonoThreadHazardPointers*
mono_hazard_pointer_get_by_id (int small_id)
{
	g_assert (small_id >= 0 && small_id <= highest_small_id);
	return &hazard_table [small_id];
}

/* Can be called with hp==NULL, in which case it acts as an ordinary
   pointer fetch.  It's used that way indirectly from
   mono_jit_info_table_add(), which doesn't have to care about hazards
   because it holds the respective domain lock. */
gpointer
get_hazardous_pointer (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index)
{
	gpointer p;

	for (;;) {
		/* Get the pointer */
		p = *pp;
		/* If we don't have hazard pointers just return the
		   pointer. */
		if (!hp)
			return p;
		/* Make it hazardous */
		mono_hazard_pointer_set (hp, hazard_index, p);
		/* Check that it's still the same.  If not, try
		   again. */
		if (*pp != p) {
			mono_hazard_pointer_clear (hp, hazard_index);
			continue;
		}
		break;
	}

	return p;
}

static gboolean
try_free_delayed_free_item (void)
{
	DelayedFreeItem item;
	gboolean popped = delayed_free_pop (&item);

	if (!popped)
		return FALSE;

	if (is_pointer_hazardous (item.p)) {
		delayed_free_push (item);
		return FALSE;
	}

	item.free_func (item.p);

	return TRUE;
}

void
mono_thread_hazardous_free_or_queue (gpointer p, MonoHazardousFreeFunc free_func)
{
	int i;

	/* First try to free a few entries in the delayed free
	   table. */
	for (i = 0; i < 3; ++i)
		try_free_delayed_free_item ();

	/* Now see if the pointer we're freeing is hazardous.  If it
	   isn't, free it.  Otherwise put it in the delay list. */
	if (is_pointer_hazardous (p)) {
		DelayedFreeItem item = { p, free_func };

		++mono_stats.hazardous_pointer_count;

		delayed_free_push (item);
	} else {
		free_func (p);
	}
}

void
mono_thread_hazardous_try_free_all (void)
{
	while (try_free_delayed_free_item ())
		;
}

void
mono_thread_smr_init (void)
{
	InitializeCriticalSection(&small_id_mutex);
}

void
mono_thread_smr_cleanup (void)
{
	DelayedFreeChunk *chunk;

	mono_thread_hazardous_try_free_all ();

	chunk = delayed_free_chunk_list;
	delayed_free_chunk_list = NULL;
	while (chunk) {
		DelayedFreeChunk *next = chunk->next;
		free_delayed_free_chunk (chunk);
		chunk = next;
	}

	/*FIXME, can't we release the small id table here?*/
}
