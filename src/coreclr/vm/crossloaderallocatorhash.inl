// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CROSSLOADERALLOCATORHASH_INL
#define CROSSLOADERALLOCATORHASH_INL
#ifdef CROSSLOADERALLOCATORHASH_H

template<class TRAITS>
/*static*/ typename CrossLoaderAllocatorHash<TRAITS>::TCount
CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHashTraits::ComputeUsedEntries(
    KeyValueStore *keyValueStore,
    TCount *pEntriesInArrayTotal)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // An empty value slot may be used to store a count
    static_assert_no_msg(sizeof(TValue) >= sizeof(TCount));

    TCount entriesInArrayTotal = keyValueStore->GetCapacity();
    TCount usedEntries;
    TValue *pStartOfValuesData = keyValueStore->GetValues();

    if (entriesInArrayTotal == 0)
    {
        usedEntries = 0;
    }
    else if ((entriesInArrayTotal >= 2) && TRAITS::IsNullValue(pStartOfValuesData[entriesInArrayTotal - 2]))
    {
        usedEntries = *(TCount *)&pStartOfValuesData[entriesInArrayTotal - 1];
    }
    else if (TRAITS::IsNullValue(pStartOfValuesData[entriesInArrayTotal - 1]))
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
template<class TRAITS>
/*static*/ void CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHashTraits::SetUsedEntries(
    KeyValueStore *keyValueStore,
    TCount entriesInArrayTotal,
    TCount usedEntries)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // An empty value slot may be used to store a count
    static_assert_no_msg(sizeof(TValue) >= sizeof(TCount));

    _ASSERTE(entriesInArrayTotal == keyValueStore->GetCapacity());

    if (usedEntries < entriesInArrayTotal)
    {
        TValue *pStartOfValuesData = keyValueStore->GetValues();
        if (usedEntries == (entriesInArrayTotal - 1))
        {
            pStartOfValuesData[entriesInArrayTotal - 1] = TRAITS::NullValue();
        }
        else
        {
            *(TCount *)&pStartOfValuesData[entriesInArrayTotal - 1] = usedEntries;
            pStartOfValuesData[entriesInArrayTotal - 2] = TRAITS::NullValue();
        }
    }
}

template<class TRAITS>
/*static*/ void* CrossLoaderAllocatorHash<TRAITS>::KeyValueStore::operator new(size_t baseSize, CountWrapper capacity)
{
    return ::operator new(baseSize + capacity.value * sizeof(TValue));
}

template<class TRAITS>
/*static*/ typename CrossLoaderAllocatorHash<TRAITS>::KeyValueStore *CrossLoaderAllocatorHash<TRAITS>::KeyValueStore::Create(
    TCount capacity,
    const TKey &key)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    KeyValueStore *keyValueStore = new({capacity}) KeyValueStore(capacity, key);
    for (TCount i = 0; i < capacity; i++)
    {
        keyValueStore->GetValues()[i] = TRAITS::NullValue();
    }

    return keyValueStore;
}

template<class TRAITS>
CrossLoaderAllocatorHash<TRAITS>::LAHashKeyToTrackers::~LAHashKeyToTrackers()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    delete _laLocalKeyValueStore;

    LAHashDependentHashTrackerOrTrackerSet *trackerOrTrackerSet = _trackerOrTrackerSet;
    if (trackerOrTrackerSet == NULL)
    {
        return;
    }

    if (trackerOrTrackerSet->IsTrackerSet())
    {
        delete static_cast<LAHashDependentHashTrackerSetWrapper *>(trackerOrTrackerSet);
    }
    else
    {
        static_cast<LAHashDependentHashTracker *>(trackerOrTrackerSet)->DecRefCount();
    }
}

template<class TRAITS>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHashTraits::AddToValuesInHeapMemory(
    KeyValueStore **pKeyValueStore,
    NewHolder<KeyValueStore> &keyValueStoreHolder,
    const TKey& key,
    const TValue& value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    bool updatedKeyValueStore = false;
    KeyValueStore *keyValueStore = *pKeyValueStore;

    if (keyValueStore == NULL)
    {
        keyValueStoreHolder = *pKeyValueStore = keyValueStore =
            KeyValueStore::Create(TRAITS::IsNullValue(value) ? 0 : 1, key);
        updatedKeyValueStore = true;
        if (!TRAITS::IsNullValue(value))
        {
            keyValueStore->GetValues()[0] = value;
        }
    }
    else if (!TRAITS::IsNullValue(value))
    {
        _ASSERTE(TRAITS::KeyEquals(key, keyValueStore->GetKey()));

        TCount entriesInArrayTotal;
        TCount usedEntries = ComputeUsedEntries(keyValueStore, &entriesInArrayTotal);

        if (usedEntries == entriesInArrayTotal)
        {
            // There isn't free space. Build a new, bigger array with the existing data
            TCount newSize;
            if (usedEntries < 8)
                newSize = usedEntries + 1; // Grow very slowly initially. The cost of allocation/copy is cheap, and this holds very tight on memory usage
            else
                newSize = usedEntries * 2;

            if (newSize < usedEntries)
                COMPlusThrow(kOverflowException);

            // Allocate the new array.
            KeyValueStore *newKeyValueStore = KeyValueStore::Create(newSize, key);
            memcpyNoGCRefs(newKeyValueStore->GetValues(), keyValueStore->GetValues(), entriesInArrayTotal * sizeof(TValue));

            keyValueStoreHolder = *pKeyValueStore = keyValueStore = newKeyValueStore;
            updatedKeyValueStore = true;

            entriesInArrayTotal = newSize;
        }

        // There is free space. Append on the end
        SetUsedEntries(keyValueStore, entriesInArrayTotal, usedEntries + 1);
        keyValueStore->GetValues()[usedEntries] = value;
    }

    return updatedKeyValueStore;
}
#endif //!DACCESS_COMPILE

template<class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHashTraits::VisitKeyValueStore(
    LoaderAllocator *loaderAllocator,
    KeyValueStore *keyValueStore,
    Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    TKey key = keyValueStore->GetKey();
    TCount entriesInArrayTotal;
    TCount usedEntries = ComputeUsedEntries(keyValueStore, &entriesInArrayTotal);

    for (TCount index = 0; index < usedEntries; ++index)
    {
        if (!visitor(loaderAllocator, key, keyValueStore->GetValues()[index]))
        {
            return false;
        }
    }

    return true;
}

#ifndef DACCESS_COMPILE
template<class TRAITS>
/*static*/ void CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHashTraits::DeleteValueInHeapMemory(
    KeyValueStore *keyValueStore,
    const TValue& value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // TODO: Consider optimizing this by changing the add to ensure that the
    // values list is sorted, and then doing a binary search for the value instead
    // of the linear search

    TCount entriesInArrayTotal;
    TCount usedEntries = ComputeUsedEntries(keyValueStore, &entriesInArrayTotal);
    TValue *pStartOfValuesData = keyValueStore->GetValues();

    for (TCount iEntry = 0; iEntry < usedEntries; iEntry++)
    {
        if (TRAITS::ValueEquals(pStartOfValuesData[iEntry], value))
        {
            memmove(pStartOfValuesData + iEntry, pStartOfValuesData + iEntry + 1, (usedEntries - iEntry - 1) * sizeof(TValue));
            SetUsedEntries(keyValueStore, entriesInArrayTotal, usedEntries - 1);
            return;
        }
    }
}

template <class TRAITS>
/*static*/ LADependentHandleToNativeObject *CrossLoaderAllocatorHash<TRAITS>::LAHashDependentHashTracker::CreateDependentHandle(
    LoaderAllocator *loaderAllocator,
    LADependentKeyToValuesHash *dependentKeyValueStoreHash)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    NewHolder<LADependentHandleToNativeObject> dependentHandleHolder =
        new LADependentHandleToNativeObject(dependentKeyValueStoreHash);
    loaderAllocator->RegisterDependentHandleToNativeObjectForCleanup(dependentHandleHolder);
    return dependentHandleHolder.Extract();
}

template <class TRAITS>
CrossLoaderAllocatorHash<TRAITS>::LAHashDependentHashTracker::~LAHashDependentHashTracker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsLoaderAllocatorLive())
    {
        _loaderAllocator->UnregisterDependentHandleToNativeObjectFromCleanup(_dependentHandle);
    }

    delete _dependentHandle;
}
#endif //!DACCESS_COMPILE

template <class TRAITS>
typename CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHash *
CrossLoaderAllocatorHash<TRAITS>::LAHashDependentHashTracker::GetDependentKeyToValuesHash() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LADependentNativeObject *dependentObject = _dependentHandle->GetDependentObject();
    if (dependentObject == NULL)
    {
        return NULL;
    }

    return static_cast<LADependentKeyToValuesHash *>(dependentObject)->GetKeyToValuesHash();
}

template <class TRAITS>
/*static*/ typename CrossLoaderAllocatorHash<TRAITS>::TKey CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHashTraits::GetKey(
    KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    KeyValueStore *keyValueStore;
    if (hashKeyEntry->IsLAHashKeyToTrackers())
    {
        LAHashKeyToTrackers *hashKeyToTrackers = static_cast<LAHashKeyToTrackers *>(hashKeyEntry);
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
    }

    return keyValueStore->GetKey();
}

#ifndef DACCESS_COMPILE
template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::Add(TKey key, TValue value, LoaderAllocator *pLoaderAllocatorOfValue)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(TRAITS::GetLoaderAllocator(key) == m_pLoaderAllocator);

    // Add may involve multiple changes to the data structure. The data structure is set up such that each of those changes
    // keeps the data structure in a valid state, so OOMs in the middle of an Add operation may end up making some changes but
    // the data structure remains in a valid state. Holders are used to clean up memory that was allocated and not yet inserted
    // into the data structure. Once inserted into the data structure, it takes over the lifetime of the memory.

    NewHolder<KeyValueStore> keyValueStoreHolder;
    NewHolder<LAHashKeyToTrackers> hashKeyToTrackersHolder;
    KeyValueStore *keyValueStore = NULL;
    LAHashKeyToTrackers *hashKeyToTrackers = NULL;

    KeyToValuesHash &keyToTrackersHash = m_keyToDependentTrackersHash;
    KeyValueStoreOrLAHashKeyToTrackers *const *hashKeyEntryPtr = keyToTrackersHash.LookupPtr(key);
    KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry;

    if (hashKeyEntryPtr == NULL)
    {
        KeyToValuesHashTraits::AddToValuesInHeapMemory(
            &keyValueStore,
            keyValueStoreHolder,
            key,
            pLoaderAllocatorOfValue == m_pLoaderAllocator ? value : TRAITS::NullValue());

        if (pLoaderAllocatorOfValue != m_pLoaderAllocator)
        {
            hashKeyToTrackersHolder = hashKeyToTrackers = new LAHashKeyToTrackers(keyValueStore);
            keyValueStoreHolder.SuppressRelease();
            hashKeyEntry = hashKeyToTrackers;
        }
        else
        {
            hashKeyEntry = keyValueStore;
        }

        keyToTrackersHash.Add(hashKeyEntry);
        keyValueStoreHolder.SuppressRelease();
        hashKeyToTrackersHolder.SuppressRelease();
    }
    else
    {
        hashKeyEntry = *hashKeyEntryPtr;
        if (hashKeyEntry->IsLAHashKeyToTrackers())
        {
            hashKeyToTrackers = static_cast<LAHashKeyToTrackers *>(hashKeyEntry);
            keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
        }
        else
        {
            keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
        }

        if (pLoaderAllocatorOfValue == m_pLoaderAllocator)
        {
            bool updatedKeyValueStore =
                KeyToValuesHashTraits::AddToValuesInHeapMemory(&keyValueStore, keyValueStoreHolder, key, value);
            if (updatedKeyValueStore)
            {
                if (hashKeyToTrackers != NULL)
                {
                    delete hashKeyToTrackers->_laLocalKeyValueStore;
                    hashKeyToTrackers->_laLocalKeyValueStore = keyValueStore;
                }
                else
                {
                    hashKeyEntry = keyValueStore;
                    keyToTrackersHash.ReplacePtr(hashKeyEntryPtr, hashKeyEntry);
                }

                keyValueStoreHolder.SuppressRelease();
            }
        }
    }

    // If the LoaderAllocator matches, we've finished adding by now, otherwise, we need to get the remove hash and work with that
    if (pLoaderAllocatorOfValue != m_pLoaderAllocator)
    {
        if (hashKeyToTrackers == NULL)
        {
            // Nothing has yet caused the trackers proxy object to be setup. Create it now, and update the keyToTrackersHash.
            // Don't need to use the holder here since there's no allocation before it's put into the hash table. The previous
            // element was the key-value store, and should not be deleted because its lifetime is being assigned to the new
            // LAHashKeyToTrackers, so replace the element without cleanup.
            hashKeyToTrackers = new LAHashKeyToTrackers(keyValueStore);
            hashKeyEntry = hashKeyToTrackers;
            keyToTrackersHash.ReplacePtr(hashKeyEntryPtr, hashKeyEntry, false /* invokeCleanupAction */);
        }

        // Must add it to the cross LA structure
        KeyToValuesHash *keyToValuePerLAHash =
            GetKeyToValueCrossLAHashForHashkeyToTrackers(hashKeyToTrackers, pLoaderAllocatorOfValue);

        KeyValueStoreOrLAHashKeyToTrackers *const *hashKeyEntryInPerLAHashPtr = keyToValuePerLAHash->LookupPtr(key);
        if (hashKeyEntryInPerLAHashPtr != NULL)
        {
            _ASSERTE(!(*hashKeyEntryInPerLAHashPtr)->IsLAHashKeyToTrackers());
            keyValueStore = static_cast<KeyValueStore *>(*hashKeyEntryInPerLAHashPtr);

            if (KeyToValuesHashTraits::AddToValuesInHeapMemory(&keyValueStore, keyValueStoreHolder, key, value))
            {
                keyToValuePerLAHash->ReplacePtr(hashKeyEntryInPerLAHashPtr, keyValueStore);
                keyValueStoreHolder.SuppressRelease();
            }
        }
        else
        {
            keyValueStore = NULL;
            KeyToValuesHashTraits::AddToValuesInHeapMemory(&keyValueStore, keyValueStoreHolder, key, value);

            keyToValuePerLAHash->Add(keyValueStore);
            keyValueStoreHolder.SuppressRelease();
        }
    }
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::Remove(TKey key, TValue value, LoaderAllocator *pLoaderAllocatorOfValue)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(TRAITS::GetLoaderAllocator(key) == m_pLoaderAllocator);

    KeyToValuesHash &keyToTrackersHash = m_keyToDependentTrackersHash;
    KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry = keyToTrackersHash.Lookup(key);
    if (hashKeyEntry == NULL)
    {
        return;
    }

    LAHashKeyToTrackers *hashKeyToTrackers = NULL;
    KeyValueStore *keyValueStore;

    if (hashKeyEntry->IsLAHashKeyToTrackers())
    {
        hashKeyToTrackers = static_cast<LAHashKeyToTrackers *>(hashKeyEntry);
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
    }

    // Check to see if value can be removed from this data structure directly.
    if (m_pLoaderAllocator == pLoaderAllocatorOfValue)
    {
        KeyToValuesHashTraits::DeleteValueInHeapMemory(keyValueStore, value);
    }
    else if (hashKeyToTrackers != NULL)
    {
        // Must remove it from the cross LA structure
        KeyToValuesHash *keyToValuePerLAHash =
            GetKeyToValueCrossLAHashForHashkeyToTrackers(hashKeyToTrackers, pLoaderAllocatorOfValue);
        hashKeyEntry = keyToValuePerLAHash->Lookup(key);
        if (hashKeyEntry != NULL)
        {
            _ASSERTE(!hashKeyEntry->IsLAHashKeyToTrackers());
            keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
            KeyToValuesHashTraits::DeleteValueInHeapMemory(keyValueStore, value);
        }
    }
}
#endif // !DACCESS_COMPILE

template <class TRAITS>
template <class Visitor>
bool CrossLoaderAllocatorHash<TRAITS>::VisitValuesOfKey(TKey key, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(TRAITS::GetLoaderAllocator(key) == m_pLoaderAllocator);

    KeyToValuesHash &keyToTrackersHash = m_keyToDependentTrackersHash;
    KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry = keyToTrackersHash.Lookup(key);
    if (hashKeyEntry == NULL)
    {
        return true;
    }

    // We have an entry in the hashtable for the key/dependenthandle.
    LAHashKeyToTrackers *hashKeyToTrackers = NULL;
    KeyValueStore *keyValueStore;

    if (hashKeyEntry->IsLAHashKeyToTrackers())
    {
        hashKeyToTrackers = static_cast<LAHashKeyToTrackers *>(hashKeyEntry);
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
    }

    // Now hashKeyToTrackers is filled in and keyValueStore

    // visit local entries
    if (!VisitKeyValueStore(NULL, keyValueStore, visitor))
    {
        return false;
    }

    if (hashKeyToTrackers == NULL)
    {
        return true;
    }

    // Is there a single dependenttracker here, or a set.

    if (!hashKeyToTrackers->_trackerOrTrackerSet->IsTrackerSet())
    {
        LAHashDependentHashTracker *dependentTracker =
            static_cast<LAHashDependentHashTracker *>(hashKeyToTrackers->_trackerOrTrackerSet);
        return VisitTracker(key, dependentTracker, visitor);
    }
    else
    {
        LAHashDependentHashTrackerHash *dependentTrackerHash =
            static_cast<LAHashDependentHashTrackerSetWrapper *>(hashKeyToTrackers->_trackerOrTrackerSet)->GetTrackerSet();
        for (typename LAHashDependentHashTrackerHash::Iterator it = dependentTrackerHash->Begin(),
                itEnd = dependentTrackerHash->End();
            it != itEnd;
            ++it)
        {
            LAHashDependentHashTracker *dependentTracker = *it;
            if (!VisitTracker(key, dependentTracker, visitor))
            {
                return false;
            }
        }

        return true;
    }
}

template <class TRAITS>
template <class Visitor>
bool CrossLoaderAllocatorHash<TRAITS>::VisitAllKeyValuePairs(Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    KeyToValuesHash &keyToTrackersHash = m_keyToDependentTrackersHash;
    for (typename KeyToValuesHash::Iterator it = keyToTrackersHash.Begin(), itEnd = keyToTrackersHash.End(); it != itEnd; ++it)
    {
        KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry = *it;
        if (!VisitKeyToTrackerAllLALocalEntries(hashKeyEntry, visitor))
        {
            return false;
        }
    }

    LAHashDependentHashTrackerHash &dependentTrackerHash = m_loaderAllocatorToDependentTrackerHash;
    for (typename LAHashDependentHashTrackerHash::Iterator it = dependentTrackerHash.Begin(),
            itEnd = dependentTrackerHash.End();
        it != itEnd;
        ++it)
    {
        LAHashDependentHashTracker *dependentTracker = *it;
        if (!VisitTrackerAllEntries(dependentTracker, visitor))
        {
            return false;
        }
    }

    return true;
}

#ifndef DACCESS_COMPILE
template <class TRAITS>
void CrossLoaderAllocatorHash<TRAITS>::RemoveAll(TKey key)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This data structure actually doesn't have this invariant, but it is expected that uses of this
    // data structure will require that the key's loader allocator is the same as that of this data structure.
    _ASSERTE(TRAITS::GetLoaderAllocator(key) == m_pLoaderAllocator);

    KeyToValuesHash &keyToTrackersHash = m_keyToDependentTrackersHash;
    KeyValueStoreOrLAHashKeyToTrackers *const *hashKeyEntryPtr = keyToTrackersHash.LookupPtr(key);
    if (hashKeyEntryPtr == NULL)
    {
        return;
    }

    // We have an entry in the hashtable for the key/dependenthandle.
    KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry = *hashKeyEntryPtr;
    LAHashKeyToTrackers *hashKeyToTrackers = NULL;
    KeyValueStore *keyValueStore;

    if (hashKeyEntry->IsLAHashKeyToTrackers())
    {
        hashKeyToTrackers = static_cast<LAHashKeyToTrackers *>(hashKeyEntry);
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
    }

    // Now hashKeyToTrackers is filled in

    if (hashKeyToTrackers != NULL)
    {
        // Is there a single dependenttracker here, or a set.

        if (!hashKeyToTrackers->_trackerOrTrackerSet->IsTrackerSet())
        {
            LAHashDependentHashTracker *dependentTracker =
                static_cast<LAHashDependentHashTracker *>(hashKeyToTrackers->_trackerOrTrackerSet);
            DeleteEntryTracker(key, dependentTracker);
        }
        else
        {
            LAHashDependentHashTrackerHash *dependentTrackerHash =
                static_cast<LAHashDependentHashTrackerSetWrapper *>(hashKeyToTrackers->_trackerOrTrackerSet)->GetTrackerSet();
            for (typename LAHashDependentHashTrackerHash::Iterator it = dependentTrackerHash->Begin(),
                    itEnd = dependentTrackerHash->End();
                it != itEnd;
                ++it)
            {
                LAHashDependentHashTracker *dependentTracker = *it;
                DeleteEntryTracker(key, dependentTracker);
            }
        }
    }

    // Remove entry from key to tracker hash
    keyToTrackersHash.RemovePtr(const_cast<KeyValueStoreOrLAHashKeyToTrackers **>(hashKeyEntryPtr));
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
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitKeyValueStore(LoaderAllocator *loaderAllocator, KeyValueStore *keyValueStore, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    return KeyToValuesHashTraits::VisitKeyValueStore(loaderAllocator, keyValueStore, visitor);
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitTracker(TKey key, LAHashDependentHashTracker *tracker, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    KeyToValuesHash *keyToValuesHash = tracker->GetDependentKeyToValuesHash();
    if (keyToValuesHash != NULL)
    {
        KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry = keyToValuesHash->Lookup(key);
        if (hashKeyEntry != NULL)
        {
            _ASSERTE(!hashKeyEntry->IsLAHashKeyToTrackers());
            return VisitKeyValueStore(tracker->GetLoaderAllocatorUnsafe(), static_cast<KeyValueStore *>(hashKeyEntry), visitor);
        }
    }

    return true;
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitTrackerAllEntries(LAHashDependentHashTracker *tracker, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    KeyToValuesHash *keyToValuesHash = tracker->GetDependentKeyToValuesHash();
    if (keyToValuesHash != NULL)
    {
        LoaderAllocator *loaderAllocator = tracker->GetLoaderAllocatorUnsafe();
        for (typename KeyToValuesHash::Iterator it = keyToValuesHash->Begin(), itEnd = keyToValuesHash->End();
            it != itEnd;
            ++it)
        {
            KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry = *it;
            _ASSERTE(!hashKeyEntry->IsLAHashKeyToTrackers());
            if (!VisitKeyValueStore(loaderAllocator, static_cast<KeyValueStore *>(hashKeyEntry), visitor))
            {
                return false;
            }
        }
    }

    return true;
}

template <class TRAITS>
template <class Visitor>
/*static*/ bool CrossLoaderAllocatorHash<TRAITS>::VisitKeyToTrackerAllLALocalEntries(KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry, Visitor &visitor)
{
    WRAPPER_NO_CONTRACT;

    KeyValueStore *keyValueStore;
    if (hashKeyEntry->IsLAHashKeyToTrackers())
    {
        LAHashKeyToTrackers *hashKeyToTrackers = static_cast<LAHashKeyToTrackers *>(hashKeyEntry);
        keyValueStore = hashKeyToTrackers->_laLocalKeyValueStore;
    }
    else
    {
        keyValueStore = static_cast<KeyValueStore *>(hashKeyEntry);
    }

    return VisitKeyValueStore(NULL, keyValueStore, visitor);
}

template <class TRAITS>
/*static*/ void CrossLoaderAllocatorHash<TRAITS>::DeleteEntryTracker(TKey key, LAHashDependentHashTracker *tracker)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    KeyToValuesHash *keyToValuesHash = tracker->GetDependentKeyToValuesHash();
    if (keyToValuesHash != NULL)
    {
        keyToValuesHash->Remove(key);
    }
}

#ifndef DACCESS_COMPILE
template <class TRAITS>
typename CrossLoaderAllocatorHash<TRAITS>::LAHashDependentHashTracker *
CrossLoaderAllocatorHash<TRAITS>::GetDependentTrackerForLoaderAllocator(LoaderAllocator *pLoaderAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LAHashDependentHashTrackerHash &dependentTrackerHash = m_loaderAllocatorToDependentTrackerHash;
    LAHashDependentHashTracker *dependentTracker = dependentTrackerHash.Lookup(pLoaderAllocator);
    if (dependentTracker != NULL)
    {
        // We have an entry in the hashtable for the key/dependenthandle.
        return dependentTracker;
    }

    NewHolder<LADependentKeyToValuesHash> laDependentKeyToValuesHashHolder = new LADependentKeyToValuesHash();
    typename LAHashDependentHashTracker::NewTrackerHolder dependentTrackerHolder =
        new LAHashDependentHashTracker(pLoaderAllocator, laDependentKeyToValuesHashHolder);
    laDependentKeyToValuesHashHolder.SuppressRelease();

    dependentTrackerHash.Add(dependentTrackerHolder);
    return dependentTrackerHolder.Extract();
}
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
template <class TRAITS>
typename CrossLoaderAllocatorHash<TRAITS>::KeyToValuesHash *
CrossLoaderAllocatorHash<TRAITS>::GetKeyToValueCrossLAHashForHashkeyToTrackers(
    LAHashKeyToTrackers *hashKeyToTrackers,
    LoaderAllocator *pValueLoaderAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LAHashDependentHashTracker *dependentTracker;

    // Is there a single dependenttracker here, or a set, or no dependenttracker at all
    if (hashKeyToTrackers->_trackerOrTrackerSet == NULL)
    {
        dependentTracker = GetDependentTrackerForLoaderAllocator(pValueLoaderAllocator);
        hashKeyToTrackers->_trackerOrTrackerSet = dependentTracker;
        dependentTracker->IncRefCount();
    }
    else if (!hashKeyToTrackers->_trackerOrTrackerSet->IsTrackerSet())
    {
        LAHashDependentHashTracker *dependentTrackerMaybe =
            static_cast<LAHashDependentHashTracker *>(hashKeyToTrackers->_trackerOrTrackerSet);
        if (dependentTrackerMaybe->IsTrackerFor(pValueLoaderAllocator))
        {
            // We've found the right dependent tracker.
            dependentTracker = dependentTrackerMaybe;
        }
        else
        {
            dependentTracker = GetDependentTrackerForLoaderAllocator(pValueLoaderAllocator);
            if (!dependentTrackerMaybe->IsLoaderAllocatorLive())
            {
                hashKeyToTrackers->_trackerOrTrackerSet = dependentTracker;
                dependentTrackerMaybe->DecRefCount();
                dependentTracker->IncRefCount();
            }
            else
            {
                // Allocate the dependent tracker hash
                // Fill with the existing dependentTrackerMaybe, and DependentTracker
                NewHolder<LAHashDependentHashTrackerSetWrapper> dependentTrackerHashWrapperHolder =
                    new LAHashDependentHashTrackerSetWrapper();
                LAHashDependentHashTrackerHash *dependentTrackerHash = dependentTrackerHashWrapperHolder->GetTrackerSet();
                dependentTrackerHash->Add(dependentTracker);
                dependentTracker->IncRefCount();
                dependentTrackerHash->Add(dependentTrackerMaybe);
                hashKeyToTrackers->_trackerOrTrackerSet = dependentTrackerHashWrapperHolder.Extract();
            }
        }
    }
    else
    {
        LAHashDependentHashTrackerHash *dependentTrackerHash =
            static_cast<LAHashDependentHashTrackerSetWrapper *>(hashKeyToTrackers->_trackerOrTrackerSet)->GetTrackerSet();

        dependentTracker = dependentTrackerHash->Lookup(pValueLoaderAllocator);
        if (dependentTracker == NULL)
        {
            // Dependent tracker not yet attached to this key

            // Get dependent tracker
            dependentTracker = GetDependentTrackerForLoaderAllocator(pValueLoaderAllocator);
            dependentTrackerHash->Add(dependentTracker);
            dependentTracker->IncRefCount();
        }
    }

    // At this stage dependentTracker is setup to have a good value
    return dependentTracker->GetDependentKeyToValuesHash();
}
#endif // !DACCESS_COMPILE

#endif // CROSSLOADERALLOCATORHASH_H
#endif // CROSSLOADERALLOCATORHASH_INL
