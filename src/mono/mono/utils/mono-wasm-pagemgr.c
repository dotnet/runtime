// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <config.h>
#include "mono-wasm-pagemgr.h"
#include <memory.h>
#include <unistd.h>
#include <glib.h>

static uint8_t page_table[MWPM_MAX_PAGES];
static uint8_t is_initialized = 0;
static uint32_t
	// The index of the first page that we control. Not all pages after this
	//  necessarily belong to us, but scans can start here.
	first_controlled_page_index = UINT32_MAX,
	// The index of the last page we've allocated. Not all pages between this
	//  and first_controlled_page_index belong to us, but scans can end here.
	last_controlled_page_index = 0,
	// When we scan for free pages we keep track of where the scan ended,
	//  and start the next scan there.
	free_page_scan_start = UINT32_MAX;
static uint8_t *prev_waste_start = NULL,
	*prev_waste_end = NULL;

static inline void *
address_from_page_index (uint32_t page_index) {
	uint64_t address = ((uint64_t)page_index * MWPM_PAGE_SIZE);
	g_assert (address < UINT32_MAX);
	return (void *)(uint32_t)address;
}

static inline uint32_t
first_page_from_address (void *addr) {
	return ((uint64_t)addr) / MWPM_PAGE_SIZE;
}

static inline uint32_t
page_count_from_size (size_t size) {
	return ((size + MWPM_PAGE_SIZE - 1) / MWPM_PAGE_SIZE);
}

static inline uint32_t
last_page_of_range (void *addr, size_t size) {
	uint32_t page_count_rounded_up = page_count_from_size (size),
		first_page = first_page_from_address (addr);
	return first_page + page_count_rounded_up - 1;
}

// returns the number of pages in the range that were successfully transitioned.
static uint32_t
transition_page_states (mwpm_page_state from_state, mwpm_page_state to_state, uint32_t first_page, uint32_t page_count) {
	if (page_count == 0)
		return 0;

	if (first_page >= MWPM_MAX_PAGES)
		return 0;

	uint32_t last_page = first_page + (page_count - 1);
	g_assert (last_page >= first_page);

	if (last_page >= MWPM_MAX_PAGES)
		return 0;

	uint32_t result = 0;
	// POSIX specifies that munmap () on an address range that isn't mapped has no,
	//  effect, so we need to make sure that it's harmless to try and unmap pages we
	//  don't control. We can't use memset since it might trample UNKNOWN pages.
	for (uint32_t i = first_page; i <= last_page; i++) {
		if (page_table[i] != from_state)
			continue;

		page_table[i] = to_state;
		result++;
	}

	return result;
}

static void
print_stats () {
	uint32_t in_use = 0, free = 0, unallocated = 0,
		max_run = 0, current_run = 0;

	for (uint32_t i = first_controlled_page_index; i <= last_controlled_page_index; i++) {
		switch (page_table[i]) {
			case MWPM_ALLOCATED:
				in_use++;
				current_run = 0;
				break;

			case MWPM_FREE_DIRTY:
			case MWPM_FREE_ZEROED:
				free++;
				current_run++;
				if (current_run > max_run)
					max_run = current_run;
				break;

			default:
				unallocated++;
				current_run = 0;
				break;
		}
	}

	uint32_t total = in_use + free; // + unallocated;
	g_print (
		"%u pages in use (%f%%), %u pages free, %u pages unknown. largest possible allocation: %u pages\n",
		in_use, in_use * 100.0 / total, free, unallocated, max_run
	);
}

static void *
acquire_new_pages_initialized (uint32_t page_count) {
	if (page_count < 1)
		return NULL;
	uint64_t bytes = (page_count + 1) * MWPM_PAGE_SIZE;
	uint32_t recovered_bytes = 0;
	if (bytes >= UINT32_MAX)
		return NULL;
	// We know that on WASM, sbrk grows the heap by a set number of pages,
	//  and returns the start of the new allocation.
	uint8_t *allocation = sbrk ((uint32_t)bytes),
		*allocation_end = allocation + bytes;

	if (!allocation) {
		g_print ("mwpm failed to acquire memory\n");
		return NULL;
	}

	g_assert (allocation_end != allocation);

	// If nobody else has called sbrk since we did, stitch the allocations together
	//  to eliminate the wasted page in the middle.
	if (prev_waste_start && (prev_waste_end == allocation)) {
		recovered_bytes = allocation - prev_waste_start;
		allocation = prev_waste_start;
	}

	uint8_t *result = allocation;
	// Unfortunately emscripten libc doesn't page-align sbrk's return value.
	uint32_t realignment = MWPM_PAGE_SIZE - (((uint64_t)result) % MWPM_PAGE_SIZE);
	if (realignment < MWPM_PAGE_SIZE) {
		result += realignment;
		g_assert ((((uint64_t)result) % MWPM_PAGE_SIZE) == 0);
	}

	// Figure out how many wasted bytes are hanging off the end of our last page.
	page_count = (allocation_end - result) / MWPM_PAGE_SIZE;
	g_assert (page_count);
	// Record the region of wasted bytes we allocated, so we can try to use it later.
	prev_waste_start = result + (page_count * MWPM_PAGE_SIZE);
	prev_waste_end = allocation_end;

	// Mark all the allocated pages as free and zeroed
	uint32_t first_page_index = first_page_from_address (result),
		last_page_index = first_page_index + page_count - 1;

	if ((first_page_index >= MWPM_MAX_PAGES) || (last_page_index >= MWPM_MAX_PAGES)) {
		g_print ("mwpm failed to acquire pages because resulting page index was out of range: %u-%u\n", first_page_index, last_page_index);
		return NULL;
	}

	// g_print ("mwpm allocated %u bytes (%u pages) starting at @%u (%u recovered)\n", (uint32_t)bytes, page_count, (uint32_t)allocation, recovered_bytes);
	uint32_t pages_transitioned = transition_page_states (MWPM_UNKNOWN, MWPM_FREE_ZEROED, first_page_index, page_count);
	print_stats ();
	g_assert (pages_transitioned == page_count);
	last_controlled_page_index = last_page_index;
	return result;
}

static inline void
free_pages_initialized (uint32_t first_page, uint32_t page_count) {
	// expected behavior: freeing UNKNOWN pages leaves them unknown.
	// freeing FREE_ZEROED pages leaves them zeroed.
	// freeing ALLOCATED or FREE_DIRTY pages makes them FREE_DIRTY.
	transition_page_states (MWPM_ALLOCATED, MWPM_FREE_DIRTY, first_page, page_count);
}

static uint32_t
find_n_free_pages_in_range (uint32_t start_scan_where, uint32_t end_scan_where, uint32_t page_count) {
	if (page_count == 0)
		return UINT32_MAX;

	uint32_t i = start_scan_where;

	while (i <= end_scan_where) {
		uint8_t found_obstruction = 0;
		uint32_t j = i + page_count - 1;
		if (j > last_controlled_page_index)
			break;

		// Scan backwards from the last candidate page to look for any non-free pages
		//  the first non-free page we find is the next place we will search from.
		for (; j >= i; j--) {
			if (page_table[j] < MWPM_FREE_ZEROED) {
				i = j + 1;
				found_obstruction = 1;
				break;
			}
		}

		if (found_obstruction)
			continue;

		// We scanned page_count pages starting from i and they were all free.
		return i;
	}

	return UINT32_MAX;
}

// Scans all controlled pages to look for at least page_count free pages.
static uint32_t
find_n_free_pages (uint32_t page_count) {
	// Start scanning from the beginning. This ensures we will try to grab small allocations
	//  from the front of the page table, and large allocations from anywhere we can find.
	// This does make scans slower though.
	uint32_t result = find_n_free_pages_in_range (first_controlled_page_index, last_controlled_page_index, page_count);
	/*
	// If we didn't find anything, scan the remaining pages we didn't check
	if (result == UINT32_MAX)
		result = find_n_free_pages_in_range (first_controlled_page_index, free_page_scan_start, page_count);

	if (result != UINT32_MAX)
		free_page_scan_start = result + page_count;
	else
		free_page_scan_start = first_controlled_page_index;
	*/

	return result;
}

static void
mwpm_init () {
	// Set the entire page table to 'unknown state'. As we acquire pages from sbrk, we will
	//  set those respective ranges in the table to a known state.
	memset (page_table, MWPM_UNKNOWN, sizeof(page_table));
	void *first_controlled_page_address = acquire_new_pages_initialized (MWPM_MINIMUM_PAGE_COUNT);
	g_assert (first_controlled_page_address);
	first_controlled_page_index = first_page_from_address (first_controlled_page_address);
	free_page_scan_start = first_controlled_page_index;
	is_initialized = 1;
}

static inline void
mwpm_ensure_initialized () {
	if (is_initialized)
		return;

	mwpm_init ();
}

void *
mwpm_alloc_range (size_t size, uint8_t zeroed) {
	mwpm_ensure_initialized ();

	void *result = NULL;
	if (!size)
		return result;

	uint32_t page_count = page_count_from_size (size),
		first_existing_page = find_n_free_pages (page_count),
		allocation_page_count = page_count;

	// If we didn't find existing pages to service our alloc,
	if (first_existing_page == UINT32_MAX) {
		// g_print ("mwpm could not find %u free pages\n", page_count);
		if (allocation_page_count < MWPM_MINIMUM_PAGE_COUNT)
			allocation_page_count = MWPM_MINIMUM_PAGE_COUNT;
		// Ensure we have space for the whole allocation
		void *start_of_new_pages = acquire_new_pages_initialized (allocation_page_count);
		if (start_of_new_pages) {
			// FIXME: Scan backwards from the new allocation to look for free pages
			//  before it that we can use to reduce fragmentation
			result = start_of_new_pages;
		} else {
			g_print ("mwpm failed to acquire new pages\n");
			return result;
		}
	} else {
		result = address_from_page_index (first_existing_page);
		// g_print ("mwpm found %u free pages at %u\n", page_count, (uint32_t) result);
	}

	if (!result)
		return result;

	uint32_t first_result_page = first_page_from_address (result),
		zeroed_pages = transition_page_states (MWPM_FREE_ZEROED, MWPM_ALLOCATED, first_result_page, page_count),
		nonzeroed_pages = 0;
	// FIXME: Do this in one pass instead of two
	if (zeroed_pages != page_count) {
		// g_print ("only %u of %u page(s) were zeroed\n", zeroed_pages, page_count);
		// If we got here, not all of the pages in our allocation were in FREE_ZEROED state, so we need to
		//  zero at least one of them.
		if (zeroed) {
			// g_print ("mwpm zeroing %u bytes at %u\n", size, (uint32_t)result);
			// FIXME: Only zero the dirty pages instead of the whole region.
			memset (result, 0, size);
		}
	}
	nonzeroed_pages = transition_page_states (MWPM_FREE_DIRTY, MWPM_ALLOCATED, first_result_page, page_count);

	if ((nonzeroed_pages + zeroed_pages) != page_count) {
		g_print ("nwpm failed to allocate because zeroed + nonzeroed pages != page_count. OOM?\n");
		return NULL;
	}

	// g_print ("mwpm allocated %u bytes at %u\n", size, (uint32_t)result);
	return result;
}

void
mwpm_free_range (void *base, size_t size) {
	mwpm_ensure_initialized ();

	uint32_t first_page = first_page_from_address (base),
		page_count = page_count_from_size (size);
	free_pages_initialized (first_page, page_count);
	if (page_count >= 32) {
		g_print ("Just freed %u pages\n", page_count);
		print_stats ();
	}
	if (first_page < free_page_scan_start)
		free_page_scan_start = first_page;

	// g_print ("mwpm freed %u bytes at %u\n", size, (uint32_t)base);
}
