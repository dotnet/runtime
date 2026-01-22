// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include <time.h>

#ifdef _WIN32
#include <windows.h>
#else
#include <pthread.h>
#include <unistd.h>
#endif

#include "../dn-ebr.h"
#include "../dn-vector.h"
#include <minipal/mutex.h>
#include <minipal/atomic.h>
#include <minipal/xoshiro128pp.h>

typedef struct TestObj {
    uint64_t id;
    uint32_t checksum;
    volatile uint32_t deleted; // set to 1 in delete callback
} TestObj;

// Globals
#define SLOT_COUNT 8
static void *g_slots[SLOT_COUNT] = {0};
static volatile uint32_t g_allocations = 0;
static volatile uint32_t g_deletions_marked = 0;

// Deferred free list to allow use-after-free detection window
typedef struct DeletedNode { void *ptr; struct DeletedNode *next; } DeletedNode;
static DeletedNode *g_deleted_head = NULL;
static dn_ebr_collector_t g_collector_storage_obj;
static dn_ebr_collector_t *g_collector = NULL;

static size_t estimate_size_cb(void *obj) {
    (void)obj;
    return sizeof(TestObj);
}

static void delete_cb(void *obj) {
    TestObj *t = (TestObj*)obj;
    t->deleted = 1;
    // Track deleted objects in a list and eventually free
    DeletedNode *node = (DeletedNode*)malloc(sizeof(DeletedNode));
    if (!node) abort();
    node->ptr = obj;
    node->next = g_deleted_head;
    g_deleted_head = node;

    // Count deletions marked
    minipal_atomic_increment_u32(&g_deletions_marked);

    // When list grows large, free a batch to avoid indefinite leaks
    // Free oldest ~1000 by popping from head repeatedly
    size_t count = 0;
    DeletedNode *iter = g_deleted_head;
    while (iter) { count++; iter = iter->next; }
    if (count > 1000) {
        // Free 1000 nodes
        for (size_t i = 0; i < 1000 && g_deleted_head; i++) {
            DeletedNode *cur = g_deleted_head;
            g_deleted_head = g_deleted_head->next;
            free(cur->ptr);
            free(cur);
        }
    }
}

static const dn_ebr_deletion_traits_t g_traits = {
    .estimate_size = estimate_size_cb,
    .delete_object = delete_cb,
};

static void fatal_cb(const char *msg) {
    fprintf(stderr, "FATAL: %s\n", msg);
    fflush(stderr);
    abort();
}

static TestObj * alloc_obj(uint64_t id) {
    TestObj *t = (TestObj*)malloc(sizeof(TestObj));
    if (!t) abort();
    t->id = id;
    t->checksum = (uint32_t)(id ^ 0xA5A5A5A5u);
    t->deleted = 0;
    minipal_atomic_increment_u32(&g_allocations);
    return t;
}

typedef struct ThreadArgs {
    uint32_t seed;
    uint32_t iterations;
    uint32_t swaps_per_thousand;
} ThreadArgs;

static
#ifdef _WIN32
DWORD WINAPI
#else
void *
#endif
thread_func(void *arg) {
    ThreadArgs *ta = (ThreadArgs*)arg;
    // Initialize per-thread RNG
    struct minipal_xoshiro128pp rng;
    minipal_xoshiro128pp_init(&rng, ta->seed ^ (uint32_t)(uintptr_t)&rng);

    for (uint32_t it = 0; it < ta->iterations; it++) {
        dn_ebr_enter_critical_region(g_collector);

        uint32_t r = minipal_xoshiro128pp_next(&rng);
        uint32_t slot = (r >> 8) % SLOT_COUNT;
        TestObj *obj = (TestObj*)minipal_atomic_load_ptr(&g_slots[slot]);
        if (obj != NULL) {
            // Detect use-after-delete by checking deleted flag
            if (obj->deleted) {
                fprintf(stderr, "Detected use-after-delete on object id=%llu\n", (unsigned long long)obj->id);
                // Immediate failure: exit the process
                exit(1);
            } else {
                // simulate some work
                (void)(obj->checksum + (uint32_t)obj->id);
            }
        }

        // Occasionally swap in a new object and retire the old one
        if ((r % 1000u) < ta->swaps_per_thousand) {
            TestObj *new_obj = alloc_obj(((uint64_t)r << 16) ^ it);
            void *old = minipal_atomic_exchange_ptr(&g_slots[slot], new_obj);
            if (old) {
                dn_ebr_queue_for_deletion(g_collector, old, &g_traits);
            }
        }

        dn_ebr_exit_critical_region(g_collector);
    }

#ifdef _WIN32
    return 0;
#else
    return NULL;
#endif
}

int main(int argc, char **argv) {
    (void)argc; (void)argv;

    // Init EBR collector with small budget to encourage rapid cycling
    g_collector = dn_ebr_collector_init(&g_collector_storage_obj, 100 /*bytes*/, DN_DEFAULT_ALLOCATOR, fatal_cb);
    if (!g_collector) { fprintf(stderr, "failed to init collector\n"); return 2; }

    // Pre-populate slots
    for (uint32_t i = 0; i < SLOT_COUNT; i++) {
        g_slots[i] = alloc_obj(i + 1);
    }

    const uint32_t thread_count = 8;
    const uint32_t iterations = 5000000;
    const uint32_t swaps_per_thousand = 5; // 0.5% swaps

#ifdef _WIN32
    HANDLE threads[64];
#else
    pthread_t threads[64];
#endif
    ThreadArgs args[64];

    for (uint32_t i = 0; i < thread_count; i++) {
        args[i].seed = (uint32_t)(0xC001D00Du ^ (i * 2654435761u));
        args[i].iterations = iterations;
        args[i].swaps_per_thousand = swaps_per_thousand;
#ifdef _WIN32
        threads[i] = CreateThread(NULL, 0, thread_func, &args[i], 0, NULL);
        if (!threads[i]) { fprintf(stderr, "CreateThread failed\n"); return 2; }
#else
        if (pthread_create(&threads[i], NULL, thread_func, &args[i]) != 0) {
            fprintf(stderr, "pthread_create failed\n");
            return 2;
        }
#endif
    }

#ifdef _WIN32
    WaitForMultipleObjects(thread_count, threads, TRUE, INFINITE);
    for (uint32_t i = 0; i < thread_count; i++) CloseHandle(threads[i]);
#else
    for (uint32_t i = 0; i < thread_count; i++) pthread_join(threads[i], NULL);
#endif

    // Validate deletion pace BEFORE shutdown so shutdown-queued items aren't counted
    uint32_t allocs = g_allocations;
    uint32_t dels = g_deletions_marked;
    printf("Allocations=%u, DeletionsMarked=%u\n", allocs, dels);
    if (allocs > dels + 30) {
        fprintf(stderr, "EBR did not delete quickly enough (allocations exceed deletions by %u)\n", (allocs - dels));
        return 1;
    }

    // Teardown after validation
    dn_ebr_collector_shutdown(g_collector);
    // Free any remaining deleted objects
    while (g_deleted_head) {
        DeletedNode *cur = g_deleted_head;
        g_deleted_head = g_deleted_head->next;
        free(cur->ptr);
        free(cur);
    }

    return 0;
}
