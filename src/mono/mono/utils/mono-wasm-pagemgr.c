// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <config.h>
#include "mono-wasm-pagemgr.h"
#include <memory.h>
#include <unistd.h>
#include <glib.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-os-mutex.h>

#ifndef DISABLE_THREADS
#include <threads.h>
#endif

// #define MWPM_LOGGING
// #define MWPM_STATS

typedef enum {
	MWPM_MARK_DEAD_PAGES,
	MWPM_MARK_NEW_PAGES,
	MWPM_FREE_TO_ALLOCATED,
	MWPM_FREE_TO_ALLOCATED_ZEROED,
	MWPM_ALLOCATED_TO_FREE,
} page_action;

#define is_page_free(state) (state & MWPM_FREE_BIT)
#define is_page_owned(state) (state & MWPM_STATE_MASK)
#define is_page_in_use(state) ((state & MWPM_STATE_MASK) == MWPM_ALLOCATED)
#define get_page_skip_count(state) (state & MWPM_SKIP_MASK)

typedef uint8_t mwpm_page_state;

static mono_mutex_t mutex;
static uint8_t page_table[MWPM_MAX_PAGES];
#ifdef DISABLE_THREADS
static uint8_t is_initialized = 0;
#else
static once_flag is_initialized = ONCE_FLAG_INIT;
#endif
static uint32_t
	// The index of the first page that we control. Not all pages after this
	//  necessarily belong to us, but scans can start here.
	first_controlled_page_index = UINT32_MAX,
	// The index of the last page we've allocated. Not all pages between this
	//  and first_controlled_page_index belong to us, but scans can end here.
	last_controlled_page_index = 0;
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

static inline const char *
get_state_name (uint8_t state) {
	switch (state & MWPM_STATE_MASK) {
		case MWPM_EXTERNAL:
			return "external";
		case MWPM_FREE_DIRTY:
			return "dirty";
		case MWPM_FREE_ZEROED:
			return "zeroed";
		case MWPM_ALLOCATED:
			return "in use";
		default:
			g_assert_not_reached ();
	}
}

static inline mwpm_page_state
encode_page_state (uint8_t bits, uint32_t skip_count) {
	// We encode state into the page table like so:
	// The top two bits are the free bit and the meta bit.
	// For a free page, the meta bit indicates whether it is zeroed.
	// For an occupied page, the meta bit indicates whether we control it.
	// The remaining 6 bits encode the "skip count", which is a promise that
	//  the following N pages have the same state as the current page.
	// The skip count allows us to jump forward safely during scans for free
	//  pages so that we don't have to do a full linear scan of the page table.
	if (skip_count > MWPM_SKIP_MASK)
		skip_count = MWPM_SKIP_MASK;

	return (bits & MWPM_STATE_MASK) | (skip_count & MWPM_SKIP_MASK);
}

static void
cleanup_preceding_pages (uint32_t successor_page) {
	uint32_t first_page = successor_page > 64
		? successor_page - 64
		: 0;

	for (uint32_t i = first_page; i < successor_page; i++) {
		mwpm_page_state page_state = page_table[i];
		// for a skip_count of 0 we will skip exactly one page (otherwise we would
		//  get stuck on pages with a 0 skip count). so the maximum skip value is
		//  distance - 1 to produce an actual skip of distance pages
		uint32_t maximum_skip_value = successor_page - i - 1;
		if (maximum_skip_value > MWPM_SKIP_MASK)
			maximum_skip_value = MWPM_SKIP_MASK;
		if (get_page_skip_count (page_state) <= maximum_skip_value)
			continue;

#if defined(MWPM_LOGGING)
		g_print (
			"Repairing invalid skip value in predecessor page %u: %s %u -> %u\n",
			i, get_state_name (page_state), get_page_skip_count (page_state),
			maximum_skip_value
		);
#endif
		page_table[i] = encode_page_state (page_state & MWPM_STATE_MASK, maximum_skip_value);
	}
}

static void
transition_page_states (page_action action, uint32_t first_page, uint32_t page_count) {
	if (page_count == 0)
		return;

	g_assert (first_page < MWPM_MAX_PAGES);

	uint32_t last_page = first_page + (page_count - 1);
	g_assert (last_page >= first_page);

	g_assert (last_page < MWPM_MAX_PAGES);

	// POSIX specifies that munmap () on an address range that isn't mapped has no,
	//  effect, so we need to make sure that it's harmless to try and unmap pages we
	//  don't control. We can't use memset since it might trample UNKNOWN pages.
	for (uint32_t i = first_page, skip_value = page_count - 1; i <= last_page; i++) {
		mwpm_page_state page_state = page_table[i];

		// TODO: Remove the duplication in here
		switch (action) {
			case MWPM_MARK_DEAD_PAGES:
				g_assert (!is_page_owned (page_state));
				page_table[i] = encode_page_state (MWPM_EXTERNAL, skip_value--);
				break;
			case MWPM_MARK_NEW_PAGES:
				g_assert (!is_page_owned (page_state));
				page_table[i] = encode_page_state (MWPM_FREE_ZEROED, skip_value--);
				break;
			case MWPM_FREE_TO_ALLOCATED:
				g_assert (is_page_free (page_state));
				page_table[i] = encode_page_state (MWPM_ALLOCATED, skip_value--);
				break;
			case MWPM_FREE_TO_ALLOCATED_ZEROED:
				g_assert (is_page_free (page_state));
				page_table[i] = encode_page_state (MWPM_ALLOCATED, skip_value--);
				if (!(page_state & MWPM_META_BIT))
					// TODO: Don't recalculate the address from scratch each time
					memset (address_from_page_index (i), 0, MWPM_PAGE_SIZE);
				break;
			case MWPM_ALLOCATED_TO_FREE:
				// FIXME: Can we generate correct skip_value here? This is used
				//  by munmap, which is valid to call even on pages that are not mapped
				if (is_page_in_use (page_state))
					page_table[i] = encode_page_state (MWPM_FREE_DIRTY, 0);
				break;
			default:
				g_assert_not_reached ();
				break;
		}
	}

	if (action == MWPM_ALLOCATED_TO_FREE)
		cleanup_preceding_pages (first_page);
}

static void
print_stats () {
#if defined(MWPM_LOGGING) || defined(MWPM_STATS)
	uint32_t in_use = 0, free = 0, unallocated = 0,
		max_run = 0, current_run = 0;

	for (uint32_t i = first_controlled_page_index; i <= last_controlled_page_index; i++) {
		switch (page_table[i] & MWPM_STATE_MASK) {
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
		"sbrk(0)==%u. %u pages in use (%f%%), %u pages free, %u pages unknown. largest possible allocation: %u pages\n",
		(uint32_t)sbrk(0), in_use, in_use * 100.0 / total, free, unallocated, max_run
	);
#endif
}

static void *
acquire_new_pages_initialized (uint32_t page_count) {
	if (page_count < 1)
		return NULL;
	// Pad the allocation with an extra page, this will create waste bytes at the
	//  start and end we can use to align the resulting allocation. We will try
	//  to recover the waste if possible
	uint64_t bytes = (page_count + 1) * MWPM_PAGE_SIZE;
	uint32_t recovered_bytes = 0;
	if (bytes >= UINT32_MAX)
		return NULL;

	// We know that on WASM, sbrk grows the heap as necessary in order to return,
	//  a region of N zeroed bytes, which isn't necessarily aligned or page-sized
	uint8_t *allocation = sbrk ((uint32_t)bytes);

	if (allocation == (uint8_t *)-1) {
		// HACK: It is theoretically possible for sbrk to fail in a non-OOM condition
		//  due to identical bugs in v8 and spidermonkey, so retry exactly once.
		allocation = sbrk ((uint32_t)bytes);
		if (allocation == (uint8_t *)-1) {
#ifdef MWPM_LOGGING
			g_print ("mwpm failed to acquire memory\n");
#endif
			return NULL;
		} else {
			g_print ("MWPM WARNING: sbrk() failed once, then succeeded. Continuing.\n");
		}
	}

	uint8_t *allocation_end = allocation + bytes;

	g_assert (allocation_end != allocation);

	// If nobody else has called sbrk since we did, stitch the allocations together
	//  to eliminate the wasted page in the middle.
	if (prev_waste_start && (prev_waste_end == allocation)) {
		recovered_bytes = allocation - prev_waste_start;
		allocation = prev_waste_start;
	} else {
		// Update the dead pages that were allocated by someone else via sbrk()
		//  so that they have skip data
		uint32_t first_dead_page = first_page_from_address (prev_waste_end),
			dead_page_count = page_count_from_size (allocation - prev_waste_end);
		transition_page_states (MWPM_MARK_DEAD_PAGES, first_dead_page, dead_page_count);
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
#ifdef MWPM_LOGGING
		g_print ("mwpm failed to acquire pages because resulting page index was out of range: %u-%u\n", first_page_index, last_page_index);
#endif
		return NULL;
	}

	// g_print ("mwpm allocated %u bytes (%u pages) starting at @%u (%u recovered)\n", (uint32_t)bytes, page_count, (uint32_t)allocation, recovered_bytes);
	transition_page_states (MWPM_MARK_NEW_PAGES, first_page_index, page_count);
	print_stats ();
	last_controlled_page_index = last_page_index;
	return result;
}

static inline void
free_pages_initialized (uint32_t first_page, uint32_t page_count) {
	// expected behavior: freeing UNKNOWN pages leaves them unknown.
	// freeing FREE_ZEROED pages leaves them zeroed.
	// freeing ALLOCATED or FREE_DIRTY pages makes them FREE_DIRTY.
	transition_page_states (MWPM_ALLOCATED_TO_FREE, first_page, page_count);
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

		// Avoid worst case scenario of starting on an occupied page, then scanning
		//  backwards through a bunch of free pages to arrive at the occupied one
		mwpm_page_state page_state = page_table[i];
		if (!is_page_free (page_state)) {
			uint32_t skip_count = get_page_skip_count (page_state) + 1;
			if (skip_count < 1)
				skip_count = 1;
			i += skip_count;

#ifdef ENABLE_CHECKED_BUILD
			g_assert (!is_page_free (page_table[i - 1]));
#endif

#ifdef MWPM_LOGGING
			if (skip_count > 1)
				g_print (
					"scan skipping %u %s page(s) (head); new page is #%u with state %s\n",
					skip_count, get_state_name (page_state),
					i, get_state_name (page_table[i])
				);
#endif

			continue;
		}

		// TODO: If we find a free page with a skip count in it, that would indicate
		//  that there are N sequential free pages left we can claim without doing
		//  the scan below.

		// Scan backwards from the last candidate page to look for any non-free pages
		//  the first non-free page we find is the next place we will search from.
		for (; j >= i; j--) {
			page_state = page_table[j];

			if (!is_page_free (page_state)) {
				// Skip multiple pages
				uint32_t skip_count = get_page_skip_count (page_state) + 1;
				if (skip_count < 1)
					skip_count = 1;
				i = j + skip_count;

#ifdef ENABLE_CHECKED_BUILD
				g_assert (!is_page_free (page_table[i - 1]));
#endif

#ifdef MWPM_LOGGING
				if (skip_count > 1)
					g_print (
						"scan skipping %u %s page(s) (tail); new page is #%u with state %s\n",
						skip_count, get_state_name (page_state),
						i, get_state_name (page_table[i])
					);
#endif
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
	// This does make scans slower, but other approaches I tried have much worse fragmentation.
	uint32_t result = find_n_free_pages_in_range (first_controlled_page_index, last_controlled_page_index, page_count);
	return result;
}

static void
mwpm_init () {
	mono_os_mutex_init_recursive (&mutex);
	// Set the entire page table to 'unknown state'. As we acquire pages from sbrk, we will
	//  set those respective ranges in the table to a known state.
	memset (page_table, MWPM_EXTERNAL, sizeof(page_table));
	void *first_controlled_page_address = acquire_new_pages_initialized (MWPM_MINIMUM_PAGE_COUNT);
	g_assert (first_controlled_page_address);
	first_controlled_page_index = first_page_from_address (first_controlled_page_address);
}

static inline void
mwpm_ensure_initialized () {
#ifdef DISABLE_THREADS
	if (is_initialized)
		return;
	is_initialized = 1;
	mwpm_init ();
#else
	call_once (&is_initialized, mwpm_init);
#endif
}

void *
mwpm_alloc_range (size_t size, uint8_t zeroed) {
	void *result = NULL;
	if (!size)
		return result;

	mwpm_ensure_initialized ();
	mono_os_mutex_lock (&mutex);

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
#ifdef MWPM_LOGGING
			g_print ("mwpm failed to acquire new pages\n");
#endif
			goto exit;
		}
	} else {
		result = address_from_page_index (first_existing_page);
		// g_print ("mwpm found %u free pages at %u\n", page_count, (uint32_t) result);
	}

	if (!result)
		goto exit;

	uint32_t first_result_page = first_page_from_address (result);
	transition_page_states (zeroed ? MWPM_FREE_TO_ALLOCATED_ZEROED : MWPM_FREE_TO_ALLOCATED, first_result_page, page_count);

#ifdef MWPM_LOGGING
	g_print ("mwpm allocated %u bytes at %u\n", size, (uint32_t)result);
#endif

exit:
	mono_os_mutex_unlock (&mutex);
	return result;
}

void
mwpm_free_range (void *base, size_t size) {
	mwpm_ensure_initialized ();

	mono_os_mutex_lock (&mutex);
	uint32_t first_page = first_page_from_address (base),
		page_count = page_count_from_size (size);
	free_pages_initialized (first_page, page_count);
	mono_os_mutex_unlock (&mutex);
#ifdef MWPM_LOGGING
	g_print ("mwpm freed %u bytes at %u\n", size, (uint32_t)base);
#endif
}
