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

static mword
cards_in_range (mword address, mword size)
{
	mword end = address + size;
	return (end >> CARD_BITS) - (address >> CARD_BITS) + 1;
}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS

guint8 *sgen_shadow_cardtable;

static gboolean
sgen_card_table_region_begin_scanning (mword start, mword end)
{
	while (start <= end) {
		if (sgen_card_table_card_begin_scanning (start))
			return TRUE;
		start += CARD_SIZE_IN_BYTES;
	}
	return FALSE;
}

void
sgen_card_table_get_card_data (guint8 *dest, mword address, mword cards)
{
	memcpy (dest, sgen_card_table_get_shadow_card_address (address), cards);
}

#else

static gboolean
sgen_card_table_region_begin_scanning (mword start, mword size)
{
	gboolean res = FALSE;
	guint8 *card = sgen_card_table_get_card_address (start);
	guint8 *end = card + cards_in_range (start, size);

	while (card != end) {
		if (*card++) {
			res = TRUE;
			break;
		}
	}

	memset (sgen_card_table_get_card_address (start), 0, size >> CARD_BITS);

	return res;
}

void
sgen_card_table_get_card_data (guint8 *dest, mword address, mword cards)
{
	guint8 *src = sgen_card_table_get_card_address (address);
	memcpy (dest, src, cards);
	memset (src, 0, cards);
}

#endif

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
sgen_card_table_is_range_marked (guint8 *cards, mword size)
{
	mword start = 0;
	while (start <= size) {
		if (*cards++)
			return TRUE;
		start += CARD_SIZE_IN_BYTES;
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
}


void los_scan_card_table (GrayQueue *queue);
void los_iterate_live_block_ranges (sgen_cardtable_block_callback callback);


static void
clear_cards (mword start, mword size)
{
	memset (sgen_card_table_get_card_address (start), 0, size >> CARD_BITS);
}

#ifdef SGEN_HAVE_OVERLAPPING_CARDS

static void
move_cards_to_shadow_table (mword start, mword size)
{
	guint8 *from = sgen_card_table_get_card_address (start);
	guint8 *to = sgen_card_table_get_shadow_card_address (start);
	size_t bytes = cards_in_range (start, size);
	memcpy (to, from, bytes);
}

#endif

static void
card_table_clear (void)
{
	/*XXX we could do this in 2 ways. using mincore or iterating over all sections/los objects */
	if (use_cardtable) {
		major.iterate_live_block_ranges (clear_cards);
		los_iterate_live_block_ranges (clear_cards);
	}
}
static void
scan_from_card_tables (void *start_nursery, void *end_nursery, GrayQueue *queue)
{
	if (use_cardtable) {
#ifdef SGEN_HAVE_OVERLAPPING_CARDS
	/*First we copy*/
	major.iterate_live_block_ranges (move_cards_to_shadow_table);
	los_iterate_live_block_ranges (move_cards_to_shadow_table);

	/*Then we clear*/
	card_table_clear ();
#endif
		major.scan_card_table (queue);
		los_scan_card_table (queue);
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


void
sgen_cardtable_scan_object (char *obj, mword obj_size, guint8 *cards, SgenGrayQueue *queue)
{
	MonoVTable *vt = (MonoVTable*)LOAD_VTABLE (obj);
	MonoClass *klass = vt->klass;

	if (!klass->has_references)
		return;

	if (vt->rank) {
		MonoArray *arr = (MonoArray*)obj;
		mword desc = (mword)klass->element_class->gc_descr;
		char *start = sgen_card_table_align_pointer (obj);
		char *end = obj + obj_size;
		int size = mono_array_element_size (klass);

		g_assert (desc);

		for (; start <= end; start += CARD_SIZE_IN_BYTES) {
			char *elem, *card_end;
			uintptr_t index;

			if (cards) {
				if (!*cards++)
					continue;
			} else if (!sgen_card_table_card_begin_scanning ((mword)start)) {
				continue;
			}

			card_end = start + CARD_SIZE_IN_BYTES;
			if (end < card_end)
				card_end = end;

			if (start <= (char*)arr->vector)
				index = 0;
			else
				index = ARRAY_OBJ_INDEX (start, obj, size);

			elem = (char*)mono_array_addr_with_size ((MonoArray*)obj, size, index);
			if (klass->element_class->valuetype) {
				while (elem < card_end) {
					major.minor_scan_vtype (elem, desc, nursery_start, nursery_next, queue);
					elem += size;
				}
			} else {
				while (elem < card_end) {
					gpointer new, old = *(gpointer*)elem;
					if (old) {
						major.copy_object ((void**)elem, queue);
						new = *(gpointer*)elem;
						if (G_UNLIKELY (ptr_in_nursery (new)))
							mono_sgen_add_to_global_remset (elem);
					}
					elem += size;
				}
			}
		}
	} else {
		if (cards) {
			if (sgen_card_table_is_range_marked (cards, obj_size))
				major.minor_scan_object (obj, queue);
		} else if (sgen_card_table_region_begin_scanning ((mword)obj, obj_size)) {
			major.minor_scan_object (obj, queue);
		}
	}
}

#ifdef CARDTABLE_STATS

static int total_cards, marked_cards, remarked_cards;

static void
count_marked_cards (mword start, mword size)
{
	mword end = start + size;
	while (start <= end) {
		++total_cards;
		if (sgen_card_table_address_is_marked (start))
			++marked_cards;
		start += CARD_SIZE_IN_BYTES;
	}
}

static void
count_remarked_cards (mword start, mword size)
{
	mword end = start + size;
	while (start <= end) {
		if (sgen_card_table_address_is_marked (start))
			++remarked_cards;
		start += CARD_SIZE_IN_BYTES;
	}
}

#endif

static void
card_tables_collect_starts (gboolean begin)
{
#ifdef CARDTABLE_STATS
	if (begin) {
		total_cards = marked_cards = remarked_cards = 0;
		major.iterate_live_block_ranges (count_marked_cards);
		los_iterate_live_block_ranges (count_marked_cards);
	} else {
		major.iterate_live_block_ranges (count_marked_cards);
		los_iterate_live_block_ranges (count_remarked_cards);
		printf ("cards total %d marked %d remarked %d\n", total_cards, marked_cards, remarked_cards);
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
#define card_tables_collect_starts(begin)

guint8*
mono_gc_get_card_table (int *shift_bits, gpointer *mask)
{
	return NULL;
}

#endif
