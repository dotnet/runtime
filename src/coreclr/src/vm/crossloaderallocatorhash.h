// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef CROSSLOADERALLOCATORHASH_H
#define CROSSLOADERALLOCATORHASH_H
#ifndef CROSSGEN_COMPILE

#include "gcheaphashtable.h"

class LoaderAllocator;

template <class TKey_, class TValue_>
class NoRemoveDefaultCrossLoaderAllocatorHashTraits
{
public:
    typedef TKey_ TKey;
    typedef TValue_ TValue;

    static bool IsNull(const TValue &value) { return value == NULL; }
    static TValue NullValue() { return NULL; }

#ifndef DACCESS_COMPILE
    static void SetUsedEntries(TValue* pStartOfValuesData, DWORD entriesInArrayTotal, DWORD usedEntries);
    static bool AddToValuesInHeapMemory(OBJECTREF *pKeyValueStore, const TKey& key, const TValue& value);
#endif //!DACCESS_COMPILE
    static DWORD ComputeUsedEntries(OBJECTREF *pKeyValueStore, DWORD *pEntriesInArrayTotal);
    template <class Visitor>
    static bool VisitKeyValueStore(OBJECTREF *pLoaderAllocatorRef, OBJECTREF *pKeyValueStore, Visitor &visitor);
    static TKey ReadKeyFromKeyValueStore(OBJECTREF *pKeyValueStore);
};

template <class TKey_, class TValue_>
class DefaultCrossLoaderAllocatorHashTraits : public NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>
{
public:
    typedef TKey_ TKey;
    typedef TValue_ TValue;

#ifndef DACCESS_COMPILE
    static void DeleteValueInHeapMemory(OBJECTREF keyValueStore, const TValue& value);
#endif //!DACCESS_COMPILE
};

struct GCHeapHashDependentHashTrackerHashTraits : public DefaultGCHeapHashTraits<true>
{
    typedef LoaderAllocator* PtrTypeKey;

    static INT32 Hash(PtrTypeKey *pValue);
    static INT32 Hash(PTRARRAYREF arr, INT32 index);
    static bool DoesEntryMatchKey(PTRARRAYREF arr, INT32 index, PtrTypeKey *pKey);
    static bool IsDeleted(PTRARRAYREF arr, INT32 index, GCHEAPHASHOBJECTREF gcHeap);
};

typedef GCHeapHash<GCHeapHashDependentHashTrackerHashTraits> GCHeapHashDependentHashTrackerHash;

template<class TRAITS>
struct KeyToValuesGCHeapHashTraits : public DefaultGCHeapHashTraits<true>
{
    template <class TKey>
    static INT32 Hash(TKey *pValue);
    static INT32 Hash(PTRARRAYREF arr, INT32 index);

    template<class TKey>
    static bool DoesEntryMatchKey(PTRARRAYREF arr, INT32 index, TKey *pKey);
};

// Hashtable of key to a list of values where the key may live in a different loader allocator
// than the value and this should not keep the loaderallocator of the value alive. The type of
// keys/values is defined via the TRAITS template argument, but must be non-gc pointers, and
// must be copyable without a copy constructor/require a destructor.
//
// This is managed via a series of different hashtables and data structures that are carefully
// engineered to be relatively memory efficient, yet still provide the ability to safely use
// the hashtable to hold relationships across LoaderAllocators which are not generally safe.
//
// In particular, given LoaderAllocator LA1 and LA2, where a reference to LA1 is not
// guaranteed to keep LA2 alive, this data structure can permit a pointer to an object which
// is defined as part of LA1 to be used as a key to find a pointer to an object that has the
// same lifetime as LA2.
//
// This data structure exposes Remove api's, but its primary use case is the combination of
// the Add and VisitValuesOfKey apis.
//
// To use Add, simply, call Add(TKey key, TValue value). This will add to the list of values
// associated with a key. The Add api should be called on a key's which are associated with
// the same LoaderAllocator as the CrossLoaderAllocatorHash.
//
// VisitValuesOfKey will visit all values that have the same key.
//
// IMPLEMENTATION DESIGN
// This data structure is a series of hashtables and lists.
// 
// In general, this data structure builds a set of values associated with a key per
// LoaderAllocator. The lists per loader allocator are controlled via the TRAITS template. The
// TRAITS specify how the individual lists are handled, and do the copying in and out of the data
// structures. It is not expected that additional traits implementations will be needed for use,
// unless duplicate prevention is needed.
//
// BASIC STRUCTURE
//
// m_keyToDependentTrackersHash - Hashtable of key -> (list of values in primary loader allocator,
//                                                   hashtable of DependentTrackers)
//
//   For each key in the table, there is at list of values in the primary loader allocator,
//   and optionally there may be a hashtable of dependent trackers
//
// m_loaderAllocatorToDependentTrackerHash - Hashtable of LoaderAllocator to DependentTracker. Used to find
// dependent trackers for insertion into per key sets.
//
// The DependentTracker is an object (with a finalizer) which is associated with a specific
// LoaderAllocator, and uses a DependentHandle to hold onto  a hashtable from Key to List of
// Values (for a specific LoaderAllocator). This dependent handle will keep that hashtable alive
// as long as the associated LoaderAllocator is live.
//
// The DependentTracker hashes (both the m_loaderAllocatorToDependentTrackerHash, and the per key hashes) are
// implemented via a hashtable which is "self-cleaning". In particular as the hashtable is
// walked for Add/Visit/Remove operations, if a DependentTracker is found which where the
// DependentHandle has detected that the LoaderAllocator has been freed, then the entry in
// the hashtable will set itself to the DELETED state. This cleaning operation will not occur
// eagerly, but it should prevent unbounded size growth as collectible LoaderAllocators are
// allocated and freed.
//
// Memory efficiency of this data structure.
//  - This data structure is reasonably memory efficient. If many values share the same key
//    then the memory efficiency per key trends toward 1.3333 * sizeof(Value). Otherwise basic
//    cost per key/value pair (assuming they are pointer sized has an overhead of about 4 
//    pointers + key/value data size.)
template <class TRAITS>
class CrossLoaderAllocatorHash
{
private:
    typedef typename TRAITS::TKey TKey;
    typedef typename TRAITS::TValue TValue;
    typedef GCHeapHash<KeyToValuesGCHeapHashTraits<TRAITS>> KeyToValuesGCHeapHash;

public:

#ifndef DACCESS_COMPILE
    // Add an entry to the CrossLoaderAllocatorHash, the default implementation of does DefaultCrossLoaderAllocatorHashTraits will not check for duplicates.
    void Add(TKey key, TValue value, LoaderAllocator *pLoaderAllocatorOfValue);

    // Remove an entry to the CrossLoaderAllocatorHash, only removes one entry
    void Remove(TKey key, TValue value, LoaderAllocator *pLoaderAllocatorOfValue);

    // Remove all entries that can be looked up by key
    void RemoveAll(TKey key);
#endif

    // Using visitor walk all values associated with a given key. The visitor
    // is expected to implement bool operator ()(OBJECTREF keepAlive, TKey key, TValue value).
    // Return false from that function to stop visitation.
    // This can be done simply by utilizing a lambda, or if a lambda cannot be used, a functor will do.
    // The value of "value" in this case must not escape from the visitor object
    // unless the keepAlive OBJECTREF is also kept alive
    template <class Visitor>
    bool VisitValuesOfKey(TKey key, Visitor &visitor);

    // Visit all key/value pairs
    template <class Visitor>
    bool VisitAllKeyValuePairs(Visitor &visitor);

    // Initialize this CrossLoaderAllocatorHash to be associated with a specific LoaderAllocator
    // Must be called before any use of Add
    void Init(LoaderAllocator *pAssociatedLoaderAllocator);

private:
#ifndef DACCESS_COMPILE
    void EnsureManagedObjectsInitted();
    LAHASHDEPENDENTHASHTRACKERREF GetDependentTrackerForLoaderAllocator(LoaderAllocator* pLoaderAllocator);
    GCHEAPHASHOBJECTREF GetKeyToValueCrossLAHashForHashkeyToTrackers(LAHASHKEYTOTRACKERSREF hashKeyToTrackersUnsafe, LoaderAllocator* pValueLoaderAllocator);
#endif // !DACCESS_COMPILE
    
    template <class Visitor>
    static bool VisitKeyValueStore(OBJECTREF *pLoaderAllocatorRef, OBJECTREF *pKeyValueStore, Visitor &visitor);
    template <class Visitor>
    static bool VisitTracker(TKey key, LAHASHDEPENDENTHASHTRACKERREF trackerUnsafe, Visitor &visitor);
    template <class Visitor>
    static bool VisitTrackerAllEntries(LAHASHDEPENDENTHASHTRACKERREF trackerUnsafe, Visitor &visitor);
    template <class Visitor>
    static bool VisitKeyToTrackerAllEntries(OBJECTREF hashKeyEntryUnsafe, Visitor &visitor);
    static void DeleteEntryTracker(TKey key, LAHASHDEPENDENTHASHTRACKERREF trackerUnsafe);

private:
    LoaderAllocator *m_pLoaderAllocator = 0;
    OBJECTHANDLE m_loaderAllocatorToDependentTrackerHash = 0;
    OBJECTHANDLE m_keyToDependentTrackersHash = 0;
    OBJECTHANDLE m_globalDependentTrackerRootHandle = 0;
};

class CrossLoaderAllocatorHashSetup
{
public:
    inline static void EnsureTypesLoaded();
};

#endif // !CROSSGEN_COMPILE
#endif // CROSSLOADERALLOCATORHASH_H
