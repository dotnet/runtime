/*
 * hazard-pointer.c: Hazard pointer related code.
 *
 * (C) Copyright 2011 Novell, Inc
 */

#include <config.h>

#include <mono/metadata/object-internals.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/monobitset.h>
#include <mono/io-layer/io-layer.h>

typedef struct {
	gpointer p;
	MonoHazardousFreeFunc free_func;
} DelayedFreeItem;

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
static CRITICAL_SECTION delayed_free_table_mutex;
static GArray *delayed_free_table = NULL;

/* The table for small ID assignment */
static CRITICAL_SECTION small_id_mutex;
static int small_id_next;
static int highest_small_id = -1;
static MonoBitSet *small_id_table;

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

//	if (small_id_next >= small_id_table->size)
//		small_id_next = 0;

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
	MonoInternalThread *current_thread = mono_thread_internal_current ();

	if (!(current_thread && current_thread->small_id >= 0)) {
		static MonoThreadHazardPointers emerg_hazard_table;
		g_warning ("Thread %p may have been prematurely finalized", current_thread);
		return &emerg_hazard_table;
	}

	return &hazard_table [current_thread->small_id];
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

static void
try_free_delayed_free_item (int index)
{
	if (delayed_free_table->len > index) {
		DelayedFreeItem item = { NULL, NULL };

		EnterCriticalSection (&delayed_free_table_mutex);
		/* We have to check the length again because another
		   thread might have freed an item before we acquired
		   the lock. */
		if (delayed_free_table->len > index) {
			item = g_array_index (delayed_free_table, DelayedFreeItem, index);

			if (!is_pointer_hazardous (item.p))
				g_array_remove_index_fast (delayed_free_table, index);
			else
				item.p = NULL;
		}
		LeaveCriticalSection (&delayed_free_table_mutex);

		if (item.p != NULL)
			item.free_func (item.p);
	}
}

void
mono_thread_hazardous_free_or_queue (gpointer p, MonoHazardousFreeFunc free_func)
{
	int i;

	/* First try to free a few entries in the delayed free
	   table. */
	for (i = 2; i >= 0; --i)
		try_free_delayed_free_item (i);

	/* Now see if the pointer we're freeing is hazardous.  If it
	   isn't, free it.  Otherwise put it in the delay list. */
	if (is_pointer_hazardous (p)) {
		DelayedFreeItem item = { p, free_func };

		++mono_stats.hazardous_pointer_count;

		EnterCriticalSection (&delayed_free_table_mutex);
		g_array_append_val (delayed_free_table, item);
		LeaveCriticalSection (&delayed_free_table_mutex);
	} else
		free_func (p);
}

void
mono_thread_hazardous_try_free_all (void)
{
	int len;
	int i;

	if (!delayed_free_table)
		return;

	len = delayed_free_table->len;

	for (i = len - 1; i >= 0; --i)
		try_free_delayed_free_item (i);
}

void
mono_thread_smr_init (void)
{
	InitializeCriticalSection(&delayed_free_table_mutex);
	InitializeCriticalSection(&small_id_mutex);	

	delayed_free_table = g_array_new (FALSE, FALSE, sizeof (DelayedFreeItem));
}

void
mono_thread_smr_cleanup (void)
{
	mono_thread_hazardous_try_free_all ();

	g_array_free (delayed_free_table, TRUE);
	delayed_free_table = NULL;
	/*FIXME, can't we release the small id table here?*/
}
