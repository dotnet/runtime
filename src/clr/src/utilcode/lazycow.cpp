// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// LazyCOW.cpp
//

#include "stdafx.h"

#include "pedecoder.h"
#include "volatile.h"
#include "lazycow.h"

#ifdef FEATURE_LAZY_COW_PAGES

// 
// We can't use the hosted ClrVirtualProtect, because EnsureWritablePages is called in places where we can't
// call into the host.
//
#ifdef VirtualProtect
#undef VirtualProtect
#endif

#ifdef VirtualAlloc
#undef VirtualAlloc
#endif

#ifdef VirtualFree
#undef VirtualFree
#endif

LONG* g_pCOWPageMap;    // one bit for each page in the virtual address space
LONG  g_COWPageMapMap;  // one bit for each page in g_pCOWPageMap.

LONG* EnsureCOWPageMapAllocated()
{
    if (g_pCOWPageMap == NULL)
    {
        // We store one bit for every page in the virtual address space.  We may need to revisit this for 64-bit. :)
        MEMORYSTATUSEX stats;
        stats.dwLength = sizeof(stats);
        if (GlobalMemoryStatusEx(&stats))
        {
            _ASSERTE(stats.ullTotalVirtual < 0x100000000ULL);

            SIZE_T mapSize = (SIZE_T)((stats.ullTotalVirtual / GetOsPageSize()) / 8);
            _ASSERTE(mapSize / GetOsPageSize() <= 32); // g_COWPageMapMap can only track 32 pages

            // Note that VirtualAlloc will zero-fill the pages for us.
            LONG* pMap = (LONG*)VirtualAlloc(
                NULL,
                mapSize,
                MEM_RESERVE,
                PAGE_READWRITE);

            if (pMap != NULL)
            {
                if (NULL != InterlockedCompareExchangeT(&g_pCOWPageMap, pMap, NULL))
                    VirtualFree(pMap, 0, MEM_RELEASE);
            }
        }
    }
    return g_pCOWPageMap;
}

bool EnsureCOWPageMapElementAllocated(LONG* elem)
{
    _ASSERTE(elem > g_pCOWPageMap);
    _ASSERTE(g_pCOWPageMap != NULL);

    size_t offset = (size_t)elem - (size_t)g_pCOWPageMap;
    size_t page = offset / GetOsPageSize();
    
    _ASSERTE(page < 32);
    int bit = (int)(1 << page);
    
    if (!(g_COWPageMapMap & bit))
    {
        if (!VirtualAlloc(elem, 1, MEM_COMMIT, PAGE_READWRITE))
            return false;

        InterlockedOr(&g_COWPageMapMap, bit);
    }

    return true;
}

bool IsCOWPageMapElementAllocated(LONG* elem)
{
    _ASSERTE(elem >= g_pCOWPageMap);
    _ASSERTE(g_pCOWPageMap != NULL);

    size_t offset = (size_t)elem - (size_t)g_pCOWPageMap;
    size_t page = offset / GetOsPageSize();
    
    _ASSERTE(page < 32);
    int bit = (int)(1 << page);
    
    return (g_COWPageMapMap & bit) != 0;
}

bool SetCOWPageBits(BYTE* pStart, size_t len, bool value)
{
    _ASSERTE(len > 0);

    // we don't need a barrier here, since:
    //  a) all supported hardware maintains ordering of dependent reads
    //  b) it's ok if additional reads happen, because this never changes
    //     once initialized.
    LONG* pCOWPageMap = g_pCOWPageMap;

    //
    // Write the bits in 32-bit chunks, to avoid doing one interlocked instruction for each bit.
    //
    size_t page = (size_t)pStart / GetOsPageSize();
    size_t lastPage = (size_t)(pStart+len-1) / GetOsPageSize();
    size_t elem = page / 32;
    LONG bits = 0;
    do
    {
        bits |= 1 << (page % 32);

        ++page;

        //
        // if we've moved to a new element of the map, or we've covered every page,
        // we need to write out the already-accumulated element.
        //
        size_t newElem = page / 32;
        if (page > lastPage || newElem != elem)
        {
            LONG* pElem = &pCOWPageMap[elem];
            if (!EnsureCOWPageMapElementAllocated(pElem))
                return false;

            if (value)
                InterlockedOr(&pCOWPageMap[elem], bits);
            else
                InterlockedAnd(&pCOWPageMap[elem], ~bits);

            elem = newElem;
            bits = 0;
        }
    }
    while (page <= lastPage);

    return true;
}

bool SetCOWPageBitsForImage(PEDecoder * pImage, bool value)
{
    if (!pImage->HasNativeHeader())
        return true;

    bool success = true;

    IMAGE_SECTION_HEADER* pSection;
    BYTE * pStart;
    size_t len;

    pSection = pImage->FindSection(".data");
    if (pSection != NULL)
    {
        pStart = (BYTE*) dac_cast<TADDR>(pImage->GetBase()) + pSection->VirtualAddress;
        len = pSection->Misc.VirtualSize;

        if (!SetCOWPageBits(pStart, len, value))
            success = false;
    }

    pSection = pImage->FindSection(".xdata");
    if (pSection != NULL)
    {
        pStart = (BYTE*) dac_cast<TADDR>(pImage->GetBase()) + pSection->VirtualAddress;
        len = pSection->Misc.VirtualSize;

        if (!SetCOWPageBits(pStart, len, value))
            success = false;
    }

    return success;
}

bool AreAnyCOWPageBitsSet(BYTE* pStart, size_t len)
{
    LONG* pCOWPageMap = g_pCOWPageMap;
    if (pCOWPageMap == NULL)
        return false;

    _ASSERTE(len > 0);
    size_t page = (size_t)pStart / GetOsPageSize();
    size_t lastPage = (size_t)(pStart+len-1) / GetOsPageSize();
    do
    {
        LONG* pElem = &pCOWPageMap[page / 32];
        if (IsCOWPageMapElementAllocated(pElem) && 
            (*pElem & (1 << (page %32))))
        {
            return true;
        }
        ++page;
    }
    while (page <= lastPage);

    return false;
}


void AllocateLazyCOWPages(PEDecoder * pImage)
{
    //
    // Note: it's ok to call AllocateLazyCOWPages multiple times for the same loaded image.
    // This will result in any already-writable pages being incorrectly marked as read-only
    // in our records, but that just means we'll call VirtualProtect one more time.
    //
    // However, FreeLazyCOWPages must be called just once per loaded image, when it is actually.  
    // unloaded.  Otherwise we will lose track of the COW pages in that image while it might 
    // still be accessible.
    //

    if (!EnsureCOWPageMapAllocated())
        ThrowOutOfMemory();

    if (!SetCOWPageBitsForImage(pImage, true))
        ThrowOutOfMemory();
}

void FreeLazyCOWPages(PEDecoder * pImage)
{
    //
    // Must be called just once per image; see note in AllocateLazyCOWPages.
    //
    SetCOWPageBitsForImage(pImage, false);
}

bool IsInReadOnlyLazyCOWPage(void* p)
{
    return AreAnyCOWPageBitsSet((BYTE*)p, 1);
}


bool MakeWritable(BYTE* pStart, size_t len, DWORD protect)
{
    DWORD oldProtect;
    if (!VirtualProtect(pStart, len, protect, &oldProtect))
        return false;
    INDEBUG(bool result = ) SetCOWPageBits(pStart, len, false);
    _ASSERTE(result); // we already set these, so we must be able to clear them.
    return true;
}

bool EnsureWritablePagesNoThrow(void* p, size_t len)
{
    BYTE* pStart = (BYTE*)p;

    if (len == 0)
        return true;

    if (!AreAnyCOWPageBitsSet(pStart, len))
        return true;

    return MakeWritable(pStart, len, PAGE_READWRITE);
}

void EnsureWritablePages(void* p, size_t len)
{
    CONTRACTL {
        THROWS;
    } CONTRACTL_END;

    BYTE* pStart = (BYTE*)p;

    if (len == 0)
        return;

    if (!AreAnyCOWPageBitsSet(pStart, len))
        return;

    if (!MakeWritable(pStart, len, PAGE_READWRITE))
        ThrowOutOfMemory();
}

bool EnsureWritableExecutablePagesNoThrow(void* p, size_t len)
{
    BYTE* pStart = (BYTE*) p;

    if (len == 0)
        return true;

    if (!AreAnyCOWPageBitsSet(pStart, len))
        return true;

    return MakeWritable(pStart, len, PAGE_EXECUTE_READWRITE);
}

void EnsureWritableExecutablePages(void* p, size_t len)
{
    CONTRACTL {
        THROWS;
    } CONTRACTL_END;

    BYTE* pStart = (BYTE*) p;

    if (len == 0)
        return;

    if (!AreAnyCOWPageBitsSet(pStart, len))
        return;

    if (!MakeWritable(pStart, len, PAGE_EXECUTE_READWRITE))
        ThrowOutOfMemory();
}

#endif // FEATURE_LAZY_COW_PAGES


