// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "memory_debug.h"

#include <containers/dn-list.h>
#include <pthread.h>
#include <assert.h>

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

static uint32_t kPartitionCount = 32;
static int32_t g_trackingEnabled = 0;
static list_t* g_trackedMemory = NULL;

static void list_init(list_t* list)
{
    list->head.next = &list->head;
    list->head.prev = &list->head;

    int res = pthread_mutex_init(&list->lock, NULL);
    assert(res == 0);
}

static list_t* get_item_bucket(link_t* item)
{
    // TODO: better hash
    return g_trackedMemory + ((uintptr_t)item % kPartitionCount);
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

static void list_unlik_item(link_t* item)
{
    link_t* prev = item->prev;
    link_t* next = item->next;

    prev->next = next;
    next->prev = prev;
}

static void untrack_item(link_t* item)
{
    list_t* list = get_item_bucket(item);

    int res = pthread_mutex_lock(&list->lock);
    assert (res == 0);

    list_unlik_item(item);

    res = pthread_mutex_unlock(&list->lock);
    assert (res == 0);

    item->next = item;
    item->prev = item;
}

typedef struct memoryEntry
{
    struct link_t link;
    int size;
    int line;
    const char* file;
    int index;

    __attribute__((aligned(8)))
    uint8_t data[];
} memoryEntry;

static CRYPTO_RWLOCK* g_allocLock = NULL;
static int g_allocatedMemory;
static int g_allocationCount;

static void init_memory_entry(memoryEntry* entry, size_t size, const char* file, int line)
{
    entry->size = (int)size;
    entry->line = line;
    entry->file = file;
    entry->link.next = &entry->link;
    entry->link.prev = &entry->link;
}

static void* mallocFunction(size_t size, const char *file, int line)
{
    memoryEntry* entry = malloc(size + sizeof(struct memoryEntry));
    if (entry != NULL)
    {
        int newCount;
        CRYPTO_atomic_add(&g_allocatedMemory, (int)size, &newCount, g_allocLock);
        CRYPTO_atomic_add(&g_allocationCount, 1, &newCount, g_allocLock);
        init_memory_entry(entry, size, file, line);

        if (g_trackingEnabled)
        {
            track_item(&entry->link);
        }
    }

    return (void*)(&entry->data);
}

static void* reallocFunction (void *ptr, size_t size, const char *file, int line)
{
    struct memoryEntry* entry = NULL;
    int newCount;

    if (ptr != NULL)
    {
        ptr = (void*)((char*)ptr - sizeof(struct memoryEntry));
        entry = (struct memoryEntry*)ptr;
        CRYPTO_atomic_add(&g_allocatedMemory, (int)(-entry->size), &newCount, g_allocLock);

        // untrack the item as realloc will change ptrs and we will need to put it to correct bucket
        if (g_trackedMemory)
        {
            untrack_item(&entry->link);
        }
    }

    void* newPtr = realloc(ptr, size + sizeof(struct memoryEntry));
    if (newPtr != NULL)
    {
        CRYPTO_atomic_add(&g_allocatedMemory, (int)size, &newCount, g_allocLock);
        CRYPTO_atomic_add(&g_allocationCount, 1, &newCount, g_allocLock);

        // no need to lock a specific list lock, as we don't change pointers
        entry = (struct memoryEntry*)newPtr;
        init_memory_entry(entry, size, file, line);

        if (g_trackingEnabled)
        {
            track_item(&entry->link);
        }

        return (void*)(&entry->data);
    }

    if (entry && g_trackingEnabled)
    {
        track_item(&entry->link); // put back the original entry as the original pointer is still live
    }

    return NULL;
}

static void freeFunction(void *ptr, const char *file, int line)
{
    (void)file;
    (void)line;

    if (ptr != NULL)
    {
        int newCount;
        struct memoryEntry* entry = (struct memoryEntry*)((char*)ptr - offsetof(struct memoryEntry, data));
        CRYPTO_atomic_add(&g_allocatedMemory, (int)-entry->size, &newCount, g_allocLock);

        // unconditonally untrack item if we ever started tracking, so that we never leave
        // dangling pointers in previously tracked memory. This prevents AVs when tracking
        // is toggled quickly back and forth in succession.
        if (g_trackedMemory)
        {
            untrack_item(&entry->link);
        }

        free(entry);
    }
}

int32_t CryptoNative_GetMemoryUse(int* totalUsed, int* allocationCount)
{
    if (totalUsed == NULL || allocationCount == NULL)
    {
        return 0;
    }
    *totalUsed = g_allocatedMemory;
    *allocationCount = g_allocationCount;

    return 1;
}

PALEXPORT void CryptoNative_EnableMemoryTracking(int32_t enable)
{
    if (g_trackedMemory == NULL)
    {
        // initialize the list
        g_trackedMemory = malloc(kPartitionCount * sizeof(list_t));
        for (uint32_t i = 0; i < kPartitionCount; i++)
        {
            list_init(g_trackedMemory + i);
        }
    }
    else
    {
        // clear the lists by unlinking the list heads, any existing items
        // in the list will become orphaned in a "floating" circular list.
        // That is fine, as the free callback will keep removing them from the list.
        for (uint32_t i = 0; i < kPartitionCount; i++)
        {
            list_t* list = g_trackedMemory + i;

            pthread_mutex_lock(&list->lock);

            list_unlik_item(&list->head);

            pthread_mutex_unlock(&list->lock);
        }
    }

    g_trackingEnabled = enable;
}

PALEXPORT void CryptoNative_ForEachTrackedAllocation(void (*callback)(void* ptr, int size, const char* file, int line, void* ctx), void* ctx)
{
    if (g_trackedMemory != NULL)
    {
        for (uint32_t i = 0; i < kPartitionCount; i++)
        {
            list_t* list = g_trackedMemory + i;

            pthread_mutex_lock(&list->lock);
            for (link_t* node = list->head.next; node != &list->head; node = node->next)
            {
                memoryEntry* entry = (memoryEntry*)node;
                callback(entry->data, entry->size, entry->file, entry->line, ctx);
            }
            pthread_mutex_unlock(&list->lock);
        }
    }
}

void InitializeMemoryDebug(void)
{
    const char* debug = getenv("DOTNET_SYSTEM_NET_SECURITY_OPENSSL_MEMORY_DEBUG");
    if (debug != NULL && strcmp(debug, "1") == 0)
    {
        // This needs to be done before any allocation is done e.g. EnsureOpenSsl* is called.
        // And it also needs to be after the pointers are loaded for DISTRO_AGNOSTIC_SSL
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
        if (API_EXISTS(CRYPTO_THREAD_lock_new))
        {
            // This should cover 1.1.1+

            CRYPTO_set_mem_functions11(mallocFunction, reallocFunction, freeFunction);
            g_allocLock = CRYPTO_THREAD_lock_new();

            if (!API_EXISTS(SSL_state))
            {
                // CRYPTO_set_mem_functions exists in OpenSSL 1.0.1 as well but it has different prototype
                // and that makes it difficult to use with managed callbacks.
                // Since 1.0 is long time out of support we use it only on 1.1.1+
                CRYPTO_set_mem_functions11(mallocFunction, reallocFunction, freeFunction);
                g_allocLock = CRYPTO_THREAD_lock_new();
            }
        }
#elif OPENSSL_VERSION_NUMBER >= OPENSSL_VERSION_1_1_0_RTM
        // OpenSSL 1.0 has different prototypes and it is out of support so we enable this only
        // on 1.1.1+
        CRYPTO_set_mem_functions(mallocFunction, reallocFunction, freeFunction);
        g_allocLock = CRYPTO_THREAD_lock_new();
#endif
        pthread_rwlock_init(&g_memoryTrackerLock, NULL);
    }
}
