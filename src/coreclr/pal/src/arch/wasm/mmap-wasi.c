// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// WASI mmap/munmap — replaces wasi-emulated-mman.
// Handles both anonymous (GC) and file-backed (assembly loading) mappings.

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <stdint.h>
#include <unistd.h>

#define MAX_TRACKED 1024
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
    (void)addr; (void)prot; (void)flags;

    if (length == 0) { errno = EINVAL; return (void *)-1; }

    void *ptr = malloc(length);
    if (ptr == NULL) { errno = ENOMEM; return (void *)-1; }

    if (fd >= 0) {
        // File-backed mapping: read file content into buffer
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
        // Zero-fill remainder
        if (total < length)
            memset((char*)ptr + total, 0, length - total);
        lseek(fd, orig, SEEK_SET);
    } else {
        // Anonymous mapping: zero-fill
        memset(ptr, 0, length);
    }

    track_alloc(ptr, length);
    return ptr;
}

int munmap(void *addr, size_t length) {
    (void)length;
    if (addr != NULL && addr != (void *)-1) {
        if (untrack_alloc(addr))
            free(addr);
    }
    return 0;
}

int mprotect(void *addr, size_t length, int prot) {
    (void)addr; (void)length; (void)prot;
    return 0;
}
