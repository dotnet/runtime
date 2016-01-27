// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// LazyCOW.h
//

//
// Provides support for "lazy copy-on-write" pages.  
//
// NGEN images contain a large amount of writable data.  At runtime, we typically only actually write to a small portion of this data.
// When we write to a writable page in an image, the OS must create a process-local copy of that page, so that other proceses
// cannot see the written data.  To prevent this copy from failing, the OS pre-commits space in the pagefile for all writable
// pages when the image is loaded.  Thus we get charged for every writable page, even if we only write to a few of them.
//
// FEATURE_LAZY_COW_PAGES enables "lazy copy-on-write."  We mark the pages in the image file as read-only, and thus the OS
// does not pre-commit pagefile for these pages.   At runtime, prior to writing to any page, we update it to be writable.
// This may fail, and thus is not appropriate for scenarios where strong reliability guarantees must be met.  But for
// devices with small memory this is still worth it.
//
// --------------------------------------------------------------------------------

#ifndef LAZY_COW_H
#define LAZY_COW_H

#ifdef FEATURE_LAZY_COW_PAGES

#ifdef _WIN64 // due to the way we track pages, we cannot currently support 64-bit.
#error FEATURE_LAZY_COW_PAGES is only supported on 32-bit platforms.
#endif

class PEDecoder;

// If hModule is a native image, establishes copy-on-write tracking for the image.
// FreeLazyCOWPages must be called immediately before the module is unloaded.
void AllocateLazyCOWPages(PEDecoder * pImage);

// If hModule is a native image, disestablishes copy-on-write tracking for the image.
// The image must be immediately unloaded following this call.
void FreeLazyCOWPages(PEDecoder * pImage);

bool IsInReadOnlyLazyCOWPage(void* p);


// Forces the page(s) covered by the given address range to be made writable,
// if they are being tracked as copy-on-write pages.  Otherwise does nothing.
// Returns false if we could not allocate the necessary memory.
bool EnsureWritablePagesNoThrow(void* p, size_t len);

// Version for executable pages
bool EnsureWritableExecutablePagesNoThrow(void* p, size_t len);

// Throwing version of EnsureWritablePagesNoThrow 
void EnsureWritablePages(void* p, size_t len);

// Version for executable pages
void EnsureWritableExecutablePages(void* p, size_t len);

#else //FEATURE_LAZY_COW_PAGES

inline bool EnsureWritablePagesNoThrow(void* p, size_t len)
{
    return true;
}

inline bool EnsureWritableExecutablePagesNoThrow(void* p, size_t len)
{
    return true;
}

inline void EnsureWritablePages(void* p, size_t len)
{
}

inline void EnsureWritableExecutablePages(void* p, size_t len)
{
}

#endif //FEATURE_LAZY_COW_PAGES

// Typed version of EnsureWritable.  Returns p, so this can be inserted in expressions.
// Ignores any failure to allocate.  In typical cases this means that the write will AV.
// In the CLR that's OK; we handle the AV, try EnsureWritable(void*,size_t), and
// fail-fast when it fails.
template<typename T>
inline T* EnsureWritablePages(T* p)
{
    EnsureWritablePages(p, sizeof(T));
    return p;
}

template<typename T>
inline T* EnsureWritableExecutablePages(T* p)
{
    EnsureWritableExecutablePages(p, sizeof(T));
    return p;
}

#endif // LAZY_COW_H
