// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "memory_debug.h"

#include <pthread.h>
#include <string.h>
#include <assert.h>
#include <stdatomic.h>

//
// OpenSSL memory tracking/debugging facilities.
//
// We can use CRYPTO_set_mem_functions to replace allocation routines in
// OpenSSL. This allows us to prepend each allocated memory with a header that
// contains the size and source of the allocation, which allows us to track how
// much memory is OpenSSL consuming (not including malloc overhead).
//
// Additionally, if requested, we can track all allocations over a period of
// time and present them to managed code for analysis (via reflection APIs).
//
// Given that there is an overhead associated with tracking, the feature is
// gated behind the DOTNET_OPENSSL_MEMORY_DEBUG environment variable and should
// not be enabled by default in production.
//
// To track all allocated objects in a given period, the allocated objects are
// stringed in a circular, doubly-linked list. This allows us to do O(1)
// insertion and deletion. All operations are done under lock to prevent data
// corruption. To prevent lock contention over a single list, we maintain
// multiple lists, each with its own lock and allocate them round-robin.
//

// helper macro for getting the pointer to containing type from a pointer to a member
#define container_of(ptr, type, member) ((type*)((char*)(ptr) - offsetof(type, member)))

typedef struct link_t
{
    struct link_t* next;
    struct link_t* prev;
} link_t;

typedef struct list_t
{
    link_t head;
    pthread_mutex_t lock;
} list_t;

// Whether outstanding allocations should be tracked
static int32_t g_trackingEnabled = 0;
// Static lists of tracked allocations.
static list_t* g_trackedMemory = NULL;
// Number of partitions (distinct lists in g_trackedMemory) to track outstanding
// allocations in.
static uint32_t kPartitionCount = 32;

// header for each tracked allocation
typedef struct header_t
{
    // link for the circular list
    struct link_t link;

    // size of the allocation (of the data field)
    uint64_t size;

    // filename from where the allocation was made
    const char* file;

    // line number in the file where the allocation was made
    int32_t line;

    // index of the list where this entry is stored
    uint32_t index;

    // the start of actual allocated memory
    __attribute__((aligned(8))) uint8_t data[];
} header_t;

static uint64_t g_allocatedMemory;
static uint64_t g_allocationCount;

static void list_link_init(link_t* link)
{
    link->next = link;
    link->prev = link;
}

static void list_init(list_t* list)
{
    list_link_init(&list->head);

    int res = pthread_mutex_init(&list->lock, NULL);
    assert(res == 0);
}

static void list_insert_link(link_t* item, link_t* prev, link_t* next)
{
    next->prev = item;
    item->next = next;
    item->prev = prev;
    prev->next = item;
}

static void list_unlink_item(link_t* item)
{
    link_t* prev = item->prev;
    link_t* next = item->next;

    prev->next = next;
    next->prev = prev;

    list_link_init(item);
}

static void init_memory_entry(header_t* entry, size_t size, const char* file, int32_t line)
{
    uint64_t newCount = __atomic_fetch_add(&g_allocationCount, 1, __ATOMIC_SEQ_CST);

    entry->size = size;
    entry->line = line;
    entry->file = file;
    list_link_init(&entry->link);
    entry->index = (uint32_t)(newCount % kPartitionCount);
}

static list_t* get_item_bucket(header_t* entry)
{
    uint32_t index = entry->index % kPartitionCount;
    return &g_trackedMemory[index];
}

static void do_track_entry(header_t* entry, int32_t add)
{
    __atomic_fetch_add(&g_allocatedMemory, (add != 0 ? entry->size : -entry->size), __ATOMIC_SEQ_CST);

    if (add != 0 && !g_trackingEnabled)
    {
        // don't track this (new) allocation individually
        return;
    }

    if (add == 0 && entry->link.next == &entry->link)
    {
        // freeing allocation, which is not in any list, skip taking the lock
        return;
    }

    list_t* list = get_item_bucket(entry);
    int res = pthread_mutex_lock(&list->lock);
    assert (res == 0);

    if (add != 0)
    {
        list_insert_link(&entry->link, &list->head, list->head.next);
    }
    else
    {
        list_unlink_item(&entry->link);
    }

    res = pthread_mutex_unlock(&list->lock);
    assert (res == 0);
}

static void* mallocFunction(size_t size, const char *file, int line)
{
    header_t* entry = malloc(size + sizeof(header_t));
    if (entry != NULL)
    {
        init_memory_entry(entry, size, file, line);
        do_track_entry(entry, 1);
    }

    return (void*)(&entry->data);
}

static void* reallocFunction (void *ptr, size_t size, const char *file, int line)
{
    struct header_t* entry = NULL;

    if (ptr != NULL)
    {
        entry = container_of(ptr, header_t, data);

        // untrack the item as realloc will free the memory and copy the contents elsewhere
        do_track_entry(entry, 0);
    }

    void* toReturn = NULL;
    header_t* newEntry = (header_t*) realloc((void*)entry, size + sizeof(header_t));
    if (newEntry != NULL)
    {
        entry = newEntry;

        init_memory_entry(entry, size, file, line);
        toReturn = (void*)(&entry->data);
    }

    // either track the new memory, or add back the original one if realloc failed
    if (entry)
    {
        do_track_entry(entry, 1);
    }

    return toReturn;
}

static void freeFunction(void *ptr, const char *file, int line)
{
    (void)file;
    (void)line;

    if (ptr != NULL)
    {
        header_t* entry = container_of(ptr, header_t, data);
        do_track_entry(entry, 0);
        free(entry);
    }
}

void CryptoNative_GetMemoryUse(uint64_t* totalUsed, uint64_t* allocationCount)
{
    assert(totalUsed != NULL);
    assert(allocationCount != NULL);

    *totalUsed = g_allocatedMemory;
    *allocationCount = g_allocationCount;
}

void CryptoNative_EnableMemoryTracking(int32_t enable)
{
    if (g_trackedMemory == NULL)
    {
        return;
    }

    if (enable)
    {
        // Clear the lists by unlinking the list heads, any existing items in
        // the list will become orphaned in a "floating" circular list.
        // we will keep removing items from the list as they are freed
        for (uint32_t i = 0; i < kPartitionCount; i++)
        {
            list_t* list = &g_trackedMemory[i];

            pthread_mutex_lock(&list->lock);

            list_unlink_item(&list->head);

            pthread_mutex_unlock(&list->lock);
        }
    }

    g_trackingEnabled = enable;
}

void CryptoNative_ForEachTrackedAllocation(void (*callback)(void* ptr, uint64_t size, const char* file, int32_t line, void* ctx), void* ctx)
{
    assert(callback != NULL);

    if (g_trackedMemory == NULL)
    {
        return;
    }

    for (uint32_t i = 0; i < kPartitionCount; i++)
    {
        list_t* list = &g_trackedMemory[i];

        pthread_mutex_lock(&list->lock);
        for (link_t* node = list->head.next; node != &list->head; node = node->next)
        {
            header_t* entry = container_of(node, header_t, link);
            callback(entry->data, entry->size, entry->file, entry->line, ctx);
        }
        pthread_mutex_unlock(&list->lock);
    }
}

static void init_tracking_lists(void)
{
    g_trackedMemory = malloc(kPartitionCount * sizeof(list_t));
    for (uint32_t i = 0; i < kPartitionCount; i++)
    {
        list_init(&g_trackedMemory[i]);
    }
}

void InitializeMemoryDebug(void)
{
    const char* debug = getenv("DOTNET_OPENSSL_MEMORY_DEBUG");
    if (debug != NULL && strcmp(debug, "1") == 0)
    {
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
        if (API_EXISTS(CRYPTO_set_mem_functions))
        {
            // This should cover 1.1.1+
            CRYPTO_set_mem_functions(mallocFunction, reallocFunction, freeFunction);
            init_tracking_lists();
        }
#elif OPENSSL_VERSION_NUMBER >= OPENSSL_VERSION_1_1_1_RTM
        // OpenSSL 1.0 has different prototypes and it is out of support so we enable this only
        // on 1.1.1+
        CRYPTO_set_mem_functions(mallocFunction, reallocFunction, freeFunction);
        init_tracking_lists();
#endif
    }
}
