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
class PendingTypeLoadEntry
{
    friend class ClassLoader;       // workaround really need to beef up the API below

public:
    PendingTypeLoadEntry(TypeKey typeKey, TypeHandle typeHnd)
        : m_Crst(
                 CrstPendingTypeLoadEntry,
                 CrstFlags(CRST_HOST_BREAKABLE|CRST_UNSAFE_SAMELEVEL)
                 ),
        m_typeKey(typeKey)

    {
        WRAPPER_NO_CONTRACT;

        m_typeHandle = typeHnd;
        m_dwWaitCount = 1;
        m_hrResult = S_OK;
        m_pException = NULL;
#ifdef _DEBUG
        if (LoggingOn(LF_CLASSLOADER, LL_INFO10000))
        {
            SString name;
            TypeString::AppendTypeKeyDebug(name, &m_typeKey);
            LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: Creating loading entry for type %S\n", name.GetUnicode()));
        }
#endif

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

    ~PendingTypeLoadEntry()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_fLockAcquired)
            m_Crst.Leave();

        if (m_pException && !m_pException->IsPreallocatedException()) {
            delete m_pException;
        }
    }

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

    void AddRef()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement(&m_dwWaitCount);
    }

    void Release()
    {
        LIMITED_METHOD_CONTRACT;
        if (InterlockedDecrement(&m_dwWaitCount) == 0)
            delete this;
    }

    BOOL HasWaiters()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwWaitCount > 1;
    }

 private:
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

    // m_Crst was acquired
    BOOL                m_fLockAcquired;
};

// Hash table used to hold pending type loads
// @todo : use shash.h when it supports LoaderHeap/Alloc\MemTracker
class PendingTypeLoadTable
{
protected:
    struct TableEntry
    {
        TableEntry* pNext;
        DWORD                 dwHashValue;
        PendingTypeLoadEntry* pData;
    };

    TableEntry     **m_pBuckets;    // Pointer to first entry for each bucket
    DWORD           m_dwNumBuckets;

public:

#ifdef _DEBUG
    DWORD           m_dwDebugMemory;
#endif

    static PendingTypeLoadTable *Create(LoaderHeap *pHeap, DWORD dwNumBuckets, AllocMemTracker *pamTracker);

private:
    // These functions don't actually exist - declared private to prevent bypassing PendingTypeLoadTable::Create
    void *          operator new(size_t size);
    void            operator delete(void *p);

    PendingTypeLoadTable();
    ~PendingTypeLoadTable();

public:
    BOOL            InsertValue(PendingTypeLoadEntry* pEntry);
    BOOL            DeleteValue(TypeKey *pKey);
    PendingTypeLoadEntry* GetValue(TypeKey *pKey);
    TableEntry* AllocNewEntry();
    void FreeEntry(TableEntry* pEntry);
#ifdef _DEBUG
    void            Dump();
#endif

private:
    TableEntry* FindItem(TypeKey *pKey);
};


#endif // _H_PENDINGLOAD
