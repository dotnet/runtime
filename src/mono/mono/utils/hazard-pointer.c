/**
 * \file
 * Hazard pointer related code.
 *
 * (C) Copyright 2011 Novell, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <string.h>

#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/lock-free-array-queue.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-os-mutex.h>
#ifdef SGEN_WITHOUT_MONO
#include <mono/sgen/sgen-gc.h>
#include <mono/sgen/sgen-client.h>
#else
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-counters.h>
#endif

#if _MSC_VER
#pragma warning(disable:4312) // FIXME pointer cast to different size
#endif

typedef struct {
	gpointer p;
	MonoHazardousFreeFunc free_func;
} DelayedFreeItem;

/* The hazard table */
#if MONO_SMALL_CONFIG
#define HAZARD_TABLE_MAX_SIZE	256
#define HAZARD_TABLE_OVERFLOW	4
#else
#define HAZARD_TABLE_MAX_SIZE	16384 /* There cannot be more threads than this number. */
#define HAZARD_TABLE_OVERFLOW	64
#endif

static volatile int hazard_table_size = 0;
static MonoThreadHazardPointers * volatile hazard_table = NULL;
static MonoHazardFreeQueueSizeCallback queue_size_cb;

/*
 * Each entry is either 0 or 1, indicating whether that overflow small
 * ID is busy.
 */
static volatile gint32 overflow_busy [HAZARD_TABLE_OVERFLOW];

/* The table where we keep pointers to blocks to be freed but that
   have to wait because they're guarded by a hazard pointer. */
static MonoLockFreeArrayQueue delayed_free_queue = MONO_LOCK_FREE_ARRAY_QUEUE_INIT (sizeof (DelayedFreeItem), MONO_MEM_ACCOUNT_HAZARD_POINTERS);

/* The table for small ID assignment */
static mono_mutex_t small_id_mutex;
static int small_id_next;
static int highest_small_id = -1;
static MonoBitSet *small_id_table;
static int hazardous_pointer_count;

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

	mono_os_mutex_lock (&small_id_mutex);

	if (!small_id_table)
		small_id_table = mono_bitset_new (1, 0);

	id = mono_bitset_find_first_unset (small_id_table, small_id_next - 1);
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
#if defined(__PASE__)
		/*
		 * HACK: allocating the table with none prot will cause i 7.1
		 * to segfault when accessing or protecting it
		 */
		int table_prot = MONO_MMAP_READ | MONO_MMAP_WRITE;
#else
		int table_prot = MONO_MMAP_NONE;
#endif
		int pagesize = mono_pagesize ();
		int num_pages = (hazard_table_size * sizeof (MonoThreadHazardPointers) + pagesize - 1) / pagesize;

		if (hazard_table == NULL) {
			hazard_table = (MonoThreadHazardPointers*) mono_valloc (NULL,
				sizeof (MonoThreadHazardPointers) * HAZARD_TABLE_MAX_SIZE,
				table_prot, MONO_MEM_ACCOUNT_HAZARD_POINTERS);
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

	mono_os_mutex_unlock (&small_id_mutex);

	return id;
}

void
mono_thread_small_id_free (int id)
{
	/* MonoBitSet operations are not atomic. */
	mono_os_mutex_lock (&small_id_mutex);

	g_assert (id >= 0 && id < small_id_table->size);
	g_assert (mono_bitset_test_fast (small_id_table, id));
	mono_bitset_clear_fast (small_id_table, id);

	mono_os_mutex_unlock (&small_id_mutex);
}

static gboolean
is_pointer_hazardous (gpointer p)
{
	int i, j;
	int highest = highest_small_id;

	g_assert (highest < hazard_table_size);

	for (i = 0; i <= highest; ++i) {
		for (j = 0; j < HAZARD_POINTER_COUNT; ++j) {
			if (hazard_table [i].hazard_pointers [j] == p)
				return TRUE;
			LOAD_LOAD_FENCE;
		}
	}

	return FALSE;
}

MonoThreadHazardPointers*
mono_hazard_pointer_get (void)
{
	int small_id = mono_thread_info_get_small_id ();

	if (small_id < 0) {
		static MonoThreadHazardPointers emerg_hazard_table;
		g_warning ("Thread %p may have been prematurely finalized", (gpointer) (gsize) mono_native_thread_id_get ());
		return &emerg_hazard_table;
	}

	return &hazard_table [small_id];
}

/* Can be called with hp==NULL, in which case it acts as an ordinary
   pointer fetch.  It's used that way indirectly from
   mono_jit_info_table_add(), which doesn't have to care about hazards
   because it holds the respective domain lock. */
gpointer
mono_get_hazardous_pointer (gpointer volatile *pp, MonoThreadHazardPointers *hp, int hazard_index)
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

int
mono_hazard_pointer_save_for_signal_handler (void)
{
	int small_id, i;
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoThreadHazardPointers *hp_overflow;

	for (i = 0; i < HAZARD_POINTER_COUNT; ++i)
		if (hp->hazard_pointers [i])
			goto search;
	return -1;

 search:
	for (small_id = 0; small_id < HAZARD_TABLE_OVERFLOW; ++small_id) {
		if (!overflow_busy [small_id])
			break;
	}

	/*
	 * If this assert fails we don't have enough overflow slots.
	 * We should contemplate adding them dynamically.  If we can
	 * make mono_thread_small_id_alloc() lock-free we can just
	 * allocate them on-demand.
	 */
	g_assert (small_id < HAZARD_TABLE_OVERFLOW);

	if (mono_atomic_cas_i32 (&overflow_busy [small_id], 1, 0) != 0)
		goto search;

	hp_overflow = &hazard_table [small_id];

	for (i = 0; i < HAZARD_POINTER_COUNT; ++i)
		g_assert (!hp_overflow->hazard_pointers [i]);
	*hp_overflow = *hp;

	mono_memory_write_barrier ();

	memset (hp, 0, sizeof (MonoThreadHazardPointers));

	return small_id;
}

void
mono_hazard_pointer_restore_for_signal_handler (int small_id)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();
	MonoThreadHazardPointers *hp_overflow;
	int i;

	if (small_id < 0)
		return;

	g_assert (small_id < HAZARD_TABLE_OVERFLOW);
	g_assert (overflow_busy [small_id]);

	for (i = 0; i < HAZARD_POINTER_COUNT; ++i)
		g_assert (!hp->hazard_pointers [i]);

	hp_overflow = &hazard_table [small_id];

	*hp = *hp_overflow;

	mono_memory_write_barrier ();

	memset (hp_overflow, 0, sizeof (MonoThreadHazardPointers));

	mono_memory_write_barrier ();

	overflow_busy [small_id] = 0;
}

/**
 * mono_thread_hazardous_try_free:
 * \param p the pointer to free
 * \param free_func the function that can free the pointer
 *
 * If \p p is not a hazardous pointer it will be immediately freed by calling \p free_func.
 * Otherwise it will be queued for later.
 *
 * Use this function if \p free_func can ALWAYS be called in the context where this function is being called.
 *
 * This function doesn't pump the free queue so try to accommodate a call at an appropriate time.
 * See mono_thread_hazardous_try_free_some for when it's appropriate.
 *
 * \returns TRUE if \p p was free or FALSE if it was queued.
 */
gboolean
mono_thread_hazardous_try_free (gpointer p, MonoHazardousFreeFunc free_func)
{
	if (!is_pointer_hazardous (p)) {
		free_func (p);
		return TRUE;
	} else {
		mono_thread_hazardous_queue_free (p, free_func);
		return FALSE;
	}
}

/**
 * mono_thread_hazardous_queue_free:
 * \param p the pointer to free
 * \param free_func the function that can free the pointer
 * Queue \p p to be freed later. \p p will be freed once the hazard free queue is pumped.
 *
 * This function doesn't pump the free queue so try to accommodate a call at an appropriate time.
 * See \c mono_thread_hazardous_try_free_some for when it's appropriate.
 */
void
mono_thread_hazardous_queue_free (gpointer p, MonoHazardousFreeFunc free_func)
{
	DelayedFreeItem item = { p, free_func };

	mono_atomic_inc_i32 (&hazardous_pointer_count);

	mono_lock_free_array_queue_push (&delayed_free_queue, &item);

	guint32 queue_size = delayed_free_queue.num_used_entries;
	if (queue_size && queue_size_cb)
		queue_size_cb (queue_size);
}


void
mono_hazard_pointer_install_free_queue_size_callback (MonoHazardFreeQueueSizeCallback cb)
{
	queue_size_cb = cb;
}

static void
try_free_delayed_free_items (guint32 limit)
{
	GArray *hazardous = NULL;
	DelayedFreeItem item;
	guint32 freed = 0;

	// Free all the items we can and re-add the ones we can't to the queue.
	while (mono_lock_free_array_queue_pop (&delayed_free_queue, &item)) {
		if (is_pointer_hazardous (item.p)) {
			if (!hazardous)
				hazardous = g_array_sized_new (FALSE, FALSE, sizeof (DelayedFreeItem), delayed_free_queue.num_used_entries);

			g_array_append_val (hazardous, item);
			continue;
		}

		item.free_func (item.p);
		freed++;

		if (limit && freed == limit)
			break;
	}

	if (hazardous) {
		for (gint i = 0; i < hazardous->len; i++)
			mono_lock_free_array_queue_push (&delayed_free_queue, &g_array_index (hazardous, DelayedFreeItem, i));

		g_array_free (hazardous, TRUE);
	}
}

void
mono_thread_hazardous_try_free_all (void)
{
	try_free_delayed_free_items (0);
}

void
mono_thread_hazardous_try_free_some (void)
{
	try_free_delayed_free_items (10);
}

void
mono_thread_smr_init (void)
{
	int i;

	mono_os_mutex_init (&small_id_mutex);
	mono_counters_register ("Hazardous pointers", MONO_COUNTER_JIT | MONO_COUNTER_INT, &hazardous_pointer_count);

	for (i = 0; i < HAZARD_TABLE_OVERFLOW; ++i) {
		int small_id = mono_thread_small_id_alloc ();
		g_assert (small_id == i);
	}
}

void
mono_thread_smr_cleanup (void)
{
	mono_thread_hazardous_try_free_all ();

	mono_lock_free_array_queue_cleanup (&delayed_free_queue);

	/*FIXME, can't we release the small id table here?*/
}
