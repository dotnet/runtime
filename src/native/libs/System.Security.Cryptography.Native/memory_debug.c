// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "memory_debug.h"

static CRYPTO_RWLOCK* g_allocLock = NULL;
static int g_allocatedMemory;
static int g_allocationCount;

static CRYPTO_allocation_cb g_memoryCallback;

struct memoryEntry
{
    int size;
    int line;
    const char* file;
} __attribute__((aligned(8)));

static void* mallocFunction(size_t size, const char *file, int line)
{
    void* ptr = malloc(size + sizeof(struct memoryEntry));
    if (ptr != NULL)
    {
        int newCount;
        CRYPTO_atomic_add(&g_allocatedMemory, (int)size, &newCount, g_allocLock);
        CRYPTO_atomic_add(&g_allocationCount, 1, &newCount, g_allocLock);
        struct memoryEntry* entry = (struct memoryEntry*)ptr;
        entry->size = (int)size;
        entry->line = line;
        entry->file = file;

        if (g_memoryCallback != NULL)
        {
            g_memoryCallback(MallocOperation, ptr, NULL, entry->size, file, line);
        }
    }

    return (void*)((char*)ptr + sizeof(struct memoryEntry));
}

static void* reallocFunction (void *ptr, size_t size, const char *file, int line)
{
    struct memoryEntry* entry;
    int newCount;

    if (ptr != NULL)
    {
        ptr = (void*)((char*)ptr - sizeof(struct memoryEntry));
        entry = (struct memoryEntry*)ptr;
        CRYPTO_atomic_add(&g_allocatedMemory, (int)(-entry->size), &newCount, g_allocLock);
    }

    void* newPtr = realloc(ptr, size + sizeof(struct memoryEntry));
    if (newPtr != NULL)
    {
        CRYPTO_atomic_add(&g_allocatedMemory, (int)size, &newCount, g_allocLock);
        CRYPTO_atomic_add(&g_allocationCount, 1, &newCount, g_allocLock);

        entry = (struct memoryEntry*)newPtr;
        entry->size = (int)size;
        entry->line = line;
        entry->file = file;

        if (g_memoryCallback != NULL)
        {
#if defined(__GNUC__) &&  __GNUC__ > 11
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wuse-after-free"
#endif
        // Now try just the _majorVer added
            g_memoryCallback(ReallocOperation, newPtr, ptr, entry->size, file, line);

#if defined(__GNUC__) &&  __GNUC__ > 11
#pragma GCC diagnostic pop
#endif
        }

        return (void*)((char*)newPtr + sizeof(struct memoryEntry));
    }

    return NULL;
}

static void freeFunction(void *ptr, const char *file, int line)
{
    if (ptr != NULL)
    {
        int newCount;
        struct memoryEntry* entry = (struct memoryEntry*)((char*)ptr - sizeof(struct memoryEntry));
        CRYPTO_atomic_add(&g_allocatedMemory, (int)-entry->size, &newCount, g_allocLock);
        if (g_memoryCallback != NULL)
        {
            g_memoryCallback(FreeOperation, entry, NULL, entry->size, file, line);
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

PALEXPORT int32_t CryptoNative_SetMemoryTracking(CRYPTO_allocation_cb callback)
{
    g_memoryCallback = callback;
    return 1;
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
    }
}
