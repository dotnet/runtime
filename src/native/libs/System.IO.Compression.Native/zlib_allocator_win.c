// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <heapapi.h>
#include <intsafe.h>
#include <crtdbg.h> /* _ASSERTE */

#include <zlib_allocator.h>

/* A custom allocator for zlib that uses a dedicated heap.
   This provides better performance and avoids fragmentation
   that can occur on Windows when using the default process heap.
 */

// Gets the special heap we'll allocate from.
HANDLE GetZlibHeap()
{
    static HANDLE s_hPublishedHeap = NULL;

    // If already initialized, return immediately.
    // We don't need a volatile read here since the publish is performed with release semantics.
    if (s_hPublishedHeap != NULL) { return s_hPublishedHeap; }

    // Attempt to create a new heap. The heap will be dynamically sized.
    HANDLE hNewHeap = HeapCreate(0, 0, 0);

    if (hNewHeap != NULL)
    {
        // We created a new heap. Attempt to publish it.
        if (InterlockedCompareExchangePointer(&s_hPublishedHeap, hNewHeap, NULL) != NULL)
        {
            HeapDestroy(hNewHeap); // Somebody published before us. Destroy our heap.
            hNewHeap = NULL; // Guard against accidental use later in the method.
        }
    }
    else
    {
        // If we can't create a new heap, fall back to the process default heap.
        InterlockedCompareExchangePointer(&s_hPublishedHeap, GetProcessHeap(), NULL);
    }

    // Some thread - perhaps us, perhaps somebody else - published the heap. Return it.
    // We don't need a volatile read here since the publish is performed with release semantics.
    _ASSERTE(s_hPublishedHeap != NULL);
    return s_hPublishedHeap;
}

voidpf z_custom_calloc(opaque, items, size)
    voidpf opaque;
    unsigned items;
    unsigned size;
{

    SIZE_T cbRequested;
    if (sizeof(items) + sizeof(size) <= sizeof(cbRequested))
    {
        // multiplication can't overflow; no need for safeint
        cbRequested = (SIZE_T)items * (SIZE_T)size;
    }
    else
    {
        // multiplication can overflow; go through safeint
        if (FAILED(SIZETMult(items, size, &cbRequested))) { return NULL; }
    }

    return HeapAlloc(GetZlibHeap(), 0, cbRequested);
}

void z_custom_cfree(opaque, ptr)
    voidpf opaque;
    voidpf ptr;
{
    if (ptr == NULL) { return; } // ok to free nullptr

    if (!HeapFree(GetZlibHeap(), 0, ptr)) { goto Fail; }
    return;

Fail:
    __fastfail(FAST_FAIL_HEAP_METADATA_CORRUPTION);
}
