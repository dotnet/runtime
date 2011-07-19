/*
 * sgen-pinned-allocator.c: Simple generational GC.
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2005-2010 Novell, Inc (http://www.novell.com)
 *
 * Thread start/stop adapted from Boehm's GC:
 * Copyright (c) 1994 by Xerox Corporation.  All rights reserved.
 * Copyright (c) 1996 by Silicon Graphics.  All rights reserved.
 * Copyright (c) 1998 by Fergus Henderson.  All rights reserved.
 * Copyright (c) 2000-2004 by Hewlett-Packard Company.  All rights reserved.
 *
 * THIS MATERIAL IS PROVIDED AS IS, WITH ABSOLUTELY NO WARRANTY EXPRESSED
 * OR IMPLIED.  ANY USE IS AT YOUR OWN RISK.
 *
 * Permission is hereby granted to use or copy this program
 * for any purpose,  provided the above notices are retained on all copies.
 * Permission to modify the code and to distribute modified code is granted,
 * provided the above notices are retained, and a notice that the code was
 * modified is included with the above copyright notice.
 *
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

#include "config.h"

#ifdef HAVE_SGEN_GC

#include "utils/mono-counters.h"
#include "metadata/sgen-gc.h"

/* Pinned objects are allocated in the LOS space if bigger than half a page
 * or from freelists otherwise. We assume that pinned objects are relatively few
 * and they have a slow dying speed (like interned strings, thread objects).
 * As such they will be collected only at major collections.
 * free lists are not global: when we need memory we allocate a PinnedChunk.
 * Each pinned chunk is made of several pages, the first of wich is used
 * internally for bookeeping (here think of a page as 4KB). The bookeeping
 * includes the freelists vectors and info about the object size of each page
 * in the pinned chunk. So, when needed, a free page is found in a pinned chunk,
 * a size is assigned to it, the page is divided in the proper chunks and each
 * chunk is added to the freelist. To not waste space, the remaining space in the
 * first page is used as objects of size 16 or 32 (need to measure which are more
 * common).
 * We use this same structure to allocate memory used internally by the GC, so
 * we never use malloc/free if we need to alloc during collection: the world is stopped
 * and malloc/free will deadlock.
 * When we want to iterate over pinned objects, we just scan a page at a time
 * linearly according to the size of objects in the page: the next pointer used to link
 * the items in the freelist uses the same word as the vtable. Since we keep freelists
 * for each pinned chunk, if the word points outside the pinned chunk it means
 * it is an object.
 * We could avoid this expensive scanning in creative ways. We could have a policy
 * of putting in the pinned space only objects we know about that have no struct fields
 * with references and we can easily use a even expensive write barrier for them,
 * since pointer writes on such objects should be rare.
 * The best compromise is to just alloc interned strings and System.MonoType in them.
 * It would be nice to allocate MonoThread in it, too: must check that we properly
 * use write barriers so we don't have to do any expensive scanning of the whole pinned
 * chunk list during minor collections. We can avoid it now because we alloc in it only
 * reference-free objects.
 */
struct _SgenPinnedChunk {
	SgenBlock block;
	int num_pages;
	SgenPinnedAllocator *allocator;
	int *page_sizes; /* a 0 means the page is still unused */
	void **free_list;
	SgenPinnedChunk *free_list_nexts [SGEN_PINNED_FREELIST_NUM_SLOTS];
	void *start_data;
	void *data [1]; /* page sizes and free lists are stored here */
};

#define PINNED_FIRST_SLOT_SIZE (sizeof (gpointer) * 4)
#define MAX_FREELIST_SIZE 8192

/* This is a fixed value used for pinned chunks, not the system pagesize */
#define FREELIST_PAGESIZE (16*1024)

/* keep each size a multiple of ALLOC_ALIGN */
/* on 64 bit systems 8 is likely completely unused. */
static const int freelist_sizes [] = {
	   8,   16,   24,   32,   40,   48,   64,   80,
	  96,  128,  160,  192,  224,  256,  320,  384,
	 448,  512,  584,  680,  816, 1024, 1360, 2048,
	2336, 2728, 3272, 4096, 5456, 8192 };

#define LARGE_PINNED_MEM_HEADER_MAGIC	0x7d289f3a

typedef struct _LargePinnedMemHeader LargePinnedMemHeader;
struct _LargePinnedMemHeader {
	guint32 magic;
	size_t size;
	double data[0];
};

static long long pinned_chunk_bytes_alloced = 0;
static long long large_pinned_bytes_alloced = 0;

#ifdef HEAVY_STATISTICS
static long long stat_pinned_alloc = 0;
#endif

/*
 * Debug reporting.
 */
static void
report_pinned_chunk (SgenPinnedChunk *chunk, int seq) {
	void **p;
	int i, free_pages, num_free, free_mem;
	free_pages = 0;
	for (i = 0; i < chunk->num_pages; ++i) {
		if (!chunk->page_sizes [i])
			free_pages++;
	}
	printf ("Pinned chunk %d at %p, size: %d, pages: %d, free: %d\n", seq, chunk, chunk->num_pages * FREELIST_PAGESIZE, chunk->num_pages, free_pages);
	free_mem = FREELIST_PAGESIZE * free_pages;
	for (i = 0; i < SGEN_PINNED_FREELIST_NUM_SLOTS; ++i) {
		if (!chunk->free_list [i])
			continue;
		num_free = 0;
		p = chunk->free_list [i];
		while (p) {
			num_free++;
			p = *p;
		}
		printf ("\tfree list of size %d, %d items\n", freelist_sizes [i], num_free);
		free_mem += freelist_sizes [i] * num_free;
	}
	printf ("\tfree memory in chunk: %d\n", free_mem);
}

/*
 * Debug reporting.
 */
void
mono_sgen_report_pinned_mem_usage (SgenPinnedAllocator *alc)
{
	SgenPinnedChunk *chunk;
	int i = 0;
	for (chunk = alc->chunk_list; chunk; chunk = chunk->block.next)
		report_pinned_chunk (chunk, i++);
}

/*
 * Find the slot number in the freelist for memory chunks that
 * can contain @size objects.
 */
static int
slot_for_size (size_t size)
{
	int slot;
	/* do a binary search or lookup table later. */
	for (slot = 0; slot < SGEN_PINNED_FREELIST_NUM_SLOTS; ++slot) {
		if (freelist_sizes [slot] >= size)
			return slot;
	}
	g_assert_not_reached ();
	return -1;
}

/*
 * Build a free list for @size memory chunks from the memory area between
 * start_page and end_page.
 */
static void
build_freelist (SgenPinnedAllocator *alc, SgenPinnedChunk *chunk, int slot, int size, char *start_page, char *end_page)
{
	void **p, **end;
	int count = 0;
	/*g_print ("building freelist for slot %d, size %d in %p\n", slot, size, chunk);*/
	p = (void**)start_page;
	end = (void**)(end_page - size);
	g_assert (!chunk->free_list [slot]);
	chunk->free_list [slot] = p;
	while ((char*)p + size <= (char*)end) {
		count++;
		*p = (void*)((char*)p + size);
		p = *p;
	}
	*p = NULL;
	/*g_print ("%d items created, max: %d\n", count, (end_page - start_page) / size);*/

	g_assert (!chunk->free_list_nexts [slot]);
	chunk->free_list_nexts [slot] = alc->free_lists [slot];
	alc->free_lists [slot] = chunk;
}

static SgenPinnedChunk*
alloc_pinned_chunk (SgenPinnedAllocator *alc)
{
	SgenPinnedChunk *chunk;
	int offset;
	int size = SGEN_PINNED_CHUNK_SIZE;

	chunk = mono_sgen_alloc_os_memory_aligned (size, size, TRUE);
	chunk->block.role = MEMORY_ROLE_PINNED;

	mono_sgen_update_heap_boundaries ((mword)chunk, ((mword)chunk + size));

	pinned_chunk_bytes_alloced += size;

	/* setup the bookeeping fields */
	chunk->num_pages = size / FREELIST_PAGESIZE;
	offset = G_STRUCT_OFFSET (SgenPinnedChunk, data);
	chunk->page_sizes = (void*)((char*)chunk + offset);
	offset += sizeof (int) * chunk->num_pages;
	offset = SGEN_ALIGN_UP (offset);
	chunk->free_list = (void*)((char*)chunk + offset);
	offset += sizeof (void*) * SGEN_PINNED_FREELIST_NUM_SLOTS;
	offset = SGEN_ALIGN_UP (offset);
	chunk->start_data = (void*)((char*)chunk + offset);

	/* allocate the first page to the freelist */
	chunk->page_sizes [0] = PINNED_FIRST_SLOT_SIZE;
	build_freelist (alc, chunk, slot_for_size (PINNED_FIRST_SLOT_SIZE), PINNED_FIRST_SLOT_SIZE,
			chunk->start_data, ((char*)chunk + FREELIST_PAGESIZE));
	mono_sgen_debug_printf (4, "Allocated pinned chunk %p, size: %d\n", chunk, size);

	chunk->block.next = alc->chunk_list;
	alc->chunk_list = chunk;

	chunk->allocator = alc;

	return chunk;
}

/* Must be called with an empty freelist for the given slot. */
static gboolean
populate_chunk_page (SgenPinnedAllocator *alc, SgenPinnedChunk *chunk, int slot)
{
	int size = freelist_sizes [slot];
	int i;
	g_assert (!chunk->free_list [slot]);
	g_assert (!chunk->free_list_nexts [slot]);
	for (i = 0; i < chunk->num_pages; ++i) {
		if (chunk->page_sizes [i])
			continue;
		chunk->page_sizes [i] = size;
		build_freelist (alc, chunk, slot, size, (char*)chunk + FREELIST_PAGESIZE * i, (char*)chunk + FREELIST_PAGESIZE * (i + 1));
		return TRUE;
	}
	return FALSE;
}

static void*
alloc_from_slot (SgenPinnedAllocator *alc, int slot)
{
	SgenPinnedChunk *pchunk;
	size_t size = freelist_sizes [slot];

	if (alc->delayed_free_lists [slot]) {
		void **p;
		do {
			p = alc->delayed_free_lists [slot];
		} while (SGEN_CAS_PTR (&alc->delayed_free_lists [slot], *p, p) != p);
		memset (p, 0, size);
		return p;
	}

 restart:
	pchunk = alc->free_lists [slot];
	if (pchunk) {
		void **p = pchunk->free_list [slot];
		void *next;

		g_assert (p);

		next = *p;
		pchunk->free_list [slot] = next;

		if (!next) {
			alc->free_lists [slot] = pchunk->free_list_nexts [slot];
			pchunk->free_list_nexts [slot] = NULL;
		}

		memset (p, 0, size);
		return p;
	}

	for (pchunk = alc->chunk_list; pchunk; pchunk = pchunk->block.next) {
		if (populate_chunk_page (alc, pchunk, slot))
			goto restart;
	}

	pchunk = alloc_pinned_chunk (alc);
	/* FIXME: handle OOM */
	if (pchunk->free_list [slot])
		goto restart;
	if (!populate_chunk_page (alc, pchunk, slot))
		g_assert_not_reached ();
	goto restart;
}

/* used for the GC-internal data structures */
void*
mono_sgen_alloc_pinned (SgenPinnedAllocator *alc, size_t size)
{
	int slot;
	void *res = NULL;

	HEAVY_STAT (++stat_pinned_alloc);

	if (size > freelist_sizes [SGEN_PINNED_FREELIST_NUM_SLOTS - 1]) {
		LargePinnedMemHeader *mh;

		size += sizeof (LargePinnedMemHeader);
		mh = mono_sgen_alloc_os_memory (size, TRUE);
		mh->magic = LARGE_PINNED_MEM_HEADER_MAGIC;
		mh->size = size;
		/* FIXME: do a CAS here */
		large_pinned_bytes_alloced += size;
		return mh->data;
	}

	slot = slot_for_size (size);
	g_assert (size <= freelist_sizes [slot]);
	res = alloc_from_slot (alc, slot);

	return res;
}

static void
free_from_slot (SgenPinnedAllocator *alc, void *addr, int slot)
{
	SgenPinnedChunk *pchunk = (SgenPinnedChunk*)SGEN_PINNED_CHUNK_FOR_PTR (addr);
	void **p = addr;
	void *next;

	g_assert (addr >= (void*)pchunk && (char*)addr < (char*)pchunk + pchunk->num_pages * FREELIST_PAGESIZE);

	next = pchunk->free_list [slot];
	*p = next;
	pchunk->free_list [slot] = p;

	if (!next) {
		g_assert (!pchunk->free_list_nexts [slot]);
		pchunk->free_list_nexts [slot] = alc->free_lists [slot];
		alc->free_lists [slot] = pchunk;
	}
}

void
mono_sgen_free_pinned (SgenPinnedAllocator *alc, void *addr, size_t size)
{
	LargePinnedMemHeader *mh;

	if (!addr)
		return;

	if (size <= freelist_sizes [SGEN_PINNED_FREELIST_NUM_SLOTS - 1]) {
		int slot = slot_for_size (size);
		free_from_slot (alc, addr, slot);
		return;
	}

	mh = (LargePinnedMemHeader*)((char*)addr - G_STRUCT_OFFSET (LargePinnedMemHeader, data));
	g_assert (mh->magic == LARGE_PINNED_MEM_HEADER_MAGIC);
	g_assert (mh->size == size + sizeof (LargePinnedMemHeader));
	/* FIXME: do a CAS */
	large_pinned_bytes_alloced -= mh->size;
	mono_sgen_free_os_memory (mh, mh->size);
}

void
mono_sgen_init_pinned_allocator (void)
{
	g_assert (SGEN_PINNED_FREELIST_NUM_SLOTS == sizeof (freelist_sizes) / sizeof (freelist_sizes [0]));

#ifdef HEAVY_STATISTICS
	mono_counters_register ("Pinned allocs", MONO_COUNTER_GC | MONO_COUNTER_LONG, &stat_pinned_alloc);
#endif
}

void
mono_sgen_pinned_scan_objects (SgenPinnedAllocator *alc, IterateObjectCallbackFunc callback, void *callback_data)
{
	SgenPinnedChunk *chunk;
	int i, obj_size;
	char *p, *endp;
	void **ptr;
	void *end_chunk;
	for (chunk = alc->chunk_list; chunk; chunk = chunk->block.next) {
		end_chunk = (char*)chunk + chunk->num_pages * FREELIST_PAGESIZE;
		mono_sgen_debug_printf (6, "Scanning pinned chunk %p (range: %p-%p)\n", chunk, chunk->start_data, end_chunk);
		for (i = 0; i < chunk->num_pages; ++i) {
			obj_size = chunk->page_sizes [i];
			if (!obj_size)
				continue;
			p = i? (char*)chunk + i * FREELIST_PAGESIZE: chunk->start_data;
			endp = i? p + FREELIST_PAGESIZE: (char*)chunk + FREELIST_PAGESIZE;
			mono_sgen_debug_printf (6, "Page %d (size: %d, range: %p-%p)\n", i, obj_size, p, endp);
			while (p + obj_size <= endp) {
				ptr = (void**)p;
				/* if the first word (the vtable) is outside the chunk we have an object */
				if (*ptr && (*ptr < (void*)chunk || *ptr >= end_chunk))
					callback ((char*)ptr, obj_size, callback_data);
				p += obj_size;
			}
		}
	}
}

void
mono_sgen_pinned_update_heap_boundaries (SgenPinnedAllocator *alc)
{
	SgenPinnedChunk *chunk;
	for (chunk = alc->chunk_list; chunk; chunk = chunk->block.next) {
		char *end_chunk = (char*)chunk + chunk->num_pages * FREELIST_PAGESIZE;
		mono_sgen_update_heap_boundaries ((mword)chunk, (mword)end_chunk);
	}
}

/*
 * the array of pointers from @start to @end contains conservative
 * pointers to objects inside @chunk: mark each referenced object
 * with the PIN bit.
 */
static void
mark_pinned_from_addresses (SgenPinnedChunk *chunk, void **start, void **end, IterateObjectCallbackFunc callback, void *callback_data)
{
	for (; start < end; start++) {
		char *addr = *start;
		int offset = (char*)addr - (char*)chunk;
		int page = offset / FREELIST_PAGESIZE;
		int obj_offset = page == 0? offset - ((char*)chunk->start_data - (char*)chunk): offset % FREELIST_PAGESIZE;
		int slot_size = chunk->page_sizes [page];
		void **ptr;
		/* the page is not allocated */
		if (!slot_size)
			continue;
		/* would be faster if we restrict the sizes to power of two,
		 * but that's a waste of memory: need to measure. it could reduce
		 * fragmentation since there are less pages needed, if for example
		 * someone interns strings of each size we end up with one page per
		 * interned string (still this is just ~40 KB): with more fine-grained sizes
		 * this increases the number of used pages.
		 */
		if (page == 0) {
			obj_offset /= slot_size;
			obj_offset *= slot_size;
			addr = (char*)chunk->start_data + obj_offset;
		} else {
			obj_offset /= slot_size;
			obj_offset *= slot_size;
			addr = (char*)chunk + page * FREELIST_PAGESIZE + obj_offset;
		}
		ptr = (void**)addr;
		/* if the vtable is inside the chunk it's on the freelist, so skip */
		/* FIXME: is it possible that we're pinning objects more than once here? */
		if (*ptr && (*ptr < (void*)chunk->start_data || *ptr > (void*)((char*)chunk + chunk->num_pages * FREELIST_PAGESIZE)))
			callback (addr, slot_size, callback_data);
	}
}

void
mono_sgen_pinned_scan_pinned_objects (SgenPinnedAllocator *alc, IterateObjectCallbackFunc callback, void *callback_data)
{
	SgenPinnedChunk *chunk;

	/* look for pinned addresses for pinned-alloc objects */
	mono_sgen_debug_printf (6, "Pinning from pinned-alloc objects\n");
	for (chunk = alc->chunk_list; chunk; chunk = chunk->block.next) {
		int num_pinned;
		void **pinned = mono_sgen_find_optimized_pin_queue_area (chunk->start_data,
				(char*)chunk + chunk->num_pages * FREELIST_PAGESIZE, &num_pinned);
		if (num_pinned)
			mark_pinned_from_addresses (chunk, pinned, pinned + num_pinned, callback, callback_data);
	}
}

#endif
