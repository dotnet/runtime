// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Hash table associated with each module that records for all types defined in that module the mapping
// between type name and token (or TypeHandle).
//

#ifndef __TYPEEQUIVALENCE_HASH_INCLUDED
#define __TYPEEQUIVALENCE_HASH_INCLUDED

#ifdef FEATURE_TYPEEQUIVALENCE

#include "ngenhash.h"

// The type of each entry in the hash.
typedef DPTR(struct TypeEquivalenceEntry) PTR_TypeEquivalenceEntry;
struct TypeEquivalenceEntry
{
    static NgenHashValue HashTypeHandles(TypeHandle thA, TypeHandle thB)
    {
        LIMITED_METHOD_CONTRACT;

        UINT_PTR aPtr = thA.AsTAddr();
        UINT_PTR bPtr = thB.AsTAddr();
        DWORD hash = (DWORD)((aPtr + bPtr) >> 3);

        return hash;
    }

    bool Match(TypeHandle thA, TypeHandle thB)
    {
        LIMITED_METHOD_CONTRACT;

        return (((thA == m_thA) && (thB == m_thB))
                || ((thB == m_thA) && (thA == m_thB)));
    }

    void SetData(TypeHandle thA, TypeHandle thB, bool fEquivalent)
    {
        LIMITED_METHOD_CONTRACT;

        m_thA = thA;
        m_thB = thB;
        m_fEquivalent = fEquivalent;
    }

    bool GetEquivalence()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fEquivalent;
    }

private:
    TypeHandle m_thA;
    TypeHandle m_thB;
    bool       m_fEquivalent;
};

// The hash type itself. All common logic is provided by the NgenHashTable templated base class. See
// NgenHash.h for details.
typedef DPTR(class TypeEquivalenceHashTable) PTR_TypeEquivalenceHashTable;
class TypeEquivalenceHashTable : public NgenHashTable<TypeEquivalenceHashTable, TypeEquivalenceEntry, 4>
{
    friend class NgenHashTable<TypeEquivalenceHashTable, TypeEquivalenceEntry, 4>;
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

public:
    enum EquivalenceMatch
    {
        MatchUnknown,
        Match,
        NoMatch
    };

    // The LookupContext type we export to track GetValue/FindNextNestedClass enumerations is simply a rename
    // of the base classes' hash value enumerator.
    typedef NgenHashTable<TypeEquivalenceHashTable, TypeEquivalenceEntry, 4>::LookupContext LookupContext;

#ifndef DACCESS_COMPILE
    static TypeEquivalenceHashTable *Create(AppDomain *pDomain, DWORD dwNumBuckets, CrstExplicitInit *pCrst);
    void RecordEquivalence(TypeHandle thA, TypeHandle thB, EquivalenceMatch match);
#endif
    EquivalenceMatch CheckEquivalence(TypeHandle thA, TypeHandle thB);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegionsForEntry(TypeEquivalenceEntry *pEntry, CLRDataEnumMemoryFlags flags) { return; }
#endif

private:
#ifndef DACCESS_COMPILE
    TypeEquivalenceHashTable(LoaderHeap *pHeap, DWORD cInitialBuckets, CrstExplicitInit *pCrst)
        : NgenHashTable<TypeEquivalenceHashTable, TypeEquivalenceEntry, 4>(NULL, pHeap, cInitialBuckets)
        , m_pHashTableCrst(pCrst)
    {
    }
#endif // DACCESS_COMPILE

    CrstExplicitInit*   m_pHashTableCrst;
};

#endif // FEATURE_TYPEEQUIVALENCE
#endif // !__TYPEEQUIVALENCE_HASH_INCLUDED
