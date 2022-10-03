// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CROSSLOADERALLOCATORHASH_H
#define CROSSLOADERALLOCATORHASH_H

#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

class LoaderAllocator;

template <class TKey_, class TValue_>
class NoRemoveDefaultCrossLoaderAllocatorHashTraits
{
public:
    typedef TKey_ TKey;
    typedef TValue_ TValue;
    typedef COUNT_T TCount;

    static const bool s_supports_remove = false;

    // CrossLoaderAllocatorHash requires that a particular null value exist, which represents an empty value slot
    static bool IsNullValue(const TValue &value) { return value == NULL; }
    static TValue NullValue() { return NULL; }
    
    static BOOL KeyEquals(const TKey &k1, const TKey &k2) { return k1 == k2; }
    static BOOL ValueEquals(const TValue &v1, const TValue &v2) { return v1 == v2; }
    static TCount Hash(const TKey &k) { return (TCount)(size_t)k; }
    
    static LoaderAllocator *GetLoaderAllocator(const TKey &k) { return k->GetLoaderAllocator(); }
};

template <class TKey_, class TValue_>
class DefaultCrossLoaderAllocatorHashTraits : public NoRemoveDefaultCrossLoaderAllocatorHashTraits<TKey_, TValue_>
{
public:
    static const bool s_supports_remove = true;
};

// Base class for a native object that depends on a LoaderAllocator's lifetime
class LADependentNativeObject
{
protected:
    LADependentNativeObject() = default;

public:
    virtual ~LADependentNativeObject() = default;

    DISABLE_COPY(LADependentNativeObject);
};

// A handle to an LADependentNativeObject that is registered with the LoaderAllocator. The handle would be cleared when the
// LoaderAllocator is collected. Appropriate locking must be used with this class and in the LoaderAllocator's code that clears
// handles.
class LADependentHandleToNativeObject
{
private:
    LADependentNativeObject *m_dependentObject;

public:
    LADependentHandleToNativeObject(LADependentNativeObject *dependentObject) : m_dependentObject(dependentObject) {}
    ~LADependentHandleToNativeObject() { delete m_dependentObject; }

    // See notes about synchronization in Clear()
    LADependentNativeObject *GetDependentObject() const { return m_dependentObject; }

    void Clear()
    {
        _ASSERTE(m_dependentObject != nullptr);

        // The callers of GetDependentObject() and Clear() must ensure that they cannot run concurrently, and that a
        // GetDependentObject() following a Clear() sees a null dependent object in a thread-safe manner
        delete m_dependentObject;
        m_dependentObject = nullptr;
    }

    DISABLE_COPY(LADependentHandleToNativeObject);
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
// IMPORTANT NOTE ABOUT SYNCHRONIZATION
//
// Any lock used to synchronize access to an instance of this data structure must also be
// acquired in AssemblyLoaderAllocator::CleanupDependentHandlesToNativeObjects() to ensure that
// while visiting values, the LoaderAllocator of the value remains alive inside the lock.
// Visited values must not be used outside the lock, as the LoaderAllocator of the value may
// may not be alive outside the lock.
//
// IMPLEMENTATION DESIGN
//
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
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Types used by CrossLoaderAllocatorHash

private:
    typedef typename TRAITS::TKey TKey;
    typedef typename TRAITS::TValue TValue;
    typedef typename TRAITS::TCount TCount;

    class KeyValueStoreOrLAHashKeyToTrackers
    {
    protected:
        KeyValueStoreOrLAHashKeyToTrackers() = default;

    public:
        virtual ~KeyValueStoreOrLAHashKeyToTrackers() = default;

        virtual bool IsLAHashKeyToTrackers() const { return false; }
    };

    class LAHashDependentHashTrackerOrTrackerSet
    {
    private:
        const bool _isTrackerSet;

    protected:
        LAHashDependentHashTrackerOrTrackerSet(bool isTrackerSet) : _isTrackerSet(isTrackerSet) {}

    public:
        bool IsTrackerSet() const { return _isTrackerSet; }
    };

    class KeyValueStore : public KeyValueStoreOrLAHashKeyToTrackers
    {
    private:
        const TCount _capacity;
        const TKey _key;
        TValue _values[0];

    private:
        KeyValueStore(TCount capacity, const TKey &key) : _capacity(capacity), _key(key) {}

    public:
        static KeyValueStore *Create(TCount capacity, const TKey &key);

        TCount GetCapacity() const { return _capacity; }
        TKey GetKey() const { return _key; }
        TValue *GetValues() { return _values; }
    };

    class LAHashKeyToTrackers : public KeyValueStoreOrLAHashKeyToTrackers
    {
    public:
        LAHashDependentHashTrackerOrTrackerSet *_trackerOrTrackerSet;

        // _laLocalKeyValueStore holds an object that represents a Key value (which must always be valid for the lifetime of the
        // CrossLoaderAllocatorHeapHash, and the values which must also be valid for that entire lifetime. When a value might
        // have a shorter lifetime it is accessed through the _trackerOrTrackerSet variable, which allows access to hashtables which
        // are associated with that remote loaderallocator through a dependent handle, so that lifetime can be managed.
        KeyValueStore *_laLocalKeyValueStore;

        LAHashKeyToTrackers(KeyValueStore *laLocalKeyValueStore)
            : _trackerOrTrackerSet(NULL), _laLocalKeyValueStore(laLocalKeyValueStore)
        {}

        virtual ~LAHashKeyToTrackers() override;

        virtual bool IsLAHashKeyToTrackers() const override { return true; }
    };

    class EMPTY_BASES_DECL KeyToValuesHashTraits : public DefaultSHashTraits<KeyValueStoreOrLAHashKeyToTrackers *>
    {
    private:
        typedef DefaultSHashTraits<KeyValueStoreOrLAHashKeyToTrackers *> Base;

    public:
        typedef TCount count_t;
        typedef TKey key_t;

        static const bool s_supports_remove = TRAITS::s_supports_remove;
        static const bool s_RemovePerEntryCleanupAction = true;

        static KeyValueStoreOrLAHashKeyToTrackers *Deleted()
        {
            if (s_supports_remove)
            {
                return Base::Deleted();
            }

            UNREACHABLE();
        }

        static bool IsDeleted(KeyValueStoreOrLAHashKeyToTrackers *e) { return s_supports_remove && Base::IsDeleted(e); }
        static void OnRemovePerEntryCleanupAction(KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry) { delete hashKeyEntry; }
        static TKey GetKey(KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry);
        static BOOL Equals(const TKey &k1, const TKey &k2) { return TRAITS::KeyEquals(k1, k2); }
        static TCount Hash(const TKey &k) { return TRAITS::Hash(k); }

    #ifndef DACCESS_COMPILE
        static void SetUsedEntries(KeyValueStore *keyValueStore, TCount entriesInArrayTotal, TCount usedEntries);
        static bool AddToValuesInHeapMemory(
            KeyValueStore **pKeyValueStore,
            NewHolder<KeyValueStore> &keyValueStoreHolder,
            const TKey& key,
            const TValue& value);
    #endif // !DACCESS_COMPILE
        static TCount ComputeUsedEntries(KeyValueStore *keyValueStore, TCount *pEntriesInArrayTotal);
        template <class Visitor>
        static bool VisitKeyValueStore(LoaderAllocator *loaderAllocator, KeyValueStore *keyValueStore, Visitor &visitor);
    #ifndef DACCESS_COMPILE
        static void DeleteValueInHeapMemory(KeyValueStore *keyValueStore, const TValue& value);
    #endif // !DACCESS_COMPILE
    };

    typedef SHash<KeyToValuesHashTraits> KeyToValuesHash;

    class LADependentKeyToValuesHash : public LADependentNativeObject
    {
    private:
        KeyToValuesHash _keyToValuesHash;

    public:
        virtual ~LADependentKeyToValuesHash() override = default;

        KeyToValuesHash *GetKeyToValuesHash() { return &_keyToValuesHash; }
    };

    class LAHashDependentHashTracker : public LAHashDependentHashTrackerOrTrackerSet
    {
    private:
        LoaderAllocator *const _loaderAllocator;
        LADependentHandleToNativeObject *const _dependentHandle;
        UINT64 _refCount;

    public:
        LAHashDependentHashTracker(LoaderAllocator *loaderAllocator, LADependentKeyToValuesHash *dependentKeyValueStoreHash)
            : LAHashDependentHashTrackerOrTrackerSet(false /* isTrackerSet */),
            _loaderAllocator(loaderAllocator),
            _dependentHandle(CreateDependentHandle(loaderAllocator, dependentKeyValueStoreHash)),
            _refCount(1)
        {}

    private:
        static LADependentHandleToNativeObject *CreateDependentHandle(
            LoaderAllocator *loaderAllocator,
            LADependentKeyToValuesHash *dependentKeyValueStoreHash);
        ~LAHashDependentHashTracker(); // only accessible via DecRefCount()

    public:
        bool IsLoaderAllocatorLive() const { return _dependentHandle->GetDependentObject() != NULL; }

        bool IsTrackerFor(LoaderAllocator *loaderAllocator) const
        {
            return loaderAllocator == _loaderAllocator && IsLoaderAllocatorLive();
        }

        KeyToValuesHash *GetDependentKeyToValuesHash() const;

        // Be careful with this. This isn't safe to use unless something is keeping the LoaderAllocator live, or there is no
        // intention to dereference this pointer.
        LoaderAllocator *GetLoaderAllocatorUnsafe() const { return _loaderAllocator; }

    #ifndef DACCESS_COMPILE
        void IncRefCount()
        {
            _ASSERTE(_refCount != 0);
            ++_refCount;
        }

        void DecRefCount()
        {
            _ASSERTE(_refCount != 0);
            if (--_refCount == 0)
            {
                delete this;
            }
        }

        static void StaticDecRefCount(LAHashDependentHashTracker *dependentTracker)
        {
            if (dependentTracker != NULL)
            {
                dependentTracker->DecRefCount();
            }
        }

        using NewTrackerHolder = SpecializedWrapper<LAHashDependentHashTracker, StaticDecRefCount>;
    #endif // !DACCESS_COMPILE
    };

    class EMPTY_BASES_DECL LAHashDependentHashTrackerHashTraits : public DefaultSHashTraits<LAHashDependentHashTracker *>
    {
    public:
        typedef TCount count_t;
        typedef LoaderAllocator *key_t;

        static const bool s_supports_autoremove = true;
        static const bool s_RemovePerEntryCleanupAction = true;

        static bool ShouldDelete(LAHashDependentHashTracker *dependentTracker)
        {
        #ifndef DACCESS_COMPILE
            // This is a tricky bit of logic used which detects freed loader allocators lazily
            // and deletes them from the hash table while looking up or otherwise walking the hashtable
            // for any purpose. OnRemovePerEntryCleanupAction() is invoked for removed elements to
            // handle cleanup of the actual data.
            return !dependentTracker->IsLoaderAllocatorLive();
        #else
            return false;
        #endif
        }

        static void OnRemovePerEntryCleanupAction(LAHashDependentHashTracker *dependentTracker)
        {
        #ifndef DACCESS_COMPILE
            // Dependent trackers may be stored in multiple hash tables and are ref-counted when added/removed from a hash
            // table. The ref count decrement may also delete the tracker.
            dependentTracker->DecRefCount();
        #endif
        }

        static LoaderAllocator *GetKey(LAHashDependentHashTracker *tracker) { return tracker->GetLoaderAllocatorUnsafe(); }
        static BOOL Equals(LoaderAllocator *la1, LoaderAllocator *la2) { return la1 == la2; }
        static TCount Hash(LoaderAllocator *la) { return (TCount)(size_t)la; }
    };

    typedef SHash<LAHashDependentHashTrackerHashTraits> LAHashDependentHashTrackerHash;

    class LAHashDependentHashTrackerSetWrapper : public LAHashDependentHashTrackerOrTrackerSet
    {
    private:
        LAHashDependentHashTrackerHash _trackerSet;

    public:
        LAHashDependentHashTrackerSetWrapper() : LAHashDependentHashTrackerOrTrackerSet(true /* isTrackerSet */) {}

        LAHashDependentHashTrackerHash *GetTrackerSet() { return &_trackerSet; }
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CrossLoaderAllocatorHash members

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
    LAHashDependentHashTracker *GetDependentTrackerForLoaderAllocator(LoaderAllocator *pLoaderAllocator);
    KeyToValuesHash *GetKeyToValueCrossLAHashForHashkeyToTrackers(
        LAHashKeyToTrackers *hashKeyToTrackers,
        LoaderAllocator *pValueLoaderAllocator);
#endif // !DACCESS_COMPILE

    template <class Visitor>
    static bool VisitKeyValueStore(LoaderAllocator *loaderAllocator, KeyValueStore *keyValueStore, Visitor &visitor);
    template <class Visitor>
    static bool VisitTracker(TKey key, LAHashDependentHashTracker *tracker, Visitor &visitor);
    template <class Visitor>
    static bool VisitTrackerAllEntries(LAHashDependentHashTracker *tracker, Visitor &visitor);
    template <class Visitor>
    static bool VisitKeyToTrackerAllLALocalEntries(KeyValueStoreOrLAHashKeyToTrackers *hashKeyEntry, Visitor &visitor);
    static void DeleteEntryTracker(TKey key, LAHashDependentHashTracker *tracker);

private:
    LoaderAllocator *m_pLoaderAllocator = 0;
    LAHashDependentHashTrackerHash m_loaderAllocatorToDependentTrackerHash;
    KeyToValuesHash m_keyToDependentTrackersHash;
};

#undef DISABLE_COPY

#endif // CROSSLOADERALLOCATORHASH_H
