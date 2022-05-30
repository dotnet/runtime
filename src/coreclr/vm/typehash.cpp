// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: typehash.cpp
//

//

#include "common.h"
#include "excep.h"
#include "typehash.h"
#include "eeconfig.h"
#include "generics.h"
#include "typestring.h"
#include "typedesc.h"
#include "typekey.h"
#include "dacenumerablehash.inl"

#ifndef DACCESS_COMPILE

// ============================================================================
// Class hash table methods
// ============================================================================
/* static */
EETypeHashTable *EETypeHashTable::Create(LoaderAllocator* pAllocator, Module *pModule, DWORD dwNumBuckets, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LoaderHeap *pHeap = pAllocator->GetLowFrequencyHeap();
    EETypeHashTable *pThis = (EETypeHashTable*)pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(EETypeHashTable)));

    new (pThis) EETypeHashTable(pModule, pHeap, dwNumBuckets);

#ifdef _DEBUG
    pThis->InitUnseal();
#endif

    pThis->m_pAllocator = pAllocator;

    return pThis;
}

LoaderAllocator *EETypeHashTable::GetLoaderAllocator()
{
    WRAPPER_NO_CONTRACT;

    if (m_pAllocator)
    {
        return m_pAllocator;
    }
    else
    {
        _ASSERTE(m_pModule != NULL);
        return GetModule()->GetLoaderAllocator();
    }
}

#endif // #ifdef DACCESS_COMPILE

void EETypeHashTable::Iterator::Reset()
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

void EETypeHashTable::Iterator::Init()
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    if (m_pTable)
        m_pTable->Seal(); // The table cannot be changing while it is being iterated
#endif

    m_fIterating = false;
}

EETypeHashTable::Iterator::Iterator()
{
    WRAPPER_NO_CONTRACT;
    m_pTable = NULL;
    Init();
}

EETypeHashTable::Iterator::Iterator(EETypeHashTable * pTable)
{
    WRAPPER_NO_CONTRACT;
    m_pTable = pTable;
    Init();
}

EETypeHashTable::Iterator::~Iterator()
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    if (m_pTable)
        m_pTable->Unseal(); // Done with the iterator so we unseal
#endif
}

BOOL EETypeHashTable::FindNext(Iterator *it, EETypeHashEntry **ppEntry)
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

DWORD EETypeHashTable::GetCount()
{
    LIMITED_METHOD_CONTRACT;

    return BaseGetElementCount();
}

static DWORD HashTypeHandle(TypeHandle t);

// Calculate hash value for a type def or instantiated type def
static DWORD HashPossiblyInstantiatedType(mdTypeDef token, Instantiation inst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(TypeFromToken(token) == mdtTypeDef);
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    INT_PTR dwHash = 5381;

    dwHash = ((dwHash << 5) + dwHash) ^ token;
    if (!inst.IsEmpty())
    {
        // Hash n type parameters
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            dwHash = ((dwHash << 5) + dwHash) ^ inst[i].AsTAddr();
        }
    }

    return (DWORD)dwHash;
}

// Calculate hash value for a function pointer type
static DWORD HashFnPtrType(BYTE callConv, DWORD numArgs, TypeHandle *retAndArgTypes)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    INT_PTR dwHash = 5381;

    dwHash = ((dwHash << 5) + dwHash) ^ ELEMENT_TYPE_FNPTR;
    dwHash = ((dwHash << 5) + dwHash) ^ callConv;
    dwHash = ((dwHash << 5) + dwHash) ^ numArgs;

    for (DWORD i = 0; i <= numArgs; i++)
    {
        dwHash = ((dwHash << 5) + dwHash) ^ retAndArgTypes[i].AsTAddr();
    }

    return (DWORD)dwHash;
}

// Calculate hash value for an array/pointer/byref type
static DWORD HashParamType(CorElementType kind, TypeHandle typeParam)
{
    WRAPPER_NO_CONTRACT;
    INT_PTR dwHash = 5381;

    dwHash = ((dwHash << 5) + dwHash) ^ kind;
    dwHash = ((dwHash << 5) + dwHash) ^ typeParam.AsTAddr();

    return (DWORD)dwHash;
}

// Calculate hash value from type handle
static DWORD HashTypeHandle(TypeHandle t)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(t));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DWORD retVal = 0;

    if (t.HasTypeParam())
    {
        retVal = HashParamType(t.GetInternalCorElementType(), t.GetTypeParam());
    }
    else if (t.HasInstantiation())
    {
        retVal = HashPossiblyInstantiatedType(t.GetCl(), t.GetInstantiation());
    }
    else if (t.IsFnPtrType())
    {
        FnPtrTypeDesc* pTD = t.AsFnPtrType();
        retVal = HashFnPtrType(pTD->GetCallConv(), pTD->GetNumArgs(), pTD->GetRetAndArgTypesPointer());
    }
    else if (t.IsGenericVariable())
    {
        _ASSERTE(!"Generic variables are unexpected here.");
        retVal = 0;
    }
    else
    {
        retVal = HashPossiblyInstantiatedType(t.GetCl(), Instantiation());
    }

    return retVal;
}

// Calculate hash value from key
DWORD HashTypeKey(TypeKey* pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pKey));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (pKey->GetKind() == ELEMENT_TYPE_CLASS)
    {
        return HashPossiblyInstantiatedType(pKey->GetTypeToken(), pKey->GetInstantiation());
    }
    else if (pKey->GetKind() == ELEMENT_TYPE_FNPTR)
    {
        return HashFnPtrType(pKey->GetCallConv(), pKey->GetNumArgs(), pKey->GetRetAndArgTypes());
    }
    else
    {
        return HashParamType(pKey->GetKind(), pKey->GetElementType());
    }
}

// Look up a value in the hash table
//
// The logic is subtle: type handles in the hash table may not be
// restored, but we need to compare components of the types (rank and
// element type for arrays, generic type and instantiation for
// instantiated types) against pKey
//
// We avoid restoring types during search by cracking the signature
// encoding used by the zapper for out-of-module types e.g. in the
// instantiation of an instantiated type.
EETypeHashEntry_t *EETypeHashTable::FindItem(TypeKey* pKey)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pKey));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    EETypeHashEntry_t *  result = NULL;

    DWORD           dwHash = HashTypeKey(pKey);
    EETypeHashEntry_t * pSearch;
    CorElementType kind = pKey->GetKind();
    LookupContext sContext;

    if (kind == ELEMENT_TYPE_CLASS)
    {
        pSearch = BaseFindFirstEntryByHash(dwHash, &sContext);
        while (pSearch)
        {
            if (CompareInstantiatedType(pSearch->GetTypeHandle(), pKey->GetModule(), pKey->GetTypeToken(), pKey->GetInstantiation()))
            {
                result = pSearch;
                break;
            }

            pSearch = BaseFindNextEntryByHash(&sContext);
        }
    }
    else if (kind == ELEMENT_TYPE_FNPTR)
    {
        BYTE callConv = pKey->GetCallConv();
        DWORD numArgs = pKey->GetNumArgs();
        TypeHandle *retAndArgTypes = pKey->GetRetAndArgTypes();

        pSearch = BaseFindFirstEntryByHash(dwHash, &sContext);
        while (pSearch)
        {
            if (CompareFnPtrType(pSearch->GetTypeHandle(), callConv, numArgs, retAndArgTypes))
            {
                result = pSearch;
                break;
            }

            pSearch = BaseFindNextEntryByHash(&sContext);
        }
    }
    else
    {
        // Type parameters for array and pointer types are necessarily in the same loader module
        // as the constructed type itself, so we can just do handle comparisons
        // Unfortunately the rank of the array might live elsewhere

        for (pSearch = BaseFindFirstEntryByHash(dwHash, &sContext);
             pSearch != NULL;
             pSearch = BaseFindNextEntryByHash(&sContext))
        {
            if (!pSearch->GetTypeHandle().IsRestored())
            {
                // workaround: If we encounter an unrestored MethodTable, then it
                // isn't the type for which we are looking (plus, it will crash
                // in GetSignatureCorElementType).  However TypeDescs can be
                // accessed when unrestored.  Also they are accessed in that
                // manner at startup when we're loading the global types
                // (i.e. System.Object).

                if (!pSearch->GetTypeHandle().IsTypeDesc())
                {
                    // Not a match
                   continue;
                }
                else
                {
                    // We have an unrestored TypeDesc
                }
            }

            if (pSearch->GetTypeHandle().GetSignatureCorElementType() != kind)
                continue;

            if (pSearch->GetTypeHandle().GetTypeParam() != pKey->GetElementType())
                continue;

            if (kind == ELEMENT_TYPE_ARRAY)
            {
                TypeHandle th = pSearch->GetTypeHandle();
                {
                    if (th.GetRank() != pKey->GetRank())
                        continue;
                }
            }

            result = pSearch;
            break;
        }
    }

    return result;
}

BOOL EETypeHashTable::CompareInstantiatedType(TypeHandle t, Module *pModule, mdTypeDef token, Instantiation inst)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(t));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(!inst.IsEmpty());
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if (t.IsTypeDesc())
        return FALSE;

    // Even the EEClass pointer might be encoded
    MethodTable * pMT = t.AsMethodTable();

    if (pMT->GetNumGenericArgs() != inst.GetNumArgs())
        return FALSE;

    {
        // First check that the typedef tokens match
        if (pMT->GetCl() != token)
            return FALSE;

        // The class might not be restored, and its metadata module pointer might be encoded.
        // This will return NULL if the module for the corresponding generic class
        // is not loaded.
        Module *pGenericModuleIfLoaded = pMT->GetModuleIfLoaded();

        // Now check that the modules match
        if (!pGenericModuleIfLoaded ||
            dac_cast<TADDR>(pGenericModuleIfLoaded) !=
            dac_cast<TADDR>(pModule))
            return FALSE;

    }

    Instantiation candidateInst = t.GetInstantiation();

    // Now check the instantiations. Some type arguments might be encoded.
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        if (candidateInst[i] != inst[i])
        {
            return FALSE;
        }
    }

    return TRUE;
}

BOOL EETypeHashTable::CompareFnPtrType(TypeHandle t, BYTE callConv, DWORD numArgs, TypeHandle *retAndArgTypes)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(t));
        PRECONDITION(CheckPointer(retAndArgTypes));
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    if (!t.IsFnPtrType())
        return FALSE;

#ifndef DACCESS_COMPILE

    FnPtrTypeDesc* pTD = t.AsFnPtrType();

    if (pTD->GetNumArgs() != numArgs || pTD->GetCallConv() != callConv)
        return FALSE;

    // Now check the return and argument types. Some type arguments might be encoded.
    TypeHandle *retAndArgTypes2 = pTD->GetRetAndArgTypesPointer();
    for (DWORD i = 0; i <= numArgs; i++)
    {
        if (retAndArgTypes2[i] != retAndArgTypes[i])
        {
            return FALSE;
        }
    }

    return TRUE;

#else
    DacNotImpl();
    return FALSE;
#endif // #ifndef DACCESS_COMPILE
}

TypeHandle EETypeHashTable::GetValue(TypeKey *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    EETypeHashEntry_t *pItem = FindItem(pKey);

    if (pItem)
    {
        return pItem->GetTypeHandle();
    }

    return TypeHandle();
}

#ifndef DACCESS_COMPILE

BOOL EETypeHashTable::ContainsValue(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TypeKey typeKey = th.GetTypeKey();
    return !GetValue(&typeKey).IsNull();
}

// Insert a value not already in the hash table
VOID EETypeHashTable::InsertValue(TypeHandle data)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsUnsealed());          // If we are sealed then we should not be adding to this hashtable
        PRECONDITION(CheckPointer(data));
        PRECONDITION(!data.IsGenericTypeDefinition()); // Generic type defs live in typedef table (availableClasses)
        PRECONDITION(data.HasInstantiation() || data.HasTypeParam() || data.IsFnPtrType()); // It's an instantiated type or an array/ptr/byref type
        PRECONDITION(m_pModule == NULL || GetModule()->IsTenured()); // Destruct won't destruct m_pAvailableParamTypes for non-tenured modules - so make sure no one tries to insert one before the Module has been tenured
    }
    CONTRACTL_END

    EETypeHashEntry_t * pNewEntry = (EETypeHashEntry_t*)BaseAllocateEntry(NULL);

    pNewEntry->SetTypeHandle(data);

    BaseInsertEntry(HashTypeHandle(data), pNewEntry);
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void EETypeHashTable::EnumMemoryRegionsForEntry(EETypeHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    pEntry->GetTypeHandle().EnumMemoryRegions(flags);
}

#endif // #ifdef DACCESS_COMPILE

TypeHandle EETypeHashEntry::GetTypeHandle()
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Remove any hot entry indicator bit that may have been set as the result of Ngen saving.
    TADDR data = dac_cast<TADDR>(GetData());
    return TypeHandle::FromTAddr(data & ~0x1);
}

#ifndef DACCESS_COMPILE
void EETypeHashEntry::SetTypeHandle(TypeHandle handle)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // We plan to steal the low-order bit of the handle for ngen purposes.
    _ASSERTE((handle.AsTAddr() & 0x1) == 0);
    m_data = handle.AsPtr();
}
#endif // !DACCESS_COMPILE
