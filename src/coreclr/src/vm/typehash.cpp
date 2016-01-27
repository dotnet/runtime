// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#ifdef FEATURE_PREJIT
#include "zapsig.h"
#include "compile.h"
#endif
#include "ngenhash.inl"

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4244)
#endif // _MSC_VER

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
        return m_pModule->GetLoaderAllocator();
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

static DWORD HashTypeHandle(DWORD level, TypeHandle t);

// Calculate hash value for a type def or instantiated type def
static DWORD HashPossiblyInstantiatedType(DWORD level, mdTypeDef token, Instantiation inst)
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
        dwHash = ((dwHash << 5) + dwHash) ^ inst.GetNumArgs();

        // Hash two levels of the hiearchy. A simple nesting of generics instantiations is 
        // pretty common in generic collections, e.g.: ICollection<KeyValuePair<TKey, TValue>>
        if (level < 2)
        {
            // Hash n type parameters
            for (DWORD i = 0; i < inst.GetNumArgs(); i++)
            {
                dwHash = ((dwHash << 5) + dwHash) ^ HashTypeHandle(level+1, inst[i]);
            }
        }
    }

    return dwHash;
}

// Calculate hash value for a function pointer type
static DWORD HashFnPtrType(DWORD level, BYTE callConv, DWORD numArgs, TypeHandle *retAndArgTypes)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    INT_PTR dwHash = 5381;
    
    dwHash = ((dwHash << 5) + dwHash) ^ ELEMENT_TYPE_FNPTR;
    dwHash = ((dwHash << 5) + dwHash) ^ callConv;
    dwHash = ((dwHash << 5) + dwHash) ^ numArgs;
    if (level < 1)
    {
        for (DWORD i = 0; i <= numArgs; i++)
        {
            dwHash = ((dwHash << 5) + dwHash) ^ HashTypeHandle(level+1, retAndArgTypes[i]);
        }
    }

    return dwHash;
}

// Calculate hash value for an array/pointer/byref type
static DWORD HashParamType(DWORD level, CorElementType kind, TypeHandle typeParam)
{
    WRAPPER_NO_CONTRACT;
    INT_PTR dwHash = 5381;
    
    dwHash = ((dwHash << 5) + dwHash) ^ kind;
    dwHash = ((dwHash << 5) + dwHash) ^ HashTypeHandle(level, typeParam);

    return dwHash;
}

// Calculate hash value from type handle
static DWORD HashTypeHandle(DWORD level, TypeHandle t)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(t));
        PRECONDITION(!t.IsEncodedFixup());
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DWORD retVal = 0;
    
    INTERIOR_STACK_PROBE_NOTHROW_CHECK_THREAD(goto Exit;);

    if (t.HasTypeParam())
    {
        retVal =  HashParamType(level, t.GetInternalCorElementType(), t.GetTypeParam());
    }
    else if (t.IsGenericVariable())
    {
        retVal = (dac_cast<PTR_TypeVarTypeDesc>(t.AsTypeDesc())->GetToken());
    }
    else if (t.HasInstantiation())
    {
        retVal = HashPossiblyInstantiatedType(level, t.GetCl(), t.GetInstantiation());
    }
    else if (t.IsFnPtrType())
    {
        FnPtrTypeDesc* pTD = t.AsFnPtrType();
        retVal = HashFnPtrType(level, pTD->GetCallConv(), pTD->GetNumArgs(), pTD->GetRetAndArgTypesPointer());
    }
    else
        retVal = HashPossiblyInstantiatedType(level, t.GetCl(), Instantiation());

#if defined(FEATURE_STACK_PROBE) && !defined(DACCESS_COMPILE)
Exit: 
    ;
#endif
    END_INTERIOR_STACK_PROBE;
    
    return retVal;
}

// Calculate hash value from key
static DWORD HashTypeKey(TypeKey* pKey)
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
        return HashPossiblyInstantiatedType(0, pKey->GetTypeToken(), pKey->GetInstantiation());
    }
    else if (pKey->GetKind() == ELEMENT_TYPE_FNPTR)
    {
        return HashFnPtrType(0, pKey->GetCallConv(), pKey->GetNumArgs(), pKey->GetRetAndArgTypes());
    }
    else
    {
        return HashParamType(0, pKey->GetKind(), pKey->GetElementType());
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

            if (pSearch->GetTypeHandle().IsTypeDesc() == pKey->IsTemplateMethodTable())
                continue;

            if (kind == ELEMENT_TYPE_ARRAY)
            {
                if (pKey->IsTemplateMethodTable())
                {
                    if (pSearch->GetTypeHandle().AsMethodTable()->GetRank() != pKey->GetRank())
                        continue;
                }
                else
                {
                    ArrayTypeDesc *pATD = pSearch->GetTypeHandle().AsArray();   
#ifdef FEATURE_PREJIT
                    // This ensures that GetAssemblyIfLoaded operations that may be triggered by signature walks will succeed if at all possible.
                    ClrFlsThreadTypeSwitch genericInstantionCompareHolder(ThreadType_GenericInstantiationCompare); 

                    TADDR fixup = pATD->GetTemplateMethodTableMaybeTagged();
                    if (!CORCOMPILE_IS_POINTER_TAGGED(fixup))
                    {
                        TADDR canonFixup = pATD->GetTemplateMethodTable()->GetCanonicalMethodTableFixup();
                        if (CORCOMPILE_IS_POINTER_TAGGED(canonFixup))
                            fixup = canonFixup;
                    }

                    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
                    {
                        Module *pDefiningModule;
                        PCCOR_SIGNATURE pSig = m_pModule->GetEncodedSigIfLoaded(CORCOMPILE_UNTAG_TOKEN(fixup), &pDefiningModule);
                        if (pDefiningModule == NULL)
                            break;

                        _ASSERTE(*pSig == ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG);
                        pSig++;
                        _ASSERTE(*pSig == ELEMENT_TYPE_ARRAY);
                        pSig++;
                        SigPointer sp(pSig);
                        if (FAILED(sp.SkipExactlyOne()))
                            break; // return NULL;

                        ULONG data;
                        if (FAILED(sp.GetData(&data)))
                            break; // return NULL;
                        
                        if (data != pKey->GetRank())
                            continue;
                    }
                    else
#endif //FEATURE_PREJIT
                    {
                        if (pATD->GetRank() != pKey->GetRank())
                            continue;
                    }
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

#ifdef FEATURE_PREJIT
    // This ensures that GetAssemblyIfLoaded operations that may be triggered by signature walks will succeed if at all possible.
    ClrFlsThreadTypeSwitch genericInstantionCompareHolder(ThreadType_GenericInstantiationCompare); 

    TADDR fixup = pMT->GetCanonicalMethodTableFixup();
        
    // The EEClass pointer is actually an encoding. 
    if (CORCOMPILE_IS_POINTER_TAGGED(fixup))
    {
        Module *pDefiningModule;
        PCCOR_SIGNATURE pSig = m_pModule->GetEncodedSigIfLoaded(CORCOMPILE_UNTAG_TOKEN(fixup), &pDefiningModule);

        // First check that the modules for the generic type defs match
        if (dac_cast<TADDR>(pDefiningModule) !=
            dac_cast<TADDR>(pModule))
            return FALSE;

        // Now crack the signature encoding, expected to be an instantiated type
        _ASSERTE(*pSig == ELEMENT_TYPE_GENERICINST);
        pSig++;
        _ASSERTE(*pSig == ELEMENT_TYPE_CLASS || *pSig == ELEMENT_TYPE_VALUETYPE);
        pSig++;

        // Check that the tokens of the generic type def match
        if (CorSigUncompressToken(pSig) != token)
            return FALSE;
    }

    // The EEClass pointer is a real pointer
    else 
#endif //FEATURE_PREJIT
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
        // Fetch the type handle as TADDR. It may be may be encoded fixup - TypeHandle debug-only validation 
        // asserts on encoded fixups.
        DACCOP_IGNORE(CastOfMarshalledType, "Dual mode DAC problem, but since the size is the same, the cast is safe");
        TADDR candidateArg = ((FixupPointer<TADDR> *)candidateInst.GetRawArgs())[i].GetValue();

        if (!ZapSig::CompareTaggedPointerToTypeHandle(m_pModule, candidateArg, inst[i]))
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
#ifdef FEATURE_PREJIT
    // This ensures that GetAssemblyIfLoaded operations that may be triggered by signature walks will succeed if at all possible.
    ClrFlsThreadTypeSwitch genericInstantionCompareHolder(ThreadType_GenericInstantiationCompare); 
#endif

    FnPtrTypeDesc* pTD = t.AsFnPtrType();

    if (pTD->GetNumArgs() != numArgs || pTD->GetCallConv() != callConv)
        return FALSE;

    // Now check the return and argument types. Some type arguments might be encoded.
    TypeHandle *retAndArgTypes2 = pTD->GetRetAndArgTypesPointer();
    for (DWORD i = 0; i <= numArgs; i++)
    {
        TADDR candidateArg = retAndArgTypes2[i].AsTAddr();
        if (!ZapSig::CompareTaggedPointerToTypeHandle(m_pModule, candidateArg, retAndArgTypes[i]))
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
        TypeHandle th = pItem->GetTypeHandle();
        g_IBCLogger.LogTypeHashTableAccess(&th);
        return pItem->GetTypeHandle();
    }
    else
        return TypeHandle();
}

#ifndef DACCESS_COMPILE

BOOL EETypeHashTable::ContainsValue(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
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
        PRECONDITION(!data.IsEncodedFixup());
        PRECONDITION(!data.IsGenericTypeDefinition()); // Generic type defs live in typedef table (availableClasses)
        PRECONDITION(data.HasInstantiation() || data.HasTypeParam() || data.IsFnPtrType()); // It's an instantiated type or an array/ptr/byref type
        PRECONDITION(!m_pModule || m_pModule->IsTenured()); // Destruct won't destruct m_pAvailableParamTypes for non-tenured modules - so make sure no one tries to insert one before the Module has been tenured
    }
    CONTRACTL_END

    EETypeHashEntry_t * pNewEntry = (EETypeHashEntry_t*)BaseAllocateEntry(NULL);

    pNewEntry->SetTypeHandle(data);

    BaseInsertEntry(HashTypeHandle(0, data), pNewEntry);
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

#ifdef _DEBUG
void EETypeHashTableSeal(EETypeHashTable * pTable) { WRAPPER_NO_CONTRACT; pTable->Seal(); }
void EETypeHashTableUnseal(EETypeHashTable * pTable) { WRAPPER_NO_CONTRACT; pTable->Unseal(); }
typedef  Wrapper<EETypeHashTable *, EETypeHashTableSeal, EETypeHashTableUnseal> EETypeHashTableSealHolder;
#endif

// Save the hash table and any type descriptors referenced by it
// Method tables must be saved separately
void EETypeHashTable::Save(DataImage *image, Module *module, CorProfileData *profileData)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(image->GetModule() == m_pModule);
    }
    CONTRACTL_END;

#ifdef _DEBUG
    // The table should not change while we are walking the buckets
    EETypeHashTableSealHolder h(this);
#endif

    // The base class will call us back for every entry to see if it's considered hot. To determine this we
    // have to walk through the profiling data. It's very inefficient for us to do this every time. Instead
    // we'll walk the data once just now and mark each hot entry as we find it.
    CORBBTPROF_TOKEN_INFO * pTypeProfilingData = profileData->GetTokenFlagsData(TypeProfilingData);
    DWORD                   cTypeProfilingData = profileData->GetTokenFlagsCount(TypeProfilingData);

    for (unsigned int i = 0; i < cTypeProfilingData; i++)
    {
        CORBBTPROF_TOKEN_INFO *entry = &pTypeProfilingData[i];
        mdToken token = entry->token;
        DWORD   flags = entry->flags;
                
        if (TypeFromToken(token) != ibcTypeSpec)
            continue;

        if ((flags & (1 << ReadTypeHashTable)) == 0)
            continue;

        CORBBTPROF_BLOB_ENTRY *pBlobEntry = profileData->GetBlobStream();
        if (pBlobEntry)
        {
            while (pBlobEntry->TypeIsValid())
            {
                if (TypeFromToken(pBlobEntry->token) == ibcTypeSpec)
                {
                    _ASSERTE(pBlobEntry->type == ParamTypeSpec);
                            
                    CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY *) pBlobEntry;
                            
                    if (pBlobEntry->token == token)
                    {
                        if (flags & (1<<ReadTypeHashTable))
                        {
                            TypeHandle th = m_pModule->LoadIBCTypeHelper(pBlobSigEntry);
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
                            g_pConfig->DebugCheckAndForceIBCFailure(EEConfig::CallSite_8);
#endif
                            if (!th.IsNull())
                            {
                                // Found a hot type. See if we have it in our table.
                                DWORD dwHash = HashTypeHandle(0, th);
                                LookupContext sContext;
                                EETypeHashEntry_t *pSearch = BaseFindFirstEntryByHash(dwHash, &sContext);
                                while (pSearch)
                                {
                                    if (pSearch->GetTypeHandle() == th)
                                    {
                                        // Found the corresponding entry in the table. Mark it as hot.
                                        pSearch->MarkAsHot();
                                        break;
                                    }

                                    pSearch = BaseFindNextEntryByHash(&sContext);
                                }
                            }
                        }
                    }
                }
                pBlobEntry = pBlobEntry->GetNextEntry();
            }
        }
    }

    BaseSave(image, profileData);
}

bool EETypeHashTable::ShouldSave(DataImage *pImage, EETypeHashEntry_t *pEntry)
{
    STANDARD_VM_CONTRACT;

    return !!pImage->GetPreloader()->IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE(pEntry->GetTypeHandle().AsPtr()));
}

bool EETypeHashTable::IsHotEntry(EETypeHashEntry_t *pEntry, CorProfileData *pProfileData)
{
    STANDARD_VM_CONTRACT;

    // EETypeHashTable::Save() will have marked the entry as hot if the profile data indicated this.
    return pEntry->IsHot();
}

bool EETypeHashTable::SaveEntry(DataImage *pImage, CorProfileData *pProfileData, EETypeHashEntry_t *pOldEntry, EETypeHashEntry_t *pNewEntry, EntryMappingTable *pMap)
{
    LIMITED_METHOD_CONTRACT;

    return false;
}

void EETypeHashTable::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    BaseFixup(image);

    image->ZeroPointerField(this, offsetof(EETypeHashTable, m_pAllocator));

#ifdef _DEBUG
    // The persisted table should be unsealed.
    EETypeHashTable *pNewTable = (EETypeHashTable*) image->GetImagePointer(this);
    pNewTable->InitUnseal();
#endif
}

void EETypeHashTable::FixupEntry(DataImage *pImage, EETypeHashEntry_t *pEntry, void *pFixupBase, DWORD cbFixupOffset)
{
    STANDARD_VM_CONTRACT;

    TypeHandle pType = pEntry->GetTypeHandle();
    _ASSERTE(!pType.IsNull());

    // Clear any hot entry marking in the data, it's not needed after the Save phase.
    pEntry->SetTypeHandle(pType);

    if (pType.IsTypeDesc())
    {
        pImage->FixupField(pFixupBase, cbFixupOffset + offsetof(EETypeHashEntry_t, m_data),
                           pType.AsTypeDesc(), 2);

        pType.AsTypeDesc()->Fixup(pImage);
    }
    else
    {
        pImage->FixupField(pFixupBase, cbFixupOffset + offsetof(EETypeHashEntry_t, m_data),
                           pType.AsMethodTable());

        pType.AsMethodTable()->Fixup(pImage);
    }
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
EETypeHashTable::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    
    BaseEnumMemoryRegions(flags);
}

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
    return TypeHandle::FromTAddr(m_data & ~0x1);
}

void EETypeHashEntry::SetTypeHandle(TypeHandle handle)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // We plan to steal the low-order bit of the handle for ngen purposes.
    _ASSERTE((handle.AsTAddr() & 0x1) == 0);
    m_data = handle.AsTAddr();
}

#ifdef FEATURE_PREJIT
bool EETypeHashEntry::IsHot()
{
    LIMITED_METHOD_CONTRACT;

    // Low order bit of data field indicates a hot entry.
    return (m_data & 1) != 0;
}

void EETypeHashEntry::MarkAsHot()
{
    LIMITED_METHOD_CONTRACT;

    // Low order bit of data field indicates a hot entry.
    m_data |= 0x1;
}
#endif // FEATURE_PREJIT

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER: warning C4244
