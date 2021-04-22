// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "utilcode.h"
#include "corjit.h"
#include "jithost.h"

void* JitHost::allocateMemory(size_t size)
{
    WRAPPER_NO_CONTRACT;

    return new BYTE[size];
}

void JitHost::freeMemory(void* block)
{
    WRAPPER_NO_CONTRACT;

    delete [] (BYTE*)block;
}

int JitHost::getIntConfigValue(const WCHAR* name, int defaultValue)
{
    WRAPPER_NO_CONTRACT;

    // Translate JIT call into runtime configuration query
    CLRConfig::ConfigDWORDInfo info{ name, (DWORD)defaultValue, CLRConfig::LookupOptions::Default };

    // Perform a CLRConfig look up on behalf of the JIT.
    return CLRConfig::GetConfigValue(info);
}

const WCHAR* JitHost::getStringConfigValue(const WCHAR* name)
{
    WRAPPER_NO_CONTRACT;

    // Translate JIT call into runtime configuration query
    CLRConfig::ConfigStringInfo info{ name, CLRConfig::LookupOptions::Default };

    // Perform a CLRConfig look up on behalf of the JIT.
    return CLRConfig::GetConfigValue(info);
}

void JitHost::freeStringConfigValue(const WCHAR* value)
{
    WRAPPER_NO_CONTRACT;

    CLRConfig::FreeConfigString(const_cast<WCHAR*>(value));
}

//
// Pool memory blocks for JIT to avoid frequent commit/decommit. The frequent commit/decommit has been
// shown to slow down the JIT significantly (10% or more). The memory blocks used by the JIT tend to be too big
// to be covered by pooling done by the default malloc.
//
// - Keep up to some limit worth of memory, with loose affinization of memory blocks to threads.
// - On finalizer thread, release the extra memory that was not used recently.
//

void* JitHost::allocateSlab(size_t size, size_t* pActualSize)
{
    size = max(size, sizeof(Slab));

    Thread* pCurrentThread = GetThreadNULLOk();
    if (m_pCurrentCachedList != NULL || m_pPreviousCachedList != NULL)
    {
        CrstHolder lock(&m_jitSlabAllocatorCrst);
        Slab** ppCandidate = NULL;

        for (Slab ** ppList = &m_pCurrentCachedList; *ppList != NULL; ppList = &(*ppList)->pNext)
        {
            Slab* p = *ppList;
            if (p->size >= size && p->size <= 4 * size) // Avoid wasting more than 4x memory
            {
                ppCandidate = ppList;
                if (p->affinity == pCurrentThread)
                    break;
            }
        }

        if (ppCandidate == NULL)
        {
            for (Slab ** ppList = &m_pPreviousCachedList; *ppList != NULL; ppList = &(*ppList)->pNext)
            {
                Slab* p = *ppList;
                if (p->size == size) // Allocation from previous list requires exact match
                {
                    ppCandidate = ppList;
                    if (p->affinity == pCurrentThread)
                        break;
                }
            }
        }

        if (ppCandidate != NULL)
        {
            Slab* p = *ppCandidate;
            *ppCandidate = p->pNext;

            m_totalCached -= p->size;
            *pActualSize = p->size;

            return p;
        }
    }

    *pActualSize = size;
    return new BYTE[size];
}

void JitHost::freeSlab(void* slab, size_t actualSize)
{
    _ASSERTE(actualSize >= sizeof(Slab));

    if (actualSize < 0x100000) // Do not cache blocks that are more than 1MB
    {
        CrstHolder lock(&m_jitSlabAllocatorCrst);

        if (m_totalCached < g_pConfig->JitHostMaxSlabCache()) // Do not cache more than maximum allowed value
        {
            m_totalCached += actualSize;

            Slab* pSlab = (Slab*)slab;
            pSlab->size = actualSize;
            pSlab->affinity = GetThreadNULLOk();
            pSlab->pNext = m_pCurrentCachedList;
            m_pCurrentCachedList = pSlab;
            return;
        }
    }

    delete [] (BYTE*)slab;
}

void JitHost::init()
{
    m_jitSlabAllocatorCrst.Init(CrstLeafLock);
}

void JitHost::reclaim()
{
    if (m_pCurrentCachedList != NULL || m_pPreviousCachedList != NULL)
    {
        DWORD ticks = ::GetTickCount();

        if (m_lastFlush == 0) // Just update m_lastFlush first time around
        {
            m_lastFlush = ticks;
            return;
        }

        if ((DWORD)(ticks - m_lastFlush) < 2000) // Flush the free lists every 2 seconds
            return;
        m_lastFlush = ticks;

        // Flush all slabs in m_pPreviousCachedList
        for (;;)
        {
            Slab* slabToDelete = NULL;

            {
                CrstHolder lock(&m_jitSlabAllocatorCrst);
                slabToDelete = m_pPreviousCachedList;
                if (slabToDelete == NULL)
                {
                    m_pPreviousCachedList = m_pCurrentCachedList;
                    m_pCurrentCachedList = NULL;
                    break;
                }
                m_totalCached -= slabToDelete->size;
                m_pPreviousCachedList = slabToDelete->pNext;
            }

            delete [] (BYTE*)slabToDelete;
        }
    }
}

JitHost JitHost::s_theJitHost;
