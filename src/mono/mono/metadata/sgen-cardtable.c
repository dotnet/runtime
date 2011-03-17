/*
 * sgen-cardtable.c: Card table implementation for sgen
 *
 * Author:
 * 	Rodrigo Kumpera (rkumpera@novell.com)
 *
 * SGen is licensed under the terms of the MIT X11 license
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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

#ifdef SGEN_HAVE_CARDTABLE

//#define CARDTABLE_STATS

#include <unistd.h>
#include <sys/mman.h>
#include <sys/types.h>

guint8 *sgen_cardtable;


#ifdef HEAVY_STATISTICS
long long marked_cards;
long long scanned_cards;
long long scanned_objects;

static long long los_marked_cards;
static long long large_objects;
static long long bloby_objects;
static long long los_array_cards;
static long long los_array_remsets;

#endif
static long long major_card_scan_time;
static long long los_card_scan_time;

static long long last_major_scan_time;
static long long last_los_scan_time;
/*WARNING: This function returns the number of cards regardless of overflow in case of overlapping cards.*/
static mword
cards_in_range (mword address, mword size)
{
	mword end = address + MAX (1, size) - 1;
	return (end >> CARD_BITS) - (address >> CARD_BITS) + 1;
}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS

guint8 *sgen_shadow_cardtable;

#define SGEN_SHADOW_CARDTABLE_END (sgen_shadow_cardtable + CARD_COUNT_IN_BYTES)
#define SGEN_CARDTABLE_END (sgen_cardtable + CARD_COUNT_IN_BYTES)

static gboolean
sgen_card_table_region_begin_scanning (mword start, mword end)
{
	/*XXX this can be improved to work on words and have a single loop induction var */
	while (start <= end) {
		if (sgen_card_table_card_begin_scanning (start))
			return TRUE;
		start += CARD_SIZE_IN_BYTES;
	}
	return FALSE;
}

#else

static gboolean
sgen_card_table_region_begin_scanning (mword start, mword size)
{
	gboolean res = FALSE;
	guint8 *card = sgen_card_table_get_card_address (start);
	guint8 *end = card + cards_in_range (start, size);

	/*XXX this can be improved to work on words and have a branchless body */
	while (card != end) {
		if (*card++) {
			res = TRUE;
			break;
		}
	}

	memset (sgen_card_table_get_card_address (start), 0, size >> CARD_BITS);

	return res;
}

#endif

/*FIXME this assumes that major blocks are multiple of 4K which is pretty reasonable */
gboolean
sgen_card_table_get_card_data (guint8 *data_dest, mword address, mword cards)
{
	mword *start = (mword*)sgen_card_table_get_card_scan_address (address);
	mword *dest = (mword*)data_dest;
	mword *end = (mword*)(data_dest + cards);
	mword mask = 0;

	for (; dest < end; ++dest, ++start) {
		mword v = *start;
		*dest = v;
		mask |= v;

#ifndef SGEN_HAVE_OVERLAPPING_CARDS
		*start = 0;
#endif
	}

	return mask;
}

static gboolean
sgen_card_table_address_is_marked (mword address)
{
	return *sgen_card_table_get_card_address (address) != 0;
}

void
sgen_card_table_mark_address (mword address)
{
	*sgen_card_table_get_card_address (address) = 1;
}

void*
sgen_card_table_align_pointer (void *ptr)
{
	return (void*)((mword)ptr & ~(CARD_SIZE_IN_BYTES - 1));
}

void
sgen_card_table_mark_range (mword address, mword size)
{
	mword end = address + size;
	do {
		sgen_card_table_mark_address (address);
		address += CARD_SIZE_IN_BYTES;
	} while (address < end);
}

static gboolean
sgen_card_table_is_range_marked (guint8 *cards, mword address, mword size)
{
	guint8 *end = cards + cards_in_range (address, size);

	/*This is safe since this function is only called by code that only passes continuous card blocks*/
	while (cards != end) {
		if (*cards++)
			return TRUE;
	}
	return FALSE;

}

static void
card_table_init (void)
{
	sgen_cardtable = mono_sgen_alloc_os_memory (CARD_COUNT_IN_BYTES, TRUE);

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	sgen_shadow_cardtable = mono_sgen_alloc_os_memory (CARD_COUNT_IN_BYTES, TRUE);
#endif

#ifdef HEAVY_STATISTICS
	mono_counters_register ("marked cards", MONO_COUNTER_GC | MONO_COUNTER_LONG, &marked_cards);
	mono_counters_register ("scanned cards", MONO_COUNTER_GC | MONO_COUNTER_LONG, &scanned_cards);
	mono_counters_register ("los marked cards", MONO_COUNTER_GC | MONO_COUNTER_LONG, &los_marked_cards);
	mono_counters_register ("los array cards scanned ", MONO_COUNTER_GC | MONO_COUNTER_LONG, &los_array_cards);
	mono_counters_register ("los array remsets", MONO_COUNTER_GC | MONO_COUNTER_LONG, &los_array_remsets);
	mono_counters_register ("cardtable scanned objects", MONO_COUNTER_GC | MONO_COUNTER_LONG, &scanned_objects);
	mono_counters_register ("cardtable large objects", MONO_COUNTER_GC | MONO_COUNTER_LONG, &large_objects);
	mono_counters_register ("cardtable bloby objects", MONO_COUNTER_GC | MONO_COUNTER_LONG, &bloby_objects);
#endif
	mono_counters_register ("cardtable major scan time", MONO_COUNTER_GC | MONO_COUNTER_LONG, &major_card_scan_time);
	mono_counters_register ("cardtable los scan time", MONO_COUNTER_GC | MONO_COUNTER_LONG, &los_card_scan_time);
}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS

static void
move_cards_to_shadow_table (mword start, mword size)
{
	guint8 *from = sgen_card_table_get_card_address (start);
	guint8 *to = sgen_card_table_get_shadow_card_address (start);
	size_t bytes = cards_in_range (start, size);

	if (to + bytes > SGEN_SHADOW_CARDTABLE_END) {
		size_t first_chunk = SGEN_SHADOW_CARDTABLE_END - to;
		size_t second_chunk = MIN (CARD_COUNT_IN_BYTES, bytes) - first_chunk;

		memcpy (to, from, first_chunk);
		memcpy (sgen_shadow_cardtable, sgen_cardtable, second_chunk);
	} else {
		memcpy (to, from, bytes);
	}
}

static void
clear_cards (mword start, mword size)
{
	guint8 *addr = sgen_card_table_get_card_address (start);
	size_t bytes = cards_in_range (start, size);

	if (addr + bytes > SGEN_CARDTABLE_END) {
		size_t first_chunk = SGEN_CARDTABLE_END - addr;

		memset (addr, 0, first_chunk);
		memset (sgen_cardtable, 0, bytes - first_chunk);
	} else {
		memset (addr, 0, bytes);
	}
}


#else

static void
clear_cards (mword start, mword size)
{
	memset (sgen_card_table_get_card_address (start), 0, cards_in_range (start, size));
}


#endif

static void
card_table_clear (void)
{
	/*XXX we could do this in 2 ways. using mincore or iterating over all sections/los objects */
	if (use_cardtable) {
		major_collector.iterate_live_block_ranges (clear_cards);
		mono_sgen_los_iterate_live_block_ranges (clear_cards);
	}
}
static void
scan_from_card_tables (void *start_nursery, void *end_nursery, GrayQueue *queue)
{
	if (use_cardtable) {
		TV_DECLARE (atv);
		TV_DECLARE (btv);

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	/*FIXME we should have a bit on each block/los object telling if the object have marked cards.*/
	/*First we copy*/
	major_collector.iterate_live_block_ranges (move_cards_to_shadow_table);
	mono_sgen_los_iterate_live_block_ranges (move_cards_to_shadow_table);

	/*Then we clear*/
	card_table_clear ();
#endif
		TV_GETTIME (atv);
		major_collector.scan_card_table (queue);
		TV_GETTIME (btv);
		last_major_scan_time = TV_ELAPSED_MS (atv, btv); 
		major_card_scan_time += last_major_scan_time;
		mono_sgen_los_scan_card_table (queue);
		TV_GETTIME (atv);
		last_los_scan_time = TV_ELAPSED_MS (btv, atv);
		los_card_scan_time += last_los_scan_time;
	}
}

guint8*
mono_gc_get_card_table (int *shift_bits, gpointer *mask)
{
	if (!use_cardtable)
		return NULL;

	g_assert (sgen_cardtable);
	*shift_bits = CARD_BITS;
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	*mask = (gpointer)CARD_MASK;
#else
	*mask = NULL;
#endif

	return sgen_cardtable;
}

#if 0
static void
collect_faulted_cards (void)
{
#define CARD_PAGES (CARD_COUNT_IN_BYTES / 4096)
	int i, count = 0;
	unsigned char faulted [CARD_PAGES] = { 0 };
	mincore (sgen_cardtable, CARD_COUNT_IN_BYTES, faulted);

	for (i = 0; i < CARD_PAGES; ++i) {
		if (faulted [i])
			++count;
	}

	printf ("TOTAL card pages %d faulted %d\n", CARD_PAGES, count);
}
#endif

#define MWORD_MASK (sizeof (mword) - 1)

static inline int
find_card_offset (mword card)
{
/*XXX Use assembly as this generates some pretty bad code */
#if defined(__i386__) && defined(__GNUC__)
	return  (__builtin_ffs (card) - 1) / 8;
#elif defined(__x86_64__) && defined(__GNUC__)
	return (__builtin_ffsll (card) - 1) / 8;
#elif defined(__s390x__)
	return (__builtin_ffsll (card) - 1) / 8;
#else
	// FIXME:
	g_assert_not_reached ();
	/*
	int i;
	guint8 *ptr = (guint *) &card;
	for (i = 0; i < sizeof (mword); ++i) {
		if (ptr[i])
			return i;
	}
	*/
	return 0;
#endif
}

static guint8*
find_next_card (guint8 *card_data, guint8 *end)
{
	mword *cards, *cards_end;
	mword card;

	while ((((mword)card_data) & MWORD_MASK) && card_data < end) {
		if (*card_data)
			return card_data;
		++card_data;
	}

	if (card_data == end)
		return end;

	cards = (mword*)card_data;
	cards_end = (mword*)((mword)end & ~MWORD_MASK);
	while (cards < cards_end) {
		card = *cards;
		if (card)
			return (guint8*)cards + find_card_offset (card);
		++cards;
	}

	card_data = (guint8*)cards_end;
	while (card_data < end) {
		if (*card_data)
			return card_data;
		++card_data;
	}

	return end;
}

void
sgen_cardtable_scan_object (char *obj, mword obj_size, guint8 *cards, SgenGrayQueue *queue)
{
	MonoVTable *vt = (MonoVTable*)LOAD_VTABLE (obj);
	MonoClass *klass = vt->klass;

	HEAVY_STAT (++large_objects);

	if (!SGEN_VTABLE_HAS_REFERENCES (vt))
		return;

	if (vt->rank) {
		guint8 *card_data, *card_base;
		guint8 *card_data_end;
		char *obj_start = sgen_card_table_align_pointer (obj);
		char *obj_end = obj + obj_size;
		size_t card_count;
		int extra_idx = 0;

		MonoArray *arr = (MonoArray*)obj;
		mword desc = (mword)klass->element_class->gc_descr;
		int elem_size = mono_array_element_size (klass);

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		guint8 *overflow_scan_end = NULL;
#endif

		if (cards)
			card_data = cards;
		else
			card_data = sgen_card_table_get_card_scan_address ((mword)obj);

		card_base = card_data;
		card_count = cards_in_range ((mword)obj, obj_size);
		card_data_end = card_data + card_count;


#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		/*Check for overflow and if so, setup to scan in two steps*/
		if (!cards && card_data_end >= SGEN_SHADOW_CARDTABLE_END) {
			overflow_scan_end = sgen_shadow_cardtable + (card_data_end - SGEN_SHADOW_CARDTABLE_END);
			card_data_end = SGEN_SHADOW_CARDTABLE_END;
		}

LOOP_HEAD:
#endif

		card_data = find_next_card (card_data, card_data_end);
		for (; card_data < card_data_end; card_data = find_next_card (card_data + 1, card_data_end)) {
			int index;
			int idx = (card_data - card_base) + extra_idx;
			char *start = (char*)(obj_start + idx * CARD_SIZE_IN_BYTES);
			char *card_end = start + CARD_SIZE_IN_BYTES;
			char *elem;

			HEAVY_STAT (++los_marked_cards);

			if (!cards)
				sgen_card_table_prepare_card_for_scanning (card_data);

			card_end = MIN (card_end, obj_end);

			if (start <= (char*)arr->vector)
				index = 0;
			else
				index = ARRAY_OBJ_INDEX (start, obj, elem_size);

			elem = (char*)mono_array_addr_with_size ((MonoArray*)obj, elem_size, index);
			if (klass->element_class->valuetype) {
				for (; elem < card_end; elem += elem_size)
					major_collector.minor_scan_vtype (elem, desc, nursery_start, nursery_next, queue);
			} else {
				HEAVY_STAT (++los_array_cards);
				for (; elem < card_end; elem += SIZEOF_VOID_P) {
					gpointer new, old = *(gpointer*)elem;
					if (G_UNLIKELY (ptr_in_nursery (old))) {
						HEAVY_STAT (++los_array_remsets);
						major_collector.copy_object ((void**)elem, queue);
						new = *(gpointer*)elem;
						if (G_UNLIKELY (ptr_in_nursery (new)))
							mono_sgen_add_to_global_remset (queue->allocator, elem);
					}
				}
			}
		}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS
		if (overflow_scan_end) {
			extra_idx = card_data - card_base;
			card_base = card_data = sgen_shadow_cardtable;
			card_data_end = overflow_scan_end;
			overflow_scan_end = NULL;
			goto LOOP_HEAD;
		}
#endif

	} else {
		HEAVY_STAT (++bloby_objects);
		if (cards) {
			if (sgen_card_table_is_range_marked (cards, (mword)obj, obj_size))
				major_collector.minor_scan_object (obj, queue);
		} else if (sgen_card_table_region_begin_scanning ((mword)obj, obj_size)) {
			major_collector.minor_scan_object (obj, queue);
		}
	}
}

#ifdef CARDTABLE_STATS

typedef struct {
	int total, marked, remarked;	
} card_stats;

static card_stats major_stats, los_stats;
static card_stats *cur_stats;

static void
count_marked_cards (mword start, mword size)
{
	mword end = start + size;
	while (start <= end) {
		++cur_stats->total;
		if (sgen_card_table_address_is_marked (start))
			++cur_stats->marked;
		start += CARD_SIZE_IN_BYTES;
	}
}

static void
count_remarked_cards (mword start, mword size)
{
	mword end = start + size;
	while (start <= end) {
		if (sgen_card_table_address_is_marked (start))
			++cur_stats->remarked;
		start += CARD_SIZE_IN_BYTES;
	}
}

#endif

static void
card_tables_collect_stats (gboolean begin)
{
#ifdef CARDTABLE_STATS
	if (begin) {
		memset (&major_stats, 0, sizeof (card_stats));
		memset (&los_stats, 0, sizeof (card_stats));
		cur_stats = &major_stats;
		major_collector.iterate_live_block_ranges (count_marked_cards);
		cur_stats = &los_stats;
		mono_sgen_los_iterate_live_block_ranges (count_marked_cards);
	} else {
		cur_stats = &major_stats;
		major_collector.iterate_live_block_ranges (count_marked_cards);
		cur_stats = &los_stats;
		mono_sgen_los_iterate_live_block_ranges (count_remarked_cards);
		printf ("cards major (t %d m %d r %d)  los (t %d m %d r %d) major_scan %lld los_scan %lld\n", 
			major_stats.total, major_stats.marked, major_stats.remarked,
			los_stats.total, los_stats.marked, los_stats.remarked,
			last_major_scan_time, last_los_scan_time);
	}
#endif
}

#else

void
sgen_card_table_mark_address (mword address)
{
	g_assert_not_reached ();
}

void
sgen_card_table_mark_range (mword address, mword size)
{
	g_assert_not_reached ();
}

#define sgen_card_table_address_is_marked(p)	FALSE
#define scan_from_card_tables(start,end,queue)
#define card_table_clear()
#define card_table_init()
#define card_tables_collect_stats(begin)

guint8*
mono_gc_get_card_table (int *shift_bits, gpointer *mask)
{
	return NULL;
}

#endif
