// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides a simple, efficient implementation of anonymous mmap/munmap for WASM.
// The emscripten libc version of mmap for anonymous mappings is slow during startup
//  because it spends a lot of time zeroing already-zeroed pages, and in some cases
//  the allocated pages will then get zeroed one more time by a malloc0 operation.

// This is distinct from malloc/free in that we support freeing a subset of an allocation,
//  i.e. you can allocate 4 pages and then free page 1, leaving pages 0/2/3 allocated.

// Note that pages mapped by MWPM can't be unmapped by libc, and vice versa.

#ifndef __MONO_UTILS_WASM_PAGEMGR_H__
#define __MONO_UTILS_WASM_PAGEMGR_H__

#ifdef HOST_WASI
#ifndef DISABLE_THREADS
#error MWPM does not support multithreaded WASI due to lack of call_once
#endif
#endif

#include <stddef.h>
#include <stdint.h>

// 64KB (WASM native page size)
// NOTE: emscripten sbrk() allows you to allocate less than 1 page, and doesn't
//  return page-aligned addresses. So this value can theoretically be smaller.
// Shrinking this would reduce wasted memory if callers try to mmap less than a
//  whole page. My initial testing didn't show big improvements though, and it
//  would make mmap (and to a lesser degree, munmap) have to scan/touch more pages.
// I tested with 16KB pages and it wasn't an improvement over 64KB.
// 8KB pages produces a slight reduction in total memory usage (1.3%). 4KB is worse.
#define MWPM_PAGE_SIZE (64 * 1024)

// 4GB (even though JS can't consistently handle addresses above 2GB)
// System.Text.JSON.Tests needs to allocate more than 2GB...
#define MWPM_MAX_MEMORY ((1024UL * 1024UL * 1024UL) * 4UL)

// The proper definition compiles down to 0 for some reason even if I put typecasts
//  around it
// #define MWPM_MAX_PAGES (uint32_t)(MWPM_MAX_MEMORY / MWPM_PAGE_SIZE)
#define MWPM_MAX_PAGES (64 * 1024)

// When allocating new zeroed pages, always allocate at least this many.
// This ensures that we don't waste a bunch of time allocating 1-2 pages at once.
// sbrk() also doesn't return page-aligned addresses, so this produces fewer
//  wasted page fragments and in general ensures we can service larger allocations.
// Setting this constant incorrectly will cause higher fragmentation and higher
//  memory usage, potentially causing OOM. Data from S.T.J test suite:
// (minimum count) (total pages allocated) (largest available space)
// 16   13677 363
// 24   14675 340
// 30   14653 192
// 32   11399 463
// 34   14412 338
// 48   14460 461
// 64   11767 230
// 96   12986  96
// 128  14059 321
// 192  14686 289
// 256  12281 256
#define MWPM_MINIMUM_PAGE_COUNT 32

#define MWPM_FREE_BIT   0b10000000
#define MWPM_META_BIT   0b01000000
#define MWPM_STATE_MASK 0b11000000
#define MWPM_SKIP_MASK  0b00111111

#define MWPM_FREE_ZEROED (uint8_t)(MWPM_FREE_BIT | MWPM_META_BIT)
#define MWPM_FREE_DIRTY  (uint8_t)(MWPM_FREE_BIT)
#define MWPM_ALLOCATED   (uint8_t)(MWPM_META_BIT)
#define MWPM_EXTERNAL    (uint8_t)(0)

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

#endif
