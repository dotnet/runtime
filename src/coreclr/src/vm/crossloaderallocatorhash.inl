// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CROSSLOADERALLOCATORHASH_INL
#define CROSSLOADERALLOCATORHASH_INL
#ifdef CROSSLOADERALLOCATORHASH_H
#ifndef CROSSGEN_COMPILE

#include "gcheaphashtable.inl"

template <class TKey_, class TValue_>
/*static*/ DWORD NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>::ComputeUsedEntries(OBJECTREF *pKeyValueStore, DWORD *pEntriesInArrayTotal)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DWORD entriesInArrayTotal = (((I1ARRAYREF)*pKeyValueStore)->GetNumComponents() - sizeof(TKey))/sizeof(TValue);
    DWORD usedEntries;
    TValue* pStartOfValuesData = (TValue*)(((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements() + sizeof(TKey));

    if (entriesInArrayTotal == 0)
    {
        usedEntries = 0;
    }
    else if ((entriesInArrayTotal >= 2) && (pStartOfValuesData[entriesInArrayTotal - 2] == (TValue)0))
    {
        usedEntries = (DWORD)(SIZE_T)pStartOfValuesData[entriesInArrayTotal - 1];
    }
    else if (pStartOfValuesData[entriesInArrayTotal - 1] == (TValue)0)
    {
        usedEntries = entriesInArrayTotal - 1;
    }
    else
    {
        usedEntries = entriesInArrayTotal;
    }

    *pEntriesInArrayTotal = entriesInArrayTotal;
    return usedEntries;
}

#ifndef DACCESS_COMPILE
template <class TKey_, class TValue_>
/*static*/ void NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>::SetUsedEntries(TValue* pStartOfValuesData, DWORD entriesInArrayTotal, DWORD usedEntries)
{
    if (usedEntries < entriesInArrayTotal)
    {
        if (usedEntries == (entriesInArrayTotal - 1))
        {
            pStartOfValuesData[entriesInArrayTotal - 1] = (TValue)0;
        }
        else
        {
            pStartOfValuesData[entriesInArrayTotal - 1] = (TValue)((INT_PTR)usedEntries);
            pStartOfValuesData[entriesInArrayTotal - 2] = (TValue)0;
        }
    }
}

template <class TKey_, class TValue_>
/*static*/ bool NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>::AddToValuesInHeapMemory(OBJECTREF *pKeyValueStore, const TKey& key, const TValue& value)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    static_assert(sizeof(TKey)==sizeof(TValue), "Assume keys and values are the same size");

    bool updatedKeyValueStore = false;

    if (*pKeyValueStore == NULL)
    {
        *pKeyValueStore = AllocatePrimitiveArray(ELEMENT_TYPE_I1, IsNull(value) ? sizeof(TKey) : sizeof(TKey) + sizeof(TValue));
        updatedKeyValueStore = true;
        TKey* pKeyLoc = (TKey*)((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements();
        *pKeyLoc = key;
        if (!IsNull(value))
        {
            TValue* pValueLoc = (TValue*)(((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements() + sizeof(TKey));
            *pValueLoc = value;
        }
    }
    else if (!IsNull(value))
    {
        DWORD entriesInArrayTotal;
        DWORD usedEntries = ComputeUsedEntries(pKeyValueStore, &entriesInArrayTotal);

        if (usedEntries == entriesInArrayTotal)
        {
            // There isn't free space. Build a new, bigger array with the existing data
            DWORD newSize;
            if (usedEntries < 8)
                newSize = usedEntries + 1; // Grow very slowly initially. The cost of allocation/copy is cheap, and this holds very tight on memory usage
            else
                newSize = usedEntries * 2;

            if (newSize < usedEntries)
                COMPlusThrow(kOverflowException);

            // Allocate the new array.
            I1ARRAYREF newKeyValueStore = (I1ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_I1, newSize*sizeof(TValue) + sizeof(TKey));

            // Since, AllocatePrimitiveArray may have triggered a GC, recapture all data pointers from GC objects
            void* pStartOfNewArray = newKeyValueStore->GetDirectPointerToNonObjectElements();
            void* pStartOfOldArray = ((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements();

            memcpyNoGCRefs(pStartOfNewArray, pStartOfOldArray, ((I1ARRAYREF)*pKeyValueStore)->GetNumComponents());

            *pKeyValueStore = (OBJECTREF)newKeyValueStore;
            updatedKeyValueStore = true;

            entriesInArrayTotal = newSize;
        }

        // There is free space. Append on the end
        TValue* pStartOfValuesData = (TValue*)(((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements() + sizeof(TKey));
        SetUsedEntries(pStartOfValuesData, entriesInArrayTotal, usedEntries + 1);
        pStartOfValuesData[usedEntries] = value;
    }

    return updatedKeyValueStore;
}
#endif //!DACCESS_COMPILE

template <class TKey_, class TValue_>
/*static*/ TKey_ NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>::ReadKeyFromKeyValueStore(OBJECTREF *pKeyValueStore)
{
    WRAPPER_NO_CONTRACT;

    TKey* pKeyLoc = (TKey*)((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements();
    return *pKeyLoc;
}

template <class TKey_, class TValue_>
template <class Visitor>
/*static*/ bool NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>::VisitKeyValueStore(OBJECTREF *pLoaderAllocatorRef, OBJECTREF *pKeyValueStore, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    DWORD entriesInArrayTotal;
    DWORD usedEntries = ComputeUsedEntries(pKeyValueStore, &entriesInArrayTotal);

    for (DWORD index = 0; index < usedEntries; ++index)
    {
        // Capture pKeyLoc and pStartOfValuesData inside of loop, as we aren't protecting these pointers into the GC heap, so they
        // are not permitted to live across the call to visitor (in case visitor triggers a GC)
        TKey* pKeyLoc = (TKey*)((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements();
        TValue* pStartOfValuesData = (TValue*)(((I1ARRAYREF)*pKeyValueStore)->GetDirectPointerToNonObjectElements() + sizeof(TKey));

        if (!visitor(*pLoaderAllocatorRef, *pKeyLoc, pStartOfValuesData[index]))
        {
            return false;
        }
    }

    return true;
}

#ifndef DACCESS_COMPILE
template <class TKey_, class TValue_>
/*static*/ void DefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>::DeleteValueInHeapMemory(OBJECTREF keyValueStore, const TValue& value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // TODO: Consider optimizing this by changing the add to ensure that the
    // values list is sorted, and then doing a binary search for the value instead
    // of the linear search

    DWORD entriesInArrayTotal;
    DWORD usedEntries = NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey,TValue>::ComputeUsedEntries(&keyValueStore, &entriesInArrayTotal);
    TValue* pStartOfValuesData = (TValue*)(((I1ARRAYREF)keyValueStore)->GetDirectPointerToNonObjectElements() + sizeof(TKey));

    for (DWORD iEntry = 0; iEntry < usedEntries; iEntry++)
    {
        if (pStartOfValuesData[iEntry] == value)
        {
            memmove(pStartOfValuesData + iEntry, pStartOfValuesData + iEntry + 1, (usedEntries - iEntry - 1) * sizeof(TValue));
            SetUsedEntries(pStartOfValuesData, entriesInArrayTotal, usedEntries - 1);
            return;
        }
    }
}
#endif //!DACCESS_COMPILE

/*static*/ inline INT32 GCHeapHashDependentHashTrackerHashTraits::Hash(PtrTypeKey *pValue)
{
    LIMITED_METHOD_CONTRACT;
    return (INT32)(SIZE_T)*pValue;
}

/*static*/ inline INT32 GCHeapHashDependentHashTrackerHashTraits::Hash(PTRARRAYREF arr, INT32 index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    LAHASHDEPENDENTHASHTRACKERREF value = (LAHASHDEPENDENTHASHTRACKERREF)arr->GetAt(index);
    LoaderAllocator *pLoaderAllocator = value->GetLoaderAllocatorUnsafe();
    return Hash(&pLoaderAllocator);
}

/*static*/ inline bool GCHeapHashDependentHashTrackerHashTraits::DoesEntryMatchKey(PTRARRAYREF arr, INT32 index, PtrTypeKey *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    LAHASHDEPENDENTHASHTRACKERREF value = (LAHASHDEPENDENTHASHTRACKERREF)arr->GetAt(index);

    return value->IsTrackerFor(*pKey);
}

/*static*/ inline bool GCHeapHashDependentHashTrackerHashTraits::IsDeleted(PTRARRAYREF arr, INT32 index, GCHEAPHASHOBJECTREF gcHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF valueInHeap = arr->GetAt(index);

    if (valueInHeap == NULL)
        return false;

    if (gcHeap == valueInHeap)
        return true;

    // This is a tricky bit of logic used which detects freed loader allocators lazily
    // and deletes them from the GCHeapHash while looking up or otherwise walking the hashtable
    // for any purpose.
    LAHASHDEPENDENTHASHTRACKERREF value = (LAHASHDEPENDENTHASHTRACKERREF)valueInHeap;
    if (!value->IsLoaderAllocatorLive())
    {
#ifndef DACCESS_COMPILE
        arr->SetAt(index, gcHeap);
        gcHeap->DecrementCount(true);
#endif // DACCESS_COMPILE

        return true;
    }

    return false;
}

template<class TRAITS>
template <class TKey>
/*static*/ INT32 KeyToValuesGCHeapHashTraits<TRAITS>::Hash(TKey *pValue)
{
    LIMITED_METHOD_CONTRACT;
    return (INT32)(SIZE_T)*pValue;
}

template<class TRAITS>
/*static*/ inline INT32 KeyToValuesGCHeapHashTraits<TRAITS>::Hash(PTRARRAYREF arr, INT32 index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF hashKeyEntry = arr->GetAt(index);
    LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
    OBJECTREF keyValueStore;

    if (hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
    {
        hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)hashKeyEntry;
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = hashKeyEntry;
    }

    typename TRAITS::TKey key = TRAITS::ReadKeyFromKeyValueStore(&keyValueStore);
    return Hash(&key);
}

template<class TRAITS>
template<class TKey>
/*static*/ bool KeyToValuesGCHeapHashTraits<TRAITS>::DoesEntryMatchKey(PTRARRAYREF arr, INT32 index, TKey *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF hashKeyEntry = arr->GetAt(index);
    LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
    OBJECTREF keyValueStore;

    if (hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
    {
        hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)hashKeyEntry;
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = hashKeyEntry;
    }

    TKey key = TRAITS::ReadKeyFromKeyValueStore(&keyValueStore);

    return key == *pKey;
}

#ifndef DACCESS_COMPILE
template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::Add(TKey key, TValue value, LoaderAllocator *pLoaderAllocatorOfValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;


    struct {
        KeyToValuesGCHeapHash keyToTrackersHash;
        KeyToValuesGCHeapHash keyToValuePerLAHash;
        OBJECTREF keyValueStore;
        OBJECTREF hashKeyEntry;
        LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc)
    {
        EnsureManagedObjectsInitted();

        bool addToKeyValuesHash = false;
        // This data structure actually doesn't have this invariant, but it is expected that uses of this
        // data structure will require that the key's loader allocator is the same as that of this data structure.
        _ASSERTE(key->GetLoaderAllocator() == m_pLoaderAllocator);

        gc.keyToTrackersHash = KeyToValuesGCHeapHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_keyToDependentTrackersHash));
        INT32 index = gc.keyToTrackersHash.GetValueIndex(&key);

        if (index == -1)
        {
            addToKeyValuesHash = true;
            TRAITS::AddToValuesInHeapMemory(&gc.keyValueStore, key, pLoaderAllocatorOfValue == m_pLoaderAllocator ? value : TRAITS::NullValue());

            if (pLoaderAllocatorOfValue != m_pLoaderAllocator)
            {
                gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)AllocateObject(CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS));
                SetObjectReference(&gc.hashKeyToTrackers->_laLocalKeyValueStore, gc.keyValueStore);
                gc.hashKeyEntry = gc.hashKeyToTrackers;
            }
            else
            {
                gc.hashKeyEntry = gc.keyValueStore;
            }

            gc.keyToTrackersHash.Add(&key, [&gc](PTRARRAYREF arr, INT32 index)
            {
                arr->SetAt(index, (OBJECTREF)gc.hashKeyEntry);
            });
        }
        else
        {
            gc.keyToTrackersHash.GetElement(index, gc.hashKeyEntry);

            if (gc.hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
            {
                gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)gc.hashKeyEntry;
                gc.keyValueStore = gc.hashKeyToTrackers->_laLocalKeyValueStore;
            }
            else
            {
                gc.keyValueStore = gc.hashKeyEntry;
            }

            bool updatedKeyValueStore = false;

            if (pLoaderAllocatorOfValue == m_pLoaderAllocator)
            {
                updatedKeyValueStore = TRAITS::AddToValuesInHeapMemory(&gc.keyValueStore, key, value);
            }

            if (updatedKeyValueStore)
            {
                if (gc.hashKeyToTrackers != NULL)
                {
                    SetObjectReference(&gc.hashKeyToTrackers->_laLocalKeyValueStore, gc.keyValueStore);
                }
                else
                {
                    gc.hashKeyEntry = gc.keyValueStore;
                    gc.keyToTrackersHash.SetElement(index, gc.hashKeyEntry);
                }
            }
        }

        // If the LoaderAllocator matches, we've finished adding by now, otherwise, we need to get the remove hash and work with that
        if (pLoaderAllocatorOfValue != m_pLoaderAllocator)
        {
            if (gc.hashKeyToTrackers == NULL)
            {
                // Nothing has yet caused the trackers proxy object to be setup. Create it now, and update the keyToTrackersHash
                gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)AllocateObject(CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS));
                SetObjectReference(&gc.hashKeyToTrackers->_laLocalKeyValueStore, gc.keyValueStore);
                gc.hashKeyEntry = gc.hashKeyToTrackers;
                gc.keyToTrackersHash.SetElement(index, gc.hashKeyEntry);
            }

            // Must add it to the cross LA structure
            GCHEAPHASHOBJECTREF gcheapKeyToValue = GetKeyToValueCrossLAHashForHashkeyToTrackers(gc.hashKeyToTrackers, pLoaderAllocatorOfValue);

            gc.keyToValuePerLAHash = KeyToValuesGCHeapHash(gcheapKeyToValue);

            INT32 indexInKeyValueHash = gc.keyToValuePerLAHash.GetValueIndex(&key);
            if (indexInKeyValueHash != -1)
            {
                gc.keyToValuePerLAHash.GetElement(indexInKeyValueHash, gc.keyValueStore);

                if (TRAITS::AddToValuesInHeapMemory(&gc.keyValueStore, key, value))
                {
                    gc.keyToValuePerLAHash.SetElement(indexInKeyValueHash, gc.keyValueStore);
                }
            }
            else
            {
                gc.keyValueStore = NULL;
                TRAITS::AddToValuesInHeapMemory(&gc.keyValueStore, key, value);

                gc.keyToValuePerLAHash.Add(&key, [&gc](PTRARRAYREF arr, INT32 index)
                {
                    arr->SetAt(index, gc.keyValueStore);
                });
            }
        }
    }
    GCPROTECT_END();
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::Remove(TKey key, TValue value, LoaderAllocator *pLoaderAllocatorOfValue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(key->GetLoaderAllocator() == m_pLoaderAllocator);

    if (m_keyToDependentTrackersHash == NULL)
    {
        // If the heap objects haven't been initted, then there is nothing to delete
        return;
    }

    struct {
        KeyToValuesGCHeapHash keyToTrackersHash;
        KeyToValuesGCHeapHash keyToValuePerLAHash;
        OBJECTREF hashKeyEntry;
        LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
        OBJECTREF keyValueStore;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc)
    {
        gc.keyToTrackersHash = KeyToValuesGCHeapHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_keyToDependentTrackersHash));
        INT32 index = gc.keyToTrackersHash.GetValueIndex(&key);

        if (index != -1)
        {
            gc.keyToTrackersHash.GetElement(index, gc.hashKeyEntry);

            if (gc.hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
            {
                gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)gc.hashKeyEntry;
                gc.keyValueStore = gc.hashKeyToTrackers->_laLocalKeyValueStore;
            }
            else
            {
                gc.keyValueStore = gc.hashKeyEntry;
            }

            // Check to see if value can be added to this data structure directly.
            if (m_pLoaderAllocator == pLoaderAllocatorOfValue)
            {
                TRAITS::DeleteValueInHeapMemory(gc.keyValueStore, value);
            }
            else if (gc.hashKeyToTrackers != NULL)
            {
                // Must remove it from the cross LA structure
                GCHEAPHASHOBJECTREF gcheapKeyToValue = GetKeyToValueCrossLAHashForHashkeyToTrackers(gc.hashKeyToTrackers, pLoaderAllocatorOfValue);

                gc.keyToValuePerLAHash = KeyToValuesGCHeapHash(gcheapKeyToValue);

                INT32 indexInKeyValueHash = gc.keyToValuePerLAHash.GetValueIndex(&key);
                if (indexInKeyValueHash != -1)
                {
                    gc.keyToValuePerLAHash.GetElement(indexInKeyValueHash, gc.keyValueStore);
                    TRAITS::DeleteValueInHeapMemory(gc.keyValueStore, value);
                }
            }
        }
    }
    GCPROTECT_END();
}
#endif // !DACCESS_COMPILE

template <class TRAITS>
template <class Visitor>
bool CrossLoaderAllocatorHash<TRAITS>::VisitValuesOfKey(TKey key, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    class VisitIndividualEntryKeyValueHash
    {
        public:
        TKey m_key;
        Visitor *m_pVisitor;
        GCHeapHashDependentHashTrackerHash *m_pDependentTrackerHash;

        VisitIndividualEntryKeyValueHash(TKey key, Visitor *pVisitor,  GCHeapHashDependentHashTrackerHash *pDependentTrackerHash) :
            m_key(key),
            m_pVisitor(pVisitor),
            m_pDependentTrackerHash(pDependentTrackerHash)
            {}

        bool operator()(INT32 index)
        {
            WRAPPER_NO_CONTRACT;

            LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
            m_pDependentTrackerHash->GetElement(index, dependentTracker);
            return VisitTracker(m_key, dependentTracker, *m_pVisitor);
        }
    };

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(key->GetLoaderAllocator() == m_pLoaderAllocator);

    // Check to see that something has been added
    if (m_keyToDependentTrackersHash == NULL)
        return true;

    bool result = true;
    struct
    {
        KeyToValuesGCHeapHash keyToTrackersHash;
        GCHeapHashDependentHashTrackerHash dependentTrackerHash;
        LAHASHDEPENDENTHASHTRACKERREF dependentTrackerMaybe;
        LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
        OBJECTREF hashKeyEntry;
        LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
        OBJECTREF keyValueStore;
        OBJECTREF nullref;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc)
    {
        gc.keyToTrackersHash = KeyToValuesGCHeapHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_keyToDependentTrackersHash));
        INT32 index = gc.keyToTrackersHash.GetValueIndex(&key);
        if (index != -1)
        {
            // We have an entry in the hashtable for the key/dependenthandle.
            gc.keyToTrackersHash.GetElement(index, gc.hashKeyEntry);

            if (gc.hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
            {
                gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)gc.hashKeyEntry;
                gc.keyValueStore = gc.hashKeyToTrackers->_laLocalKeyValueStore;
            }
            else
            {
                gc.keyValueStore = gc.hashKeyEntry;
            }

            // Now gc.hashKeyToTrackers is filled in and keyValueStore

            // visit local entries
            result = VisitKeyValueStore(&gc.nullref, &gc.keyValueStore, visitor);

            if (gc.hashKeyToTrackers != NULL)
            {
                // Is there a single dependenttracker here, or a set.

                if (gc.hashKeyToTrackers->_trackerOrTrackerSet->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHDEPENDENTHASHTRACKER))
                {
                    gc.dependentTracker = (LAHASHDEPENDENTHASHTRACKERREF)gc.hashKeyToTrackers->_trackerOrTrackerSet;
                    result = VisitTracker(key, gc.dependentTracker, visitor);
                }
                else
                {
                    gc.dependentTrackerHash = GCHeapHashDependentHashTrackerHash(gc.hashKeyToTrackers->_trackerOrTrackerSet);
                    VisitIndividualEntryKeyValueHash visitIndivididualKeys(key, &visitor, &gc.dependentTrackerHash);
                    result = gc.dependentTrackerHash.VisitAllEntryIndices(visitIndivididualKeys);
                }
            }
        }
    }
    GCPROTECT_END();

    return result;
}

template <class TRAITS>
template <class Visitor>
bool CrossLoaderAllocatorHash<TRAITS>::VisitAllKeyValuePairs(Visitor &visitor)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    class VisitAllEntryKeyToDependentTrackerHash
    {
        public:
        Visitor *m_pVisitor;
        KeyToValuesGCHeapHash *m_pKeyToTrackerHash;

        VisitAllEntryKeyToDependentTrackerHash(Visitor *pVisitor,  KeyToValuesGCHeapHash *pKeyToTrackerHash) :
            m_pVisitor(pVisitor),
            m_pKeyToTrackerHash(pKeyToTrackerHash)
            {}

        bool operator()(INT32 index)
        {
            WRAPPER_NO_CONTRACT;

            OBJECTREF hashKeyEntry;
            m_pKeyToTrackerHash->GetElement(index, hashKeyEntry);
            return VisitKeyToTrackerAllEntries(hashKeyEntry, *m_pVisitor);
        }
    };

    class VisitAllEntryDependentTrackerHash
    {
        public:
        Visitor *m_pVisitor;
        GCHeapHashDependentHashTrackerHash *m_pDependentTrackerHash;

        VisitAllEntryDependentTrackerHash(Visitor *pVisitor,  GCHeapHashDependentHashTrackerHash *pDependentTrackerHash) :
            m_pVisitor(pVisitor),
            m_pDependentTrackerHash(pDependentTrackerHash)
            {}

        bool operator()(INT32 index)
        {
            WRAPPER_NO_CONTRACT;

            LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
            m_pDependentTrackerHash->GetElement(index, dependentTracker);
            return VisitTrackerAllEntries(dependentTracker, *m_pVisitor);
        }
    };

    struct
    {
        KeyToValuesGCHeapHash keyToTrackersHash;
        GCHeapHashDependentHashTrackerHash dependentTrackerHash;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    bool result = true;
    GCPROTECT_BEGIN(gc)
    {
        if (m_keyToDependentTrackersHash != NULL)
        {
            // Visit all local entries
            gc.keyToTrackersHash = KeyToValuesGCHeapHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_keyToDependentTrackersHash));
            VisitAllEntryKeyToDependentTrackerHash visitAllEntryKeys(&visitor, &gc.keyToTrackersHash);
            result = gc.keyToTrackersHash.VisitAllEntryIndices(visitAllEntryKeys);
        }

        if (m_loaderAllocatorToDependentTrackerHash != NULL)
        {
            // Visit the non-local data
            gc.dependentTrackerHash = GCHeapHashDependentHashTrackerHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_loaderAllocatorToDependentTrackerHash));
            VisitAllEntryDependentTrackerHash visitDependentTrackers(&visitor, &gc.dependentTrackerHash);
            result = gc.dependentTrackerHash.VisitAllEntryIndices(visitDependentTrackers);
        }
    }
    GCPROTECT_END();

    return result;
}

#ifndef DACCESS_COMPILE
template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::RemoveAll(TKey key)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    class DeleteIndividualEntryKeyValueHash
    {
        public:
        TKey m_key;
        GCHeapHashDependentHashTrackerHash *m_pDependentTrackerHash;

        DeleteIndividualEntryKeyValueHash(TKey key, GCHeapHashDependentHashTrackerHash *pDependentTrackerHash) :
            m_key(key),
            m_pDependentTrackerHash(pDependentTrackerHash)
            {}

        bool operator()(INT32 index)
        {
            WRAPPER_NO_CONTRACT;

            LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
            m_pDependentTrackerHash->GetElement(index, dependentTracker);
            DeleteEntryTracker(m_key, dependentTracker);
            return true;
        }
    };

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(key->GetLoaderAllocator() == m_pLoaderAllocator);

    if (m_keyToDependentTrackersHash == NULL)
    {
        return; // Nothing was ever added, so removing all is easy
    }

    struct
    {
        KeyToValuesGCHeapHash keyToTrackersHash;
        GCHeapHashDependentHashTrackerHash dependentTrackerHash;
        LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
        OBJECTREF hashKeyEntry;
        LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
        OBJECTREF keyValueStore;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc)
    {
        gc.keyToTrackersHash = KeyToValuesGCHeapHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_keyToDependentTrackersHash));
        INT32 index = gc.keyToTrackersHash.GetValueIndex(&key);
        if (index != -1)
        {
            // We have an entry in the hashtable for the key/dependenthandle.
            gc.keyToTrackersHash.GetElement(index, gc.hashKeyEntry);

            if (gc.hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
            {
                gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)gc.hashKeyEntry;
                gc.keyValueStore = gc.hashKeyToTrackers->_laLocalKeyValueStore;
            }
            else
            {
                gc.keyValueStore = gc.hashKeyEntry;
            }

            // Now gc.hashKeyToTrackers is filled in

            if (gc.hashKeyToTrackers != NULL)
            {
                // Is there a single dependenttracker here, or a set.

                if (gc.hashKeyToTrackers->_trackerOrTrackerSet->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHDEPENDENTHASHTRACKER))
                {
                    gc.dependentTracker = (LAHASHDEPENDENTHASHTRACKERREF)gc.hashKeyToTrackers->_trackerOrTrackerSet;
                    DeleteEntryTracker(key, gc.dependentTracker);
                }
                else
                {
                    gc.dependentTrackerHash = GCHeapHashDependentHashTrackerHash(gc.hashKeyToTrackers->_trackerOrTrackerSet);
                    DeleteIndividualEntryKeyValueHash deleteIndividualKeyValues(key, &gc.dependentTrackerHash);
                    gc.dependentTrackerHash.VisitAllEntryIndices(deleteIndividualKeyValues);
                }
            }

            // Remove entry from key to tracker hash
            gc.keyToTrackersHash.DeleteEntry(&key);
        }
    }
    GCPROTECT_END();
}
#endif // !DACCESS_COMPILE

template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::Init(LoaderAllocator *pAssociatedLoaderAllocator)
{
    LIMITED_METHOD_CONTRACT;
    m_pLoaderAllocator = pAssociatedLoaderAllocator;
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitKeyValueStore(OBJECTREF *pLoaderAllocatorRef, OBJECTREF *pKeyValueStore, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    return TRAITS::VisitKeyValueStore(pLoaderAllocatorRef, pKeyValueStore, visitor);
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitTracker(TKey key, LAHASHDEPENDENTHASHTRACKERREF trackerUnsafe, Visitor &visitor)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    struct
    {
        LAHASHDEPENDENTHASHTRACKERREF tracker;
        OBJECTREF loaderAllocatorRef;
        GCHEAPHASHOBJECTREF keyToValuesHashObject;
        KeyToValuesGCHeapHash keyToValuesHash;
        OBJECTREF keyValueStore;
    }gc;

    ZeroMemory(&gc, sizeof(gc));
    gc.tracker = trackerUnsafe;

    bool result = true;

    GCPROTECT_BEGIN(gc);
    {
        gc.tracker->GetDependentAndLoaderAllocator(&gc.loaderAllocatorRef, &gc.keyToValuesHashObject);
        if (gc.keyToValuesHashObject != NULL)
        {
            gc.keyToValuesHash = KeyToValuesGCHeapHash(gc.keyToValuesHashObject);
            INT32 indexInKeyValueHash = gc.keyToValuesHash.GetValueIndex(&key);
            if (indexInKeyValueHash != -1)
            {
                gc.keyToValuesHash.GetElement(indexInKeyValueHash, gc.keyValueStore);

                result = VisitKeyValueStore(&gc.loaderAllocatorRef, &gc.keyValueStore, visitor);
            }
        }
    }
    GCPROTECT_END();

    return result;
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitTrackerAllEntries(LAHASHDEPENDENTHASHTRACKERREF trackerUnsafe, Visitor &visitor)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    struct
    {
        LAHASHDEPENDENTHASHTRACKERREF tracker;
        OBJECTREF loaderAllocatorRef;
        GCHEAPHASHOBJECTREF keyToValuesHashObject;
        KeyToValuesGCHeapHash keyToValuesHash;
        OBJECTREF keyValueStore;
    }gc;

    class VisitAllEntryKeyValueHash
    {
        public:
        Visitor *m_pVisitor;
        KeyToValuesGCHeapHash *m_pKeysToValueHash;
        OBJECTREF *m_pKeyValueStore;
        OBJECTREF *m_pLoaderAllocatorRef;

        VisitAllEntryKeyValueHash(Visitor *pVisitor,  KeyToValuesGCHeapHash *pKeysToValueHash, OBJECTREF *pKeyValueStore, OBJECTREF *pLoaderAllocatorRef) :
            m_pVisitor(pVisitor),
            m_pKeysToValueHash(pKeysToValueHash),
            m_pKeyValueStore(pKeyValueStore),
            m_pLoaderAllocatorRef(pLoaderAllocatorRef)
            {}

        bool operator()(INT32 index)
        {
            WRAPPER_NO_CONTRACT;

            m_pKeysToValueHash->GetElement(index, *m_pKeyValueStore);
            return VisitKeyValueStore(m_pLoaderAllocatorRef, m_pKeyValueStore, m_pVisitor);
        }
    };

    ZeroMemory(&gc, sizeof(gc));
    gc.tracker = trackerUnsafe;

    bool result = true;

    GCPROTECT_BEGIN(gc);
    {
        gc.tracker->GetDependentAndLoaderAllocator(&gc.loaderAllocatorRef, &gc.keyToValuesHashObject);
        if (gc.keyToValuesHashObject != NULL)
        {
            gc.keyToValuesHash = KeyToValuesGCHeapHash(gc.keyToValuesHashObject);
            result = gc.keyToValuesHash.VisitAllEntryIndices(VisitAllEntryKeyValueHash(&visitor, &gc.keyToValuesHash, &gc.keyValueStore, &gc.loaderAllocatorRef));
        }
    }
    GCPROTECT_END();

    return result;
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitKeyToTrackerAllEntries(OBJECTREF hashKeyEntryUnsafe, Visitor &visitor)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    struct
    {
        OBJECTREF hashKeyEntry;
        LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
        OBJECTREF keyValueStore;
        OBJECTREF loaderAllocatorRef;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    gc.hashKeyEntry = hashKeyEntryUnsafe;

    bool result = true;

    GCPROTECT_BEGIN(gc);
    {
        if (gc.hashKeyEntry->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHKEYTOTRACKERS))
        {
            gc.hashKeyToTrackers = (LAHASHKEYTOTRACKERSREF)gc.hashKeyEntry;
            gc.keyValueStore = gc.hashKeyToTrackers->_laLocalKeyValueStore;
        }
        else
        {
            gc.keyValueStore = gc.hashKeyEntry;
        }

        result = VisitKeyValueStore(&gc.loaderAllocatorRef, &gc.keyValueStore, visitor);
    }
    GCPROTECT_END();

    return result;
}

template <class TRAITS>
/*static*/ void CrossLoaderAllocatorHash<TRAITS>::DeleteEntryTracker(TKey key, LAHASHDEPENDENTHASHTRACKERREF trackerUnsafe)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct
    {
        LAHASHDEPENDENTHASHTRACKERREF tracker;
        OBJECTREF loaderAllocatorRef;
        GCHEAPHASHOBJECTREF keyToValuesHashObject;
        KeyToValuesGCHeapHash keyToValuesHash;
    }gc;

    ZeroMemory(&gc, sizeof(gc));
    gc.tracker = trackerUnsafe;

    GCPROTECT_BEGIN(gc);
    {
        gc.tracker->GetDependentAndLoaderAllocator(&gc.loaderAllocatorRef, &gc.keyToValuesHashObject);
        if (gc.keyToValuesHashObject != NULL)
        {
            gc.keyToValuesHash = KeyToValuesGCHeapHash(gc.keyToValuesHashObject);
            gc.keyToValuesHash.DeleteEntry(&key);
        }
    }
    GCPROTECT_END();
}

#ifndef DACCESS_COMPILE
/*static */inline void CrossLoaderAllocatorHashSetup::EnsureTypesLoaded()
{
    STANDARD_VM_CONTRACT;

    // Force these types to be loaded, so that the hashtable logic can use CoreLibBinder::GetExistingClass
    // throughout and avoid lock ordering issues
    CoreLibBinder::GetClass(CLASS__LAHASHKEYTOTRACKERS);
    CoreLibBinder::GetClass(CLASS__LAHASHDEPENDENTHASHTRACKER);
    CoreLibBinder::GetClass(CLASS__GCHEAPHASH);
    TypeHandle elemType = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_I1));
    TypeHandle typHnd = ClassLoader::LoadArrayTypeThrowing(elemType, ELEMENT_TYPE_SZARRAY, 0);
    elemType = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_OBJECT));
    typHnd = ClassLoader::LoadArrayTypeThrowing(elemType, ELEMENT_TYPE_SZARRAY, 0);
}

template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::EnsureManagedObjectsInitted()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_loaderAllocatorToDependentTrackerHash == NULL)
    {
        OBJECTREF laToDependentHandleHashObject = AllocateObject(CoreLibBinder::GetExistingClass(CLASS__GCHEAPHASH));
        m_loaderAllocatorToDependentTrackerHash = m_pLoaderAllocator->GetDomain()->CreateHandle(laToDependentHandleHashObject);
        m_pLoaderAllocator->RegisterHandleForCleanup(m_loaderAllocatorToDependentTrackerHash);
    }

    if (m_keyToDependentTrackersHash == NULL)
    {
        OBJECTREF m_keyToDependentTrackersHashObject = AllocateObject(CoreLibBinder::GetExistingClass(CLASS__GCHEAPHASH));
        m_keyToDependentTrackersHash = m_pLoaderAllocator->GetDomain()->CreateHandle(m_keyToDependentTrackersHashObject);
        m_pLoaderAllocator->RegisterHandleForCleanup(m_keyToDependentTrackersHash);
    }
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
template <class TRAITS>
LAHASHDEPENDENTHASHTRACKERREF CrossLoaderAllocatorHash<TRAITS>::GetDependentTrackerForLoaderAllocator(LoaderAllocator* pLoaderAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct
    {
        GCHeapHashDependentHashTrackerHash dependentTrackerHash;
        LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
        GCHEAPHASHOBJECTREF GCHeapHashForKeyToValueStore;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc)
    {
        gc.dependentTrackerHash = GCHeapHashDependentHashTrackerHash((GCHEAPHASHOBJECTREF)ObjectFromHandle(m_loaderAllocatorToDependentTrackerHash));
        INT32 index = gc.dependentTrackerHash.GetValueIndex(&pLoaderAllocator);
        if (index != -1)
        {
            // We have an entry in the hashtable for the key/dependenthandle.
            gc.dependentTrackerHash.GetElement(index, gc.dependentTracker);
        }
        else
        {
            gc.dependentTracker = (LAHASHDEPENDENTHASHTRACKERREF)AllocateObject(CoreLibBinder::GetExistingClass(CLASS__LAHASHDEPENDENTHASHTRACKER));
            gc.GCHeapHashForKeyToValueStore = (GCHEAPHASHOBJECTREF)AllocateObject(CoreLibBinder::GetExistingClass(CLASS__GCHEAPHASH));

            OBJECTREF exposedObject = pLoaderAllocator->GetExposedObject();
            if (exposedObject == NULL)
            {
                if (m_globalDependentTrackerRootHandle == NULL)
                {
                    // Global LoaderAllocator does not have an exposed object, so create a fake one
                    exposedObject = AllocateObject(CoreLibBinder::GetExistingClass(CLASS__OBJECT));
                    m_globalDependentTrackerRootHandle = GetAppDomain()->CreateHandle(exposedObject);
                    m_pLoaderAllocator->RegisterHandleForCleanup(m_globalDependentTrackerRootHandle);
                }
                else
                {
                    exposedObject = ObjectFromHandle(m_globalDependentTrackerRootHandle);
                }
            }

            OBJECTHANDLE dependentHandle = GetAppDomain()->CreateDependentHandle(exposedObject, gc.GCHeapHashForKeyToValueStore);
            gc.dependentTracker->Init(dependentHandle, pLoaderAllocator);
            gc.dependentTrackerHash.Add(&pLoaderAllocator, [&gc](PTRARRAYREF arr, INT32 index)
            {
                arr->SetAt(index, (OBJECTREF)gc.dependentTracker);
            });
        }
    }
    GCPROTECT_END();

    return gc.dependentTracker;
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
template <class TRAITS>
GCHEAPHASHOBJECTREF CrossLoaderAllocatorHash<TRAITS>::GetKeyToValueCrossLAHashForHashkeyToTrackers(LAHASHKEYTOTRACKERSREF hashKeyToTrackersUnsafe, LoaderAllocator* pValueLoaderAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct
    {
        GCHeapHashDependentHashTrackerHash dependentTrackerHash;
        LAHASHDEPENDENTHASHTRACKERREF dependentTrackerMaybe;
        LAHASHDEPENDENTHASHTRACKERREF dependentTracker;
        LAHASHKEYTOTRACKERSREF hashKeyToTrackers;
        GCHEAPHASHOBJECTREF returnValue;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    // Now gc.hashKeyToTrackers is filled in.
    gc.hashKeyToTrackers = hashKeyToTrackersUnsafe;
    GCPROTECT_BEGIN(gc)
    {
        EnsureManagedObjectsInitted();

        // Is there a single dependenttracker here, or a set, or no dependenttracker at all
        if (gc.hashKeyToTrackers->_trackerOrTrackerSet == NULL)
        {
            gc.dependentTracker = GetDependentTrackerForLoaderAllocator(pValueLoaderAllocator);
            SetObjectReference(&gc.hashKeyToTrackers->_trackerOrTrackerSet, gc.dependentTracker);
        }
        else if (gc.hashKeyToTrackers->_trackerOrTrackerSet->GetMethodTable() == CoreLibBinder::GetExistingClass(CLASS__LAHASHDEPENDENTHASHTRACKER))
        {
            gc.dependentTrackerMaybe = (LAHASHDEPENDENTHASHTRACKERREF)gc.hashKeyToTrackers->_trackerOrTrackerSet;
            if (gc.dependentTrackerMaybe->IsTrackerFor(pValueLoaderAllocator))
            {
                // We've found the right dependent tracker.
                gc.dependentTracker = gc.dependentTrackerMaybe;
            }
            else
            {
                gc.dependentTracker = GetDependentTrackerForLoaderAllocator(pValueLoaderAllocator);
                if (!gc.dependentTrackerMaybe->IsLoaderAllocatorLive())
                {
                    SetObjectReference(&gc.hashKeyToTrackers->_trackerOrTrackerSet, gc.dependentTracker);
                }
                else
                {
                    // Allocate the dependent tracker hash
                    // Fill with the existing dependentTrackerMaybe, and gc.DependentTracker
                    gc.dependentTrackerHash = GCHeapHashDependentHashTrackerHash(AllocateObject(CoreLibBinder::GetExistingClass(CLASS__GCHEAPHASH)));
                    LoaderAllocator *pLoaderAllocatorKey = gc.dependentTracker->GetLoaderAllocatorUnsafe();
                    gc.dependentTrackerHash.Add(&pLoaderAllocatorKey, [&gc](PTRARRAYREF arr, INT32 index)
                        {
                            arr->SetAt(index, (OBJECTREF)gc.dependentTracker);
                        });
                    pLoaderAllocatorKey = gc.dependentTrackerMaybe->GetLoaderAllocatorUnsafe();
                    gc.dependentTrackerHash.Add(&pLoaderAllocatorKey, [&gc](PTRARRAYREF arr, INT32 index)
                        {
                            arr->SetAt(index, (OBJECTREF)gc.dependentTrackerMaybe);
                        });
                    SetObjectReference(&gc.hashKeyToTrackers->_trackerOrTrackerSet, gc.dependentTrackerHash.GetGCHeapRef());
                }
            }
        }
        else
        {
            gc.dependentTrackerHash = GCHeapHashDependentHashTrackerHash(gc.hashKeyToTrackers->_trackerOrTrackerSet);

            INT32 indexOfTracker = gc.dependentTrackerHash.GetValueIndex(&pValueLoaderAllocator);
            if (indexOfTracker == -1)
            {
                // Dependent tracker not yet attached to this key

                // Get dependent tracker
                gc.dependentTracker = GetDependentTrackerForLoaderAllocator(pValueLoaderAllocator);
                gc.dependentTrackerHash.Add(&pValueLoaderAllocator, [&gc](PTRARRAYREF arr, INT32 index)
                    {
                        arr->SetAt(index, (OBJECTREF)gc.dependentTracker);
                    });
            }
            else
            {
                gc.dependentTrackerHash.GetElement(indexOfTracker, gc.dependentTracker);
            }
        }

        // At this stage gc.dependentTracker is setup to have a good value
        gc.dependentTracker->GetDependentAndLoaderAllocator(NULL, &gc.returnValue);
    }
    GCPROTECT_END();

    return gc.returnValue;
}
#endif // !DACCESS_COMPILE

#endif // !CROSSGEN_COMPILE
#endif // CROSSLOADERALLOCATORHASH_H
#endif // CROSSLOADERALLOCATORHASH_INL
