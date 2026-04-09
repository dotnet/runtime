// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CeeSectionString.cpp
//

//
// ===========================================================================
#include "stdafx.h"

struct StringTableEntry {
    ULONG m_hashId;
    int m_offset;
    StringTableEntry *m_next;
};

CeeSectionString::CeeSectionString(CCeeGen &ceeFile, CeeSectionImpl &impl)
    : CeeSection(ceeFile, impl)
{
    memset(stringTable, 0, sizeof(stringTable));
}

void CeeSectionString::deleteEntries(StringTableEntry *e)
{
    if (!e)
        return;
    deleteEntries(e->m_next);
    delete e;
}

CeeSectionString::~CeeSectionString()
{
    for (int i=0; i < MaxRealEntries; i++)
        deleteEntries(stringTable[i]);
}

StringTableEntry* CeeSectionString::createEntry(_In_z_ LPWSTR target, ULONG hashId)
{
    StringTableEntry *entry = new (nothrow) StringTableEntry;
    if (!entry)
        return NULL;
    entry->m_next = NULL;
    entry->m_hashId = hashId;
    entry->m_offset = dataLen();
    size_t len = (u16_strlen(target)+1) * sizeof(WCHAR);
    if (len > UINT32_MAX) {
        delete entry;
        return NULL;
    }
    void *buf = getBlock((ULONG)len);
    if (!buf) {
        delete entry;
        return NULL;
    }
    memcpy(buf, target, len);
    return entry;
}

// Searches through the linked list looking for a match on hashID. If
// multiple elements hash to the same value, a strcmp must be done to
// check for match. The goal is to have very large hashId space so that
// string compares are minimized
StringTableEntry *CeeSectionString::findStringInsert(
                        StringTableEntry *&head, _In_z_ LPWSTR target, ULONG hashId)
{
    StringTableEntry *cur, *prev;
    cur = prev = head;
    while (cur && cur->m_hashId < hashId) {
        prev = cur;
        cur = cur->m_next;
    }
    while (cur && cur->m_hashId == hashId) {
        if (u16_strcmp(target, (LPWSTR)(computePointer(cur->m_offset))) == 0)
            return cur;
        prev = cur;
        cur = cur->m_next;
    }
    // didn't find in chain so insert at prev
    StringTableEntry *entry = createEntry(target, hashId);
    if (cur == head) {
        head = entry;
        entry->m_next = prev;
    } else {
        prev->m_next = entry;
        entry->m_next = cur;
    }
    return entry;
}

HRESULT CeeSectionString::getEmittedStringRef(_In_z_ LPWSTR target, StringRef *ref)
{
    TESTANDRETURN(ref!=NULL, E_POINTER);
    ULONG hashId = HashString(target) % MaxVirtualEntries;
    ULONG bucketIndex = hashId / MaxRealEntries;

    StringTableEntry *entry;
    entry = findStringInsert(stringTable[bucketIndex], target, hashId);

    if (! entry)
        return E_OUTOFMEMORY;
    *ref = entry->m_offset;
    return S_OK;
}
