// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: instmethhash.cpp
//


//

//
// ============================================================================

#include "common.h"
#include "excep.h"
#include "instmethhash.h"
#include "eeconfig.h"
#include "generics.h"
#include "typestring.h"
#include "dacenumerablehash.inl"

PTR_MethodDesc InstMethodHashEntry::GetMethod()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return dac_cast<PTR_MethodDesc>(dac_cast<TADDR>(data) & ~0x3);
}

DWORD InstMethodHashEntry::GetFlags()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return (DWORD)(dac_cast<TADDR>(data) & 0x3);
}

#ifndef DACCESS_COMPILE

void InstMethodHashEntry::SetMethodAndFlags(MethodDesc *pMethod, DWORD dwFlags)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(dwFlags <= 0x3);
    _ASSERTE(((TADDR)pMethod & 0x3) == 0);

    data = (MethodDesc*)((TADDR)pMethod | dwFlags);
}

// ============================================================================
// Instantiated method hash table methods
// ============================================================================
/* static */ InstMethodHashTable *InstMethodHashTable::Create(LoaderAllocator *pAllocator, Module *pModule, DWORD dwNumBuckets, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LoaderHeap *pHeap = pAllocator->GetLowFrequencyHeap();
    InstMethodHashTable *pThis = (InstMethodHashTable*)pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(InstMethodHashTable)));

    new (pThis) InstMethodHashTable(pModule, pHeap, dwNumBuckets);

#ifdef _DEBUG
    pThis->InitUnseal();
#endif

    pThis->m_pLoaderAllocator = pAllocator;

    return pThis;
}

PTR_LoaderAllocator InstMethodHashTable::GetLoaderAllocator()
{
    WRAPPER_NO_CONTRACT;

    if (m_pLoaderAllocator)
    {
        return m_pLoaderAllocator;
    }
    else
    {
        _ASSERTE(m_pModule != NULL);
        return GetModule()->GetLoaderAllocator();
    }
}


// Calculate a hash value for a method-desc key
static DWORD Hash(TypeHandle declaringType, mdMethodDef token, Instantiation inst)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    DWORD dwHash = 0x87654321;
#define INST_HASH_ADD(_value) dwHash = ((dwHash << 5) + dwHash) ^ (_value)

    INST_HASH_ADD(declaringType.GetCl());
    INST_HASH_ADD(token);

    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle thArg = inst[i];

        if (thArg.GetMethodTable())
        {
            INST_HASH_ADD(thArg.GetCl());

            Instantiation sArgInst = thArg.GetInstantiation();
            for (DWORD j = 0; j < sArgInst.GetNumArgs(); j++)
            {
                TypeHandle thSubArg = sArgInst[j];
                if (thSubArg.GetMethodTable())
                    INST_HASH_ADD(thSubArg.GetCl());
                else
                    INST_HASH_ADD(thSubArg.GetSignatureCorElementType());
            }
        }
        else
            INST_HASH_ADD(thArg.GetSignatureCorElementType());
    }

    return dwHash;
}

MethodDesc* InstMethodHashTable::FindMethodDesc(TypeHandle declaringType,
                                                mdMethodDef token,
                                                BOOL unboxingStub,
                                                Instantiation inst,
                                                BOOL getSharedNotStub)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(declaringType));
    }
    CONTRACTL_END

        // We temporarily disable IBC logging here
        // because the pMD that we search through may not be restored
        // and ComputePreferredZapModule will assert on finding an
        // encode fixup pointer
        //
        IBCLoggingDisabler disableIbcLogging;

    MethodDesc *pMDResult = NULL;

    DWORD dwHash = Hash(declaringType, token, inst);
    InstMethodHashEntry_t* pSearch;
    LookupContext sContext;

    for (pSearch = BaseFindFirstEntryByHash(dwHash, &sContext);
         pSearch != NULL;
         pSearch = BaseFindNextEntryByHash(&sContext))
    {
        MethodDesc *pMD = pSearch->GetMethod();

        if (pMD->GetMemberDef() != token)
            continue;  // Next iteration of the for loop

        if (pMD->GetNumGenericMethodArgs() != inst.GetNumArgs())
            continue;  // Next iteration of the for loop

        DWORD dwKeyFlags = pSearch->GetFlags();

        if ( ((dwKeyFlags & InstMethodHashEntry::RequiresInstArg) == 0) != (getSharedNotStub == 0) )
            continue;

        if ( ((dwKeyFlags & InstMethodHashEntry::UnboxingStub)    == 0) != (unboxingStub == 0) )
            continue;

        if (TypeHandle(pMD->GetMethodTable()) != declaringType)
        {
            continue;  // Next iteration of the for loop
        }

        if (!inst.IsEmpty())
        {
            Instantiation candidateInst = pMD->GetMethodInstantiation();

            // We have matched the method already, thus the number of arguments in the instantiation should match too.
            _ASSERTE(inst.GetNumArgs() == candidateInst.GetNumArgs());

            bool match = true;   // This is true when all instantiation arguments match

            for (DWORD i = 0; i < inst.GetNumArgs(); i++)
            {
                if (candidateInst[i] != inst[i])
                {
                    match = false;
                    break;
                }
            }
            if (!match)
                continue;  // Next iteration of the pSearch for loop;
        }
        //
        // Success, we found a pMD that matches

        pMDResult = pMD;
        break;  // Exit the for loop and jump to the return pMDResult
    }

    return pMDResult;
}

BOOL InstMethodHashTable::ContainsMethodDesc(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return FindMethodDesc(
        pMD->GetMethodTable(), pMD->GetMemberDef(), pMD->IsUnboxingStub(),
        pMD->GetMethodInstantiation(), pMD->RequiresInstArg()) != NULL;
}

#endif // #ifndef DACCESS_COMPILE

void InstMethodHashTable::Iterator::Reset()
{
    WRAPPER_NO_CONTRACT;

    if (m_pTable)
    {
#ifdef _DEBUG
        m_pTable->Unseal();
#endif
        m_pTable = NULL;
    }

    Init();
}

void InstMethodHashTable::Iterator::Init()
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    if (m_pTable)
        m_pTable->Seal(); // The table cannot be changing while it is being iterated
#endif

    m_fIterating = false;
}

InstMethodHashTable::Iterator::Iterator()
{
    WRAPPER_NO_CONTRACT;
    m_pTable = NULL;
    Init();
}

InstMethodHashTable::Iterator::Iterator(InstMethodHashTable * pTable)
{
    WRAPPER_NO_CONTRACT;
    m_pTable = pTable;
    Init();
}

InstMethodHashTable::Iterator::~Iterator()
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    if (m_pTable)
        m_pTable->Unseal(); // Done with the iterator so we unseal
#endif
}

BOOL InstMethodHashTable::FindNext(Iterator *it, InstMethodHashEntry **ppEntry)
{
    LIMITED_METHOD_CONTRACT;

    if (!it->m_fIterating)
    {
        BaseInitIterator(&it->m_sIterator);
        it->m_fIterating = true;
    }

    *ppEntry = it->m_sIterator.Next();
    return *ppEntry ? TRUE : FALSE;
}

DWORD InstMethodHashTable::GetCount()
{
    LIMITED_METHOD_CONTRACT;

    return BaseGetElementCount();
}

#ifndef DACCESS_COMPILE

// Add method desc to the hash table; must not be present already
void InstMethodHashTable::InsertMethodDesc(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsUnsealed());          // If we are sealed then we should not be adding to this hashtable
        PRECONDITION(CheckPointer(pMD));

        // Generic method definitions (e.g. D.m<U> or C<int>.m<U>) belong in method tables, not here
        PRECONDITION(!pMD->IsGenericMethodDefinition());
    }
    CONTRACTL_END

    InstMethodHashEntry_t * pNewEntry = (InstMethodHashEntry_t*)BaseAllocateEntry(NULL);

    DWORD dwKeyFlags = 0;
    if (pMD->RequiresInstArg())
        dwKeyFlags |= InstMethodHashEntry::RequiresInstArg;
    if (pMD->IsUnboxingStub())
        dwKeyFlags |= InstMethodHashEntry::UnboxingStub;
    pNewEntry->SetMethodAndFlags(pMD, dwKeyFlags);

    DWORD dwHash = Hash(pMD->GetMethodTable(), pMD->GetMemberDef(), pMD->GetMethodInstantiation());
    BaseInsertEntry(dwHash, pNewEntry);
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void InstMethodHashTable::EnumMemoryRegionsForEntry(InstMethodHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    if (pEntry->GetMethod().IsValid())
        pEntry->GetMethod()->EnumMemoryRegions(flags);
}
#endif // #ifdef DACCESS_COMPILE
