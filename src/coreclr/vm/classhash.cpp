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

    return VolatileLoadWithoutBarrier(&m_Data);
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
EEClassHashTable *EEClassHashTable::Create(Module *pModule, DWORD dwNumBuckets, PTR_EEClassHashTable pCaseSensitiveTable, AllocMemTracker *pamTracker)
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

    pThis->m_pCaseSensitiveTable = pCaseSensitiveTable != NULL ? pCaseSensitiveTable : pThis;

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

    Key[0] = (LPUTF8)nameSpace.GetUTF8();

    StackSString name(SString::Utf8, pszName);
    name.LowerCase();

    Key[1] = (LPUTF8)name.GetUTF8();

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
        if (IsCaseInsensitiveTable()) INJECT_FAULT(COMPlusThrowOM();); else WRAPPER(FORBID_FAULT);
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    {
#ifdef _DEBUG_IMPL
        _ASSERTE(!(IsCaseInsensitiveTable() && FORBIDGC_LOADER_USE_ENABLED()));
#endif

        // If IsCaseInsensitiveTable() is true for the hash table, strings passed to the ConstructKeyCallback instance
        // will be dynamically allocated. This is to prevent wasting bytes in the Loader Heap. Thusly, it is important 
        // to note that in this case, the lifetime of Key is bounded by the lifetime of the single call to UseKeys, and
        // will be freed when that function returns.

        _ASSERTE(m_pModule != NULL);
        LPSTR        pszName = NULL;
        LPSTR        pszNameSpace = NULL;
        IMDInternalImport *pInternalImport = NULL;

        PTR_VOID Data = NULL;
        if (!IsCaseInsensitiveTable())
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

        if (!IsCaseInsensitiveTable())
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

    BaseInsertEntry(Hash(pszNamespace, pszClassName, pEncloser != NULL ? GetHash(pEncloser) : 0), pNewEntry);

    return pNewEntry;
}

#endif // !DACCESS_COMPILE

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
        if (IsCaseInsensitiveTable()) THROWS; else NOTHROW;
        if (IsCaseInsensitiveTable()) GC_TRIGGERS; else GC_NOTRIGGER;
        if (IsCaseInsensitiveTable()) INJECT_FAULT(COMPlusThrowOM();); else FORBID_FAULT;
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

        //Build the canonical name (convert it to lowercase).
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
                                                                max(BaseGetElementCount() / 2, 11u),
                                                                this,
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
        pCaseInsTable->InsertValueUsingPreallocatedEntry(pCaseInsTable->BaseAllocateEntry(pamTracker), pszLowerNameSpace, pszLowerClsName, pTempEntry, pTempEntry->GetEncloser());
    }

    return pCaseInsTable;
}
#endif // !DACCESS_COMPILE

BOOL CompareNestedEntryWithExportedType(IMDInternalImport *  pImport,
                                        mdExportedType       mdCurrent,
                                        EEClassHashTable *   pClassHash,
                                        PTR_EEClassHashEntry pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCUTF8 Key[2];

    do
    {
        if (FAILED(pImport->GetExportedTypeProps(
            mdCurrent,
            &Key[0],
            &Key[1],
            &mdCurrent,
            NULL,   //binding (type def)
            NULL))) //flags
        {
            return FALSE;
        }

        if (pClassHash->CompareKeys(pEntry, Key))
        {
            // Reached top level class for mdCurrent - return whether
            // or not pEntry is a top level class
            // (pEntry is a top level class if its pEncloser is NULL)
            if ((TypeFromToken(mdCurrent) != mdtExportedType) ||
                (mdCurrent == mdExportedTypeNil))
            {
                return pEntry->GetEncloser() == NULL;
            }
        }
        else // Keys don't match - wrong entry
        {
            return FALSE;
        }
    }
    while ((pEntry = pEntry->GetEncloser()) != NULL);

    // Reached the top level class for pEntry, but mdCurrent is nested
    return FALSE;
}

DWORD ComputeHashFunctionWithExportedType(EEClassHashTable * pClassHash, EEClassHashTable * pCaseSensitiveClassHash, IMDInternalImport *pImport, mdExportedType etCurrent, BOOL *pFailed)
{
    LPCSTR _namespace, name;
    if (FAILED(pImport->GetExportedTypeProps(
        etCurrent,
        &_namespace,
        &name,
        &etCurrent,
        NULL,   //binding (type def)
        NULL))) //flags
    {
        return FALSE;
    }
    DWORD hashEncloser = 0;
    if (TypeFromToken(etCurrent) == mdtExportedType && etCurrent != mdExportedTypeNil)
    {
        // The enclosing hash is always based on the entry from the case sensitive table
        hashEncloser = ComputeHashFunctionWithExportedType(pCaseSensitiveClassHash, pCaseSensitiveClassHash, pImport, etCurrent, pFailed);
    }
    return pClassHash->Hash(_namespace, name, hashEncloser);
}

BOOL CompareNestedEntryWithTypeDef(IMDInternalImport *  pImport,
                                                mdTypeDef            mdCurrent,
                                                EEClassHashTable *   pClassHash,
                                                PTR_EEClassHashEntry pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCUTF8 Key[2];

    do {
        if (FAILED(pImport->GetNameOfTypeDef(mdCurrent, &Key[1], &Key[0])))
        {
            return FALSE;
        }

        if (pClassHash->CompareKeys(pEntry, Key)) {
            // Reached top level class for mdCurrent - return whether
            // or not pEntry is a top level class
            // (pEntry is a top level class if its pEncloser is NULL)
            if (FAILED(pImport->GetNestedClassProps(mdCurrent, &mdCurrent)))
                return pEntry->GetEncloser() == NULL;
        }
        else // Keys don't match - wrong entry
            return FALSE;
    }
    while ((pEntry = pEntry->GetEncloser()) != NULL);

    // Reached the top level class for pEntry, but mdCurrent is nested
    return FALSE;
}

DWORD ComputeHashFunctionWithTypeDef(EEClassHashTable *pClassHash, EEClassHashTable *pCaseSensitiveClassHash, IMDInternalImport *pImport, mdTypeDef tdCurrent, BOOL *pFailed)
{
    LPCSTR _namespace, name;
    if (FAILED(pImport->GetNameOfTypeDef(tdCurrent, &name, &_namespace)))
    {
        *pFailed = TRUE;
        return 0;
    }
    DWORD hashEncloser = 0;
    if (SUCCEEDED(pImport->GetNestedClassProps(tdCurrent, &tdCurrent)))
    {
        // The enclosing hash is always based on the entry from the case sensitive table
        hashEncloser = ComputeHashFunctionWithTypeDef(pCaseSensitiveClassHash, pCaseSensitiveClassHash, pImport, tdCurrent, pFailed);
    }
    return pClassHash->Hash(_namespace, name, hashEncloser);
}

BOOL CompareNestedEntryWithTypeRef(IMDInternalImport *  pImport,
                                                mdTypeRef            mdCurrent,
                                                EEClassHashTable *   pClassHash,
                                                PTR_EEClassHashEntry pEntry)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCUTF8 Key[2];

    do {
        if (FAILED(pImport->GetNameOfTypeRef(mdCurrent, &Key[0], &Key[1])))
        {
            return FALSE;
        }

        if (pClassHash->CompareKeys(pEntry, Key))
        {
            if (FAILED(pImport->GetResolutionScopeOfTypeRef(mdCurrent, &mdCurrent)))
            {
                return FALSE;
            }
            // Reached top level class for mdCurrent - return whether
            // or not pEntry is a top level class
            // (pEntry is a top level class if its pEncloser is NULL)
            if ((TypeFromToken(mdCurrent) != mdtTypeRef) ||
                (mdCurrent == mdTypeRefNil))
                return pEntry->GetEncloser() == NULL;
        }
        else // Keys don't match - wrong entry
            return FALSE;
    }
    while ((pEntry = pEntry->GetEncloser())!=NULL);

    // Reached the top level class for pEntry, but mdCurrent is nested
    return FALSE;
}


DWORD ComputeHashFunctionWithTypeRef(EEClassHashTable *pClassHash, EEClassHashTable *pCaseSensitiveClassHash, IMDInternalImport *pImport, mdTypeRef trCurrent, BOOL *pFailed)
{
    LPCSTR _namespace, name;
    if (FAILED(pImport->GetNameOfTypeRef(trCurrent, &_namespace, &name)))
    {
        *pFailed = TRUE;
        return 0;
    }
    DWORD hashEncloser = 0;
    if (SUCCEEDED(pImport->GetResolutionScopeOfTypeRef(trCurrent, &trCurrent)) && TypeFromToken(trCurrent) == mdtTypeRef)
    {
        // The enclosing hash is always based on the entry from the case sensitive table
        hashEncloser = ComputeHashFunctionWithTypeRef(pCaseSensitiveClassHash, pCaseSensitiveClassHash, pImport, trCurrent, pFailed);
    }
    return pClassHash->Hash(_namespace, name, hashEncloser);
}

/*static*/
BOOL EEClassHashTable::IsNested(ModuleBase *pModule, mdToken token, mdToken *mdEncloser)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    switch(TypeFromToken(token)) {
        case mdtTypeDef:
            return (SUCCEEDED(pModule->GetMDImport()->GetNestedClassProps(token, mdEncloser)));

        case mdtTypeRef:
            IfFailThrow(pModule->GetMDImport()->GetResolutionScopeOfTypeRef(token, mdEncloser));
            return ((TypeFromToken(*mdEncloser) == mdtTypeRef) &&
                    (*mdEncloser != mdTypeRefNil));

        case mdtExportedType:
            _ASSERTE(pModule->IsFullModule());
            IfFailThrow(((Module*)pModule)->GetAssembly()->GetMDImport()->GetExportedTypeProps(
                token,
                NULL,   // namespace
                NULL,   // name
                mdEncloser,
                NULL,   //binding (type def)
                NULL)); //flags
            return ((TypeFromToken(*mdEncloser) == mdtExportedType) &&
                    (*mdEncloser != mdExportedTypeNil));

        default:
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
    }
}

BOOL EEClassHashTable::IsNested(const NameHandle* pName, mdToken *mdEncloser)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (pName->GetTypeModule()) {
        if (TypeFromToken(pName->GetTypeToken()) == mdtBaseType)
        {
            if (!pName->GetBucket().IsNull())
                return TRUE;
            return FALSE;
        }
        else
            return IsNested(pName->GetTypeModule(), pName->GetTypeToken(), mdEncloser);
    }
    else
        return FALSE;
}

PTR_EEClassHashEntry EEClassHashTable::FindByNameHandle(const NameHandle* pName)
{
    // TODO remove this pointless local
    EEClassHashTable *pTable = this;

    PTR_EEClassHashEntry pBucket;
    mdToken mdEncloser;
    bool isNested = IsNested(pName, &mdEncloser);
    LPCUTF8 pszName = NULL, pszNamespace = NULL;
    DWORD hash;
    BOOL failed;

    _ASSERTE(pName->GetNameSpace() != NULL);
    _ASSERTE(pName->GetName() != NULL);

    pszName = pName->GetName();
    pszNamespace = pName->GetNameSpace();

    mdToken typeToken = pName->GetTypeToken();
    ModuleBase *pNameModule = pName->GetTypeModule();

    switch (TypeFromToken(typeToken))
    {
    case mdtTypeDef:
        PREFIX_ASSUME(pNameModule != NULL);
        hash = ComputeHashFunctionWithTypeDef(pTable, m_pCaseSensitiveTable, pNameModule->GetMDImport(), typeToken, &failed);
        break;
    case mdtTypeRef:
        PREFIX_ASSUME(pNameModule != NULL);
        hash = ComputeHashFunctionWithTypeRef(pTable, m_pCaseSensitiveTable, pNameModule->GetMDImport(), typeToken, &failed);
        break;
    case mdtExportedType:
        PREFIX_ASSUME(pNameModule != NULL);
        hash = ComputeHashFunctionWithExportedType(pTable, m_pCaseSensitiveTable, pNameModule->GetMDImport(), typeToken, &failed);
        break;
    default:
        DWORD enclosingHash;
        if (pName->GetBucket().IsNull())
        {
            enclosingHash = 0;
        }
        else
        {
            // The enclosing hash is always based on the entry from the case sensitive table
            // A NameHandle bucket is always the entry in the CaseSensitive table
            enclosingHash = GetHash(pName->GetBucket().GetClassHashBasedEntryValue());
        }
        hash = Hash(pszNamespace, pszName, enclosingHash);
        break;
    }

    EEClassHashTable::LookupContext lookupContext;
    pBucket = pTable->BaseFindFirstEntryByHash(hash, &lookupContext);
    LPCUTF8 key[] = {pszNamespace, pszName};
    while (pBucket != NULL)
    {
        if (pTable->CompareKeys(pBucket, key))
        {
            if ((pBucket->GetEncloser() != NULL) == isNested)
            {
                if (isNested)
                {
                    bool hasNameBucket = !pName->GetBucket().IsNull();
#ifdef _DEBUG
                    bool expectedMatchCheck = !hasNameBucket || (pBucket->GetEncloser() == pName->GetBucket().GetClassHashBasedEntryValue());
                    bool expectedNotMatchCheck = !hasNameBucket || (pBucket->GetEncloser() != pName->GetBucket().GetClassHashBasedEntryValue());
#endif
#ifndef _DEBUG
                    // In non-debug builds, we can simply check the encloser in the name first. If it matches then we've found the right
                    // result, and if it doesn't match it also indicates that it shouldn't match. We only do this in non-debug builds
                    // as we want to validate via asserts that this code is correct.
                    if (!pName->GetBucket().IsNull())
                    {
                        if (pBucket->GetEncloser() == pName->GetBucket().GetClassHashBasedEntryValue())
                        {
                            // We found our result
                            break;
                        }
                    } else
#endif // !_DEBUG
                    if (TypeFromToken(typeToken) == mdtTypeDef)
                    {
                        if (CompareNestedEntryWithTypeDef(pNameModule->GetMDImport(),
                                                            mdEncloser,
                                                            this,
                                                            pBucket->GetEncloser()))
                        {
                            _ASSERTE(expectedMatchCheck);
                            // We found our result
                            break;
                        }
                        _ASSERTE(expectedNotMatchCheck);
                    }
                    else if (TypeFromToken(typeToken) == mdtTypeRef)
                    {
                        if (CompareNestedEntryWithTypeRef(pNameModule->GetMDImport(),
                                                            mdEncloser,
                                                            this,
                                                            pBucket->GetEncloser()))
                        {
                            _ASSERTE(expectedMatchCheck);
                            // We found our result
                            break;
                        }
                        _ASSERTE(expectedNotMatchCheck);
                    }
                    else if (TypeFromToken(typeToken) == mdtExportedType)
                    {
                        if (CompareNestedEntryWithExportedType(pNameModule->GetMDImport(),
                                                            mdEncloser,
                                                            this,
                                                            pBucket->GetEncloser()))
                        {
                            _ASSERTE(expectedMatchCheck);
                            // We found our result
                            break;
                        }
                        _ASSERTE(expectedNotMatchCheck);
                    }
                    else
                    {
                        // String based lookup always set the encloser bucket on the name. We will only
                        // hit this particular block in debug builds
                        if (pBucket->GetEncloser() == pName->GetBucket().GetClassHashBasedEntryValue())
                        {
                            // We found our result
                            break;
                        }
                    }
                }
                else
                {
                    // We found our result
                    break;
                }
            }
        }
        pBucket = pTable->BaseFindNextEntryByHash(&lookupContext);
    }
    return pBucket;
}
