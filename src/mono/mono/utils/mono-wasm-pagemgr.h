// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides a simple, efficient implementation of anonymous mmap/munmap for WASM.
// The emscripten libc version of mmap for anonymous mappings is slow during startup
//  because it spends a lot of time zeroing already-zeroed pages, and in some cases
//  the allocated pages will then get zeroed one more time by a malloc0 operation.

// This is distinct from malloc/free in that we support freeing a subset of an allocation,
//  i.e. you can allocate 4 pages and then free page 1, leaving pages 0/2/3 allocated.

// Note that pages mapped by MWPM can't be unmapped by libc, and vice versa.

#include <stddef.h>
#include <stdint.h>

#ifdef ENABLE_CHECKED_BUILD
#define MWPM_CHECKED
#else
// HACK
#define MWPM_CHECKED
#endif

// 64KB (WASM page size)
#define MWPM_PAGE_SIZE (64UL * 1024UL)
// 4GB (even though JS can't consistently handle addresses above 2GB)
// System.Text.JSON.Tests needs to allocate more than 2GB...
#define MWPM_MAX_MEMORY ((1024UL * 1024UL * 1024UL) * 4UL)
// #define MWPM_MAX_PAGES (uint32_t)(MWPM_MAX_MEMORY / MWPM_PAGE_SIZE)
#define MWPM_MAX_PAGES (1024 * 64)
// When allocating new zeroed pages, always allocate at least this many.
// This ensures that we don't waste a bunch of time allocating 1-2 pages at once.
// sbrk() also doesn't return page-aligned addresses, so this produces fewer
//  wasted page fragments.
#define MWPM_MINIMUM_PAGE_COUNT 16

typedef enum {
	// We don't know the state of this page
	MWPM_UNKNOWN = 0,
	// We control this page, it's in use
	MWPM_ALLOCATED,
	// We control this page, it's unused, and it's zeroed (fresh pages)
	MWPM_FREE_ZEROED,
	// We control this page, it's unused, and it contains garbage
	MWPM_FREE_DIRTY,
} mwpm_page_state;

// Allocate enough pages to hold size bytes of data, optionally ensuring they are zeroed.
// Zeroing memory on wasm is somewhat expensive, so use this option wisely!
void *
mwpm_alloc_range (size_t size, uint8_t zeroed);

// Free all the pages containing the memory range from base to base+size-1.
// If the specified range does not occupy an entire page, the page will still
//  be freed! This matches the specified behavior of posix munmap.
// base must be a multiple of MWPM_PAGE_SIZE.
void
mwpm_free_range (void *base, size_t size);
