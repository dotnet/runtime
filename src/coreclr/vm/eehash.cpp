// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eehash.cpp
//

//


#include "common.h"
#include "excep.h"
#include "eehash.h"
#include "stringliteralmap.h"
#include "clsload.hpp"
#include "typectxt.h"
#include "genericdict.h"

// ============================================================================
// UTF8 string hash table helper.
// ============================================================================
EEHashEntry_t * EEUtf8HashTableHelper::AllocateEntry(LPCUTF8 pKey, BOOL bDeepCopy, void *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    EEHashEntry_t *pEntry;

    if (bDeepCopy)
    {
        SIZE_T StringLen = strlen(pKey);
        SIZE_T BufLen = 0;
        if (!ClrSafeInt<SIZE_T>::addition(StringLen, SIZEOF_EEHASH_ENTRY + sizeof(LPUTF8) + 1, BufLen))
            return NULL;
        pEntry = (EEHashEntry_t *) new (nothrow) BYTE[BufLen];
        if (!pEntry)
            return NULL;

        memcpy(pEntry->Key + sizeof(LPUTF8), pKey, StringLen + 1);
        *((LPUTF8*)pEntry->Key) = (LPUTF8)(pEntry->Key + sizeof(LPUTF8));
    }
    else
    {
        pEntry = (EEHashEntry_t *) new (nothrow)BYTE[SIZEOF_EEHASH_ENTRY + sizeof(LPUTF8)];
        if (pEntry)
            *((LPCUTF8*)pEntry->Key) = pKey;
    }

    return pEntry;
}


void EEUtf8HashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, void *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    delete [] (BYTE*)pEntry;
}


BOOL EEUtf8HashTableHelper::CompareKeys(EEHashEntry_t *pEntry, LPCUTF8 pKey)
{
    LIMITED_METHOD_DAC_CONTRACT;

    LPCUTF8 pEntryKey = *((LPCUTF8*)pEntry->Key);
    return (strcmp(pEntryKey, pKey) == 0) ? TRUE : FALSE;
}


DWORD EEUtf8HashTableHelper::Hash(LPCUTF8 pKey)
{
    LIMITED_METHOD_DAC_CONTRACT;

    DWORD dwHash = 0;

    while (*pKey != 0)
    {
        dwHash = (dwHash << 5) + (dwHash >> 5) + (*pKey);
        pKey++;
    }

    return dwHash;
}


LPCUTF8 EEUtf8HashTableHelper::GetKey(EEHashEntry_t *pEntry)
{
    LIMITED_METHOD_CONTRACT;

    return *((LPCUTF8*)pEntry->Key);
}

#ifndef DACCESS_COMPILE

// ============================================================================
// Unicode string hash table helper.
// ============================================================================
EEHashEntry_t * EEUnicodeHashTableHelper::AllocateEntry(EEStringData *pKey, BOOL bDeepCopy, void *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    EEHashEntry_t *pEntry;

    if (bDeepCopy)
    {
        pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(EEStringData) + ((pKey->GetCharCount() + 1) * sizeof(WCHAR))];
        if (pEntry) {
            EEStringData *pEntryKey = (EEStringData *)(&pEntry->Key);
            pEntryKey->SetIsOnlyLowChars (pKey->GetIsOnlyLowChars());
            pEntryKey->SetCharCount (pKey->GetCharCount());
            pEntryKey->SetStringBuffer ((LPWSTR) ((LPBYTE)pEntry->Key + sizeof(EEStringData)));
            memcpy((LPWSTR)pEntryKey->GetStringBuffer(), pKey->GetStringBuffer(), pKey->GetCharCount() * sizeof(WCHAR));
        }
    }
    else
    {
        pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(EEStringData)];
        if (pEntry) {
            EEStringData *pEntryKey = (EEStringData *) pEntry->Key;
            pEntryKey->SetIsOnlyLowChars (pKey->GetIsOnlyLowChars());
            pEntryKey->SetCharCount (pKey->GetCharCount());
            pEntryKey->SetStringBuffer (pKey->GetStringBuffer());
        }
    }

    return pEntry;
}


void EEUnicodeHashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, void *pHeap)
{
    LIMITED_METHOD_CONTRACT;

    delete [] (BYTE*)pEntry;
}


BOOL EEUnicodeHashTableHelper::CompareKeys(EEHashEntry_t *pEntry, EEStringData *pKey)
{
    LIMITED_METHOD_CONTRACT;

    EEStringData *pEntryKey = (EEStringData*) pEntry->Key;

    // Same buffer, same string.
    if (pEntryKey->GetStringBuffer() == pKey->GetStringBuffer())
        return TRUE;

    // Length not the same, never a match.
    if (pEntryKey->GetCharCount() != pKey->GetCharCount())
        return FALSE;

    // Compare the entire thing.
    // We'll deliberately ignore the bOnlyLowChars field since this derived from the characters
    return !memcmp(pEntryKey->GetStringBuffer(), pKey->GetStringBuffer(), pEntryKey->GetCharCount() * sizeof(WCHAR));
}


DWORD EEUnicodeHashTableHelper::Hash(EEStringData *pKey)
{
    LIMITED_METHOD_CONTRACT;

    return (HashBytes((const BYTE *) pKey->GetStringBuffer(), pKey->GetCharCount()*sizeof(WCHAR)));
}


EEStringData *EEUnicodeHashTableHelper::GetKey(EEHashEntry_t *pEntry)
{
    LIMITED_METHOD_CONTRACT;

    return (EEStringData*)pEntry->Key;
}

void EEUnicodeHashTableHelper::ReplaceKey(EEHashEntry_t *pEntry, EEStringData *pNewKey)
{
    LIMITED_METHOD_CONTRACT;

    ((EEStringData*)pEntry->Key)->SetStringBuffer (pNewKey->GetStringBuffer());
    ((EEStringData*)pEntry->Key)->SetCharCount (pNewKey->GetCharCount());
    ((EEStringData*)pEntry->Key)->SetIsOnlyLowChars (pNewKey->GetIsOnlyLowChars());
}

// ============================================================================
// Unicode stringliteral hash table helper.
// ============================================================================
EEHashEntry_t * EEUnicodeStringLiteralHashTableHelper::AllocateEntry(EEStringData *pKey, BOOL bDeepCopy, void *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    // We assert here because we expect that the heap is not null for EEUnicodeStringLiteralHash table.
    // If someone finds more uses of this kind of hashtable then remove this asserte.
    // Also note that in case of heap being null we go ahead and use new /delete which is EXPENSIVE
    // But for production code this might be ok if the memory is fragmented then thers a better chance
    // of getting smaller allocations than full pages.
    _ASSERTE (pHeap);

    if (pHeap)
        return (EEHashEntry_t *) ((MemoryPool*)pHeap)->AllocateElementNoThrow ();
    else
        return (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY];
}


void EEUnicodeStringLiteralHashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, void *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // We assert here because we expect that the heap is not null for EEUnicodeStringLiteralHash table.
    // If someone finds more uses of this kind of hashtable then remove this asserte.
    // Also note that in case of heap being null we go ahead and use new /delete which is EXPENSIVE
    // But for production code this might be ok if the memory is fragmented then thers a better chance
    // of getting smaller allocations than full pages.
    _ASSERTE (pHeap);

    if (pHeap)
        ((MemoryPool*)pHeap)->FreeElement(pEntry);
    else
        delete [] (BYTE*)pEntry;
}


BOOL EEUnicodeStringLiteralHashTableHelper::CompareKeys(EEHashEntry_t *pEntry, EEStringData *pKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    GCX_COOP();

    StringLiteralEntry *pHashData = (StringLiteralEntry *)pEntry->Data;

    EEStringData pEntryKey;
    pHashData->GetStringData(&pEntryKey);

    // Length not the same, never a match.
    if (pEntryKey.GetCharCount() != pKey->GetCharCount())
        return FALSE;

    // Compare the entire thing.
    // We'll deliberately ignore the bOnlyLowChars field since this derived from the characters
    return (!memcmp(pEntryKey.GetStringBuffer(), pKey->GetStringBuffer(), pEntryKey.GetCharCount() * sizeof(WCHAR)));
}


DWORD EEUnicodeStringLiteralHashTableHelper::Hash(EEStringData *pKey)
{
    LIMITED_METHOD_CONTRACT;

    return (HashBytes((const BYTE *) pKey->GetStringBuffer(), pKey->GetCharCount() * sizeof(WCHAR)));
}


// ============================================================================
// Instantiation hash table helper.
// ============================================================================

EEHashEntry_t *EEInstantiationHashTableHelper::AllocateEntry(const SigTypeContext *pKey, BOOL bDeepCopy, AllocationHeap pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    EEHashEntry_t *pEntry = (EEHashEntry_t *) new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(SigTypeContext)];
    if (!pEntry)
        return NULL;
    *((SigTypeContext*)pEntry->Key) = *pKey;

    return pEntry;
}

void EEInstantiationHashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap pHeap)
{
    LIMITED_METHOD_CONTRACT;

    delete [] (BYTE*)pEntry;
}

BOOL EEInstantiationHashTableHelper::CompareKeys(EEHashEntry_t *pEntry, const SigTypeContext *pKey)
{
    LIMITED_METHOD_CONTRACT;

    SigTypeContext *pThis = (SigTypeContext*)&pEntry->Key;
    return SigTypeContext::Equal(pThis, pKey);
}

DWORD EEInstantiationHashTableHelper::Hash(const SigTypeContext *pKey)
{
    LIMITED_METHOD_CONTRACT;

    DWORD dwHash = 5381;
    DWORD i;

    for (i = 0; i < pKey->m_classInst.GetNumArgs(); i++)
        dwHash = ((dwHash << 5) + dwHash) ^ (unsigned int)(SIZE_T)pKey->m_classInst[i].AsPtr();

    for (i = 0; i < pKey->m_methodInst.GetNumArgs(); i++)
        dwHash = ((dwHash << 5) + dwHash) ^ (unsigned int)(SIZE_T)pKey->m_methodInst[i].AsPtr();

    return dwHash;
}

const SigTypeContext *EEInstantiationHashTableHelper::GetKey(EEHashEntry_t *pEntry)
{
    LIMITED_METHOD_CONTRACT;

    return (const SigTypeContext*)&pEntry->Key;
}



// ============================================================================
// ComComponentInfo hash table helper.
// ============================================================================

EEHashEntry_t *EEClassFactoryInfoHashTableHelper::AllocateEntry(ClassFactoryInfo *pKey, BOOL bDeepCopy, void *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return NULL;);
    }
    CONTRACTL_END

    EEHashEntry_t *pEntry;
    S_SIZE_T cbStringLen = S_SIZE_T(0);

    _ASSERTE(bDeepCopy && "Non deep copy is not supported by the EEComCompInfoHashTableHelper");

    if (pKey->m_strServerName)
        cbStringLen = (S_SIZE_T(u16_strlen(pKey->m_strServerName)) + S_SIZE_T(1)) * S_SIZE_T(sizeof(WCHAR));

    S_SIZE_T cbEntry = S_SIZE_T(SIZEOF_EEHASH_ENTRY + sizeof(ClassFactoryInfo)) + cbStringLen;

    if (cbEntry.IsOverflow())
        return NULL;

    _ASSERTE(!cbStringLen.IsOverflow());

    pEntry = (EEHashEntry_t *) new (nothrow) BYTE[cbEntry.Value()];
    if (pEntry) {
        memcpy(pEntry->Key + sizeof(ClassFactoryInfo), pKey->m_strServerName, cbStringLen.Value());
        ((ClassFactoryInfo*)pEntry->Key)->m_strServerName = pKey->m_strServerName ? (WCHAR*)(pEntry->Key + sizeof(ClassFactoryInfo)) : NULL;
        ((ClassFactoryInfo*)pEntry->Key)->m_clsid = pKey->m_clsid;
    }

    return pEntry;
}

void EEClassFactoryInfoHashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, void *pHeap)
{
    LIMITED_METHOD_CONTRACT;

    delete [] (BYTE*) pEntry;
}

BOOL EEClassFactoryInfoHashTableHelper::CompareKeys(EEHashEntry_t *pEntry, ClassFactoryInfo *pKey)
{
    LIMITED_METHOD_CONTRACT;

    // First check the GUIDs.
    if (((ClassFactoryInfo*)pEntry->Key)->m_clsid != pKey->m_clsid)
        return FALSE;

    // Next do a trivial comparison on the server name pointer values.
    if (((ClassFactoryInfo*)pEntry->Key)->m_strServerName == pKey->m_strServerName)
        return TRUE;

    // If the pointers are not equal then if one is NULL then the server names are different.
    if (!((ClassFactoryInfo*)pEntry->Key)->m_strServerName || !pKey->m_strServerName)
        return FALSE;

    // Finally do a string comparison of the server names.
    return u16_strcmp(((ClassFactoryInfo*)pEntry->Key)->m_strServerName, pKey->m_strServerName) == 0;
}

DWORD EEClassFactoryInfoHashTableHelper::Hash(ClassFactoryInfo *pKey)
{
    LIMITED_METHOD_CONTRACT;

    DWORD dwHash = 0;
    BYTE *pGuidData = (BYTE*)&pKey->m_clsid;

    for (unsigned int i = 0; i < sizeof(GUID); i++)
    {
        dwHash = (dwHash << 5) + (dwHash >> 5) + (*pGuidData);
        pGuidData++;
    }

    if (pKey->m_strServerName)
    {
        PCWSTR pSrvNameData = pKey->m_strServerName;

        while (*pSrvNameData != 0)
        {
            dwHash = (dwHash << 5) + (dwHash >> 5) + (*pSrvNameData);
            pSrvNameData++;
        }
    }

    return dwHash;
}

ClassFactoryInfo *EEClassFactoryInfoHashTableHelper::GetKey(EEHashEntry_t *pEntry)
{
    LIMITED_METHOD_CONTRACT;

    return (ClassFactoryInfo*)pEntry->Key;
}
#endif // !DACCESS_COMPILE
