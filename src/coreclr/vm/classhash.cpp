// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Hash table associated with each module that records for all types defined in that module the mapping
// between type name and token (or TypeHandle).
//

#include "common.h"
#include "classhash.h"
#include "dacenumerablehash.inl"
#include "fstring.h"
#include "classhash.inl"

PTR_EEClassHashEntry EEClassHashEntry::GetEncloser()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return m_pEncloser;
}

PTR_VOID EEClassHashEntry::GetData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return m_Data;
}

#ifndef DACCESS_COMPILE
void EEClassHashEntry::SetData(void *data)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_Data = data;
}

void EEClassHashEntry::SetEncloser(EEClassHashEntry *pEncloser)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pEncloser = pEncloser;
}

/*static*/
EEClassHashTable *EEClassHashTable::Create(Module *pModule, DWORD dwNumBuckets, BOOL bCaseInsensitive, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());

    }
    CONTRACTL_END;

    LoaderHeap *pHeap = pModule->GetAssembly()->GetLowFrequencyHeap();
    EEClassHashTable *pThis = (EEClassHashTable*)pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(EEClassHashTable)));

    // The base class get initialized through chaining of constructors. We allocated the hash instance via the
    // loader heap instead of new so use an in-place new to call the constructors now.
    new (pThis) EEClassHashTable(pModule, pHeap, dwNumBuckets);

    pThis->m_bCaseInsensitive = bCaseInsensitive;

    return pThis;
}

EEClassHashEntry_t *EEClassHashTable::AllocNewEntry(AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;

        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    // Simply defer to the base class for entry allocation (this is required, the base class wraps the entry
    // it returns to us in its own metadata).
    return BaseAllocateEntry(pamTracker);
}

#endif // !DACCESS_COMPILE

VOID EEClassHashTable::UncompressModuleAndNonExportClassDef(HashDatum Data, Module **ppModule, mdTypeDef *pCL)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    DWORD dwData = (DWORD)dac_cast<TADDR>(Data);
    _ASSERTE((dwData & EECLASSHASH_TYPEHANDLE_DISCR) == EECLASSHASH_TYPEHANDLE_DISCR);
    _ASSERTE(!(dwData & EECLASSHASH_MDEXPORT_DISCR));

    *pCL = ((dwData >> 1) & 0x00ffffff) | mdtTypeDef;
    *ppModule = GetModule();
}

bool EEClassHashTable::UncompressModuleAndClassDef(HashDatum Data, Loader::LoadFlag loadFlag,
                                                   Module **ppModule, mdTypeDef *pCL,
                                                   mdExportedType *pmdFoundExportedType)
{
    CONTRACT(bool)
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM();); }
        MODE_ANY;

        PRECONDITION(CheckPointer(pCL));
        PRECONDITION(CheckPointer(ppModule));
        POSTCONDITION(*ppModule != nullptr || loadFlag != Loader::Load);
        SUPPORTS_DAC;
    }
    CONTRACT_END

    DWORD dwData = (DWORD)dac_cast<TADDR>(Data);
    _ASSERTE((dwData & EECLASSHASH_TYPEHANDLE_DISCR) == EECLASSHASH_TYPEHANDLE_DISCR);
    if(dwData & EECLASSHASH_MDEXPORT_DISCR) {
        *pmdFoundExportedType = ((dwData >> 1) & 0x00ffffff) | mdtExportedType;

        *ppModule = GetModule()->GetAssembly()->FindModuleByExportedType(*pmdFoundExportedType, loadFlag, mdTypeDefNil, pCL);
    }
    else {
        UncompressModuleAndNonExportClassDef(Data, ppModule, pCL);
        *pmdFoundExportedType = mdTokenNil;
        _ASSERTE(*ppModule != nullptr); // Should never fail.
    }

    RETURN (*ppModule != nullptr);
}

/* static */
mdToken EEClassHashTable::UncompressModuleAndClassDef(HashDatum Data)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    DWORD dwData = (DWORD)dac_cast<TADDR>(Data); // 64Bit: Pointer truncation is OK here - it's not actually a pointer
    _ASSERTE((dwData & EECLASSHASH_TYPEHANDLE_DISCR) == EECLASSHASH_TYPEHANDLE_DISCR);

    if(dwData & EECLASSHASH_MDEXPORT_DISCR)
        return ((dwData >> 1) & 0x00ffffff) | mdtExportedType;
    else
        return ((dwData >> 1) & 0x00ffffff) | mdtTypeDef;
}

static void ConstructKeyFromDataCaseInsensitive(EEClassHashTable::ConstructKeyCallback* pCallback, LPSTR pszNameSpace, LPSTR pszName)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END

    LPUTF8 Key[2];

    StackSString nameSpace(SString::Utf8, pszNameSpace);
    nameSpace.LowerCase();

    StackScratchBuffer nameSpaceBuffer;
    Key[0] = (LPUTF8)nameSpace.GetUTF8(nameSpaceBuffer);

    StackSString name(SString::Utf8, pszName);
    name.LowerCase();

    StackScratchBuffer nameBuffer;
    Key[1] = (LPUTF8)name.GetUTF8(nameBuffer);

    pCallback->UseKeys(Key);
}

VOID EEClassHashTable::ConstructKeyFromData(PTR_EEClassHashEntry pEntry, // IN  : Entry to compare
                                            ConstructKeyCallback *pCallback) // This class will process the output
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(MODE_ANY);
        WRAPPER(GC_TRIGGERS);
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else WRAPPER(FORBID_FAULT);
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    {
#ifdef _DEBUG_IMPL
        _ASSERTE(!(m_bCaseInsensitive && FORBIDGC_LOADER_USE_ENABLED()));
#endif

        // cqb - If m_bCaseInsensitive is true for the hash table, the bytes in Key will be allocated
        // from cqb. This is to prevent wasting bytes in the Loader Heap. Thusly, it is important to note that
        // in this case, the lifetime of Key is bounded by the lifetime of cqb, which will free the memory
        // it allocated on destruction.

        _ASSERTE(m_pModule != NULL);
        LPSTR        pszName = NULL;
        LPSTR        pszNameSpace = NULL;
        IMDInternalImport *pInternalImport = NULL;

        PTR_VOID Data = NULL;
        if (!m_bCaseInsensitive)
            Data = pEntry->GetData();
        else
            Data = (PTR_EEClassHashEntry(pEntry->GetData()))->GetData();

        // Lower bit is a discriminator.  If the lower bit is NOT SET, it means we have
        // a TypeHandle, otherwise, we have a mdtTypedef/mdtExportedType.
        if ((dac_cast<TADDR>(Data) & EECLASSHASH_TYPEHANDLE_DISCR) == 0)
        {
            TypeHandle pType = TypeHandle::FromPtr(Data);
            _ASSERTE (pType.GetMethodTable());
            MethodTable *pMT = pType.GetMethodTable();
            _ASSERTE(pMT != NULL);
            IfFailThrow(pMT->GetMDImport()->GetNameOfTypeDef(pMT->GetCl(), (LPCSTR *)&pszName, (LPCSTR *)&pszNameSpace));
        }
        else // We have a mdtoken
        {
            // call the lightweight version first
            mdToken mdtUncompressed = UncompressModuleAndClassDef(Data);
            if (TypeFromToken(mdtUncompressed) == mdtExportedType)
            {
                IfFailThrow(GetModule()->GetClassLoader()->GetAssembly()->GetMDImport()->GetExportedTypeProps(
                    mdtUncompressed,
                    (LPCSTR *)&pszNameSpace,
                    (LPCSTR *)&pszName,
                    NULL,   //mdImpl
                    NULL,   // type def
                    NULL)); // flags
            }
            else
            {
                _ASSERTE(TypeFromToken(mdtUncompressed) == mdtTypeDef);

                Module *    pUncompressedModule;
                mdTypeDef   UncompressedCl;
                UncompressModuleAndNonExportClassDef(Data, &pUncompressedModule, &UncompressedCl);
                _ASSERTE (pUncompressedModule && "Uncompressed token of unexpected type");
                pInternalImport = pUncompressedModule->GetMDImport();
                _ASSERTE(pInternalImport && "Uncompressed token has no MD import");
                IfFailThrow(pInternalImport->GetNameOfTypeDef(UncompressedCl, (LPCSTR *)&pszName, (LPCSTR *)&pszNameSpace));
            }
        }

        if (!m_bCaseInsensitive)
        {
            LPUTF8 Key[2];

            Key[0] = pszNameSpace;
            Key[1] = pszName;

            pCallback->UseKeys(Key);
        }
        else
        {
#ifndef DACCESS_COMPILE
            CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);
            ConstructKeyFromDataCaseInsensitive(pCallback, pszNameSpace, pszName);
#else
            DacNotImpl();
#endif // #ifndef DACCESS_COMPILE
        }
    }

}

#ifndef DACCESS_COMPILE

EEClassHashEntry_t *EEClassHashTable::InsertValue(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, PTR_VOID Data, EEClassHashEntry_t *pEncloser, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;

        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    _ASSERTE(pszNamespace != NULL);
    _ASSERTE(pszClassName != NULL);
    _ASSERTE(m_pModule != NULL);

    EEClassHashEntry *pEntry = BaseAllocateEntry(pamTracker);

    pEntry->SetData(Data);
    pEntry->SetEncloser(pEncloser);
#ifdef _DEBUG
    pEntry->DebugKey[0] = pszNamespace;
    pEntry->DebugKey[1] = pszClassName;
#endif

    BaseInsertEntry(Hash(pszNamespace, pszClassName), pEntry);

    return pEntry;
}

#ifdef _DEBUG
class ConstructKeyCallbackValidate : public EEClassHashTable::ConstructKeyCallback
{
public:
    virtual void UseKeys(_In_reads_(2) LPUTF8 *Key)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_DEBUG_ONLY;
        _ASSERTE (strcmp(pNewEntry->DebugKey[1], Key[1]) == 0);
        _ASSERTE (strcmp(pNewEntry->DebugKey[0], Key[0]) == 0);
        SUPPORTS_DAC;
    }

    EEClassHashEntry_t *pNewEntry;

};
#endif // _DEBUG

// This entrypoint lets the caller separate the allocation of the entrypoint from the actual insertion into the hashtable. (This lets us
// do multiple insertions without having to worry about an OOM occurring inbetween.)
//
// The newEntry must have been allocated using AllocEntry. It must not be referenced by any other entity (other than a holder or tracker)
// If this function throws, the caller is responsible for freeing the entry.
EEClassHashEntry_t *EEClassHashTable::InsertValueUsingPreallocatedEntry(EEClassHashEntry_t *pNewEntry, LPCUTF8 pszNamespace, LPCUTF8 pszClassName, PTR_VOID Data, EEClassHashEntry_t *pEncloser)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;

        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    pNewEntry->SetData(Data);
    pNewEntry->SetEncloser(pEncloser);

#ifdef _DEBUG
    pNewEntry->DebugKey[0] = pszNamespace;
    pNewEntry->DebugKey[1] = pszClassName;
#endif

    BaseInsertEntry(Hash(pszNamespace, pszClassName), pNewEntry);

    return pNewEntry;
}

EEClassHashEntry_t *EEClassHashTable::InsertValueIfNotFound(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, PTR_VOID *pData, EEClassHashEntry_t *pEncloser, BOOL IsNested, BOOL *pbFound, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););

        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;

    _ASSERTE(m_pModule != NULL);
    _ASSERTE(pszNamespace != NULL);
    _ASSERTE(pszClassName != NULL);

    EEClassHashEntry_t * pNewEntry = FindItem(pszNamespace, pszClassName, IsNested, NULL);

    if (pNewEntry)
    {
        *pData = pNewEntry->GetData();
        *pbFound = TRUE;
        return pNewEntry;
    }

    // Reached here implies that we didn't find the entry and need to insert it
    *pbFound = FALSE;

    pNewEntry = BaseAllocateEntry(pamTracker);

    pNewEntry->SetData(*pData);
    pNewEntry->SetEncloser(pEncloser);

#ifdef _DEBUG
    pNewEntry->DebugKey[0] = pszNamespace;
    pNewEntry->DebugKey[1] = pszClassName;
#endif

    BaseInsertEntry(Hash(pszNamespace, pszClassName), pNewEntry);

    return pNewEntry;
}

#endif // !DACCESS_COMPILE

EEClassHashEntry_t *EEClassHashTable::FindItem(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, BOOL IsNested, LookupContext *pContext)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(m_pModule != NULL);
    _ASSERTE(pszNamespace != NULL);
    _ASSERTE(pszClassName != NULL);

    // It's legal for the caller not to pass us a LookupContext (when the type being queried is not nested
    // there will never be any need to iterate over the search results). But we might need to iterate
    // internally (since we lookup via hash and hashes may collide). So substitute our own private context if
    // one was not provided.
    LookupContext sAltContext;
    if (pContext == NULL)
        pContext = &sAltContext;

    // The base class provides the ability to enumerate all entries with the same hash code. We call this and
    // further check which of these entries actually match the full key (there can be multiple hits with
    // nested types in the picture).
    PTR_EEClassHashEntry pSearch = BaseFindFirstEntryByHash(Hash(pszNamespace, pszClassName), pContext);

    while (pSearch)
    {
        LPCUTF8 rgKey[] = { pszNamespace, pszClassName };

        if (CompareKeys(pSearch, rgKey))
        {
            // If (IsNested), then we're looking for a nested class
            // If (pSearch->pEncloser), we've found a nested class
            if ((IsNested != FALSE) == (pSearch->GetEncloser() != NULL))
            {
                return pSearch;
            }
        }

        pSearch = BaseFindNextEntryByHash(pContext);
    }

    return NULL;
}

EEClassHashEntry_t *EEClassHashTable::FindNextNestedClass(const NameHandle* pName, PTR_VOID *pData, LookupContext *pContext)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(m_pModule != NULL);
    _ASSERTE(pName);

    if (pName->GetNameSpace())
    {
        return FindNextNestedClass(pName->GetNameSpace(), pName->GetName(), pData, pContext);
    }
    else {
#ifndef DACCESS_COMPILE
        return FindNextNestedClass(pName->GetName(), pData, pContext); // this won't support dac--
                                                                       // it allocates a new namespace string
#else
        DacNotImpl();
        return NULL;
#endif
    }
}


EEClassHashEntry_t *EEClassHashTable::FindNextNestedClass(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, PTR_VOID *pData, LookupContext *pContext)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(m_pModule != NULL);

    PTR_EEClassHashEntry pSearch = BaseFindNextEntryByHash(pContext);

    while (pSearch)
    {
        LPCUTF8 rgKey[] = { pszNamespace, pszClassName };

        if (pSearch->GetEncloser() && CompareKeys(pSearch, rgKey))
        {
            *pData = pSearch->GetData();
            return pSearch;
        }

        pSearch = BaseFindNextEntryByHash(pContext);
    }

    return NULL;
}

const UTF8 Utf8Empty[] = { 0 };

EEClassHashEntry_t *EEClassHashTable::FindNextNestedClass(LPCUTF8 pszFullyQualifiedName, PTR_VOID *pData, LookupContext *pContext)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_pModule != NULL);

    CQuickBytes szNamespace;

    LPCUTF8 pNamespace = Utf8Empty;
    LPCUTF8 p;

    if ((p = ns::FindSep(pszFullyQualifiedName)) != NULL)
    {
        SIZE_T d = p - pszFullyQualifiedName;

        FAULT_NOT_FATAL();
        pNamespace = szNamespace.SetStringNoThrow(pszFullyQualifiedName, d);

        if (NULL == pNamespace)
        {
            return NULL;
        }

        p++;
    }
    else
    {
        p = pszFullyQualifiedName;
    }

    return FindNextNestedClass(pNamespace, p, pData, pContext);
}


EEClassHashEntry_t * EEClassHashTable::GetValue(LPCUTF8 pszFullyQualifiedName, PTR_VOID *pData, BOOL IsNested, LookupContext *pContext)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(m_pModule != NULL);

    CQuickBytes szNamespace;

    LPCUTF8 pNamespace = Utf8Empty;

    LPCUTF8 p = ns::FindSep(pszFullyQualifiedName);

    if (p != NULL)
    {
        SIZE_T d = p - pszFullyQualifiedName;

        FAULT_NOT_FATAL();
        pNamespace = szNamespace.SetStringNoThrow(pszFullyQualifiedName, d);

        if (NULL == pNamespace)
        {
            return NULL;
        }

        p++;
    }
    else
    {
        p = pszFullyQualifiedName;
    }

    EEClassHashEntry_t * ret = GetValue(pNamespace, p, pData, IsNested, pContext);

    return ret;
}


EEClassHashEntry_t * EEClassHashTable::GetValue(LPCUTF8 pszNamespace, LPCUTF8 pszClassName, PTR_VOID *pData, BOOL IsNested, LookupContext *pContext)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    _ASSERTE(m_pModule != NULL);
    EEClassHashEntry_t *pItem = FindItem(pszNamespace, pszClassName, IsNested, pContext);
    if (pItem)
        *pData = pItem->GetData();

    return pItem;
}


EEClassHashEntry_t * EEClassHashTable::GetValue(const NameHandle* pName, PTR_VOID *pData, BOOL IsNested, LookupContext *pContext)
{
    CONTRACTL
    {
        // for DAC builds m_bCaseInsensitive should be false
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    _ASSERTE(pName);
    _ASSERTE(m_pModule != NULL);
    if(pName->GetNameSpace() == NULL) {
        return GetValue(pName->GetName(), pData, IsNested, pContext);
    }
    else {
        return GetValue(pName->GetNameSpace(), pName->GetName(), pData, IsNested, pContext);
    }
}

class ConstructKeyCallbackCompare : public EEClassHashTable::ConstructKeyCallback
{
public:
    virtual void UseKeys(_In_reads_(2) LPUTF8 *pKey1)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        bReturn = (
            ((pKey1[0] == pKey2[0]) && (pKey1[1] == pKey2[1])) ||
            ((strcmp (pKey1[0], pKey2[0]) == 0) && (strcmp (pKey1[1], pKey2[1]) == 0))
            );
    }

    LPCUTF8 *pKey2;
    BOOL     bReturn;
};

// Returns TRUE if two keys are the same string.
//
// The case-insensitive table can throw OOM out of this function. The case-sensitive table can't.
BOOL EEClassHashTable::CompareKeys(PTR_EEClassHashEntry pEntry, LPCUTF8 * pKey2)
{
    CONTRACTL
    {
        if (m_bCaseInsensitive) THROWS; else NOTHROW;
        if (m_bCaseInsensitive) GC_TRIGGERS; else GC_NOTRIGGER;
        if (m_bCaseInsensitive) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    _ASSERTE(m_pModule != NULL);
    _ASSERTE (pEntry);
    _ASSERTE (pKey2);

    ConstructKeyCallbackCompare cback;

    cback.pKey2 = pKey2;

    {
        CONTRACT_VIOLATION(ThrowsViolation);
        ConstructKeyFromData(pEntry, &cback);
    }

    return cback.bReturn;
}


#ifndef DACCESS_COMPILE

/*===========================MakeCaseInsensitiveTable===========================
**Action: Creates a case-insensitive lookup table for class names.  We create a
**        full path (namespace & class name) in lowercase and then use that as the
**        key in our table.  The hash datum is a pointer to the EEClassHashEntry in this
**        table.
**
!!        You MUST have already acquired the appropriate lock before calling this.!!
**
**Returns:The newly allocated and completed hashtable.
==============================================================================*/

class ConstructKeyCallbackCaseInsensitive : public EEClassHashTable::ConstructKeyCallback
{
public:
    virtual void UseKeys(_In_reads_(2) LPUTF8 *key)
    {
        WRAPPER_NO_CONTRACT;

        //Build the cannonical name (convert it to lowercase).
        //Key[0] is the namespace, Key[1] is class name.

        pLoader->CreateCanonicallyCasedKey(key[0], key[1], ppszLowerNameSpace, ppszLowerClsName);
    }

    ClassLoader *pLoader;
    LPUTF8      *ppszLowerNameSpace;
    LPUTF8      *ppszLowerClsName;

};

EEClassHashTable *EEClassHashTable::MakeCaseInsensitiveTable(Module *pModule, AllocMemTracker *pamTracker)
{
    EEClassHashEntry_t *pTempEntry;
    LPUTF8         pszLowerClsName = NULL;
    LPUTF8         pszLowerNameSpace = NULL;

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););

        PRECONDITION(!FORBIDGC_LOADER_USE_ENABLED());
    }
    CONTRACTL_END;



    _ASSERTE(m_pModule != NULL);
    _ASSERTE(pModule == GetModule());

    // Allocate the table and verify that we actually got one.
    EEClassHashTable * pCaseInsTable = EEClassHashTable::Create(pModule,
                                                                max(BaseGetElementCount() / 2, 11),
                                                                TRUE /* bCaseInsensitive */,
                                                                pamTracker);

    // Walk all of the buckets and insert them into our new case insensitive table
    BaseIterator sIter;
    BaseInitIterator(&sIter);
    while ((pTempEntry = sIter.Next()) != NULL)
    {
        ConstructKeyCallbackCaseInsensitive cback;

        cback.pLoader            = pModule->GetClassLoader();
        cback.ppszLowerNameSpace = &pszLowerNameSpace;
        cback.ppszLowerClsName   = &pszLowerClsName;
        ConstructKeyFromData(pTempEntry, &cback);

        //Add the newly created name to our hash table.  The hash datum is a pointer
        //to the entry associated with that name in this hashtable.
        pCaseInsTable->InsertValue(pszLowerNameSpace, pszLowerClsName, (PTR_VOID)pTempEntry, pTempEntry->GetEncloser(), pamTracker);
    }

    return pCaseInsTable;
}
#endif // !DACCESS_COMPILE
