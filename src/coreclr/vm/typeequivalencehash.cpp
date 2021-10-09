// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Hash table associated with each module that records for all types defined in that module the mapping
// between type name and token (or TypeHandle).
//

#include "common.h"
#include "typeequivalencehash.hpp"
#include "dacenumerablehash.inl"

#ifdef FEATURE_TYPEEQUIVALENCE
TypeEquivalenceHashTable::EquivalenceMatch TypeEquivalenceHashTable::CheckEquivalence(TypeHandle thA, TypeHandle thB)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    LookupContext lookupContext;
    DacEnumerableHashValue hash = TypeEquivalenceEntry::HashTypeHandles(thA, thB);

    PTR_TypeEquivalenceEntry search = BaseFindFirstEntryByHash(hash, &lookupContext);
    while (search != NULL)
    {
        if (search->Match(thA, thB))
        {
            return search->GetEquivalence() ? Match : NoMatch;
        }

        search = BaseFindNextEntryByHash(&lookupContext);
    }
    return MatchUnknown;
}

#ifndef DACCESS_COMPILE
/*static*/
TypeEquivalenceHashTable *TypeEquivalenceHashTable::Create(AppDomain *pAppDomain, DWORD dwNumBuckets, CrstExplicitInit *pCrst)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    AllocMemTracker amt;
    LoaderHeap *pHeap = pAppDomain->GetLowFrequencyHeap();
    TypeEquivalenceHashTable *pThis = (TypeEquivalenceHashTable*)amt.Track(pHeap->AllocMem((S_SIZE_T)sizeof(TypeEquivalenceHashTable)));

    // The base class gets initialized through chaining of constructors.
    // Use in-place new to create instance.
    new (pThis) TypeEquivalenceHashTable(pHeap, dwNumBuckets, pCrst);
    amt.SuppressRelease();

    return pThis;
}

void TypeEquivalenceHashTable::RecordEquivalence(TypeHandle thA, TypeHandle thB, TypeEquivalenceHashTable::EquivalenceMatch match)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(match != TypeEquivalenceHashTable::MatchUnknown);
    }
    CONTRACTL_END;

    CrstHolder ch(m_pHashTableCrst);

    // Was there a race in calculating equivalence and this thread lost?
    // If so, return
    EquivalenceMatch checkedMatch = CheckEquivalence(thA, thB);
    if (checkedMatch != TypeEquivalenceHashTable::MatchUnknown)
    {
        _ASSERTE(checkedMatch == match);
        return;
    }

    AllocMemTracker amt;
    PTR_TypeEquivalenceEntry pNewEntry = BaseAllocateEntry(&amt);
    amt.SuppressRelease();

    pNewEntry->SetData(thA, thB, match == TypeEquivalenceHashTable::Match ? true : false);
    DacEnumerableHashValue hash = TypeEquivalenceEntry::HashTypeHandles(thA, thB);

    BaseInsertEntry(hash, pNewEntry);
}
#endif // !DACCESS_COMPILE
#endif // FEATURE_TYPEEQUIVALENCE

