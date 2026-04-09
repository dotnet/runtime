// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// WASI mmap/munmap — replaces wasi-emulated-mman.
//
// Anonymous mappings (GC heap, virtual reserve) use a bump allocator backed
// by a large arena to prevent address reuse. The GC reserves and releases
// large regions; with plain malloc, freed regions can be reused by file-backed
// mappings, and then the GC zeroes them — corrupting assembly data.
//
// File-backed mappings use malloc + read since they are long-lived and small.

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <stdint.h>
#include <unistd.h>

// --- Bump allocator for anonymous mappings ---
// 256MB arena — sufficient for GC heap + runtime structures.
// On WASM this is virtual (backed by memory.grow on demand).
#define ANON_ARENA_SIZE (256 * 1024 * 1024)

static char *s_arena_base;
static size_t s_arena_offset;

static void ensure_arena(void) {
    if (s_arena_base == NULL) {
        s_arena_base = (char *)malloc(ANON_ARENA_SIZE);
        if (s_arena_base == NULL) {
            fprintf(stderr, "WASI mmap: failed to allocate %d byte arena\n", ANON_ARENA_SIZE);
            abort();
        }
        s_arena_offset = 0;
    }
}

// Allocate from the bump arena with alignment. Never reuses freed addresses.
static void *arena_alloc(size_t length, size_t align) {
    ensure_arena();
    // Align up
    size_t aligned = (s_arena_offset + align - 1) & ~(align - 1);
    if (aligned + length > ANON_ARENA_SIZE) {
        return NULL;
    }
    void *ptr = s_arena_base + aligned;
    s_arena_offset = aligned + length;
    return ptr;
}

static int is_arena_ptr(void *addr) {
    if (s_arena_base == NULL) return 0;
    return (char *)addr >= s_arena_base &&
           (char *)addr < s_arena_base + ANON_ARENA_SIZE;
}

// --- Tracking for malloc-based file mappings ---
#define MAX_TRACKED 256
static struct { void *addr; size_t size; } s_allocs[MAX_TRACKED];
static int s_alloc_count;

static void track_alloc(void *addr, size_t size) {
    if (s_alloc_count < MAX_TRACKED) {
        s_allocs[s_alloc_count].addr = addr;
        s_allocs[s_alloc_count].size = size;
        s_alloc_count++;
    }
}

static int untrack_alloc(void *addr) {
    for (int i = 0; i < s_alloc_count; i++) {
        if (s_allocs[i].addr == addr) {
            s_allocs[i] = s_allocs[--s_alloc_count];
            return 1;
        }
    }
    return 0;
}

void *mmap(void *addr, size_t length, int prot, int flags, int fd, long long offset) {
    (void)addr; (void)prot;

    if (length == 0) { errno = EINVAL; return (void *)-1; }

    if (fd >= 0) {
        // File-backed mapping: use malloc + read (isolated from GC arena)
        void *ptr = malloc(length);
        if (ptr == NULL) { errno = ENOMEM; return (void *)-1; }

        long long orig = lseek(fd, 0, SEEK_CUR);
        if (lseek(fd, offset, SEEK_SET) == -1) {
            free(ptr);
            errno = EIO;
            return (void *)-1;
        }
        size_t total = 0;
        while (total < length) {
            ssize_t n = read(fd, (char*)ptr + total, length - total);
            if (n <= 0) break;
            total += n;
        }
        if (total < length)
            memset((char*)ptr + total, 0, length - total);
        lseek(fd, orig, SEEK_SET);

        track_alloc(ptr, length);
        return ptr;
    } else {
        // Anonymous mapping: allocate from bump arena (never reuses addresses)
        void *ptr = arena_alloc(length, 65536); // page-aligned
        if (ptr == NULL) { errno = ENOMEM; return (void *)-1; }
        memset(ptr, 0, length);
        return ptr;
    }
}

int munmap(void *addr, size_t length) {
    (void)length;
    if (addr == NULL || addr == (void *)-1)
        return 0;

    if (is_arena_ptr(addr)) {
        // Arena allocations are never freed — bump allocator doesn't reuse.
        // This matches real mmap behavior where munmap'd pages can't be
        // returned to other callers.
        return 0;
    }

    // File-backed malloc allocation — free it
    if (untrack_alloc(addr))
        free(addr);
    return 0;
}

int mprotect(void *addr, size_t length, int prot) {
    (void)addr; (void)length; (void)prot;
    return 0;
}
