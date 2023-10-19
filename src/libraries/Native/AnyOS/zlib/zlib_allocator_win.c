// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <heapapi.h>
#include <intsafe.h>
#include <winnt.h>
#include <crtdbg.h> /* _ASSERTE */

#ifdef INTERNAL_ZLIB_INTEL
#include "../../Windows/System.IO.Compression.Native/zlib-intel/zutil.h"
#else
#include "../../Windows/System.IO.Compression.Native/zlib/zutil.h"
#endif

/* A custom allocator for zlib that provides some defense-in-depth over standard malloc / free.
 * (Windows-specific version)
 *
 * 1. In 64-bit processes, we use a custom heap rather than relying on the standard process heap.
 *    This should cause zlib's buffers to go into a separate address range from the rest of app
 *    data, making it more difficult for buffer overruns to affect non-zlib-related data structures.
 *
 * 2. When zlib allocates fixed-length data structures for containing stream metadata, we zero
 *    the memory before using it, preventing use of uninitialized memory within these structures.
 *    Ideally we would do this for dynamically-sized buffers as well, but there is a measurable
 *    perf impact to doing this. Zeroing fixed structures seems like a good trade-off here, since
 *    these data structures contain most of the metadata used for managing the variable-length
 *    dynamically allocated buffers.
 *
 * 3. We put a cookie both before and after any allocated memory, which allows us to detect local
 *    buffer overruns on the call to free(). The cookie values are enciphered to make it more
 *    difficult for somebody to guess a correct value.
 *
 * 4. We trash the aforementioned cookie on free(), which allows us to detect double-free.
 *
 * If any of these checks fails, the application terminates immediately, optionally triggering a
 * crash dump. We use a special code that's easy to search for in Watson.
 */

BOOL IsMitigationDisabled()
{
    enum _MitigationEnablementTristate
    {
        MITIGATION_NOT_YET_QUERIED = 0,
        MITIGATION_DISABLED = 1,
        MITIGATION_ENABLED = 2 // really, anything other than 0 or 1
    };
    static long s_fMitigationEnablementState = MITIGATION_NOT_YET_QUERIED;

    // If already initialized, return immediately.
    // We don't need a volatile read here since the publish is performed with release semantics.
    if (s_fMitigationEnablementState != MITIGATION_NOT_YET_QUERIED)
    {
        return (s_fMitigationEnablementState == MITIGATION_DISABLED);
    }

    // Initialize the tri-state now.
    // It's ok for multiple threads to do this simultaneously. Only one thread will win.
    // Valid env var values to disable mitigation: "true" and "1"
    // All other env var values (or error) leaves mitigation enabled.
    //
    // Buffer needs to be large enough to hold null terminator, but returned cch does not include
    // null terminator. Note *exclusive* bounds of 0 and buffer length on returned cch.
    // Ref: https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-getenvironmentvariable
    CHAR pchBuffer[5]; // enough to hold "true" and a terminator
    DWORD dwEnvVarLength = GetEnvironmentVariableA("DOTNET_SYSTEM_IO_COMPRESSION_DISABLEZLIBMITIGATIONS", pchBuffer, _countof(pchBuffer));
    BOOL fMitigationDisabled = (dwEnvVarLength > 0 && dwEnvVarLength < _countof(pchBuffer))
        && (strcmp(pchBuffer, "1") == 0 || strcmp(pchBuffer, "true") == 0);
    
    // We really don't care about the return value of the ICE operation. If another thread
    // beat us to it, so be it. The recursive call will figure it out.
    InterlockedCompareExchange(
        /* destination: */ &s_fMitigationEnablementState,
        /* exchange:    */ fMitigationDisabled ? MITIGATION_DISABLED : MITIGATION_ENABLED,
        /* comparand:   */ MITIGATION_NOT_YET_QUERIED);
    return IsMitigationDisabled();
}

// Gets the special heap we'll allocate from.
HANDLE GetZlibHeap()
{
#ifdef _WIN64
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
#else
    // We don't want to create a new heap in a 32-bit process because it could end up
    // reserving too much of the address space. Instead, fall back to the normal process heap.
    return GetProcessHeap();
#endif
}

typedef struct _DOTNET_ALLOC_COOKIE
{
    PVOID CookieValue;
    union _Size
    {
        SIZE_T RawValue;
        LPVOID EncodedValue;
    } Size;
} DOTNET_ALLOC_COOKIE;

// Historically, the Windows memory allocator always returns addresses aligned to some
// particular boundary. We'll make that same guarantee here just in case somebody
// depends on it.
const SIZE_T DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING = (sizeof(DOTNET_ALLOC_COOKIE) + MEMORY_ALLOCATION_ALIGNMENT - 1) & ~((SIZE_T)MEMORY_ALLOCATION_ALIGNMENT  - 1);
const SIZE_T DOTNET_ALLOC_TRAILER_COOKIE_SIZE = sizeof(DOTNET_ALLOC_COOKIE);

voidpf ZLIB_INTERNAL zcalloc(opaque, items, size)
    voidpf opaque;
    unsigned items;
    unsigned size;
{
    (void)opaque; // suppress C4100 - unreferenced formal parameter

    if (IsMitigationDisabled())
    {
        // fallback logic copied from zutil.c
        return sizeof(uInt) > 2 ? (voidpf)malloc(items * size) :
                                  (voidpf)calloc(items, size);
    }

    // If initializing a fixed-size structure, zero the memory.
    DWORD dwFlags = (items == 1) ? HEAP_ZERO_MEMORY : 0;

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

    // Make sure the actual allocation has enough room for our frontside & backside cookies.
    SIZE_T cbActualAllocationSize;
    if (FAILED(SIZETAdd(cbRequested, DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING + DOTNET_ALLOC_TRAILER_COOKIE_SIZE, &cbActualAllocationSize))) { return NULL; }

    LPVOID pAlloced = HeapAlloc(GetZlibHeap(), dwFlags, cbActualAllocationSize);
    if (pAlloced == NULL) { return NULL; } // OOM

    // Now set the header & trailer cookies
    DOTNET_ALLOC_COOKIE* pHeaderCookie = (DOTNET_ALLOC_COOKIE*)pAlloced;
    pHeaderCookie->CookieValue = EncodePointer(&pHeaderCookie->CookieValue);
    pHeaderCookie->Size.RawValue = cbRequested;

    LPBYTE pReturnToCaller = (LPBYTE)pHeaderCookie + DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING;

    UNALIGNED DOTNET_ALLOC_COOKIE* pTrailerCookie = (UNALIGNED DOTNET_ALLOC_COOKIE*)(pReturnToCaller + cbRequested);
    pTrailerCookie->CookieValue = EncodePointer(&pTrailerCookie->CookieValue);
    pTrailerCookie->Size.EncodedValue = EncodePointer((PVOID)cbRequested);

    return pReturnToCaller;
}

FORCEINLINE
void zcfree_trash_cookie(UNALIGNED DOTNET_ALLOC_COOKIE* pCookie)
{
    memset(pCookie, 0, sizeof(*pCookie));
    pCookie->CookieValue = (PVOID)(SIZE_T)0xDEADBEEF;
}

// Marked noinline to keep it on the call stack during crash reports.
DECLSPEC_NOINLINE
DECLSPEC_NORETURN
void zcfree_cookie_check_failed()
{
    __fastfail(FAST_FAIL_HEAP_METADATA_CORRUPTION);
}

void ZLIB_INTERNAL zcfree(opaque, ptr)
    voidpf opaque;
    voidpf ptr;
{
    (void)opaque; // suppress C4100 - unreferenced formal parameter

    if (IsMitigationDisabled())
    {
        // fallback logic copied from zutil.c
        free(ptr);
        return;
    }

    if (ptr == NULL) { return; } // ok to free nullptr

    // Check cookie at beginning and end

    DOTNET_ALLOC_COOKIE* pHeaderCookie = (DOTNET_ALLOC_COOKIE*)((LPBYTE)ptr - DOTNET_ALLOC_HEADER_COOKIE_SIZE_WITH_PADDING);
    if (DecodePointer(pHeaderCookie->CookieValue) != &pHeaderCookie->CookieValue) { goto Fail; }
    SIZE_T cbRequested = pHeaderCookie->Size.RawValue;

    UNALIGNED DOTNET_ALLOC_COOKIE* pTrailerCookie = (UNALIGNED DOTNET_ALLOC_COOKIE*)((LPBYTE)ptr + cbRequested);
    if (DecodePointer(pTrailerCookie->CookieValue) != &pTrailerCookie->CookieValue) { goto Fail; }
    if (DecodePointer(pTrailerCookie->Size.EncodedValue) != (LPVOID)cbRequested) { goto Fail; }

    // Checks passed - now trash the cookies and free memory

    zcfree_trash_cookie(pHeaderCookie);
    zcfree_trash_cookie(pTrailerCookie);

    if (!HeapFree(GetZlibHeap(), 0, pHeaderCookie)) { goto Fail; }
    return;

Fail:
    zcfree_cookie_check_failed();
}
