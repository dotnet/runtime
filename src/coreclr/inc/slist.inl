// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of SList methods that depend on platform atomics.
//

#ifndef _H_SLIST_INL_
#define _H_SLIST_INL_

#include "slist.h"

#if defined(FEATURE_NATIVEAOT)
#include "Pal.h"            // PalInterlockedCompareExchangePointer
#else
#include "utilcode.h"       // InterlockedCompareExchangeT
#endif

template <typename T, typename Traits>
void SList<T, Traits>::InsertHeadInterlocked(PTR_T pItem)
{
    static_assert(!Traits::HasTail, "InsertHeadInterlocked is incompatible with tail tracking");
    static_assert(Traits::IsInterlocked, "InsertHeadInterlocked requires SListMode::Interlocked");
    _ASSERTE(pItem != NULL);

    for (;;)
    {
        *Traits::GetNextPtr(pItem) = m_pHead;
        T* expected = (T*)*Traits::GetNextPtr(pItem);
#if defined(FEATURE_NATIVEAOT)
        if (static_cast<T*>(PalInterlockedCompareExchangePointer(
                reinterpret_cast<void * volatile *>(&m_pHead),
                reinterpret_cast<void *>(pItem),
                reinterpret_cast<void *>(expected))) == expected)
#else
        if (InterlockedCompareExchangeT(
                reinterpret_cast<T * volatile *>(&m_pHead),
                (T*)pItem,
                expected) == expected)
#endif
        {
            break;
        }
    }
}

#endif // _H_SLIST_INL_
