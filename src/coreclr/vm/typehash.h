// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: typehash.h
//

//

#ifndef _TYPE_HASH_H
#define _TYPE_HASH_H

#include "ngenhash.h"

//========================================================================================
// This hash table is used by class loaders to look up constructed types:
// arrays, pointers and instantiations of user-defined generic types.
//
// Each persisted module structure has an EETypeHashTable used for constructed types that
// were ngen'ed into that module. See ceeload.hpp for more information about ngen modules.
//
// Types created at runtime are placed in an EETypeHashTable in BaseDomain.
//
// Keys are derivable from the data stored in the table (TypeHandle)
// - for an instantiated type, the typedef module, typedef token, and instantiation
// - for an array/pointer type, the CorElementType, rank, and type parameter
//
//========================================================================================

// One of these is present for each element in the table
// It simply chains together (hash,data) pairs
typedef DPTR(struct EETypeHashEntry) PTR_EETypeHashEntry;
typedef struct EETypeHashEntry
{
    TypeHandle GetTypeHandle();
    void SetTypeHandle(TypeHandle handle);

#ifndef DACCESS_COMPILE
    EETypeHashEntry& operator=(const EETypeHashEntry& src)
    {
        m_data = src.m_data;

        return *this;
    }
#endif // !DACCESS_COMPILE

    PTR_VOID GetData()
    {
        return m_data;
    }

private:
    friend class EETypeHashTable;

    PTR_VOID m_data;
} EETypeHashEntry_t;


// The type hash table itself
typedef DPTR(class EETypeHashTable) PTR_EETypeHashTable;
class EETypeHashTable : public NgenHashTable<EETypeHashTable, EETypeHashEntry, 2>
{

public:
    // This is the domain in which the hash table is allocated
    PTR_LoaderAllocator  m_pAllocator;

#ifdef _DEBUG
private:
    Volatile<LONG>  m_dwSealCount; // Can more types be added to the table?

public:
    void            InitUnseal() { LIMITED_METHOD_CONTRACT; m_dwSealCount = 0; }
    bool            IsUnsealed() { LIMITED_METHOD_CONTRACT; return (m_dwSealCount == 0); }
    void            Seal()   { LIMITED_METHOD_CONTRACT; FastInterlockIncrement(&m_dwSealCount); }
    void            Unseal() { LIMITED_METHOD_CONTRACT; FastInterlockDecrement(&m_dwSealCount); }
#endif  // _DEBUG

private:
#ifndef DACCESS_COMPILE
    EETypeHashTable();
    ~EETypeHashTable();
#endif
public:
    static EETypeHashTable *Create(LoaderAllocator *pAllocator, Module *pModule, DWORD dwNumBuckets, AllocMemTracker *pamTracker);

private:
    friend class NgenHashTable<EETypeHashTable, EETypeHashEntry, 2>;

#ifndef DACCESS_COMPILE
    EETypeHashTable(Module *pModule, LoaderHeap *pHeap, DWORD cInitialBuckets) :
        NgenHashTable<EETypeHashTable, EETypeHashEntry, 2>(pModule, pHeap, cInitialBuckets) {}
#endif
    void               operator delete(void *p);

public:
    // Insert a value in the hash table, key implicit in data
    // Value must not be present in the table already
    VOID InsertValue(TypeHandle data);

    // Look up a value in the hash table, key explicit in pKey
    // Return a null type handle if not found
    TypeHandle GetValue(TypeKey* pKey);

    BOOL ContainsValue(TypeHandle th);

    // An iterator for the table
    class Iterator
    {
    public:
        // This iterator can be reused for walking different tables
        void Reset();
        Iterator();

        Iterator(EETypeHashTable * pTable);
        ~Iterator();

    private:
        friend class EETypeHashTable;

        void Init();

        EETypeHashTable    *m_pTable;
        BaseIterator        m_sIterator;
        bool                m_fIterating;
    };

    BOOL FindNext(Iterator *it, EETypeHashEntry **ppEntry);

    DWORD GetCount();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    void EnumMemoryRegionsForEntry(EETypeHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags);
#endif

private:
    EETypeHashEntry_t * FindItem(TypeKey* pKey);
    BOOL CompareInstantiatedType(TypeHandle t, Module *pModule, mdTypeDef token, Instantiation inst);
    BOOL CompareFnPtrType(TypeHandle t, BYTE callConv, DWORD numArgs, TypeHandle *retAndArgTypes);
    BOOL GrowHashTable();
    LoaderAllocator* GetLoaderAllocator();
};

#endif /* _TYPE_HASH_H */

