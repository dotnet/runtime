// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: pendingload.cpp
//

//

#include "common.h"
#include "excep.h"
#include "pendingload.h"

#ifndef DACCESS_COMPILE

#ifdef PENDING_TYPE_LOAD_TABLE_STATS
// Enable PENDING_TYPE_LOAD_TABLE_STATS to gather counts of Entry structures which are allocated

static LONG pendingTypeLoadEntryDynamicAllocations = 0;
void PendingTypeLoadEntryDynamicAlloc()
{
    InterlockedIncrement(&pendingTypeLoadEntryDynamicAllocations);
}
#endif // PENDING_TYPE_LOAD_TABLE_STATS

PendingTypeLoadTable PendingTypeLoadTable::s_table;

PendingTypeLoadTable::Entry::Entry()
    : m_typeKey(TypeKey::InvalidTypeKey()),
        m_fIsPreallocated(true)
{
}

PendingTypeLoadTable::Entry::Entry(const TypeKey& typeKey)
    : m_typeKey(typeKey),
        m_fIsPreallocated(false)
{
#ifdef PENDING_TYPE_LOAD_TABLE_STATS
    PendingTypeLoadEntryDynamicAlloc();
#endif // PENDING_TYPE_LOAD_TABLE_STATS
}

void PendingTypeLoadTable::Entry::SetTypeKey(TypeKey typeKey)
{
    m_typeKey = typeKey;
}

void PendingTypeLoadTable::Entry::InitCrst()
{
    WRAPPER_NO_CONTRACT;
    m_Crst.Init(CrstPendingTypeLoadEntry,
                CrstFlags(CRST_HOST_BREAKABLE|CRST_UNSAFE_SAMELEVEL));
}

void PendingTypeLoadTable::Entry::Init(Entry *pNext, DWORD hash, TypeHandle typeHnd)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_fIsUnused);
    m_dwHash = hash;
    m_pNext = pNext;
    m_typeHandle = typeHnd;
    m_dwWaitCount = 1;
    m_hrResult = S_OK;
    m_pException = NULL;
#ifdef _DEBUG
    if (LoggingOn(LF_CLASSLOADER, LL_INFO10000))
    {
        SString name;
        TypeString::AppendTypeKeyDebug(name, &m_typeKey);
        LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: Creating loading entry for type %s\n", name.GetUTF8()));
    }
#endif

    m_fIsUnused = false;
    m_fLockAcquired = TRUE;

    //---------------------------------------------------------------------------
    // The PendingTypeLoadEntry() lock has a higher level than UnresolvedClassLock.
    // But whenever we create one, we have to acquire it while holding the UnresolvedClassLock.
    // This is safe since we're the ones that created the lock and are guaranteed to acquire
    // it without blocking. But to prevent the crstlevel system from asserting, we
    // must acquire using a special method.
    //---------------------------------------------------------------------------
    m_Crst.Enter(INDEBUG(Crst::CRST_NO_LEVEL_CHECK));
}

void PendingTypeLoadTable::Entry::Reset()
{
    LIMITED_METHOD_CONTRACT;

    if (m_fLockAcquired)
    {
        m_Crst.Leave();
        m_fLockAcquired = false;
    }

    if (m_pException && !m_pException->IsPreallocatedException()) {
        delete m_pException;
        m_pException = NULL;
    }
}

bool PendingTypeLoadTable::Entry::IsUnused()
{
    // This VolatileLoad synchrnonizes with the Release()
    return (m_fIsUnused && VolatileLoad(&m_fIsUnused));
}

#ifdef _DEBUG
bool PendingTypeLoadTable::Entry::HasLock()
{
    LIMITED_METHOD_CONTRACT;
    return !!m_Crst.OwnedByCurrentThread();
}
#endif

VOID DECLSPEC_NORETURN PendingTypeLoadTable::Entry::ThrowException()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_pException)
        PAL_CPP_THROW(Exception *, m_pException->Clone());

    _ASSERTE(FAILED(m_hrResult));

    if (m_hrResult == COR_E_TYPELOAD)
    {
        ClassLoader::ThrowTypeLoadException(GetTypeKey(),
                                            IDS_CLASSLOAD_GENERAL);

    }
    else
        EX_THROW(EEMessageException, (m_hrResult));
}

void PendingTypeLoadTable::Entry::SetException(Exception *pException)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(HasLock());
        PRECONDITION(m_pException == NULL);
        PRECONDITION(m_dwWaitCount > 0);
    }
    CONTRACTL_END;

    m_typeHandle = TypeHandle();
    m_hrResult = COR_E_TYPELOAD;

    // we don't care if this fails
    // we already know the HRESULT so if we can't store
    // the details - so be it
    EX_TRY
    {
        FAULT_NOT_FATAL();
        m_pException = pException->Clone();
    }
    EX_CATCH
    {
        m_pException=NULL;
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void PendingTypeLoadTable::Entry::SetResult(TypeHandle typeHnd)
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(HasLock());
        PRECONDITION(m_pException == NULL);
        PRECONDITION(m_dwWaitCount > 0);
    }
    CONTRACTL_END;

    m_typeHandle = typeHnd;
}

void PendingTypeLoadTable::Entry::UnblockWaiters()
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(HasLock());
        PRECONDITION(m_dwWaitCount > 0);
    }
    CONTRACTL_END;

    _ASSERTE(m_fLockAcquired);
    m_Crst.Leave();
    m_fLockAcquired = FALSE;
}

const TypeKey* PendingTypeLoadTable::Entry::GetTypeKey()
{
    LIMITED_METHOD_CONTRACT;
    return &m_typeKey;
}

void PendingTypeLoadTable::Entry::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    InterlockedIncrement(&m_dwWaitCount);
}

void PendingTypeLoadTable::Entry::Release()
{
    LIMITED_METHOD_CONTRACT;
    if (InterlockedDecrement(&m_dwWaitCount) == 0)
    {
        Reset();

        if (this->m_fIsPreallocated)
        {
            // We won't be holding the lock while Releasing, so use a VolatileStore to ensure all writes during Reset are complete.
            VolatileStore(&m_fIsUnused, true);
        }
        else
        {
            // Call the derived type with a destructor
            delete static_cast<DynamicallyAllocatedEntry*>(this);
        }
    }
}

bool PendingTypeLoadTable::Entry::HasWaiters()
{
    LIMITED_METHOD_CONTRACT;
    return m_dwWaitCount > 1;
}

HRESULT PendingTypeLoadTable::Entry::DelayForProgress(TypeHandle* typeHndWithProgress)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;
    {
        CrstHolder crstHolder(&m_Crst);
        _ASSERTE(HasLock());
        hr = m_hrResult;

        if (SUCCEEDED(hr))
        {
            *typeHndWithProgress = m_typeHandle;
        }
    }

    return hr;
}

void PendingTypeLoadTable::Shard::Init()
{
    m_shardCrst.Init(CrstUnresolvedClassLock);
    for (int i = 0; i < PreallocatedEntryCount; i++)
    {
        m_preAllocatedEntries[i].InitCrst();
    }
}

PendingTypeLoadTable::Entry* PendingTypeLoadTable::Shard::FindPendingTypeLoadEntry(DWORD hash, const TypeKey& typeKey)
{
    WRAPPER_NO_CONTRACT;
    for (PendingTypeLoadTable::Entry *current = m_pLinkedListOfActiveEntries; current != NULL; current = current->m_pNext)
    {
        if (current->m_dwHash != hash)
            continue;
        if (TypeKey::Equals(&typeKey, current->GetTypeKey()))
        {
            return current;
        }
    }

    return NULL;
}

void PendingTypeLoadTable::Shard::RemovePendingTypeLoadEntry(Entry* pEntry)
{
    LIMITED_METHOD_CONTRACT;

    Entry **pCurrent = &m_pLinkedListOfActiveEntries;

    while (*pCurrent != pEntry)
    {
        pCurrent = &((*pCurrent)->m_pNext);
    }
    *pCurrent = (*pCurrent)->m_pNext;
}

PendingTypeLoadTable::Entry* PendingTypeLoadTable::Shard::InsertPendingTypeLoadEntry(DWORD hash, const TypeKey& typeKey, TypeHandle typeHnd)
{
    STANDARD_VM_CONTRACT;
    Entry* result = NULL;

    for (int iEntry = 0; iEntry < PreallocatedEntryCount; iEntry++)
    {
        if (m_preAllocatedEntries[iEntry].IsUnused())
        {
            result = &m_preAllocatedEntries[iEntry];
            result->SetTypeKey(typeKey);
            result->Init(m_pLinkedListOfActiveEntries, hash, typeHnd);
            break;
        }
    }
    if (result == NULL)
    {
        NewHolder<DynamicallyAllocatedEntry> dynamicResult(new DynamicallyAllocatedEntry(typeKey));
        dynamicResult->Init(m_pLinkedListOfActiveEntries, hash, typeHnd);
        result = dynamicResult.Extract();
    }
    
    m_pLinkedListOfActiveEntries = result;
    return result;
}

/*static*/
PendingTypeLoadTable* PendingTypeLoadTable::GetTable()
{
    LIMITED_METHOD_CONTRACT;
    return &s_table;
}

/*static*/
void PendingTypeLoadTable::Init()
{
    STANDARD_VM_CONTRACT;
    for (int i = 0; i < PendingTypeLoadTableShardCount; i++)
        GetTable()->m_shards[i].Init();
}

PendingTypeLoadTable::Shard* PendingTypeLoadTable::GetShard(const TypeKey &typeKey, ClassLoader* pClassLoader, DWORD *pHashCodeForType)
{
    STANDARD_VM_CONTRACT;
    DWORD hash = HashTypeKey(&typeKey) ^ (DWORD)(size_t)pClassLoader; // Mix in some entropy about which classloader is in use
    *pHashCodeForType = hash;
    return &m_shards[hash % PendingTypeLoadTableShardCount];
}

#ifdef _DEBUG
void PendingTypeLoadTable::Dump()
{
    STANDARD_VM_CONTRACT;
    for (int iShard = 0; iShard < PendingTypeLoadTableShardCount; iShard++)
    {
        CrstHolder unresolvedClassLockHolder(m_shards[iShard].GetCrst());
        m_shards[iShard].Dump();
    }
}

void PendingTypeLoadTable::Shard::Dump()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: shard contains:\n"));
    for (Entry *pSearch = this->m_pLinkedListOfActiveEntries; pSearch; pSearch = pSearch->m_pNext)
    {
        SString name;
        TypeString::AppendTypeKeyDebug(name, pSearch->GetTypeKey());
        LOG((LF_CLASSLOADER, LL_INFO10000, "  Entry %s with handle %p at level %s\n", name.GetUTF8(), pSearch->m_typeHandle.AsPtr(),
                pSearch->m_typeHandle.IsNull() ? "not-applicable" : classLoadLevelName[pSearch->m_typeHandle.GetLoadLevel()]));
    }
}
#endif

#endif // #ifndef DACCESS_COMPILE

