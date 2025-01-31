// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "memory_debug.h"

#include <containers/dn-list.h>
#include <pthread.h>
#include <assert.h>
#include <stdatomic.h>

static uint64_t atomic_add64(uint64_t* value, uint64_t addend, CRYPTO_RWLOCK* lock)
{
    if (API_EXISTS(CRYPTO_atomic_add64))
    {
        uint64_t result;
        CRYPTO_atomic_add64(value, addend, &result, lock);
        return result;
    }

    // TODO: test other compilers, solve for 32-bit platforms.
    return __atomic_fetch_add(value, addend, __ATOMIC_SEQ_CST);
}

//
// OpenSSL memory tracking/debugging facilities.
//
// We can use CRYPTO_set_mem_functions to replace allocation routines
// in OpenSSL. This allows us to prepend each allocated memory with a
// header that contains the size and source of the allocation, which
// allows us to track how much memory is OpenSSL consuming (not including
// malloc overhead).
//
// Additionally, if requested, we can track all allocations over a period
// of time and present them to managed code for analysis (via reflection APIs).
//
// Given that there is an overhead associated with tracking, the feature
// is gated behind the DOTNET_SYSTEM_NET_SECURITY_OPENSSL_MEMORY_DEBUG
// environment variable and should not be enabled by default in production.
//
// To track all allocated objects in a given period, the allocated objects
// are stringed in a circular, doubly-linked list. This allows us to do
// O(1) insertion and deletion. All operations are done under lock to prevent
// data corruption. To prevent lock contention over a single list, we maintain
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

// Whether outstanding allocations should be tracked, UINT_MAX if tracking is disabled.
static uint32_t g_trackingEnabledSince = UINT_MAX;
// Static lists of tracked allocations.
static list_t* g_trackedMemory = NULL;
// Number of partitions (distinct lists in g_trackedMemory) to track outstanding allocations in.
static uint32_t kPartitionCount = 32;
// Lock to protect the above globals. We are using rwlock reduce contention. Practically, we
// need to exclude { EnableMemoryTracking, ForEachTrackedAllocation } and { mallocFunction,
// reallocFunction, freeFunction } from running concurrently. Specifically, prevent race between
// disabling tracking and inserting an allocation that would not be considered tracked.
// 
// Since memory hooks can run in parallel (due to locks on individual lists), we can use the
// reader side for them, and writer side of the lock for EnableMemoryTracking and ForEachTrackedAllocation.
static pthread_rwlock_t g_trackedMemoryLock;

// header for each tracked allocation
typedef struct header_t
{
    // link for the circular list
    struct link_t link;

    // ordinal number of the allocation. Used to determine the target partition.
    uint64_t index;

    // size of the allocation (of the data field)
    uint64_t size;

    // filename from where the allocation was made
    const char* file;

    // line number in the file where the allocation was made
    int32_t line;

    // the start of actual allocated memory
    __attribute__((aligned(8))) uint8_t data[];
} header_t;

static CRYPTO_RWLOCK* g_allocLock = NULL;
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

static list_t* get_item_bucket(link_t* item)
{
    header_t* entry = container_of(item, header_t, link);
    uint32_t index = entry->index % kPartitionCount;
    return &g_trackedMemory[index];
}

static int32_t should_be_tracked(header_t* entry)
{
    return entry->index > g_trackingEnabledSince;
}

static void track_item(link_t* item)
{
    list_t* list = get_item_bucket(item);

    int res = pthread_mutex_lock(&list->lock);
    assert (res == 0);

    link_t* prev = &list->head;
    link_t* next = list->head.next;

    next->prev = item;
    item->next = next;
    item->prev = prev;
    prev->next = item;

    res = pthread_mutex_unlock(&list->lock);
    assert (res == 0);
}

static void list_unlink_item(link_t* item)
{
    link_t* prev = item->prev;
    link_t* next = item->next;

    prev->next = next;
    next->prev = prev;

    list_link_init(item);
}

static void untrack_item(link_t* item)
{
    list_t* list = get_item_bucket(item);

    int res = pthread_mutex_lock(&list->lock);
    assert (res == 0);

    list_unlink_item(item);

    res = pthread_mutex_unlock(&list->lock);
    assert (res == 0);

    item->next = item;
    item->prev = item;
}

static void init_memory_entry(header_t* entry, size_t size, const char* file, int32_t line, uint64_t index)
{
    entry->size = size;
    entry->line = line;
    entry->file = file;
    entry->link.next = &entry->link;
    entry->link.prev = &entry->link;
    entry->index = index;
}

static void* mallocFunction(size_t size, const char *file, int line)
{
    pthread_rwlock_rdlock(&g_trackedMemoryLock);

    header_t* entry = malloc(size + sizeof(header_t));
    if (entry != NULL)
    {
        atomic_add64(&g_allocatedMemory, size, g_allocLock);
        uint64_t newCount = atomic_add64(&g_allocationCount, 1, g_allocLock);
        init_memory_entry(entry, size, file, line, newCount);

        if (should_be_tracked(entry))
        {
            track_item(&entry->link);
        }
    }

    pthread_rwlock_unlock(&g_trackedMemoryLock);

    return (void*)(&entry->data);
}

static void* reallocFunction (void *ptr, size_t size, const char *file, int line)
{
    pthread_rwlock_rdlock(&g_trackedMemoryLock);

    struct header_t* entry = NULL;

    if (ptr != NULL)
    {
        entry = container_of(ptr, header_t, data);
        atomic_add64(&g_allocatedMemory, -entry->size, g_allocLock);

        // untrack the item as realloc will change ptrs and we will need to put it to correct bucket
        if (should_be_tracked(entry))
        {
            untrack_item(&entry->link);
        }
    }

    void* toReturn = NULL;
    void* newPtr = realloc((void*)entry, size + sizeof(header_t));
    if (newPtr != NULL)
    {
        atomic_add64(&g_allocatedMemory, size, g_allocLock);
        uint64_t newCount = atomic_add64(&g_allocationCount, 1, g_allocLock);

        entry = (struct header_t*)newPtr;
        init_memory_entry(entry, size, file, line, newCount);
        toReturn = (void*)(&entry->data);
    }

    if (entry && should_be_tracked(entry))
    {
        track_item(&entry->link);
    }

    pthread_rwlock_unlock(&g_trackedMemoryLock);

    return toReturn;
}

static void freeFunction(void *ptr, const char *file, int line)
{
    pthread_rwlock_rdlock(&g_trackedMemoryLock);

    (void)file;
    (void)line;

    if (ptr != NULL)
    {
        header_t* entry = container_of(ptr, header_t, data);
        atomic_add64(&g_allocatedMemory, -entry->size, g_allocLock);

        if (should_be_tracked(entry))
        {
            untrack_item(&entry->link);
        }

        free(entry);
    }

    pthread_rwlock_unlock(&g_trackedMemoryLock);
}

int32_t CryptoNative_GetMemoryUse(uint64_t* totalUsed, uint64_t* allocationCount)
{
    if (totalUsed == NULL || allocationCount == NULL)
    {
        return 0;
    }
    *totalUsed = g_allocatedMemory;
    *allocationCount = g_allocationCount;

    return 1;
}

void CryptoNative_EnableMemoryTracking(int32_t enable)
{
    pthread_rwlock_wrlock(&g_trackedMemoryLock);

    if (g_trackedMemory == NULL)
    {
        // initialize the list
        g_trackedMemory = malloc(kPartitionCount * sizeof(list_t));
        for (uint32_t i = 0; i < kPartitionCount; i++)
        {
            list_init(&g_trackedMemory[i]);
        }
    }
    else
    {
        // Clear the lists by unlinking the list heads, any existing items
        // in the list will become orphaned in a "floating" circular list.
        // We will not touch the links in those items during subsequent free
        // calls due to setting g_trackingEnabledSince later in this function.
        for (uint32_t i = 0; i < kPartitionCount; i++)
        {
            list_t* list = &g_trackedMemory[i];

            pthread_mutex_lock(&list->lock);

            list_unlink_item(&list->head);

            pthread_mutex_unlock(&list->lock);
        }
    }

    g_trackingEnabledSince = enable ? (uint32_t)g_allocationCount : UINT_MAX;

    pthread_rwlock_unlock(&g_trackedMemoryLock);
}

void CryptoNative_ForEachTrackedAllocation(void (*callback)(void* ptr, uint64_t size, const char* file, int32_t line, void* ctx), void* ctx)
{
    if (g_trackedMemory != NULL)
    {
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
}

void InitializeMemoryDebug(void)
{
    const char* debug = getenv("DOTNET_OPENSSL_MEMORY_DEBUG");
    if (debug != NULL && strcmp(debug, "1") == 0)
    {
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
        if (API_EXISTS(CRYPTO_THREAD_lock_new))
        {
            // This should cover 1.1.1+
            CRYPTO_set_mem_functions(mallocFunction, reallocFunction, freeFunction);
            g_allocLock = CRYPTO_THREAD_lock_new();
            pthread_rwlock_init(&g_trackedMemoryLock, NULL);
        }
#elif OPENSSL_VERSION_NUMBER >= OPENSSL_VERSION_1_1_1_RTM
        // OpenSSL 1.0 has different prototypes and it is out of support so we enable this only
        // on 1.1.1+
        CRYPTO_set_mem_functions(mallocFunction, reallocFunction, freeFunction);
        g_allocLock = CRYPTO_THREAD_lock_new();
        pthread_rwlock_init(&g_trackedMemoryLock, NULL);
#endif
    }
}
