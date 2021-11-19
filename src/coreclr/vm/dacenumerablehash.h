// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// DacEnumerableHash is an base class using the "Curiously recurring template pattern"
// (https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern)
// designed to factor out the functionality common to hash tables that are captured during DAC memory enumeration
//
// SEMANTICS
//
//  * Arbitrary entry payload via payload.
//  * 32-bit hash code for entries.
//  * Separate entry allocation and insertion (allowing reliable insertion if required).
//  * Enumerate all entries or entries matching a particular hash.
//  * No entry deletion.
//  * Base logic to support DAC memory enumeration of the hash (including per-entry tweaks as needed).
//  * Lock free lookup (the caller must follow the protocol laid out below under USER REQUIREMENTS).
//  * Automatic hash expansion (with dialable scale factor).
//
// BENEFITS
//
//  * Provides built-in DAC memory enumeration with optional per-entry customization for any size of hash entry.
//  * Factors out common code:
//      o Less chance of bugs, one place to make fixes.
//      o Less code overall.
//
// SUB-CLASSING REQUIREMENTS
//
// To author a new DacEnumerableHash-based hashtable, the following steps are required:
//  1) In most cases (where each hash entry will have multiple fields) a structure defining the hash entry
//     should be declared (see EEClassHashEntry in ClassHash.h for an example). This structure need not
//     include a field for the hash code or pointer to the next entry in the hash bucket; these are taken care
//     of automatically by the base class.
//  2) Declare your new hash class deriving from DacEnumerableHash and providing the following template parameters:
//      FINAL_CLASS  : The class you're declaring (this is used by the base class to locate certain helper
//                     methods in your class used to tweak hash behavior).
//      VALUE        : The type of your hash entries (the class defined in the previous step).
//      SCALE_FACTOR : A multiplier on bucket count every time the hash table is grown (currently once the
//                     number of hash entries exceeds twice the number of buckets). A value of 2 would double
//                     the number of buckets on each grow operation for example.
//  3) Define a constructor that invokes the base class constructor with various setup parameters (see
//     DacEnumerableHash constructor in this header). If your hash table is created via a static method rather than
//     direct construction (common) then call your constructor using an in-place new inside the static method
//     (see EEClassHashTable::Create in ClassHash.cpp for an example).
//  4) Define your basic hash functionality (creation, insertion, lookup, enumeration, and DAC
//     memory enumeration) using the Base* methods provided by DacEnumerableHash.
//  5) The following methods can be defined on the derived class to customize the DAC memory enumeration:
//
//          void EnumMemoryRegionsForEntry(EEClassHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags);
//              Called during EnumMemoryRegions for each entry in the hash. Use to enumerate any memory
//              referenced by the entry (but not the entry itself, that is already done by EnumMemoryRegions).
//
// USER REQUIREMENTS
//
// Synchronization: It is permissable to read data from the hash without taking a lock as long as:
//  1) Any hash modifications are performed under a lock or otherwise serialized.
//  2) Any miss on a lookup is handled by taking a lock are retry-ing the lookup.
//
// OVERALL DESIGN
//
// The table must cope with entry insertions and growing the
// bucket list when the table becomes too loaded (too many entries causing excessive bucket collisions). This
// happens when an entry insertion notes that there are twice as many entries as buckets. The bucket list is
// then reallocated (from a loader heap, consequently the old one is leaked) and resized based on a scale
// factor supplied by the hash sub-class.
//

#ifndef __DAC_ENUMERABLE_HASH_INCLUDED
#define __DAC_ENUMERABLE_HASH_INCLUDED

// The type used to contain an entry hash value. This is not customizable on a per-hash class basis: all
// DacEnumerableHash derived hashes will share the same definition. Note that we only care about the data size, and the
// fact that it is an unsigned integer value (so we can take a modulus for bucket computation and use bitwise
// equality checks). The base class does not care about or participate in how these hash values are calculated.
typedef DWORD DacEnumerableHashValue;

// The following code (and code in DacEnumerableHash.inl) has to replicate the base class template parameters (and in
// some cases the arguments) many many times. In the interests of brevity (and to make it a whole lot easier
// to modify these parameters in the future) we define macro shorthands for them here. Scan through the code
// to see how these are used.
#define DAC_ENUM_HASH_PARAMS typename FINAL_CLASS, typename VALUE, int SCALE_FACTOR
#define DAC_ENUM_HASH_ARGS FINAL_CLASS, VALUE, SCALE_FACTOR

// The base hash class itself. It's abstract and exposes its functionality via protected members (nothing is
// public).
template <DAC_ENUM_HASH_PARAMS>
class DacEnumerableHashTable
{
public:

#ifdef DACCESS_COMPILE
    // Call during DAC enumeration of memory regions to save in mini-dump to enumerate all hash table data
    // structures. Calls derived-class implementation of EnumMemoryRegionsForEntry to allow additional
    // per-entry memory to be reported.
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

private:
    struct VolatileEntry;
    typedef DPTR(struct VolatileEntry) PTR_VolatileEntry;
    struct VolatileEntry
    {
        VALUE               m_sValue;           // The derived-class format of an entry
        PTR_VolatileEntry   m_pNextEntry;       // Pointer to the next entry in the bucket chain (or NULL)
        DacEnumerableHashValue       m_iHashValue;       // The hash value associated with the entry
    };

protected:
    // This opaque structure provides enumeration context when walking the set of entries which share a common
    // hash code. Initialized by BaseFindFirstEntryByHash and read/updated by BaseFindNextEntryByHash.
    class LookupContext
    {
        friend class DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>;

        TADDR   m_pEntry;               // The entry the caller is currently looking at (or NULL to begin
                                        // with). This is a VolatileEntry* and should always be a target address
                                        // not a DAC PTR_.
        DPTR(PTR_VolatileEntry)   m_curBuckets;   // The bucket table we are working with.
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
        friend class DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>;

        DPTR(DacEnumerableHashTable<DAC_ENUM_HASH_ARGS>) m_pTable;   // Pointer back to the table being enumerated.
        TADDR                   m_pEntry;               // The entry the caller is currently looking at (or
                                                        // NULL to begin with). This is a VolatileEntry* and
                                                        // should always be a target address not a DAC PTR_.
        DWORD               m_dwBucket;             // Index of bucket we're currently walking
    };

#ifndef DACCESS_COMPILE
    // Base constructor. Call this from your derived constructor to provide the owning module, loader heap and
    // initial number of buckets (which must be non-zero). Module must be provided if this hash is to be
    // serialized into an ngen image. It is exposed to the derived hash class (many need it) but otherwise is
    // only used to locate a loader heap for allocating bucket lists and entries unless an alternative heap is
    // provided. Note that the heap provided is not serialized (so you'll allocate from that heap at
    // ngen-time, but revert to allocating from the module's heap at runtime). If no Module pointer is
    // supplied (non-ngen'd hash table) you must provide a direct heap pointer.
    DacEnumerableHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets);

    // Allocate an uninitialized entry for the hash table (it's not inserted). The AllocMemTracker is optional
    // and may be specified as NULL for untracked allocations. This is split from the hash insertion logic so
    // that callers can pre-allocate entries and then perform insertions which cannot fault.
    VALUE *BaseAllocateEntry(AllocMemTracker *pamTracker);

    // Insert an entry previously allocated via BaseAllocateEntry (you cannot allocated entries in any other
    // manner) and associated with the given hash value. The entry should have been initialized prior to
    // insertion.
    void BaseInsertEntry(DacEnumerableHashValue iHash, VALUE *pEntry);
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
    DPTR(VALUE) BaseFindFirstEntryByHash(DacEnumerableHashValue iHash, LookupContext *pContext);
    DPTR(VALUE) BaseFindNextEntryByHash(LookupContext *pContext);

    PTR_Module GetModule()
    {
        return m_pModule;
    }

    // Owning module set at hash creation time (possibly NULL if this hash instance is not to be ngen'd).
    PTR_Module m_pModule;

private:
    private:
    // Internal implementation details. Nothing of interest to sub-classers for here on.
    DPTR(VALUE) BaseFindFirstEntryByHashCore(DPTR(PTR_VolatileEntry) curBuckets, DacEnumerableHashValue iHash, LookupContext* pContext);

#ifndef DACCESS_COMPILE
    // Determine loader heap to be used for allocation of entries and bucket lists.
    LoaderHeap *GetHeap();

    // Increase the size of the bucket list in order to reduce the size of bucket chains. Does nothing on
    // failure to allocate (since this impacts perf, not correctness).
    void GrowTable();

    // Returns the next prime larger (or equal to) than the number given.
    DWORD NextLargestPrime(DWORD dwNumber);
#endif // !DACCESS_COMPILE

    DPTR(PTR_VolatileEntry) GetBuckets()
    {
        SUPPORTS_DAC;

        return m_pBuckets;
    }

    // our bucket table uses two extra slots - slot [0] contains the length of the table,
    //                                         slot [1] will contain the next version of the table if it resizes
    static const int SLOT_LENGTH = 0;
    static const int SLOT_NEXT = 1;
    // normal slots start at slot #2
    static const int SKIP_SPECIAL_SLOTS = 2;
    
    static DWORD GetLength(DPTR(PTR_VolatileEntry) buckets)
    {
        return (DWORD)dac_cast<TADDR>(buckets[SLOT_LENGTH]);
    }

    static DPTR(PTR_VolatileEntry) GetNext(DPTR(PTR_VolatileEntry) buckets)
    {
        return dac_cast<DPTR(PTR_VolatileEntry)>(buckets[SLOT_NEXT]);
    }

    // Loader heap provided at construction time. May be NULL (in which case m_pModule must *not* be NULL).
    LoaderHeap             *m_pHeap;

    DPTR(PTR_VolatileEntry)                  m_pBuckets;  // Pointer to a simple bucket list (array of VolatileEntry pointers)
    DWORD                                    m_cEntries;  // Count of elements
};

#endif // __DAC_ENUMERABLE_HASH_INCLUDED
