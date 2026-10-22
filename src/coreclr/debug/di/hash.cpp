// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: hash.cpp
//

//
//*****************************************************************************
#include "stdafx.h"

/* ------------------------------------------------------------------------- *
 * Hash Table class
 * ------------------------------------------------------------------------- */

HRESULT CordbHashTable::UnsafeAddBase(CordbBase *pBase)
{
    AssertIsProtected();

    if (!m_table.AddNoThrow(pBase))
        return E_OUTOFMEMORY;

    pBase->InternalAddRef();
    return S_OK;
}
bool CordbHashTable::IsInitialized()
{
    return m_table.GetCapacity() != 0;
}

CordbBase *CordbHashTable::UnsafeGetBase(ULONG_PTR id, BOOL fFab)
{
    AssertIsProtected();

    return m_table.Lookup(id);
}

HRESULT CordbHashTable::UnsafeSwapBase(CordbBase *pOldBase, CordbBase *pNewBase)
{
    AssertIsProtected();

    ULONG_PTR id = (ULONG_PTR)pOldBase->m_id;

    if (!m_table.Update(id, [&](CordbBase *&entry)
        {
            _ASSERTE(entry == pOldBase);
            _ASSERTE(pOldBase->m_id == pNewBase->m_id);
            pNewBase->InternalAddRef();
            entry = pNewBase;
        }))
    {
        return E_FAIL;
    }

    // release the hash table's reference to the old base and transfer it
    // to the new one.
    pOldBase->InternalRelease();

    return S_OK;
}


CordbBase *CordbHashTable::UnsafeRemoveBase(ULONG_PTR id)
{
    AssertIsProtected();

    CordbBase *base = m_table.Lookup(id);
    if (base == NULL)
    {
        return NULL;
    }

    m_table.Remove(id);
    base->InternalRelease();

    return base;
}

CordbHashTable::Iterator CordbHashTable::Begin()
{
    AssertIsProtected();
    return m_table.Begin();
}

CordbHashTable::Iterator CordbHashTable::End()
{
    AssertIsProtected();
    return m_table.End();
}

/* ------------------------------------------------------------------------- *
 * Hash Table Enumerator class
 * ------------------------------------------------------------------------- */

// This constructor is part of 2 phase construction.
// Use the BuildOrThrow method to instantiate.
CordbHashTableEnum::CordbHashTableEnum(
    CordbBase * pOwnerObj, NeuterList * pOwnerList,
    REFIID guid
)
  : CordbBase(pOwnerObj->GetProcess(), 0, enumCordbHashTableEnum),
    m_pOwnerObj(pOwnerObj),
    m_pOwnerNeuterList(pOwnerList),
    m_guid(guid),
    m_iCurElt(0)
{
    m_items.EnableAutoClear();
}

//---------------------------------------------------------------------------------------
//
// Build a new Hash enumerator or throw
//
// Arguments:
//     pOwnerObj - owner
//     pOwnerList - neuter list to add to
//     table - hash table to enumerate.
//     id - guid of objects to enumerate
//     pHolder - holder to get ownership.
//
void CordbHashTableEnum::BuildOrThrow(
    CordbBase * pOwnerObj,
    NeuterList * pOwnerList,
    CordbHashTable *pTable,
    const GUID &id,
    RSInitHolder<CordbHashTableEnum> * pHolder)
{
    CordbProcess *pProcess = pOwnerObj->GetProcess();
    RSLock *pLock = pProcess == nullptr ? nullptr : pProcess->GetProcessLock();
    RSLockHolder lockHolder(pLock, FALSE);
    if (pLock != nullptr)
        lockHolder.Acquire();

    CordbHashTableEnum * pEnum = new CordbHashTableEnum(pOwnerObj, pOwnerList, id);
    pHolder->Assign(pEnum);

    pEnum->m_items.AllocOrThrow(pTable->GetCount());
    ULONG index = 0;
    for (CordbHashTable::Iterator iterator = pTable->Begin(), end = pTable->End(); iterator != end; iterator++)
    {
        pEnum->m_items.Assign(index++, *iterator);
    }
    _ASSERTE(index == pEnum->m_items.Length());

    // If no neuter-list supplied, then our owner is manually managing us.
    // It also means we can't be cloned.
    if (pOwnerList != NULL)
    {
        pOwnerList->Add(pOwnerObj->GetProcess(), pEnum);
    }

}

// Only for cloning.
// Copy constructor makes life easy & fun!
CordbHashTableEnum::CordbHashTableEnum(CordbHashTableEnum *cloneSrc)
  : CordbBase(cloneSrc->m_pOwnerObj->GetProcess(), 0, enumCordbHashTableEnum),
    m_pOwnerObj(cloneSrc->m_pOwnerObj),
    m_pOwnerNeuterList(cloneSrc->m_pOwnerNeuterList),
    m_guid(cloneSrc->m_guid),
    m_iCurElt(cloneSrc->m_iCurElt)
{
    m_items.EnableAutoClear();

    // We can get cloned at any time, so our owner can't manually control us,
    // so we need explicit access to a neuter list.
    _ASSERTE(m_pOwnerNeuterList != NULL);

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Add to neuter list
        if (m_pOwnerObj->GetProcess() != NULL)
        {
            // Normal case. For things enumerating stuff within a CordbProcess tree.
            m_pOwnerNeuterList->Add(m_pOwnerObj->GetProcess(), this);
        }
        else
        {
            // For Process-list enums that have broken neuter semantics.
            // @dbgtodo: this goes away once we remove the top-level ICorDebug interface,
            // and thus no longer have a Process enumerator.
            m_pOwnerNeuterList->UnsafeAdd(NULL, this);
        }
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);

}

CordbHashTableEnum::~CordbHashTableEnum()
{
    _ASSERTE(this->IsNeutered());

    _ASSERTE(m_pOwnerObj == NULL);
    _ASSERTE(m_pOwnerNeuterList == NULL);
}

void CordbHashTableEnum::Neuter()
{
    m_items.Clear();
    m_pOwnerObj = NULL;
    m_pOwnerNeuterList = NULL;

    CordbBase::Neuter();
}


HRESULT CordbHashTableEnum::Reset()
{
    HRESULT hr = S_OK;

    m_iCurElt = 0;

    return hr;
}

HRESULT CordbHashTableEnum::Clone(ICorDebugEnum **ppEnum)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    AssertValid();

    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);

    HRESULT hr;
    hr = S_OK;

    CordbProcess * pProc = GetProcess();
    *ppEnum = nullptr;

    EX_TRY
    {
        auto cloneSnapshot = [&]()
        {
            RSInitHolder<CordbHashTableEnum> clone;
            clone.Assign(new CordbHashTableEnum(this));
            clone->m_items.AllocOrThrow(m_items.Length());
            for (ULONG index = 0; index < m_items.Length(); index++)
                clone->m_items.Assign(index, m_items[index]);
            IfFailThrow(clone->QueryInterface(m_guid, reinterpret_cast<void **>(ppEnum)));
            clone.ClearAndMarkDontNeuter();
        };

        if (pProc != NULL)
        {
            ATT_REQUIRE_STOPPED_MAY_FAIL(pProc);
            cloneSnapshot();
        }
        else
        {
            cloneSnapshot();
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbHashTableEnum::GetCount(ULONG *pcelt)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    AssertValid();

    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    *pcelt = m_items.Length();

    return S_OK;
}

// This is an public function implementing all of the ICorDebugXXXEnum interfaces.
HRESULT CordbHashTableEnum::Next(ULONG celt,
                                 CordbBase *bases[],
                                 ULONG *pceltFetched)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    AssertValid();

    VALIDATE_POINTER_TO_OBJECT_ARRAY(bases,
                                     CordbBase *,
                                     celt,
                                     true,
                                     true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched,
                                       ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    if (celt == 0)
    {
        if (pceltFetched != NULL)
        {
            *pceltFetched = 0;
        }
        return S_OK;
    }

    HRESULT         hr      = S_OK;
    CordbBase     **b       = bases;
    CordbBase     **bEnd    = bases + celt;

    while (b < bEnd && m_iCurElt < m_items.Length())
    {
        CordbBase *base = m_items[m_iCurElt++];
        if (m_guid == IID_ICorDebugProcessEnum)
        {
            *b = (CordbBase*)(ICorDebugProcess*)(CordbProcess*)base;
        }
        else if (m_guid == IID_ICorDebugBreakpointEnum)
        {
            *b = (CordbBase*)(ICorDebugBreakpoint*)(CordbBreakpoint*)base;
        }
        else if (m_guid == IID_ICorDebugStepperEnum)
        {
            *b = (CordbBase*)(ICorDebugStepper*)(CordbStepper*)base;
        }
        else if (m_guid == IID_ICorDebugModuleEnum)
        {
            *b = (CordbBase*)(ICorDebugModule*)(CordbModule*)base;
        }
        else if (m_guid == IID_ICorDebugThreadEnum)
        {
            *b = (CordbBase*)(ICorDebugThread*)(CordbThread*)base;
        }
        else if (m_guid == IID_ICorDebugAppDomainEnum)
        {
            *b = (CordbBase*)(ICorDebugAppDomain*)(CordbAppDomain*)base;
        }
        else if (m_guid == IID_ICorDebugAssemblyEnum)
        {
            *b = (CordbBase*)(ICorDebugAssembly*)(CordbAssembly*)base;
        }
        else
        {
            *b = (CordbBase*)(IUnknown*)base;
        }

        // 'b' is not a valid CordbBase ptr.
        base->ExternalAddRef();
        b++;
    }

    //
    // If celt == 1, then the pceltFetched parameter is optional.
    //
    if (pceltFetched != NULL)
    {
        *pceltFetched = (ULONG)(b - bases);
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (b != bEnd)
    {
        hr = S_FALSE;
    }

    return hr;
}

HRESULT CordbHashTableEnum::Skip(ULONG celt)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    AssertValid();

    ULONG remaining = m_items.Length() - m_iCurElt;
    if (celt > remaining)
    {
        m_iCurElt = m_items.Length();
        return S_OK;
    }

    m_iCurElt += celt;
    return S_OK;
}

HRESULT CordbHashTableEnum::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugEnum)
    {
        ExternalAddRef();
        *pInterface = static_cast<ICorDebugEnum *>(static_cast<ICorDebugProcessEnum *>(this));

        return S_OK;
    }
    if (id == IID_IUnknown)
    {
        ExternalAddRef();
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugProcessEnum *>(this));

        return S_OK;
    }
    if (id == m_guid)
    {
        ExternalAddRef();

        if (id == IID_ICorDebugProcessEnum)
            *pInterface = static_cast<ICorDebugProcessEnum *>(this);
        else if (id == IID_ICorDebugBreakpointEnum)
            *pInterface = static_cast<ICorDebugBreakpointEnum *>(this);
        else if (id == IID_ICorDebugStepperEnum)
            *pInterface = static_cast<ICorDebugStepperEnum *>(this);
        else if (id == IID_ICorDebugModuleEnum)
            *pInterface = static_cast<ICorDebugModuleEnum *>(this);
        else if (id == IID_ICorDebugThreadEnum)
            *pInterface = static_cast<ICorDebugThreadEnum *>(this);
        else if (id == IID_ICorDebugAppDomainEnum)
            *pInterface = static_cast<ICorDebugAppDomainEnum *>(this);
        else if (id == IID_ICorDebugAssemblyEnum)
            *pInterface = static_cast<ICorDebugAssemblyEnum *>(this);

        return S_OK;
    }

    return E_NOINTERFACE;
}

#ifdef _DEBUG
void CordbHashTableEnum::AssertValid()
{
    _ASSERTE(m_iCurElt <= m_items.Length());
}


//
void CordbHashTable::AssertIsProtected()
{
#ifdef RSCONTRACTS
    if (m_pDbgLock != NULL)
    {
        DbgRSThread * pThread = DbgRSThread::GetThread();
        if (pThread->IsInRS())
        {
            CONSISTENCY_CHECK_MSGF(m_pDbgLock->HasLock(), ("Hash table being accessed w/o holding '%s'", m_pDbgLock->Name()));
        }
    }
#endif
}
#endif
