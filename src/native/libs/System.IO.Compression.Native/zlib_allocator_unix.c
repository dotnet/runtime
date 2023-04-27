// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdbool.h>
#include <stdint.h>
#include <external/zlib/zutil.h>

/* A custom allocator for zlib that provides some defense-in-depth over standard malloc / free.
 * (non-Windows version)
 *
 * 1. When zlib allocates fixed-length data structures for containing stream metadata, we zero
 *    the memory before using it, preventing use of uninitialized memory within these structures.
 *    Ideally we would do this for dynamically-sized buffers as well, but there is a measurable
 *    perf impact to doing this. Zeroing fixed structures seems like a good trade-off here, since
 *    these data structures contain most of the metadata used for managing the variable-length
 *    dynamically allocated buffers.
 *
 * 2. We put a cookie both before and after any allocated memory, which allows us to detect local
 *    buffer overruns on the call to free(). The cookie values are tied to the addresses where
 *    the data is located in memory.
 *
 * 3. We trash the aforementioned cookie on free(), which allows us to detect double-free.
 *
 * If any of these checks fails, the application raises SIGABRT.
 */

#ifndef MEMORY_ALLOCATION_ALIGNMENT
// malloc() returns an address suitably aligned for any built-in data type.
// Historically, this has been twice the arch's natural word size.
#ifdef HOST_64BIT
#define MEMORY_ALLOCATION_ALIGNMENT 16
#else
#define MEMORY_ALLOCATION_ALIGNMENT 8
#endif
#endif

typedef struct _DOTNET_ALLOC_COOKIE
{
    void* Address;
    size_t Size;
} DOTNET_ALLOC_COOKIE;

static bool SafeAdd(size_t a, size_t b, size_t* sum)
{
    if (SIZE_MAX - a >= b) { *sum = a + b; return true; }
    else { *sum = 0; return false; }
}

static bool SafeMult(size_t a, size_t b, size_t* product)
{
    if (SIZE_MAX / a >= b) { *product = a * b; return true; }
    else { *product = 0; return false; }
}

static DOTNET_ALLOC_COOKIE ReadAllocCookieUnaligned(const void* pSrc)
{
    DOTNET_ALLOC_COOKIE vCookie;
    memcpy(&vCookie, pSrc, sizeof(DOTNET_ALLOC_COOKIE));
    return vCookie;
}

static void WriteAllocCookieUnaligned(void* pDest, DOTNET_ALLOC_COOKIE vCookie)
{
    memcpy(pDest, &vCookie, sizeof(DOTNET_ALLOC_COOKIE));
}

// Historically, the memory allocator always returns addresses aligned to some
// particular boundary. We'll make that same guarantee here just in case somebody
// depends on it.
const size_t DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING = (sizeof(DOTNET_ALLOC_COOKIE) + MEMORY_ALLOCATION_ALIGNMENT - 1) & ~((size_t)MEMORY_ALLOCATION_ALIGNMENT  - 1);
const size_t DOTNET_ALLOC_TRAILER_COOKIE_SIZE = sizeof(DOTNET_ALLOC_COOKIE);

voidpf ZLIB_INTERNAL zcalloc(opaque, items, size)
    voidpf opaque;
    unsigned items;
    unsigned size;
{
    (void)opaque; // unreferenced formal parameter

    // If initializing a fixed-size structure, zero the memory.
    bool fZeroMemory = (items == 1);
    
    size_t cbRequested;
    if (sizeof(items) + sizeof(size) <= sizeof(cbRequested))
    {
        // multiplication can't overflow; no need for safeint
        cbRequested = (size_t)items * (size_t)size;
    }
    else
    {
        // multiplication can overflow; go through safeint
        if (!SafeMult((size_t)items, (size_t)size, &cbRequested)) { return NULL; }
    }

    // Make sure the actual allocation has enough room for our frontside & backside cookies.
    size_t cbActualAllocationSize;
    if (!SafeAdd(cbRequested, DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING + DOTNET_ALLOC_TRAILER_COOKIE_SIZE, &cbActualAllocationSize)) { return NULL; }

    void* pAlloced = (fZeroMemory) ? calloc(1, cbActualAllocationSize) : malloc(cbActualAllocationSize);
    if (pAlloced == NULL) { return NULL; } // OOM

    DOTNET_ALLOC_COOKIE* pHeaderCookie = (DOTNET_ALLOC_COOKIE*)pAlloced;
    uint8_t* pReturnToCaller = (uint8_t*)pAlloced + DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING;
    uint8_t* pTrailerCookie = pReturnToCaller + cbRequested;

    // Write out the same cookie for the header & the trailer, then we're done.

    DOTNET_ALLOC_COOKIE vCookie = { 0 };
    vCookie.Address = pReturnToCaller;
    vCookie.Size = cbRequested;
    *pHeaderCookie = vCookie; // aligned
    WriteAllocCookieUnaligned(pTrailerCookie, vCookie);

    return pReturnToCaller;
}

static void zcfree_trash_cookie(void* pCookie)
{
    memset(pCookie, 0, sizeof(DOTNET_ALLOC_COOKIE));
}

void ZLIB_INTERNAL zcfree(opaque, ptr)
    voidpf opaque;
    voidpf ptr;
{
    (void)opaque; // unreferenced formal parameter

    if (ptr == NULL) { return; } // ok to free nullptr

    // Check cookie at beginning

    DOTNET_ALLOC_COOKIE* pHeaderCookie = (DOTNET_ALLOC_COOKIE*)((uint8_t*)ptr - DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING);
    if (pHeaderCookie->Address != ptr) { goto Fail; }
    size_t cbRequested = pHeaderCookie->Size;

    // Check cookie at end

    uint8_t* pTrailerCookie = (uint8_t*)ptr + cbRequested;
    DOTNET_ALLOC_COOKIE vTrailerCookie = ReadAllocCookieUnaligned(pTrailerCookie);
    if (vTrailerCookie.Address != ptr) { goto Fail; }
    if (vTrailerCookie.Size != cbRequested) { goto Fail; }

    // Checks passed - now trash the cookies and free memory

    zcfree_trash_cookie(pHeaderCookie);
    zcfree_trash_cookie(pTrailerCookie);

    free(pHeaderCookie);
    return;

Fail:
    abort(); // cookie check failed
}
