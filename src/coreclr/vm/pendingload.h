// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// pendingload.h
//

//

#ifndef _H_PENDINGLOAD
#define _H_PENDINGLOAD

#include "crst.h"
#include "class.h"
#include "typekey.h"
#include "typehash.h"
#include "vars.hpp"
#include "shash.h"
#include "typestring.h"

#ifdef PENDING_TYPE_LOAD_TABLE_STATS
// Enable PENDING_TYPE_LOAD_TABLE_STATS to gather counts of Entry structures which are allocated
void PendingTypeLoadEntryDynamicAlloc();
#endif // PENDING_TYPE_LOAD_TABLE_STATS

//
// A temporary structure used when loading and resolving classes
//

// Hash table used to hold pending type loads
// This is a sharded fixed bucket count hashtable, with locking per shard
// This is a singleton
class PendingTypeLoadTable
{
    static void CallCrstEnter(Crst* pCrst)
    {
        pCrst->Enter(INDEBUG(Crst::CRST_NO_LEVEL_CHECK));
    }

public:
    struct Shard;
    class Entry
    {
        friend class PendingTypeLoadTable;
    private:
        Entry()
            : m_Crst(
                    CrstPendingTypeLoadEntry,
                    CrstFlags(CRST_HOST_BREAKABLE|CRST_UNSAFE_SAMELEVEL)
                    ),
              m_typeKey(TypeKey::InvalidTypeKey()),
              m_fIsPreallocated(true)
        {
        }

        Entry(Entry *pNext, DWORD hash, const TypeKey& typeKey, TypeHandle typeHnd)
            : m_Crst(
                    CrstPendingTypeLoadEntry,
                    CrstFlags(CRST_HOST_BREAKABLE|CRST_UNSAFE_SAMELEVEL)
                    ),
              m_typeKey(typeKey),
              m_fIsPreallocated(false)
        {
#ifdef PENDING_TYPE_LOAD_TABLE_STATS
            PendingTypeLoadEntryDynamicAlloc();
#endif // PENDING_TYPE_LOAD_TABLE_STATS
            Init(pNext, hash, typeHnd);
        }

        void SetTypeKey(const TypeKey& typeKey)
        {
            m_typeKey = typeKey;
        }

        void Init(Entry *pNext, DWORD hash, TypeHandle typeHnd)
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
            PendingTypeLoadTable::CallCrstEnter(&m_Crst);
        }

        void Reset()
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
        bool IsUnused()
        {
            // This VolatileLoad synchrnonizes with the Release()
            return (m_fIsUnused && VolatileLoad(&m_fIsUnused));
        }

    public:
    #ifdef _DEBUG
        BOOL HasLock()
        {
            LIMITED_METHOD_CONTRACT;
            return m_Crst.OwnedByCurrentThread();
        }
    #endif

    #ifndef DACCESS_COMPILE
        VOID DECLSPEC_NORETURN ThrowException()
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
                TypeKey typeKey = GetTypeKey();
                ClassLoader::ThrowTypeLoadException(&typeKey,
                                                    IDS_CLASSLOAD_GENERAL);

            }
            else
                EX_THROW(EEMessageException, (m_hrResult));
        }

        void SetException(Exception *pException)
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

        void SetResult(TypeHandle typeHnd)
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

        void UnblockWaiters()
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
    #endif //DACCESS_COMPILE

        TypeKey& GetTypeKey()
        {
            LIMITED_METHOD_CONTRACT;
            return m_typeKey;
        }

        // This is only safe to call on an AddRef'd PendingTypeLoadEntry which has had DelayForProgress() called on it
        TypeHandle GetTypeHandle()
        {
            LIMITED_METHOD_CONTRACT;
            return m_typeHandle;
        }

        void AddRef()
        {
            LIMITED_METHOD_CONTRACT;
            InterlockedIncrement(&m_dwWaitCount);
        }

        void Release()
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
                    delete this;
            }
        }

        BOOL HasWaiters()
        {
            LIMITED_METHOD_CONTRACT;
            return m_dwWaitCount > 1;
        }

        // Call this when After calling AddRef to see what the next amount of progress to wait for the load in progress to complete
        // and to find out if that load in progress succeeded
        HRESULT DelayForProgress()
        {
            STANDARD_VM_CONTRACT;
            CrstHolder crstHolder(&m_Crst);
            _ASSERTE(HasLock());
            return m_hrResult;
        }

    private:
        Entry*              m_pNext;
        Crst                m_Crst;

    public:
        // Result of loading; this is first created in the CREATE stage of class loading
        TypeHandle          m_typeHandle;

    private:
        // Type that we're loading
        TypeKey             m_typeKey;

        // Number of threads waiting for this type
        LONG                m_dwWaitCount;

        // Error result, propagated to all threads loading this class
        HRESULT             m_hrResult;

        // Exception object to throw
        Exception          *m_pException;

        DWORD               m_dwHash;

        // m_Crst was acquired
        bool                m_fLockAcquired;

        bool                m_fIsPreallocated;

        bool                m_fIsUnused = true;
    };

    struct Shard
    {
        friend class PendingTypeLoadTable;
        // This number chosen by experimentation with a fairly complex ASP.NET application that would naturally use about 40,000 Entry structures on startup.
        // Entry allocations were shifted to about 11 during that startup phase.
        static constexpr int PreallocatedEntryCount = 2;

private:
        Shard() : m_shardCrst(CrstUnresolvedClassLock)
        {}

        Entry *m_pLinkedListOfActiveEntries = NULL;
        Crst                  m_shardCrst;
        Entry  m_preAllocatedEntries[PreallocatedEntryCount];

public:
        Crst* GetCrst()
        {
            LIMITED_METHOD_CONTRACT;
            return &m_shardCrst;
        }

        Entry* FindPendingTypeLoadEntry(DWORD hash, const TypeKey& typeKey)
        {
            WRAPPER_NO_CONTRACT;
            for (PendingTypeLoadTable::Entry *current = m_pLinkedListOfActiveEntries; current != NULL; current = current->m_pNext)
            {
                if (current->m_dwHash != hash)
                    continue;
                TypeKey entryTypeKey = current->GetTypeKey();
                if (TypeKey::Equals(&typeKey, &entryTypeKey))
                {
                    return current;
                }
            }

            return NULL;
        }

        void RemovePendingTypeLoadEntry(Entry* pEntry)
        {
            LIMITED_METHOD_CONTRACT;

            Entry **pCurrent = &m_pLinkedListOfActiveEntries;

            while (*pCurrent != pEntry)
            {
                pCurrent = &((*pCurrent)->m_pNext);
            }
            *pCurrent = (*pCurrent)->m_pNext;
        }

        Entry* InsertPendingTypeLoadEntry(DWORD hash, const TypeKey& typeKey, TypeHandle typeHnd)
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
                result = new Entry(m_pLinkedListOfActiveEntries, hash, typeKey, typeHnd);
            
            m_pLinkedListOfActiveEntries = result;
            return result;
        }

#ifdef _DEBUG
        void Dump();
#endif
    };

    // This number chosen by experimentation with a fairly complex ASP.NET application that would naturally use about 40,000 Entry structures on startup.
    // Entry allocations were shifted to about 11 during that startup phase.
    static constexpr int PendingTypeLoadTableShardCount = 31;
    Shard     m_shards[PendingTypeLoadTableShardCount]; 

public:
    static PendingTypeLoadTable* GetTable();

    static void Init()
    {
        STANDARD_VM_CONTRACT;
        new(GetTable())PendingTypeLoadTable();
    }

    Shard* GetShard(const TypeKey &typeKey, ClassLoader* pClassLoader, DWORD *pHashCodeForType)
    {
        STANDARD_VM_CONTRACT;
        DWORD hash = HashTypeKey(&typeKey) ^ (DWORD)(size_t)pClassLoader; // Mix in some entropy about which classloader is in use
        *pHashCodeForType = hash;
        return &m_shards[hash % PendingTypeLoadTableShardCount];
    }

#ifdef _DEBUG
    void Dump()
    {
        STANDARD_VM_CONTRACT;
        for (int iShard = 0; iShard < PendingTypeLoadTableShardCount; iShard++)
        {
            CrstHolder unresolvedClassLockHolder(m_shards[iShard].GetCrst());
            m_shards[iShard].Dump();
        }
    }
#endif
};

#endif // _H_PENDINGLOAD
