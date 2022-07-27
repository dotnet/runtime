// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*++

Module Name:

    synchash.cpp

--*/

#include "common.h"

#include "hash.h"

#include "excep.h"

#include "syncclean.hpp"

#include "threadsuspend.h"

//---------------------------------------------------------------------
//  Array of primes, used by hash table to choose the number of buckets
//  Review: would we want larger primes? e.g., for 64-bit?

const DWORD g_rgPrimes[] = {
5,11,17,23,29,37,47,59,71,89,107,131,163,197,239,293,353,431,521,631,761,919,
1103,1327,1597,1931,2333,2801,3371,4049,4861,5839,7013,8419,10103,12143,14591,
17519,21023,25229,30293,36353,43627,52361,62851,75431,90523, 108631, 130363,
156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287,
4999559, 5999471, 7199369
};
const SIZE_T g_rgNumPrimes = sizeof(g_rgPrimes) / sizeof(*g_rgPrimes);

const unsigned int SLOTS_PER_BUCKET = 4;

#ifndef DACCESS_COMPILE

void *PtrHashMap::operator new(size_t size, LoaderHeap *pHeap)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT; //return NULL;

    return pHeap->AllocMem(S_SIZE_T(size));
}

void PtrHashMap::operator delete(void *p)
{
}


//-----------------------------------------------------------------
// Bucket methods

BOOL Bucket::InsertValue(const UPTR key, const UPTR value)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;  //return FALSE;

    _ASSERTE(key != EMPTY);
    _ASSERTE(key != DELETED);

    if (!HasFreeSlots())
        return false; //no free slots

    // might have a free slot
    for (UPTR i = 0; i < SLOTS_PER_BUCKET; i++)
    {
        //@NOTE we can't reuse DELETED slots
        if (m_rgKeys[i] == EMPTY)
        {
            SetValue (value, i);

            // On multiprocessors we should make sure that
            // the value is propagated before we proceed.
            // inline memory barrier call, refer to
            // function description at the beginning of this
            MemoryBarrier();

            m_rgKeys[i] = key;
            return true;
        }
    }       // for i= 0; i < SLOTS_PER_BUCKET; loop

    SetCollision(); // otherwise set the collision bit
    return false;
}

#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------
//  inline Bucket* HashMap::Buckets()
//  get the pointer to the bucket array
inline
PTR_Bucket HashMap::Buckets()
{
    LIMITED_METHOD_DAC_CONTRACT;

#if !defined(DACCESS_COMPILE)
    _ASSERTE (!g_fEEStarted || !m_fAsyncMode || GetThreadNULLOk() == NULL || GetThread()->PreemptiveGCDisabled() || IsGCThread());
#endif
    return m_rgBuckets + 1;
}

//---------------------------------------------------------------------
//  inline size_t HashMap::GetSize(PTR_Bucket rgBuckets)
//  get the number of buckets
inline
DWORD HashMap::GetSize(PTR_Bucket rgBuckets)
{
    LIMITED_METHOD_DAC_CONTRACT;
    PTR_size_t pSize = dac_cast<PTR_size_t>(rgBuckets - 1);
    _ASSERTE(FitsIn<DWORD>(pSize[0]));
    return static_cast<DWORD>(pSize[0]);
}


//---------------------------------------------------------------------
//  inline size_t HashMap::HashFunction(UPTR key, UINT numBuckets, UINT &seed, UINT &incr)
//  get the first & second hash function.
//   H(key, i) = h1(key) + i*h2(key, hashSize);  0 <= i < numBuckets
//   h2 must return a value >= 1 and < numBuckets.
inline
void HashMap::HashFunction(const UPTR key, const UINT numBuckets, UINT &seed, UINT &incr)
{
    LIMITED_METHOD_CONTRACT;
    // First hash function
    // We commonly use pointers, which are 4 byte aligned, so the two least
    // significant bits are often 0, then we mod this value by something like
    // 11.  We can get a better distribution for pointers by dividing by 4.
    // REVIEW: Is 64-bit truncation better or should we be doing something with the
    // upper 32-bits in either of these hash functions.
    seed = static_cast<UINT>(key >> 2);
    // Second hash function
    incr = (UINT)(1 + (((static_cast<UINT>(key >> 5)) + 1) % ((UINT)numBuckets - 1)));
    _ASSERTE(incr > 0 && incr < numBuckets);
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------
//  inline void HashMap::SetSize(Bucket *rgBuckets, size_t size)
//  set the number of buckets
inline
void HashMap::SetSize(Bucket *rgBuckets, size_t size)
{
    LIMITED_METHOD_CONTRACT;
    ((size_t*)rgBuckets)[0] = size;
}

//---------------------------------------------------------------------
//  HashMap::HashMap()
//  constructor, initialize all values
//
HashMap::HashMap()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    m_rgBuckets = NULL;
    m_pCompare = NULL;  // comparsion object
    m_cbInserts = 0;        // track inserts
    m_cbDeletes = 0;        // track deletes
    m_cbPrevSlotsInUse = 0; // track valid slots present during previous rehash

    //Debug data member
#ifdef _DEBUG
    m_fInSyncCode = false;
#endif
    // profile data members
#ifdef HASHTABLE_PROFILE
    m_cbRehash = 0;
    m_cbRehashSlots = 0;
    m_cbObsoleteTables = 0;
    m_cbTotalBuckets =0;
    m_cbInsertProbesGt8 = 0; // inserts that needed more than 8 probes
    maxFailureProbe =0;
    memset(m_rgLookupProbes,0,HASHTABLE_LOOKUP_PROBES_DATA*sizeof(LONG));
#endif // HASHTABLE_PROFILE
#ifdef _DEBUG
    m_lockData = NULL;
    m_pfnLockOwner = NULL;
#endif // _DEBUG
}

//---------------------------------------------------------------------
//  void HashMap::Init(unsigned cbInitialSize, CompareFnPtr ptr, bool fAsyncMode)
//  set the initial size of the hash table and provide the comparison
//  function pointer
//
void HashMap::Init(DWORD cbInitialSize, CompareFnPtr ptr, BOOL fAsyncMode, LockOwner *pLock)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    Compare* pCompare = NULL;
    if (ptr != NULL)
    {
        pCompare = new Compare(ptr);
    }
    Init(cbInitialSize, pCompare, fAsyncMode, pLock);
}

DWORD HashMap::GetNearestIndex(DWORD cbInitialSize)
{
    LIMITED_METHOD_CONTRACT;

    DWORD lowIndex = 0;
    DWORD highIndex = g_rgNumPrimes - 1;
    DWORD midIndex = (highIndex + 1) / 2;

    if (cbInitialSize <= g_rgPrimes[0])
        return 0;

    if (cbInitialSize >= g_rgPrimes[highIndex])
        return highIndex;

    while (true)
    {
        if (cbInitialSize < g_rgPrimes[midIndex])
        {
            highIndex = midIndex;
        }
        else
        {
            if (cbInitialSize == g_rgPrimes[midIndex])
                return midIndex;
            lowIndex = midIndex;
        }
        midIndex = lowIndex + (highIndex - lowIndex + 1)/2;
        if (highIndex == midIndex)
        {
            _ASSERTE(g_rgPrimes[highIndex] >= cbInitialSize);
            _ASSERTE(highIndex < g_rgNumPrimes);
            return highIndex;
        }
    }
}

//---------------------------------------------------------------------
//  void HashMap::Init(unsigned cbInitialSize, Compare* pCompare, bool fAsyncMode)
//  set the initial size of the hash table and provide the comparison
//  function pointer
//
void HashMap::Init(DWORD cbInitialSize, Compare* pCompare, BOOL fAsyncMode, LockOwner *pLock)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    m_iPrimeIndex = GetNearestIndex(cbInitialSize);
    DWORD size = g_rgPrimes[m_iPrimeIndex];
    PREFIX_ASSUME(size < 0x7fffffff);

    m_rgBuckets = new Bucket[size+1];

    memset (m_rgBuckets, 0, (size+1)*sizeof(Bucket));
    SetSize(m_rgBuckets, size);

    m_pCompare = pCompare;

    m_fAsyncMode = fAsyncMode != FALSE;

    // assert null comparison returns true
    //ASSERT(
    //      m_pCompare == NULL ||
    //      (m_pCompare->CompareHelper(0,0) != 0)
    //    );

#ifdef HASHTABLE_PROFILE
    m_cbTotalBuckets = size+1;
#endif

#ifdef _DEBUG
    if (pLock == NULL) {
        m_lockData = NULL;
        m_pfnLockOwner = NULL;
    }
    else
    {
        m_lockData = pLock->lock;
        m_pfnLockOwner = pLock->lockOwnerFunc;
    }
    if (m_pfnLockOwner == NULL) {
        m_writerThreadId.SetToCurrentThread();
    }
#endif // _DEBUG
}

//---------------------------------------------------------------------
//  void PtrHashMap::Init(unsigned cbInitialSize, CompareFnPtr ptr, bool fAsyncMode)
//  set the initial size of the hash table and provide the comparison
//  function pointer
//
void PtrHashMap::Init(DWORD cbInitialSize, CompareFnPtr ptr, BOOL fAsyncMode, LockOwner *pLock)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    ComparePtr *compare = NULL;
    if (ptr != NULL)
        compare = new ComparePtr(ptr);

    m_HashMap.Init(cbInitialSize, compare, fAsyncMode, pLock);
}

//---------------------------------------------------------------------
//  HashMap::~HashMap()
//  destructor, free the current array of buckets
//
HashMap::~HashMap()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    // free the current table
    Clear();
    // compare object
    if (NULL != m_pCompare)
        delete m_pCompare;
}


//---------------------------------------------------------------------
//  HashMap::Clear()
//  Remove all elements from table
//
void HashMap::Clear()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    // free the current table
    delete [] m_rgBuckets;

    m_rgBuckets = NULL;
}


//---------------------------------------------------------------------
//  UPTR   HashMap::CompareValues(const UPTR value1, const UPTR value2)
//  compare values with the function pointer provided
//
#ifndef _DEBUG
inline
#endif
UPTR   HashMap::CompareValues(const UPTR value1, const UPTR value2)
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

    /// NOTE:: the ordering of arguments are random
    return (m_pCompare == NULL || m_pCompare->CompareHelper(value1,value2));
}

//---------------------------------------------------------------------
//  bool HashMap::Enter()
//  bool HashMap::Leave()
//  check  valid use of the hash table in synchronus mode

#ifdef _DEBUG
#ifndef DACCESS_COMPILE
void HashMap::Enter(HashMap *map)
{
    LIMITED_METHOD_CONTRACT;

    // check proper concurrent use of the hash table
    if (map->m_fInSyncCode)
        ASSERT(0); // oops multiple access to sync.-critical code
    map->m_fInSyncCode = true;
}
#else
// In DAC builds, we don't want to take the lock, we just want to know if it's held. If it is,
// we assume the hash map is in an inconsistent state and throw an exception.
// Arguments:
//     input: map - the map controlled by the lock.
// Note: Throws
void HashMap::Enter(HashMap *map)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // check proper concurrent use of the hash table
    if (map->m_fInSyncCode)
    {
        ThrowHR(CORDBG_E_PROCESS_NOT_SYNCHRONIZED); // oops multiple access to sync.-critical code
    }
}
#endif // DACCESS_COMPILE

void HashMap::Leave(HashMap *map)
{
    LIMITED_METHOD_CONTRACT;

    // check proper concurrent use of the hash table
    if (map->m_fInSyncCode == false)
        ASSERT(0); // oops multiple access to sync.-critical code
    map->m_fInSyncCode = false;
}
#endif // _DEBUG

#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------
//  void HashMap::ProfileLookup(unsigned ntry)
//  profile helper code
void HashMap::ProfileLookup(UPTR ntry, UPTR retValue)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

#ifndef DACCESS_COMPILE
    #ifdef HASHTABLE_PROFILE
        if (ntry < HASHTABLE_LOOKUP_PROBES_DATA - 2)
            InterlockedIncrement(&m_rgLookupProbes[ntry]);
        else
            InterlockedIncrement(&m_rgLookupProbes[HASHTABLE_LOOKUP_PROBES_DATA - 2]);

        if (retValue == NULL)
        {   // failure probes
            InterlockedIncrement(&m_rgLookupProbes[HASHTABLE_LOOKUP_PROBES_DATA - 1]);
            // the following code is usually executed
            // only for special case of lookup done before insert
            // check hash.h SyncHash::InsertValue
            if (maxFailureProbe < ntry)
            {
                maxFailureProbe = ntry;
            }
        }
    #endif // HASHTABLE_PROFILE
#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------
//  void HashMap::InsertValue (UPTR key, UPTR value)
//  Insert into hash table, if the number of retries
//  becomes greater than threshold, expand hash table
//
void HashMap::InsertValue (UPTR key, UPTR value)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    _ASSERTE (OwnLock());

    // BROKEN: This is called for the RCWCache on the GC thread
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(m_fAsyncMode);

    ASSERT(m_rgBuckets != NULL);

    // check proper use in synchronous mode
    SyncAccessHolder holder(this);   // no-op in NON debug code

    ASSERT(value <= VALUE_MASK);

    ASSERT (key > DELETED);

    Bucket* rgBuckets = Buckets();
    DWORD cbSize = GetSize(rgBuckets);

    UINT seed, incr;
    HashFunction(key, cbSize, seed, incr);

    for (UPTR ntry =0; ntry < 8; ntry++)
    {
        Bucket* pBucket = &rgBuckets[seed % cbSize];
        if(pBucket->InsertValue(key,value))
        {
            goto LReturn;
        }

        seed += incr;
    } // for ntry loop

    // We need to expand to keep lookup short
    Rehash();

    // Try again
    PutEntry (Buckets(), key,value);

LReturn: // label for return

    m_cbInserts++;

    #ifdef _DEBUG
        ASSERT (m_pCompare != NULL || value == LookupValue (key,value));
        // check proper concurrent use of the hash table in synchronous mode
    #endif // _DEBUG

    return;
}
#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------
//  UPTR HashMap::LookupValue(UPTR key, UPTR value)
//  Lookup value in the hash table, use the comparison function
//  to verify the values match
//
UPTR HashMap::LookupValue(UPTR key, UPTR value)
{
    CONTRACTL
    {
        DISABLED(THROWS);       // This is not a bug, we cannot decide, since the function ptr called may be either.
        DISABLED(GC_NOTRIGGER); // This is not a bug, we cannot decide, since the function ptr called may be either.
    }
    CONTRACTL_END;

    SCAN_IGNORE_THROW;          // See contract above.
    SCAN_IGNORE_TRIGGER;        // See contract above.

#ifndef DACCESS_COMPILE
    _ASSERTE (m_fAsyncMode || OwnLock());

    // BROKEN: This is called for the RCWCache on the GC thread
    // Also called by AppDomain::FindCachedAssembly to resolve AssemblyRef -- this is used by stack walking on the GC thread.
    // See comments in GCHeapUtilities::RestartEE (above the call to SyncClean::CleanUp) for reason to enter COOP mode.
    // However, if the current thread is the GC thread, we know we're not going to call GCHeapUtilities::RestartEE
    // while accessing the HashMap, so it's safe to proceed.
    // (m_fAsyncMode && !IsGCThread() is the condition for entering COOP mode.  I.e., enable COOP GC only if
    // the HashMap is in async mode and this is not a GC thread.)
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(m_fAsyncMode && !IsGCThread());

    ASSERT(m_rgBuckets != NULL);
    // This is necessary in case some other thread
    // replaces m_rgBuckets
    ASSERT (key > DELETED);

    // perform this check at lookup time as well
    ASSERT(value <= VALUE_MASK);
#endif // !DACCESS_COMPILE

    PTR_Bucket rgBuckets = Buckets(); //atomic fetch
    DWORD cbSize = GetSize(rgBuckets);

    UINT seed, incr;
    HashFunction(key, cbSize, seed, incr);

    UPTR ntry;
    for(ntry =0; ntry < cbSize; ntry++)
    {
        PTR_Bucket pBucket = rgBuckets+(seed % cbSize);
        for (unsigned int i = 0; i < SLOTS_PER_BUCKET; i++)
        {
            if (pBucket->m_rgKeys[i] == key) // keys match
            {

                // inline memory barrier call, refer to
                // function description at the beginning of this
                MemoryBarrier();

                UPTR storedVal = pBucket->GetValue(i);
                // if compare function is provided
                // dupe keys are possible, check if the value matches,
// Not using compare function in DAC build.
#ifndef DACCESS_COMPILE
                if (CompareValues(value,storedVal))
#endif
                {
                    ProfileLookup(ntry,storedVal); //no-op in non HASHTABLE_PROFILE code

                    // return the stored value
                    return storedVal;
                }
            }
        }

        seed += incr;
        if(!pBucket->IsCollision())
            break;
    }   // for ntry loop

    // not found
    ProfileLookup(ntry,INVALIDENTRY); //no-op in non HASHTABLE_PROFILE code

    return INVALIDENTRY;
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------
//  UPTR HashMap::ReplaceValue(UPTR key, UPTR value)
//  Replace existing value in the hash table, use the comparison function
//  to verify the values match
//
UPTR HashMap::ReplaceValue(UPTR key, UPTR value)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(OwnLock());

    // BROKEN: This is called for the RCWCache on the GC thread
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(m_fAsyncMode);

    ASSERT(m_rgBuckets != NULL);
    // This is necessary in case some other thread
    // replaces m_rgBuckets
    ASSERT (key > DELETED);

    // perform this check during replacing as well
    ASSERT(value <= VALUE_MASK);

    Bucket* rgBuckets = Buckets(); //atomic fetch
    DWORD  cbSize = GetSize(rgBuckets);

    UINT seed, incr;
    HashFunction(key, cbSize, seed, incr);

    UPTR ntry;
    for(ntry =0; ntry < cbSize; ntry++)
    {
        Bucket* pBucket = &rgBuckets[seed % cbSize];
        for (unsigned int i = 0; i < SLOTS_PER_BUCKET; i++)
        {
            if (pBucket->m_rgKeys[i] == key) // keys match
            {

                // inline memory barrier call, refer to
                // function description at the beginning of this
                MemoryBarrier();

                UPTR storedVal = pBucket->GetValue(i);
                // if compare function is provided
                // dupe keys are possible, check if the value matches,
                if (CompareValues(value,storedVal))
                {
                    ProfileLookup(ntry,storedVal); //no-op in non HASHTABLE_PROFILE code

                    pBucket->SetValue(value, i);

                    // On multiprocessors we should make sure that
                    // the value is propagated before we proceed.
                    // inline memory barrier call, refer to
                    // function description at the beginning of this
                    MemoryBarrier();

                    // return the previous stored value
                    return storedVal;
                }
            }
        }

        seed += incr;
        if(!pBucket->IsCollision())
            break;
    }   // for ntry loop

    // not found
    ProfileLookup(ntry,INVALIDENTRY); //no-op in non HASHTABLE_PROFILE code

    return INVALIDENTRY;
}

//---------------------------------------------------------------------
//  UPTR HashMap::DeleteValue (UPTR key, UPTR value)
//  if found mark the entry deleted and return the stored value
//
UPTR HashMap::DeleteValue (UPTR key, UPTR value)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE (OwnLock());

    // BROKEN: This is called for the RCWCache on the GC thread
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(m_fAsyncMode);

    // check proper use in synchronous mode
    SyncAccessHolder holoder(this);  //no-op in non DEBUG code

    ASSERT(m_rgBuckets != NULL);
    // This is necessary in case some other thread
    // replaces m_rgBuckets
    ASSERT (key > DELETED);

    // perform this check during replacing as well
    ASSERT(value <= VALUE_MASK);

    Bucket* rgBuckets = Buckets();
    DWORD  cbSize = GetSize(rgBuckets);

    UINT seed, incr;
    HashFunction(key, cbSize, seed, incr);

    UPTR ntry;
    for(ntry =0; ntry < cbSize; ntry++)
    {
        Bucket* pBucket = &rgBuckets[seed % cbSize];
        for (unsigned int i = 0; i < SLOTS_PER_BUCKET; i++)
        {
            if (pBucket->m_rgKeys[i] == key) // keys match
            {
                // inline memory barrier call, refer to
                // function description at the beginning of this
                MemoryBarrier();

                UPTR storedVal = pBucket->GetValue(i);
                // if compare function is provided
                // dupe keys are possible, check if the value matches,
                if (CompareValues(value,storedVal))
                {
                    if(m_fAsyncMode)
                    {
                        pBucket->m_rgKeys[i] = DELETED; // mark the key as DELETED
                    }
                    else
                    {
                        pBucket->m_rgKeys[i] = EMPTY;// otherwise mark the entry as empty
                        pBucket->SetFreeSlots();
                    }
                    m_cbDeletes++;  // track the deletes

                    ProfileLookup(ntry,storedVal); //no-op in non HASHTABLE_PROFILE code

                    // return the stored value
                    return storedVal;
                }
            }
        }

        seed += incr;
        if(!pBucket->IsCollision())
            break;
    }   // for ntry loop

    // not found
    ProfileLookup(ntry,INVALIDENTRY); //no-op in non HASHTABLE_PROFILE code

#ifdef _DEBUG
    ASSERT (m_pCompare != NULL || (UPTR) INVALIDENTRY == LookupValue (key,value));
    // check proper concurrent use of the hash table in synchronous mode
#endif // _DEBUG

    return INVALIDENTRY;
}


//---------------------------------------------------------------------
//  UPTR HashMap::Gethash (UPTR key)
//  use this for lookups with unique keys
// don't need to pass an input value to perform the lookup
//
UPTR HashMap::Gethash (UPTR key)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    return LookupValue(key,NULL);
}


//---------------------------------------------------------------------
//  UPTR PutEntry (Bucket* rgBuckets, UPTR key, UPTR value)
//  helper used by expand method below

UPTR HashMap::PutEntry (Bucket* rgBuckets, UPTR key, UPTR value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    ASSERT (value > 0);
    ASSERT (key > DELETED);

    DWORD size = GetSize(rgBuckets);
    UINT seed, incr;
    HashFunction(key, size, seed, incr);

    UPTR ntry;
    for (ntry =0; ntry < size; ntry++)
    {
        Bucket* pBucket = &rgBuckets[seed % size];
        if(pBucket->InsertValue(key,value))
        {
            return ntry;
        }

        seed += incr;
    } // for ntry loop
    _ASSERTE(!"Hash table insert failed.  Bug in PutEntry or the code that resizes the hash table?");
    return INVALIDENTRY;
}

//---------------------------------------------------------------------
//
//  UPTR HashMap::NewSize()
//  compute the new size based on the number of free slots
//
inline
UPTR HashMap::NewSize()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    ASSERT(m_cbInserts >= m_cbDeletes);
    UPTR cbValidSlots = m_cbInserts-m_cbDeletes;
    UPTR cbNewSlots = m_cbInserts > m_cbPrevSlotsInUse ? m_cbInserts - m_cbPrevSlotsInUse : 0;

    ASSERT(cbValidSlots >=0 );
    if (cbValidSlots == 0)
        return g_rgPrimes[0]; // Minimum size for this hash table.

    UPTR cbTotalSlots = (m_fAsyncMode) ? (UPTR)(cbValidSlots*3/2+cbNewSlots*.6) : cbValidSlots*3/2;

    //UPTR cbTotalSlots = cbSlotsInUse*3/2+m_cbDeletes;

    UPTR iPrimeIndex;
    for (iPrimeIndex = 0; iPrimeIndex < g_rgNumPrimes; iPrimeIndex++)
    {
        if (g_rgPrimes[iPrimeIndex] > cbTotalSlots)
        {
            return iPrimeIndex;
        }
    }
    ASSERT(iPrimeIndex == g_rgNumPrimes);
    ASSERT(0 && !"Hash table walked beyond end of primes array");
    return g_rgNumPrimes - 1;
}

//---------------------------------------------------------------------
//  void HashMap::Rehash()
//  Rehash the hash table, create a new array of buckets and rehash
// all non deleted values from the previous array
//
void HashMap::Rehash()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    // BROKEN: This is called for the RCWCache on the GC thread
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(m_fAsyncMode);

    _ASSERTE (!g_fEEStarted || !m_fAsyncMode || GetThreadNULLOk() == NULL || GetThread()->PreemptiveGCDisabled());
    _ASSERTE (OwnLock());

    UPTR newPrimeIndex = NewSize();

    ASSERT(newPrimeIndex < g_rgNumPrimes);

    if ((m_iPrimeIndex == newPrimeIndex) && (m_cbDeletes == 0))
    {
        return;
    }

    m_iPrimeIndex = newPrimeIndex;

    DWORD cbNewSize = g_rgPrimes[m_iPrimeIndex];

    Bucket* rgBuckets = Buckets();
    UPTR cbCurrSize =   GetSize(rgBuckets);

    S_SIZE_T cbNewBuckets = (S_SIZE_T(cbNewSize) + S_SIZE_T(1)) * S_SIZE_T(sizeof(Bucket));

    if (cbNewBuckets.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    Bucket* rgNewBuckets = (Bucket *) new BYTE[cbNewBuckets.Value()];
    memset (rgNewBuckets, 0, cbNewBuckets.Value());
    SetSize(rgNewBuckets, cbNewSize);

    // current valid slots
    UPTR cbValidSlots = m_cbInserts-m_cbDeletes;
    m_cbInserts = cbValidSlots; // reset insert count to the new valid count
    m_cbPrevSlotsInUse = cbValidSlots; // track the previous delete count
    m_cbDeletes = 0;            // reset delete count
    // rehash table into it

    if (cbValidSlots) // if there are valid slots to be rehashed
    {
        for (unsigned int nb = 0; nb < cbCurrSize; nb++)
        {
            for (unsigned int i = 0; i < SLOTS_PER_BUCKET; i++)
            {
                UPTR key =rgBuckets[nb].m_rgKeys[i];
                if (key > DELETED)
                {
#ifdef HASHTABLE_PROFILE
                    UPTR ntry =
#endif
                    PutEntry (rgNewBuckets+1, key, rgBuckets[nb].GetValue (i));
                    #ifdef HASHTABLE_PROFILE
                        if(ntry >=8)
                            m_cbInsertProbesGt8++;
                    #endif // HASHTABLE_PROFILE

                        // check if we can bail out
                    if (--cbValidSlots == 0)
                        goto LDone; // break out of both the loops
                }
            } // for i =0 thru SLOTS_PER_BUCKET
        } //for all buckets
    }


LDone:

    Bucket* pObsoleteTables = m_rgBuckets;

    // memory barrier, to replace the pointer to array of bucket
    MemoryBarrier();

    // replace the old array with the new one.
    m_rgBuckets = rgNewBuckets;

    #ifdef HASHTABLE_PROFILE
        m_cbRehash++;
        m_cbRehashSlots+=m_cbInserts;
        m_cbObsoleteTables++; // track statistics
        m_cbTotalBuckets += (cbNewSize+1);
    #endif // HASHTABLE_PROFILE

#ifdef _DEBUG

    unsigned nb;
    if (m_fAsyncMode)
    {
        // for all non deleted keys in the old table, make sure the corresponding values
        // are in the new lookup table

        for (nb = 1; nb <= ((size_t*)pObsoleteTables)[0]; nb++)
        {
            for (unsigned int i =0; i < SLOTS_PER_BUCKET; i++)
            {
                if (pObsoleteTables[nb].m_rgKeys[i] > DELETED)
                {
                    UPTR value = pObsoleteTables[nb].GetValue (i);
                    // make sure the value is present in the new table
                    ASSERT (m_pCompare != NULL || value == LookupValue (pObsoleteTables[nb].m_rgKeys[i], value));
                }
            }
        }
    }

    // make sure there are no deleted entries in the new lookup table
    // if the compare function provided is null, then keys must be unique
    for (nb = 0; nb < cbNewSize; nb++)
    {
        for (unsigned int i = 0; i < SLOTS_PER_BUCKET; i++)
        {
            UPTR keyv = Buckets()[nb].m_rgKeys[i];
            ASSERT (keyv != DELETED);
            if (m_pCompare == NULL && keyv != EMPTY)
            {
                ASSERT ((Buckets()[nb].GetValue (i)) == Gethash (keyv));
            }
        }
    }
#endif // _DEBUG

    if (m_fAsyncMode)
    {
        // If we are allowing asynchronous reads, we must delay bucket cleanup until GC time.
        SyncClean::AddHashMap (pObsoleteTables);
    }
    else
    {
        Bucket* pBucket = pObsoleteTables;
        while (pBucket) {
            Bucket* pNextBucket = NextObsolete(pBucket);
            delete [] pBucket;
            pBucket = pNextBucket;
        }
    }

}

//---------------------------------------------------------------------
//  void HashMap::Compact()
//  delete obsolete tables, try to compact deleted slots by sliding entries
//  in the bucket, note we can slide only if the bucket's collison bit is reset
//  otherwise the lookups will break
//  @perf, use the m_cbDeletes to m_cbInserts ratio to reduce the size of the hash
//   table
//
void HashMap::Compact()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    _ASSERTE (OwnLock());

    //
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(m_fAsyncMode);
    ASSERT(m_rgBuckets != NULL);

    // Try to resize if that makes sense (reduce the size of the table), but
    // don't fail the operation simply because we've run out of memory.
    UPTR iNewIndex = NewSize();
    if (iNewIndex != m_iPrimeIndex)
    {
        EX_TRY
        {
            FAULT_NOT_FATAL();
            Rehash();
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions)
    }

    //compact deleted slots, mark them as EMPTY

    if (m_cbDeletes)
    {
        UPTR cbCurrSize = GetSize(Buckets());
        Bucket *pBucket = Buckets();
        Bucket *pSentinel;

        for (pSentinel = pBucket+cbCurrSize; pBucket < pSentinel; pBucket++)
        {   //loop thru all buckets
            for (unsigned int i = 0; i < SLOTS_PER_BUCKET; i++)
            {   //loop through all slots
                if (pBucket->m_rgKeys[i] == DELETED)
                {
                    pBucket->m_rgKeys[i] = EMPTY;
                    pBucket->SetFreeSlots(); // mark the bucket as containing
                                             // free slots

                    // Need to decrement insert and delete counts at the same
                    // time to preserve correct live count.
                    _ASSERTE(m_cbInserts >= m_cbDeletes);
                    --m_cbInserts;

                    if(--m_cbDeletes == 0) // decrement count
                        return;
                }
            }
        }
    }

}

#ifdef _DEBUG
// A thread must own a lock for a hash if it is a writer.
BOOL HashMap::OwnLock()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    DEBUG_ONLY_FUNCTION;

    if (m_pfnLockOwner == NULL) {
        return m_writerThreadId.IsCurrentThread();
    }
    else {
        BOOL ret = m_pfnLockOwner(m_lockData);
        if (!ret) {
            if (Debug_IsLockedViaThreadSuspension()) {
                ret = TRUE;
            }
        }
        return ret;
    }
}
#endif // _DEBUG

#ifdef HASHTABLE_PROFILE
//---------------------------------------------------------------------
//  void HashMap::DumpStatistics()
//  dump statistics collected in profile mode
//
void HashMap::DumpStatistics()
{
    LIMITED_METHOD_CONTRACT;

    cout << "\n Hash Table statistics "<< endl;
    cout << "--------------------------------------------------" << endl;

    cout << "Current Insert count         " << m_cbInserts << endl;
    cout << "Current Delete count         "<< m_cbDeletes << endl;

    cout << "Current # of tables " << m_cbObsoleteTables << endl;
    cout << "Total # of times Rehashed " << m_cbRehash<< endl;
    cout << "Total # of slots rehashed " << m_cbRehashSlots << endl;

    cout << "Insert : Probes gt. 8 during rehash " << m_cbInsertProbesGt8 << endl;

    cout << " Max # of probes for a failed lookup " << maxFailureProbe << endl;

    cout << "Prime Index " << m_iPrimeIndex << endl;
    cout <<  "Current Buckets " << g_rgPrimes[m_iPrimeIndex]+1 << endl;

    cout << "Total Buckets " << m_cbTotalBuckets << endl;

    cout << " Lookup Probes " << endl;
    for (unsigned i = 0; i < HASHTABLE_LOOKUP_PROBES_DATA; i++)
    {
        cout << "# Probes:" << i << " #entries:" << m_rgLookupProbes[i] << endl;
    }
    cout << "\n--------------------------------------------------" << endl;
}
#endif // HASHTABLE_PROFILE

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
HashMap::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Assumed to be embedded, so no this enumeration.

    if (m_rgBuckets.IsValid())
    {
        ULONG32 numBuckets = (ULONG32)GetSize(Buckets()) + 1;
        DacEnumMemoryRegion(dac_cast<TADDR>(m_rgBuckets),
                            numBuckets * sizeof(Bucket));

        for (size_t i = 0; i < numBuckets; i++)
        {
            PTR_Bucket bucket = m_rgBuckets + i;
            if (bucket.IsValid())
            {
                bucket.EnumMem();
            }
        }
    }
}

#endif // DACCESS_COMPILE

#if 0 // Perf test code, enabled on-demand for private testing.
#ifndef DACCESS_COMPILE
// This is for testing purposes only!
void HashMap::HashMapTest()
{
    printf("HashMap test\n");

    const unsigned int MinValue = 2;  // Deleted is reserved, and is 1.
    const unsigned int MinThreshold = 10000;
    const unsigned int MaxThreshold = 30000;
    HashMap * table = new HashMap();
    Crst m_lock("HashMap", CrstSyncHashLock, CrstFlags(CRST_REENTRANCY | CRST_UNSAFE_ANYMODE));
    CrstHolder holder(&m_lock);
    LockOwner lock = {&m_lock, IsOwnerOfCrst};
    table->Init(10, (CompareFnPtr) NULL, false, &lock);
    for(unsigned int i=MinValue; i < MinThreshold; i++)
        table->InsertValue(i, i);
    printf("Added %d values.\n", MinThreshold);
    //table.DumpStatistics();

    LookupPerfTest(table, MinThreshold);

    INT64 t0 = GetTickCount();
    INT64 t1;
    for(int rep = 0; rep < 10000000; rep++) {
        for(unsigned int i=MinThreshold; i < MaxThreshold; i++) {
            table->InsertValue(rep + i, rep + i);
        }
        for(unsigned int i=MinThreshold; i < MaxThreshold; i++) {
            table->DeleteValue(rep + i, rep + i);
        }
        for(unsigned int i=MinValue; i < MinThreshold; i++)
            table->DeleteValue(i, i);
        for(unsigned int i=MinValue; i < MinThreshold; i++)
            table->InsertValue(i, i);

        if (rep % 500 == 0) {
            t1 = GetTickCount();
            printf("Repetition %d, took %d ms\n", rep, (int) (t1-t0));
            t0 = t1;
            LookupPerfTest(table, MinThreshold);
            //table.DumpStatistics();
        }
    }
    delete table;
}

// For testing purposes only.
void HashMap::LookupPerfTest(HashMap * table, const unsigned int MinThreshold)
{
    INT64 t0 = GetTickCount();
    for(int rep = 0; rep < 1000; rep++) {
        for(unsigned int i=2; i<MinThreshold; i++) {
            UPTR v = table->LookupValue(i, i);
            if (v != i) {
                printf("LookupValue didn't return the expected value!");
                _ASSERTE(v == i);
            }
        }
    }
    INT64 t1 = GetTickCount();
    for(unsigned int i = MinThreshold * 80; i < MinThreshold * 80 + 1000; i++)
        table->LookupValue(i, i);
    //cout << "Lookup perf test (1000 * " << MinThreshold << ": " << (t1-t0) << " ms." << endl;
#ifdef HASHTABLE_PROFILE
    printf("Lookup perf test time: %d ms  table size: %d  max failure probe: %d  longest collision chain: %d\n", (int) (t1-t0), (int) table->GetSize(table->Buckets()), (int) table->maxFailureProbe, (int) table->m_cbMaxCollisionLength);
    table->DumpStatistics();
#else // !HASHTABLE_PROFILE
    printf("Lookup perf test time: %d ms   table size: %d\n", (int) (t1-t0), table->GetSize(table->Buckets()));
#endif // !HASHTABLE_PROFILE
}
#endif // !DACCESS_COMPILE
#endif // 0 // Perf test code, enabled on-demand for private testing.
