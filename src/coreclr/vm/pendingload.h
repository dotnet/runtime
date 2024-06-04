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

//
// A temporary structure used when loading and resolving classes
//

// Hash table used to hold pending type loads
// This is a sharded fixed bucket count hashtable, with locking per shard
// This is a singleton
class PendingTypeLoadTable
{
public:
    class Entry
    {
        friend class PendingTypeLoadTable;
    private:
        Entry();
        Entry(const TypeKey& typeKey);

    protected:

        void SetTypeKey(TypeKey typeKey);
        void InitCrst();
        void Init(Entry *pNext, DWORD hash, TypeHandle typeHnd);
        void Reset();
        bool IsUnused();

    public:

    #ifdef _DEBUG
        bool HasLock();
    #endif

        VOID DECLSPEC_NORETURN ThrowException();
        void SetException(Exception *pException);
        void SetResult(TypeHandle typeHnd);
        void UnblockWaiters();
        const TypeKey* GetTypeKey();

        void AddRef();
        void Release();
        bool HasWaiters();
        // Call this when After calling AddRef to see what the next amount of progress to wait for the load in progress to complete
        // and to find the TypeHandle if progress was successful.
        HRESULT DelayForProgress(TypeHandle* typeHndWithProgress);

    protected:
        Entry*              m_pNext;
        CrstStatic          m_Crst; // While this isn't a static, we use CrstStatic, so that Entry structs can be part of a static variable

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

private:
    class DynamicallyAllocatedEntry : public Entry
    {
    public:
        DynamicallyAllocatedEntry(const TypeKey& typeKey) : Entry(typeKey)
        {
            InitCrst();
        }

        void Init(Entry *pNext, DWORD hash, TypeHandle typeHnd)
        {
            WRAPPER_NO_CONTRACT;
            Entry::Init(pNext, hash, typeHnd);
        }

        ~DynamicallyAllocatedEntry()
        {
            WRAPPER_NO_CONTRACT;
            m_Crst.Destroy();
        }
    };

    class StaticallyAllocatedEntry : public Entry
    {
    };

public:
    struct Shard
    {
        friend class PendingTypeLoadTable;
        // This number chosen by experimentation with a fairly complex ASP.NET application that would naturally use about 40,000 Entry structures on startup.
        // Entry allocations were shifted to about 11 during that startup phase.
        static constexpr int PreallocatedEntryCount = 2;

private:
        Shard() = default;

        Entry *m_pLinkedListOfActiveEntries = NULL;
        CrstStatic m_shardCrst;
        Entry  m_preAllocatedEntries[PreallocatedEntryCount];

        void Init();
public:
        CrstBase* GetCrst()
        {
            LIMITED_METHOD_CONTRACT;
            return &m_shardCrst;
        }

        Entry* FindPendingTypeLoadEntry(DWORD hash, const TypeKey& typeKey);
        void RemovePendingTypeLoadEntry(Entry* pEntry);
        Entry* InsertPendingTypeLoadEntry(DWORD hash, const TypeKey& typeKey, TypeHandle typeHnd);

#ifdef _DEBUG
        void Dump();
#endif
    };

    // This number chosen by experimentation with a fairly complex ASP.NET application that would naturally allocate
    // about 40,000 Entry structures on startup. With PendingTypeLoadTableShardCount(31) number of shards and 
    // PreallocatedEntryCount(2) number of pre-allocated entries in each shard, the number of dynamic allocations of
    // Entry structures was reduced to 11.
    static constexpr int PendingTypeLoadTableShardCount = 31;
    Shard     m_shards[PendingTypeLoadTableShardCount];

    static PendingTypeLoadTable s_table;
    static PendingTypeLoadTable* GetTable();

public:
    static void Init();
    Shard* GetShard(const TypeKey &typeKey, ClassLoader* pClassLoader, DWORD *pHashCodeForType);
#ifdef _DEBUG
    void Dump();
#endif
};

#endif // _H_PENDINGLOAD
