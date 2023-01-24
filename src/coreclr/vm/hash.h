// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*++---------------------------------------------------------------------------------------

Module Name:

    hash.h

Abstract:

    Fast hash table classes,
--*/

#ifndef _HASH_H_
#define _HASH_H_

#ifndef ASSERT
#define ASSERT _ASSERTE
#endif


#include "crst.h"

// #define HASHTABLE_PROFILE

// Track collision chains of up to length X
const unsigned int HASHTABLE_LOOKUP_PROBES_DATA = 20;

//-------------------------------------------------------
//  enums for special Key values used in hash table
//
enum
{
    EMPTY  = 0,
    DELETED = 1,
    INVALIDENTRY = ~0
};

typedef ULONG_PTR UPTR;

//------------------------------------------------------------------------------
// classes in use
//------------------------------------------------------------------------------
class Bucket;
class HashMap;

//-------------------------------------------------------
//  class Bucket
//  used by hash table implementation
//
typedef DPTR(class Bucket) PTR_Bucket;
class Bucket
{
public:
    UPTR m_rgKeys[4];
    UPTR m_rgValues[4];

#define VALUE_MASK (sizeof(LPVOID) == 4 ? 0x7FFFFFFF : I64(0x7FFFFFFFFFFFFFFF))

    void SetValue (UPTR value, UPTR i)
    {
        LIMITED_METHOD_CONTRACT;

        ASSERT(value <= VALUE_MASK);
        m_rgValues[i] = (UPTR) ((m_rgValues[i] & ~VALUE_MASK) | value);
    }

    UPTR GetValue (UPTR i)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (UPTR)(m_rgValues[i] & VALUE_MASK);
    }

    UPTR IsCollision() // useful sentinel for fast fail of lookups
    {
        LIMITED_METHOD_CONTRACT;

        return (UPTR) (m_rgValues[0] & ~VALUE_MASK);
    }

    void SetCollision()
    {
        LIMITED_METHOD_CONTRACT;

        m_rgValues[0] |= ~VALUE_MASK; // set collision bit
        m_rgValues[1] &= VALUE_MASK;   // reset has free slots bit
    }

    BOOL HasFreeSlots()
    {
        WRAPPER_NO_CONTRACT;

        // check for free slots available in the bucket
        // either there is no collision or a free slot has been during
        // compaction
        return (!IsCollision() || (m_rgValues[1] & ~VALUE_MASK));
    }

    void SetFreeSlots()
    {
        LIMITED_METHOD_CONTRACT;

        m_rgValues[1] |= ~VALUE_MASK; // set has free slots bit
    }

    BOOL InsertValue(const UPTR key, const UPTR value);
};


//------------------------------------------------------------------------------
// bool (*CompareFnPtr)(UPTR,UPTR); pointer to a function that takes 2 UPTRs
// and returns a boolean, provide a function with this signature to the HashTable
// to use for comparing Values during lookup
//------------------------------------------------------------------------------
typedef  BOOL (*CompareFnPtr)(UPTR,UPTR);

class Compare
{
protected:
    Compare()
    {
        LIMITED_METHOD_CONTRACT;

        m_ptr = NULL;
    }
public:
    CompareFnPtr m_ptr;

    Compare(CompareFnPtr ptr)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ptr != NULL);
        m_ptr = ptr;
    }

    virtual ~Compare()
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual UPTR CompareHelper(UPTR val1, UPTR storedval)
    {
        WRAPPER_NO_CONTRACT;

#ifndef _DEBUG
        CONTRACTL
        {
            DISABLED(THROWS);       // This is not a bug, we cannot decide, since the function ptr called may be either.
            DISABLED(GC_NOTRIGGER); // This is not a bug, we cannot decide, since the function ptr called may be either.
        }
        CONTRACTL_END;
#endif // !_DEBUG

        return (*m_ptr)(val1,storedval);
    }
};

class ComparePtr : public Compare
{
public:
    ComparePtr (CompareFnPtr ptr)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ptr != NULL);
        m_ptr = ptr;
    }

    virtual UPTR CompareHelper(UPTR val1, UPTR storedval)
    {
        WRAPPER_NO_CONTRACT;

#ifndef _DEBUG
        CONTRACTL
        {
            DISABLED(THROWS);       // This is not a bug, we cannot decide, since the function ptr called may be either.
            DISABLED(GC_NOTRIGGER); // This is not a bug, we cannot decide, since the function ptr called may be either.
        }
        CONTRACTL_END;
#endif // !_DEBUG

        storedval <<=1;
        return (*m_ptr)(val1,storedval);
    }
};

//------------------------------------------------------------------------------
// Class HashMap
// Fast Hash table, for concurrent use,
// stores a 4 byte Key and a 4 byte Value for each slot.
// Duplicate keys are allowed, (keys are compared as 4 byte UPTRs)
// Duplicate values are allowed,(values are compared using comparison fn. provided)
// but if no comparison function is provided then the values should be unique
//
// Lookup's don't require to take locks, unless you specify fAsyncMode.
// Insert and Delete operations require locks
// Inserting a duplicate value will assert in DEBUG mode, the PROPER way to perform inserts
// is to take a lock, do a lookup and if the lookup fails then Insert
//
// In async mode, deleted slots are not immediately reclaimed (until a rehash), and
// accesses to the hash table cause a transition to cooperative GC mode, and reclamation of old
// hash maps (after a rehash) are deferred until GC time.
// In sync mode, none of this is necessary; however calls to LookupValue must be synchronized as well.
//
// Algorithm:
//   The Hash table is an array of buckets, each bucket can contain 4 key/value pairs
//   Special key values are used to identify EMPTY and DELETED slots
//   Hash function uses the current size of the hash table and a SEED based on the key
//   to choose the bucket, seed starts of being the key and gets refined every time
//   the hash function is re-applied.
//
//   Inserts choose an empty slot in the current bucket for new entries, if the current bucket
//   is full, then the seed is refined and a new bucket is chosen, if an empty slot is not found
//   after 8 retries, the hash table is expanded, this causes the current array of buckets to
//   be put in a free list and a new array of buckets is allocated and all non-deleted entries
//   from the old hash table are rehashed to the new array
//   The old arrays are reclaimed during Compact phase, which should only be called during GC or
//   any other time it is guaranteed that no Lookups are taking place.
//   Concurrent Insert and Delete operations need to be serialized
//
//   Delete operations, mark the Key in the slot as DELETED, the value is not removed and inserts
//   don't reuse these slots, they get reclaimed during expansion and compact phases.
//
//------------------------------------------------------------------------------

class HashMap
{
public:

    //@constructor
    HashMap() DAC_EMPTY();
    //destructor
    ~HashMap() DAC_EMPTY();

    // Init
    void Init(BOOL fAsyncMode, LockOwner *pLock)
    {
        WRAPPER_NO_CONTRACT;

        Init(0, (Compare *)NULL,fAsyncMode, pLock);
    }
    // Init
    void Init(DWORD cbInitialSize, BOOL fAsyncMode, LockOwner *pLock)
    {
        WRAPPER_NO_CONTRACT;

        Init(cbInitialSize, (Compare*)NULL, fAsyncMode, pLock);
    }
    // Init
    void Init(CompareFnPtr ptr, BOOL fAsyncMode, LockOwner *pLock)
    {
        WRAPPER_NO_CONTRACT;

        Init(0, ptr, fAsyncMode, pLock);
    }

    // Init method
    void Init(DWORD cbInitialSize, CompareFnPtr ptr, BOOL fAsyncMode, LockOwner *pLock);


    //Init method
    void Init(DWORD cbInitialSize, Compare* pCompare, BOOL fAsyncMode, LockOwner *pLock);

    // check to see if the value is already in the Hash Table
    // key should be > DELETED
    // if provided, uses the comparison function ptr to compare values
    // returns INVALIDENTRY if not found
    UPTR LookupValue(UPTR key, UPTR value);

    // Insert if the value is not already present
    // it is illegal to insert duplicate values in the hash map
    // do a lookup to verify the value is not already present

    void InsertValue(UPTR key, UPTR value);

    // Replace the value if present
    // returns the previous value, or INVALIDENTRY if not present
    // does not insert a new value under any circumstances

    UPTR ReplaceValue(UPTR key, UPTR value);

    // mark the entry as deleted and return the stored value
    // returns INVALIDENTRY, if not found
    UPTR DeleteValue (UPTR key, UPTR value);

    // for unique keys, use this function to get the value that is
    // stored in the hash table, returns INVALIDENTRY if key not found
    UPTR Gethash(UPTR key);

    // Called only when all threads are frozed, like during GC
    // for a SINGLE user mode, call compact after every delete
    // operation on the hash table
    void Compact();

    // Remove all entries from the hash tablex
    void Clear();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    // inline helper, in non HASHTABLE_PROFILE mode becomes a NO-OP
    void        ProfileLookup(UPTR ntry, UPTR retValue);
    // data members used for profiling
#ifdef HASHTABLE_PROFILE
    unsigned    m_cbRehash;    // number of times rehashed
    unsigned    m_cbRehashSlots; // number of slots that were rehashed
    unsigned    m_cbObsoleteTables;
    unsigned    m_cbTotalBuckets;
    unsigned    m_cbInsertProbesGt8; // inserts that needed more than 8 probes
    LONG        m_rgLookupProbes[HASHTABLE_LOOKUP_PROBES_DATA]; // lookup probes
    UPTR        maxFailureProbe; // cost of failed lookup

    void DumpStatistics();
#endif // HASHTABLE_PROFILE

#if 0 // Test-only code for debugging this class.
#ifndef DACCESS_COMPILE
    static void LookupPerfTest(HashMap * table, const unsigned int MinThreshold);
    static void HashMapTest();
#endif // !DACCESS_COMPILE
#endif // 0 // Test-only code for debugging this class.

protected:
    // static helper function
    static UPTR PutEntry (Bucket* rgBuckets, UPTR key, UPTR value);
private:

    DWORD       GetNearestIndex(DWORD cbInitialSize);

#ifdef _DEBUG
    static void            Enter(HashMap *);        // check valid to enter
    static void            Leave(HashMap *);        // check valid to leave

    typedef Holder<HashMap *, HashMap::Enter, HashMap::Leave> SyncAccessHolder;
    BOOL            m_fInSyncCode; // test for non-synchronous access
#else // !_DEBUG
    // in non DEBUG mode use a no-op helper
    typedef NoOpBaseHolder<HashMap *> SyncAccessHolder;
#endif // !_DEBUG

    // compute the new size, based on the number of free slots
    // available, compact or expand
    UPTR            NewSize();
    // create a new bucket array and rehash the non-deleted entries
    void            Rehash();
    static DWORD    GetSize(PTR_Bucket rgBuckets);
    static void     SetSize(Bucket* rgBuckets, size_t size);
    PTR_Bucket      Buckets();
    UPTR            CompareValues(const UPTR value1, const UPTR value2);

    // For double hashing, compute the second hash function once, then add.
    // H(key, i) = H1(key) + i * H2(key), where 0 <= i < numBuckets
    static void     HashFunction(const UPTR key, const UINT numBuckets, UINT &seed, UINT &incr);

    Compare*        m_pCompare;         // compare object to be used in lookup
    SIZE_T          m_iPrimeIndex;      // current size (index into prime array)
    PTR_Bucket      m_rgBuckets;        // array of buckets

    // track the number of inserts and deletes
    SIZE_T          m_cbPrevSlotsInUse;
    SIZE_T          m_cbInserts;
    SIZE_T          m_cbDeletes;
    // mode of operation, synchronous or single user
    bool            m_fAsyncMode;

#ifdef _DEBUG
    LPVOID          m_lockData;
    FnLockOwner     m_pfnLockOwner;
    EEThreadId      m_writerThreadId;
#endif // _DEBUG

#ifdef _DEBUG
    // A thread must own a lock for a hash if it is a writer.
    BOOL OwnLock();
#endif // _DEBUG

public:
    ///---------Iterator----------------

    // Iterator,
    class Iterator
    {
        PTR_Bucket m_pBucket;
        PTR_Bucket m_pSentinel;
        int        m_id;
        BOOL       m_fEnd;

    public:

        // Constructor
        Iterator(Bucket* pBucket) :
            m_pBucket(dac_cast<PTR_Bucket>(pBucket)),
            m_id(-1), m_fEnd(false)
        {
            SUPPORTS_DAC;
            WRAPPER_NO_CONTRACT;

            if (!m_pBucket) {
                m_pSentinel = NULL;
                m_fEnd = true;
                return;
            }
            size_t cbSize = (PTR_size_t(m_pBucket))[0];
            m_pBucket++;
            m_pSentinel = m_pBucket+cbSize;
            MoveNext(); // start
        }

        Iterator(const Iterator& iter)
        {
            LIMITED_METHOD_CONTRACT;

            m_pBucket = iter.m_pBucket;
            m_pSentinel = iter.m_pSentinel;
            m_id    = iter.m_id;
            m_fEnd = iter.m_fEnd;

        }

        //destructor
        ~Iterator(){ LIMITED_METHOD_DAC_CONTRACT; };

        // friend operator==
        friend bool operator == (const Iterator& lhs, const Iterator& rhs)
        {
            LIMITED_METHOD_CONTRACT;

            return (lhs.m_pBucket == rhs.m_pBucket && lhs.m_id == rhs.m_id);
        }
        // operator =
        inline Iterator& operator= (const Iterator& iter)
        {
            LIMITED_METHOD_CONTRACT;

            m_pBucket = iter.m_pBucket;
            m_pSentinel = iter.m_pSentinel;
            m_id    = iter.m_id;
            m_fEnd = iter.m_fEnd;
            return *this;
        }

        // operator ++
        inline void operator++ ()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(!m_fEnd); // check we are not already at end
            MoveNext();
        }
        // operator --



        //accessors : GetDisc() , returns the discriminator
        inline UPTR GetKey()
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(!m_fEnd); // check we are not already at end
            return m_pBucket->m_rgKeys[m_id];
        }
        //accessors : SetDisc() , sets the discriminator


        //accessors : GetValue(),
        // returns the pointer that corresponds to the discriminator
        inline UPTR GetValue()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            _ASSERTE(!m_fEnd); // check we are not already at end
            return m_pBucket->GetValue(m_id);
        }


        // end(), check if the iterator is at the end of the bucket
        inline BOOL end() const
        {
            LIMITED_METHOD_DAC_CONTRACT;

            return m_fEnd;
        }

    protected:

        void MoveNext()
        {
            LIMITED_METHOD_DAC_CONTRACT;

            for (;m_pBucket < m_pSentinel; m_pBucket++)
            {   //loop thru all buckets
                for (m_id = m_id+1; m_id < 4; m_id++)
                {   //loop through all slots
                    if (m_pBucket->m_rgKeys[m_id] > DELETED)
                    {
                        return;
                    }
                }
                m_id  = -1;
            }
            m_fEnd = true;
        }

    };

    inline Bucket* firstBucket()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return m_rgBuckets;
    }

    // return an iterator, positioned at the beginning of the bucket
    inline Iterator begin()
    {
        WRAPPER_NO_CONTRACT;

        return Iterator(m_rgBuckets);
    }

    inline SIZE_T GetCount()
    {
        LIMITED_METHOD_CONTRACT;

        return m_cbInserts-m_cbDeletes;
    }
};

//---------------------------------------------------------------------------------------
// class PtrHashMap
//  Wrapper class for using Hash table to store pointer values
//  HashMap class requires that high bit is always reset
//  The allocator used within the runtime, always allocates objects 8 byte aligned
//  so we can shift right one bit, and store the result in the hash table
class PtrHashMap
{
    HashMap         m_HashMap;

    // key really acts as a hash code. Sanitize it from special values used by the underlying HashMap.
    inline static UPTR SanitizeKey(UPTR key)
    {
        return (key > DELETED) ? key : (key + 100);
    }

public:
#ifndef DACCESS_COMPILE
    void *operator new(size_t size, LoaderHeap *pHeap);
    void operator delete(void *p);
#endif // !DACCESS_COMPILE

    // Init
    void Init(BOOL fAsyncMode, LockOwner *pLock)
    {
        WRAPPER_NO_CONTRACT;

        Init(0,NULL,fAsyncMode,pLock);
    }
    // Init
    void Init(DWORD cbInitialSize, BOOL fAsyncMode, LockOwner *pLock)
    {
        WRAPPER_NO_CONTRACT;

        Init(cbInitialSize, NULL, fAsyncMode,pLock);
    }
    // Init
    void Init(CompareFnPtr ptr, BOOL fAsyncMode, LockOwner *pLock)
    {
        WRAPPER_NO_CONTRACT;

        Init(0, ptr, fAsyncMode,pLock);
    }

    // Init method
    void Init(DWORD cbInitialSize, CompareFnPtr ptr, BOOL fAsyncMode, LockOwner *pLock);

    // check to see if the value is already in the Hash Table
    LPVOID LookupValue(UPTR key, LPVOID pv)
    {
        WRAPPER_NO_CONTRACT;

        key = SanitizeKey(key);

        // gmalloc allocator, always allocates 8 byte aligned
        // so we can shift out the lowest bit
        // ptr right shift by 1
        UPTR value = (UPTR)pv;
        _ASSERTE((value & 0x1) == 0);
        value>>=1;
        UPTR val =  m_HashMap.LookupValue (key, value);
        if (val != (UPTR) INVALIDENTRY)
        {
            val<<=1;
        }
        return (LPVOID)val;
    }

    // Insert if the value is not already present
    // it is illegal to insert duplicate values in the hash map
    // users should do a lookup to verify the value is not already present

    void InsertValue(UPTR key, LPVOID pv)
    {
        WRAPPER_NO_CONTRACT;

        key = SanitizeKey(key);

        // gmalloc allocator, always allocates 8 byte aligned
        // so we can shift out the lowest bit
        // ptr right shift by 1
        UPTR value = (UPTR)pv;
        _ASSERTE((value & 0x1) == 0);
        value>>=1;
        m_HashMap.InsertValue (key, value);
    }

    // Replace the value if present
    // returns the previous value, or INVALIDENTRY if not present
    // does not insert a new value under any circumstances

    LPVOID ReplaceValue(UPTR key, LPVOID pv)
    {
        WRAPPER_NO_CONTRACT;

        key = SanitizeKey(key);

        // gmalloc allocator, always allocates 8 byte aligned
        // so we can shift out the lowest bit
        // ptr right shift by 1
        UPTR value = (UPTR)pv;
        _ASSERTE((value & 0x1) == 0);
        value>>=1;
        UPTR val = m_HashMap.ReplaceValue (key, value);
        if (val != (UPTR) INVALIDENTRY)
        {
            val<<=1;
        }
        return (LPVOID)val;
    }

    // mark the entry as deleted and return the stored value
    // returns INVALIDENTRY if not found
    LPVOID DeleteValue (UPTR key,LPVOID pv)
    {
        WRAPPER_NO_CONTRACT;

        key = SanitizeKey(key);

        UPTR value = (UPTR)pv;
        _ASSERTE((value & 0x1) == 0);
        value >>=1 ;
        UPTR val = m_HashMap.DeleteValue(key, value);
        if (val != (UPTR) INVALIDENTRY)
        {
            val <<= 1;
        }
        return (LPVOID)val;
    }

    // for unique keys, use this function to get the value that is
    // stored in the hash table, returns INVALIDENTRY if key not found
    LPVOID Gethash(UPTR key)
    {
        WRAPPER_NO_CONTRACT;

        key = SanitizeKey(key);

        UPTR val = m_HashMap.Gethash(key);
        if (val != (UPTR) INVALIDENTRY)
        {
            val <<= 1;
        }
        return (LPVOID)val;
    }

    void Compact()
    {
        WRAPPER_NO_CONTRACT;

        m_HashMap.Compact();
    }

    void Clear()
    {
        WRAPPER_NO_CONTRACT;

        m_HashMap.Clear();
    }

    class PtrIterator
    {
        HashMap::Iterator iter;

    public:
        PtrIterator(HashMap& hashMap) : iter(hashMap.begin())
        {
            LIMITED_METHOD_DAC_CONTRACT;
        }
        PtrIterator(Bucket* bucket) : iter(bucket)
        {
            LIMITED_METHOD_DAC_CONTRACT;
        }

        ~PtrIterator()
        {
            LIMITED_METHOD_DAC_CONTRACT;
        }

        BOOL end()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            return iter.end();
        }

        UPTR GetKey()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            return iter.GetKey();
        }

        PTR_VOID GetValue()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            UPTR val = iter.GetValue();
            if (val != (UPTR) INVALIDENTRY)
            {
                val <<= 1;
            }
            return PTR_VOID(val);
        }

        void operator++()
        {
            WRAPPER_NO_CONTRACT;
            SUPPORTS_DAC;

            iter.operator++();
        }
    };

    inline Bucket* firstBucket()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return m_HashMap.firstBucket();
    }

    // return an iterator, positioned at the beginning of the bucket
    inline PtrIterator begin()
    {
        WRAPPER_NO_CONTRACT;

        return PtrIterator(m_HashMap);
    }

    inline SIZE_T GetCount()
    {
        LIMITED_METHOD_CONTRACT;

        return m_HashMap.GetCount();
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        SUPPORTS_DAC;
        m_HashMap.EnumMemoryRegions(flags);
    }
#endif // DACCESS_COMPILE
};

//---------------------------------------------------------------------
//  inline Bucket*& NextObsolete (Bucket* rgBuckets)
//  get the next obsolete bucket in the chain
inline
Bucket*& NextObsolete (Bucket* rgBuckets)
{
    LIMITED_METHOD_CONTRACT;

    return *(Bucket**)&((size_t*)rgBuckets)[1];
}

#endif // !_HASH_H_
