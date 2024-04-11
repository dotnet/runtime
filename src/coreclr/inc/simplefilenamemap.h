// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//=============================================================================================
// Data structures for Simple Name -> File Name hash

#ifndef __SIMPLEFILENAMEMAP_H_
#define __SIMPLEFILENAMEMAP_H_

#include "clrtypes.h"
#include "shash.h"

// Entry in SHash table that maps namespace to list of files
struct SimpleNameToFileNameMapEntry
{
    LPWSTR m_wszSimpleName;
    LPWSTR m_wszILFileName;
};

// SHash traits for Namespace -> FileNameList hash
class SimpleNameToFileNameMapTraits : public NoRemoveSHashTraits< DefaultSHashTraits< SimpleNameToFileNameMapEntry > >
{
    public:
    typedef PCWSTR key_t;
    static const SimpleNameToFileNameMapEntry Null() { SimpleNameToFileNameMapEntry e; e.m_wszSimpleName = nullptr; return e; }
    static bool IsNull(const SimpleNameToFileNameMapEntry & e) { return e.m_wszSimpleName == nullptr; }
    static key_t GetKey(const SimpleNameToFileNameMapEntry & e)
    {
        key_t key;
        key = e.m_wszSimpleName;
        return key;
    }
    static count_t Hash(const key_t &str)
    {
        SString ssKey(SString::Literal, str);
        return ssKey.HashCaseInsensitive();
    }
    static BOOL Equals(const key_t &lhs, const key_t &rhs) { LIMITED_METHOD_CONTRACT; return (SString::_wcsicmp(lhs, rhs) == 0); }

    void OnDestructPerEntryCleanupAction(const SimpleNameToFileNameMapEntry & e)
    {
        if (e.m_wszILFileName == nullptr)
        {
            // Don't delete simple name here since it's a filename only entry and will be cleaned up
            // by the SimpleName -> FileName entry which reuses the same filename pointer.
            return;
        }

        if (e.m_wszSimpleName != nullptr)
        {
            delete [] e.m_wszSimpleName;
        }
        if (e.m_wszILFileName != nullptr)
        {
            delete [] e.m_wszILFileName;
        }
    }
    static const bool s_DestructPerEntryCleanupAction = true;
};

typedef SHash<SimpleNameToFileNameMapTraits> SimpleNameToFileNameMap;

#endif // __SIMPLEFILENAMEMAP_H_
