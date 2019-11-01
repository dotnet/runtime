// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
** Header:  Map used for interning of string literals.
**
===========================================================*/

#ifndef _STRINGLITERALMAP_H
#define _STRINGLITERALMAP_H

#include "vars.hpp"
#include "appdomain.hpp"
#include "eehash.h"
#include "eeconfig.h" // For OS pages size
#include "memorypool.h"


class StringLiteralEntry;
// Allocate 16 entries (approx size sizeof(StringLiteralEntry)*16)
#define MAX_ENTRIES_PER_CHUNK 16

STRINGREF AllocateStringObject(EEStringData *pStringData);

// Loader allocator specific string literal map.
class StringLiteralMap
{
public:
    // Constructor and destructor.
    StringLiteralMap();
    ~StringLiteralMap();

    // Initialization method.
    void  Init();

    size_t GetSize()
    {
        LIMITED_METHOD_CONTRACT;
        return m_MemoryPool?m_MemoryPool->GetSize():0;
    }

    // Method to retrieve a string from the map.
    STRINGREF *GetStringLiteral(EEStringData *pStringData, BOOL bAddIfNotFound, BOOL bAppDomainWontUnload);

    // Method to explicitly intern a string object.
    STRINGREF *GetInternedString(STRINGREF *pString, BOOL bAddIfNotFound, BOOL bAppDomainWontUnload);

private:
    // Hash tables that maps a Unicode string to a COM+ string handle.
    EEUnicodeStringLiteralHashTable    *m_StringToEntryHashTable;

    // The memorypool for hash entries for this hash table.
    MemoryPool                  *m_MemoryPool;
};

// Global string literal map.
class GlobalStringLiteralMap
{
    // StringLiteralMap and StringLiteralEntry need to acquire the crst of the global string literal map.
    friend class StringLiteralMap;
    friend class StringLiteralEntry;

public:
    // Constructor and destructor.
    GlobalStringLiteralMap();
    ~GlobalStringLiteralMap();

    // Initialization method.
    void Init();

    // Method to retrieve a string from the map. Takes a precomputed hash (for perf).
    StringLiteralEntry *GetStringLiteral(EEStringData *pStringData, DWORD dwHash, BOOL bAddIfNotFound);

    // Method to explicitly intern a string object. Takes a precomputed hash (for perf).
    StringLiteralEntry *GetInternedString(STRINGREF *pString, DWORD dwHash, BOOL bAddIfNotFound);

    // Method to calculate the hash
    DWORD GetHash(EEStringData* pData)
    {
        WRAPPER_NO_CONTRACT;
        return m_StringToEntryHashTable->GetHash(pData);
    }

    // public method to retrieve m_HashTableCrstGlobal
    Crst* GetHashTableCrstGlobal() 
    {
        LIMITED_METHOD_CONTRACT;
        return &m_HashTableCrstGlobal;
    }

private:    
    // Helper method to add a string to the global string literal map.
    StringLiteralEntry *AddStringLiteral(EEStringData *pStringData);

    // Helper method to add an interned string.
    StringLiteralEntry *AddInternedString(STRINGREF *pString);

    // Called by StringLiteralEntry when its RefCount falls to 0.
    void RemoveStringLiteralEntry(StringLiteralEntry *pEntry);
    
    // Hash tables that maps a Unicode string to a LiteralStringEntry.
    EEUnicodeStringLiteralHashTable    *m_StringToEntryHashTable;

    // The memorypool for hash entries for this hash table.
    MemoryPool                  *m_MemoryPool;

    // The hash table table critical section.  
    // (the Global suffix is so that it is clear in context whether the global table is being locked 
    // or the per app domain table is being locked.  Sometimes there was confusion in the code
    // changing the name of the global one will avoid this problem and prevent copy/paste errors)
    
    Crst                        m_HashTableCrstGlobal;

    // The large heap handle table.
    LargeHeapHandleTable        m_LargeHeapHandleTable;

};

class StringLiteralEntryArray;

// Ref counted entry representing a string literal.
class StringLiteralEntry
{
private:
    StringLiteralEntry(EEStringData *pStringData, STRINGREF *pStringObj)
    : m_pStringObj(pStringObj), m_dwRefCount(1)
#ifdef _DEBUG
      , m_bDeleted(FALSE)
#endif
    {
        LIMITED_METHOD_CONTRACT;
    }
protected:
    ~StringLiteralEntry()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer<void>(this));
        }
        CONTRACTL_END;
    }

public:
    void AddRef()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer<void>(this));
            PRECONDITION((LONG)VolatileLoad(&m_dwRefCount) > 0);            
            PRECONDITION(SystemDomain::GetGlobalStringLiteralMapNoCreate()->m_HashTableCrstGlobal.OwnedByCurrentThread());            
        }
        CONTRACTL_END;

        _ASSERTE (!m_bDeleted);

        // We will keep the item alive forever if the refcount overflowed
        if ((LONG)VolatileLoad(&m_dwRefCount) < 0)
            return;

        VolatileStore(&m_dwRefCount, VolatileLoad(&m_dwRefCount) + 1);
    }
#ifndef DACCESS_COMPILE
    FORCEINLINE static void StaticRelease(StringLiteralEntry* pEntry)
    {        
        CONTRACTL
        {
            PRECONDITION(SystemDomain::GetGlobalStringLiteralMapNoCreate()->m_HashTableCrstGlobal.OwnedByCurrentThread());            
        }
        CONTRACTL_END;
        
        pEntry->Release();
    }
#else
    FORCEINLINE static void StaticRelease(StringLiteralEntry* /* pEntry */)
    {
        WRAPPER_NO_CONTRACT;
        DacNotImpl();
    }
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
    void Release()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer<void>(this));
            PRECONDITION(VolatileLoad(&m_dwRefCount) > 0);
            PRECONDITION(SystemDomain::GetGlobalStringLiteralMapNoCreate()->m_HashTableCrstGlobal.OwnedByCurrentThread());
        }
        CONTRACTL_END;

        // We will keep the item alive forever if the refcount overflowed
        if ((LONG)VolatileLoad(&m_dwRefCount) < 0)
            return;

        VolatileStore(&m_dwRefCount, VolatileLoad(&m_dwRefCount) - 1);
        if (VolatileLoad(&m_dwRefCount) == 0)
        {
            _ASSERTE(SystemDomain::GetGlobalStringLiteralMapNoCreate());
            SystemDomain::GetGlobalStringLiteralMapNoCreate()->RemoveStringLiteralEntry(this);
            // Puts this entry in the free list
            DeleteEntry (this);             
        }
    }
#endif // DACCESS_COMPILE
    
    LONG GetRefCount()
    {
        CONTRACTL
        {
            NOTHROW;
            if(GetThread()){GC_NOTRIGGER;}else{DISABLED(GC_TRIGGERS);};
            PRECONDITION(CheckPointer(this));
        }
        CONTRACTL_END;

        _ASSERTE (!m_bDeleted);

        return (VolatileLoad(&m_dwRefCount));
    }

    STRINGREF* GetStringObject()
    {
        CONTRACTL
        {
            NOTHROW;
            if(GetThread()){GC_NOTRIGGER;}else{DISABLED(GC_TRIGGERS);};
            PRECONDITION(CheckPointer(this));
        }
        CONTRACTL_END;
        return m_pStringObj;
    }

    void GetStringData(EEStringData *pStringData)
    {
        CONTRACTL
        {
            NOTHROW;
            if(GetThread()){GC_NOTRIGGER;}else{DISABLED(GC_TRIGGERS);};
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(this));
            PRECONDITION(CheckPointer(pStringData));
        }
        CONTRACTL_END;
            
        WCHAR *thisChars;
        int thisLength;

        ObjectToSTRINGREF(*(StringObject**)m_pStringObj)->RefInterpretGetStringValuesDangerousForGC(&thisChars, &thisLength);
        pStringData->SetCharCount (thisLength); // thisLength is in WCHARs and that's what EEStringData's char count wants
        pStringData->SetStringBuffer (thisChars);
    }

    static StringLiteralEntry *AllocateEntry(EEStringData *pStringData, STRINGREF *pStringObj);
    static void DeleteEntry (StringLiteralEntry *pEntry);

private:
    STRINGREF*                  m_pStringObj;
    union
    {
        DWORD                       m_dwRefCount;
        StringLiteralEntry         *m_pNext;
    };

#ifdef _DEBUG
    BOOL m_bDeleted;       
#endif

    // The static lists below are protected by GetGlobalStringLiteralMap()->m_HashTableCrstGlobal
    static StringLiteralEntryArray *s_EntryList; // always the first entry array in the chain. 
    static DWORD                    s_UsedEntries;   // number of entries used up in the first array
    static StringLiteralEntry      *s_FreeEntryList; // free list chained thru the arrays.
};

typedef Wrapper<StringLiteralEntry*,DoNothing,StringLiteralEntry::StaticRelease> StringLiteralEntryHolder; 

class StringLiteralEntryArray
{
public:
    StringLiteralEntryArray *m_pNext;
    BYTE                     m_Entries[MAX_ENTRIES_PER_CHUNK*sizeof(StringLiteralEntry)];
};

#endif // _STRINGLITERALMAP_H

