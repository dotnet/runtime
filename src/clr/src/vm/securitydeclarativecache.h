// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#ifndef __SecurityDecarativeCache_h__
#define __SecurityDecarativeCache_h__

struct PsetCacheKey
{
public:
    PBYTE m_pbPset;
    DWORD m_cbPset;
    BOOL m_bCopyArray;

    void Init (PBYTE pbPset, DWORD cbPset, BOOL CopyArray, LoaderHeap *pHeap) 
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_cbPset = cbPset;
        
        if (CopyArray) {
            m_pbPset = (PBYTE) ((void*)pHeap->AllocMem((S_SIZE_T)(cbPset * sizeof(BYTE)))) ;
            memcpy (m_pbPset, pbPset, cbPset);
        } else {
            m_pbPset = pbPset;
        }
    }
    
    BOOL IsEquiv(PsetCacheKey *pOther);
    DWORD Hash();
};

//
// Records a serialized permission set we've seen and decoded. 
//

enum CanUnrestrictedOverride
{
    CUO_DontKnow = 0,
    CUO_Yes = 1,
    CUO_No = 2,
};

class PsetCacheEntry
{
private:
    PsetCacheKey* m_pKey;
    OBJECTHANDLE m_handle;
    BYTE m_eCanUnrestrictedOverride;
    bool m_fEmptyPermissionSet;

    bool ContainsBuiltinCASPermsOnlyInternal(DWORD dwAction);

public:

    void Init(PsetCacheKey* pKey, AppDomain* pDomain);

    OBJECTREF CreateManagedPsetObject(DWORD dwAction, bool createEmptySet = false);
    
    OBJECTREF GetManagedPsetObject()
    {
        WRAPPER_NO_CONTRACT;
        return ObjectFromHandle(m_handle); 
    }

    bool ContainsBuiltinCASPermsOnly (DWORD dwAction);
    PsetCacheEntry() {m_pKey = NULL;}
    ~PsetCacheEntry()
    {
    	 if (m_pKey) { 
        	delete m_pKey;
    	}
    }
};



class SecurityDeclarativeCache {

private:
    EEPsetHashTable* m_pCachedPsetsHash;
    SimpleRWLock* m_prCachedPsetsLock;
    LoaderHeap* m_pHeap;

    PsetCacheEntry* GetCachedPsetWithoutLocks(IN PBYTE pbAttrBlob,
                                              IN DWORD cbAttrBlob
        );

public:
    void Init(LoaderHeap *pHeap);

    SecurityDeclarativeCache() :
       m_pCachedPsetsHash(NULL),
       m_prCachedPsetsLock(NULL),
       m_pHeap(NULL)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
    ~SecurityDeclarativeCache();

    PsetCacheEntry* CreateAndCachePset(IN PBYTE pbAttrBlob,
                                       IN DWORD cbAttrBlob
        );

    PsetCacheEntry* GetCachedPset(IN PBYTE pbAttrBlob,
                                  IN DWORD cbAttrBlob
        );

    
};

#endif















