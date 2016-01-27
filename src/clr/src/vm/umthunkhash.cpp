// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: umthunkhash.cpp
// 

//


#include "common.h"
#include "umthunkhash.h"

#ifdef FEATURE_MIXEDMODE

UMThunkHash::UMThunkHash(Module *pModule, AppDomain *pDomain) :
                    CClosedHashBase(
#ifdef _DEBUG
                     3,
#else
                    17,    // CClosedHashTable will grow as necessary
#endif

                    sizeof(UTHEntry),
                    FALSE
                    ),
    m_crst(CrstUMThunkHash)

{
    WRAPPER_NO_CONTRACT;
    m_pModule = pModule;
    m_dwAppDomainId = pDomain->GetId();
}

UMThunkHash::~UMThunkHash()
{
    CONTRACT_VOID
    {
        NOTHROW;
        DESTRUCTOR_CHECK;
        GC_TRIGGERS;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACT_END
        
#ifndef DACCESS_COMPILE
    UTHEntry *phe = (UTHEntry*)GetFirst();
    while (phe) {
        DeleteExecutable(phe->m_pUMEntryThunk);
        phe->m_UMThunkMarshInfo.~UMThunkMarshInfo();
        phe = (UTHEntry*)GetNext((BYTE*)phe);
    }
#endif
    RETURN;
}

LPVOID UMThunkHash::GetUMThunk(LPVOID pTarget, PCCOR_SIGNATURE pSig, DWORD cSig)            
{
    CONTRACT (LPVOID)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END
    
    UTHEntry *phe;
    CrstHolder ch(&m_crst);
    
    UTHKey key;
    key.m_pTarget = pTarget;
    key.m_pSig    = pSig;
    key.m_cSig  = cSig;
    
    phe =(UTHEntry *)Find((LPVOID)&key);
#ifndef DACCESS_COMPILE
    if (phe == NULL)
    {
        NewExecutableHolder<UMEntryThunk> uET= new (executable) UMEntryThunk();
            
        bool bNew = FALSE;
        phe = (UTHEntry *)FindOrAdd((LPVOID)&key,bNew);
        if (phe != NULL)
        {
            _ASSERTE(bNew); // we are under lock
                
            phe->m_pUMEntryThunk=uET.Extract();
            
            //nothrow
            phe->m_UMThunkMarshInfo.LoadTimeInit(Signature(pSig, cSig), m_pModule);
            phe->m_pUMEntryThunk->LoadTimeInit((PCODE)pTarget, NULL, &(phe->m_UMThunkMarshInfo), 
                                               MethodTable::GetMethodDescForSlotAddress((PCODE)pTarget), 
                                               m_dwAppDomainId);

            phe->m_key = key;
            phe->m_status = USED;
        }
    }
#endif //DACESS_COMPILE
    if (phe)
        RETURN (LPVOID)(phe->m_pUMEntryThunk->GetCode());
    else
        RETURN NULL;
}


unsigned int UMThunkHash::Hash(void const  *pData)                 
{
    LIMITED_METHOD_CONTRACT;
    UTHKey *pKey = (UTHKey*)pData;
    return (ULONG)(size_t)(pKey->m_pTarget);
}

inline unsigned int UMThunkHash::Compare(              // 0, -1, or 1.
                      void const  *pData,               // Raw key data on lookup.
                      BYTE        *pElement)            // The element to compare data against.
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    UTHKey *pkey1 = (UTHKey*)pData;
    UTHKey *pkey2 = &( ((UTHEntry*)pElement)->m_key );

    if (pkey1->m_pTarget != pkey2->m_pTarget)
        return 1;

    if (S_OK != MetaSig::CompareMethodSigsNT(pkey1->m_pSig, pkey1->m_cSig, m_pModule, NULL, pkey2->m_pSig, pkey2->m_cSig, m_pModule, NULL))
        return 1;

    return 0;
}



CClosedHashBase::ELEMENTSTATUS UMThunkHash::Status(           // The status of the entry.
    BYTE        *pElement)            // The element to check.
{
    LIMITED_METHOD_CONTRACT;
    return ((UTHEntry*)pElement)->m_status;
}

//*****************************************************************************
// Sets the status of the given element.
//*****************************************************************************
void UMThunkHash::SetStatus(
    BYTE        *pElement,              // The element to set status for.
    ELEMENTSTATUS eStatus)            // New status.
{
    LIMITED_METHOD_CONTRACT;
    ((UTHEntry*)pElement)->m_status = eStatus;
}

//*****************************************************************************
// Returns the internal key value for an element.
//*****************************************************************************
void *UMThunkHash::GetKey(                   // The data to hash on.
    BYTE        *pElement)            // The element to return data ptr for.
{
    LIMITED_METHOD_CONTRACT;
    return (BYTE*) &(((UTHEntry*)pElement)->m_key);
}

#endif // FEATURE_MIXEDMODE
