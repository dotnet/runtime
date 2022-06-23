// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header:  Map used for interning of string literals.
**
===========================================================*/

#include "common.h"
#include "eeconfig.h"
#include "stringliteralmap.h"

/*
    Thread safety in GlobalStringLiteralMap / StringLiteralMap

    A single lock protects the N StringLiteralMap objects and single
    GlobalStringLiteralMap rooted in the SystemDomain at any time. It is

    SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal

    At one time each StringLiteralMap had it's own lock to protect
    the entry hash table as well, and Interlocked operations were done on the
    ref count of the contained StringLiteralEntries. But anything of import
    needed to be done under the global lock mentioned above or races would
    result. (For example, an app domain shuts down, doing final release on
    a StringLiteralEntry, but at that moment the entry is being handed out
    in another appdomain and addref'd only after the count went to 0.)

    The rule is:

    Any AddRef()/Release() calls on StringLiteralEntry need to be under the lock.
    Any insert/deletes from the StringLiteralMap or GlobalStringLiteralMap
    need to be done under the lock.

    The only thing you can do without the lock is look up an existing StringLiteralEntry
    in an StringLiteralMap hash table. This is true because these lookup calls
    will all come before destruction of the map, the hash table is safe for multiple readers,
    and we know the StringLiteralEntry so found 1) can't be destroyed because that table keeps
    an AddRef on it and 2) isn't internally modified once created.
*/

#define GLOBAL_STRING_TABLE_BUCKET_SIZE 128
#define INIT_NUM_APP_DOMAIN_STRING_BUCKETS 59
#define INIT_NUM_GLOBAL_STRING_BUCKETS 131

// assumes that memory pools's per block data is same as sizeof (StringLiteralEntry)
#define EEHASH_MEMORY_POOL_GROW_COUNT 128

StringLiteralEntryArray *StringLiteralEntry::s_EntryList = NULL;
DWORD StringLiteralEntry::s_UsedEntries = NULL;
StringLiteralEntry *StringLiteralEntry::s_FreeEntryList = NULL;

StringLiteralMap::StringLiteralMap()
: m_StringToEntryHashTable(NULL)
, m_MemoryPool(NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
}

void StringLiteralMap::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(this));
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    // Allocate the memory pool and set the initial count to quarter as grow count
    m_MemoryPool = new MemoryPool (SIZEOF_EEHASH_ENTRY, EEHASH_MEMORY_POOL_GROW_COUNT, EEHASH_MEMORY_POOL_GROW_COUNT/4);

    m_StringToEntryHashTable =  new EEUnicodeStringLiteralHashTable ();

    LockOwner lock = {&(SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal), IsOwnerOfCrst};
    if (!m_StringToEntryHashTable->Init(INIT_NUM_APP_DOMAIN_STRING_BUCKETS, &lock, m_MemoryPool))
        ThrowOutOfMemory();
}

StringLiteralMap::~StringLiteralMap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // We do need to take the globalstringliteralmap lock because we are manipulating
    // StringLiteralEntry objects that belong to it.
    // Note that we remember the current entry and release it only when the
    // enumerator has advanced to the next entry so that we don't endup deleteing the
    // current entry itself and killing the enumerator.

    if (m_StringToEntryHashTable != NULL)
    {
        // We need the global lock anytime we release StringLiteralEntry objects
        CrstHolder gch(&(SystemDomain::GetGlobalStringLiteralMapNoCreate()->m_HashTableCrstGlobal));

        StringLiteralEntry *pEntry = NULL;
        EEHashTableIteration Iter;

#ifdef _DEBUG
        m_StringToEntryHashTable->SuppressSyncCheck();
#endif

        m_StringToEntryHashTable->IterateStart(&Iter);
        if (m_StringToEntryHashTable->IterateNext(&Iter))
        {
            pEntry = (StringLiteralEntry*)m_StringToEntryHashTable->IterateGetValue(&Iter);

            while (m_StringToEntryHashTable->IterateNext(&Iter))
            {
                // Release the previous entry
                _ASSERTE(pEntry);
                pEntry->Release();

                // Set the
                pEntry = (StringLiteralEntry*)m_StringToEntryHashTable->IterateGetValue(&Iter);
            }
            // Release the last entry
            _ASSERTE(pEntry);
            pEntry->Release();
        }
        // else there were no entries.

        // Delete the hash table first. The dtor of the hash table would clean up all the entries.
        delete m_StringToEntryHashTable;
    }

    // Delete the pool later, since the dtor above would need it.
    if (m_MemoryPool != NULL)
        delete m_MemoryPool;
}



STRINGREF *StringLiteralMap::GetStringLiteral(EEStringData *pStringData, BOOL bAddIfNotFound, BOOL bAppDomainWontUnload)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(pStringData));
    }
    CONTRACTL_END;

    HashDatum Data;

    DWORD dwHash = m_StringToEntryHashTable->GetHash(pStringData);
    // Retrieve the string literal from the global string literal map.
    CrstHolder gch(&(SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal));

    // TODO: We can be more efficient by checking our local hash table now to see if
    // someone beat us to inserting it. (m_StringToEntryHashTable->GetValue(pStringData, &Data))
    // (Rather than waiting until after we look the string up in the global map)

    StringLiteralEntryHolder pEntry(SystemDomain::GetGlobalStringLiteralMap()->GetStringLiteral(pStringData, dwHash, bAddIfNotFound));

    _ASSERTE(pEntry || !bAddIfNotFound);

    // If pEntry is non-null then the entry exists in the Global map. (either we retrieved it or added it just now)
    if (pEntry)
    {
        // If the entry exists in the Global map and the appdomain wont ever unload then we really don't need to add a
        // hashentry in the appdomain specific map.
        // TODO: except that by not inserting into our local table we always take the global map lock
        // and come into this path, when we could succeed at a lock free lookup above.

        if (!bAppDomainWontUnload)
        {
            // Make sure some other thread has not already added it.
            if (!m_StringToEntryHashTable->GetValue(pStringData, &Data))
            {
                // Insert the handle to the string into the hash table.
                m_StringToEntryHashTable->InsertValue(pStringData, (LPVOID)pEntry, FALSE);
            }
            else
            {
                pEntry.Release(); //while we're still under lock
            }
        }
#ifdef _DEBUG
        else
        {
            LOG((LF_APPDOMAIN, LL_INFO10000, "Avoided adding String literal to appdomain map: size: %d bytes\n", pStringData->GetCharCount()));
        }
#endif
        pEntry.SuppressRelease();
        STRINGREF *pStrObj = NULL;
        // Retrieve the string objectref from the string literal entry.
        pStrObj = pEntry->GetStringObject();
        _ASSERTE(!bAddIfNotFound || pStrObj);
        return pStrObj;
    }
    // If the bAddIfNotFound flag is set then we better have a string
    // string object at this point.
    _ASSERTE(!bAddIfNotFound);
    return NULL;
}

STRINGREF *StringLiteralMap::GetInternedString(STRINGREF *pString, BOOL bAddIfNotFound, BOOL bAppDomainWontUnload)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(pString));
    }
    CONTRACTL_END;

    HashDatum Data;
    EEStringData StringData = EEStringData((*pString)->GetStringLength(), (*pString)->GetBuffer());

    DWORD dwHash = m_StringToEntryHashTable->GetHash(&StringData);
    if (m_StringToEntryHashTable->GetValue(&StringData, &Data, dwHash))
    {
        STRINGREF *pStrObj = NULL;
        pStrObj = ((StringLiteralEntry*)Data)->GetStringObject();
        _ASSERTE(!bAddIfNotFound || pStrObj);
        return pStrObj;

    }
    else
    {
        CrstHolder gch(&(SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal));

        // TODO: We can be more efficient by checking our local hash table now to see if
        // someone beat us to inserting it. (m_StringToEntryHashTable->GetValue(pStringData, &Data))
        // (Rather than waiting until after we look the string up in the global map)

        // Retrieve the string literal from the global string literal map.
        StringLiteralEntryHolder pEntry(SystemDomain::GetGlobalStringLiteralMap()->GetInternedString(pString, dwHash, bAddIfNotFound));

        _ASSERTE(pEntry || !bAddIfNotFound);

        // If pEntry is non-null then the entry exists in the Global map. (either we retrieved it or added it just now)
        if (pEntry)
        {
            // If the entry exists in the Global map and the appdomain wont ever unload then we really don't need to add a
            // hashentry in the appdomain specific map.
            // TODO: except that by not inserting into our local table we always take the global map lock
            // and come into this path, when we could succeed at a lock free lookup above.

            if (!bAppDomainWontUnload)
            {
                // Since GlobalStringLiteralMap::GetInternedString() could have caused a GC,
                // we need to recreate the string data.
                StringData = EEStringData((*pString)->GetStringLength(), (*pString)->GetBuffer());

                // Make sure some other thread has not already added it.
                if (!m_StringToEntryHashTable->GetValue(&StringData, &Data))
                {
                    // Insert the handle to the string into the hash table.
                    m_StringToEntryHashTable->InsertValue(&StringData, (LPVOID)pEntry, FALSE);
                }
                else
                {
                    pEntry.Release(); // while we're under lock
                }
            }
            pEntry.SuppressRelease();
            // Retrieve the string objectref from the string literal entry.
            STRINGREF *pStrObj = NULL;
            pStrObj = pEntry->GetStringObject();
            return pStrObj;
        }
    }
    // If the bAddIfNotFound flag is set then we better have a string
    // string object at this point.
    _ASSERTE(!bAddIfNotFound);

    return NULL;
}

GlobalStringLiteralMap::GlobalStringLiteralMap()
: m_StringToEntryHashTable(NULL)
, m_MemoryPool(NULL)
, m_HashTableCrstGlobal(CrstGlobalStrLiteralMap)
, m_PinnedHeapHandleTable(SystemDomain::System(), GLOBAL_STRING_TABLE_BUCKET_SIZE)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_PinnedHeapHandleTable.RegisterCrstDebug(&m_HashTableCrstGlobal);
#endif
}

GlobalStringLiteralMap::~GlobalStringLiteralMap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // if we are deleting the map then either it is shutdown time or else there was a race trying to create
    // the initial map and this one was the loser
    // (i.e. two threads made a map and the InterlockedCompareExchange failed for one of them and
    // now it is deleting the map)
    //
    // if it's not the main map, then the map we are deleting better be empty!

    // there must be *some* global table
    _ASSERTE(SystemDomain::GetGlobalStringLiteralMapNoCreate()  != NULL);

    if (SystemDomain::GetGlobalStringLiteralMapNoCreate() != this)
    {
        // if this isn't the real global table then it must be empty
        _ASSERTE(m_StringToEntryHashTable->IsEmpty());

        // Delete the hash table first. The dtor of the hash table would clean up all the entries.
        delete m_StringToEntryHashTable;
        // Delete the pool later, since the dtor above would need it.
        delete m_MemoryPool;
    }
    else
    {
        // We are shutting down, the OS will reclaim the memory from the StringLiteralEntries,
        // m_MemoryPool and m_StringToEntryHashTable.
        _ASSERTE(g_fProcessDetach);
    }
}

void GlobalStringLiteralMap::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    // Allocate the memory pool and set the initial count to quarter as grow count
    m_MemoryPool = new MemoryPool (SIZEOF_EEHASH_ENTRY, EEHASH_MEMORY_POOL_GROW_COUNT, EEHASH_MEMORY_POOL_GROW_COUNT/4);

    m_StringToEntryHashTable =  new EEUnicodeStringLiteralHashTable ();

    LockOwner lock = {&m_HashTableCrstGlobal, IsOwnerOfCrst};
    if (!m_StringToEntryHashTable->Init(INIT_NUM_GLOBAL_STRING_BUCKETS, &lock, m_MemoryPool))
        ThrowOutOfMemory();
}

StringLiteralEntry *GlobalStringLiteralMap::GetStringLiteral(EEStringData *pStringData, DWORD dwHash, BOOL bAddIfNotFound)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(pStringData));
        PRECONDITION(m_HashTableCrstGlobal.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    HashDatum Data;
    StringLiteralEntry *pEntry = NULL;

    if (m_StringToEntryHashTable->GetValueSpeculative(pStringData, &Data, dwHash)) // Since we hold the critical section here, we can safely use the speculative variant of GetValue
    {
        pEntry = (StringLiteralEntry*)Data;
        // If the entry is already in the table then addref it before we return it.
        if (pEntry)
            pEntry->AddRef();
    }
    else
    {
        if (bAddIfNotFound)
            pEntry = AddStringLiteral(pStringData);
    }

    return pEntry;
}

StringLiteralEntry *GlobalStringLiteralMap::GetInternedString(STRINGREF *pString, DWORD dwHash, BOOL bAddIfNotFound)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckPointer(pString));
        PRECONDITION(m_HashTableCrstGlobal.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    EEStringData StringData = EEStringData((*pString)->GetStringLength(), (*pString)->GetBuffer());

    HashDatum Data;
    StringLiteralEntry *pEntry = NULL;

    if (m_StringToEntryHashTable->GetValue(&StringData, &Data, dwHash))
    {
        pEntry = (StringLiteralEntry*)Data;
        // If the entry is already in the table then addref it before we return it.
        if (pEntry)
            pEntry->AddRef();
    }
    else
    {
        if (bAddIfNotFound)
            pEntry = AddInternedString(pString);
    }

    return pEntry;
}

#ifdef LOGGING
static void LogStringLiteral(_In_z_ const char* action, EEStringData *pStringData)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    int length = pStringData->GetCharCount();
    length = min(length, 100);
    WCHAR *szString = (WCHAR *)_alloca((length + 1) * sizeof(WCHAR));
    memcpyNoGCRefs((void*)szString, (void*)pStringData->GetStringBuffer(), length * sizeof(WCHAR));
    szString[length] = '\0';
    LOG((LF_APPDOMAIN, LL_INFO10000, "String literal \"%S\" %s to Global map, size %d bytes\n", szString, action, pStringData->GetCharCount()));
}
#endif

STRINGREF AllocateStringObject(EEStringData *pStringData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Create the COM+ string object.
    DWORD cCount = pStringData->GetCharCount();

    STRINGREF strObj = AllocateString(cCount);

    GCPROTECT_BEGIN(strObj)
    {
        // Copy the string constant into the COM+ string object.  The code
        // will add an extra null at the end for safety purposes, but since
        // we support embedded nulls, one should never treat the string as
        // null termianted.
        LPWSTR strDest = strObj->GetBuffer();
        memcpyNoGCRefs(strDest, pStringData->GetStringBuffer(), cCount*sizeof(WCHAR));
        strDest[cCount] = 0;
    }
    GCPROTECT_END();

    return strObj;
}
StringLiteralEntry *GlobalStringLiteralMap::AddStringLiteral(EEStringData *pStringData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(m_HashTableCrstGlobal.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    StringLiteralEntry *pRet;

    {
    PinnedHeapHandleBlockHolder pStrObj(&m_PinnedHeapHandleTable,1);
    // Create the COM+ string object.
    STRINGREF strObj = AllocateStringObject(pStringData);

    // Allocate a handle for the string.
    SetObjectReference(pStrObj[0], (OBJECTREF) strObj);


    // Allocate the StringLiteralEntry.
    StringLiteralEntryHolder pEntry(StringLiteralEntry::AllocateEntry(pStringData, (STRINGREF*)pStrObj[0]));
    pStrObj.SuppressRelease();
    // Insert the handle to the string into the hash table.
    m_StringToEntryHashTable->InsertValue(pStringData, (LPVOID)pEntry, FALSE);
    pEntry.SuppressRelease();
    pRet = pEntry;

#ifdef LOGGING
    LogStringLiteral("added", pStringData);
#endif
    }

    return pRet;
}

StringLiteralEntry *GlobalStringLiteralMap::AddInternedString(STRINGREF *pString)
{

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(m_HashTableCrstGlobal.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    EEStringData StringData = EEStringData((*pString)->GetStringLength(), (*pString)->GetBuffer());
    StringLiteralEntry *pRet;

    {
    PinnedHeapHandleBlockHolder pStrObj(&m_PinnedHeapHandleTable,1);
    SetObjectReference(pStrObj[0], (OBJECTREF) *pString);

    // Since the allocation might have caused a GC we need to re-get the
    // string data.
    StringData = EEStringData((*pString)->GetStringLength(), (*pString)->GetBuffer());

    StringLiteralEntryHolder pEntry(StringLiteralEntry::AllocateEntry(&StringData, (STRINGREF*)pStrObj[0]));
    pStrObj.SuppressRelease();

    // Insert the handle to the string into the hash table.
    m_StringToEntryHashTable->InsertValue(&StringData, (LPVOID)pEntry, FALSE);
    pEntry.SuppressRelease();
    pRet = pEntry;
    }

    return pRet;
}

void GlobalStringLiteralMap::RemoveStringLiteralEntry(StringLiteralEntry *pEntry)
{
   CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pEntry));
        PRECONDITION(m_HashTableCrstGlobal.OwnedByCurrentThread());
        PRECONDITION(CheckPointer(this));
    }
    CONTRACTL_END;

    // Remove the entry from the hash table.
    {
        GCX_COOP();

        EEStringData StringData;
        pEntry->GetStringData(&StringData);

        BOOL bSuccess;
        bSuccess = m_StringToEntryHashTable->DeleteValue(&StringData);
        // this assert is comented out to accomodate case when StringLiteralEntryHolder
        // releases this object after failed insertion into hash
        //_ASSERTE(bSuccess);

#ifdef LOGGING
        // We need to do this logging within the GCX_COOP(), as a gc will render
        // our StringData pointers stale.
        if (bSuccess)
        {
            LogStringLiteral("removed", &StringData);
        }
#endif

        // Release the object handle that the entry was using.
        STRINGREF *pObjRef = pEntry->GetStringObject();
        m_PinnedHeapHandleTable.ReleaseHandles((OBJECTREF*)pObjRef, 1);
    }

    // We do not delete the StringLiteralEntry itself that will be done in the
    // release method of the StringLiteralEntry.
}

StringLiteralEntry *StringLiteralEntry::AllocateEntry(EEStringData *pStringData, STRINGREF *pStringObj)
{
   CONTRACTL
    {
        THROWS;
        GC_TRIGGERS; // GC_TRIGGERS because in the precondition below GetGlobalStringLiteralMap() might need to create the map
        MODE_COOPERATIVE;
        PRECONDITION(SystemDomain::GetGlobalStringLiteralMap()->m_HashTableCrstGlobal.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Note: we don't synchronize here because allocateEntry is called when HashCrst is held.
    void *pMem = NULL;
    if (s_FreeEntryList != NULL)
    {
        pMem = s_FreeEntryList;
        s_FreeEntryList = s_FreeEntryList->m_pNext;
        _ASSERTE (((StringLiteralEntry*)pMem)->m_bDeleted);
    }
    else
    {
        if (s_EntryList == NULL || (s_UsedEntries >= MAX_ENTRIES_PER_CHUNK))
        {
            StringLiteralEntryArray *pNew = new StringLiteralEntryArray();
            pNew->m_pNext = s_EntryList;
            s_EntryList = pNew;
            s_UsedEntries = 0;
        }
        pMem = &(s_EntryList->m_Entries[s_UsedEntries++*sizeof(StringLiteralEntry)]);
    }
    _ASSERTE (pMem && "Unable to allocate String literal Entry");

    return new (pMem) StringLiteralEntry (pStringData, pStringObj);
}

void StringLiteralEntry::DeleteEntry (StringLiteralEntry *pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(SystemDomain::GetGlobalStringLiteralMapNoCreate()->m_HashTableCrstGlobal.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    _ASSERTE (VolatileLoad(&pEntry->m_dwRefCount) == 0);

#ifdef _DEBUG
    memset (pEntry, 0xc, sizeof(StringLiteralEntry));
#endif

#ifdef _DEBUG
    pEntry->m_bDeleted = TRUE;
#endif

    // The free list needs protection from the m_HashTableCrstGlobal
    pEntry->m_pNext = s_FreeEntryList;
    s_FreeEntryList = pEntry;
}



