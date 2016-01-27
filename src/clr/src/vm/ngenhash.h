// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// NgenHash is an abstract base class (actually a templated base class) designed to factor out the
// functionality common to hashes persisted into ngen images.
//
// SEMANTICS
//
//  * Arbitrary entry payload via payload.
//  * 32-bit hash code for entries.
//  * Separate entry allocation and insertion (allowing reliable insertion if required).
//  * Enumerate all entries or entries matching a particular hash.
//  * No entry deletion.
//  * Base logic to efficiently serialize hash contents at ngen time. Hot/cold splitting of entries is
//    supported (along with the ability to tweak the Save and Fixup stages of each entry if needed).
//  * Base logic to support DAC memory enumeration of the hash (including per-entry tweaks as needed).
//  * Lock free lookup (the caller must follow the protocol laid out below under USER REQUIREMENTS).
//  * Automatic hash expansion (with dialable scale factor).
//  * Hash insertion is supported at runtime even when an ngen image is loaded with previously serialized hash
//    entries.
//  * Base logic to support formatting hashes in the nidump tool (only need to supply code for the unique
//    aspects of your hash).
//
// BENEFITS
//
//  * Removes next pointer from all persisted hash entries:
//      o Reduces data footprint of each entry.
//      o Increases density of entries.
//      o Removes a base relocation entry.
//      o Removes a runtime write to each entry (from the relocation above).
//  * Serializes all hot/cold hash entries contigiuously:
//      o Helps keeps hash entries in the same bucket in the same cache line.
//  * Compresses persisted bucket list and removes the use of pointers:
//      o Reduces working set hit of reading hash table (especially on 64-bit systems).
//      o Allows bucket list to be saved in read-only memory and thus use shared rather than private pages.
//  * Factors out common code:
//      o Less chance of bugs, one place to make fixes.
//      o Less code overall.
//
// SUB-CLASSING REQUIREMENTS
//
// To author a new NgenHash-based hashtable, the following steps are required:
//  1) In most cases (where each hash entry will have multiple fields) a structure defining the hash entry
//     should be declared (see EEClassHashEntry in ClassHash.h for an example). This structure need not
//     include a field for the hash code or pointer to the next entry in the hash bucket; these are taken care
//     of automatically by the base class. If the entry must reference another entry in the hash (this should
//     be rare) the NgenHashEntryRef<> template class should be used to abstract the reference (this class
//     hides some of the transformation work that must take place when entries are re-ordered during ngen
//     serialization).
//  2) Declare your new hash class deriving from NgenHash and providing the following template parameters:
//      FINAL_CLASS  : The class you're declaring (this is used by the base class to locate certain helper
//                     methods in your class used to tweak hash behavior).
//      VALUE        : The type of your hash entries (the class defined in the previous step).
//      SCALE_FACTOR : A multipler on bucket count every time the hash table is grown (currently once the
//                     number of hash entries exceeds twice the number of buckets). A value of 2 would double
//                     the number of buckets on each grow operation for example.
//  3) Define a constructor that invokes the base class constructor with various setup parameters (see
//     NgenHash constructor in this header). If your hash table is created via a static method rather than
//     direct construction (common) then call your constructor using an in-place new inside the static method
//     (see EEClassHashTable::Create in ClassHash.cpp for an example).
//  4) Define your basic hash functionality (creation, insertion, lookup, enumeration, ngen Save/Fixup and DAC
//     memory enumeration) using the Base* methods provided by NgenHash.
//  5) Tweak the operation of BaseSave, BaseFixup and BaseEnumMemoryRegions by providing definitions of the
//     following methods (note that all methods must be defined though they may be no-ops):
//
//          bool ShouldSave(DataImage *pImage, VALUE *pEntry);
//              Return true if the given entry should be persisted into the ngen image (otherwise it won't be
//              saved with the rest).
//
//          bool IsHotEntry(VALUE *pEntry, CorProfileData *pProfileData);
//              Return true is the entry is considered hot given the profiling data.
//
//          bool SaveEntry(DataImage *pImage, CorProfileData *pProfileData, VALUE *pOldEntry, VALUE *pNewEntry, EntryMappingTable *pMap);
//              Gives your hash class a chance to save any additional data needed into the ngen image during
//              the Save phase or otherwise make entry updates prior to saving. The saving process creates a
//              new copy of each hash entry and this method is passed pointers both to the original entry and
//              the new version along with a mapping class that can translate any old entry address in the
//              table into the corresponding new address. If you have inter-entry pointer fields this is your
//              chance to fix up those fields with the new location of their target entries.
//
//          void FixupEntry(DataImage *pImage, VALUE *pEntry, void *pFixupBase, DWORD cbFixupOffset);
//              Similar to SaveEntry but called during BaseFixup. This is your chance to register fixups for
//              any pointer type fields in your entry. Due to the way hash entries are packed during ngen
//              serialization individual hash entries are not saved as separate ngen zap nodes. So this method
//              is passed a pointer to the enclosing zapped data structure (pFixupBase) and the offset of the
//              entry from this base (cbFixupOffset). When calling pImage->FixupPointerField(...) for
//              instance, pass pFixupBase as the first parameter and cbFixupOffset + offsetof(YourEntryClass,
//              yourField) as the second parameter.
//
//          void EnumMemoryRegionsForEntry(EEClassHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags);
//              Called during BaseEnumMemoryRegions for each entry in the hash. Use to enumerate any memory
//              referenced by the entry (but not the entry itself).
//
// USER REQUIREMENTS
//
// Synchronization: It is permissable to read data from the hash without taking a lock as long as:
//  1) Any hash modifications are performed under a lock or otherwise serialized.
//  2) Any miss on a lookup is handled by taking a lock are retry-ing the lookup.
//
// OVERALL DESIGN
//
// The hash contains up to three groups of hash entries. These consist of two groups of entries persisted to
// disk at ngen time (split into hot and cold based on profile data) and live entries added at runtime (or
// during the ngen process itself, prior to the save operation).
//
// The persisted entries are tightly packed together and can eliminate some pointers and other metadata since
// we statically know about every entry at the time we format the hash entries (the save phase of ngen
// generation).
//
// Each persisted entry is assigned to a bucket based on its hash code and all entries that collide on a given
// bucket are placed contiguously in memory. The bucket list itself therefore consists of an array or pairs,
// each pair containing the count of entries in the bucket and the location of the first entry in the chain.
// Since all entries are allocated contiguously entry location can be specified by an index into the array of
// entries.
//
// Separate bucket lists and entry arrays are stored for hot and cold entries.
//
// The live entries (referred to here as volatile or warm entries) follow a more traditional hash
// implementation where entries are allocated individually from a loader heap and are chained together with a
// singly linked list if they collide. Here the bucket list is a simple array of pointers to the first entry
// in each chain (if any).
//
// Unlike the persisted entris the warm section of the table must cope with entry insertions and growing the
// bucket list when the table becomes too loaded (too many entries causing excessive bucket collisions). This
// happens when an entry insertion notes that there are twice as many entries as buckets. The bucket list is
// then reallocated (from a loader heap, consequently the old one is leaked) and resized based on a scale
// factor supplied by the hash sub-class.
//
// At runtime we lookup or enumerate entries by visiting all three sets of entries in the order Hot, Warm and
// Cold. This imposes a slight but constant time overhead.
//

#ifndef __NGEN_HASH_INCLUDED
#define __NGEN_HASH_INCLUDED

#ifdef FEATURE_PREJIT
#include "corcompile.h"
#endif

// The type used to contain an entry hash value. This is not customizable on a per-hash class basis: all
// NgenHash derived hashes will share the same definition. Note that we only care about the data size, and the
// fact that it is an unsigned integer value (so we can take a modulus for bucket computation and use bitwise
// equality checks). The base class does not care about or participate in how these hash values are calculated.
typedef DWORD NgenHashValue;

// The following code (and code in NgenHash.inl) has to replicate the base class template parameters (and in
// some cases the arguments) many many times. In the interests of brevity (and to make it a whole lot easier
// to modify these parameters in the future) we define macro shorthands for them here. Scan through the code
// to see how these are used.
#define NGEN_HASH_PARAMS typename FINAL_CLASS, typename VALUE, int SCALE_FACTOR
#define NGEN_HASH_ARGS FINAL_CLASS, VALUE, SCALE_FACTOR

// Forward definition of NgenHashEntryRef (it takes the same template parameters as NgenHash and simplifies
// hash entries that need to refer to other hash entries).
template <NGEN_HASH_PARAMS>
class NgenHashEntryRef;

// The base hash class itself. It's abstract and exposes its functionality via protected members (nothing is
// public).
template <NGEN_HASH_PARAMS>
class NgenHashTable
{
    // NgenHashEntryRef needs access to the base table internal during Fixup in order to compute zap node
    // bases.
    friend class NgenHashEntryRef<NGEN_HASH_ARGS>;

#ifdef DACCESS_COMPILE
    // Nidump knows how to walk this data structure.
    friend class NativeImageDumper;
#endif
#ifdef BINDER
    template<class HashEntry, class HashTableType> friend class NGenHashTableBuilder;
    friend class MdilModule;
#endif

protected:
    // This opaque structure provides enumeration context when walking the set of entries which share a common
    // hash code. Initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash.
    class LookupContext
    {
        friend class NgenHashTable<NGEN_HASH_ARGS>;

        TADDR   m_pEntry;               // The entry the caller is currently looking at (or NULL to begin
                                        // with). This is a VolatileEntry* or PersistedEntry* (depending on
                                        // m_eType below) and should always be a target address not a DAC
                                        // PTR_.
        DWORD   m_eType;                // The entry types we're currently walking (Hot, Warm, Cold in that order)
        DWORD   m_cRemainingEntries;    // The remaining entries in the bucket chain (Hot or Cold entries only)
    };

    // This opaque structure provides enumeration context when walking all entries in the table. Initialized
    // by BaseInitIterator and updated via the BaseIterator::Next. Note that this structure is somewhat
    // similar to LookupContext above (though it requires a bit more state). It's possible we could factor
    // these two iterators into some common base code but the actual implementations have enough differing
    // requirements that the resultant code could be less readable (and slightly less performant).
    class BaseIterator
    {
    public:
        // Returns a pointer to the next entry in the hash table or NULL once all entries have been
        // enumerated. Once NULL has been return the only legal operation is to re-initialize the iterator
        // with BaseInitIterator.
        DPTR(VALUE) Next();

    private:
        friend class NgenHashTable<NGEN_HASH_ARGS>;

        NgenHashTable<NGEN_HASH_ARGS> *m_pTable;        // Pointer back to the table being enumerated.
        TADDR                   m_pEntry;               // The entry the caller is currently looking at (or
                                                        // NULL to begin with). This is a VolatileEntry* or
                                                        // PersistedEntry* (depending on m_eType below) and
                                                        // should always be a target address not a DAC PTR_.
        DWORD                   m_eType;                // The entry types we're currently walking (Hot, Warm,
                                                        // Cold in that order).
        union
        {
            DWORD               m_dwBucket;             // Index of bucket we're currently walking (Warm).
            DWORD               m_cRemainingEntries;    // Number of entries remaining in hot/cold section
                                                        // (Hot, Cold).
        };
    };

#ifndef DACCESS_COMPILE
    // Base constructor. Call this from your derived constructor to provide the owning module, loader heap and
    // initial number of buckets (which must be non-zero). Module must be provided if this hash is to be
    // serialized into an ngen image. It is exposed to the derived hash class (many need it) but otherwise is
    // only used to locate a loader heap for allocating bucket lists and entries unless an alternative heap is
    // provided. Note that the heap provided is not serialized (so you'll allocate from that heap at
    // ngen-time, but revert to allocating from the module's heap at runtime). If no Module pointer is
    // supplied (non-ngen'd hash table) you must provide a direct heap pointer.
    NgenHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets);

    // Allocate an uninitialized entry for the hash table (it's not inserted). The AllocMemTracker is optional
    // and may be specified as NULL for untracked allocations. This is split from the hash insertion logic so
    // that callers can pre-allocate entries and then perform insertions which cannot fault.
    VALUE *BaseAllocateEntry(AllocMemTracker *pamTracker);

    // Insert an entry previously allocated via BaseAllocateEntry (you cannot allocated entries in any other
    // manner) and associated with the given hash value. The entry should have been initialized prior to
    // insertion.
    void BaseInsertEntry(NgenHashValue iHash, VALUE *pEntry);
#endif // !DACCESS_COMPILE

    // Return the number of entries held in the table (does not include entries allocated but not inserted
    // yet).
    DWORD BaseGetElementCount();

    // Initializes the iterator context passed by the caller to make it ready to walk every entry in the table
    // in an arbitrary order. Call pIterator->Next() to retrieve the first entry.
    void BaseInitIterator(BaseIterator *pIterator);

    // Find first entry matching a given hash value (returns NULL on no match). Call BaseFindNextEntryByHash
    // to iterate the remaining matches (until it returns NULL). The LookupContext supplied by the caller is
    // initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash to keep track of
    // where we are.
    DPTR(VALUE) BaseFindFirstEntryByHash(NgenHashValue iHash, LookupContext *pContext);
    DPTR(VALUE) BaseFindNextEntryByHash(LookupContext *pContext);

#if defined(FEATURE_PREJIT) && !defined(DACCESS_COMPILE)
    // Call during ngen to save hash table data structures into the ngen image. Calls derived-class
    // implementations of ShouldSave to determine which entries should be serialized, IsHotEntry to hot/cold
    // split the entries and SaveEntry to allow per-entry extension of the saving process.
    void BaseSave(DataImage *pImage, CorProfileData *pProfileData);

    // Call during ngen to register fixups for hash table data structure fields. Calls derived-class
    // implementation of FixupEntry to allow per-entry extension of the fixup process.
    void BaseFixup(DataImage *pImage);

    // Opaque structure used to store state will BaseSave is re-arranging hash entries and also passed to
    // sub-classes' SaveEntry method to facilitate mapping old entry addresses to their new locations.
    class EntryMappingTable
    {
    public:
        ~EntryMappingTable();

        // Given an old entry address (pre-BaseSave) return the address of the entry relocated ready for
        // saving to disk. Note that this address is the (ngen) runtime address, not the disk image address
        // you can further obtain by calling DataImage::GetImagePointer().
        VALUE *GetNewEntryAddress(VALUE *pOldEntry);

    private:
        friend class NgenHashTable<NGEN_HASH_ARGS>;

        // Each Entry holds the mapping from one old-to-new hash entry.
        struct Entry
        {
            VALUE          *m_pOldEntry;        // Pointer to the user part of the old entry
            VALUE          *m_pNewEntry;        // Pointer to the user part of the new version
            NgenHashValue   m_iHashValue;       // The hash code of the entry
            DWORD           m_dwNewBucket;      // The new bucket index of the entry
            DWORD           m_dwChainOrdinal;   // The 0-based position within the chain of that bucket
            bool            m_fHot;             // If true this entry was identified as hot by the sub-class
        };

        Entry  *m_pEntries;                     // Pointer to array of Entries
        DWORD   m_cEntries;                     // Count of valid entries in the above (may be smaller than
                                                // allocated size)
    };
#endif // FEATURE_PREJIT && !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    // Call during DAC enumeration of memory regions to save in mini-dump to enumerate all hash table data
    // structures. Calls derived-class implementation of EnumMemoryRegionsForEntry to allow additional
    // per-entry memory to be reported.
    void BaseEnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

    // Owning module set at hash creation time (possibly NULL if this hash instance is not to be ngen'd).
    PTR_Module          m_pModule;

private:
    // Internal implementation details. Nothing of interest to sub-classers for here on.

#ifdef FEATURE_PREJIT
    // This is the format of each hash entry that is persisted to disk (Hot or Cold entries).
    struct PersistedEntry
    {
        VALUE           m_sValue;       // The sub-class supplied entry layout
        NgenHashValue   m_iHashValue;   // The hash code associated with the entry (comes after m_sValue to
                                        // minimize chance of pad bytes on a 64-bit system).
    };
    typedef DPTR(PersistedEntry) PTR_PersistedEntry;
    typedef ArrayDPTR(PersistedEntry) APTR_PersistedEntry;

    // This class encapsulates a bucket list identifying chains of related persisted entries. It's compressed
    // rather than being a simple array, hence the encapsulation.
    // A bucket list represents a non-zero sequence of buckets, each bucket identified by a zero-based index.
    // Each bucket holds the index of the entry at the start of the bucket chain and a count of entries in
    // that chain. (In persisted form hash entries are collated into hot and cold entries which are then
    // allocated in contiguous blocks: this allows entries to be identified by an index into the entry block).
    // Buckets with zero entries have an undefined start index (and a zero count obviously).
    class PersistedBucketList
    {
#ifdef DACCESS_COMPILE
        friend class NativeImageDumper;
#endif
#ifdef BINDER
        template<class HashEntry, class HashTableType> friend class NGenHashTableBuilder;
        friend class MdilModule;
#endif

    public:
        // Allocate and initialize a new list with the given count of buckets and configured to hold no more
        // than the given number of entries or have a bucket chain longer than the specified maximum. These
        // two maximums allow the implementation to choose an optimal data format for the bucket list at
        // runtime and are enforced by asserts in the debug build.
        static PersistedBucketList *CreateList(DWORD cBuckets, DWORD cEntries, DWORD cMaxEntriesInBucket);

        // For the given bucket set the index of the initial entry and the count of entries in the chain. If
        // the count is zero the initial entry index is meaningless and ignored.
        void SetBucket(DWORD dwIndex, DWORD dwFirstEntry, DWORD cEntries);

        // Get the size in bytes of this entire bucket list (need to pass in the bucket count since we save
        // space by not storing it here, but we do validate this in debug mode).
        size_t GetSize(DWORD cBuckets);

        // Get the initial entry index and entry count for the given bucket. Initial entry index value is
        // undefined when count comes back as zero.
        void GetBucket(DWORD dwIndex, DWORD *pdwFirstEntry, DWORD *pdwCount);

        // Simplified initial entry index when you don't need the count (don't call this for buckets with zero
        // entries).
        DWORD GetInitialEntry(DWORD dwIndex);

    private:
        // Return the number of bits required to express a unique ID for the number of entities given.
        static DWORD BitsRequired(DWORD cEntities);

        // Return the minimum size (in bytes) of each bucket list entry that can express all buckets given the
        // max count of entries and entries in a single bucket chain.
        static DWORD GetBucketSize(DWORD cEntries, DWORD cMaxEntriesInBucket);

        // Each bucket is represented by a variable sized bitfield (16, 32 or 64 bits) whose low-order bits
        // contain the index of the first entry in the chain and higher-order (just above the initial entry
        // bits) contain the count of entries in the chain.

        DWORD           m_cbBucket;             // The size in bytes of each bucket descriptor (2, 4 or 8)
        DWORD           m_dwInitialEntryMask;   // The bitmask used to extract the initial entry index from a bucket
        DWORD           m_dwEntryCountShift;    // The bit shift used to extract the entry count from a bucket

#ifdef _DEBUG
        // In debug mode we remember more initial state to catch common errors.
        DWORD           m_cBuckets;             // Number of buckets in the list
        DWORD           m_cEntries;             // Total number of entries mapped by the list
        DWORD           m_cMaxEntriesInBucket;  // Largest bucket chain in the list
#endif
    };
    typedef DPTR(PersistedBucketList) PTR_PersistedBucketList;

    // Pointers and counters for entries and buckets persisted to disk during ngen. Collected into a structure
    // because this logic is replicated for Hot and Cold entries so we can factor some common code.
    struct PersistedEntries
    {
        APTR_PersistedEntry     m_pEntries;     // Pointer to a contiguous block of PersistedEntry structures
                                                // (NULL if zero entries)
        PTR_PersistedBucketList m_pBuckets;     // Pointer to abstracted bucket list mapping above entries
                                                // into a hash (NULL if zero buckets, which is iff zero
                                                // entries)
        DWORD                   m_cEntries;     // Count of entries in the above block
        DWORD                   m_cBuckets;     // Count of buckets in the above bucket list
    };
#endif // FEATURE_PREJIT

    // This is the format of a Warm entry, defined for our purposes to be a non-persisted entry (i.e. those
    // created at runtime or during the creation of the ngen image itself).
    struct VolatileEntry;
    typedef DPTR(struct VolatileEntry) PTR_VolatileEntry;
    struct VolatileEntry
    {
        VALUE               m_sValue;           // The derived-class format of an entry
        PTR_VolatileEntry   m_pNextEntry;       // Pointer to the next entry in the bucket chain (or NULL)
        NgenHashValue       m_iHashValue;       // The hash value associated with the entry
    };

    // Types of hash entry.
    enum EntryType
    {
        Cold,   // Persisted, profiling suggests this data is not read typically
        Warm,   // Volatile (in-memory)
        Hot     // Persisted, profiling suggests this data is probably read (or no profiling data was available)
    };

#ifdef FEATURE_PREJIT
    // Find the first persisted entry (hot or cold based on pEntries) that matches the given hash. Looks only
    // in the persisted block given (i.e. searches only hot *or* cold entries). Returns NULL on failure.
    // Otherwise returns pointer to the derived class portion of the entry and initializes the provided
    // LookupContext to allow enumeration of any further matches.
    DPTR(VALUE) FindPersistedEntryByHash(PersistedEntries *pEntries, NgenHashValue iHash, LookupContext *pContext);
#endif // FEATURE_PREJIT

    // Find the first volatile (warm) entry that matches the given hash. Looks only at warm entries. Returns
    // NULL on failure. Otherwise returns pointer to the derived class portion of the entry and initializes
    // the provided LookupContext to allow enumeration of any further matches.
    DPTR(VALUE) FindVolatileEntryByHash(NgenHashValue iHash, LookupContext *pContext);

#ifndef DACCESS_COMPILE
    // Determine loader heap to be used for allocation of entries and bucket lists.
    LoaderHeap *GetHeap();

    // Increase the size of the bucket list in order to reduce the size of bucket chains. Does nothing on
    // failure to allocate (since this impacts perf, not correctness).
    void GrowTable();

    // Returns the next prime larger (or equal to) than the number given.
    DWORD NextLargestPrime(DWORD dwNumber);
#endif // !DACCESS_COMPILE

    // Loader heap provided at construction time. May be NULL (in which case m_pModule must *not* be NULL).
    LoaderHeap             *m_pHeap;

    // Fields related to the runtime (volatile or warm) part of the hash.
    DPTR(PTR_VolatileEntry) m_pWarmBuckets;     // Pointer to a simple bucket list (array of VolatileEntry pointers)
    DWORD                   m_cWarmBuckets;     // Count of buckets in the above array (always non-zero)
    DWORD                   m_cWarmEntries;     // Count of elements in the warm section of the hash

#ifdef FEATURE_PREJIT
    PersistedEntries        m_sHotEntries;      // Hot persisted hash entries (if any)
    PersistedEntries        m_sColdEntries;     // Cold persisted hash entries (if any)

    DWORD                   m_cInitialBuckets;  // Initial number of warm buckets we started with. Only used
                                                // to reset warm bucket count in ngen-persisted table.
#endif // FEATURE_PREJIT
};

// Abstraction around cross-hash entry references (e.g. EEClassHashTable, where entries for nested types point
// to entries for their enclosing types). Under the covers we use a relative pointer which avoids the need to
// allocate a base relocation fixup and the resulting write into the entry at load time. The abstraction hides
// some of the complexity needed to achieve this.
template <NGEN_HASH_PARAMS>
class NgenHashEntryRef
{
public:
    // Get a pointer to the referenced entry.
    DPTR(VALUE) Get();

#ifndef DACCESS_COMPILE
    // Set the reference to point to the given entry.
    void Set(VALUE *pEntry);

#ifdef FEATURE_PREJIT
    // Call this during the ngen Fixup phase to adjust the relative pointer to account for ngen image layout.
    void Fixup(DataImage *pImage, NgenHashTable<NGEN_HASH_ARGS> *pTable);
#endif // FEATURE_PREJIT
#endif // !DACCESS_COMPILE

private:
    RelativePointer<DPTR(VALUE)> m_rpEntryRef;  // Entry ref encoded as a delta from this field's location.
};

#endif // __NGEN_HASH_INCLUDED
