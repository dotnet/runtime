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

#include <unistd.h>
#include <sys/mman.h>
#include <sys/types.h>

#define CARD_COUNT_BITS (32 - 9)
#define CARD_COUNT_IN_BYTES (1 << CARD_COUNT_BITS)


static guint8 *cardtable;


guint8*
sgen_card_table_get_card_address (mword address)
{
	return cardtable + (address >> CARD_BITS);
}


void
sgen_card_table_mark_address (mword address)
{
	*sgen_card_table_get_card_address (address) = 1;
}

static gboolean
sgen_card_table_address_is_marked (mword address)
{
	return *sgen_card_table_get_card_address (address) != 0;
}

void*
sgen_card_table_align_pointer (void *ptr)
{
	return (void*)((mword)ptr & ~(CARD_SIZE_IN_BYTES - 1));
}

void
sgen_card_table_reset_region (mword start, mword end)
{
	memset (sgen_card_table_get_card_address (start), 0, (end - start) >> CARD_BITS);
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

gboolean
sgen_card_table_is_region_marked (mword start, mword end)
{
	while (start <= end) {
		if (sgen_card_table_address_is_marked (start))
			return TRUE;
		start += CARD_SIZE_IN_BYTES;
	}
	return FALSE;
}

static void
card_table_init (void)
{
	cardtable = mono_sgen_alloc_os_memory (CARD_COUNT_IN_BYTES, TRUE);
}


void los_scan_card_table (GrayQueue *queue);

static void
scan_from_card_tables (void *start_nursery, void *end_nursery, GrayQueue *queue)
{
	if (use_cardtable) {
		major.scan_card_table (queue);
		los_scan_card_table (queue);
	}
}

static void
card_table_clear (void)
{
	/*XXX we could do this in 2 ways. using mincore or iterating over all sections/los objects */
	if (use_cardtable) {
		major.clear_card_table ();
		los_clear_card_table ();
	}
}

guint8*
mono_gc_get_card_table (int *shift_bits)
{
	if (!use_cardtable)
		return NULL;

	g_assert (cardtable);
	*shift_bits = CARD_BITS;

	return cardtable;
}

static void
collect_faulted_cards (void)
{
#define CARD_PAGES (CARD_COUNT_IN_BYTES / 4096)
	int i, count = 0;
	unsigned char faulted [CARD_PAGES] = { 0 };
	mincore (cardtable, CARD_COUNT_IN_BYTES, faulted);

	for (i = 0; i < CARD_PAGES; ++i) {
		if (faulted [i])
			++count;
	}

	printf ("TOTAL card pages %d faulted %d\n", CARD_PAGES, count);
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

guint8*
mono_gc_get_card_table (int *shift_bits)
{
	return NULL;
}

#endif
