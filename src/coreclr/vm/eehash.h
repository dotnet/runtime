// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
//emp
// File: eehash.h
//
// Provides hash table functionality needed in the EE - intended to be replaced later with better
// algorithms, but which have the same interface.
//
// Two requirements are:
//
// 1. Any number of threads can be reading the hash table while another thread is writing, without error.
// 2. Only one thread can write at a time.
// 3. When calling ReplaceValue(), a reader will get the old value, or the new value, but not something
//    in between.
// 4. DeleteValue() is an unsafe operation - no other threads can be in the hash table when this happens.
//

#ifndef _EE_HASH_H
#define _EE_HASH_H

#include "exceptmacros.h"
#include "syncclean.hpp"

#include "util.hpp"

class AllocMemTracker;
class ClassLoader;
struct LockOwner;
class NameHandle;
class SigTypeContext;

// The "blob" you get to store in the hash table

typedef PTR_VOID HashDatum;

// The heap that you want the allocation to be done in

typedef void* AllocationHeap;


// One of these is present for each element in the table.
// Update the SIZEOF_EEHASH_ENTRY macro below if you change this
// struct

typedef struct EEHashEntry EEHashEntry_t;
typedef DPTR(EEHashEntry_t) PTR_EEHashEntry_t;
struct EEHashEntry
{
    PTR_EEHashEntry_t   pNext;
    DWORD               dwHashValue;
    HashDatum           Data;
    BYTE                Key[1]; // The key is stored inline
};

// The key[1] is a place holder for the key
// SIZEOF_EEHASH_ENTRY is the size of struct up to (and not including) the key
#define SIZEOF_EEHASH_ENTRY (offsetof(EEHashEntry,Key[0]))


// Struct to hold a client's iteration state
struct EEHashTableIteration;

class GCHeap;

// Generic hash table.

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
class EEHashTableBase
{
public:


    BOOL            Init(DWORD dwNumBuckets, LockOwner *pLock, AllocationHeap pHeap = 0,BOOL CheckThreadSafety = TRUE);

    void            InsertValue(KeyType pKey, HashDatum Data, BOOL bDeepCopyKey = bDefaultCopyIsDeep);
    void            InsertKeyAsValue(KeyType pKey, BOOL bDeepCopyKey = bDefaultCopyIsDeep);
    BOOL            DeleteValue(KeyType pKey);
    BOOL            ReplaceValue(KeyType pKey, HashDatum Data);
    BOOL            ReplaceKey(KeyType pOldKey, KeyType pNewKey);
    void            ClearHashTable();
    void            EmptyHashTable();
    BOOL            IsEmpty();
    void            Destroy();

    // Reader functions. Please place any functions that can be called from the
    // reader threads here.
    BOOL            GetValue(KeyType pKey, HashDatum *pData);
    BOOL            GetValue(KeyType pKey, HashDatum *pData, DWORD hashValue);


    // A fast inlinable flavor of GetValue that can return false instead of the actual item
    // if there is race with updating of the hashtable. Callers of GetValueSpeculative
    // should fall back to the slow GetValue if GetValueSpeculative returns false.
    // Assumes that we are in cooperative mode already. For performance-sensitive codepaths.
    BOOL            GetValueSpeculative(KeyType pKey, HashDatum *pData);
    BOOL            GetValueSpeculative(KeyType pKey, HashDatum *pData, DWORD hashValue);

    DWORD           GetHash(KeyType Key);
    DWORD           GetCount();

    // Walk through all the entries in the hash table, in meaningless order, without any
    // synchronization.
    //
    //           IterateStart()
    //           while (IterateNext())
    //              IterateGetKey();
    //
    // This is guaranteed to be DeleteValue-friendly if you advance the iterator before
    // deletig, i.e. if used in the following pattern:
    //
    // IterateStart();
    // BOOL keepGoing = IterateNext();
    // while(keepGoing)
    // {
    //      key = IterateGetKey();
    //      keepGoing = IterateNext();
    //     ...
    //         DeleteValue(key);
    //       ..
    //  }
    void            IterateStart(EEHashTableIteration *pIter);
    BOOL            IterateNext(EEHashTableIteration *pIter);
    KeyType         IterateGetKey(EEHashTableIteration *pIter);
    HashDatum       IterateGetValue(EEHashTableIteration *pIter);
#ifdef _DEBUG
    void  SuppressSyncCheck()
    {
        LIMITED_METHOD_CONTRACT;
        m_CheckThreadSafety=FALSE;
    }
#endif
protected:
    BOOL            GrowHashTable();
    EEHashEntry_t * FindItem(KeyType pKey);
    EEHashEntry_t * FindItem(KeyType pKey, DWORD hashValue);

    // A fast inlinable flavor of FindItem that can return null instead of the actual item
    // if there is race with updating of the hashtable. Callers of FindItemSpeculative
    // should fall back to the slow FindItem if FindItemSpeculative returns null.
    // Assumes that we are in cooperative mode already. For performance-sensitive codepaths.
    EEHashEntry_t * FindItemSpeculative(KeyType pKey, DWORD hashValue);

    // Double buffer to fix the race condition of growhashtable (the update
    // of m_pBuckets and m_dwNumBuckets has to be atomic, so we double buffer
    // the structure and access it through a pointer, which can be updated
    // atomically. The union is in order to not change the SOS macros.

    struct BucketTable
    {
        DPTR(PTR_EEHashEntry_t) m_pBuckets;       // Pointer to first entry for each bucket
        DWORD                   m_dwNumBuckets;
#ifdef TARGET_64BIT
        UINT64                  m_dwNumBucketsMul; // "Fast Mod" multiplier for "X % m_dwNumBuckets"
#endif
    } m_BucketTable[2];
    typedef DPTR(BucketTable) PTR_BucketTable;

    // In a function we MUST only read this value ONCE, as the writer thread can change
    // the value asynchronously. We make this member volatile the compiler won't do copy propagation
    // optimizations that can make this read happen more than once. Note that we  only need
    // this property for the readers. As they are the ones that can have
    // this variable changed (note also that if the variable was enregistered we wouldn't
    // have any problem)
    // BE VERY CAREFUL WITH WHAT YOU DO WITH THIS VARIABLE AS USING IT BADLY CAN CAUSE
    // RACING CONDITIONS
    VolatilePtr<BucketTable, PTR_BucketTable> m_pVolatileBucketTable;


    DWORD                   m_dwNumEntries;
    AllocationHeap          m_Heap;
    Volatile<LONG>          m_bGrowing;
#ifdef _DEBUG
    LPVOID          m_lockData;
    FnLockOwner     m_pfnLockOwner;

    EEThreadId      m_writerThreadId;
    BOOL            m_CheckThreadSafety;

#endif

#ifdef _DEBUG_IMPL
    // A thread must own a lock for a hash if it is a writer.
    BOOL OwnLock();
#endif  // _DEBUG_IMPL
};

template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
class EEHashTable : public EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>
{
public:
    EEHashTable()
    {
        LIMITED_METHOD_CONTRACT;
        this->m_BucketTable[0].m_pBuckets        = NULL;
        this->m_BucketTable[0].m_dwNumBuckets    = 0;
        this->m_BucketTable[1].m_pBuckets        = NULL;
        this->m_BucketTable[1].m_dwNumBuckets    = 0;
#ifdef TARGET_64BIT
        this->m_BucketTable[0].m_dwNumBucketsMul = 0;
        this->m_BucketTable[1].m_dwNumBucketsMul = 0;
#endif

#ifndef DACCESS_COMPILE
        this->m_pVolatileBucketTable = NULL;
#endif
        this->m_dwNumEntries = 0;
        this->m_bGrowing = 0;
#ifdef _DEBUG
        this->m_lockData = NULL;
        this->m_pfnLockOwner = NULL;
#endif
    }

    ~EEHashTable()
    {
        WRAPPER_NO_CONTRACT;
        this->Destroy();
    }
};

/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */
template <class KeyType, class Helper, BOOL bDefaultCopyIsDeep>
class EEHashTableStatic : public EEHashTableBase<KeyType, Helper, bDefaultCopyIsDeep>
{
};

class EEIntHashTableHelper
{
public:
    static EEHashEntry_t *AllocateEntry(int iKey, BOOL bDeepCopy, AllocationHeap pHeap = 0)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_NOTRIGGER);
            INJECT_FAULT(return NULL;);
        }
        CONTRACTL_END

        _ASSERTE(!bDeepCopy && "Deep copy is not supported by the EEPtrHashTableHelper");

        EEHashEntry_t *pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(int)];
        if (!pEntry)
            return NULL;
        *((int*) pEntry->Key) = iKey;

        return pEntry;
    }

    static void DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap pHeap = 0)
    {
        LIMITED_METHOD_CONTRACT;

        // Delete the entry.
        delete [] (BYTE*) pEntry;
    }

    static BOOL CompareKeys(EEHashEntry_t *pEntry, int iKey)
    {
        LIMITED_METHOD_CONTRACT;

        return *((int*)pEntry->Key) == iKey;
    }

    static DWORD Hash(int iKey)
    {
        LIMITED_METHOD_CONTRACT;

        return (DWORD)iKey;
    }

    static int GetKey(EEHashEntry_t *pEntry)
    {
        LIMITED_METHOD_CONTRACT;

        return *((int*) pEntry->Key);
    }
};
typedef EEHashTable<int, EEIntHashTableHelper, FALSE> EEIntHashTable;

typedef struct PtrPlusInt
{
	void* pValue;
	int iValue;
} *PPtrPlusInt;

class EEPtrPlusIntHashTableHelper
{
public:
    static EEHashEntry_t *AllocateEntry(PtrPlusInt ppiKey, BOOL bDeepCopy, AllocationHeap pHeap = 0)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_NOTRIGGER);
            INJECT_FAULT(return NULL;);
        }
        CONTRACTL_END

        _ASSERTE(!bDeepCopy && "Deep copy is not supported by the EEPtrPlusIntHashTableHelper");

        EEHashEntry_t *pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(PtrPlusInt)];
        if (!pEntry)
            return NULL;
        *((PPtrPlusInt) pEntry->Key) = ppiKey;

        return pEntry;
    }

    static void DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap pHeap = 0)
    {
        LIMITED_METHOD_CONTRACT;

        // Delete the entry.
        delete [] (BYTE*) pEntry;
    }

    static BOOL CompareKeys(EEHashEntry_t *pEntry, PtrPlusInt ppiKey)
    {
        LIMITED_METHOD_CONTRACT;

        return (((PPtrPlusInt)pEntry->Key)->pValue == ppiKey.pValue) &&
               (((PPtrPlusInt)pEntry->Key)->iValue == ppiKey.iValue);
    }

    static DWORD Hash(PtrPlusInt ppiKey)
    {
        LIMITED_METHOD_CONTRACT;

		return (DWORD)ppiKey.iValue ^
#ifdef TARGET_X86
        	(DWORD)(size_t) ppiKey.pValue;
#else
        // <TODO> IA64: Is this a good hashing mechanism on IA64?</TODO>
        	(DWORD)(((size_t) ppiKey.pValue) >> 3);
#endif
    }

    static PtrPlusInt GetKey(EEHashEntry_t *pEntry)
    {
        LIMITED_METHOD_CONTRACT;

        return *((PPtrPlusInt) pEntry->Key);
    }
};

typedef EEHashTable<PtrPlusInt, EEPtrPlusIntHashTableHelper, FALSE> EEPtrPlusIntHashTable;

// UTF8 string hash table. The UTF8 strings are NULL terminated.

class EEUtf8HashTableHelper
{
public:
    static EEHashEntry_t * AllocateEntry(LPCUTF8 pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void            DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap Heap);
    static BOOL            CompareKeys(EEHashEntry_t *pEntry, LPCUTF8 pKey);
    static DWORD           Hash(LPCUTF8 pKey);
    static LPCUTF8         GetKey(EEHashEntry_t *pEntry);
};

typedef EEHashTable<LPCUTF8, EEUtf8HashTableHelper, TRUE> EEUtf8StringHashTable;
typedef DPTR(EEUtf8StringHashTable) PTR_EEUtf8StringHashTable;

// Unicode String hash table - the keys are UNICODE strings which may
// contain embedded nulls.  An EEStringData struct is used for the key
// which contains the length of the item.  Note that this string is
// not necessarily null terminated and should never be treated as such.
const DWORD ONLY_LOW_CHARS_MASK = 0x80000000;

class EEStringData
{
private:
    LPCWSTR         szString;           // The string data.
    DWORD           cch;                // Characters in the string.
#ifdef _DEBUG
    BOOL            bDebugOnlyLowChars;      // Does the string contain only characters less than 0x80?
    DWORD           dwDebugCch;
#endif // _DEBUG

public:
    // explicilty initialize cch to 0 because SetCharCount uses cch
    EEStringData() : cch(0)
    {
        LIMITED_METHOD_CONTRACT;

        SetStringBuffer(NULL);
        SetCharCount(0);
        SetIsOnlyLowChars(FALSE);
    };
    EEStringData(DWORD cchString, LPCWSTR str) : cch(0)
    {
        LIMITED_METHOD_CONTRACT;

        SetStringBuffer(str);
        SetCharCount(cchString);
        SetIsOnlyLowChars(FALSE);
    };
    EEStringData(DWORD cchString, LPCWSTR str, BOOL onlyLow) : cch(0)
    {
        LIMITED_METHOD_CONTRACT;

        SetStringBuffer(str);
        SetCharCount(cchString);
        SetIsOnlyLowChars(onlyLow);
    };
    inline ULONG GetCharCount() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE ((cch & ~ONLY_LOW_CHARS_MASK) == dwDebugCch);
        return (cch & ~ONLY_LOW_CHARS_MASK);
    }
    inline void SetCharCount(ULONG _cch)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
        dwDebugCch = _cch;
#endif // _DEBUG
        cch = ((DWORD)_cch) | (cch & ONLY_LOW_CHARS_MASK);
    }
    inline LPCWSTR GetStringBuffer() const
    {
        LIMITED_METHOD_CONTRACT;

        return (szString);
    }
    inline void SetStringBuffer(LPCWSTR _szString)
    {
        LIMITED_METHOD_CONTRACT;

        szString = _szString;
    }
    inline BOOL GetIsOnlyLowChars() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(bDebugOnlyLowChars == ((cch & ONLY_LOW_CHARS_MASK) ? TRUE : FALSE));
        return ((cch & ONLY_LOW_CHARS_MASK) ? TRUE : FALSE);
    }
    inline void SetIsOnlyLowChars(BOOL bIsOnlyLowChars)
    {
        LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
        bDebugOnlyLowChars = bIsOnlyLowChars;
#endif // _DEBUG
        bIsOnlyLowChars ? (cch |= ONLY_LOW_CHARS_MASK) : (cch &= ~ONLY_LOW_CHARS_MASK);
    }
};

class EEUnicodeHashTableHelper
{
public:
    static EEHashEntry_t * AllocateEntry(EEStringData *pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void            DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap Heap);
    static BOOL            CompareKeys(EEHashEntry_t *pEntry, EEStringData *pKey);
    static DWORD           Hash(EEStringData *pKey);
    static EEStringData *  GetKey(EEHashEntry_t *pEntry);
    static void            ReplaceKey(EEHashEntry_t *pEntry, EEStringData *pNewKey);
};

typedef EEHashTable<EEStringData *, EEUnicodeHashTableHelper, TRUE> EEUnicodeStringHashTable;


class EEUnicodeStringLiteralHashTableHelper
{
public:
    static EEHashEntry_t * AllocateEntry(EEStringData *pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void            DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap Heap);
    static BOOL            CompareKeys(EEHashEntry_t *pEntry, EEStringData *pKey);
    static DWORD           Hash(EEStringData *pKey);
    static void            ReplaceKey(EEHashEntry_t *pEntry, EEStringData *pNewKey);
};

typedef EEHashTable<EEStringData *, EEUnicodeStringLiteralHashTableHelper, TRUE> EEUnicodeStringLiteralHashTable;


// Generic pointer hash table helper.

template <class KeyPointerType>
class EEPtrHashTableHelper
{
public:
    static EEHashEntry_t *AllocateEntry(KeyPointerType pKey, BOOL bDeepCopy, AllocationHeap Heap)
    {
        CONTRACTL
        {
            WRAPPER(THROWS);
            WRAPPER(GC_NOTRIGGER);
            INJECT_FAULT(return FALSE;);
        }
        CONTRACTL_END

        _ASSERTE(!bDeepCopy && "Deep copy is not supported by the EEPtrHashTableHelper");
        _ASSERTE(sizeof(KeyPointerType) == sizeof(void *) && "KeyPointerType must be a pointer type");

        EEHashEntry_t *pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(KeyPointerType)];
        if (!pEntry)
            return NULL;
        *((KeyPointerType*)pEntry->Key) = pKey;

        return pEntry;
    }

    static void DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap Heap)
    {
        LIMITED_METHOD_CONTRACT;

        // Delete the entry.
        delete [] (BYTE*) pEntry;
    }

    static BOOL CompareKeys(EEHashEntry_t *pEntry, KeyPointerType pKey)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        KeyPointerType pEntryKey = *((KeyPointerType*)pEntry->Key);
        return pEntryKey == pKey;
    }

    static DWORD Hash(KeyPointerType pKey)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

#ifdef TARGET_X86
        return (DWORD)(size_t) dac_cast<TADDR>(pKey);
#else
        // <TODO> IA64: Is this a good hashing mechanism on IA64?</TODO>
        return (DWORD)(((size_t) dac_cast<TADDR>(pKey)) >> 3);
#endif
    }

    static KeyPointerType GetKey(EEHashEntry_t *pEntry)
    {
        LIMITED_METHOD_CONTRACT;

        return *((KeyPointerType*)pEntry->Key);
    }
};

typedef EEHashTable<PTR_VOID, EEPtrHashTableHelper<PTR_VOID>, FALSE> EEPtrHashTable;
typedef DPTR(EEPtrHashTable) PTR_EEPtrHashTable;

// Define a hash of generic instantiations (represented by a SigTypeContext).
class EEInstantiationHashTableHelper
{
public:
    static EEHashEntry_t *AllocateEntry(const SigTypeContext *pKey, BOOL bDeepCopy, AllocationHeap pHeap = 0);
    static void DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap pHeap = 0);
    static BOOL CompareKeys(EEHashEntry_t *pEntry, const SigTypeContext *pKey);
    static DWORD Hash(const SigTypeContext *pKey);
    static const SigTypeContext *GetKey(EEHashEntry_t *pEntry);
};
typedef EEHashTable<const SigTypeContext*, EEInstantiationHashTableHelper, FALSE> EEInstantiationHashTable;

// ComComponentInfo hashtable.

struct ClassFactoryInfo
{
    GUID    m_clsid;
    PCWSTR  m_strServerName;
};

class EEClassFactoryInfoHashTableHelper
{
public:
    static EEHashEntry_t *AllocateEntry(ClassFactoryInfo *pKey, BOOL bDeepCopy, AllocationHeap Heap);
    static void DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap Heap);
    static BOOL CompareKeys(EEHashEntry_t *pEntry, ClassFactoryInfo *pKey);
    static DWORD Hash(ClassFactoryInfo *pKey);
    static ClassFactoryInfo *GetKey(EEHashEntry_t *pEntry);
};

typedef EEHashTable<ClassFactoryInfo *, EEClassFactoryInfoHashTableHelper, TRUE> EEClassFactoryInfoHashTable;
// Struct to hold a client's iteration state
struct EEHashTableIteration
{
    DWORD              m_dwBucket;
    EEHashEntry_t     *m_pEntry;

#ifdef _DEBUG
    void              *m_pTable;
#endif
};

#endif /* _EE_HASH_H */
