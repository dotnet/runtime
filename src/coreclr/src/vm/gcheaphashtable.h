// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef GCHEAPHASHTABLE_H
#define GCHEAPHASHTABLE_H

class GCHeapHashObject;

template <bool removeSupported>
struct DefaultGCHeapHashTraits
{
    typedef PTRARRAYREF THashArrayType;
    static const INT32 s_growth_factor_numerator = 3;
    static const INT32 s_growth_factor_denominator = 2;

    static const INT32 s_density_factor_numerator = 3;
    static const INT32 s_density_factor_denominator = 4;

    static const INT32 s_densitywithdeletes_factor_numerator = 7;
    static const INT32 s_densitywithdeletes_factor_denominator = 8;

    static const INT32 s_minimum_allocation = 7;

    static bool IsNull(PTRARRAYREF arr, INT32 index);
    static bool IsDeleted(PTRARRAYREF arr, INT32 index, GCHEAPHASHOBJECTREF gcHeap);
#ifndef DACCESS_COMPILE
    static THashArrayType AllocateArray(INT32 size);
#endif

    // Not a part of the traits api, but used to allow derived traits to save on code
    static OBJECTREF GetValueAtIndex(GCHEAPHASHOBJECTREF *pgcHeap, INT32 index);

#ifndef DACCESS_COMPILE
    static void CopyValue(THashArrayType srcArray, INT32 indexSrc, THashArrayType destinationArray, INT32 indexDest);
    static void DeleteEntry(GCHEAPHASHOBJECTREF *pgcHeap, INT32 index);
#endif // !DACCESS_COMPILE

    template<class TElement>
    static void GetElement(GCHEAPHASHOBJECTREF *pgcHeap, INT32 index, TElement& foundElement);

#ifndef DACCESS_COMPILE
    template<class TElement>
    static void SetElement(GCHEAPHASHOBJECTREF *pgcHeap, INT32 index, TElement& foundElement);
#endif // !DACCESS_COMPILE
};

template <class PtrTypeKey, bool supports_remove>
struct GCHeapHashTraitsPointerToPointerList : public DefaultGCHeapHashTraits<supports_remove>
{
    static INT32 Hash(PtrTypeKey *pValue);
    static INT32 Hash(PTRARRAYREF arr, INT32 index);
    static bool DoesEntryMatchKey(PTRARRAYREF arr, INT32 index, PtrTypeKey *pKey);
};


// GCHeapHash is based on the logic of SHash, and utilizes the same basic structure (which allows the key/value
// to be one and the same, or other interesting memory tweaks.) To avoid GC pointer issues, responsibility for allocating
// the underlying arrays and manipulating the entries is entirely extracted to the traits class, and responsibility
// for creation of elements is deferred into the caller of the add function. (See example uses in CrossLoaderAllocatorHash)
// As the GCHeapHash is actually a managed object, but the code for manipulating the hash is written here in native code,
// allocating an instance of this class does not actually allocate a hashtable. Instead, the hashtable is allocated by
// allocating an instance of the GCHeapHash type, and then passing the allocated object into this type's constructor to
// assign the value. This class is designed to be used protected within a GC_PROTECT region. See examples in CrossLoaderAllocatorHash.
template <class TRAITS>
class GCHeapHash
{
    GCHEAPHASHOBJECTREF m_gcHeapHash;

    typedef typename TRAITS::THashArrayType THashArrayType;
    typedef INT32 count_t;

    private:
    // Insert into hashtable without growing. GCHEAPHASHOBJECTREF must be GC protected as must be TKey if needed
    template<class TKey, class TValueSetter>
    void Insert(TKey *pKey, const TValueSetter &valueSetter);
    void CheckGrowth();
    void Grow();
    THashArrayType Grow_OnlyAllocateNewTable();

    bool IsPrime(count_t number);
    count_t NextPrime(count_t number);

    void ReplaceTable(THashArrayType newTable);

    template<class TKey>
    count_t CallHash(TKey* pValue)
    {
        WRAPPER_NO_CONTRACT;

        count_t hash = TRAITS::Hash(pValue);
        hash = hash < 0 ? -hash : hash;
        if (hash < 0)
            return 1;
        else
            return hash;
    }

    count_t CallHash(THashArrayType arr, count_t index)
    {
        WRAPPER_NO_CONTRACT;

        count_t hash = TRAITS::Hash(arr, index);
        hash = hash < 0 ? -hash : hash;
        if (hash < 0)
            return 1;
        else
            return hash;
    }

    public:

    template<class TVisitor>
    bool VisitAllEntryIndices(TVisitor &visitor);

    template<class TKey, class TValueSetter>
    void Add(TKey *pKey, const TValueSetter &valueSetter);

    // Get the index in the hashtable of the value which matches key, or -1 if there are no matches
    template<class TKey>
    INT32 GetValueIndex(TKey *pKey);

    template<class TElement>
    void GetElement(INT32 index, TElement& foundElement);

    // Use this to update an value within the hashtable directly.
    // It is ONLY safe to do if the index already points at an element
    // which already exists and has the same key as the newElementValue
    template<class TElement>
    void SetElement(INT32 index, TElement& newElementValue);

    template<class TKey>
    void DeleteEntry(TKey *pKey);

    GCHEAPHASHOBJECTREF GetGCHeapRef() { LIMITED_METHOD_CONTRACT; return m_gcHeapHash; }

    GCHeapHash(GCHEAPHASHOBJECTREF gcHeap) : m_gcHeapHash(gcHeap) {}
    GCHeapHash(OBJECTREF gcHeap) : m_gcHeapHash((GCHEAPHASHOBJECTREF)gcHeap) {}
    GCHeapHash() : m_gcHeapHash((GCHEAPHASHOBJECTREF)TADDR(NULL)) {}
};

#endif // GCHEAPHASHTABLE_H
