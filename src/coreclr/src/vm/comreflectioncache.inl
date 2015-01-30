//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef __COMReflectionCache_inl__
#define __COMReflectionCache_inl__

#ifndef DACCESS_COMPILE

template <class Element, class CacheType, int CacheSize>
void ReflectionCache<Element, CacheType, CacheSize>::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pResult = (CacheTable *)(void *) ::GetAppDomain()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(CacheSize) * S_SIZE_T(sizeof(CacheTable)));
    m_pHashTable = (HashTable *)(void *) ::GetAppDomain()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(CacheSize) * S_SIZE_T(sizeof(HashTable)));


    for (int i = 0; i < CacheSize; i ++)
        m_pHashTable[i].slot = -1;
}

#endif //!DACCESS_COMPILE

#endif // __COMReflectionCache_inl__
