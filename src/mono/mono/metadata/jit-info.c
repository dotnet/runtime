/**
 * \file
 * MonoJitInfo functionality
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011-2012 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <sys/stat.h>

#include <mono/metadata/jit-info.h>
#include <mono/metadata/gc-internals.h>

#include <mono/utils/atomic.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/unlocked.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/coree.h>

static MonoJitInfoFindInAot jit_info_find_in_aot_func = NULL;
static MonoJitInfoTable * volatile jit_info_table;
static MonoJitInfoTable * volatile aot_modules;
static GSList *jit_info_free_queue;
static int num_jit_info_table_duplicates;
static mono_mutex_t jit_info_mutex;

#define JIT_INFO_TABLE_FILL_RATIO_NOM		3
#define JIT_INFO_TABLE_FILL_RATIO_DENOM		4
#define JIT_INFO_TABLE_FILLED_NUM_ELEMENTS	(MONO_JIT_INFO_TABLE_CHUNK_SIZE * JIT_INFO_TABLE_FILL_RATIO_NOM / JIT_INFO_TABLE_FILL_RATIO_DENOM)

#define JIT_INFO_TABLE_LOW_WATERMARK(n)		((n) / 2)
#define JIT_INFO_TABLE_HIGH_WATERMARK(n)	((n) * 5 / 6)

#define JIT_INFO_TOMBSTONE_MARKER	((MonoMethod*)NULL)
#define IS_JIT_INFO_TOMBSTONE(ji)	((ji)->d.method == JIT_INFO_TOMBSTONE_MARKER)

#define JIT_INFO_TABLE_HAZARD_INDEX		0
#define JIT_INFO_HAZARD_INDEX			1

static inline void
jit_info_lock (void)
{
	mono_os_mutex_lock (&jit_info_mutex);
}

static inline void
jit_info_unlock (void)
{
	mono_os_mutex_unlock (&jit_info_mutex);
}

static int
jit_info_table_num_elements (MonoJitInfoTable *table)
{
	return table->num_valid;
}

static MonoJitInfoTableChunk*
jit_info_table_new_chunk (void)
{
	MonoJitInfoTableChunk *chunk = g_new0 (MonoJitInfoTableChunk, 1);
	chunk->refcount = 1;

	return chunk;
}

void
mono_jit_info_tables_init (void)
{
	jit_info_table = mono_jit_info_table_new ();
	mono_os_mutex_init_recursive (&jit_info_mutex);
}

MonoJitInfoTable *
mono_jit_info_table_new (void)
{
	MonoJitInfoTable *table = (MonoJitInfoTable *)g_malloc0 (MONO_SIZEOF_JIT_INFO_TABLE + sizeof (MonoJitInfoTableChunk*));

	table->num_chunks = 1;
	table->chunks [0] = jit_info_table_new_chunk ();
	table->num_valid = 0;

	return table;
}

static void
jit_info_table_free (MonoJitInfoTable *table, gboolean duplicate)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int i;
	int num_chunks = table->num_chunks;

	jit_info_lock ();

	if (duplicate) {
		num_jit_info_table_duplicates--;
		if (!num_jit_info_table_duplicates) {
			GSList *list;

			for (list = jit_info_free_queue; list; list = list->next)
				g_free (list->data);

			g_slist_free (jit_info_free_queue);
			jit_info_free_queue = NULL;
		}
	}

	/* At this point we assume that there are no other threads
	   still accessing the table, so we don't have to worry about
	   hazardous pointers. */

	for (i = 0; i < num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = table->chunks [i];
		MonoJitInfo *tombstone;

		if (--chunk->refcount > 0)
			continue;

		for (tombstone = chunk->next_tombstone; tombstone; ) {
			MonoJitInfo *next = tombstone->n.next_tombstone;
			g_free (tombstone);
			tombstone = next;
		}

		g_free (chunk);
	}

	jit_info_unlock ();

	g_free (table);
}

static void
jit_info_table_free_duplicate (MonoJitInfoTable *table)
{
	jit_info_table_free (table, TRUE);
}

void
mono_jit_info_table_free (MonoJitInfoTable *table)
{
	jit_info_table_free (table, FALSE);
}

/* The jit_info_table is sorted in ascending order by the end
 * addresses of the compiled methods.  The reason why we have to do
 * this is that once we introduce tombstones, it becomes possible for
 * code ranges to overlap, and if we sort by code start and insert at
 * the back of the table, we cannot guarantee that we won't overlook
 * an entry.
 *
 * There are actually two possible ways to do the sorting and
 * inserting which work with our lock-free mechanism:
 *
 * 1. Sort by start address and insert at the front.  When looking for
 * an entry, find the last one with a start address lower than the one
 * you're looking for, then work your way to the front of the table.
 *
 * 2. Sort by end address and insert at the back.  When looking for an
 * entry, find the first one with an end address higher than the one
 * you're looking for, then work your way to the end of the table.
 *
 * We chose the latter out of convenience.
 */
static int
jit_info_table_index (MonoJitInfoTable *table, gint8 *addr)
{
	int left = 0, right = table->num_chunks;

	g_assert (left < right);

	do {
		int pos = (left + right) / 2;
		MonoJitInfoTableChunk *chunk = table->chunks [pos];

		if (addr < chunk->last_code_end)
			right = pos;
		else
			left = pos + 1;
	} while (left < right);
	g_assert (left == right);

	if (left >= table->num_chunks)
		return table->num_chunks - 1;
	return left;
}

static int
jit_info_table_chunk_index (MonoJitInfoTableChunk *chunk, MonoThreadHazardPointers *hp, gint8 *addr)
{
	int left = 0, right = chunk->num_elements;

	while (left < right) {
		int pos = (left + right) / 2;
		MonoJitInfo *ji = (MonoJitInfo *)mono_get_hazardous_pointer((gpointer volatile*)&chunk->data [pos], hp, JIT_INFO_HAZARD_INDEX);
		gint8 *code_end = (gint8*)ji->code_start + ji->code_size;

		if (addr < code_end)
			right = pos;
		else
			left = pos + 1;
	}
	g_assert (left == right);

	return left;
}

static MonoJitInfo*
jit_info_table_find (MonoJitInfoTable *table, MonoThreadHazardPointers *hp, gint8 *addr)
{
	MonoJitInfo *ji;
	int chunk_pos, pos;

	chunk_pos = jit_info_table_index (table, (gint8*)addr);
	g_assert (chunk_pos < table->num_chunks);

	pos = jit_info_table_chunk_index (table->chunks [chunk_pos], hp, (gint8*)addr);

	/* We now have a position that's very close to that of the
	   first element whose end address is higher than the one
	   we're looking for.  If we don't have the exact position,
	   then we have a position below that one, so we'll just
	   search upward until we find our element. */
	do {
		MonoJitInfoTableChunk *chunk = table->chunks [chunk_pos];

		while (pos < chunk->num_elements) {
			ji = (MonoJitInfo *)mono_get_hazardous_pointer ((gpointer volatile*)&chunk->data [pos], hp, JIT_INFO_HAZARD_INDEX);

			++pos;

			if (IS_JIT_INFO_TOMBSTONE (ji)) {
				mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);
				continue;
			}
			if ((gint8*)addr >= (gint8*)ji->code_start
					&& (gint8*)addr < (gint8*)ji->code_start + ji->code_size) {
				mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);
				return ji;
			}

			/* If we find a non-tombstone element which is already
			   beyond what we're looking for, we have to end the
			   search. */
			if ((gint8*)addr < (gint8*)ji->code_start)
				goto not_found;
		}

		++chunk_pos;
		pos = 0;
	} while (chunk_pos < table->num_chunks);

 not_found:
	if (hp)
		mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);
	return NULL;
}

/*
 * mono_jit_info_table_find_internal:
 *
 * If TRY_AOT is FALSE, avoid loading information for missing methods from AOT images, which is currently not async safe.
 * In this case, only those AOT methods will be found whose jit info is already loaded.
 * If ALLOW_TRAMPOLINES is TRUE, this can return a MonoJitInfo which represents a trampoline (ji->is_trampoline is true).
 * ASYNC SAFETY: When called in an async context (mono_thread_info_is_async_context ()), this is async safe.
 * In this case, the returned MonoJitInfo might not have metadata information, in particular,
 * mono_jit_info_get_method () could fail.
 */
MonoJitInfo*
mono_jit_info_table_find_internal (gpointer addr, gboolean try_aot, gboolean allow_trampolines)
{
	MonoJitInfoTable *table;
	MonoJitInfo *ji, *module_ji;
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	UnlockedIncrement (&mono_stats.jit_info_table_lookup_count);

	/* First we have to get the domain's jit_info_table.  This is
	   complicated by the fact that a writer might substitute a
	   new table and free the old one.  What the writer guarantees
	   us is that it looks at the hazard pointers after it has
	   changed the jit_info_table pointer.  So, if we guard the
	   table by a hazard pointer and make sure that the pointer is
	   still there after we've made it hazardous, we don't have to
	   worry about the writer freeing the table. */
	table = (MonoJitInfoTable *)mono_get_hazardous_pointer ((gpointer volatile*)&jit_info_table, hp, JIT_INFO_TABLE_HAZARD_INDEX);

	ji = jit_info_table_find (table, hp, (gint8*)addr);
	if (hp)
		mono_hazard_pointer_clear (hp, JIT_INFO_TABLE_HAZARD_INDEX);
	if (ji && ji->is_trampoline && !allow_trampolines)
		return NULL;
	if (ji)
		return ji;

	/* Maybe its an AOT module */
	if (try_aot && mono_get_root_domain () && aot_modules) {
		table = (MonoJitInfoTable *)mono_get_hazardous_pointer ((gpointer volatile*)&aot_modules, hp, JIT_INFO_TABLE_HAZARD_INDEX);
		module_ji = jit_info_table_find (table, hp, (gint8*)addr);
		if (module_ji)
			ji = jit_info_find_in_aot_func (module_ji->d.image, addr);
		if (hp)
			mono_hazard_pointer_clear (hp, JIT_INFO_TABLE_HAZARD_INDEX);
	}

	if (ji && ji->is_trampoline && !allow_trampolines)
		return NULL;
	
	return ji;
}

void
mono_jit_info_table_foreach_internal (MonoJitInfoFunc func, gpointer user_data)
{
	MonoJitInfoTable *table;
	MonoJitInfo *ji;
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	table = (MonoJitInfoTable *)mono_get_hazardous_pointer ((gpointer volatile*)&jit_info_table, hp, JIT_INFO_TABLE_HAZARD_INDEX);
	if (table) {
		for (int chunk_index = 0; chunk_index < table->num_chunks; ++chunk_index) {
			MonoJitInfoTableChunk *chunk = table->chunks [chunk_index];
			for (int jit_info_index = 0; jit_info_index < chunk->num_elements; ++jit_info_index) {

				ji = (MonoJitInfo *)mono_get_hazardous_pointer ((gpointer volatile*)&chunk->data [jit_info_index], hp, JIT_INFO_HAZARD_INDEX);

				if (func && !IS_JIT_INFO_TOMBSTONE (ji))
					func (ji, user_data);

				mono_hazard_pointer_clear (hp, JIT_INFO_HAZARD_INDEX);
			}
		}
	}

	if (hp)
		mono_hazard_pointer_clear (hp, JIT_INFO_TABLE_HAZARD_INDEX);
}

static G_GNUC_UNUSED void
jit_info_table_check (MonoJitInfoTable *table)
{
	int i;

	for (i = 0; i < table->num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = table->chunks [i];
		int j;

		g_assert (chunk->refcount > 0 /* && chunk->refcount <= 8 */);
		if (chunk->refcount > 10)
			printf("warning: chunk refcount is %d\n", chunk->refcount);
		g_assert (chunk->num_elements <= MONO_JIT_INFO_TABLE_CHUNK_SIZE);

		for (j = 0; j < chunk->num_elements; ++j) {
			MonoJitInfo *this_ji = chunk->data [j];
			MonoJitInfo *next;

			g_assert ((gint8*)this_ji->code_start + this_ji->code_size <= chunk->last_code_end);

			if (j < chunk->num_elements - 1)
				next = chunk->data [j + 1];
			else if (i < table->num_chunks - 1) {
				int k;

				for (k = i + 1; k < table->num_chunks; ++k)
					if (table->chunks [k]->num_elements > 0)
						break;

				if (k >= table->num_chunks)
					return;

				g_assert (table->chunks [k]->num_elements > 0);
				next = table->chunks [k]->data [0];
			} else
				return;

			g_assert ((gint8*)this_ji->code_start + this_ji->code_size <= (gint8*)next->code_start + next->code_size);
		}
	}
}

static MonoJitInfoTable*
jit_info_table_realloc (MonoJitInfoTable *old)
{
	int i;
	int num_elements = jit_info_table_num_elements (old);
	int required_size;
	int num_chunks;
	int new_chunk, new_element;
	MonoJitInfoTable *result;

	/* number of needed places for elements needed */
	required_size = (int)((long)num_elements * JIT_INFO_TABLE_FILL_RATIO_DENOM / JIT_INFO_TABLE_FILL_RATIO_NOM);
	num_chunks = (required_size + MONO_JIT_INFO_TABLE_CHUNK_SIZE - 1) / MONO_JIT_INFO_TABLE_CHUNK_SIZE;
	if (num_chunks == 0) {
		g_assert (num_elements == 0);
		return mono_jit_info_table_new ();
	}
	g_assert (num_chunks > 0);

	result = (MonoJitInfoTable *)g_malloc (MONO_SIZEOF_JIT_INFO_TABLE + sizeof (MonoJitInfoTableChunk*) * num_chunks);
	result->num_chunks = num_chunks;
	result->num_valid = old->num_valid;

	for (i = 0; i < num_chunks; ++i)
		result->chunks [i] = jit_info_table_new_chunk ();

	new_chunk = 0;
	new_element = 0;
	for (i = 0; i < old->num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = old->chunks [i];
		int chunk_num_elements = chunk->num_elements;
		int j;

		for (j = 0; j < chunk_num_elements; ++j) {
			if (!IS_JIT_INFO_TOMBSTONE (chunk->data [j])) {
				g_assert (new_chunk < num_chunks);
				result->chunks [new_chunk]->data [new_element] = chunk->data [j];
				if (++new_element >= JIT_INFO_TABLE_FILLED_NUM_ELEMENTS) {
					result->chunks [new_chunk]->num_elements = new_element;
					++new_chunk;
					new_element = 0;
				}
			}
		}
	}

	if (new_chunk < num_chunks) {
		g_assert (new_chunk == num_chunks - 1);
		result->chunks [new_chunk]->num_elements = new_element;
		g_assert (result->chunks [new_chunk]->num_elements > 0);
	}

	for (i = 0; i < num_chunks; ++i) {
		MonoJitInfoTableChunk *chunk = result->chunks [i];
		MonoJitInfo *ji = chunk->data [chunk->num_elements - 1];

		result->chunks [i]->last_code_end = (gint8*)ji->code_start + ji->code_size;
	}

	return result;
}

static void
jit_info_table_split_chunk (MonoJitInfoTableChunk *chunk, MonoJitInfoTableChunk **new1p, MonoJitInfoTableChunk **new2p)
{
	MonoJitInfoTableChunk *new1 = jit_info_table_new_chunk ();
	MonoJitInfoTableChunk *new2 = jit_info_table_new_chunk ();

	g_assert (chunk->num_elements == MONO_JIT_INFO_TABLE_CHUNK_SIZE);

	new1->num_elements = MONO_JIT_INFO_TABLE_CHUNK_SIZE / 2;
	new2->num_elements = MONO_JIT_INFO_TABLE_CHUNK_SIZE - new1->num_elements;

	memcpy ((void*)new1->data, (void*)chunk->data, sizeof (MonoJitInfo*) * new1->num_elements);
	memcpy ((void*)new2->data, (void*)(chunk->data + new1->num_elements), sizeof (MonoJitInfo*) * new2->num_elements);

	new1->last_code_end = (gint8*)new1->data [new1->num_elements - 1]->code_start
		+ new1->data [new1->num_elements - 1]->code_size;
	new2->last_code_end = (gint8*)new2->data [new2->num_elements - 1]->code_start
		+ new2->data [new2->num_elements - 1]->code_size;

	*new1p = new1;
	*new2p = new2;
}

static MonoJitInfoTable*
jit_info_table_copy_and_split_chunk (MonoJitInfoTable *table, MonoJitInfoTableChunk *chunk)
{
	MonoJitInfoTable *new_table = (MonoJitInfoTable *)g_malloc (MONO_SIZEOF_JIT_INFO_TABLE
		+ sizeof (MonoJitInfoTableChunk*) * (table->num_chunks + 1));
	int i, j;

	new_table->num_chunks = table->num_chunks + 1;
	new_table->num_valid = table->num_valid;

	j = 0;
	for (i = 0; i < table->num_chunks; ++i) {
		if (table->chunks [i] == chunk) {
			jit_info_table_split_chunk (chunk, &new_table->chunks [j], &new_table->chunks [j + 1]);
			j += 2;
		} else {
			new_table->chunks [j] = table->chunks [i];
			++new_table->chunks [j]->refcount;
			++j;
		}
	}

	g_assert (j == new_table->num_chunks);

	return new_table;
}

static MonoJitInfoTableChunk*
jit_info_table_purify_chunk (MonoJitInfoTableChunk *old)
{
	MonoJitInfoTableChunk *result = jit_info_table_new_chunk ();
	int i, j;

	j = 0;
	for (i = 0; i < old->num_elements; ++i) {
		if (!IS_JIT_INFO_TOMBSTONE (old->data [i]))
			result->data [j++] = old->data [i];
	}

	result->num_elements = j;
	if (result->num_elements > 0)
		result->last_code_end = (gint8*)result->data [j - 1]->code_start + result->data [j - 1]->code_size;
	else
		result->last_code_end = old->last_code_end;

	return result;
}

static MonoJitInfoTable*
jit_info_table_copy_and_purify_chunk (MonoJitInfoTable *table, MonoJitInfoTableChunk *chunk)
{
	MonoJitInfoTable *new_table = (MonoJitInfoTable *)g_malloc (MONO_SIZEOF_JIT_INFO_TABLE
		+ sizeof (MonoJitInfoTableChunk*) * table->num_chunks);
	int i, j;

	new_table->num_chunks = table->num_chunks;
	new_table->num_valid = table->num_valid;

	j = 0;
	for (i = 0; i < table->num_chunks; ++i) {
		if (table->chunks [i] == chunk)
			new_table->chunks [j++] = jit_info_table_purify_chunk (table->chunks [i]);
		else {
			new_table->chunks [j] = table->chunks [i];
			++new_table->chunks [j]->refcount;
			++j;
		}
	}

	g_assert (j == new_table->num_chunks);

	return new_table;
}

/* As we add an element to the table the case can arise that the chunk
 * to which we need to add is already full.  In that case we have to
 * allocate a new table and do something about that chunk.  We have
 * several strategies:
 *
 * If the number of elements in the table is below the low watermark
 * or above the high watermark, we reallocate the whole table.
 * Otherwise we only concern ourselves with the overflowing chunk:
 *
 * If there are no tombstones in the chunk then we split the chunk in
 * two, each half full.
 *
 * If the chunk does contain tombstones, we just make a new copy of
 * the chunk without the tombstones, which will have room for at least
 * the one element we have to add.
 */
static MonoJitInfoTable*
jit_info_table_chunk_overflow (MonoJitInfoTable *table, MonoJitInfoTableChunk *chunk)
{
	int num_elements = jit_info_table_num_elements (table);
	int i;

	if (num_elements < JIT_INFO_TABLE_LOW_WATERMARK (table->num_chunks * MONO_JIT_INFO_TABLE_CHUNK_SIZE)
			|| num_elements > JIT_INFO_TABLE_HIGH_WATERMARK (table->num_chunks * MONO_JIT_INFO_TABLE_CHUNK_SIZE)) {
		//printf ("reallocing table\n");
		return jit_info_table_realloc (table);
	}

	/* count the number of non-tombstone elements in the chunk */
	num_elements = 0;
	for (i = 0; i < chunk->num_elements; ++i) {
		if (!IS_JIT_INFO_TOMBSTONE (chunk->data [i]))
			++num_elements;
	}

	if (num_elements == MONO_JIT_INFO_TABLE_CHUNK_SIZE) {
		//printf ("splitting chunk\n");
		return jit_info_table_copy_and_split_chunk (table, chunk);
	}

	//printf ("purifying chunk\n");
	return jit_info_table_copy_and_purify_chunk (table, chunk);
}

/* We add elements to the table by first making space for them by
 * shifting the elements at the back to the right, one at a time.
 * This results in duplicate entries during the process, but during
 * all the time the table is in a sorted state.  Also, when an element
 * is replaced by another one, the element that replaces it has an end
 * address that is equal to or lower than that of the replaced
 * element.  That property is necessary to guarantee that when
 * searching for an element we end up at a position not higher than
 * the one we're looking for (i.e. we either find the element directly
 * or we end up to the left of it).
 */
static void
jit_info_table_add (MonoJitInfoTable *volatile *table_ptr, MonoJitInfo *ji)
{
	MonoJitInfoTable *table;
	MonoJitInfoTableChunk *chunk;
	int chunk_pos, pos;
	int num_elements;
	int i;

	table = *table_ptr;

 restart:
	chunk_pos = jit_info_table_index (table, (gint8*)ji->code_start + ji->code_size);
	g_assert (chunk_pos < table->num_chunks);
	chunk = table->chunks [chunk_pos];

	if (chunk->num_elements >= MONO_JIT_INFO_TABLE_CHUNK_SIZE) {
		MonoJitInfoTable *new_table = jit_info_table_chunk_overflow (table, chunk);

		/* Debugging code, should be removed. */
		//jit_info_table_check (new_table);

		*table_ptr = new_table;
		mono_memory_barrier ();
		num_jit_info_table_duplicates++;
		mono_thread_hazardous_try_free (table, (MonoHazardousFreeFunc)jit_info_table_free_duplicate);
		table = new_table;

		goto restart;
	}

	/* Debugging code, should be removed. */
	//jit_info_table_check (table);

	num_elements = chunk->num_elements;

	pos = jit_info_table_chunk_index (chunk, NULL, (gint8*)ji->code_start + ji->code_size);

	/* First we need to size up the chunk by one, by copying the
	   last item, or inserting the first one, if the table is
	   empty. */
	if (num_elements > 0)
		chunk->data [num_elements] = chunk->data [num_elements - 1];
	else
		chunk->data [0] = ji;
	mono_memory_write_barrier ();
	chunk->num_elements = ++num_elements;

	/* Shift the elements up one by one. */
	for (i = num_elements - 2; i >= pos; --i) {
		mono_memory_write_barrier ();
		chunk->data [i + 1] = chunk->data [i];
	}

	/* Now we have room and can insert the new item. */
	mono_memory_write_barrier ();
	chunk->data [pos] = ji;

	/* Set the high code end address chunk entry. */
	chunk->last_code_end = (gint8*)chunk->data [chunk->num_elements - 1]->code_start
		+ chunk->data [chunk->num_elements - 1]->code_size;

	++table->num_valid;

	/* Debugging code, should be removed. */
	//jit_info_table_check (table);
}

void
mono_jit_info_table_add (MonoJitInfo *ji)
{
	g_assert (ji->d.method != NULL);

	jit_info_lock ();

	UnlockedIncrement (&mono_stats.jit_info_table_insert_count);

	jit_info_table_add (&jit_info_table, ji);

	jit_info_unlock ();
}

static MonoJitInfo*
mono_jit_info_make_tombstone (MonoJitInfoTableChunk *chunk, MonoJitInfo *ji)
{
	MonoJitInfo *tombstone = g_new0 (MonoJitInfo, 1);

	tombstone->code_start = ji->code_start;
	tombstone->code_size = ji->code_size;
	tombstone->d.method = JIT_INFO_TOMBSTONE_MARKER;
	tombstone->n.next_tombstone = chunk->next_tombstone;
	chunk->next_tombstone = tombstone;

	return tombstone;
}

/*
 * LOCKING: domain lock
 */
static void
mono_jit_info_free_or_queue (MonoJitInfo *ji)
{
	/*
	 * When we run out of space in a jit info table and we reallocate it, a
	 * ji structure can be temporary present in multiple tables. If the ji
	 * structure is freed while another thread is doing a jit lookup and still
	 * accessing the old table, it might be accessing this jit info (which
	 * would have been removed and freed only from the new table). The hazard
	 * pointer doesn't stop this since the jinfo would have been freed before
	 * we get to set the hazard pointer for ji. Delay the free-ing for when
	 * there are no jit info table duplicates.
	 */
	if (!num_jit_info_table_duplicates)
		mono_thread_hazardous_try_free (ji, g_free);
	else
		jit_info_free_queue = g_slist_prepend (jit_info_free_queue, ji);
}

static void
jit_info_table_remove (MonoJitInfoTable *table, MonoJitInfo *ji)
{
	MonoJitInfoTableChunk *chunk;
	gpointer start = ji->code_start;
	int chunk_pos, pos;

	chunk_pos = jit_info_table_index (table, (gint8 *)start);
	g_assert (chunk_pos < table->num_chunks);

	pos = jit_info_table_chunk_index (table->chunks [chunk_pos], NULL, (gint8 *)start);

	do {
		chunk = table->chunks [chunk_pos];

		while (pos < chunk->num_elements) {
			if (chunk->data [pos] == ji)
				goto found;

			g_assert (IS_JIT_INFO_TOMBSTONE (chunk->data [pos]));
			g_assert ((guint8*)chunk->data [pos]->code_start + chunk->data [pos]->code_size
				<= (guint8*)ji->code_start + ji->code_size);

			++pos;
		}

		++chunk_pos;
		pos = 0;
	} while (chunk_pos < table->num_chunks);

 found:
	g_assert (chunk->data [pos] == ji);

	chunk->data [pos] = mono_jit_info_make_tombstone (chunk, ji);
	--table->num_valid;

	/* Debugging code, should be removed. */
	//jit_info_table_check (table);
}

void
mono_jit_info_table_remove (MonoJitInfo *ji)
{
	MonoJitInfoTable *table;

	jit_info_lock ();

	table = jit_info_table;

	UnlockedIncrement (&mono_stats.jit_info_table_remove_count);

	jit_info_table_remove (table, ji);

	mono_jit_info_free_or_queue (ji);

	jit_info_unlock ();
}

/*
 * mono_jit_info_add_aot_module:
 *
 *   Associate the AOT code range [start, end) with IMAGE in the JIT info table.
 */
void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end)
{
	MonoJitInfo *ji;

	jit_info_lock ();

	/*
	 * We reuse MonoJitInfoTable to store AOT module info,
	 * this gives us async-safe lookup.
	 */
	if (!aot_modules)
		aot_modules = mono_jit_info_table_new ();

	ji = g_new0 (MonoJitInfo, 1);
	ji->d.image = image;
	ji->code_start = start;
	ji->code_size = (guint8*)end - (guint8*)start;
	jit_info_table_add (&aot_modules, ji);

	jit_info_unlock ();
}

void
mono_install_jit_info_find_in_aot (MonoJitInfoFindInAot func)
{
	jit_info_find_in_aot_func = func;
}

int
mono_jit_info_size (MonoJitInfoFlags flags, int num_clauses, int num_holes)
{
	int size = MONO_SIZEOF_JIT_INFO;

	size += num_clauses * sizeof (MonoJitExceptionInfo);
	if (flags & JIT_INFO_HAS_GENERIC_JIT_INFO)
		size += sizeof (MonoGenericJitInfo);
	if (flags & JIT_INFO_HAS_TRY_BLOCK_HOLES)
		size += sizeof (MonoTryBlockHoleTableJitInfo) + num_holes * sizeof (MonoTryBlockHoleJitInfo);
	if (flags & JIT_INFO_HAS_ARCH_EH_INFO)
		size += sizeof (MonoArchEHJitInfo);
	if (flags & JIT_INFO_HAS_THUNK_INFO)
		size += sizeof (MonoThunkJitInfo);
	if (flags & JIT_INFO_HAS_UNWIND_INFO)
		size += sizeof (MonoUnwindJitInfo);
	return size;
}

void
mono_jit_info_init (MonoJitInfo *ji, MonoMethod *method, guint8 *code, int code_size,
					MonoJitInfoFlags flags, int num_clauses, int num_holes)
{
	ji->d.method = method;
	ji->code_start = code;
	ji->code_size = code_size;
	ji->num_clauses = num_clauses;
	if (flags & JIT_INFO_HAS_GENERIC_JIT_INFO)
		ji->has_generic_jit_info = 1;
	if (flags & JIT_INFO_HAS_TRY_BLOCK_HOLES)
		ji->has_try_block_holes = 1;
	if (flags & JIT_INFO_HAS_ARCH_EH_INFO)
		ji->has_arch_eh_info = 1;
	if (flags & JIT_INFO_HAS_THUNK_INFO)
		ji->has_thunk_info = 1;
	if (flags & JIT_INFO_HAS_UNWIND_INFO)
		ji->has_unwind_info = 1;
}

/**
 * mono_jit_info_get_code_start:
 * \param ji the JIT information handle
 *
 * Use this function to get the starting address for the method described by
 * the \p ji object.  You can use this plus the \c mono_jit_info_get_code_size
 * to determine the start and end of the native code.
 *
 * \returns Starting address with the native code.
 */
gpointer
mono_jit_info_get_code_start (MonoJitInfo* ji)
{
	return ji->code_start;
}

/**
 * mono_jit_info_get_code_size:
 * \param ji the JIT information handle
 *
 * Use this function to get the code size for the method described by
 * the \p ji object. You can use this plus the \c mono_jit_info_get_code_start
 * to determine the start and end of the native code.
 *
 * \returns Starting address with the native code.
 */
int
mono_jit_info_get_code_size (MonoJitInfo* ji)
{
	return ji->code_size;
}

/**
 * mono_jit_info_get_method:
 * \param ji the JIT information handle
 *
 * Use this function to get the \c MonoMethod* that backs
 * the \p ji object.
 *
 * \returns The \c MonoMethod that represents the code tracked
 * by \p ji.
 */
MonoMethod*
mono_jit_info_get_method (MonoJitInfo* ji)
{
	g_assert (!ji->async);
	g_assert (!ji->is_trampoline);
	return ji->d.method;
}

static gpointer
jit_info_key_extract (gpointer value)
{
	MonoJitInfo *info = (MonoJitInfo*)value;

	return info->d.method;
}

static gpointer*
jit_info_next_value (gpointer value)
{
	MonoJitInfo *info = (MonoJitInfo*)value;

	return (gpointer*)&info->n.next_jit_code_hash;
}

void
mono_jit_code_hash_init (MonoInternalHashTable *jit_code_hash)
{
	mono_internal_hash_table_init (jit_code_hash,
				       mono_aligned_addr_hash,
				       jit_info_key_extract,
				       jit_info_next_value);
}

MonoGenericJitInfo*
mono_jit_info_get_generic_jit_info (MonoJitInfo *ji)
{
	if (ji->has_generic_jit_info)
		return (MonoGenericJitInfo*)&ji->clauses [ji->num_clauses];
	else
		return NULL;
}

/*
 * mono_jit_info_get_generic_sharing_context:
 * @ji: a jit info
 *
 * Returns the jit info's generic sharing context, or NULL if it
 * doesn't have one.
 */
MonoGenericSharingContext*
mono_jit_info_get_generic_sharing_context (MonoJitInfo *ji)
{
	MonoGenericJitInfo *gi = mono_jit_info_get_generic_jit_info (ji);

	if (gi)
		return gi->generic_sharing_context;
	else
		return NULL;
}

/*
 * mono_jit_info_set_generic_sharing_context:
 * @ji: a jit info
 * @gsctx: a generic sharing context
 *
 * Sets the jit info's generic sharing context.  The jit info must
 * have memory allocated for the context.
 */
void
mono_jit_info_set_generic_sharing_context (MonoJitInfo *ji, MonoGenericSharingContext *gsctx)
{
	MonoGenericJitInfo *gi = mono_jit_info_get_generic_jit_info (ji);

	g_assert (gi);

	gi->generic_sharing_context = gsctx;
}

MonoTryBlockHoleTableJitInfo*
mono_jit_info_get_try_block_hole_table_info (MonoJitInfo *ji)
{
	if (ji->has_try_block_holes) {
		char *ptr = (char*)&ji->clauses [ji->num_clauses];
		if (ji->has_generic_jit_info)
			ptr += sizeof (MonoGenericJitInfo);
		return (MonoTryBlockHoleTableJitInfo*)ptr;
	} else {
		return NULL;
	}
}

static int
try_block_hole_table_size (MonoJitInfo *ji)
{
	MonoTryBlockHoleTableJitInfo *table;

	table = mono_jit_info_get_try_block_hole_table_info (ji);
	g_assert (table);
	return sizeof (MonoTryBlockHoleTableJitInfo) + table->num_holes * sizeof (MonoTryBlockHoleJitInfo);
}

MonoArchEHJitInfo*
mono_jit_info_get_arch_eh_info (MonoJitInfo *ji)
{
	if (ji->has_arch_eh_info) {
		char *ptr = (char*)&ji->clauses [ji->num_clauses];
		if (ji->has_generic_jit_info)
			ptr += sizeof (MonoGenericJitInfo);
		if (ji->has_try_block_holes)
			ptr += try_block_hole_table_size (ji);
		return (MonoArchEHJitInfo*)ptr;
	} else {
		return NULL;
	}
}

MonoThunkJitInfo*
mono_jit_info_get_thunk_info (MonoJitInfo *ji)
{
	if (ji->has_thunk_info) {
		char *ptr = (char*)&ji->clauses [ji->num_clauses];
		if (ji->has_generic_jit_info)
			ptr += sizeof (MonoGenericJitInfo);
		if (ji->has_try_block_holes)
			ptr += try_block_hole_table_size (ji);
		if (ji->has_arch_eh_info)
			ptr += sizeof (MonoArchEHJitInfo);
		return (MonoThunkJitInfo*)ptr;
	} else {
		return NULL;
	}
}

MonoUnwindJitInfo*
mono_jit_info_get_unwind_info (MonoJitInfo *ji)
{
	if (ji->has_unwind_info) {
		char *ptr = (char*)&ji->clauses [ji->num_clauses];
		if (ji->has_generic_jit_info)
			ptr += sizeof (MonoGenericJitInfo);
		if (ji->has_try_block_holes)
			ptr += try_block_hole_table_size (ji);
		if (ji->has_arch_eh_info)
			ptr += sizeof (MonoArchEHJitInfo);
		if (ji->has_thunk_info)
			ptr += sizeof (MonoThunkJitInfo);
		return (MonoUnwindJitInfo*)ptr;
	} else {
		return NULL;
	}
}
