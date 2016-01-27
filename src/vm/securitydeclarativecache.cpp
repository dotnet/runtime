// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"
#include "appdomain.inl"
#include "security.h"
#include "field.h"
#include "comcallablewrapper.h"
#include "typeparse.h"


//
//----------------------------------------------------
//
//Brief design overview:
//
//Essentially we moved away from the old scheme of a per-process hash table for blob->index mapping, 
//and a growable per appdomain array containing the managed objects. The new scheme has a per 
//appdomain hash that does memory allocs from the appdomain heap. The hash table maps the metadata 
//blob to a data structure called PsetCacheEntry. PsetCacheEntry has the metadata blob and a handle 
//to the managed pset object. It is the central place where caching/creation of the managed pset 
//objects happen. Essentially whenever we see a new decl security blob, we insert it into the 
//appdomain hash (if it's not already there). The object is lazily created as needed (we let 
//threads race for object creation). 
//
//----------------------------------------------------
//

BOOL PsetCacheKey::IsEquiv(PsetCacheKey *pOther)
{
    WRAPPER_NO_CONTRACT;
    if (m_cbPset != pOther->m_cbPset || !m_pbPset || !pOther->m_pbPset)
        return FALSE;
    return memcmp(m_pbPset, pOther->m_pbPset, m_cbPset) == 0;
}

DWORD PsetCacheKey::Hash()
{
    LIMITED_METHOD_CONTRACT;
    DWORD dwHash = 0;
    for (DWORD i = 0; i < (m_cbPset / sizeof(DWORD)); i++)
        dwHash ^= GET_UNALIGNED_VAL32(&((DWORD*)m_pbPset)[i]);
    return dwHash;
}

void PsetCacheEntry::Init (PsetCacheKey *pKey, AppDomain *pDomain)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;  // From CreateHandle()
        MODE_COOPERATIVE;
    } 
    CONTRACTL_END;
    
    m_pKey = pKey;
    m_eCanUnrestrictedOverride = CUO_DontKnow;
    m_fEmptyPermissionSet = false;
#ifndef CROSSGEN_COMPILE
    m_handle = pDomain->CreateHandle(NULL);
#endif // CROSSGEN_COMPILE
}

#ifndef CROSSGEN_COMPILE
OBJECTREF PsetCacheEntry::CreateManagedPsetObject(DWORD dwAction, bool createEmptySet /* = false */)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    OBJECTREF orRet;

    orRet = GetManagedPsetObject();
    if (orRet != NULL) {
        return orRet;
    }

    if (!createEmptySet && m_fEmptyPermissionSet) {
        return NULL;
    }

    struct _gc {
        OBJECTREF pset;
        OBJECTREF encoding;
        OBJECTREF nonCasPset;
        OBJECTREF orNonCasPset;
        OBJECTREF orNonCasEncoding;
    } gc;
    memset(&gc, 0, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    if ( (m_pKey->m_cbPset > 0) && (m_pKey->m_pbPset[0] == LAZY_DECL_SEC_FLAG) ) {

        SecurityAttributes::AttrSetBlobToPermissionSets(m_pKey->m_pbPset, 
                                                        m_pKey->m_cbPset, 
                                                        &gc.pset, 
                                                        dwAction);
        
    } else {

#ifdef FEATURE_CAS_POLICY
        SecurityAttributes::XmlToPermissionSet(m_pKey->m_pbPset,
                                               m_pKey->m_cbPset,  
                                               &gc.pset, 
                                               &gc.encoding, 
                                               NULL, 
                                               0, 
                                               &gc.orNonCasPset, 
                                               &gc.orNonCasEncoding);
#else
        // The v1.x serialized permission set format is not supported on CoreCLR
        COMPlusThrowHR(CORSECATTR_E_BAD_ATTRIBUTE);
#endif //FEATURE_CAS_POLICY
    }

    StoreFirstObjectInHandle(m_handle, gc.pset);

    if (gc.pset == NULL)
        m_fEmptyPermissionSet = true;

    GCPROTECT_END();
    
    //
    // Some other thread may have won the race, and stored away a different
    // object in the handle.
    //

    orRet = GetManagedPsetObject();
    return orRet;
}
#endif // CROSSGEN_COMPILE

bool PsetCacheEntry::ContainsBuiltinCASPermsOnly (DWORD dwAction)
{
 
    if (m_eCanUnrestrictedOverride == CUO_Yes) {
        return true;
    }
    
    if (m_eCanUnrestrictedOverride == CUO_No) {
        return false;
    }
    
    bool bRet = ContainsBuiltinCASPermsOnlyInternal(dwAction);
    
    //
    // Cache the results.
    //

    if(bRet) {
        m_eCanUnrestrictedOverride = CUO_Yes;
    } else {
        m_eCanUnrestrictedOverride = CUO_No;
    }
    
    return bRet;
}

bool PsetCacheEntry::ContainsBuiltinCASPermsOnlyInternal(DWORD dwAction)
{
    //
    // Deserialize the CORSEC_ATTRSET
    //

    CORSEC_ATTRSET attrSet;
    HRESULT hr = BlobToAttributeSet(m_pKey->m_pbPset, m_pKey->m_cbPset, &attrSet, dwAction);
    
    if(FAILED(hr)) {
        COMPlusThrowHR(hr);
    }

    if (hr == S_FALSE) {
        //
        // BlobToAttributeSet didn't work as expected - bail out early
        //
        return FALSE; 
    }

    // Check the attributes
    return SecurityAttributes::ContainsBuiltinCASPermsOnly(&attrSet);
}

void SecurityDeclarativeCache::Init(LoaderHeap *pHeap) 
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

	_ASSERTE (pHeap);

    m_pHeap = pHeap;
	
    m_pCachedPsetsHash = new EEPsetHashTable;

    m_prCachedPsetsLock = new SimpleRWLock (COOPERATIVE_OR_PREEMPTIVE, 
                                            LOCK_TYPE_DEFAULT);

    if (!m_pCachedPsetsHash->Init(19, &g_lockTrustMeIAmThreadSafe, m_pHeap)) {
        ThrowOutOfMemory();
    }
}

PsetCacheEntry* SecurityDeclarativeCache::CreateAndCachePset(
    IN PBYTE pbAttrBlob,
    IN DWORD cbAttrBlob
    )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    PsetCacheEntry *pPCE;
    LoaderHeap *pHeap;
    SimpleWriteLockHolder writeLockHolder(m_prCachedPsetsLock);

    //
    // Check for Duplicates.
    //

    pPCE = GetCachedPsetWithoutLocks (pbAttrBlob, cbAttrBlob);
    if (pPCE) {
        return pPCE;
    }

    AppDomain *pDomain;
    PsetCacheKey *pKey;
    HashDatum datum;

    //
    // Buffer permission set blob (it might go away if the metadata scope it
    // came from is closed).
    //

    pDomain = GetAppDomain ();
    pHeap = pDomain->GetLowFrequencyHeap ();

    pKey = (PsetCacheKey*) ((void*) pHeap->AllocMem ((S_SIZE_T)sizeof(PsetCacheKey)));

    pKey->Init (pbAttrBlob, cbAttrBlob, TRUE, pHeap);


    
    pPCE = (PsetCacheEntry*) 
        ((void*) pHeap->AllocMem ((S_SIZE_T)sizeof(PsetCacheEntry)));

    pPCE->Init (pKey, pDomain);

    datum = reinterpret_cast<HashDatum>(pPCE);
    m_pCachedPsetsHash->InsertValue (pKey, datum);

    return pPCE;
}

PsetCacheEntry* SecurityDeclarativeCache::GetCachedPset(IN PBYTE pbAttrBlob,
                                                        IN DWORD cbAttrBlob
    )
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    PsetCacheEntry *pPCE;
    SimpleReadLockHolder readLockHolder(m_prCachedPsetsLock);

    pPCE = GetCachedPsetWithoutLocks(pbAttrBlob, cbAttrBlob);
    return pPCE;
}

PsetCacheEntry* SecurityDeclarativeCache::GetCachedPsetWithoutLocks(
    IN PBYTE pbAttrBlob,
    IN DWORD cbAttrBlob
    )
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    PsetCacheKey sKey;
    PsetCacheEntry *pPCE;
    BOOL found;
    HashDatum datum;

    sKey.Init (pbAttrBlob, cbAttrBlob, FALSE, NULL);

    found = m_pCachedPsetsHash->GetValue(&sKey, &datum);

    if (found) {
        pPCE = reinterpret_cast<PsetCacheEntry*>(datum);
        return pPCE;
    } else {
        return NULL;
    }
}

SecurityDeclarativeCache::~SecurityDeclarativeCache() 
{
    WRAPPER_NO_CONTRACT;

    // Destroy the hash table even if entries are allocated from 
    // appdomain heap: the hash table may have used non heap memory for internal data structures
    if (m_pCachedPsetsHash)
    {
        delete m_pCachedPsetsHash;
    }

    if (m_prCachedPsetsLock) 
    {
        delete m_prCachedPsetsLock;
    }
}































